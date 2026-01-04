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

    public TerminalParserHandlers(TerminalEmulator terminal, ILogger logger, IRpcHandler? rpcHandler = null)
    {
        _terminal = terminal;
        _logger = logger;
        _rpcHandler = rpcHandler;
        _sgrHandler = new SgrHandler(terminal, logger);
        _dcsHandler = new DcsHandler(terminal, logger);
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
        switch (message.Type)
        {
            case "csi.cursorUp":
                _terminal.MoveCursorUp(message.Count ?? 1);
                break;

            case "csi.cursorDown":
                _terminal.MoveCursorDown(message.Count ?? 1);
                break;

            case "csi.cursorForward":
                _terminal.MoveCursorForward(message.Count ?? 1);
                break;

            case "csi.cursorBackward":
                _terminal.MoveCursorBackward(message.Count ?? 1);
                break;

            case "csi.cursorPosition":
                _terminal.SetCursorPosition(message.Row ?? 1, message.Column ?? 1);
                break;

            case "csi.cursorHorizontalAbsolute":
                _terminal.SetCursorColumn(message.Count ?? 1);
                break;

            case "csi.cursorNextLine":
                _terminal.MoveCursorDown(message.Count ?? 1);
                _terminal.SetCursorColumn(1); // Move to beginning of line
                break;

            case "csi.cursorPrevLine":
                _terminal.MoveCursorUp(message.Count ?? 1);
                _terminal.SetCursorColumn(1); // Move to beginning of line
                break;

            case "csi.verticalPositionAbsolute":
                _terminal.SetCursorPosition(message.Count ?? 1, _terminal.Cursor.Col + 1);
                break;

            case "csi.saveCursorPosition":
                // ANSI cursor save (CSI s) - separate from DEC save (ESC 7)
                _terminal.SaveCursorPositionAnsi();
                break;

            case "csi.restoreCursorPosition":
                // ANSI cursor restore (CSI u) - separate from DEC restore (ESC 8)
                _terminal.RestoreCursorPositionAnsi();
                break;

            case "csi.eraseInDisplay":
                _terminal.ClearDisplay(message.Mode ?? 0);
                break;

            case "csi.eraseInLine":
                _terminal.ClearLine(message.Mode ?? 0);
                break;

            case "csi.cursorForwardTab":
                _terminal.CursorForwardTab(message.Count ?? 1);
                break;

            case "csi.cursorBackwardTab":
                _terminal.CursorBackwardTab(message.Count ?? 1);
                break;

            case "csi.tabClear":
                if (message.Mode == 3)
                {
                    _terminal.ClearAllTabStops();
                }
                else
                {
                    _terminal.ClearTabStopAtCursor();
                }

                break;

            case "csi.scrollUp":
                _terminal.ScrollScreenUp(message.Lines ?? 1);
                break;

            case "csi.scrollDown":
                _terminal.ScrollScreenDown(message.Lines ?? 1);
                break;

            case "csi.setScrollRegion":
                _terminal.SetScrollRegion(message.Top, message.Bottom);
                break;

            case "csi.selectiveEraseInDisplay":
                _terminal.ClearDisplaySelective(message.Mode ?? 0);
                break;

            case "csi.selectiveEraseInLine":
                _terminal.ClearLineSelective(message.Mode ?? 0);
                break;

            case "csi.selectCharacterProtection":
                // DECSCA - Select Character Protection Attribute
                if (message.Protected.HasValue)
                {
                    _terminal.SetCharacterProtection(message.Protected.Value);
                    _logger.LogDebug("Set character protection: {Protected}", message.Protected.Value);
                }

                break;

            case "csi.sgr":
                // Standard SGR sequence (CSI ... m) - delegate to SGR parser
                var sgrSequence = _terminal.AttributeManager.ParseSgrFromCsi(message.Parameters, message.Raw);
                _sgrHandler.HandleSgrSequence(sgrSequence);
                break;

            case "csi.enhancedSgrMode":
                // Enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
                var enhancedSgrSequence = _terminal.AttributeManager.ParseEnhancedSgrFromCsi(message.Parameters, message.Raw);
                _sgrHandler.HandleSgrSequence(enhancedSgrSequence);
                break;

            case "csi.privateSgrMode":
                // Private SGR sequences with ? prefix (e.g., CSI ? 4 m)
                var privateSgrSequence = _terminal.AttributeManager.ParsePrivateSgrFromCsi(message.Parameters, message.Raw);
                _sgrHandler.HandleSgrSequence(privateSgrSequence);
                break;

            case "csi.sgrWithIntermediate":
                // SGR sequences with intermediate characters (e.g., CSI 0 % m)
                var sgrWithIntermediateSequence = _terminal.AttributeManager.ParseSgrWithIntermediateFromCsi(
                    message.Parameters, message.Intermediate ?? "", message.Raw);
                _sgrHandler.HandleSgrSequence(sgrWithIntermediateSequence);
                break;

            // Device query sequences
            case "csi.deviceAttributesPrimary":
                // Primary DA query: respond with device attributes
                string primaryResponse = DeviceResponses.GenerateDeviceAttributesPrimaryResponse();
                _terminal.EmitResponse(primaryResponse);
                break;

            case "csi.deviceAttributesSecondary":
                // Secondary DA query: respond with terminal version
                string secondaryResponse = DeviceResponses.GenerateDeviceAttributesSecondaryResponse();
                _terminal.EmitResponse(secondaryResponse);
                break;

            case "csi.cursorPositionReport":
                // CPR query: respond with current cursor position
                string cprResponse =
                    DeviceResponses.GenerateCursorPositionReport(_terminal.Cursor.Col, _terminal.Cursor.Row);
                _terminal.EmitResponse(cprResponse);
                break;

            case "csi.deviceStatusReport":
                // DSR ready query: respond with CSI 0 n
                string dsrResponse = DeviceResponses.GenerateDeviceStatusReportResponse();
                _terminal.EmitResponse(dsrResponse);
                break;

            case "csi.terminalSizeQuery":
                // Terminal size query: respond with dimensions
                string sizeResponse = DeviceResponses.GenerateTerminalSizeResponse(_terminal.Height, _terminal.Width);
                _terminal.EmitResponse(sizeResponse);
                break;

            case "csi.characterSetQuery":
                // Character set query: respond with current character set
                string charsetResponse = _terminal.GenerateCharacterSetQueryResponse();
                _terminal.EmitResponse(charsetResponse);
                break;

            case "csi.decModeSet":
                // DEC private mode set (CSI ? Pm h)
                if (message.DecModes != null)
                {
                    foreach (int mode in message.DecModes)
                    {
                        _terminal.SetDecMode(mode, true);
                    }
                }
                break;

            case "csi.decModeReset":
                // DEC private mode reset (CSI ? Pm l)
                if (message.DecModes != null)
                {
                    foreach (int mode in message.DecModes)
                    {
                        _terminal.SetDecMode(mode, false);
                    }
                }
                break;

            case "csi.insertLines":
                // Insert Lines (CSI L) - insert blank lines at cursor position
                _terminal.InsertLinesInRegion(message.Count ?? 1);
                break;

            case "csi.deleteLines":
                // Delete Lines (CSI M) - delete lines at cursor position
                _terminal.DeleteLinesInRegion(message.Count ?? 1);
                break;

            case "csi.insertChars":
                // Insert Characters (CSI @) - insert blank characters at cursor position
                _terminal.InsertCharactersInLine(message.Count ?? 1);
                break;

            case "csi.deleteChars":
                // Delete Characters (CSI P) - delete characters at cursor position
                _terminal.DeleteCharactersInLine(message.Count ?? 1);
                break;

            case "csi.eraseCharacter":
                // Erase Character (CSI X) - erase characters at cursor position
                _terminal.EraseCharactersInLine(message.Count ?? 1);
                break;

            case "csi.savePrivateMode":
                // Save private modes (CSI ? Pm s)
                if (message.DecModes != null)
                {
                    _terminal.SavePrivateModes(message.DecModes);
                }
                break;

            case "csi.restorePrivateMode":
                // Restore private modes (CSI ? Pm r)
                if (message.DecModes != null)
                {
                    _terminal.RestorePrivateModes(message.DecModes);
                }
                break;

            case "csi.setCursorStyle":
                // Set cursor style (CSI Ps SP q) - DECSCUSR
                if (message.CursorStyle.HasValue)
                {
                    _terminal.SetCursorStyle(message.CursorStyle.Value);
                }
                break;

            case "csi.decSoftReset":
                // DEC soft reset (CSI ! p) - DECSTR
                _terminal.SoftReset();
                _logger.LogDebug("DEC soft reset executed");
                break;

            case "csi.insertMode":
                // Insert/Replace Mode (CSI 4 h/l) - IRM
                if (message.Enable.HasValue)
                {
                    _terminal.SetInsertMode(message.Enable.Value);
                    _logger.LogDebug("Insert mode {Action}: {Enabled}", 
                        message.Enable.Value ? "set" : "reset", message.Enable.Value);
                }
                break;

            case "csi.windowManipulation":
                // Window manipulation commands (CSI Ps t) - handle title stack operations and size queries
                if (message.Operation.HasValue)
                {
                    int[] windowParams = message.WindowParams ?? Array.Empty<int>();
                    _terminal.HandleWindowManipulation(message.Operation.Value, windowParams);
                }
                break;

            default:
                // TODO: Implement other CSI sequence handling (task 2.8, etc.)
                _logger.LogDebug("CSI sequence: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
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

            default:
                // Log unhandled xterm OSC sequences for debugging
                _logger.LogDebug("Xterm OSC: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
    }

}
