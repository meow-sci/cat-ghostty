# Design Document

## Overview

C# terminal emulator translating TypeScript caTTY for KSA game integration. Headless core (pure C#) + ImGui controller (KSA BRUTAL ImGui). Direct TypeScript translation maintaining API compatibility and behavior consistency.

## Architecture

### Project Structure
```
catty-ksa.sln
├── caTTY.Core/              # Headless terminal logic
├── caTTY.ImGui/             # ImGui display controller  
├── caTTY.ImGui.Playground/  # ImGui rendering experiments
├── caTTY.TestApp/           # Standalone BRUTAL ImGui test app
├── caTTY.GameMod/           # Game mod build target (DLL)
├── caTTY.Core.Tests/        # Unit and property tests for Core
└── caTTY.ImGui.Tests/       # Unit and property tests for ImGui
```

**Reference**: Use `KsaExampleMod/` for KSA integration patterns (project config, mod.toml, StarMap attributes, Harmony patching).

## Components and Interfaces

### Code Organization Principles
- **Modular Architecture**: Single responsibility, classes <400 lines, interface segregation, dependency injection
- **Parser Decomposition**: Main Parser coordinates specialized parsers (CsiParser, SgrParser, OscParser, EscParser, DcsParser, Utf8Decoder)
- **State Management**: TerminalEmulator coordinates focused managers (ScreenBufferManager, CursorManager, ScrollbackManager, AlternateScreenManager, ModeManager, AttributeManager)

### Core Interfaces

```csharp
public interface ITerminalEmulator : IDisposable
{
    int Width { get; } int Height { get; } int ScrollbackSize { get; }
    IScreenBuffer ScreenBuffer { get; } ICursor Cursor { get; } IScrollbackBuffer ScrollbackBuffer { get; }
    void Write(ReadOnlySpan<byte> data); void Write(string text); void Resize(int width, int height);
    event EventHandler<BellEventArgs> Bell; event EventHandler<TitleChangeEventArgs> TitleChanged;
    event EventHandler<ClipboardEventArgs> ClipboardRequest; event EventHandler<DataOutputEventArgs> DataOutput;
}

public interface IScreenBuffer
{
    int Width { get; } int Height { get; }
    ICell GetCell(int row, int col); void SetCell(int row, int col, ICell cell);
    void Clear(); void ScrollUp(int lines); void ScrollDown(int lines);
    ReadOnlySpan<ICell> GetRow(int row); void CopyTo(Span<ICell> destination, int startRow, int endRow);
}

public readonly struct Cell : ICell
{
    public char Character { get; } public SgrAttributes Attributes { get; }
    public bool IsWide { get; } public string? HyperlinkUrl { get; }
}

public readonly struct SgrAttributes
{
    public Color Foreground { get; } public Color Background { get; }
    public bool Bold { get; } public bool Italic { get; } public bool Underline { get; }
    public bool Strikethrough { get; } public bool Inverse { get; } public bool Dim { get; } public bool Blink { get; }
}
```

### Parser Interfaces

```csharp
public interface IParser { void PushBytes(ReadOnlySpan<byte> data); void PushByte(byte data); void FlushIncompleteSequences(); }
public interface ICsiParser { CsiMessage ParseCsiSequence(ReadOnlySpan<byte> sequence); bool TryParseParameters(ReadOnlySpan<char> parameterString, out int[] parameters); }
public interface ISgrParser { SgrSequence ParseSgrSequence(ReadOnlySpan<int> parameters, SgrAttributes current); SgrAttributes ApplyAttributes(SgrAttributes current, SgrMessage message); }
public interface IOscParser { OscMessage ParseOscSequence(ReadOnlySpan<byte> sequence, string terminator); bool TryParseCommand(ReadOnlySpan<char> payload, out int command, out string parameters); }
public interface IEscParser { EscMessage ParseEscSequence(ReadOnlySpan<byte> sequence); bool IsCharacterSetDesignation(ReadOnlySpan<byte> sequence); }
public interface IDcsParser { DcsMessage ParseDcsSequence(ReadOnlySpan<byte> sequence, string terminator); bool TryParseCommand(ReadOnlySpan<char> payload, out string command, out string[] parameters); }
public interface IUtf8Decoder { bool TryDecodeSequence(ReadOnlySpan<byte> bytes, out int codePoint, out int bytesConsumed); bool IsValidUtf8Start(byte b); int GetExpectedLength(byte startByte); }
```

