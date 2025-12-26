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
