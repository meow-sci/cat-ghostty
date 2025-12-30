using caTTY.Core.Parsing;
using caTTY.Core.Types;
using caTTY.Core.Tracing;
using Microsoft.Extensions.Logging;

namespace caTTY.Core.Terminal;

/// <summary>
///     Parser handlers implementation for the terminal emulator.
///     Bridges parsed sequences to terminal operations.
/// </summary>
internal class TerminalParserHandlers : IParserHandlers
{
    private readonly ILogger _logger;
    private readonly TerminalEmulator _terminal;

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
                HandleSgrSequence(sgrSequence);
                break;

            case "csi.enhancedSgrMode":
                // Enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
                var enhancedSgrSequence = _terminal.AttributeManager.ParseEnhancedSgrFromCsi(message.Parameters, message.Raw);
                HandleSgrSequence(enhancedSgrSequence);
                break;

            case "csi.privateSgrMode":
                // Private SGR sequences with ? prefix (e.g., CSI ? 4 m)
                var privateSgrSequence = _terminal.AttributeManager.ParsePrivateSgrFromCsi(message.Parameters, message.Raw);
                HandleSgrSequence(privateSgrSequence);
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
            HandleDecrqss(message);
            return;
        }

        // Log unhandled DCS sequences for debugging
        _logger.LogDebug("DCS sequence: {Type} - {Raw}", message.Type, message.Raw);
    }

    public void HandleSgr(SgrSequence sequence)
    {
        HandleSgrSequence(sequence);
    }

    /// <summary>
    ///     Handles SGR sequence processing and applies attributes to the terminal.
    /// </summary>
    /// <param name="sequence">The SGR sequence to process</param>
    private void HandleSgrSequence(SgrSequence sequence)
    {
        // Apply SGR messages to current attributes
        var currentAttributes = _terminal.AttributeManager.CurrentAttributes;
        var newAttributes = _terminal.AttributeManager.ApplyAttributes(currentAttributes, sequence.Messages);
        _terminal.AttributeManager.CurrentAttributes = newAttributes;

        // Sync with terminal state for compatibility
        _terminal.State.CurrentSgrState = newAttributes;

        // Trace SGR sequence with complete attribute change information
        TraceSgrSequence(sequence, currentAttributes, newAttributes);

        _logger.LogDebug("Applied SGR sequence: {Raw} - {MessageCount} messages", sequence.Raw, sequence.Messages.Length);
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

            default:
                // Log unhandled xterm OSC sequences for debugging
                _logger.LogDebug("Xterm OSC: {Type} - {Raw}", message.Type, message.Raw);
                break;
        }
    }

    /// <summary>
    ///     Handles DECRQSS (Request Status String) DCS sequences.
    ///     Responds with current terminal state for supported requests.
    /// </summary>
    /// <param name="message">The DCS message containing the request</param>
    private void HandleDecrqss(DcsMessage message)
    {
        // Extract payload from the raw DCS sequence
        // The raw string contains the full sequence: DCS [params] [intermediates] Final [payload] ST
        // We need to find the 'q' that terminates the header and extract the payload after it

        string? payload = ExtractDecrqssPayload(message.Raw);
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
                int top = _terminal.State.ScrollTop + 1; // Convert to 1-indexed
                int bottom = _terminal.State.ScrollBottom + 1; // Convert to 1-indexed
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
        string status = valid ? "1" : "0";
        string decrqssResponse = $"\x1bP{status}$r{response}\x1b\\";

        _logger.LogDebug("DECRQSS request '{Payload}' -> response: {Response}", payload, decrqssResponse);
        _terminal.EmitResponse(decrqssResponse);
    }

    /// <summary>
    ///     Extracts the payload from a DECRQSS DCS sequence.
    /// </summary>
    /// <param name="raw">The raw DCS sequence</param>
    /// <returns>The extracted payload, or null if extraction failed</returns>
    private string? ExtractDecrqssPayload(string raw)
    {
        int payloadStart = -1;

        // Skip DCS initiator (ESC P or 0x90)
        int i = 0;
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
            int code = raw[i];
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
        int payloadEnd = raw.Length;
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
    ///     Generates an SGR state response for DECRQSS.
    /// </summary>
    /// <returns>The SGR sequence representing current state</returns>
    private string GenerateSgrStateResponse()
    {
        SgrAttributes sgr = _terminal.State.CurrentSgrState;
        var parts = new List<string> { "0" }; // Always start with reset

        if (sgr.Bold)
        {
            parts.Add("1");
        }

        if (sgr.Faint)
        {
            parts.Add("2");
        }

        if (sgr.Italic)
        {
            parts.Add("3");
        }

        if (sgr.Underline)
        {
            parts.Add("4");
        }

        if (sgr.Blink)
        {
            parts.Add("5"); // Slow blink
        }

        if (sgr.Inverse)
        {
            parts.Add("7");
        }

        if (sgr.Hidden)
        {
            parts.Add("8");
        }

        if (sgr.Strikethrough)
        {
            parts.Add("9");
        }

        // Add foreground color if set
        if (sgr.ForegroundColor.HasValue)
        {
            Color fg = sgr.ForegroundColor.Value;
            switch (fg.Type)
            {
                case ColorType.Named:
                    int fgCode = fg.NamedColor switch
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
            Color bg = sgr.BackgroundColor.Value;
            switch (bg.Type)
            {
                case ColorType.Named:
                    int bgCode = bg.NamedColor switch
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

    /// <summary>
    ///     Traces SGR sequence with complete attribute change information.
    /// </summary>
    /// <param name="sequence">The SGR sequence that was processed</param>
    /// <param name="beforeAttributes">Attributes before applying the sequence</param>
    /// <param name="afterAttributes">Attributes after applying the sequence</param>
    private void TraceSgrSequence(SgrSequence sequence, SgrAttributes beforeAttributes, SgrAttributes afterAttributes)
    {
        if (!TerminalTracer.Enabled)
            return;

        // Get cursor position for tracing context
        int? row = _terminal.Cursor?.Row;
        int? col = _terminal.Cursor?.Col;

        // Create a detailed trace message that includes both raw and formatted sequence with attribute changes
        var formattedSequence = $"\\x1b[{ExtractSgrParameters(sequence.Raw)}m";
        var traceMessage = $"{formattedSequence} - ";

        // Add information about what changed
        var changes = new List<string>();

        if (beforeAttributes.Bold != afterAttributes.Bold)
            changes.Add($"bold:{beforeAttributes.Bold}->{afterAttributes.Bold}");
        if (beforeAttributes.Italic != afterAttributes.Italic)
            changes.Add($"italic:{beforeAttributes.Italic}->{afterAttributes.Italic}");
        if (beforeAttributes.Underline != afterAttributes.Underline)
            changes.Add($"underline:{beforeAttributes.Underline}->{afterAttributes.Underline}");
        if (beforeAttributes.Strikethrough != afterAttributes.Strikethrough)
            changes.Add($"strikethrough:{beforeAttributes.Strikethrough}->{afterAttributes.Strikethrough}");
        if (beforeAttributes.Inverse != afterAttributes.Inverse)
            changes.Add($"inverse:{beforeAttributes.Inverse}->{afterAttributes.Inverse}");
        if (beforeAttributes.Hidden != afterAttributes.Hidden)
            changes.Add($"hidden:{beforeAttributes.Hidden}->{afterAttributes.Hidden}");
        if (beforeAttributes.Blink != afterAttributes.Blink)
            changes.Add($"blink:{beforeAttributes.Blink}->{afterAttributes.Blink}");
        if (beforeAttributes.Faint != afterAttributes.Faint)
            changes.Add($"faint:{beforeAttributes.Faint}->{afterAttributes.Faint}");

        // Check color changes
        if (!Equals(beforeAttributes.ForegroundColor, afterAttributes.ForegroundColor))
            changes.Add($"fg:{FormatColor(beforeAttributes.ForegroundColor)}->{FormatColor(afterAttributes.ForegroundColor)}");
        if (!Equals(beforeAttributes.BackgroundColor, afterAttributes.BackgroundColor))
            changes.Add($"bg:{FormatColor(beforeAttributes.BackgroundColor)}->{FormatColor(afterAttributes.BackgroundColor)}");
        if (!Equals(beforeAttributes.UnderlineColor, afterAttributes.UnderlineColor))
            changes.Add($"ul:{FormatColor(beforeAttributes.UnderlineColor)}->{FormatColor(afterAttributes.UnderlineColor)}");

        // Check underline style changes
        if (beforeAttributes.UnderlineStyle != afterAttributes.UnderlineStyle)
            changes.Add($"ul-style:{beforeAttributes.UnderlineStyle}->{afterAttributes.UnderlineStyle}");

        // Check font changes
        if (beforeAttributes.Font != afterAttributes.Font)
            changes.Add($"font:{beforeAttributes.Font}->{afterAttributes.Font}");

        traceMessage += string.Join(", ", changes);
        traceMessage += "";

        // Trace the SGR sequence with Output direction (program output to terminal)
        // Use TerminalTracer.TraceEscape with SGR type to preserve detailed attribute information
        TerminalTracer.TraceEscape(traceMessage, TraceDirection.Output, row, col, "SGR");
    }

    /// <summary>
    ///     Formats a color for tracing display.
    /// </summary>
    /// <param name="color">The color to format</param>
    /// <returns>A string representation of the color</returns>
    private static string FormatColor(Color? color)
    {
        if (!color.HasValue)
            return "null";

        var c = color.Value;
        return c.Type switch
        {
            ColorType.Named => c.NamedColor.ToString(),
            ColorType.Indexed => $"#{c.Index}",
            ColorType.Rgb => $"rgb({c.Red},{c.Green},{c.Blue})",
            _ => "unknown"
        };
    }

    /// <summary>
    ///     Extracts SGR parameters from a raw SGR sequence for tracing.
    /// </summary>
    /// <param name="rawSequence">The raw SGR sequence (e.g., "ESC[1;32m")</param>
    /// <returns>The parameter string (e.g., "1;32")</returns>
    private static string ExtractSgrParameters(string rawSequence)
    {
        // SGR sequences have the format ESC[<parameters>m
        // Extract the parameters between '[' and 'm'
        if (string.IsNullOrEmpty(rawSequence))
            return "0"; // Default to reset

        var startIndex = rawSequence.IndexOf('[');
        var endIndex = rawSequence.LastIndexOf('m');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            var parameters = rawSequence.Substring(startIndex + 1, endIndex - startIndex - 1);
            return string.IsNullOrEmpty(parameters) ? "0" : parameters;
        }

        return "0"; // Default to reset if parsing fails
    }
}
