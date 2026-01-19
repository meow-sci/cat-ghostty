using System.Text;
using caTTY.Core.Terminal;

namespace caTTY.CustomShells.GameStuffShell;

public sealed class GameStuffShell : BaseLineBufferedShell
{
    private readonly CustomShellMetadata _metadata = CustomShellMetadata.Create(
        name: "Game Stuff",
        description: "Game Stuff shell - bash-like command interpreter",
        version: new Version(1, 0, 0),
        author: "caTTY",
        supportedFeatures: new[] { "line-editing", "history", "clear-screen" }
    );

    private string _promptValue = "gstuff> ";

    public override CustomShellMetadata Metadata => _metadata;

    protected override Task OnStartingAsync(CustomShellStartOptions options, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected override Task OnStoppingAsync(CancellationToken cancellationToken)
    {
        QueueOutput("\r\n\x1b[1;33mGame Stuff shell terminated.\x1b[0m\r\n");
        RaiseTerminated(0, "User requested shutdown");
        return Task.CompletedTask;
    }

    public override void SendInitialOutput()
    {
        var banner = new StringBuilder()
            .Append("\x1b[1;36m")
            .Append("=================================================\r\n")
            .Append("  Game Stuff Shell v1.0.0\r\n")
            .Append("  Type commands below to get started\r\n")
            .Append("  Press Ctrl+L to clear screen\r\n")
            .Append("=================================================\x1b[0m\r\n")
            .ToString();

        QueueOutput(banner);
        QueueOutput(_promptValue);
    }

    protected override string GetPrompt()
    {
        return _promptValue;
    }

    protected override void ExecuteCommandLine(string commandLine)
    {
        SendError($"\x1b[31mGame Stuff Shell: command execution not implemented: {commandLine}\x1b[0m\r\n");
        SendPrompt();
    }

    protected override void HandleClearScreen()
    {
        SendOutput("\x1b[2J\x1b[H");
    }

    public override void RequestCancellation()
    {
        SendOutput("\r\n\x1b[33m^C\x1b[0m\r\n");
        SendPrompt();
    }
}
