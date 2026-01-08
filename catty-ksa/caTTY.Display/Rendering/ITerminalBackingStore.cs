using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;

namespace caTTY.Display.Rendering;

/// <summary>
///     Interface for the backing store of a terminal render cache.
///     This abstracts the specific implementation (Texture, DisplayList, etc.)
///     from the caching logic.
/// </summary>
public interface ITerminalBackingStore : IDisposable
{
    /// <summary>
    ///     Prepares the backing store for capturing a new frame.
    ///     Returns true if capture can proceed, false if it fails.
    /// </summary>
    bool BeginCapture(int width, int height);

    /// <summary>
    ///     Finalizes the capture.
    /// </summary>
    void EndCapture();

    /// <summary>
    ///     Draws the cached content to the given draw list.
    /// </summary>
    void Draw(ImDrawListPtr drawList, float2 position, float2 size);
    
    /// <summary>
    ///     Returns true if the backing store is ready to draw.
    /// </summary>
    bool IsReady { get; }
}
