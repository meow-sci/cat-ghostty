using System.Text;
using caTTY.Core.Types;
using caTTY.Core.Tracing;
using caTTY.Core.Rpc;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Parsing;

/// <summary>
///     Escape sequence parser state machine for terminal emulation.
///     Coordinates between specialized parsers for different sequence types.
///     Based on the TypeScript Parser implementation with identical state transitions and sequence detection.
/// </summary>
public class Parser
{
    private readonly ICsiParser _csiParser;
    private readonly IEscParser _escParser;
    private readonly IDcsParser _dcsParser;
    private readonly IOscParser _oscParser;
    private readonly ISgrParser _sgrParser;
    private readonly StringBuilder _csiSequence = new();
    private readonly StringBuilder _dcsParamBuffer = new();
    private readonly bool _emitNormalBytesDuringEscapeSequence;
    private readonly List<byte> _escapeSequence = new();
    private readonly IParserHandlers _handlers;
    private readonly ILogger _logger;
    private readonly bool _processC0ControlsDuringEscapeSequence;
    private readonly ICursorPositionProvider? _cursorPositionProvider;

    // RPC components (optional)
    private readonly IRpcSequenceDetector? _rpcSequenceDetector;
    private readonly IRpcSequenceParser? _rpcSequenceParser;
    private readonly IRpcHandler? _rpcHandler;

    // UTF-8 decoding
    private readonly IUtf8Decoder _utf8Decoder;

    // Control string state (DCS/SOS/PM/APC)
    private ControlStringKind? _controlStringKind;
    private string? _dcsCommand;
    private string[] _dcsParameters = Array.Empty<string>();

    private ParserState _state = ParserState.Normal;

    /// <summary>
    ///     Creates a new parser with the specified options.
    /// </summary>
    /// <param name="options">Parser configuration options</param>
    public Parser(ParserOptions options)
    {
        _handlers = options.Handlers ?? throw new ArgumentNullException(nameof(options.Handlers));
        _logger = options.Logger ?? throw new ArgumentNullException(nameof(options.Logger));
        _emitNormalBytesDuringEscapeSequence = options.EmitNormalBytesDuringEscapeSequence;
        _processC0ControlsDuringEscapeSequence = options.ProcessC0ControlsDuringEscapeSequence;
        _cursorPositionProvider = options.CursorPositionProvider;
        _utf8Decoder = options.Utf8Decoder ?? new Utf8Decoder();
        _csiParser = options.CsiParser ?? new CsiParser(options.CursorPositionProvider);
        _escParser = options.EscParser ?? new EscParser(_logger, options.CursorPositionProvider);
        _dcsParser = options.DcsParser ?? new DcsParser(_logger, options.CursorPositionProvider);
        _oscParser = options.OscParser ?? new OscParser(_logger, options.CursorPositionProvider);
        _sgrParser = options.SgrParser ?? new SgrParser(_logger, options.CursorPositionProvider);

        // RPC components are optional
        _rpcSequenceDetector = options.RpcSequenceDetector;
        _rpcSequenceParser = options.RpcSequenceParser;
        _rpcHandler = options.RpcHandler;
        // Console.WriteLine($"Parser: RPC enabled={IsRpcHandlingEnabled()}");
    }

    /// <summary>
    ///     Processes a span of bytes through the parser state machine.
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
    ///     Flushes any incomplete UTF-8 sequences at the end of input.
    ///     This should be called when no more input is expected to ensure
    ///     incomplete sequences are handled gracefully.
    /// </summary>
    public void FlushIncompleteSequences()
    {
        if (_utf8Decoder.FlushIncompleteSequence(out ReadOnlySpan<byte> invalidBytes))
        {
            // Send each invalid byte as a separate character
            foreach (byte b in invalidBytes)
            {
                _handlers.HandleNormalByte(b);
            }
        }
    }

