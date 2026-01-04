using caTTY.Core.Parsing;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal.ParserHandlers;

/// <summary>
///     CSI (Control Sequence Introducer) message dispatcher.
///     Routes CSI messages to appropriate handlers.
/// </summary>
internal class CsiDispatcher
{
    private readonly TerminalEmulator _terminal;
    private readonly ILogger _logger;
    private readonly SgrHandler _sgrHandler;

    public CsiDispatcher(TerminalEmulator terminal, ILogger logger, SgrHandler sgrHandler)
    {
        _terminal = terminal;
        _logger = logger;
        _sgrHandler = sgrHandler;
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
}
