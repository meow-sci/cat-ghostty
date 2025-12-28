using Brutal.Numerics;
using caTTY.Core.Types;

namespace caTTY.Display.Rendering;

/// <summary>
/// Terminal color palette containing all standard ANSI colors and UI colors.
/// Based on TypeScript TerminalTheme implementation.
/// </summary>
public readonly struct TerminalColorPalette
{
    // Standard 16 ANSI colors
    public float4 Black { get; }
    public float4 Red { get; }
    public float4 Green { get; }
    public float4 Yellow { get; }
    public float4 Blue { get; }
    public float4 Magenta { get; }
    public float4 Cyan { get; }
    public float4 White { get; }
    public float4 BrightBlack { get; }
    public float4 BrightRed { get; }
    public float4 BrightGreen { get; }
    public float4 BrightYellow { get; }
    public float4 BrightBlue { get; }
    public float4 BrightMagenta { get; }
    public float4 BrightCyan { get; }
    public float4 BrightWhite { get; }
    
    // Terminal UI colors
    public float4 Foreground { get; }
    public float4 Background { get; }
    public float4 Cursor { get; }
    public float4 Selection { get; }

    public TerminalColorPalette(
        float4 black, float4 red, float4 green, float4 yellow,
        float4 blue, float4 magenta, float4 cyan, float4 white,
        float4 brightBlack, float4 brightRed, float4 brightGreen, float4 brightYellow,
        float4 brightBlue, float4 brightMagenta, float4 brightCyan, float4 brightWhite,
        float4 foreground, float4 background, float4 cursor, float4 selection)
    {
        Black = black;
        Red = red;
        Green = green;
        Yellow = yellow;
        Blue = blue;
        Magenta = magenta;
        Cyan = cyan;
        White = white;
        BrightBlack = brightBlack;
        BrightRed = brightRed;
        BrightGreen = brightGreen;
        BrightYellow = brightYellow;
        BrightBlue = brightBlue;
        BrightMagenta = brightMagenta;
        BrightCyan = brightCyan;
        BrightWhite = brightWhite;
        Foreground = foreground;
        Background = background;
        Cursor = cursor;
        Selection = selection;
    }
}

/// <summary>
/// Cursor configuration for terminal themes.
/// </summary>
public readonly struct CursorConfig
{
    /// <summary>
    /// Default cursor style for the theme.
    /// </summary>
    public CursorStyle DefaultStyle { get; }

    /// <summary>
    /// Whether cursor should blink by default.
    /// </summary>
    public bool DefaultBlink { get; }

    /// <summary>
    /// Cursor blink interval in milliseconds.
    /// </summary>
    public int BlinkIntervalMs { get; }

    public CursorConfig(CursorStyle defaultStyle, bool defaultBlink, int blinkIntervalMs = 500)
    {
        DefaultStyle = defaultStyle;
        DefaultBlink = defaultBlink;
        BlinkIntervalMs = blinkIntervalMs;
    }
}

/// <summary>
/// Complete terminal theme definition.
/// </summary>
public readonly struct TerminalTheme
{
    public string Name { get; }
    public ThemeType Type { get; }
    public TerminalColorPalette Colors { get; }
    public CursorConfig Cursor { get; }

    public TerminalTheme(string name, ThemeType type, TerminalColorPalette colors, CursorConfig cursor)
    {
        Name = name;
        Type = type;
        Colors = colors;
        Cursor = cursor;
    }
}

/// <summary>
/// Theme type enumeration.
/// </summary>
public enum ThemeType
{
    Dark,
    Light
}

/// <summary>
/// Theme manager for handling terminal color themes.
/// Provides default themes and color resolution.
/// </summary>
public static class ThemeManager
{
    /// <summary>
    /// Default dark theme using standard terminal colors.
    /// Matches the TypeScript DEFAULT_DARK_THEME.
    /// </summary>
    public static readonly TerminalTheme DefaultDarkTheme = new(
        "Default Dark",
        ThemeType.Dark,
        new TerminalColorPalette(
            // Standard ANSI colors
            black: new float4(0.0f, 0.0f, 0.0f, 1.0f),
            red: new float4(0.667f, 0.0f, 0.0f, 1.0f),
            green: new float4(0.0f, 0.667f, 0.0f, 1.0f),
            yellow: new float4(0.667f, 0.333f, 0.0f, 1.0f),
            blue: new float4(0.0f, 0.0f, 0.667f, 1.0f),
            magenta: new float4(0.667f, 0.0f, 0.667f, 1.0f),
            cyan: new float4(0.0f, 0.667f, 0.667f, 1.0f),
            white: new float4(0.667f, 0.667f, 0.667f, 1.0f),
            
            // Bright ANSI colors
            brightBlack: new float4(0.333f, 0.333f, 0.333f, 1.0f),
            brightRed: new float4(1.0f, 0.333f, 0.333f, 1.0f),
            brightGreen: new float4(0.333f, 1.0f, 0.333f, 1.0f),
            brightYellow: new float4(1.0f, 1.0f, 0.333f, 1.0f),
            brightBlue: new float4(0.333f, 0.333f, 1.0f, 1.0f),
            brightMagenta: new float4(1.0f, 0.333f, 1.0f, 1.0f),
            brightCyan: new float4(0.333f, 1.0f, 1.0f, 1.0f),
            brightWhite: new float4(1.0f, 1.0f, 1.0f, 1.0f),
            
            // Terminal UI colors
            foreground: new float4(0.667f, 0.667f, 0.667f, 1.0f),
            background: new float4(0.0f, 0.0f, 0.0f, 1.0f),
            cursor: new float4(0.667f, 0.667f, 0.667f, 1.0f),
            selection: new float4(0.267f, 0.267f, 0.267f, 1.0f)
        ),
        new CursorConfig(CursorStyle.BlinkingBlock, true, 500)
    );

