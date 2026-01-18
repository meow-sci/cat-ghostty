using System.Text;
using Brutal.ImGuiApi.Abstractions;
using caTTY.ShellContract;
using caTTY.Display.Configuration;
using HarmonyLib;
using KSA;

namespace caTTY.CustomShells;

/// <summary>
///     Custom shell implementation that provides a command-line interface to the KSA game console.
///     This shell executes commands through KSA's TerminalInterface and displays results with appropriate formatting.
///
///     Extends LineDisciplineShell to inherit input buffering, command history, and line editing features.
/// </summary>
public class GameConsoleShell : LineDisciplineShell
{
    // Track if we're currently executing a command (for Harmony patch)
    private static GameConsoleShell? _activeInstance;
    private static readonly object _activeLock = new();
    private bool _isExecutingCommand;

    private string _prompt = "ksa> ";

    /// <summary>
    ///     Creates a new GameConsoleShell with default line discipline options.
    /// </summary>
    public GameConsoleShell() : base(LineDisciplineOptions.CreateDefault())
    {
    }

    /// <inheritdoc />
    public override CustomShellMetadata Metadata { get; } = CustomShellMetadata.Create(
        name: "Game Console",
        description: "KSA game console interface - execute game commands directly",
        version: new Version(1, 0, 0),
        author: "caTTY",
        supportedFeatures: new[] { "colors", "clear-screen", "command-execution", "line-editing", "history" }
    );

    /// <summary>
    ///     Loads the prompt string from the saved configuration.
    /// </summary>
    private void LoadPromptFromConfiguration()
    {
        try
        {
            var config = ThemeConfiguration.Load();
            _prompt = config.GameShellPrompt;
        }
        catch
        {
            // If loading fails, keep default prompt
            _prompt = "ksa> ";
        }
    }

    /// <inheritdoc />
    protected override async Task OnShellStartingAsync(CustomShellStartOptions options, CancellationToken cancellationToken)
    {
        await base.OnShellStartingAsync(options, cancellationToken);

        // Validate that TerminalInterface is available
        if (Program.TerminalInterface == null)
        {
            throw new InvalidOperationException("KSA TerminalInterface is not available. The game may not be fully initialized.");
        }

        // Load prompt from configuration
        LoadPromptFromConfiguration();
    }

    /// <inheritdoc />
    protected override string GetPrompt()
    {
        return _prompt;
    }

    /// <inheritdoc />
    protected override string? GetBanner()
    {
        return "\x1b[36m╔══════════════════════════════════════════════════════════════╗\r\n" +
               "║  KSA Game Console - caTTY Terminal Interface v1.0            ║\r\n" +
               "║  Type 'help' for available commands                         ║\r\n" +
               "╚══════════════════════════════════════════════════════════════╝\x1b[0m\r\n";
    }

    /// <inheritdoc />
    protected override void ExecuteCommand(string command)
    {
        // Check if it's a built-in command first
        if (TryHandleBuiltinCommand(command))
        {
            return;
        }

        try
        {
            // Set this instance as active so Harmony patch can capture output
            lock (_activeLock)
            {
                _activeInstance = this;
                _isExecutingCommand = true;
            }

            try
            {
                // Execute the command via KSA's TerminalInterface
                // Output will be captured by our Harmony patch on ConsoleWindow.Print()
                bool success = Program.TerminalInterface.Execute(command);
            }
            finally
            {
                // Clear active instance
                lock (_activeLock)
                {
                    _isExecutingCommand = false;
                    _activeInstance = null;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle any exceptions during command execution
            QueueOutput($"\x1b[31mError executing command: {ex.Message}\x1b[0m\r\n");
        }
    }

    /// <summary>
    ///     Tries to handle built-in commands that are implemented in the shell itself.
    /// </summary>
    /// <param name="command">The command to check</param>
    /// <returns>True if the command was handled, false otherwise</returns>
    private bool TryHandleBuiltinCommand(string command)
    {
        switch (command.Trim().ToLowerInvariant())
        {
            case "clear":
                ClearScreenAndScrollback();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///     Internal method called by Harmony patch to handle captured console output.
    /// </summary>
    internal static void OnConsolePrint(string output, uint color, ConsoleLineType lineType)
    {
        lock (_activeLock)
        {
            if (_activeInstance == null || !_activeInstance._isExecutingCommand)
            {
                Console.WriteLine($"[GameConsoleShell] Output dropped - shell not active or not executing");
                return; // Not currently executing in our shell
            }

            try
            {
                // Forward to the active shell instance
                // Determine if this is an error based on color (red = error)
                bool isError = color == ConsoleWindow.ErrorColor || color == ConsoleWindow.CriticalColor;

                string formattedOutput = isError
                    ? $"\x1b[31m{output}\x1b[0m\r\n"  // Red for errors
                    : $"{output}\r\n";               // Default color for normal output

                // Use QueueOutputUnchecked because game console output arrives asynchronously
                // via Harmony patch and may arrive during shell state transitions.
                // The normal QueueOutput guards would drop this external output.
                _activeInstance.QueueOutputUnchecked(formattedOutput);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameConsoleShell] Exception in OnConsolePrint: {ex.Message}");
                // Silently handle errors to avoid disrupting the game console
            }
        }
    }
}

/// <summary>
///     Harmony patch for ConsoleWindow.Print to capture console output.
///     This patch is automatically installed by Patcher.patch() at mod startup.
/// </summary>
[HarmonyPatch(typeof(ConsoleWindow))]
[HarmonyPatch(nameof(ConsoleWindow.Print))]
[HarmonyPatch(new[] { typeof(string), typeof(uint), typeof(int), typeof(ConsoleLineType) })]
public static class ConsoleWindowPrintPatch
{
    /// <summary>
    ///     Postfix patch that captures console output.
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix(string inOutput, uint inColor, ConsoleLineType inType)
    {
        try
        {
            GameConsoleShell.OnConsolePrint(inOutput, inColor, inType);
        }
        catch (Exception)
        {
            // Silently handle errors to avoid disrupting the game console
        }
    }
}
