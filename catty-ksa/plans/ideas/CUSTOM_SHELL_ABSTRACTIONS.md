# Custom Game Shell Abstraction Architecture

## Executive Summary

This document defines the architecture for custom game shell abstractions in caTTY. The design introduces three composable layers that separate concerns:

1. **Base Layer** (`BaseCustomShell`): PTY plumbing, channel-based output pump, event wiring
2. **Line Discipline Layer** (`LineDisciplineShell`): Input buffering, command history, escape sequences, echo
3. **Application Layer** (e.g., `GameConsoleShell`): Command execution logic only

**Key Benefit**: Implementing a new custom shell requires writing only `ExecuteCommand(string)` - all infrastructure is provided by base layers.

## Problem Statement

The current `GameConsoleShell` implementation (500+ LOC) mixes three distinct responsibilities:

```
GameConsoleShell.cs (500+ LOC)
├── PTY Plumbing (lines 44-209)
│   ├── Channel-based output pump
│   ├── Background pump task
│   └── Event wiring
├── Line Discipline (lines 272-499)
│   ├── Input buffering
│   ├── Command history
│   ├── Escape sequence parsing
│   └── Character echo
└── Game Logic (lines 520-698)
    ├── KSA TerminalInterface execution
    └── Harmony patch integration
```

**Issues**:
- Violates Single Responsibility Principle
- Difficult to test in isolation
- Cannot reuse infrastructure for other custom shells
- Mixes low-level PTY concerns with high-level game logic

## Proposed Architecture

### Class Hierarchy

```
┌─────────────────────────────────────────────────────────────────┐
│                        ICustomShell                             │
│  (existing interface - no changes)                              │
│  - StartAsync(), StopAsync(), WriteInputAsync()                │
│  - Events: OutputReceived, Terminated                           │
│  - SendInitialOutput()                                          │
└─────────────────────────────────────────────────────────────────┘
                              △
                              │ implements
                              │
┌─────────────────────────────────────────────────────────────────┐
│              BaseCustomShell (abstract)                         │
│  ─────────────────────────────────────────────────────────────  │
│  Responsibilities:                                              │
│  • Channel-based output pump infrastructure                    │
│  • Event wiring (OutputReceived, Terminated)                   │
│  • Thread-safe state management (IsRunning)                    │
│  • Output queueing primitives                                  │
│  ─────────────────────────────────────────────────────────────  │
│  Protected API:                                                 │
│  • QueueOutput(byte[])/QueueOutput(string)                     │
│  • OnInputByte(byte) - abstract hook                           │
│  • OnShellStartingAsync() - virtual hook                       │
│  • OnShellStoppingAsync() - virtual hook                       │
│  • GetInitialOutput() - virtual                                │
│  ─────────────────────────────────────────────────────────────  │
│  Final (non-overridable):                                       │
│  • StartAsync(), StopAsync(), WriteInputAsync()                │
│  • OutputPumpAsync() - background task                         │
│  • Dispose()                                                    │
│  └─────────────────────────────────────────────────────────────┘
                              △
                              │ extends
                              │
┌─────────────────────────────────────────────────────────────────┐
│         LineDisciplineShell (abstract)                          │
│  ─────────────────────────────────────────────────────────────  │
│  Responsibilities:                                              │
│  • Input buffering and line editing                            │
│  • Command history with up/down navigation                     │
│  • Escape sequence parsing (arrows, Ctrl+L, etc.)              │
│  • Character echo                                               │
│  • Prompt management                                            │
│  ─────────────────────────────────────────────────────────────  │
│  Protected API:                                                 │
│  • ExecuteCommand(string) - abstract                           │
│  • GetPrompt() - virtual (returns "$ ")                        │
│  • GetBanner() - virtual (returns null)                        │
│  • SendPrompt() - helper                                        │
│  • ClearScreen() - helper                                       │
│  • ClearScreenAndScrollback() - helper                         │
│  ─────────────────────────────────────────────────────────────  │
│  Configuration:                                                 │
│  • LineDisciplineOptions (history size, echo mode, etc.)       │
│  └─────────────────────────────────────────────────────────────┘
                              △
                              │ extends
                              │
┌─────────────────────────────────────────────────────────────────┐
│              GameConsoleShell (concrete)                        │
│  ─────────────────────────────────────────────────────────────  │
│  Responsibilities:                                              │
│  • Execute commands via KSA TerminalInterface                  │
│  • Handle Harmony patch output capture                         │
│  • Built-in command handling (clear)                           │
│  • Prompt configuration loading                                │
│  ─────────────────────────────────────────────────────────────  │
│  Implementation (~150 LOC):                                     │
│  • ExecuteCommand(string) - calls TerminalInterface.Execute()  │
│  • GetPrompt() - loads from ThemeConfiguration                 │
│  • GetBanner() - startup banner                                │
│  • OnShellStartingAsync() - validates TerminalInterface        │
│  └─────────────────────────────────────────────────────────────┘
```

