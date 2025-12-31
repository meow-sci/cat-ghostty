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
    ///     **Feature: multi-session-support, Property 4: Session Switching Behavior**
    ///     **Validates: Requirements 3.2, 4.3**
    ///     Property: For any session switch operation, the target session should become active 
    ///     and the previous session should become inactive. When closing the active session,
    ///     another session should automatically become active.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 2, QuietOnSuccess = true)]
    [Ignore(reason:"wsl2 too slow to do this regularly")]
    public FsCheck.Property SessionSwitchingBehavior()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                // Need at least 2 sessions to test switching behavior
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

                    // Test Requirement 3.2: Session switching makes target session active
                    for (int i = 0; i < createCount; i++)
                    {
                        var targetSession = createdSessions[i];
                        var previousActiveSession = sessionManager.ActiveSession;
                        
                        sessionManager.SwitchToSession(targetSession.Id);

                        // Target session should become active (Requirement 3.2)
                        if (sessionManager.ActiveSession?.Id != targetSession.Id)
                        {
                            return false.ToProperty().Label($"Expected session {targetSession.Id} to become active after switch");
                        }

                        if (targetSession.State != SessionState.Active)
                        {
                            return false.ToProperty().Label($"Expected switched session {targetSession.Id} to have Active state, got {targetSession.State}");
                        }

                        // Previous session should become inactive
                        if (previousActiveSession != null && previousActiveSession.Id != targetSession.Id)
                        {
                            if (previousActiveSession.State != SessionState.Inactive)
                            {
                                return false.ToProperty().Label($"Expected previous session {previousActiveSession.Id} to become Inactive, got {previousActiveSession.State}");
                            }
                        }
                    }

                    // Test Requirement 4.3: Closing active session activates another session
                    while (sessionManager.SessionCount > 1)
                    {
                        var activeSession = sessionManager.ActiveSession;
                        if (activeSession == null)
                        {
                            return false.ToProperty().Label("Expected an active session before closing");
                        }

                        var activeSessionId = activeSession.Id;
                        var remainingSessionsBefore = sessionManager.Sessions.Where(s => s.Id != activeSessionId).ToList();

                        sessionManager.CloseSessionAsync(activeSessionId).Wait(TimeSpan.FromSeconds(2));

                        // Another session should become active (Requirement 4.3)
                        var newActiveSession = sessionManager.ActiveSession;
                        if (newActiveSession == null)
                        {
                            return false.ToProperty().Label("Expected another session to become active after closing active session");
                        }

                        if (newActiveSession.Id == activeSessionId)
                        {
                            return false.ToProperty().Label("Expected a different session to become active after closing active session");
                        }

                        if (newActiveSession.State != SessionState.Active)
                        {
                            return false.ToProperty().Label($"Expected new active session {newActiveSession.Id} to have Active state, got {newActiveSession.State}");
                        }

                        // New active session should be one of the remaining sessions
                        if (!remainingSessionsBefore.Any(s => s.Id == newActiveSession.Id))
                        {
                            return false.ToProperty().Label($"New active session {newActiveSession.Id} was not in the remaining sessions list");
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

    /// <summary>
    ///     **Feature: multi-session-support, Property 6: Last Session Protection**
    ///     **Validates: Requirements 4.4, 4.5**
    ///     Property: For any session manager with only one session, attempting to close that
    ///     session should throw InvalidOperationException and preserve the session.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property LastSessionProtection()
    {
        return Prop.ForAll(SessionTitleArb,
            sessionTitle =>
            {
                using var sessionManager = new SessionManager(1);
                TerminalSession? createdSession = null;

                try
                {
                    // Create a single session
                    createdSession = sessionManager.CreateSessionAsync(sessionTitle).Result;

                    // Verify we have exactly one session
                    if (sessionManager.SessionCount != 1)
                    {
                        return false.ToProperty().Label($"Expected 1 session, got {sessionManager.SessionCount}");
                    }

                    // Verify the session is active
                    if (sessionManager.ActiveSession?.Id != createdSession.Id)
                    {
                        return false.ToProperty().Label("Expected the single session to be active");
                    }

                    // Attempt to close the last session should throw InvalidOperationException (Requirement 4.4)
                    bool exceptionThrown = false;
                    string? exceptionMessage = null;
                    
                    try
                    {
                        sessionManager.CloseSessionAsync(createdSession.Id).Wait(TimeSpan.FromSeconds(2));
                    }
                    catch (AggregateException ae) when (ae.InnerException is InvalidOperationException)
                    {
                        exceptionThrown = true;
                        exceptionMessage = ae.InnerException.Message;
                    }
                    catch (InvalidOperationException ex)
                    {
                        exceptionThrown = true;
                        exceptionMessage = ex.Message;
                    }

                    if (!exceptionThrown)
                    {
                        return false.ToProperty().Label("Expected InvalidOperationException when closing last session");
                    }

                    // Verify exception message indicates last session protection (Requirement 4.4)
                    if (string.IsNullOrEmpty(exceptionMessage) || !exceptionMessage.Contains("last remaining session"))
                    {
                        return false.ToProperty().Label($"Expected exception message about last session, got: {exceptionMessage}");
                    }

                    // Verify session is still present and active (Requirement 4.5)
                    if (sessionManager.SessionCount != 1)
                    {
                        return false.ToProperty().Label($"Expected session to be preserved after failed close, got count: {sessionManager.SessionCount}");
                    }

                    if (sessionManager.ActiveSession?.Id != createdSession.Id)
                    {
                        return false.ToProperty().Label("Expected the session to remain active after failed close");
                    }

                    if (createdSession.State == SessionState.Disposed || createdSession.State == SessionState.Terminating)
                    {
                        return false.ToProperty().Label($"Expected session to remain in valid state after failed close, got: {createdSession.State}");
                    }

                    // Verify session is still functional by checking its properties
                    if (string.IsNullOrEmpty(createdSession.Title))
                    {
                        return false.ToProperty().Label("Expected session title to be preserved after failed close");
                    }

                    if (createdSession.Terminal == null || createdSession.ProcessManager == null)
                    {
                        return false.ToProperty().Label("Expected session resources to be preserved after failed close");
                    }

                    return true.ToProperty();
                }
                finally
                {
                    // Clean up - force dispose the session manager to clean up resources
                    // This bypasses the last session protection for cleanup purposes
                    try
                    {
                        sessionManager.Dispose();
                    }
                    catch
                    {
                        // Ignore cleanup errors in tests
                    }
                }
            });
    }

    /// <summary>
    ///     **Feature: multi-session-support, Property 3: Tab Management Consistency**
    ///     **Validates: Requirements 3.1, 3.4**
    ///     Property: For any session manager, the tab order should remain consistent with session
    ///     creation order, and tab switching should properly update the active session display.
    /// </summary>
    [FsCheck.NUnit.Property(MaxTest = 100, QuietOnSuccess = true)]
    public FsCheck.Property TabManagementConsistency()
    {
        return Prop.ForAll(SessionCreateCountArb,
            createCount =>
            {
                // Need at least 2 sessions to test tab management
                if (createCount < 2) return true.ToProperty();

                using var sessionManager = new SessionManager(createCount);
                var createdSessions = new List<TerminalSession>();

                try
                {
                    // Create multiple sessions and track creation order
                    var expectedTabOrder = new List<Guid>();
                    for (int i = 0; i < createCount; i++)
                    {
                        var session = sessionManager.CreateSessionAsync($"Tab {i + 1}").Result;
                        createdSessions.Add(session);
                        expectedTabOrder.Add(session.Id);
                    }

                    // Verify tab order matches creation order (Requirement 3.1)
                    var actualTabOrder = sessionManager.Sessions.Select(s => s.Id).ToList();
                    if (!expectedTabOrder.SequenceEqual(actualTabOrder))
                    {
                        return false.ToProperty().Label($"Tab order mismatch. Expected: [{string.Join(", ", expectedTabOrder)}], Got: [{string.Join(", ", actualTabOrder)}]");
                    }

                    // Verify session titles are unique and follow expected pattern
                    var sessionTitles = sessionManager.Sessions.Select(s => s.Title).ToList();
                    if (sessionTitles.Count != sessionTitles.Distinct().Count())
                    {
                        return false.ToProperty().Label("Found duplicate session titles in tab management");
                    }

                    // Test tab switching updates active session properly (Requirement 3.4)
                    for (int i = 0; i < createCount; i++)
                    {
                        var targetSession = createdSessions[i];
                        sessionManager.SwitchToSession(targetSession.Id);

                        // Active session should match the switched tab
                        if (sessionManager.ActiveSession?.Id != targetSession.Id)
                        {
                            return false.ToProperty().Label($"Tab switch failed: expected session {targetSession.Id} to be active");
                        }

                        // Active session should have correct state
                        if (targetSession.State != SessionState.Active)
                        {
                            return false.ToProperty().Label($"Active tab session {targetSession.Id} should have Active state, got {targetSession.State}");
                        }

                        // All other sessions should be inactive
                        var inactiveSessions = sessionManager.Sessions.Where(s => s.Id != targetSession.Id).ToList();
                        foreach (var inactiveSession in inactiveSessions)
                        {
                            if (inactiveSession.State != SessionState.Inactive)
                            {
                                return false.ToProperty().Label($"Non-active tab session {inactiveSession.Id} should have Inactive state, got {inactiveSession.State}");
                            }
                        }

                        // Tab order should remain unchanged after switching
                        var tabOrderAfterSwitch = sessionManager.Sessions.Select(s => s.Id).ToList();
                        if (!expectedTabOrder.SequenceEqual(tabOrderAfterSwitch))
                        {
                            return false.ToProperty().Label($"Tab order changed after switching. Expected: [{string.Join(", ", expectedTabOrder)}], Got: [{string.Join(", ", tabOrderAfterSwitch)}]");
                        }
                    }

                    // Test next/previous tab navigation maintains consistency
                    var startingActiveSession = sessionManager.ActiveSession;
                    if (startingActiveSession == null)
                    {
                        return false.ToProperty().Label("Expected an active session for navigation testing");
                    }

                    // Test next session navigation
                    sessionManager.SwitchToNextSession();
                    var nextSession = sessionManager.ActiveSession;
                    if (nextSession == null)
                    {
                        return false.ToProperty().Label("Expected an active session after SwitchToNextSession");
                    }

                    // Should be the next session in tab order
                    var startingIndex = expectedTabOrder.IndexOf(startingActiveSession.Id);
                    var expectedNextIndex = (startingIndex + 1) % expectedTabOrder.Count;
                    var expectedNextSessionId = expectedTabOrder[expectedNextIndex];
                    
                    if (nextSession.Id != expectedNextSessionId)
                    {
                        return false.ToProperty().Label($"Next session navigation failed: expected {expectedNextSessionId}, got {nextSession.Id}");
                    }

                    // Test previous session navigation
                    sessionManager.SwitchToPreviousSession();
                    var prevSession = sessionManager.ActiveSession;
                    if (prevSession == null)
                    {
                        return false.ToProperty().Label("Expected an active session after SwitchToPreviousSession");
                    }

                    // Should be back to the starting session
                    if (prevSession.Id != startingActiveSession.Id)
                    {
                        return false.ToProperty().Label($"Previous session navigation failed: expected {startingActiveSession.Id}, got {prevSession.Id}");
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
