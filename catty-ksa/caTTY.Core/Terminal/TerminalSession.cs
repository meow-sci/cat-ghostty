

namespace caTTY.Core.Terminal;

/// <summary>
///     Represents a single terminal session with its own terminal emulator, process, and settings.
///     Encapsulates all state and resources for one discrete terminal instance.
/// </summary>
public class TerminalSession : IDisposable
{
    private bool _disposed = false;

    /// <summary>
    ///     Creates a new terminal session with the specified components.
    /// </summary>
    /// <param name="id">Unique identifier for the session</param>
    /// <param name="title">Display title for the session</param>
    /// <param name="terminal">Terminal emulator instance</param>
    /// <param name="processManager">Process manager instance</param>
    /// <exception cref="ArgumentNullException">Thrown if any required parameter is null</exception>
    public TerminalSession(Guid id, string title, ITerminalEmulator terminal, IProcessManager processManager)
    {
        Id = id;
        Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        ProcessManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
        
        // Initialize Settings first, then set title
        Settings = new SessionSettings { Title = title ?? throw new ArgumentNullException(nameof(title)) };
        State = SessionState.Creating;
        CreatedAt = DateTime.UtcNow;
        
        // Wire up events from terminal and process manager
        ProcessManager.ProcessExited += OnProcessExited;
        Terminal.TitleChanged += OnTerminalTitleChanged;
    }

    /// <summary>
    ///     Gets the unique identifier for this session.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    ///     Gets or sets the display title for this session.
    /// </summary>
    public string Title 
    { 
        get => Settings.Title;
        set
        {
            if (Settings.Title != value)
            {
                string oldTitle = Settings.Title;
                Settings.Title = value ?? string.Empty;
                TitleChanged?.Invoke(this, new SessionTitleChangedEventArgs(oldTitle, Settings.Title));
            }
        }
    }

    /// <summary>
    ///     Gets the terminal emulator for this session.
    /// </summary>
    public ITerminalEmulator Terminal { get; }

    /// <summary>
    ///     Gets the process manager for this session.
    /// </summary>
    public IProcessManager ProcessManager { get; }

    /// <summary>
    ///     Gets the settings for this session.
    /// </summary>
    public SessionSettings Settings { get; }

    /// <summary>
    ///     Gets the current state of this session.
    /// </summary>
    public SessionState State { get; private set; }

    /// <summary>
    ///     Gets the time when this session was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    ///     Gets or sets the time when this session was last active.
    /// </summary>
    public DateTime? LastActiveAt { get; set; }

    /// <summary>
    ///     Event raised when the session state changes.
    /// </summary>
    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    /// <summary>
    ///     Event raised when the session title changes.
    /// </summary>
    public event EventHandler<SessionTitleChangedEventArgs>? TitleChanged;

    /// <summary>
    ///     Event raised when the session process exits.
    /// </summary>
    public event EventHandler<SessionProcessExitedEventArgs>? ProcessExited;

    /// <summary>
    ///     Initializes the session by starting the shell process.
    /// </summary>
    /// <param name="launchOptions">Options for launching the shell process</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when initialization is finished</returns>
    /// <exception cref="InvalidOperationException">Thrown if session is not in Creating state</exception>
    /// <exception cref="ObjectDisposedException">Thrown if session has been disposed</exception>
    public async Task InitializeAsync(ProcessLaunchOptions launchOptions, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (State != SessionState.Creating)
        {
            throw new InvalidOperationException($"Cannot initialize session in state {State}");
        }

        try
        {
            await ProcessManager.StartAsync(launchOptions, cancellationToken);
            ChangeState(SessionState.Active);
            LastActiveAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            ChangeState(SessionState.Failed, ex);
            throw;
        }
    }

    /// <summary>
    ///     Activates this session, making it the current active session.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if session has been disposed</exception>
    public void Activate()
    {
        ThrowIfDisposed();
        
        if (State == SessionState.Inactive)
        {
            ChangeState(SessionState.Active);
            LastActiveAt = DateTime.UtcNow;
            Settings.IsActive = true;
        }
    }

    /// <summary>
    ///     Deactivates this session, making it inactive.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if session has been disposed</exception>
    public void Deactivate()
    {
        ThrowIfDisposed();
        
        if (State == SessionState.Active)
        {
            ChangeState(SessionState.Inactive);
            Settings.IsActive = false;
        }
    }

    /// <summary>
    ///     Closes the session and cleans up its resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the session is closed</returns>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (State != SessionState.Disposed)
        {
            ChangeState(SessionState.Terminating);
            
            try
            {
                if (ProcessManager.IsRunning)
                {
                    await ProcessManager.StopAsync(cancellationToken);
                }
            }
            catch
            {
                // Log error but continue with disposal
                // UI components can handle error reporting through events
            }
            finally
            {
                Dispose();
            }
        }
    }

    /// <summary>
    ///     Changes the session state and raises the StateChanged event.
    /// </summary>
    /// <param name="newState">The new state</param>
    /// <param name="error">Optional error that caused the state change</param>
    private void ChangeState(SessionState newState, Exception? error = null)
    {
        if (State != newState)
        {
            State = newState;
            StateChanged?.Invoke(this, new SessionStateChangedEventArgs(newState, error));
        }
    }

    /// <summary>
    ///     Handles process exit events from the ProcessManager.
    /// </summary>
    private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
    {
        ProcessExited?.Invoke(this, new SessionProcessExitedEventArgs(e.ExitCode, e.ProcessId));
    }

    /// <summary>
    ///     Handles title change events from the Terminal.
    /// </summary>
    private void OnTerminalTitleChanged(object? sender, TitleChangeEventArgs e)
    {
        // Update session title when terminal emits title change
        Title = e.NewTitle;
    }

    /// <summary>
    ///     Throws ObjectDisposedException if the session has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TerminalSession));
        }
    }

    /// <summary>
    ///     Disposes the session and its resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            ChangeState(SessionState.Disposed);
            
            // Unsubscribe from events
            if (ProcessManager != null)
            {
                ProcessManager.ProcessExited -= OnProcessExited;
            }
            
            if (Terminal != null)
            {
                Terminal.TitleChanged -= OnTerminalTitleChanged;
            }
            
            // Dispose resources
            ProcessManager?.Dispose();
            Terminal?.Dispose();
            
            _disposed = true;
        }
    }
}