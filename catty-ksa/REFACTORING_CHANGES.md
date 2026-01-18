# Terminal Emulator Refactoring Changes (Since 412f7aa)

## Summary
The custom shell refactoring moved `GameConsoleShell` from a 725-line implementation directly in `caTTY.GameMod/` to a new 201-line implementation in `caTTY.CustomShells/`, with a new three-layer architecture:
- `BaseCustomShell` (333 LOC) - PTY infrastructure, output channel pump, event wiring
- `LineDisciplineShell` (385 LOC) - Input buffering, command history, escape sequence parsing
- `GameConsoleShell` (201 LOC) - Game-specific implementation

## Major Architectural Changes

### 1. Output Channel System
**Before (old GameConsoleShell):** Direct event raising and output handling inline
**After (BaseCustomShell):**
```csharp
// Channel-based pattern - shell writes to channel, pump reads and raises events
private Channel<byte[]>? _outputChannel;
private Task? _outputPumpTask;
```

This is a significant change that affects:
- How output is queued and delivered
- Event timing and order
- Buffering behavior

### 2. Line Discipline Layer
**Before:** All input handling mixed in GameConsoleShell
**After:** Extracted to `LineDisciplineShell` with:
- Input buffering via `LineDisciplineOptions`
- Unified escape sequence parsing (Ctrl+L, CSI sequences)
- Configurable echo mode, history, etc.

### 3. Custom Shell Event Arguments
**Before:** Used `ShellOutputEventArgs` and `ShellTerminatedEventArgs` from custom shell code
**After:** Same classes but now in `caTTY.ShellContract` namespace

## Files Moved/Removed/Created

### Deleted from caTTY.GameMod:
- `GameConsoleShell.cs` (725 LOC) - Entire implementation

### Created in caTTY.ShellContract:
- `BaseCustomShell.cs` (333 LOC) - PTY infrastructure base
- `LineDisciplineShell.cs` (385 LOC) - Line editing and history base
- `ICustomShell.cs` (updated interface)
- `ShellEventArgs.cs` (137 LOC) - Event argument classes
- `LineDisciplineOptions.cs` (66 LOC) - Configuration
- `CustomShellStartException.cs` (34 LOC) - Exception class
- `CustomShellStartOptions.cs` (updated)
- `CustomShellMetadata.cs` (updated)

### Created in caTTY.CustomShells:
- `GameConsoleShell.cs` (201 LOC) - Simplified implementation inheriting from `LineDisciplineShell`
- `Examples/` - New example shells (CalculatorShell, EchoShell, RawShell)

### Created in caTTY.Core:
- `CustomShellRegistry.cs` - Shell discovery/registration
- `CustomShellPtyBridge.cs` - Adapter between `ICustomShell` and `IProcessManager`

## Behavioral Changes to Investigate

### 1. Output Channel Buffering
The output channel pump may batch or delay output compared to the old inline approach:
- Check: Is output appearing correctly in real-time?
- Check: Are there buffering delays?
- Check: Is the pump terminating correctly?

### 2. Input Processing Order
The new `LineDisciplineShell` changes when input is processed:
- **Before:** Input directly processed in `OnInputByte()`
- **After:** Input buffered, line-edited, THEN passed to `ExecuteCommand()`
- Risk: Lost/reordered input bytes, especially during rapid input

### 3. Escape Sequence Handling
**Before:** Custom escape sequence parsing in GameConsoleShell
**After:** Moved to `LineDisciplineShell.HandleEscapeSequence()`
- Check: Are terminal control sequences (Ctrl+L, arrow keys) working?
- Check: Is CSI parsing correct?

### 4. Event Wiring
The `CustomShellPtyBridge` adapts `ICustomShell` events to `IProcessManager` events:
```csharp
_customShell.OutputReceived += OnCustomShellOutput;
_customShell.Terminated += OnCustomShellTerminated;
```
- Check: Are these events being raised?
- Check: Is the mapping from `ShellOutputEventArgs` to `DataReceivedEventArgs` correct?

### 5. Shell Options Conversion
`CustomShellPtyBridge` converts `ProcessLaunchOptions` to `CustomShellStartOptions`:
```csharp
var customOptions = new CustomShellStartOptions {
    InitialWidth = options.InitialWidth,
    InitialHeight = options.InitialHeight,
    WorkingDirectory = options.WorkingDirectory,
    EnvironmentVariables = new Dictionary<string, string>(options.EnvironmentVariables)
};
```
- Check: Are all options being passed through correctly?
- Check: Are there missing options?

### 6. Process ID Handling
The old code tracked real process IDs from ConPTY. The new code uses a placeholder:
```csharp
_processId = Environment.ProcessId;  // Placeholder for custom shells
```
- Check: Could this cause issues with process tracking?
- Check: Are there race conditions with `_processId` updates?

### 7. Initialization Flow
**Before:** Shell initialized directly in GameMod
**After:** Shell auto-discovered via `CustomShellRegistry.DiscoverShells()`
- Check: Is the discovery working?
- Check: Is the shell being loaded when sessions are created?

## Critical Questions

1. **What specifically is broken?** (Output not appearing? Terminal not responding? Crash?)
2. **When does it fail?** (At startup? When typing? When running specific commands?)
3. **Are there error messages or exceptions?**
4. **Does the test suite pass?** (Run `.\scripts\dotnet-test.ps1`)

## Recommended Investigation Path

1. Run tests: `.\scripts\dotnet-test.ps1`
2. Run the game mod and check console output for errors
3. Check if output appears at all
4. Test line editing (backspace, arrows) vs command execution
5. Verify the output channel pump is running
