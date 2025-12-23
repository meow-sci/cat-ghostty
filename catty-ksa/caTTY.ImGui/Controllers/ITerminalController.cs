using System;

namespace caTTY.ImGui.Controllers;

/// <summary>
/// Interface for terminal controllers that handle ImGui display and input.
/// This interface defines the contract for terminal controllers that can be used
/// by both the standalone test application and the game mod.
/// </summary>
public interface ITerminalController : IDisposable
{
    /// <summary>
    /// Gets or sets whether the terminal window is visible.
    /// </summary>
    bool IsVisible { get; set; }

    /// <summary>
    /// Gets whether the terminal window currently has focus.
    /// </summary>
    bool HasFocus { get; }

    /// <summary>
    /// Renders the terminal window using ImGui.
    /// This method should be called every frame when the terminal should be displayed.
    /// </summary>
    void Render();

    /// <summary>
    /// Updates the terminal controller state.
    /// This method can be used for time-based updates if needed.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update in seconds</param>
    void Update(float deltaTime);
}