### Data Flow

**Input Flow**:
```
User Keyboard Input
  ↓
TerminalEmulator.ProcessInput()
  ↓
CustomShellPtyBridge.Write(byte[])
  ↓
BaseCustomShell.WriteInputAsync(ReadOnlyMemory<byte>)
  ↓
BaseCustomShell.OnInputByte(byte) [ABSTRACT - implemented by subclass]
  ↓
LineDisciplineShell.OnInputByte(byte) [parses escape sequences, buffers input]
  ↓
LineDisciplineShell.ExecuteCommand(string) [ABSTRACT - on Enter key]
  ↓
GameConsoleShell.ExecuteCommand(string) [calls TerminalInterface.Execute()]
```

**Output Flow**:
```
GameConsoleShell.ExecuteCommand()
  ↓
GameConsoleShell.QueueOutput(string) [inherited from BaseCustomShell]
  ↓
BaseCustomShell._outputChannel.Writer.TryWrite(byte[])
  ↓
BaseCustomShell.OutputPumpAsync() [background task]
  ↓
BaseCustomShell.OutputReceived?.Invoke(ShellOutputEventArgs)
  ↓
CustomShellPtyBridge.OnCustomShellOutput()
  ↓
CustomShellPtyBridge.DataReceived?.Invoke(DataReceivedEventArgs)
  ↓
TerminalEmulator processes escape sequences
  ↓
ScreenBuffer rendered by Display layer
```

## Layer Specifications

### Layer 1: BaseCustomShell (PTY Plumbing)

**File**: `caTTY.Core/Terminal/BaseCustomShell.cs`

**Purpose**: Provides the foundational PTY-style infrastructure for all custom shells.

**Key Features**:
- **Channel-based output pump**: Uses `Channel<byte[]>` (unbounded) to buffer output, mimicking real PTY pipe behavior
- **Background pump task**: `OutputPumpAsync()` continuously reads channel and fires `OutputReceived` events
- **Thread-safe state management**: Locks protect `IsRunning`, `_disposed`, terminal dimensions
- **Event wiring**: Implements `ICustomShell` events (`OutputReceived`, `Terminated`)
- **Lifecycle management**: Handles `StartAsync()`, `StopAsync()`, `Dispose()` with graceful cleanup

