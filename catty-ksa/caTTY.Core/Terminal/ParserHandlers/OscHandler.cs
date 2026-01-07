using System.Text.Json;
using caTTY.Core.Types;
using KSA;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal.ParserHandlers;

/// <summary>
///     Handles OSC (Operating System Command) sequence processing.
/// </summary>
internal class OscHandler
{
    private readonly ILogger _logger;
    private readonly TerminalEmulator _terminal;

    /// <summary>
    ///     Known action names for JSON dispatch.
    /// </summary>
    private static class Actions
    {
        public const string EngineIgnite = "engine_ignite";
        public const string EngineShutdown = "engine_shutdown";
    }

    public OscHandler(TerminalEmulator terminal, ILogger logger)
    {
        _terminal = terminal;
        _logger = logger;
    }

    public void HandleOsc(OscMessage message)
    {
        // Check if this is an implemented xterm OSC message
        if (message.XtermMessage != null && message.XtermMessage.Implemented)
        {
            HandleXtermOsc(message.XtermMessage);
            return;
        }

        // Handle generic OSC sequences
        _logger.LogDebug("OSC sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleXtermOsc(XtermOscMessage message)
    {
        switch (message.Type)
        {
            case "osc.setTitleAndIcon":
                // OSC 0: Set both window title and icon name
                _terminal.SetTitleAndIcon(message.Title ?? string.Empty);
                _logger.LogDebug("Set title and icon: {Title}", message.Title);
                break;

            case "osc.setIconName":
                // OSC 1: Set icon name only
                _terminal.SetIconName(message.IconName ?? string.Empty);
                _logger.LogDebug("Set icon name: {IconName}", message.IconName);
                break;

            case "osc.setWindowTitle":
                // OSC 2: Set window title only
                _terminal.SetWindowTitle(message.Title ?? string.Empty);
                _logger.LogDebug("Set window title: {Title}", message.Title);
                break;

            case "osc.queryWindowTitle":
                // OSC 21: Query window title - respond with OSC ] L <title> ST (ESC \\)
                string currentTitle = _terminal.GetWindowTitle();
                string titleResponse = $"\x1b]L{currentTitle}\x1b\\";
                _terminal.EmitResponse(titleResponse);
                _logger.LogDebug("Query window title response: {Response}", titleResponse);
                break;

            case "osc.clipboard":
                // OSC 52: Clipboard operations - handle clipboard data and queries
                if (message.ClipboardData != null)
                {
                    _terminal.HandleClipboard(message.ClipboardData);
                    _logger.LogDebug("Clipboard operation: {Data}", message.ClipboardData);
                }
                break;

            case "osc.hyperlink":
                // OSC 8: Hyperlink operations - associate URLs with character ranges
                if (message.HyperlinkUrl != null)
                {
                    _terminal.HandleHyperlink(message.HyperlinkUrl);
                    _logger.LogDebug("Hyperlink operation: {Url}", message.HyperlinkUrl);
                }
                break;

            case "osc.queryForegroundColor":
                // OSC 10;? : Query foreground color - respond with current foreground color
                var currentForeground = _terminal.GetCurrentForegroundColor();
                string foregroundResponse = DeviceResponses.GenerateForegroundColorResponse(
                    currentForeground.Red, currentForeground.Green, currentForeground.Blue);
                _terminal.EmitResponse(foregroundResponse);
                _logger.LogDebug("Query foreground color response: {Response}", foregroundResponse);
                break;

            case "osc.queryBackgroundColor":
                // OSC 11;? : Query background color - respond with current background color
                var currentBackground = _terminal.GetCurrentBackgroundColor();
                string backgroundResponse = DeviceResponses.GenerateBackgroundColorResponse(
                    currentBackground.Red, currentBackground.Green, currentBackground.Blue);
                _terminal.EmitResponse(backgroundResponse);
                _logger.LogDebug("Query background color response: {Response}", backgroundResponse);
                break;

            case "osc.jsonAction":
                // OSC 1010: Custom JSON action command for KSA game integration
                HandleJsonActionCommand(message.Payload);
                break;

            default:
                // Log unhandled xterm OSC sequences for debugging
                _logger.LogDebug("Xterm OSC: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
    }

    /// <summary>
    ///     Handles JSON action commands from OSC 1010 sequences.
    ///     Format: ESC ] 1010 ; {"action":"action_name"} BEL/ST
    /// </summary>
    private void HandleJsonActionCommand(string? payload)
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

            // Dispatch to the appropriate handler
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
        catch (JsonException ex)
        {
            _logger.LogWarning("OSC 1010: Failed to parse JSON payload: {Error}. Payload: {Payload}", ex.Message, payload);
        }
    }
}
