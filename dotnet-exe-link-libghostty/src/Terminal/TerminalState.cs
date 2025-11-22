namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Represents the complete state of a terminal emulator.
/// Maintains an 80x24 screen buffer, cursor position, and current text attributes.
/// </summary>
public class TerminalState
{
    public const int Width = 80;
    public const int Height = 24;
    
    /// <summary>
    /// The screen buffer as a 2D array [row, col].
    /// </summary>
    private readonly TerminalCell[,] _buffer;
    
    /// <summary>
    /// Current cursor row (0-based, 0 = top).
    /// </summary>
    public int CursorRow { get; private set; }
    
    /// <summary>
    /// Current cursor column (0-based, 0 = left).
    /// </summary>
    public int CursorColumn { get; private set; }
    
    /// <summary>
    /// Current text attributes to apply to newly written characters.
    /// </summary>
    public TerminalCell CurrentAttributes { get; set; }
    
    /// <summary>
    /// Window title (updated by OSC sequences).
    /// </summary>
    public string WindowTitle { get; set; }
    
    /// <summary>
    /// Creates a new terminal state with an empty screen buffer.
    /// </summary>
    public TerminalState()
    {
        _buffer = new TerminalCell[Height, Width];
        Clear();
        CurrentAttributes = new TerminalCell();
        WindowTitle = string.Empty;
    }
    
    /// <summary>
    /// Gets the cell at the specified position.
    /// </summary>
    public TerminalCell GetCell(int row, int col)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width)
            return new TerminalCell();
        
        return _buffer[row, col];
    }
    
    /// <summary>
    /// Sets the cell at the specified position.
    /// </summary>
    public void SetCell(int row, int col, TerminalCell cell)
    {
        if (row >= 0 && row < Height && col >= 0 && col < Width)
        {
            _buffer[row, col] = cell;
        }
    }
    
    /// <summary>
    /// Writes a character at the current cursor position with current attributes,
    /// then advances the cursor.
    /// </summary>
    public void WriteChar(char ch)
    {
        if (ch == '\r')
        {
            CursorColumn = 0;
            return;
        }
        
        if (ch == '\n')
        {
            CursorRow++;
            if (CursorRow >= Height)
            {
                ScrollUp();
                CursorRow = Height - 1;
            }
            return;
        }
        
        if (ch == '\b')
        {
            if (CursorColumn > 0)
                CursorColumn--;
            return;
        }
        
        if (ch == '\t')
        {
            // Tab stops every 8 columns
            int nextTab = ((CursorColumn / 8) + 1) * 8;
            CursorColumn = Math.Min(nextTab, Width - 1);
            return;
        }
        
        // Write character with current attributes
        if (CursorColumn < Width)
        {
            var cell = CurrentAttributes.WithCharacter(ch);
            SetCell(CursorRow, CursorColumn, cell);
            CursorColumn++;
            
            // Wrap to next line if we reach the edge
            if (CursorColumn >= Width)
            {
                CursorColumn = 0;
                CursorRow++;
                if (CursorRow >= Height)
                {
                    ScrollUp();
                    CursorRow = Height - 1;
                }
            }
        }
    }
    
    /// <summary>
    /// Sets the cursor position (0-based).
    /// </summary>
    public void SetCursorPosition(int row, int col)
    {
        CursorRow = Math.Clamp(row, 0, Height - 1);
        CursorColumn = Math.Clamp(col, 0, Width - 1);
    }
    
    /// <summary>
    /// Moves the cursor up by the specified number of rows.
    /// </summary>
    public void MoveCursorUp(int count = 1)
    {
        CursorRow = Math.Max(0, CursorRow - count);
    }
    
    /// <summary>
    /// Moves the cursor down by the specified number of rows.
    /// </summary>
    public void MoveCursorDown(int count = 1)
    {
        CursorRow = Math.Min(Height - 1, CursorRow + count);
    }
    
    /// <summary>
    /// Moves the cursor forward (right) by the specified number of columns.
    /// </summary>
    public void MoveCursorForward(int count = 1)
    {
        CursorColumn = Math.Min(Width - 1, CursorColumn + count);
    }
    
    /// <summary>
    /// Moves the cursor backward (left) by the specified number of columns.
    /// </summary>
    public void MoveCursorBackward(int count = 1)
    {
        CursorColumn = Math.Max(0, CursorColumn - count);
    }
    
    /// <summary>
    /// Clears the entire screen buffer.
    /// </summary>
    public void Clear()
    {
        var emptyCell = new TerminalCell();
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _buffer[row, col] = emptyCell;
            }
        }
        CursorRow = 0;
        CursorColumn = 0;
    }
    
    /// <summary>
    /// Clears from the cursor to the end of the screen.
    /// </summary>
    public void ClearToEndOfScreen()
    {
        var emptyCell = new TerminalCell();
        
        // Clear from cursor to end of current line
        for (int col = CursorColumn; col < Width; col++)
        {
            _buffer[CursorRow, col] = emptyCell;
        }
        
        // Clear all lines below current
        for (int row = CursorRow + 1; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _buffer[row, col] = emptyCell;
            }
        }
    }
    
    /// <summary>
    /// Clears from the beginning of the screen to the cursor.
    /// </summary>
    public void ClearToBeginningOfScreen()
    {
        var emptyCell = new TerminalCell();
        
        // Clear all lines above current
        for (int row = 0; row < CursorRow; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _buffer[row, col] = emptyCell;
            }
        }
        
        // Clear from beginning of current line to cursor
        for (int col = 0; col <= CursorColumn; col++)
        {
            _buffer[CursorRow, col] = emptyCell;
        }
    }
    
    /// <summary>
    /// Clears the current line from cursor to end.
    /// </summary>
    public void ClearToEndOfLine()
    {
        var emptyCell = new TerminalCell();
        for (int col = CursorColumn; col < Width; col++)
        {
            _buffer[CursorRow, col] = emptyCell;
        }
    }
    
    /// <summary>
    /// Clears the current line from beginning to cursor.
    /// </summary>
    public void ClearToBeginningOfLine()
    {
        var emptyCell = new TerminalCell();
        for (int col = 0; col <= CursorColumn; col++)
        {
            _buffer[CursorRow, col] = emptyCell;
        }
    }
    
    /// <summary>
    /// Clears the entire current line.
    /// </summary>
    public void ClearLine()
    {
        var emptyCell = new TerminalCell();
        for (int col = 0; col < Width; col++)
        {
            _buffer[CursorRow, col] = emptyCell;
        }
    }
    
    /// <summary>
    /// Scrolls the entire screen up by one line.
    /// The top line is lost, and a blank line is added at the bottom.
    /// </summary>
    private void ScrollUp()
    {
        // Move all lines up by one
        for (int row = 0; row < Height - 1; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _buffer[row, col] = _buffer[row + 1, col];
            }
        }
        
        // Clear the bottom line
        var emptyCell = new TerminalCell();
        for (int col = 0; col < Width; col++)
        {
            _buffer[Height - 1, col] = emptyCell;
        }
    }
}
