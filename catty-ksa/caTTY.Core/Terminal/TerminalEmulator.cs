using System.Text;
using caTTY.Core.Parsing;
using caTTY.Core.Types;
using caTTY.Core.Managers;
using caTTY.Core.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace caTTY.Core.Terminal;

/// <summary>
///     Core terminal emulator implementation that processes raw byte data and maintains screen state.
///     This is a headless implementation with no UI dependencies.
/// </summary>
public class TerminalEmulator : ITerminalEmulator, ICursorPositionProvider
{
    private readonly ILogger _logger;
    private readonly Parser _parser;
    private readonly IScreenBufferManager _screenBufferManager;
    private readonly ICursorManager _cursorManager;
    private readonly IModeManager _modeManager;
    private readonly IAttributeManager _attributeManager;
    private readonly IScrollbackManager _scrollbackManager;
    private readonly IScrollbackBuffer _scrollbackBuffer;
    private readonly IAlternateScreenManager _alternateScreenManager;
    private readonly ICharacterSetManager _characterSetManager;
    private bool _disposed;

    /// <summary>
    ///     Creates a new terminal emulator with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <param name="logger">Optional logger for debugging (uses NullLogger if not provided)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public TerminalEmulator(int width, int height, ILogger? logger = null) : this(width, height, 1000, logger)
    {
    }

