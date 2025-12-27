using caTTY.Core.Types;
using caTTY.Core.Managers;

namespace caTTY.Core.Terminal;

/// <summary>
///     Interface for a terminal emulator that processes raw byte data and maintains screen state.
///     Provides a headless terminal implementation with no UI dependencies.
/// </summary>
public interface ITerminalEmulator : IDisposable
{
    /// <summary>
    ///     Gets the width of the terminal in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    ///     Gets the height of the terminal in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    ///     Gets the current screen buffer for rendering.
    /// </summary>
    IScreenBuffer ScreenBuffer { get; }

    /// <summary>
    ///     Gets the current cursor state.
    /// </summary>
    ICursor Cursor { get; }

    /// <summary>
    ///     Gets the scrollback buffer for accessing historical lines.
    /// </summary>
    IScrollbackBuffer ScrollbackBuffer { get; }

    /// <summary>
    ///     Gets the scrollback manager for viewport and scrollback operations.
    /// </summary>
    IScrollbackManager ScrollbackManager { get; }

    /// <summary>
    ///     Processes raw byte data from a shell or other source.
    ///     Can be called with partial chunks and in rapid succession.
    /// </summary>
    /// <param name="data">The raw byte data to process</param>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>
    ///     Processes string data by converting to UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text to process</param>
    void Write(string text);

    /// <summary>
    ///     Resizes the terminal to the specified dimensions.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    void Resize(int width, int height);

    /// <summary>
    ///     Scrolls the viewport up by the specified number of lines.
    ///     Disables auto-scroll if not already at the top.
    /// </summary>
    /// <param name="lines">Number of lines to scroll up</param>
    void ScrollViewportUp(int lines);

    /// <summary>
    ///     Scrolls the viewport down by the specified number of lines.
    ///     Re-enables auto-scroll if scrolled to the bottom.
    /// </summary>
    /// <param name="lines">Number of lines to scroll down</param>
    void ScrollViewportDown(int lines);

    /// <summary>
    ///     Scrolls to the top of the scrollback buffer.
    ///     Disables auto-scroll.
    /// </summary>
    void ScrollViewportToTop();

    /// <summary>
    ///     Scrolls to the bottom of the scrollback buffer.
    ///     Re-enables auto-scroll.
    /// </summary>
    void ScrollViewportToBottom();

    /// <summary>
    ///     Gets whether auto-scroll is currently enabled.
    ///     Auto-scroll is disabled when user scrolls up and re-enabled when they return to bottom.
    /// </summary>
    bool IsAutoScrollEnabled { get; }

    /// <summary>
    ///     Gets the current viewport offset from the bottom.
    ///     0 means viewing the most recent content, positive values mean scrolled up into history.
    /// </summary>
    int ViewportOffset { get; }

    /// <summary>
    ///     Event raised when the screen content has been updated and needs refresh.
    /// </summary>
    event EventHandler<ScreenUpdatedEventArgs>? ScreenUpdated;

    /// <summary>
    ///     Event raised when the terminal needs to send a response back to the shell.
    ///     Used for device query replies and other terminal-generated responses.
    /// </summary>
    event EventHandler<ResponseEmittedEventArgs>? ResponseEmitted;

    /// <summary>
    ///     Event raised when a bell character (BEL, 0x07) is received.
    /// </summary>
    event EventHandler<BellEventArgs>? Bell;
}
