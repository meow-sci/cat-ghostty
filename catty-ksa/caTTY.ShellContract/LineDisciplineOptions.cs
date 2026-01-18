namespace caTTY.ShellContract;

/// <summary>
///     Configuration options for line discipline behavior in custom shells.
///     Controls input processing features like echo, history, and escape sequence parsing.
/// </summary>
public class LineDisciplineOptions
{
    /// <summary>
    ///     Gets or sets the maximum number of commands to retain in history.
    ///     Default: 100
    /// </summary>
    public int MaxHistorySize { get; set; } = 100;

    /// <summary>
    ///     Gets or sets whether input characters should be echoed back to the terminal.
    ///     When false, input is processed silently (raw mode).
    ///     Default: true
    /// </summary>
    public bool EchoInput { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether command history is enabled.
    ///     When false, up/down arrow keys will not navigate history.
    ///     Default: true
    /// </summary>
    public bool EnableHistory { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether escape sequences (arrows, Ctrl+L, etc.) should be parsed.
    ///     When false, escape sequences are passed through as raw bytes.
    ///     Default: true
    /// </summary>
    public bool ParseEscapeSequences { get; set; } = true;

    /// <summary>
    ///     Creates default line discipline options with all features enabled.
    /// </summary>
    /// <returns>Options with echo, history, and escape parsing enabled</returns>
    public static LineDisciplineOptions CreateDefault()
    {
        return new LineDisciplineOptions
        {
            MaxHistorySize = 100,
            EchoInput = true,
            EnableHistory = true,
            ParseEscapeSequences = true
        };
    }

    /// <summary>
    ///     Creates raw mode options with all features disabled.
    ///     Raw mode provides direct byte-level input without echo, history, or parsing.
    /// </summary>
    /// <returns>Options with all features disabled</returns>
    public static LineDisciplineOptions CreateRawMode()
    {
        return new LineDisciplineOptions
        {
            MaxHistorySize = 0,
            EchoInput = false,
            EnableHistory = false,
            ParseEscapeSequences = false
        };
    }
}
