using Brutal.Numerics;
using caTTY.Display.Configuration;

namespace caTTY.Display.Controllers;

/// <summary>
/// Interface for managing terminal layout operations including menu bar, tab area, and dimension calculations.
/// Provides abstraction for layout management to support different UI modes and testing.
/// </summary>
public interface ITerminalLayoutManager
{
    /// <summary>
    /// Renders the complete terminal layout including menu bar and tab area.
    /// </summary>
    void RenderLayout();

    /// <summary>
    /// Calculates the available content area after accounting for UI overhead.
    /// </summary>
    /// <returns>The available content area size (width, height)</returns>
    float2 CalculateContentArea();

    /// <summary>
    /// Calculates optimal terminal dimensions based on available window space.
    /// Uses character metrics to determine how many columns and rows can fit.
    /// </summary>
    /// <param name="availableSize">The available window content area size</param>
    /// <param name="characterWidth">Width of a single character in pixels</param>
    /// <param name="lineHeight">Height of a single line in pixels</param>
    /// <returns>Terminal dimensions (cols, rows) or null if invalid</returns>
    (int cols, int rows)? CalculateTerminalDimensions(float2 availableSize, float characterWidth, float lineHeight);

    /// <summary>
    /// Updates layout constraints and configuration.
    /// </summary>
    /// <param name="config">The rendering configuration to use for layout calculations</param>
    void UpdateLayoutConstraints(TerminalRenderingConfig config);

    /// <summary>
    /// Gets the current UI overhead height (menu bar, tab area, padding).
    /// </summary>
    /// <returns>Total UI overhead height in pixels</returns>
    float GetUIOverheadHeight();

    /// <summary>
    /// Gets the current horizontal padding for content area.
    /// </summary>
    /// <returns>Horizontal padding in pixels</returns>
    float GetHorizontalPadding();
}