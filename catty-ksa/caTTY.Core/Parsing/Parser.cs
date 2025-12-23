using System.Text;
using Microsoft.Extensions.Logging;
using caTTY.Core.Types;

namespace caTTY.Core.Parsing;

/// <summary>
/// Escape sequence parser state machine for terminal emulation.
/// Based on the TypeScript Parser implementation with identical state transitions and sequence detection.
/// </summary>
public class Parser
{
    private readonly IParserHandlers _handlers;
    private readonly ILogger _logger;
    private readonly bool _emitNormalBytesDuringEscapeSequence;
    private readonly bool _processC0ControlsDuringEscapeSequence;

    private ParserState _state = ParserState.Normal;
    private readonly List<byte> _escapeSequence = new();
    private readonly StringBuilder _csiSequence = new();

    // Control string state (DCS/SOS/PM/APC)
    private ControlStringKind? _controlStringKind;
    private string? _dcsCommand;
    private readonly StringBuilder _dcsParamBuffer = new();
    private string[] _dcsParameters = Array.Empty<string>();

    // UTF-8 decoding state
    private readonly List<byte> _utf8Buffer = new();
    private int _utf8ExpectedLength = 0;

    /// <summary>
    /// Creates a new parser with the specified options.
    /// </summary>
    /// <param name="options">Parser configuration options</param>
    public Parser(ParserOptions options)
    {
        _handlers = options.Handlers ?? throw new ArgumentNullException(nameof(options.Handlers));
        _logger = options.Logger ?? throw new ArgumentNullException(nameof(options.Logger));
        _emitNormalBytesDuringEscapeSequence = options.EmitNormalBytesDuringEscapeSequence;
        _processC0ControlsDuringEscapeSequence = options.ProcessC0ControlsDuringEscapeSequence;
    }

    /// <summary>
    /// Processes a span of bytes through the parser state machine.
    /// </summary>
    /// <param name="data">The byte data to process</param>
    public void PushBytes(ReadOnlySpan<byte> data)
    {
        foreach (byte b in data)
        {
            PushByte(b);
        }
    }

    /// <summary>
    /// Processes a single byte through the parser state machine.
    /// </summary>
    /// <param name="data">The byte to process</param>
    public void PushByte(byte data)
    {
        ProcessByte(data);
    }

    /// <summary>
    /// Main state machine processor for handling bytes based on current parser state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void ProcessByte(byte b)
    {
        if (b > 255)
        {
            _logger.LogWarning("Ignoring out-of-range byte: {Byte}", b);
            return;
        }

        switch (_state)
        {
            case ParserState.Normal:
                HandleNormalState(b);
                break;

            case ParserState.Escape:
                HandleEscapeState(b);
                break;

            case ParserState.CsiEntry:
                HandleCsiState(b);
                break;

            case ParserState.Osc:
                HandleOscState(b);
                break;

            case ParserState.OscEscape:
                HandleOscEscapeState(b);
                break;

            case ParserState.Dcs:
                HandleDcsState(b);
                break;

            case ParserState.DcsEscape:
                HandleDcsEscapeState(b);
                break;

            case ParserState.ControlString:
                HandleControlStringState(b);
                break;

            case ParserState.ControlStringEscape:
                HandleControlStringEscapeState(b);
                break;
        }
    }

    /// <summary>
    /// Handles bytes in normal text processing state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleNormalState(byte b)
    {
        // C0 controls (including BEL/BS/TAB/LF/CR) execute immediately in normal mode
        if (b < 0x20 && b != 0x1b && HandleC0ExceptEscape(b))
        {
            return;
        }

        if (b == 0x1b) // ESC
        {
            StartEscapeSequence(b);
            return;
        }

        HandleNormalByte(b);
    }