**Parser Strategy**: Each specialized parser handles specific escape sequence types:
- **CsiParser**: CSI sequences (ESC [ ... final), parameter parsing, command identification
- **SgrParser**: SGR sequences (CSI ... m), color parsing, attribute management  
- **OscParser**: OSC sequences (ESC ] ... ST/BEL), command parsing, payload extraction
- **EscParser**: ESC sequences (ESC ...), character set designation, cursor operations
- **DcsParser**: DCS sequences (ESC P ... ST), device control, query responses
- **Utf8Decoder**: UTF-8 multi-byte sequences, validation, code point extraction

### Manager Interfaces

```csharp
public interface IScreenBufferManager
{
    int Width { get; } int Height { get; }
    ICell GetCell(int row, int col); void SetCell(int row, int col, ICell cell);
    void Clear(); void ClearRegion(int startRow, int startCol, int endRow, int endCol);
    void ScrollUp(int lines); void ScrollDown(int lines); void Resize(int width, int height);
    ReadOnlySpan<ICell> GetRow(int row); void CopyTo(Span<ICell> destination, int startRow, int endRow);
}

public interface ICursorManager
{
    int Row { get; set; } int Column { get; set; } bool Visible { get; set; }
    void MoveTo(int row, int col); void MoveUp(int lines); void MoveDown(int lines);
    void MoveLeft(int columns); void MoveRight(int columns);
    void SavePosition(); void RestorePosition(); void ClampToBuffer(int width, int height);
}

public interface IScrollbackManager
{
    int MaxLines { get; } int CurrentLines { get; } int ViewportOffset { get; set; }
    void AddLine(ReadOnlySpan<ICell> line); ReadOnlySpan<ICell> GetLine(int index);
    void Clear(); void SetViewportOffset(int offset); bool IsAtBottom { get; }
}

public interface IAlternateScreenManager
{
    bool IsAlternateActive { get; }
    void ActivateAlternate(); void DeactivateAlternate();
    IScreenBufferManager GetCurrentBuffer(); IScreenBufferManager GetPrimaryBuffer(); IScreenBufferManager GetAlternateBuffer();
}

public interface IModeManager
{
    bool AutoWrapMode { get; set; } bool ApplicationCursorKeys { get; set; } bool BracketedPasteMode { get; set; }
    bool CursorVisible { get; set; } bool OriginMode { get; set; } bool Utf8Mode { get; set; }
    void SetMode(int mode, bool enabled); bool GetMode(int mode); void SaveModes(); void RestoreModes();
}

public interface IAttributeManager
{
    SgrAttributes CurrentAttributes { get; set; }
    void ApplySgrMessage(SgrMessage message); void ResetAttributes(); SgrAttributes GetDefaultAttributes();
    void SetForegroundColor(Color color); void SetBackgroundColor(Color color); void SetTextStyle(bool bold, bool italic, bool underline);
}
```

**Manager Strategy**: Each manager focuses on specific terminal state aspects:
- **ScreenBufferManager**: 2D character grid, cell operations, buffer resizing
- **CursorManager**: Cursor position, visibility, movement operations
- **ScrollbackManager**: Scrollback buffer, viewport management, history navigation
- **AlternateScreenManager**: Primary/alternate buffer switching, state isolation
- **ModeManager**: Terminal mode state tracking (auto-wrap, cursor keys, etc.)
- **AttributeManager**: SGR attributes and their application to characters

### Performance-Optimized ImGui Controller

**Hot Path Optimization** (zero allocation required):
- `ImGuiTerminalController.Render()` - Called every frame
- `ScreenBuffer.GetRow()` - Called for each visible row
- `InputHandler.ProcessKeyboard()` - Called on every keystroke
- `Parser.ParseSequence()` - Called for every incoming byte

**Memory Architecture**:
- **Pre-Allocation**: StringBuilder, char arrays, render commands, color cache allocated once at init
- **Span-Based Access**: All screen buffer access uses spans to avoid copying
- **Object Pooling**: Reuse keyboard/mouse events, temporary arrays via ArrayPool<T>
- **Buffer Management**: Pre-allocate 2D cell arrays, cache row spans, implement copy-on-write

```csharp
public class ImGuiTerminalController : ITerminalController
{
    // Pre-allocated buffers (no allocations during rendering)
    private readonly StringBuilder _textBuilder = new(4096);
    private readonly char[] _renderBuffer = new char[8192];
    private readonly Dictionary<Color, uint> _colorCache = new(64);
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    
    public void Render()
    {
        if (!IsVisible) return;
        ImGui.Begin("Terminal", ref _isVisible);
        RenderTerminalContent(); // Uses spans and pre-allocated buffers
        ProcessInputEvents();    // Uses object pooling
        ImGui.End();
    }
}
```