    /// <summary>
    ///     Creates a new terminal emulator with the specified dimensions and scrollback.
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <param name="scrollbackLines">Maximum number of scrollback lines (default: 1000)</param>
    /// <param name="logger">Optional logger for debugging (uses NullLogger if not provided)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public TerminalEmulator(int width, int height, int scrollbackLines, ILogger? logger = null)
    {
        if (width < 1 || width > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 1 and 1000");
        }

        if (height < 1 || height > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be between 1 and 1000");
        }

        if (scrollbackLines < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scrollbackLines), "Scrollback lines cannot be negative");
        }

        Cursor = new Cursor();
        State = new TerminalState(width, height);

        // Use a dual buffer so alternate-screen applications (htop/vim/less/etc.)
        // don't corrupt primary screen content and scrollback behavior.
        ScreenBuffer = new DualScreenBuffer(width, height, () => State.IsAlternateScreenActive);
        _logger = logger ?? NullLogger.Instance;

        // Initialize scrollback infrastructure
        _scrollbackBuffer = new ScrollbackBuffer(scrollbackLines, width);
        _scrollbackManager = new ScrollbackManager(scrollbackLines, width);

        // Initialize managers
        _screenBufferManager = new ScreenBufferManager(ScreenBuffer);
        _cursorManager = new CursorManager(Cursor);
        _modeManager = new ModeManager();
        _attributeManager = new AttributeManager();
        _characterSetManager = new CharacterSetManager(State);
        _alternateScreenManager = new AlternateScreenManager(State, _cursorManager, (DualScreenBuffer)ScreenBuffer);

        // Set up scrollback integration
        _screenBufferManager.SetScrollbackIntegration(
            row => _scrollbackManager.AddLine(row),
            () => State.IsAlternateScreenActive
        );

        // Initialize parser with terminal handlers
        var handlers = new TerminalParserHandlers(this, _logger);
        var parserOptions = new ParserOptions
        {
            Handlers = handlers,
            Logger = _logger,
            EmitNormalBytesDuringEscapeSequence = false,
            ProcessC0ControlsDuringEscapeSequence = true,
            CursorPositionProvider = this
        };
        _parser = new Parser(parserOptions);

        _disposed = false;
    }

    /// <summary>
    ///     Gets the current terminal state.
    /// </summary>
    public TerminalState State { get; }

    /// <summary>
    ///     Gets the screen buffer manager for buffer operations.
    /// </summary>
    public IScreenBufferManager ScreenBufferManager => _screenBufferManager;

    /// <summary>
    ///     Gets the cursor manager for cursor operations.
    /// </summary>
    public ICursorManager CursorManager => _cursorManager;

    /// <summary>
    ///     Gets the mode manager for terminal mode operations.
    /// </summary>
    public IModeManager ModeManager => _modeManager;

    /// <summary>
    ///     Gets the attribute manager for SGR attribute operations.
    /// </summary>
    public IAttributeManager AttributeManager => _attributeManager;

    /// <summary>
    ///     Gets the alternate screen manager for buffer switching operations.
    /// </summary>
    public IAlternateScreenManager AlternateScreenManager => _alternateScreenManager;

    /// <summary>
    ///     Gets the width of the terminal in columns.
    /// </summary>
    public int Width => ScreenBuffer.Width;

    /// <summary>
    ///     Gets the height of the terminal in rows.
    /// </summary>
    public int Height => ScreenBuffer.Height;

    /// <summary>
    ///     Gets the current screen buffer for rendering.
    /// </summary>
    public IScreenBuffer ScreenBuffer { get; }

    /// <summary>
    ///     Gets the current cursor state.
    /// </summary>
    public ICursor Cursor { get; }

    /// <summary>
    /// Gets the current cursor row position (0-based) for tracing purposes.
    /// </summary>
    public int Row => _cursorManager.Row;

    /// <summary>
    /// Gets the current cursor column position (0-based) for tracing purposes.
    /// </summary>
    public int Column => _cursorManager.Column;

    /// <summary>
    ///     Gets the scrollback buffer for accessing historical lines.
    /// </summary>
    public IScrollbackBuffer ScrollbackBuffer => _scrollbackBuffer;

    /// <summary>
    ///     Gets the scrollback manager for viewport and scrollback operations.
    /// </summary>
    public IScrollbackManager ScrollbackManager => _scrollbackManager;

    /// <summary>
    ///     Event raised when the screen content has been updated and needs refresh.
    /// </summary>
    public event EventHandler<ScreenUpdatedEventArgs>? ScreenUpdated;

    /// <summary>
    ///     Event raised when the terminal needs to send a response back to the shell.
    /// </summary>
    public event EventHandler<ResponseEmittedEventArgs>? ResponseEmitted;

    /// <summary>
    ///     Event raised when a bell character (BEL, 0x07) is received.
    /// </summary>
    public event EventHandler<BellEventArgs>? Bell;

    /// <summary>
    ///     Event raised when the window title is changed via OSC sequences.
    /// </summary>
    public event EventHandler<TitleChangeEventArgs>? TitleChanged;

    /// <summary>
    ///     Event raised when the icon name is changed via OSC sequences.
    /// </summary>
    public event EventHandler<IconNameChangeEventArgs>? IconNameChanged;

    /// <summary>
    ///     Event raised when a clipboard operation is requested via OSC 52 sequences.
    /// </summary>
    public event EventHandler<ClipboardEventArgs>? ClipboardRequest;

    /// <summary>
    ///     Processes raw byte data from a shell or other source.
    ///     Can be called with partial chunks and in rapid succession.
    /// </summary>
    /// <param name="data">The raw byte data to process</param>
    public void Write(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();

        if (data.IsEmpty)
        {
            return;
        }

        // Use parser for proper UTF-8 decoding and escape sequence handling
        _parser.PushBytes(data);

        // Notify that the screen has been updated
        OnScreenUpdated();
    }

    /// <summary>
    ///     Processes string data by converting to UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text to process</param>
    public void Write(string text)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Convert string to UTF-8 bytes and process
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        Write(bytes.AsSpan());
    }

    /// <summary>
    ///     Resizes the terminal to the specified dimensions.
    ///     Preserves cursor position and updates scrollback during resize operations.
    ///     Uses simple resize policy: height change preserves top-to-bottom rows,
    ///     width change truncates/pads each row without complex reflow.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    public void Resize(int width, int height)
    {
        ThrowIfDisposed();

        if (width < 1 || width > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Width must be between 1 and 1000");
        }

        if (height < 1 || height > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Height must be between 1 and 1000");
        }

        // If dimensions are the same, no work needed
        if (width == Width && height == Height)
        {
            return;
        }

        // Save current cursor position for preservation
        int oldCursorX = _cursorManager.Column;
        int oldCursorY = _cursorManager.Row;
        int oldWidth = Width;
        int oldHeight = Height;

        // Handle scrollback updates during resize
        // If height is decreasing and cursor is below the new height,
        // we need to push the excess rows to scrollback
        if (height < oldHeight && oldCursorY >= height)
        {
            // Calculate how many rows need to be moved to scrollback
            int excessRows = oldHeight - height;
            
            // Push the top rows to scrollback to preserve content
            for (int row = 0; row < excessRows; row++)
            {
                var rowSpan = _screenBufferManager.GetRow(row);
                if (!rowSpan.IsEmpty)
                {
                    _scrollbackManager.AddLine(rowSpan);
                }
            }
        }

        // Resize the screen buffer (this preserves content according to the simple policy)
        _screenBufferManager.Resize(width, height);

        // Update terminal state dimensions
        State.Resize(width, height);

        // Preserve cursor position with intelligent clamping
        int newCursorX = Math.Min(oldCursorX, width - 1);
        int newCursorY;

        if (height < oldHeight)
        {
            // Height decreased - adjust cursor position
            int rowsLost = oldHeight - height;
            newCursorY = Math.Max(0, oldCursorY - rowsLost);
        }
        else
        {
            // Height increased or same - keep cursor position
            newCursorY = Math.Min(oldCursorY, height - 1);
        }

        // Update cursor position
        _cursorManager.MoveTo(newCursorY, newCursorX);

        // Update scroll region to match new dimensions if it was full-screen
        if (State.ScrollTop == 0 && State.ScrollBottom == oldHeight - 1)
        {
            State.ScrollTop = 0;
            State.ScrollBottom = height - 1;
        }
        else
        {
            // Clamp existing scroll region to new dimensions
            State.ScrollTop = Math.Min(State.ScrollTop, height - 1);
            State.ScrollBottom = Math.Min(State.ScrollBottom, height - 1);
            
            // Ensure scroll region is still valid
            if (State.ScrollTop >= State.ScrollBottom)
            {
                State.ScrollTop = 0;
                State.ScrollBottom = height - 1;
            }
        }

        // Update tab stops array to match new width
        State.ResizeTabStops(width);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;

        // Notify that the screen has been updated
        OnScreenUpdated();
    }

    /// <summary>
    ///     Disposes the terminal emulator and releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _scrollbackBuffer?.Dispose();
            (_scrollbackManager as IDisposable)?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    ///     Scrolls the viewport up by the specified number of lines.
    ///     Disables auto-scroll if not already at the top.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up</param>
    public void ScrollViewportUp(int lines)
    {
        ThrowIfDisposed();
        _scrollbackManager.ScrollUp(lines);
        OnScreenUpdated();
    }

    /// <summary>
    ///     Scrolls the viewport down by the specified number of lines.
    ///     Re-enables auto-scroll if scrolled to the bottom.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    public void ScrollViewportDown(int lines)
    {
        ThrowIfDisposed();
        _scrollbackManager.ScrollDown(lines);
        OnScreenUpdated();
    }

    /// <summary>
    ///     Scrolls to the top of the scrollback buffer.
    ///     Disables auto-scroll.
    /// </summary>
    public void ScrollViewportToTop()
    {
        ThrowIfDisposed();
        _scrollbackManager.ScrollToTop();
        OnScreenUpdated();
    }

    /// <summary>
    ///     Scrolls to the bottom of the scrollback buffer.
    ///     Re-enables auto-scroll.
    /// </summary>
    public void ScrollViewportToBottom()
    {
        ThrowIfDisposed();
        _scrollbackManager.ScrollToBottom();
        OnScreenUpdated();
    }

    /// <summary>
    ///     Gets whether auto-scroll is currently enabled.
    ///     Auto-scroll is disabled when user scrolls up and re-enabled when they return to bottom.
    /// </summary>
    public bool IsAutoScrollEnabled => _scrollbackManager.AutoScrollEnabled;

    /// <summary>
    ///     Gets the current viewport offset from the bottom.
    ///     0 means viewing the most recent content, positive values mean scrolled up into history.
    /// </summary>
    public int ViewportOffset => _scrollbackManager.ViewportOffset;

    /// <summary>
    ///     Flushes any incomplete UTF-8 sequences in the parser.
    ///     This should be called when no more input is expected to ensure
    ///     incomplete sequences are handled gracefully.
    /// </summary>
    public void FlushIncompleteSequences()
    {
        ThrowIfDisposed();
        _parser.FlushIncompleteSequences();
        OnScreenUpdated();
    }

    /// <summary>
    ///     Handles a line feed (LF) character - move down one line, keeping same column.
    ///     In raw terminal mode, LF only moves down without changing column position.
    ///     Uses cursor manager for proper cursor management.
    /// </summary>
    internal void HandleLineFeed()
    {
        // Clear wrap pending and move cursor down
        _cursorManager.SetWrapPending(false);
        
        // Move cursor down one line
        if (_cursorManager.Row + 1 > State.ScrollBottom)
        {
            // At bottom of scroll region - need to scroll up by one line within the region
            _screenBufferManager.ScrollUpInRegion(1, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);
            _cursorManager.MoveTo(State.ScrollBottom, _cursorManager.Column);
        }
        else
        {
            _cursorManager.MoveTo(_cursorManager.Row + 1, _cursorManager.Column);
        }

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Handles index operation (ESC D) - move cursor down one line without changing column.
    ///     Used by ESC D sequence.
    /// </summary>
    internal void HandleIndex()
    {
        if (Cursor.Row < Height - 1)
        {
            // Move cursor down one row, keep same column
            Cursor.SetPosition(Cursor.Row + 1, Cursor.Col);
        }
        else
        {
            // At bottom row - need to scroll up by one line
            _screenBufferManager.ScrollUpInRegion(1, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);
            // Cursor stays at the bottom row
        }

        // Sync state with cursor
        State.CursorX = Cursor.Col;
        State.CursorY = Cursor.Row;
        State.WrapPending = false;
    }

    /// <summary>
    ///     Handles a carriage return (CR) character - move to column 0.
    ///     Uses cursor manager for proper cursor management.
    /// </summary>
    internal void HandleCarriageReturn()
    {
        _cursorManager.MoveTo(_cursorManager.Row, 0);
        _cursorManager.SetWrapPending(false);
        
        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Handles a bell character (BEL) - emit bell event for notification.
    /// </summary>
    internal void HandleBell()
    {
        OnBell();
    }

    /// <summary>
    ///     Sets the window title and emits a title change event.
    ///     Handles empty titles and title reset.
    /// </summary>
    /// <param name="title">The new window title</param>
    internal void SetWindowTitle(string title)
    {
        title ??= string.Empty;
        State.WindowProperties.Title = title;
        OnTitleChanged(title);
    }

    /// <summary>
    ///     Sets the icon name and emits an icon name change event.
    ///     Handles empty icon names and icon name reset.
    /// </summary>
    /// <param name="iconName">The new icon name</param>
    internal void SetIconName(string iconName)
    {
        iconName ??= string.Empty;
        State.WindowProperties.IconName = iconName;
        OnIconNameChanged(iconName);
    }

    /// <summary>
    ///     Sets both window title and icon name to the same value.
    ///     Emits both title change and icon name change events.
    /// </summary>
    /// <param name="title">The new title and icon name</param>
    internal void SetTitleAndIcon(string title)
    {
        title ??= string.Empty;
        State.WindowProperties.Title = title;
        State.WindowProperties.IconName = title;
        OnTitleChanged(title);
        OnIconNameChanged(title);
    }

    /// <summary>
    ///     Gets the current window title.
    /// </summary>
    /// <returns>The current window title</returns>
    internal string GetWindowTitle()
    {
        return State.WindowProperties.Title;
    }

    /// <summary>
    ///     Gets the current icon name.
    /// </summary>
    /// <returns>The current icon name</returns>
    internal string GetIconName()
    {
        return State.WindowProperties.IconName;
    }

    /// <summary>
    ///     Handles clipboard operations from OSC 52 sequences.
    ///     Parses selection targets and clipboard data, applies safety limits,
    ///     and emits clipboard events for game integration.
    /// </summary>
    /// <param name="payload">The OSC 52 payload (selection;data)</param>
    internal void HandleClipboard(string payload)
    {
        if (string.IsNullOrEmpty(payload))
        {
            return;
        }

        // OSC 52 format: ESC ] 52 ; selection ; data BEL/ST
        // where selection can be: c (clipboard), p (primary), s (secondary), 0-7 (cut buffers)
        // and data can be: base64 encoded data, ? (query), or empty (clear)

        // Parse selection target and data
        string[] parts = payload.Split(';', 3); // Limit to 3 parts to handle data with semicolons
        if (parts.Length < 2)
        {
            _logger.LogWarning("Invalid OSC 52 format: missing selection or data part");
            return;
        }

        string selectionTarget = parts[1];
        string? data = parts.Length > 2 ? parts[2] : null;

        // Validate selection target
        if (string.IsNullOrEmpty(selectionTarget))
        {
            _logger.LogWarning("Invalid OSC 52: empty selection target");
            return;
        }

        // Handle clipboard query
        if (data == "?")
        {
            OnClipboardRequest(selectionTarget, null, isQuery: true);
            _logger.LogDebug("Clipboard query for selection: {Selection}", selectionTarget);
            return;
        }

        // Handle clipboard clear
        if (string.IsNullOrEmpty(data))
        {
            OnClipboardRequest(selectionTarget, string.Empty, isQuery: false);
            _logger.LogDebug("Clipboard clear for selection: {Selection}", selectionTarget);
            return;
        }

        // Handle clipboard data - decode from base64
        try
        {
            // Apply safety limit: cap base64 data length before decoding
            const int MaxBase64Length = 4096; // ~3KB decoded data
            if (data.Length > MaxBase64Length)
            {
                _logger.LogWarning("OSC 52 base64 data too long ({Length} > {Max}), ignoring", 
                    data.Length, MaxBase64Length);
                return;
            }

            // Decode base64 data
            byte[] decodedBytes = Convert.FromBase64String(data);
            
            // Apply safety limit: cap decoded data size
            const int MaxDecodedSize = 2048; // 2KB max decoded size
            if (decodedBytes.Length > MaxDecodedSize)
            {
                _logger.LogWarning("OSC 52 decoded data too large ({Size} > {Max}), ignoring", 
                    decodedBytes.Length, MaxDecodedSize);
                return;
            }

            // Convert to UTF-8 string
            string decodedText = System.Text.Encoding.UTF8.GetString(decodedBytes);
            
            // Emit clipboard event
            OnClipboardRequest(selectionTarget, decodedText, isQuery: false);
            _logger.LogDebug("Clipboard data for selection {Selection}: {Length} bytes", 
                selectionTarget, decodedBytes.Length);
        }
        catch (FormatException)
        {
            // Invalid base64 - ignore gracefully
            _logger.LogWarning("OSC 52 invalid base64 data, ignoring gracefully");
        }
        catch (Exception ex)
        {
            // Other decoding errors - ignore gracefully
            _logger.LogWarning(ex, "OSC 52 clipboard decoding error, ignoring gracefully");
        }
    }

    /// <summary>
    ///     Handles hyperlink operations from OSC 8 sequences.
    ///     Associates URLs with character ranges by setting current hyperlink state.
    ///     Clears hyperlink state when empty URL is provided.
    /// </summary>
    /// <param name="url">The hyperlink URL, or empty string to clear hyperlink state</param>
    internal void HandleHyperlink(string url)
    {
        // OSC 8 format: ESC ] 8 ; [params] ; [url] BEL/ST
        // where params can include id=<id> and other key=value pairs
        // For now, we only handle the URL part and ignore parameters
        
        if (string.IsNullOrEmpty(url))
        {
            // Clear hyperlink state - OSC 8 ;; ST
            _attributeManager.SetHyperlinkUrl(null);
            State.CurrentHyperlinkUrl = null;
            _logger.LogDebug("Cleared hyperlink state");
        }
        else
        {
            // Set hyperlink URL for subsequent characters
            _attributeManager.SetHyperlinkUrl(url);
            State.CurrentHyperlinkUrl = url;
            _logger.LogDebug("Set hyperlink URL: {Url}", url);
        }
    }

    /// <summary>
    ///     Handles a backspace character (BS) - move cursor one position left if not at column 0.
    ///     Uses cursor manager for proper cursor management.
    /// </summary>
    internal void HandleBackspace()
    {
        _cursorManager.SetWrapPending(false);
        
        if (_cursorManager.Column > 0)
        {
            _cursorManager.MoveLeft(1);
        }

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Handles a tab character - move to next tab stop using terminal state.
    /// </summary>
    internal void HandleTab()
    {
        // Sync state with cursor
        State.CursorX = Cursor.Col;
        State.CursorY = Cursor.Row;

        // Clear wrap pending state since we're moving the cursor
        State.WrapPending = false;

        // Find next tab stop
        int nextTabStop = -1;
        for (int col = State.CursorX + 1; col < Width; col++)
        {
            if (col < State.TabStops.Length && State.TabStops[col])
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
        State.CursorX = nextTabStop;

        // Handle wrap pending if we're at the right edge and auto-wrap is enabled
        if (State.CursorX >= Width - 1 && State.AutoWrapMode)
        {
            State.WrapPending = true;
        }

        // Update cursor to match state
        Cursor.SetPosition(State.CursorY, State.CursorX);
    }

    /// <summary>
    ///     Writes a character at the current cursor position and advances the cursor.
    ///     Implements proper auto-wrap behavior matching TypeScript reference implementation.
    /// </summary>
    /// <param name="character">The character to write</param>
    internal void WriteCharacterAtCursor(char character)
    {
        // Bounds check - ensure we have valid dimensions
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        // Bounds check - ensure cursor Y is within screen bounds
        if (_cursorManager.Row < 0 || _cursorManager.Row >= Height)
        {
            return;
        }

        // Clamp cursor X to valid range
        if (_cursorManager.Column < 0)
        {
            _cursorManager.MoveTo(_cursorManager.Row, 0);
        }

        // Handle wrap pending state - if set, wrap to next line first
        // This matches TypeScript putChar behavior: wrap pending triggers on next character
        if (_modeManager.AutoWrapMode && _cursorManager.WrapPending)
        {
            _cursorManager.MoveTo(_cursorManager.Row, 0);
            
            // Move to next line
            if (_cursorManager.Row + 1 >= Height)
            {
                // At bottom - need to scroll up by one line within scroll region
                _screenBufferManager.ScrollUpInRegion(1, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);
                _cursorManager.MoveTo(Height - 1, 0);
            }
            else
            {
                _cursorManager.MoveTo(_cursorManager.Row + 1, 0);
            }
            
            _cursorManager.SetWrapPending(false);
        }

        // Clamp cursor X to screen bounds (best-effort recovery)
        if (_cursorManager.Column >= Width)
        {
            _cursorManager.MoveTo(_cursorManager.Row, Width - 1);
        }

        // Write the character to the screen buffer with current SGR attributes, protection status, and hyperlink URL
        // Determine if this is a wide character for proper cell marking
        bool isWide = IsWideCharacter(character);
        var cell = new Cell(character, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection, _attributeManager.CurrentHyperlinkUrl, isWide);
        _screenBufferManager.SetCell(_cursorManager.Row, _cursorManager.Column, cell);

        // Trace the printable character with position information
        TerminalTracer.TracePrintable(character.ToString(), TraceDirection.Output, _cursorManager.Row, _cursorManager.Column);

        // Handle cursor advancement and wrap pending logic
        if (_cursorManager.Column == Width - 1)
        {
            // At right edge - set wrap pending if auto-wrap is enabled
            if (_modeManager.AutoWrapMode)
            {
                _cursorManager.SetWrapPending(true);
            }
            // Cursor stays at right edge (don't advance beyond)
        }
        else
        {
            // Normal advancement - move cursor right
            // For wide characters, advance by 2 if there's space, otherwise treat as normal
            int advanceAmount = 1;
            if (isWide && _cursorManager.Column + 1 < Width - 1)
            {
                // Wide character with room for 2 cells - advance by 2
                advanceAmount = 2;
                // Note: We don't overwrite the next cell, just advance the cursor
                // The rendering system should handle wide character display
            }
            
            _cursorManager.MoveTo(_cursorManager.Row, _cursorManager.Column + advanceAmount);
        }

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        State.AutoWrapMode = _modeManager.AutoWrapMode;
    }

    /// <summary>
    ///     Determines if a character is wide (occupies two terminal cells).
    ///     Based on Unicode East Asian Width property for CJK characters.
    /// </summary>
    /// <param name="character">The character to check</param>
    /// <returns>True if the character is wide, false otherwise</returns>
    private static bool IsWideCharacter(char character)
    {
        // For now, disable wide character detection to maintain compatibility with existing tests
        // The existing tests expect CJK characters to be treated as single-width
        // TODO: Implement proper wide character handling based on Unicode East Asian Width property
        return false;
    }

    /// <summary>
    ///     Emits a response string back to the shell process.
    ///     Used for device queries and other terminal responses.
    /// </summary>
    /// <param name="responseText">The response text to emit</param>
    internal void EmitResponse(string responseText)
    {
        OnResponseEmitted(responseText);
    }

    /// <summary>
    ///     Raises the ScreenUpdated event.
    /// </summary>
    private void OnScreenUpdated()
    {
        ScreenUpdated?.Invoke(this, new ScreenUpdatedEventArgs());
    }

    /// <summary>
    ///     Raises the ResponseEmitted event.
    /// </summary>
    /// <param name="responseData">The response data to emit</param>
    protected void OnResponseEmitted(ReadOnlyMemory<byte> responseData)
    {
        ResponseEmitted?.Invoke(this, new ResponseEmittedEventArgs(responseData));
    }

    /// <summary>
    ///     Raises the ResponseEmitted event with string data.
    /// </summary>
    /// <param name="responseText">The response text to emit</param>
    protected void OnResponseEmitted(string responseText)
    {
        ResponseEmitted?.Invoke(this, new ResponseEmittedEventArgs(responseText));
    }

    /// <summary>
    ///     Handles scroll up sequence (CSI S) - scroll screen up by specified lines.
    ///     Implements CSI Ps S (Scroll Up) sequence.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up (default: 1)</param>
    internal void ScrollScreenUp(int lines = 1)
    {
        if (lines <= 0)
        {
            return; // Do nothing for zero or negative lines
        }
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for proper scrollback integration
        _screenBufferManager.ScrollUpInRegion(lines, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Handles scroll down sequence (CSI T) - scroll screen down by specified lines.
    ///     Implements CSI Ps T (Scroll Down) sequence.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down (default: 1)</param>
    internal void ScrollScreenDown(int lines = 1)
    {
        if (lines <= 0)
        {
            return; // Do nothing for zero or negative lines
        }
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for proper scrollback integration
        _screenBufferManager.ScrollDownInRegion(lines, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Sets the scroll region (DECSTBM - Set Top and Bottom Margins).
    ///     Implements CSI Ps ; Ps r sequence.
    /// </summary>
    /// <param name="top">Top boundary (1-indexed, null for default)</param>
    /// <param name="bottom">Bottom boundary (1-indexed, null for default)</param>
    internal void SetScrollRegion(int? top, int? bottom)
    {
        // DECSTBM - Set Top and Bottom Margins
        if (top == null && bottom == null)
        {
            // Reset to full screen
            State.ScrollTop = 0;
            State.ScrollBottom = Height - 1;
        }
        else
        {
            // Convert from 1-indexed to 0-indexed and validate bounds
            int newTop = top.HasValue ? Math.Max(0, Math.Min(Height - 1, top.Value - 1)) : 0;
            int newBottom = bottom.HasValue ? Math.Max(0, Math.Min(Height - 1, bottom.Value - 1)) : Height - 1;

            // Ensure top < bottom
            if (newTop < newBottom)
            {
                State.ScrollTop = newTop;
                State.ScrollBottom = newBottom;
            }
        }

        // Move cursor to home position within scroll region (following TypeScript behavior)
        _cursorManager.MoveTo(State.ScrollTop, 0);
        _cursorManager.SetWrapPending(false);

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Sets a DEC private mode.
    /// </summary>
    /// <param name="mode">The DEC mode number</param>
    /// <param name="enabled">True to enable, false to disable</param>
    internal void SetDecMode(int mode, bool enabled)
    {
        // Update the mode manager first
        _modeManager.SetPrivateMode(mode, enabled);
        
        switch (mode)
        {
            case 6: // DECOM - Origin Mode
                State.SetOriginMode(enabled);
                // Sync with cursor manager after origin mode change
                _cursorManager.MoveTo(State.CursorY, State.CursorX);
                _cursorManager.SetWrapPending(State.WrapPending);
                break;

            case 7: // DECAWM - Auto Wrap Mode
                State.SetAutoWrapMode(enabled);
                // Clear wrap pending when auto-wrap mode is disabled (matches TypeScript)
                if (!enabled)
                {
                    _cursorManager.SetWrapPending(false);
                }
                // Sync with cursor manager after auto wrap mode change
                _cursorManager.SetWrapPending(State.WrapPending);
                break;

            case 25: // DECTCEM - Text Cursor Enable Mode
                State.CursorVisible = enabled;
                _cursorManager.Visible = enabled;
                break;

            case 1: // DECCKM - Application Cursor Keys
                State.ApplicationCursorKeys = enabled;
                break;

            case 47: // Alternate Screen Buffer
            case 1047: // Alternate Screen Buffer with cursor save
            case 1049: // Alternate Screen Buffer with cursor save and clear
                HandleAlternateScreenMode(mode, enabled);
                break;

            case 1000: // VT200 mouse tracking (click)
            case 1002: // button-event tracking (drag)
            case 1003: // any-event tracking (motion)
                State.SetMouseTrackingMode(mode, enabled);
                break;

            case 1006: // SGR mouse encoding
                State.MouseSgrEncodingEnabled = enabled;
                break;

            case 2004: // Bracketed paste mode
                State.BracketedPasteMode = enabled;
                break;

            case 2027: // UTF-8 Mode
                State.Utf8Mode = enabled;
                _characterSetManager.SetUtf8Mode(enabled);
                break;

            default:
                _logger.LogDebug("Unknown DEC mode {Mode} {Action}", mode, enabled ? "set" : "reset");
                break;
        }
    }

    private void HandleAlternateScreenMode(int mode, bool enabled)
    {
        if (enabled)
        {
            switch (mode)
            {
                case 47: // Basic alternate screen
                    _alternateScreenManager.ActivateAlternate();
                    break;
                case 1047: // Alternate screen with cursor save
                    _alternateScreenManager.ActivateAlternateWithCursorSave();
                    break;
                case 1049: // Alternate screen with cursor save and clear
                    _alternateScreenManager.ActivateAlternateWithClearAndCursorSave();
                    break;
            }
        }
        else
        {
            // Store whether we were in alternate screen before deactivation
            bool wasAlternate = State.IsAlternateScreenActive;
            
            switch (mode)
            {
                case 47: // Basic alternate screen
                    _alternateScreenManager.DeactivateAlternate();
                    break;
                case 1047: // Alternate screen with cursor restore
                case 1049: // Alternate screen with cursor restore
                    _alternateScreenManager.DeactivateAlternateWithCursorRestore();
                    break;
            }
            
            // Leaving a full-screen TUI should restore the prompt/cursor at the bottom
            // (matches catty-web controller behavior).
            if (wasAlternate)
            {
                _scrollbackManager.ScrollToBottom();
            }
        }
        
        // Sync cursor manager with terminal state after buffer switching
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Moves the cursor up by the specified number of lines.
    /// </summary>
    /// <param name="count">Number of lines to move up (minimum 1)</param>
    internal void MoveCursorUp(int count)
    {
        count = Math.Max(1, count);
        _cursorManager.MoveUp(count);
        
        // Sync cursor manager position to terminal state
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Use terminal state clamping to respect scroll regions and origin mode
        State.ClampCursor();
        
        // Sync back to cursor manager after clamping
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.SetWrapPending(State.WrapPending);
    }

    /// <summary>
    ///     Moves the cursor down by the specified number of lines.
    /// </summary>
    /// <param name="count">Number of lines to move down (minimum 1)</param>
    internal void MoveCursorDown(int count)
    {
        count = Math.Max(1, count);
        _cursorManager.MoveDown(count);
        
        // Sync cursor manager position to terminal state
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Use terminal state clamping to respect scroll regions and origin mode
        State.ClampCursor();
        
        // Sync back to cursor manager after clamping
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.SetWrapPending(State.WrapPending);
    }

    /// <summary>
    ///     Moves the cursor forward (right) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move forward (minimum 1)</param>
    internal void MoveCursorForward(int count)
    {
        count = Math.Max(1, count);
        _cursorManager.MoveRight(count);
        
        // Sync cursor manager position to terminal state
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Use terminal state clamping to respect scroll regions and origin mode
        State.ClampCursor();
        
        // Sync back to cursor manager after clamping
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.SetWrapPending(State.WrapPending);
    }

    /// <summary>
    ///     Moves the cursor backward (left) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move backward (minimum 1)</param>
    internal void MoveCursorBackward(int count)
    {
        count = Math.Max(1, count);
        _cursorManager.MoveLeft(count);
        
        // Sync cursor manager position to terminal state
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Use terminal state clamping to respect scroll regions and origin mode
        State.ClampCursor();
        
        // Sync back to cursor manager after clamping
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.SetWrapPending(State.WrapPending);
    }

    /// <summary>
    ///     Sets the cursor to an absolute position.
    /// </summary>
    /// <param name="row">Target row (1-based, will be converted to 0-based)</param>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorPosition(int row, int column)
    {
        // Map row parameter based on origin mode (following TypeScript mapRowParamToCursorY)
        int baseRow = State.OriginMode ? State.ScrollTop : 0;
        int targetRow = baseRow + (row - 1);
        
        // Convert column from 1-based to 0-based
        int targetCol = Math.Max(0, Math.Min(Width - 1, column - 1));

        _cursorManager.MoveTo(targetRow, targetCol);
        
        // Sync cursor manager position to terminal state
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Clamp cursor to respect scroll region and origin mode
        State.ClampCursor();
        
        // Sync back to cursor manager after clamping
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.SetWrapPending(State.WrapPending);
    }

    /// <summary>
    ///     Sets the cursor to an absolute column position on the current row.
    ///     Implements CSI G (Cursor Horizontal Absolute) sequence.
    /// </summary>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorColumn(int column)
    {
        // Convert from 1-based to 0-based coordinates and clamp to bounds
        int targetCol = Math.Max(0, Math.Min(Width - 1, column - 1));

        _cursorManager.MoveTo(_cursorManager.Row, targetCol);

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Clears the display according to the specified erase mode.
    ///     Implements CSI J (Erase in Display) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end, 1=start to cursor, 2=entire screen, 3=entire screen and scrollback</param>
    internal void ClearDisplay(int mode)
    {
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Create empty cell with current SGR attributes (unprotected)
        var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false);

        switch (mode)
        {
            case 0: // From cursor to end of display
                ClearLine(0); // Clear from cursor to end of current line
                // Clear all lines below cursor
                for (int row = _cursorManager.Row + 1; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        _screenBufferManager.SetCell(row, col, emptyCell);
                    }
                }
                break;

            case 1: // From start of display to cursor
                // Clear all lines above cursor
                for (int row = 0; row < _cursorManager.Row; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        _screenBufferManager.SetCell(row, col, emptyCell);
                    }
                }
                ClearLine(1); // Clear from start of current line to cursor
                break;

            case 2: // Entire display
                _screenBufferManager.Clear();
                break;

            case 3: // Entire display and scrollback (xterm extension)
                // TODO: Clear scrollback buffer when implemented (task 4.1-4.6)
                _screenBufferManager.Clear();
                break;
        }

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Clears the current line according to the specified erase mode.
    ///     Implements CSI K (Erase in Line) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end of line, 1=start of line to cursor, 2=entire line</param>
    internal void ClearLine(int mode)
    {
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Bounds check
        if (_cursorManager.Row < 0 || _cursorManager.Row >= Height)
        {
            return;
        }

        // Create empty cell with current SGR attributes (unprotected)
        var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false);

        switch (mode)
        {
            case 0: // From cursor to end of line
                for (int col = _cursorManager.Column; col < Width; col++)
                {
                    _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                }
                break;

            case 1: // From start of line to cursor
                for (int col = 0; col <= _cursorManager.Column && col < Width; col++)
                {
                    _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                }
                break;

            case 2: // Entire line
                for (int col = 0; col < Width; col++)
                {
                    _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                }
                break;
        }

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Clears the display selectively according to the specified erase mode.
    ///     Implements CSI ? J (Selective Erase in Display) sequence.
    ///     Only erases unprotected cells, preserving protected cells.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end, 1=start to cursor, 2=entire screen, 3=entire screen and scrollback</param>
    internal void ClearDisplaySelective(int mode)
    {
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Create empty cell with current SGR attributes (unprotected)
        var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false);

        switch (mode)
        {
            case 0: // From cursor to end of display
                ClearLineSelective(0); // Clear from cursor to end of current line
                // Clear all lines below cursor
                for (int row = _cursorManager.Row + 1; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        Cell currentCell = _screenBufferManager.GetCell(row, col);
                        if (!currentCell.IsProtected)
                        {
                            _screenBufferManager.SetCell(row, col, emptyCell);
                        }
                    }
                }
                break;

            case 1: // From start of display to cursor
                // Clear all lines above cursor
                for (int row = 0; row < _cursorManager.Row; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        Cell currentCell = _screenBufferManager.GetCell(row, col);
                        if (!currentCell.IsProtected)
                        {
                            _screenBufferManager.SetCell(row, col, emptyCell);
                        }
                    }
                }
                ClearLineSelective(1); // Clear from start of current line to cursor
                break;

            case 2: // Entire display
            case 3: // Entire display and scrollback (xterm extension)
                if (mode == 3)
                {
                    // TODO: Clear scrollback buffer when implemented (task 4.1-4.6)
                }

                // Clear entire display selectively
                for (int row = 0; row < Height; row++)
                {
                    for (int col = 0; col < Width; col++)
                    {
                        Cell currentCell = _screenBufferManager.GetCell(row, col);
                        if (!currentCell.IsProtected)
                        {
                            _screenBufferManager.SetCell(row, col, emptyCell);
                        }
                    }
                }
                break;
        }

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Clears the current line selectively according to the specified erase mode.
    ///     Implements CSI ? K (Selective Erase in Line) sequence.
    ///     Only erases unprotected cells, preserving protected cells.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end of line, 1=start of line to cursor, 2=entire line</param>
    internal void ClearLineSelective(int mode)
    {
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Bounds check
        if (_cursorManager.Row < 0 || _cursorManager.Row >= Height)
        {
            return;
        }

        // Create empty cell with current SGR attributes (unprotected)
        var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false);

        switch (mode)
        {
            case 0: // From cursor to end of line
                for (int col = _cursorManager.Column; col < Width; col++)
                {
                    Cell currentCell = _screenBufferManager.GetCell(_cursorManager.Row, col);
                    if (!currentCell.IsProtected)
                    {
                        _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                    }
                }
                break;

            case 1: // From start of line to cursor
                for (int col = 0; col <= _cursorManager.Column && col < Width; col++)
                {
                    Cell currentCell = _screenBufferManager.GetCell(_cursorManager.Row, col);
                    if (!currentCell.IsProtected)
                    {
                        _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                    }
                }
                break;

            case 2: // Entire line
                for (int col = 0; col < Width; col++)
                {
                    Cell currentCell = _screenBufferManager.GetCell(_cursorManager.Row, col);
                    if (!currentCell.IsProtected)
                    {
                        _screenBufferManager.SetCell(_cursorManager.Row, col, emptyCell);
                    }
                }
                break;
        }

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Sets the character protection attribute for subsequently written characters.
    ///     Implements DECSCA (CSI Ps " q) sequence.
    /// </summary>
    /// <param name="isProtected">Whether new characters should be protected from selective erase</param>
    internal void SetCharacterProtection(bool isProtected)
    {
        _attributeManager.CurrentCharacterProtection = isProtected;
    }

    /// <summary>
    ///     Saves the current cursor position for later restoration with ESC 8.
    ///     Implements ESC 7 (Save Cursor) sequence.
    /// </summary>
    internal void SaveCursorPosition()
    {
        _cursorManager.SavePosition();
        
        // Also save in terminal state for compatibility
        State.SavedCursor = (_cursorManager.Column, _cursorManager.Row);
    }

    /// <summary>
    ///     Restores the previously saved cursor position.
    ///     Implements ESC 8 (Restore Cursor) sequence.
    /// </summary>
    internal void RestoreCursorPosition()
    {
        _cursorManager.RestorePosition();

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
        
        // Update saved cursor in state for compatibility
        if (State.SavedCursor.HasValue)
        {
            State.SavedCursor = (_cursorManager.Column, _cursorManager.Row);
        }
    }

    /// <summary>
    ///     Handles reverse index (ESC M) - move cursor up; if at top margin, scroll region down.
    ///     Used by full-screen applications like less to scroll the display down within the scroll region.
    /// </summary>
    internal void HandleReverseIndex()
    {
        // Sync cursor with state
        State.CursorX = Cursor.Col;
        State.CursorY = Cursor.Row;

        // Clear wrap pending state
        State.WrapPending = false;

        if (State.CursorY <= State.ScrollTop)
        {
            // At top of scroll region - scroll the region down
            State.CursorY = State.ScrollTop;
            _screenBufferManager.ScrollDownInRegion(1, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes);
        }
        else
        {
            // Move cursor up one line
            State.CursorY = Math.Max(State.ScrollTop, State.CursorY - 1);
        }

        // Update cursor to match state
        Cursor.SetPosition(State.CursorY, State.CursorX);
    }

    /// <summary>
    ///     Sets a tab stop at the current cursor position.
    ///     Implements ESC H (Horizontal Tab Set) sequence.
    /// </summary>
    internal void SetTabStopAtCursor()
    {
        // Sync cursor with state
        State.CursorX = Cursor.Col;
        State.CursorY = Cursor.Row;

        // Set tab stop at current cursor position
        if (State.CursorX >= 0 && State.CursorX < State.TabStops.Length)
        {
            State.TabStops[State.CursorX] = true;
        }
    }

    /// <summary>
    ///     Moves cursor forward to the next tab stop.
    ///     Implements CSI I (Cursor Forward Tab) sequence.
    /// </summary>
    /// <param name="count">Number of tab stops to move forward</param>
    internal void CursorForwardTab(int count)
    {
        // Clear wrap pending state first
        _cursorManager.SetWrapPending(false);

        int n = Math.Max(1, count);
        int currentCol = _cursorManager.Column;

        if (currentCol < 0)
        {
            currentCol = 0;
        }

        for (int i = 0; i < n; i++)
        {
            int nextStop = -1;
            for (int x = currentCol + 1; x < Width; x++)
            {
                if (x < State.TabStops.Length && State.TabStops[x])
                {
                    nextStop = x;
                    break;
                }
            }

            currentCol = nextStop == -1 ? Width - 1 : nextStop;
        }

        // Update cursor position through cursor manager
        _cursorManager.MoveTo(_cursorManager.Row, currentCol);

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Moves cursor backward to the previous tab stop.
    ///     Implements CSI Z (Cursor Backward Tab) sequence.
    /// </summary>
    /// <param name="count">Number of tab stops to move backward</param>
    internal void CursorBackwardTab(int count)
    {
        // Clear wrap pending state first
        _cursorManager.SetWrapPending(false);

        int n = Math.Max(1, count);
        int currentCol = _cursorManager.Column;

        if (currentCol < 0)
        {
            currentCol = 0;
        }

        for (int i = 0; i < n; i++)
        {
            int prevStop = -1;
            for (int x = currentCol - 1; x >= 0; x--)
            {
                if (x < State.TabStops.Length && State.TabStops[x])
                {
                    prevStop = x;
                    break;
                }
            }

            currentCol = prevStop == -1 ? 0 : prevStop;
        }

        // Update cursor position through cursor manager
        _cursorManager.MoveTo(_cursorManager.Row, currentCol);

        // Sync state with cursor manager
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Clears the tab stop at the current cursor position.
    ///     Implements CSI g (Tab Clear) sequence with mode 0.
    /// </summary>
    internal void ClearTabStopAtCursor()
    {
        // Sync cursor with state
        State.CursorX = Cursor.Col;
        State.CursorY = Cursor.Row;

        // Clear tab stop at current cursor position
        if (State.CursorX >= 0 && State.CursorX < State.TabStops.Length)
        {
            State.TabStops[State.CursorX] = false;
        }
    }

    /// <summary>
    ///     Clears all tab stops.
    ///     Implements CSI 3 g (Tab Clear) sequence with mode 3.
    /// </summary>
    internal void ClearAllTabStops()
    {
        // Clear all tab stops
        for (int i = 0; i < State.TabStops.Length; i++)
        {
            State.TabStops[i] = false;
        }
    }

    /// <summary>
    ///     Inserts blank lines at the cursor position within the scroll region.
    ///     Implements CSI L (Insert Lines) sequence.
    ///     Lines below the cursor are shifted down, and lines that would go beyond
    ///     the scroll region bottom are lost.
    /// </summary>
    /// <param name="count">Number of lines to insert (minimum 1)</param>
    internal void InsertLinesInRegion(int count)
    {
        count = Math.Max(1, count);
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for line insertion within scroll region
        _screenBufferManager.InsertLinesInRegion(count, _cursorManager.Row, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Deletes lines at the cursor position within the scroll region.
    ///     Implements CSI M (Delete Lines) sequence.
    ///     Lines below the cursor are shifted up, and blank lines are added
    ///     at the bottom of the scroll region.
    /// </summary>
    /// <param name="count">Number of lines to delete (minimum 1)</param>
    internal void DeleteLinesInRegion(int count)
    {
        count = Math.Max(1, count);
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for line deletion within scroll region
        _screenBufferManager.DeleteLinesInRegion(count, _cursorManager.Row, State.ScrollTop, State.ScrollBottom, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Inserts blank characters at the cursor position within the current line.
    ///     Implements CSI @ (Insert Characters) sequence.
    ///     Characters to the right of the cursor are shifted right, and characters
    ///     that would go beyond the line end are lost.
    /// </summary>
    /// <param name="count">Number of characters to insert (minimum 1)</param>
    internal void InsertCharactersInLine(int count)
    {
        count = Math.Max(1, count);
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for character insertion within current line
        _screenBufferManager.InsertCharactersInLine(count, _cursorManager.Row, _cursorManager.Column, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Deletes characters at the cursor position within the current line.
    ///     Implements CSI P (Delete Characters) sequence.
    ///     Characters to the right of the cursor are shifted left, and blank characters
    ///     are added at the end of the line.
    /// </summary>
    /// <param name="count">Number of characters to delete (minimum 1)</param>
    internal void DeleteCharactersInLine(int count)
    {
        count = Math.Max(1, count);
        
        // Clear wrap pending state
        _cursorManager.SetWrapPending(false);

        // Use screen buffer manager for character deletion within current line
        _screenBufferManager.DeleteCharactersInLine(count, _cursorManager.Row, _cursorManager.Column, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection);

        // Sync state with managers
        State.CursorX = _cursorManager.Column;
        State.CursorY = _cursorManager.Row;
        State.WrapPending = _cursorManager.WrapPending;
    }

    /// <summary>
    ///     Resets the terminal to its initial state.
    ///     Implements ESC c (Reset to Initial State) sequence.
    /// </summary>
    internal void ResetToInitialState()
    {
        // Reset terminal state
        State.Reset();

        // Clear the screen buffer
        ScreenBuffer.Clear();

        // Update cursor to match reset state
        Cursor.SetPosition(State.CursorY, State.CursorX);
        
        // Reset cursor manager style to match state
        _cursorManager.Style = State.CursorStyle;
    }

    /// <summary>
    ///     Designates a character set to a specific G slot.
    ///     Implements ESC ( X, ESC ) X, ESC * X, ESC + X sequences.
    /// </summary>
    /// <param name="slot">The G slot to designate (G0, G1, G2, G3)</param>
    /// <param name="charset">The character set identifier</param>
    internal void DesignateCharacterSet(string slot, string charset)
    {
        CharacterSetKey slotKey = slot switch
        {
            "G0" => CharacterSetKey.G0,
            "G1" => CharacterSetKey.G1,
            "G2" => CharacterSetKey.G2,
            "G3" => CharacterSetKey.G3,
            _ => CharacterSetKey.G0
        };

        _characterSetManager.DesignateCharacterSet(slotKey, charset);
    }

    /// <summary>
    ///     Handles shift-in (SI) control character.
    ///     Switches active character set to G0.
    /// </summary>
    internal void HandleShiftIn()
    {
        _characterSetManager.SwitchCharacterSet(CharacterSetKey.G0);
    }

    /// <summary>
    ///     Handles shift-out (SO) control character.
    ///     Switches active character set to G1.
    /// </summary>
    internal void HandleShiftOut()
    {
        _characterSetManager.SwitchCharacterSet(CharacterSetKey.G1);
    }

    /// <summary>
    ///     Translates a character according to the current character set.
    ///     Handles DEC Special Graphics and other character set mappings.
    /// </summary>
    /// <param name="ch">The character to translate</param>
    /// <returns>The translated character string</returns>
    internal string TranslateCharacter(char ch)
    {
        return _characterSetManager.TranslateCharacter(ch);
    }

    /// <summary>
    ///     Generates a character set query response.
    /// </summary>
    /// <returns>The character set query response string</returns>
    internal string GenerateCharacterSetQueryResponse()
    {
        return _characterSetManager.GenerateCharacterSetQueryResponse();
    }

    /// <summary>
    ///     Raises the Bell event.
    /// </summary>
    private void OnBell()
    {
        Bell?.Invoke(this, new BellEventArgs());
    }

    /// <summary>
    ///     Raises the TitleChanged event.
    /// </summary>
    /// <param name="newTitle">The new window title</param>
    private void OnTitleChanged(string newTitle)
    {
        TitleChanged?.Invoke(this, new TitleChangeEventArgs(newTitle));
    }

    /// <summary>
    ///     Raises the IconNameChanged event.
    /// </summary>
    /// <param name="newIconName">The new icon name</param>
    private void OnIconNameChanged(string newIconName)
    {
        IconNameChanged?.Invoke(this, new IconNameChangeEventArgs(newIconName));
    }

    /// <summary>
    ///     Raises the ClipboardRequest event.
    /// </summary>
    /// <param name="selectionTarget">The selection target (e.g., "c" for clipboard, "p" for primary)</param>
    /// <param name="data">The clipboard data (null for queries)</param>
    /// <param name="isQuery">Whether this is a clipboard query operation</param>
    private void OnClipboardRequest(string selectionTarget, string? data, bool isQuery = false)
    {
        ClipboardRequest?.Invoke(this, new ClipboardEventArgs(selectionTarget, data, isQuery));
    }

    /// <summary>
    ///     Throws an ObjectDisposedException if the terminal has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TerminalEmulator));
        }
    }

    /// <summary>
    ///     Saves the current state of specified private modes for later restoration.
    /// </summary>
    /// <param name="modes">Array of private mode numbers to save</param>
    internal void SavePrivateModes(int[] modes)
    {
        // Save the current state of each specified mode
        _modeManager.SavePrivateModes(modes);
    }

    /// <summary>
    ///     Restores the previously saved state of specified private modes.
    /// </summary>
    /// <param name="modes">Array of private mode numbers to restore</param>
    internal void RestorePrivateModes(int[] modes)
    {
        // Restore the saved state of each specified mode
        _modeManager.RestorePrivateModes(modes);
    }

    /// <summary>
    ///     Sets the cursor style (DECSCUSR).
    /// </summary>
    /// <param name="style">Cursor style parameter from DECSCUSR sequence (0-6)</param>
    public void SetCursorStyle(int style)
    {
        // Validate and normalize cursor style using the new enum system
        CursorStyle validatedStyle = CursorStyleExtensions.ValidateStyle(style);
        
        // Update cursor manager
        _cursorManager.Style = validatedStyle;
        
        // Update terminal state
        State.CursorStyle = validatedStyle;
    }

    /// <summary>
    ///     Sets the cursor style using the CursorStyle enum.
    /// </summary>
    /// <param name="style">The cursor style to set</param>
    public void SetCursorStyle(CursorStyle style)
    {
        // Update cursor manager
        _cursorManager.Style = style;
        
        // Update terminal state
        State.CursorStyle = style;
    }

    /// <summary>
    ///     Wraps paste content with bracketed paste escape sequences if bracketed paste mode is enabled.
    ///     When bracketed paste mode is enabled (DECSET 2004), paste content is wrapped with:
    ///     - Start marker: ESC[200~
    ///     - End marker: ESC[201~
    /// </summary>
    /// <param name="pasteContent">The content to be pasted</param>
    /// <returns>The paste content, optionally wrapped with bracketed paste markers</returns>
    public string WrapPasteContent(string pasteContent)
    {
        if (string.IsNullOrEmpty(pasteContent))
        {
            return pasteContent;
        }

        if (State.BracketedPasteMode)
        {
            return $"\x1b[200~{pasteContent}\x1b[201~";
        }

        return pasteContent;
    }

    /// <summary>
    ///     Wraps paste content with bracketed paste escape sequences if bracketed paste mode is enabled.
    ///     This overload accepts ReadOnlySpan&lt;char&gt; for performance-sensitive scenarios.
    /// </summary>
    /// <param name="pasteContent">The content to be pasted</param>
    /// <returns>The paste content, optionally wrapped with bracketed paste markers</returns>
    public string WrapPasteContent(ReadOnlySpan<char> pasteContent)
    {
        if (pasteContent.IsEmpty)
        {
            return string.Empty;
        }

        if (State.BracketedPasteMode)
        {
            return $"\x1b[200~{pasteContent.ToString()}\x1b[201~";
        }

        return pasteContent.ToString();
    }

    /// <summary>
    ///     Checks if bracketed paste mode is currently enabled.
    ///     This is a convenience method for external components that need to check paste mode state.
    /// </summary>
    /// <returns>True if bracketed paste mode is enabled, false otherwise</returns>
    public bool IsBracketedPasteModeEnabled()
    {
        return State.BracketedPasteMode;
    }

}
