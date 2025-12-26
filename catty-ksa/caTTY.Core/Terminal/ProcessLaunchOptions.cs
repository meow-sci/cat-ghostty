namespace caTTY.Core.Terminal;

/// <summary>
///     Shell type enumeration for process launching.
/// </summary>
public enum ShellType
{
    /// <summary>
    ///     Automatically detect the best shell for the current platform.
    /// </summary>
    Auto,

    /// <summary>
    ///     Windows PowerShell (powershell.exe).
    /// </summary>
    PowerShell,

    /// <summary>
    ///     PowerShell Core (pwsh.exe).
    /// </summary>
    PowerShellCore,

    /// <summary>
    ///     Windows Command Prompt (cmd.exe).
    /// </summary>
    Cmd,

    /// <summary>
    ///     Custom shell with explicit path.
    /// </summary>
    Custom
}

/// <summary>
///     Options for launching a shell process.
/// </summary>
public class ProcessLaunchOptions
{
    /// <summary>
    ///     Gets or sets the shell type to launch.
    /// </summary>
    public ShellType ShellType { get; set; } = ShellType.Auto;

    /// <summary>
    ///     Gets or sets the custom shell path (used when ShellType is Custom).
    /// </summary>
    public string? CustomShellPath { get; set; }

    /// <summary>
    ///     Gets or sets additional arguments to pass to the shell.
    /// </summary>
    public List<string> Arguments { get; set; } = new();

    /// <summary>
    ///     Gets or sets the working directory for the shell process.
    ///     If null, uses the current process working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    ///     Gets or sets additional environment variables for the shell process.
    ///     These will be added to or override the current process environment.
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();

    /// <summary>
    ///     Gets or sets the initial terminal width in columns.
    /// </summary>
    public int InitialWidth { get; set; } = 80;

    /// <summary>
    ///     Gets or sets the initial terminal height in rows.
    /// </summary>
    public int InitialHeight { get; set; } = 25;

    /// <summary>
    ///     Gets or sets whether to create a window for the process.
    ///     Should be false for game mod integration.
    /// </summary>
    public bool CreateWindow { get; set; } = false;

    /// <summary>
    ///     Gets or sets whether to use shell execute.
    ///     Should be false for direct process control.
    /// </summary>
    public bool UseShellExecute { get; set; } = false;

    /// <summary>
    ///     Creates default launch options for the current platform.
    /// </summary>
    /// <returns>Default launch options</returns>
    public static ProcessLaunchOptions CreateDefault()
    {
        var options = new ProcessLaunchOptions();

        // Set platform-specific defaults
        if (OperatingSystem.IsWindows())
        {
            options.ShellType = ShellType.PowerShell;
            options.Arguments.AddRange(["-NoLogo", "-NoProfile"]);
            options.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            options.ShellType = ShellType.Auto; // Will detect bash/zsh/sh
            options.WorkingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        // Add common environment variables
        options.EnvironmentVariables["TERM"] = "xterm-256color";
        options.EnvironmentVariables["TERM_PROGRAM"] = "caTTY";
        options.EnvironmentVariables["COLORTERM"] = "truecolor";
        options.EnvironmentVariables["FORCE_COLOR"] = "1";
        options.EnvironmentVariables["CLICOLOR_FORCE"] = "1";

        return options;
    }

    /// <summary>
    ///     Creates launch options for PowerShell on Windows.
    /// </summary>
    /// <returns>PowerShell launch options</returns>
    public static ProcessLaunchOptions CreatePowerShell()
    {
        ProcessLaunchOptions options = CreateDefault();
        options.ShellType = ShellType.PowerShell;
        options.Arguments.Clear();
        options.Arguments.AddRange(["-NoLogo", "-NoProfile"]);
        return options;
    }

    /// <summary>
    ///     Creates launch options for PowerShell Core (pwsh).
    /// </summary>
    /// <returns>PowerShell Core launch options</returns>
    public static ProcessLaunchOptions CreatePowerShellCore()
    {
        ProcessLaunchOptions options = CreateDefault();
        options.ShellType = ShellType.PowerShellCore;
        options.Arguments.Clear();
        options.Arguments.AddRange(["-NoLogo", "-NoProfile"]);
        return options;
    }

    /// <summary>
    ///     Creates launch options for Windows Command Prompt.
    /// </summary>
    /// <returns>Command Prompt launch options</returns>
    public static ProcessLaunchOptions CreateCmd()
    {
        ProcessLaunchOptions options = CreateDefault();
        options.ShellType = ShellType.Cmd;
        options.Arguments.Clear();
        return options;
    }

    /// <summary>
    ///     Creates launch options for a custom shell.
    /// </summary>
    /// <param name="shellPath">Path to the shell executable</param>
    /// <param name="arguments">Arguments to pass to the shell</param>
    /// <returns>Custom shell launch options</returns>
    public static ProcessLaunchOptions CreateCustom(string shellPath, params string[] arguments)
    {
        ProcessLaunchOptions options = CreateDefault();
        options.ShellType = ShellType.Custom;
        options.CustomShellPath = shellPath;
        options.Arguments.Clear();
        options.Arguments.AddRange(arguments);
        return options;
    }
}
