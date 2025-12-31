using System;
using caTTY.Display.Configuration;

namespace caTTY.Display.Rendering;

/// <summary>
/// Manages global opacity settings for the terminal display.
/// Provides opacity control, validation, persistence, and change notifications.
/// </summary>
public static class OpacityManager
{
    /// <summary>
    /// Event fired when the global opacity changes.
    /// </summary>
    public static event Action<float>? OpacityChanged;

    /// <summary>
    /// Current global opacity value (0.0 to 1.0).
    /// </summary>
    private static float _currentOpacity = 1.0f;

    /// <summary>
    /// Minimum allowed opacity value.
    /// </summary>
    public const float MinOpacity = 0.0f;

    /// <summary>
    /// Maximum allowed opacity value.
    /// </summary>
    public const float MaxOpacity = 1.0f;

    /// <summary>
    /// Default opacity value.
    /// </summary>
    public const float DefaultOpacity = 1.0f;

    /// <summary>
    /// Gets the current global opacity value.
    /// </summary>
    public static float CurrentOpacity => _currentOpacity;

    /// <summary>
    /// Initialize the opacity manager by loading the saved opacity setting.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var config = ThemeConfiguration.Load();
            _currentOpacity = ValidateOpacity(config.GlobalOpacity);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing opacity manager: {ex.Message}");
            _currentOpacity = DefaultOpacity;
        }
    }

    /// <summary>
    /// Set the global opacity value with validation and persistence.
    /// </summary>
    /// <param name="opacity">New opacity value (0.0 to 1.0)</param>
    /// <returns>True if opacity was successfully set, false if invalid</returns>
    public static bool SetOpacity(float opacity)
    {
        var validatedOpacity = ValidateOpacity(opacity);
        
        // Only update if the value actually changed
        if (Math.Abs(_currentOpacity - validatedOpacity) < 0.001f)
        {
            return true; // No change needed
        }

        var previousOpacity = _currentOpacity;
        _currentOpacity = validatedOpacity;

        try
        {
            // Persist the change
            SaveOpacityToConfiguration();
            
            // Notify listeners of the change
            OpacityChanged?.Invoke(_currentOpacity);
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting opacity: {ex.Message}");
            
            // Revert on failure
            _currentOpacity = previousOpacity;
            return false;
        }
    }

    /// <summary>
    /// Validate and clamp opacity value to valid range.
    /// </summary>
    /// <param name="opacity">Opacity value to validate</param>
    /// <returns>Validated opacity value clamped to valid range</returns>
    public static float ValidateOpacity(float opacity)
    {
        // Handle NaN and infinity
        if (float.IsNaN(opacity) || float.IsInfinity(opacity))
        {
            return DefaultOpacity;
        }

        // Clamp to valid range
        return Math.Clamp(opacity, MinOpacity, MaxOpacity);
    }

    /// <summary>
    /// Reset opacity to default value.
    /// </summary>
    /// <returns>True if reset was successful</returns>
    public static bool ResetOpacity()
    {
        return SetOpacity(DefaultOpacity);
    }

    /// <summary>
    /// Check if the current opacity is at the default value.
    /// </summary>
    /// <returns>True if opacity is at default value</returns>
    public static bool IsDefaultOpacity()
    {
        return Math.Abs(_currentOpacity - DefaultOpacity) < 0.001f;
    }

    /// <summary>
    /// Get opacity as a percentage (0-100).
    /// </summary>
    /// <returns>Opacity as percentage</returns>
    public static int GetOpacityPercentage()
    {
        return (int)Math.Round(_currentOpacity * 100.0f);
    }

    /// <summary>
    /// Set opacity from percentage (0-100).
    /// </summary>
    /// <param name="percentage">Percentage value (0-100)</param>
    /// <returns>True if opacity was successfully set</returns>
    public static bool SetOpacityFromPercentage(int percentage)
    {
        var opacity = Math.Clamp(percentage, 0, 100) / 100.0f;
        return SetOpacity(opacity);
    }

    /// <summary>
    /// Save the current opacity to the configuration file.
    /// </summary>
    private static void SaveOpacityToConfiguration()
    {
        var config = ThemeConfiguration.Load();
        config.GlobalOpacity = _currentOpacity;
        config.Save();
    }

    /// <summary>
    /// Apply opacity to a color value.
    /// </summary>
    /// <param name="color">Original color</param>
    /// <returns>Color with opacity applied</returns>
    public static Brutal.Numerics.float4 ApplyOpacity(Brutal.Numerics.float4 color)
    {
        return new Brutal.Numerics.float4(color.X, color.Y, color.Z, color.W * _currentOpacity);
    }

    /// <summary>
    /// Apply opacity to an alpha value.
    /// </summary>
    /// <param name="alpha">Original alpha value</param>
    /// <returns>Alpha with opacity applied</returns>
    public static float ApplyOpacity(float alpha)
    {
        return alpha * _currentOpacity;
    }
}