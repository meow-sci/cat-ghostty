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
        // TODO: Implement ESC sequence handling (task 2.11)
        _logger.LogDebug("ESC sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleCsi(CsiMessage message)
    {
        // TODO: Implement CSI sequence handling (task 2.5, 2.6, 2.8)
        _logger.LogDebug("CSI sequence: {Type} - {Raw}", message.Type, message.Raw);
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