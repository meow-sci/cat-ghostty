using System.Collections.Concurrent;

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
    private readonly ProcessLaunchOptions _defaultLaunchOptions;
    
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
        _defaultLaunchOptions = defaultLaunchOptions ?? ProcessLaunchOptions.CreateDefault();
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
        var terminal = new TerminalEmulator(80, 25); // Default terminal size
        var processManager = new ProcessManager();
        
        var session = new TerminalSession(sessionId, sessionTitle, terminal, processManager);
        
        // Wire up session events
        session.StateChanged += OnSessionStateChanged;
        session.TitleChanged += OnSessionTitleChanged;
        session.ProcessExited += OnSessionProcessExited;
        
        TerminalSession? previousActiveSession = null;
        
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
        
        try
        {
            await session.InitializeAsync(launchOptions ?? _defaultLaunchOptions, cancellationToken);
            session.Activate();
            
            SessionCreated?.Invoke(this, new SessionCreatedEventArgs(session));
            ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousActiveSession, session));
            
            return session;
        }
        catch
        {
            // Cleanup on failure
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
                    }
                }
            }
            
            // Unsubscribe from events
            session.StateChanged -= OnSessionStateChanged;
            session.TitleChanged -= OnSessionTitleChanged;
            session.ProcessExited -= OnSessionProcessExited;
            
            session.Dispose();
            throw;
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
            if (!_sessions.TryGetValue(sessionId, out var targetSession))
            {
                throw new ArgumentException($"Session {sessionId} not found", nameof(sessionId));
            }
            
            if (_activeSessionId == sessionId)
            {
                return; // Already active
            }
            
            var previousSession = _activeSessionId.HasValue && _sessions.TryGetValue(_activeSessionId.Value, out var prev) 
                ? prev 
                : null;
            
            // Deactivate current session
            previousSession?.Deactivate();
            
            // Activate target session
            _activeSessionId = sessionId;
            targetSession.Activate();
            
            ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(previousSession, targetSession));
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
        
        TerminalSession? sessionToClose = null;
        TerminalSession? newActiveSession = null;
        
        lock (_lock)
        {
            if (!_sessions.TryGetValue(sessionId, out sessionToClose))
            {
                return; // Session doesn't exist
            }
            
            // Prevent closing the last session
            if (_sessions.Count == 1)
            {
                throw new InvalidOperationException("Cannot close the last remaining session");
            }
            
            _sessions.Remove(sessionId);
            _sessionOrder.Remove(sessionId);
            
            // If this was the active session, find a new active session
            if (_activeSessionId == sessionId)
            {
                _activeSessionId = null;
                
                // Find the next session in order, or the previous one
                var remainingIds = _sessionOrder.Where(id => _sessions.ContainsKey(id)).ToList();
                if (remainingIds.Any())
                {
                    var newActiveId = remainingIds.First();
                    _activeSessionId = newActiveId;
                    newActiveSession = _sessions[newActiveId];
                }
            }
        }
        
        // Perform cleanup outside the lock
        try
        {
            // Unsubscribe from events
            sessionToClose.StateChanged -= OnSessionStateChanged;
            sessionToClose.TitleChanged -= OnSessionTitleChanged;
            sessionToClose.ProcessExited -= OnSessionProcessExited;
            
            await sessionToClose.CloseAsync(cancellationToken);
            SessionClosed?.Invoke(this, new SessionClosedEventArgs(sessionToClose));
            
            if (newActiveSession != null)
            {
                newActiveSession.Activate();
                ActiveSessionChanged?.Invoke(this, new ActiveSessionChangedEventArgs(sessionToClose, newActiveSession));
            }
        }
        catch
        {
            // Log error but continue - session cleanup errors should not prevent operation
            // UI components can handle error reporting through events
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
            if (_sessions.Count <= 1 || !_activeSessionId.HasValue)
            {
                return;
            }
            
            var currentIndex = _sessionOrder.IndexOf(_activeSessionId.Value);
            var nextIndex = (currentIndex + 1) % _sessionOrder.Count;
            var nextSessionId = _sessionOrder[nextIndex];
            
            SwitchToSession(nextSessionId);
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
            if (_sessions.Count <= 1 || !_activeSessionId.HasValue)
            {
                return;
            }
            
            var currentIndex = _sessionOrder.IndexOf(_activeSessionId.Value);
            var prevIndex = currentIndex == 0 ? _sessionOrder.Count - 1 : currentIndex - 1;
            var prevSessionId = _sessionOrder[prevIndex];
            
            SwitchToSession(prevSessionId);
        }
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
        // Session state changes are handled through events
        // UI components can subscribe to these events for notifications
    }

    /// <summary>
    ///     Handles session title change events.
    /// </summary>
    private void OnSessionTitleChanged(object? sender, SessionTitleChangedEventArgs e)
    {
        // Title changes are propagated through the session's TitleChanged event
        // UI components can subscribe to individual session events or manager events
    }

    /// <summary>
    ///     Handles session process exit events.
    /// </summary>
    private void OnSessionProcessExited(object? sender, SessionProcessExitedEventArgs e)
    {
        // Process exit events are propagated through the session's ProcessExited event
        // UI components can subscribe to these events for notifications
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
            lock (_lock)
            {
                foreach (var session in _sessions.Values)
                {
                    try
                    {
                        // Unsubscribe from events
                        session.StateChanged -= OnSessionStateChanged;
                        session.TitleChanged -= OnSessionTitleChanged;
                        session.ProcessExited -= OnSessionProcessExited;
                        
                        session.Dispose();
                    }
                    catch
                    {
                        // Log error but continue with disposal of other sessions
                        // UI components can handle error reporting through events
                    }
                }
                _sessions.Clear();
                _sessionOrder.Clear();
                _activeSessionId = null;
            }
            _disposed = true;
        }
    }
}