    /// <summary>
    ///     Processes a single byte through the parser state machine.
    /// </summary>
    /// <param name="data">The byte to process</param>
    public void PushByte(byte data)
    {
        ProcessByte(data);
    }

    /// <summary>
    ///     Main state machine processor for handling bytes based on current parser state.
    /// </summary>
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
    ///     Handles bytes in normal text processing state.
    /// </summary>
    private void HandleNormalState(byte b)
    {
        // C0 controls (including BEL/BS/TAB/LF/CR) execute immediately in normal mode
        if (b < 0x20 && b != 0x1b && HandleC0ExceptEscape(b))
        {
            return;
        }

        // Handle other C0 control characters that aren't explicitly handled
        if (b < 0x20 && b != 0x1b)
        {
            TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
            // These control characters are typically ignored or have no specific handler
            return;
        }

        if (b == 0x1b) // ESC
        {
            StartEscapeSequence(b);
            return;
        }

        // DEL (0x7F) should be ignored in terminal emulation
        if (b == 0x7F)
        {
            TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
            return;
        }

        HandleNormalByte(b);
    }

    /// <summary>
    ///     Handles bytes in escape sequence state.
    /// </summary>
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
    ///     Handles bytes in CSI sequence state.
    /// </summary>
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
    ///     Handles bytes in OSC sequence state.
    /// </summary>
    private void HandleOscState(byte b)
    {
        // Allow UTF-8 bytes in OSC sequences
        // Only reject control characters (0x00-0x1F) except for BEL (0x07) and ESC (0x1b) which are terminators
        // Keep 1:1 behavior with the pre-refactor Parser: warn + optionally emit as normal byte, without altering OSC state.
        if (b < 0x20 && b != 0x07 && b != 0x1b)
        {
            _logger.LogWarning("OSC: control character byte 0x{Byte:X2}", b);
            MaybeEmitNormalByteDuringEscapeSequence(b);
            return;
        }

        if (_oscParser.ProcessOscByte(b, _escapeSequence, out OscMessage? message))
        {
            if (message != null)
            {
                // If we have a parsed xterm message, handle it specifically
                if (message.XtermMessage != null)
                {
                    _handlers.HandleXtermOsc(message.XtermMessage);
                }
                else
                {
                    _handlers.HandleOsc(message);
                }
            }
            ResetEscapeState();
            return;
        }

        if (b == 0x1b) // ESC
        {
            _state = ParserState.OscEscape;
        }
    }

    /// <summary>
    ///     Handles bytes in OSC escape state (checking for ST terminator).
    /// </summary>
    private void HandleOscEscapeState(byte b)
    {
        if (_oscParser.ProcessOscEscapeByte(b, _escapeSequence, out OscMessage? message))
        {
            if (message != null)
            {
                // If we have a parsed xterm message, handle it specifically
                if (message.XtermMessage != null)
                {
                    _handlers.HandleXtermOsc(message.XtermMessage);
                }
                else
                {
                    _handlers.HandleOsc(message);
                }
            }
            ResetEscapeState();
            return;
        }

        // Continue OSC payload
        _state = ParserState.Osc;
    }

    /// <summary>
    ///     Handles bytes in DCS sequence state.
    /// </summary>
    private void HandleDcsState(byte b)
    {
        if (_dcsParser.ProcessDcsByte(b, _escapeSequence, ref _dcsCommand, _dcsParamBuffer, ref _dcsParameters, out DcsMessage? message))
        {
            // Sequence aborted (CAN/SUB)
            if (message == null)
            {
                ResetEscapeState();
                return;
            }
        }

        if (b == 0x1b) // ESC
        {
            _state = ParserState.DcsEscape;
        }
    }

