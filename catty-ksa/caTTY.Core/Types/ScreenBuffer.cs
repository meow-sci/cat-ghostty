namespace caTTY.Core.Types;

/// <summary>
///     Implementation of IScreenBuffer using a simple 2D array of cells.
///     All cells are initialized to the default empty cell (space character).
/// </summary>
public class ScreenBuffer : IScreenBuffer
{
    private readonly Cell[,] _cells;

    /// <summary>
    ///     Creates a new screen buffer with the specified dimensions.
    ///     All cells are initialized to empty (space character).
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when width or height is less than 1</exception>
    public ScreenBuffer(int width, int height)
    {
        if (width < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be at least 1");
        }

        if (height < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be at least 1");
        }

        Width = width;
        Height = height;
        _cells = new Cell[height, width];

        // Initialize all cells to empty (space character)
        Clear();
    }

    /// <summary>
    ///     Gets the width of the screen buffer in columns.
    /// </summary>
    public int Width { get; }

    /// <summary>
    ///     Gets the height of the screen buffer in rows.
    /// </summary>
    public int Height { get; }

    /// <summary>
    ///     Gets the cell at the specified position.
    ///     Returns Empty cell if coordinates are out of bounds.
    /// </summary>
    /// <param name="row">The row index (0-based)</param>
    /// <param name="col">The column index (0-based)</param>
    /// <returns>The cell at the specified position</returns>
    public Cell GetCell(int row, int col)
    {
        if (!IsInBounds(row, col))
        {
            return Cell.Empty;
        }

        return _cells[row, col];
    }

    /// <summary>
    ///     Sets the cell at the specified position.
    ///     Ignores the operation if coordinates are out of bounds.
    /// </summary>
    /// <param name="row">The row index (0-based)</param>
    /// <param name="col">The column index (0-based)</param>
    /// <param name="cell">The cell to set</param>
    public void SetCell(int row, int col, Cell cell)
    {
        if (!IsInBounds(row, col))
        {
            return;
        }

        _cells[row, col] = cell;
    }

