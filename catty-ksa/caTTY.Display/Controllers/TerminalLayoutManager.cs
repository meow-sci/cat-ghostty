using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using caTTY.Display.Rendering;
using caTTY.Display.Utils;

namespace caTTY.Display.Controllers;

/// <summary>
/// Manages terminal layout operations including menu bar, tab area, and dimension calculations.
/// Handles the structured layout with constrained variable sizing for multi-session UI.
/// </summary>
public class TerminalLayoutManager : ITerminalLayoutManager
{
    private readonly SessionManager _sessionManager;
    private readonly ThemeConfiguration _themeConfig;
    private TerminalRenderingConfig _config;

    // Font configuration access (for menu operations)
    private readonly Func<TerminalFontConfig> _getFontConfig;
    private readonly Action<TerminalFontConfig> _updateFontConfig;
    private readonly Action _saveFontSettings;
    private readonly Func<string> _getCurrentFontFamily;
    private readonly Action<string> _selectFontFamily;
    private readonly Action<float> _setFontSize;

    // Selection and clipboard operations
    private readonly Func<bool> _hasSelection;
    private readonly Action _copySelection;
    private readonly Action _pasteFromClipboard;
    private readonly Action _selectAllText;

    // Focus management
    private readonly Action _forceFocus;
    private readonly Action _exitApplication;

    /// <summary>
    /// Creates a new terminal layout manager with the specified dependencies.
    /// </summary>
    /// <param name="sessionManager">The session manager for multi-session operations</param>
    /// <param name="themeConfig">The theme configuration</param>
    /// <param name="config">The rendering configuration</param>
    /// <param name="getFontConfig">Function to get current font configuration</param>
    /// <param name="updateFontConfig">Action to update font configuration</param>
    /// <param name="saveFontSettings">Action to save font settings</param>
    /// <param name="getCurrentFontFamily">Function to get current font family</param>
    /// <param name="selectFontFamily">Action to select font family</param>
    /// <param name="setFontSize">Action to set font size</param>
    /// <param name="hasSelection">Function to check if text is selected</param>
    /// <param name="copySelection">Action to copy selection to clipboard</param>
    /// <param name="pasteFromClipboard">Action to paste from clipboard</param>
    /// <param name="selectAllText">Action to select all text</param>
    /// <param name="forceFocus">Action to force terminal focus</param>
    /// <param name="exitApplication">Action to exit the application</param>
    public TerminalLayoutManager(
        SessionManager sessionManager,
        ThemeConfiguration themeConfig,
        TerminalRenderingConfig config,
        Func<TerminalFontConfig> getFontConfig,
        Action<TerminalFontConfig> updateFontConfig,
        Action saveFontSettings,
        Func<string> getCurrentFontFamily,
        Action<string> selectFontFamily,
        Action<float> setFontSize,
        Func<bool> hasSelection,
        Action copySelection,
        Action pasteFromClipboard,
        Action selectAllText,
        Action forceFocus,
        Action exitApplication)
    {
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _themeConfig = themeConfig ?? throw new ArgumentNullException(nameof(themeConfig));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _getFontConfig = getFontConfig ?? throw new ArgumentNullException(nameof(getFontConfig));
        _updateFontConfig = updateFontConfig ?? throw new ArgumentNullException(nameof(updateFontConfig));
        _saveFontSettings = saveFontSettings ?? throw new ArgumentNullException(nameof(saveFontSettings));
        _getCurrentFontFamily = getCurrentFontFamily ?? throw new ArgumentNullException(nameof(getCurrentFontFamily));
        _selectFontFamily = selectFontFamily ?? throw new ArgumentNullException(nameof(selectFontFamily));
        _setFontSize = setFontSize ?? throw new ArgumentNullException(nameof(setFontSize));
        _hasSelection = hasSelection ?? throw new ArgumentNullException(nameof(hasSelection));
        _copySelection = copySelection ?? throw new ArgumentNullException(nameof(copySelection));
        _pasteFromClipboard = pasteFromClipboard ?? throw new ArgumentNullException(nameof(pasteFromClipboard));
        _selectAllText = selectAllText ?? throw new ArgumentNullException(nameof(selectAllText));
        _forceFocus = forceFocus ?? throw new ArgumentNullException(nameof(forceFocus));
        _exitApplication = exitApplication ?? throw new ArgumentNullException(nameof(exitApplication));
    }

