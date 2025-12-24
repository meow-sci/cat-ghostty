using caTTY.Core.Parsing;
using caTTY.Core.Types;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal;

/// <summary>
/// Parser handlers implementation for the terminal emulator.
/// Bridges parsed sequences to terminal operations.
/// </summary>
internal class TerminalParserHandlers : IParserHandlers
{
    private readonly TerminalEmulator _terminal;
    private readonly ILogger _logger;

    public TerminalParserHandlers(TerminalEmulator terminal, ILogger logger)
    {
        _terminal = terminal;
        _logger = logger;
    }

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
        // TODO: Implement character set switching (task 6.9)
        _logger.LogDebug("Shift In (SI) - character set switching not yet implemented");
    }

    public void HandleShiftOut()
    {
        // TODO: Implement character set switching (task 6.9)
        _logger.LogDebug("Shift Out (SO) - character set switching not yet implemented");
    }

    public void HandleNormalByte(int codePoint)
    {
        // Convert Unicode code point to character and write to terminal
        if (codePoint <= 0xFFFF)
        {
            // Basic Multilingual Plane - single char
            char character = (char)codePoint;
            _terminal.WriteCharacterAtCursor(character);
        }
        else
        {
            // Supplementary planes - surrogate pair
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
                
            case "csi.eraseInDisplay":
                _terminal.ClearDisplay(message.Mode ?? 0);
                break;
                
            case "csi.eraseInLine":
                _terminal.ClearLine(message.Mode ?? 0);
                break;
                
            case "csi.selectiveEraseInDisplay":
                // TODO: Implement selective erase (task 2.14)
                _logger.LogDebug("Selective erase in display not yet implemented: {Raw}", message.Raw);
                break;
                
            case "csi.selectiveEraseInLine":
                // TODO: Implement selective erase (task 2.14)
                _logger.LogDebug("Selective erase in line not yet implemented: {Raw}", message.Raw);
                break;
                
            default:
                // TODO: Implement other CSI sequence handling (task 2.8, etc.)
                _logger.LogDebug("CSI sequence: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
    }

    public void HandleOsc(OscMessage message)
    {
        // TODO: Implement OSC sequence handling (task 6.1, 6.2, 6.3)
        _logger.LogDebug("OSC sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleDcs(DcsMessage message)
    {
        // TODO: Implement DCS sequence handling (task 2.15)
        _logger.LogDebug("DCS sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleSgr(SgrSequence sequence)
    {
        // TODO: Implement SGR sequence handling (task 3.1-3.6)
        _logger.LogDebug("SGR sequence: {Type} - {Raw}", sequence.Type, sequence.Raw);
    }

    public void HandleXtermOsc(XtermOscMessage message)
    {
        // TODO: Implement xterm OSC handling (task 6.2, 6.3, 6.5)
        _logger.LogDebug("Xterm OSC: {Type} - {Raw}", message.Type, message.Raw);
    }
}