using System.Text;
using System.Threading.Channels;
using Brutal.ImGuiApi.Abstractions;
using caTTY.Core.Terminal;
using caTTY.ShellContract;
using caTTY.Display.Configuration;
using HarmonyLib;
using KSA;

namespace caTTY.GameMod;

/// <summary>
///     Custom shell implementation that provides a command-line interface to the KSA game console.
///     This shell executes commands through KSA's TerminalInterface and displays results with appropriate formatting.
///
///     Output is handled through a channel-based pattern that mimics real PTY behavior:
///     - Shell writes to an output channel (like writing to stdout)
///     - A background pump reads from the channel and raises OutputReceived events
///     - This decouples shell output from event handling, matching how ConPTY works
/// </summary>
public class GameConsoleShell : ICustomShell
{
    private readonly object _lock = new();
    private readonly StringBuilder _lineBuffer = new();
    private bool _isRunning;
    private bool _disposed;
    private int _terminalWidth = 80;
    private int _terminalHeight = 24;

    // Command history
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private string _savedCurrentLine = string.Empty;

    // Escape sequence parsing
    private enum EscapeState { None, Escape, Csi }
    private EscapeState _escapeState = EscapeState.None;
    private readonly StringBuilder _escapeBuffer = new();

    // Track if we're currently executing a command (for Harmony patch)
    private static GameConsoleShell? _activeInstance;
    private static readonly object _activeLock = new();
    private bool _isExecutingCommand;

    // PTY-style output channel - mimics a pipe/stdout buffer
    // The shell writes to this channel, and a background task pumps it to OutputReceived events
    private Channel<byte[]>? _outputChannel;
    private Task? _outputPumpTask;
    private CancellationTokenSource? _outputPumpCancellation;

    private string _prompt = "ksa> ";
    private const byte CtrlL = 0x0C;
    private const byte CarriageReturn = 0x0D;
    private const byte LineFeed = 0x0A;
    private const byte Backspace = 0x7F;
    private const byte BackspaceAlt = 0x08;

    /// <inheritdoc />
    public CustomShellMetadata Metadata { get; } = CustomShellMetadata.Create(
        name: "Game Console",
        description: "KSA game console interface - execute game commands directly",
        version: new Version(1, 0, 0),
        author: "caTTY",
        supportedFeatures: new[] { "colors", "clear-screen", "command-execution" }
    );

    /// <inheritdoc />
    public bool IsRunning
    {
        get
        {
            lock (_lock)
            {
                return _isRunning;
            }
        }
        private set
        {
            lock (_lock)
            {
                _isRunning = value;
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler<ShellOutputEventArgs>? OutputReceived;

    /// <inheritdoc />
    public event EventHandler<ShellTerminatedEventArgs>? Terminated;

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
        catch (Exception)
        {
            // If loading fails, keep the default prompt
            _prompt = "ksa> ";
        }
    }

    /// <inheritdoc />
    public Task StartAsync(CustomShellStartOptions options, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Shell is already running");
        }

        // NOTE: Harmony patch is already installed by Patcher.patch() at mod startup.
        // No need to install it again here.

        // Verify TerminalInterface is available
        if (Program.TerminalInterface == null)
        {
            throw new InvalidOperationException(
                "KSA TerminalInterface is not available. The game may not be fully initialized yet.");
        }

        // Store terminal dimensions
        _terminalWidth = options.InitialWidth;
        _terminalHeight = options.InitialHeight;

        // NOTE: We no longer use OnOutput event handler because the Harmony patch
        // on ConsoleWindow.Print() captures ALL output (including OnOutput events
        // which the game forwards to ConsoleWindow.Print anyway).
        // Using both would cause duplicate output.

        // Load prompt from configuration
        LoadPromptFromConfiguration();

        // Create PTY-style output channel - unbounded like a real pipe buffer
        // This channel acts as the "stdout pipe" that a real shell would write to
        _outputChannel = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false // Multiple sources can write (shell, command output, etc.)
        });

