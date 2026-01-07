using System.Text.Json;
using KSA;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Rpc;

/// <summary>
///     Handles OSC-based RPC commands for KSA game integration.
///     Uses OSC sequences in the private-use range (1000+) because they pass through
///     Windows ConPTY, unlike DCS sequences which are filtered.
///     
///     Supported commands:
///     - OSC 1010: JSON action dispatch (engine_ignite, engine_shutdown, etc.)
///     
///     Format: ESC ] {command} ; {json_payload} BEL/ST
///     Example: ESC ] 1010 ; {"action":"engine_ignite"} BEL
/// </summary>
public class OscRpcHandler : IOscRpcHandler
{
    private readonly ILogger _logger;

    /// <summary>
    ///     Minimum command number for private/application-specific OSC commands.
    ///     Standard xterm OSC codes are 0-119, we use 1000+ for custom commands.
    /// </summary>
    private const int PrivateCommandRangeStart = 1000;

    /// <summary>
    ///     OSC command for JSON action dispatch.
    /// </summary>
    public const int JsonActionCommand = 1010;

    /// <summary>
    ///     Known action names for JSON dispatch.
    /// </summary>
    public static class Actions
    {
        public const string EngineIgnite = "engine_ignite";
        public const string EngineShutdown = "engine_shutdown";
    }

    public OscRpcHandler(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsPrivateCommand(int command)
    {
        return command >= PrivateCommandRangeStart;
    }

    /// <inheritdoc />
    public void HandleCommand(int command, string? payload)
    {
        switch (command)
        {
            case JsonActionCommand:
                HandleJsonAction(payload);
                break;

            default:
                _logger.LogWarning("OSC RPC: Unknown private command {Command}", command);
                break;
        }
    }

    /// <summary>
    ///     Handles JSON action commands from OSC 1010 sequences.
    ///     Format: {"action":"action_name", ...optional_params}
    /// </summary>
    private void HandleJsonAction(string? payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            _logger.LogWarning("OSC 1010: JSON action command received with empty payload");
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("action", out var actionElement))
            {
                _logger.LogWarning("OSC 1010: JSON payload missing 'action' property: {Payload}", payload);
                return;
            }

            string? action = actionElement.GetString();
            if (string.IsNullOrEmpty(action))
            {
                _logger.LogWarning("OSC 1010: JSON 'action' property is empty");
                return;
            }

            _logger.LogInformation("OSC 1010: Executing action '{Action}'", action);
            DispatchAction(action, root);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("OSC 1010: Failed to parse JSON payload: {Error}. Payload: {Payload}", ex.Message, payload);
        }
    }

    /// <summary>
    ///     Dispatches a parsed action to the appropriate KSA handler.
    /// </summary>
    private void DispatchAction(string action, JsonElement root)
    {
        switch (action)
        {
            case Actions.EngineIgnite:
                Program.ControlledVehicle?.SetEnum(VehicleEngine.MainIgnite);
                _logger.LogDebug("OSC 1010: Engine ignite command executed");
                break;

            case Actions.EngineShutdown:
                Program.ControlledVehicle?.SetEnum(VehicleEngine.MainShutdown);
                _logger.LogDebug("OSC 1010: Engine shutdown command executed");
                break;

            default:
                _logger.LogWarning("OSC 1010: Unknown action '{Action}'", action);
                break;
        }
    }
}
