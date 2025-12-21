using StarMap.API;

namespace caTTY.GameMod;

/// <summary>
/// KSA game mod for caTTY terminal emulator.
/// This is a placeholder implementation that will be expanded in later tasks.
/// </summary>
[StarMapMod]
public class TerminalMod
{
    /// <summary>
    /// Gets a value indicating whether the mod should be unloaded immediately.
    /// </summary>
    public bool ImmediateUnload => false;

    /// <summary>
    /// Called after the GUI is rendered.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    [StarMapAfterGui]
    public void OnAfterUi(double dt)
    {
        // Terminal UI will be implemented in later tasks
    }

    /// <summary>
    /// Called before the GUI is rendered.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    [StarMapBeforeGui]
    public void OnBeforeUi(double dt)
    {
        // Pre-UI logic will be implemented in later tasks
    }

    /// <summary>
    /// Called when all mods are loaded.
    /// </summary>
    [StarMapAllModsLoaded]
    public void OnFullyLoaded()
    {
        // Initialization logic will be implemented in later tasks
    }

    /// <summary>
    /// Called for immediate loading.
    /// </summary>
    [StarMapImmediateLoad]
    public void OnImmediatLoad()
    {
        // Immediate load logic will be implemented in later tasks
    }

    /// <summary>
    /// Called when the mod is unloaded.
    /// </summary>
    [StarMapUnload]
    public void Unload()
    {
        // Cleanup logic will be implemented in later tasks
    }
}