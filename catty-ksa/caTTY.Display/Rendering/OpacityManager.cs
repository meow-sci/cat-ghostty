using System;
using caTTY.Display.Configuration;

namespace caTTY.Display.Rendering;

/// <summary>
/// Manages separate opacity settings for background and foreground colors in the terminal display.
/// Provides opacity control, validation, persistence, and change notifications for both color types.
/// </summary>
public static class OpacityManager
{
    /// <summary>
    /// Event fired when the background opacity changes.
    /// </summary>
    public static event Action<float>? BackgroundOpacityChanged;

    /// <summary>
    /// Event fired when the foreground opacity changes.
    /// </summary>
    public static event Action<float>? ForegroundOpacityChanged;

    /// <summary>
    /// Current background opacity value (0.0 to 1.0).
    /// </summary>
    private static float _currentBackgroundOpacity = 1.0f;

    /// <summary>
    /// Current foreground opacity value (0.0 to 1.0).
    /// </summary>
    private static float _currentForegroundOpacity = 1.0f;

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
    /// Gets the current background opacity value.
    /// </summary>
    public static float CurrentBackgroundOpacity => _currentBackgroundOpacity;

    /// <summary>
    /// Gets the current foreground opacity value.
    /// </summary>
    public static float CurrentForegroundOpacity => _currentForegroundOpacity;

    /// <summary>
    /// Gets the current global opacity value (for backward compatibility).
    /// Returns the background opacity as the primary opacity value.
    /// </summary>
    public static float CurrentOpacity => _currentBackgroundOpacity;

    /// <summary>
    /// Initialize the opacity manager by loading the saved opacity settings.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var config = ThemeConfiguration.Load();

