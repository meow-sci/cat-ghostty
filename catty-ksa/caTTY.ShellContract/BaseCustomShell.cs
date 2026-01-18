using System.Text;
using System.Threading.Channels;

namespace caTTY.ShellContract;

/// <summary>
///     Base implementation of ICustomShell providing PTY plumbing infrastructure.
///     Handles output pumping, state management, and lifecycle coordination.
///     Subclasses only need to implement OnInputByte() to handle input processing.
/// </summary>
public abstract class BaseCustomShell : ICustomShell
{
    private readonly Channel<byte[]> _outputChannel;
    private readonly CancellationTokenSource _shellCts;
    private readonly object _stateLock = new();
    private Task? _outputPumpTask;
    private bool _disposed;
    private bool _isRunning;

    /// <summary>
    ///     Creates a new BaseCustomShell with default channel capacity.
    /// </summary>
    protected BaseCustomShell()
        : this(capacity: 1000)
    {
    }

    /// <summary>
    ///     Creates a new BaseCustomShell with specified channel capacity.
    /// </summary>
    /// <param name="capacity">Maximum number of queued output chunks</param>
    protected BaseCustomShell(int capacity)
    {
        _outputChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
        _shellCts = new CancellationTokenSource();
    }

    /// <inheritdoc />
    public abstract CustomShellMetadata Metadata { get; }

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (_stateLock)
            {
                return _isRunning;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                _isRunning = value;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ShellOutputEventArgs>? OutputReceived;

    /// <inheritdoc />
    public event EventHandler<ShellTerminatedEventArgs>? Terminated;

    /// <inheritdoc />
    public async Task StartAsync(CustomShellStartOptions options, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException($"Shell '{Metadata.Name}' is already running");
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BaseCustomShell));
        }

        try
        {
            // Start output pump before setting IsRunning so it's ready to handle output
            _outputPumpTask = Task.Run(() => OutputPumpAsync(_shellCts.Token), _shellCts.Token);

            // Call subclass lifecycle hook
            await OnShellStartingAsync(options, cancellationToken).ConfigureAwait(false);

            IsRunning = true;
        }
        catch (Exception ex)
        {
            // Cleanup on failure
            _shellCts.Cancel();
            if (_outputPumpTask != null)
            {
                try { await _outputPumpTask.ConfigureAwait(false); }
                catch { /* Ignore pump cleanup errors */ }
            }
            throw new CustomShellStartException(
                $"Failed to start shell '{Metadata.Name}': {ex.Message}",
                ex,
                Metadata.Name);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        try
        {
            // Call subclass lifecycle hook
            await OnShellStoppingAsync(cancellationToken).ConfigureAwait(false);

            // Signal shutdown
            _shellCts.Cancel();

            // Complete output channel and wait for pump to drain
            _outputChannel.Writer.Complete();
            if (_outputPumpTask != null)
            {
                try
                {
                    await _outputPumpTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected during shutdown
                }
            }

            IsRunning = false;

            // Fire terminated event
            Terminated?.Invoke(this, new ShellTerminatedEventArgs(0, "Shell stopped normally"));
        }
        catch (Exception ex)
        {
            IsRunning = false;
            Terminated?.Invoke(this, new ShellTerminatedEventArgs(1, $"Shell stopped with error: {ex.Message}"));
            throw;
        }
    }

    /// <inheritdoc />
    public Task WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException($"Shell '{Metadata.Name}' is not running");
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(BaseCustomShell));
        }

        // Process each byte through subclass handler
        var span = data.Span;
        for (int i = 0; i < span.Length; i++)
        {
            OnInputByte(span[i]);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual void NotifyTerminalResize(int width, int height)
    {
        // Default: no-op, subclasses can override
    }

    /// <inheritdoc />
    public virtual void RequestCancellation()
    {
        // Default: no-op, subclasses can override
    }

    /// <inheritdoc />
    public void SendInitialOutput()
    {
        if (!IsRunning)
        {
            return;
        }

        var initialOutput = GetInitialOutput();
        if (!string.IsNullOrEmpty(initialOutput))
        {
            QueueOutput(initialOutput);
        }
    }

    /// <summary>
    ///     Queues raw bytes to be sent to the terminal output stream.
    ///     Thread-safe and can be called from any thread.
    /// </summary>
    /// <param name="data">The bytes to queue</param>
    protected void QueueOutput(byte[] data)
    {
        if (_disposed || !IsRunning)
        {
            return;
        }

        // Non-blocking write - will wait if channel is full
        _outputChannel.Writer.TryWrite(data);
    }

    /// <summary>
    ///     Queues text to be sent to the terminal output stream (converted to UTF-8).
    ///     Thread-safe and can be called from any thread.
    /// </summary>
    /// <param name="text">The text to queue</param>
    protected void QueueOutput(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        QueueOutput(Encoding.UTF8.GetBytes(text));
    }

    /// <summary>
    ///     Handles a single input byte. Subclasses must implement this to process input.
    ///     This method is called synchronously for each input byte.
    /// </summary>
    /// <param name="b">The input byte to process</param>
    protected abstract void OnInputByte(byte b);

    /// <summary>
    ///     Called when the shell is starting. Subclasses can override to perform initialization.
    /// </summary>
    /// <param name="options">The start options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when initialization is done</returns>
    protected virtual Task OnShellStartingAsync(CustomShellStartOptions options, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Called when the shell is stopping. Subclasses can override to perform cleanup.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A task that completes when cleanup is done</returns>
    protected virtual Task OnShellStoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    ///     Returns initial output to send when the shell starts (banner, prompt, etc.).
    ///     Called by SendInitialOutput() after the shell is fully initialized.
    /// </summary>
    /// <returns>Initial output string, or null if no initial output</returns>
    protected virtual string? GetInitialOutput()
    {
        return null;
    }

    /// <summary>
    ///     Background task that pumps output from the channel to OutputReceived events.
    /// </summary>
    private async Task OutputPumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var data in _outputChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                if (data.Length > 0)
                {
                    OutputReceived?.Invoke(this, new ShellOutputEventArgs(data, ShellOutputType.Stdout));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown
        }
        catch (Exception)
        {
            // Suppress pump errors to avoid unhandled exceptions
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Disposes the shell and its resources.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // Stop the shell if still running
            if (IsRunning)
            {
                try
                {
                    StopAsync().GetAwaiter().GetResult();
                }
                catch
                {
                    // Suppress errors during disposal
                }
            }

            _shellCts.Dispose();
        }

        _disposed = true;
    }
}
