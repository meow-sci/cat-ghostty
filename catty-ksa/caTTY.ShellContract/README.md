# caTTY.ShellContract

**Shell contracts and base implementations for the caTTY terminal emulator.**

This library provides a three-layer architecture for building custom terminal shells, dramatically simplifying shell development by providing reusable infrastructure for PTY plumbing, line discipline, and command processing.

## Architecture Overview

The custom shell architecture consists of three layers:

```
┌──────────────────────────────────────────────────────────┐
│  ICustomShell (interface)                                │
│  - Contract for custom shell implementations             │
└──────────────────────────────────────────────────────────┘
                           ▲
                           │
┌──────────────────────────────────────────────────────────┐
│  BaseCustomShell (abstract, ~200 LOC)                    │
│  - PTY infrastructure layer                              │
│  - Channel-based output pump                             │
│  - Event wiring (OutputReceived, Terminated)             │
│  - Thread-safe state management                          │
│  - Lifecycle hooks (OnShellStartingAsync, etc.)          │
│                                                           │
│  Abstract: OnInputByte(byte)                             │
└──────────────────────────────────────────────────────────┘
                           ▲
                           │
┌──────────────────────────────────────────────────────────┐
│  LineDisciplineShell (abstract, ~300 LOC)                │
│  - Line discipline layer                                 │
│  - Input buffering (StringBuilder)                       │
│  - Command history with navigation (up/down arrows)      │
│  - Escape sequence parsing (CSI, arrows, Ctrl+L)         │
│  - Line editing (backspace, echo)                        │
│  - Configurable via LineDisciplineOptions                │
│                                                           │
│  Abstract: ExecuteCommand(string)                        │
│  Virtual: GetPrompt(), GetBanner()                       │
└──────────────────────────────────────────────────────────┘
                           ▲
                           │
┌──────────────────────────────────────────────────────────┐
│  YourCustomShell (concrete, ~50-150 LOC)                 │
│  - Application logic layer                               │
│  - Implement ExecuteCommand(string)                      │
│  - Optionally override GetPrompt(), GetBanner()          │
│                                                           │
│  Examples: GameConsoleShell, CalculatorShell             │
└──────────────────────────────────────────────────────────┘
```

## When to Use Which Layer

### Extend BaseCustomShell When:
- You need **raw byte-level processing** (e.g., binary protocols, custom editors)
- You want to implement your own line discipline from scratch
- You don't need command history or line editing

**Effort:** ~200 LOC to implement basic shell with custom input handling

### Extend LineDisciplineShell When:
- You want a **command-line interface** with standard features
- You need line editing, backspace, and command history
- You want to focus only on command execution logic

**Effort:** ~50 LOC to implement functional shell with full line editing

## Quick Start Examples

### Example 1: Minimal Shell (BaseCustomShell)

Simple echo shell that immediately echoes back input bytes:

```csharp
using caTTY.ShellContract;

public class EchoShell : BaseCustomShell
{
    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Echo Shell",
        "Echoes input bytes back");

    protected override void OnInputByte(byte b)
    {
        // Echo byte immediately
        QueueOutput(new[] { b });
    }

    protected override string? GetInitialOutput()
    {
        return "Echo Shell - Type anything\r\n";
    }
}
```

### Example 2: Command-Line Shell (LineDisciplineShell)

Calculator shell with full line editing and history:

```csharp
using System.Data;
using caTTY.ShellContract;

public class CalculatorShell : LineDisciplineShell
{
    public CalculatorShell() : base(LineDisciplineOptions.CreateDefault())
    {
    }

    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Calculator Shell",
        "Mathematical expression evaluator");

    protected override void ExecuteCommand(string command)
    {
        try
        {
            var table = new DataTable();
            var result = table.Compute(command, null);
            QueueOutput($"= {result}\r\n");
        }
        catch (Exception ex)
        {
            QueueOutput($"Error: {ex.Message}\r\n");
        }
    }

    protected override string GetPrompt() => "calc> ";

    protected override string? GetBanner() =>
        "Calculator Shell - Enter expressions (e.g., 2 + 2)\r\n";
}
```

