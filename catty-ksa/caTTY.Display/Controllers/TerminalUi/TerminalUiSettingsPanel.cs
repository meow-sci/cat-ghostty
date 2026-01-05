using System;
using System.Linq;
using Brutal.ImGuiApi;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers.TerminalUi.Menus;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
/// Handles settings panel UI rendering for the terminal controller.
/// Includes menu bar, settings menus, shell configuration, and theme selection.
/// </summary>
internal class TerminalUiSettingsPanel
{
  private readonly TerminalController _controller;
  private readonly SessionManager _sessionManager;
  private readonly ThemeConfiguration _themeConfig;
  private readonly TerminalUiFonts _fonts;
  private readonly TerminalUiSelection _selection;
  private readonly Action _triggerTerminalResizeForAllSessions;
  private readonly FileMenuRenderer _fileMenuRenderer;
  private readonly EditMenuRenderer _editMenuRenderer;
  private readonly SessionsMenuRenderer _sessionsMenuRenderer;
  private readonly FontMenuRenderer _fontMenuRenderer;
  private readonly ThemeMenuRenderer _themeMenuRenderer;
  private readonly GeneralSettingsMenuRenderer _generalSettingsMenuRenderer;

  public TerminalUiSettingsPanel(
    TerminalController controller,
    SessionManager sessionManager,
    ThemeConfiguration themeConfig,
    TerminalUiFonts fonts,
    TerminalUiSelection selection,
    Action triggerTerminalResizeForAllSessions)
  {
    _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _themeConfig = themeConfig ?? throw new ArgumentNullException(nameof(themeConfig));
    _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));
    _selection = selection ?? throw new ArgumentNullException(nameof(selection));
    _triggerTerminalResizeForAllSessions = triggerTerminalResizeForAllSessions ?? throw new ArgumentNullException(nameof(triggerTerminalResizeForAllSessions));
    _fileMenuRenderer = new FileMenuRenderer(controller, sessionManager);
    _editMenuRenderer = new EditMenuRenderer(controller, selection);
    _sessionsMenuRenderer = new SessionsMenuRenderer(sessionManager);
    _fontMenuRenderer = new FontMenuRenderer(fonts, sessionManager, triggerTerminalResizeForAllSessions);
    _themeMenuRenderer = new ThemeMenuRenderer(themeConfig);
    _generalSettingsMenuRenderer = new GeneralSettingsMenuRenderer(themeConfig, sessionManager);
  }

  /// <summary>
  /// Renders the menu bar with File, Edit, and Font menus.
  /// Uses ImGui menu widgets to provide standard menu functionality.
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

  /// <summary>
  /// Resets the font size to the default value.
  /// </summary>
  public void ResetFontSize()
  {
    try
    {
      var currentConfig = _fonts.CurrentFontConfig;
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = 32.0f, // Default font size
        RegularFontName = currentConfig.RegularFontName,
        BoldFontName = currentConfig.BoldFontName,
        ItalicFontName = currentConfig.ItalicFontName,
        BoldItalicFontName = currentConfig.BoldItalicFontName,
        AutoDetectContext = currentConfig.AutoDetectContext
      };
      _controller.UpdateFontConfig(newFontConfig);

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
  public void IncreaseFontSize()
  {
    _fonts.IncreaseFontSize(() =>
    {
      // Callback when font configuration changes
      _sessionManager.ApplyFontConfigToAllSessions(_fonts.CurrentFontConfig);
      _triggerTerminalResizeForAllSessions();
    });
    _fonts.SaveFontSettings();
  }

  /// <summary>
  /// Decreases the font size by 1.0f.
  /// </summary>
  public void DecreaseFontSize()
  {
    _fonts.DecreaseFontSize(() =>
    {
      // Callback when font configuration changes
      _sessionManager.ApplyFontConfigToAllSessions(_fonts.CurrentFontConfig);
      _triggerTerminalResizeForAllSessions();
    });
    _fonts.SaveFontSettings();
  }

  /// <summary>
  /// Saves current font settings to persistent configuration.
  /// </summary>
  private void SaveFontSettings()
  {
    _fonts.SaveFontSettings();
  }
}
