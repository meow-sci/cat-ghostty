using System.Runtime.InteropServices;
using System.Text;
using AttributeListBuilder = caTTY.Core.Terminal.Process.AttributeListBuilder;
using ConPtyNative = caTTY.Core.Terminal.Process.ConPtyNative;
using ShellCommandResolver = caTTY.Core.Terminal.Process.ShellCommandResolver;
using SysProcess = System.Diagnostics.Process;

namespace caTTY.Core.Terminal;

/// <summary>
///     Manages shell processes using Windows Pseudoconsole (ConPTY) for true PTY functionality.
///     Follows Microsoft's recommended approach from:
///     https://learn.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session
/// </summary>
public class ProcessManager : IProcessManager
{
    private readonly object _processLock = new();
    private Process.ConPtyNative.COORD _currentSize;
    private bool _disposed;
    private IntPtr _inputReadHandle = IntPtr.Zero;
    private IntPtr _inputWriteHandle = IntPtr.Zero;
    private IntPtr _outputReadHandle = IntPtr.Zero;
    private Task? _outputReadTask;
    private IntPtr _outputWriteHandle = IntPtr.Zero;
    private SysProcess? _process;

    private IntPtr _pseudoConsole = IntPtr.Zero;
    private CancellationTokenSource? _readCancellationSource;

