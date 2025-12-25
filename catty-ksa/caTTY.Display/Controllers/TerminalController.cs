using System.Text;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using caTTY.Display.Configuration;
using KSA;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Display.Controllers;

/// <summary>
/// ImGui terminal controller that handles display and input for the terminal emulator.
/// This is the shared controller implementation that is used by both the TestApp and GameMod.
/// </summary>
public class TerminalController : ITerminalController
{
  private readonly ITerminalEmulator _terminal;
  private readonly IProcessManager _processManager;
  private readonly TerminalRenderingConfig _config;
  private bool _isVisible = true;
  private bool _hasFocus = false;
  private bool _disposed = false;

  // Font and rendering settings (now config-based)
  private float _fontSize;
  private float _charWidth;
  private float _lineHeight;

  // Input handling
  private readonly StringBuilder _inputBuffer = new();

  /// <summary>
  /// Gets or sets whether the terminal window is visible.
  /// </summary>
  public bool IsVisible
  {
    get => _isVisible;
    set => _isVisible = value;
  }

  /// <summary>
  /// Gets whether the terminal window currently has focus.
  /// </summary>
  public bool HasFocus => _hasFocus;

  /// <summary>
  /// Gets the current font size for debugging purposes.
  /// </summary>
  public float CurrentFontSize => _fontSize;

  /// <summary>
  /// Gets the current character width for debugging purposes.
  /// </summary>
  public float CurrentCharacterWidth => _charWidth;

  /// <summary>
  /// Gets the current line height for debugging purposes.
  /// </summary>
  public float CurrentLineHeight => _lineHeight;

  /// <summary>
  /// Gets the current DPI scaling factor for debugging purposes.
  /// </summary>
  public float CurrentDpiScalingFactor => _config.DpiScalingFactor;

  /// <summary>
  /// Event raised when user input should be sent to the process.
  /// </summary>
  public event EventHandler<DataInputEventArgs>? DataInput;