    /// <summary>
    ///     Handles bytes in DCS escape state (checking for ST terminator).
    /// </summary>
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
    ///     Handles bytes in control string state (SOS/PM/APC).
    /// </summary>
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
        }
    }

    /// <summary>
    ///     Handles bytes in control string escape state (checking for ST terminator).
    /// </summary>
    private void HandleControlStringEscapeState(byte b)
    {
        _escapeSequence.Add(b);

        if (b == 0x5c) // \
        {
            // ST terminator
            string raw = BytesToString(_escapeSequence);
            string kind = _controlStringKind?.ToString().ToUpperInvariant() ?? "STR";
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
    ///     Handles bytes in CSI sequence, building the sequence until final byte.
    /// </summary>
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
        }
    }

    /// <summary>
    ///     Handles ESC byte, accumulating into escape sequence and determining next state.
    /// </summary>
    private void HandleEscapeByte(byte b)
    {
        // Guard against bytes outside the allowed ESC byte range (0x20 - 0x7E)
        // Keep 1:1 behavior with the pre-refactor Parser: warn + optionally emit as normal byte, without resetting ESC state.
        if (b < 0x20 || b > 0x7e)
        {
            _logger.LogWarning("ESC: byte out of range 0x{Byte:X2}", b);
            MaybeEmitNormalByteDuringEscapeSequence(b);
            return;
        }

        // First byte after ESC decides the submode
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
                _dcsParser.Reset();
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
        }

        // Use the specialized ESC parser for all other ESC sequences
        if (_escParser.ProcessEscByte(b, _escapeSequence, out EscMessage? message))
        {
            if (message != null)
            {
                _handlers.HandleEsc(message);
            }
            ResetEscapeState();
        }
    }

    /// <summary>
    ///     Handles normal printable bytes and UTF-8 sequences.
    /// </summary>
    private void HandleNormalByte(byte b)
    {
        // Use the dedicated UTF-8 decoder
        if (_utf8Decoder.ProcessByte(b, out int codePoint))
        {
            // Trace the decoded UTF-8 character
            if (TerminalTracer.Enabled) {
              string decodedText = char.ConvertFromUtf32(codePoint);
              int? row = _cursorPositionProvider?.Row;
              int? col = _cursorPositionProvider?.Column;
              TraceHelper.TraceUtf8Text(decodedText, TraceDirection.Output, row, col);
            }
            _handlers.HandleNormalByte(codePoint);
        }
    }

    /// <summary>
    ///     Resets the parser to normal state and clears all buffers.
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
        _dcsParser.Reset();
    }

    /// <summary>
    ///     Handles C0 control characters except ESC.
    /// </summary>
    private bool HandleC0ExceptEscape(byte b)
    {
        switch (b)
        {
            case 0x07: // Bell
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleBell();
                return true;
            case 0x08: // Backspace
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleBackspace();
                return true;
            case 0x09: // Tab
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleTab();
                return true;
            case 0x0e: // Shift Out (SO)
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleShiftOut();
                return true;
            case 0x0f: // Shift In (SI)
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleShiftIn();
                return true;
            case 0x0a: // Line Feed
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleLineFeed();
                return true;
            case 0x0c: // Form Feed
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleFormFeed();
                return true;
            case 0x0d: // Carriage Return
                TraceHelper.TraceControlChar(b, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);
                _handlers.HandleCarriageReturn();
                return true;
        }

        return false;
    }

    /// <summary>
    ///     Starts a new escape sequence and resets state.
    /// </summary>
    private void StartEscapeSequence(byte b)
    {
        _state = ParserState.Escape;
        _escapeSequence.Clear();
        _escapeSequence.Add(b);
    }

    /// <summary>
    ///     Finishes a CSI sequence and sends it to the handler.
    /// </summary>
    private void FinishCsiSequence()
    {
        string raw = BytesToString(_escapeSequence);
        byte finalByte = _escapeSequence[^1];

        // Check for RPC sequences first (ESC [ > format) if RPC handling is enabled
        if (IsRpcHandlingEnabled() && TryHandleRpcSequence())
        {
            ResetEscapeState();
            return;
        }

        // CSI SGR: parse using the dedicated SGR parser
        if (finalByte == 0x6d) // 'm'
        {
            SgrSequence sgrSequence = _sgrParser.ParseSgrSequence(_escapeSequence.ToArray(), raw);
            _handlers.HandleSgr(sgrSequence);
        }
        else
        {
            // Parse CSI sequence using the dedicated CSI parser
            CsiMessage message = _csiParser.ParseCsiSequence(_escapeSequence.ToArray(), raw);
            _handlers.HandleCsi(message);
        }

        ResetEscapeState();
    }

    /// <summary>
    ///     Finishes a DCS sequence and sends it to the handler.
    /// </summary>
    private void FinishDcsSequence(string terminator)
    {
        DcsMessage message = _dcsParser.CreateDcsMessage(_escapeSequence, terminator, _dcsCommand, _dcsParameters);

        // Trace the DCS sequence with command, parameters, and data payload
        string? parametersString = _dcsParameters.Length > 0 ? string.Join(";", _dcsParameters) : null;
        TraceHelper.TraceDcsSequence(_dcsCommand ?? string.Empty, parametersString, null, TraceDirection.Output, _cursorPositionProvider?.Row, _cursorPositionProvider?.Column);

        _handlers.HandleDcs(message);
        ResetEscapeState();
    }

    /// <summary>
    ///     Optionally emits a normal byte during escape sequence processing.
    /// </summary>
    private void MaybeEmitNormalByteDuringEscapeSequence(byte b)
    {
        if (_emitNormalBytesDuringEscapeSequence)
        {
            HandleNormalByte(b);
        }
    }

    /// <summary>
    ///     Converts a list of bytes to a string representation.
    /// </summary>
    private static string BytesToString(IEnumerable<byte> bytes)
    {
        return string.Concat(bytes.Select(b => (char)b));
    }

    /// <summary>
    ///     Checks if RPC handling is enabled and all required components are available.
    /// </summary>
    /// <returns>True if RPC handling is enabled</returns>
    private bool IsRpcHandlingEnabled()
    {
        return _rpcSequenceDetector != null &&
               _rpcSequenceParser != null &&
               _rpcHandler != null &&
               _rpcHandler.IsEnabled;
    }

    /// <summary>
    ///     Attempts to handle the current escape sequence as an RPC sequence.
    ///     This method maintains clean separation between core terminal emulation and RPC functionality.
    /// </summary>
    /// <returns>True if the sequence was handled as an RPC sequence</returns>
    private bool TryHandleRpcSequence()
    {
        if (!IsRpcHandlingEnabled())
        {
            return false;
        }

        ReadOnlySpan<byte> sequenceSpan = _escapeSequence.ToArray().AsSpan();

        // First check if this is an RPC sequence
        if (!_rpcSequenceDetector!.IsRpcSequence(sequenceSpan))
        {
            return false;
        }

        // Get the sequence type for more detailed validation
        RpcSequenceType sequenceType = _rpcSequenceDetector.GetSequenceType(sequenceSpan);


        // Handle malformed sequences
        if (sequenceType != RpcSequenceType.Valid)
        {
            _rpcHandler!.HandleMalformedRpcSequence(sequenceSpan, sequenceType);
            return true; // Sequence was handled (even if malformed)
        }

        // Try to parse the valid RPC sequence
        if (_rpcSequenceParser!.TryParseRpcSequence(sequenceSpan, out RpcMessage? message) && message != null)
        {
            // Console.WriteLine($"!!! GOT VALID RPC SEQUENCE sequenceType={sequenceType} message={message}");

            _rpcHandler!.HandleRpcMessage(message);
            return true;
        }

        // Console.WriteLine($"!!! GOT INVALID RPC SEQUENCE");


        // Parsing failed for a supposedly valid sequence
        _rpcHandler!.HandleMalformedRpcSequence(sequenceSpan, RpcSequenceType.Malformed);
        return true;
    }
}