**Interface**:
```csharp
public abstract class BaseCustomShell : ICustomShell
{
    // Abstract members (subclasses must implement)
    public abstract CustomShellMetadata Metadata { get; }
    protected abstract void OnInputByte(byte b);

    // Virtual hooks (subclasses can override)
    protected virtual Task OnShellStartingAsync(CustomShellStartOptions options, CancellationToken ct);
    protected virtual Task OnShellStoppingAsync(CancellationToken ct);
    public virtual void SendInitialOutput();
    public virtual void RequestCancellation();
    public virtual void NotifyTerminalResize(int width, int height);

    // Protected API (for subclasses)
    protected void QueueOutput(byte[] data);
    protected void QueueOutput(string text);
    protected int TerminalWidth { get; }
    protected int TerminalHeight { get; }
    protected void ThrowIfDisposed();

    // Final methods (cannot override)
    public Task StartAsync(CustomShellStartOptions options, CancellationToken ct);
    public Task StopAsync(CancellationToken ct);
    public Task WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken ct);
    public void Dispose();

    // Events (ICustomShell)
    public event EventHandler<ShellOutputEventArgs>? OutputReceived;
    public event EventHandler<ShellTerminatedEventArgs>? Terminated;
    public bool IsRunning { get; }
}
```

**Implementation Details**:
- `_outputChannel`: `Channel.CreateUnbounded<byte[]>()` with `SingleReader=true, SingleWriter=false`
- `_outputPumpTask`: Background task that runs `OutputPumpAsync()`
- `_outputPumpCancellation`: `CancellationTokenSource` for graceful shutdown
- `_lock`: Object lock for thread-safe state access
- `StartAsync()`:
  1. Validates not already running
  2. Creates output channel
  3. Starts background pump task
  4. Sets `IsRunning = true`
  5. Calls virtual `OnShellStartingAsync()` hook
- `StopAsync()`:
  1. Calls virtual `OnShellStoppingAsync()` hook
  2. Sets `IsRunning = false`
  3. Completes channel (`Writer.Complete()`)
  4. Waits for pump task to drain with timeout
  5. Fires `Terminated` event

**Usage Example** (Minimal Shell):
```csharp
public class EchoShell : BaseCustomShell
{
    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create("Echo", "Echoes input back");

    protected override void OnInputByte(byte b)
    {
        // Echo byte back
        QueueOutput(new byte[] { b });

        // Execute on Enter
        if (b == 0x0D || b == 0x0A)
        {
            QueueOutput("Echo!\r\n");
        }
    }
}
```

### Layer 2: LineDisciplineShell (Line Editing)

**File**: `caTTY.Core/Terminal/LineDisciplineShell.cs`

**Purpose**: Adds line-editing capabilities on top of base shell (optional enhancement layer).

**Key Features**:
- **Input buffering**: `StringBuilder _lineBuffer` accumulates characters until Enter
- **Command history**: `List<string> _commandHistory` with up/down arrow navigation
- **Escape sequence parsing**: State machine (None → Escape → Csi) for control keys
- **Character echo**: Configurable echo of printable characters back to terminal
- **Line editing**: Backspace removes characters, sends erase sequences
- **Keyboard handling**: Enter executes command, Ctrl+L clears screen, arrows navigate history

**Interface**:
```csharp
public abstract class LineDisciplineShell : BaseCustomShell
{
    // Constructor
    protected LineDisciplineShell();
    protected LineDisciplineShell(LineDisciplineOptions options);

    // Abstract members (subclasses must implement)
    protected abstract void ExecuteCommand(string command);

    // Virtual members (subclasses can override)
    protected virtual string GetPrompt();         // Default: "$ "
    protected virtual string? GetBanner();        // Default: null
    public override void SendInitialOutput();     // Default: banner + prompt

    // Protected helpers (for subclasses)
    protected void SendPrompt();
    protected void ClearScreen();                 // ESC[2J ESC[H (preserves scrollback)
    protected void ClearScreenAndScrollback();    // ESC[3J

    // Configuration
    protected LineDisciplineOptions Options { get; }
}
```

**Configuration** (`LineDisciplineOptions.cs`):
```csharp
public class LineDisciplineOptions
{
    public int MaxHistorySize { get; set; } = 100;
    public bool EchoInput { get; set; } = true;
    public bool EnableHistory { get; set; } = true;
    public bool ParseEscapeSequences { get; set; } = true;

    public static LineDisciplineOptions CreateDefault();
    public static LineDisciplineOptions CreateRawMode(); // All features disabled
}
```