    /// <summary>
    ///     Gets whether a shell process is currently running.
    /// </summary>
    public bool IsRunning
    {
        get
        {
            lock (_processLock)
            {
                if (_process == null)
                {
                    return false;
                }

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
    ///     Gets the process ID of the running shell, or null if no process is running.
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
    ///     Gets the exit code of the last process, or null if no process has exited.
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
    ///     Event raised when data is received from the shell process stdout/stderr.
    /// </summary>
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    /// <summary>
    ///     Event raised when the shell process exits.
    /// </summary>
    public event EventHandler<ProcessExitedEventArgs>? ProcessExited;

    /// <summary>
    ///     Event raised when an error occurs during process operations.
    /// </summary>
    public event EventHandler<ProcessErrorEventArgs>? ProcessError;

    /// <summary>
    ///     Starts a new shell process with the specified options using Windows ConPTY.
    /// </summary>
    /// <param name="options">Launch options for the shell process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the process has started</returns>
    /// <exception cref="InvalidOperationException">Thrown if a process is already running</exception>
    /// <exception cref="ProcessStartException">Thrown if the process fails to start</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown on non-Windows platforms</exception>
    public async Task StartAsync(ProcessLaunchOptions options, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("ConPTY is only supported on Windows 10 version 1809 and later");
        }

        lock (_processLock)
        {
            if (_process != null)
            {
                throw new InvalidOperationException(
                    "A process is already running. Stop the current process before starting a new one.");
            }
        }

        try
        {
            // Store terminal size
            _currentSize = new ConPtyNative.COORD((short)options.InitialWidth, (short)options.InitialHeight);

            // Create communication pipes
            if (!ConPtyNative.CreatePipe(out _inputReadHandle, out _inputWriteHandle, IntPtr.Zero, 0))
            {
                throw new ProcessStartException($"Failed to create input pipe: {Marshal.GetLastWin32Error()}");
            }

            if (!ConPtyNative.CreatePipe(out _outputReadHandle, out _outputWriteHandle, IntPtr.Zero, 0))
            {
                CleanupHandles();
                throw new ProcessStartException($"Failed to create output pipe: {Marshal.GetLastWin32Error()}");
            }

            // Create pseudoconsole
            int result = ConPtyNative.CreatePseudoConsole(_currentSize, _inputReadHandle, _outputWriteHandle, 0, out _pseudoConsole);
            if (result != 0)
            {
                CleanupHandles();
                throw new ProcessStartException($"Failed to create pseudoconsole: {result}");
            }

            // Close the handles that were passed to the pseudoconsole (as per Microsoft docs)
            ConPtyNative.CloseHandle(_inputReadHandle);
            ConPtyNative.CloseHandle(_outputWriteHandle);
            _inputReadHandle = IntPtr.Zero;
            _outputWriteHandle = IntPtr.Zero;

            // Prepare startup information
            var startupInfo = new ConPtyNative.STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf<ConPtyNative.STARTUPINFOEX>();

            // Initialize process thread attribute list with pseudoconsole
            try
            {
                startupInfo.lpAttributeList = AttributeListBuilder.CreateAttributeListWithPseudoConsole(_pseudoConsole);
            }
            catch (ProcessStartException)
            {
                CleanupPseudoConsole();
                throw;
            }

            // Resolve shell command
            (string shellPath, string shellArgs) = ShellCommandResolver.ResolveShellCommand(options);
            string commandLine = string.IsNullOrEmpty(shellArgs) ? shellPath : $"{shellPath} {shellArgs}";

            // Create the process
            var processInfo = new ConPtyNative.PROCESS_INFORMATION();
            if (!ConPtyNative.CreateProcessW(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    ConPtyNative.EXTENDED_STARTUPINFO_PRESENT,
                    IntPtr.Zero,
                    options.WorkingDirectory ?? Environment.CurrentDirectory,
                    ref startupInfo,
                    out processInfo))
            {
                int error = Marshal.GetLastWin32Error();
                AttributeListBuilder.FreeAttributeList(startupInfo.lpAttributeList);
                CleanupPseudoConsole();
                throw new ProcessStartException($"Failed to create process: {error}");
            }

            // Clean up startup info
            AttributeListBuilder.FreeAttributeList(startupInfo.lpAttributeList);

            // Wrap the process handle in a Process object for lifecycle management
            var process = SysProcess.GetProcessById(processInfo.dwProcessId);
            process.EnableRaisingEvents = true;
            process.Exited += OnProcessExited;

            // Close process and thread handles (we have the Process object now)
            ConPtyNative.CloseHandle(processInfo.hProcess);
            ConPtyNative.CloseHandle(processInfo.hThread);

            lock (_processLock)
            {
                _process = process;
                _readCancellationSource = new CancellationTokenSource();
            }

            // Start reading output
            CancellationToken readToken = _readCancellationSource.Token;
            _outputReadTask = ReadOutputAsync(readToken);

            // Wait a short time to ensure the process started successfully
            await Task.Delay(100, cancellationToken);

            // Check if process exited immediately
            try
            {
                if (process.HasExited)
                {
                    int exitCode = process.ExitCode;
                    CleanupProcess();
                    throw new ProcessStartException(
                        $"Shell process exited immediately with code {exitCode}: {shellPath}", shellPath);
                }
            }
            catch (InvalidOperationException)
            {
                // Process has already exited and been disposed - let the exit handler deal with cleanup
            }
        }
        catch (Exception ex) when (!(ex is ProcessStartException))
        {
            CleanupProcess();
            throw new ProcessStartException($"Failed to start shell process: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Stops the currently running shell process and cleans up ConPTY resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the process has stopped</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        SysProcess? processToStop = null;
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
                    // For ConPTY processes, we can try CloseMainWindow first, then Kill if needed
                    processToStop.CloseMainWindow();

                    // Wait a short time for graceful shutdown
                    if (!processToStop.WaitForExit(2000))
                    {
                        processToStop.Kill(true);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }
            }

            // Wait for read task to complete
            if (_outputReadTask != null)
            {
                try
                {
                    await _outputReadTask;
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
    ///     Writes data to the shell process stdin via ConPTY.
    /// </summary>
    /// <param name="data">The data to write</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    /// <exception cref="ProcessWriteException">Thrown if writing to the process fails</exception>
    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        SysProcess? currentProcess;
        lock (_processLock)
        {
            currentProcess = _process;
        }

        if (currentProcess == null || currentProcess.HasExited)
        {
            throw new InvalidOperationException("No process is currently running");
        }

        if (_inputWriteHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Input handle is not available");
        }

        try
        {
            byte[] buffer = data.ToArray();
            if (!ConPtyNative.WriteFile(_inputWriteHandle, buffer, (uint)buffer.Length, out uint bytesWritten, IntPtr.Zero))
            {
                int error = Marshal.GetLastWin32Error();
                int processId = currentProcess.Id;
                var writeException =
                    new ProcessWriteException($"Failed to write to ConPTY input: Win32 error {error}", processId);
                OnProcessError(new ProcessErrorEventArgs(writeException, writeException.Message, processId));
                throw writeException;
            }
        }
        catch (Exception ex) when (!(ex is ProcessWriteException))
        {
            int processId = currentProcess.Id;
            var writeException =
                new ProcessWriteException($"Failed to write to ConPTY input: {ex.Message}", ex, processId);
            OnProcessError(new ProcessErrorEventArgs(writeException, writeException.Message, processId));
            throw writeException;
        }
    }

    /// <summary>
    ///     Writes string data to the shell process stdin.
    /// </summary>
    /// <param name="text">The text to write (will be converted to UTF-8)</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    /// <exception cref="ProcessWriteException">Thrown if writing to the process fails</exception>
    public void Write(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        Write(bytes.AsSpan());
    }

    /// <summary>
    ///     Resizes the pseudoconsole terminal dimensions.
    ///     Uses Windows ConPTY ResizePseudoConsole API for proper terminal resizing.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    /// <exception cref="InvalidOperationException">Thrown if no process is running</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown on non-Windows platforms</exception>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("ConPTY resizing is only supported on Windows");
        }

        SysProcess? currentProcess;
        lock (_processLock)
        {
            currentProcess = _process;
        }

        if (currentProcess == null || currentProcess.HasExited)
        {
            throw new InvalidOperationException("No process is currently running");
        }

        if (_pseudoConsole == IntPtr.Zero)
        {
            throw new InvalidOperationException("Pseudoconsole is not available");
        }

        var newSize = new ConPtyNative.COORD((short)width, (short)height);
        int result = ConPtyNative.ResizePseudoConsole(_pseudoConsole, newSize);

        if (result != 0)
        {
            throw new InvalidOperationException($"Failed to resize pseudoconsole: Win32 error {result}");
        }

        _currentSize = newSize;
    }

    /// <summary>
    ///     Disposes the process manager and cleans up all resources.
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

    /// <summary>
    ///     Reads data from the ConPTY output pipe asynchronously and raises DataReceived events.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task ReadOutputAsync(CancellationToken cancellationToken)
    {
        const int bufferSize = 4096;
        byte[] buffer = new byte[bufferSize];

        try
        {
            while (!cancellationToken.IsCancellationRequested && _outputReadHandle != IntPtr.Zero)
            {
                if (ConPtyNative.ReadFile(_outputReadHandle, buffer, bufferSize, out uint bytesRead, IntPtr.Zero))
                {
                    if (bytesRead == 0)
                    {
                        // End of stream
                        break;
                    }

                    // Create a copy of the data to avoid buffer reuse issues
                    byte[] data = new byte[bytesRead];
                    Array.Copy(buffer, 0, data, 0, (int)bytesRead);

                    // Raise the DataReceived event (ConPTY output is never "error" stream)
                    OnDataReceived(new DataReceivedEventArgs(data));
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == 109) // ERROR_BROKEN_PIPE
                    {
                        // Process has exited
                        break;
                    }

                    OnProcessError(new ProcessErrorEventArgs(
                        new InvalidOperationException($"ReadFile failed with error {error}"),
                        $"Error reading from ConPTY output: Win32 error {error}",
                        ProcessId));
                    break;
                }

                // Small delay to prevent tight loop
                await Task.Delay(1, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            OnProcessError(new ProcessErrorEventArgs(ex, $"Error reading from ConPTY output: {ex.Message}", ProcessId));
        }
    }

    /// <summary>
    ///     Handles process exit events.
    /// </summary>
    private void OnProcessExited(object? sender, EventArgs e)
    {
        if (sender is SysProcess process)
        {
            int exitCode = process.ExitCode;
            int processId = process.Id;

            // Clean up resources
            CleanupProcess();

            // Raise the ProcessExited event
            ProcessExited?.Invoke(this, new ProcessExitedEventArgs(exitCode, processId));
        }
    }

    /// <summary>
    ///     Raises the DataReceived event.
    /// </summary>
    private void OnDataReceived(DataReceivedEventArgs args)
    {
        DataReceived?.Invoke(this, args);
    }

    /// <summary>
    ///     Raises the ProcessError event.
    /// </summary>
    private void OnProcessError(ProcessErrorEventArgs args)
    {
        ProcessError?.Invoke(this, args);
    }

    /// <summary>
    ///     Cleans up process resources including ConPTY handles.
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

            _outputReadTask = null;

            CleanupPseudoConsole();
        }
    }

    /// <summary>
    ///     Cleans up ConPTY-specific resources.
    /// </summary>
    private void CleanupPseudoConsole()
    {
        if (_pseudoConsole != IntPtr.Zero)
        {
            ConPtyNative.ClosePseudoConsole(_pseudoConsole);
            _pseudoConsole = IntPtr.Zero;
        }

        CleanupHandles();
    }

    /// <summary>
    ///     Cleans up pipe handles.
    /// </summary>
    private void CleanupHandles()
    {
        if (_inputWriteHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_inputWriteHandle);
            _inputWriteHandle = IntPtr.Zero;
        }

        if (_outputReadHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_outputReadHandle);
            _outputReadHandle = IntPtr.Zero;
        }

        if (_inputReadHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_inputReadHandle);
            _inputReadHandle = IntPtr.Zero;
        }

        if (_outputWriteHandle != IntPtr.Zero)
        {
            ConPtyNative.CloseHandle(_outputWriteHandle);
            _outputWriteHandle = IntPtr.Zero;
        }
    }

    /// <summary>
    ///     Throws an ObjectDisposedException if the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ProcessManager));
        }
    }
}
