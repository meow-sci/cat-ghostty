using caTTY.Core.Types;

namespace caTTY.Core.Managers;

/// <summary>
///     Manages screen buffer operations including cell access, clearing, and resizing.
///     Provides efficient access to the 2D character grid with bounds checking.
/// </summary>
public class ScreenBufferManager : IScreenBufferManager
{
    private readonly IScreenBuffer _screenBuffer;

    /// <summary>
    ///     Creates a new screen buffer manager with the specified screen buffer.
    /// </summary>
    /// <param name="screenBuffer">The screen buffer to manage</param>
    /// <exception cref="ArgumentNullException">Thrown when screenBuffer is null</exception>
    public ScreenBufferManager(IScreenBuffer screenBuffer)
    {
        _screenBuffer = screenBuffer ?? throw new ArgumentNullException(nameof(screenBuffer));
    }

    /// <summary>
    ///     Gets the width of the screen buffer in columns.
    /// </summary>
    public int Width => _screenBuffer.Width;

    /// <summary>
    ///     Gets the height of the screen buffer in rows.
    /// </summary>
    public int Height => _screenBuffer.Height;

    /// <summary>
    ///     Gets a cell at the specified position with bounds checking.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <param name="col">Column index (0-based)</param>
    /// <returns>The cell at the specified position, or default cell if out of bounds</returns>
    public Cell GetCell(int row, int col)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width)
        {
            return Cell.Empty;
        }

        return _screenBuffer.GetCell(row, col);
    }

    /// <summary>
    ///     Sets a cell at the specified position with bounds checking.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <param name="col">Column index (0-based)</param>
    /// <param name="cell">The cell to set</param>
    public void SetCell(int row, int col, Cell cell)
    {
        if (row < 0 || row >= Height || col < 0 || col >= Width)
        {
            return;
        }

        _screenBuffer.SetCell(row, col, cell);
    }

    /// <summary>
    ///     Clears the entire screen buffer.
    /// </summary>
    public void Clear()
    {
        _screenBuffer.Clear();
    }

    /// <summary>
    ///     Clears a specific region of the screen buffer.
    /// </summary>
    /// <param name="startRow">Starting row (0-based, inclusive)</param>
    /// <param name="startCol">Starting column (0-based, inclusive)</param>
    /// <param name="endRow">Ending row (0-based, inclusive)</param>
    /// <param name="endCol">Ending column (0-based, inclusive)</param>
    public void ClearRegion(int startRow, int startCol, int endRow, int endCol)
    {
        // Clamp bounds
        startRow = Math.Max(0, Math.Min(Height - 1, startRow));
        startCol = Math.Max(0, Math.Min(Width - 1, startCol));
        endRow = Math.Max(0, Math.Min(Height - 1, endRow));
        endCol = Math.Max(0, Math.Min(Width - 1, endCol));

        // Ensure start <= end
        if (startRow > endRow)
        {
            (startRow, endRow) = (endRow, startRow);
        }
        if (startCol > endCol)
        {
            (startCol, endCol) = (endCol, startCol);
        }

        var emptyCell = Cell.Empty;
        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = startCol; col <= endCol; col++)
            {
                _screenBuffer.SetCell(row, col, emptyCell);
            }
        }
    }

    /// <summary>
    ///     Scrolls the buffer up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up</param>
    public void ScrollUp(int lines)
    {
        if (lines <= 0)
        {
            return;
        }

        _screenBuffer.ScrollUp(lines);
    }

    /// <summary>
    ///     Scrolls the buffer down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    public void ScrollDown(int lines)
    {
        if (lines <= 0)
        {
            return;
        }

        _screenBuffer.ScrollDown(lines);
    }

    /// <summary>
    ///     Resizes the screen buffer to the specified dimensions.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    public void Resize(int width, int height)
    {
        if (width < 1 || width > 1000 || height < 1 || height > 1000)
        {
            throw new ArgumentOutOfRangeException("Width and height must be between 1 and 1000");
        }

        // For now, throw NotImplementedException as resizing will be implemented in task 4.9
        throw new NotImplementedException("Screen buffer resizing will be implemented in task 4.9");
    }

    /// <summary>
    ///     Gets a read-only span of cells for the specified row.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <returns>Read-only span of cells for the row</returns>
    public ReadOnlySpan<Cell> GetRow(int row)
    {
        if (row < 0 || row >= Height)
        {
            return ReadOnlySpan<Cell>.Empty;
        }

        return _screenBuffer.GetRow(row);
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

        _screenBuffer.CopyTo(destination, startRow, endRow);
    }
}