using System;
using System.Text;
using caTTY.Core.Types;
using caTTY.Core.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Terminal;

/// <summary>
/// Core terminal emulator implementation that processes raw byte data and maintains screen state.
/// This is a headless implementation with no UI dependencies.
/// </summary>
public class TerminalEmulator : ITerminalEmulator
{
    private readonly IScreenBuffer _screenBuffer;
    private readonly ICursor _cursor;
    private readonly TerminalState _state;
    private readonly Parser _parser;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the width of the terminal in columns.
    /// </summary>
    public int Width => _screenBuffer.Width;

    /// <summary>
    /// Gets the height of the terminal in rows.
    /// </summary>
    public int Height => _screenBuffer.Height;

    /// <summary>
    /// Gets the current screen buffer for rendering.
    /// </summary>
    public IScreenBuffer ScreenBuffer => _screenBuffer;

    /// <summary>
    /// Gets the current cursor state.
    /// </summary>
    public ICursor Cursor => _cursor;

    /// <summary>
    /// Gets the current terminal state.
    /// </summary>
    public TerminalState State => _state;

    /// <summary>
    /// Event raised when the screen content has been updated and needs refresh.
    /// </summary>
    public event EventHandler<ScreenUpdatedEventArgs>? ScreenUpdated;

    /// <summary>
    /// Event raised when the terminal needs to send a response back to the shell.
    /// </summary>
    public event EventHandler<ResponseEmittedEventArgs>? ResponseEmitted;

    /// <summary>
    /// Event raised when a bell character (BEL, 0x07) is received.
    /// </summary>
    public event EventHandler<BellEventArgs>? Bell;

    /// <summary>
    /// Creates a new terminal emulator with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <param name="logger">Optional logger for debugging (uses NullLogger if not provided)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public TerminalEmulator(int width, int height, ILogger? logger = null)
    {
        if (width < 1 || width > 1000)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 1 and 1000");
        if (height < 1 || height > 1000)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be between 1 and 1000");

        _screenBuffer = new ScreenBuffer(width, height);
        _cursor = new Cursor();
        _state = new TerminalState(width, height);
        _logger = logger ?? NullLogger.Instance;
        
        // Initialize parser with terminal handlers
        var handlers = new TerminalParserHandlers(this, _logger);
        var parserOptions = new ParserOptions
        {
            Handlers = handlers,
            Logger = _logger,
            EmitNormalBytesDuringEscapeSequence = false,
            ProcessC0ControlsDuringEscapeSequence = true
        };
        _parser = new Parser(parserOptions);
        
        _disposed = false;
    }

    /// <summary>
    /// Processes raw byte data from a shell or other source.
    /// Can be called with partial chunks and in rapid succession.
    /// </summary>
    /// <param name="data">The raw byte data to process</param>
    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (data.IsEmpty)
            return;

        // Use parser for proper UTF-8 decoding and escape sequence handling
        _parser.PushBytes(data);