**Implementation Details**:
- **Escape Sequence State Machine**:
  ```
  None → (ESC byte) → Escape → ('[' byte) → Csi → (final byte 0x40-0x7E) → None
  ```
  - CSI 'A' → Up arrow (navigate history up)
  - CSI 'B' → Down arrow (navigate history down)

- **History Navigation**:
  - On first Up: save current line to `_savedCurrentLine`, set `_historyIndex = _commandHistory.Count - 1`
  - Each Up: decrement `_historyIndex`, replace line buffer
  - Each Down: increment `_historyIndex`, restore `_savedCurrentLine` if past end
  - On Enter: reset `_historyIndex = -1`, add command to history (avoid duplicates)

- **Line Editing**:
  - Backspace (0x7F or 0x08): remove char from buffer, send `ESC[D ESC[K` (move left, erase to end)
  - Printable chars (0x20-0x7E): append to buffer, echo if `EchoInput=true`
  - Enter (0x0D or 0x0A): execute command, reset buffer, show prompt

- **Special Keys**:
  - Ctrl+L (0x0C): call `ClearScreen()`, send prompt

**Usage Example** (Line-Editing Shell):
```csharp
public class MyShell : LineDisciplineShell
{
    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create("MyShell", "Custom shell with line editing");

    protected override void ExecuteCommand(string command)
    {
        QueueOutput($"You said: {command}\r\n");

        if (command == "hello")
        {
            QueueOutput("Hello, world!\r\n");
        }
    }

    protected override string GetPrompt() => "my> ";

    protected override string? GetBanner() =>
        "\x1b[1;36mWelcome to MyShell!\x1b[0m\r\n";
}
```

**Raw Mode Example** (No Echo/History):
```csharp
public class RawShell : LineDisciplineShell
{
    public RawShell() : base(LineDisciplineOptions.CreateRawMode())
    {
    }

    protected override void ExecuteCommand(string command)
    {
        // Still called on Enter, but no echo/history during input
        QueueOutput($"Raw: {command}\r\n");
    }
}
```

### Layer 3: Application Layer (GameConsoleShell)

**File**: `caTTY.GameMod/GameConsoleShell.cs`

**Purpose**: KSA-specific shell that executes game console commands.

