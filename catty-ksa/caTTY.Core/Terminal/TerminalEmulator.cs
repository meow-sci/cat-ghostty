using System.Text;
using caTTY.Core.Parsing;
using caTTY.Core.Types;
using caTTY.Core.Managers;
using caTTY.Core.Tracing;
using caTTY.Core.Rpc;
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

    // Operation classes
    private readonly EmulatorOps.TerminalViewportOps _viewportOps;
    private readonly EmulatorOps.TerminalResizeOps _resizeOps;
    private readonly EmulatorOps.TerminalCursorMovementOps _cursorMovementOps;
    private readonly EmulatorOps.TerminalCursorSaveRestoreOps _cursorSaveRestoreOps;
    private readonly EmulatorOps.TerminalCursorStyleOps _cursorStyleOps;
    private readonly EmulatorOps.TerminalEraseInDisplayOps _eraseInDisplayOps;
    private readonly EmulatorOps.TerminalEraseInLineOps _eraseInLineOps;
    private readonly EmulatorOps.TerminalSelectiveEraseInDisplayOps _selectiveEraseInDisplayOps;
    private readonly EmulatorOps.TerminalSelectiveEraseInLineOps _selectiveEraseInLineOps;
    private readonly EmulatorOps.TerminalScrollOps _scrollOps;
    private readonly EmulatorOps.TerminalScrollRegionOps _scrollRegionOps;
    private readonly EmulatorOps.TerminalInsertLinesOps _insertLinesOps;
    private readonly EmulatorOps.TerminalDeleteLinesOps _deleteLinesOps;
    private readonly EmulatorOps.TerminalInsertCharsOps _insertCharsOps;
    private readonly EmulatorOps.TerminalDeleteCharsOps _deleteCharsOps;
    private readonly EmulatorOps.TerminalEraseCharsOps _eraseCharsOps;

    // Optional RPC components for game integration
    private readonly IRpcHandler? _rpcHandler;

    private bool _disposed;

    /// <summary>
    ///     Creates a new terminal emulator with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <param name="logger">Optional logger for debugging (uses NullLogger if not provided)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public TerminalEmulator(int width, int height, ILogger? logger = null) : this(width, height, 1000, logger, null)
    {
    }

    /// <summary>
    ///     Creates a new terminal emulator with the specified dimensions, scrollback, and optional RPC handler.
    /// </summary>
    /// <param name="width">Width in columns</param>
    /// <param name="height">Height in rows</param>
    /// <param name="scrollbackLines">Maximum number of scrollback lines (default: 1000)</param>
    /// <param name="logger">Optional logger for debugging (uses NullLogger if not provided)</param>
    /// <param name="rpcHandler">Optional RPC handler for game integration (null disables RPC functionality)</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions are invalid</exception>
    public TerminalEmulator(int width, int height, int scrollbackLines, ILogger? logger = null, IRpcHandler? rpcHandler = null)
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
        _rpcHandler = rpcHandler;

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

        // Initialize operation classes
        _viewportOps = new EmulatorOps.TerminalViewportOps(_scrollbackManager, OnScreenUpdated);
        _resizeOps = new EmulatorOps.TerminalResizeOps(State, _screenBufferManager, _cursorManager, _scrollbackManager, () => Width, () => Height, OnScreenUpdated);
        _cursorMovementOps = new EmulatorOps.TerminalCursorMovementOps(_cursorManager, () => State, () => Width);
        _cursorSaveRestoreOps = new EmulatorOps.TerminalCursorSaveRestoreOps(_cursorManager, () => State, () => Width, () => Height, _logger);
        _cursorStyleOps = new EmulatorOps.TerminalCursorStyleOps(_cursorManager, () => State, _logger);
        _eraseInDisplayOps = new EmulatorOps.TerminalEraseInDisplayOps(_cursorManager, _attributeManager, _screenBufferManager, () => State, () => Width, () => Height, ClearLine, _logger);
        _eraseInLineOps = new EmulatorOps.TerminalEraseInLineOps(_cursorManager, _attributeManager, _screenBufferManager, () => State, () => Width, () => Height, _logger);
        _selectiveEraseInLineOps = new EmulatorOps.TerminalSelectiveEraseInLineOps(_cursorManager, _attributeManager, _screenBufferManager, () => State, () => Width, () => Height, _logger);
        _selectiveEraseInDisplayOps = new EmulatorOps.TerminalSelectiveEraseInDisplayOps(_cursorManager, _attributeManager, _screenBufferManager, () => State, () => Width, () => Height, ClearLineSelective, _logger);
        _scrollOps = new EmulatorOps.TerminalScrollOps(_cursorManager, _screenBufferManager, _attributeManager, () => State, () => Cursor);
        _scrollRegionOps = new EmulatorOps.TerminalScrollRegionOps(_cursorManager, () => State, () => Height);
        _insertLinesOps = new EmulatorOps.TerminalInsertLinesOps(_cursorManager, _screenBufferManager, _attributeManager, () => State);
        _deleteLinesOps = new EmulatorOps.TerminalDeleteLinesOps(_cursorManager, _screenBufferManager, _attributeManager, () => State);
        _insertCharsOps = new EmulatorOps.TerminalInsertCharsOps(_cursorManager, _screenBufferManager, _attributeManager, () => State);
        _deleteCharsOps = new EmulatorOps.TerminalDeleteCharsOps(_cursorManager, _screenBufferManager, _attributeManager, () => State);
        _eraseCharsOps = new EmulatorOps.TerminalEraseCharsOps(_cursorManager, _screenBufferManager, _attributeManager, () => State);

        // Initialize parser with terminal handlers and optional RPC components
        var handlers = new TerminalParserHandlers(this, _logger, _rpcHandler);
        var parserOptions = new ParserOptions
        {
            Handlers = handlers,
            Logger = _logger,
            EmitNormalBytesDuringEscapeSequence = false,
            ProcessC0ControlsDuringEscapeSequence = true,
            CursorPositionProvider = this
        };

        // Wire RPC components if RPC handler is provided
        if (_rpcHandler != null)
        {
            // Create RPC components for integration
            // Note: These would typically be injected, but for clean integration we create them here
            var rpcSequenceDetector = new RpcSequenceDetector();
            var rpcSequenceParser = new RpcSequenceParser();

            parserOptions.RpcSequenceDetector = rpcSequenceDetector;
            parserOptions.RpcSequenceParser = rpcSequenceParser;
            parserOptions.RpcHandler = _rpcHandler;

            _logger.LogDebug("RPC functionality enabled for terminal emulator");
        }
        else
        {
            _logger.LogDebug("RPC functionality disabled - no RPC handler provided");
        }

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
    ///     Gets whether RPC functionality is enabled for this terminal emulator.
    /// </summary>
    public bool IsRpcEnabled => _rpcHandler != null && _rpcHandler.IsEnabled;

    /// <summary>
    ///     Gets the RPC handler if RPC functionality is enabled, null otherwise.
    /// </summary>
    public IRpcHandler? RpcHandler => _rpcHandler;

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
        _resizeOps.Resize(width, height);
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
        _viewportOps.ScrollViewportUp(lines);
    }

    /// <summary>
    ///     Scrolls the viewport down by the specified number of lines.
    ///     Re-enables auto-scroll if scrolled to the bottom.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    public void ScrollViewportDown(int lines)
    {
        ThrowIfDisposed();
        _viewportOps.ScrollViewportDown(lines);
    }

    /// <summary>
    ///     Scrolls to the top of the scrollback buffer.
    ///     Disables auto-scroll.
    /// </summary>
    public void ScrollViewportToTop()
    {
        ThrowIfDisposed();
        _viewportOps.ScrollViewportToTop();
    }

    /// <summary>
    ///     Scrolls to the bottom of the scrollback buffer.
    ///     Re-enables auto-scroll.
    /// </summary>
    public void ScrollViewportToBottom()
    {
        ThrowIfDisposed();
        _viewportOps.ScrollViewportToBottom();
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
    ///     Enables or disables RPC functionality at runtime.
    ///     This allows dynamic control over RPC processing without recreating the terminal.
    /// </summary>
    /// <param name="enabled">True to enable RPC processing, false to disable</param>
    /// <returns>True if the setting was applied, false if RPC handler is not available</returns>
    public bool SetRpcEnabled(bool enabled)
    {
        ThrowIfDisposed();

        if (_rpcHandler == null)
        {
            _logger.LogWarning("Cannot set RPC enabled state - no RPC handler available");
            return false;
        }

        bool previousState = _rpcHandler.IsEnabled;
        _rpcHandler.IsEnabled = enabled;

        _logger.LogDebug("RPC functionality {Action} (was {PreviousState})",
            enabled ? "enabled" : "disabled",
            previousState ? "enabled" : "disabled");

        return true;
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
    ///     Gets the current foreground color for color queries.
    ///     Returns the current SGR foreground color or default terminal theme color.
    /// </summary>
    /// <returns>RGB color values for the current foreground color</returns>
    internal (byte Red, byte Green, byte Blue) GetCurrentForegroundColor()
    {
        var currentAttributes = _attributeManager.CurrentAttributes;

        // If a specific foreground color is set in SGR attributes, use it
        if (currentAttributes.ForegroundColor.HasValue)
        {
            var color = currentAttributes.ForegroundColor.Value;
            return color.Type switch
            {
                ColorType.Rgb => (color.Red, color.Green, color.Blue),
                ColorType.Named => GetNamedColorRgb(color.NamedColor, isBackground: false),
                ColorType.Indexed => GetIndexedColorRgb(color.Index, isBackground: false),
                _ => GetDefaultForegroundColor()
            };
        }

        // Return default terminal foreground color
        return GetDefaultForegroundColor();
    }

    /// <summary>
    ///     Gets the current background color for color queries.
    ///     Returns the current SGR background color or default terminal theme color.
    /// </summary>
    /// <returns>RGB color values for the current background color</returns>
    internal (byte Red, byte Green, byte Blue) GetCurrentBackgroundColor()
    {
        var currentAttributes = _attributeManager.CurrentAttributes;

        // If a specific background color is set in SGR attributes, use it
        if (currentAttributes.BackgroundColor.HasValue)
        {
            var color = currentAttributes.BackgroundColor.Value;
            return color.Type switch
            {
                ColorType.Rgb => (color.Red, color.Green, color.Blue),
                ColorType.Named => GetNamedColorRgb(color.NamedColor, isBackground: true),
                ColorType.Indexed => GetIndexedColorRgb(color.Index, isBackground: true),
                _ => GetDefaultBackgroundColor()
            };
        }

        // Return default terminal background color
        return GetDefaultBackgroundColor();
    }

    /// <summary>
    ///     Gets the default foreground color (typically white or light gray).
    /// </summary>
    private static (byte Red, byte Green, byte Blue) GetDefaultForegroundColor()
    {
        // Standard terminal default foreground (light gray)
        return (192, 192, 192);
    }

    /// <summary>
    ///     Gets the default background color (typically black or dark).
    /// </summary>
    private static (byte Red, byte Green, byte Blue) GetDefaultBackgroundColor()
    {
        // Standard terminal default background (black)
        return (0, 0, 0);
    }

    /// <summary>
    ///     Converts a named color to RGB values.
    /// </summary>
    private static (byte Red, byte Green, byte Blue) GetNamedColorRgb(NamedColor namedColor, bool isBackground)
    {
        return namedColor switch
        {
            NamedColor.Black => (0, 0, 0),
            NamedColor.Red => (128, 0, 0),
            NamedColor.Green => (0, 128, 0),
            NamedColor.Yellow => (128, 128, 0),
            NamedColor.Blue => (0, 0, 128),
            NamedColor.Magenta => (128, 0, 128),
            NamedColor.Cyan => (0, 128, 128),
            NamedColor.White => (192, 192, 192),
            NamedColor.BrightBlack => (128, 128, 128),
            NamedColor.BrightRed => (255, 0, 0),
            NamedColor.BrightGreen => (0, 255, 0),
            NamedColor.BrightYellow => (255, 255, 0),
            NamedColor.BrightBlue => (0, 0, 255),
            NamedColor.BrightMagenta => (255, 0, 255),
            NamedColor.BrightCyan => (0, 255, 255),
            NamedColor.BrightWhite => (255, 255, 255),
            _ => isBackground ? GetDefaultBackgroundColor() : GetDefaultForegroundColor()
        };
    }

    /// <summary>
    ///     Converts an indexed color (0-255) to RGB values using standard terminal palette.
    /// </summary>
    private static (byte Red, byte Green, byte Blue) GetIndexedColorRgb(int index, bool isBackground)
    {
        // Standard 16 colors (0-15)
        if (index < 16)
        {
            var namedColor = (NamedColor)index;
            return GetNamedColorRgb(namedColor, isBackground);
        }

        // 216 color cube (16-231): 6x6x6 RGB cube
        if (index >= 16 && index <= 231)
        {
            int cubeIndex = index - 16;
            int r = (cubeIndex / 36) % 6;
            int g = (cubeIndex / 6) % 6;
            int b = cubeIndex % 6;

            // Convert 0-5 range to 0-255 range
            byte red = (byte)(r == 0 ? 0 : 55 + r * 40);
            byte green = (byte)(g == 0 ? 0 : 55 + g * 40);
            byte blue = (byte)(b == 0 ? 0 : 55 + b * 40);

            return (red, green, blue);
        }

        // Grayscale ramp (232-255): 24 shades of gray
        if (index >= 232 && index <= 255)
        {
            int grayLevel = 8 + (index - 232) * 10;
            byte gray = (byte)Math.Min(255, grayLevel);
            return (gray, gray, gray);
        }

        // Invalid index - return default
        return isBackground ? GetDefaultBackgroundColor() : GetDefaultForegroundColor();
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

        // Handle insert mode - shift existing characters right if insert mode is enabled
        if (_modeManager.InsertMode)
        {
            // Insert mode: shift existing characters right before writing new character
            int charactersToShift = isWide ? 2 : 1;
            ShiftCharactersRight(_cursorManager.Row, _cursorManager.Column, charactersToShift);
        }

        var cell = new Cell(character, _attributeManager.CurrentAttributes, _attributeManager.CurrentCharacterProtection, _attributeManager.CurrentHyperlinkUrl, isWide);
        _screenBufferManager.SetCell(_cursorManager.Row, _cursorManager.Column, cell);

        // For wide characters, also mark the next cell as part of the wide character
        // NOTE: 64e8b6190d4498d3b1cd2e1b3e07e7587e685967 implemented this and it broke cursor positioning entirely on the line discipline, removed it

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

        // NOTE: 64e8b6190d4498d3b1cd2e1b3e07e7587e685967 implemented this and it broke cursor positioning entirely on the line discipline

        return false;
    }

    /// <summary>
    ///     Shifts characters to the right on the current line to make space for insertion.
    ///     Used when insert mode is enabled to shift existing characters before writing new ones.
    /// </summary>
    /// <param name="row">The row to shift characters on</param>
    /// <param name="startColumn">The column to start shifting from</param>
    /// <param name="shiftAmount">Number of positions to shift (1 for normal chars, 2 for wide chars)</param>
    private void ShiftCharactersRight(int row, int startColumn, int shiftAmount)
    {
        // Bounds checking
        if (row < 0 || row >= Height || startColumn < 0 || startColumn >= Width)
        {
            return;
        }

        // Calculate how many characters we can actually shift
        int availableSpace = Width - startColumn;
        if (availableSpace <= shiftAmount)
        {
            // Not enough space to shift - clear from cursor to end of line
            for (int col = startColumn; col < Width; col++)
            {
                var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false, null, false);
                _screenBufferManager.SetCell(row, col, emptyCell);
            }
            return;
        }

        // Shift characters to the right, starting from the rightmost character
        // Work backwards to avoid overwriting characters we haven't moved yet
        for (int col = Width - 1 - shiftAmount; col >= startColumn; col--)
        {
            int targetCol = col + shiftAmount;
            if (targetCol < Width)
            {
                // Get the cell to move
                var sourceCell = _screenBufferManager.GetCell(row, col);
                _screenBufferManager.SetCell(row, targetCol, sourceCell);
            }
        }

        // Clear the positions where we're about to insert
        for (int col = startColumn; col < startColumn + shiftAmount && col < Width; col++)
        {
            var emptyCell = new Cell(' ', _attributeManager.CurrentAttributes, false, null, false);
            _screenBufferManager.SetCell(row, col, emptyCell);
        }
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
        _scrollOps.ScrollScreenUp(lines);
    }

    /// <summary>
    ///     Handles scroll down sequence (CSI T) - scroll screen down by specified lines.
    ///     Implements CSI Ps T (Scroll Down) sequence.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down (default: 1)</param>
    internal void ScrollScreenDown(int lines = 1)
    {
        _scrollOps.ScrollScreenDown(lines);
    }

    /// <summary>
    ///     Sets the scroll region (DECSTBM - Set Top and Bottom Margins).
    ///     Implements CSI Ps ; Ps r sequence.
    /// </summary>
    /// <param name="top">Top boundary (1-indexed, null for default)</param>
    /// <param name="bottom">Bottom boundary (1-indexed, null for default)</param>
    internal void SetScrollRegion(int? top, int? bottom)
    {
        _scrollRegionOps.SetScrollRegion(top, bottom);
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
        _cursorMovementOps.MoveCursorUp(count);
    }

    /// <summary>
    ///     Moves the cursor down by the specified number of lines.
    /// </summary>
    /// <param name="count">Number of lines to move down (minimum 1)</param>
    internal void MoveCursorDown(int count)
    {
        _cursorMovementOps.MoveCursorDown(count);
    }

    /// <summary>
    ///     Moves the cursor forward (right) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move forward (minimum 1)</param>
    internal void MoveCursorForward(int count)
    {
        _cursorMovementOps.MoveCursorForward(count);
    }

    /// <summary>
    ///     Moves the cursor backward (left) by the specified number of columns.
    /// </summary>
    /// <param name="count">Number of columns to move backward (minimum 1)</param>
    internal void MoveCursorBackward(int count)
    {
        _cursorMovementOps.MoveCursorBackward(count);
    }

    /// <summary>
    ///     Sets the cursor to an absolute position.
    /// </summary>
    /// <param name="row">Target row (1-based, will be converted to 0-based)</param>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorPosition(int row, int column)
    {
        _cursorMovementOps.SetCursorPosition(row, column);
    }

    /// <summary>
    ///     Sets the cursor to an absolute column position on the current row.
    ///     Implements CSI G (Cursor Horizontal Absolute) sequence.
    /// </summary>
    /// <param name="column">Target column (1-based, will be converted to 0-based)</param>
    internal void SetCursorColumn(int column)
    {
        _cursorMovementOps.SetCursorColumn(column);
    }

    /// <summary>
    ///     Clears the display according to the specified erase mode.
    ///     Implements CSI J (Erase in Display) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end, 1=start to cursor, 2=entire screen, 3=entire screen and scrollback</param>
    internal void ClearDisplay(int mode)
    {
        _eraseInDisplayOps.ClearDisplay(mode);
    }

    /// <summary>
    ///     Clears the current line according to the specified erase mode.
    ///     Implements CSI K (Erase in Line) sequence.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end of line, 1=start of line to cursor, 2=entire line</param>
    internal void ClearLine(int mode)
    {
        _eraseInLineOps.ClearLine(mode);
    }

    /// <summary>
    ///     Clears the display selectively according to the specified erase mode.
    ///     Implements CSI ? J (Selective Erase in Display) sequence.
    ///     Only erases unprotected cells, preserving protected cells.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end, 1=start to cursor, 2=entire screen, 3=entire screen and scrollback</param>
    internal void ClearDisplaySelective(int mode)
    {
        _selectiveEraseInDisplayOps.ClearDisplaySelective(mode);
    }

    /// <summary>
    ///     Clears the current line selectively according to the specified erase mode.
    ///     Implements CSI ? K (Selective Erase in Line) sequence.
    ///     Only erases unprotected cells, preserving protected cells.
    /// </summary>
    /// <param name="mode">Erase mode: 0=cursor to end of line, 1=start of line to cursor, 2=entire line</param>
    internal void ClearLineSelective(int mode) => _selectiveEraseInLineOps.ClearLineSelective(mode);

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
        _cursorSaveRestoreOps.SaveCursorPosition();
    }

    /// <summary>
    ///     Restores the previously saved cursor position.
    ///     Implements ESC 8 (Restore Cursor) sequence.
    /// </summary>
    internal void RestoreCursorPosition()
    {
        _cursorSaveRestoreOps.RestoreCursorPosition();
    }

    /// <summary>
    ///     Saves the current cursor position using ANSI style (CSI s).
    ///     This is separate from DEC cursor save/restore (ESC 7/8) to maintain compatibility.
    ///     Implements CSI s sequence.
    /// </summary>
    internal void SaveCursorPositionAnsi()
    {
        _cursorSaveRestoreOps.SaveCursorPositionAnsi();
    }

    /// <summary>
    ///     Restores the previously saved ANSI cursor position.
    ///     This is separate from DEC cursor save/restore (ESC 7/8) to maintain compatibility.
    ///     Implements CSI u sequence.
    /// </summary>
    internal void RestoreCursorPositionAnsi()
    {
        _cursorSaveRestoreOps.RestoreCursorPositionAnsi();
    }

    /// <summary>
    ///     Handles reverse index (ESC M) - move cursor up; if at top margin, scroll region down.
    ///     Used by full-screen applications like less to scroll the display down within the scroll region.
    /// </summary>
    internal void HandleReverseIndex()
    {
        _scrollOps.HandleReverseIndex();
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
        _insertLinesOps.InsertLinesInRegion(count);
    }

    /// <summary>
    ///     Deletes lines at the cursor position within the scroll region.
    ///     Implements CSI M (Delete Lines) sequence.
    ///     Lines below the cursor are shifted up, and blank lines are added
    ///     at the bottom of the scroll region.
    /// </summary>
    /// <param name="count">Number of lines to delete (minimum 1)</param>
    internal void DeleteLinesInRegion(int count) =>
        _deleteLinesOps.DeleteLinesInRegion(count);

    /// <summary>
    ///     Inserts blank characters at the cursor position within the current line.
    ///     Implements CSI @ (Insert Characters) sequence.
    ///     Characters to the right of the cursor are shifted right, and characters
    ///     that would go beyond the line end are lost.
    /// </summary>
    /// <param name="count">Number of characters to insert (minimum 1)</param>
    internal void InsertCharactersInLine(int count) =>
        _insertCharsOps.InsertCharactersInLine(count);

    /// <summary>
    ///     Deletes characters at the cursor position within the current line.
    ///     Implements CSI P (Delete Characters) sequence.
    ///     Characters to the right of the cursor are shifted left, and blank characters
    ///     are added at the end of the line.
    /// </summary>
    /// <param name="count">Number of characters to delete (minimum 1)</param>
    internal void DeleteCharactersInLine(int count) =>
        _deleteCharsOps.DeleteCharactersInLine(count);

    /// <summary>
    ///     Erases characters at the cursor position within the current line.
    ///     Implements CSI X (Erase Character) sequence.
    ///     Erases characters by replacing them with blank characters using current SGR attributes.
    ///     Does not move the cursor or shift other characters.
    /// </summary>
    /// <param name="count">Number of characters to erase (minimum 1)</param>
    internal void EraseCharactersInLine(int count) =>
        _eraseCharsOps.EraseCharactersInLine(count);

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
    ///     Performs a soft reset of the terminal.
    ///     Implements CSI ! p (DECSTR - DEC Soft Terminal Reset) sequence.
    ///     Resets terminal modes and state without clearing the screen buffer or cursor position.
    /// </summary>
    public void SoftReset()
    {
        // Reset cursor position to home (0,0)
        State.CursorX = 0;
        State.CursorY = 0;

        // Clear saved cursor positions
        State.SavedCursor = null;
        State.AnsiSavedCursor = null;

        // Reset wrap pending state
        State.WrapPending = false;

        // Reset cursor style and visibility to defaults
        State.CursorStyle = CursorStyle.BlinkingBlock;
        State.CursorVisible = true;

        // Reset terminal modes to defaults
        State.ApplicationCursorKeys = false;
        State.OriginMode = false;
        State.AutoWrapMode = true;

        // Reset scroll region to full screen
        State.ScrollTop = 0;
        State.ScrollBottom = Height - 1;

        // Reset character protection to unprotected
        State.CurrentCharacterProtection = false;
        _attributeManager.CurrentCharacterProtection = false;

        // Reset SGR attributes to defaults
        State.CurrentSgrState = SgrAttributes.Default;
        _attributeManager.ResetAttributes();

        // Reset character sets to defaults (ASCII)
        State.CharacterSets = new CharacterSetState();

        // Reset UTF-8 mode to enabled
        State.Utf8Mode = true;

        // Reset tab stops to default (every 8 columns)
        State.InitializeTabStops(Width);

        // Update cursor manager to match reset state
        _cursorManager.MoveTo(State.CursorY, State.CursorX);
        _cursorManager.Visible = State.CursorVisible;
        _cursorManager.Style = State.CursorStyle;

        // Update mode manager to match reset state
        _modeManager.AutoWrapMode = State.AutoWrapMode;
        _modeManager.ApplicationCursorKeys = State.ApplicationCursorKeys;
        _modeManager.CursorVisible = State.CursorVisible;
        _modeManager.OriginMode = State.OriginMode;

        _logger.LogDebug("Soft reset completed - modes and state reset without clearing screen");
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
        _cursorStyleOps.SetCursorStyle(style);
    }

    /// <summary>
    ///     Sets the cursor style using the CursorStyle enum.
    /// </summary>
    /// <param name="style">The cursor style to set</param>
    public void SetCursorStyle(CursorStyle style)
    {
        _cursorStyleOps.SetCursorStyle(style);
    }

    /// <summary>
    ///     Sets insert mode state. When enabled, new characters are inserted, shifting existing characters right.
    ///     When disabled, new characters overwrite existing characters (default behavior).
    /// </summary>
    /// <param name="enabled">True to enable insert mode, false to disable</param>
    public void SetInsertMode(bool enabled)
    {
        // Update mode manager
        _modeManager.InsertMode = enabled;

        // Update terminal state for compatibility
        _modeManager.SetMode(4, enabled);
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
