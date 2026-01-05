using System;
using System.IO;
using System.Linq;
using Brutal.ImGuiApi;
using caTTY.Core.Managers;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Controllers.TerminalUi.Menus;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;
using float2 = Brutal.Numerics.float2;
using float4 = Brutal.Numerics.float4;

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
        int currentFontSize = (int)_fonts.CurrentFontConfig.FontSize;
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
          bool isSelected = fontFamily == _fonts.CurrentFontFamily;

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
    _fonts.SelectFontFamily(displayName, () =>
    {
      // Callback when font configuration changes
      _sessionManager.ApplyFontConfigToAllSessions(_fonts.CurrentFontConfig);
      _triggerTerminalResizeForAllSessions();
    });
    _fonts.SaveFontSettings();
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
  /// Sets the font size to the specified value.
  /// </summary>
  /// <param name="fontSize">The new font size to set</param>
  private void SetFontSize(float fontSize)
  {
    try
    {
      // Clamp the font size to valid range
      fontSize = Math.Max(LayoutConstants.MIN_FONT_SIZE, Math.Min(LayoutConstants.MAX_FONT_SIZE, fontSize));

      var currentConfig = _fonts.CurrentFontConfig;
      var newFontConfig = new TerminalFontConfig
      {
        FontSize = fontSize,
        RegularFontName = currentConfig.RegularFontName,
        BoldFontName = currentConfig.BoldFontName,
        ItalicFontName = currentConfig.ItalicFontName,
        BoldItalicFontName = currentConfig.BoldItalicFontName,
        AutoDetectContext = currentConfig.AutoDetectContext
      };
      _controller.UpdateFontConfig(newFontConfig);

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
