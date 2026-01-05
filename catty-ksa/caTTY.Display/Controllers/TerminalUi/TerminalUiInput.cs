using System;
using System.Linq;
using System.Text;
using Brutal.ImGuiApi;
using caTTY.Core.Input;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Core.Utils;
using caTTY.Display.Configuration;
using caTTY.Display.Input;
using caTTY.Display.Rendering;
using caTTY.Display.Types;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
///     Handles input capture, keyboard input, and mouse wheel input for the terminal.
///     This class is responsible for processing user input and sending it to the terminal process.
/// </summary>
public class TerminalUiInput
{
  private readonly TerminalController _controller;
  private readonly SessionManager _sessionManager;
  private readonly CursorRenderer _cursorRenderer;
  private readonly MouseWheelScrollConfig _scrollConfig;
  private float _wheelAccumulator = 0.0f;

  public TerminalUiInput(
      TerminalController controller,
      SessionManager sessionManager,
      CursorRenderer cursorRenderer,
      MouseWheelScrollConfig scrollConfig)
  {
    _controller = controller;
    _sessionManager = sessionManager;
    _cursorRenderer = cursorRenderer;
    _scrollConfig = scrollConfig;
  }

  /// <summary>
  ///     Manages input capture state using ImGui's keyboard capture mechanism.
  ///     This ensures the terminal receives keyboard input when focused and visible.
  /// </summary>
  public void ManageInputCapture()
  {
    try
    {

      // Invisible input widget
      // even this doesn't fully prevent KSA from processing global hot keys like 'm'

      // ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
      // // Dummy buffer for InputText - we don't need the captured text so use a local stack buffer
      // ReadOnlySpan<byte> dummySpan = stackalloc byte[64];
      // ImGui.InputText("##hidden", dummySpan, ImGuiInputTextFlags.None);
      // ImGui.PopStyleVar();

      // Console.WriteLine($"IsInputCaptureActive={IsInputCaptureActive}");

      // ImGui.GetIO().WantCaptureKeyboard = true;
      // Console.WriteLine($"ImGui.GetIO().WantCaptureKeyboard {ImGui.GetIO().WantCaptureKeyboard}");
      // ImGui.SetNextFrameWantCaptureKeyboard(true);
      // ImGui.SetKeyboardFocusHere();

      // Use SetNextFrameWantCaptureKeyboard when terminal should capture input
      // This tells ImGui (and the game) that we want exclusive keyboard access for the next frame
      // This is the proper way to capture keyboard input in KSA game context
      if (_controller.IsInputCaptureActive)
      {
        // ImGui.SetKeyboardFocusHere();

        // TODO: FIXME: this still doesn't prevent global hotkeys like 'm' from taking place
        // ImGui.SetNextFrameWantCaptureKeyboard(true);
        // ImGui.SetKeyboardFocusHere();
        // Console.WriteLine("TerminalController: Capturing keyboard input (suppressing game hotkeys)");
      }
      // Note: No need to explicitly set to false due to ImGui immediate mode design
      // Just don't call SetNextFrameWantCaptureKeyboard when terminal shouldn't capture input
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error managing input capture: {ex.Message}");
    }
  }

  /// <summary>
  ///     Determines whether the terminal should capture input.
  ///     Terminal captures input only when both focused and visible.
  /// </summary>
  public bool ShouldCaptureInput()
  {
    // Terminal captures input only when both focused and visible
    // This matches the TypeScript implementation's input priority management
    return _controller.IsInputCaptureActive;
  }

  /// <summary>
  ///     Handles all keyboard input for the terminal including special keys and text input.
  ///     Processes mouse wheel events and manages scrollback interaction.
  /// </summary>
  public void HandleInput()
  {
    // Verify focus state before processing input (defensive programming)
    if (!_controller.HasFocus || !_controller.IsVisible)
    {
      return;
    }

    ImGuiIOPtr io = ImGui.GetIO();

    // Note: Input capture is now managed centrally in ManageInputCapture() using SetNextFrameWantCaptureKeyboard()
    // Note: Mouse input for selection is now handled in RenderTerminalContent()
    // via the invisible button approach to prevent window dragging

    // Any user input (typing/keypresses that generate terminal input) should snap to the latest output.
    // This is intentionally independent from new-content behavior.
    bool userProvidedInputThisFrame = false;
    void MarkUserInput()
    {
      if (userProvidedInputThisFrame)
      {
        return;
      }

      userProvidedInputThisFrame = true;
      var activeSession = _sessionManager.ActiveSession;
      activeSession?.Terminal.ScrollbackManager?.OnUserInput();

      // Make cursor immediately visible when user provides input
      _cursorRenderer.ForceVisible();
    }

    // Handle mouse wheel input first
    HandleMouseWheelInput();

    // Get current terminal state for input encoding
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null) return;

