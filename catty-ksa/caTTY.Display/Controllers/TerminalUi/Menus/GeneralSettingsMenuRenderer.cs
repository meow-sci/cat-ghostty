using System;
using System.Linq;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;
using float4 = Brutal.Numerics.float4;

namespace caTTY.Display.Controllers.TerminalUi.Menus;

/// <summary>
/// Handles rendering of the general Settings menu with opacity controls and shell configuration.
/// Provides sliders for background, foreground, and cell background opacity adjustment,
/// along with shell type selection and shell-specific options.
/// </summary>
internal class GeneralSettingsMenuRenderer
{
  private readonly ThemeConfiguration _themeConfig;
  private readonly SessionManager _sessionManager;

  public GeneralSettingsMenuRenderer(
    ThemeConfiguration themeConfig,
    SessionManager sessionManager)
  {
    _themeConfig = themeConfig ?? throw new ArgumentNullException(nameof(themeConfig));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
  }

  /// <summary>
  /// Renders the Settings menu with separate background and foreground opacity controls.
  /// Provides sliders for independent opacity adjustment with immediate visual feedback.
  /// </summary>
  /// <returns>True if the menu is currently open, false otherwise.</returns>
  public bool Render()
  {
    bool isOpen = ImGui.BeginMenu("Settings");
    if (isOpen)
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
    return isOpen;
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
}
