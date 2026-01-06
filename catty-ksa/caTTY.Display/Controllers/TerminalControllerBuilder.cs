using System;
using caTTY.Core.Input;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers.TerminalUi;
using caTTY.Display.Input;
using caTTY.Display.Rendering;
using caTTY.Display.Types;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers;

/// <summary>
///     Builder for TerminalController initialization.
///     Separates complex construction logic from the controller facade.
/// </summary>
internal class TerminalControllerBuilder
{
  private readonly SessionManager _sessionManager;
  private readonly TerminalRenderingConfig _config;
  private readonly TerminalFontConfig _fontConfig;
  private readonly MouseWheelScrollConfig _scrollConfig;
  private readonly ThemeConfiguration _themeConfig;

  // Subsystem instances
  private TerminalUiFonts? _fonts;
  private TerminalUiRender? _render;
  private TerminalUiInput? _input;
  private TerminalUiMouseTracking? _mouseTracking;
  private TerminalUiSelection? _selection;
  private TerminalUiResize? _resize;
  private TerminalUiTabs? _tabs;
  private TerminalUiSettingsPanel? _settingsPanel;
  private TerminalUiEvents? _events;

  // Mouse infrastructure
  private MouseTrackingManager? _mouseTrackingManager;
  private MouseStateManager? _mouseStateManager;
  private CoordinateConverter? _coordinateConverter;
  private MouseEventProcessor? _mouseEventProcessor;
  private MouseInputHandler? _mouseInputHandler;

  // Rendering components
  private CursorRenderer? _cursorRenderer;
  private ColorResolver? _colorResolver;
  private StyleManager? _styleManager;

  /// <summary>
  ///     Creates a new builder with the specified dependencies.
  /// </summary>
  public TerminalControllerBuilder(
    SessionManager sessionManager,
    TerminalRenderingConfig config,
    TerminalFontConfig fontConfig,
    MouseWheelScrollConfig scrollConfig)
  {
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _config = config ?? throw new ArgumentNullException(nameof(config));
    _fontConfig = fontConfig ?? throw new ArgumentNullException(nameof(fontConfig));
    _scrollConfig = scrollConfig ?? throw new ArgumentNullException(nameof(scrollConfig));

    // Load theme configuration (includes shell settings)
    _themeConfig = ThemeConfiguration.Load();
  }

  /// <summary>
  ///     Builds UI subsystems (fonts, render, input, events, etc.).
  /// </summary>
  public TerminalControllerBuilder BuildUiSubsystems(TerminalController controller)
  {
    // Initialize font subsystem
    _fonts = new TerminalUiFonts(_config, _fontConfig, "Hack");
    _fonts.LoadFontSettingsInConstructor();
    _fonts.InitializeCurrentFontFamily();

    // Initialize cursor renderer
    _cursorRenderer = new CursorRenderer();

    // Initialize color and style managers
    _colorResolver = new ColorResolver(controller.PerfWatch);
    _styleManager = new StyleManager(controller.PerfWatch, _colorResolver);

    // Initialize render subsystem
    _render = new TerminalUiRender(_fonts, _cursorRenderer, controller.PerfWatch, _colorResolver, _styleManager);

    // Initialize input subsystem
    _input = new TerminalUiInput(controller, _sessionManager, _cursorRenderer, _scrollConfig);

    // Validate configurations
    _config.Validate();
    _fonts.CurrentFontConfig.Validate();
    _scrollConfig.Validate();

    return this;
  }

  /// <summary>
  ///     Builds input handlers (mouse tracking infrastructure).
  /// </summary>
  public TerminalControllerBuilder BuildInputHandlers(TerminalController controller)
  {
    // Initialize mouse tracking infrastructure
    _mouseTrackingManager = new MouseTrackingManager();
    _mouseStateManager = new MouseStateManager();
    _coordinateConverter = new CoordinateConverter();
    _mouseEventProcessor = new MouseEventProcessor(_mouseTrackingManager, _mouseStateManager);
    _mouseInputHandler = new MouseInputHandler(_mouseEventProcessor, _coordinateConverter, _mouseStateManager, _mouseTrackingManager);

    // Initialize mouse tracking subsystem
    _mouseTracking = new TerminalUiMouseTracking(
      controller,
      _sessionManager,
      _mouseTrackingManager,
      _mouseStateManager,
      _coordinateConverter,
      _mouseEventProcessor,
      _mouseInputHandler);

    // Initialize selection subsystem
    _selection = new TerminalUiSelection(controller, _sessionManager, _mouseTracking);

    return this;
  }