        // Start the output pump - this is like ProcessManager's ReadOutputAsync task
        // It runs on a background thread and raises OutputReceived events as data arrives
        _outputPumpCancellation = new CancellationTokenSource();
        _outputPumpTask = Task.Run(() => OutputPumpAsync(_outputPumpCancellation.Token));

        IsRunning = true;

        // Shell is now ready to accept input with cursor at 0,0
        // Initial output (banner/prompt) will be sent via SendInitialOutput()
        // AFTER the session is fully initialized and wired up

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void SendInitialOutput()
    {
        // Load prompt from configuration before sending
        LoadPromptFromConfiguration();

        // Send banner and prompt
        // This happens AFTER the shell is fully initialized and wired to the terminal
        var banner = "\x1b[1;36m" +  // Cyan bold
                     "=================================================\r\n" +
                     "  KSA Game Console Shell\r\n" +
                     "  Type 'help' for available commands\r\n" +
                     "  Press Ctrl+L to clear screen\r\n" +
                     "=================================================\x1b[0m\r\n";
        QueueOutput(banner);
        QueueOutput(_prompt);
    }

    /// <summary>
    ///     Background task that pumps output from the channel to OutputReceived events.
    ///     This mimics how ProcessManager's ReadOutputAsync reads from the ConPTY pipe.
    /// </summary>
    private async Task OutputPumpAsync(CancellationToken cancellationToken)
    {
        if (_outputChannel == null) return;

        try
        {
            await foreach (var data in _outputChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    // Raise the event from this background thread, just like ProcessManager does
                    OutputReceived?.Invoke(this, new ShellOutputEventArgs(data, ShellOutputType.Stdout));
                }
                catch (Exception)
                {
                    // Silently handle errors to avoid disrupting the output pump
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when shell is stopped
        }
        catch (Exception)
        {
            // Silently handle errors to avoid crashing the background task
        }
    }

    /// <summary>
    ///     Queues data to the output channel for asynchronous delivery.
    ///     This is like a shell writing to its stdout file descriptor.
    /// </summary>
    private void QueueOutput(byte[] data)
    {
        _outputChannel?.Writer.TryWrite(data);
    }

    /// <summary>
    ///     Queues text to the output channel for asynchronous delivery.
    /// </summary>
    private void QueueOutput(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        QueueOutput(bytes);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;

        // Send goodbye message through the channel
        QueueOutput("\r\n\x1b[1;33mGame Console shell terminated.\x1b[0m\r\n");

        // Complete the output channel - this signals the pump to finish after draining
        _outputChannel?.Writer.Complete();

        // Wait for the output pump to finish processing remaining items
        if (_outputPumpTask != null)
        {
            try
            {
                // Wait for pump to drain, but don't wait forever
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(2));
                await _outputPumpTask.WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Pump didn't finish in time, cancel it
                _outputPumpCancellation?.Cancel();
            }
        }

        // Cleanup
        _outputPumpCancellation?.Dispose();
        _outputPumpCancellation = null;
        _outputPumpTask = null;

        // Raise termination event
        Terminated?.Invoke(this, new ShellTerminatedEventArgs(0, "User requested shutdown"));
    }

