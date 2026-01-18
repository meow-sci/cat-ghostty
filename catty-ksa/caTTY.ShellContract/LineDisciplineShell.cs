using System.Text;

namespace caTTY.ShellContract;

/// <summary>
///     Abstract shell implementation providing line discipline features: input buffering,
///     command history, escape sequence handling, and line editing (backspace, arrows).
///     Subclasses only need to implement ExecuteCommand() to handle command execution.
/// </summary>
public abstract class LineDisciplineShell : BaseCustomShell
{
    private readonly LineDisciplineOptions _options;
    private readonly StringBuilder _lineBuffer = new();
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private string? _savedCurrentLine;
    private EscapeState _escapeState = EscapeState.None;
    private readonly StringBuilder _escapeBuffer = new();

    /// <summary>
    ///     Escape sequence parsing state.
    /// </summary>
    private enum EscapeState
    {
        /// <summary>No escape sequence in progress.</summary>
        None,
        /// <summary>ESC received, waiting for next byte.</summary>
        Escape,
        /// <summary>ESC [ received (CSI), accumulating sequence.</summary>
        Csi
    }

    /// <summary>
    ///     Creates a new LineDisciplineShell with default options.
    /// </summary>
    protected LineDisciplineShell()
        : this(LineDisciplineOptions.CreateDefault())
    {
    }

