using System.Text;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// ANSI/DEC VT parser state machine.
/// Implements the state diagram from dec-parser.md for parsing terminal escape sequences.
/// </summary>
public class AnsiParser
{
    private enum State
    {
        Ground,
        Escape,
        EscapeIntermediate,
        CsiEntry,
        CsiParam,
        CsiIntermediate,
        CsiIgnore,
        DcsEntry,
        DcsParam,
        DcsIntermediate,
        DcsPassthrough,
        DcsIgnore,
        OscString,
        SosPmApcString
    }
    
    private State _state;
    private readonly List<int> _params;
    private readonly List<byte> _separators;
    private readonly List<byte> _intermediates;
    private readonly StringBuilder _oscString;
    private readonly TerminalState _terminalState;
    private readonly CsiHandler _csiHandler;
    private Action<string>? _oscHandler;
    private Action<ushort[], byte[]>? _sgrHandler;
    
    public AnsiParser(TerminalState terminalState, CsiHandler csiHandler)
    {
        _state = State.Ground;
        _params = new List<int>();
        _separators = new List<byte>();
        _intermediates = new List<byte>();
        _oscString = new StringBuilder();
        _terminalState = terminalState;
        _csiHandler = csiHandler;
    }
    
    /// <summary>
    /// Sets the OSC handler callback.
    /// </summary>
    public void SetOscHandler(Action<string> handler)
    {
        _oscHandler = handler;
    }
    
    /// <summary>
    /// Sets the SGR handler callback.
    /// </summary>
    public void SetSgrHandler(Action<ushort[], byte[]> handler)
    {
        _sgrHandler = handler;
    }
    
    /// <summary>
    /// Processes a single byte of input.
    /// </summary>
    public void ProcessByte(byte b)
    {
        // C0 controls (0x00-0x1F) and DEL (0x7F) are handled specially in most states
        if (b <= 0x1F && b != 0x1B)
        {
            HandleC0Control(b);
            return;
        }
        
        if (b == 0x7F)
        {
            // DEL - ignore in most states
            return;
        }
        
        // Process based on current state
        switch (_state)
        {
            case State.Ground:
                ProcessGround(b);
                break;
                
            case State.Escape:
                ProcessEscape(b);
                break;
                
            case State.EscapeIntermediate:
                ProcessEscapeIntermediate(b);
                break;
                
            case State.CsiEntry:
                ProcessCsiEntry(b);
                break;
                
            case State.CsiParam:
                ProcessCsiParam(b);
                break;
                
            case State.CsiIntermediate:
                ProcessCsiIntermediate(b);
                break;
                
            case State.CsiIgnore:
                ProcessCsiIgnore(b);
                break;
                
            case State.OscString:
                ProcessOscString(b);
                break;
                
            default:
                // Other states not fully implemented in MVP
                break;
        }
    }
    
    private void ProcessGround(byte b)
    {
        if (b == 0x1B) // ESC
        {
            EnterEscape();
        }
        else if (b == 0x9B) // CSI (8-bit C1 control)
        {
            EnterCsi();
        }
        else if (b == 0x9D) // OSC (8-bit C1 control)
        {
            EnterOsc();
        }
        else if (b >= 0x20) // Printable character
        {
            _terminalState.WriteChar((char)b);
        }
    }
    
    private void ProcessEscape(byte b)
    {
        if (b == 0x1B) // Another ESC - restart
        {
            EnterEscape();
            return;
        }
        
        if (b == '[') // CSI
        {
            EnterCsi();
        }
        else if (b == ']') // OSC
        {
            EnterOsc();
        }
        else if (b == 'P') // DCS
        {
            EnterDcs();
        }
        else if (b >= 0x20 && b <= 0x2F) // Intermediate character
        {
            _intermediates.Add(b);
            _state = State.EscapeIntermediate;
        }
        else if (b >= 0x30 && b <= 0x7E) // Final character
        {
            ExecuteEscapeSequence(b);
            _state = State.Ground;
        }
        else
        {
            _state = State.Ground;
        }
    }
    
    private void ProcessEscapeIntermediate(byte b)
    {
        if (b == 0x1B) // ESC - restart
        {
            EnterEscape();
            return;
        }
        
        if (b >= 0x20 && b <= 0x2F) // Another intermediate
        {
            _intermediates.Add(b);
        }
        else if (b >= 0x30 && b <= 0x7E) // Final character
        {
            ExecuteEscapeSequence(b);
            _state = State.Ground;
        }
        else
        {
            _state = State.Ground;
        }
    }
    
    private void ProcessCsiEntry(byte b)
    {
        if (b == 0x1B) // ESC
        {
            EnterEscape();
            return;
        }
        
        if (b >= 0x30 && b <= 0x39) // Digit
        {
            _params.Clear();
            _params.Add(b - 0x30);
            _separators.Clear();
            _state = State.CsiParam;
        }
        else if (b == 0x3B) // Semicolon
        {
            _params.Add(0); // Empty parameter
            _separators.Add((byte)';');
            _state = State.CsiParam;
        }
        else if (b == 0x3A) // Colon
        {
            _separators.Add((byte)':');
            _state = State.CsiParam;
        }
        else if (b >= 0x3C && b <= 0x3F) // Private parameter
        {
            _intermediates.Add(b);
            _state = State.CsiParam;
        }
        else if (b >= 0x20 && b <= 0x2F) // Intermediate
        {
            _intermediates.Add(b);
            _state = State.CsiIntermediate;
        }
        else if (b >= 0x40 && b <= 0x7E) // Final
        {
            ExecuteCsiSequence(b);
            _state = State.Ground;
        }
    }
    
