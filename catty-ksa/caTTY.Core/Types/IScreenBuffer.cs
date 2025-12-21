using System;

namespace caTTY.Core.Types;

/// <summary>
/// Interface for a terminal screen buffer that manages a 2D grid of cells.
/// Provides operations for cell access and clearing operations needed by CSI erase modes.
/// </summary>
public interface IScreenBuffer
{
    /// <summary>
    /// Gets the width of the screen buffer in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the screen buffer in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the cell at the specified position.
    /// Returns Empty cell if coordinates are out of bounds.
    /// </summary>
    /// <param name="row">The row index (0-based)</param>
    /// <param name="col">The column index (0-based)</param>
    /// <returns>The cell at the specified position</returns>
    Cell GetCell(int row, int col);

    /// <summary>
    /// Sets the cell at the specified position.
    /// Ignores the operation if coordinates are out of bounds.
    /// </summary>
    /// <param name="row">The row index (0-based)</param>
    /// <param name="col">The column index (0-based)</param>
    /// <param name="cell">The cell to set</param>
    void SetCell(int row, int col, Cell cell);

    /// <summary>
    /// Clears the entire screen buffer, setting all cells to empty.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clears a specific row, setting all cells in that row to empty.
    /// </summary>
    /// <param name="row">The row index to clear</param>
    void ClearRow(int row);

    /// <summary>
    /// Clears a region of the screen buffer.
    /// </summary>
    /// <param name="startRow">Starting row (inclusive)</param>
    /// <param name="startCol">Starting column (inclusive)</param>
    /// <param name="endRow">Ending row (inclusive)</param>
    /// <param name="endCol">Ending column (inclusive)</param>
    void ClearRegion(int startRow, int startCol, int endRow, int endCol);

    /// <summary>
    /// Gets a read-only span of cells for the specified row.
    /// Returns empty span if row is out of bounds.
    /// </summary>
    /// <param name="row">The row index</param>
    /// <returns>Read-only span of cells in the row</returns>
    ReadOnlySpan<Cell> GetRow(int row);

    /// <summary>
    /// Checks if the specified coordinates are within bounds.
    /// </summary>
    /// <param name="row">The row index</param>
    /// <param name="col">The column index</param>
    /// <returns>True if coordinates are valid, false otherwise</returns>
    bool IsInBounds(int row, int col);
}