  /// <summary>
  ///     Builds rendering components (resize, tabs, settings panel, events).
  /// </summary>
  public TerminalControllerBuilder BuildRenderComponents(
    TerminalController controller,
    Action triggerTerminalResizeForAllSessions,
    Action resetCursorToThemeDefaults)
  {
    if (_fonts == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");
    if (_selection == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_cursorRenderer == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");
    if (_mouseTracking == null) throw new InvalidOperationException("BuildInputHandlers must be called first");

    // Initialize resize subsystem
    _resize = new TerminalUiResize(_sessionManager, _fonts);

    // Initialize tabs subsystem
    _tabs = new TerminalUiTabs(controller, _sessionManager);

    // Initialize settings panel subsystem
    _settingsPanel = new TerminalUiSettingsPanel(
      controller,
      _sessionManager,
      _themeConfig,
      _fonts,
      _selection,
      triggerTerminalResizeForAllSessions);

    // Initialize events subsystem
    _events = new TerminalUiEvents(
      _sessionManager,
      _cursorRenderer,
      _selection,
      _mouseTracking,
      resetCursorToThemeDefaults);

    return this;
  }

  /// <summary>
  ///     Wires up event handlers and initializes final state.
  /// </summary>
  public TerminalControllerBuilder WireUpEvents()
  {
    if (_mouseEventProcessor == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_mouseInputHandler == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_mouseTracking == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_events == null) throw new InvalidOperationException("BuildRenderComponents must be called first");

    // Wire up mouse event handlers through mouse tracking subsystem
    _mouseEventProcessor.MouseEventGenerated += _mouseTracking.OnMouseEventGenerated;
    _mouseEventProcessor.LocalMouseEvent += _mouseTracking.OnLocalMouseEvent;
    _mouseEventProcessor.ProcessingError += _mouseTracking.OnMouseProcessingError;
    _mouseInputHandler.InputError += _mouseTracking.OnMouseInputError;

    // Wire up session manager events to events subsystem
    _sessionManager.SessionCreated += _events.OnSessionCreated;
    _sessionManager.SessionClosed += _events.OnSessionClosed;
    _sessionManager.ActiveSessionChanged += _events.OnActiveSessionChanged;

    // Wire up title change events for any existing sessions
    foreach (var session in _sessionManager.Sessions)
    {
      session.TitleChanged += _events.OnSessionTitleChanged;
    }

    // Subscribe to theme change events
    ThemeManager.ThemeChanged += _events.OnThemeChanged;

    return this;
  }

  /// <summary>
  ///     Builds the final TerminalController with all initialized subsystems.
  /// </summary>
  public TerminalController BuildController(TerminalController controller)
  {
    if (_fonts == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");
    if (_render == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");
    if (_input == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");
    if (_mouseTracking == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_selection == null) throw new InvalidOperationException("BuildInputHandlers must be called first");
    if (_resize == null) throw new InvalidOperationException("BuildRenderComponents must be called first");
    if (_tabs == null) throw new InvalidOperationException("BuildRenderComponents must be called first");
    if (_settingsPanel == null) throw new InvalidOperationException("BuildRenderComponents must be called first");
    if (_events == null) throw new InvalidOperationException("BuildRenderComponents must be called first");
    if (_cursorRenderer == null) throw new InvalidOperationException("BuildUiSubsystems must be called first");

    // Initialize the controller with all subsystems
    controller.Initialize(
      _config,
      _fontConfig,
      _scrollConfig,
      _themeConfig,
      _fonts,
      _cursorRenderer,
      _render,
      _input,
      _mouseTrackingManager!,
      _mouseStateManager!,
      _coordinateConverter!,
      _mouseEventProcessor!,
      _mouseInputHandler!,
      _mouseTracking,
      _selection,
      _resize,
      _tabs,
      _settingsPanel,
      _events);

    return controller;
  }
}
