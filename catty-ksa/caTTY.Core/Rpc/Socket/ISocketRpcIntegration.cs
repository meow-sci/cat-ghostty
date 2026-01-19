namespace caTTY.Core.Rpc.Socket;

/// <summary>
/// Interface for components that can host a socket RPC server.
/// Implemented by ProcessManager to provide RPC alongside PTY.
/// </summary>
public interface ISocketRpcIntegration
{
    /// <summary>
    /// Gets the socket RPC server, if configured.
    /// </summary>
    ISocketRpcServer? SocketRpcServer { get; }

    /// <summary>
    /// Gets the socket path for client connections.
    /// Returns null if no RPC server is configured.
    /// </summary>
    string? SocketRpcPath { get; }

    /// <summary>
    /// Configures the socket RPC handler to use when starting processes.
    /// Must be called before StartAsync.
    /// </summary>
    /// <param name="handler">The handler to dispatch requests to</param>
    void ConfigureSocketRpc(ISocketRpcHandler handler);
}