    /// <summary>
    /// Handles bytes in escape sequence state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleEscapeState(byte b)
    {
        // Optional: still execute C0 controls while parsing ESC sequences (common terminal behavior)
        if (b < 0x20 && b != 0x1b && _processC0ControlsDuringEscapeSequence && HandleC0ExceptEscape(b))
        {
            return;
        }

        HandleEscapeByte(b);
    }

    /// <summary>
    /// Handles bytes in CSI sequence state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleCsiState(byte b)
    {
        // Optional: still execute C0 controls while parsing CSI (common terminal behavior)
        if (b < 0x20 && b != 0x1b && _processC0ControlsDuringEscapeSequence && HandleC0ExceptEscape(b))
        {
            return;
        }

        HandleCsiByte(b);
    }

    /// <summary>
    /// Handles bytes in OSC sequence state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleOscState(byte b)
    {
        // Guard against bytes outside the allowed OSC byte range (0x20 - 0x7E)
        // Special allowances for BEL (0x07) and ESC (0x1b) must be allowed, these are valid terminators
        if (b != 0x07 && b != 0x1b && (b < 0x20 || b > 0x7e))
        {
            _logger.LogWarning("OSC: byte out of range 0x{Byte:X2}", b);
            MaybeEmitNormalByteDuringEscapeSequence(b);
            return;
        }

        _escapeSequence.Add(b);

        // OSC terminators: BEL or ST (ESC \)
        if (b == 0x07) // BEL
        {
            FinishOscSequence("BEL");
            return;
        }

        if (b == 0x1b) // ESC
        {
            _state = ParserState.OscEscape;
            return;
        }
    }

    /// <summary>
    /// Handles bytes in OSC escape state (checking for ST terminator).
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleOscEscapeState(byte b)
    {
        // We just saw an ESC while inside OSC. If next byte is "\" then it's ST terminator.
        // Otherwise, it was a literal ESC in the OSC payload and we continue in OSC.
        _escapeSequence.Add(b);

        if (b == 0x5c) // \
        {
            FinishOscSequence("ST");
            return;
        }

        if (b == 0x07) // BEL
        {
            FinishOscSequence("BEL");
            return;
        }

        // Continue OSC payload
        _state = ParserState.Osc;
    }

    /// <summary>
    /// Handles bytes in DCS sequence state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleDcsState(byte b)
    {
        // CAN (0x18) / SUB (0x1a) abort a control string per ECMA-48
        if (b == 0x18 || b == 0x1a)
        {
            ResetEscapeState();
            return;
        }

        _escapeSequence.Add(b);

        if (b == 0x1b) // ESC
        {
            _state = ParserState.DcsEscape;
            return;
        }

        // Parse the DCS "function identifier" (params/intermediates/final) once
        if (_dcsCommand == null)
        {
            // Final byte (0x40-0x7E) ends the identifier
            if (b >= 0x40 && b <= 0x7e)
            {
                _dcsCommand = ((char)b).ToString();
                _dcsParameters = _dcsParamBuffer.Length == 0 
                    ? Array.Empty<string>() 
                    : _dcsParamBuffer.ToString().Split(';');
                return;
            }

            // Parameter bytes are typically 0x30-0x3F (digits and delimiters)
            // We keep it simple and buffer printable parameter bytes
            if (b >= 0x20 && b <= 0x3f)
            {
                _dcsParamBuffer.Append((char)b);
            }
        }
    }

    /// <summary>
    /// Handles bytes in DCS escape state (checking for ST terminator).
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleDcsEscapeState(byte b)
    {
        // We just saw an ESC while inside DCS. If next byte is "\" then it's ST terminator.
        // Otherwise, it was a literal ESC in the payload and we continue in DCS.
        _escapeSequence.Add(b);

        if (b == 0x5c) // \
        {
            FinishDcsSequence("ST");
            return;
        }

        // CAN/SUB should still abort even if we were in the ESC lookahead
        if (b == 0x18 || b == 0x1a)
        {
            ResetEscapeState();
            return;
        }

        _state = ParserState.Dcs;
    }

