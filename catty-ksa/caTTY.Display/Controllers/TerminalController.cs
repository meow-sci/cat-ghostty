using System.Diagnostics;
using System.Reflection;
using System.Text;
using Brutal.ImGuiApi;
using caTTY.Core.Input;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Core.Types;
using caTTY.Core.Utils;
using caTTY.Display.Configuration;
using caTTY.Display.Input;
using caTTY.Display.Rendering;
using caTTY.Display.Types;
using caTTY.Display.Utils;
using KSA;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Display.Controllers;

/// <summary>
///     ImGui terminal controller that handles display and input for the terminal emulator.
///     This is the shared controller implementation that is used by both the TestApp and GameMod.
/// </summary>
public class TerminalController : ITerminalController
{
  private readonly TerminalRenderingConfig _config;
  private TerminalFontConfig _fontConfig;
  private MouseWheelScrollConfig _scrollConfig;

  // Input handling
  private readonly StringBuilder _inputBuffer = new();
  private readonly IProcessManager _processManager;
  private readonly ITerminalEmulator _terminal;
  private bool _disposed;

  // Mouse tracking infrastructure
  private readonly MouseTrackingManager _mouseTrackingManager;
  private readonly MouseStateManager _mouseStateManager;
  private readonly CoordinateConverter _coordinateConverter;
  private readonly MouseEventProcessor _mouseEventProcessor;
  private readonly MouseInputHandler _mouseInputHandler;

  // Cursor rendering
  private readonly CursorRenderer _cursorRenderer = new();

  // Mouse wheel scrolling
  private float _wheelAccumulator = 0.0f;

  // Selection state
  private TextSelection _currentSelection = TextSelection.None;
  private bool _isSelecting = false;
  private SelectionPosition _selectionStartPosition;

  // Cached terminal rect for mouse position -> cell coordinate conversion
  private float2 _lastTerminalOrigin;
  private float2 _lastTerminalSize;

  // Window resize detection
  private float2 _lastWindowSize = new(0, 0);
  private bool _windowSizeInitialized = false;
  private DateTime _lastResizeTime = DateTime.MinValue;
  private const float RESIZE_DEBOUNCE_SECONDS = 0.1f; // Debounce rapid resize events

  // Font pointers for different styles
  private ImFontPtr _regularFont;
  private ImFontPtr _boldFont;
  private ImFontPtr _italicFont;
  private ImFontPtr _boldItalicFont;

  // Font loading state
  private bool _fontsLoaded = false;

  // Font and rendering settings (now config-based)
  private bool _isVisible = true;

  private static int _numFocusedTerminals = 0;
  private static int _numVisibleTerminals = 0;

    /// <summary>
  ///     Gets whether any terminal is visible and focused
  /// </summary>
  public static bool IsAnyTerminalActive => _numFocusedTerminals > 0 && _numVisibleTerminals > 0;


