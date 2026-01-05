using System;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using caTTY.Display.Controllers.TerminalUi.Resize;
using KSA;
using float2 = Brutal.Numerics.float2;

namespace caTTY.Display.Controllers.TerminalUi;

/// <summary>
///     Handles window resize detection, dimension calculations, and terminal resize operations for the terminal UI.
/// </summary>
internal class TerminalUiResize
{
  private readonly SessionManager _sessionManager;
  private readonly TerminalUiFonts _fonts;
  private readonly WindowResizeHandler _windowResizeHandler;

  // Font resize tracking
  private bool _fontResizePending = false; // Flag to trigger resize on next render frame

  public TerminalUiResize(SessionManager sessionManager, TerminalUiFonts fonts)
  {
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));

    // Initialize window resize handler with dependencies
    _windowResizeHandler = new WindowResizeHandler(
      sessionManager,
      CalculateTerminalDimensions,
      ApplyTerminalDimensionsToAllSessions
    );
  }

  /// <summary>
  ///     Gets whether a font-triggered resize is pending.
  /// </summary>
  public bool IsFontResizePending => _fontResizePending;

  /// <summary>
  ///     Handles window resize events by detecting size changes and triggering terminal dimension updates.
  ///     Called on every render frame to detect when the ImGui window size has changed.
  ///     Debounces rapid resize events and validates new dimensions before applying changes.
  ///     Matches the TypeScript implementation's approach of detecting display size changes
  ///     and updating both the headless terminal and the PTY process dimensions.
  /// </summary>
  public void HandleWindowResize()
  {
    _windowResizeHandler.HandleWindowResize();
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
      if (_fonts.CurrentCharacterWidth <= 0 || _fonts.CurrentLineHeight <= 0)
      {
        Console.WriteLine($"TerminalController: Invalid character metrics: width={_fonts.CurrentCharacterWidth}, height={_fonts.CurrentLineHeight}");
        return null;
      }

      int cols = (int)Math.Floor(availableWidth / _fonts.CurrentCharacterWidth);
      int rows = (int)Math.Floor(availableHeight / _fonts.CurrentLineHeight);

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
    return _windowResizeHandler.GetCurrentWindowSize();
  }

  /// <summary>
  ///     Triggers terminal resize calculation based on current window size and updated character metrics.
  ///     This method is called when font configuration changes to ensure terminal dimensions
  ///     are recalculated with the new character metrics without requiring manual window resize.
  /// </summary>
  public void TriggerTerminalResize()
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
  public void ProcessPendingFontResize()
  {
    if (!_fontResizePending)
      return;

    try
    {
      // Get current window size (we're now in ImGui render context)
      float2 currentWindowSize = ImGui.GetWindowSize();

      // Skip if window size is not initialized or invalid
      if (!_windowResizeHandler.IsWindowSizeInitialized || currentWindowSize.X <= 0 || currentWindowSize.Y <= 0)
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
  public void TriggerTerminalResizeForAllSessions()
  {
    try
    {
      // Use the last known window size instead of trying to get current window size
      // This avoids ImGui context issues when called from font configuration updates
      float2 currentWindowSize = _windowResizeHandler.LastWindowSize;

      // Skip if window size is not initialized or invalid
      if (!_windowResizeHandler.IsWindowSizeInitialized || currentWindowSize.X <= 0 || currentWindowSize.Y <= 0)
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
  /// Calculates the current tab area height based on the number of terminal instances.
  /// Uses constrained sizing to prevent excessive height growth.
  /// </summary>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1 for current single terminal)</param>
  /// <returns>Tab area height in pixels</returns>
  public static float CalculateTabAreaHeight(int tabCount = 1)
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
  public static float CalculateSettingsAreaHeight(int controlRows = 1)
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
  public static float CalculateHeaderHeight(int tabCount = 1, int settingsControlRows = 1)
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
  public static float CalculateMinHeaderHeight()
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
  public static float CalculateMaxHeaderHeight()
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
  public static float2 CalculateTerminalCanvasSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
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
  public static bool ValidateWindowSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
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
  public static float2 CalculateTerminalCanvasPosition(float2 windowPos, int tabCount = 1, int settingsControlRows = 1)
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
  public static (int cols, int rows)? CalculateOptimalTerminalDimensions(
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
}
