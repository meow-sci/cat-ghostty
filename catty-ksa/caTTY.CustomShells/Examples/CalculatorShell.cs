using System.Data;
using caTTY.ShellContract;

namespace caTTY.CustomShells.Examples;

/// <summary>
///     Calculator shell that evaluates mathematical expressions.
///     Demonstrates LineDisciplineShell usage with command-line editing and history.
/// </summary>
public class CalculatorShell : LineDisciplineShell
{
    public CalculatorShell() : base(LineDisciplineOptions.CreateDefault())
    {
    }

    /// <inheritdoc />
    public override CustomShellMetadata Metadata => CustomShellMetadata.Create(
        "Calculator Shell",
        "Mathematical expression evaluator - demonstrates LineDisciplineShell usage");

    /// <inheritdoc />
    protected override void ExecuteCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        try
        {
            // Use DataTable.Compute for simple expression evaluation
            var table = new DataTable();
            var result = table.Compute(command, null);
            QueueOutput($"= {result}\r\n");
        }
        catch (Exception ex)
        {
            QueueOutput($"\x1b[31mError: {ex.Message}\x1b[0m\r\n");
        }
    }

    /// <inheritdoc />
    protected override string GetPrompt()
    {
        return "calc> ";
    }

    /// <inheritdoc />
    protected override string? GetBanner()
    {
        return "Calculator Shell - Enter mathematical expressions (e.g., 2 + 2, 5 * 10)\r\n" +
               "Features: line editing, command history, backspace\r\n" +
               "Type 'exit' or press Ctrl+C to quit\r\n";
    }
}