### Example 3: Raw Mode Shell (LineDisciplineShell)

Shell with no echo and no history:

```csharp
using caTTY.ShellContract;

public class RawShell : LineDisciplineShell
{
    public RawShell() : base(LineDisciplineOptions.CreateRawMode())
    {
    }

    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Raw Shell",
        "No echo, no history mode");

    protected override void ExecuteCommand(string command)
    {
        QueueOutput($"You typed: {command}\r\n");
    }

    protected override string GetPrompt() => "> ";
}
```

## API Reference

### BaseCustomShell

**Protected Methods for Subclasses:**

```csharp
// Output methods
protected void QueueOutput(byte[] data)
protected void QueueOutput(string text)

// Abstract - must implement
protected abstract void OnInputByte(byte b)

// Virtual lifecycle hooks - can override
protected virtual Task OnShellStartingAsync(CustomShellStartOptions options, CancellationToken ct)
protected virtual Task OnShellStoppingAsync(CancellationToken ct)
protected virtual string? GetInitialOutput()
```

**Properties:**
- `bool IsRunning` - Shell running state
- `CustomShellMetadata Metadata` - Shell metadata (override required)

**Events:**
- `EventHandler<ShellOutputEventArgs> OutputReceived` - Fired when output is available
- `EventHandler<ShellTerminatedEventArgs> Terminated` - Fired when shell terminates

### LineDisciplineShell

**Extends BaseCustomShell with:**

```csharp
// Abstract - must implement
protected abstract void ExecuteCommand(string command)

// Virtual - can override
protected virtual string GetPrompt()  // Default: "$ "
protected virtual string? GetBanner()  // Default: null

// Protected helpers available to subclasses
protected void SendPrompt()
protected void ClearScreen()  // Preserves scrollback
protected void ClearScreenAndScrollback()  // Erases everything
```

**Constructor:**
```csharp
protected LineDisciplineShell(LineDisciplineOptions options)
```

### LineDisciplineOptions

**Configuration class for line discipline behavior:**

```csharp
public class LineDisciplineOptions
{
    public int MaxHistorySize { get; set; } = 100;
    public bool EchoInput { get; set; } = true;
    public bool EnableHistory { get; set; } = true;
    public bool ParseEscapeSequences { get; set; } = true;

    // Factory methods
    public static LineDisciplineOptions CreateDefault()
    public static LineDisciplineOptions CreateRawMode()
}
```

**Presets:**
- `CreateDefault()` - All features enabled (echo, history, escape parsing, 100 command history)
- `CreateRawMode()` - All features disabled (no echo, no history, no escape parsing)

## Handling Asynchronous External Output

If your shell needs to capture output from external sources (e.g., Harmony patches, callbacks from game engines, or other asynchronous systems), use `QueueOutputUnchecked()` instead of `QueueOutput()`:

```csharp
// Example: Harmony patch capturing game console output
internal static void OnConsolePrint(string output)
{
    lock (_activeLock)
    {
        if (_activeInstance != null)
        {
            // Use QueueOutputUnchecked for external output that may arrive
            // during shell state transitions
            _activeInstance.QueueOutputUnchecked($"{output}\r\n");
        }
    }
}
```

**When to use `QueueOutputUnchecked()`:**
- ✅ Harmony patches capturing output from external systems
- ✅ Callbacks from game engines or frameworks
- ✅ Asynchronous output that may arrive during shell startup/shutdown
- ❌ Normal shell output (use `QueueOutput()` instead)

**Why:** `QueueOutputUnchecked()` bypasses the `IsRunning` check, allowing output to be captured even during state transitions. Normal `QueueOutput()` has protective guards that would drop external output arriving at inopportune times.