    /// <summary>
    /// Handles bytes in control string state (SOS/PM/APC).
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleControlStringState(byte b)
    {
        // CAN (0x18) / SUB (0x1a) abort a control string per ECMA-48
        if (b == 0x18 || b == 0x1a)
        {
            ResetEscapeState();
            return;
        }

        _escapeSequence.Add(b);

        if (b == 0x1b) // ESC
        {
            _state = ParserState.ControlStringEscape;
            return;
        }
    }

    /// <summary>
    /// Handles bytes in control string escape state (checking for ST terminator).
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleControlStringEscapeState(byte b)
    {
        _escapeSequence.Add(b);

        if (b == 0x5c) // \
        {
            // ST terminator
            var raw = BytesToString(_escapeSequence);
            var kind = _controlStringKind?.ToString().ToUpperInvariant() ?? "STR";
            _logger.LogDebug("{Kind} (ST): {Raw}", kind, raw);
            ResetEscapeState();
            return;
        }

        if (b == 0x18 || b == 0x1a) // CAN/SUB
        {
            ResetEscapeState();
            return;
        }

        _state = ParserState.ControlString;
    }

    /// <summary>
    /// Handles bytes in CSI sequence, building the sequence until final byte.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleCsiByte(byte b)
    {
        // Guard against bytes outside the allowed CSI byte range (0x20 - 0x7E)
        if (b < 0x20 || b > 0x7e)
        {
            _logger.LogWarning("CSI: byte out of range 0x{Byte:X2}", b);
            MaybeEmitNormalByteDuringEscapeSequence(b);
            return;
        }

        // Always add byte to the escape sequence and csi sequence
        _csiSequence.Append((char)b);
        _escapeSequence.Add(b);

        // CSI final bytes are 0x40-0x7E
        if (b >= 0x40 && b <= 0x7e)
        {
            FinishCsiSequence();
            return;
        }
    }

    /// <summary>
    /// Handles ESC byte, accumulating into escape sequence and determining next state.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleEscapeByte(byte b)
    {
        // Guard against bytes outside the allowed ESC byte range (0x20 - 0x7E)
        if (b < 0x20 || b > 0x7e)
        {
            _logger.LogWarning("ESC: byte out of range 0x{Byte:X2}", b);
            MaybeEmitNormalByteDuringEscapeSequence(b);
            return;
        }

        // First byte after ESC decides the submode for CSI/OSC
        if (_escapeSequence.Count == 1)
        {
            if (b == 0x5b) // [
            {
                _escapeSequence.Add(b);
                _csiSequence.Clear();
                _state = ParserState.CsiEntry;
                return;
            }

            if (b == 0x5d) // ]
            {
                _escapeSequence.Add(b);
                _state = ParserState.Osc;
                return;
            }

            // DCS: ESC P ... ST
            if (b == 0x50) // P
            {
                _escapeSequence.Add(b);
                _dcsCommand = null;
                _dcsParamBuffer.Clear();
                _dcsParameters = Array.Empty<string>();
                _state = ParserState.Dcs;
                return;
            }

            // SOS / PM / APC: ESC X / ESC ^ / ESC _ ... ST
            if (b == 0x58 || b == 0x5e || b == 0x5f) // X, ^, _
            {
                _escapeSequence.Add(b);
                _controlStringKind = b switch
                {
                    0x58 => ControlStringKind.Sos,
                    0x5e => ControlStringKind.Pm,
                    0x5f => ControlStringKind.Apc,
                    _ => null
                };
                _state = ParserState.ControlString;
                return;
            }

            // Handle single-byte ESC sequences
            HandleSingleByteEscSequence(b);
            return;
        }

        // Handle multi-byte ESC sequences (like character set designation)
        HandleMultiByteEscSequence(b);
    }

