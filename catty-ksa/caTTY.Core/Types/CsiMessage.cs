namespace caTTY.Core.Types;

/// <summary>
/// Represents a parsed CSI sequence message.
/// Based on the TypeScript CsiMessage type.
/// </summary>
public class CsiMessage
{
    /// <summary>
    /// The type of CSI sequence.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The raw sequence string.
    /// </summary>
    public string Raw { get; set; } = string.Empty;

    /// <summary>
    /// Whether this sequence type is implemented.
    /// </summary>
    public bool Implemented { get; set; }

    /// <summary>
    /// The final byte of the CSI sequence.
    /// </summary>
    public byte FinalByte { get; set; }

    /// <summary>
    /// Parsed parameters from the CSI sequence.
    /// </summary>
    public int[] Parameters { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Private mode prefix (e.g., '?' for DEC private modes).
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Intermediate characters in the sequence.
    /// </summary>
    public string? Intermediate { get; set; }
}