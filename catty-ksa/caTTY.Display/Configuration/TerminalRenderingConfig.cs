namespace caTTY.Display.Configuration;

/// <summary>
///     Configuration class that encapsulates all rendering-related settings for the terminal controller.
///     This allows different execution contexts (TestApp vs GameMod) to provide appropriate metrics
///     without changing the core rendering logic.
/// </summary>
public class TerminalRenderingConfig
{
    /// <summary>
    ///     Gets or sets the font size in points for terminal text rendering.
    ///     Default value is 16.0f.
    /// </summary>
    public float FontSize { get; set; } = 32.0f;

    /// <summary>
    ///     Gets or sets the character width in pixels for monospace character spacing.
    ///     Default value is 9.6f (typical monospace approximation).
    /// </summary>
    public float CharacterWidth { get; set; } = 19.2f;

    /// <summary>
    ///     Gets or sets the line height in pixels for vertical character spacing.
    ///     Default value is 18.0f.
    /// </summary>
    public float LineHeight { get; set; } = 36.0f;

    /// <summary>
    ///     Gets or sets whether to automatically detect DPI scaling from the execution context.
    ///     When true, the system will attempt to detect and compensate for DPI scaling.
    ///     Default value is true.
    /// </summary>
    public bool AutoDetectDpiScaling { get; set; } = true;

    /// <summary>
    ///     Gets or sets the DPI scaling factor applied by the system or game engine.
    ///     This value is used for debugging and logging purposes.
    ///     Default value is 1.0f (no scaling).
    /// </summary>
    public float DpiScalingFactor { get; set; } = 1.0f;

    /// <summary>
    ///     Creates a configuration optimized for the standalone TestApp context.
    ///     Uses standard metrics without DPI scaling compensation.
    /// </summary>
    /// <returns>A TerminalRenderingConfig configured for TestApp usage.</returns>
    public static TerminalRenderingConfig CreateForTestApp()
    {
        return new TerminalRenderingConfig
        {
            FontSize = 32.0f,
            CharacterWidth = 19.2f,
            LineHeight = 36.0f,
            AutoDetectDpiScaling = false,
            DpiScalingFactor = 1.0f
        };
    }

    /// <summary>
    ///     Creates a configuration optimized for the GameMod context with DPI scaling compensation.
    ///     Applies scaling compensation by dividing standard metrics by the DPI scale factor.
    /// </summary>
    /// <param name="dpiScale">The DPI scaling factor to compensate for (typically 2.0f for 200% scaling).</param>
    /// <returns>A TerminalRenderingConfig configured for GameMod usage with DPI compensation.</returns>
    /// <exception cref="ArgumentException">Thrown when dpiScale is less than or equal to 0.</exception>
    public static TerminalRenderingConfig CreateForGameMod(float dpiScale = 1.0f)
    {
        if (dpiScale <= 0)
        {
            throw new ArgumentException("DPI scale factor must be greater than 0", nameof(dpiScale));
        }

        return new TerminalRenderingConfig
        {
            FontSize = 32.0f / dpiScale,
            CharacterWidth = 19.2f / dpiScale,
            LineHeight = 36.0f / dpiScale,
            AutoDetectDpiScaling = false,
            DpiScalingFactor = dpiScale
        };
    }

    /// <summary>
    ///     Creates a default configuration that uses automatic DPI detection.
    ///     This configuration will attempt to detect the execution context and apply appropriate settings.
    /// </summary>
    /// <returns>A TerminalRenderingConfig with automatic detection enabled.</returns>
    public static TerminalRenderingConfig CreateDefault()
    {
        return new TerminalRenderingConfig
        {
            FontSize = 32.0f,
            CharacterWidth = 19.2f,
            LineHeight = 36.0f,
            AutoDetectDpiScaling = true,
            DpiScalingFactor = 1.0f
        };
    }

    /// <summary>
    ///     Validates that all character metrics are within reasonable bounds.
    ///     This method ensures that the configuration values will produce usable terminal rendering.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any metric is outside acceptable bounds.</exception>
    public void Validate()
    {
        if (FontSize <= 0 || FontSize > 128)
        {
            throw new ArgumentException($"FontSize must be between 0 and 128, but was {FontSize}", nameof(FontSize));
        }

        if (CharacterWidth <= 0 || CharacterWidth > 100)
        {
            throw new ArgumentException($"CharacterWidth must be between 0 and 50, but was {CharacterWidth}",
                nameof(CharacterWidth));
        }

        if (LineHeight <= 0 || LineHeight > 100)
        {
            throw new ArgumentException($"LineHeight must be between 0 and 100, but was {LineHeight}",
                nameof(LineHeight));
        }

        if (DpiScalingFactor <= 0)
        {
            throw new ArgumentException($"DpiScalingFactor must be greater than 0, but was {DpiScalingFactor}",
                nameof(DpiScalingFactor));
        }
    }

    /// <summary>
    ///     Creates a copy of this configuration with the specified modifications.
    ///     This method provides a fluent interface for creating configuration variants.
    /// </summary>
    /// <param name="fontSize">Optional new font size. If null, uses current value.</param>
    /// <param name="characterWidth">Optional new character width. If null, uses current value.</param>
    /// <param name="lineHeight">Optional new line height. If null, uses current value.</param>
    /// <param name="dpiScalingFactor">Optional new DPI scaling factor. If null, uses current value.</param>
    /// <returns>A new TerminalRenderingConfig with the specified modifications.</returns>
    public TerminalRenderingConfig WithModifications(
        float? fontSize = null,
        float? characterWidth = null,
        float? lineHeight = null,
        float? dpiScalingFactor = null)
    {
        return new TerminalRenderingConfig
        {
            FontSize = fontSize ?? FontSize,
            CharacterWidth = characterWidth ?? CharacterWidth,
            LineHeight = lineHeight ?? LineHeight,
            AutoDetectDpiScaling = AutoDetectDpiScaling,
            DpiScalingFactor = dpiScalingFactor ?? DpiScalingFactor
        };
    }

    /// <summary>
    ///     Returns a string representation of this configuration for debugging purposes.
    /// </summary>
    /// <returns>A formatted string containing all configuration values.</returns>
    public override string ToString()
    {
        return $"TerminalRenderingConfig {{ FontSize={FontSize:F1}, CharacterWidth={CharacterWidth:F1}, " +
               $"LineHeight={LineHeight:F1}, AutoDetectDpiScaling={AutoDetectDpiScaling}, " +
               $"DpiScalingFactor={DpiScalingFactor:F1} }}";
    }
}