    /// <summary>
    /// Handles single-byte ESC sequences that complete immediately.
    /// </summary>
    /// <param name="b">The final byte of the sequence</param>
    private void HandleSingleByteEscSequence(byte b)
    {
        _escapeSequence.Add(b);
        var raw = BytesToString(_escapeSequence);

        var message = b switch
        {
            0x37 => new EscMessage { Type = "esc.saveCursor", Raw = raw, Implemented = true }, // ESC 7
            0x38 => new EscMessage { Type = "esc.restoreCursor", Raw = raw, Implemented = true }, // ESC 8
            0x4d => new EscMessage { Type = "esc.reverseIndex", Raw = raw, Implemented = true }, // ESC M
            0x44 => new EscMessage { Type = "esc.index", Raw = raw, Implemented = true }, // ESC D
            0x45 => new EscMessage { Type = "esc.nextLine", Raw = raw, Implemented = true }, // ESC E
            0x48 => new EscMessage { Type = "esc.horizontalTabSet", Raw = raw, Implemented = true }, // ESC H
            0x63 => new EscMessage { Type = "esc.resetToInitialState", Raw = raw, Implemented = true }, // ESC c
            // Character set designation sequences need one more byte
            0x28 or 0x29 or 0x2a or 0x2b => null, // (, ), *, +
            _ => null
        };

        if (message != null)
        {
            _handlers.HandleEsc(message);
            ResetEscapeState();
            return;
        }

        // Character set designation sequences need one more byte
        if (b == 0x28 || b == 0x29 || b == 0x2a || b == 0x2b)
        {
            // Need one more byte for the character set identifier
            return;
        }

        // Unknown ESC sequence - log and reset
        _logger.LogDebug("ESC (opaque): {Raw}", raw);
        ResetEscapeState();
    }

    /// <summary>
    /// Handles multi-byte ESC sequences like character set designation.
    /// </summary>
    /// <param name="b">The current byte being processed</param>
    private void HandleMultiByteEscSequence(byte b)
    {
        // Second byte after ESC for character set designation
        if (_escapeSequence.Count == 2)
        {
            var firstByte = _escapeSequence[1];
            if (firstByte == 0x28 || firstByte == 0x29 || firstByte == 0x2a || firstByte == 0x2b)
            {
                _escapeSequence.Add(b);
                var raw = BytesToString(_escapeSequence);

                // Determine which G slot is being designated
                var slot = firstByte switch
                {
                    0x28 => "G0",
                    0x29 => "G1",
                    0x2a => "G2",
                    0x2b => "G3",
                    _ => "G0"
                };

                var charset = ((char)b).ToString();
                var message = new EscMessage
                {
                    Type = "esc.designateCharacterSet",
                    Raw = raw,
                    Slot = slot,
                    Charset = charset,
                    Implemented = true
                };

                _handlers.HandleEsc(message);
                ResetEscapeState();
                return;
            }
        }

        _escapeSequence.Add(b);

        // Basic ESC sequence: intermediates 0x20-0x2F, final 0x30-0x7E
        if (b >= 0x30 && b <= 0x7e)
        {
            var raw = BytesToString(_escapeSequence);
            _logger.LogDebug("ESC (opaque): {Raw}", raw);
            ResetEscapeState();
            return;
        }
    }

    /// <summary>
    /// Handles normal printable bytes and UTF-8 sequences.
    /// </summary>
    /// <param name="b">The byte to process</param>
    private void HandleNormalByte(byte b)
    {
        // Handle UTF-8 multi-byte sequences
        if (HandleUtf8Byte(b))
        {
            return;
        }

        // Single ASCII byte
        _handlers.HandleNormalByte(b);
    }

