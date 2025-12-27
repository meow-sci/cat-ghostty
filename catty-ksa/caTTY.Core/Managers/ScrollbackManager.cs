namespace caTTY.Core.Managers;

using caTTY.Core.Types;
using System.Buffers;

/// <summary>
///     Manages scrollback buffer operations and viewport management.
///     Uses a circular buffer for efficient memory usage and line reuse.
/// </summary>
public class ScrollbackManager : IScrollbackManager, IDisposable
{
    private readonly int _maxLines;
    private readonly int _columns;
    private readonly ArrayPool<Cell> _cellPool;
    
    // Circular buffer for scrollback lines
    private Cell[][] _lines;
    private int _startIndex; // Index of the oldest line
    private int _currentLines; // Number of lines currently stored
    private int _viewportOffset; // Offset from bottom (0 = bottom, positive = scroll up)

    /// <summary>
    ///     Creates a new scrollback manager with the specified capacity.
    /// </summary>
    /// <param name="maxLines">Maximum number of lines to store</param>
    /// <param name="columns">Number of columns per line</param>
    public ScrollbackManager(int maxLines, int columns)
    {
        if (maxLines < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLines), "Max lines cannot be negative");
        if (columns <= 0)
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be positive");

        _maxLines = maxLines;
        _columns = columns;
        _cellPool = ArrayPool<Cell>.Shared;
        _lines = new Cell[maxLines][];
        _startIndex = 0;
        _currentLines = 0;
        _viewportOffset = 0;
    }

    /// <inheritdoc />
    public int MaxLines => _maxLines;

    /// <inheritdoc />
    public int CurrentLines => _currentLines;

    /// <inheritdoc />
    public int ViewportOffset
    {
        get => _viewportOffset;
        set => SetViewportOffset(value);
    }

    /// <inheritdoc />
    public bool IsAtBottom => _viewportOffset == 0;

    /// <inheritdoc />
    public void AddLine(ReadOnlySpan<Cell> line)
    {
        if (_maxLines <= 0)
            return; // No scrollback configured

        // Get or create array for this line
        Cell[] lineArray;
        if (_currentLines < _maxLines)
        {
            // Still have space, allocate new array
            lineArray = _cellPool.Rent(_columns);
            var insertIndex = (_startIndex + _currentLines) % _maxLines;
            _lines[insertIndex] = lineArray;
            _currentLines++;
        }
        else
        {
            // Buffer is full, reuse the oldest line's array
            lineArray = _lines[_startIndex];
            _startIndex = (_startIndex + 1) % _maxLines;
        }

        // Copy line data, ensuring it's exactly _columns length
        var copyLength = Math.Min(line.Length, _columns);
        line.Slice(0, copyLength).CopyTo(lineArray);
        
        // Fill remaining cells with spaces if line is shorter than columns
        for (int i = copyLength; i < _columns; i++)
        {
            lineArray[i] = Cell.Space;
        }
    }

    /// <inheritdoc />
    public ReadOnlySpan<Cell> GetLine(int index)
    {
        if (index < 0 || index >= _currentLines)
            throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range [0, {_currentLines})");

        var actualIndex = (_startIndex + index) % _maxLines;
        return _lines[actualIndex].AsSpan(0, _columns);
    }

    /// <inheritdoc />
    public void Clear()
    {
        // Return all arrays to the pool
        for (int i = 0; i < _currentLines; i++)
        {
            var actualIndex = (_startIndex + i) % _maxLines;
            if (_lines[actualIndex] != null)
            {
                _cellPool.Return(_lines[actualIndex]);
                _lines[actualIndex] = null!;
            }
        }

        _startIndex = 0;
        _currentLines = 0;
        _viewportOffset = 0;
    }

    /// <inheritdoc />
    public void SetViewportOffset(int offset)
    {
        // Clamp offset to valid range [0, CurrentLines]
        _viewportOffset = Math.Max(0, Math.Min(offset, _currentLines));
    }

    /// <inheritdoc />
    public List<ReadOnlyMemory<Cell>> GetViewportRows(ReadOnlyMemory<Cell>[] screenBuffer, bool isAlternateScreenActive, int requestedRows)
    {
        if (requestedRows <= 0)
            return new List<ReadOnlyMemory<Cell>>();

        // In alternate screen mode, don't show scrollback (matches TypeScript behavior)
        if (isAlternateScreenActive)
        {
            return screenBuffer.Take(requestedRows).ToList();
        }

        var result = new List<ReadOnlyMemory<Cell>>(requestedRows);
        var scrollbackRows = _currentLines;
        var viewportTop = Math.Max(0, Math.Min(_viewportOffset, scrollbackRows));

        for (int i = 0; i < requestedRows; i++)
        {
            var globalRow = viewportTop + i;
            
            if (globalRow < scrollbackRows)
            {
                // Show scrollback content
                var line = GetLine(globalRow);
                var lineArray = new Cell[line.Length];
                line.CopyTo(lineArray);
                result.Add(lineArray.AsMemory());
            }
            else
            {
                // Show screen buffer content
                var screenRow = globalRow - scrollbackRows;
                if (screenRow < screenBuffer.Length)
                {
                    result.Add(screenBuffer[screenRow]);
                }
                else
                {
                    // Fill with empty cells if we run out of content
                    var emptyLine = new Cell[_columns];
                    for (int j = 0; j < _columns; j++)
                    {
                        emptyLine[j] = Cell.Space;
                    }
                    result.Add(emptyLine.AsMemory());
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     Disposes resources used by the scrollback manager.
    /// </summary>
    public void Dispose()
    {
        Clear();
    }
}