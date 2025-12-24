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
                
            // Device query sequences
            case "csi.deviceAttributesPrimary":
                // Primary DA query: respond with device attributes
                var primaryResponse = DeviceResponses.GenerateDeviceAttributesPrimaryResponse();
                _terminal.EmitResponse(primaryResponse);
                break;
                
            case "csi.deviceAttributesSecondary":
                // Secondary DA query: respond with terminal version
                var secondaryResponse = DeviceResponses.GenerateDeviceAttributesSecondaryResponse();
                _terminal.EmitResponse(secondaryResponse);
                break;
                
            case "csi.cursorPositionReport":
                // CPR query: respond with current cursor position
                var cprResponse = DeviceResponses.GenerateCursorPositionReport(_terminal.Cursor.Col, _terminal.Cursor.Row);
                _terminal.EmitResponse(cprResponse);
                break;
                
            case "csi.deviceStatusReport":
                // DSR ready query: respond with CSI 0 n
                var dsrResponse = DeviceResponses.GenerateDeviceStatusReportResponse();
                _terminal.EmitResponse(dsrResponse);
                break;
                
            case "csi.terminalSizeQuery":
                // Terminal size query: respond with dimensions
                var sizeResponse = DeviceResponses.GenerateTerminalSizeResponse(_terminal.Height, _terminal.Width);
                _terminal.EmitResponse(sizeResponse);
                break;
                
            case "csi.characterSetQuery":
                // Character set query: respond with current character set
                var charsetResponse = DeviceResponses.GenerateCharacterSetQueryResponse();
                _terminal.EmitResponse(charsetResponse);
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
        // DECRQSS: DCS $ q <request> ST
        // The parser puts intermediates (like $) in the parameters list (or attached to the last parameter).
        // We check if the last parameter ends with '$' and the command is 'q'.
        var lastParam = message.Parameters.Length > 0 ? message.Parameters[^1] : "";
        
        if (message.Command == "q" && lastParam.EndsWith("$"))
        {
            HandleDecrqss(message);
            return;
        }
        
        // Log unhandled DCS sequences for debugging
        _logger.LogDebug("DCS sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    /// <summary>
    /// Handles DECRQSS (Request Status String) DCS sequences.
    /// Responds with current terminal state for supported requests.
    /// </summary>
    /// <param name="message">The DCS message containing the request</param>
    private void HandleDecrqss(DcsMessage message)
    {
        // Extract payload from the raw DCS sequence
        // The raw string contains the full sequence: DCS [params] [intermediates] Final [payload] ST
        // We need to find the 'q' that terminates the header and extract the payload after it
        
        var payload = ExtractDecrqssPayload(message.Raw);
        if (payload == null)
        {
            _logger.LogWarning("Failed to extract DECRQSS payload from: {Raw}", message.Raw);
            return;
        }
        
        // DECRQSS response: DCS <status> $ r <response> ST
        // status: 1 = valid, 0 = invalid (following xterm convention)
        
        string response;
        bool valid;
        
        switch (payload)
        {
            case "\"q": // DECSCA - Select Character Protection Attribute
                // Not implemented yet
                valid = false;
                response = payload;
                break;
                
            case "\"p": // DECSCL - Set Conformance Level
                // Not implemented yet
                valid = false;
                response = payload;
                break;
                
            case "m": // SGR - Select Graphic Rendition
                // Request current SGR state
                response = GenerateSgrStateResponse();
                valid = true;
                break;
                
            case "r": // DECSTBM - Set Top and Bottom Margins
                // Request current scroll region
                var top = _terminal.State.ScrollTop + 1; // Convert to 1-indexed
                var bottom = _terminal.State.ScrollBottom + 1; // Convert to 1-indexed
                response = $"{top};{bottom}r";
                valid = true;
                break;
                
            default:
                // Unknown request
                valid = false;
                response = payload;
                break;
        }
        
        // Generate DECRQSS response
        var status = valid ? "1" : "0";
        var decrqssResponse = $"\x1bP{status}$r{response}\x1b\\";
        
        _logger.LogDebug("DECRQSS request '{Payload}' -> response: {Response}", payload, decrqssResponse);
        _terminal.EmitResponse(decrqssResponse);
    }

    /// <summary>
    /// Extracts the payload from a DECRQSS DCS sequence.
    /// </summary>
    /// <param name="raw">The raw DCS sequence</param>
    /// <returns>The extracted payload, or null if extraction failed</returns>
    private string? ExtractDecrqssPayload(string raw)
    {
        var payloadStart = -1;
        
        // Skip DCS initiator (ESC P or 0x90)
        var i = 0;
        if (raw.Length >= 2 && raw[0] == '\x1b' && raw[1] == 'P')
        {
            i = 2;
        }
        else if (raw.Length >= 1 && raw[0] == '\x90')
        {
            i = 1;
        }
        else
        {
            return null; // Invalid DCS sequence
        }
        
        // Scan for the Final Byte (0x40-0x7E) which is the command ('q')
        for (; i < raw.Length; i++)
        {
            var code = (int)raw[i];
            if (code >= 0x40 && code <= 0x7e)
            {
                // Found the command byte. Payload starts after this.
                payloadStart = i + 1;
                break;
            }
        }
        
        if (payloadStart == -1)
        {
            return null; // Should not happen if parser is correct
        }
        
        // Payload ends before the terminator
        // Terminator is ST (ESC \ or 0x9C) or BEL (0x07)
        var payloadEnd = raw.Length;
        if (raw.EndsWith("\x1b\\"))
        {
            payloadEnd -= 2;
        }
        else if (raw.EndsWith("\x9c"))
        {
            payloadEnd -= 1;
        }
        else if (raw.EndsWith("\x07"))
        {
            payloadEnd -= 1;
        }
        
        if (payloadStart >= payloadEnd)
        {
            return string.Empty; // Empty payload
        }
        
        return raw.Substring(payloadStart, payloadEnd - payloadStart);
    }

    /// <summary>
    /// Generates an SGR state response for DECRQSS.
    /// </summary>
    /// <returns>The SGR sequence representing current state</returns>
    private string GenerateSgrStateResponse()
    {
        var sgr = _terminal.State.CurrentSgrState;
        var parts = new List<string> { "0" }; // Always start with reset
        
        if (sgr.Bold) parts.Add("1");
        if (sgr.Faint) parts.Add("2");
        if (sgr.Italic) parts.Add("3");
        if (sgr.Underline) parts.Add("4");
        if (sgr.Blink) parts.Add("5"); // Slow blink
        if (sgr.Inverse) parts.Add("7");
        if (sgr.Hidden) parts.Add("8");
        if (sgr.Strikethrough) parts.Add("9");
        
        // Add foreground color if set
        if (sgr.ForegroundColor.HasValue)
        {
            var fg = sgr.ForegroundColor.Value;
            switch (fg.Type)
            {
                case ColorType.Named:
                    var fgCode = fg.NamedColor switch
                    {
                        NamedColor.Black => 30,
                        NamedColor.Red => 31,
                        NamedColor.Green => 32,
                        NamedColor.Yellow => 33,
                        NamedColor.Blue => 34,
                        NamedColor.Magenta => 35,
                        NamedColor.Cyan => 36,
                        NamedColor.White => 37,
                        NamedColor.BrightBlack => 90,
                        NamedColor.BrightRed => 91,
                        NamedColor.BrightGreen => 92,
                        NamedColor.BrightYellow => 93,
                        NamedColor.BrightBlue => 94,
                        NamedColor.BrightMagenta => 95,
                        NamedColor.BrightCyan => 96,
                        NamedColor.BrightWhite => 97,
                        _ => 39 // Default
                    };
                    parts.Add(fgCode.ToString());
                    break;
                    
                case ColorType.Indexed:
                    parts.Add($"38;5;{fg.Index}");
                    break;
                    
                case ColorType.Rgb:
                    parts.Add($"38;2;{fg.Red};{fg.Green};{fg.Blue}");
                    break;
            }
        }
        
        // Add background color if set
        if (sgr.BackgroundColor.HasValue)
        {
            var bg = sgr.BackgroundColor.Value;
            switch (bg.Type)
            {
                case ColorType.Named:
                    var bgCode = bg.NamedColor switch
                    {
                        NamedColor.Black => 40,
                        NamedColor.Red => 41,
                        NamedColor.Green => 42,
                        NamedColor.Yellow => 43,
                        NamedColor.Blue => 44,
                        NamedColor.Magenta => 45,
                        NamedColor.Cyan => 46,
                        NamedColor.White => 47,
                        NamedColor.BrightBlack => 100,
                        NamedColor.BrightRed => 101,
                        NamedColor.BrightGreen => 102,
                        NamedColor.BrightYellow => 103,
                        NamedColor.BrightBlue => 104,
                        NamedColor.BrightMagenta => 105,
                        NamedColor.BrightCyan => 106,
                        NamedColor.BrightWhite => 107,
                        _ => 49 // Default
                    };
                    parts.Add(bgCode.ToString());
                    break;
                    
                case ColorType.Indexed:
                    parts.Add($"48;5;{bg.Index}");
                    break;
                    
                case ColorType.Rgb:
                    parts.Add($"48;2;{bg.Red};{bg.Green};{bg.Blue}");
                    break;
            }
        }
        
        return string.Join(";", parts) + "m";
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