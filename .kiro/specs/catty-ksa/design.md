# Design Document

## Overview

This design document describes a C# terminal emulator implementation that translates the existing TypeScript caTTY terminal emulator for integration with the Kitten Space Agency (KSA) game engine. The architecture maintains the same headless design pattern as the TypeScript version but adapts it for C#, .NET 10, and ImGui rendering within a game context.

The terminal emulator follows a strict separation between headless core logic and display-specific controller code. The headless core contains all terminal emulation logic implemented in pure C# with no external dependencies, while the ImGui controller handles game integration, input processing, and rendering using the KSA game engine's BRUTAL ImGui framework.

The implementation strategy focuses on direct translation from the TypeScript codebase, maintaining API compatibility and behavior consistency while leveraging C#'s type safety, memory efficiency, and performance characteristics.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   KSA Game Engine                        │
│  ┌───────────────────────────────────────────────────┐  │
│  │                ImGui Controller                    │  │
│  │  ┌──────────────┐         ┌──────────────┐       │  │
│  │  │ Input Handler│────────▶│ ImGui Renderer│      │  │
│  │  └──────┬───────┘         └──────▲───────┘       │  │
│  │         │                        │                │  │
│  │         │    ┌──────────────┐    │                │  │
│  │         └───▶│   Process    │◀───┘                │  │
│  │              │   Manager    │                     │  │
│  │              └──────┬───────┘                     │  │
│  └─────────────────────┼─────────────────────────────┘  │
│                        │                                 │
│  ┌─────────────────────┼─────────────────────────────┐  │
│  │                     ▼      Headless Core (C#)     │  │
│  │  ┌──────────────────────────────────────────────┐│  │
│  │  │           Terminal Emulator Core             ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │   Screen   │  │    Parser    │          ││  │
│  │  │  │   Buffer   │  │   (CSI/ESC)  │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │   Cursor   │  │  Scrollback  │          ││  │
│  │  │  │   State    │  │    Buffer    │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │    SGR     │  │     OSC      │          ││  │
│  │  │  │   Parser   │  │    Parser    │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  └──────────────────────────────────────────────┘│  │
│  └─────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│                Development Environment                    │
│  ┌───────────────────────────────────────────────────┐  │
│  │              Console Test App                      │  │
│  │  ┌──────────────┐         ┌──────────────┐       │  │
│  │  │Console Input │────────▶│Console Output│       │  │
│  │  └──────┬───────┘         └──────▲───────┘       │  │
│  │         │                        │                │  │
│  │         └────────────────────────┘                │  │
│  │              (Same Headless Core)                 │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Project Structure

The solution follows a multi-project architecture that enables both standalone development and game integration:

```
catty-ksa.sln
├── caTTY.Core/              # Headless terminal logic
│   ├── Terminal/            # Core terminal emulation
│   ├── Input/               # Input processing and encoding
│   ├── Parsing/             # Escape sequence parsers
│   ├── Types/               # Data structures and enums
│   └── Utils/               # Utility functions
├── caTTY.ImGui/             # ImGui display controller
│   ├── Controllers/         # Terminal controller
│   ├── Rendering/           # ImGui rendering logic
│   └── Input/               # ImGui input handling
├── caTTY.ImGui.Playground/  # ImGui rendering experiments
│   ├── Experiments/         # Proof-of-concept rendering tests
│   └── Rendering/           # Experimental ImGui techniques
├── caTTY.TestApp/           # Standalone BRUTAL ImGui test application
├── caTTY.GameMod/           # Game mod build target (DLL output)
├── caTTY.Core.Tests/        # Unit and property tests for Core
└── caTTY.ImGui.Tests/       # Unit and property tests for ImGui
```

**Reference Implementation**: The `KsaExampleMod/` folder in the repository root provides a complete working example of KSA game mod structure. Use it as a reference for:
- Project file configuration with KSA DLL references (`modone.csproj`)
- Mod metadata file structure (`mod.toml`)
- StarMap attribute-based mod implementation (`Class1.cs`)
- Harmony patching patterns (`Patcher.cs`)
- Asset management and build targets

## Components and Interfaces

### Code Organization Principles

**Modular Architecture**: The C# implementation follows strict modular design principles to ensure maintainability and testability:

- **Single Responsibility**: Each class has a focused, well-defined responsibility
- **Size Constraints**: Classes are kept small and manageable (typically under 400 lines)
- **Interface Segregation**: Interfaces are focused and specific to their use cases
- **Dependency Injection**: Components depend on interfaces, not concrete implementations

**Parser Decomposition**: The terminal parser is decomposed into specialized parsers to avoid monolithic classes:

```csharp
// Main parser coordinates state machine and delegates to specialized parsers
public class Parser
{
    private readonly ICsiParser _csiParser;
    private readonly ISgrParser _sgrParser;
    private readonly IOscParser _oscParser;
    private readonly IEscParser _escParser;
    private readonly IDcsParser _dcsParser;
    private readonly IUtf8Decoder _utf8Decoder;
    
    // State machine coordination only - delegates parsing to specialists
}
```

**State Management Decomposition**: Terminal state is managed by focused managers:

```csharp
// Terminal emulator coordinates between specialized managers
public class TerminalEmulator
{
    private readonly IScreenBufferManager _screenBufferManager;
    private readonly ICursorManager _cursorManager;
    private readonly IScrollbackManager _scrollbackManager;
    private readonly IAlternateScreenManager _alternateScreenManager;
    private readonly IModeManager _modeManager;
    private readonly IAttributeManager _attributeManager;
    
    // Coordinates operations between managers
}
```

### Core Terminal Components

#### ITerminalEmulator Interface

```csharp
public interface ITerminalEmulator : IDisposable
{
    // Configuration
    int Width { get; }
    int Height { get; }
    int ScrollbackSize { get; }
    
    // State Access
    IScreenBuffer ScreenBuffer { get; }
    ICursor Cursor { get; }
    IScrollbackBuffer ScrollbackBuffer { get; }
    
    // Input Processing
    void Write(ReadOnlySpan<byte> data);
    void Write(string text);
    void Resize(int width, int height);
    
    // Events
    event EventHandler<BellEventArgs> Bell;
    event EventHandler<TitleChangeEventArgs> TitleChanged;
    event EventHandler<ClipboardEventArgs> ClipboardRequest;
    event EventHandler<DataOutputEventArgs> DataOutput;
    event EventHandler<ResizeEventArgs> Resized;
}
```

#### IScreenBuffer Interface

```csharp
public interface IScreenBuffer
{
    int Width { get; }
    int Height { get; }
    
    ICell GetCell(int row, int col);
    void SetCell(int row, int col, ICell cell);
    void Clear();
    void ScrollUp(int lines);
    void ScrollDown(int lines);
    
    // Efficient access for rendering
    ReadOnlySpan<ICell> GetRow(int row);
    void CopyTo(Span<ICell> destination, int startRow, int endRow);
}
```

#### ICell Interface

```csharp
public readonly struct Cell : ICell
{
    public char Character { get; }
    public SgrAttributes Attributes { get; }
    public bool IsWide { get; }
    public string? HyperlinkUrl { get; }
    
    public Cell(char character, SgrAttributes attributes, bool isWide = false, string? hyperlinkUrl = null);
}

public readonly struct SgrAttributes
{
    public Color Foreground { get; }
    public Color Background { get; }
    public bool Bold { get; }
    public bool Italic { get; }
    public bool Underline { get; }
    public bool Strikethrough { get; }
    public bool Inverse { get; }
    public bool Dim { get; }
    public bool Blink { get; }
}
```

### Parser Components

#### Main Parser Interface

```csharp
public interface IParser
{
    void PushBytes(ReadOnlySpan<byte> data);
    void PushByte(byte data);
    void FlushIncompleteSequences();
}
```

#### Specialized Parser Interfaces

```csharp
public interface ICsiParser
{
    CsiMessage ParseCsiSequence(ReadOnlySpan<byte> sequence);
    bool TryParseParameters(ReadOnlySpan<char> parameterString, out int[] parameters);
}

public interface ISgrParser
{
    SgrSequence ParseSgrSequence(ReadOnlySpan<int> parameters, SgrAttributes current);
    SgrAttributes ApplyAttributes(SgrAttributes current, SgrMessage message);
}

public interface IOscParser
{
    OscMessage ParseOscSequence(ReadOnlySpan<byte> sequence, string terminator);
    bool TryParseCommand(ReadOnlySpan<char> payload, out int command, out string parameters);
}

public interface IEscParser
{
    EscMessage ParseEscSequence(ReadOnlySpan<byte> sequence);
    bool IsCharacterSetDesignation(ReadOnlySpan<byte> sequence);
}

public interface IDcsParser
{
    DcsMessage ParseDcsSequence(ReadOnlySpan<byte> sequence, string terminator);
    bool TryParseCommand(ReadOnlySpan<char> payload, out string command, out string[] parameters);
}

public interface IUtf8Decoder
{
    bool TryDecodeSequence(ReadOnlySpan<byte> bytes, out int codePoint, out int bytesConsumed);
    bool IsValidUtf8Start(byte b);
    int GetExpectedLength(byte startByte);
}
```

#### Parser Implementation Strategy

Each specialized parser focuses on a specific type of escape sequence:

- **CsiParser**: Handles CSI sequences (ESC [ ... final), parameter parsing, and command identification
- **SgrParser**: Handles SGR sequences (CSI ... m), color parsing, and attribute management
- **OscParser**: Handles OSC sequences (ESC ] ... ST/BEL), command parsing, and payload extraction
- **EscParser**: Handles ESC sequences (ESC ...), character set designation, and cursor operations
- **DcsParser**: Handles DCS sequences (ESC P ... ST), device control, and query responses
- **Utf8Decoder**: Handles UTF-8 multi-byte sequences, validation, and code point extraction

### State Management Components

#### Manager Interfaces

```csharp
public interface IScreenBufferManager
{
    int Width { get; }
    int Height { get; }
    
    ICell GetCell(int row, int col);
    void SetCell(int row, int col, ICell cell);
    void Clear();
    void ClearRegion(int startRow, int startCol, int endRow, int endCol);
    void ScrollUp(int lines);
    void ScrollDown(int lines);
    void Resize(int width, int height);
    
    ReadOnlySpan<ICell> GetRow(int row);
    void CopyTo(Span<ICell> destination, int startRow, int endRow);
}

public interface ICursorManager
{
    int Row { get; set; }
    int Column { get; set; }
    bool Visible { get; set; }
    
    void MoveTo(int row, int col);
    void MoveUp(int lines);
    void MoveDown(int lines);
    void MoveLeft(int columns);
    void MoveRight(int columns);
    void SavePosition();
    void RestorePosition();
    void ClampToBuffer(int width, int height);
}

public interface IScrollbackManager
{
    int MaxLines { get; }
    int CurrentLines { get; }
    int ViewportOffset { get; set; }
    
    void AddLine(ReadOnlySpan<ICell> line);
    ReadOnlySpan<ICell> GetLine(int index);
    void Clear();
    void SetViewportOffset(int offset);
    bool IsAtBottom { get; }
}

public interface IAlternateScreenManager
{
    bool IsAlternateActive { get; }
    
    void ActivateAlternate();
    void DeactivateAlternate();
    IScreenBufferManager GetCurrentBuffer();
    IScreenBufferManager GetPrimaryBuffer();
    IScreenBufferManager GetAlternateBuffer();
}

public interface IModeManager
{
    bool AutoWrapMode { get; set; }
    bool ApplicationCursorKeys { get; set; }
    bool BracketedPasteMode { get; set; }
    bool CursorVisible { get; set; }
    bool OriginMode { get; set; }
    bool Utf8Mode { get; set; }
    
    void SetMode(int mode, bool enabled);
    bool GetMode(int mode);
    void SaveModes();
    void RestoreModes();
}

public interface IAttributeManager
{
    SgrAttributes CurrentAttributes { get; set; }
    
    void ApplySgrMessage(SgrMessage message);
    void ResetAttributes();
    SgrAttributes GetDefaultAttributes();
    void SetForegroundColor(Color color);
    void SetBackgroundColor(Color color);
    void SetTextStyle(bool bold, bool italic, bool underline);
}
```

#### Manager Implementation Strategy

Each manager focuses on a specific aspect of terminal state:

- **ScreenBufferManager**: Manages the 2D character grid, cell operations, and buffer resizing
- **CursorManager**: Tracks cursor position, visibility, and movement operations
- **ScrollbackManager**: Handles scrollback buffer, viewport management, and history navigation
- **AlternateScreenManager**: Manages primary/alternate buffer switching and state isolation
- **ModeManager**: Tracks all terminal modes and their state changes
- **AttributeManager**: Manages SGR attributes and their application to characters

#### Performance-Optimized Render Loop Architecture

The ImGui controller is designed with strict allocation minimization for smooth game performance:

```csharp
public class ImGuiTerminalController : ITerminalController
{
    // Pre-allocated buffers for render loop (no allocations during rendering)
    private readonly StringBuilder _textBuilder = new(4096);
    private readonly char[] _renderBuffer = new char[8192];
    private readonly List<ImDrawCmd> _drawCommands = new(256);
    private readonly Dictionary<Color, uint> _colorCache = new(64);
    
    // Reusable spans for hot path operations
    private Memory<char> _workingMemory;
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    
    public void Render()
    {
        if (!IsVisible) return;
        
        ImGui.Begin("Terminal", ref _isVisible);
        
        // Hot path: zero-allocation rendering using pre-allocated buffers
        RenderTerminalContent(); // Uses spans and pre-allocated buffers
        ProcessInputEvents();    // Uses object pooling for event data
        
        ImGui.End();
    }
    
    private void RenderTerminalContent()
    {
        // Use span-based access to avoid allocations
        var screenBuffer = _terminal.ScreenBuffer;
        for (int row = 0; row < screenBuffer.Height; row++)
        {
            ReadOnlySpan<ICell> rowSpan = screenBuffer.GetRow(row);
            RenderRowOptimized(rowSpan, row); // No allocations in inner loop
        }
    }
}
```

#### Memory-Efficient Buffer Management

```csharp
public class OptimizedScreenBuffer : IScreenBuffer
{
    // Pre-allocated 2D array - no allocations during normal operation
    private readonly Cell[,] _cells;
    private readonly int _width, _height;
    
    // Cached row spans to avoid repeated span creation
    private readonly ReadOnlyMemory<Cell>[] _rowMemories;
    
    public ReadOnlySpan<Cell> GetRow(int row)
    {
        // Return pre-computed span - zero allocation
        return _rowMemories[row].Span;
    }
    
    public void UpdateCell(int row, int col, Cell cell)
    {
        // Direct array access - zero allocation
        _cells[row, col] = cell;
    }
}
```

#### ITerminalController Interface

```csharp
public interface ITerminalController : IDisposable
{
    bool IsVisible { get; set; }
    bool HasFocus { get; }
    
    void Update(float deltaTime);
    void Render();
    void HandleInput();
    
    event EventHandler<string> DataInput;
}
```

#### ImGuiTerminalController Implementation

```csharp
public class ImGuiTerminalController : ITerminalController
{
    private readonly ITerminalEmulator _terminal;
    private readonly IProcessManager _processManager;
    private readonly ImGuiRenderer _renderer;
    private readonly ImGuiInputHandler _inputHandler;
    
    public void Render()
    {
        if (!IsVisible) return;
        
        ImGui.Begin("Terminal", ref _isVisible);
        
        // Render terminal content
        _renderer.RenderTerminal(_terminal);
        
        // Handle input
        _inputHandler.ProcessInput();
        
        ImGui.End();
    }
}
```

### Process Management

#### IProcessManager Interface

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

### Terminal State Model

```csharp
public class TerminalState : ITerminalState
{
    // Screen management
    public IScreenBuffer PrimaryBuffer { get; }
    public IScreenBuffer AlternateBuffer { get; }
    public IScreenBuffer CurrentBuffer { get; private set; }
    public IScrollbackBuffer ScrollbackBuffer { get; }
    
    // Cursor state
    public ICursor Cursor { get; }
    public bool CursorVisible { get; set; }
    
    // Terminal modes
    public bool AutoWrapMode { get; set; }
    public bool ApplicationCursorKeys { get; set; }
    public bool BracketedPasteMode { get; set; }
    
    // Character sets
    public CharacterSet G0CharacterSet { get; set; }
    public CharacterSet G1CharacterSet { get; set; }
    public CharacterSet ActiveCharacterSet { get; set; }
    
    // Tab stops
    public ITabStops TabStops { get; }
    
    // Scroll region
    public int ScrollTop { get; set; }
    public int ScrollBottom { get; set; }
    
    // Current attributes
    public SgrAttributes CurrentAttributes { get; set; }
}
```

### Memory-Efficient Buffer Implementation

```csharp
public class ScreenBuffer : IScreenBuffer
{
    private readonly Cell[,] _cells;
    private readonly int _width;
    private readonly int _height;
    
    // Pre-allocated row memories for zero-allocation span access
    private readonly ReadOnlyMemory<Cell>[] _rowMemories;
    
    public ScreenBuffer(int width, int height)
    {
        _width = width;
        _height = height;
        _cells = new Cell[height, width];
        
        // Pre-compute row memories to avoid allocation during GetRow calls
        _rowMemories = new ReadOnlyMemory<Cell>[height];
        for (int i = 0; i < height; i++)
        {
            _rowMemories[i] = MemoryMarshal.CreateReadOnlyMemory(
                ref _cells[i, 0], width);
        }
    }
    
    public ReadOnlySpan<Cell> GetRow(int row)
    {
        // Return pre-computed span - zero allocation in hot path
        return _rowMemories[row].Span;
    }
}
```

### Allocation-Optimized Scrollback Buffer

```csharp
public class ScrollbackBuffer : IScrollbackBuffer
{
    private readonly Cell[][] _lines;
    private readonly int _maxLines;
    private int _head;
    private int _count;
    
    // Pre-allocated arrays for line reuse
    private readonly Queue<Cell[]> _recycledArrays = new();
    
    public void AddLine(ReadOnlySpan<Cell> line)
    {
        // Reuse arrays to minimize allocations
        Cell[] lineArray = GetRecycledArray(line.Length);
        line.CopyTo(lineArray);
        
        if (_count < _maxLines)
        {
            _lines[_count++] = lineArray;
        }
        else
        {
            // Recycle the array being replaced
            RecycleArray(_lines[_head]);
            _lines[_head] = lineArray;
            _head = (_head + 1) % _maxLines;
        }
    }
    
    private Cell[] GetRecycledArray(int length)
    {
        // Object pooling pattern for array reuse
        return _recycledArrays.Count > 0 && _recycledArrays.Peek().Length >= length
            ? _recycledArrays.Dequeue()
            : new Cell[Math.Max(length, 80)]; // Pre-allocate common terminal width
    }
}
```

## Performance Optimization Architecture

### Render Loop Allocation Minimization

The terminal emulator architecture is specifically designed to minimize memory allocations during the render loop and other hot paths that execute frequently during game operation.

#### Hot Path Identification

**Critical Hot Paths (Zero Allocation Required):**
- `ImGuiTerminalController.Render()` - Called every frame
- `ImGuiTerminalController.Update()` - Called every frame  
- `ScreenBuffer.GetRow()` - Called for each visible row during rendering
- `InputHandler.ProcessKeyboard()` - Called on every keystroke
- `Parser.ParseSequence()` - Called for every incoming byte sequence

**Warm Paths (Minimal Allocation Acceptable):**
- Terminal resize operations
- Process I/O handling
- Configuration changes
- Error handling and logging

#### Memory Architecture Strategy

**Pre-Allocation Pattern:**
```csharp
public class ImGuiTerminalController
{
    // Allocated once during initialization, reused throughout lifetime
    private readonly StringBuilder _stringBuilder = new(4096);
    private readonly char[] _renderBuffer = new char[8192];
    private readonly List<RenderCommand> _renderCommands = new(512);
    private readonly Dictionary<SgrAttributes, uint> _attributeColorCache = new(128);
    
    // ArrayPool for temporary allocations
    private readonly ArrayPool<char> _charPool = ArrayPool<char>.Shared;
    private readonly ArrayPool<byte> _bytePool = ArrayPool<byte>.Shared;
}
```

**Span-Based Data Access:**
```csharp
// All screen buffer access uses spans to avoid copying
public ReadOnlySpan<Cell> GetVisibleRows(int startRow, int endRow)
{
    // Return span slice without allocation
    return _screenData.AsSpan().Slice(startRow * _width, (endRow - startRow) * _width);
}

// String operations use span-based APIs
public void ProcessText(ReadOnlySpan<char> text)
{
    // Process character by character without string allocation
    foreach (char c in text)
    {
        ProcessCharacter(c); // No substring creation
    }
}
```

**Object Pooling for Temporary Objects:**
```csharp
public class EventObjectPool
{
    private readonly ConcurrentQueue<KeyboardEvent> _keyboardEvents = new();
    private readonly ConcurrentQueue<MouseEvent> _mouseEvents = new();
    
    public KeyboardEvent RentKeyboardEvent()
    {
        return _keyboardEvents.TryDequeue(out var evt) ? evt : new KeyboardEvent();
    }
    
    public void ReturnKeyboardEvent(KeyboardEvent evt)
    {
        evt.Reset(); // Clear state for reuse
        _keyboardEvents.Enqueue(evt);
    }
}
```

#### Buffer Management Strategy

**Screen Buffer Optimization:**
- Pre-allocate 2D cell array during initialization
- Use `ReadOnlyMemory<Cell>` for row access to avoid span creation overhead
- Cache frequently accessed spans and memory regions
- Implement copy-on-write for buffer operations that modify large regions

**String Building Optimization:**
- Maintain reusable `StringBuilder` instances with pre-allocated capacity
- Use `ArrayPool<char>` for temporary character arrays
- Implement custom string formatting that avoids boxing and temporary strings
- Cache formatted strings for repeated values (colors, common escape sequences)

**Input Processing Optimization:**
- Pre-allocate input event objects and reuse through object pooling
- Use span-based parsing for escape sequence processing
- Maintain lookup tables for key code to escape sequence mapping
- Avoid string concatenation in input encoding

#### Memory vs Performance Trade-offs

**Acceptable Memory Usage Increases:**
- **Larger Buffer Sizes**: Pre-allocate buffers 2-4x larger than minimum required to avoid resize operations
- **Caching**: Maintain caches for computed values (color conversions, formatted strings, glyph metrics)
- **Object Pools**: Keep pools of reusable objects even when not actively in use
- **Lookup Tables**: Use memory for lookup tables to avoid runtime computation

**Performance Benefits:**
- **Reduced GC Pressure**: Fewer allocations mean less garbage collection overhead
- **Better Cache Locality**: Pre-allocated, reused objects stay in CPU cache longer
- **Predictable Performance**: Avoid allocation spikes that cause frame drops
- **Lower Latency**: No GC pauses during critical rendering operations

#### Implementation Guidelines

**DO:**
- Use `ReadOnlySpan<T>` and `Span<T>` for all data processing in hot paths
- Pre-allocate all buffers and collections during initialization
- Implement object pooling for frequently created/destroyed objects
- Use `ArrayPool<T>` for temporary arrays
- Cache computed values that are expensive to recalculate
- Use `StringBuilder.Clear()` instead of creating new instances

**DON'T:**
- Use LINQ operations in render loop or hot paths
- Create temporary strings or collections in frequently called methods
- Box value types unnecessarily
- Use `string.Concat()` or `+` operator for string building in hot paths
- Create delegates or lambdas in hot paths (causes allocation)
- Use reflection or dynamic operations in performance-critical code

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Now I'll analyze the acceptance criteria to determine which ones are testable as properties:

<function_calls>
<invoke name="prework">
<parameter name="featureName">catty-ksa

### Property Reflection

After analyzing all acceptance criteria, I identified numerous properties that can be tested. However, many of these properties are logically related and can be consolidated to avoid redundancy:

**Consolidation Analysis:**
- Properties 8.2, 9.1, 9.2 (character writing and cursor advancement) can be combined into a comprehensive character processing property
- Properties 11.1-11.5 (cursor movement sequences) can be combined into a general cursor movement property
- Properties 11.6-11.7 (erase sequences) can be combined into a screen clearing property
- Properties 12.1-12.5 (SGR parsing and application) can be combined into comprehensive SGR handling properties
- Properties 13.1-13.5 (OSC parsing) can be combined into comprehensive OSC handling properties
- Properties 14.1-14.5 (scrollback behavior) can be combined into scrollback management properties
- Properties 15.1-15.5 (alternate screen) can be combined into alternate screen management properties

This consolidation reduces redundancy while maintaining comprehensive coverage of all testable behaviors.

### Correctness Properties

Property 1: Event notification consistency
*For any* terminal state change operation, the terminal should emit appropriate events to notify observers of the change
**Validates: Requirements 2.3**

Property 2: TypeScript compatibility for escape sequences
*For any* valid escape sequence, the C# parser should produce the same parsing result as the TypeScript implementation
**Validates: Requirements 3.1**

Property 3: TypeScript compatibility for screen operations
*For any* sequence of screen operations, the C# terminal should reach the same final state as the TypeScript implementation
**Validates: Requirements 3.2**

Property 4: TypeScript compatibility for cursor operations
*For any* cursor operation, the C# terminal should position the cursor identically to the TypeScript implementation
**Validates: Requirements 3.3**

Property 5: TypeScript compatibility for scrollback behavior
*For any* scrolling operation, the C# terminal should handle scrollback identically to the TypeScript implementation
**Validates: Requirements 3.4**

Property 6: TypeScript compatibility for alternate screen
*For any* alternate screen buffer operation, the C# terminal should behave identically to the TypeScript implementation
**Validates: Requirements 3.5**

Property 7: Screen buffer initialization
*For any* valid width and height parameters, terminal initialization should create a screen buffer with the specified dimensions
**Validates: Requirements 7.1**

Property 8: Screen buffer resize preservation
*For any* resize operation, existing content should be preserved where possible within the new dimensions
**Validates: Requirements 7.2**

Property 9: Cell data integrity
*For any* cell operation, the character and SGR attributes should be maintained correctly and remain accessible
**Validates: Requirements 7.3, 7.4**

Property 10: Terminal size constraints
*For any* terminal size within bounds (1x1 to 1000x1000), initialization should succeed; sizes outside bounds should be rejected
**Validates: Requirements 7.5**

Property 11: Cursor initialization and advancement
*For any* terminal initialization, cursor should start at (0,0), and for any character write, cursor should advance correctly
**Validates: Requirements 8.1, 8.2**

Property 12: Cursor wrapping behavior
*For any* cursor at the right edge, wrapping should occur to the next line if and only if auto-wrap mode is enabled
**Validates: Requirements 8.3**

Property 13: Cursor movement sequences
*For any* valid cursor movement sequence, the cursor should move to the correct position according to the sequence parameters
**Validates: Requirements 8.4, 11.1, 11.2, 11.3, 11.4, 11.5**

Property 14: Cursor visibility tracking
*For any* cursor visibility toggle operation, the visibility state should be tracked correctly
**Validates: Requirements 8.5**

Property 15: Character processing with attributes
*For any* printable character, it should be written at the cursor position with current SGR attributes applied
**Validates: Requirements 9.1, 9.2**

Property 16: UTF-8 character handling
*For any* valid UTF-8 sequence, it should be decoded and displayed correctly, with wide characters occupying two cells
**Validates: Requirements 9.3, 9.4**

Property 17: Line wrapping behavior
*For any* character write that would exceed line width, handling should follow auto-wrap mode settings
**Validates: Requirements 9.5**

Property 18: Control character processing
*For any* control character (newline, carriage return, backspace, tab, bell), the appropriate cursor movement or event should occur
**Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**

Property 19: Screen clearing operations
*For any* erase sequence (display or line), the correct portions of the screen should be cleared according to parameters
**Validates: Requirements 11.6, 11.7**

Property 20: Screen scrolling operations
*For any* scroll sequence (up or down), the screen should scroll by the specified number of lines
**Validates: Requirements 11.8, 11.9**

Property 21: SGR parsing and application
*For any* valid SGR sequence, it should be parsed correctly and attributes should be applied to subsequent characters
**Validates: Requirements 12.1, 12.2, 12.4, 12.5**

Property 22: SGR reset behavior
*For any* SGR reset sequence, all attributes should return to default state
**Validates: Requirements 12.3**

Property 23: OSC parsing and event emission
*For any* valid OSC sequence, it should be parsed correctly and appropriate events should be emitted
**Validates: Requirements 13.1, 13.2, 13.4**

Property 24: OSC hyperlink association
*For any* OSC 8 hyperlink sequence, the URL should be correctly associated with subsequent characters
**Validates: Requirements 13.3**

Property 25: Unknown OSC sequence handling
*For any* unknown OSC sequence, it should be ignored without causing errors
**Validates: Requirements 13.5**

Property 26: Scrollback buffer management
*For any* content that scrolls off screen, it should be added to scrollback, and scrollback should maintain size limits
**Validates: Requirements 14.1, 14.2**

Property 27: Viewport and auto-scroll behavior
*For any* scroll operation, viewport offset should be maintained correctly, and auto-scroll should work when content is written while scrolled
**Validates: Requirements 14.3, 14.4**

Property 28: Scrollback access
*For any* scrollback query, historical lines should be accessible correctly
**Validates: Requirements 14.5**

Property 29: Alternate screen buffer switching
*For any* alternate screen activation/deactivation, buffer switching should work correctly and preserve state independently
**Validates: Requirements 15.1, 15.2, 15.4**

Property 30: Alternate screen scrollback isolation
*For any* content written in alternate screen mode, it should not be added to scrollback
**Validates: Requirements 15.3**

Property 31: Alternate screen initialization
*For any* alternate screen activation, the buffer should be cleared to default state
**Validates: Requirements 15.5**

## Error Handling

### Exception Handling Strategy

The C# implementation follows a defensive programming approach with explicit error handling:

```csharp
public enum TerminalError
{
    InvalidDimensions,
    InvalidSequence,
    BufferOverflow,
    ProcessError,
    ResourceDisposed
}

public class TerminalException : Exception
{
    public TerminalError ErrorType { get; }
    
    public TerminalException(TerminalError errorType, string message) 
        : base(message)
    {
        ErrorType = errorType;
    }
}
```

### Error Recovery Patterns

1. **Invalid Escape Sequences**: Log and ignore, continue processing
2. **Buffer Overflows**: Resize or truncate gracefully
3. **Process Failures**: Emit events, attempt reconnection
4. **Resource Exhaustion**: Clean up and notify observers

### Logging Integration

```csharp
public interface ITerminalLogger
{
    void LogError(string message, Exception? exception = null);
    void LogWarning(string message);
    void LogDebug(string message);
}

// Integration with game logging system
public class KsaTerminalLogger : ITerminalLogger
{
    public void LogError(string message, Exception? exception = null)
    {
        // Use KSA game logging framework
        KSA.Logging.LogError($"Terminal: {message}", exception);
    }
}
```

## Testing Strategy

### Dual Testing Approach

The testing strategy combines unit tests and property-based tests to ensure comprehensive coverage:

- **Unit tests**: Verify specific examples, edge cases, and error conditions
- **Property tests**: Verify universal properties across all inputs using FsCheck.NUnit
- Both approaches are complementary and necessary for comprehensive validation

### Property-Based Testing Configuration

- **Framework**: FsCheck.NUnit for property-based testing
- **Iterations**: Minimum 100 iterations per property test
- **Test Tagging**: Each property test references its design document property
- **Tag Format**: `[Property] // Feature: catty-ksa, Property {number}: {property_text}`

### Unit Testing Focus Areas

Unit tests should focus on:
- Specific examples that demonstrate correct behavior
- Integration points between Core and ImGui components
- Edge cases and error conditions
- Game engine integration scenarios

Property tests should focus on:
- Universal properties that hold for all inputs
- Comprehensive input coverage through randomization
- Compatibility verification with TypeScript implementation

### Test Organization

```
Tests/
├── caTTY.Core.Tests/
│   ├── Unit/
│   │   ├── TerminalEmulatorTests.cs
│   │   ├── ParserTests.cs
│   │   └── ScreenBufferTests.cs
│   └── Property/
│       ├── TerminalBehaviorProperties.cs
│       ├── ParsingProperties.cs
│       └── CompatibilityProperties.cs
├── caTTY.ImGui.Tests/
│   ├── Unit/
│   │   ├── TerminalControllerTests.cs
│   │   └── RendererTests.cs
│   └── Integration/
│       └── GameIntegrationTests.cs
└── Integration/
    └── ProcessManagementTests.cs
```

### TypeScript Compatibility Testing

A key aspect of the testing strategy is ensuring behavioral compatibility with the TypeScript implementation. The C# implementation must match or exceed the TypeScript test coverage, which includes 42 test files covering:

**Parser Testing Coverage:**
- Parser state integrity and consistency during mixed operations
- CSI sequence parsing (cursor movement, screen clearing, scrolling)
- SGR parsing (colors, styling, underline variants, enhanced modes)
- OSC sequence parsing (window title, clipboard, hyperlinks)
- DCS handling (device control strings and responses)
- ESC sequences (save/restore cursor, character sets)

**Terminal Behavior Coverage:**
- Cursor operations (positioning, wrapping, visibility, application mode)
- Screen buffer operations (initialization, resizing, cell integrity)
- Scrollback management (buffer operations, viewport tracking)
- Alternate screen (buffer switching, state isolation)
- Character processing (UTF-8, wide characters, control characters)
- Line operations (insertion, deletion, content preservation)

**Advanced Feature Coverage:**
- Tab stop controls, window manipulation, device queries
- Color systems, selection operations, hyperlink support
- Enhanced SGR modes, selective erase, character sets

```csharp
[Property]
// Feature: catty-ksa, Property 2: TypeScript compatibility for escape sequences
public Property EscapeSequenceCompatibility()
{
    return Prop.ForAll(
        EscapeSequenceGenerator.ValidSequences(),
        sequence =>
        {
            var csharpResult = _csharpParser.Parse(sequence);
            var typescriptResult = GetTypescriptResult(sequence);
            return csharpResult.Equals(typescriptResult);
        });
}
```

### Performance Testing

While not part of correctness properties, performance characteristics should be validated:

- Memory allocation patterns
- Garbage collection pressure
- Rendering performance in ImGui context
- Process communication latency

### Game Integration Testing

Specific tests for KSA game integration:

- ImGui rendering correctness
- Input handling within game context
- Resource cleanup on mod unload
- Integration with game's logging and error handling systems