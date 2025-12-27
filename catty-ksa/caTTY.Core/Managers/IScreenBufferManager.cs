using caTTY.Core.Types;

namespace caTTY.Core.Managers;

/// <summary>
///     Interface for managing screen buffer operations including cell access, clearing, and resizing.
/// </summary>
public interface IScreenBufferManager
{
    /// <summary>
    ///     Gets the width of the screen buffer in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the height of the screen buffer in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    ///     Gets a cell at the specified position.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <param name="col">Column index (0-based)</param>
    /// <returns>The cell at the specified position</returns>
    Cell GetCell(int row, int col);

    /// <summary>
    ///     Sets a cell at the specified position.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <param name="col">Column index (0-based)</param>
    /// <param name="cell">The cell to set</param>
    void SetCell(int row, int col, Cell cell);

    /// <summary>
    ///     Clears the entire screen buffer.
    /// </summary>
    void Clear();

    /// <summary>
    ///     Clears a specific region of the screen buffer.
    /// </summary>
    /// <param name="startRow">Starting row (0-based, inclusive)</param>
    /// <param name="startCol">Starting column (0-based, inclusive)</param>
    /// <param name="endRow">Ending row (0-based, inclusive)</param>
    /// <param name="endCol">Ending column (0-based, inclusive)</param>
    void ClearRegion(int startRow, int startCol, int endRow, int endCol);

    /// <summary>
    ///     Scrolls the buffer up by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up</param>
    void ScrollUp(int lines);

    /// <summary>
    ///     Scrolls the buffer down by the specified number of lines.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    void ScrollDown(int lines);

    /// <summary>
    ///     Resizes the screen buffer to the specified dimensions.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    void Resize(int width, int height);

    /// <summary>
    ///     Gets a read-only span of cells for the specified row.
    /// </summary>
    /// <param name="row">Row index (0-based)</param>
    /// <returns>Read-only span of cells for the row</returns>
    ReadOnlySpan<Cell> GetRow(int row);

    /// <summary>
    ///     Copies a range of rows to the specified destination span.
    /// </summary>
    /// <param name="destination">Destination span to copy to</param>
    /// <param name="startRow">Starting row (0-based, inclusive)</param>
    /// <param name="endRow">Ending row (0-based, inclusive)</param>
    void CopyTo(Span<Cell> destination, int startRow, int endRow);
}