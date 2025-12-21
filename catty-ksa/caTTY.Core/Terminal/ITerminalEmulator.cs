using System;
using caTTY.Core.Types;

namespace caTTY.Core.Terminal;

/// <summary>
/// Interface for a terminal emulator that processes raw byte data and maintains screen state.
/// Provides a headless terminal implementation with no UI dependencies.
/// </summary>
public interface ITerminalEmulator : IDisposable
{
    /// <summary>
    /// Gets the width of the terminal in columns.
    /// </summary>
    int Width { get; }

    /// <summary>
    /// Gets the height of the terminal in rows.
    /// </summary>
    int Height { get; }

    /// <summary>
    /// Gets the current screen buffer for rendering.
    /// </summary>
    IScreenBuffer ScreenBuffer { get; }

    /// <summary>
    /// Gets the current cursor state.
    /// </summary>
    ICursor Cursor { get; }

    /// <summary>
    /// Processes raw byte data from a shell or other source.
    /// Can be called with partial chunks and in rapid succession.
    /// </summary>
    /// <param name="data">The raw byte data to process</param>
    void Write(ReadOnlySpan<byte> data);

    /// <summary>
    /// Processes string data by converting to UTF-8 bytes.
    /// </summary>
    /// <param name="text">The text to process</param>
    void Write(string text);

    /// <summary>
    /// Resizes the terminal to the specified dimensions.
    /// </summary>
    /// <param name="width">New width in columns</param>
    /// <param name="height">New height in rows</param>
    void Resize(int width, int height);

    /// <summary>
    /// Event raised when the screen content has been updated and needs refresh.
    /// </summary>
    event EventHandler<ScreenUpdatedEventArgs>? ScreenUpdated;

    /// <summary>
    /// Event raised when the terminal needs to send a response back to the shell.
    /// Used for device query replies and other terminal-generated responses.
    /// </summary>
    event EventHandler<ResponseEmittedEventArgs>? ResponseEmitted;
}