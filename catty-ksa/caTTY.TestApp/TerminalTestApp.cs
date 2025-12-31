using caTTY.Core.Terminal;
using caTTY.Core.Tracing;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers;
using caTTY.Display.Types;
using caTTY.TestApp.Rendering;

namespace caTTY.TestApp;

/// <summary>
///     Main terminal test application that integrates terminal emulator, process manager, and ImGui controller.
///     This class manages the application lifecycle and coordinates between components.
/// </summary>
public class TerminalTestApp : IDisposable
{
    private readonly ProcessManager _processManager;
    private readonly TerminalEmulator _terminal;
    private ITerminalController? _controller;
    private bool _disposed;

    /// <summary>
    ///     Creates a new terminal test application with default terminal dimensions.
    /// </summary>
    public TerminalTestApp()
    {
        // Create terminal with standard 80x24 dimensions
        _terminal = new TerminalEmulator(80, 24);
        _processManager = new ProcessManager();

        // Wire up events
        _processManager.DataReceived += OnProcessDataReceived;
        _processManager.ProcessExited += OnProcessExited;
        _processManager.ProcessError += OnProcessError;
    }

    /// <summary>
    ///     Disposes the application and cleans up all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _controller?.Dispose();
            _processManager?.Dispose();
            _terminal?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    ///     Runs the terminal test application with BRUTAL ImGui rendering.
    /// </summary>
    public async Task RunAsync()
    {
        Console.WriteLine("Starting shell process...");
        // TerminalTracer.Enabled = true;

        // Display console color test if supported
        ConsoleColorTest.DisplayColorTest();
        Console.WriteLine();

        // Start a shell process
        // EASY SHELL SWITCHING: Uncomment one of the following options to change shells
        
        // Option 1: Use default (WSL2 on Windows)
        var launchOptions = ShellConfiguration.Default();
        
        // Option 2: Simple WSL2 configurations
        // var launchOptions = ShellConfiguration.Wsl();                    // Default WSL distribution
        // var launchOptions = ShellConfiguration.Wsl("Ubuntu");           // Specific distribution
        // var launchOptions = ShellConfiguration.Wsl("Ubuntu", "/home/username"); // With working directory
        
        // Option 3: Windows shells
        // var launchOptions = ShellConfiguration.PowerShell();            // Windows PowerShell
        // var launchOptions = ShellConfiguration.PowerShellCore();        // PowerShell Core (pwsh)
        // var launchOptions = ShellConfiguration.Cmd();                   // Command Prompt
        
        // Option 4: Common pre-configured shells
        // var launchOptions = ShellConfiguration.Common.Ubuntu;           // Ubuntu WSL2
        // var launchOptions = ShellConfiguration.Common.Debian;           // Debian WSL2
        // var launchOptions = ShellConfiguration.Common.GitBash;          // Git Bash
        // var launchOptions = ShellConfiguration.Common.Msys2Bash;        // MSYS2 Bash
        
        // Option 5: Custom shell
        // var launchOptions = ShellConfiguration.Custom(@"C:\custom\shell.exe", "--arg1", "--arg2");

        // Set terminal dimensions
        launchOptions.InitialWidth = _terminal.Width;
        launchOptions.InitialHeight = _terminal.Height;
        launchOptions.WorkingDirectory = Environment.CurrentDirectory;

        try
        {
            await _processManager.StartAsync(launchOptions);
            Console.WriteLine($"Shell process started (PID: {_processManager.ProcessId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start shell process: {ex.Message}");
            throw;
        }

        Console.WriteLine("Initializing BRUTAL ImGui context...");

        // Option 1: Explicit font configuration (current approach - recommended for production)
        // Create session manager and add a session
        var sessionManager = new SessionManager();
        var session = sessionManager.CreateSessionAsync().Result;
        
        var fontConfig = TerminalFontConfig.CreateForTestApp();
        Console.WriteLine($"TestApp using explicit font configuration: Regular={fontConfig.RegularFontName}, Bold={fontConfig.BoldFontName}");
        _controller = new TerminalController(sessionManager, fontConfig);

        // Option 2: Automatic detection (alternative approach - convenient for development)
        // Uncomment the following lines to use automatic detection instead:
        // Console.WriteLine("TestApp using automatic font detection");
        // _controller = new TerminalController(sessionManager);

        // Option 3: Explicit automatic detection (alternative approach - shows detection explicitly)
        // Uncomment the following lines to use explicit automatic detection:
        // var autoConfig = FontContextDetector.DetectAndCreateConfig();
        // Console.WriteLine($"TestApp using detected font configuration: Regular={autoConfig.RegularFontName}, Bold={autoConfig.BoldFontName}");
        // _controller = new TerminalController(sessionManager, autoConfig);

        Console.WriteLine("Starting ImGui render loop...");
        Console.WriteLine("Try running colored commands like: ls --color, echo -e \"\\033[31mRed text\\033[0m\"");
        Console.WriteLine();

        // Run the ImGui application loop with update and render
        StandaloneImGui.Run((deltaTime) => 
        {
            // Update controller (handles cursor blinking)
            _controller.Update(deltaTime);
            
            // Render the terminal
            _controller.Render();
        });
    }

    /// <summary>
    ///     Handles data received from the shell process.
    /// </summary>
    private void OnProcessDataReceived(object? sender, DataReceivedEventArgs e)
    {
        // Forward shell output to terminal emulator
        _terminal.Write(e.Data.Span);
    }

    /// <summary>
    ///     Handles shell process exit.
    /// </summary>
    private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
    {
        Console.WriteLine($"Shell process exited with code {e.ExitCode}");
    }

    /// <summary>
    ///     Handles shell process errors.
    /// </summary>
    private void OnProcessError(object? sender, ProcessErrorEventArgs e)
    {
        Console.WriteLine($"Shell process error: {e.Message}");
    }
}
