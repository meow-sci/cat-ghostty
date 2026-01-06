using System;
using Brutal.ImGuiApi;
using caTTY.Core.Terminal;
using float2 = Brutal.Numerics.float2;

namespace caTTY.Display.Controllers.TerminalUi.Resize;

/// <summary>
///     Handles window-level resize detection for the terminal UI.
///     Detects ImGui window size changes, applies debouncing, and triggers terminal dimension updates.
/// </summary>
internal class WindowResizeHandler
{
  private readonly SessionManager _sessionManager;
  private readonly Func<float2, (int cols, int rows)?> _calculateDimensions;
  private readonly Action<int, int> _applyResize;

  // Window resize detection state
  private float2 _lastWindowSize = new(0, 0);
  private bool _windowSizeInitialized = false;
  private DateTime _lastResizeTime = DateTime.MinValue;
  private const float RESIZE_DEBOUNCE_SECONDS = 0.1f; // Debounce rapid resize events

  /// <summary>
  ///     Creates a new WindowResizeHandler.
  /// </summary>
  /// <param name="sessionManager">Session manager for accessing terminal dimensions</param>
  /// <param name="calculateDimensions">Function to calculate terminal dimensions from window size</param>
  /// <param name="applyResize">Action to apply resize to all sessions</param>
  public WindowResizeHandler(
    SessionManager sessionManager,
    Func<float2, (int cols, int rows)?> calculateDimensions,
    Action<int, int> applyResize)
  {
    _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
    _calculateDimensions = calculateDimensions ?? throw new ArgumentNullException(nameof(calculateDimensions));
    _applyResize = applyResize ?? throw new ArgumentNullException(nameof(applyResize));
  }

  /// <summary>
  ///     Handles window resize events by detecting size changes and triggering terminal dimension updates.
  ///     Called on every render frame to detect when the ImGui window size has changed.
  ///     Debounces rapid resize events and validates new dimensions before applying changes.
  ///     Matches the TypeScript implementation's approach of detecting display size changes
  ///     and updating both the headless terminal and the PTY process dimensions.
  /// </summary>
  public void HandleWindowResize()
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
      var newDimensions = _calculateDimensions(currentWindowSize);
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

      _applyResize(newCols, newRows);

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
  ///     Gets whether the window size has been initialized.
  /// </summary>
  public bool IsWindowSizeInitialized => _windowSizeInitialized;

  /// <summary>
  ///     Gets the last window size for use in calculations.
  ///     Used by font resize operations that need the last known window size.
  /// </summary>
  public float2 LastWindowSize => _lastWindowSize;
}
