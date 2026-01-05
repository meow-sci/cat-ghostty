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
  private readonly TerminalDimensionCalculator _dimensionCalculator;
  private readonly FontResizeProcessor _fontResizeProcessor;

  public TerminalUiResize(SessionManager sessionManager, TerminalUiFonts fonts)
  {
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _fonts = fonts ?? throw new ArgumentNullException(nameof(fonts));

    // Initialize dimension calculator
    _dimensionCalculator = new TerminalDimensionCalculator(fonts);

    // Initialize window resize handler with dependencies
    _windowResizeHandler = new WindowResizeHandler(
      sessionManager,
      _dimensionCalculator.CalculateTerminalDimensions,
      ApplyTerminalDimensionsToAllSessions
    );

    // Initialize font resize processor
    _fontResizeProcessor = new FontResizeProcessor(
      sessionManager,
      _dimensionCalculator,
      _windowResizeHandler
    );
  }

  /// <summary>
  ///     Gets whether a font-triggered resize is pending.
  /// </summary>
  public bool IsFontResizePending => _fontResizeProcessor.IsFontResizePending;

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
    _fontResizeProcessor.TriggerTerminalResize();
  }

  /// <summary>
  ///     Performs the actual terminal resize calculation when font changes are pending.
  ///     Called during render frame when ImGui context is available.
  /// </summary>
  public void ProcessPendingFontResize()
  {
    _fontResizeProcessor.ProcessPendingFontResize();
  }

  /// <summary>
  ///     Triggers terminal resize for all sessions when font configuration changes.
  ///     This method ensures all sessions recalculate their dimensions with new character metrics.
  /// </summary>
  public void TriggerTerminalResizeForAllSessions()
  {
    _fontResizeProcessor.TriggerTerminalResizeForAllSessions();
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
    => TerminalDimensionCalculator.CalculateTabAreaHeight(tabCount);

  /// <summary>
  /// Calculates the current settings area height based on the number of control rows.
  /// Uses constrained sizing to prevent excessive height growth.
  /// </summary>
  /// <param name="controlRows">Number of control rows (defaults to 1 for basic settings)</param>
  /// <returns>Settings area height in pixels</returns>
  public static float CalculateSettingsAreaHeight(int controlRows = 1)
    => TerminalDimensionCalculator.CalculateSettingsAreaHeight(controlRows);

  /// <summary>
  /// Calculates the total height of all header areas (menu bar, tab area, settings area).
  /// Uses current terminal state to determine variable area heights.
  /// </summary>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Total height of header areas in pixels</returns>
  public static float CalculateHeaderHeight(int tabCount = 1, int settingsControlRows = 1)
    => TerminalDimensionCalculator.CalculateHeaderHeight(tabCount, settingsControlRows);

  /// <summary>
  /// Calculates the minimum possible header height (all areas at minimum size).
  /// Used for minimum window size calculations and initial estimates.
  /// </summary>
  /// <returns>Minimum header height in pixels</returns>
  public static float CalculateMinHeaderHeight()
    => TerminalDimensionCalculator.CalculateMinHeaderHeight();

  /// <summary>
  /// Calculates the maximum possible header height (all areas at maximum size).
  /// Used for layout validation and bounds checking.
  /// </summary>
  /// <returns>Maximum header height in pixels</returns>
  public static float CalculateMaxHeaderHeight()
    => TerminalDimensionCalculator.CalculateMaxHeaderHeight();

  /// <summary>
  /// Calculates the available space for the terminal canvas after accounting for header areas.
  /// Uses current header configuration for accurate space calculation.
  /// </summary>
  /// <param name="windowSize">Total window size</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Available size for terminal canvas</returns>
  public static float2 CalculateTerminalCanvasSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
    => TerminalDimensionCalculator.CalculateTerminalCanvasSize(windowSize, tabCount, settingsControlRows);

  /// <summary>
  /// Validates that window dimensions are sufficient for the layout with current configuration.
  /// Accounts for variable header heights in validation.
  /// </summary>
  /// <param name="windowSize">Window size to validate</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>True if window size is valid for layout</returns>
  public static bool ValidateWindowSize(float2 windowSize, int tabCount = 1, int settingsControlRows = 1)
    => TerminalDimensionCalculator.ValidateWindowSize(windowSize, tabCount, settingsControlRows);

  /// <summary>
  /// Calculates the position for the terminal canvas area.
  /// Accounts for current header height configuration.
  /// </summary>
  /// <param name="windowPos">Window position</param>
  /// <param name="tabCount">Number of terminal tabs (defaults to 1)</param>
  /// <param name="settingsControlRows">Number of settings control rows (defaults to 1)</param>
  /// <returns>Position where terminal canvas should be rendered</returns>
  public static float2 CalculateTerminalCanvasPosition(float2 windowPos, int tabCount = 1, int settingsControlRows = 1)
    => TerminalDimensionCalculator.CalculateTerminalCanvasPosition(windowPos, tabCount, settingsControlRows);

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
    => TerminalDimensionCalculator.CalculateOptimalTerminalDimensions(windowSize, charWidth, lineHeight, tabCount, settingsControlRows);
}
