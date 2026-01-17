using caTTY.Core.Rpc;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Terminal;

/// <summary>
///     Factory for creating and wiring terminal sessions.
/// </summary>
internal class TerminalSessionFactory
{
    /// <summary>
    ///     Creates a new terminal session with all necessary components and event subscriptions.
    /// </summary>
    /// <param name="sessionId">Unique identifier for the session</param>
    /// <param name="sessionTitle">Title for the session</param>
    /// <param name="initialWidth">Initial width in columns</param>
    /// <param name="initialHeight">Initial height in rows</param>
    /// <param name="onStateChanged">Event handler for session state changes</param>
    /// <param name="onTitleChanged">Event handler for session title changes</param>
    /// <param name="onProcessExited">Event handler for process exit</param>
    /// <param name="rpcHandler">Optional RPC handler for game integration (null disables RPC functionality)</param>
    /// <param name="oscRpcHandler">Optional OSC RPC handler for OSC-based RPC commands (null uses default no-op handler)</param>
    /// <param name="launchOptions">Optional launch options for the session (used to determine shell type)</param>
    /// <returns>A fully configured terminal session</returns>
    public static TerminalSession CreateSession(
        Guid sessionId,
        string sessionTitle,
        int initialWidth,
        int initialHeight,
        EventHandler<SessionStateChangedEventArgs> onStateChanged,
        EventHandler<SessionTitleChangedEventArgs> onTitleChanged,
        EventHandler<SessionProcessExitedEventArgs> onProcessExited,
        IRpcHandler? rpcHandler = null,
        IOscRpcHandler? oscRpcHandler = null,
        ProcessLaunchOptions? launchOptions = null)
    {
        var terminal = TerminalEmulator.Create(initialWidth, initialHeight, 2500, NullLogger.Instance, rpcHandler, oscRpcHandler);

        // Conditional process manager creation based on shell type
        IProcessManager processManager;
        if (launchOptions?.ShellType == ShellType.CustomGame && !string.IsNullOrEmpty(launchOptions.CustomShellId))
        {
            var customShell = CustomShellRegistry.Instance.CreateShell(launchOptions.CustomShellId);
            processManager = new CustomShellPtyBridge(customShell);
        }
        else
        {
            processManager = new ProcessManager();
        }

        var session = new TerminalSession(sessionId, sessionTitle, terminal, processManager);

        // Wire up session events
        session.StateChanged += onStateChanged;
        session.TitleChanged += onTitleChanged;
        session.ProcessExited += onProcessExited;

        return session;
    }
}