    /// <summary>
    /// Default light theme for terminals.
    /// </summary>
    public static readonly TerminalTheme DefaultLightTheme = new(
        "Default Light",
        ThemeType.Light,
        new TerminalColorPalette(
            // Standard ANSI colors (darker for light background)
            black: new float4(0.0f, 0.0f, 0.0f, 1.0f),
            red: new float4(0.8f, 0.0f, 0.0f, 1.0f),
            green: new float4(0.0f, 0.6f, 0.0f, 1.0f),
            yellow: new float4(0.8f, 0.6f, 0.0f, 1.0f),
            blue: new float4(0.0f, 0.0f, 0.8f, 1.0f),
            magenta: new float4(0.8f, 0.0f, 0.8f, 1.0f),
            cyan: new float4(0.0f, 0.6f, 0.6f, 1.0f),
            white: new float4(0.8f, 0.8f, 0.8f, 1.0f),
            
            // Bright ANSI colors
            brightBlack: new float4(0.4f, 0.4f, 0.4f, 1.0f),
            brightRed: new float4(1.0f, 0.2f, 0.2f, 1.0f),
            brightGreen: new float4(0.2f, 0.8f, 0.2f, 1.0f),
            brightYellow: new float4(1.0f, 0.8f, 0.2f, 1.0f),
            brightBlue: new float4(0.2f, 0.2f, 1.0f, 1.0f),
            brightMagenta: new float4(1.0f, 0.2f, 1.0f, 1.0f),
            brightCyan: new float4(0.2f, 0.8f, 0.8f, 1.0f),
            brightWhite: new float4(1.0f, 1.0f, 1.0f, 1.0f),
            
            // Terminal UI colors (inverted for light theme)
            foreground: new float4(0.0f, 0.0f, 0.0f, 1.0f),
            background: new float4(1.0f, 1.0f, 1.0f, 1.0f),
            cursor: new float4(0.0f, 0.0f, 0.0f, 1.0f),
            selection: new float4(0.8f, 0.8f, 0.8f, 1.0f)
        ),
        new CursorConfig(CursorStyle.BlinkingBlock, true, 500)
    );

    /// <summary>
    /// Current active theme. Defaults to dark theme.
    /// </summary>
    public static TerminalTheme CurrentTheme { get; private set; } = DefaultDarkTheme;

    /// <summary>
    /// Apply a new theme.
    /// </summary>
    /// <param name="theme">The theme to apply</param>
    public static void ApplyTheme(TerminalTheme theme)
    {
        CurrentTheme = theme;
    }

    /// <summary>
    /// Resolve a color by ANSI color code using the current theme.
    /// </summary>
    /// <param name="colorCode">ANSI color code (0-15 for standard colors)</param>
    /// <returns>float4 color value</returns>
    public static float4 ResolveThemeColor(int colorCode)
    {
        var palette = CurrentTheme.Colors;
        
        return colorCode switch
        {
            0 => palette.Black,
            1 => palette.Red,
            2 => palette.Green,
            3 => palette.Yellow,
            4 => palette.Blue,
            5 => palette.Magenta,
            6 => palette.Cyan,
            7 => palette.White,
            8 => palette.BrightBlack,
            9 => palette.BrightRed,
            10 => palette.BrightGreen,
            11 => palette.BrightYellow,
            12 => palette.BrightBlue,
            13 => palette.BrightMagenta,
            14 => palette.BrightCyan,
            15 => palette.BrightWhite,
            _ => palette.Foreground
        };
    }

    /// <summary>
    /// Get the default foreground color from the current theme.
    /// </summary>
    public static float4 GetDefaultForeground() => CurrentTheme.Colors.Foreground;

    /// <summary>
    /// Get the default background color from the current theme.
    /// </summary>
    public static float4 GetDefaultBackground() => CurrentTheme.Colors.Background;

    /// <summary>
    /// Get the cursor color from the current theme.
    /// </summary>
    public static float4 GetCursorColor() => CurrentTheme.Colors.Cursor;

    /// <summary>
    /// Get the selection color from the current theme.
    /// </summary>
    public static float4 GetSelectionColor() => CurrentTheme.Colors.Selection;

    /// <summary>
    /// Get the default cursor style from the current theme.
    /// </summary>
    public static CursorStyle GetDefaultCursorStyle() => CurrentTheme.Cursor.DefaultStyle;

    /// <summary>
    /// Get the default cursor blink setting from the current theme.
    /// </summary>
    public static bool GetDefaultCursorBlink() => CurrentTheme.Cursor.DefaultBlink;

    /// <summary>
    /// Get the cursor blink interval from the current theme.
    /// </summary>
    public static int GetCursorBlinkInterval() => CurrentTheme.Cursor.BlinkIntervalMs;
}