    /// <summary>
    /// Renders the complete terminal layout including menu bar and tab area.
    /// </summary>
    public void RenderLayout()
    {
        // Render menu bar (uses UI font) - preserved for accessibility
        RenderMenuBar();

        // Render tab area for session management
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new float2(4.0f, 0.0f));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new float2(4.0f, 0.0f));
        RenderTabArea();
        // Note: PopStyleVar intentionally omitted to match original behavior
    }

    /// <summary>
    /// Calculates the available content area after accounting for UI overhead.
    /// </summary>
    /// <returns>The available content area size (width, height)</returns>
    public float2 CalculateContentArea()
    {
        float2 windowSize = ImGui.GetWindowSize();
        float uiOverheadHeight = GetUIOverheadHeight();
        float horizontalPadding = GetHorizontalPadding();

        return new float2(
            windowSize.X - horizontalPadding,
            windowSize.Y - uiOverheadHeight
        );
    }

    /// <summary>
    /// Calculates optimal terminal dimensions based on available window space.
    /// Uses character metrics to determine how many columns and rows can fit.
    /// Accounts for the complete UI layout structure: menu bar, tab area, and padding.
    /// </summary>
    /// <param name="availableSize">The available window content area size</param>
    /// <param name="characterWidth">Width of a single character in pixels</param>
    /// <param name="lineHeight">Height of a single line in pixels</param>
    /// <returns>Terminal dimensions (cols, rows) or null if invalid</returns>
    public (int cols, int rows)? CalculateTerminalDimensions(float2 availableSize, float characterWidth, float lineHeight)
    {
        try
        {
            // Calculate UI overhead for multi-session UI layout
            float totalUIOverheadHeight = GetUIOverheadHeight();
            float horizontalPadding = GetHorizontalPadding();

            float availableWidth = availableSize.X - horizontalPadding;
            float availableHeight = availableSize.Y - totalUIOverheadHeight;

            // Ensure we have positive dimensions
            if (availableWidth <= 0 || availableHeight <= 0)
            {
                return null;
            }

            // Validate character metrics
            if (characterWidth <= 0 || lineHeight <= 0)
            {
                return null;
            }

            int cols = (int)Math.Floor(availableWidth / characterWidth);
            int rows = (int)Math.Floor(availableHeight / lineHeight);

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
            Console.WriteLine($"TerminalLayoutManager: Error calculating terminal dimensions: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates layout constraints and configuration.
    /// </summary>
    /// <param name="config">The rendering configuration to use for layout calculations</param>
    public void UpdateLayoutConstraints(TerminalRenderingConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();
    }

    /// <summary>
    /// Gets the current UI overhead height (menu bar, tab area, padding).
    /// </summary>
    /// <returns>Total UI overhead height in pixels</returns>
    public float GetUIOverheadHeight()
    {
        float menuBarHeight = LayoutConstants.MENU_BAR_HEIGHT;     // 25.0f
        float tabAreaHeight = LayoutConstants.TAB_AREA_HEIGHT;     // 50.0f
        float windowPadding = LayoutConstants.WINDOW_PADDING * 2;  // Top and bottom padding

        return menuBarHeight + tabAreaHeight + windowPadding;
    }

    /// <summary>
    /// Gets the current horizontal padding for content area.
    /// </summary>
    /// <returns>Horizontal padding in pixels</returns>
    public float GetHorizontalPadding()
    {
        return LayoutConstants.WINDOW_PADDING * 2; // Left and right padding
    }

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
                    _exitApplication();
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
                bool hasSelection = _hasSelection();
                if (ImGui.MenuItem("Copy", "", false, hasSelection))
                {
                    _copySelection();
                }

                // Paste - always enabled
                if (ImGui.MenuItem("Paste"))
                {
                    _pasteFromClipboard();
                }

                // Select All - always enabled
                if (ImGui.MenuItem("Select All"))
                {
                    _selectAllText();
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
                var fontConfig = _getFontConfig();

                // Font Size Slider
                int currentFontSize = (int)fontConfig.FontSize;
                ImGui.Text("Font Size:");
                ImGui.SameLine();
                if (ImGui.SliderInt("##FontSize", ref currentFontSize, 4, 72))
                {
                    _setFontSize((float)currentFontSize);
                }

                ImGui.Separator();

                // Font Family Selection
                var availableFonts = CaTTYFontManager.GetAvailableFontFamilies();
                string currentFontFamily = _getCurrentFontFamily();

                foreach (var fontFamily in availableFonts)
                {
                    bool isSelected = fontFamily == currentFontFamily;

                    if (ImGui.MenuItem(fontFamily, "", isSelected))
                    {
                        _selectFontFamily(fontFamily);
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
                        // Console.WriteLine($"TerminalLayoutManager: Background opacity set to {newBgOpacityPercent}%");
                    }
                    else
                    {
                        Console.WriteLine($"TerminalLayoutManager: Failed to set background opacity to {newBgOpacityPercent}%");
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
                        Console.WriteLine("TerminalLayoutManager: Background opacity reset to default");
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset background opacity to 100% (fully opaque)");
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
                        // Console.WriteLine($"TerminalLayoutManager: Foreground opacity set to {newFgOpacityPercent}%");
                    }
                    else
                    {
                        Console.WriteLine($"TerminalLayoutManager: Failed to set foreground opacity to {newFgOpacityPercent}%");
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
                        Console.WriteLine("TerminalLayoutManager: Foreground opacity reset to default");
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset foreground opacity to 100% (fully opaque)");
                }

                // Reset both button
                ImGui.Separator();
                if (ImGui.Button("Reset Both##ResetBothOpacity"))
                {
                    if (OpacityManager.ResetOpacity())
                    {
                        Console.WriteLine("TerminalLayoutManager: Both opacity values reset to default");
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Reset both background and foreground opacity to 100%");
                }

                // Show current opacity status
                ImGui.Separator();
                var bgOpacity = OpacityManager.CurrentBackgroundOpacity;
                var fgOpacity = OpacityManager.CurrentForegroundOpacity;
                var bgIsDefault = OpacityManager.IsDefaultBackgroundOpacity();
                var fgIsDefault = OpacityManager.IsDefaultForegroundOpacity();

                var bgStatusText = bgIsDefault ? "Default (100%)" : $"{bgOpacity:F2} ({currentBgOpacityPercent}%)";
                var fgStatusText = fgIsDefault ? "Default (100%)" : $"{fgOpacity:F2} ({currentFgOpacityPercent}%)";

                ImGui.Text($"Background: {bgStatusText}");
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
                        _forceFocus();
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
                                                        Console.WriteLine($"TerminalLayoutManager: Failed to restart session {session.Id}: {ex.Message}");
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
            Console.WriteLine($"TerminalLayoutManager: Error rendering tab area: {ex.Message}");

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

            Console.WriteLine($"TerminalLayoutManager: Shell configuration applied: {_themeConfig.GetShellDisplayName()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalLayoutManager: Error applying shell configuration: {ex.Message}");
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
            Console.WriteLine($"TerminalLayoutManager: Applying theme: {theme.Name}");

            // Apply the theme through ThemeManager
            ThemeManager.ApplyTheme(theme);

            Console.WriteLine($"TerminalLayoutManager: Successfully applied theme: {theme.Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalLayoutManager: Failed to apply theme {theme.Name}: {ex.Message}");
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
            Console.WriteLine("TerminalLayoutManager: Refreshing themes...");

            // Refresh themes through ThemeManager
            ThemeManager.RefreshAvailableThemes();

            Console.WriteLine($"TerminalLayoutManager: Themes refreshed. Available themes: {ThemeManager.AvailableThemes.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TerminalLayoutManager: Failed to refresh themes: {ex.Message}");
        }
    }

    /// <summary>
    /// Shows a not implemented message for future features.
    /// </summary>
    /// <param name="featureName">The name of the feature that is not implemented</param>
    private static void ShowNotImplementedMessage(string featureName)
    {
        Console.WriteLine($"TerminalLayoutManager: {featureName} is not yet implemented");
    }
}