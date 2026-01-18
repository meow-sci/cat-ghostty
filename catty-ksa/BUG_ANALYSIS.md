# Terminal Emulator Bug Analysis - Since Commit 412f7aa

## Root Cause

Commits after 412f7aa deleted two critical files containing types that are still actively used throughout the codebase:

### Deleted Files:
1. **ProcessEventArgs.cs** (94 lines) - Contained event argument classes
2. **ProcessExceptions.cs** (33 lines) - Contained custom exception classes

## Deleted Types Still In Use

### Deleted Event Argument Classes (from ProcessEventArgs.cs)
- `ShellOutputType` - enum
- `DataReceivedEventArgs` - event args for process output
- `ProcessExitedEventArgs` - event args for process exit
- `ProcessErrorEventArgs` - event args for process errors
- `ShellOutputEventArgs` - event args for shell output
- `ShellTerminatedEventArgs` - event args for shell termination

### Deleted Exception Classes (from ProcessExceptions.cs)
- `ProcessStartException` - thrown when process fails to start
- `ProcessWriteException` - thrown when writing to process fails
- (Note: `CustomShellStartException` was moved to caTTY.ShellContract, so it still exists)

## Code References to Deleted Types

### IProcessManager.cs
Lines using deleted types:
- Line 31: `ProcessStartException` (in XML doc)
- Line 46: `ProcessWriteException` (in XML doc)
- Line 68: `event EventHandler<DataReceivedEventArgs>?`
- Line 73: `event EventHandler<ProcessExitedEventArgs>?`
- Line 78: `event EventHandler<ProcessErrorEventArgs>?`

### ProcessManager.cs (Windows ConPTY implementation)
Lines using deleted types:
- Line 56: `event EventHandler<DataReceivedEventArgs>? DataReceived`
- Line 61: `event EventHandler<ProcessExitedEventArgs>? ProcessExited`
- Line 66: `event EventHandler<ProcessErrorEventArgs>? ProcessError`
- Also uses these in method implementations throughout the file

### CustomShellPtyBridge.cs (Custom shell PTY adapter)
Lines using deleted types:
- Line 68: `event EventHandler<DataReceivedEventArgs>? DataReceived`
- Line 71: `event EventHandler<ProcessExitedEventArgs>? ProcessExited`
- Line 74: `event EventHandler<ProcessErrorEventArgs>? ProcessError`
- Line 112-116: `ProcessStartException` exception handling
- Line 141: `ProcessErrorEventArgs` construction
- Line 167: `ProcessWriteException` exception
- Line 204: `ProcessErrorEventArgs` construction
- Line 213: `ShellOutputEventArgs` event handler parameter
- Line 218: `ShellOutputType.Stderr` enum usage
- Line 219: `DataReceivedEventArgs` construction
- Line 223: `ProcessErrorEventArgs` construction
- Line 232: `ShellTerminatedEventArgs` event handler parameter
- Line 245: `ProcessExitedEventArgs` construction
- Line 249: `ProcessErrorEventArgs` construction
- Line 309: `ProcessErrorEventArgs` construction

## Impact

The terminal emulator will fail to compile or execute because:
1. `IProcessManager` interface defines events using deleted event argument classes
2. Both `ProcessManager` and `CustomShellPtyBridge` try to implement these events using the deleted types
3. Throughout the codebase, these deleted exception types are thrown and caught
4. Custom shell event arguments are used but don't have matching types in the main terminal infrastructure

## Solution

The deleted types need to be restored to ProcessEventArgs.cs and ProcessExceptions.cs, OR they need to be moved to a new location and all references need to be updated.

Most likely they should be:
1. Restored to their original files, OR
2. Moved to caTTY.ShellContract project where CustomShellStartException was moved, OR
3. Integrated into the custom shell contract types

The current state is broken and will not compile.
