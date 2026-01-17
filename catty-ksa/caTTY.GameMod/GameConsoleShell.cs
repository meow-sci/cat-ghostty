using System.Text;
using Brutal.ImGuiApi.Abstractions;
using caTTY.Core.Terminal;
using caTTY.Display.Configuration;
using KSA;

namespace caTTY.GameMod;

/// <summary>
///     Custom shell implementation that provides a command-line interface to the KSA game console.
///     This shell executes commands through KSA's TerminalInterface and displays results with appropriate formatting.
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

    // Store the event handler delegate so we can properly unsubscribe
    private Action<string, TerminalInterfaceOutputType>? _outputHandler;

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

        // Verify TerminalInterface is available
        if (Program.TerminalInterface == null)
        {
            throw new InvalidOperationException(
                "KSA TerminalInterface is not available. The game may not be fully initialized yet.");
        }

        // Store terminal dimensions
        _terminalWidth = options.InitialWidth;
        _terminalHeight = options.InitialHeight;

        // Register event handler for game output
        // Store the delegate so we can properly unsubscribe later
        _outputHandler = (text, outputType) => HandleGameOutput(text, outputType);
        Program.TerminalInterface.OnOutput += _outputHandler;

        IsRunning = true;

        // Load prompt from configuration
        LoadPromptFromConfiguration();

        // Send welcome message
        SendOutput("\x1b[1;36m");  // Cyan bold
        SendOutput("=================================================\r\n");
        SendOutput("  KSA Game Console Shell\r\n");
        SendOutput("  Type 'help' for available commands\r\n");
        SendOutput("  Press Ctrl+L to clear screen\r\n");
        SendOutput("=================================================\x1b[0m\r\n");

        // Display initial prompt
        SendPrompt();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        // Unregister event handler using the stored delegate
        if (_outputHandler != null && Program.TerminalInterface != null)
        {
            Program.TerminalInterface.OnOutput -= _outputHandler;
            _outputHandler = null;
        }

        IsRunning = false;

        // Send goodbye message
        SendOutput("\r\n\x1b[1;33mGame Console shell terminated.\x1b[0m\r\n");

        // Raise termination event
        Terminated?.Invoke(this, new ShellTerminatedEventArgs(0, "User requested shutdown"));

        return Task.CompletedTask;
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
                ClearScreen();
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
            // Execute the command via KSA's TerminalInterface
            // The result indicates whether the command was valid (true) or invalid (false)
            bool success = Program.TerminalInterface.Execute(command);

            // Note: The actual output will come asynchronously via the OnOutput event handler
            // We don't need to do anything special here - just wait for the output

            // If the command was invalid and no output event fires, we'll show an error
            // But typically TerminalInterface.Execute will trigger OnOutput events
        }
        catch (Exception ex)
        {
            // Handle any exceptions during command execution
            SendOutput($"\x1b[31mError executing command: {ex.Message}\x1b[0m\r\n");
            SendPrompt();
        }
    }

    /// <summary>
    ///     Handles output from the game's TerminalInterface.
    /// </summary>
    /// <param name="text">The output text</param>
    /// <param name="outputType">The type of output (Message or Error)</param>
    private void HandleGameOutput(string text, TerminalInterfaceOutputType outputType)
    {
        if (!IsRunning)
        {
            return;
        }

        // Format output based on type
        // outputType is TerminalInterfaceOutputType enum with values: Message, Error
        string formattedOutput = outputType switch
        {
            TerminalInterfaceOutputType.Error => $"\x1b[31m{text}\x1b[0m\r\n",  // Red for errors
            TerminalInterfaceOutputType.Message => $"{text}\r\n",               // Default color for messages
            _ => $"{text}\r\n"
        };

        SendOutput(formattedOutput);

        // After output is complete, send new prompt
        SendPrompt();
    }

    /// <summary>
    ///     Sends the command prompt to the terminal.
    /// </summary>
    private void SendPrompt()
    {
        SendOutput(_prompt);
    }

    /// <summary>
    ///     Clears the screen without redisplaying the prompt.
    /// </summary>
    private void ClearScreen()
    {
        // ESC[2J = Clear entire screen
        // ESC[H = Move cursor to home position (1,1)
        SendOutput("\x1b[2J\x1b[H");
    }

    /// <summary>
    ///     Sends text output to the terminal.
    /// </summary>
    /// <param name="text">The text to send</param>
    private void SendOutput(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        SendOutput(bytes);
    }

    /// <summary>
    ///     Sends raw byte data to the terminal.
    /// </summary>
    /// <param name="data">The data to send</param>
    private void SendOutput(byte[] data)
    {
        OutputReceived?.Invoke(this, new ShellOutputEventArgs(data, ShellOutputType.Stdout));
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
            // Use Wait() instead of GetAwaiter().GetResult() to avoid potential deadlocks
            StopAsync().Wait(TimeSpan.FromSeconds(5));
        }

        // Extra safety: ensure handler is unsubscribed even if StopAsync failed/timed out
        if (_outputHandler != null && Program.TerminalInterface != null)
        {
            try
            {
                Program.TerminalInterface.OnOutput -= _outputHandler;
            }
            catch
            {
                // Ignore unsubscribe errors during disposal
            }
            _outputHandler = null;
        }

        _disposed = true;
    }
}
