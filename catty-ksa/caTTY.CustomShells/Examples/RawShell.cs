using caTTY.ShellContract;

namespace caTTY.CustomShells.Examples;

/// <summary>
///     Raw mode shell with no echo, no history, and no escape sequence parsing.
///     Commands are still line-buffered and executed on Enter, but input is not echoed.
///     Demonstrates LineDisciplineShell raw mode usage.
/// </summary>
public class RawShell : LineDisciplineShell
{
    public RawShell() : base(LineDisciplineOptions.CreateRawMode())
    {
    }

    /// <inheritdoc />
    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Raw Shell",
        "Raw mode shell with no echo or history - demonstrates raw mode LineDisciplineShell usage");

    /// <inheritdoc />
    protected override void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            QueueOutput("\r\n");
            return;
        }

        // In raw mode, echo the command since it wasn't echoed during input
        QueueOutput($"You typed: {command}\r\n");

        // Handle some basic commands
        if (command.Trim().ToLowerInvariant() == "help")
        {
            QueueOutput("Available commands:\r\n");
            QueueOutput("  help  - Show this help\r\n");
            QueueOutput("  clear - Clear the screen\r\n");
            QueueOutput("  Any other input will be echoed back\r\n");
        }
        else if (command.Trim().ToLowerInvariant() == "clear")
        {
            ClearScreen();
        }

        QueueOutput("\r\n");
    }

    /// <inheritdoc />
    protected override string GetPrompt()
    {
        return "> ";
    }

    /// <inheritdoc />
    protected override string? GetBanner()
    {
        return "Raw Shell - No echo, no history mode\r\n" +
               "Your input will not be displayed as you type\r\n" +
               "Type 'help' for available commands\r\n\r\n";
    }
}
