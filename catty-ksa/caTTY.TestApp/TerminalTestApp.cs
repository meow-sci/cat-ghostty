using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers;
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

        // Start a shell process
        var launchOptions = new ProcessLaunchOptions
        {
            ShellType = ShellType.Auto,
            InitialWidth = _terminal.Width,
            InitialHeight = _terminal.Height,
            WorkingDirectory = Environment.CurrentDirectory
        };

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

        // Create font configuration for TestApp
        var fontConfig = TerminalFontConfig.CreateForTestApp();
        Console.WriteLine($"TestApp using font configuration: Regular={fontConfig.RegularFontName}, Bold={fontConfig.BoldFontName}");

        // Create terminal controller with explicit font configuration
        _controller = new TerminalController(_terminal, _processManager, fontConfig);

        // Run the ImGui application loop
        StandaloneImGui.Run(() => _controller.Render());
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