**Simplified Implementation** (~150 LOC, down from 500+):
```csharp
public class GameConsoleShell : LineDisciplineShell
{
    private static GameConsoleShell? _activeInstance;
    private static readonly object _activeLock = new();
    private bool _isExecutingCommand;
    private string _prompt = "ksa> ";

    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create(
            name: "Game Console",
            description: "KSA game console interface",
            version: new Version(1, 0, 0),
            author: "caTTY",
            supportedFeatures: new[] { "colors", "clear-screen", "command-execution" }
        );

    protected override Task OnShellStartingAsync(CustomShellStartOptions options, CancellationToken ct)
    {
        // Validate KSA TerminalInterface is available
        if (Program.TerminalInterface == null)
        {
            throw new InvalidOperationException("KSA TerminalInterface not available");
        }
        LoadPromptFromConfiguration();
        return Task.CompletedTask;
    }

    protected override string GetPrompt()
    {
        LoadPromptFromConfiguration(); // Reload in case changed
        return _prompt;
    }

    protected override string? GetBanner() =>
        "\x1b[1;36m" +
        "=================================================\r\n" +
        "  KSA Game Console Shell\r\n" +
        "  Type 'help' for available commands\r\n" +
        "  Press Ctrl+L to clear screen\r\n" +
        "=================================================\x1b[0m\r\n";

    protected override void ExecuteCommand(string command)
    {
        // Handle built-in commands
        if (TryHandleBuiltinCommand(command))
            return;

        try
        {
            // Set active instance for Harmony patch
            lock (_activeLock)
            {
                _activeInstance = this;
                _isExecutingCommand = true;
            }

            try
            {
                // Execute via KSA TerminalInterface
                // Output captured by Harmony patch
                Program.TerminalInterface.Execute(command);
            }
            finally
            {
                lock (_activeLock)
                {
                    _isExecutingCommand = false;
                    _activeInstance = null;
                }
            }
        }
        catch (Exception ex)
        {
            QueueOutput($"\x1b[31mError: {ex.Message}\x1b[0m\r\n");
        }
    }

    private bool TryHandleBuiltinCommand(string command)
    {
        if (command.Trim().ToLowerInvariant() == "clear")
        {
            ClearScreenAndScrollback();
            return true;
        }
        return false;
    }

    private void LoadPromptFromConfiguration()
    {
        try
        {
            var config = ThemeConfiguration.Load();
            _prompt = config.GameShellPrompt;
        }
        catch
        {
            _prompt = "ksa> ";
        }
    }

    // Harmony patch callback (static, called by ConsoleWindowPrintPatch)
    internal static void OnConsolePrint(string output, uint color, ConsoleLineType lineType)
    {
        lock (_activeLock)
        {
            if (_activeInstance == null || !_activeInstance._isExecutingCommand)
                return;

            bool isError = color == ConsoleWindow.ErrorColor ||
                          color == ConsoleWindow.CriticalColor;
            string formatted = isError
                ? $"\x1b[31m{output}\x1b[0m\r\n"
                : $"{output}\r\n";

            _activeInstance.QueueOutput(formatted);
        }
    }
}

// Harmony patch (unchanged from original)
[HarmonyPatch(typeof(ConsoleWindow))]
[HarmonyPatch(nameof(ConsoleWindow.Print))]
public static class ConsoleWindowPrintPatch
{
    [HarmonyPostfix]
    public static void Postfix(string inOutput, uint inColor, ConsoleLineType inType)
    {
        try
        {
            GameConsoleShell.OnConsolePrint(inOutput, inColor, inType);
        }
        catch { }
    }
}
```

**Key Changes from Original**:
- ✅ Removed ~350 LOC of infrastructure (channels, pump, input parsing, history)
- ✅ Extends `LineDisciplineShell` instead of implementing `ICustomShell`
- ✅ Only implements game-specific logic (command execution, prompt config, Harmony patch)
- ✅ Identical behavior to original implementation
- ✅ Reduced from 500+ LOC to ~150 LOC

## File Organization

Contracts and base implementations in **`caTTY.ShellContract`** project:

```
caTTY.ShellContract/
  ├── caTTY.ShellContract.csproj         (NEW)
  ├── ICustomShell.cs                    (MOVED from caTTY.Core/Terminal/)
  ├── BaseCustomShell.cs                 (NEW - base layer)
  ├── LineDisciplineShell.cs             (NEW - line discipline layer)
  ├── LineDisciplineOptions.cs           (NEW - configuration)
  ├── CustomShellMetadata.cs             (MOVED from caTTY.Core/Terminal/)
  ├── CustomShellStartOptions.cs         (MOVED from caTTY.Core/Terminal/)
  ├── ProcessEventArgs.cs                (MOVED from caTTY.Core/Terminal/)
  └── README.md                          (NEW - architecture guide)

caTTY.ShellContract.Tests/
  ├── caTTY.ShellContract.Tests.csproj   (NEW)
  ├── Unit/
  │   ├── BaseCustomShellTests.cs        (NEW - base layer tests)
  │   ├── LineDisciplineShellTests.cs    (NEW - line discipline tests)
  │   └── LineDisciplineOptionsTests.cs  (NEW - config tests)
  └── Integration/
      └── CustomShellIntegrationTests.cs (NEW - integration tests)

caTTY.CustomShells/
  ├── caTTY.CustomShells.csproj          (NEW)
  ├── GameConsoleShell.cs                (MOVED from caTTY.GameMod, REFACTORED to ~150 LOC)
  └── Examples/
      ├── EchoShell.cs                   (NEW - demo)
      ├── CalculatorShell.cs             (NEW - demo)
      └── RawShell.cs                    (NEW - demo)

caTTY.CustomShells.Tests/
  ├── caTTY.CustomShells.Tests.csproj    (NEW)
  └── Unit/
      ├── GameConsoleShellTests.cs       (NEW)
      └── ExampleShellTests.cs           (NEW)

caTTY.Core/Terminal/
  ├── CustomShellPtyBridge.cs            (MODIFIED - references caTTY.ShellContract types)
  ├── CustomShellRegistry.cs             (MODIFIED - references caTTY.ShellContract types)
  └── Sessions/
      ├── TerminalSessionFactory.cs      (existing)
      └── SessionCreator.cs              (existing)

caTTY.GameMod/
  ├── caTTY.GameMod.csproj               (MODIFIED - add reference to caTTY.CustomShells)
  ├── TerminalMod.cs                     (existing - no changes)
  └── Patcher.cs                         (existing - no changes)
```