  /// <summary>
  ///     Creates a new terminal controller with default configuration.
  ///     This constructor maintains backward compatibility.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager)
      : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig(), FontContextDetector.DetectAndCreateConfig(), MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified rendering configuration.
  ///     Uses automatic font detection for font configuration and default scroll configuration.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
      TerminalRenderingConfig config)
      : this(terminal, processManager, config, FontContextDetector.DetectAndCreateConfig(), MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified font configuration.
  ///     Uses automatic DPI detection for rendering configuration and default scroll configuration.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="fontConfig">The font configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
      TerminalFontConfig fontConfig)
      : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig(), fontConfig, MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified configurations.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  /// <param name="fontConfig">The font configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
      TerminalRenderingConfig config, TerminalFontConfig fontConfig)
      : this(terminal, processManager, config, fontConfig, MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified scroll configuration.
  ///     Uses automatic detection for rendering and font configurations.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="scrollConfig">The mouse wheel scroll configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
      MouseWheelScrollConfig scrollConfig)
      : this(terminal, processManager, DpiContextDetector.DetectAndCreateConfig(), FontContextDetector.DetectAndCreateConfig(), scrollConfig)
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified configurations.
  /// </summary>
  /// <param name="terminal">The terminal emulator instance</param>
  /// <param name="processManager">The process manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  /// <param name="fontConfig">The font configuration to use</param>
  /// <param name="scrollConfig">The mouse wheel scroll configuration to use</param>
  public TerminalController(ITerminalEmulator terminal, IProcessManager processManager,
      TerminalRenderingConfig config, TerminalFontConfig fontConfig, MouseWheelScrollConfig scrollConfig)
  {
    _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    _processManager = processManager ?? throw new ArgumentNullException(nameof(processManager));
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _fontConfig = fontConfig ?? throw new ArgumentNullException(nameof(fontConfig));
    _scrollConfig = scrollConfig ?? throw new ArgumentNullException(nameof(scrollConfig));

    // Validate configurations
    _config.Validate();
    _fontConfig.Validate();
    _scrollConfig.Validate();

    // Initialize mouse tracking infrastructure
    _mouseTrackingManager = new MouseTrackingManager();
    _mouseStateManager = new MouseStateManager();
    _coordinateConverter = new CoordinateConverter();
    _mouseEventProcessor = new MouseEventProcessor(_mouseTrackingManager, _mouseStateManager);
    _mouseInputHandler = new MouseInputHandler(_mouseEventProcessor, _coordinateConverter, _mouseStateManager, _mouseTrackingManager);

    // Wire up mouse event handlers
    _mouseEventProcessor.MouseEventGenerated += OnMouseEventGenerated;
    _mouseEventProcessor.LocalMouseEvent += OnLocalMouseEvent;
    _mouseEventProcessor.ProcessingError += OnMouseProcessingError;
    _mouseInputHandler.InputError += OnMouseInputError;

    // Note: Font loading is deferred until first render call when ImGui context is ready
    // LoadFonts(); // Moved to EnsureFontsLoaded()

    // Calculate character metrics will be done after fonts are loaded
    // CalculateCharacterMetrics(); // Moved to EnsureFontsLoaded()

    // Apply configuration to rendering metrics
    CurrentFontSize = _fontConfig.FontSize;

    // Log configuration for debugging
    // LogConfiguration();
    // LogFontConfiguration();

    // Subscribe to terminal events
    _terminal.ScreenUpdated += OnScreenUpdated;
    _terminal.ResponseEmitted += OnResponseEmitted;

    // Initialize cursor style to theme default
    ResetCursorToThemeDefaults();
  }

  /// <summary>
  ///     Gets the current font size for debugging purposes.
  /// </summary>
  public float CurrentFontSize { get; private set; }

  /// <summary>
  ///     Gets the current character width for debugging purposes.
  /// </summary>
  public float CurrentCharacterWidth { get; private set; }

  /// <summary>
  ///     Gets the current line height for debugging purposes.
  /// </summary>
  public float CurrentLineHeight { get; private set; }

  /// <summary>
  ///     Gets the current DPI scaling factor for debugging purposes.
  /// </summary>
  public float CurrentDpiScalingFactor => _config.DpiScalingFactor;

  /// <summary>
  ///     Gets the current font configuration for debugging purposes.
  /// </summary>
  public TerminalFontConfig CurrentFontConfig => _fontConfig;

  /// <summary>
  ///     Gets the current regular font name for debugging purposes.
  /// </summary>
  public string CurrentRegularFontName => _fontConfig.RegularFontName;

  /// <summary>
  ///     Gets the current bold font name for debugging purposes.
  /// </summary>
  public string CurrentBoldFontName => _fontConfig.BoldFontName;

  /// <summary>
  ///     Gets the current italic font name for debugging purposes.
  /// </summary>
  public string CurrentItalicFontName => _fontConfig.ItalicFontName;

  /// <summary>
  ///     Gets the current bold+italic font name for debugging purposes.
  /// </summary>
  public string CurrentBoldItalicFontName => _fontConfig.BoldItalicFontName;

  /// <summary>
  ///     Gets or sets whether the terminal window is visible.
  /// </summary>
  public bool IsVisible
  {
    get => _isVisible;
    set
    {
      _isVisible = value;
      if (value)
      {
        _numVisibleTerminals++;
      }
      else
      {
        _numVisibleTerminals = Math.Max(0, _numVisibleTerminals - 1);
      }
    }
  }

  /// <summary>
  ///     Gets whether the terminal window currently has focus.
  /// </summary>
  public bool HasFocus { get; private set; }

  /// <summary>
  ///     Gets whether the terminal window was focused in the previous frame.
  ///     Used for detecting focus state changes.
  /// </summary>
  private bool _wasFocusedLastFrame = false;

  /// <summary>
  ///     Gets whether input capture is currently active.
  ///     When true, terminal should suppress game hotkeys bound to typing.
  /// </summary>
  public bool IsInputCaptureActive => HasFocus && IsVisible;

  /// <summary>
  ///     Event raised when the terminal focus state changes.
  /// </summary>
  public event EventHandler<FocusChangedEventArgs>? FocusChanged;

  /// <summary>
  ///     Event raised when user input should be sent to the process.
  /// </summary>
  public event EventHandler<DataInputEventArgs>? DataInput;

  /// <summary>
  ///     Renders the terminal window using ImGui.
  /// </summary>
  public void Render()
  {
    if (!_isVisible)
    {
      return;
    }

    // Ensure fonts are loaded before rendering (deferred loading)
    EnsureFontsLoaded();

    // Push monospace font if available
    PushMonospaceFont(out bool fontUsed);

    try
    {
      // Create terminal window
      ImGui.Begin("Terminal", ref _isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

      // Track focus state and detect changes
      bool currentFocus = ImGui.IsWindowFocused();
      UpdateFocusState(currentFocus);

      // CRITICAL: Manage ImGui input capture based on terminal focus
      // This ensures the game doesn't process keyboard input when terminal is focused
      ManageInputCapture();

      // Handle window resize detection and terminal resizing
      HandleWindowResize();

      // Display terminal info
      ImGui.Text($"Terminal: {_terminal.Width}x{_terminal.Height}");
      ImGui.SameLine();
      ImGui.Text($"Cursor: ({_terminal.Cursor.Row}, {_terminal.Cursor.Col})");
      ImGui.SameLine();
      ImGui.Text(
          $"Process: {(_processManager.IsRunning ? $"Running (PID: {_processManager.ProcessId})" : "Stopped")}");

      if (_processManager.ExitCode.HasValue)
      {
        ImGui.SameLine();
        ImGui.Text($"Exit Code: {_processManager.ExitCode}");
      }

      ImGui.Separator();

      // Render terminal content
      RenderTerminalContent();

      // Render focus indicators
      RenderFocusIndicators();

      // Handle input if focused
      if (HasFocus)
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
  ///     Updates the terminal controller state.
  ///     This method handles time-based updates like cursor blinking.
  /// </summary>
  /// <param name="deltaTime">Time elapsed since last update in seconds</param>
  public void Update(float deltaTime)
  {
    // Update cursor blink state using theme configuration
    int blinkInterval = ThemeManager.GetCursorBlinkInterval();
    _cursorRenderer.UpdateBlinkState(blinkInterval);
  }

  /// <summary>
  ///     Disposes the terminal controller and cleans up resources.
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

  /// <summary>
  ///     Updates the rendering configuration at runtime.
  /// </summary>
  /// <param name="newConfig">The new configuration to apply</param>
  /// <exception cref="ArgumentNullException">Thrown when newConfig is null</exception>
  /// <exception cref="ArgumentException">Thrown when newConfig contains invalid values</exception>
  public void UpdateRenderingConfig(TerminalRenderingConfig newConfig)
  {
    if (newConfig == null)
    {
      throw new ArgumentNullException(nameof(newConfig));
    }

    // Validate the new configuration
    newConfig.Validate();

    // Apply the new metrics
    CurrentFontSize = newConfig.FontSize;
    CurrentCharacterWidth = newConfig.CharacterWidth;
    CurrentLineHeight = newConfig.LineHeight;

    // Log the configuration change
    // Console.WriteLine("TerminalController: Runtime configuration updated");
    // LogConfiguration();
  }

  /// <summary>
  ///     Updates the font configuration at runtime.
  /// </summary>
  /// <param name="newFontConfig">The new font configuration to apply</param>
  /// <exception cref="ArgumentNullException">Thrown when newFontConfig is null</exception>
  /// <exception cref="ArgumentException">Thrown when newFontConfig contains invalid values</exception>
  public void UpdateFontConfig(TerminalFontConfig newFontConfig)
  {
    if (newFontConfig == null)
    {
      throw new ArgumentNullException(nameof(newFontConfig));
    }

    try
    {
      // Validate the new configuration before applying any changes
      newFontConfig.Validate();

      // Store current cursor position for accuracy maintenance
      ICursor cursor = _terminal.Cursor;
      int currentCursorRow = cursor.Row;
      int currentCursorCol = cursor.Col;

      // Store previous metrics for comparison logging
      float previousCharWidth = CurrentCharacterWidth;
      float previousLineHeight = CurrentLineHeight;
      float previousFontSize = CurrentFontSize;
      string previousRegularFont = _fontConfig.RegularFontName;

      // Log the configuration change attempt
      Console.WriteLine("TerminalController: Attempting runtime font configuration update");
      Console.WriteLine($"  Previous: Font={previousRegularFont}, Size={previousFontSize:F1}, CharWidth={previousCharWidth:F1}, LineHeight={previousLineHeight:F1}");
      Console.WriteLine($"  New: Font={newFontConfig.RegularFontName}, Size={newFontConfig.FontSize:F1}");

      // Update font configuration
      _fontConfig = newFontConfig;

      // Reset font loading state to trigger reload
      _fontsLoaded = false;

      // Reload fonts from ImGui font system immediately
      LoadFonts();

      // Recalculate character metrics based on new fonts immediately
      CalculateCharacterMetrics();

      // Update font size immediately
      CurrentFontSize = _fontConfig.FontSize;

      // Verify cursor position accuracy after font changes
      // The cursor position in terminal coordinates should remain the same,
      // but the pixel position will change based on new character metrics
      ICursor updatedCursor = _terminal.Cursor;
      bool cursorPositionMaintained = (updatedCursor.Row == currentCursorRow &&
                                     updatedCursor.Col == currentCursorCol);

      if (!cursorPositionMaintained)
      {
        Console.WriteLine($"TerminalController: Warning - Cursor position changed during font update. " +
                        $"Before: ({currentCursorRow}, {currentCursorCol}), After: ({updatedCursor.Row}, {updatedCursor.Col})");
      }

      // Calculate new pixel position for cursor (for logging purposes)
      float newCursorPixelX = currentCursorCol * CurrentCharacterWidth;
      float newCursorPixelY = currentCursorRow * CurrentLineHeight;

      // Log successful configuration change with detailed metrics
      Console.WriteLine("TerminalController: Runtime font configuration updated successfully");
      Console.WriteLine($"  Applied: Font={_fontConfig.RegularFontName}, Size={CurrentFontSize:F1}, CharWidth={CurrentCharacterWidth:F1}, LineHeight={CurrentLineHeight:F1}");
      Console.WriteLine($"  Cursor position maintained: {cursorPositionMaintained} at terminal coords ({currentCursorRow}, {currentCursorCol})");
      Console.WriteLine($"  New cursor pixel position: ({newCursorPixelX:F1}, {newCursorPixelY:F1})");
      Console.WriteLine($"  Metrics change: CharWidth {previousCharWidth:F1} -> {CurrentCharacterWidth:F1} ({(CurrentCharacterWidth - previousCharWidth):+F1;-F1;0})");
      Console.WriteLine($"  Metrics change: LineHeight {previousLineHeight:F1} -> {CurrentLineHeight:F1} ({(CurrentLineHeight - previousLineHeight):+F1;-F1;0})");

      // Log detailed font configuration for debugging
      LogFontConfiguration();
    }
    catch (ArgumentException ex)
    {
      // Log validation failure and re-throw
      Console.WriteLine($"TerminalController: Font configuration validation failed: {ex.Message}");
      throw;
    }
    catch (Exception ex)
    {
      // Log unexpected errors during font configuration update
      Console.WriteLine($"TerminalController: Unexpected error during font configuration update: {ex.Message}");
      Console.WriteLine($"TerminalController: Font configuration may be in an inconsistent state");

      // Re-throw the exception to notify caller of the failure
      throw new InvalidOperationException($"Failed to update font configuration: {ex.Message}", ex);
    }
  }

  /// <summary>
  ///     Updates the mouse wheel scroll configuration at runtime.
  ///     Includes comprehensive error handling and validation.
  /// </summary>
  /// <param name="newScrollConfig">The new scroll configuration to apply</param>
  /// <exception cref="ArgumentNullException">Thrown when newScrollConfig is null</exception>
  /// <exception cref="ArgumentException">Thrown when newScrollConfig contains invalid values</exception>
  public void UpdateScrollConfig(MouseWheelScrollConfig newScrollConfig)
  {
    if (newScrollConfig == null)
    {
      throw new ArgumentNullException(nameof(newScrollConfig));
    }

    try
    {
      // Validate the new configuration before applying any changes
      newScrollConfig.Validate();

      // Log the configuration change attempt
      Console.WriteLine("TerminalController: Attempting runtime scroll configuration update");
      Console.WriteLine($"  Previous: {_scrollConfig}");
      Console.WriteLine($"  New: {newScrollConfig}");

      // Reset wheel accumulator when changing configuration to prevent inconsistent state
      float previousAccumulator = _wheelAccumulator;
      _wheelAccumulator = 0.0f;

      // Update scroll configuration
      _scrollConfig = newScrollConfig;

      // Log successful configuration change
      Console.WriteLine("TerminalController: Runtime scroll configuration updated successfully");
      Console.WriteLine($"  Applied: {_scrollConfig}");
      Console.WriteLine($"  Wheel accumulator reset from {previousAccumulator:F3} to 0.0");
    }
    catch (ArgumentException ex)
    {
      // Log validation failure and re-throw with additional context
      Console.WriteLine($"TerminalController: Scroll configuration validation failed: {ex.Message}");
      Console.WriteLine($"  Attempted config: {newScrollConfig}");
      Console.WriteLine($"  Current config preserved: {_scrollConfig}");
      throw;
    }
    catch (Exception ex)
    {
      // Log unexpected errors during scroll configuration update
      Console.WriteLine($"TerminalController: Unexpected error during scroll configuration update: {ex.GetType().Name}: {ex.Message}");
      Console.WriteLine($"  Attempted config: {newScrollConfig}");
      Console.WriteLine($"  Current config preserved: {_scrollConfig}");

#if DEBUG
      Console.WriteLine($"TerminalController: UpdateScrollConfig error stack trace: {ex.StackTrace}");
#endif

      // Re-throw the exception wrapped in InvalidOperationException to provide more context
      throw new InvalidOperationException($"Failed to update scroll configuration: {ex.Message}", ex);
    }
  }

  /// <summary>
  ///     Manages ImGui input capture based on terminal focus state.
  ///     This is critical for preventing game hotkeys from being processed when terminal is focused.
  /// </summary>
  private void ManageInputCapture()
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
      if (IsInputCaptureActive)
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
  ///     Updates the focus state and handles focus change events.
  ///     Provides visual focus indicators and manages input capture priority.
  /// </summary>
  /// <param name="currentFocus">The current focus state from ImGui</param>
  private void UpdateFocusState(bool currentFocus)
  {
    bool focusChanged = currentFocus != _wasFocusedLastFrame;

    if (focusChanged)
    {
      if (currentFocus)
      {
        _numFocusedTerminals++;
      }
      else
      {
        _numFocusedTerminals = Math.Max(0, _numFocusedTerminals - 1);
      }

      // Log focus changes for debugging
      Console.WriteLine($"TerminalController: Focus changed from {_wasFocusedLastFrame} to {currentFocus}");

      // Update focus state
      HasFocus = currentFocus;

      // Handle focus gained
      if (currentFocus && !_wasFocusedLastFrame)
      {
        OnFocusGained();
      }
      // Handle focus lost
      else if (!currentFocus && _wasFocusedLastFrame)
      {
        OnFocusLost();
      }

      // Raise focus changed event
      FocusChanged?.Invoke(this, new FocusChangedEventArgs(currentFocus, _wasFocusedLastFrame));

      _wasFocusedLastFrame = currentFocus;
    }
    else
    {
      // Update focus state even if no change (for consistency)
      HasFocus = currentFocus;
    }
  }

  /// <summary>
  ///     Handles focus gained event.
  ///     Called when the terminal window gains focus.
  /// </summary>
  private void OnFocusGained()
  {
    try
    {
      // Make cursor immediately visible when gaining focus
      _cursorRenderer.ForceVisible();

      // Clear any existing selection when gaining focus (matches TypeScript behavior)
      // This prevents stale selections from interfering with new input
      if (!_currentSelection.IsEmpty)
      {
        Console.WriteLine("TerminalController: Clearing selection on focus gained");
        ClearSelection();
      }

      // Reset cursor blink state to ensure it's visible
      _cursorRenderer.ResetBlinkState();

      Console.WriteLine("TerminalController: Terminal gained focus - input capture active");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error handling focus gained: {ex.Message}");
    }
  }

  /// <summary>
  ///     Handles focus lost event.
  ///     Called when the terminal window loses focus.
  /// </summary>
  private void OnFocusLost()
  {
    try
    {
      // Stop any ongoing selection when losing focus
      if (_isSelecting)
      {
        Console.WriteLine("TerminalController: Stopping selection on focus lost");
        _isSelecting = false;
      }

      // Reset mouse wheel accumulator to prevent stuck scrolling
      _wheelAccumulator = 0.0f;

      Console.WriteLine("TerminalController: Terminal lost focus - input capture inactive");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error handling focus lost: {ex.Message}");
    }
  }

  /// <summary>
  ///     Renders visual focus indicators for the terminal window.
  ///     Provides clear visual feedback about focus state to match TypeScript implementation.
  /// </summary>
  private void RenderFocusIndicators()
  {
    try
    {
      // Get window draw list for custom drawing
      ImDrawListPtr drawList = ImGui.GetWindowDrawList();

      // Get window bounds for focus indicator
      float2 windowMin = ImGui.GetWindowPos();
      float2 windowMax = windowMin + ImGui.GetWindowSize();

      if (HasFocus)
      {
        // Draw subtle focus border (matches TypeScript visual feedback)
        float4 focusColor = new float4(0.4f, 0.6f, 1.0f, 0.8f); // Light blue
        uint focusColorU32 = ImGui.ColorConvertFloat4ToU32(focusColor);

        // Draw thin border around window content area
        drawList.AddRect(windowMin, windowMax, focusColorU32, 0.0f, ImDrawFlags.None, 2.0f);
      }
      else
      {
        // Draw subtle unfocused border
        float4 unfocusedColor = new float4(0.3f, 0.3f, 0.3f, 0.5f); // Dark gray
        uint unfocusedColorU32 = ImGui.ColorConvertFloat4ToU32(unfocusedColor);

        // Draw thin border around window content area
        drawList.AddRect(windowMin, windowMax, unfocusedColorU32, 0.0f, ImDrawFlags.None, 1.0f);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error rendering focus indicators: {ex.Message}");
    }
  }

  /// <summary>
  ///     Determines whether the terminal should capture input based on focus and visibility.
  ///     When terminal is focused, it should suppress game hotkeys bound to typing.
  ///     When terminal is unfocused/hidden, all input should pass through to game.
  /// </summary>
  /// <returns>True if terminal should capture input, false if input should pass to game</returns>
  public bool ShouldCaptureInput()
  {
    // Terminal captures input only when both focused and visible
    // This matches the TypeScript implementation's input priority management
    return IsInputCaptureActive;
  }

  /// <summary>
  ///     Forces the terminal to gain focus.
  ///     This can be used by external systems to programmatically focus the terminal.
  /// </summary>
  public void ForceFocus()
  {
    try
    {
      // Set the window focus (ImGui will handle this on next frame)
      // This may fail in unit test environments where ImGui is not initialized
      ImGui.SetWindowFocus("Terminal");
      Console.WriteLine("TerminalController: Focus forced programmatically");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Cannot force focus - ImGui not available: {ex.Message}");
    }
  }

  /// <summary>
  ///     Ensures fonts are loaded when ImGui context is ready.
  ///     This method performs deferred font loading on first render call.
  /// </summary>
  private void EnsureFontsLoaded()
  {
    if (_fontsLoaded)
    {
      return;
    }

    try
    {
      Console.WriteLine("TerminalController: Performing deferred font loading...");

      // Load fonts from ImGui font system
      LoadFonts();

      // Calculate character metrics from loaded fonts
      CalculateCharacterMetrics();

      // Log configuration for debugging
      LogFontConfiguration();

      _fontsLoaded = true;
      Console.WriteLine("TerminalController: Deferred font loading completed successfully");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error during deferred font loading: {ex.Message}");

      // Set fallback values to prevent crashes
      CurrentCharacterWidth = _config.CharacterWidth;
      CurrentLineHeight = _config.LineHeight;

      // Mark as loaded to prevent repeated attempts
      _fontsLoaded = true;
    }
  }

  /// <summary>
  ///     Loads fonts from the ImGui font system by name.
  /// </summary>
  private void LoadFonts()
  {
    try
    {
      // Try to find fonts by name, fall back to default if not found
      var defaultFont = ImGui.GetFont();

      var regularFont = FindFont(_fontConfig.RegularFontName);
      _regularFont = regularFont.HasValue ? regularFont.Value : defaultFont;

      var boldFont = FindFont(_fontConfig.BoldFontName);
      _boldFont = boldFont.HasValue ? boldFont.Value : _regularFont;

      var italicFont = FindFont(_fontConfig.ItalicFontName);
      _italicFont = italicFont.HasValue ? italicFont.Value : _regularFont;

      var boldItalicFont = FindFont(_fontConfig.BoldItalicFontName);
      _boldItalicFont = boldItalicFont.HasValue ? boldItalicFont.Value : _regularFont;

      Console.WriteLine("TerminalController: Fonts loaded successfully");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error loading fonts: {ex.Message}");

      // Fallback to default font for all styles
      var defaultFont = ImGui.GetFont();
      _regularFont = defaultFont;
      _boldFont = defaultFont;
      _italicFont = defaultFont;
      _boldItalicFont = defaultFont;
    }
  }

  /// <summary>
  ///     Finds a font by name in the ImGui font atlas.
  /// </summary>
  /// <param name="fontName">The name of the font to find</param>
  /// <returns>The font pointer if found, null otherwise</returns>
  private ImFontPtr? FindFont(string fontName)
  {
    if (string.IsNullOrWhiteSpace(fontName))
    {
      return null;
    }

    try
    {
      // First try the standard FontManager (works in standalone apps)
      if (FontManager.Fonts.TryGetValue(fontName, out ImFontPtr fontPtr))
      {
        return fontPtr;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: FontManager.Fonts not available for '{fontName}': {ex.Message}");
    }

    try
    {
      // Try the GameMod's font loading system (works in game mod context)
      var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
      if (gameModType != null)
      {
        MethodInfo? getFontMethod = gameModType.GetMethod("GetFont", BindingFlags.Public | BindingFlags.Static);
        if (getFontMethod != null)
        {
          object? result = getFontMethod.Invoke(null, new object[] { fontName });
          if (result is ImFontPtr font)
          {
            return font;
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: GameMod font loading failed for '{fontName}': {ex.Message}");
    }

    // Try to iterate through ImGui font atlas (fallback method)
    try
    {
      var io = ImGui.GetIO();
      var fonts = io.Fonts;

      // This is a simplified approach - in a real implementation,
      // we would need to iterate through the font atlas and match names
      // For now, return null to indicate font not found
      Console.WriteLine($"TerminalController: Font '{fontName}' not found in ImGui font atlas");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error searching ImGui font atlas for '{fontName}': {ex.Message}");
    }

    return null;
  }

  /// <summary>
  ///     Calculates character metrics from the loaded fonts.
  /// </summary>
  private void CalculateCharacterMetrics()
  {
    try
    {
      // Use the regular font for metric calculations
      ImGui.PushFont(_regularFont, _fontConfig.FontSize);

      try
      {
        // Calculate character width using multiple test characters to ensure accuracy
        var testChars = new[] { 'M', 'W', '@', '#' }; // Wide characters for accurate measurement
        float maxWidth = 0.0f;

        foreach (char testChar in testChars)
        {
          var textSize = ImGui.CalcTextSize(testChar.ToString());
          maxWidth = Math.Max(maxWidth, textSize.X);
        }

        // Use the maximum width found to ensure all characters fit properly
        CurrentCharacterWidth = maxWidth;

        // Calculate line height using a standard character
        var lineSize = ImGui.CalcTextSize("M");
        CurrentLineHeight = lineSize.Y * 1.2f; // Add 20% line spacing for readability

        Console.WriteLine($"TerminalController: Calculated metrics from font - CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
      }
      finally
      {
        ImGui.PopFont();
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error calculating character metrics: {ex.Message}");

      // Fallback to DPI-based metrics from config
      CurrentCharacterWidth = _config.CharacterWidth;
      CurrentLineHeight = _config.LineHeight;

      Console.WriteLine($"TerminalController: Using fallback metrics from config - CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
    }
  }

  /// <summary>
  ///     Selects the appropriate font based on SGR attributes.
  /// </summary>
  /// <param name="attributes">The SGR attributes of the character</param>
  /// <returns>The appropriate font pointer for the attributes</returns>
  private ImFontPtr SelectFont(SgrAttributes attributes)
  {
    if (attributes.Bold && attributes.Italic)
      return _boldItalicFont;
    else if (attributes.Bold)
      return _boldFont;
    else if (attributes.Italic)
      return _italicFont;
    else
      return _regularFont;
  }

  /// <summary>
  ///     Logs the current configuration for debugging purposes.
  /// </summary>
  private void LogConfiguration()
  {
    try
    {
      Console.WriteLine(
          $"TerminalController: FontSize={CurrentFontSize:F1}, CharWidth={CurrentCharacterWidth:F1}, LineHeight={CurrentLineHeight:F1}, DpiScale={_config.DpiScalingFactor:F1}");
    }
    catch (Exception ex)
    {
      // Ignore logging failures to prevent crashes
      Debug.WriteLine($"Failed to log configuration: {ex.Message}");
    }
  }

  /// <summary>
  ///     Logs the current font configuration for debugging purposes.
  /// </summary>
  private void LogFontConfiguration()
  {
    try
    {
      Console.WriteLine($"TerminalController Font Config:");
      Console.WriteLine($"  Regular: {_fontConfig.RegularFontName}");
      Console.WriteLine($"  Bold: {_fontConfig.BoldFontName}");
      Console.WriteLine($"  Italic: {_fontConfig.ItalicFontName}");
      Console.WriteLine($"  BoldItalic: {_fontConfig.BoldItalicFontName}");
      Console.WriteLine($"  FontSize: {_fontConfig.FontSize}");
      Console.WriteLine($"  AutoDetectContext: {_fontConfig.AutoDetectContext}");
      Console.WriteLine($"  Calculated CharWidth: {CurrentCharacterWidth:F1}, LineHeight: {CurrentLineHeight:F1}");
    }
    catch (Exception ex)
    {
      // Ignore logging failures to prevent crashes
      Debug.WriteLine($"Failed to log font configuration: {ex.Message}");
    }
  }

  /// <summary>
  ///     Handles window resize detection and triggers terminal resizing when needed.
  ///     Matches the TypeScript implementation's approach of detecting display size changes
  ///     and updating both the headless terminal and the PTY process dimensions.
  /// </summary>
  private void HandleWindowResize()
  {
    try
    {
      // Get current window size (total window including title bar, borders, etc.)
      float2 currentWindowSize = ImGui.GetWindowSize();

      // Initialize window size tracking on first frame
      if (!_windowSizeInitialized)
      {
        _lastWindowSize = currentWindowSize;
        _windowSizeInitialized = true;
        return;
      }

      // Check if window size has changed significantly (avoid floating point precision issues)
      float deltaX = Math.Abs(currentWindowSize.X - _lastWindowSize.X);
      float deltaY = Math.Abs(currentWindowSize.Y - _lastWindowSize.Y);

      if (deltaX <= 1.0f && deltaY <= 1.0f)
      {
        return; // No significant size change
      }

      // Debounce rapid resize events to avoid excessive processing
      DateTime now = DateTime.Now;
      if ((now - _lastResizeTime).TotalSeconds < RESIZE_DEBOUNCE_SECONDS)
      {
        return;
      }

      // Calculate new terminal dimensions based on available space
      var newDimensions = CalculateTerminalDimensions(currentWindowSize);
      if (!newDimensions.HasValue)
      {
        return; // Invalid dimensions
      }

      var (newCols, newRows) = newDimensions.Value;

      // Check if terminal dimensions would actually change
      if (newCols == _terminal.Width && newRows == _terminal.Height)
      {
        _lastWindowSize = currentWindowSize;
        return; // No terminal dimension change needed
      }

      // Validate new dimensions are reasonable
      if (newCols < 10 || newRows < 3 || newCols > 1000 || newRows > 1000)
      {
        Console.WriteLine($"TerminalController: Invalid terminal dimensions calculated: {newCols}x{newRows}, ignoring resize");
        return;
      }

      // Log the resize operation
      // Console.WriteLine($"TerminalController: Window resized from {_lastWindowSize.X:F0}x{_lastWindowSize.Y:F0} to {currentWindowSize.X:F0}x{currentWindowSize.Y:F0}");
      // Console.WriteLine($"TerminalController: Resizing terminal from {_terminal.Width}x{_terminal.Height} to {newCols}x{newRows}");

      // Resize the headless terminal emulator (matches TypeScript StatefulTerminal behavior)
      _terminal.Resize(newCols, newRows);

      // Resize the PTY process (matches TypeScript BackendServer behavior)
      if (_processManager.IsRunning)
      {
        try
        {
          _processManager.Resize(newCols, newRows);
          // Console.WriteLine($"TerminalController: PTY process resized to {newCols}x{newRows}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Failed to resize PTY process: {ex.Message}");
          // Continue anyway - terminal emulator resize succeeded
        }
      }

      // Update tracking variables
      _lastWindowSize = currentWindowSize;
      _lastResizeTime = now;

      // Console.WriteLine($"TerminalController: Terminal resize completed successfully");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error during window resize handling: {ex.Message}");

#if DEBUG
      Console.WriteLine($"TerminalController: Resize error stack trace: {ex.StackTrace}");
#endif
    }
  }

  /// <summary>
  ///     Calculates optimal terminal dimensions based on available window space.
  ///     Uses character metrics to determine how many columns and rows can fit.
  ///     Matches the approach used in the TypeScript implementation and playground experiments.
  /// </summary>
  /// <param name="availableSize">The available window content area size</param>
  /// <returns>Terminal dimensions (cols, rows) or null if invalid</returns>
  private (int cols, int rows)? CalculateTerminalDimensions(float2 availableSize)
  {
    try
    {
      // Reserve space for UI elements (terminal info line, separator, padding)
      const float UI_OVERHEAD_HEIGHT = 60.0f; // Approximate height of info line + separator + padding
      const float PADDING_WIDTH = 20.0f; // Horizontal padding

      float availableWidth = availableSize.X - PADDING_WIDTH;
      float availableHeight = availableSize.Y - UI_OVERHEAD_HEIGHT;

      // Ensure we have positive dimensions
      if (availableWidth <= 0 || availableHeight <= 0)
      {
        return null;
      }

      // Calculate dimensions using current character metrics
      if (CurrentCharacterWidth <= 0 || CurrentLineHeight <= 0)
      {
        Console.WriteLine($"TerminalController: Invalid character metrics: width={CurrentCharacterWidth}, height={CurrentLineHeight}");
        return null;
      }

      int cols = (int)Math.Floor(availableWidth / CurrentCharacterWidth);
      int rows = (int)Math.Floor(availableHeight / CurrentLineHeight);

      // Apply reasonable bounds (matching TypeScript validation)
      cols = Math.Max(10, Math.Min(1000, cols));
      rows = Math.Max(3, Math.Min(1000, rows));

      return (cols, rows);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error calculating terminal dimensions: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  ///     Gets the current terminal dimensions for external access.
  ///     Useful for debugging and integration testing.
  /// </summary>
  /// <returns>Current terminal dimensions (width, height)</returns>
  public (int width, int height) GetTerminalDimensions()
  {
    return (_terminal.Width, _terminal.Height);
  }

  /// <summary>
  ///     Gets the current window size for debugging purposes.
  /// </summary>
  /// <returns>Current window content area size</returns>
  public float2 GetCurrentWindowSize()
  {
    if (!_windowSizeInitialized)
    {
      return new float2(0, 0);
    }
    return _lastWindowSize;
  }

  /// <summary>
  ///     Resets cursor style and blink state to theme defaults.
  ///     Called during initialization and when theme changes.
  /// </summary>
  private void ResetCursorToThemeDefaults()
  {
    try
    {
      // Get theme defaults
      CursorStyle defaultStyle = ThemeManager.GetDefaultCursorStyle();

      // Update terminal state using the new public API
      _terminal.SetCursorStyle(defaultStyle);

      // Reset cursor renderer blink state
      _cursorRenderer.ResetBlinkState();

      // Console.WriteLine($"TerminalController: Cursor reset to theme defaults - Style: {defaultStyle}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error resetting cursor to theme defaults: {ex.Message}");
    }
  }

  /// <summary>
  ///     Manually triggers a terminal resize to the specified dimensions.
  ///     This method can be used for testing or external resize requests.
  /// </summary>
  /// <param name="cols">New width in columns</param>
  /// <param name="rows">New height in rows</param>
  /// <exception cref="ArgumentException">Thrown when dimensions are invalid</exception>
  public void ResizeTerminal(int cols, int rows)
  {
    if (cols < 1 || rows < 1 || cols > 1000 || rows > 1000)
    {
      throw new ArgumentException($"Invalid terminal dimensions: {cols}x{rows}. Must be between 1x1 and 1000x1000.");
    }

    try
    {
      // Console.WriteLine($"TerminalController: Manual terminal resize requested: {cols}x{rows}");

      // Resize the headless terminal emulator
      _terminal.Resize(cols, rows);

      // Resize the PTY process if running
      if (_processManager.IsRunning)
      {
        _processManager.Resize(cols, rows);
        // Console.WriteLine($"TerminalController: PTY process resized to {cols}x{rows}");
      }

      // Console.WriteLine($"TerminalController: Manual terminal resize completed successfully");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error during manual terminal resize: {ex.Message}");
      throw new InvalidOperationException($"Failed to resize terminal to {cols}x{rows}: {ex.Message}", ex);
    }
  }

  /// <summary>
  ///     Renders the terminal screen content.
  /// </summary>
  private void RenderTerminalContent()
  {
    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
    float2 windowPos = ImGui.GetCursorScreenPos();

    // Calculate terminal area
    float terminalWidth = _terminal.Width * CurrentCharacterWidth;
    float terminalHeight = _terminal.Height * CurrentLineHeight;

    // Cache terminal rect for input encoding (mouse wheel / mouse reporting)
    _lastTerminalOrigin = windowPos;
    _lastTerminalSize = new float2(terminalWidth, terminalHeight);

    // CRITICAL: Create an invisible button that captures mouse input and prevents window dragging
    // This is the key to preventing ImGui window dragging when selecting text
    ImGui.InvisibleButton("terminal_content", new float2(terminalWidth, terminalHeight));
    bool terminalHovered = ImGui.IsItemHovered();
    bool terminalActive = ImGui.IsItemActive();

    // Get the draw position after the invisible button
    float2 terminalDrawPos = windowPos;

    // Draw terminal background using theme
    float4 terminalBg = ThemeManager.GetDefaultBackground();
    uint bgColor = ImGui.ColorConvertFloat4ToU32(terminalBg);
    var terminalRect = new float2(terminalDrawPos.X + terminalWidth, terminalDrawPos.Y + terminalHeight);
    drawList.AddRectFilled(terminalDrawPos, terminalRect, bgColor);

    // Get viewport content from ScrollbackManager instead of directly from screen buffer
    var screenBuffer = new ReadOnlyMemory<Cell>[_terminal.Height];
    for (int i = 0; i < _terminal.Height; i++)
    {
      var rowSpan = _terminal.ScreenBuffer.GetRow(i);
      var rowArray = new Cell[rowSpan.Length];
      rowSpan.CopyTo(rowArray);
      screenBuffer[i] = rowArray.AsMemory();
    }

    // Get the viewport rows that should be displayed (combines scrollback + screen buffer)
    var isAlternateScreenActive = ((TerminalEmulator)_terminal).State.IsAlternateScreenActive;
    var viewportRows = _terminal.ScrollbackManager.GetViewportRows(
        screenBuffer,
        isAlternateScreenActive,
        _terminal.Height
    );

    // Render each cell from the viewport content
    for (int row = 0; row < Math.Min(viewportRows.Count, _terminal.Height); row++)
    {
      var rowMemory = viewportRows[row];
      var rowSpan = rowMemory.Span;

      for (int col = 0; col < Math.Min(rowSpan.Length, _terminal.Width); col++)
      {
        Cell cell = rowSpan[col];
        RenderCell(drawList, terminalDrawPos, row, col, cell);
      }
    }

    // Render cursor
    RenderCursor(drawList, terminalDrawPos);

    // Handle mouse input only when the invisible button is hovered/active
    if (terminalHovered || terminalActive)
    {
      HandleMouseInputForTerminal();
    }

    // Also handle mouse tracking for applications (this works regardless of hover state)
    HandleMouseTrackingForApplications();
  }

  /// <summary>
  ///     Renders a single terminal cell.
  /// </summary>
  private void RenderCell(ImDrawListPtr drawList, float2 windowPos, int row, int col, Cell cell)
  {
    float x = windowPos.X + (col * CurrentCharacterWidth);
    float y = windowPos.Y + (row * CurrentLineHeight);
    var pos = new float2(x, y);

    // Check if this cell is selected
    bool isSelected = !_currentSelection.IsEmpty && _currentSelection.Contains(row, col);

    // Resolve colors using the new color resolution system
    float4 baseForeground = ColorResolver.Resolve(cell.Attributes.ForegroundColor, false);
    float4 baseBackground = ColorResolver.Resolve(cell.Attributes.BackgroundColor, true);

    // Apply SGR attributes to colors
    var (fgColor, bgColor) = StyleManager.ApplyAttributes(cell.Attributes, baseForeground, baseBackground);

    // Apply selection highlighting
    if (isSelected)
    {
      // Use selection colors - invert foreground and background for selected text
      var selectionBg = new float4(0.3f, 0.5f, 0.8f, 0.7f); // Semi-transparent blue
      var selectionFg = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text

      bgColor = selectionBg;
      fgColor = selectionFg;
    }

    // Always draw background
    var bgRect = new float2(x + CurrentCharacterWidth, y + CurrentLineHeight);
    drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));

    // Draw character if not space or null
    if (cell.Character != ' ' && cell.Character != '\0')
    {
      // Select appropriate font based on SGR attributes
      var font = SelectFont(cell.Attributes);

      // Draw the character with selected font using proper PushFont/PopFont pattern
      ImGui.PushFont(font, _fontConfig.FontSize);
      try
      {
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(fgColor), cell.Character.ToString());
      }
      finally
      {
        ImGui.PopFont();
      }

      // Draw underline if needed (but not for selected text to avoid visual clutter)
      if (!isSelected && StyleManager.ShouldRenderUnderline(cell.Attributes))
      {
        RenderUnderline(drawList, pos, cell.Attributes, fgColor);
      }

      // Draw strikethrough if needed (but not for selected text to avoid visual clutter)
      if (!isSelected && StyleManager.ShouldRenderStrikethrough(cell.Attributes))
      {
        RenderStrikethrough(drawList, pos, fgColor);
      }
    }
  }

  /// <summary>
  ///     Renders the terminal cursor using the new cursor rendering system.
  /// </summary>
  private void RenderCursor(ImDrawListPtr drawList, float2 windowPos)
  {
    var terminalState = ((TerminalEmulator)_terminal).State;
    ICursor cursor = _terminal.Cursor;

    // Ensure cursor position is within bounds
    int cursorCol = Math.Max(0, Math.Min(cursor.Col, _terminal.Width - 1));
    int cursorRow = Math.Max(0, Math.Min(cursor.Row, _terminal.Height - 1));

    float x = windowPos.X + (cursorCol * CurrentCharacterWidth);
    float y = windowPos.Y + (cursorRow * CurrentLineHeight);
    var cursorPos = new float2(x, y);

    // Get cursor color from theme
    float4 cursorColor = ThemeManager.GetCursorColor();

    // Check if terminal is at bottom (not scrolled back)
    var scrollbackManager = _terminal.ScrollbackManager;
    bool isAtBottom = scrollbackManager?.IsAtBottom ?? true;

    // Render cursor using the new cursor rendering system
    _cursorRenderer.RenderCursor(
        drawList,
        cursorPos,
        CurrentCharacterWidth,
        CurrentLineHeight,
        terminalState.CursorStyle,
        terminalState.CursorVisible,
        cursorColor,
        isAtBottom
    );
  }

  /// <summary>
  ///     Handles keyboard input when the terminal has focus.
  ///     Enhanced to match TypeScript implementation with comprehensive key encoding.
  ///     Integrates with game input system to manage input capture priority.
  /// </summary>
  private void HandleInput()
  {
    // Verify focus state before processing input (defensive programming)
    if (!HasFocus || !IsVisible)
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
      _terminal.ScrollbackManager?.OnUserInput();

      // Make cursor immediately visible when user provides input
      _cursorRenderer.ForceVisible();
    }

    // Handle mouse wheel input first
    HandleMouseWheelInput();

    // Get current terminal state for input encoding
    var terminalState = ((TerminalEmulator)_terminal).State;
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
  /// Handles mouse tracking for applications (separate from local selection).
  /// This method processes mouse events for terminal applications that request mouse tracking.
  /// </summary>
  private void HandleMouseTrackingForApplications()
  {
    try
    {
      // Sync mouse tracking configuration from terminal state
      SyncMouseTrackingConfiguration();

      // Only process if mouse tracking is enabled
      var config = _mouseTrackingManager.Configuration;
      if (config.Mode == MouseTrackingMode.Off)
      {
        return; // No mouse tracking requested
      }

      // Update mouse input handler with current terminal state
      _mouseInputHandler.SetTerminalFocus(HasFocus);

      // Update coordinate converter with current terminal metrics
      UpdateCoordinateConverterMetrics();

      // Update terminal size for mouse input handler
      var terminalSize = new float2(_lastTerminalSize.X, _lastTerminalSize.Y);
      _mouseInputHandler.UpdateTerminalSize(terminalSize, _terminal.Width, _terminal.Height);

      // Process mouse input through the mouse input handler
      _mouseInputHandler.HandleMouseInput();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error in mouse tracking for applications: {ex.Message}");
    }
  }

  /// <summary>
  /// Handles integrated mouse input for both application tracking and local selection.
  /// This method coordinates between mouse tracking for applications and local selection handling.
  /// </summary>
  private void HandleMouseInputIntegrated()
  {
    try
    {
      // Sync mouse tracking configuration from terminal state
      SyncMouseTrackingConfiguration();

      // Update mouse input handler with current terminal state
      _mouseInputHandler.SetTerminalFocus(HasFocus);

      // Update coordinate converter with current terminal metrics
      UpdateCoordinateConverterMetrics();

      // Update terminal size for mouse input handler
      var terminalSize = new float2(_lastTerminalSize.X, _lastTerminalSize.Y);
      _mouseInputHandler.UpdateTerminalSize(terminalSize, _terminal.Width, _terminal.Height);

      // Process mouse input through the mouse input handler
      _mouseInputHandler.HandleMouseInput();

      // Also handle local selection (existing functionality)
      // This runs after mouse tracking to allow shift-key bypass
      HandleMouseInputForTerminal();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error in integrated mouse input handling: {ex.Message}");
    }
  }

  /// <summary>
  /// Updates the coordinate converter with current terminal metrics.
  /// </summary>
  private void UpdateCoordinateConverterMetrics()
  {
    // Update coordinate converter with current font metrics
    _coordinateConverter.UpdateMetrics(
      CurrentCharacterWidth,
      CurrentLineHeight,
      _lastTerminalOrigin);
  }

  /// <summary>
  /// Synchronizes mouse tracking configuration from terminal state to mouse tracking manager.
  /// </summary>
  private void SyncMouseTrackingConfiguration()
  {
    try
    {
      var terminalState = ((TerminalEmulator)_terminal).State;

      // Convert terminal state mouse tracking bits to MouseTrackingMode
      MouseTrackingMode mode = MouseTrackingMode.Off;

      // Check bits in priority order (highest mode wins)
      if ((terminalState.MouseTrackingModeBits & 4) != 0) // 1003 bit
      {
        mode = MouseTrackingMode.Any;
      }
      else if ((terminalState.MouseTrackingModeBits & 2) != 0) // 1002 bit
      {
        mode = MouseTrackingMode.Button;
      }
      else if ((terminalState.MouseTrackingModeBits & 1) != 0) // 1000 bit
      {
        mode = MouseTrackingMode.Click;
      }

      // Only update if mode changed
      if (_mouseTrackingManager.CurrentMode != mode)
      {
        Console.WriteLine($"[DEBUG] Mouse tracking mode changed: {_mouseTrackingManager.CurrentMode} -> {mode} (bits={terminalState.MouseTrackingModeBits})");
        _mouseTrackingManager.SetTrackingMode(mode);
      }

      // Only update if SGR encoding changed
      if (_mouseTrackingManager.SgrEncodingEnabled != terminalState.MouseSgrEncodingEnabled)
      {
        Console.WriteLine($"[DEBUG] SGR encoding changed: {_mouseTrackingManager.SgrEncodingEnabled} -> {terminalState.MouseSgrEncodingEnabled}");
        _mouseTrackingManager.SetSgrEncoding(terminalState.MouseSgrEncodingEnabled);
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error syncing mouse tracking configuration: {ex.Message}");
    }
  }

  /// <summary>
  /// Handles mouse input for selection and copying.
  /// Integrates with ImGui mouse state to provide text selection functionality.
  /// </summary>
  private void HandleMouseInput()
  {
    ImGuiIOPtr io = ImGui.GetIO();

    // CRITICAL: Check if mouse is over the terminal content area first
    // This prevents window dragging when clicking in the terminal area
    var mousePos = ImGui.GetMousePos();
    bool mouseOverTerminal = IsMouseOverTerminal(mousePos);

    if (!mouseOverTerminal)
    {
      return; // Don't handle mouse input if not over terminal content
    }

    // Handle mouse button press
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
    {
      // Check if Ctrl+C is being pressed for copy operation
      if (io.KeyCtrl && !_currentSelection.IsEmpty)
      {
        CopySelectionToClipboard();
        return;
      }

      // Start new selection
      HandleSelectionMouseDown();
    }

    // Handle mouse drag for selection
    if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
    {
      HandleSelectionMouseMove();
    }

    // Handle mouse button release
    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
    {
      HandleSelectionMouseUp();
    }

    // Handle right-click for copy (alternative to Ctrl+C)
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !_currentSelection.IsEmpty)
    {
      CopySelectionToClipboard();
    }

    // Handle keyboard shortcuts for selection
    if (io.KeyCtrl)
    {
      // Ctrl+A: Select all visible content
      if (ImGui.IsKeyPressed(ImGuiKey.A))
      {
        SelectAllVisibleContent();
      }
      // Ctrl+C: Copy selection (handled above in mouse click, but also handle as pure keyboard shortcut)
      else if (ImGui.IsKeyPressed(ImGuiKey.C) && !_currentSelection.IsEmpty)
      {
        CopySelectionToClipboard();
      }
    }

    // Clear selection on Escape
    if (ImGui.IsKeyPressed(ImGuiKey.Escape))
    {
      ClearSelection();
    }
  }

  /// <summary>
  /// Selects all visible content in the terminal viewport.
  /// </summary>
  private void SelectAllVisibleContent()
  {
    if (_terminal.Height == 0 || _terminal.Width == 0)
    {
      return;
    }

    // Select from top-left to bottom-right of the visible area
    var startPos = new SelectionPosition(0, 0);
    var endPos = new SelectionPosition(_terminal.Height - 1, _terminal.Width - 1);

    _currentSelection = new TextSelection(startPos, endPos);
    _isSelecting = false;

    Console.WriteLine("TerminalController: Selected all visible content");
  }

  /// <summary>
  /// Handles mouse input only when the invisible button is hovered/active.
  /// This method contains the actual mouse input logic for text selection.
  /// This approach prevents ImGui window dragging when selecting text in the terminal.
  /// </summary>
  private void HandleMouseInputForTerminal()
  {
    ImGuiIOPtr io = ImGui.GetIO();

    // Handle mouse button press
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
    {
      // Check if Ctrl+C is being pressed for copy operation
      if (io.KeyCtrl && !_currentSelection.IsEmpty)
      {
        CopySelectionToClipboard();
        return;
      }

      // Start new selection
      HandleSelectionMouseDown();
    }

    // Handle mouse drag for selection
    if (ImGui.IsMouseDragging(ImGuiMouseButton.Left))
    {
      HandleSelectionMouseMove();
    }

    // Handle mouse button release
    if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
    {
      HandleSelectionMouseUp();
    }

    // Handle right-click for copy (alternative to Ctrl+C)
    if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) && !_currentSelection.IsEmpty)
    {
      CopySelectionToClipboard();
    }

    // Handle keyboard shortcuts for selection
    if (io.KeyCtrl)
    {
      // Ctrl+A: Select all visible content
      if (ImGui.IsKeyPressed(ImGuiKey.A))
      {
        SelectAllVisibleContent();
      }
      // Ctrl+C: Copy selection (handled above in mouse click, but also handle as pure keyboard shortcut)
      else if (ImGui.IsKeyPressed(ImGuiKey.C) && !_currentSelection.IsEmpty)
      {
        CopySelectionToClipboard();
      }
    }

    // Clear selection on Escape
    if (ImGui.IsKeyPressed(ImGuiKey.Escape))
    {
      ClearSelection();
    }
  }

  /// <summary>
  ///     Handles special key input using the keyboard input encoder.
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
      if (!HasFocus)
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

      var emulator = (TerminalEmulator)_terminal;
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
        string seq = EncodeAltScreenWheelAsKeys(scrollUp, scrollLines, _terminal.Height, state.ApplicationCursorKeys);
        if (!string.IsNullOrEmpty(seq))
        {
          SendToProcess(seq);
        }

        float consumedDelta = scrollLines * (scrollUp ? 1 : -1);
        _wheelAccumulator -= consumedDelta;
        return;
      }

      // Store current viewport state for boundary condition handling and error recovery
      var scrollbackManager = _terminal.ScrollbackManager;
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

        if (actuallyScrolled)
        {
          // Update accumulator by removing consumed delta only if scrolling occurred
          float consumedDelta = scrollLines * (scrollUp ? 1 : -1);
          _wheelAccumulator -= consumedDelta;
        }
        else
        {
          // At boundary - clear accumulator to prevent stuck state
          _wheelAccumulator = 0.0f;
        }
      }
      catch (ArgumentException ex)
      {
        // Handle invalid arguments to ScrollbackManager methods
        Console.WriteLine($"TerminalController: Invalid argument to ScrollbackManager: {ex.Message}");
        _wheelAccumulator = 0.0f;
      }
      catch (InvalidOperationException ex)
      {
        // Handle ScrollbackManager state errors
        Console.WriteLine($"TerminalController: ScrollbackManager operation error: {ex.Message}");
        _wheelAccumulator = 0.0f;
      }
      catch (NullReferenceException ex)
      {
        // Handle unexpected null references in ScrollbackManager
        Console.WriteLine($"TerminalController: Null reference in ScrollbackManager: {ex.Message}");
        _wheelAccumulator = 0.0f;
      }
      catch (Exception ex)
      {
        // Handle any other ScrollbackManager integration errors
        Console.WriteLine($"TerminalController: Unexpected ScrollbackManager error: {ex.GetType().Name}: {ex.Message}");
        _wheelAccumulator = 0.0f;

#if DEBUG
        Console.WriteLine($"TerminalController: ScrollbackManager error stack trace: {ex.StackTrace}");
#endif
      }
    }
    catch (OverflowException ex)
    {
      // Handle arithmetic overflow in accumulator calculations
      Console.WriteLine($"TerminalController: Arithmetic overflow in wheel processing: {ex.Message}");
      _wheelAccumulator = 0.0f;
    }
    catch (ArithmeticException ex)
    {
      // Handle other arithmetic errors (division by zero, etc.)
      Console.WriteLine($"TerminalController: Arithmetic error in wheel processing: {ex.Message}");
      _wheelAccumulator = 0.0f;
    }
    catch (Exception ex)
    {
      // Handle any other unexpected errors in wheel processing
      Console.WriteLine($"TerminalController: Unexpected error in mouse wheel processing: {ex.GetType().Name}: {ex.Message}");
      _wheelAccumulator = 0.0f;

#if DEBUG
      Console.WriteLine($"TerminalController: Wheel processing error stack trace: {ex.StackTrace}");
#endif
    }
  }

  private (int x1, int y1) GetMouseCellCoordinates1Based()
  {
    // Mouse position is in screen coordinates.
    var mouse = ImGui.GetMousePos();

    float relX = mouse.X - _lastTerminalOrigin.X;
    float relY = mouse.Y - _lastTerminalOrigin.Y;

    int col0 = (int)Math.Floor(relX / Math.Max(1e-6f, CurrentCharacterWidth));
    int row0 = (int)Math.Floor(relY / Math.Max(1e-6f, CurrentLineHeight));

    col0 = Math.Max(0, Math.Min(_terminal.Width - 1, col0));
    row0 = Math.Max(0, Math.Min(_terminal.Height - 1, row0));

    return (col0 + 1, row0 + 1);
  }

  /// <summary>
  /// Converts mouse coordinates to terminal cell coordinates (0-based).
  /// </summary>
  /// <returns>The cell coordinates, or null if the mouse is outside the terminal area</returns>
  private SelectionPosition? GetMouseCellCoordinates()
  {
    var mouse = ImGui.GetMousePos();

    float relX = mouse.X - _lastTerminalOrigin.X;
    float relY = mouse.Y - _lastTerminalOrigin.Y;

    // Check if mouse is within terminal bounds
    if (relX < 0 || relY < 0 || relX >= _lastTerminalSize.X || relY >= _lastTerminalSize.Y)
    {
      return null;
    }

    int col = (int)Math.Floor(relX / Math.Max(1e-6f, CurrentCharacterWidth));
    int row = (int)Math.Floor(relY / Math.Max(1e-6f, CurrentLineHeight));

    col = Math.Max(0, Math.Min(_terminal.Width - 1, col));
    row = Math.Max(0, Math.Min(_terminal.Height - 1, row));

    return new SelectionPosition(row, col);
  }

  /// <summary>
  /// Checks if the mouse is currently over the terminal content area.
  /// This is used to prevent window dragging when selecting text in the terminal.
  /// </summary>
  /// <param name="mousePos">The current mouse position in screen coordinates</param>
  /// <returns>True if the mouse is over the terminal content area, false otherwise</returns>
  private bool IsMouseOverTerminal(float2 mousePos)
  {
    float relX = mousePos.X - _lastTerminalOrigin.X;
    float relY = mousePos.Y - _lastTerminalOrigin.Y;

    // Check if mouse is within terminal bounds
    return relX >= 0 && relY >= 0 && relX < _lastTerminalSize.X && relY < _lastTerminalSize.Y;
  }

  /// <summary>
  /// Handles mouse button press for selection.
  /// </summary>
  private void HandleSelectionMouseDown()
  {
    var mousePos = GetMouseCellCoordinates();
    if (!mousePos.HasValue)
    {
      return;
    }

    // Start new selection
    _selectionStartPosition = mousePos.Value;
    _currentSelection = TextSelection.Empty(mousePos.Value.Row, mousePos.Value.Col);
    _isSelecting = true;
  }

  /// <summary>
  /// Handles mouse movement for selection.
  /// </summary>
  private void HandleSelectionMouseMove()
  {
    if (!_isSelecting)
    {
      return;
    }

    var mousePos = GetMouseCellCoordinates();
    if (!mousePos.HasValue)
    {
      return;
    }

    // Update selection to extend from start position to current mouse position
    _currentSelection = new TextSelection(_selectionStartPosition, mousePos.Value);
  }

  /// <summary>
  /// Handles mouse button release for selection.
  /// </summary>
  private void HandleSelectionMouseUp()
  {
    if (!_isSelecting)
    {
      return;
    }

    var mousePos = GetMouseCellCoordinates();
    if (mousePos.HasValue)
    {
      // Finalize selection
      _currentSelection = new TextSelection(_selectionStartPosition, mousePos.Value);
    }

    _isSelecting = false;
  }

  /// <summary>
  /// Clears the current selection.
  /// </summary>
  private void ClearSelection()
  {
    _currentSelection = TextSelection.None;
    _isSelecting = false;
  }

  /// <summary>
  /// Copies the current selection to the clipboard.
  /// </summary>
  /// <returns>True if text was copied successfully, false otherwise</returns>
  public bool CopySelectionToClipboard()
  {
    if (_currentSelection.IsEmpty)
    {
      return false;
    }

    try
    {
      // Get viewport content from ScrollbackManager
      var screenBuffer = new ReadOnlyMemory<Cell>[_terminal.Height];
      for (int i = 0; i < _terminal.Height; i++)
      {
        var rowSpan = _terminal.ScreenBuffer.GetRow(i);
        var rowArray = new Cell[rowSpan.Length];
        rowSpan.CopyTo(rowArray);
        screenBuffer[i] = rowArray.AsMemory();
      }

      var isAlternateScreenActive = ((TerminalEmulator)_terminal).State.IsAlternateScreenActive;
      var viewportRows = _terminal.ScrollbackManager.GetViewportRows(
          screenBuffer,
          isAlternateScreenActive,
          _terminal.Height
      );

      // Extract text from selection
      string selectedText = TextExtractor.ExtractText(
          _currentSelection,
          viewportRows,
          _terminal.Width,
          normalizeLineEndings: true,
          trimTrailingSpaces: true
      );

      if (string.IsNullOrEmpty(selectedText))
      {
        return false;
      }

      // Copy to clipboard
      bool success = ClipboardManager.SetText(selectedText);

      if (success)
      {
        Console.WriteLine($"TerminalController: Copied {selectedText.Length} characters to clipboard");
      }
      else
      {
        Console.WriteLine("TerminalController: Failed to copy selection to clipboard");
      }

      return success;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error copying selection to clipboard: {ex.Message}");
      return false;
    }
  }

  /// <summary>
  /// Gets the current text selection.
  /// </summary>
  /// <returns>The current selection</returns>
  public TextSelection GetCurrentSelection()
  {
    return _currentSelection;
  }

  /// <summary>
  /// Sets the current text selection.
  /// </summary>
  /// <param name="selection">The selection to set</param>
  public void SetSelection(TextSelection selection)
  {
    _currentSelection = selection;
    _isSelecting = false;
  }

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
    if (_processManager.IsRunning)
    {
      try
      {
        // Send directly to process manager (primary data path)
        _processManager.Write(text);

        // Also raise the DataInput event for external subscribers (monitoring/logging)
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        DataInput?.Invoke(this, new DataInputEventArgs(text, bytes));
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send input to process: {ex.Message}");
      }
    }
  }

  /// <summary>
  ///     Renders underline for a cell based on SGR attributes.
  /// </summary>
  private void RenderUnderline(ImDrawListPtr drawList, float2 pos, SgrAttributes attributes, float4 foregroundColor)
  {
    float4 underlineColor = StyleManager.GetUnderlineColor(attributes, foregroundColor);
    float thickness = StyleManager.GetUnderlineThickness(attributes.UnderlineStyle);

    float underlineY = pos.Y + CurrentLineHeight - 2;
    var underlineStart = new float2(pos.X, underlineY);
    var underlineEnd = new float2(pos.X + CurrentCharacterWidth, underlineY);

    switch (attributes.UnderlineStyle)
    {
      case UnderlineStyle.Single:
        drawList.AddLine(underlineStart, underlineEnd, ImGui.ColorConvertFloat4ToU32(underlineColor), thickness);
        break;

      case UnderlineStyle.Double:
        // Draw two lines for double underline
        drawList.AddLine(underlineStart, underlineEnd, ImGui.ColorConvertFloat4ToU32(underlineColor), thickness);
        var doubleStart = new float2(pos.X, underlineY - 2);
        var doubleEnd = new float2(pos.X + CurrentCharacterWidth, underlineY - 2);
        drawList.AddLine(doubleStart, doubleEnd, ImGui.ColorConvertFloat4ToU32(underlineColor), thickness);
        break;

      case UnderlineStyle.Curly:
      case UnderlineStyle.Dotted:
      case UnderlineStyle.Dashed:
        // For now, render these as single underlines (conservative approach)
        // Future enhancement could implement proper curly/dotted/dashed rendering
        drawList.AddLine(underlineStart, underlineEnd, ImGui.ColorConvertFloat4ToU32(underlineColor), thickness);
        break;
    }
  }

  /// <summary>
  ///     Renders strikethrough for a cell.
  /// </summary>
  private void RenderStrikethrough(ImDrawListPtr drawList, float2 pos, float4 foregroundColor)
  {
    float strikeY = pos.Y + (CurrentLineHeight / 2);
    var strikeStart = new float2(pos.X, strikeY);
    var strikeEnd = new float2(pos.X + CurrentCharacterWidth, strikeY);
    drawList.AddLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(foregroundColor));
  }

  /// <summary>
  ///     Pushes a monospace font if available.
  /// </summary>
  private void PushMonospaceFont(out bool fontUsed)
  {
    try
    {
      // Use the regular font from our font configuration
      ImGui.PushFont(_regularFont, _fontConfig.FontSize);
      fontUsed = true;
      return;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error pushing configured font: {ex.Message}");
    }

    // Fallback: First try the standard FontManager (works in standalone apps)
    try
    {
      if (FontManager.Fonts.TryGetValue(_fontConfig.RegularFontName, out ImFontPtr fontPtr))
      {
        ImGui.PushFont(fontPtr, _fontConfig.FontSize);
        fontUsed = true;
        return;
      }
    }
    catch (Exception ex)
    {
      // FontManager.Fonts may not be available in game mod context
      Console.WriteLine($"FontManager.Fonts not available: {ex.Message}");
    }

    // Try the GameMod's font loading system (works in game mod context)
    try
    {
      // Use reflection to call the GameMod's GetFont method
      var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
      if (gameModType != null)
      {
        MethodInfo? getFontMethod = gameModType.GetMethod("GetFont", BindingFlags.Public | BindingFlags.Static);
        if (getFontMethod != null)
        {
          object? result = getFontMethod.Invoke(null, new object[] { _fontConfig.RegularFontName });
          if (result is ImFontPtr font)
          {
            ImGui.PushFont(font, _fontConfig.FontSize);
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
    }

    fontUsed = false;
  }

  /// <summary>
  ///     Pops the font if it was pushed.
  /// </summary>
  private static void MaybePopFont(bool wasUsed)
  {
    if (wasUsed)
    {
      ImGui.PopFont();
    }
  }

  /// <summary>
  ///     Handles screen updated events from the terminal.
  /// </summary>
  private void OnScreenUpdated(object? sender, ScreenUpdatedEventArgs e)
  {
    // Screen will be redrawn on next frame
  }

  /// <summary>
  ///     Handles response emitted events from the terminal.
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
  ///     Handles mouse events that should be sent to the application as escape sequences.
  /// </summary>
  private void OnMouseEventGenerated(object? sender, MouseEventArgs e)
  {
    try
    {
      var mouseEvent = e.MouseEvent;
      var config = _mouseTrackingManager.Configuration;

      // Generate the appropriate escape sequence
      string? escapeSequence = mouseEvent.Type switch
      {
        MouseEventType.Press => EscapeSequenceGenerator.GenerateMousePress(
          mouseEvent.Button, mouseEvent.X1, mouseEvent.Y1, mouseEvent.Modifiers, config.SgrEncodingEnabled),
        MouseEventType.Release => EscapeSequenceGenerator.GenerateMouseRelease(
          mouseEvent.Button, mouseEvent.X1, mouseEvent.Y1, mouseEvent.Modifiers, config.SgrEncodingEnabled),
        MouseEventType.Motion => EscapeSequenceGenerator.GenerateMouseMotion(
          mouseEvent.Button, mouseEvent.X1, mouseEvent.Y1, mouseEvent.Modifiers, config.SgrEncodingEnabled),
        MouseEventType.Wheel => EscapeSequenceGenerator.GenerateMouseWheel(
          mouseEvent.Button == MouseButton.WheelUp, mouseEvent.X1, mouseEvent.Y1, mouseEvent.Modifiers, config.SgrEncodingEnabled),
        _ => null
      };

      if (escapeSequence != null)
      {
        // Send directly to process manager (primary data path)
        if (_processManager.IsRunning)
        {
          _processManager.Write(escapeSequence);
        }

        // Also raise the DataInput event for external subscribers (monitoring/logging)
        byte[] bytes = Encoding.UTF8.GetBytes(escapeSequence);
        DataInput?.Invoke(this, new DataInputEventArgs(escapeSequence, bytes));
      }
      else
      {
        Console.WriteLine($"[WARN] No escape sequence generated for event type: {mouseEvent.Type}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error generating mouse escape sequence: {ex.Message}");
    }
  }

  /// <summary>
  ///     Handles mouse events that should be processed locally (selection, scrolling).
  /// </summary>
  private void OnLocalMouseEvent(object? sender, MouseEventArgs e)
  {
    // Local mouse events are handled by the existing selection system
    // This is where we could extend local mouse handling if needed
    // For now, selection is handled directly in HandleMouseInputForTerminal()
  }

  /// <summary>
  ///     Handles mouse processing errors.
  /// </summary>
  private void OnMouseProcessingError(object? sender, MouseProcessingErrorEventArgs e)
  {
    Console.WriteLine($"[ERROR] Mouse processing error: {e.Message}");
    if (e.Exception != null)
    {
      Console.WriteLine($"Exception: {e.Exception}");
    }
  }

  /// <summary>
  ///     Handles mouse input errors.
  /// </summary>
  private void OnMouseInputError(object? sender, MouseInputErrorEventArgs e)
  {
    Console.WriteLine($"Mouse input error: {e.Message}");
    if (e.Exception != null)
    {
      Console.WriteLine($"Exception: {e.Exception}");
    }
  }

  /// <summary>
  ///     Handles terminal reset events by resetting cursor to theme defaults.
  ///     This method should be called when terminal reset sequences are processed.
  /// </summary>
  public void OnTerminalReset()
  {
    ResetCursorToThemeDefaults();
  }

}
