namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Handles CSI (Control Sequence Introducer) sequences.
/// CSI sequences control cursor movement, text editing, and display clearing.
/// Format: ESC [ [parameters] [intermediate bytes] final_byte
/// </summary>
public class CsiHandler
{
    private readonly TerminalState _state;
    
    public CsiHandler(TerminalState state)
    {
        _state = state;
    }
    
    /// <summary>
    /// Dispatches a CSI sequence to the appropriate handler based on the final byte.
    /// </summary>
    /// <param name="finalByte">The final byte that identifies the command</param>
    /// <param name="parameters">Array of numeric parameters (empty if none)</param>
    /// <param name="intermediates">Intermediate bytes (rare, usually empty)</param>
    public void Handle(char finalByte, int[] parameters, byte[] intermediates)
    {
        // Most CSI sequences have a default parameter of 1 if none provided
        int GetParam(int index, int defaultValue = 1)
        {
            if (index < parameters.Length && parameters[index] > 0)
                return parameters[index];
            return defaultValue;
        }
        
        switch (finalByte)
        {
            // Cursor movement
            case 'A': // CUU - Cursor Up
                _state.MoveCursorUp(GetParam(0));
                break;
                
            case 'B': // CUD - Cursor Down
                _state.MoveCursorDown(GetParam(0));
                break;
                
            case 'C': // CUF - Cursor Forward
                _state.MoveCursorForward(GetParam(0));
                break;
                
            case 'D': // CUB - Cursor Backward
                _state.MoveCursorBackward(GetParam(0));
                break;
                
            case 'E': // CNL - Cursor Next Line
                _state.MoveCursorDown(GetParam(0));
                _state.SetCursorPosition(_state.CursorRow, 0);
                break;
                
            case 'F': // CPL - Cursor Previous Line
                _state.MoveCursorUp(GetParam(0));
                _state.SetCursorPosition(_state.CursorRow, 0);
                break;
                
            case 'G': // CHA - Cursor Horizontal Absolute
                _state.SetCursorPosition(_state.CursorRow, GetParam(0, 1) - 1);
                break;
                
            case 'H': // CUP - Cursor Position
            case 'f': // HVP - Horizontal Vertical Position (same as CUP)
                {
                    int row = GetParam(0, 1) - 1; // Convert from 1-based to 0-based
                    int col = GetParam(1, 1) - 1;
                    _state.SetCursorPosition(row, col);
                }
                break;
                
            // Erase functions
            case 'J': // ED - Erase in Display
                {
                    int mode = GetParam(0, 0);
                    switch (mode)
                    {
                        case 0: // Clear from cursor to end of screen
                            _state.ClearToEndOfScreen();
                            break;
                        case 1: // Clear from beginning of screen to cursor
                            _state.ClearToBeginningOfScreen();
                            break;
                        case 2: // Clear entire screen
                        case 3: // Clear entire screen + scrollback (we don't have scrollback)
                            _state.Clear();
                            break;
                    }
                }
                break;
                
            case 'K': // EL - Erase in Line
                {
                    int mode = GetParam(0, 0);
                    switch (mode)
                    {
                        case 0: // Clear from cursor to end of line
                            _state.ClearToEndOfLine();
                            break;
                        case 1: // Clear from beginning of line to cursor
                            _state.ClearToBeginningOfLine();
                            break;
                        case 2: // Clear entire line
                            _state.ClearLine();
                            break;
                    }
                }
                break;
                
            case 'S': // SU - Scroll Up
                // Not implemented in MVP (no scrollback)
                break;
                
            case 'T': // SD - Scroll Down
                // Not implemented in MVP (no scrollback)
                break;
                
            case 's': // SCP - Save Cursor Position (not ANSI standard)
                // Not implemented in MVP
                break;
                
            case 'u': // RCP - Restore Cursor Position (not ANSI standard)
                // Not implemented in MVP
                break;
                
            case 'm': // SGR - Select Graphic Rendition
                // This should be handled by SgrHandler, but we include a fallback
                // In case it's called directly (though the parser should route to SGR)
                break;
                
            case 'L': // IL - Insert Line
                // Not implemented in MVP
                break;
                
            case 'M': // DL - Delete Line
                // Not implemented in MVP
                break;
                
            case '@': // ICH - Insert Character
                // Not implemented in MVP
                break;
                
            case 'P': // DCH - Delete Character
                // Not implemented in MVP
                break;
                
            case 'X': // ECH - Erase Character
                // Not implemented in MVP
                break;
                
            default:
                // Unknown CSI sequence - ignore
                break;
        }
    }
}