**Protected Methods:**
```csharp
protected void QueueOutputUnchecked(byte[] data)
protected void QueueOutputUnchecked(string text)
```

Both methods still respect disposal state to prevent writing to disposed channels, but skip the `IsRunning` check to handle asynchronous external sources correctly.

## Features Provided by LineDisciplineShell

### Input Processing
- **Line buffering:** Characters accumulate until Enter is pressed
- **Echo:** Input characters displayed as typed (configurable)
- **Backspace:** Remove characters with visual feedback (sends `\b \b` sequence)
- **Enter:** Execute accumulated command line

### Command History
- **Up arrow (ESC[A):** Navigate to previous command
- **Down arrow (ESC[B):** Navigate to next command or restore current line
- **History size limit:** Configurable max history entries (default: 100)
- **Duplicate suppression:** Consecutive identical commands not stored

### Escape Sequences
- **Ctrl+L (0x0C):** Clear screen (preserves scrollback)
- **CSI sequences:** Parsed via state machine (None → ESC → CSI → final byte)
- **Configurable:** Can disable escape parsing for raw mode

### Prompt and Banner
- **Custom prompt:** Override `GetPrompt()` to customize (default: "$ ")
- **Startup banner:** Override `GetBanner()` to show welcome message
- **Auto-prompt:** Prompt displayed after each command execution

## Output Formatting

### ANSI Color Codes

Shells can use ANSI escape sequences for colored output:

```csharp
protected override void ExecuteCommand(string command)
{
    // Red error text
    QueueOutput("\x1b[31mError: Command failed\x1b[0m\r\n");

    // Green success text
    QueueOutput("\x1b[32mSuccess!\x1b[0m\r\n");

    // Cyan info text
    QueueOutput("\x1b[36mInformation\x1b[0m\r\n");
}
```

**Common ANSI codes:**
- `\x1b[31m` - Red
- `\x1b[32m` - Green
- `\x1b[33m` - Yellow
- `\x1b[36m` - Cyan
- `\x1b[0m` - Reset to default

### Line Endings

Always use `\r\n` (CRLF) for line endings in terminal output:

```csharp
QueueOutput("Line 1\r\n");
QueueOutput("Line 2\r\n");
```

## Integration with Terminal Emulator

Shells are integrated via `CustomShellPtyBridge`:

```csharp
using var shell = new YourCustomShell();
using var bridge = new CustomShellPtyBridge(shell);

var options = new ProcessLaunchOptions
{
    InitialWidth = 80,
    InitialHeight = 24
};

await bridge.StartAsync(options);
bridge.SendInitialOutput();  // Send banner + prompt

// Input routing
bridge.Write(Encoding.UTF8.GetBytes("command\r"));

// Output handling
bridge.DataReceived += (sender, args) =>
{
    // Process output bytes
};

await bridge.StopAsync();
```

## Thread Safety

Both `BaseCustomShell` and `LineDisciplineShell` are **thread-safe**:

- `QueueOutput()` can be called from any thread
- Output pump runs on background task
- State changes protected by locks
- Channel-based architecture prevents race conditions

## Testing

Example test for a custom shell:

```csharp
[Test]
public async Task MyShell_ExecutesCommands()
{
    using var shell = new MyCustomShell();
    var receivedData = new ConcurrentQueue<byte[]>();
    shell.OutputReceived += (sender, args) => receivedData.Enqueue(args.Data.ToArray());

    await shell.StartAsync(CustomShellStartOptions.CreateDefault());

    await shell.WriteInputAsync(Encoding.UTF8.GetBytes("test\r"));
    await Task.Delay(100); // Wait for processing

    await shell.StopAsync();

    var allOutput = string.Concat(receivedData.Select(chunk => Encoding.UTF8.GetString(chunk)));
    Assert.That(allOutput, Does.Contain("expected output"));
}
```

## License

Part of the caTTY terminal emulator project.
