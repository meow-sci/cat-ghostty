using System.Reflection;
using Brutal.ImGuiApi;
using StarMap.API;
using caTTY.Core.Terminal;
using caTTY.Display.Controllers;
using KSA;
using caTTY.Display.Rendering;

namespace caTTY.GameMod;

/// <summary>
/// KSA game mod for caTTY terminal emulator.
/// Provides a terminal window that can be toggled with F12 key.
/// </summary>
[StarMapMod]
public class TerminalMod
{
  private ITerminalEmulator? _terminal;
  private IProcessManager? _processManager;
  private ITerminalController? _controller;
  private bool _isInitialized = false;
  private bool _isDisposed = false;
  private bool _terminalVisible = false;


  /// <summary>
  /// Gets a value indicating whether the mod should be unloaded immediately.
  /// </summary>
  public bool ImmediateUnload => false;

  /// <summary>
  /// Called after the GUI is rendered.
  /// </summary>
  /// <param name="dt">Delta time.</param>
  [StarMapAfterGui]
  public void OnAfterUi(double dt)
  {
    // Console.WriteLine("caTTY OnAfterUi");
    if (!_isInitialized || _isDisposed)
      return;

    try
    {
      // Handle terminal toggle keybind (F12)
      if (Brutal.ImGuiApi.ImGui.IsKeyPressed(ImGuiKey.F12))
      {
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
  /// Called before the GUI is rendered.
  /// </summary>
  /// <param name="dt">Delta time.</param>
  [StarMapBeforeGui]
  public void OnBeforeUi(double dt)
  {
    // Console.WriteLine("caTTY OnBeforeUi");
    // No pre-UI logic needed currently
  }

  /// <summary>
  /// Called when all mods are loaded.
  /// </summary>
  [StarMapAllModsLoaded]
  public void OnFullyLoaded()
  {
    Console.WriteLine("caTTY OnFullyLoaded");
    try
    {
      InitializeTerminal();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"caTTY GameMod initialization failed: {ex.Message}");
    }
  }

  /// <summary>
  /// Called for immediate loading.
  /// </summary>
  [StarMapImmediateLoad]
  public void OnImmediatLoad()
  {
    Console.WriteLine("caTTY OnImmediatLoad");
    // No immediate load logic needed
  }

  /// <summary>
  /// Called when the mod is unloaded.
  /// </summary>
  [StarMapUnload]
  public void Unload()
  {
    Console.WriteLine("caTTY Unload");
    try
    {
      DisposeResources();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"caTTY GameMod unload error: {ex.Message}");
    }
  }

  /// <summary>
  /// Initializes the terminal emulator and related components.
  /// Guards against double initialization.
  /// </summary>
  private void InitializeTerminal()
  {
    if (_isInitialized || _isDisposed)
      return;

    Console.WriteLine("caTTY GameMod: Initializing terminal...");

    try
    {
      // Load fonts first
      CaTTYFontManager.LoadFonts();

      // Create terminal emulator (80x24 is a standard terminal size)
      _terminal = new TerminalEmulator(80, 24);

      // Create process manager
      _processManager = new ProcessManager();

      // Create terminal controller
      _controller = new TerminalController(_terminal, _processManager);

      // Set up event handlers
      _processManager.DataReceived += OnProcessDataReceived;
      _processManager.ProcessExited += OnProcessExited;
      _processManager.ProcessError += OnProcessError;

      _controller.DataInput += OnControllerDataInput;

      // Start a shell process
      var options = ProcessLaunchOptions.CreateDefault();
      options.InitialWidth = _terminal.Width;
      options.InitialHeight = _terminal.Height;

      // Start the process asynchronously (fire and forget for game context)
      _ = Task.Run(async () =>
      {
        try
        {
          await _processManager.StartAsync(options);
          Console.WriteLine("caTTY GameMod: Shell process started successfully");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"caTTY GameMod: Failed to start shell process: {ex.Message}");
        }
      });

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
  /// Toggles the terminal window visibility.
  /// </summary>
  private void ToggleTerminal()
  {
    if (!_isInitialized || _isDisposed)
      return;

    _terminalVisible = !_terminalVisible;

    if (_controller != null)
    {
      _controller.IsVisible = _terminalVisible;
    }

    Console.WriteLine($"caTTY GameMod: Terminal {(_terminalVisible ? "shown" : "hidden")}");
  }

  /// <summary>
  /// Handles data received from the shell process.
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
  /// Handles shell process exit events.
  /// </summary>
  private void OnProcessExited(object? sender, ProcessExitedEventArgs e)
  {
    Console.WriteLine($"caTTY GameMod: Shell process exited with code {e.ExitCode}");

    // Optionally restart the shell or show a message
    // For now, just log the exit
  }

  /// <summary>
  /// Handles shell process error events.
  /// </summary>
  private void OnProcessError(object? sender, ProcessErrorEventArgs e)
  {
    Console.WriteLine($"caTTY GameMod: Shell process error: {e.Message}");
  }

  /// <summary>
  /// Handles input from the terminal controller.
  /// </summary>
  private void OnControllerDataInput(object? sender, DataInputEventArgs e)
  {
    // The controller already sends data to the process manager,
    // but we could add additional processing here if needed
  }


  /// <summary>
  /// Gets a loaded font by name, or null if not found.
  /// </summary>
  public static ImFontPtr? GetFont(string fontName)
  {
    return CaTTYFontManager.LoadedFonts.TryGetValue(fontName, out ImFontPtr font) ? font : null;
  }

  /// <summary>
  /// Disposes all resources and cleans up.
  /// Guards against double disposal.
  /// </summary>
  private void DisposeResources()
  {
    if (_isDisposed)
      return;

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

      if (_controller != null)
      {
        _controller.DataInput -= OnControllerDataInput;
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