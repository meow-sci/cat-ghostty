using caTTY.Core.Terminal;
using FsCheck;
using NUnit.Framework;

namespace caTTY.Core.Tests.Property;

/// <summary>
///     Property-based tests for session lifecycle management in the SessionManager.
///     These tests verify universal properties that should hold for all session operations.
///     **Feature: multi-session-support, Property 1: Session Lifecycle Management**
///     **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**
/// </summary>
[TestFixture]
[Category("Property")]
[Ignore(reason:"wsl2 too slow to do this regularly")]
public class SessionLifecycleProperties
{
    /// <summary>
    ///     Generator for valid session titles.
    /// </summary>
    public static Arbitrary<string> SessionTitleArb =>
        Arb.From(Gen.Elements("Terminal 1", "Terminal 2", "Shell", "PowerShell", "WSL", "Custom Session", "Test Terminal"));

    /// <summary>
    ///     Generator for valid maximum session counts.
    /// </summary>
    public static Arbitrary<int> MaxSessionCountArb =>
        Arb.From(Gen.Choose(1, 10)); // Keep reasonable for testing

    /// <summary>
    ///     Generator for session creation counts (less than max).
    /// </summary>
    public static Arbitrary<int> SessionCreateCountArb =>
        Arb.From(Gen.Choose(1, 5));