        // Notify that the screen has been updated
        OnScreenUpdated();
    }

    /// <summary>
    /// Processes string data by converting to UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text to process</param>
    public void Write(string text)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(text))
            return;

        // Convert string to UTF-8 bytes and process
        var bytes = Encoding.UTF8.GetBytes(text);
        Write(bytes.AsSpan());
    }

    /// <summary>
    /// Flushes any incomplete UTF-8 sequences in the parser.
    /// This should be called when no more input is expected to ensure
    /// incomplete sequences are handled gracefully.
    /// </summary>
    public void FlushIncompleteSequences()
    {
        ThrowIfDisposed();
        _parser.FlushIncompleteSequences();
        OnScreenUpdated();
    }

    /// <summary>
    /// Resizes the terminal to the specified dimensions.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        if (width < 1 || width > 1000)
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 1 and 1000");
        if (height < 1 || height > 1000)
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be between 1 and 1000");

        // For now, we don't support resizing the existing buffer
        // This will be implemented in a future task
        throw new NotImplementedException("Terminal resizing will be implemented in task 4.9");
    }

    /// <summary>
    /// Handles a line feed (LF) character - move down and scroll if at bottom.
    /// Uses terminal state for proper cursor management.
    /// </summary>
    internal void HandleLineFeed()
    {
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;

        if (_state.CursorY < Height - 1)
        {
            // Move cursor down one row
            _state.CursorY++;
        }
        else
        {
            // At bottom row - need to scroll (will be implemented in future task)
            // For now, just stay at the bottom row
        }

        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
        _state.ClampCursor();
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Handles a carriage return (CR) character - move to column 0.
    /// Uses terminal state for proper cursor management.
    /// </summary>
    internal void HandleCarriageReturn()
    {
        _state.CursorX = 0;
        _state.CursorY = _cursor.Row;
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Handles a bell character (BEL) - emit bell event for notification.
    /// </summary>
    internal void HandleBell()
    {
        OnBell();
    }

    /// <summary>
    /// Handles a backspace character (BS) - move cursor one position left if not at column 0.
    /// Uses terminal state for proper cursor management.
    /// </summary>
    internal void HandleBackspace()
    {
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;

        // Move cursor left if not at column 0
        if (_state.CursorX > 0)
        {
            _state.CursorX--;
        }

        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;

        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Handles a tab character - move to next tab stop using terminal state.
    /// </summary>
    internal void HandleTab()
    {
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;

        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;

        // Find next tab stop
        int nextTabStop = -1;
        for (int col = _state.CursorX + 1; col < Width; col++)
        {
            if (col < _state.TabStops.Length && _state.TabStops[col])
            {
                nextTabStop = col;
                break;
            }
        }

        // If no tab stop found, go to right edge
        if (nextTabStop == -1)
        {
            nextTabStop = Width - 1;
        }

        // Move cursor to the tab stop
        _state.CursorX = nextTabStop;

        // Handle wrap pending if we're at the right edge and auto-wrap is enabled
        if (_state.CursorX >= Width - 1 && _state.AutoWrapMode)
        {
            _state.WrapPending = true;
        }

        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Writes a character at the current cursor position and advances the cursor.
    /// Uses terminal state for wrap pending and SGR attributes.
    /// </summary>
    /// <param name="character">The character to write</param>
    internal void WriteCharacterAtCursor(char character)
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;

        // Handle wrap pending state - if set, wrap to next line first
        if (_state.WrapPending)
        {
            _state.WrapPending = false;
            _state.CursorX = 0;
            if (_state.CursorY < Height - 1)
            {
                _state.CursorY++;
            }
            else
            {
                // At bottom - need to scroll (will be implemented in future task)
                // For now, just stay at the bottom row
            }
        }

        // Clamp cursor position
        if (_state.CursorX >= Width)
        {
            _state.CursorX = Width - 1;
        }

        // Write the character to the screen buffer with current SGR attributes
        var cell = new Cell(character, _state.CurrentSgrState);
        _screenBuffer.SetCell(_state.CursorY, _state.CursorX, cell);

        // Handle cursor advancement and wrap pending
        if (_state.CursorX == Width - 1)
        {
            // At right edge
            if (_state.AutoWrapMode)
            {
                _state.WrapPending = true;
                // Don't advance cursor yet - wait for next character
            }
            // If not in auto-wrap mode, cursor stays at right edge
        }
        else
        {
            // Normal advancement
            _state.CursorX++;
        }

        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Raises the ScreenUpdated event.
    /// </summary>
    private void OnScreenUpdated()
    {
        ScreenUpdated?.Invoke(this, new ScreenUpdatedEventArgs());
    }

    /// <summary>
    /// Raises the ResponseEmitted event.
    /// </summary>
    /// <param name="responseData">The response data to emit</param>
    protected void OnResponseEmitted(ReadOnlyMemory<byte> responseData)
    {
        ResponseEmitted?.Invoke(this, new ResponseEmittedEventArgs(responseData));
    }

    /// <summary>
    /// Raises the ResponseEmitted event with string data.
    /// </summary>
    /// <param name="responseText">The response text to emit</param>
    protected void OnResponseEmitted(string responseText)
    {
        ResponseEmitted?.Invoke(this, new ResponseEmittedEventArgs(responseText));
    }

    /// <summary>
    /// Raises the Bell event.
    /// </summary>
    private void OnBell()
    {
        Bell?.Invoke(this, new BellEventArgs());
    }

    /// <summary>
    /// Throws an ObjectDisposedException if the terminal has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(TerminalEmulator));
    }

    /// <summary>
    /// Disposes the terminal emulator and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }
}