using caTTY.Core.Types;
using Brutal.Numerics;

namespace caTTY.Display.Rendering;

/// <summary>
/// Color resolver for different SGR color types.
/// Handles ANSI colors, 256-color palette, and RGB colors.
/// Based on TypeScript ColorResolver implementation.
/// </summary>
public static class ColorResolver
{
    /// <summary>
    /// Standard 16 ANSI color values mapped to float4 colors.
    /// These match the TypeScript implementation's color variables.
    /// </summary>
    private static readonly float4[] StandardColors = 
    [
        new(0.0f, 0.0f, 0.0f, 1.0f),       // 0: Black
        new(0.667f, 0.0f, 0.0f, 1.0f),    // 1: Red
        new(0.0f, 0.667f, 0.0f, 1.0f),    // 2: Green
        new(0.667f, 0.667f, 0.0f, 1.0f),  // 3: Yellow
        new(0.0f, 0.0f, 0.667f, 1.0f),    // 4: Blue
        new(0.667f, 0.0f, 0.667f, 1.0f),  // 5: Magenta
        new(0.0f, 0.667f, 0.667f, 1.0f),  // 6: Cyan
        new(0.667f, 0.667f, 0.667f, 1.0f), // 7: White
        new(0.333f, 0.333f, 0.333f, 1.0f), // 8: Bright Black
        new(1.0f, 0.333f, 0.333f, 1.0f),   // 9: Bright Red
        new(0.333f, 1.0f, 0.333f, 1.0f),   // 10: Bright Green
        new(1.0f, 1.0f, 0.333f, 1.0f),     // 11: Bright Yellow
        new(0.333f, 0.333f, 1.0f, 1.0f),   // 12: Bright Blue
        new(1.0f, 0.333f, 1.0f, 1.0f),     // 13: Bright Magenta
        new(0.333f, 1.0f, 1.0f, 1.0f),     // 14: Bright Cyan
        new(1.0f, 1.0f, 1.0f, 1.0f)        // 15: Bright White
    ];

    /// <summary>
    /// Default terminal colors.
    /// </summary>
    public static readonly float4 DefaultForeground = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly float4 DefaultBackground = new(0.0f, 0.0f, 0.0f, 1.0f);

    /// <summary>
    /// Resolve SGR color type to ImGui float4 color value.
    /// </summary>
    /// <param name="color">SGR color specification</param>
    /// <param name="isBackground">Whether this is a background color (affects defaults)</param>
    /// <returns>ImGui float4 color value</returns>
    public static float4 Resolve(caTTY.Core.Types.Color? color, bool isBackground = false)
    {
        if (!color.HasValue)
        {
            return isBackground ? DefaultBackground : DefaultForeground;
        }

        return color.Value.Type switch
        {
            ColorType.Named => ResolveNamedColor(color.Value.NamedColor),
            ColorType.Indexed => ResolveIndexedColor(color.Value.Index),
            ColorType.Rgb => ResolveRgbColor(color.Value.Red, color.Value.Green, color.Value.Blue),
            _ => isBackground ? DefaultBackground : DefaultForeground
        };
    }

    /// <summary>
    /// Resolve named ANSI color to float4 color.
    /// </summary>
    private static float4 ResolveNamedColor(NamedColor namedColor)
    {
        return namedColor switch
        {
            NamedColor.Black => StandardColors[0],
            NamedColor.Red => StandardColors[1],
            NamedColor.Green => StandardColors[2],
            NamedColor.Yellow => StandardColors[3],
            NamedColor.Blue => StandardColors[4],
            NamedColor.Magenta => StandardColors[5],
            NamedColor.Cyan => StandardColors[6],
            NamedColor.White => StandardColors[7],
            NamedColor.BrightBlack => StandardColors[8],
            NamedColor.BrightRed => StandardColors[9],
            NamedColor.BrightGreen => StandardColors[10],
            NamedColor.BrightYellow => StandardColors[11],
            NamedColor.BrightBlue => StandardColors[12],
            NamedColor.BrightMagenta => StandardColors[13],
            NamedColor.BrightCyan => StandardColors[14],
            NamedColor.BrightWhite => StandardColors[15],
            _ => DefaultForeground
        };
    }

    /// <summary>
    /// Resolve indexed color (256-color palette) to float4 color.
    /// </summary>
    private static float4 ResolveIndexedColor(byte index)
    {
        // Standard 16 colors (0-15)
        if (index <= 15)
        {
            return StandardColors[index];
        }

        // 216 color cube (16-231)
        if (index >= 16 && index <= 231)
        {
            return GetCubeColor(index - 16);
        }

        // Grayscale ramp (232-255)
        if (index >= 232 && index <= 255)
        {
            return GetGrayscaleColor(index - 232);
        }

        // Invalid index, return default
        return DefaultForeground;
    }