    /// <summary>
    /// Handles UTF-8 multi-byte sequence decoding.
    /// </summary>
    /// <param name="b">The byte to process</param>
    /// <returns>True if the byte was part of a UTF-8 sequence</returns>
    private bool HandleUtf8Byte(byte b)
    {
        // If we're not in a UTF-8 sequence and this is ASCII, let it pass through
        if (_utf8ExpectedLength == 0 && b < 0x80)
        {
            return false; // Not a UTF-8 sequence, handle as normal ASCII
        }

        // Start of a new UTF-8 sequence
        if (_utf8ExpectedLength == 0)
        {
            if ((b & 0xE0) == 0xC0)
            {
                // 2-byte sequence: 110xxxxx
                _utf8ExpectedLength = 2;
            }
            else if ((b & 0xF0) == 0xE0)
            {
                // 3-byte sequence: 1110xxxx
                _utf8ExpectedLength = 3;
            }
            else if ((b & 0xF8) == 0xF0)
            {
                // 4-byte sequence: 11110xxx
                _utf8ExpectedLength = 4;
            }
            else
            {
                // Invalid UTF-8 start byte, treat as single byte
                _handlers.HandleNormalByte(b);
                return true;
            }

            _utf8Buffer.Clear();
            _utf8Buffer.Add(b);
            return true;
        }

        // Continuation byte in UTF-8 sequence
        if ((b & 0xC0) != 0x80)
        {
            // Invalid continuation byte, flush buffer and start over
            FlushUtf8Buffer();
            return HandleUtf8Byte(b); // Retry with this byte
        }

        _utf8Buffer.Add(b);

        // Check if we have a complete UTF-8 sequence
        if (_utf8Buffer.Count == _utf8ExpectedLength)
        {
            DecodeUtf8Sequence();
        }

        return true;
    }

