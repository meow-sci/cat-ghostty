namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Main terminal emulator class that orchestrates all MVC components.
/// Implements the event loop for input/output processing.
/// </summary>
public class TerminalEmulator : IDisposable
{
    private readonly TerminalState _state;
    private readonly ConsoleView _view;
    private readonly InputController _inputController;
    private readonly OutputController _outputController;
    private readonly ProcessManager _processManager;
    private bool _disposed;
    private bool _running;
    
    public TerminalEmulator()
    {
        // Initialize Model
        _state = new TerminalState();
        
        // Initialize View
        _view = new ConsoleView(_state);
        
        // Initialize Controllers
        _inputController = new InputController();
        _outputController = new OutputController(_state, _view);
        _processManager = new ProcessManager();
        
        // Hook up process exit event
        _processManager.ProcessExited += OnProcessExited;
    }
    
    /// <summary>
    /// Starts the terminal emulator.
    /// </summary>
    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TerminalEmulator));
        
        if (_running)
            throw new InvalidOperationException("Terminal emulator already running");
        
        try
        {
            // Initialize console
            _view.Initialize();
            
            // Start shell process
            _processManager.Start();
            
            // Initial render
            _view.Render();
            
            // Run main loop
            _running = true;
            RunMainLoop();
        }
        finally
        {
            _view.Cleanup();
            _running = false;
        }
    }
    
    /// <summary>
    /// Stops the terminal emulator.
    /// </summary>
    public void Stop()
    {
        _running = false;
    }
    
    private void RunMainLoop()
    {
        Console.WriteLine("Terminal Emulator Started. Press Ctrl+C to exit.");
        Thread.Sleep(1000);
        _view.Render();
        
        while (_running && _processManager.IsRunning)
        {
            try
            {
                // Process input from console
                var keyData = _inputController.ReadAndEncodeKey();
                if (keyData != null && keyData.Length > 0)
                {
                    _processManager.SendInput(keyData);
                }
                
                // Process output from shell
                var outputData = _processManager.ReadOutput();
                if (outputData != null && outputData.Length > 0)
                {
                    _outputController.ProcessOutput(outputData);
                }
                
                // Also process stderr
                var errorData = _processManager.ReadError();
                if (errorData != null && errorData.Length > 0)
                {
                    _outputController.ProcessOutput(errorData);
                }
                
                // Small sleep to prevent CPU spin
                if (keyData == null && outputData == null && errorData == null)
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                // Log error but continue running
                Console.Error.WriteLine($"Error in main loop: {ex.Message}");
                Thread.Sleep(100);
            }
        }
        
        Console.WriteLine("\nProcess exited. Press any key to close...");
        Console.ReadKey(true);
    }
    
    private void OnProcessExited(object? sender, EventArgs e)
    {
        _running = false;
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _running = false;
            _processManager?.Dispose();
            _outputController?.Dispose();
            _inputController?.Dispose();
            _disposed = true;
        }
    }
}
