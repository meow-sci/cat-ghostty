# Design Document

## Overview

This design implements a custom shell system that allows C# implementations to integrate seamlessly with the existing PTY infrastructure. Custom shells behave like real terminal shells while being backed by custom code, enabling game-specific terminal interfaces that can use standard TUI libraries and terminal escape sequences.

The system extends the existing shell selection mechanism to include custom shell types alongside traditional shells (PowerShell, WSL, CMD). Custom shells integrate through a standardized interface and bridge component that handles PTY communication.

## Architecture

The custom shell system follows a layered architecture that integrates with the existing caTTY terminal infrastructure:

```
┌─────────────────────────────────────────────────────────────┐
│                    Shell Selection UI                        │
├─────────────────────────────────────────────────────────────┤
│                    Shell Registry                           │
├─────────────────────────────────────────────────────────────┤
│  Standard Shells  │              Custom Shells              │
│  ┌─────────────┐  │  ┌─────────────┐  ┌─────────────────┐  │
│  │ PowerShell  │  │  │ Game RCS    │  │ Other Custom    │  │
│  │ WSL         │  │  │ Shell       │  │ Shells          │  │
│  │ CMD         │  │  └─────────────┘  └─────────────────┘  │
│  └─────────────┘  │         │                 │            │
├─────────────────────────────────────────────────────────────┤
│                    PTY Bridge                               │
├─────────────────────────────────────────────────────────────┤
│                 Process Manager                             │
├─────────────────────────────────────────────────────────────┤
│                Terminal Emulator                            │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **ICustomShell Interface**: Standardized contract for custom shell implementations
2. **PTY Bridge**: Glue code that connects custom shells to the PTY mechanism
3. **Shell Registry**: Discovery and management system for available shell types
4. **Custom Process Manager**: Specialized process manager for custom shell lifecycle
5. **Game RCS Shell**: Reference implementation demonstrating custom shell capabilities

## Components and Interfaces

### ICustomShell Interface

```csharp
public interface ICustomShell : IDisposable
{
    /// <summary>
    /// Gets the shell metadata including name, description, and version.
    /// </summary>
    CustomShellMetadata Metadata { get; }
    
    /// <summary>
    /// Gets whether the shell is currently running.
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Event raised when the shell produces output data.
    /// </summary>
    event EventHandler<ShellOutputEventArgs>? OutputReceived;
    
    /// <summary>
    /// Event raised when the shell terminates.
    /// </summary>
    event EventHandler<ShellTerminatedEventArgs>? Terminated;
    
    /// <summary>
    /// Starts the custom shell with the specified options.
    /// </summary>
    Task StartAsync(CustomShellStartOptions options, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Stops the custom shell gracefully.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends input data to the shell.
    /// </summary>
    Task WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Notifies the shell of terminal size changes.
    /// </summary>
    void NotifyTerminalResize(int width, int height);
    
    /// <summary>
    /// Requests graceful cancellation of long-running operations.
    /// </summary>
    void RequestCancellation();
}
```

### PTY Bridge Implementation

The PTY Bridge acts as an adapter between custom shells and the existing PTY infrastructure:

```csharp
public class CustomShellPtyBridge : IProcessManager
{
    private readonly ICustomShell _customShell;
    private readonly TaskCompletionSource<int> _exitCodeSource;
    private bool _isRunning;
    
    public CustomShellPtyBridge(ICustomShell customShell)
    {
        _customShell = customShell;
        _exitCodeSource = new TaskCompletionSource<int>();
        
        // Wire up custom shell events to PTY events
        _customShell.OutputReceived += OnCustomShellOutput;
        _customShell.Terminated += OnCustomShellTerminated;
    }
    
    // Implement IProcessManager interface to integrate with existing infrastructure
    public bool IsRunning => _isRunning;
    public int? ProcessId => _isRunning ? Environment.ProcessId : null;
    public int? ExitCode => _exitCodeSource.Task.IsCompleted ? _exitCodeSource.Task.Result : null;
    
    public event EventHandler<DataReceivedEventArgs>? DataReceived;
    public event EventHandler<ProcessExitedEventArgs>? ProcessExited;
    public event EventHandler<ProcessErrorEventArgs>? ProcessError;
    
    public async Task StartAsync(ProcessLaunchOptions options, CancellationToken cancellationToken = default)
    {
        var customOptions = new CustomShellStartOptions
        {
            InitialWidth = options.InitialWidth,
            InitialHeight = options.InitialHeight,
            WorkingDirectory = options.WorkingDirectory,
            EnvironmentVariables = options.EnvironmentVariables
        };
        
        await _customShell.StartAsync(customOptions, cancellationToken);
        _isRunning = true;
    }
    
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _customShell.StopAsync(cancellationToken);
        _isRunning = false;
    }
    
