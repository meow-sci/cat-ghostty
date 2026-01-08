using caTTY.Core.Rpc;
using Microsoft.Extensions.Logging;

namespace caTTY.TermSequenceRpc;

/// <summary>
/// Factory for creating and wiring KSA-specific RPC handlers.
/// Provides a single entry point for initializing all RPC components needed for KSA game integration.
/// </summary>
public static class RpcBootstrapper
{
    /// <summary>
    /// Creates fully configured RPC handlers for KSA game integration.
    /// Wires up CSI RPC command router and OSC RPC handler with KSA-specific implementations.
    /// </summary>
    /// <param name="logger">Logger instance for debugging and error reporting</param>
    /// <param name="outputCallback">Optional callback for RPC response output (e.g., sending bytes to terminal).
    /// If null, responses will be discarded.</param>
    /// <returns>Tuple of (IRpcHandler for CSI RPC, IOscRpcHandler for OSC RPC)</returns>
    public static (IRpcHandler rpcHandler, IOscRpcHandler oscRpcHandler)
        CreateKsaRpcHandlers(ILogger logger, Action<byte[]>? outputCallback = null)
    {
        // Create RPC infrastructure
        var router = new RpcCommandRouter(logger);
        var responseGenerator = new RpcResponseGenerator();

        var rpcHandler = new RpcHandler(
            router,
            responseGenerator,
            outputCallback ?? (_ => { }), // No-op if no callback provided
            logger);

        // Create KSA-specific handlers
        var oscRpcHandler = new KsaOscRpcHandler(logger);
        var registry = new KsaGameActionRegistry(router, logger, null);

        // Register vehicle commands
        registry.RegisterVehicleCommands();

        return (rpcHandler, oscRpcHandler);
    }
}