    /// <inheritdoc />
    public Task WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            throw new InvalidOperationException("Shell is not running");
        }

        var bytes = data.Span;

        for (int i = 0; i < bytes.Length; i++)
        {
            var b = bytes[i];

            // Escape sequence state machine
            switch (_escapeState)
            {
                case EscapeState.None:
                    if (b == 0x1B) // ESC
                    {
                        _escapeState = EscapeState.Escape;
                        _escapeBuffer.Clear();
                    }
                    else
                    {
                        HandleNormalByte(b);
                    }
                    break;

                case EscapeState.Escape:
                    if (b == '[')
                    {
                        _escapeState = EscapeState.Csi;
                        _escapeBuffer.Clear();
                    }
                    else
                    {
                        // Unknown escape sequence, reset
                        _escapeState = EscapeState.None;
                    }
                    break;

                case EscapeState.Csi:
                    if (b >= 0x40 && b <= 0x7E) // Final byte
                    {
                        HandleCsiSequence((char)b);
                        _escapeState = EscapeState.None;
                    }
                    else
                    {
                        _escapeBuffer.Append((char)b);
                    }
                    break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handles a CSI sequence final byte.
    /// </summary>
    /// <param name="finalByte">The final byte of the CSI sequence</param>
    private void HandleCsiSequence(char finalByte)
    {
        string param = _escapeBuffer.ToString();

        switch (finalByte)
        {
            case 'A': // Up arrow
                NavigateHistoryUp();
                break;
            case 'B': // Down arrow
                NavigateHistoryDown();
                break;
            // Other CSI sequences can be added here in the future
        }
    }

    /// <summary>
    ///     Handles a normal (non-escape) byte.
    /// </summary>
    /// <param name="b">The byte to handle</param>
    private void HandleNormalByte(byte b)
    {
        // Handle special control characters
        if (b == CtrlL)
        {
            // Ctrl+L: Clear screen
            ClearScreen();
            SendPrompt();
        }
        else if (b == CarriageReturn || b == LineFeed)
        {
            // Enter: Execute command
            string commandLine;
            lock (_lock)
            {
                commandLine = _lineBuffer.ToString();
                _lineBuffer.Clear();
            }

            // Echo newline
            SendOutput("\r\n");

            // Execute command if not empty
            if (!string.IsNullOrWhiteSpace(commandLine))
            {
                string trimmedCommand = commandLine.Trim();

                // Add to history (avoid consecutive duplicates)
                if (_commandHistory.Count == 0 || _commandHistory[^1] != trimmedCommand)
                {
                    _commandHistory.Add(trimmedCommand);
                }

                // Reset history navigation
                _historyIndex = -1;
                _savedCurrentLine = string.Empty;

                ExecuteCommand(trimmedCommand);
            }
            else
            {
                // Empty command, just show new prompt
                _historyIndex = -1;
                _savedCurrentLine = string.Empty;
                SendPrompt();
            }
        }
        else if (b == Backspace || b == BackspaceAlt)
        {
            // Backspace: Remove last character
            lock (_lock)
            {
                if (_lineBuffer.Length > 0)
                {
                    _lineBuffer.Length--;
                    // Send backspace sequence: move cursor left, erase to end of line
                    SendOutput("\x1b[D\x1b[K");
                }
            }
        }
        else if (b >= 0x20 && b < 0x7F)
        {
            // Printable ASCII character
            lock (_lock)
            {
                _lineBuffer.Append((char)b);
            }
            // Echo the character back
            SendOutput(new byte[] { b });
        }
        // Note: We ignore other control characters and non-ASCII bytes for now
        // A more sophisticated implementation would handle UTF-8 multi-byte sequences
    }

    /// <summary>
    ///     Navigates to the previous command in history.
    /// </summary>
    private void NavigateHistoryUp()
    {
        if (_commandHistory.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            // Save current line if starting navigation
            if (_historyIndex == -1)
            {
                _savedCurrentLine = _lineBuffer.ToString();
                _historyIndex = _commandHistory.Count;
            }

            // Move up in history
            if (_historyIndex > 0)
            {
                _historyIndex--;
                ReplaceLineBuffer(_commandHistory[_historyIndex]);
            }
        }
    }

    /// <summary>
    ///     Navigates to the next command in history.
    /// </summary>
    private void NavigateHistoryDown()
    {
        if (_historyIndex == -1)
        {
            return; // Not navigating
        }

        lock (_lock)
        {
            _historyIndex++;

            if (_historyIndex >= _commandHistory.Count)
            {
                // Past end of history, restore saved line
                _historyIndex = -1;
                ReplaceLineBuffer(_savedCurrentLine);
                _savedCurrentLine = string.Empty;
            }
            else
            {
                ReplaceLineBuffer(_commandHistory[_historyIndex]);
            }
        }
    }

    /// <summary>
    ///     Replaces the current line buffer with new text and redraws the line.
    /// </summary>
    /// <param name="newText">The new text for the line buffer</param>
    private void ReplaceLineBuffer(string newText)
    {
        // Clear current line: move to start, erase to end
        SendOutput($"\r{_prompt}\x1b[K");

        // Update buffer
        _lineBuffer.Clear();
        _lineBuffer.Append(newText);

        // Display new text
        SendOutput(newText);
    }

    /// <inheritdoc />
    public void NotifyTerminalResize(int width, int height)
    {
        lock (_lock)
        {
            _terminalWidth = width;
            _terminalHeight = height;
        }
    }

    /// <inheritdoc />
    public void RequestCancellation()
    {
        // For now, we don't support command cancellation
        // In the future, this could interrupt long-running game commands
        SendOutput("\r\n\x1b[33m^C\x1b[0m\r\n");
        SendPrompt();
    }

    /// <summary>
    ///     Tries to handle a built-in shell command.
    /// </summary>
    /// <param name="command">The command to check</param>
    /// <returns>True if the command was handled as a built-in command, false otherwise</returns>
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
    ///     Executes a command via the game's TerminalInterface.
    /// </summary>
    /// <param name="command">The command to execute</param>
    private void ExecuteCommand(string command)
    {
        // Check if it's a built-in command first
        if (TryHandleBuiltinCommand(command))
        {
            SendPrompt();
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

                // Send prompt
                SendPrompt();
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
            SendOutput($"\x1b[31mError executing command: {ex.Message}\x1b[0m\r\n");
            SendPrompt();
        }
    }

    /// <summary>
    ///     Sends the command prompt to the terminal.
    /// </summary>
    private void SendPrompt()
    {
        SendOutput(_prompt);
    }

    /// <summary>
    ///     Clears the current screen without clearing the scrollback buffer.
    ///     Used by Ctrl+L for screen clearing while preserving history.
    /// </summary>
    private void ClearScreen()
    {
        // ESC[2J = Clear entire screen (but preserves scrollback)
        // ESC[H = Move cursor to home position (0,0)
        SendOutput("\x1b[2J\x1b[H");
    }

    /// <summary>
    ///     Clears the entire display including the scrollback buffer and moves cursor to home.
    ///     Used by the "clear" command to completely wipe all terminal history.
    /// </summary>
    private void ClearScreenAndScrollback()
    {
        // ESC[3J = Clear entire screen and scrollback buffer (xterm extension, mode 3)
        SendOutput("\x1b[3J");
    }

    /// <summary>
    ///     Sends text output to the terminal via the PTY-style output channel.
    /// </summary>
    /// <param name="text">The text to send</param>
    private void SendOutput(string text)
    {
        QueueOutput(text);
    }

    /// <summary>
    ///     Sends raw byte data to the terminal via the PTY-style output channel.
    /// </summary>
    /// <param name="data">The data to send</param>
    private void SendOutput(byte[] data)
    {
        QueueOutput(data);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (IsRunning)
        {
            // Complete the channel to stop the pump
            _outputChannel?.Writer.TryComplete();

            // Cancel the pump immediately
            _outputPumpCancellation?.Cancel();

            // Wait briefly for cleanup
            try
            {
                _outputPumpTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Ignore errors during disposal
            }

            _outputPumpCancellation?.Dispose();
            _outputPumpCancellation = null;
            _outputPumpTask = null;

            IsRunning = false;
        }

        _disposed = true;
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

                _activeInstance.SendOutput(formattedOutput);
            }
            catch (Exception)
            {
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
