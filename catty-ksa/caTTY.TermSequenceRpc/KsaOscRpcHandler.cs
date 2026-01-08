using System.Text.Json;
using caTTY.Core.Rpc;
using KSA;
using Microsoft.Extensions.Logging;

namespace caTTY.TermSequenceRpc;

/// <summary>
/// KSA-specific implementation of OSC RPC handler.
/// Dispatches JSON action commands to KSA game engine for vehicle control.
/// Uses OSC sequences in the private-use range (1000+) which pass through
/// Windows ConPTY, unlike DCS sequences which are filtered.
/// </summary>
public class KsaOscRpcHandler : OscRpcHandler
{
    /// <summary>
    /// Known action names for KSA JSON dispatch.
    /// Supported commands for KSA game vehicle control.
    /// </summary>
    public static class Actions
    {
        /// <summary>Engine ignition action</summary>
        public const string EngineIgnite = "engine_ignite";
        /// <summary>Engine shutdown action</summary>
        public const string EngineShutdown = "engine_shutdown";
    }

    /// <summary>
    /// Initializes a new instance of the KsaOscRpcHandler.
    /// </summary>
    /// <param name="logger">Logger for debugging and error reporting</param>
    public KsaOscRpcHandler(ILogger logger) : base(logger)
    {
    }

    /// <inheritdoc />
    /// <summary>
    /// Dispatches parsed JSON actions to KSA game engine.
    /// Accesses Program.ControlledVehicle to control the active spacecraft.
    /// </summary>
    protected override void DispatchAction(string action, JsonElement root)
    {
        switch (action)
        {
            case Actions.EngineIgnite:
                Program.ControlledVehicle?.SetEnum(VehicleEngine.MainIgnite);
                Logger.LogDebug("KSA OSC RPC: Engine ignite executed");
                break;

            case Actions.EngineShutdown:
                Program.ControlledVehicle?.SetEnum(VehicleEngine.MainShutdown);
                Logger.LogDebug("KSA OSC RPC: Engine shutdown executed");
                break;

            default:
                Logger.LogWarning("KSA OSC RPC: Unknown action '{Action}'", action);
                break;
        }
    }
}
