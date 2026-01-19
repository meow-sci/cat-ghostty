using System.Text;

namespace caTTY.Core.Terminal;

/// <summary>
///     Base class for custom shells that implement line buffering with command history
///     and escape sequence handling. Extends BaseChannelOutputShell with input processing
///     features like backspace, enter, arrow key navigation, and Ctrl+L clear screen.
/// </summary>
public abstract class BaseLineBufferedShell : BaseChannelOutputShell
{
    /// <summary>
    ///     Current line being edited by the user.
    /// </summary>
    private readonly StringBuilder _lineBuffer = new();

    /// <summary>
    ///     Command history storage.
    /// </summary>
    private readonly List<string> _commandHistory = new();

    /// <summary>
    ///     Current position in command history (-1 means not navigating).
    /// </summary>
    private int _historyIndex = -1;

    /// <summary>
    ///     Saved current line when navigating history.
    /// </summary>
    private string _savedCurrentLine = string.Empty;

    /// <summary>
    ///     Current cursor position in the line buffer (0-indexed).
    /// </summary>
    private int _cursorPosition = 0;

    /// <summary>
    ///     Escape sequence state machine states.
    /// </summary>
    private enum EscapeState
    {
        /// <summary>Normal character processing</summary>
        None,
        /// <summary>ESC received, waiting for next byte</summary>
        Escape,
        /// <summary>CSI sequence in progress (ESC [ received)</summary>
        Csi
    }

    /// <summary>
    ///     Current escape sequence processing state.
    /// </summary>
    private EscapeState _escapeState = EscapeState.None;

    /// <summary>
    ///     Buffer for collecting CSI sequence parameters.
    /// </summary>
    private readonly StringBuilder _escapeBuffer = new();

    /// <summary>
    ///     Terminal width in columns.
    /// </summary>
    private int _terminalWidth = 80;

    /// <summary>
    ///     Terminal height in rows.
    /// </summary>
    private int _terminalHeight = 24;

    // Control character constants
    /// <summary>Ctrl+L (clear screen)</summary>
    protected const byte CtrlL = 0x0C;
    /// <summary>Carriage return (Enter key)</summary>
    protected const byte CarriageReturn = 0x0D;
    /// <summary>Line feed</summary>
    protected const byte LineFeed = 0x0A;
    /// <summary>Backspace (DEL)</summary>
    protected const byte Backspace = 0x7F;
    /// <summary>Backspace alternative (BS)</summary>
    protected const byte BackspaceAlt = 0x08;

    /// <summary>
    ///     Gets the command history for testing/inspection purposes.
    /// </summary>
    protected IReadOnlyList<string> CommandHistory => _commandHistory;

    /// <summary>
    ///     Gets the current line buffer content for testing/inspection purposes.
    /// </summary>
    protected string CurrentLine
    {
        get
        {
            lock (_lock)
            {
                return _lineBuffer.ToString();
            }
        }
    }

    /// <summary>
    ///     Gets the current cursor position for testing/inspection purposes.
    /// </summary>
    protected int CursorPosition => _cursorPosition;

    /// <summary>
    ///     Abstract method called when user presses Enter to execute a command.
    /// </summary>
    /// <param name="commandLine">The command line to execute (trimmed)</param>
    protected abstract void ExecuteCommandLine(string commandLine);

    /// <summary>
    ///     Abstract method called when user presses Ctrl+L to clear the screen.
    ///     Default behavior should send ESC[2J ESC[H.
    /// </summary>
    protected abstract void HandleClearScreen();

    /// <summary>
    ///     Abstract method to get the current prompt string.
    /// </summary>
    /// <returns>The prompt string to display</returns>
    protected abstract string GetPrompt();

    /// <inheritdoc />
    public override Task WriteInputAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
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
            case 'C': // Right arrow
                MoveCursorRight();
                break;
            case 'D': // Left arrow
                MoveCursorLeft();
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
            HandleClearScreen();
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
                _cursorPosition = 0;
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

                ExecuteCommandLine(trimmedCommand);
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
                    _cursorPosition--;
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
                if (_cursorPosition == _lineBuffer.Length)
                {
                    // At end - append (current behavior)
                    _lineBuffer.Append((char)b);
                    SendOutput(new byte[] { b });
                }
                else
                {
                    // Mid-line - insert
                    _lineBuffer.Insert(_cursorPosition, (char)b);

                    // Redraw: insert char + tail + move cursor back
                    string tail = _lineBuffer.ToString(_cursorPosition + 1, _lineBuffer.Length - _cursorPosition - 1);
                    SendOutput($"{(char)b}{tail}\x1b[{tail.Length}D");
                }
                _cursorPosition++;
            }
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
    ///     Moves the cursor one position to the right within the line buffer.
    /// </summary>
    private void MoveCursorRight()
    {
        lock (_lock)
        {
            if (_cursorPosition < _lineBuffer.Length)
            {
                _cursorPosition++;
                SendOutput("\x1b[C");
            }
        }
    }

    /// <summary>
    ///     Moves the cursor one position to the left within the line buffer.
    /// </summary>
    private void MoveCursorLeft()
    {
        lock (_lock)
        {
            if (_cursorPosition > 0)
            {
                _cursorPosition--;
                SendOutput("\x1b[D");
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
        SendOutput($"\r{GetPrompt()}\x1b[K");

        // Update buffer
        _lineBuffer.Clear();
        _lineBuffer.Append(newText);
        _cursorPosition = _lineBuffer.Length;

        // Display new text
        SendOutput(newText);
    }

    /// <summary>
    ///     Sends the command prompt to the terminal.
    /// </summary>
    protected void SendPrompt()
    {
        SendOutput(GetPrompt());
    }

    /// <summary>
    ///     Sends text output to the terminal via the channel-based output pump.
    /// </summary>
    /// <param name="text">The text to send</param>
    protected void SendOutput(string text)
    {
        QueueOutput(text);
    }

    /// <summary>
    ///     Sends raw byte data to the terminal via the channel-based output pump.
    /// </summary>
    /// <param name="data">The data to send</param>
    protected void SendOutput(byte[] data)
    {
        QueueOutput(data);
    }

    /// <inheritdoc />
    public override void NotifyTerminalResize(int width, int height)
    {
        lock (_lock)
        {
            _terminalWidth = width;
            _terminalHeight = height;
        }
    }
}
