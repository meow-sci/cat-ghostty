using caTTY.Core.Types;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal.ParserHandlers;

/// <summary>
///     Handles CSI window manipulation sequences (CSI Ps t).
///     Implements title stack operations for vi compatibility and window size queries.
/// </summary>
internal class CsiWindowManipulationHandler
{
    private readonly TerminalEmulator _terminal;
    private readonly ILogger _logger;

    public CsiWindowManipulationHandler(TerminalEmulator terminal, ILogger logger)
    {
        _terminal = terminal;
        _logger = logger;
    }

    /// <summary>
    ///     Handles window manipulation sequences (CSI Ps t).
    ///     Implements title stack operations for vi compatibility and window size queries.
    ///     Gracefully handles unsupported operations (minimize/restore) in game context.
    /// </summary>
    /// <param name="operation">The window manipulation operation code</param>
    /// <param name="parameters">Additional parameters for the operation</param>
    public void HandleWindowManipulation(int operation, int[] parameters)
    {
        switch (operation)
        {
            case 22:
                // Push title/icon name to stack
                if (parameters.Length >= 1)
                {
                    int subOperation = parameters[0];
                    if (subOperation == 1)
                    {
                        // CSI 22;1t - Push icon name to stack
                        _terminal.State.IconNameStack.Add(_terminal.State.WindowProperties.IconName);
                        _logger.LogDebug("Pushed icon name to stack: \"{IconName}\"", _terminal.State.WindowProperties.IconName);
                    }
                    else if (subOperation == 2)
                    {
                        // CSI 22;2t - Push window title to stack
                        _terminal.State.TitleStack.Add(_terminal.State.WindowProperties.Title);
                        _logger.LogDebug("Pushed window title to stack: \"{Title}\"", _terminal.State.WindowProperties.Title);
                    }
                }
                break;

            case 23:
                // Pop title/icon name from stack
                if (parameters.Length >= 1)
                {
                    int subOperation = parameters[0];
                    if (subOperation == 1)
                    {
                        // CSI 23;1t - Pop icon name from stack
                        if (_terminal.State.IconNameStack.Count > 0)
                        {
                            string poppedIconName = _terminal.State.IconNameStack[^1];
                            _terminal.State.IconNameStack.RemoveAt(_terminal.State.IconNameStack.Count - 1);
                            _terminal.SetIconName(poppedIconName);
                            _logger.LogDebug("Popped icon name from stack: \"{IconName}\"", poppedIconName);
                        }
                        else
                        {
                            _logger.LogDebug("Attempted to pop icon name from empty stack");
                        }
                    }
                    else if (subOperation == 2)
                    {
                        // CSI 23;2t - Pop window title from stack
                        if (_terminal.State.TitleStack.Count > 0)
                        {
                            string poppedTitle = _terminal.State.TitleStack[^1];
                            _terminal.State.TitleStack.RemoveAt(_terminal.State.TitleStack.Count - 1);
                            _terminal.SetWindowTitle(poppedTitle);
                            _logger.LogDebug("Popped window title from stack: \"{Title}\"", poppedTitle);
                        }
                        else
                        {
                            _logger.LogDebug("Attempted to pop window title from empty stack");
                        }
                    }
                }
                break;

            case 18:
                // Terminal size query - respond with current dimensions
                string sizeResponse = DeviceResponses.GenerateTerminalSizeResponse(_terminal.Height, _terminal.Width);
                _terminal.EmitResponse(sizeResponse);
                _logger.LogDebug("Terminal size query response: {Response}", sizeResponse);
                break;

            default:
                // Other window manipulation commands - gracefully ignore
                // This includes minimize (2), restore (1), resize (8), etc.
                // These are not applicable in a game context and should be ignored
                _logger.LogDebug("Window manipulation operation {Operation} with params [{Parameters}] - gracefully ignored",
                    operation, string.Join(", ", parameters));
                break;
        }
    }
}
