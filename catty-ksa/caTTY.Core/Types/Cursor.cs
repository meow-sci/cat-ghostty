using System;

namespace caTTY.Core.Types;

/// <summary>
/// Implementation of ICursor for basic cursor position tracking.
/// Includes save/restore functionality and bounds clamping.
/// </summary>
public class Cursor : ICursor
{
    private int _row;
    private int _col;
    private bool _visible;
    private (int Row, int Col)? _savedPosition;

    /// <summary>
    /// Gets the current row position (0-based).
    /// </summary>
    public int Row => _row;

    /// <summary>
    /// Gets the current column position (0-based).
    /// </summary>
    public int Col => _col;

    /// <summary>
    /// Gets whether the cursor is currently visible.
    /// </summary>
    public bool Visible => _visible;

    /// <summary>
    /// Creates a new cursor at position (0, 0) with visibility enabled.
    /// </summary>
    public Cursor()
    {
        _row = 0;
        _col = 0;
        _visible = true;
        _savedPosition = null;
    }

    /// <summary>
    /// Creates a new cursor at the specified position.
    /// </summary>
    /// <param name="row">Initial row position</param>
    /// <param name="col">Initial column position</param>
    /// <param name="visible">Initial visibility state</param>
    public Cursor(int row, int col, bool visible = true)
    {
        _row = Math.Max(0, row);
        _col = Math.Max(0, col);
        _visible = visible;
        _savedPosition = null;
    }

    /// <summary>
    /// Sets the cursor position.
    /// </summary>
    /// <param name="row">The row position (0-based)</param>
    /// <param name="col">The column position (0-based)</param>
    public void SetPosition(int row, int col)
    {
        _row = Math.Max(0, row);
        _col = Math.Max(0, col);
    }

    /// <summary>
    /// Moves the cursor by the specified offset.
    /// </summary>
    /// <param name="deltaRow">Row offset (can be negative)</param>
    /// <param name="deltaCol">Column offset (can be negative)</param>
    public void Move(int deltaRow, int deltaCol)
    {
        _row = Math.Max(0, _row + deltaRow);
        _col = Math.Max(0, _col + deltaCol);
    }

    /// <summary>
    /// Sets the cursor visibility.
    /// </summary>
    /// <param name="visible">True to show cursor, false to hide</param>
    public void SetVisible(bool visible)
    {
        _visible = visible;
    }

    /// <summary>
    /// Clamps the cursor position to stay within the specified bounds.
    /// </summary>
    /// <param name="maxRow">Maximum row (exclusive)</param>
    /// <param name="maxCol">Maximum column (exclusive)</param>
    public void ClampToBounds(int maxRow, int maxCol)
    {
        if (maxRow <= 0 || maxCol <= 0)
            return;

        _row = Math.Max(0, Math.Min(_row, maxRow - 1));
        _col = Math.Max(0, Math.Min(_col, maxCol - 1));
    }

    /// <summary>
    /// Saves the current cursor position for later restoration.
    /// </summary>
    public void Save()
    {
        _savedPosition = (_row, _col);
    }

    /// <summary>
    /// Restores the cursor to the previously saved position.
    /// If no position was saved, moves to (0, 0).
    /// </summary>
    public void Restore()
    {
        if (_savedPosition.HasValue)
        {
            _row = _savedPosition.Value.Row;
            _col = _savedPosition.Value.Col;
        }
        else
        {
            _row = 0;
            _col = 0;
        }
    }

    /// <summary>
    /// Returns a string representation of the cursor state.
    /// </summary>
    public override string ToString()
    {
        return $"Cursor(Row={_row}, Col={_col}, Visible={_visible})";
    }
}