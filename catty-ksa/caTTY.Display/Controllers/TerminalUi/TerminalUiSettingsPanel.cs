using System;
using System.Linq;
using Brutal.ImGuiApi;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers.TerminalUi.Menus;
using caTTY.Display.Performance;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
/// Coordinates menu bar rendering and shell configuration initialization.
/// Delegates all menu rendering to specialized renderer classes.
/// </summary>
internal class TerminalUiSettingsPanel
{
  private readonly SessionManager _sessionManager;
  private readonly ThemeConfiguration _themeConfig;
  private readonly FileMenuRenderer _fileMenuRenderer;
  private readonly EditMenuRenderer _editMenuRenderer;
  private readonly SessionsMenuRenderer _sessionsMenuRenderer;
  private readonly FontMenuRenderer _fontMenuRenderer;
  private readonly ThemeMenuRenderer _themeMenuRenderer;
  private readonly GeneralSettingsMenuRenderer _generalSettingsMenuRenderer;
  private readonly PerformanceMenuRenderer _performanceMenuRenderer;

  public TerminalUiSettingsPanel(
    TerminalController controller,
    SessionManager sessionManager,
    ThemeConfiguration themeConfig,
    TerminalUiFonts fonts,
    TerminalUiSelection selection,
    Action triggerTerminalResizeForAllSessions,
    PerformanceStopwatch perfWatch)
  {
    if (controller == null) throw new ArgumentNullException(nameof(controller));
    if (fonts == null) throw new ArgumentNullException(nameof(fonts));
    if (selection == null) throw new ArgumentNullException(nameof(selection));
    if (triggerTerminalResizeForAllSessions == null) throw new ArgumentNullException(nameof(triggerTerminalResizeForAllSessions));
    if (perfWatch == null) throw new ArgumentNullException(nameof(perfWatch));

    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _themeConfig = themeConfig ?? throw new ArgumentNullException(nameof(themeConfig));
    _fileMenuRenderer = new FileMenuRenderer(controller, sessionManager);
    _editMenuRenderer = new EditMenuRenderer(controller, selection);
    _sessionsMenuRenderer = new SessionsMenuRenderer(sessionManager);
    _fontMenuRenderer = new FontMenuRenderer(fonts, sessionManager, triggerTerminalResizeForAllSessions);
    _themeMenuRenderer = new ThemeMenuRenderer(themeConfig);
    _generalSettingsMenuRenderer = new GeneralSettingsMenuRenderer(themeConfig, sessionManager);
    _performanceMenuRenderer = new PerformanceMenuRenderer(perfWatch);
  }

  /// <summary>
  /// Renders the menu bar by coordinating all menu renderers.
  /// Delegates to File, Edit, Sessions, Font, Theme, Performance, and Settings menu renderers.
  /// </summary>
  public void RenderMenuBar()
  {
    if (ImGui.BeginMenuBar())
    {
      try
      {
        _fileMenuRenderer.Render();
        _editMenuRenderer.Render();
        _sessionsMenuRenderer.Render();
        _fontMenuRenderer.Render();
        _themeMenuRenderer.Render();
        _performanceMenuRenderer.Render();
        _generalSettingsMenuRenderer.Render();
      }
      finally
      {
        ImGui.EndMenuBar();
      }
    }
  }


  /// <summary>
  /// Applies the loaded shell configuration to the session manager during initialization.
  /// </summary>
  public void ApplyShellConfigurationToSessionManager()
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
}