    /// <summary>
    /// Generate color from 6x6x6 color cube.
    /// </summary>
    /// <param name="cubeIndex">Index in the color cube (0-215)</param>
    /// <returns>float4 color</returns>
    private static float4 GetCubeColor(int cubeIndex)
    {
        int r = cubeIndex / 36;
        int g = (cubeIndex % 36) / 6;
        int b = cubeIndex % 6;

        static float ToColorValue(int n) => n == 0 ? 0.0f : (55 + n * 40) / 255.0f;

        return new float4(ToColorValue(r), ToColorValue(g), ToColorValue(b), 1.0f);
    }

    /// <summary>
    /// Generate grayscale color.
    /// </summary>
    /// <param name="grayIndex">Index in grayscale ramp (0-23)</param>
    /// <returns>float4 color</returns>
    private static float4 GetGrayscaleColor(int grayIndex)
    {
        float gray = (8 + grayIndex * 10) / 255.0f;
        return new float4(gray, gray, gray, 1.0f);
    }

    /// <summary>
    /// Resolve RGB color to float4 color.
    /// </summary>
    private static float4 ResolveRgbColor(byte red, byte green, byte blue)
    {
        return new float4(red / 255.0f, green / 255.0f, blue / 255.0f, 1.0f);
    }

    /// <summary>
    /// Convert ANSI color code to Color type.
    /// </summary>
    /// <param name="colorCode">ANSI color code (30-37, 40-47, 90-97, 100-107)</param>
    /// <returns>Color or null if invalid</returns>
    public static caTTY.Core.Types.Color? AnsiCodeToColor(int colorCode)
    {
        // Standard foreground colors (30-37)
        if (colorCode >= 30 && colorCode <= 37)
        {
            var namedColors = new[]
            {
                NamedColor.Black, NamedColor.Red, NamedColor.Green, NamedColor.Yellow,
                NamedColor.Blue, NamedColor.Magenta, NamedColor.Cyan, NamedColor.White
            };
            return new caTTY.Core.Types.Color(namedColors[colorCode - 30]);
        }

        // Bright foreground colors (90-97)
        if (colorCode >= 90 && colorCode <= 97)
        {
            var namedColors = new[]
            {
                NamedColor.BrightBlack, NamedColor.BrightRed, NamedColor.BrightGreen, NamedColor.BrightYellow,
                NamedColor.BrightBlue, NamedColor.BrightMagenta, NamedColor.BrightCyan, NamedColor.BrightWhite
            };
            return new caTTY.Core.Types.Color(namedColors[colorCode - 90]);
        }

        // Standard background colors (40-47)
        if (colorCode >= 40 && colorCode <= 47)
        {
            var namedColors = new[]
            {
                NamedColor.Black, NamedColor.Red, NamedColor.Green, NamedColor.Yellow,
                NamedColor.Blue, NamedColor.Magenta, NamedColor.Cyan, NamedColor.White
            };
            return new caTTY.Core.Types.Color(namedColors[colorCode - 40]);
        }

        // Bright background colors (100-107)
        if (colorCode >= 100 && colorCode <= 107)
        {
            var namedColors = new[]
            {
                NamedColor.BrightBlack, NamedColor.BrightRed, NamedColor.BrightGreen, NamedColor.BrightYellow,
                NamedColor.BrightBlue, NamedColor.BrightMagenta, NamedColor.BrightCyan, NamedColor.BrightWhite
            };
            return new caTTY.Core.Types.Color(namedColors[colorCode - 100]);
        }

        return null;
    }

    /// <summary>
    /// Create indexed color type.
    /// </summary>
    /// <param name="index">Color index (0-255)</param>
    /// <returns>Color for indexed color</returns>
    public static caTTY.Core.Types.Color CreateIndexedColor(byte index)
    {
        return new caTTY.Core.Types.Color(index);
    }

    /// <summary>
    /// Create RGB color type.
    /// </summary>
    /// <param name="r">Red component (0-255)</param>
    /// <param name="g">Green component (0-255)</param>
    /// <param name="b">Blue component (0-255)</param>
    /// <returns>Color for RGB color</returns>
    public static caTTY.Core.Types.Color CreateRgbColor(byte r, byte g, byte b)
    {
        return new caTTY.Core.Types.Color(r, g, b);
    }

    /// <summary>
    /// Create named color type.
    /// </summary>
    /// <param name="namedColor">Named color</param>
    /// <returns>Color for named color</returns>
    public static caTTY.Core.Types.Color CreateNamedColor(NamedColor namedColor)
    {
        return new caTTY.Core.Types.Color(namedColor);
    }
}