    /// <summary>
    ///     Creates a new LineDisciplineShell with custom options.
    /// </summary>
    /// <param name="options">Line discipline configuration options</param>
    protected LineDisciplineShell(LineDisciplineOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///     Handles a single input byte with line discipline processing.
    ///     Processes escape sequences, echo, backspace, enter, and history navigation.
    /// </summary>
    /// <param name="b">Input byte to process</param>
    protected override void OnInputByte(byte b)
    {
        // Handle escape sequences if enabled
        if (_options.ParseEscapeSequences && ProcessEscapeSequence(b))
        {
            return;
        }

        // Ctrl+L (clear screen)
        if (b == 0x0C)
        {
            ClearScreen();
            return;
        }

        // Enter (CR or LF)
        if (b == 0x0D || b == 0x0A)
        {
            HandleEnter();
            return;
        }

        // Backspace (DEL or BS)
        if (b == 0x7F || b == 0x08)
        {
            HandleBackspace();
            return;
        }

        // Printable ASCII characters
        if (b >= 0x20 && b <= 0x7E)
        {
            _lineBuffer.Append((char)b);
            if (_options.EchoInput)
            {
                QueueOutput(new[] { b });
            }
            return;
        }

        // Ignore other control characters
    }

    /// <summary>
    ///     Processes escape sequence bytes. Returns true if byte was consumed by escape processing.
    /// </summary>
    private bool ProcessEscapeSequence(byte b)
    {
        switch (_escapeState)
        {
            case EscapeState.None:
                if (b == 0x1B) // ESC
                {
                    _escapeState = EscapeState.Escape;
                    _escapeBuffer.Clear();
                    return true;
                }
                return false;

            case EscapeState.Escape:
                if (b == (byte)'[')
                {
                    _escapeState = EscapeState.Csi;
                    _escapeBuffer.Append('[');
                    return true;
                }
                // Unknown escape sequence, reset
                _escapeState = EscapeState.None;
                return true;

            case EscapeState.Csi:
                _escapeBuffer.Append((char)b);

                // CSI sequences end with a character in the range 0x40-0x7E
                if (b >= 0x40 && b <= 0x7E)
                {
                    HandleCsiSequence((char)b);
                    _escapeState = EscapeState.None;
                    return true;
                }
                // Still accumulating CSI sequence
                return true;

            default:
                _escapeState = EscapeState.None;
                return false;
        }
    }

    /// <summary>
    ///     Handles a complete CSI sequence.
    /// </summary>
    private void HandleCsiSequence(char finalByte)
    {
        if (!_options.EnableHistory)
        {
            return;
        }

        switch (finalByte)
        {
            case 'A': // Up arrow
                NavigateHistoryUp();
                break;
            case 'B': // Down arrow
                NavigateHistoryDown();
                break;
            // Ignore other CSI sequences (C=right, D=left, etc.)
        }
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

        // Save current line if this is the first up arrow
        if (_historyIndex == -1)
        {
            _savedCurrentLine = _lineBuffer.ToString();
            _historyIndex = _commandHistory.Count - 1;
        }
        else if (_historyIndex > 0)
        {
            _historyIndex--;
        }
        else
        {
            // Already at oldest command
            return;
        }

        ReplaceLineBuffer(_commandHistory[_historyIndex]);
    }

    /// <summary>
    ///     Navigates to the next command in history (or back to saved current line).
    /// </summary>
    private void NavigateHistoryDown()
    {
        if (_historyIndex == -1)
        {
            return; // Not navigating history
        }

        _historyIndex++;

        if (_historyIndex >= _commandHistory.Count)
        {
            // Restore saved current line
            ReplaceLineBuffer(_savedCurrentLine ?? string.Empty);
            _historyIndex = -1;
            _savedCurrentLine = null;
        }
        else
        {
            ReplaceLineBuffer(_commandHistory[_historyIndex]);
        }
    }

    /// <summary>
    ///     Replaces the current line buffer with new text and updates the display.
    /// </summary>
    private void ReplaceLineBuffer(string newText)
    {
        // Clear current line
        ClearCurrentLine();

        // Set new text
        _lineBuffer.Clear();
        _lineBuffer.Append(newText);

        // Redraw prompt and new line
        SendPrompt();
        if (_options.EchoInput)
        {
            QueueOutput(newText);
        }
    }

    /// <summary>
    ///     Clears the current input line on the terminal.
    /// </summary>
    private void ClearCurrentLine()
    {
        // Move to beginning of line and erase
        QueueOutput("\r\x1b[K");
    }

    /// <summary>
    ///     Handles Enter key: executes command and resets state.
    /// </summary>
    private void HandleEnter()
    {
        // Echo newline
        if (_options.EchoInput)
        {
            QueueOutput("\r\n");
        }

        var command = _lineBuffer.ToString().Trim();
        _lineBuffer.Clear();

        // Add to history (if enabled and non-empty, avoiding duplicates)
        if (_options.EnableHistory && !string.IsNullOrWhiteSpace(command))
        {
            // Don't add if it's the same as the last command
            if (_commandHistory.Count == 0 || _commandHistory[^1] != command)
            {
                _commandHistory.Add(command);

                // Enforce max history size
                if (_options.MaxHistorySize > 0 && _commandHistory.Count > _options.MaxHistorySize)
                {
                    _commandHistory.RemoveAt(0);
                }
            }
        }

        // Reset history navigation
        _historyIndex = -1;
        _savedCurrentLine = null;

        // Execute command
        if (!string.IsNullOrWhiteSpace(command))
        {
            ExecuteCommand(command);
        }

        // Send new prompt
        SendPrompt();
    }

    /// <summary>
    ///     Handles Backspace key: removes last character from buffer.
    /// </summary>
    private void HandleBackspace()
    {
        if (_lineBuffer.Length == 0)
        {
            return;
        }

        _lineBuffer.Length--;

        if (_options.EchoInput)
        {
            // Send backspace sequence: BS + space + BS
            QueueOutput("\b \b");
        }
    }

    /// <summary>
    ///     Executes a command. Subclasses must implement this to provide shell functionality.
    /// </summary>
    /// <param name="command">The command line to execute</param>
    protected abstract void ExecuteCommand(string command);

    /// <summary>
    ///     Gets the prompt string to display. Default is "$ ".
    /// </summary>
    /// <returns>The prompt string</returns>
    protected virtual string GetPrompt()
    {
        return "$ ";
    }

    /// <summary>
    ///     Gets the banner text to display when the shell starts.
    /// </summary>
    /// <returns>Banner text, or null for no banner</returns>
    protected virtual string? GetBanner()
    {
        return null;
    }

    /// <summary>
    ///     Returns initial output (banner + prompt) for the shell.
    /// </summary>
    protected override string? GetInitialOutput()
    {
        var sb = new StringBuilder();

        var banner = GetBanner();
        if (!string.IsNullOrEmpty(banner))
        {
            sb.Append(banner);
            if (!banner.EndsWith('\n'))
            {
                sb.Append("\r\n");
            }
        }

        sb.Append(GetPrompt());

        return sb.ToString();
    }

    /// <summary>
    ///     Sends the prompt to the terminal.
    /// </summary>
    protected void SendPrompt()
    {
        QueueOutput(GetPrompt());
    }

    /// <summary>
    ///     Clears the screen while preserving scrollback.
    ///     Sends: ESC[2J (clear screen) + ESC[H (cursor to home)
    /// </summary>
    protected void ClearScreen()
    {
        QueueOutput("\x1b[2J\x1b[H");
        SendPrompt();
    }

    /// <summary>
    ///     Clears the screen AND scrollback buffer.
    ///     Sends: ESC[3J (clear screen + scrollback) + ESC[2J + ESC[H
    /// </summary>
    protected void ClearScreenAndScrollback()
    {
        QueueOutput("\x1b[3J\x1b[2J\x1b[H");
        SendPrompt();
    }
}
