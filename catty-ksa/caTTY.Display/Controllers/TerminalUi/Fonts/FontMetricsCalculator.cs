using System;
using Brutal.ImGuiApi;
using caTTY.Display.Configuration;

namespace caTTY.Display.Controllers.TerminalUi.Fonts;

/// <summary>
///     Calculates font metrics (character width, line height) for terminal rendering.
/// </summary>
internal class FontMetricsCalculator
{
  private readonly TerminalRenderingConfig _config;

  // Current metrics
  private float _currentCharacterWidth;
  private float _currentLineHeight;

  public FontMetricsCalculator(TerminalRenderingConfig config)
  {
    _config = config ?? throw new ArgumentNullException(nameof(config));

    // Initialize with DPI-based defaults from config
    _currentCharacterWidth = _config.CharacterWidth;
    _currentLineHeight = _config.LineHeight;
  }

  /// <summary>
  ///     Gets the current character width.
  /// </summary>
  public float CurrentCharacterWidth => _currentCharacterWidth;

  /// <summary>
  ///     Gets the current line height.
  /// </summary>
  public float CurrentLineHeight => _currentLineHeight;

  /// <summary>
  ///     Calculates character metrics from the loaded fonts.
  /// </summary>
  /// <param name="regularFont">The regular font to use for metric calculations</param>
  /// <param name="fontSize">The font size to use for calculations</param>
  public void CalculateCharacterMetrics(ImFontPtr regularFont, float fontSize)
  {
    try
    {
      Console.WriteLine($"FontMetricsCalculator: Calculating character metrics using font size: {fontSize}");

      // Use the regular font for metric calculations
      ImGui.PushFont(regularFont, fontSize);

      try
      {
        // Calculate character width using multiple test characters to ensure accuracy
        var testChars = new[] { 'M', 'W', '@', '#' }; // Wide characters for accurate measurement
        float maxWidth = 0.0f;

        foreach (char testChar in testChars)
        {
          var textSize = ImGui.CalcTextSize(testChar.ToString());
          maxWidth = Math.Max(maxWidth, textSize.X);
        }

        // Use the maximum width found to ensure all characters fit properly
        _currentCharacterWidth = (float)Math.Floor(maxWidth);

        // Calculate line height using a standard character
        var lineSize = ImGui.CalcTextSize("M");
        // CRITICAL FIX: Use exact font height without extra spacing to prevent gaps between rows
        // Terminal emulators should have tight line spacing with no gaps
        _currentLineHeight = (float)Math.Round(lineSize.Y);

        Console.WriteLine($"FontMetricsCalculator: Calculated metrics from font - CharWidth: {_currentCharacterWidth:F1}, LineHeight: {_currentLineHeight:F1}");
      }
      finally
      {
        ImGui.PopFont();
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine($"FontMetricsCalculator: Error calculating character metrics: {ex.Message}");

      // Fallback to DPI-based metrics from config
      _currentCharacterWidth = _config.CharacterWidth;
      _currentLineHeight = _config.LineHeight;

      Console.WriteLine($"FontMetricsCalculator: Using fallback metrics from config - CharWidth: {_currentCharacterWidth:F1}, LineHeight: {_currentLineHeight:F1}");
    }
  }

  /// <summary>
  ///     Resets metrics to DPI-based defaults from config.
  /// </summary>
  public void ResetToDefaults()
  {
    _currentCharacterWidth = _config.CharacterWidth;
    _currentLineHeight = _config.LineHeight;
  }
}