    /// <summary>
    ///     Clears the entire screen buffer, setting all cells to empty.
    /// </summary>
    public void Clear()
    {
        Cell emptyCell = Cell.Empty;
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _cells[row, col] = emptyCell;
            }
        }
    }

    /// <summary>
    ///     Clears a specific row, setting all cells in that row to empty.
    /// </summary>
    /// <param name="row">The row index to clear</param>
    public void ClearRow(int row)
    {
        if (row < 0 || row >= Height)
        {
            return;
        }

        Cell emptyCell = Cell.Empty;
        for (int col = 0; col < Width; col++)
        {
            _cells[row, col] = emptyCell;
        }
    }

    /// <summary>
    ///     Clears a region of the screen buffer.
    /// </summary>
    /// <param name="startRow">Starting row (inclusive)</param>
    /// <param name="startCol">Starting column (inclusive)</param>
    /// <param name="endRow">Ending row (inclusive)</param>
    /// <param name="endCol">Ending column (inclusive)</param>
    public void ClearRegion(int startRow, int startCol, int endRow, int endCol)
    {
        // Clamp coordinates to valid bounds
        startRow = Math.Max(0, Math.Min(startRow, Height - 1));
        startCol = Math.Max(0, Math.Min(startCol, Width - 1));
        endRow = Math.Max(0, Math.Min(endRow, Height - 1));
        endCol = Math.Max(0, Math.Min(endCol, Width - 1));

        // Ensure start <= end
        if (startRow > endRow || startCol > endCol)
        {
            return;
        }

        Cell emptyCell = Cell.Empty;
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                _cells[row, col] = emptyCell;
            }
        }
    }

    /// <summary>
    ///     Gets a read-only span of cells for the specified row.
    ///     Returns empty span if row is out of bounds.
    /// </summary>
    /// <param name="row">The row index</param>
    /// <returns>Read-only span of cells in the row</returns>
    public ReadOnlySpan<Cell> GetRow(int row)
    {
        if (row < 0 || row >= Height)
        {
            return ReadOnlySpan<Cell>.Empty;
        }

        // Create an array for the row data since we can't create spans from 2D arrays directly
        var rowData = new Cell[Width];
        for (int col = 0; col < Width; col++)
        {
            rowData[col] = _cells[row, col];
        }

        return new ReadOnlySpan<Cell>(rowData);
    }

    /// <summary>
    ///     Scrolls the buffer up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up</param>
    public void ScrollUp(int lines)
    {
        if (lines <= 0 || lines >= Height)
        {
            // If scrolling entire buffer or more, just clear it
            if (lines >= Height)
            {
                Clear();
            }
            return;
        }

        // Move rows up
        for (int row = 0; row < Height - lines; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _cells[row, col] = _cells[row + lines, col];
            }
        }

        // Clear the bottom rows
        Cell emptyCell = Cell.Empty;
        for (int row = Height - lines; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _cells[row, col] = emptyCell;
            }
        }
    }

    /// <summary>
    ///     Scrolls the buffer down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    public void ScrollDown(int lines)
    {
        if (lines <= 0 || lines >= Height)
        {
            // If scrolling entire buffer or more, just clear it
            if (lines >= Height)
            {
                Clear();
            }
            return;
        }

        // Move rows down
        for (int row = Height - 1; row >= lines; row--)
        {
            for (int col = 0; col < Width; col++)
            {
                _cells[row, col] = _cells[row - lines, col];
            }
        }

        // Clear the top rows
        Cell emptyCell = Cell.Empty;
        for (int row = 0; row < lines; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                _cells[row, col] = emptyCell;
            }
        }
    }

    /// <summary>
    ///     Copies a range of rows to the specified destination span.
    /// </summary>
    /// <param name="destination">Destination span to copy to</param>
    /// <param name="startRow">Starting row (0-based, inclusive)</param>
    /// <param name="endRow">Ending row (0-based, inclusive)</param>
    public void CopyTo(Span<Cell> destination, int startRow, int endRow)
    {
        if (startRow < 0 || startRow >= Height || endRow < 0 || endRow >= Height)
        {
            return;
        }

        if (startRow > endRow)
        {
            (startRow, endRow) = (endRow, startRow);
        }

        int rowCount = endRow - startRow + 1;
        int totalCells = rowCount * Width;

        if (destination.Length < totalCells)
        {
            return; // Not enough space in destination
        }

        int destIndex = 0;
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                destination[destIndex++] = _cells[row, col];
            }
        }
    }

    /// <summary>
    ///     Resizes the screen buffer to the specified dimensions with content preservation.
    ///     Height change: preserve top-to-bottom rows where possible.
    ///     Width change: truncate/pad each row; do not attempt complex reflow.
    /// </summary>
    /// <param name="newWidth">New width in columns</param>
    /// <param name="newHeight">New height in rows</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public void Resize(int newWidth, int newHeight)
    {
        if (newWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(newWidth), "Width must be at least 1");
        }

        if (newHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(newHeight), "Height must be at least 1");
        }

        // If dimensions are the same, no work needed
        if (newWidth == Width && newHeight == Height)
        {
            return;
        }

        // Create new buffer with new dimensions
        var newCells = new Cell[newHeight, newWidth];

        // Initialize all new cells to empty
        Cell emptyCell = Cell.Empty;
        for (int row = 0; row < newHeight; row++)
        {
            for (int col = 0; col < newWidth; col++)
            {
                newCells[row, col] = emptyCell;
            }
        }

        // Copy existing content with preservation policy
        int rowsToCopy = Math.Min(Height, newHeight);
        int colsToCopy = Math.Min(Width, newWidth);

        for (int row = 0; row < rowsToCopy; row++)
        {
            for (int col = 0; col < colsToCopy; col++)
            {
                newCells[row, col] = _cells[row, col];
            }

            // If new width is larger, pad the row with empty cells (already initialized above)
            // If new width is smaller, truncation happens naturally by not copying beyond colsToCopy
        }

        // Replace the old buffer with the new one
        // Note: We can't directly replace the array reference since Width/Height are readonly
        // Instead, we need to copy the new buffer back to the existing array
        // This requires creating a new ScreenBuffer instance, which we'll handle in the manager

        // For now, we'll use reflection to update the fields since this is an internal resize operation
        var widthField = typeof(ScreenBuffer).GetField("<Width>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var heightField = typeof(ScreenBuffer).GetField("<Height>k__BackingField", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cellsField = typeof(ScreenBuffer).GetField("_cells", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (widthField != null && heightField != null && cellsField != null)
        {
            widthField.SetValue(this, newWidth);
            heightField.SetValue(this, newHeight);
            cellsField.SetValue(this, newCells);
        }
        else
        {
            throw new InvalidOperationException("Unable to resize buffer due to reflection failure");
        }
    }

    /// <summary>
    ///     Checks if the specified coordinates are within bounds.
    /// </summary>
    /// <param name="row">The row index</param>
    /// <param name="col">The column index</param>
    /// <returns>True if coordinates are valid, false otherwise</returns>
    public bool IsInBounds(int row, int col)
    {
        return row >= 0 && row < Height && col >= 0 && col < Width;
    }
}
