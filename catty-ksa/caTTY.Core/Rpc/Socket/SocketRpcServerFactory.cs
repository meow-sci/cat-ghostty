using Microsoft.Extensions.Logging;

namespace caTTY.Core.Rpc.Socket;

/// <summary>
/// Factory for creating socket RPC servers with unique socket paths.
/// </summary>
public static class SocketRpcServerFactory
{
    /// <summary>
    /// Environment variable name for the RPC socket path.
    /// </summary>
    public const string SocketPathEnvVar = "KSA_RPC_SOCKET";

    /// <summary>
    /// Generates a unique socket path for a terminal session.
    /// </summary>
    /// <param name="sessionId">Optional session identifier for uniqueness</param>
    /// <returns>Full path to the socket file</returns>
    public static string GenerateSocketPath(string? sessionId = null)
    {
        var id = sessionId ?? Guid.NewGuid().ToString("N")[..8];
        var fileName = $"ksa-rpc-{id}.sock";

        // Use temp directory for cross-platform compatibility
        return Path.Combine(Path.GetTempPath(), fileName);
    }

    /// <summary>
    /// Creates a new socket RPC server instance.
    /// </summary>
    /// <param name="handler">Handler to dispatch requests to</param>
    /// <param name="logger">Logger for diagnostics</param>
    /// <param name="sessionId">Optional session identifier</param>
    /// <returns>Configured but not started server instance</returns>
    public static ISocketRpcServer Create(ISocketRpcHandler handler, ILogger logger, string? sessionId = null)
    {
        var socketPath = GenerateSocketPath(sessionId);
        return new SocketRpcServer(socketPath, handler, logger);
    }
}
