using System;

namespace caTTY.Core.Terminal;

/// <summary>
/// Event arguments for screen update notifications.
/// </summary>
public class ScreenUpdatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the region that was updated, or null if the entire screen should be refreshed.
    /// </summary>
    public ScreenRegion? UpdatedRegion { get; }

    /// <summary>
    /// Creates a new ScreenUpdatedEventArgs for a full screen refresh.
    /// </summary>
    public ScreenUpdatedEventArgs()
    {
        UpdatedRegion = null;
    }

    /// <summary>
    /// Creates a new ScreenUpdatedEventArgs for a specific region update.
    /// </summary>
    /// <param name="updatedRegion">The region that was updated</param>
    public ScreenUpdatedEventArgs(ScreenRegion updatedRegion)
    {
        UpdatedRegion = updatedRegion;
    }
}

/// <summary>
/// Event arguments for terminal response emissions.
/// </summary>
public class ResponseEmittedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the response data that should be sent back to the shell.
    /// </summary>
    public ReadOnlyMemory<byte> ResponseData { get; }

    /// <summary>
    /// Creates a new ResponseEmittedEventArgs with the specified response data.
    /// </summary>
    /// <param name="responseData">The response data to send</param>
    public ResponseEmittedEventArgs(ReadOnlyMemory<byte> responseData)
    {
        ResponseData = responseData;
    }

    /// <summary>
    /// Creates a new ResponseEmittedEventArgs with string response data.
    /// </summary>
    /// <param name="responseText">The response text to send (will be converted to UTF-8)</param>
    public ResponseEmittedEventArgs(string responseText)
    {
        ResponseData = System.Text.Encoding.UTF8.GetBytes(responseText);
    }
}

/// <summary>
/// Represents a rectangular region of the screen.
/// </summary>
public readonly struct ScreenRegion
{
    /// <summary>
    /// Gets the starting row (inclusive).
    /// </summary>
    public int StartRow { get; }

    /// <summary>
    /// Gets the starting column (inclusive).
    /// </summary>
    public int StartCol { get; }

    /// <summary>
    /// Gets the ending row (inclusive).
    /// </summary>
    public int EndRow { get; }

    /// <summary>
    /// Gets the ending column (inclusive).
    /// </summary>
    public int EndCol { get; }

    /// <summary>
    /// Creates a new screen region.
    /// </summary>
    /// <param name="startRow">Starting row (inclusive)</param>
    /// <param name="startCol">Starting column (inclusive)</param>
    /// <param name="endRow">Ending row (inclusive)</param>
    /// <param name="endCol">Ending column (inclusive)</param>
    public ScreenRegion(int startRow, int startCol, int endRow, int endCol)
    {
        StartRow = startRow;
        StartCol = startCol;
        EndRow = endRow;
        EndCol = endCol;
    }

    /// <summary>
    /// Creates a screen region for a single cell.
    /// </summary>
    /// <param name="row">The row</param>
    /// <param name="col">The column</param>
    /// <returns>A screen region covering the single cell</returns>
    public static ScreenRegion SingleCell(int row, int col) => new(row, col, row, col);

    /// <summary>
    /// Creates a screen region for an entire row.
    /// </summary>
    /// <param name="row">The row</param>
    /// <param name="width">The width of the screen</param>
    /// <returns>A screen region covering the entire row</returns>
    public static ScreenRegion EntireRow(int row, int width) => new(row, 0, row, width - 1);
}