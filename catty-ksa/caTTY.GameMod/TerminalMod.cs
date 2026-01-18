using Brutal.ImGuiApi;
using caTTY.Core.Rpc;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers;
using caTTY.Display.Rendering;
using caTTY.TermSequenceRpc;
using Microsoft.Extensions.Logging.Abstractions;
using StarMap.API;

namespace caTTY.GameMod;

/// <summary>
///     KSA game mod for caTTY terminal emulator.
///     Provides a terminal window that can be toggled with F12 key.
/// </summary>
[StarMapMod]
public class TerminalMod
{
    private ITerminalController? _controller;
    private bool _isDisposed;
    private bool _isInitialized;
    private IProcessManager? _processManager;
    private ITerminalEmulator? _terminal;
    private bool _terminalVisible;


    /// <summary>
    ///     Gets a value indicating whether the mod should be unloaded immediately.
    /// </summary>
    public bool ImmediateUnload => false;

    /// <summary>
    ///     Called after the GUI is rendered.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    [StarMapAfterGui]
    public void OnAfterUi(double dt)
    {
        // Console.WriteLine("caTTY OnAfterUi");
        if (!_isInitialized || _isDisposed)
        {
            return;
        }

        try
        {
            // Handle terminal toggle keybind (F12)
            if (ImGui.IsKeyPressed(ImGuiKey.F12))
            {
                // Console.WriteLine($"DEBUG: GameMod detected F12 press, current _terminalVisible={_terminalVisible}");
                ToggleTerminal();
            }

            // Update and render terminal if visible
            if (_terminalVisible && _controller != null)
            {
                _controller.Update((float)dt);
                _controller.Render();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod OnAfterUi error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            // Don't let exceptions crash the game
        }
    }

    /// <summary>
    ///     Called before the GUI is rendered.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    [StarMapBeforeGui]
    public void OnBeforeUi(double dt)
    {
        // Console.WriteLine("caTTY OnBeforeUi");
        // No pre-UI logic needed currently
    }

    /// <summary>
    ///     Called when all mods are loaded.
    /// </summary>
    [StarMapAllModsLoaded]
    public void OnFullyLoaded()
    {
        Console.WriteLine("caTTY OnFullyLoaded");
        try
        {
            Patcher.patch();

            // BUGFIX: Explicitly load caTTY.CustomShells assembly so that CustomShellRegistry can discover
            // custom shell implementations via reflection. The assembly is referenced in the project
            // but doesn't get loaded automatically unless a type from it is instantiated. We need it
            // loaded before shell discovery happens.
            try
            {
                System.Reflection.Assembly.Load("caTTY.CustomShells");
                Console.WriteLine("caTTY GameMod: Loaded caTTY.CustomShells assembly for custom shell discovery");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"caTTY GameMod: Warning - Failed to load caTTY.CustomShells assembly: {ex.Message}");
                // Continue anyway - custom shells just won't be available
            }

            InitializeTerminal();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    ///     Called for immediate loading.
    /// </summary>
    [StarMapImmediateLoad]
    public void OnImmediatLoad()
    {
        Console.WriteLine("caTTY OnImmediatLoad");
        // No immediate load logic needed
    }

    /// <summary>
    ///     Called when the mod is unloaded.
    /// </summary>
    [StarMapUnload]
    public void Unload()
    {
        Console.WriteLine("caTTY Unload");
        try
        {
            Patcher.unload();

            DisposeResources();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod unload error: {ex.Message}");
        }
    }

    /// <summary>
    ///     Initializes the terminal emulator and related components.
    ///     Guards against double initialization.
    /// </summary>
    private void InitializeTerminal()
    {
        if (_isInitialized || _isDisposed)
        {
            return;
        }

        Console.WriteLine("caTTY GameMod: Initializing terminal...");

        try
        {
            // Load fonts first
            CaTTYFontManager.LoadFonts();

            var _outputBuffer = new List<byte[]>();
            var (rpcHandler, oscRpcHandler) = RpcBootstrapper.CreateKsaRpcHandlers(
                NullLogger.Instance,
                bytes => _outputBuffer.Add(bytes));

            // Create session manager with persisted shell configuration and RPC handlers
            var sessionManager = SessionManagerFactory.CreateWithPersistedConfiguration(
                maxSessions: 20,
                rpcHandler: rpcHandler,
                oscRpcHandler: oscRpcHandler);

            // BUGFIX: Create shell options BEFORE creating session, so that session can create the correct
            // ProcessManager and initialize it. SessionManager.CreateSessionAsync will call SessionCreator,
            // which passes launchOptions to TerminalSessionFactory.CreateSession, which uses launchOptions
            // to decide whether to create ProcessManager or CustomShellPtyBridge. SessionCreator then calls
            // session.InitializeAsync(launchOptions) which starts the shell.
            // EASY SHELL SWITCHING: Uncomment one of the following options to change shells

            // Option 1: Use default (WSL2 on Windows)
            var options = ShellConfiguration.Default();

            // Option 2: Simple WSL2 configurations
            // var options = ShellConfiguration.Wsl();                    // Default WSL distribution
            // var options = ShellConfiguration.Wsl("Ubuntu");           // Specific distribution
            // var options = ShellConfiguration.Wsl("Ubuntu", "/home/username"); // With working directory

            // Option 3: Windows shells
            // var options = ShellConfiguration.PowerShell();            // Windows PowerShell
            // var options = ShellConfiguration.PowerShellCore();        // PowerShell Core (pwsh)
            // var options = ShellConfiguration.Cmd();                   // Command Prompt

            // Option 4: Common pre-configured shells
            // var options = ShellConfiguration.Common.Ubuntu;           // Ubuntu WSL2
            // var options = ShellConfiguration.Common.Debian;           // Debian WSL2
            // var options = ShellConfiguration.Common.GitBash;          // Git Bash
            // var options = ShellConfiguration.Common.Msys2Bash;        // MSYS2 Bash

            // Option 5: Custom shell
            // var options = ShellConfiguration.Custom(@"C:\custom\shell.exe", "--arg1", "--arg2");

            // Create session WITH launchOptions so that the session creates and initializes the correct ProcessManager
            var session = sessionManager.CreateSessionAsync(launchOptions: options).Result;

            _terminal = session.Terminal;
            // BUGFIX: Get ProcessManager from session (already initialized by SessionCreator)
            _processManager = session.ProcessManager;

            Console.WriteLine($"TerminalMod: rpc enabled={_terminal.IsRpcEnabled}");

            var fontConfig = TerminalFontConfig.CreateForGameMod();
            Console.WriteLine(
                $"caTTY GameMod using explicit font configuration: Regular={fontConfig.RegularFontName}, Bold={fontConfig.BoldFontName}");
            _controller = new TerminalController(sessionManager, fontConfig);

            // Option 2: Automatic detection (alternative approach - convenient for development)
            // Uncomment the following lines to use automatic detection instead:
            // Console.WriteLine("caTTY GameMod using automatic font detection");
            // _controller = new TerminalController(sessionManager);

            // Option 3: Explicit automatic detection (alternative approach - shows detection explicitly)
            // Uncomment the following lines to use explicit automatic detection:
            // var autoConfig = FontContextDetector.DetectAndCreateConfig();
            // Console.WriteLine($"caTTY GameMod using detected font configuration: Regular={autoConfig.RegularFontName}, Bold={autoConfig.BoldFontName}");
            // _controller = new TerminalController(sessionManager, autoConfig);

            // Set up event handlers
            _processManager.DataReceived += OnProcessDataReceived;
            _processManager.ProcessExited += OnProcessExited;
            _processManager.ProcessError += OnProcessError;

            _isInitialized = true;
            Console.WriteLine("caTTY GameMod: Terminal initialized successfully. Press F12 to toggle.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod: Terminal initialization failed: {ex.Message}");
            DisposeResources();
            throw;
        }
    }

    /// <summary>
    ///     Toggles the terminal window visibility.
    /// </summary>
    private void ToggleTerminal()
    {
        if (!_isInitialized || _isDisposed)
        {
            return;
        }

        Console.WriteLine(
            $"DEBUG: ToggleTerminal called, changing _terminalVisible from {_terminalVisible} to {!_terminalVisible}");

        _terminalVisible = !_terminalVisible;

        if (_controller != null)
        {
            _controller.IsVisible = _terminalVisible;
            // Console.WriteLine($"DEBUG: Set controller.IsVisible to {_terminalVisible}");
        }

        Console.WriteLine($"caTTY GameMod: Terminal {(_terminalVisible ? "shown" : "hidden")}");
    }

    /// <summary>
    ///     Handles data received from the shell process.
    /// </summary>
    private void OnProcessDataReceived(object? sender, DataReceivedEventArgs e)
    {
        try
        {
            // Forward data to terminal emulator
            _terminal?.Write(e.Data.Span);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod: Error processing shell data: {ex.Message}");
        }
    }

    /// <summary>
    ///     Handles shell process exit events.
    /// </summary>
    private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
    {
        Console.WriteLine($"caTTY GameMod: Shell process exited with code {e.ExitCode}");

        // Optionally restart the shell or show a message
        // For now, just log the exit
    }

    /// <summary>
    ///     Handles shell process error events.
    /// </summary>
    private void OnProcessError(object? sender, ProcessErrorEventArgs e)
    {
        Console.WriteLine($"caTTY GameMod: Shell process error: {e.Message}");
    }

    /// <summary>
    ///     Gets a loaded font by name, or null if not found.
    /// </summary>
    public static ImFontPtr? GetFont(string fontName)
    {
        return CaTTYFontManager.LoadedFonts.TryGetValue(fontName, out ImFontPtr font) ? font : null;
    }

    /// <summary>
    ///     Disposes all resources and cleans up.
    ///     Guards against double disposal.
    /// </summary>
    private void DisposeResources()
    {
        if (_isDisposed)
        {
            return;
        }

        Console.WriteLine("caTTY GameMod: Disposing resources...");

        try
        {
            // Unsubscribe from events first to prevent callbacks during disposal
            if (_processManager != null)
            {
                _processManager.DataReceived -= OnProcessDataReceived;
                _processManager.ProcessExited -= OnProcessExited;
                _processManager.ProcessError -= OnProcessError;
            }

            // Dispose components in reverse order of creation
            _controller?.Dispose();
            _controller = null;

            // Stop process manager (this will terminate the shell)
            if (_processManager != null)
            {
                try
                {
                    if (_processManager.IsRunning)
                    {
                        // Use Task.Run to avoid blocking the game thread
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _processManager.StopAsync();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"caTTY GameMod: Error stopping process: {ex.Message}");
                            }
                        });
                    }
                }
                finally
                {
                    _processManager.Dispose();
                    _processManager = null;
                }
            }

            _terminal?.Dispose();
            _terminal = null;

            _isInitialized = false;
            _isDisposed = true;

            Console.WriteLine("caTTY GameMod: Resources disposed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caTTY GameMod: Error during resource disposal: {ex.Message}");
        }
    }
}
