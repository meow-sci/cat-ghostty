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
    /// Handles a line feed (LF) character - move down and to beginning of line, scroll if at bottom.
    /// Uses terminal state for proper cursor management.
    /// </summary>
    internal void HandleLineFeed()
    {
        if (_cursor.Row < Height - 1)
        {
            // Move cursor down one row and to beginning of line
            _cursor.SetPosition(_cursor.Row + 1, 0);
        }
        else
        {
            // At bottom row - need to scroll (will be implemented in future task)
            // For now, just stay at the bottom row and move to beginning
            _cursor.SetPosition(_cursor.Row, 0);
        }
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        _state.WrapPending = false;
    }

    /// <summary>
    /// Handles index operation (ESC D) - move cursor down one line without changing column.
    /// Used by ESC D sequence.
    /// </summary>
    internal void HandleIndex()
    {
        if (_cursor.Row < Height - 1)
        {
            // Move cursor down one row, keep same column
            _cursor.SetPosition(_cursor.Row + 1, _cursor.Col);
        }
        else
        {
            // At bottom row - need to scroll (will be implemented in future task)
            // For now, just stay at the bottom row
        }
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        _state.WrapPending = false;
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
    /// Moves the cursor up by the specified number of lines.
    /// </summary>
    /// <param name="count">Number of lines to move up (minimum 1)</param>
    internal void MoveCursorUp(int count)
    {
        count = Math.Max(1, count);
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Move cursor up, clamping to top boundary
        _state.CursorY = Math.Max(0, _state.CursorY - count);
        
        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Moves the cursor down by the specified number of lines.
    /// </summary>
    /// <param name="count">Number of lines to move down (minimum 1)</param>
    internal void MoveCursorDown(int count)
    {
        count = Math.Max(1, count);
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Move cursor down, clamping to bottom boundary
        _state.CursorY = Math.Min(Height - 1, _state.CursorY + count);
        
        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Moves the cursor forward (right) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move forward (minimum 1)</param>
    internal void MoveCursorForward(int count)
    {
        count = Math.Max(1, count);
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Move cursor forward, clamping to right boundary
        _state.CursorX = Math.Min(Width - 1, _state.CursorX + count);
        
        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Moves the cursor backward (left) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move backward (minimum 1)</param>
    internal void MoveCursorBackward(int count)
    {
        count = Math.Max(1, count);
        
        // Sync state with cursor
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Move cursor backward, clamping to left boundary
        _state.CursorX = Math.Max(0, _state.CursorX - count);
        
        // Clear wrap pending state since we're moving the cursor
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Sets the cursor to an absolute position.
    /// </summary>
    /// <param name="row">Target row (1-based, will be converted to 0-based)</param>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorPosition(int row, int column)
    {
        // Convert from 1-based to 0-based coordinates and clamp to bounds
        var targetRow = Math.Max(0, Math.Min(Height - 1, row - 1));
        var targetCol = Math.Max(0, Math.Min(Width - 1, column - 1));
        
        // Update state
        _state.CursorX = targetCol;
        _state.CursorY = targetRow;
        
        // Clear wrap pending state since we're setting absolute position
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Sets the cursor to an absolute column position on the current row.
    /// Implements CSI G (Cursor Horizontal Absolute) sequence.
    /// </summary>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorColumn(int column)
    {
        // Convert from 1-based to 0-based coordinates and clamp to bounds
        var targetCol = Math.Max(0, Math.Min(Width - 1, column - 1));
        
        // Update state - keep current row, change column
        _state.CursorX = targetCol;
        
        // Clear wrap pending state since we're setting absolute position
        _state.WrapPending = false;
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Clears the display according to the specified erase mode.
    /// Implements CSI J (Erase in Display) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end, 1=start to cursor, 2=entire screen, 3=entire screen and scrollback</param>
    internal void ClearDisplay(int mode)
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Clear wrap pending state
        _state.WrapPending = false;
        
        // Create empty cell with current SGR attributes
        var emptyCell = new Cell(' ', _state.CurrentSgrState);
        
        switch (mode)
        {
            case 0: // From cursor to end of display
                ClearLine(0); // Clear from cursor to end of current line
                // Clear all lines below cursor
                for (int row = _state.CursorY + 1; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        _screenBuffer.SetCell(row, col, emptyCell);
                    }
                }
                break;
                
            case 1: // From start of display to cursor
                // Clear all lines above cursor
                for (int row = 0; row < _state.CursorY; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        _screenBuffer.SetCell(row, col, emptyCell);
                    }
                }
                ClearLine(1); // Clear from start of current line to cursor
                break;
                
            case 2: // Entire display
                _screenBuffer.Clear();
                break;
                
            case 3: // Entire display and scrollback (xterm extension)
                // TODO: Clear scrollback buffer when implemented (task 4.1-4.6)
                _screenBuffer.Clear();
                break;
        }
    }

    /// <summary>
    /// Clears the current line according to the specified erase mode.
    /// Implements CSI K (Erase in Line) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end of line, 1=start of line to cursor, 2=entire line</param>
    internal void ClearLine(int mode)
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Clear wrap pending state
        _state.WrapPending = false;
        
        // Bounds check
        if (_state.CursorY < 0 || _state.CursorY >= Height)
        {
            return;
        }
        
        // Create empty cell with current SGR attributes
        var emptyCell = new Cell(' ', _state.CurrentSgrState);
        
        switch (mode)
        {
            case 0: // From cursor to end of line
                for (int col = _state.CursorX; col < Width; col++)
                {
                    _screenBuffer.SetCell(_state.CursorY, col, emptyCell);
                }
                break;
                
            case 1: // From start of line to cursor
                for (int col = 0; col <= _state.CursorX && col < Width; col++)
                {
                    _screenBuffer.SetCell(_state.CursorY, col, emptyCell);
                }
                break;
                
            case 2: // Entire line
                for (int col = 0; col < Width; col++)
                {
                    _screenBuffer.SetCell(_state.CursorY, col, emptyCell);
                }
                break;
        }
    }

    /// <summary>
    /// Saves the current cursor position for later restoration with ESC 8.
    /// Implements ESC 7 (Save Cursor) sequence.
    /// </summary>
    internal void SaveCursorPosition()
    {
        // Save current cursor position in terminal state
        _state.SavedCursor = (_cursor.Col, _cursor.Row);
    }

    /// <summary>
    /// Restores the previously saved cursor position.
    /// Implements ESC 8 (Restore Cursor) sequence.
    /// </summary>
    internal void RestoreCursorPosition()
    {
        if (_state.SavedCursor.HasValue)
        {
            var (x, y) = _state.SavedCursor.Value;
            
            // Clamp to bounds
            x = Math.Max(0, Math.Min(Width - 1, x));
            y = Math.Max(0, Math.Min(Height - 1, y));
            
            // Update cursor directly
            _cursor.SetPosition(y, x);
            
            // Sync state with cursor
            _state.CursorX = x;
            _state.CursorY = y;
            _state.WrapPending = false;
        }
    }

    /// <summary>
    /// Handles reverse index (ESC M) - move cursor up; if at top margin, scroll region down.
    /// Used by full-screen applications like less to scroll the display down within the scroll region.
    /// </summary>
    internal void HandleReverseIndex()
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Clear wrap pending state
        _state.WrapPending = false;
        
        if (_state.CursorY <= _state.ScrollTop)
        {
            // At top of scroll region - scroll the region down
            _state.CursorY = _state.ScrollTop;
            // TODO: Implement scroll region down operation (will be implemented in task 4.6)
            // For now, just stay at the top
        }
        else
        {
            // Move cursor up one line
            _state.CursorY = Math.Max(_state.ScrollTop, _state.CursorY - 1);
        }
        
        // Update cursor to match state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Sets a tab stop at the current cursor position.
    /// Implements ESC H (Horizontal Tab Set) sequence.
    /// </summary>
    internal void SetTabStopAtCursor()
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Set tab stop at current cursor position
        if (_state.CursorX >= 0 && _state.CursorX < _state.TabStops.Length)
        {
            _state.TabStops[_state.CursorX] = true;
        }
    }

    /// <summary>
    /// Moves cursor forward to the next tab stop.
    /// Implements CSI I (Cursor Forward Tab) sequence.
    /// </summary>
    /// <param name="count">Number of tab stops to move forward</param>
    internal void CursorForwardTab(int count)
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        var n = Math.Max(1, count);
        
        if (_state.CursorX < 0)
        {
            _state.CursorX = 0;
        }

        for (int i = 0; i < n; i++)
        {
            int nextStop = -1;
            for (int x = _state.CursorX + 1; x < Width; x++)
            {
                if (x < _state.TabStops.Length && _state.TabStops[x])
                {
                    nextStop = x;
                    break;
                }
            }
            _state.CursorX = nextStop == -1 ? (Width - 1) : nextStop;
        }

        _state.WrapPending = false;
        
        // Update cursor position
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Moves cursor backward to the previous tab stop.
    /// Implements CSI Z (Cursor Backward Tab) sequence.
    /// </summary>
    /// <param name="count">Number of tab stops to move backward</param>
    internal void CursorBackwardTab(int count)
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        var n = Math.Max(1, count);
        
        if (_state.CursorX < 0)
        {
            _state.CursorX = 0;
        }

        for (int i = 0; i < n; i++)
        {
            int prevStop = -1;
            for (int x = _state.CursorX - 1; x >= 0; x--)
            {
                if (x < _state.TabStops.Length && _state.TabStops[x])
                {
                    prevStop = x;
                    break;
                }
            }
            _state.CursorX = prevStop == -1 ? 0 : prevStop;
        }

        _state.WrapPending = false;
        
        // Update cursor position
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Clears the tab stop at the current cursor position.
    /// Implements CSI g (Tab Clear) sequence with mode 0.
    /// </summary>
    internal void ClearTabStopAtCursor()
    {
        // Sync cursor with state
        _state.CursorX = _cursor.Col;
        _state.CursorY = _cursor.Row;
        
        // Clear tab stop at current cursor position
        if (_state.CursorX >= 0 && _state.CursorX < _state.TabStops.Length)
        {
            _state.TabStops[_state.CursorX] = false;
        }
    }

    /// <summary>
    /// Clears all tab stops.
    /// Implements CSI 3 g (Tab Clear) sequence with mode 3.
    /// </summary>
    internal void ClearAllTabStops()
    {
        // Clear all tab stops
        for (int i = 0; i < _state.TabStops.Length; i++)
        {
            _state.TabStops[i] = false;
        }
    }

    /// <summary>
    /// Resets the terminal to its initial state.
    /// Implements ESC c (Reset to Initial State) sequence.
    /// </summary>
    internal void ResetToInitialState()
    {
        // Reset terminal state
        _state.Reset();
        
        // Clear the screen buffer
        _screenBuffer.Clear();
        
        // Update cursor to match reset state
        _cursor.SetPosition(_state.CursorY, _state.CursorX);
    }

    /// <summary>
    /// Designates a character set to a specific G slot.
    /// Implements ESC ( X, ESC ) X, ESC * X, ESC + X sequences.
    /// </summary>
    /// <param name="slot">The G slot to designate (G0, G1, G2, G3)</param>
    /// <param name="charset">The character set identifier</param>
    internal void DesignateCharacterSet(string slot, string charset)
    {
        switch (slot)
        {
            case "G0":
                _state.CharacterSets.G0 = charset;
                break;
            case "G1":
                _state.CharacterSets.G1 = charset;
                break;
            case "G2":
                _state.CharacterSets.G2 = charset;
                break;
            case "G3":
                _state.CharacterSets.G3 = charset;
                break;
        }
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