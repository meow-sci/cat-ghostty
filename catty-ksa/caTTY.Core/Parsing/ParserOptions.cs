using Microsoft.Extensions.Logging;

namespace caTTY.Core.Parsing;

/// <summary>
/// Configuration options for the escape sequence parser.
/// Based on the TypeScript ParserOptions interface.
/// </summary>
public class ParserOptions
{
    /// <summary>
    /// Logger instance for parser debugging and warnings.
    /// </summary>
    public ILogger Logger { get; set; } = null!;

    /// <summary>
    /// Whether to emit normal bytes during escape sequences (undefined behavior).
    /// Default is false.
    /// </summary>
    public bool EmitNormalBytesDuringEscapeSequence { get; set; } = false;

    /// <summary>
    /// Whether to process C0 controls during escape sequences (common terminal behavior).
    /// Default is true.
    /// </summary>
    public bool ProcessC0ControlsDuringEscapeSequence { get; set; } = true;

    /// <summary>
    /// Handlers for processing parsed sequences and control characters.
    /// </summary>
    public IParserHandlers Handlers { get; set; } = null!;
}