    /// <summary>
    ///     **Feature: multi-session-support, Property 1: Session Lifecycle Management**
    ///     **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**
    ///     Property: For any session manager, creating sessions should maintain unique IDs,
    ///     assign proper titles, and track sessions correctly in the collection.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore(reason:"wsl2 too slow to do this regularly")]
    public FsCheck.Property SessionCreationMaintainsUniqueIds()
    {
        return Prop.ForAll(MaxSessionCountArb, SessionCreateCountArb,
            (maxSessions, createCount) =>
            {
                // Ensure createCount doesn't exceed maxSessions
                var actualCreateCount = Math.Min(createCount, maxSessions);

                using var sessionManager = new SessionManager(maxSessions);
                var createdSessions = new List<TerminalSession>();
                var sessionIds = new HashSet<Guid>();

                try
                {
                    // Create multiple sessions
                    for (int i = 0; i < actualCreateCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync().Result;
                        createdSessions.Add(session);

                        // Verify unique ID
                        if (sessionIds.Contains(session.Id))
                        {
                            return false.ToProperty().Label($"Duplicate session ID found: {session.Id}");
                        }
                        sessionIds.Add(session.Id);

                        // Verify session is tracked in manager
                        if (!sessionManager.Sessions.Any(s => s.Id == session.Id))
                        {
                            return false.ToProperty().Label($"Session {session.Id} not found in manager collection");
                        }

                        // Verify session count is correct
                        if (sessionManager.SessionCount != i + 1)
                        {
                            return false.ToProperty().Label($"Expected session count {i + 1}, got {sessionManager.SessionCount}");
                        }
                    }

                    // Verify all sessions have unique IDs
                    var allSessionIds = sessionManager.Sessions.Select(s => s.Id).ToList();
                    if (allSessionIds.Count != allSessionIds.Distinct().Count())
                    {
                        return false.ToProperty().Label("Found duplicate session IDs in manager collection");
                    }

                    // Verify session count matches created count
                    if (sessionManager.SessionCount != actualCreateCount)
                    {
                        return false.ToProperty().Label($"Expected final session count {actualCreateCount}, got {sessionManager.SessionCount}");
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up sessions
                    foreach (var session in createdSessions)
                    {
                        try
                        {
                            session.CloseAsync().Wait(TimeSpan.FromSeconds(1));
                        }
                        catch
                        {
                            // Ignore cleanup errors in tests
                        }
                    }
                }
            });
    }

    /// <summary>
    ///     **Feature: multi-session-support, Property 1: Session Lifecycle Management**
    ///     **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**
    ///     Property: For any session manager, the active session should always be properly tracked
    ///     and only one session should be active at a time.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore(reason:"wsl2 too slow to do this regularly")]
    public FsCheck.Property ActiveSessionTrackingIsConsistent()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                using var sessionManager = new SessionManager(Math.Max(createCount, 1));
                var createdSessions = new List<TerminalSession>();

                try
                {
                    // Initially no active session
                    if (sessionManager.ActiveSession != null)
                    {
                        return false.ToProperty().Label("Expected no active session initially");
                    }

                    // Create sessions and verify active session tracking
                    for (int i = 0; i < createCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync().Result;
                        createdSessions.Add(session);

                        // Newly created session should be active
                        if (sessionManager.ActiveSession?.Id != session.Id)
                        {
                            return false.ToProperty().Label($"Expected session {session.Id} to be active after creation");
                        }

                        // Session state should be Active
                        if (session.State != SessionState.Active)
                        {
                            return false.ToProperty().Label($"Expected session {session.Id} to have Active state, got {session.State}");
                        }

                        // Only one session should be active
                        var activeSessions = sessionManager.Sessions.Where(s => s.State == SessionState.Active).ToList();
                        if (activeSessions.Count != 1)
                        {
                            return false.ToProperty().Label($"Expected exactly 1 active session, found {activeSessions.Count}");
                        }

                        // Previous sessions should be inactive
                        for (int j = 0; j < i; j++)
                        {
                            if (createdSessions[j].State != SessionState.Inactive)
                            {
                                return false.ToProperty().Label($"Expected previous session {createdSessions[j].Id} to be Inactive, got {createdSessions[j].State}");
                            }
                        }
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up sessions
                    foreach (var session in createdSessions)
                    {
                        try
                        {
                            session.CloseAsync().Wait(TimeSpan.FromSeconds(1));
                        }
                        catch
                        {
                            // Ignore cleanup errors in tests
                        }
                    }
                }
            });
    }

    /// <summary>
    ///     **Feature: multi-session-support, Property 1: Session Lifecycle Management**
    ///     **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**
    ///     Property: For any session manager, session switching should properly activate the target
    ///     session and deactivate the previous session.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore(reason:"wsl2 too slow to do this regularly")]
    public FsCheck.Property SessionSwitchingMaintainsConsistency()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                // Need at least 2 sessions to test switching
                if (createCount < 2) return true.ToProperty();

                using var sessionManager = new SessionManager(createCount);
                var createdSessions = new List<TerminalSession>();

                try
                {
                    // Create multiple sessions
                    for (int i = 0; i < createCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync().Result;
                        createdSessions.Add(session);
                    }

                    // Test switching to each session
                    for (int i = 0; i < createCount; i++)
                    {
                        var targetSession = createdSessions[i];
                        sessionManager.SwitchToSession(targetSession.Id);

                        // Target session should be active
                        if (sessionManager.ActiveSession?.Id != targetSession.Id)
                        {
                            return false.ToProperty().Label($"Expected session {targetSession.Id} to be active after switch");
                        }

                        if (targetSession.State != SessionState.Active)
                        {
                            return false.ToProperty().Label($"Expected switched session {targetSession.Id} to have Active state, got {targetSession.State}");
                        }

                        // All other sessions should be inactive
                        for (int j = 0; j < createCount; j++)
                        {
                            if (j != i && createdSessions[j].State != SessionState.Inactive)
                            {
                                return false.ToProperty().Label($"Expected non-active session {createdSessions[j].Id} to be Inactive, got {createdSessions[j].State}");
                            }
                        }

                        // Only one session should be active
                        var activeSessions = sessionManager.Sessions.Where(s => s.State == SessionState.Active).ToList();
                        if (activeSessions.Count != 1)
                        {
                            return false.ToProperty().Label($"Expected exactly 1 active session after switch, found {activeSessions.Count}");
                        }
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up sessions
                    foreach (var session in createdSessions)
                    {
                        try
                        {
                            session.CloseAsync().Wait(TimeSpan.FromSeconds(1));
                        }
                        catch
                        {
                            // Ignore cleanup errors in tests
                        }
                    }
                }
            });
    }

    /// <summary>
    ///     **Feature: multi-session-support, Property 2: Session Creation and Initialization**
    ///     **Validates: Requirements 2.2, 2.3, 2.5**
    ///     Property: For any session manager, creating a session should initialize it with default
    ///     shell configuration, assign unique titles, and start the shell process automatically.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore("Real shell test - validated and disabled for CI")]
    public FsCheck.Property SessionCreationAndInitialization()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                using var sessionManager = new SessionManager(Math.Max(createCount, 1));
                var createdSessions = new List<TerminalSession>();

                try
                {
                    // Create sessions and verify initialization
                    for (int i = 0; i < createCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync().Result;
                        createdSessions.Add(session);

                        // Verify session has unique title (Requirement 2.3)
                        if (string.IsNullOrEmpty(session.Title))
                        {
                            return false.ToProperty().Label($"Session {session.Id} has empty title");
                        }

                        // Verify title follows expected pattern
                        if (!session.Title.StartsWith("Terminal "))
                        {
                            return false.ToProperty().Label($"Session {session.Id} title '{session.Title}' doesn't follow expected pattern");
                        }

                        // Verify all session titles are unique
                        var titles = createdSessions.Select(s => s.Title).ToList();
                        if (titles.Count != titles.Distinct().Count())
                        {
                            return false.ToProperty().Label("Found duplicate session titles");
                        }

                        // Verify session is initialized with default shell configuration (Requirement 2.2)
                        if (session.Terminal == null)
                        {
                            return false.ToProperty().Label($"Session {session.Id} terminal is null");
                        }

                        if (session.ProcessManager == null)
                        {
                            return false.ToProperty().Label($"Session {session.Id} process manager is null");
                        }

                        // Verify shell process is started automatically (Requirement 2.5)
                        // Note: We check that ProcessManager exists and is in a valid state
                        // The actual process startup is tested in integration tests due to timing
                        if (session.State == SessionState.Failed)
                        {
                            return false.ToProperty().Label($"Session {session.Id} failed to initialize");
                        }

                        // Verify session has proper settings
                        if (session.Settings == null)
                        {
                            return false.ToProperty().Label($"Session {session.Id} settings is null");
                        }

                        if (session.Settings.Title != session.Title)
                        {
                            return false.ToProperty().Label($"Session {session.Id} settings title mismatch");
                        }

                        // Verify session is properly tracked
                        if (session.CreatedAt == default)
                        {
                            return false.ToProperty().Label($"Session {session.Id} has invalid creation time");
                        }
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up sessions
                    foreach (var session in createdSessions)
                    {
                        try
                        {
                            session.CloseAsync().Wait(TimeSpan.FromSeconds(1));
                        }
                        catch
                        {
                            // Ignore cleanup errors in tests
                        }
                    }
                }
            });
    }

    /// <summary>
    ///     **Feature: multi-session-support, Property 1: Session Lifecycle Management**
    ///     **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**
    ///     Property: For any session manager, resource cleanup should properly dispose of
    ///     terminal and process resources when sessions are closed.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore(reason:"wsl2 too slow to do this regularly")]
    public FsCheck.Property SessionClosurePerformsResourceCleanup()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                // Need at least 2 sessions to test closure (can't close last session)
                if (createCount < 2) return true.ToProperty();

                using var sessionManager = new SessionManager(createCount);
                var createdSessions = new List<TerminalSession>();

                try
                {
                    // Create multiple sessions
                    for (int i = 0; i < createCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync().Result;
                        createdSessions.Add(session);
                    }

                    var initialCount = sessionManager.SessionCount;

                    // Close all but the last session
                    for (int i = 0; i < createCount - 1; i++)
                    {
                        var sessionToClose = createdSessions[i];
                        var sessionId = sessionToClose.Id;

                        sessionManager.CloseSessionAsync(sessionId).Wait(TimeSpan.FromSeconds(2));

                        // Session should be disposed
                        if (sessionToClose.State != SessionState.Disposed)
                        {
                            return false.ToProperty().Label($"Expected closed session {sessionId} to have Disposed state, got {sessionToClose.State}");
                        }

                        // Session should be removed from manager
                        if (sessionManager.Sessions.Any(s => s.Id == sessionId))
                        {
                            return false.ToProperty().Label($"Closed session {sessionId} still found in manager collection");
                        }

                        // Session count should decrease
                        var expectedCount = initialCount - (i + 1);
                        if (sessionManager.SessionCount != expectedCount)
                        {
                            return false.ToProperty().Label($"Expected session count {expectedCount} after closing session, got {sessionManager.SessionCount}");
                        }

                        // There should still be an active session (the remaining one)
                        if (sessionManager.ActiveSession == null)
                        {
                            return false.ToProperty().Label("Expected an active session to remain after closing non-last session");
                        }
                    }

                    // Should have exactly one session remaining
                    if (sessionManager.SessionCount != 1)
                    {
                        return false.ToProperty().Label($"Expected 1 session remaining, got {sessionManager.SessionCount}");
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up any remaining sessions
                    foreach (var session in createdSessions)
                    {
                        try
                        {
                            if (session.State != SessionState.Disposed)
                            {
                                session.CloseAsync().Wait(TimeSpan.FromSeconds(1));
                            }
                        }
                        catch
                        {
                            // Ignore cleanup errors in tests
                        }
                    }
                }
            });
    }
}
