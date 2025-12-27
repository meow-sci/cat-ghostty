namespace caTTY.Core.Managers;

/// <summary>
///     Manages terminal mode state tracking including auto-wrap, cursor keys, and other terminal modes.
///     Handles both standard ANSI modes and DEC private modes.
/// </summary>
public class ModeManager : IModeManager
{
    private readonly Dictionary<int, bool> _modes;
    private readonly Dictionary<int, bool> _privateModes;
    private readonly Dictionary<int, bool> _savedModes;
    private readonly Dictionary<int, bool> _savedPrivateModes;

    /// <summary>
    ///     Creates a new mode manager with default mode states.
    /// </summary>
    public ModeManager()
    {
        _modes = new Dictionary<int, bool>();
        _privateModes = new Dictionary<int, bool>();
        _savedModes = new Dictionary<int, bool>();
        _savedPrivateModes = new Dictionary<int, bool>();
        
        Reset();
    }

    /// <summary>
    ///     Gets or sets auto-wrap mode. When true, cursor wraps to next line at right edge.
    /// </summary>
    public bool AutoWrapMode { get; set; } = true;

    /// <summary>
    ///     Gets or sets application cursor keys mode. When true, arrow keys send different escape sequences.
    /// </summary>
    public bool ApplicationCursorKeys { get; set; } = false;

    /// <summary>
    ///     Gets or sets bracketed paste mode. When true, paste content is wrapped with escape sequences.
    /// </summary>
    public bool BracketedPasteMode { get; set; } = false;

    /// <summary>
    ///     Gets or sets cursor visibility mode.
    /// </summary>
    public bool CursorVisible { get; set; } = true;

    /// <summary>
    ///     Gets or sets origin mode. When true, cursor positioning is relative to scroll region.
    /// </summary>
    public bool OriginMode { get; set; } = false;

    /// <summary>
    ///     Gets or sets UTF-8 mode.
    /// </summary>
    public bool Utf8Mode { get; set; } = true;

    /// <summary>
    ///     Sets a specific terminal mode by number.
    /// </summary>
    /// <param name="mode">Mode number</param>
    /// <param name="enabled">Whether the mode should be enabled</param>
    public void SetMode(int mode, bool enabled)
    {
        _modes[mode] = enabled;
        
        // Update specific mode properties for commonly used modes
        switch (mode)
        {
            case 4: // Insert/Replace mode (IRM)
                // Will be implemented when character insertion is added
                break;
            case 20: // Automatic Newline mode (LNM)
                // Will be implemented when line discipline is enhanced
                break;
        }
    }

    /// <summary>
    ///     Gets the state of a specific terminal mode by number.
    /// </summary>
    /// <param name="mode">Mode number</param>
    /// <returns>True if the mode is enabled, false otherwise</returns>
    public bool GetMode(int mode)
    {
        return _modes.TryGetValue(mode, out bool enabled) && enabled;
    }

    /// <summary>
    ///     Sets a private terminal mode by number (DEC modes).
    /// </summary>
    /// <param name="mode">Private mode number</param>
    /// <param name="enabled">Whether the mode should be enabled</param>
    public void SetPrivateMode(int mode, bool enabled)
    {
        _privateModes[mode] = enabled;
        
        // Update specific mode properties for commonly used private modes
        switch (mode)
        {
            case 1: // Application cursor keys (DECCKM)
                ApplicationCursorKeys = enabled;
                break;
            case 6: // Origin mode (DECOM)
                OriginMode = enabled;
                break;
            case 7: // Auto-wrap mode (DECAWM)
                AutoWrapMode = enabled;
                break;
            case 25: // Cursor visibility (DECTCEM)
                CursorVisible = enabled;
                break;
            case 47: // Alternate screen buffer
            case 1047: // Alternate screen buffer (xterm)
            case 1049: // Alternate screen buffer with cursor save/restore (xterm)
                // Will be handled by AlternateScreenManager when implemented
                break;
            case 2004: // Bracketed paste mode
                BracketedPasteMode = enabled;
                break;
            case 2027: // UTF-8 mode
                Utf8Mode = enabled;
                break;
        }
    }

    /// <summary>
    ///     Gets the state of a private terminal mode by number (DEC modes).
    /// </summary>
    /// <param name="mode">Private mode number</param>
    /// <returns>True if the private mode is enabled, false otherwise</returns>
    public bool GetPrivateMode(int mode)
    {
        return _privateModes.TryGetValue(mode, out bool enabled) && enabled;
    }

    /// <summary>
    ///     Saves the current state of all modes for later restoration.
    /// </summary>
    public void SaveModes()
    {
        _savedModes.Clear();
        foreach (var kvp in _modes)
        {
            _savedModes[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    ///     Restores the previously saved mode states.
    /// </summary>
    public void RestoreModes()
    {
        foreach (var kvp in _savedModes)
        {
            SetMode(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    ///     Saves the current state of private modes for later restoration.
    /// </summary>
    public void SavePrivateModes()
    {
        _savedPrivateModes.Clear();
        foreach (var kvp in _privateModes)
        {
            _savedPrivateModes[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    ///     Restores the previously saved private mode states.
    /// </summary>
    public void RestorePrivateModes()
    {
        foreach (var kvp in _savedPrivateModes)
        {
            SetPrivateMode(kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    ///     Resets all modes to their default values.
    /// </summary>
    public void Reset()
    {
        _modes.Clear();
        _privateModes.Clear();
        _savedModes.Clear();
        _savedPrivateModes.Clear();
        
        // Reset mode properties to defaults
        AutoWrapMode = true;
        ApplicationCursorKeys = false;
        BracketedPasteMode = false;
        CursorVisible = true;
        OriginMode = false;
        Utf8Mode = true;
    }
}