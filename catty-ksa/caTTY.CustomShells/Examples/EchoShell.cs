using caTTY.ShellContract;

namespace caTTY.CustomShells.Examples;

/// <summary>
///     Simple echo shell that immediately echoes back any byte received.
///     Demonstrates minimal BaseCustomShell usage with raw byte-level processing.
/// </summary>
public class EchoShell : BaseCustomShell
{
    /// <inheritdoc />
    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Echo Shell",
        "Echoes input bytes back - demonstrates BaseCustomShell usage");

    /// <inheritdoc />
    protected override void OnInputByte(byte b)
    {
        // Echo the byte back immediately
        QueueOutput(new[] { b });
    }

    /// <inheritdoc />
    protected override string? GetInitialOutput()
    {
        return "Echo Shell - Type anything and it will be echoed back\r\n";
    }
}