    public void Write(ReadOnlySpan<byte> data)
    {
        _ = _customShell.WriteInputAsync(data.ToArray(), CancellationToken.None);
    }
    
    public void Resize(int width, int height)
    {
        _customShell.NotifyTerminalResize(width, height);
    }
    
    private void OnCustomShellOutput(object? sender, ShellOutputEventArgs e)
    {
        DataReceived?.Invoke(this, new DataReceivedEventArgs(e.Data));
    }
    
    private void OnCustomShellTerminated(object? sender, ShellTerminatedEventArgs e)
    {
        _isRunning = false;
        _exitCodeSource.SetResult(e.ExitCode);
        ProcessExited?.Invoke(this, new ProcessExitedEventArgs(e.ExitCode, Environment.ProcessId));
    }
}
```

### Shell Registry System

The Shell Registry manages discovery and instantiation of custom shells:

```csharp
public class CustomShellRegistry
{
    private readonly Dictionary<string, Func<ICustomShell>> _shellFactories = new();
    private readonly Dictionary<string, CustomShellMetadata> _shellMetadata = new();
    
    public void RegisterShell<T>(string shellId, Func<T> factory) where T : ICustomShell
    {
        var instance = factory();
        try
        {
            _shellMetadata[shellId] = instance.Metadata;
            _shellFactories[shellId] = () => factory();
        }
        finally
        {
            instance.Dispose();
        }
    }
    
    public IEnumerable<(string Id, CustomShellMetadata Metadata)> GetAvailableShells()
    {
        return _shellMetadata.Select(kvp => (kvp.Key, kvp.Value));
    }
    
    public ICustomShell CreateShell(string shellId)
    {
        if (!_shellFactories.TryGetValue(shellId, out var factory))
        {
            throw new ArgumentException($"Unknown shell type: {shellId}");
        }
        
        return factory();
    }
}
```

### Extended Shell Type System

The existing ShellType enum is extended to support custom shells:

```csharp
public enum ShellType
{
    Auto,
    PowerShell,
    Wsl,
    PowerShellCore,
    Cmd,
    Custom,
    CustomGame  // New: Custom game shell type
}
```

The ProcessLaunchOptions class is extended to support custom shell selection:

```csharp
public class ProcessLaunchOptions
{
    // Existing properties...
    
    /// <summary>
    /// Gets or sets the custom shell ID when using ShellType.CustomGame.
    /// </summary>
    public string? CustomShellId { get; set; }
}
```

## Data Models

### Custom Shell Metadata

```csharp
public record CustomShellMetadata(
    string Name,
    string Description,
    Version Version,
    string Author,
    string[] SupportedFeatures
);
```

### Shell Events

```csharp
public class ShellOutputEventArgs : EventArgs
{
    public byte[] Data { get; }
    public ShellOutputType OutputType { get; }  // Stdout, Stderr
    
    public ShellOutputEventArgs(byte[] data, ShellOutputType outputType = ShellOutputType.Stdout)
    {
        Data = data;
        OutputType = outputType;
    }
}

public class ShellTerminatedEventArgs : EventArgs
{
    public int ExitCode { get; }
    public string? Reason { get; }
    
