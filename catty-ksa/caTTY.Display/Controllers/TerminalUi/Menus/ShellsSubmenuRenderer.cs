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
/// Handles rendering of the Shells submenu with shell configuration options.
/// Allows users to select default shell type and configure shell-specific options.
/// Only shows shells that are available on the current system.
/// </summary>
internal class ShellsSubmenuRenderer
{
  private readonly ThemeConfiguration _themeConfig;
  private readonly SessionManager _sessionManager;

  public ShellsSubmenuRenderer(
    ThemeConfiguration themeConfig,
    SessionManager sessionManager)
  {
    _themeConfig = themeConfig ?? throw new ArgumentNullException(nameof(themeConfig));
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
  }

  /// <summary>
  /// Renders the Shells submenu content with shell configuration options.
  /// Allows users to select default shell type and configure shell-specific options.
  /// Note: Parent menu handles BeginMenu/EndMenu calls.
  /// </summary>
  public void RenderContent()
  {
    RenderShellConfigurationSection();
  }

  /// <summary>
  /// Renders the shell configuration section in the Shells submenu.
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
    var availableCustomGameShells = ShellAvailabilityChecker.GetAvailableCustomGameShells();

    // Show message if no shells are available (shouldn't happen, but defensive programming)
    if (availableShells.Count == 0 && availableCustomGameShells.Count == 0)
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

    // Show custom game shells if available
    if (availableCustomGameShells.Count > 0)
    {
      ImGui.Spacing();
      ImGui.Separator();
      ImGui.Spacing();
      ImGui.Text("Custom Game Shells:");

      foreach (var (shellType, shellId, displayName) in availableCustomGameShells)
      {
        bool isSelected = config.DefaultShellType == ShellType.CustomGame && config.DefaultCustomGameShellId == shellId;

        if (ImGui.RadioButton($"{displayName}##shell_{shellId}", isSelected))
        {
          if (!isSelected)
          {
            config.DefaultShellType = ShellType.CustomGame;
            config.DefaultCustomGameShellId = shellId;
            configChanged = true;

            // Apply configuration immediately when shell type changes
            ApplyShellConfiguration();
          }
        }

        if (ImGui.IsItemHovered())
        {
          ImGui.SetTooltip("Custom game shell - Execute game commands directly");
        }
      }
    }

    // Show count of available shells for debugging
    ImGui.Spacing();
    ImGui.TextColored(new float4(0.7f, 0.7f, 0.7f, 1.0f), $"Showing {availableShells.Count + availableCustomGameShells.Count} available shell(s)");
    if (ImGui.IsItemHovered())
    {
      var standardShells = string.Join(", ", availableShells.Select(s => s.ShellType.ToString()));
      var customShells = string.Join(", ", availableCustomGameShells.Select(s => s.DisplayName));
      var allShells = string.IsNullOrEmpty(customShells) ? standardShells : $"{standardShells}; {customShells}";
      ImGui.SetTooltip($"Available shells: {allShells}");
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
