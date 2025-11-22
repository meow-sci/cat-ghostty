namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Manages output processing from a child process.
/// Feeds output bytes through the ANSI parser which updates terminal state.
/// </summary>
public class OutputController : IDisposable
{
    private readonly TerminalState _terminalState;
    private readonly AnsiParser _ansiParser;
    private readonly CsiHandler _csiHandler;
    private readonly OscHandler _oscHandler;
    private readonly SgrHandler _sgrHandler;
    private readonly ConsoleView _view;
    private bool _disposed;
    
    public OutputController(TerminalState terminalState, ConsoleView view)
    {
        _terminalState = terminalState;
        _view = view;
        
        // Initialize handlers
        _csiHandler = new CsiHandler(terminalState);
        _oscHandler = new OscHandler(terminalState);
        _sgrHandler = new SgrHandler(terminalState);
        
        // Initialize parser
        _ansiParser = new AnsiParser(terminalState, _csiHandler);
        _ansiParser.SetOscHandler(HandleOsc);
        _ansiParser.SetSgrHandler(HandleSgr);
    }
    
    /// <summary>
    /// Processes a buffer of output bytes from the child process.
    /// Updates terminal state and triggers view refresh.
    /// </summary>
    public void ProcessOutput(byte[] buffer, int count)
    {
        if (_disposed || buffer == null || count <= 0)
            return;
        
        try
        {
            // Feed bytes to parser
            for (int i = 0; i < count; i++)
            {
                _ansiParser.ProcessByte(buffer[i]);
            }
            
            // Render updated state
            _view.Render();
        }
        catch (Exception)
        {
            // Silently handle parsing errors in MVP
        }
    }
    
    /// <summary>
    /// Processes output bytes and triggers a view refresh.
    /// </summary>
    public void ProcessOutput(byte[] buffer)
    {
        ProcessOutput(buffer, buffer?.Length ?? 0);
    }
    
    private void HandleOsc(string oscString)
    {
        _oscHandler.Handle(oscString);
    }
    
    private void HandleSgr(ushort[] parameters, byte[] separators)
    {
        _sgrHandler.Handle(parameters, separators);
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _oscHandler?.Dispose();
            _sgrHandler?.Dispose();
            _disposed = true;
        }
    }
}
