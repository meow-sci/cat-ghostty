using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace caTTY.Core.Terminal;

/// <summary>
/// Manages shell processes and provides bidirectional data flow.
/// Uses System.Diagnostics.Process for cross-platform shell spawning.
/// </summary>
public class ProcessManager : IProcessManager
{
    private Process? _process;
    private readonly object _processLock = new();
    private CancellationTokenSource? _readCancellationSource;
    private Task? _stdoutReadTask;
    private Task? _stderrReadTask;
    private bool _disposed;

    /// <summary>
    /// Gets whether a shell process is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                if (_process == null)
                    return false;
                
                try
                {
                    return !_process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    // Process has been disposed
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Gets the process ID of the running shell, or null if no process is running.
    /// </summary>
    public int? ProcessId
    {
        get
        {
            lock (_processLock)
            {
                return _process?.Id;
            }
        }
    }

    /// <summary>
    /// Gets the exit code of the last process, or null if no process has exited.
    /// </summary>
    public int? ExitCode
    {
        get
        {
            lock (_processLock)
            {
                return _process?.HasExited == true ? _process.ExitCode : null;
            }
        }
    }

    /// <summary>
    /// Event raised when data is received from the shell process stdout/stderr.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    /// Event raised when the shell process exits.
    /// </summary>
    public event EventHandler<ProcessExitedEventArgs>? ProcessExited;

    /// <summary>
    /// Event raised when an error occurs during process operations.
    /// </summary>
    public event EventHandler<ProcessErrorEventArgs>? ProcessError;

    /// <summary>
    /// Starts a new shell process with the specified options.
    /// </summary>
    /// <param name="options">Launch options for the shell process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the process has started</returns>
    /// <exception cref="InvalidOperationException">Thrown if a process is already running</exception>
    /// <exception cref="ProcessStartException">Thrown if the process fails to start</exception>
    public async Task StartAsync(ProcessLaunchOptions options, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        lock (_processLock)
        {
            if (_process != null)
            {
                throw new InvalidOperationException("A process is already running. Stop the current process before starting a new one.");
            }
        }

        try
        {
            var (shellPath, shellArgs) = ResolveShellCommand(options);
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = shellPath,
                Arguments = shellArgs?.Length > 0 ? string.Join(" ", shellArgs) : string.Empty,
                WorkingDirectory = options.WorkingDirectory ?? Environment.CurrentDirectory,
                UseShellExecute = options.UseShellExecute,
                CreateNoWindow = !options.CreateWindow,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            // Set environment variables
            foreach (var kvp in options.EnvironmentVariables)
            {
                processStartInfo.Environment[kvp.Key] = kvp.Value;
            }

            // Set terminal dimensions if supported (Windows only for now)
            if (OperatingSystem.IsWindows())
            {
                processStartInfo.Environment["COLUMNS"] = options.InitialWidth.ToString();
                processStartInfo.Environment["LINES"] = options.InitialHeight.ToString();
            }

            var process = new Process { StartInfo = processStartInfo };

            // Set up process exit handling
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            // Start the process
            if (!process.Start())
            {
                throw new ProcessStartException($"Failed to start shell process: {shellPath}", shellPath);
            }

            // Validate that the process started correctly
            if (process.StandardOutput == null || process.StandardError == null || process.StandardInput == null)
            {
                process.Kill();
                process.Dispose();
                throw new ProcessStartException($"Process streams not available for shell: {shellPath}", shellPath);
            }

            lock (_processLock)
            {
                _process = process;
                _readCancellationSource = new CancellationTokenSource();
            }

            // Start reading stdout and stderr asynchronously
            var readToken = _readCancellationSource.Token;
            _stdoutReadTask = ReadStreamAsync(process.StandardOutput.BaseStream, false, readToken);
            _stderrReadTask = ReadStreamAsync(process.StandardError.BaseStream, true, readToken);

            // Wait a short time to ensure the process started successfully
            await Task.Delay(100, cancellationToken);

            // Check if process exited immediately (but handle the case where it's already disposed)
            try
            {
                if (process.HasExited)
                {
                    var exitCode = process.ExitCode;
                    CleanupProcess();
                    throw new ProcessStartException($"Shell process exited immediately with code {exitCode}: {shellPath}", shellPath);
                }
            }
            catch (InvalidOperationException)
            {
                // Process has already exited and been disposed - this is actually normal for short-lived commands
                // We'll let the process exit event handler deal with cleanup
            }
        }
        catch (Exception ex) when (!(ex is ProcessStartException))
        {
            CleanupProcess();
            throw new ProcessStartException($"Failed to start shell process: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Stops the currently running shell process.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the process has stopped</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        Process? processToStop = null;
        CancellationTokenSource? cancellationSource = null;

        lock (_processLock)
        {
            processToStop = _process;
            cancellationSource = _readCancellationSource;
        }

        if (processToStop == null)
        {
            return; // No process running
        }

        try
        {
            // Cancel read operations
            cancellationSource?.Cancel();

            // Try graceful shutdown first
            if (!processToStop.HasExited)
            {
                try
                {
                    // Send Ctrl+C to the process (Windows)
                    if (OperatingSystem.IsWindows())
                    {
                        // For Windows, we'll use CloseMainWindow first, then Kill if needed
                        processToStop.CloseMainWindow();
                        
                        // Wait a short time for graceful shutdown
                        if (!processToStop.WaitForExit(2000))
                        {
                            processToStop.Kill(entireProcessTree: true);
                        }
                    }
                    else
                    {
                        // For Unix-like systems, send SIGTERM first
                        processToStop.Kill(entireProcessTree: false);
                        
                        // Wait for graceful shutdown
                        if (!processToStop.WaitForExit(2000))
                        {
                            processToStop.Kill(entireProcessTree: true);
                        }
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }
            }

            // Wait for read tasks to complete
            if (_stdoutReadTask != null)
            {
                try
                {
                    await _stdoutReadTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }

            if (_stderrReadTask != null)
            {
                try
                {
                    await _stderrReadTask;
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            }
        }
        finally
        {
            CleanupProcess();
        }
    }

    /// <summary>
    /// Writes data to the shell process stdin.
    /// </summary>
    /// <param name="data">The data to write</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    /// <exception cref="ProcessWriteException">Thrown if writing to the process fails</exception>
    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        Process? currentProcess;
        lock (_processLock)
        {
            currentProcess = _process;
        }

        if (currentProcess == null || currentProcess.HasExited)
        {
            throw new InvalidOperationException("No process is currently running");
        }

        try
        {
            // Write data to stdin
            currentProcess.StandardInput.BaseStream.Write(data);
            currentProcess.StandardInput.BaseStream.Flush();
        }
        catch (Exception ex)
        {
            var processId = currentProcess.Id;
            var writeException = new ProcessWriteException($"Failed to write to process stdin: {ex.Message}", ex, processId);
            OnProcessError(new ProcessErrorEventArgs(writeException, writeException.Message, processId));
            throw writeException;
        }
    }

    /// <summary>
    /// Writes string data to the shell process stdin.
    /// </summary>
    /// <param name="text">The text to write (will be converted to UTF-8)</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    /// <exception cref="ProcessWriteException">Thrown if writing to the process fails</exception>
    public void Write(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var bytes = Encoding.UTF8.GetBytes(text);
        Write(bytes.AsSpan());
    }

    /// <summary>
    /// Resizes the shell process terminal dimensions.
    /// Note: This is a no-op for now as System.Diagnostics.Process doesn't support PTY resizing.
    /// Future implementations may use platform-specific APIs.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        Process? currentProcess;
        lock (_processLock)
        {
            currentProcess = _process;
        }

        if (currentProcess == null || currentProcess.HasExited)
        {
            throw new InvalidOperationException("No process is currently running");
        }

        // Note: System.Diagnostics.Process doesn't support PTY resizing directly.
        // This would require platform-specific implementations using:
        // - Windows: SetConsoleScreenBufferSize, SetConsoleWindowInfo
        // - Unix: ioctl with TIOCSWINSZ
        // For now, this is a no-op, but the interface is defined for future implementation.
        
        // We could potentially send SIGWINCH on Unix systems, but that requires
        // more complex process group management that System.Diagnostics.Process doesn't provide.
    }

    /// <summary>
    /// Resolves the shell command and arguments based on the launch options.
    /// </summary>
    /// <param name="options">The launch options</param>
    /// <returns>A tuple of shell path and arguments</returns>
    /// <exception cref="ProcessStartException">Thrown if the shell cannot be resolved</exception>
    private static (string shellPath, string[] arguments) ResolveShellCommand(ProcessLaunchOptions options)
    {
        return options.ShellType switch
        {
            ShellType.Auto => ResolveAutoShell(options),
            ShellType.PowerShell => ResolvePowerShell(options),
            ShellType.PowerShellCore => ResolvePowerShellCore(options),
            ShellType.Cmd => ResolveCmd(options),
            ShellType.Custom => ResolveCustomShell(options),
            _ => throw new ProcessStartException($"Unsupported shell type: {options.ShellType}")
        };
    }

    /// <summary>
    /// Resolves the best shell for the current platform automatically.
    /// </summary>
    private static (string shellPath, string[] arguments) ResolveAutoShell(ProcessLaunchOptions options)
    {
        if (OperatingSystem.IsWindows())
        {
            // Try PowerShell first, then cmd
            try
            {
                return ResolvePowerShell(options);
            }
            catch
            {
                return ResolveCmd(options);
            }
        }
        else
        {
            // Try user's preferred shell, then common shells
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (!string.IsNullOrEmpty(shell) && File.Exists(shell))
            {
                return (shell, options.Arguments?.ToArray() ?? Array.Empty<string>());
            }

            // Try common shells
            string[] commonShells = ["zsh", "bash", "sh"];
            foreach (var shellName in commonShells)
            {
                var shellPath = FindExecutableInPath(shellName);
                if (shellPath != null)
                {
                    return (shellPath, options.Arguments?.ToArray() ?? Array.Empty<string>());
                }
            }

            throw new ProcessStartException("No suitable shell found on this system");
        }
    }

    /// <summary>
    /// Resolves Windows PowerShell (powershell.exe).
    /// </summary>
    private static (string shellPath, string[] arguments) ResolvePowerShell(ProcessLaunchOptions options)
    {
        var shellPath = FindExecutableInPath("powershell.exe") ?? 
                       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), 
                                   "WindowsPowerShell", "v1.0", "powershell.exe");

        if (!File.Exists(shellPath))
        {
            throw new ProcessStartException("PowerShell not found", shellPath);
        }

        return (shellPath, options.Arguments?.ToArray() ?? Array.Empty<string>());
    }

    /// <summary>
    /// Resolves PowerShell Core (pwsh.exe).
    /// </summary>
    private static (string shellPath, string[] arguments) ResolvePowerShellCore(ProcessLaunchOptions options)
    {
        var shellPath = FindExecutableInPath("pwsh.exe") ?? FindExecutableInPath("pwsh");
        
        if (shellPath == null || !File.Exists(shellPath))
        {
            throw new ProcessStartException("PowerShell Core (pwsh) not found", shellPath);
        }

        return (shellPath, options.Arguments?.ToArray() ?? Array.Empty<string>());
    }

    /// <summary>
    /// Resolves Windows Command Prompt (cmd.exe).
    /// </summary>
    private static (string shellPath, string[] arguments) ResolveCmd(ProcessLaunchOptions options)
    {
        var shellPath = FindExecutableInPath("cmd.exe") ?? 
                       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

        if (!File.Exists(shellPath))
        {
            throw new ProcessStartException("Command Prompt not found", shellPath);
        }

        return (shellPath, options.Arguments?.ToArray() ?? Array.Empty<string>());
    }

    /// <summary>
    /// Resolves a custom shell path.
    /// </summary>
    private static (string shellPath, string[] arguments) ResolveCustomShell(ProcessLaunchOptions options)
    {
        if (string.IsNullOrEmpty(options.CustomShellPath))
        {
            throw new ProcessStartException("Custom shell path is required when using ShellType.Custom");
        }

        if (!File.Exists(options.CustomShellPath))
        {
            throw new ProcessStartException($"Custom shell not found: {options.CustomShellPath}", options.CustomShellPath);
        }

        return (options.CustomShellPath, options.Arguments?.ToArray() ?? Array.Empty<string>());
    }

    /// <summary>
    /// Finds an executable in the system PATH.
    /// </summary>
    /// <param name="executableName">The executable name to find</param>
    /// <returns>The full path to the executable, or null if not found</returns>
    private static string? FindExecutableInPath(string executableName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
            return null;

        var paths = pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var path in paths)
        {
            try
            {
                var fullPath = Path.Combine(path, executableName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }

                // On Windows, also try with .exe extension if not already present
                if (OperatingSystem.IsWindows() && !executableName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    var exePath = fullPath + ".exe";
                    if (File.Exists(exePath))
                    {
                        return exePath;
                    }
                }
            }
            catch
            {
                // Ignore errors accessing individual paths
                continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads data from a stream asynchronously and raises DataReceived events.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    /// <param name="isError">Whether this is stderr (true) or stdout (false)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ReadStreamAsync(Stream stream, bool isError, CancellationToken cancellationToken)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, bufferSize, cancellationToken);
                
                if (bytesRead == 0)
                {
                    // End of stream
                    break;
                }

                // Create a copy of the data to avoid buffer reuse issues
                var data = new byte[bytesRead];
                Array.Copy(buffer, 0, data, 0, bytesRead);

                // Raise the DataReceived event
                OnDataReceived(new DataReceivedEventArgs(data, isError));
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            OnProcessError(new ProcessErrorEventArgs(ex, $"Error reading from {(isError ? "stderr" : "stdout")}: {ex.Message}", ProcessId));
        }
    }

    /// <summary>
    /// Handles process exit events.
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (sender is Process process)
        {
            var exitCode = process.ExitCode;
            var processId = process.Id;

            // Clean up resources
            CleanupProcess();

            // Raise the ProcessExited event
            ProcessExited?.Invoke(this, new ProcessExitedEventArgs(exitCode, processId));
        }
    }

    /// <summary>
    /// Raises the DataReceived event.
    /// </summary>
    private void OnDataReceived(DataReceivedEventArgs args)
    {
        DataReceived?.Invoke(this, args);
    }

    /// <summary>
    /// Raises the ProcessError event.
    /// </summary>
    private void OnProcessError(ProcessErrorEventArgs args)
    {
        ProcessError?.Invoke(this, args);
    }

    /// <summary>
    /// Cleans up process resources.
    /// </summary>
    private void CleanupProcess()
    {
        lock (_processLock)
        {
            _readCancellationSource?.Cancel();
            _readCancellationSource?.Dispose();
            _readCancellationSource = null;

            if (_process != null)
            {
                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
            }

            _stdoutReadTask = null;
            _stderrReadTask = null;
        }
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ProcessManager));
    }

    /// <summary>
    /// Disposes the process manager and cleans up all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            try
            {
                StopAsync().Wait(5000); // Wait up to 5 seconds for graceful shutdown
            }
            catch
            {
                // Ignore errors during disposal
            }

            CleanupProcess();
            _disposed = true;
        }
    }
}