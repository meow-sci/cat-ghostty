namespace caTTY.Core.Types;

/// <summary>
/// Represents a parsed xterm OSC extension message.
/// Based on the TypeScript XtermOscMessage type.
/// </summary>
public class XtermOscMessage
{
    /// <summary>
    /// The type of xterm OSC message.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The raw sequence string.
    /// </summary>
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// Whether this message type is implemented.
    /// </summary>
    public bool Implemented { get; set; }

    /// <summary>
    /// The OSC command number.
    /// </summary>
    public int Command { get; set; }

    /// <summary>
    /// The payload data for the OSC command.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
}