  /// <summary>
  /// Creates a new terminal controller with default configuration.
  /// This constructor maintains backward compatibility.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager)
      : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig())
  {
  }

  /// <summary>
  /// Creates a new terminal controller with the specified configuration.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager, TerminalRenderingConfig config)
  {
    _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    _config = config ?? throw new ArgumentNullException(nameof(config));

    // Validate configuration
    _config.Validate();

    // Apply configuration to rendering metrics
    _fontSize = _config.FontSize;
    _charWidth = _config.CharacterWidth;
    _lineHeight = _config.LineHeight;

    // Log configuration for debugging
    LogConfiguration();

    // Subscribe to terminal events
    _terminal.ScreenUpdated += OnScreenUpdated;
    _terminal.ResponseEmitted += OnResponseEmitted;
  }

  /// <summary>
  /// Updates the rendering configuration at runtime.
  /// </summary>
  /// <param name="newConfig">The new configuration to apply</param>
  /// <exception cref="ArgumentNullException">Thrown when newConfig is null</exception>
  /// <exception cref="ArgumentException">Thrown when newConfig contains invalid values</exception>
  public void UpdateRenderingConfig(TerminalRenderingConfig newConfig)
  {
    if (newConfig == null)
      throw new ArgumentNullException(nameof(newConfig));

    // Validate the new configuration
    newConfig.Validate();

    // Apply the new metrics
    _fontSize = newConfig.FontSize;
    _charWidth = newConfig.CharacterWidth;
    _lineHeight = newConfig.LineHeight;

    // Log the configuration change
    Console.WriteLine("TerminalController: Runtime configuration updated");
    LogConfiguration();
  }

  /// <summary>
  /// Logs the current configuration for debugging purposes.
  /// </summary>
  private void LogConfiguration()
  {
    try
    {
      Console.WriteLine($"TerminalController: FontSize={_fontSize:F1}, CharWidth={_charWidth:F1}, LineHeight={_lineHeight:F1}, DpiScale={_config.DpiScalingFactor:F1}");
    }
    catch (Exception ex)
    {
      // Ignore logging failures to prevent crashes
      System.Diagnostics.Debug.WriteLine($"Failed to log configuration: {ex.Message}");
    }
  }

  /// <summary>
  /// Renders the terminal window using ImGui.
  /// </summary>
  public void Render()
  {
    if (!_isVisible)
      return;

    // Push monospace font if available
    PushMonospaceFont(out bool fontUsed);

    try
    {
      // Create terminal window
      ImGui.Begin("Terminal", ref _isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

      // Track focus state
      _hasFocus = ImGui.IsWindowFocused();

      // Display terminal info
      ImGui.Text($"Terminal: {_terminal.Width}x{_terminal.Height}");
      ImGui.SameLine();
      ImGui.Text($"Cursor: ({_terminal.Cursor.Row}, {_terminal.Cursor.Col})");
      ImGui.SameLine();
      ImGui.Text($"Process: {(_processManager.IsRunning ? $"Running (PID: {_processManager.ProcessId})" : "Stopped")}");

      if (_processManager.ExitCode.HasValue)
      {
        ImGui.SameLine();
        ImGui.Text($"Exit Code: {_processManager.ExitCode}");
      }

      ImGui.Separator();

      // Render terminal content
      RenderTerminalContent();

      // Handle input if focused
      if (_hasFocus)
      {
        HandleInput();
      }

      ImGui.End();
    }
    finally
    {
      MaybePopFont(fontUsed);
    }
  }

  /// <summary>
  /// Updates the terminal controller state.
  /// This method can be used for time-based updates if needed.
  /// </summary>
  /// <param name="deltaTime">Time elapsed since last update in seconds</param>
  public void Update(float deltaTime)
  {
    // Currently no time-based updates needed
    // This method is available for future enhancements like cursor blinking
  }

  /// <summary>
  /// Renders the terminal screen content.
  /// </summary>
  private void RenderTerminalContent()
  {
    var drawList = ImGui.GetWindowDrawList();
    var windowPos = ImGui.GetCursorScreenPos();

    // Calculate terminal area
    var terminalWidth = _terminal.Width * _charWidth;
    var terminalHeight = _terminal.Height * _lineHeight;

    // Draw terminal background
    var bgColor = ImGui.ColorConvertFloat4ToU32(new float4(0.0f, 0.0f, 0.0f, 1.0f));
    var terminalRect = new float2(windowPos.X + terminalWidth, windowPos.Y + terminalHeight);
    drawList.AddRectFilled(windowPos, terminalRect, bgColor);

    // Render each cell
    for (int row = 0; row < _terminal.Height; row++)
    {
      for (int col = 0; col < _terminal.Width; col++)
      {
        var cell = _terminal.ScreenBuffer.GetCell(row, col);
        RenderCell(drawList, windowPos, row, col, cell);
      }
    }

    // Render cursor
    RenderCursor(drawList, windowPos);

    // Reserve space for the terminal
    ImGui.Dummy(new float2(terminalWidth, terminalHeight));
  }

  /// <summary>
  /// Renders a single terminal cell.
  /// </summary>
  private void RenderCell(ImDrawListPtr drawList, float2 windowPos, int row, int col, Cell cell)
  {
    var x = windowPos.X + col * _charWidth;
    var y = windowPos.Y + row * _lineHeight;
    var pos = new float2(x, y);

    // Get colors with proper defaults
    var bgColor = ConvertColor(cell.Attributes.BackgroundColor, isBackground: true);
    var fgColor = ConvertColor(cell.Attributes.ForegroundColor, isBackground: false);

    // Always draw background (black by default)
    var bgRect = new float2(x + _charWidth, y + _lineHeight);
    drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));

    // Draw character if not space or null
    if (cell.Character != ' ' && cell.Character != '\0')
    {
      // Apply text styling
      if (cell.Attributes.Bold)
      {
        // Make color brighter for bold
        fgColor = new float4(
            Math.Min(1.0f, fgColor.X * 1.3f),
            Math.Min(1.0f, fgColor.Y * 1.3f),
            Math.Min(1.0f, fgColor.Z * 1.3f),
            fgColor.W
        );
      }

      if (cell.Attributes.Faint)
      {
        // Make color dimmer
        fgColor = new float4(fgColor.X * 0.7f, fgColor.Y * 0.7f, fgColor.Z * 0.7f, fgColor.W);
      }

      if (cell.Attributes.Inverse)
      {
        // Swap foreground and background
        var temp = fgColor;
        fgColor = bgColor;
        bgColor = temp;

        // Redraw background with swapped color
        drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
      }

      // Draw the character
      drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(fgColor), cell.Character.ToString());

      // Draw underline if needed
      if (cell.Attributes.Underline)
      {
        var underlineY = y + _lineHeight - 2;
        var underlineStart = new float2(x, underlineY);
        var underlineEnd = new float2(x + _charWidth, underlineY);
        drawList.AddLine(underlineStart, underlineEnd, ImGui.ColorConvertFloat4ToU32(fgColor));
      }

      // Draw strikethrough if needed
      if (cell.Attributes.Strikethrough)
      {
        var strikeY = y + _lineHeight / 2;
        var strikeStart = new float2(x, strikeY);
        var strikeEnd = new float2(x + _charWidth, strikeY);
        drawList.AddLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(fgColor));
      }
    }
  }

  /// <summary>
  /// Renders the terminal cursor.
  /// </summary>
  private void RenderCursor(ImDrawListPtr drawList, float2 windowPos)
  {
    if (!((TerminalEmulator)_terminal).State.CursorVisible)
      return;

    var cursor = _terminal.Cursor;

    // Ensure cursor position is within bounds
    var cursorCol = Math.Max(0, Math.Min(cursor.Col, _terminal.Width - 1));
    var cursorRow = Math.Max(0, Math.Min(cursor.Row, _terminal.Height - 1));

    var x = windowPos.X + cursorCol * _charWidth;
    var y = windowPos.Y + cursorRow * _lineHeight;

    // Draw cursor as a filled rectangle (white with some transparency)
    var cursorColor = ImGui.ColorConvertFloat4ToU32(new float4(1.0f, 1.0f, 1.0f, 0.8f));
    var cursorPos = new float2(x, y);
    var cursorRect = new float2(x + _charWidth, y + _lineHeight);

    drawList.AddRectFilled(cursorPos, cursorRect, cursorColor);
  }

  /// <summary>
  /// Handles keyboard input when the terminal has focus.
  /// </summary>
  private void HandleInput()
  {
    var io = ImGui.GetIO();

    // Handle text input
    if (io.InputQueueCharacters.Count > 0)
    {
      for (int i = 0; i < io.InputQueueCharacters.Count; i++)
      {
        var ch = (char)io.InputQueueCharacters[i];
        if (ch >= 32 && ch < 127) // Printable ASCII
        {
          SendToProcess(ch.ToString());
        }
      }
    }

    // Handle special keys
    if (ImGui.IsKeyPressed(ImGuiKey.Enter))
    {
      SendToProcess("\r\n");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.Backspace))
    {
      SendToProcess("\b");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.Tab))
    {
      SendToProcess("\t");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.UpArrow))
    {
      SendToProcess("\x1b[A");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
    {
      SendToProcess("\x1b[B");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.RightArrow))
    {
      SendToProcess("\x1b[C");
    }
    else if (ImGui.IsKeyPressed(ImGuiKey.LeftArrow))
    {
      SendToProcess("\x1b[D");
    }

    // Handle Ctrl combinations
    if (io.KeyCtrl)
    {
      if (ImGui.IsKeyPressed(ImGuiKey.C))
      {
        SendToProcess("\x03"); // Ctrl+C
      }
      else if (ImGui.IsKeyPressed(ImGuiKey.D))
      {
        SendToProcess("\x04"); // Ctrl+D
      }
      else if (ImGui.IsKeyPressed(ImGuiKey.Z))
      {
        SendToProcess("\x1a"); // Ctrl+Z
      }
    }
  }

  /// <summary>
  /// Sends text to the shell process.
  /// </summary>
  private void SendToProcess(string text)
  {
    if (_processManager.IsRunning)
    {
      try
      {
        _processManager.Write(text);

        // Also raise the DataInput event for external subscribers
        var bytes = Encoding.UTF8.GetBytes(text);
        DataInput?.Invoke(this, new DataInputEventArgs(text, bytes));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send input to process: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Converts a terminal color to ImGui float4 color.
  /// </summary>
  private static float4 ConvertColor(caTTY.Core.Types.Color? color, bool isBackground = false)
  {
    if (!color.HasValue)
    {
      // Use proper terminal defaults: white text on black background
      return isBackground
          ? new float4(0.0f, 0.0f, 0.0f, 1.0f)  // Black background
          : new float4(1.0f, 1.0f, 1.0f, 1.0f); // White foreground
    }

    return color.Value.Type switch
    {
      ColorType.Named => ConvertNamedColor(color.Value.NamedColor),
      ColorType.Indexed => ConvertIndexedColor(color.Value.Index),
      ColorType.Rgb => new float4(color.Value.Red / 255.0f, color.Value.Green / 255.0f, color.Value.Blue / 255.0f, 1.0f),
      _ => isBackground
          ? new float4(0.0f, 0.0f, 0.0f, 1.0f)  // Black background
          : new float4(1.0f, 1.0f, 1.0f, 1.0f)  // White foreground
    };
  }

  /// <summary>
  /// Converts a named color to ImGui float4 color.
  /// </summary>
  private static float4 ConvertNamedColor(NamedColor namedColor)
  {
    return namedColor switch
    {
      NamedColor.Black => new float4(0.0f, 0.0f, 0.0f, 1.0f),
      NamedColor.Red => new float4(0.8f, 0.0f, 0.0f, 1.0f),
      NamedColor.Green => new float4(0.0f, 0.8f, 0.0f, 1.0f),
      NamedColor.Yellow => new float4(0.8f, 0.8f, 0.0f, 1.0f),
      NamedColor.Blue => new float4(0.0f, 0.0f, 0.8f, 1.0f),
      NamedColor.Magenta => new float4(0.8f, 0.0f, 0.8f, 1.0f),
      NamedColor.Cyan => new float4(0.0f, 0.8f, 0.8f, 1.0f),
      NamedColor.White => new float4(0.8f, 0.8f, 0.8f, 1.0f),
      NamedColor.BrightBlack => new float4(0.4f, 0.4f, 0.4f, 1.0f),
      NamedColor.BrightRed => new float4(1.0f, 0.4f, 0.4f, 1.0f),
      NamedColor.BrightGreen => new float4(0.4f, 1.0f, 0.4f, 1.0f),
      NamedColor.BrightYellow => new float4(1.0f, 1.0f, 0.4f, 1.0f),
      NamedColor.BrightBlue => new float4(0.4f, 0.4f, 1.0f, 1.0f),
      NamedColor.BrightMagenta => new float4(1.0f, 0.4f, 1.0f, 1.0f),
      NamedColor.BrightCyan => new float4(0.4f, 1.0f, 1.0f, 1.0f),
      NamedColor.BrightWhite => new float4(1.0f, 1.0f, 1.0f, 1.0f),
      _ => new float4(0.8f, 0.8f, 0.8f, 1.0f)
    };
  }

  /// <summary>
  /// Converts an indexed color to ImGui float4 color.
  /// </summary>
  private static float4 ConvertIndexedColor(byte index)
  {
    // Standard 16-color palette
    return index switch
    {
      0 => new float4(0.0f, 0.0f, 0.0f, 1.0f), // Black
      1 => new float4(0.8f, 0.0f, 0.0f, 1.0f), // Red
      2 => new float4(0.0f, 0.8f, 0.0f, 1.0f), // Green
      3 => new float4(0.8f, 0.8f, 0.0f, 1.0f), // Yellow
      4 => new float4(0.0f, 0.0f, 0.8f, 1.0f), // Blue
      5 => new float4(0.8f, 0.0f, 0.8f, 1.0f), // Magenta
      6 => new float4(0.0f, 0.8f, 0.8f, 1.0f), // Cyan
      7 => new float4(0.8f, 0.8f, 0.8f, 1.0f), // White
      8 => new float4(0.4f, 0.4f, 0.4f, 1.0f), // Bright Black
      9 => new float4(1.0f, 0.4f, 0.4f, 1.0f), // Bright Red
      10 => new float4(0.4f, 1.0f, 0.4f, 1.0f), // Bright Green
      11 => new float4(1.0f, 1.0f, 0.4f, 1.0f), // Bright Yellow
      12 => new float4(0.4f, 0.4f, 1.0f, 1.0f), // Bright Blue
      13 => new float4(1.0f, 0.4f, 1.0f, 1.0f), // Bright Magenta
      14 => new float4(0.4f, 1.0f, 1.0f, 1.0f), // Bright Cyan
      15 => new float4(1.0f, 1.0f, 1.0f, 1.0f), // Bright White
      _ => new float4(0.8f, 0.8f, 0.8f, 1.0f) // Default
    };
  }

  /// <summary>
  /// Pushes a monospace font if available.
  /// </summary>
  private void PushMonospaceFont(out bool fontUsed)
  {
    // First try the standard FontManager (works in standalone apps)
    try
    {
      if (FontManager.Fonts.TryGetValue("HackNerdFontMono-BoldItalic", out ImFontPtr fontPtr))
      {
        ImGui.PushFont(fontPtr, _fontSize);
        fontUsed = true;
        return;
      }
    }
    catch (Exception ex)
    {
      // FontManager.Fonts may not be available in game mod context
      Console.WriteLine($"FontManager.Fonts not available: {ex.Message}");
      Console.WriteLine(ex.StackTrace);

    }

    // Try the GameMod's font loading system (works in game mod context)
    try
    {
      // Use reflection to call the GameMod's GetFont method
      var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
      if (gameModType != null)
      {
        var getFontMethod = gameModType.GetMethod("GetFont", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (getFontMethod != null)
        {
          var result = getFontMethod.Invoke(null, new object[] { "HackNerdFontMono-BoldItalic" });
          if (result is ImFontPtr font)
          {
            ImGui.PushFont(font, _fontSize);
            fontUsed = true;
            return;
          }
        }
      }
    }
    catch (Exception ex)
    {
      // GameMod font loading not available or failed
      Console.WriteLine($"GameMod font loading failed: {ex.Message}");
      Console.WriteLine(ex.StackTrace);

    }

    fontUsed = false;
  }

  /// <summary>
  /// Pops the font if it was pushed.
  /// </summary>
  private static void MaybePopFont(bool wasUsed)
  {
    if (wasUsed)
    {
      ImGui.PopFont();
    }
  }

  /// <summary>
  /// Handles screen updated events from the terminal.
  /// </summary>
  private void OnScreenUpdated(object? sender, ScreenUpdatedEventArgs e)
  {
    // Screen will be redrawn on next frame
  }

  /// <summary>
  /// Handles response emitted events from the terminal.
  /// </summary>
  private void OnResponseEmitted(object? sender, ResponseEmittedEventArgs e)
  {
    // Send terminal responses back to the process
    if (_processManager.IsRunning)
    {
      try
      {
        _processManager.Write(e.ResponseData.Span);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send terminal response to process: {ex.Message}");
      }
    }
  }

  /// <summary>
  /// Disposes the terminal controller and cleans up resources.
  /// </summary>
  public void Dispose()
  {
    if (!_disposed)
    {
      if (_terminal != null)
      {
        _terminal.ScreenUpdated -= OnScreenUpdated;
        _terminal.ResponseEmitted -= OnResponseEmitted;
      }

      _disposed = true;
    }
  }
}