### Font Configuration System

```csharp
public interface ITerminalController : IDisposable
{
    bool IsVisible { get; set; } bool HasFocus { get; }
    void Update(float deltaTime); void Render(); void HandleInput();
    void UpdateFontConfig(TerminalFontConfig fontConfig);
    event EventHandler<string> DataInput;
}

public class TerminalFontConfig
{
    public string RegularFontName { get; set; } = "HackNerdFontMono-Regular";
    public string BoldFontName { get; set; } = "HackNerdFontMono-Bold";
    public string ItalicFontName { get; set; } = "HackNerdFontMono-Italic";
    public string BoldItalicFontName { get; set; } = "HackNerdFontMono-BoldItalic";
    public float FontSize { get; set; } = 16.0f;
    public bool AutoDetectContext { get; set; } = true;
    
    public static TerminalFontConfig CreateForTestApp() => new() { FontSize = 16.0f, AutoDetectContext = false };
    public static TerminalFontConfig CreateForGameMod() => new() { FontSize = 14.0f, AutoDetectContext = false };
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(RegularFontName)) throw new ArgumentException("RegularFontName required");
        if (FontSize <= 0 || FontSize > 72) throw new ArgumentException("FontSize must be (0,72]");
        BoldFontName ??= RegularFontName; ItalicFontName ??= RegularFontName; BoldItalicFontName ??= RegularFontName;
    }
}

public static class FontContextDetector
{
    public static TerminalFontConfig DetectAndCreateConfig()
    {
        var hasKsaAssemblies = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName?.Contains("KSA") == true);
        return hasKsaAssemblies ? TerminalFontConfig.CreateForGameMod() : TerminalFontConfig.CreateForTestApp();
    }
}
```

### Process Management

```csharp
public interface IProcessManager : IDisposable
{
    bool IsRunning { get; }
    Task StartAsync(string command, string arguments = "");
    Task StopAsync();
    void Write(ReadOnlySpan<byte> data);
    void Resize(int width, int height);
    event EventHandler<DataReceivedEventArgs> DataReceived;
    event EventHandler ProcessExited;
}
```

## Data Models

```csharp
public class TerminalState : ITerminalState
{
    public IScreenBuffer PrimaryBuffer { get; } public IScreenBuffer AlternateBuffer { get; }
    public IScreenBuffer CurrentBuffer { get; private set; } public IScrollbackBuffer ScrollbackBuffer { get; }
    public ICursor Cursor { get; } public bool CursorVisible { get; set; }
    public bool AutoWrapMode { get; set; } public bool ApplicationCursorKeys { get; set; } public bool BracketedPasteMode { get; set; }
    public CharacterSet G0CharacterSet { get; set; } public CharacterSet G1CharacterSet { get; set; } public CharacterSet ActiveCharacterSet { get; set; }
    public ITabStops TabStops { get; } public int ScrollTop { get; set; } public int ScrollBottom { get; set; }
    public SgrAttributes CurrentAttributes { get; set; }
}

// Memory-efficient implementations use pre-allocated arrays, cached spans, object pooling
public class ScreenBuffer : IScreenBuffer
{
    private readonly Cell[,] _cells;
    private readonly ReadOnlyMemory<Cell>[] _rowMemories; // Pre-computed for zero-allocation GetRow()
    public ReadOnlySpan<Cell> GetRow(int row) => _rowMemories[row].Span;
}

public class ScrollbackBuffer : IScrollbackBuffer
{
    private readonly Cell[][] _lines;
    private readonly Queue<Cell[]> _recycledArrays = new(); // Object pooling for array reuse
    public void AddLine(ReadOnlySpan<Cell> line)
    {
        // Ring buffer + array reuse: copy into a reused array; when full, overwrite oldest and recycle replaced arrays.
    }
}
```

## Performance Optimization

**Hot Path Allocation Minimization**: Terminal designed for zero allocations during render loop and frequent operations.

**Critical Hot Paths** (zero allocation required):
- `ImGuiTerminalController.Render()` - Called every frame
- `ScreenBuffer.GetRow()` - Called for each visible row during rendering  
- `InputHandler.ProcessKeyboard()` - Called on every keystroke
- `Parser.ParseSequence()` - Called for every incoming byte sequence

