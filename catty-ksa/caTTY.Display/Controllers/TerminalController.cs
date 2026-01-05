using System.Diagnostics;
using System.IO;
using System.Linq;
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
///     Terminal-specific settings for future multi-terminal support.
///     Contains configuration options that apply to individual terminal instances.
///     Font configuration is handled separately by TerminalFontConfig.
/// </summary>
public class TerminalSettings
{
    /// <summary>Whether to show line numbers (future feature)</summary>
    public bool ShowLineNumbers { get; set; } = false;

    /// <summary>Whether to enable word wrap (future feature)</summary>
    public bool WordWrap { get; set; } = false;

    /// <summary>Terminal title for tab display</summary>
    public string Title { get; set; } = "Terminal 1";

    /// <summary>Whether this terminal instance is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Validates the terminal settings for consistency and reasonable values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when settings contain invalid values</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            throw new ArgumentException("Title cannot be null or empty");
        }
    }

    /// <summary>
    /// Creates a copy of the current settings.
    /// </summary>
    /// <returns>A new TerminalSettings instance with the same values</returns>
    public TerminalSettings Clone()
    {
        return new TerminalSettings
        {
            ShowLineNumbers = ShowLineNumbers,
            WordWrap = WordWrap,
            Title = Title,
            IsActive = IsActive
        };
    }
}

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
  private readonly SessionManager _sessionManager;
  private readonly ThemeConfiguration _themeConfig;
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
  private bool _fontResizePending = false; // Flag to trigger resize on next render frame
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

  // Terminal settings for current instance (preparation for multi-terminal support)
  private TerminalSettings _currentTerminalSettings = new();

  // Font selection state
  private string _currentFontFamily = "Hack"; // Default font family

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
  /// <param name="sessionManager">The session manager instance</param>
  public TerminalController(SessionManager sessionManager)
      : this(sessionManager, DpiContextDetector.DetectAndCreateConfig(), FontContextDetector.DetectAndCreateConfig(), MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified rendering configuration.
  ///     Uses automatic font detection for font configuration and default scroll configuration.
  /// </summary>
  /// <param name="sessionManager">The session manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  public TerminalController(SessionManager sessionManager, TerminalRenderingConfig config)
      : this(sessionManager, config, FontContextDetector.DetectAndCreateConfig(), MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified font configuration.
  ///     Uses automatic DPI detection for rendering configuration and default scroll configuration.
  /// </summary>
  /// <param name="sessionManager">The session manager instance</param>
  /// <param name="fontConfig">The font configuration to use</param>
  public TerminalController(SessionManager sessionManager, TerminalFontConfig fontConfig)
      : this(sessionManager, DpiContextDetector.DetectAndCreateConfig(), fontConfig, MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified configurations.
  /// </summary>
  /// <param name="sessionManager">The session manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  /// <param name="fontConfig">The font configuration to use</param>
  public TerminalController(SessionManager sessionManager, TerminalRenderingConfig config, TerminalFontConfig fontConfig)
      : this(sessionManager, config, fontConfig, MouseWheelScrollConfig.CreateDefault())
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified scroll configuration.
  ///     Uses automatic detection for rendering and font configurations.
  /// </summary>
  /// <param name="sessionManager">The session manager instance</param>
  /// <param name="scrollConfig">The mouse wheel scroll configuration to use</param>
  public TerminalController(SessionManager sessionManager, MouseWheelScrollConfig scrollConfig)
      : this(sessionManager, DpiContextDetector.DetectAndCreateConfig(), FontContextDetector.DetectAndCreateConfig(), scrollConfig)
  {
  }

  /// <summary>
  ///     Creates a new terminal controller with the specified configurations.
  /// </summary>
  /// <param name="sessionManager">The session manager instance</param>
  /// <param name="config">The rendering configuration to use</param>
  /// <param name="fontConfig">The font configuration to use</param>
  /// <param name="scrollConfig">The mouse wheel scroll configuration to use</param>
  public TerminalController(SessionManager sessionManager, TerminalRenderingConfig config, TerminalFontConfig fontConfig, MouseWheelScrollConfig scrollConfig)
  {
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _fontConfig = fontConfig ?? throw new ArgumentNullException(nameof(fontConfig));
    _scrollConfig = scrollConfig ?? throw new ArgumentNullException(nameof(scrollConfig));

    // Load theme configuration (includes shell settings)
    _themeConfig = ThemeConfiguration.Load();

    // Load font settings from persistent configuration and override the passed-in font config if needed
    LoadFontSettingsInConstructor();

    // Apply shell configuration to session manager
    ApplyShellConfigurationToSessionManager();

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

    // Wire up session manager events
    _sessionManager.SessionCreated += OnSessionCreated;
    _sessionManager.SessionClosed += OnSessionClosed;
    _sessionManager.ActiveSessionChanged += OnActiveSessionChanged;

    // Wire up title change events for any existing sessions
    foreach (var session in _sessionManager.Sessions)
    {
      session.TitleChanged += OnSessionTitleChanged;
    }

    // Note: Font loading is deferred until first render call when ImGui context is ready
    // LoadFonts(); // Moved to EnsureFontsLoaded()

    // Calculate character metrics will be done after fonts are loaded
    // CalculateCharacterMetrics(); // Moved to EnsureFontsLoaded()

    // Apply configuration to rendering metrics
    CurrentFontSize = _fontConfig.FontSize;

    // Log configuration for debugging
    // LogConfiguration();
    // LogFontConfiguration();

    // Subscribe to theme change events
    ThemeManager.ThemeChanged += OnThemeChanged;

    // Initialize opacity manager
    OpacityManager.Initialize();

    // Initialize cursor style to theme default
    ResetCursorToThemeDefaults();

    // Initialize current font family from configuration (now loads from persistent settings)
    InitializeCurrentFontFamily();
  }

  /// <summary>
  /// Loads font settings during constructor initialization.
  /// This is separate from LoadFontSettings() to avoid the deferred font loading flag.
  /// </summary>
  private void LoadFontSettingsInConstructor()
  {
    try
    {
      // Load fresh configuration from disk to get latest saved values
      var config = ThemeConfiguration.Load();
      
      // Console.WriteLine($"TerminalController: Constructor - Loaded config FontFamily: '{config.FontFamily}', FontSize: {config.FontSize}");
      
      // Apply saved font family if available
      if (!string.IsNullOrEmpty(config.FontFamily))
      {
        try
        {
          // Console.WriteLine($"TerminalController: Constructor - Attempting to create font config for family: '{config.FontFamily}'");
          
          // Create font configuration manually since CaTTYFontManager.CreateFontConfigForFamily is broken
          var savedFontConfig = CaTTYFontManager.CreateFontConfigForFamily(config.FontFamily, config.FontSize ?? _fontConfig.FontSize);
          
          if (savedFontConfig != null)
          {
            // Log what we're trying to load vs what we got
            // Console.WriteLine($"TerminalController: Constructor - Successfully created font config");
            // Console.WriteLine($"TerminalController: Constructor - Regular: {savedFontConfig.RegularFontName}");
            // Console.WriteLine($"TerminalController: Constructor - Bold: {savedFontConfig.BoldFontName}");
            // Console.WriteLine($"TerminalController: Constructor - Size: {savedFontConfig.FontSize}");
            
            // Store the old config for comparison
            var oldRegular = _fontConfig.RegularFontName;
            
            _fontConfig = savedFontConfig;
            _currentFontFamily = config.FontFamily;
            
            // Console.WriteLine($"TerminalController: Constructor - Font config updated from '{oldRegular}' to '{_fontConfig.RegularFontName}'");
            // Console.WriteLine($"TerminalController: Constructor - Current font family set to: '{_currentFontFamily}'");
          }
          else
          {
            // Console.WriteLine($"TerminalController: Constructor - Could not create font config for '{config.FontFamily}', keeping default");
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Constructor - FAILED to load saved font family '{config.FontFamily}': {ex.Message}");
          Console.WriteLine($"TerminalController: Constructor - Exception type: {ex.GetType().Name}");
          Console.WriteLine($"TerminalController: Constructor - Stack trace: {ex.StackTrace}");
          // Keep current font configuration on error
        }
      }
      else
      {
        // Console.WriteLine("TerminalController: Constructor - No saved font family found in config");
      }

      // Apply saved font size if available
      if (config.FontSize.HasValue)
      {
        var fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, config.FontSize.Value));
        var oldSize = _fontConfig.FontSize;
        _fontConfig.FontSize = fontSize;
        // Console.WriteLine($"TerminalController: Constructor - Font size updated from {oldSize} to {fontSize}");
      }
      else
      {
        // Console.WriteLine("TerminalController: Constructor - No saved font size found in config");
      }
      
      // Console.WriteLine($"TerminalController: Constructor - Final font config: Regular='{_fontConfig.RegularFontName}', Size={_fontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Constructor - ERROR loading font settings: {ex.Message}");
      Console.WriteLine($"TerminalController: Constructor - Exception type: {ex.GetType().Name}");
      Console.WriteLine($"TerminalController: Constructor - Stack trace: {ex.StackTrace}");
    }
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
  ///     Initializes the current font family from the font configuration.
  ///     Font settings are already loaded in the constructor, so this just sets up the current family tracking.
  /// </summary>
  private void InitializeCurrentFontFamily()
  {
    try
    {
      // Font settings were already loaded in LoadFontSettingsInConstructor()
      // Just ensure _currentFontFamily is set correctly
      if (string.IsNullOrEmpty(_currentFontFamily))
      {
        // Determine current font family from configuration using CaTTYFontManager
        var detectedFamily = CaTTYFontManager.GetCurrentFontFamily(_fontConfig);
        _currentFontFamily = detectedFamily ?? "Hack"; // Default fallback
        // Console.WriteLine($"TerminalController: Detected font family from config: {_currentFontFamily}");
      }
      else
      {
        // Console.WriteLine($"TerminalController: Using font family from constructor loading: {_currentFontFamily}");
      }
      
      // Console.WriteLine($"TerminalController: Final initialization - Font family: {_currentFontFamily}, Regular font: {_fontConfig.RegularFontName}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error initializing current font family: {ex.Message}");
      _currentFontFamily = "Hack"; // Safe fallback
    }
  }

  /// <summary>
  ///     Event raised when the terminal focus state changes.
  /// </summary>
  public event EventHandler<FocusChangedEventArgs>? FocusChanged;

  /// <summary>
  ///     Event raised when user input should be sent to the process.
  /// </summary>
  public event EventHandler<DataInputEventArgs>? DataInput;

  /// <summary>
  ///     Renders the terminal window using ImGui with multi-session UI layout.
  ///     Includes menu bar and tab area for session management.
  /// </summary>
  public void Render()
  {
    if (!_isVisible)
    {
      return;
    }

    // Ensure fonts are loaded before rendering (deferred loading)
    EnsureFontsLoaded();

    // Push UI font for menus (always Hack Regular 32.0f)
    PushUIFont(out bool uiFontUsed);

    try
    {
      // Create terminal window with menu bar and theme background
      // Set window background to theme background color with opacity
      float4 themeBg = ThemeManager.GetDefaultBackground();
      themeBg = OpacityManager.ApplyBackgroundOpacity(themeBg);
      ImGui.PushStyleColor(ImGuiCol.WindowBg, themeBg);

      ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new float2(0.0f, 0.0f));
      ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1.0f);
      var windowFlags = ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.MenuBar;
      ImGui.Begin("Terminal", ref _isVisible, windowFlags);
      ImGui.PopStyleVar();
      ImGui.PopStyleVar();

      // Track focus state and detect changes
      bool currentFocus = ImGui.IsWindowFocused();
      UpdateFocusState(currentFocus);

      // CRITICAL: Manage ImGui input capture based on terminal focus
      // This ensures the game doesn't process keyboard input when terminal is focused
      ManageInputCapture();

      // Render menu bar (uses UI font) - preserved for accessibility
      RenderMenuBar();

      // Render tab area for session management

      ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new float2(4.0f, 0.0f));
      ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new float2(4.0f, 0.0f));
      RenderTabArea();
      // ImGui.PopStyleVar();

      // Handle window resize detection and terminal resizing
      HandleWindowResize();

      // Process any pending font-triggered terminal resize
      ProcessPendingFontResize();

      // Pop UI font before rendering terminal content
      MaybePopFont(uiFontUsed);
      uiFontUsed = false; // Mark as popped

      // Render terminal canvas
      RenderTerminalCanvas();
      ImGui.PopStyleVar();
      ImGui.PopStyleVar();

      // Push UI font again for focus indicators
      PushUIFont(out uiFontUsed);

      // Render focus indicators (uses UI font)
      RenderFocusIndicators();

      // Handle input if focused
      if (HasFocus)
      {
        HandleInput();
      }

      ImGui.End();

      // Pop the window background color style
      ImGui.PopStyleColor();
    }
    finally
    {
      MaybePopFont(uiFontUsed);
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
      // Unsubscribe from session manager events
      if (_sessionManager != null)
      {
        _sessionManager.SessionCreated -= OnSessionCreated;
        _sessionManager.SessionClosed -= OnSessionClosed;
        _sessionManager.ActiveSessionChanged -= OnActiveSessionChanged;

        // Unsubscribe from all session events
        foreach (var session in _sessionManager.Sessions)
        {
          session.Terminal.ScreenUpdated -= OnScreenUpdated;
          session.Terminal.ResponseEmitted -= OnResponseEmitted;
          session.TitleChanged -= OnSessionTitleChanged;
        }
      }

      // Unsubscribe from theme change events
      ThemeManager.ThemeChanged -= OnThemeChanged;

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
      var activeSession = _sessionManager.ActiveSession;
      ICursor? cursor = activeSession?.Terminal.Cursor;
      int currentCursorRow = cursor?.Row ?? 0;
      int currentCursorCol = cursor?.Col ?? 0;

      // Store previous metrics for comparison logging
      float previousCharWidth = CurrentCharacterWidth;
      float previousLineHeight = CurrentLineHeight;
      float previousFontSize = CurrentFontSize;
      string previousRegularFont = _fontConfig.RegularFontName;

      // Check if font size is changing (for terminal resize trigger)
      bool fontSizeChanged = Math.Abs(_fontConfig.FontSize - newFontConfig.FontSize) > 0.1f;

      // Check if font family is changing (also requires terminal resize)
      bool fontFamilyChanged = previousRegularFont != newFontConfig.RegularFontName;

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

      // Check if character metrics have changed after font loading
      bool characterMetricsChanged = Math.Abs(CurrentCharacterWidth - previousCharWidth) > 0.1f ||
                                   Math.Abs(CurrentLineHeight - previousLineHeight) > 0.1f;

      // Trigger terminal resize if font size, font family, or character metrics changed
      if (fontSizeChanged || fontFamilyChanged || characterMetricsChanged)
      {
        Console.WriteLine($"TerminalController: Font configuration changed, triggering terminal resize for all sessions");
        Console.WriteLine($"  Font size changed: {fontSizeChanged}");
        Console.WriteLine($"  Font family changed: {fontFamilyChanged}");
        Console.WriteLine($"  Character metrics changed: {characterMetricsChanged}");

        // Apply font configuration to all sessions
        _sessionManager.ApplyFontConfigToAllSessions(_fontConfig);

        // Trigger terminal resize for all sessions with new character metrics
        TriggerTerminalResizeForAllSessions();
      }

      // Verify cursor position accuracy after font changes
      // The cursor position in terminal coordinates should remain the same,
      // but the pixel position will change based on new character metrics
      ICursor? updatedCursor = activeSession?.Terminal.Cursor;
      bool cursorPositionMaintained = (updatedCursor?.Row == currentCursorRow &&
                                     updatedCursor?.Col == currentCursorCol);

      if (!cursorPositionMaintained)
      {
        Console.WriteLine($"TerminalController: Warning - Cursor position changed during font update. " +
                        $"Before: ({currentCursorRow}, {currentCursorCol}), After: ({updatedCursor?.Row ?? -1}, {updatedCursor?.Col ?? -1})");
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
      Console.WriteLine($"TerminalController: Loading fonts with config - Regular: {_fontConfig.RegularFontName}, Size: {_fontConfig.FontSize}");
      
      // Try to find fonts by name, fall back to default if not found
      var defaultFont = ImGui.GetFont();

      var regularFont = FindFont(_fontConfig.RegularFontName);
      _regularFont = regularFont.HasValue ? regularFont.Value : defaultFont;
      Console.WriteLine($"TerminalController: Regular font loaded: {(regularFont.HasValue ? "Success" : "Fallback to default")}");

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
      Console.WriteLine($"TerminalController: Calculating character metrics using font size: {_fontConfig.FontSize}");
      
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
        CurrentCharacterWidth = (float)Math.Floor(maxWidth);

        // Calculate line height using a standard character
        var lineSize = ImGui.CalcTextSize("M");
        // CRITICAL FIX: Use exact font height without extra spacing to prevent gaps between rows
        // Terminal emulators should have tight line spacing with no gaps
        CurrentLineHeight = (float)Math.Round(lineSize.Y);

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
          $"TerminalController: FontSize={CurrentFontSize:F1}, CharWidth={CurrentCharacterWidth:F1}, LineHeight={CurrentLineHeight:F1}");
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
      // IMPORTANT: window resize should apply to all sessions, not just the active one.
      var (lastCols, lastRows) = _sessionManager.LastKnownTerminalDimensions;
      if (newCols == lastCols && newRows == lastRows)
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

      ApplyTerminalDimensionsToAllSessions(newCols, newRows);

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
  ///     Applies a terminal resize to all sessions.
  ///     This updates the headless terminal dimensions for every session and resizes any running PTY processes.
  /// </summary>
  /// <param name="cols">New terminal width in columns</param>
  /// <param name="rows">New terminal height in rows</param>
  internal void ApplyTerminalDimensionsToAllSessions(int cols, int rows)
  {
    // NOTE: This method is intentionally ImGui-free so it can be unit-tested.
    // Dimension validation is performed by callers (window resize/font resize/manual paths).
    try
    {
      var sessions = _sessionManager.Sessions;
      foreach (var session in sessions)
      {
        try
        {
          session.Terminal.Resize(cols, rows);
          session.UpdateTerminalDimensions(cols, rows);

          if (session.ProcessManager.IsRunning)
          {
            try
            {
              session.ProcessManager.Resize(cols, rows);
            }
            catch (Exception ex)
            {
              Console.WriteLine($"TerminalController: Failed to resize PTY process for session {session.Id}: {ex.Message}");
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Error resizing session {session.Id}: {ex.Message}");
        }
      }

      // Persist dimensions for future sessions
      _sessionManager.UpdateLastKnownTerminalDimensions(cols, rows);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error applying resize to all sessions: {ex.Message}");
    }
  }

  /// <summary>
  ///     Calculates optimal terminal dimensions based on available window space.
  ///     Uses character metrics to determine how many columns and rows can fit.
  ///     Accounts for the complete UI layout structure: menu bar, tab area, terminal info, and padding.
  ///     Matches the approach used in the TypeScript implementation and playground experiments.
  /// </summary>
  /// <param name="availableSize">The available window content area size</param>
  /// <returns>Terminal dimensions (cols, rows) or null if invalid</returns>
  private (int cols, int rows)? CalculateTerminalDimensions(float2 availableSize)
  {
    try
    {
      // Calculate UI overhead for multi-session UI layout
      // Multi-session UI includes menu bar and tab area
      float menuBarHeight = LayoutConstants.MENU_BAR_HEIGHT;     // 25.0f
      float tabAreaHeight = LayoutConstants.TAB_AREA_HEIGHT;     // 50.0f
      float windowPadding = LayoutConstants.WINDOW_PADDING * 2;  // Top and bottom padding

      float totalUIOverheadHeight = menuBarHeight + tabAreaHeight + windowPadding;

      // Debug logging for multi-session UI overhead calculation
      // Console.WriteLine($"TerminalController: Multi-session UI Overhead - Menu: {menuBarHeight}, Tab: {tabAreaHeight}, Padding: {windowPadding}, Total: {totalUIOverheadHeight}");

      float horizontalPadding = LayoutConstants.WINDOW_PADDING * 2; // Left and right padding

      float availableWidth = availableSize.X - horizontalPadding;
      float availableHeight = availableSize.Y - totalUIOverheadHeight;

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

      // Reduce rows by 1 to account for ImGui widget spacing that causes bottom clipping
      // This prevents the bottom row from being cut off due to ImGui layout overhead
      rows = Math.Max(3, rows - 1);

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
    var activeSession = _sessionManager.ActiveSession;
    return activeSession != null ? (activeSession.Terminal.Width, activeSession.Terminal.Height) : (0, 0);
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
  ///     Triggers terminal resize calculation based on current window size and updated character metrics.
  ///     This method is called when font configuration changes to ensure terminal dimensions
  ///     are recalculated with the new character metrics without requiring manual window resize.
  /// </summary>
  private void TriggerTerminalResize()
  {
    try
    {
      // Set flag to trigger resize on next render frame instead of immediately
      // This ensures we're in the proper ImGui context when calculating dimensions
      _fontResizePending = true;
      // Console.WriteLine("TerminalController: Font-triggered terminal resize scheduled for next render frame");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error scheduling font-triggered terminal resize: {ex.Message}");

#if DEBUG
      Console.WriteLine($"TerminalController: Font-triggered resize scheduling error stack trace: {ex.StackTrace}");
#endif
    }
  }

  /// <summary>
  ///     Performs the actual terminal resize calculation when font changes are pending.
  ///     Called during render frame when ImGui context is available.
  /// </summary>
  private void ProcessPendingFontResize()
  {
    if (!_fontResizePending)
      return;

    try
    {
      // Get current window size (we're now in ImGui render context)
      float2 currentWindowSize = ImGui.GetWindowSize();

      // Skip if window size is not initialized or invalid
      if (!_windowSizeInitialized || currentWindowSize.X <= 0 || currentWindowSize.Y <= 0)
      {
        Console.WriteLine("TerminalController: Cannot process pending font resize - window size not initialized or invalid");
        _fontResizePending = false; // Clear flag to avoid infinite retries
        return;
      }

      // Calculate new terminal dimensions with updated character metrics
      var newDimensions = CalculateTerminalDimensions(currentWindowSize);
      if (!newDimensions.HasValue)
      {
        Console.WriteLine("TerminalController: Cannot process pending font resize - invalid dimensions calculated");
        _fontResizePending = false; // Clear flag to avoid infinite retries
        return;
      }

      var (newCols, newRows) = newDimensions.Value;

      var activeSession = _sessionManager.ActiveSession;
      if (activeSession == null)
      {
        Console.WriteLine("TerminalController: Cannot process pending font resize - no active session");
        _fontResizePending = false;
        return;
      }

      // Check if terminal dimensions would actually change
      if (newCols == activeSession.Terminal.Width && newRows == activeSession.Terminal.Height)
      {
        Console.WriteLine($"TerminalController: Terminal dimensions unchanged ({newCols}x{newRows}), no resize needed");
        _fontResizePending = false;
        return;
      }

      // Validate new dimensions are reasonable
      if (newCols < 10 || newRows < 3 || newCols > 1000 || newRows > 1000)
      {
        Console.WriteLine($"TerminalController: Invalid terminal dimensions calculated: {newCols}x{newRows}, ignoring font-triggered resize");
        _fontResizePending = false;
        return;
      }

      // Log the resize operation
      Console.WriteLine($"TerminalController: Processing pending font resize from {activeSession.Terminal.Width}x{activeSession.Terminal.Height} to {newCols}x{newRows}");

      // Resize the headless terminal emulator
      activeSession.Terminal.Resize(newCols, newRows);

      // Persist dimensions for session metadata + future sessions
      activeSession.UpdateTerminalDimensions(newCols, newRows);
      _sessionManager.UpdateLastKnownTerminalDimensions(newCols, newRows);

      // Resize the PTY process if running
      if (activeSession.ProcessManager.IsRunning)
      {
        try
        {
          activeSession.ProcessManager.Resize(newCols, newRows);
          Console.WriteLine($"TerminalController: PTY process resized to {newCols}x{newRows}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Failed to resize PTY process during font-triggered resize: {ex.Message}");
          // Continue anyway - terminal emulator resize succeeded
        }
      }

      Console.WriteLine($"TerminalController: Font-triggered terminal resize completed successfully");
      _fontResizePending = false; // Clear the flag
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error during pending font-triggered terminal resize: {ex.Message}");
      _fontResizePending = false; // Clear flag to avoid infinite retries

#if DEBUG
      Console.WriteLine($"TerminalController: Pending font-triggered resize error stack trace: {ex.StackTrace}");
#endif
    }
  }

  /// <summary>
  ///     Triggers terminal resize for all sessions when font configuration changes.
  ///     This method ensures all sessions recalculate their dimensions with new character metrics.
  /// </summary>
  private void TriggerTerminalResizeForAllSessions()
  {
    try
    {
      // Use the last known window size instead of trying to get current window size
      // This avoids ImGui context issues when called from font configuration updates
      float2 currentWindowSize = _lastWindowSize;

      // Skip if window size is not initialized or invalid
      if (!_windowSizeInitialized || currentWindowSize.X <= 0 || currentWindowSize.Y <= 0)
      {
        Console.WriteLine("TerminalController: Cannot trigger resize for all sessions - window size not initialized or invalid");
        // Set flag to trigger resize on next render frame instead
        _fontResizePending = true;
        return;
      }

      var sessions = _sessionManager.Sessions;
      Console.WriteLine($"TerminalController: Triggering font-based resize for {sessions.Count} sessions");

      foreach (var session in sessions)
      {
        try
        {
          // Calculate new terminal dimensions with updated character metrics
          var newDimensions = CalculateTerminalDimensions(currentWindowSize);
          if (!newDimensions.HasValue)
          {
            Console.WriteLine($"TerminalController: Cannot resize session {session.Id} - invalid dimensions calculated");
            continue;
          }

          var (newCols, newRows) = newDimensions.Value;

          // Check if terminal dimensions would actually change
          if (newCols == session.Terminal.Width && newRows == session.Terminal.Height)
          {
            Console.WriteLine($"TerminalController: Session {session.Id} dimensions unchanged ({newCols}x{newRows}), no resize needed");
            continue;
          }

          // Validate new dimensions are reasonable
          if (newCols < 10 || newRows < 3 || newCols > 1000 || newRows > 1000)
          {
            Console.WriteLine($"TerminalController: Invalid terminal dimensions calculated for session {session.Id}: {newCols}x{newRows}, skipping resize");
            continue;
          }

          // Log the resize operation
          Console.WriteLine($"TerminalController: Resizing session {session.Id} from {session.Terminal.Width}x{session.Terminal.Height} to {newCols}x{newRows}");

          // Resize the headless terminal emulator
          session.Terminal.Resize(newCols, newRows);

          // Update session settings with new dimensions
          session.UpdateTerminalDimensions(newCols, newRows);

          // Resize the PTY process if running
          if (session.ProcessManager.IsRunning)
          {
            try
            {
              session.ProcessManager.Resize(newCols, newRows);
              Console.WriteLine($"TerminalController: PTY process for session {session.Id} resized to {newCols}x{newRows}");
            }
            catch (Exception ex)
            {
              Console.WriteLine($"TerminalController: Failed to resize PTY process for session {session.Id}: {ex.Message}");
              // Continue anyway - terminal emulator resize succeeded
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Error resizing session {session.Id}: {ex.Message}");
          // Continue with other sessions
        }
      }

      // Ensure newly created sessions start at the latest calculated dimensions.
      // All sessions share the same UI-space-derived size here, so updating once is sufficient.
      var active = _sessionManager.ActiveSession;
      if (active != null)
      {
        _sessionManager.UpdateLastKnownTerminalDimensions(active.Terminal.Width, active.Terminal.Height);
      }

      Console.WriteLine($"TerminalController: Font-triggered resize completed for all sessions");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error during font-triggered resize for all sessions: {ex.Message}");

#if DEBUG
      Console.WriteLine($"TerminalController: Font-triggered resize for all sessions error stack trace: {ex.StackTrace}");
#endif
    }
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

      // Update terminal state using the new public API for active session
      var activeSession = _sessionManager.ActiveSession;
      if (activeSession != null)
      {
        activeSession.Terminal.SetCursorStyle(defaultStyle);
      }

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

    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null)
    {
      throw new InvalidOperationException("No active session to resize");
    }

    try
    {
      // Console.WriteLine($"TerminalController: Manual terminal resize requested: {cols}x{rows}");

      // Resize the headless terminal emulator
      activeSession.Terminal.Resize(cols, rows);

      // Persist dimensions for session metadata + future sessions
      activeSession.UpdateTerminalDimensions(cols, rows);
      _sessionManager.UpdateLastKnownTerminalDimensions(cols, rows);

      // Resize the PTY process if running
      if (activeSession.ProcessManager.IsRunning)
      {
        activeSession.ProcessManager.Resize(cols, rows);
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
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null)
    {
      // No active session - show placeholder
      ImGui.Text("No terminal sessions. Click + to create one.");
      return;
    }

    // Push terminal content font for this rendering section
    PushTerminalContentFont(out bool terminalFontUsed);

    try
    {
      ImDrawListPtr drawList = ImGui.GetWindowDrawList();
      float2 windowPos = ImGui.GetCursorScreenPos();

      // Calculate terminal area
      float terminalWidth = activeSession.Terminal.Width * CurrentCharacterWidth;
      float terminalHeight = activeSession.Terminal.Height * CurrentLineHeight;

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

      // Note: Terminal background is now handled by ImGui window background color
      // No need to draw a separate terminal background rectangle

      // Get viewport content from ScrollbackManager instead of directly from screen buffer
      var screenBuffer = new ReadOnlyMemory<Cell>[activeSession.Terminal.Height];
      for (int i = 0; i < activeSession.Terminal.Height; i++)
      {
        var rowSpan = activeSession.Terminal.ScreenBuffer.GetRow(i);
        var rowArray = new Cell[rowSpan.Length];
        rowSpan.CopyTo(rowArray);
        screenBuffer[i] = rowArray.AsMemory();
      }

      // Get the viewport rows that should be displayed (combines scrollback + screen buffer)
      var isAlternateScreenActive = ((TerminalEmulator)activeSession.Terminal).State.IsAlternateScreenActive;
      var viewportRows = activeSession.Terminal.ScrollbackManager.GetViewportRows(
          screenBuffer,
          isAlternateScreenActive,
          activeSession.Terminal.Height
      );

      // Render each cell from the viewport content
      for (int row = 0; row < Math.Min(viewportRows.Count, activeSession.Terminal.Height); row++)
      {
        var rowMemory = viewportRows[row];
        var rowSpan = rowMemory.Span;

        for (int col = 0; col < Math.Min(rowSpan.Length, activeSession.Terminal.Width); col++)
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
    finally
    {
      MaybePopFont(terminalFontUsed);
    }
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

    // Apply foreground opacity to foreground colors and cell background opacity to background colors
    fgColor = OpacityManager.ApplyForegroundOpacity(fgColor);
    bgColor = OpacityManager.ApplyCellBackgroundOpacity(bgColor);

    // Apply selection highlighting or draw background only when needed
    if (isSelected)
    {
      // Use selection colors - invert foreground and background for selected text
      var selectionBg = new float4(0.3f, 0.5f, 0.8f, 0.7f); // Semi-transparent blue
      var selectionFg = new float4(1.0f, 1.0f, 1.0f, 1.0f); // White text

      // Apply foreground opacity to selection foreground and cell background opacity to selection background
      bgColor = OpacityManager.ApplyCellBackgroundOpacity(selectionBg);
      fgColor = OpacityManager.ApplyForegroundOpacity(selectionFg);

      // Always draw background for selected cells
      var bgRect = new float2(x + CurrentCharacterWidth, y + CurrentLineHeight);
      drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
    }
    else if (cell.Attributes.BackgroundColor.HasValue)
    {
      // Only draw background when SGR sequences have set a specific background color
      // This allows the theme background to show through for cells without explicit background colors
      var bgRect = new float2(x + CurrentCharacterWidth, y + CurrentLineHeight);
      drawList.AddRectFilled(pos, bgRect, ImGui.ColorConvertFloat4ToU32(bgColor));
    }
    // Note: When no SGR background color is set and cell is not selected,
    // the ImGui window background (theme background) will show through

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
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null) return;

    var terminalState = ((TerminalEmulator)activeSession.Terminal).State;
    ICursor cursor = activeSession.Terminal.Cursor;

    // Ensure cursor position is within bounds
    int cursorCol = Math.Max(0, Math.Min(cursor.Col, activeSession.Terminal.Width - 1));
    int cursorRow = Math.Max(0, Math.Min(cursor.Row, activeSession.Terminal.Height - 1));

    float x = windowPos.X + (cursorCol * CurrentCharacterWidth);
    float y = windowPos.Y + (cursorRow * CurrentLineHeight);
    var cursorPos = new float2(x, y);

    // Get cursor color from theme
    float4 cursorColor = ThemeManager.GetCursorColor();

    // Check if terminal is at bottom (not scrolled back)
    var scrollbackManager = activeSession.Terminal.ScrollbackManager;
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
      var activeSession = _sessionManager.ActiveSession;
      if (activeSession != null)
      {
        _mouseInputHandler.UpdateTerminalSize(terminalSize, activeSession.Terminal.Width, activeSession.Terminal.Height);
      }

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
      var activeSession = _sessionManager.ActiveSession;
      if (activeSession != null)
      {
        _mouseInputHandler.UpdateTerminalSize(terminalSize, activeSession.Terminal.Width, activeSession.Terminal.Height);
      }

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
      var activeSession = _sessionManager.ActiveSession;
      if (activeSession == null) return;

      var terminalState = ((TerminalEmulator)activeSession.Terminal).State;

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
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null || activeSession.Terminal.Height == 0 || activeSession.Terminal.Width == 0)
    {
      return;
    }

    // Select from top-left to bottom-right of the visible area
    var startPos = new SelectionPosition(0, 0);
    var endPos = new SelectionPosition(activeSession.Terminal.Height - 1, activeSession.Terminal.Width - 1);

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

    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null) return (1, 1);

    col0 = Math.Max(0, Math.Min(activeSession.Terminal.Width - 1, col0));
    row0 = Math.Max(0, Math.Min(activeSession.Terminal.Height - 1, row0));

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

    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null) return null;

    col = Math.Max(0, Math.Min(activeSession.Terminal.Width - 1, col));
    row = Math.Max(0, Math.Min(activeSession.Terminal.Height - 1, row));

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

    var activeSession = _sessionManager.ActiveSession;
    if (activeSession == null)
    {
      return false;
    }

    try
    {
      // Get viewport content from ScrollbackManager
      var screenBuffer = new ReadOnlyMemory<Cell>[activeSession.Terminal.Height];
      for (int i = 0; i < activeSession.Terminal.Height; i++)
      {
        var rowSpan = activeSession.Terminal.ScreenBuffer.GetRow(i);
        var rowArray = new Cell[rowSpan.Length];
        rowSpan.CopyTo(rowArray);
        screenBuffer[i] = rowArray.AsMemory();
      }

      var isAlternateScreenActive = ((TerminalEmulator)activeSession.Terminal).State.IsAlternateScreenActive;
      var viewportRows = activeSession.Terminal.ScrollbackManager.GetViewportRows(
          screenBuffer,
          isAlternateScreenActive,
          activeSession.Terminal.Height
      );

      // Extract text from selection
      string selectedText = TextExtractor.ExtractText(
          _currentSelection,
          viewportRows,
          activeSession.Terminal.Width,
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
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession?.ProcessManager.IsRunning == true)
    {
      try
      {
        // Send directly to process manager (primary data path)
        activeSession.ProcessManager.Write(text);

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
    underlineColor = OpacityManager.ApplyForegroundOpacity(underlineColor);
    float thickness = StyleManager.GetUnderlineThickness(attributes.UnderlineStyle);

    float underlineY = pos.Y + CurrentLineHeight - 2;
    var underlineStart = new float2(pos.X, underlineY);
    var underlineEnd = new float2(pos.X + CurrentCharacterWidth, underlineY);

    switch (attributes.UnderlineStyle)
    {
      case UnderlineStyle.Single:
        uint singleColor = ImGui.ColorConvertFloat4ToU32(underlineColor);
        float singleThickness = Math.Max(3.0f, thickness);
        drawList.AddLine(underlineStart, underlineEnd, singleColor, singleThickness);
        break;

      case UnderlineStyle.Double:
        // Draw two lines for double underline with proper spacing
        uint doubleColor = ImGui.ColorConvertFloat4ToU32(underlineColor);
        float doubleThickness = Math.Max(3.0f, thickness);

        // First line (bottom) - same position as single underline
        drawList.AddLine(underlineStart, underlineEnd, doubleColor, doubleThickness);

        // Second line (top) - spaced 4 pixels above the first for better visibility
        var doubleStart = new float2(pos.X, underlineY - 4);
        var doubleEnd = new float2(pos.X + CurrentCharacterWidth, underlineY - 4);
        drawList.AddLine(doubleStart, doubleEnd, doubleColor, doubleThickness);
        break;

      case UnderlineStyle.Curly:
        // Draw wavy line using bezier curves for a smooth curly effect
        RenderCurlyUnderline(drawList, pos, underlineColor, thickness);
        break;

      case UnderlineStyle.Dotted:
        // Draw dotted line using small segments with spacing
        RenderDottedUnderline(drawList, pos, underlineColor, thickness);
        break;

      case UnderlineStyle.Dashed:
        // Draw dashed line using longer segments with spacing
        RenderDashedUnderline(drawList, pos, underlineColor, thickness);
        break;
    }
  }

  /// <summary>
  ///     Renders strikethrough for a cell.
  /// </summary>
  private void RenderStrikethrough(ImDrawListPtr drawList, float2 pos, float4 foregroundColor)
  {
    // Apply foreground opacity to strikethrough color
    foregroundColor = OpacityManager.ApplyForegroundOpacity(foregroundColor);

    float strikeY = pos.Y + (CurrentLineHeight / 2);
    var strikeStart = new float2(pos.X, strikeY);
    var strikeEnd = new float2(pos.X + CurrentCharacterWidth, strikeY);
    drawList.AddLine(strikeStart, strikeEnd, ImGui.ColorConvertFloat4ToU32(foregroundColor));
  }

  /// <summary>
  ///     Renders a curly underline using bezier curves for smooth wavy effect.
  /// </summary>
  private void RenderCurlyUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness)
  {
    float underlineY = pos.Y + CurrentLineHeight - 2;
    uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
    float curlyThickness = Math.Max(3.0f, thickness);

    // Create a wavy line using multiple bezier curve segments with much higher amplitude
    float waveHeight = 4.0f; // Much bigger amplitude for very visible waves
    float segmentWidth = CurrentCharacterWidth / 2.0f; // 2 wave segments per character for smoother curves

    for (int i = 0; i < 2; i++)
    {
      float startX = pos.X + (i * segmentWidth);
      float endX = pos.X + ((i + 1) * segmentWidth);

      // Alternate wave direction for each segment to create continuous wave
      float controlOffset = (i % 2 == 0) ? -waveHeight : waveHeight;

      var p1 = new float2(startX, underlineY);
      var p2 = new float2(startX + segmentWidth * 0.3f, underlineY + controlOffset);
      var p3 = new float2(startX + segmentWidth * 0.7f, underlineY - controlOffset);
      var p4 = new float2(endX, underlineY);

      drawList.AddBezierCubic(p1, p2, p3, p4, color, curlyThickness);
    }
  }

  /// <summary>
  ///     Renders a dotted underline using small line segments with spacing.
  /// </summary>
  private void RenderDottedUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness)
  {
    float underlineY = pos.Y + CurrentLineHeight - 2;
    uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
    float dottedThickness = Math.Max(3.0f, thickness);

    float dotSize = 3.0f; // Increased dot size for better visibility
    float spacing = 3.0f; // Increased spacing for clearer separation
    float totalStep = dotSize + spacing;

    for (float x = pos.X; x < pos.X + CurrentCharacterWidth - dotSize; x += totalStep)
    {
      float dotEnd = Math.Min(x + dotSize, pos.X + CurrentCharacterWidth);
      var dotStart = new float2(x, underlineY);
      var dotEndPos = new float2(dotEnd, underlineY);
      drawList.AddLine(dotStart, dotEndPos, color, dottedThickness);
    }
  }

  /// <summary>
  ///     Renders a dashed underline using longer line segments with spacing.
  /// </summary>
  private void RenderDashedUnderline(ImDrawListPtr drawList, float2 pos, float4 underlineColor, float thickness)
  {
    float underlineY = pos.Y + CurrentLineHeight - 2;
    uint color = ImGui.ColorConvertFloat4ToU32(underlineColor);
    float dashedThickness = Math.Max(3.0f, thickness);

    float dashSize = 6.0f; // Increased dash length for better visibility
    float spacing = 4.0f; // Increased spacing for clearer separation
    float totalStep = dashSize + spacing;

    for (float x = pos.X; x < pos.X + CurrentCharacterWidth - dashSize; x += totalStep)
    {
      float dashEnd = Math.Min(x + dashSize, pos.X + CurrentCharacterWidth);
      var dashStart = new float2(x, underlineY);
      var dashEndPos = new float2(dashEnd, underlineY);
      drawList.AddLine(dashStart, dashEndPos, color, dashedThickness);
    }
  }

  /// <summary>
  ///     Pushes the fixed UI font (Hack Regular 32.0f) for menus, tabs, and info widgets.
  ///     This font never changes regardless of user font selection.
  /// </summary>
  private void PushUIFont(out bool fontUsed)
  {
    try
    {
      // Always use Hack Regular at 32.0f for UI elements
      if (CaTTYFontManager.LoadedFonts.TryGetValue("HackNerdFontMono-Regular", out ImFontPtr hackFont))
      {
        ImGui.PushFont(hackFont, 32.0f);
        fontUsed = true;
        return;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error pushing UI font from CaTTYFontManager: {ex.Message}");
    }

    // Fallback: Try FontManager (works in standalone apps)
    try
    {
      if (FontManager.Fonts.TryGetValue("HackNerdFontMono-Regular", out ImFontPtr fontPtr))
      {
        ImGui.PushFont(fontPtr, 32.0f);
        fontUsed = true;
        return;
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: FontManager.Fonts not available for UI font: {ex.Message}");
    }

    // Try the GameMod's font loading system (works in game mod context)
    try
    {
      var gameModType = Type.GetType("caTTY.GameMod.TerminalMod, caTTY");
      if (gameModType != null)
      {
        MethodInfo? getFontMethod = gameModType.GetMethod("GetFont", BindingFlags.Public | BindingFlags.Static);
        if (getFontMethod != null)
        {
          object? result = getFontMethod.Invoke(null, new object[] { "HackNerdFontMono-Regular" });
          if (result is ImFontPtr font)
          {
            ImGui.PushFont(font, 32.0f);
            fontUsed = true;
            return;
          }
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: GameMod font loading failed for UI font: {ex.Message}");
    }

    fontUsed = false;
  }

  /// <summary>
  ///     Pushes a monospace font for terminal content rendering.
  ///     This uses the user-selected font and size.
  /// </summary>
  private void PushTerminalContentFont(out bool fontUsed)
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
  ///     Pushes a monospace font if available.
  ///     DEPRECATED: Use PushUIFont() for UI elements or PushTerminalContentFont() for terminal content.
  /// </summary>
  private void PushMonospaceFont(out bool fontUsed)
  {
    // For backward compatibility, delegate to terminal content font
    PushTerminalContentFont(out fontUsed);
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
    // Find the session that emitted this response
    var emittingSession = _sessionManager.Sessions.FirstOrDefault(s => s.Terminal == sender);
    if (emittingSession?.ProcessManager.IsRunning == true)
    {
      try
      {
        emittingSession.ProcessManager.Write(e.ResponseData.Span);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to send terminal response to process: {ex.Message}");
      }
    }
  }

  /// <summary>
  ///     Handles theme change events from the ThemeManager.
  ///     Updates cursor style and other theme-dependent settings when theme changes.
  /// </summary>
  private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
  {
    try
    {
      Console.WriteLine($"TerminalController: Theme changed from '{e.PreviousTheme.Name}' to '{e.NewTheme.Name}'");

      // Reset cursor style to match new theme defaults
      ResetCursorToThemeDefaults();

      // Force cursor to be visible immediately after theme change
      _cursorRenderer.ForceVisible();

      // Reset cursor blink state to ensure proper timing with new theme
      _cursorRenderer.ResetBlinkState();

      Console.WriteLine($"TerminalController: Theme change handling completed for '{e.NewTheme.Name}'");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error handling theme change: {ex.Message}");
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
        // Send directly to active session's process manager (primary data path)
        var activeSession = _sessionManager.ActiveSession;
        if (activeSession?.ProcessManager.IsRunning == true)
        {
          activeSession.ProcessManager.Write(escapeSequence);
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
    var activeSession = _sessionManager.ActiveSession;
    if (activeSession != null)
    {
      ResetCursorToThemeDefaults();
    }
  }

  /// <summary>
  ///     Handles session creation events from the SessionManager.
  /// </summary>
  private void OnSessionCreated(object? sender, SessionCreatedEventArgs e)
  {
    // Wire up events for the new session
    var session = e.Session;
    session.Terminal.ScreenUpdated += OnScreenUpdated;
    session.Terminal.ResponseEmitted += OnResponseEmitted;
    session.TitleChanged += OnSessionTitleChanged;

    Console.WriteLine($"TerminalController: Session created - {session.Title} ({session.Id})");
  }

  /// <summary>
  ///     Handles session closure events from the SessionManager.
  /// </summary>
  private void OnSessionClosed(object? sender, SessionClosedEventArgs e)
  {
    // Unwire events for the closed session
    var session = e.Session;
    session.Terminal.ScreenUpdated -= OnScreenUpdated;
    session.Terminal.ResponseEmitted -= OnResponseEmitted;
    session.TitleChanged -= OnSessionTitleChanged;

    Console.WriteLine($"TerminalController: Session closed - {session.Title} ({session.Id})");
  }

  /// <summary>
  ///     Handles active session change events from the SessionManager.
  /// </summary>
  private void OnActiveSessionChanged(object? sender, ActiveSessionChangedEventArgs e)
  {
    Console.WriteLine($"TerminalController: Active session changed from {e.PreviousSession?.Title} to {e.NewSession?.Title}");

    // Clear any existing selection when switching sessions
    if (!_currentSelection.IsEmpty)
    {
      ClearSelection();
    }

    // Reset cursor blink state for new active session
    _cursorRenderer.ResetBlinkState();
  }

  /// <summary>
  ///     Handles session title change events from individual sessions.
  ///     This ensures the UI updates when applications like htop change the terminal title.
  /// </summary>
  private void OnSessionTitleChanged(object? sender, SessionTitleChangedEventArgs e)
  {
    // Note: No explicit UI refresh needed here since ImGui re-renders every frame
    // The tab labels will automatically show the updated session titles on next render
    Console.WriteLine($"TerminalController: Session title changed from '{e.OldTitle}' to '{e.NewTitle}'");
  }

  #region Layout Helper Methods

  /// <summary>
  /// Calculates the current tab area height based on the number of terminal instances.
  /// Uses constrained sizing to prevent excessive height growth.
  /// </summary>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1 for current single terminal)</param>
  /// <returns>Tab area height in pixels</returns>
  private static float CalculateTabAreaHeight(int tabCount = 1)
  {
    float baseHeight = LayoutConstants.MIN_TAB_AREA_HEIGHT;
    float extraHeight = Math.Max(0, (tabCount - 1) * LayoutConstants.TAB_HEIGHT_PER_EXTRA_TAB);
    return Math.Min(LayoutConstants.MAX_TAB_AREA_HEIGHT, baseHeight + extraHeight);
  }

  /// <summary>
  /// Calculates the current settings area height based on the number of control rows.
  /// Uses constrained sizing to prevent excessive height growth.
  /// </summary>
  /// <param name="controlRows">Number of control rows (defaults to 1 for basic settings)</param>
  /// <returns>Settings area height in pixels</returns>
  private static float CalculateSettingsAreaHeight(int controlRows = 1)
  {
    float baseHeight = LayoutConstants.MIN_SETTINGS_AREA_HEIGHT;
    float extraHeight = Math.Max(0, (controlRows - 1) * LayoutConstants.SETTINGS_HEIGHT_PER_CONTROL_ROW);
    return Math.Min(LayoutConstants.MAX_SETTINGS_AREA_HEIGHT, baseHeight + extraHeight);
  }

  /// <summary>
  /// Calculates the total height of all header areas (menu bar, tab area, settings area).
  /// Uses current terminal state to determine variable area heights.
  /// </summary>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Total height of header areas in pixels</returns>
  private static float CalculateHeaderHeight(int tabCount = 1, int settingsControlRows = 1)
  {
    return LayoutConstants.MENU_BAR_HEIGHT +
           CalculateTabAreaHeight(tabCount) +
           CalculateSettingsAreaHeight(settingsControlRows);
  }

  /// <summary>
  /// Calculates the minimum possible header height (all areas at minimum size).
  /// Used for minimum window size calculations and initial estimates.
  /// </summary>
  /// <returns>Minimum header height in pixels</returns>
  private static float CalculateMinHeaderHeight()
  {
    return LayoutConstants.MENU_BAR_HEIGHT +
           LayoutConstants.MIN_TAB_AREA_HEIGHT +
           LayoutConstants.MIN_SETTINGS_AREA_HEIGHT;
  }

  /// <summary>
  /// Calculates the maximum possible header height (all areas at maximum size).
  /// Used for layout validation and bounds checking.
  /// </summary>
  /// <returns>Maximum header height in pixels</returns>
  private static float CalculateMaxHeaderHeight()
  {
    return LayoutConstants.MENU_BAR_HEIGHT +
           LayoutConstants.MAX_TAB_AREA_HEIGHT +
           LayoutConstants.MAX_SETTINGS_AREA_HEIGHT;
  }

  /// <summary>
  /// Calculates the available space for the terminal canvas after accounting for header areas.
  /// Uses current header configuration for accurate space calculation.
  /// </summary>
  /// <param name="windowSize">Total window size</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Available size for terminal canvas</returns>
  private static float2 CalculateTerminalCanvasSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
  {
    float headerHeight = CalculateHeaderHeight(tabCount, settingsControlRows);
    float availableWidth = Math.Max(0, windowSize.X - LayoutConstants.WINDOW_PADDING * 2);
    float availableHeight = Math.Max(0, windowSize.Y - headerHeight - LayoutConstants.WINDOW_PADDING * 2);

    return new float2(availableWidth, availableHeight);
  }

  /// <summary>
  /// Validates that window dimensions are sufficient for the layout with current configuration.
  /// Accounts for variable header heights in validation.
  /// </summary>
  /// <param name="windowSize">Window size to validate</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>True if window size is valid for layout</returns>
  private static bool ValidateWindowSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
  {
    // Check basic minimum dimensions
    if (windowSize.X < LayoutConstants.MIN_WINDOW_WIDTH ||
        windowSize.Y < LayoutConstants.MIN_WINDOW_HEIGHT)
    {
      return false;
    }

    // Check that window can accommodate current header configuration
    float currentHeaderHeight = CalculateHeaderHeight(tabCount, settingsControlRows);
    float minRequiredHeight = currentHeaderHeight + LayoutConstants.WINDOW_PADDING * 2 + 50.0f; // 50px minimum for terminal content

    return windowSize.Y >= minRequiredHeight;
  }

  /// <summary>
  /// Calculates the position for the terminal canvas area.
  /// Accounts for current header height configuration.
  /// </summary>
  /// <param name="windowPos">Window position</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Position where terminal canvas should be rendered</returns>
  private static float2 CalculateTerminalCanvasPosition(float2 windowPos, int tabCount = 1, int settingsControlRows = 1)
  {
    float headerHeight = CalculateHeaderHeight(tabCount, settingsControlRows);
    return new float2(
      windowPos.X + LayoutConstants.WINDOW_PADDING,
      windowPos.Y + headerHeight + LayoutConstants.WINDOW_PADDING
    );
  }

  /// <summary>
  /// Calculates optimal terminal dimensions using two-pass approach for stability.
  /// Prevents sizing oscillation by using conservative estimates.
  /// </summary>
  /// <param name="windowSize">Current window size</param>
  /// <param name="charWidth">Character width in pixels</param>
  /// <param name="lineHeight">Line height in pixels</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Terminal dimensions (cols, rows) or null if invalid</returns>
  private static (int cols, int rows)? CalculateOptimalTerminalDimensions(
    float2 windowSize,
    float charWidth,
    float lineHeight,
    int tabCount = 1,
    int settingsControlRows = 1)
  {
    try
    {
      // Validate inputs
      if (charWidth <= 0 || lineHeight <= 0 || windowSize.X <= 0 || windowSize.Y <= 0)
      {
        return null;
      }

      // Pass 1: Estimate with minimum header height for conservative sizing
      float minHeaderHeight = CalculateMinHeaderHeight();
      float estimatedAvailableWidth = windowSize.X - LayoutConstants.WINDOW_PADDING * 2;
      float estimatedAvailableHeight = windowSize.Y - minHeaderHeight - LayoutConstants.WINDOW_PADDING * 2;

      if (estimatedAvailableWidth <= 0 || estimatedAvailableHeight <= 0)
      {
        return null;
      }

      int estimatedCols = (int)Math.Floor(estimatedAvailableWidth / charWidth);
      int estimatedRows = (int)Math.Floor(estimatedAvailableHeight / lineHeight);

      // Pass 2: Calculate with actual header height
      float actualHeaderHeight = CalculateHeaderHeight(tabCount, settingsControlRows);
      float actualAvailableWidth = windowSize.X - LayoutConstants.WINDOW_PADDING * 2;
      float actualAvailableHeight = windowSize.Y - actualHeaderHeight - LayoutConstants.WINDOW_PADDING * 2;

      if (actualAvailableWidth <= 0 || actualAvailableHeight <= 0)
      {
        return null;
      }

      int actualCols = (int)Math.Floor(actualAvailableWidth / charWidth);
      int actualRows = (int)Math.Floor(actualAvailableHeight / lineHeight);

      // Use the more conservative (smaller) result to prevent oscillation
      int finalCols = Math.Min(estimatedCols, actualCols);
      int finalRows = Math.Min(estimatedRows, actualRows);

      // Apply reasonable bounds
      finalCols = Math.Max(10, Math.Min(1000, finalCols));
      finalRows = Math.Max(3, Math.Min(1000, finalRows));

      return (finalCols, finalRows);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error calculating optimal terminal dimensions: {ex.Message}");
      return null;
    }
  }

  /// <summary>
  /// Gets the current terminal settings instance.
  /// </summary>
  /// <returns>Current terminal settings</returns>
  public TerminalSettings GetCurrentTerminalSettings()
  {
    return _currentTerminalSettings.Clone();
  }

  /// <summary>
  /// Updates the current terminal settings and applies changes.
  /// </summary>
  #endregion

  #region Menu Bar Rendering

  /// <summary>
  /// Renders the menu bar with File, Edit, and Font menus.
  /// Uses ImGui menu widgets to provide standard menu functionality.
  /// </summary>
  private void RenderMenuBar()
  {
    if (ImGui.BeginMenuBar())
    {
      try
      {
        RenderFileMenu();
        RenderEditMenu();
        RenderSessionsMenu();
        RenderFontMenu();
        RenderThemeMenu();
        RenderSettingsMenu();
      }
      finally
      {
        ImGui.EndMenuBar();
      }
    }
  }

  /// <summary>
  /// Renders the File menu with terminal management options.
  /// </summary>
  private void RenderFileMenu()
  {
    if (ImGui.BeginMenu("File"))
    {
      try
      {
        // New Terminal - now enabled for multi-session support
        if (ImGui.MenuItem("New Terminal"))
        {
          _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
        }

        // Close Terminal - enabled when more than one session exists
        bool canCloseTerminal = _sessionManager.SessionCount > 1;
        if (ImGui.MenuItem("Close Terminal", "", false, canCloseTerminal))
        {
          var activeSession = _sessionManager.ActiveSession;
          if (activeSession != null)
          {
            _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(activeSession.Id));
          }
        }

        ImGui.Separator();

        // Next Terminal - enabled when more than one session exists
        bool canNavigateSessions = _sessionManager.SessionCount > 1;
        if (ImGui.MenuItem("Next Terminal", "", false, canNavigateSessions))
        {
          _sessionManager.SwitchToNextSession();
        }

        // Previous Terminal - enabled when more than one session exists
        if (ImGui.MenuItem("Previous Terminal", "", false, canNavigateSessions))
        {
          _sessionManager.SwitchToPreviousSession();
        }

        ImGui.Separator();

        // Exit - closes the terminal window
        if (ImGui.MenuItem("Exit"))
        {
          _isVisible = false;
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Renders the Edit menu with text operations.
  /// </summary>
  private void RenderEditMenu()
  {
    if (ImGui.BeginMenu("Edit"))
    {
      try
      {
        // Copy - enabled only when selection exists
        bool hasSelection = !_currentSelection.IsEmpty;
        if (ImGui.MenuItem("Copy", "", false, hasSelection))
        {
          CopySelectionToClipboard();
        }

        // Paste - always enabled
        if (ImGui.MenuItem("Paste"))
        {
          PasteFromClipboard();
        }

        // Select All - always enabled
        if (ImGui.MenuItem("Select All"))
        {
          SelectAllText();
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Renders the Sessions menu with a list of all terminal sessions.
  /// Shows a checkmark for the currently active session and allows clicking to switch sessions.
  /// </summary>
  private void RenderSessionsMenu()
  {
    if (ImGui.BeginMenu("Sessions"))
    {
      try
      {
        var sessions = _sessionManager.Sessions;
        var activeSession = _sessionManager.ActiveSession;

        if (sessions.Count == 0)
        {
          ImGui.Text("No sessions available");
        }
        else
        {
          foreach (var session in sessions)
          {
            bool isActive = session == activeSession;
            string sessionLabel = session.Title;

            // Add process exit code to label if process has exited
            if (session.ProcessManager.ExitCode.HasValue)
            {
              sessionLabel += $" (Exit: {session.ProcessManager.ExitCode})";
            }

            // Create unique ImGui ID using session GUID to avoid conflicts
            string menuItemId = $"{sessionLabel}##session_menu_item_{session.Id}";

            if (ImGui.MenuItem(menuItemId, "", isActive))
            {
              if (!isActive)
              {
                _sessionManager.SwitchToSession(session.Id);
              }
            }

            // Show tooltip with session information
            if (ImGui.IsItemHovered())
            {
              var tooltip = $"Session: {session.Title}\nCreated: {session.CreatedAt:HH:mm:ss}";
              if (session.LastActiveAt.HasValue)
              {
                tooltip += $"\nLast Active: {session.LastActiveAt.Value:HH:mm:ss}";
              }
              tooltip += $"\nState: {session.State}";
              if (session.ProcessManager.IsRunning)
              {
                tooltip += "\nProcess: Running";
              }
              else if (session.ProcessManager.ExitCode.HasValue)
              {
                tooltip += $"\nProcess: Exited ({session.ProcessManager.ExitCode})";
              }
              ImGui.SetTooltip(tooltip);
            }
          }
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Renders the Font menu with font size slider and font family selection options.
  /// </summary>
  private void RenderFontMenu()
  {
    if (ImGui.BeginMenu("Font"))
    {
      try
      {
        // Font Size Slider
        int currentFontSize = (int)_fontConfig.FontSize;
        ImGui.Text("Font Size:");
        ImGui.SameLine();
        if (ImGui.SliderInt("##FontSize", ref currentFontSize, 4, 72))
        {
          SetFontSize((float)currentFontSize);
        }

        ImGui.Separator();

        // Font Family Selection
        var availableFonts = CaTTYFontManager.GetAvailableFontFamilies();

        foreach (var fontFamily in availableFonts)
        {
          bool isSelected = fontFamily == _currentFontFamily;

          if (ImGui.MenuItem(fontFamily, "", isSelected))
          {
            SelectFontFamily(fontFamily);
          }
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Selects a font family and applies it to the terminal.
  /// </summary>
  /// <param name="displayName">The display name of the font family to select</param>
  private void SelectFontFamily(string displayName)
  {
    try
    {
      Console.WriteLine($"TerminalController: Selecting font family: {displayName}");

      // Create new font configuration for the selected family
      var newFontConfig = CaTTYFontManager.CreateFontConfigForFamily(displayName, _fontConfig.FontSize);

      // Validate the new configuration
      newFontConfig.Validate();

      // Apply the new configuration immediately
      UpdateFontConfig(newFontConfig);

      // Update current selection
      _currentFontFamily = displayName;

      // Save font settings to persistent configuration
      SaveFontSettings();

      Console.WriteLine($"TerminalController: Successfully switched to font family: {displayName}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Failed to select font family {displayName}: {ex.Message}");
      // Keep current font on error - no changes to _currentFontFamily or _fontConfig
    }
  }

  /// <summary>
  /// Renders the Theme menu with theme selection options.
  /// Displays all available themes including built-in and TOML-loaded themes.
  /// </summary>
  private void RenderThemeMenu()
  {
    if (ImGui.BeginMenu("Theme"))
    {
      try
      {
        // Initialize theme system if not already done
        ThemeManager.InitializeThemes();

        var availableThemes = ThemeManager.AvailableThemes;
        var currentTheme = ThemeManager.CurrentTheme;

        // Group themes by source: built-in first, then TOML
        var builtInThemes = availableThemes.Where(t => t.Source == ThemeSource.BuiltIn)
                                          .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                                          .ToList();
        var tomlThemes = availableThemes.Where(t => t.Source == ThemeSource.TomlFile)
                                       .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
                                       .ToList();

        // Render built-in themes
        if (builtInThemes.Count > 0)
        {
          ImGui.Text("Built-in Themes:");
          ImGui.Separator();

          foreach (var theme in builtInThemes)
          {
            bool isSelected = theme.Name == currentTheme.Name;

            if (ImGui.MenuItem(theme.Name, "", isSelected))
            {
              ApplySelectedTheme(theme);
            }

            // Show tooltip with theme information
            if (ImGui.IsItemHovered())
            {
              ImGui.SetTooltip($"Theme: {theme.Name}\nType: {theme.Type}\nSource: Built-in");
            }
          }
        }

        // Add separator between built-in and TOML themes if both exist
        if (builtInThemes.Count > 0 && tomlThemes.Count > 0)
        {
          ImGui.Separator();
        }

        // Render TOML themes
        if (tomlThemes.Count > 0)
        {
          ImGui.Text("TOML Themes:");
          ImGui.Separator();

          foreach (var theme in tomlThemes)
          {
            bool isSelected = theme.Name == currentTheme.Name;

            if (ImGui.MenuItem(theme.Name, "", isSelected))
            {
              ApplySelectedTheme(theme);
            }

            // Show tooltip with theme information
            if (ImGui.IsItemHovered())
            {
              var tooltip = $"Theme: {theme.Name}\nType: {theme.Type}\nSource: TOML File";
              if (!string.IsNullOrEmpty(theme.FilePath))
              {
                tooltip += $"\nFile: {Path.GetFileName(theme.FilePath)}";
              }
              ImGui.SetTooltip(tooltip);
            }
          }
        }

        // Show message if no themes available
        if (availableThemes.Count == 0)
        {
          ImGui.Text("No themes available");
        }

        // Add refresh option
        if (tomlThemes.Count > 0 || availableThemes.Count == 0)
        {
          ImGui.Separator();
          if (ImGui.MenuItem("Refresh Themes"))
          {
            RefreshThemes();
          }

          if (ImGui.IsItemHovered())
          {
            ImGui.SetTooltip("Reload themes from TerminalThemes directory");
          }
        }
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Applies the selected theme and handles any errors.
  /// </summary>
  /// <param name="theme">The theme to apply</param>
  private void ApplySelectedTheme(TerminalTheme theme)
  {
    try
    {
      Console.WriteLine($"TerminalController: Applying theme: {theme.Name}");

      // Apply the theme through ThemeManager
      ThemeManager.ApplyTheme(theme);

      Console.WriteLine($"TerminalController: Successfully applied theme: {theme.Name}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Failed to apply theme {theme.Name}: {ex.Message}");
      // Theme system should handle fallback to default theme
    }
  }

  /// <summary>
  /// Refreshes the available themes by reloading from the filesystem.
  /// </summary>
  private void RefreshThemes()
  {
    try
    {
      Console.WriteLine("TerminalController: Refreshing themes...");

      // Refresh themes through ThemeManager
      ThemeManager.RefreshAvailableThemes();

      Console.WriteLine($"TerminalController: Themes refreshed. Available themes: {ThemeManager.AvailableThemes.Count}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Failed to refresh themes: {ex.Message}");
    }
  }

  /// <summary>
  /// Shows a message for not-yet-implemented features.
  /// </summary>
  /// <param name="feature">The feature name to display</param>
  private void ShowNotImplementedMessage(string feature)
  {
    Console.WriteLine($"TerminalController: {feature} not implemented in this phase");
    // Future: Could show ImGui popup
  }

  /// <summary>
  /// Renders the Settings menu with separate background and foreground opacity controls.
  /// Provides sliders for independent opacity adjustment with immediate visual feedback.
  /// </summary>
  private void RenderSettingsMenu()
  {
    if (ImGui.BeginMenu("Settings"))
    {
      try
      {
        // Initialize opacity manager if not already done
        OpacityManager.Initialize();

        // Display Settings Section
        ImGui.Text("Display Settings");
        ImGui.Separator();

        // Background Opacity Section
        ImGui.Text("Background Opacity:");
        int currentBgOpacityPercent = OpacityManager.GetBackgroundOpacityPercentage();
        int newBgOpacityPercent = currentBgOpacityPercent;

        if (ImGui.SliderInt("##BackgroundOpacitySlider", ref newBgOpacityPercent, 0, 100, $"{newBgOpacityPercent}%%"))
        {
          // Apply background opacity change immediately
          if (OpacityManager.SetBackgroundOpacityFromPercentage(newBgOpacityPercent))
          {
            // Console.WriteLine($"TerminalController: Background opacity set to {newBgOpacityPercent}%");
          }
          else
          {
            Console.WriteLine($"TerminalController: Failed to set background opacity to {newBgOpacityPercent}%");
          }
        }

        // Show tooltip for background opacity
        if (ImGui.IsItemHovered())
        {
          var currentBgOpacity = OpacityManager.CurrentBackgroundOpacity;
          ImGui.SetTooltip($"Background opacity: {currentBgOpacity:F2} ({currentBgOpacityPercent}%)\nAdjust terminal background transparency\nRange: 0% (transparent) to 100% (opaque)");
        }

        // Reset background opacity button
        ImGui.SameLine();
        if (ImGui.Button("Reset##BackgroundOpacityReset"))
        {
          if (OpacityManager.ResetBackgroundOpacity())
          {
            Console.WriteLine("TerminalController: Background opacity reset to default");
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip("Reset background opacity to 100% (fully opaque)");
        }

        // Cell Background Opacity Section
        ImGui.Text("Cell Background Opacity:");
        int currentCellBgOpacityPercent = OpacityManager.GetCellBackgroundOpacityPercentage();
        int newCellBgOpacityPercent = currentCellBgOpacityPercent;

        if (ImGui.SliderInt("##CellBackgroundOpacitySlider", ref newCellBgOpacityPercent, 0, 100, $"{newCellBgOpacityPercent}%%"))
        {
          // Apply cell background opacity change immediately
          if (OpacityManager.SetCellBackgroundOpacityFromPercentage(newCellBgOpacityPercent))
          {
            // Console.WriteLine($"TerminalController: Cell background opacity set to {newCellBgOpacityPercent}%");
          }
          else
          {
            Console.WriteLine($"TerminalController: Failed to set cell background opacity to {newCellBgOpacityPercent}%");
          }
        }

        // Show tooltip for cell background opacity
        if (ImGui.IsItemHovered())
        {
          var currentCellBgOpacity = OpacityManager.CurrentCellBackgroundOpacity;
          ImGui.SetTooltip($"Cell background opacity: {currentCellBgOpacity:F2} ({currentCellBgOpacityPercent}%)\nAdjust terminal cell background transparency\nRange: 0% (transparent) to 100% (opaque)");
        }

        // Reset cell background opacity button
        ImGui.SameLine();
        if (ImGui.Button("Reset##CellBackgroundOpacityReset"))
        {
          if (OpacityManager.ResetCellBackgroundOpacity())
          {
            Console.WriteLine("TerminalController: Cell background opacity reset to default");
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip("Reset cell background opacity to 100% (fully opaque)");
        }

        // Foreground Opacity Section
        ImGui.Text("Foreground Opacity:");
        int currentFgOpacityPercent = OpacityManager.GetForegroundOpacityPercentage();
        int newFgOpacityPercent = currentFgOpacityPercent;

        if (ImGui.SliderInt("##ForegroundOpacitySlider", ref newFgOpacityPercent, 0, 100, $"{newFgOpacityPercent}%%"))
        {
          // Apply foreground opacity change immediately
          if (OpacityManager.SetForegroundOpacityFromPercentage(newFgOpacityPercent))
          {
            // Console.WriteLine($"TerminalController: Foreground opacity set to {newFgOpacityPercent}%");
          }
          else
          {
            Console.WriteLine($"TerminalController: Failed to set foreground opacity to {newFgOpacityPercent}%");
          }
        }

        // Show tooltip for foreground opacity
        if (ImGui.IsItemHovered())
        {
          var currentFgOpacity = OpacityManager.CurrentForegroundOpacity;
          ImGui.SetTooltip($"Foreground opacity: {currentFgOpacity:F2} ({currentFgOpacityPercent}%)\nAdjust terminal text transparency\nRange: 0% (transparent) to 100% (opaque)");
        }

        // Reset foreground opacity button
        ImGui.SameLine();
        if (ImGui.Button("Reset##ForegroundOpacityReset"))
        {
          if (OpacityManager.ResetForegroundOpacity())
          {
            Console.WriteLine("TerminalController: Foreground opacity reset to default");
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip("Reset foreground opacity to 100% (fully opaque)");
        }

        // Reset all button
        ImGui.Separator();
        if (ImGui.Button("Reset All##ResetAllOpacity"))
        {
          if (OpacityManager.ResetOpacity())
          {
            Console.WriteLine("TerminalController: All opacity values reset to default");
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip("Reset background, foreground, and cell background opacity to 100%");
        }

        // Show current opacity status
        ImGui.Separator();
        var bgOpacity = OpacityManager.CurrentBackgroundOpacity;
        var fgOpacity = OpacityManager.CurrentForegroundOpacity;
        var cellBgOpacity = OpacityManager.CurrentCellBackgroundOpacity;
        var bgIsDefault = OpacityManager.IsDefaultBackgroundOpacity();
        var fgIsDefault = OpacityManager.IsDefaultForegroundOpacity();
        var cellBgIsDefault = OpacityManager.IsDefaultCellBackgroundOpacity();

        var bgStatusText = bgIsDefault ? "Default (100%)" : $"{bgOpacity:F2} ({currentBgOpacityPercent}%)";
        var fgStatusText = fgIsDefault ? "Default (100%)" : $"{fgOpacity:F2} ({currentFgOpacityPercent}%)";
        var cellBgStatusText = cellBgIsDefault ? "Default (100%)" : $"{cellBgOpacity:F2} ({currentCellBgOpacityPercent}%)";

        ImGui.Text($"Window Background: {bgStatusText}");
        ImGui.Text($"Cell Background: {cellBgStatusText}");
        ImGui.Text($"Foreground: {fgStatusText}");

        // Shell Configuration Section
        ImGui.Separator();
        ImGui.Text("Shell Configuration");
        ImGui.Separator();

        RenderShellConfigurationSection();
      }
      finally
      {
        ImGui.EndMenu();
      }
    }
  }

  /// <summary>
  /// Renders the shell configuration section in the Settings menu.
  /// Allows users to select default shell type and configure shell-specific options.
  /// Only shows shells that are available on the current system.
  /// </summary>
  private void RenderShellConfigurationSection()
  {
    var config = _themeConfig;
    bool configChanged = false;

    // Check if current shell is available
    bool currentShellAvailable = ShellAvailabilityChecker.IsShellAvailable(config.DefaultShellType);

    // Current shell display with availability indicator
    if (currentShellAvailable)
    {
      ImGui.Text($"Current Default Shell: {config.GetShellDisplayName()}");
    }
    else
    {
      ImGui.TextColored(new float4(1.0f, 0.6f, 0.0f, 1.0f), $"Current Default Shell: {config.GetShellDisplayName()} (Not Available)");
      if (ImGui.IsItemHovered())
      {
        ImGui.SetTooltip("The currently configured shell is not available on this system. Please select an available shell below.");
      }
    }

    if (ImGui.IsItemHovered() && currentShellAvailable)
    {
      ImGui.SetTooltip("This shell will be used for new terminal sessions");
    }

    ImGui.Spacing();

    // Shell type selection - only show available shells
    ImGui.Text("Select Default Shell:");

    var availableShells = ShellAvailabilityChecker.GetAvailableShellsWithNames();

    // Show message if no shells are available (shouldn't happen, but defensive programming)
    if (availableShells.Count == 0)
    {
      ImGui.TextColored(new float4(1.0f, 0.5f, 0.5f, 1.0f), "No shells available on this system");
      return;
    }

    // If current shell is not available, show warning (fallback is handled during initialization)
    if (!currentShellAvailable)
    {
      ImGui.TextColored(new float4(1.0f, 0.6f, 0.0f, 1.0f), "Note: Shell availability changed since last configuration");
      ImGui.Spacing();
    }

    foreach (var (shellType, displayName) in availableShells)
    {
      bool isSelected = config.DefaultShellType == shellType;

      if (ImGui.RadioButton($"{displayName}##shell_{shellType}", isSelected))
      {
        if (!isSelected)
        {
          config.DefaultShellType = shellType;
          configChanged = true;

          // Apply configuration immediately when shell type changes
          ApplyShellConfiguration();
        }
      }

      // Add tooltips for each shell type
      if (ImGui.IsItemHovered())
      {
        var tooltip = shellType switch
        {
          ShellType.Wsl => "Windows Subsystem for Linux - Recommended for development work",
          ShellType.PowerShell => "Traditional Windows PowerShell (powershell.exe)",
          ShellType.PowerShellCore => "Modern cross-platform PowerShell (pwsh.exe)",
          ShellType.Cmd => "Windows Command Prompt (cmd.exe)",
          ShellType.Custom => "Specify a custom shell executable",
          _ => "Shell option"
        };
        ImGui.SetTooltip(tooltip);
      }
    }

    // Show count of available shells for debugging
    ImGui.Spacing();
    ImGui.TextColored(new float4(0.7f, 0.7f, 0.7f, 1.0f), $"Showing {availableShells.Count} available shell(s)");
    if (ImGui.IsItemHovered())
    {
      var shellNames = string.Join(", ", availableShells.Select(s => s.ShellType.ToString()));
      ImGui.SetTooltip($"Available shells: {shellNames}");
    }

    ImGui.Spacing();

    // WSL distribution selection (only show when WSL is selected)
    if (config.DefaultShellType == ShellType.Wsl)
    {
      ImGui.Text("WSL Distribution:");
      ImGui.Text($"Current: {config.WslDistribution ?? "Default"}");

      if (ImGui.Button("Change WSL Distribution##wsl_dist"))
      {
        // For now, cycle through common distributions
        var distributions = new[] { null, "Ubuntu", "Debian", "Alpine" };
        var currentIndex = Array.IndexOf(distributions, config.WslDistribution);
        var nextIndex = (currentIndex + 1) % distributions.Length;
        config.WslDistribution = distributions[nextIndex];
        configChanged = true;

        // Apply configuration immediately when WSL distribution changes
        ApplyShellConfiguration();
      }

      if (ImGui.IsItemHovered())
      {
        ImGui.SetTooltip("Click to cycle through: Default → Ubuntu → Debian → Alpine");
      }
    }

    // Custom shell path (only show when Custom is selected)
    if (config.DefaultShellType == ShellType.Custom)
    {
      ImGui.Text("Custom Shell Path:");
      ImGui.Text($"Current: {config.CustomShellPath ?? "Not set"}");

      if (ImGui.Button("Set Custom Shell Path##custom_path"))
      {
        // For now, provide some common examples
        var commonPaths = new[] {
          null,
          @"C:\msys64\usr\bin\bash.exe",
          @"C:\Program Files\Git\bin\bash.exe",
          @"C:\Windows\System32\wsl.exe"
        };
        var currentIndex = Array.IndexOf(commonPaths, config.CustomShellPath);
        var nextIndex = (currentIndex + 1) % commonPaths.Length;
        config.CustomShellPath = commonPaths[nextIndex];
        configChanged = true;

        // Apply configuration immediately when custom shell path changes
        ApplyShellConfiguration();
      }

      if (ImGui.IsItemHovered())
      {
        ImGui.SetTooltip("Click to cycle through common shell paths\nOr manually edit the configuration file");
      }
    }

    // Show current configuration status
    ImGui.Spacing();
    ImGui.Text("Settings are applied automatically to new terminal sessions.");

    if (configChanged)
    {
      ImGui.TextColored(new Brutal.Numerics.float4(0.0f, 1.0f, 0.0f, 1.0f), "✓ Configuration updated successfully!");
    }
  }

  /// <summary>
  /// Applies the current shell configuration to the session manager and saves settings.
  /// </summary>
  private void ApplyShellConfiguration()
  {
    try
    {
      // Create launch options from current configuration
      var launchOptions = _themeConfig.CreateLaunchOptions();

      // Update session manager with new default launch options
      _sessionManager.UpdateDefaultLaunchOptions(launchOptions);

      // Sync current opacity values from OpacityManager before saving
      // This ensures global opacity settings are preserved when shell type changes
      _themeConfig.BackgroundOpacity = OpacityManager.CurrentBackgroundOpacity;
      _themeConfig.ForegroundOpacity = OpacityManager.CurrentForegroundOpacity;

      // Save configuration to disk
      _themeConfig.Save();

      Console.WriteLine($"Shell configuration applied: {_themeConfig.GetShellDisplayName()}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error applying shell configuration: {ex.Message}");
    }
  }

  /// <summary>
  /// Applies the loaded shell configuration to the session manager during initialization.
  /// </summary>
  private void ApplyShellConfigurationToSessionManager()
  {
    try
    {
      // Check if the configured shell is available
      if (!ShellAvailabilityChecker.IsShellAvailable(_themeConfig.DefaultShellType))
      {
        // Fall back to the first available shell
        var availableShells = ShellAvailabilityChecker.GetAvailableShells();
        if (availableShells.Count > 0)
        {
          // Prefer concrete shells over Auto/Custom for fallback
          var fallbackShell = availableShells.FirstOrDefault(s => s != ShellType.Auto && s != ShellType.Custom);
          if (fallbackShell == default(ShellType))
          {
            fallbackShell = availableShells[0];
          }

          _themeConfig.DefaultShellType = fallbackShell;
          _themeConfig.Save(); // Save the fallback choice
        }
      }

      // Create launch options from loaded configuration
      var launchOptions = _themeConfig.CreateLaunchOptions();

      // Set default terminal dimensions and working directory
      launchOptions.InitialWidth = 80;
      launchOptions.InitialHeight = 24;
      launchOptions.WorkingDirectory = Environment.CurrentDirectory;

      // Update session manager with loaded default launch options
      _sessionManager.UpdateDefaultLaunchOptions(launchOptions);
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Error loading shell configuration: {ex.Message}");
      // Continue with default shell configuration
    }
  }

  /// <summary>
  /// Renders the tab area using real ImGui tabs for session management.
  /// Includes add button and context menus for tab operations.
  /// </summary>
  private void RenderTabArea()
  {
    try
    {
      var sessions = _sessionManager.Sessions;
      var activeSession = _sessionManager.ActiveSession;

      // Get available width for tab area
      float availableWidth = ImGui.GetContentRegionAvail().X;
      float tabHeight = LayoutConstants.TAB_AREA_HEIGHT;

      // Create a child region for the tab area to maintain consistent height
      bool childBegun = ImGui.BeginChild("TabArea", new float2(availableWidth, tabHeight), ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar);

      try
      {
        if (childBegun)
        {
          // Add button on the left with fixed width
          float addButtonWidth = LayoutConstants.ADD_BUTTON_WIDTH;
          if (ImGui.Button("+##add_terminal", new float2(addButtonWidth, tabHeight - 5.0f)))
          {
            _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
            ForceFocus();
          }

          if (ImGui.IsItemHovered())
          {
            ImGui.SetTooltip("Add new terminal session");
          }

          // Only show tabs if we have sessions
          if (sessions.Count > 0)
          {
            ImGui.SameLine();

            // Calculate remaining width for tab bar
            float remainingWidth = availableWidth - addButtonWidth - LayoutConstants.ELEMENT_SPACING;

            // Begin tab bar with remaining width
            if (ImGui.BeginTabBar("SessionTabs", ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.AutoSelectNewTabs | ImGuiTabBarFlags.FittingPolicyScroll))
            {
              try
              {
                // Render each session as a tab
                foreach (var session in sessions)
                {
                  bool isActive = session == activeSession;
                  
                  // Create tab label with session title and optional exit code
                  string tabLabel = session.Title;
                  if (session.ProcessManager.ExitCode.HasValue)
                  {
                    tabLabel += $" (Exit: {session.ProcessManager.ExitCode})";
                  }

                  // Use unique ID for each tab
                  string tabId = $"{tabLabel}##tab_{session.Id}";

                  // Don't use SetSelected flag - let ImGui handle tab selection naturally
                  ImGuiTabItemFlags tabFlags = ImGuiTabItemFlags.None;

                  bool tabOpen = true;
                  if (ImGui.BeginTabItem(tabId, ref tabOpen, tabFlags))
                  {
                    try
                    {
                      // If this tab is being rendered and it's not the current active session, switch to it
                      // This only happens when user actually clicks the tab, not when we force selection
                      if (!isActive)
                      {
                        _sessionManager.SwitchToSession(session.Id);
                        // Don't call ForceFocus() here as it's not needed for tab switching
                      }

                      // Tab content is handled by the terminal canvas, so we don't render content here
                      // The tab item just needs to exist to show the tab
                    }
                    finally
                    {
                      ImGui.EndTabItem();
                    }
                  }

                  // Handle tab close button (when tabOpen becomes false)
                  if (!tabOpen && sessions.Count > 1)
                  {
                    _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(session.Id));
                  }

                  // Context menu for tab (right-click)
                  if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                  {
                    ImGui.OpenPopup($"tab_context_{session.Id}");
                  }

                  if (ImGui.BeginPopup($"tab_context_{session.Id}"))
                  {
                    if (ImGui.MenuItem("Close Tab") && sessions.Count > 1)
                    {
                      _ = Task.Run(async () => await _sessionManager.CloseSessionAsync(session.Id));
                    }

                    // Add restart option for terminated sessions
                    if (!session.ProcessManager.IsRunning && session.ProcessManager.ExitCode.HasValue)
                    {
                      if (ImGui.MenuItem("Restart Session"))
                      {
                        _ = Task.Run(async () =>
                        {
                          try
                          {
                            await _sessionManager.RestartSessionAsync(session.Id);
                          }
                          catch (Exception ex)
                          {
                            Console.WriteLine($"TerminalController: Failed to restart session {session.Id}: {ex.Message}");
                          }
                        });
                      }
                    }

                    if (ImGui.MenuItem("Rename Tab"))
                    {
                      // TODO: Implement tab renaming in future
                      ShowNotImplementedMessage("Tab renaming");
                    }
                    ImGui.EndPopup();
                  }
                }
              }
              finally
              {
                ImGui.EndTabBar();
              }
            }
          }
        }
      }
      finally
      {
        if (childBegun)
        {
          ImGui.EndChild();
        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error rendering tab area: {ex.Message}");

      // Fallback: render a simple text indicator if tab rendering fails
      ImGui.Text("No sessions");
      ImGui.SameLine();
      if (ImGui.Button("+##fallback_add"))
      {
        _ = Task.Run(async () => await _sessionManager.CreateSessionAsync());
      }
    }
  }

  /// <summary>
  /// Renders the terminal canvas for multi-session UI layout.
  /// This method provides terminal display within the session management framework.
  /// </summary>
  private void RenderTerminalCanvas()
  {
    try
    {
      // Pop UI font before rendering terminal content
      // Note: This assumes UI font was pushed before calling this method

      // Render terminal content directly without additional UI elements
      RenderTerminalContent();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error rendering terminal canvas: {ex.Message}");

      // Fallback: render a simple error message
      ImGui.Text("Terminal rendering error");
    }
  }

  /// <summary>
  /// Pastes text from the clipboard to the terminal.
  /// </summary>
  private void PasteFromClipboard()
  {
    try
    {
      string? clipboardText = ClipboardManager.GetText();
      if (!string.IsNullOrEmpty(clipboardText))
      {
        SendToProcess(clipboardText);
        Console.WriteLine($"TerminalController: Pasted {clipboardText.Length} characters from clipboard");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error pasting from clipboard: {ex.Message}");
    }
  }

  /// <summary>
  /// Selects all text in the terminal.
  /// </summary>
  private void SelectAllText()
  {
    SelectAllVisibleContent();
  }

  /// <summary>
  /// Sets the font size to the specified value.
  /// </summary>
  /// <param name="fontSize">The new font size to set</param>
  private void SetFontSize(float fontSize)
  {
    try
    {
      // Clamp the font size to valid range
      fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, fontSize));

      var newFontConfig = new TerminalFontConfig
      {
        FontSize = fontSize,
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig);
      
      // Save font settings to persistent configuration
      SaveFontSettings();
      
      Console.WriteLine($"TerminalController: Font size set to {fontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error setting font size: {ex.Message}");
    }
  }

  /// <summary>
  /// Resets the font size to the default value.
  /// </summary>
  private void ResetFontSize()
  {
    try
    {
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = 32.0f, // Default font size
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig);
      
      // Save font settings to persistent configuration
      SaveFontSettings();
      
      Console.WriteLine("TerminalController: Font size reset to default");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error resetting font size: {ex.Message}");
    }
  }

  /// <summary>
  /// Increases the font size by 1.0f.
  /// </summary>
  private void IncreaseFontSize()
  {
    try
    {
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = Math.Min(LayoutConstants.MAX_FONT_SIZE, _fontConfig.FontSize + 1.0f),
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig);
      
      // Save font settings to persistent configuration
      SaveFontSettings();
      
      Console.WriteLine($"TerminalController: Font size increased to {newFontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error increasing font size: {ex.Message}");
    }
  }

  /// <summary>
  /// Decreases the font size by 1.0f.
  /// </summary>
  private void DecreaseFontSize()
  {
    try
    {
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, _fontConfig.FontSize - 1.0f),
        RegularFontName = _fontConfig.RegularFontName,
        BoldFontName = _fontConfig.BoldFontName,
        ItalicFontName = _fontConfig.ItalicFontName,
        BoldItalicFontName = _fontConfig.BoldItalicFontName,
        AutoDetectContext = _fontConfig.AutoDetectContext
      };
      UpdateFontConfig(newFontConfig);
      
      // Save font settings to persistent configuration
      SaveFontSettings();
      
      Console.WriteLine($"TerminalController: Font size decreased to {newFontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error decreasing font size: {ex.Message}");
    }
  }

  #endregion

  #region Font Settings Persistence

  /// <summary>
  /// Loads font settings from persistent configuration.
  /// </summary>
  private void LoadFontSettings()
  {
    try
    {
      // Load fresh configuration from disk to get latest saved values
      var config = ThemeConfiguration.Load();
      
      bool fontConfigChanged = false;
      
      // Apply saved font family if available
      if (!string.IsNullOrEmpty(config.FontFamily))
      {
        try
        {
          // Create font configuration for the saved family
          var savedFontConfig = CaTTYFontManager.CreateFontConfigForFamily(config.FontFamily, config.FontSize ?? _fontConfig.FontSize);
          
          // Log what we're trying to load vs what we got
          Console.WriteLine($"TerminalController: Attempting to load font family '{config.FontFamily}'");
          Console.WriteLine($"TerminalController: Created font config - Regular: {savedFontConfig.RegularFontName}");
          
          _fontConfig = savedFontConfig;
          _currentFontFamily = config.FontFamily;
          fontConfigChanged = true;
          Console.WriteLine($"TerminalController: Successfully loaded font family from settings: {config.FontFamily}");
        }
        catch (Exception ex)
        {
          Console.WriteLine($"TerminalController: Failed to load saved font family '{config.FontFamily}': {ex.Message}");
          // Keep current font configuration on error
        }
      }
      else
      {
        Console.WriteLine("TerminalController: No saved font family found, using default");
      }

      // Apply saved font size if available
      if (config.FontSize.HasValue)
      {
        var fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, config.FontSize.Value));
        if (Math.Abs(_fontConfig.FontSize - fontSize) > 0.1f)
        {
          _fontConfig.FontSize = fontSize;
          fontConfigChanged = true;
          Console.WriteLine($"TerminalController: Loaded font size from settings: {fontSize}");
        }
      }
      else
      {
        Console.WriteLine("TerminalController: No saved font size found, using default");
      }
      
      // If font configuration changed, reset font loading state to force reload
      if (fontConfigChanged)
      {
        _fontsLoaded = false;
        Console.WriteLine("TerminalController: Font configuration changed, fonts will be reloaded on next render");
        Console.WriteLine($"TerminalController: Final font config after loading - Regular: {_fontConfig.RegularFontName}, Size: {_fontConfig.FontSize}");
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error loading font settings: {ex.Message}");
    }
  }

  /// <summary>
  /// Saves current font settings to persistent configuration.
  /// </summary>
  private void SaveFontSettings()
  {
    try
    {
      var config = ThemeConfiguration.Load();
      
      // Update font settings
      config.FontFamily = _currentFontFamily;
      config.FontSize = _fontConfig.FontSize;
      
      // Save configuration
      config.Save();
      
      Console.WriteLine($"TerminalController: Saved font settings - Family: {_currentFontFamily}, Size: {_fontConfig.FontSize}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"TerminalController: Error saving font settings: {ex.Message}");
    }
  }

  #endregion

}
