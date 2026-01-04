using System.Collections.Concurrent;
using caTTY.Core.Rpc;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Terminal;

/// <summary>
///     Manages multiple terminal sessions and coordinates their lifecycle.
///     Provides session creation, switching, and cleanup functionality.
/// </summary>
public class SessionManager : IDisposable
{
    private readonly Dictionary<Guid, TerminalSession> _sessions = new();
    private readonly List<Guid> _sessionOrder = new(); // For tab ordering
    private Guid? _activeSessionId;
    private readonly object _lock = new();
    private bool _disposed = false;

    // Configuration
    private readonly int _maxSessions;
    private readonly SessionDimensionTracker _dimensionTracker;

    /// <summary>
    ///     Creates a new session manager with the specified configuration.
    /// </summary>
    /// <param name="maxSessions">Maximum number of concurrent sessions (default: 20)</param>
    /// <param name="defaultLaunchOptions">Default options for launching new sessions</param>
    public SessionManager(int maxSessions = 20, ProcessLaunchOptions? defaultLaunchOptions = null)
    {
        if (maxSessions <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSessions), "Maximum sessions must be greater than zero");
        }

        _maxSessions = maxSessions;
        _dimensionTracker = new SessionDimensionTracker(defaultLaunchOptions ?? ProcessLaunchOptions.CreateDefault());
    }

    /// <summary>
    ///     Updates the default launch options for new sessions.
    /// </summary>
    /// <param name="launchOptions">New default launch options</param>
    public void UpdateDefaultLaunchOptions(ProcessLaunchOptions launchOptions)
    {
        _dimensionTracker.UpdateDefaultLaunchOptions(launchOptions);
    }

    /// <summary>
    ///     Gets the most recently known terminal dimensions (cols, rows).
    ///     Used to seed new sessions so they start at the current UI size instead of a fixed default.
    /// </summary>
    public (int cols, int rows) LastKnownTerminalDimensions
    {
        get
        {
            return _dimensionTracker.LastKnownTerminalDimensions;
        }
    }

    /// <summary>
    ///     Updates the manager's notion of the current terminal dimensions.
    ///     This also updates the default launch options so newly created processes start at the latest size.
    /// </summary>
    /// <param name="cols">Terminal width in columns</param>
    /// <param name="rows">Terminal height in rows</param>
    public void UpdateLastKnownTerminalDimensions(int cols, int rows)
    {
        _dimensionTracker.UpdateLastKnownTerminalDimensions(cols, rows);
    }

    /// <summary>
    ///     Gets the current default launch options.
    /// </summary>
    public ProcessLaunchOptions DefaultLaunchOptions
    {
        get
        {
            return _dimensionTracker.DefaultLaunchOptions;
        }
    }

    /// <summary>
    ///     Creates a deep clone of the specified launch options.
    /// </summary>
    private static ProcessLaunchOptions CloneLaunchOptions(ProcessLaunchOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return new ProcessLaunchOptions
        {
            ShellType = options.ShellType,
            CustomShellPath = options.CustomShellPath,
            Arguments = new List<string>(options.Arguments),
            WorkingDirectory = options.WorkingDirectory,
            EnvironmentVariables = new Dictionary<string, string>(options.EnvironmentVariables),
            InitialWidth = options.InitialWidth,
            InitialHeight = options.InitialHeight,
            CreateWindow = options.CreateWindow,
            UseShellExecute = options.UseShellExecute
        };
    }

    /// <summary>
    ///     Event raised when a new session is created.
    /// </summary>
    public event EventHandler<SessionCreatedEventArgs>? SessionCreated;

    /// <summary>
    ///     Event raised when a session is closed.
    /// </summary>
    public event EventHandler<SessionClosedEventArgs>? SessionClosed;

    /// <summary>
    ///     Event raised when the active session changes.
    /// </summary>
    public event EventHandler<ActiveSessionChangedEventArgs>? ActiveSessionChanged;

    /// <summary>
    ///     Gets the currently active session, or null if no sessions exist.
    /// </summary>
    public TerminalSession? ActiveSession
    {
        get
        {
            lock (_lock)
            {
                return _activeSessionId.HasValue && _sessions.TryGetValue(_activeSessionId.Value, out var session)
                    ? session
                    : null;
            }
        }
    }

    /// <summary>
    ///     Gets all sessions in tab order.
    /// </summary>
    public IReadOnlyList<TerminalSession> Sessions
    {
        get
        {
            lock (_lock)
            {
                return _sessionOrder
                    .Where(id => _sessions.ContainsKey(id))
                    .Select(id => _sessions[id])
                    .ToList();
            }
        }
    }

    /// <summary>
    ///     Gets the number of active sessions.
    /// </summary>
    public int SessionCount
    {
        get
        {
            lock (_lock)
            {
                return _sessions.Count;
            }
        }
    }

    /// <summary>
    ///     Creates a new terminal session and makes it active.
    /// </summary>
    /// <param name="title">Optional title for the session (auto-generated if null)</param>
    /// <param name="launchOptions">Optional launch options (uses default if null)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The newly created session</returns>
    /// <exception cref="InvalidOperationException">Thrown if maximum sessions reached</exception>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public async Task<TerminalSession> CreateSessionAsync(string? title = null, ProcessLaunchOptions? launchOptions = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            if (_sessions.Count >= _maxSessions)
            {
                throw new InvalidOperationException($"Maximum number of sessions ({_maxSessions}) reached");
            }
        }

        var sessionId = Guid.NewGuid();
        var sessionTitle = title ?? GenerateSessionTitle();

        // Ensure the terminal emulator and PTY process start with the same dimensions.
        // If launchOptions is null, use the last-known/default size (updated via resize handlers).
        ProcessLaunchOptions effectiveLaunchOptions = launchOptions != null
            ? CloneLaunchOptions(launchOptions)
            : _dimensionTracker.GetDefaultLaunchOptionsSnapshot();

        // Always start new sessions at the last-known UI size.
        // This prevents shell changes (WSL/PowerShell/Cmd) from reverting to 80x24/80x25 defaults.
        var lastKnown = LastKnownTerminalDimensions;
        effectiveLaunchOptions.InitialWidth = lastKnown.cols;
        effectiveLaunchOptions.InitialHeight = lastKnown.rows;

        TerminalSession session;
        TerminalSession? previousActiveSession = null;

        try
        {
            // Create and initialize session
            session = await SessionCreator.CreateSessionAsync(
                sessionId,
                sessionTitle,
                effectiveLaunchOptions,
                OnSessionStateChanged,
                OnSessionTitleChanged,
                OnSessionProcessExited,
                cancellationToken);

            // Add session to manager and switch active session
            lock (_lock)
            {
                _sessions[sessionId] = session;
                _sessionOrder.Add(sessionId);

                // Deactivate current session
                if (_activeSessionId.HasValue && _sessions.TryGetValue(_activeSessionId.Value, out var currentSession))
                {
                    previousActiveSession = currentSession;
                    currentSession.Deactivate();
                }

                _activeSessionId = sessionId;
            }

            LogSessionLifecycleEvent($"Successfully created session {sessionId} with title '{sessionTitle}'");

            SessionCreated?.Invoke(this, new SessionCreatedEventArgs(session));
            ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousActiveSession, session));

            return session;
        }
        catch (Exception ex)
        {
            LogSessionLifecycleEvent($"Failed to create session {sessionId} with title '{sessionTitle}'", ex);

            // Comprehensive cleanup on failure
            lock (_lock)
            {
                _sessions.Remove(sessionId);
                _sessionOrder.Remove(sessionId);
                if (_activeSessionId == sessionId)
                {
                    _activeSessionId = null;
                    // Restore previous active session if it exists
                    if (previousActiveSession != null)
                    {
                        _activeSessionId = previousActiveSession.Id;
                        previousActiveSession.Activate();
                        LogSessionLifecycleEvent($"Restored previous active session {previousActiveSession.Id} after creation failure");
                    }
                }
            }

            // Re-throw with more context for the caller
            throw new InvalidOperationException($"Failed to create session '{sessionTitle}': {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Switches to the specified session, making it active.
    /// </summary>
    /// <param name="sessionId">ID of the session to activate</param>
    /// <exception cref="ArgumentException">Thrown if session ID is not found</exception>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public void SwitchToSession(Guid sessionId)
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            SessionSwitcher.SwitchToSession(sessionId, _sessions, ref _activeSessionId, ActiveSessionChanged);
        }
    }

    /// <summary>
    ///     Closes the specified session and cleans up its resources.
    /// </summary>
    /// <param name="sessionId">ID of the session to close</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the session is closed</returns>
    /// <exception cref="InvalidOperationException">Thrown if trying to close the last session</exception>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public async Task CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        SessionCloser.CloseSessionState? state;

        lock (_lock)
        {
            state = SessionCloser.PrepareClose(sessionId, _sessions, _sessionOrder, ref _activeSessionId);
        }

        if (state != null)
        {
            await SessionCloser.PerformCleanupAsync(
                sessionId,
                state,
                OnSessionStateChanged,
                OnSessionTitleChanged,
                OnSessionProcessExited,
                SessionClosed,
                ActiveSessionChanged,
                LogSessionLifecycleEvent,
                cancellationToken);
        }
    }

    /// <summary>
    ///     Switches to the next session in tab order.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public void SwitchToNextSession()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            SessionSwitcher.SwitchToNextSession(_sessions, _sessionOrder, ref _activeSessionId, ActiveSessionChanged);
        }
    }

    /// <summary>
    ///     Switches to the previous session in tab order.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public void SwitchToPreviousSession()
    {
        ThrowIfDisposed();

        lock (_lock)
        {
            SessionSwitcher.SwitchToPreviousSession(_sessions, _sessionOrder, ref _activeSessionId, ActiveSessionChanged);
        }
    }

    /// <summary>
    ///     Restarts a terminated session by starting a new shell process.
    /// </summary>
    /// <param name="sessionId">ID of the session to restart</param>
    /// <param name="launchOptions">Optional launch options (uses default if null)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the session is restarted</returns>
    /// <exception cref="ArgumentException">Thrown if session ID is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown if session is not in a restartable state</exception>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public async Task RestartSessionAsync(Guid sessionId, ProcessLaunchOptions? launchOptions = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        SessionRestarter.RestartSessionState state;

        lock (_lock)
        {
            state = SessionRestarter.PrepareRestart(sessionId, _sessions);
        }

        await SessionRestarter.PerformRestartAsync(
            sessionId,
            state,
            launchOptions ?? _dimensionTracker.DefaultLaunchOptions,
            LogSessionLifecycleEvent,
            cancellationToken);
    }

    /// <summary>
    ///     Applies font configuration changes to all sessions simultaneously.
    ///     This method ensures consistent font settings across all terminal sessions.
    /// </summary>
    /// <param name="fontConfig">The font configuration to apply to all sessions</param>
    /// <exception cref="ArgumentNullException">Thrown if fontConfig is null</exception>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public void ApplyFontConfigToAllSessions(object fontConfig)
    {
        if (fontConfig == null)
        {
            throw new ArgumentNullException(nameof(fontConfig));
        }

        ThrowIfDisposed();

        List<TerminalSession> sessionsToUpdate;

        lock (_lock)
        {
            // Create a snapshot of sessions to avoid holding the lock during updates
            sessionsToUpdate = _sessions.Values.ToList();
        }

        // Apply font configuration to each session outside the lock
        foreach (var session in sessionsToUpdate)
        {
            try
            {
                // Font configuration is applied at the display layer
                // The session itself doesn't need to know about font details
                // This method serves as a coordination point for triggering
                // terminal resize operations when font metrics change

                // The actual font application happens in the TerminalController
                // which will call TriggerTerminalResizeForAllSessions
            }
            catch (Exception ex)
            {
                // Log error but continue with other sessions
                // UI components can handle error reporting through events
                Console.WriteLine($"SessionManager: Error applying font config to session {session.Id}: {ex.Message}");
            }
        }
    }

    /// <summary>
    ///     Triggers terminal resize for all sessions when font metrics change.
    ///     This method should be called after font configuration changes to ensure
    ///     all sessions recalculate their dimensions with new character metrics.
    /// </summary>
    /// <param name="newCharacterWidth">New character width in pixels</param>
    /// <param name="newLineHeight">New line height in pixels</param>
    /// <param name="windowSize">Current window size for dimension calculations</param>
    /// <exception cref="ObjectDisposedException">Thrown if manager has been disposed</exception>
    public void TriggerTerminalResizeForAllSessions(float newCharacterWidth, float newLineHeight, (float width, float height) windowSize)
    {
        ThrowIfDisposed();

        // This method is kept for API compatibility but the actual resize logic
        // is handled by the TerminalController which has access to ImGui context
        // and proper dimension calculation methods

        // The TerminalController will call TriggerTerminalResizeForAllSessions()
        // which iterates through all sessions and resizes them appropriately
    }

    /// <summary>
    ///     Generates a unique session title based on current session count.
    /// </summary>
    /// <returns>A unique session title</returns>
    private string GenerateSessionTitle()
    {
        lock (_lock)
        {
            var sessionNumber = _sessions.Count + 1;
            return $"Terminal {sessionNumber}";
        }
    }

    /// <summary>
    ///     Handles session state change events.
    /// </summary>
    private void OnSessionStateChanged(object? sender, SessionStateChangedEventArgs e)
    {
        if (sender is TerminalSession session)
        {
            // Log session state changes for debugging and monitoring
            LogSessionLifecycleEvent($"Session {session.Id} state changed to {e.NewState}", e.Error);

            // Handle failed session states
            if (e.NewState == SessionState.Failed && e.Error != null)
            {
                LogSessionLifecycleEvent($"Session {session.Id} failed during operation", e.Error);

                // Attempt graceful cleanup of failed session resources
                try
                {
                    // Don't dispose immediately - let the session remain for potential restart
                    // Just ensure process resources are cleaned up
                    if (session.ProcessManager.IsRunning)
                    {
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await session.ProcessManager.StopAsync();
                            }
                            catch (Exception cleanupEx)
                            {
                                LogSessionLifecycleEvent($"Error cleaning up failed session {session.Id} process", cleanupEx);
                            }
                        });
                    }
                }
                catch (Exception cleanupEx)
                {
                    LogSessionLifecycleEvent($"Error during failed session {session.Id} cleanup", cleanupEx);
                }
            }
        }
    }

    /// <summary>
    ///     Handles session title change events.
    /// </summary>
    private void OnSessionTitleChanged(object? sender, SessionTitleChangedEventArgs e)
    {
        if (sender is TerminalSession session)
        {
            LogSessionLifecycleEvent($"Session {session.Id} title changed from '{e.OldTitle}' to '{e.NewTitle}'");
        }
    }

    /// <summary>
    ///     Handles session process exit events.
    /// </summary>
    private void OnSessionProcessExited(object? sender, SessionProcessExitedEventArgs e)
    {
        if (sender is TerminalSession session)
        {
            LogSessionLifecycleEvent($"Session {session.Id} process {e.ProcessId} exited with code {e.ExitCode}");

            // Update session state to reflect process exit
            // The session itself handles updating its settings with exit code
            // We just need to ensure the session state is properly managed

            // If the session is still active but process exited, mark it as inactive
            // This allows the user to see the exit status while keeping the session available
            if (session.State == SessionState.Active)
            {
                // Don't change to inactive automatically - let user see the exit status
                // The session will remain active but with a terminated process
            }
        }
    }

    /// <summary>
    ///     Logs session lifecycle events for debugging and monitoring.
    ///     Uses quiet operation principles - only logs when explicitly enabled or for errors.
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="exception">Optional exception to log</param>
    private void LogSessionLifecycleEvent(string message, Exception? exception = null)
    {
        // Follow quiet operation requirements - only log errors or when debug is enabled
        // Normal session operations should produce no output
        if (exception != null)
        {
            // Always log errors for debugging
            Console.WriteLine($"SessionManager Error: {message}");
            if (exception != null)
            {
                Console.WriteLine($"SessionManager Exception: {exception.Message}");
            }
        }
        // For non-error events, only log if debug mode is enabled
        // This could be controlled by a configuration setting in the future
        else if (IsDebugLoggingEnabled())
        {
            Console.WriteLine($"SessionManager: {message}");
        }
    }

    /// <summary>
    ///     Determines if debug logging is enabled for session lifecycle events.
    ///     Currently always returns false to maintain quiet operation.
    ///     Can be enhanced with configuration in the future.
    /// </summary>
    /// <returns>True if debug logging should be enabled</returns>
    private bool IsDebugLoggingEnabled()
    {
        // For now, maintain quiet operation by default
        // This could be controlled by environment variables or configuration
        return false;
    }

    /// <summary>
    ///     Throws ObjectDisposedException if the manager has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SessionManager));
        }
    }

    /// <summary>
    ///     Disposes the session manager and all its sessions.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            LogSessionLifecycleEvent("Disposing SessionManager");

            lock (_lock)
            {
                var sessionCount = _sessions.Count;
                LogSessionLifecycleEvent($"Disposing {sessionCount} sessions");

                foreach (var session in _sessions.Values)
                {
                    try
                    {
                        LogSessionLifecycleEvent($"Disposing session {session.Id}");

                        // Unsubscribe from events
                        session.StateChanged -= OnSessionStateChanged;
                        session.TitleChanged -= OnSessionTitleChanged;
                        session.ProcessExited -= OnSessionProcessExited;

                        session.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogSessionLifecycleEvent($"Error disposing session {session.Id}", ex);
                        // Continue with other sessions even if one fails
                    }
                }
                _sessions.Clear();
                _sessionOrder.Clear();
                _activeSessionId = null;
            }

            LogSessionLifecycleEvent("SessionManager disposed successfully");
            _disposed = true;
        }
    }
}