### Project Dependencies

```
caTTY.Core
  ↓
caTTY.ShellContract (references caTTY.Core)
  ↓
caTTY.CustomShells (references caTTY.ShellContract, caTTY.Core, caTTY.Display)
  ↓
caTTY.GameMod (references caTTY.CustomShells + existing refs)
```

## Migration Strategy

### Incremental Refactoring Plan

The refactoring follows 10 independent, incremental tasks:

0. **Task 0**: Create project structure (caTTY.ShellContract, caTTY.CustomShells projects)
1. **Task 1**: Move existing shell types from caTTY.Core to caTTY.ShellContract
2. **Task 2**: Create `BaseCustomShell` (base layer) in ShellContract + tests
3. **Task 3**: Create `LineDisciplineOptions` (configuration) in ShellContract + tests
4. **Task 4**: Create `LineDisciplineShell` (line discipline layer) in ShellContract + tests
5. **Task 5**: Create integration tests in ShellContract.Tests
6. **Task 6**: Move and refactor `GameConsoleShell` to caTTY.CustomShells (500+ LOC → 150 LOC)
7. **Task 7**: Create example shells in caTTY.CustomShells (Echo, Calculator, Raw)
8. **Task 8**: Add documentation (ShellContract/README.md, update CLAUDE.md)
9. **Task 9**: Cleanup (remove backups, validate full test suite)

Each task:
- Is completable in isolation
- Results in a working system (no broken intermediate states)
- Includes compilation and test verification
- Has a clear git commit message

**See detailed task breakdown in plan file**: `C:\Users\Alex\.claude\plans\rosy-painting-valiant.md`

## Benefits

### For Shell Implementers

**Before** (implement `ICustomShell`, ~500 LOC):
```csharp
public class MyShell : ICustomShell
{
    private Channel<byte[]> _outputChannel;
    private Task _outputPumpTask;
    private CancellationTokenSource _pumpCts;
    private StringBuilder _lineBuffer;
    private List<string> _commandHistory;
    private EscapeState _escapeState;
    // ... 200+ LOC of infrastructure ...

    public async Task StartAsync(...) { /* create channel, start pump */ }
    public async Task StopAsync(...) { /* drain channel, stop pump */ }
    public Task WriteInputAsync(...) { /* parse escape sequences, buffer input */ }
    private async Task OutputPumpAsync(...) { /* pump channel to events */ }
    // ... 300+ LOC of boilerplate ...

    private void ExecuteCommand(string cmd) { /* actual logic - 50 LOC */ }
}
```

**After** (extend `LineDisciplineShell`, ~50 LOC):
```csharp
public class MyShell : LineDisciplineShell
{
    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create("MyShell", "My custom shell");

    protected override void ExecuteCommand(string command)
    {
        // Just implement your logic!
        QueueOutput($"You said: {command}\r\n");
    }

    protected override string GetPrompt() => "my> ";
}
```

### For Testing

**Before**: Testing shell logic requires mocking channels, pumps, events
**After**: Testing shell logic only requires implementing `ExecuteCommand()`

