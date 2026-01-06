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
    /// <returns>A fully configured terminal session</returns>
    public static TerminalSession CreateSession(
        Guid sessionId,
        string sessionTitle,
        int initialWidth,
        int initialHeight,
        EventHandler<SessionStateChangedEventArgs> onStateChanged,
        EventHandler<SessionTitleChangedEventArgs> onTitleChanged,
        EventHandler<SessionProcessExitedEventArgs> onProcessExited)
    {
        var router = new RpcCommandRouter(NullLogger.Instance);
        var responseGenerator = new RpcResponseGenerator();
        var _outputBuffer = new List<byte[]>();

        var rpcHandler = new RpcHandler(
            router,
            responseGenerator,
            bytes => _outputBuffer.Add(bytes),
            NullLogger.Instance);

        var registry = new GameActionRegistry(router, NullLogger.Instance, null);
        registry.RegisterVehicleCommands();


        var terminal = TerminalEmulator.Create(initialWidth, initialHeight, 2500, NullLogger.Instance, rpcHandler);



        var processManager = new ProcessManager();

        var session = new TerminalSession(sessionId, sessionTitle, terminal, processManager);

        // Wire up session events
        session.StateChanged += onStateChanged;
        session.TitleChanged += onTitleChanged;
        session.ProcessExited += onProcessExited;

        return session;
    }
}