**Memory Architecture Strategy**:
- **Pre-Allocation**: StringBuilder, char arrays, render commands, color cache allocated once at init, reused throughout lifetime
- **Span-Based Access**: All screen buffer access uses spans to avoid copying, string operations use span-based APIs
- **Object Pooling**: Reuse keyboard/mouse events, temporary arrays via ArrayPool<T>
- **Buffer Management**: Pre-allocate 2D cell arrays, cache row spans, implement copy-on-write for large region modifications

**Performance Guidelines**:
- **DO**: Use `ReadOnlySpan<T>` and `Span<T>` for hot paths, pre-allocate buffers/collections, implement object pooling, use `ArrayPool<T>`, cache expensive computations
- **DON'T**: Use LINQ in render loop, create temporary strings/collections in frequent methods, box value types, use string concatenation in hot paths, create delegates/lambdas in hot paths

## Correctness Properties

*Properties are formal statements about system behavior that should hold across all valid executions.*

### Key Properties (36 total)

**TypeScript Compatibility** (Properties 2-6): C# implementation must match TypeScript behavior for escape sequences, screen operations, cursor operations, scrollback behavior, and alternate screen operations.

**Core Terminal Behavior** (Properties 7-20): Screen buffer initialization/resize, cell data integrity, terminal size constraints, cursor management, character processing with UTF-8 support, line wrapping, control character processing, screen clearing/scrolling operations.

**Advanced Features** (Properties 21-31): SGR parsing/application/reset, OSC parsing/events/hyperlinks/unknown sequence handling, scrollback buffer management/viewport/access, alternate screen buffer switching/isolation/initialization.

**Font System** (Properties 32-35): Font configuration acceptance/application, context detection/defaults, runtime updates, style selection consistency.

**Line Operations** (Property 36): Line/character insertion/deletion with attribute preservation.

**Traceability (minimal)**:
- P2-6 → R3
- P7-11 → R7-R9
- P12-20 → R8-R11
- P21-22 → R12
- P23-25 → R13
- P26-28 → R14
- P29-31 → R15
- P32-35 → R32-R34
- P36 → R22

Each property validates specific requirements and enables property-based testing with minimum 100 iterations using FsCheck.NUnit.

## Error Handling

```csharp
public enum TerminalError { InvalidDimensions, InvalidSequence, BufferOverflow, ProcessError, ResourceDisposed }
public class TerminalException : Exception { public TerminalError ErrorType { get; } }
public interface ITerminalLogger { void LogError(string message, Exception? exception = null); void LogWarning(string message); void LogDebug(string message); }
```

**Error Recovery**: Invalid escape sequences → log and ignore; Buffer overflows → resize/truncate; Process failures → emit events, attempt reconnection; Resource exhaustion → cleanup and notify.

## Testing Strategy

### Dual Testing Approach
- **Unit tests**: Specific examples, edge cases, error conditions, game integration scenarios
- **Property tests**: Universal properties across all inputs using FsCheck.NUnit (minimum 100 iterations)
- Both approaches are complementary and necessary for comprehensive validation

### Test Organization
```
Tests/
├── caTTY.Core.Tests/
│   ├── Unit/ (TerminalEmulatorTests, ParserTests, ScreenBufferTests)
│   └── Property/ (TerminalBehaviorProperties, ParsingProperties, CompatibilityProperties)
├── caTTY.ImGui.Tests/
│   ├── Unit/ (TerminalControllerTests, RendererTests)
│   └── Integration/ (GameIntegrationTests)
└── Integration/ (ProcessManagementTests)
```

### TypeScript Compatibility Testing
**Critical Requirement**: C# implementation must match or exceed TypeScript test coverage (42+ test files).

**Coverage Areas**:
- **Parser Testing**: State integrity, CSI/SGR/OSC/DCS/ESC sequences, UTF-8 processing
- **Terminal Behavior**: Cursor operations, screen buffer operations, scrollback management, alternate screen, character processing, line operations
- **Advanced Features**: Tab stops, window manipulation, device queries, color systems, selection, hyperlinks, enhanced SGR modes, selective erase, character sets

**Property Test Format**: `[Property] // Feature: catty-ksa, Property {number}: {property_text}`

### Console Output Requirements
Unit tests MUST have no stdout/stderr output under normal conditions. Output only when: test fails (diagnostic info needed), explicit debugging enabled, critical errors occur.

### Performance Testing
Validate memory allocation patterns, GC pressure, rendering performance in ImGui context, process communication latency.

### Game Integration Testing
ImGui rendering correctness, input handling in game context, resource cleanup on mod unload, integration with game logging/error handling.