**Before**: Unit tests and integration tests are intertwined
**After**: Each layer tested independently (unit tests), then integrated (integration tests)

### For Maintenance

**Before**: 500+ LOC monolith mixing concerns
**After**: 3 focused classes with single responsibilities

**Before**: Changes to infrastructure affect all shells
**After**: Changes to base layers automatically benefit all shells

## Usage Examples

### Example 1: Calculator Shell (Full Line Editing)

```csharp
using System.Data;
using caTTY.Core.Terminal;

public class CalculatorShell : LineDisciplineShell
{
    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create(
            name: "Calculator",
            description: "Simple calculator shell",
            version: new Version(1, 0, 0),
            author: "Example"
        );

    protected override void ExecuteCommand(string command)
    {
        try
        {
            var result = new DataTable().Compute(command, null);
            QueueOutput($"= {result}\r\n");
        }
        catch (Exception ex)
        {
            QueueOutput($"\x1b[31mError: {ex.Message}\x1b[0m\r\n");
        }
    }

    protected override string GetPrompt() => "calc> ";

    protected override string? GetBanner() =>
        "Calculator Shell - Enter expressions (e.g., 2 + 2)\r\n";
}
```

### Example 2: Echo Shell (Minimal, No Line Editing)

```csharp
public class EchoShell : BaseCustomShell
{
    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create("Echo", "Echoes input back");

    protected override void OnInputByte(byte b)
    {
        QueueOutput(new byte[] { b });
    }

    public override void SendInitialOutput()
    {
        QueueOutput("Echo Shell - Type to see echo\r\n");
    }
}
```

### Example 3: Command Shell (Raw Mode, No Echo)

```csharp
public class SilentShell : LineDisciplineShell
{
    public SilentShell() : base(LineDisciplineOptions.CreateRawMode())
    {
    }

    public override CustomShellMetadata Metadata { get; } =
        CustomShellMetadata.Create("Silent", "Silent command shell");

    protected override void ExecuteCommand(string command)
    {
        // Input not echoed, but command still executed on Enter
        QueueOutput($"Executed: {command}\r\n");
    }

    protected override string GetPrompt() => ""; // No prompt in raw mode
}
```

## Testing Strategy

### Unit Tests

**BaseCustomShell Tests** (`BaseCustomShellTests.cs`):
- ✅ `StartAsync` sets `IsRunning = true`
- ✅ `StartAsync` throws if already running
- ✅ `QueueOutput` fires `OutputReceived` events
- ✅ Output pump delivers data asynchronously
- ✅ `StopAsync` completes gracefully and drains channel
- ✅ Terminal resize updates dimensions
- ✅ `Dispose` stops pump and completes channel
- ✅ Thread safety (concurrent `QueueOutput` calls)

**LineDisciplineShell Tests** (`LineDisciplineShellTests.cs`):
- ✅ Input buffering accumulates characters
- ✅ Enter key triggers `ExecuteCommand`
- ✅ Backspace removes characters and sends erase sequence
- ✅ Up arrow navigates to previous command
- ✅ Down arrow navigates to next command
- ✅ History ignores consecutive duplicates
- ✅ History respects `MaxHistorySize` limit
- ✅ Ctrl+L clears screen
- ✅ Echo mode sends characters back
- ✅ Raw mode (`EchoInput=false`) doesn't echo
- ✅ Escape sequences parsed correctly
- ✅ `ParseEscapeSequences=false` disables parsing

### Integration Tests

**CustomShellIntegration Tests** (`CustomShellIntegrationTests.cs`):
- ✅ `CustomShellPtyBridge.StartAsync` starts `LineDisciplineShell`
- ✅ Input flows: bridge → shell → `ExecuteCommand`
- ✅ Output flows: shell → bridge → `DataReceived` event
- ✅ `SendInitialOutput` called after bridge wires events
- ✅ Resize notifications propagate correctly
- ✅ Stop flows through cleanly
- ✅ Dispose cleans up resources

