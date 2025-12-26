using caTTY.Core.Types;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Parsing;

/// <summary>
///     Parser for OSC (Operating System Command) sequences.
///     Handles OSC sequence parsing and termination detection.
/// </summary>
public class OscParser : IOscParser
{
    private readonly ILogger _logger;

    /// <summary>
    ///     Creates a new OSC parser.
    /// </summary>
    /// <param name="logger">Logger for diagnostic messages</param>
    public OscParser(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///     Processes a byte in the OSC sequence parsing state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    /// <param name="escapeSequence">The current escape sequence buffer</param>
    /// <param name="message">The parsed OSC message if sequence is complete</param>
    /// <returns>True if the sequence is complete, false if more bytes are needed</returns>
    public bool ProcessOscByte(byte b, List<byte> escapeSequence, out OscMessage? message)
    {
        message = null;

        // Guard against bytes outside the allowed OSC byte range (0x20 - 0x7E)
        // Special allowances for BEL (0x07) and ESC (0x1b) must be allowed, these are valid terminators
        if (b != 0x07 && b != 0x1b && (b < 0x20 || b > 0x7e))
        {
            _logger.LogWarning("OSC: byte out of range 0x{Byte:X2}", b);
            return false; // Continue parsing (caller should handle this)
        }

        escapeSequence.Add(b);

        // OSC terminators: BEL or ST (ESC \)
        if (b == 0x07) // BEL
        {
            message = CreateOscMessage(escapeSequence, "BEL");
            return true; // Sequence complete
        }

        return false; // Continue parsing
    }

    /// <summary>
    ///     Processes a byte in the OSC escape state (checking for ST terminator).
    /// </summary>
    /// <param name="b">The byte to process</param>
    /// <param name="escapeSequence">The current escape sequence buffer</param>
    /// <param name="message">The parsed OSC message if sequence is complete</param>
    /// <returns>True if the sequence is complete, false if more bytes are needed</returns>
    public bool ProcessOscEscapeByte(byte b, List<byte> escapeSequence, out OscMessage? message)
    {
        message = null;

        // We just saw an ESC while inside OSC. If next byte is "\" then it's ST terminator.
        // Otherwise, it was a literal ESC in the OSC payload and we continue in OSC.
        escapeSequence.Add(b);

        if (b == 0x5c) // \
        {
            message = CreateOscMessage(escapeSequence, "ST");
            return true; // Sequence complete
        }

        if (b == 0x07) // BEL
        {
            message = CreateOscMessage(escapeSequence, "BEL");
            return true; // Sequence complete
        }

        return false; // Continue OSC payload
    }

    /// <summary>
    ///     Creates an OSC message from the escape sequence.
    /// </summary>
    private OscMessage CreateOscMessage(List<byte> escapeSequence, string terminator)
    {
        string raw = BytesToString(escapeSequence);
        var message = new OscMessage { Type = "osc", Raw = raw, Terminator = terminator, Implemented = false };

        // TODO: Try to parse as xterm OSC extension (will be implemented in task 6.1)
        _logger.LogDebug("OSC (opaque, {Terminator}): {Raw}", terminator, raw);
        return message;
    }

    /// <summary>
    ///     Converts a list of bytes to a string representation.
    /// </summary>
    private static string BytesToString(IEnumerable<byte> bytes)
    {
        return string.Concat(bytes.Select(b => (char)b));
    }
}