    public ShellTerminatedEventArgs(int exitCode, string? reason = null)
    {
        ExitCode = exitCode;
        Reason = reason;
    }
}
```

### Custom Shell Start Options

```csharp
public class CustomShellStartOptions
{
    public int InitialWidth { get; set; } = 80;
    public int InitialHeight { get; set; } = 24;
    public string? WorkingDirectory { get; set; }
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Custom Shell Interface Compliance
*For any* custom shell implementation, it must implement the ICustomShell interface and provide valid metadata including name, description, and version information
**Validates: Requirements 1.1, 5.1, 7.3**

### Property 2: PTY Bridge Integration
*For any* custom shell instance, when connected through the PTY bridge, input from the terminal should reach the shell's input handler and output from the shell should reach the terminal emulator
**Validates: Requirements 1.2, 1.4, 3.3**

### Property 3: Terminal Escape Sequence Processing
*For any* valid terminal escape sequence (ANSI, CSI, SGR) output by a custom shell, the terminal emulator should process it identically to sequences from standard shells
**Validates: Requirements 1.3, 2.1, 2.2, 2.3**

### Property 4: Terminal I/O Stream Support
*For any* custom shell, it should support standard terminal I/O patterns with separate stdout and stderr streams that are properly routed through the PTY bridge
**Validates: Requirements 1.5**

### Property 5: Terminal Resize Notification
*For any* terminal resize operation, all active custom shells should receive notification of the new dimensions through their NotifyTerminalResize method
**Validates: Requirements 2.4, 2.5**

### Property 6: Shell Selection and Instantiation
*For any* registered custom shell type, when selected through the shell selection UI, the system should instantiate the correct shell implementation and establish PTY connection
**Validates: Requirements 3.1, 3.2, 3.4**

### Property 7: Resource Cleanup During Shell Switching
*For any* shell switching operation, the previous shell instance should be properly disposed and its resources cleaned up before the new shell is activated
**Validates: Requirements 3.5**

### Property 8: Process Lifecycle Management
*For any* custom shell, the process manager should track it as an active process during its lifetime and clean up associated resources when it terminates
**Validates: Requirements 4.1, 4.2, 4.3**

### Property 9: Graceful Shutdown Support
*For any* custom shell, it should respond to termination requests through the standard shell termination process and support graceful shutdown
**Validates: Requirements 4.4**

### Property 10: Error Handling and Recovery
*For any* custom shell that crashes or fails, the system should handle the error gracefully, log appropriate messages, and allow shell restart without affecting other sessions
**Validates: Requirements 4.5**

### Property 11: Asynchronous Operation Support
*For any* custom shell executing long-running operations, the terminal emulator should remain responsive and the shell should be able to output results asynchronously
**Validates: Requirements 6.1, 6.2, 6.3**

### Property 12: Concurrent Operation Safety
*For any* custom shell, the PTY bridge should handle concurrent input and output operations safely without data corruption or race conditions
**Validates: Requirements 6.4**

### Property 13: Operation Cancellation Support
*For any* custom shell with long-running operations, when the terminal is closed or cancellation is requested, the operations should be cancelled gracefully
**Validates: Requirements 6.5**

### Property 14: Automatic Shell Discovery
*For any* custom shell implementation in the application domain, the shell registry should automatically discover it at startup and make it available for selection
**Validates: Requirements 7.1, 7.2**

### Property 15: Shell Registration Validation
*For any* custom shell registration attempt, the shell registry should validate the implementation before registration and handle failures gracefully with appropriate error logging
**Validates: Requirements 7.4, 7.5**

## Error Handling

### Custom Shell Errors

1. **Shell Startup Failures**: If a custom shell fails to start, the PTY bridge should report the error through the ProcessError event and allow retry or fallback to another shell type.

2. **Runtime Exceptions**: Unhandled exceptions in custom shells should be caught by the PTY bridge and reported as process errors without crashing the terminal session.

3. **Communication Failures**: If communication between the PTY bridge and custom shell fails, the bridge should attempt graceful recovery or terminate the shell cleanly.

### Registration Errors

1. **Invalid Implementations**: Custom shells that don't properly implement the ICustomShell interface should be rejected during registration with detailed error messages.

2. **Duplicate Registrations**: Attempting to register multiple shells with the same ID should result in an error with clear guidance on resolution.

3. **Missing Metadata**: Custom shells without required metadata should be rejected with specific information about missing fields.

### Resource Management Errors

1. **Disposal Failures**: If custom shell disposal fails, the error should be logged but not prevent session cleanup from continuing.

2. **Memory Leaks**: The PTY bridge should ensure proper cleanup of event handlers and resources even if custom shells don't dispose correctly.

## Testing Strategy

### Unit Testing

Unit tests will focus on individual components and their specific behaviors:

- **ICustomShell Interface**: Test that implementations conform to the contract
- **PTY Bridge**: Test input/output routing and event handling
- **Shell Registry**: Test registration, discovery, and instantiation
- **Custom Shell Metadata**: Test validation and serialization
- **Error Handling**: Test various failure scenarios and recovery

### Property-Based Testing

Property-based tests will validate universal behaviors across all inputs:

- **Escape Sequence Processing**: Generate random valid escape sequences and verify consistent processing
- **Input/Output Routing**: Test with random input data and verify correct routing
- **Concurrent Operations**: Test thread safety with concurrent input/output operations
- **Resource Cleanup**: Verify proper cleanup across different termination scenarios
- **Shell Discovery**: Test automatic discovery with various shell implementations

Each property test will run a minimum of 100 iterations to ensure comprehensive coverage through randomization. Tests will be tagged with comments referencing their corresponding design properties:

```csharp
[Test]
public void CustomShellInterfaceCompliance()
{
    // Feature: custom-game-shells, Property 1: Custom Shell Interface Compliance
    // Test implementation...
}
```

### Integration Testing

Integration tests will verify end-to-end functionality:

- **Shell Selection Workflow**: Test complete user workflow from selection to shell startup
- **Terminal Integration**: Test custom shells with real terminal emulator instances
- **Session Management**: Test custom shells within the session management system
- **Multi-Shell Scenarios**: Test switching between different custom shell types

The testing strategy ensures both specific examples work correctly (unit tests) and universal properties hold across all inputs (property tests), providing comprehensive validation of the custom shell system.