    /// <summary>
    /// Decodes a complete UTF-8 sequence and sends the code points to the handler.
    /// </summary>
    private void DecodeUtf8Sequence()
    {
        try
        {
            var utf8Array = _utf8Buffer.ToArray();
            var decoded = Encoding.UTF8.GetString(utf8Array);

            // Send each Unicode character as its code point
            for (int i = 0; i < decoded.Length; i++)
            {
                var codePoint = char.ConvertToUtf32(decoded, i);
                _handlers.HandleNormalByte(codePoint);

                // Skip the next character if this was a surrogate pair
                if (codePoint > 0xFFFF)
                {
                    i++;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UTF-8 decoding failed, treating bytes as individual characters");
            // Decoding failed, treat each byte as a separate character
            FlushUtf8Buffer();
        }

        // Reset UTF-8 state
        _utf8Buffer.Clear();
        _utf8ExpectedLength = 0;
    }

    /// <summary>
    /// Flushes the UTF-8 buffer by sending each byte as a separate character (fallback).
    /// </summary>
    private void FlushUtf8Buffer()
    {
        // Send each byte in the buffer as a separate character (fallback)
        foreach (var b in _utf8Buffer)
        {
            _handlers.HandleNormalByte(b);
        }
        _utf8Buffer.Clear();
        _utf8ExpectedLength = 0;
    }

    /// <summary>
    /// Handles C0 control characters except ESC.
    /// </summary>
    /// <param name="b">The control character byte</param>
    /// <returns>True if the byte was handled as a control character</returns>
    private bool HandleC0ExceptEscape(byte b)
    {
        switch (b)
        {
            case 0x07: // Bell
                _handlers.HandleBell();
                return true;
            case 0x08: // Backspace
                _handlers.HandleBackspace();
                return true;
            case 0x09: // Tab
                _handlers.HandleTab();
                return true;
            case 0x0e: // Shift Out (SO)
                _handlers.HandleShiftOut();
                return true;
            case 0x0f: // Shift In (SI)
                _handlers.HandleShiftIn();
                return true;
            case 0x0a: // Line Feed
                _handlers.HandleLineFeed();
                return true;
            case 0x0c: // Form Feed
                _handlers.HandleFormFeed();
                return true;
            case 0x0d: // Carriage Return
                _handlers.HandleCarriageReturn();
                return true;
        }
        return false;
    }

    /// <summary>
    /// Starts a new escape sequence.
    /// </summary>
    /// <param name="b">The ESC byte (0x1b)</param>
    private void StartEscapeSequence(byte b)
    {
        _state = ParserState.Escape;
        _escapeSequence.Clear();
        _escapeSequence.Add(b);
    }

    /// <summary>
    /// Finishes an OSC sequence and sends it to the handler.
    /// </summary>
    /// <param name="terminator">The terminator type ("BEL" or "ST")</param>
    private void FinishOscSequence(string terminator)
    {
        var raw = BytesToString(_escapeSequence);
        var message = new OscMessage
        {
            Type = "osc",
            Raw = raw,
            Terminator = terminator,
            Implemented = false
        };

        // TODO: Try to parse as xterm OSC extension (will be implemented in task 6.1)
        _logger.LogDebug("OSC (opaque, {Terminator}): {Raw}", terminator, raw);
        _handlers.HandleOsc(message);

        ResetEscapeState();
    }

    /// <summary>
    /// Finishes a CSI sequence and sends it to the handler.
    /// </summary>
    private void FinishCsiSequence()
    {
        var raw = BytesToString(_escapeSequence);
        var finalByte = _escapeSequence[^1];

        // CSI SGR: parse using the standalone SGR parser (will be implemented in task 3.1)
        if (finalByte == 0x6d) // 'm'
        {
            // TODO: Parse SGR sequence (will be implemented in task 3.1)
            var sgrSequence = new SgrSequence
            {
                Type = "sgr",
                Implemented = false,
                Raw = raw,
                Messages = Array.Empty<SgrMessage>()
            };
            _handlers.HandleSgr(sgrSequence);
            ResetEscapeState();
            return;
        }

        // TODO: Parse other CSI sequences (will be implemented in task 2.5)
        var message = new CsiMessage
        {
            Type = "csi.unknown",
            Raw = raw,
            Implemented = false,
            FinalByte = finalByte,
            Parameters = Array.Empty<int>()
        };

        _handlers.HandleCsi(message);
        ResetEscapeState();
    }

    /// <summary>
    /// Finishes a DCS sequence and sends it to the handler.
    /// </summary>
    /// <param name="terminator">The terminator type ("ST")</param>
    private void FinishDcsSequence(string terminator)
    {
        var raw = BytesToString(_escapeSequence);
        var message = new DcsMessage
        {
            Type = "dcs",
            Raw = raw,
            Terminator = terminator,
            Implemented = false,
            Command = _dcsCommand ?? string.Empty,
            Parameters = _dcsParameters
        };

        _logger.LogDebug("DCS ({Terminator}): {Raw}", terminator, raw);
        _handlers.HandleDcs(message);
        ResetEscapeState();
    }

    /// <summary>
    /// Resets the parser to normal state and clears all buffers.
    /// </summary>
    private void ResetEscapeState()
    {
        _state = ParserState.Normal;
        _escapeSequence.Clear();
        _csiSequence.Clear();
        _controlStringKind = null;
        _dcsCommand = null;
        _dcsParamBuffer.Clear();
        _dcsParameters = Array.Empty<string>();
    }

    /// <summary>
    /// Optionally emits a normal byte during escape sequence processing.
    /// </summary>
    /// <param name="b">The byte to potentially emit</param>
    private void MaybeEmitNormalByteDuringEscapeSequence(byte b)
    {
        if (_emitNormalBytesDuringEscapeSequence)
        {
            HandleNormalByte(b);
        }
    }

    /// <summary>
    /// Converts a list of bytes to a string representation.
    /// </summary>
    /// <param name="bytes">The bytes to convert</param>
    /// <returns>The string representation</returns>
    private static string BytesToString(IEnumerable<byte> bytes)
    {
        return string.Concat(bytes.Select(b => (char)b));
    }
}