    private void ProcessCsiParam(byte b)
    {
        if (b == 0x1B) // ESC
        {
            EnterEscape();
            return;
        }
        
        if (b >= 0x30 && b <= 0x39) // Digit
        {
            if (_params.Count == 0)
                _params.Add(0);
            
            int lastIndex = _params.Count - 1;
            _params[lastIndex] = _params[lastIndex] * 10 + (b - 0x30);
        }
        else if (b == 0x3B || b == 0x3A) // Separator
        {
            _separators.Add(b);
            _params.Add(0); // Start a new parameter
        }
        else if (b >= 0x3C && b <= 0x3F) // Invalid - ignore rest
        {
            _state = State.CsiIgnore;
        }
        else if (b >= 0x20 && b <= 0x2F) // Intermediate
        {
            _intermediates.Add(b);
            _state = State.CsiIntermediate;
        }
        else if (b >= 0x40 && b <= 0x7E) // Final
        {
            ExecuteCsiSequence(b);
            _state = State.Ground;
        }
    }
    
    private void ProcessCsiIntermediate(byte b)
    {
        if (b == 0x1B) // ESC
        {
            EnterEscape();
            return;
        }
        
        if (b >= 0x20 && b <= 0x2F) // Another intermediate
        {
            _intermediates.Add(b);
        }
        else if (b >= 0x40 && b <= 0x7E) // Final
        {
            ExecuteCsiSequence(b);
            _state = State.Ground;
        }
        else
        {
            _state = State.Ground;
        }
    }
    
    private void ProcessCsiIgnore(byte b)
    {
        if (b == 0x1B) // ESC
        {
            EnterEscape();
            return;
        }
        
        if (b >= 0x40 && b <= 0x7E) // Final - exit ignore state
        {
            _state = State.Ground;
        }
        // Otherwise keep ignoring
    }
    
    private void ProcessOscString(byte b)
    {
        if (b == 0x1B) // ESC - might be terminator
        {
            // Peek ahead would be needed, but for simplicity we'll handle ESC \ as terminator
            _state = State.Escape;
            return;
        }
        
        if (b == 0x07) // BEL - OSC terminator
        {
            ExecuteOscSequence();
            _state = State.Ground;
        }
        else if (b == 0x9C) // ST (8-bit)
        {
            ExecuteOscSequence();
            _state = State.Ground;
        }
        else if (b >= 0x20 && b <= 0x7E) // Printable character
        {
            _oscString.Append((char)b);
        }
        // Ignore control characters in OSC string
    }
    
    private void HandleC0Control(byte b)
    {
        switch (b)
        {
            case 0x07: // BEL
                if (_state == State.OscString)
                {
                    ExecuteOscSequence();
                    _state = State.Ground;
                }
                break;
                
            case 0x08: // BS (Backspace)
                _terminalState.WriteChar('\b');
                break;
                
            case 0x09: // HT (Tab)
                _terminalState.WriteChar('\t');
                break;
                
            case 0x0A: // LF (Line Feed)
                _terminalState.WriteChar('\n');
                break;
                
            case 0x0D: // CR (Carriage Return)
                _terminalState.WriteChar('\r');
                break;
                
            case 0x1B: // ESC
                EnterEscape();
                break;
        }
    }
    
    private void EnterEscape()
    {
        _state = State.Escape;
        _params.Clear();
        _separators.Clear();
        _intermediates.Clear();
    }
    
    private void EnterCsi()
    {
        _state = State.CsiEntry;
        _params.Clear();
        _separators.Clear();
        _intermediates.Clear();
    }
    
    private void EnterOsc()
    {
        _state = State.OscString;
        _oscString.Clear();
    }
    
    private void EnterDcs()
    {
        _state = State.DcsEntry;
        _params.Clear();
        _separators.Clear();
        _intermediates.Clear();
    }
    
    private void ExecuteEscapeSequence(byte finalByte)
    {
        // Handle simple escape sequences
        // Most escape sequences are obsolete or not needed for MVP
    }
    
    private void ExecuteCsiSequence(byte finalByte)
    {
        char finalChar = (char)finalByte;
        
        // Special handling for SGR (Select Graphic Rendition)
        if (finalChar == 'm' && _sgrHandler != null)
        {
            // Convert params to ushort array for SGR parser
            var sgrParams = new ushort[_params.Count];
            for (int i = 0; i < _params.Count; i++)
            {
                sgrParams[i] = (ushort)Math.Clamp(_params[i], 0, ushort.MaxValue);
            }
            
            _sgrHandler(sgrParams, _separators.ToArray());
        }
        else
        {
            // Pass to CSI handler
            _csiHandler.Handle(finalChar, _params.ToArray(), _intermediates.ToArray());
        }
    }
    
    private void ExecuteOscSequence()
    {
        if (_oscHandler != null)
        {
            _oscHandler(_oscString.ToString());
        }
    }
}