    var terminalState = ((TerminalEmulator)activeSession.Terminal).State;
    bool applicationCursorKeys = terminalState.ApplicationCursorKeys;

    // Create modifier state from ImGui
    var modifiers = new KeyModifiers(
        shift: io.KeyShift,
        alt: io.KeyAlt,
        ctrl: io.KeyCtrl,
        meta: false // ImGui doesn't expose Meta key directly
    );

    // Handle special keys first (they take priority over text input)
    bool specialKeyHandled = HandleSpecialKeys(modifiers, applicationCursorKeys, MarkUserInput);

    // Only handle text input if no special key was processed
    // This prevents double-sending when a key produces both a key event and text input
    if (!specialKeyHandled && io.InputQueueCharacters.Count > 0)
    {
      for (int i = 0; i < io.InputQueueCharacters.Count; i++)
      {
        char ch = (char)io.InputQueueCharacters[i];
        if (ch >= 32 && ch < 127) // Printable ASCII
        {
          MarkUserInput();
          SendToProcess(ch.ToString());
        }
      }
    }
  }

  /// <summary>
  ///     Handles special key presses (arrows, function keys, Enter, etc.).
  ///     Provides comprehensive key handling matching TypeScript implementation.
  /// </summary>
  /// <returns>True if a special key was handled, false otherwise</returns>
  private bool HandleSpecialKeys(KeyModifiers modifiers, bool applicationCursorKeys, Action markUserInput)
  {
    // Define key mappings from ImGuiKey to string for non-text keys
    var keyMappings = new[]
    {
            // Basic keys
            (ImGuiKey.Enter, "Enter"),
            (ImGuiKey.Backspace, "Backspace"),
            (ImGuiKey.Tab, "Tab"),
            (ImGuiKey.Escape, "Escape"),

            // Arrow keys
            (ImGuiKey.UpArrow, "ArrowUp"),
            (ImGuiKey.DownArrow, "ArrowDown"),
            (ImGuiKey.RightArrow, "ArrowRight"),
            (ImGuiKey.LeftArrow, "ArrowLeft"),

            // Navigation keys
            (ImGuiKey.Home, "Home"),
            (ImGuiKey.End, "End"),
            (ImGuiKey.Delete, "Delete"),
            (ImGuiKey.Insert, "Insert"),
            (ImGuiKey.PageUp, "PageUp"),
            (ImGuiKey.PageDown, "PageDown"),

            // Function keys
            (ImGuiKey.F1, "F1"),
            (ImGuiKey.F2, "F2"),
            (ImGuiKey.F3, "F3"),
            (ImGuiKey.F4, "F4"),
            (ImGuiKey.F5, "F5"),
            (ImGuiKey.F6, "F6"),
            (ImGuiKey.F7, "F7"),
            (ImGuiKey.F8, "F8"),
            (ImGuiKey.F9, "F9"),
            (ImGuiKey.F10, "F10"),
            (ImGuiKey.F11, "F11"),
            (ImGuiKey.F12, "F12")
        };

    // Process each key mapping
    foreach (var (imguiKey, keyString) in keyMappings)
    {
      if (ImGui.IsKeyPressed(imguiKey))
      {
          // Special case: Don't process F12 in GameMod context to avoid conflict with terminal toggle
        // In GameMod, F12 is reserved for terminal visibility toggle
        if (keyString == "F12")
        {
          // Console.WriteLine($"DEBUG: F12 pressed in GameMod context, skipping F12 processing to avoid conflict");
          return false; // Let F12 be handled by GameMod
        }

        // Use the keyboard input encoder to get the proper sequence
        string? encoded = KeyboardInputEncoder.EncodeKeyEvent(keyString, modifiers, applicationCursorKeys);

        if (encoded != null)
        {
          markUserInput();
          SendToProcess(encoded);
          return true; // Special key was handled
        }
      }
    }

    // Handle Ctrl+letter combinations separately (only when Ctrl is pressed)
    if (modifiers.Ctrl)
    {
      var letterKeys = new[]
      {
        (ImGuiKey.A, "a"), (ImGuiKey.B, "b"), (ImGuiKey.C, "c"), (ImGuiKey.D, "d"),
        (ImGuiKey.E, "e"), (ImGuiKey.F, "f"), (ImGuiKey.G, "g"), (ImGuiKey.H, "h"),
        (ImGuiKey.I, "i"), (ImGuiKey.J, "j"), (ImGuiKey.K, "k"), (ImGuiKey.L, "l"),
        (ImGuiKey.M, "m"), (ImGuiKey.N, "n"), (ImGuiKey.O, "o"), (ImGuiKey.P, "p"),
        (ImGuiKey.Q, "q"), (ImGuiKey.R, "r"), (ImGuiKey.S, "s"), (ImGuiKey.T, "t"),
        (ImGuiKey.U, "u"), (ImGuiKey.V, "v"), (ImGuiKey.W, "w"), (ImGuiKey.X, "x"),
        (ImGuiKey.Y, "y"), (ImGuiKey.Z, "z")
      };

      foreach (var (imguiKey, keyString) in letterKeys)
      {
        if (ImGui.IsKeyPressed(imguiKey))
        {
          // Use the keyboard input encoder to get the proper Ctrl+letter sequence
          string? encoded = KeyboardInputEncoder.EncodeKeyEvent(keyString, modifiers, applicationCursorKeys);

          if (encoded != null)
          {
            markUserInput();
            SendToProcess(encoded);
            return true; // Ctrl+letter was handled
          }
        }
      }
    }

    // Handle keypad keys (minimal implementation as requested)
    if (ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
    {
      markUserInput();
      SendToProcess("\r"); // Treat keypad Enter same as regular Enter for now
      return true;
    }

    return false; // No special key was handled
  }

  /// <summary>
  ///     Handles mouse wheel input for scrolling through terminal history.
  ///     Only processes wheel events when the terminal window has focus and the wheel delta
  ///     exceeds the minimum threshold to prevent micro-movements.
  ///     Includes comprehensive error handling and input validation.
  /// </summary>
  private void HandleMouseWheelInput()
  {
    try
    {
      // Only process mouse wheel events when terminal has focus
      if (!_controller.HasFocus)
      {
        return;
      }

      var io = ImGui.GetIO();
      float wheelDelta = io.MouseWheel;

      // Check if wheel delta exceeds minimum threshold to prevent micro-movements
      if (Math.Abs(wheelDelta) < _scrollConfig.MinimumWheelDelta)
      {
        return;
      }

      // Validate wheel delta for NaN/infinity - critical for robustness
      if (!float.IsFinite(wheelDelta))
      {
        Console.WriteLine($"TerminalController: Invalid wheel delta detected (NaN/Infinity): {wheelDelta}, ignoring");

        // Reset accumulator to prevent corruption from invalid values
        _wheelAccumulator = 0.0f;
        return;
      }

      // Additional validation for extreme values that could cause issues
      if (Math.Abs(wheelDelta) > 1000.0f)
      {
        Console.WriteLine($"TerminalController: Extreme wheel delta detected: {wheelDelta}, clamping");
        wheelDelta = Math.Sign(wheelDelta) * 10.0f; // Clamp to reasonable range
      }

      // Process the wheel scroll with validated input
      ProcessMouseWheelScroll(wheelDelta);
    }
    catch (Exception ex)
    {
      // Log detailed error information for debugging
      Console.WriteLine($"TerminalController: Mouse wheel handling error: {ex.GetType().Name}: {ex.Message}");

      // Reset accumulator to prevent stuck state - critical for recovery
      _wheelAccumulator = 0.0f;

      // Log stack trace for debugging in development builds
#if DEBUG
      Console.WriteLine($"TerminalController: Stack trace: {ex.StackTrace}");
#endif
    }
  }

  /// <summary>
  ///     Processes mouse wheel scroll by accumulating wheel deltas and converting to line scrolls.
  ///     Implements smooth scrolling with fractional accumulation and overflow protection.
  ///     Integrates with ScrollbackManager for proper scrolling behavior and boundary handling.
  ///     Includes comprehensive error handling and recovery mechanisms.
  /// </summary>
  /// <param name="wheelDelta">The mouse wheel delta value from ImGui</param>
  private void ProcessMouseWheelScroll(float wheelDelta)
  {
    try
    {
      // Additional input validation - should already be done in HandleMouseWheelInput,
      // but defensive programming requires validation at each level
      if (!float.IsFinite(wheelDelta))
      {
        Console.WriteLine($"TerminalController: Invalid wheel delta in ProcessMouseWheelScroll: {wheelDelta}");
        _wheelAccumulator = 0.0f;
        return;
      }

      // Accumulate wheel delta for smooth scrolling
      _wheelAccumulator += wheelDelta * _scrollConfig.LinesPerStep;

      // Prevent accumulator overflow - critical for stability
      if (Math.Abs(_wheelAccumulator) > 100.0f)
      {
        Console.WriteLine($"TerminalController: Wheel accumulator overflow detected: {_wheelAccumulator}, clamping");
        _wheelAccumulator = Math.Sign(_wheelAccumulator) * 10.0f;
      }

      // Extract integer scroll lines
      int scrollLines = (int)Math.Floor(Math.Abs(_wheelAccumulator));
      if (scrollLines == 0)
      {
        return;
      }

      // Determine scroll direction (positive wheel delta = scroll up)
      bool scrollUp = _wheelAccumulator > 0;

      // Clamp to maximum lines per operation - prevents excessive scrolling
      scrollLines = Math.Min(scrollLines, _scrollConfig.MaxLinesPerOperation);

      var activeSession = _sessionManager.ActiveSession;
      if (activeSession == null)
      {
        _wheelAccumulator = 0.0f;
        return;
      }

      var emulator = (TerminalEmulator)activeSession.Terminal;
      var state = emulator.State;

      // Match catty-web behavior:
      // - If mouse reporting is enabled, wheel events go to the running app (PTY), not local scrollback.
      // - If alternate screen is active and mouse reporting is off, translate wheel into arrow/page keys.
      // - Otherwise, wheel scrolls local scrollback.
      if (state.IsMouseReportingEnabled)
      {
        var (x1, y1) = GetMouseCellCoordinates1Based();

        // ImGui: wheelDelta > 0 means scroll up; xterm wheel uses button 64 for up.
        string seq = MouseInputEncoder.EncodeMouseWheel(
            directionUp: scrollUp,
            x1: x1,
            y1: y1,
            shift: ImGui.GetIO().KeyShift,
            alt: ImGui.GetIO().KeyAlt,
            ctrl: ImGui.GetIO().KeyCtrl,
            sgrEncoding: state.MouseSgrEncodingEnabled
        );

        SendToProcess(seq);

        // Consume the delta since we've emitted input.
        float consumedDelta = scrollLines * (scrollUp ? 1 : -1);
        _wheelAccumulator -= consumedDelta;
        return;
      }

      if (state.IsAlternateScreenActive)
      {
        string seq = EncodeAltScreenWheelAsKeys(scrollUp, scrollLines, activeSession.Terminal.Height, state.ApplicationCursorKeys);
        if (!string.IsNullOrEmpty(seq))
        {
          SendToProcess(seq);
        }

        float consumedDelta = scrollLines * (scrollUp ? 1 : -1);
        _wheelAccumulator -= consumedDelta;
        return;
      }

      // Store current viewport state for boundary condition handling and error recovery
      var scrollbackManager = activeSession.Terminal.ScrollbackManager;
      if (scrollbackManager == null)
      {
        Console.WriteLine("TerminalController: ScrollbackManager is null, cannot process wheel scroll");
        _wheelAccumulator = 0.0f;
        return;
      }

      int previousOffset = scrollbackManager.ViewportOffset;
      bool wasAtBottom = scrollbackManager.IsAtBottom;

      // Apply scrolling via ScrollbackManager with comprehensive error handling
      try
      {
        if (scrollUp)
        {
          scrollbackManager.ScrollUp(scrollLines);
        }
        else
        {
          scrollbackManager.ScrollDown(scrollLines);
        }

        // Check if scrolling actually occurred (boundary condition handling)
        int newOffset = scrollbackManager.ViewportOffset;
        bool actuallyScrolled = (newOffset != previousOffset);

        if (!actuallyScrolled)
        {
          // Hit boundary - clear accumulator to prevent stuck scrolling
          // This is critical for user experience at scroll boundaries
          _wheelAccumulator = 0.0f;

#if DEBUG
          if (scrollUp)
          {
            // Console.WriteLine("TerminalController: Scroll up hit top boundary, clearing accumulator");
          }
          else if (!wasAtBottom)
          {
            // Console.WriteLine("TerminalController: Scroll down hit bottom boundary, clearing accumulator");
          }
#endif
        }
        else
        {
          // Successfully scrolled - consume the delta that was actually processed
          // This maintains fractional accumulation for smooth scrolling
          float consumedDelta = scrollLines * (scrollUp ? 1 : -1);
          _wheelAccumulator -= consumedDelta;

          // Clamp accumulator to prevent excessive buildup in one direction
          // This prevents issues with rapid scrolling and ensures responsive reversal
          float maxAccumulator = _scrollConfig.LinesPerStep * 2.0f;
          if (Math.Abs(_wheelAccumulator) > maxAccumulator)
          {
            _wheelAccumulator = Math.Sign(_wheelAccumulator) * maxAccumulator;
          }
        }
      }
      catch (Exception ex)
      {
        // Catch any errors during scrolling and reset to safe state
        Console.WriteLine($"TerminalController: Error during scrollback scroll: {ex.GetType().Name}: {ex.Message}");
        _wheelAccumulator = 0.0f;

        // Attempt to recover to bottom position (most common expected state)
        try
        {
          scrollbackManager?.ScrollToBottom();
        }
        catch
        {
          // If recovery fails, just continue - we've already logged the error
        }
      }
    }
    catch (Exception ex)
    {
      // Outer catch for any unexpected errors in scroll processing logic itself
      Console.WriteLine($"TerminalController: Unexpected error in ProcessMouseWheelScroll: {ex.GetType().Name}: {ex.Message}");
      _wheelAccumulator = 0.0f;

#if DEBUG
      Console.WriteLine($"TerminalController: Stack trace: {ex.StackTrace}");
#endif
    }
  }

  /// <summary>
  ///     Converts mouse coordinates to terminal cell coordinates (1-based for mouse reporting).
  /// </summary>
  private (int x1, int y1) GetMouseCellCoordinates1Based()
  {
    // Mouse position is in screen coordinates.
    var mouse = ImGui.GetMousePos();

    float relX = mouse.X - _controller._lastTerminalOrigin.X;
    float relY = mouse.Y - _controller._lastTerminalOrigin.Y;

    int col0 = (int)Math.Floor(relX / Math.Max(1e-6f, _controller.CurrentCharacterWidth));
    int row0 = (int)Math.Floor(relY / Math.Max(1e-6f, _controller.CurrentLineHeight));

    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null) return (1, 1);

    col0 = Math.Max(0, Math.Min(activeSession.Terminal.Width - 1, col0));
    row0 = Math.Max(0, Math.Min(activeSession.Terminal.Height - 1, row0));

    return (col0 + 1, row0 + 1);
  }

  /// <summary>
  ///     Encodes mouse wheel scrolling in alternate screen mode as arrow or page keys.
  /// </summary>
  private static string EncodeAltScreenWheelAsKeys(bool directionUp, int lines, int rows, bool applicationCursorKeys)
  {
    if (lines <= 0)
    {
      return string.Empty;
    }

    rows = Math.Max(1, rows);

    // If the wheel delta is effectively a full page, use PageUp/PageDown.
    if (lines >= rows)
    {
      int pages = Math.Max(1, Math.Min(10, (int)Math.Round(lines / (double)rows)));
      string seq = directionUp ? "\x1b[5~" : "\x1b[6~";
      return string.Concat(Enumerable.Repeat(seq, pages));
    }

    int absLines = Math.Max(1, Math.Min(rows * 3, lines));
    string arrow = directionUp
        ? (applicationCursorKeys ? "\x1bOA" : "\x1b[A")
        : (applicationCursorKeys ? "\x1bOB" : "\x1b[B");
    return string.Concat(Enumerable.Repeat(arrow, absLines));
  }

  /// <summary>
  ///     Sends text to the shell process.
  /// </summary>
  private void SendToProcess(string text)
  {
    _controller.SendToProcess(text);
  }
}
