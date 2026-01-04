using caTTY.Core.Parsing;
using caTTY.Core.Rpc;
using caTTY.Core.Terminal.ParserHandlers;
using caTTY.Core.Types;
using caTTY.Core.Tracing;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal;

/// <summary>
///     Parser handlers implementation for the terminal emulator.
///     Bridges parsed sequences to terminal operations and optionally delegates RPC sequences.
/// </summary>
internal class TerminalParserHandlers : IParserHandlers
{
    private readonly ILogger _logger;
    private readonly TerminalEmulator _terminal;
    private readonly IRpcHandler? _rpcHandler;
    private readonly SgrHandler _sgrHandler;
    private readonly DcsHandler _dcsHandler;
    private readonly OscHandler _oscHandler;
    private readonly CsiCursorHandler _csiCursorHandler;
    private readonly CsiEraseHandler _csiEraseHandler;
    private readonly CsiScrollHandler _csiScrollHandler;
    private readonly CsiInsertDeleteHandler _csiInsertDeleteHandler;
    private readonly CsiDecModeHandler _csiDecModeHandler;
    private readonly CsiDispatcher _csiDispatcher;

    public TerminalParserHandlers(TerminalEmulator terminal, ILogger logger, IRpcHandler? rpcHandler = null)
    {
        _terminal = terminal;
        _logger = logger;
        _rpcHandler = rpcHandler;
        _sgrHandler = new SgrHandler(terminal, logger);
        _dcsHandler = new DcsHandler(terminal, logger);
        _oscHandler = new OscHandler(terminal, logger);
        _csiCursorHandler = new CsiCursorHandler(terminal, logger);
        _csiEraseHandler = new CsiEraseHandler(terminal);
        _csiScrollHandler = new CsiScrollHandler(terminal);
        _csiInsertDeleteHandler = new CsiInsertDeleteHandler(terminal, logger);
        _csiDecModeHandler = new CsiDecModeHandler(terminal);
        _csiDispatcher = new CsiDispatcher(terminal, logger, _sgrHandler, _csiCursorHandler, _csiEraseHandler, _csiScrollHandler, _csiInsertDeleteHandler, _csiDecModeHandler);
    }

    /// <summary>
    /// Gets whether RPC handling is currently enabled.
    /// </summary>
    public bool IsRpcEnabled => _rpcHandler?.IsEnabled ?? false;

    public void HandleBell()
    {
        _terminal.HandleBell();
    }

    public void HandleBackspace()
    {
        _terminal.HandleBackspace();
    }

    public void HandleTab()
    {
        _terminal.HandleTab();
    }

    public void HandleLineFeed()
    {
        _terminal.HandleLineFeed();
    }

    public void HandleFormFeed()
    {
        // Form feed is typically treated as line feed in modern terminals
        _terminal.HandleLineFeed();
    }

    public void HandleCarriageReturn()
    {
        _terminal.HandleCarriageReturn();
    }

    public void HandleShiftIn()
    {
        _terminal.HandleShiftIn();
    }

    public void HandleShiftOut()
    {
        _terminal.HandleShiftOut();
    }

    public void HandleNormalByte(int codePoint)
    {
        // Convert Unicode code point to character and apply character set translation
        if (codePoint <= 0xFFFF)
        {
            // Basic Multilingual Plane - single char
            char character = (char)codePoint;
            string translatedChar = _terminal.TranslateCharacter(character);

            // Write each character in the translated string
            foreach (char c in translatedChar)
            {
                _terminal.WriteCharacterAtCursor(c);
            }
        }
        else
        {
            // Supplementary planes - surrogate pair
            // For supplementary planes, we don't apply character set translation
            // as they are already Unicode and not subject to legacy character set mapping
            string characters = char.ConvertFromUtf32(codePoint);
            foreach (char c in characters)
            {
                _terminal.WriteCharacterAtCursor(c);
            }
        }
    }

    public void HandleEsc(EscMessage message)
    {
        switch (message.Type)
        {
            case "esc.saveCursor":
                _terminal.SaveCursorPosition();
                break;

            case "esc.restoreCursor":
                _terminal.RestoreCursorPosition();
                break;

            case "esc.index":
                _terminal.HandleIndex();
                break;

            case "esc.reverseIndex":
                _terminal.HandleReverseIndex();
                break;

            case "esc.nextLine":
                _terminal.HandleCarriageReturn();
                _terminal.HandleLineFeed();
                break;

            case "esc.horizontalTabSet":
                _terminal.SetTabStopAtCursor();
                break;

            case "esc.resetToInitialState":
                _terminal.ResetToInitialState();
                break;

            case "esc.designateCharacterSet":
                if (message.Slot != null && message.Charset != null)
                {
                    _terminal.DesignateCharacterSet(message.Slot, message.Charset);
                }
                else
                {
                    _logger.LogWarning("Character set designation missing slot or charset: {Raw}", message.Raw);
                }

                break;

            default:
                _logger.LogDebug("ESC sequence: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
    }

    public void HandleCsi(CsiMessage message)
    {
        _csiDispatcher.HandleCsi(message);
    }

    public void HandleOsc(OscMessage message)
    {
        _oscHandler.HandleOsc(message);
    }

    public void HandleDcs(DcsMessage message)
    {
        // DECRQSS: DCS $ q <request> ST
        // The parser puts intermediates (like $) in the parameters list (or attached to the last parameter).
        // We check if the last parameter ends with '$' and the command is 'q'.
        string lastParam = message.Parameters.Length > 0 ? message.Parameters[^1] : "";

        if (message.Command == "q" && lastParam.EndsWith("$"))
        {
            _dcsHandler.HandleDecrqss(message);
            return;
        }

        // Log unhandled DCS sequences for debugging
        _logger.LogDebug("DCS sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleSgr(SgrSequence sequence)
    {
        _sgrHandler.HandleSgrSequence(sequence);
    }

    public void HandleXtermOsc(XtermOscMessage message)
    {
        _oscHandler.HandleXtermOsc(message);
    }

}