### Manual Testing (GameConsoleShell)

After refactoring `GameConsoleShell`:
1. ✅ F12 toggles terminal
2. ✅ Commands execute correctly (e.g., `help`, `version`)
3. ✅ Output appears (normal and error colors)
4. ✅ Command history works (up/down arrows)
5. ✅ Ctrl+L clears screen
6. ✅ `clear` command clears screen + scrollback
7. ✅ Backspace editing works
8. ✅ Prompt loads from configuration

## Performance Considerations

### Memory

- **Before**: Each shell allocates channel, pump task, buffers (~10KB overhead)
- **After**: Same overhead (no change - infrastructure still needed)

### CPU

- **Before**: Background pump task per shell instance
- **After**: Same pattern (no change - pump is essential for async output)

### Latency

- **Before**: Channel latency for output (~1-5ms)
- **After**: Same channel latency (no change - necessary for decoupling)

**Conclusion**: No performance regression. The refactoring reorganizes code without changing the fundamental architecture.

## Future Extensions

### New Shell Types

With this architecture, adding new shell types is trivial:

1. **File Browser Shell**: Navigate filesystem with ls, cd, cat commands
2. **SQL Shell**: Execute SQL queries against game database
3. **Debug Shell**: Inspect game state, modify variables
4. **Script Shell**: Run Lua/Python scripts in game context
5. **Network Shell**: SSH client for remote game servers

All inherit from `LineDisciplineShell`, implement `ExecuteCommand()` - done!

### Potential Enhancements

- **Tab Completion**: Add `OnTabKey()` virtual method to `LineDisciplineShell`
- **Syntax Highlighting**: Add `HighlightLine(string)` virtual method
- **Multi-line Input**: Add `IsMultilineCommand(string)` virtual method
- **Custom Keybindings**: Add `LineDisciplineOptions.Keybindings` dictionary

## References

### Critical Files (Current Implementation)

- `caTTY.Core/Terminal/ICustomShell.cs` - Interface contract (will move to caTTY.ShellContract)
- `caTTY.Core/Terminal/CustomShellPtyBridge.cs` - PTY bridge adapter
- `caTTY.GameMod/GameConsoleShell.cs` - Current 500+ LOC implementation (will move to caTTY.CustomShells)

### New Projects

- **caTTY.ShellContract** - Shell contracts and base implementations
  - `BaseCustomShell.cs` - Base layer (200 LOC)
  - `LineDisciplineShell.cs` - Line discipline layer (300 LOC)
  - `LineDisciplineOptions.cs` - Configuration (30 LOC)
  - `ICustomShell.cs` - Interface (moved from caTTY.Core)
  - Supporting types (metadata, options, events)

- **caTTY.CustomShells** - Concrete shell implementations
  - `GameConsoleShell.cs` - Refactored (150 LOC, moved from caTTY.GameMod)
  - `Examples/` - Demo shells (Echo, Calculator, Raw)

### Documentation

- `caTTY.ShellContract/README.md` - Architecture overview and usage guide
- `CLAUDE.md` - Updated with custom shell workflow and project structure
- This file (`CUSTOM_SHELL_ABSTRACTIONS.md`) - Architecture specification

## Conclusion

This architecture provides:

1. ✅ **Clean Separation of Concerns**: PTY plumbing, line editing, application logic
2. ✅ **Opt-in Enhancement**: Line discipline is optional (use `BaseCustomShell` for raw mode)
3. ✅ **Simple Extension**: New shells implement one method (`ExecuteCommand`)
4. ✅ **Independent Testing**: Each layer tested in isolation
5. ✅ **Zero Breaking Changes**: Works within existing `ICustomShell` framework
6. ✅ **Reduced Complexity**: GameConsoleShell: 500+ LOC → 150 LOC

**Implementation**: See `C:\Users\Alex\.claude\plans\rosy-painting-valiant.md` for detailed task breakdown.