            // Use the separate values
            _currentBackgroundOpacity = ValidateOpacity(config.BackgroundOpacity);
            _currentForegroundOpacity = ValidateOpacity(config.ForegroundOpacity);

#pragma warning restore CS0618 // Type or member is obsolete
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing opacity manager: {ex.Message}");
            _currentBackgroundOpacity = DefaultOpacity;
            _currentForegroundOpacity = DefaultOpacity;
        }
    }

    /// <summary>
    /// Set the background opacity value with validation and persistence.
    /// </summary>
    /// <param name="opacity">New background opacity value (0.0 to 1.0)</param>
    /// <returns>True if opacity was successfully set, false if invalid</returns>
    public static bool SetBackgroundOpacity(float opacity)
    {
        var validatedOpacity = ValidateOpacity(opacity);

        // Only update if the value actually changed
        if (Math.Abs(_currentBackgroundOpacity - validatedOpacity) < 0.001f)
        {
            return true; // No change needed
        }

        var previousOpacity = _currentBackgroundOpacity;
        _currentBackgroundOpacity = validatedOpacity;

        try
        {
            // Persist the change
            SaveOpacityToConfiguration();

            // Notify listeners of the change
            BackgroundOpacityChanged?.Invoke(_currentBackgroundOpacity);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting background opacity: {ex.Message}");

            // Revert on failure
            _currentBackgroundOpacity = previousOpacity;
            return false;
        }
    }

    /// <summary>
    /// Set the foreground opacity value with validation and persistence.
    /// </summary>
    /// <param name="opacity">New foreground opacity value (0.0 to 1.0)</param>
    /// <returns>True if opacity was successfully set, false if invalid</returns>
    public static bool SetForegroundOpacity(float opacity)
    {
        var validatedOpacity = ValidateOpacity(opacity);

        // Only update if the value actually changed
        if (Math.Abs(_currentForegroundOpacity - validatedOpacity) < 0.001f)
        {
            return true; // No change needed
        }

        var previousOpacity = _currentForegroundOpacity;
        _currentForegroundOpacity = validatedOpacity;

        try
        {
            // Persist the change
            SaveOpacityToConfiguration();

            // Notify listeners of the change
            ForegroundOpacityChanged?.Invoke(_currentForegroundOpacity);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting foreground opacity: {ex.Message}");

            // Revert on failure
            _currentForegroundOpacity = previousOpacity;
            return false;
        }
    }

    /// <summary>
    /// Set the global opacity value (for backward compatibility).
    /// Sets both background and foreground opacity to the same value.
    /// </summary>
    /// <param name="opacity">New opacity value (0.0 to 1.0)</param>
    /// <returns>True if opacity was successfully set, false if invalid</returns>
    public static bool SetOpacity(float opacity)
    {
        var backgroundResult = SetBackgroundOpacity(opacity);
        var foregroundResult = SetForegroundOpacity(opacity);
        return backgroundResult && foregroundResult;
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
    /// Reset both background and foreground opacity to default values.
    /// </summary>
    /// <returns>True if reset was successful</returns>
    public static bool ResetOpacity()
    {
        var backgroundResult = SetBackgroundOpacity(DefaultOpacity);
        var foregroundResult = SetForegroundOpacity(DefaultOpacity);
        return backgroundResult && foregroundResult;
    }

    /// <summary>
    /// Reset background opacity to default value.
    /// </summary>
    /// <returns>True if reset was successful</returns>
    public static bool ResetBackgroundOpacity()
    {
        return SetBackgroundOpacity(DefaultOpacity);
    }

    /// <summary>
    /// Reset foreground opacity to default value.
    /// </summary>
    /// <returns>True if reset was successful</returns>
    public static bool ResetForegroundOpacity()
    {
        return SetForegroundOpacity(DefaultOpacity);
    }

    /// <summary>
    /// Check if the current background opacity is at the default value.
    /// </summary>
    /// <returns>True if background opacity is at default value</returns>
    public static bool IsDefaultBackgroundOpacity()
    {
        return Math.Abs(_currentBackgroundOpacity - DefaultOpacity) < 0.001f;
    }

    /// <summary>
    /// Check if the current foreground opacity is at the default value.
    /// </summary>
    /// <returns>True if foreground opacity is at default value</returns>
    public static bool IsDefaultForegroundOpacity()
    {
        return Math.Abs(_currentForegroundOpacity - DefaultOpacity) < 0.001f;
    }

    /// <summary>
    /// Check if both opacity values are at default (for backward compatibility).
    /// </summary>
    /// <returns>True if both opacity values are at default</returns>
    public static bool IsDefaultOpacity()
    {
        return IsDefaultBackgroundOpacity() && IsDefaultForegroundOpacity();
    }

    /// <summary>
    /// Get background opacity as a percentage (0-100).
    /// </summary>
    /// <returns>Background opacity as percentage</returns>
    public static int GetBackgroundOpacityPercentage()
    {
        return (int)Math.Round(_currentBackgroundOpacity * 100.0f);
    }

    /// <summary>
    /// Get foreground opacity as a percentage (0-100).
    /// </summary>
    /// <returns>Foreground opacity as percentage</returns>
    public static int GetForegroundOpacityPercentage()
    {
        return (int)Math.Round(_currentForegroundOpacity * 100.0f);
    }

    /// <summary>
    /// Get opacity as a percentage (0-100) - backward compatibility.
    /// Returns background opacity percentage.
    /// </summary>
    /// <returns>Opacity as percentage</returns>
    public static int GetOpacityPercentage()
    {
        return GetBackgroundOpacityPercentage();
    }

    /// <summary>
    /// Set background opacity from percentage (0-100).
    /// </summary>
    /// <param name="percentage">Percentage value (0-100)</param>
    /// <returns>True if opacity was successfully set</returns>
    public static bool SetBackgroundOpacityFromPercentage(int percentage)
    {
        var opacity = Math.Clamp(percentage, 0, 100) / 100.0f;
        return SetBackgroundOpacity(opacity);
    }

    /// <summary>
    /// Set foreground opacity from percentage (0-100).
    /// </summary>
    /// <param name="percentage">Percentage value (0-100)</param>
    /// <returns>True if opacity was successfully set</returns>
    public static bool SetForegroundOpacityFromPercentage(int percentage)
    {
        var opacity = Math.Clamp(percentage, 0, 100) / 100.0f;
        return SetForegroundOpacity(opacity);
    }

    /// <summary>
    /// Set opacity from percentage (0-100) - backward compatibility.
    /// Sets both background and foreground opacity.
    /// </summary>
    /// <param name="percentage">Percentage value (0-100)</param>
    /// <returns>True if opacity was successfully set</returns>
    public static bool SetOpacityFromPercentage(int percentage)
    {
        var opacity = Math.Clamp(percentage, 0, 100) / 100.0f;
        return SetOpacity(opacity);
    }

    /// <summary>
    /// Save the current opacity values to the configuration file.
    /// </summary>
    private static void SaveOpacityToConfiguration()
    {
        var config = ThemeConfiguration.Load();
        config.BackgroundOpacity = _currentBackgroundOpacity;
        config.ForegroundOpacity = _currentForegroundOpacity;
        config.Save();
    }

    /// <summary>
    /// Apply background opacity to a color value.
    /// </summary>
    /// <param name="color">Original color</param>
    /// <returns>Color with background opacity applied</returns>
    public static Brutal.Numerics.float4 ApplyBackgroundOpacity(Brutal.Numerics.float4 color)
    {
        return new Brutal.Numerics.float4(color.X, color.Y, color.Z, color.W * _currentBackgroundOpacity);
    }

    /// <summary>
    /// Apply foreground opacity to a color value.
    /// </summary>
    /// <param name="color">Original color</param>
    /// <returns>Color with foreground opacity applied</returns>
    public static Brutal.Numerics.float4 ApplyForegroundOpacity(Brutal.Numerics.float4 color)
    {
        return new Brutal.Numerics.float4(color.X, color.Y, color.Z, color.W * _currentForegroundOpacity);
    }

    /// <summary>
    /// Apply opacity to a color value (backward compatibility).
    /// Uses background opacity by default.
    /// </summary>
    /// <param name="color">Original color</param>
    /// <returns>Color with opacity applied</returns>
    public static Brutal.Numerics.float4 ApplyOpacity(Brutal.Numerics.float4 color)
    {
        return ApplyBackgroundOpacity(color);
    }

    /// <summary>
    /// Apply background opacity to an alpha value.
    /// </summary>
    /// <param name="alpha">Original alpha value</param>
    /// <returns>Alpha with background opacity applied</returns>
    public static float ApplyBackgroundOpacity(float alpha)
    {
        return alpha * _currentBackgroundOpacity;
    }

    /// <summary>
    /// Apply foreground opacity to an alpha value.
    /// </summary>
    /// <param name="alpha">Original alpha value</param>
    /// <returns>Alpha with foreground opacity applied</returns>
    public static float ApplyForegroundOpacity(float alpha)
    {
        return alpha * _currentForegroundOpacity;
    }

    /// <summary>
    /// Apply opacity to an alpha value (backward compatibility).
    /// Uses background opacity by default.
    /// </summary>
    /// <param name="alpha">Original alpha value</param>
    /// <returns>Alpha with opacity applied</returns>
    public static float ApplyOpacity(float alpha)
    {
        return ApplyBackgroundOpacity(alpha);
    }
}
