using System.Text.Json;
using caTTY.Core.Rpc.Socket;
using KSA;
using Microsoft.Extensions.Logging;

namespace caTTY.TermSequenceRpc.SocketRpc.Actions;

/// <summary>
/// Lists all crafts/vehicles in the current game.
/// Returns array of craft info objects.
/// </summary>
public class ListCraftsAction : ISocketRpcAction
{
    private readonly ILogger _logger;

    public string ActionName => "list-crafts";

    public ListCraftsAction(ILogger logger)
    {
        _logger = logger;
    }

    public SocketRpcResponse Execute(JsonElement? @params)
    {
        try
        {
            var crafts = new List<object>();

            // For now, only return the controlled vehicle if it exists
            // TODO: Expand to list all vehicles when KSA API is better understood
            var controlledVehicle = Program.ControlledVehicle;
            if (controlledVehicle != null)
            {
                crafts.Add(new
                {
                    name = $"Controlled Vehicle",
                    isControlled = true
                });
            }

            _logger.LogDebug("list-crafts returning {Count} vehicles", crafts.Count);
            return SocketRpcResponse.Ok(crafts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list crafts");
            return SocketRpcResponse.Fail($"Failed to list crafts: {ex.Message}");
        }
    }
}
