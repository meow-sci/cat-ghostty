using System;
using System.Collections.Generic;

namespace caTTY.Core.Types;

/// <summary>
/// Character set designation keys for G0, G1, G2, G3.
/// </summary>
public enum CharacterSetKey
{
    /// <summary>
    /// G0 character set (primary).
    /// </summary>
    G0,
    
    /// <summary>
    /// G1 character set (secondary).
    /// </summary>
    G1,
    
    /// <summary>
    /// G2 character set (tertiary).
    /// </summary>
    G2,
    
    /// <summary>
    /// G3 character set (quaternary).
    /// </summary>
    G3
}

/// <summary>
/// Character set state for terminal emulation.
/// </summary>
public class CharacterSetState
{
    /// <summary>
    /// G0 character set identifier.
    /// </summary>
    public string G0 { get; set; } = "B"; // ASCII

    /// <summary>
    /// G1 character set identifier.
    /// </summary>
    public string G1 { get; set; } = "B"; // ASCII

    /// <summary>
    /// G2 character set identifier.
    /// </summary>
    public string G2 { get; set; } = "B"; // ASCII

    /// <summary>
    /// G3 character set identifier.
    /// </summary>
    public string G3 { get; set; } = "B"; // ASCII

    /// <summary>
    /// Currently active character set.
    /// </summary>
    public CharacterSetKey Current { get; set; } = CharacterSetKey.G0;
}

/// <summary>
/// Window properties for terminal title and icon management.
/// </summary>
public class WindowProperties
{
    /// <summary>
    /// Current window title.
    /// </summary>
    public string Title { get; set; } = "";

    /// <summary>
    /// Current icon name.
    /// </summary>
    public string IconName { get; set; } = "";
}
/// <summary>
/// Manages terminal state including cursor position, modes, and attributes.
/// This class tracks all the state needed for terminal emulation including
/// cursor position, terminal modes, SGR attributes, and scroll regions.
/// </summary>
public class TerminalState
{
    /// <summary>
    /// Current cursor X position (column, 0-based).
    /// </summary>
    public int CursorX { get; set; }

    /// <summary>
    /// Current cursor Y position (row, 0-based).
    /// </summary>
    public int CursorY { get; set; }

    /// <summary>
    /// Saved cursor position for ESC 7 / ESC 8 sequences.
    /// </summary>
    public (int X, int Y)? SavedCursor { get; set; }

    /// <summary>
    /// Cursor style (1 = block, 2 = underline, etc.).
    /// </summary>
    public int CursorStyle { get; set; } = 1;

    /// <summary>
    /// Whether the cursor is visible.
    /// </summary>
    public bool CursorVisible { get; set; } = true;

    /// <summary>
    /// Wrap pending state for line overflow handling.
    /// When true, the next printable character will trigger a line wrap.
    /// </summary>
    public bool WrapPending { get; set; }

    /// <summary>
    /// Application cursor keys mode.
    /// When true, arrow keys send different escape sequences.
    /// </summary>
    public bool ApplicationCursorKeys { get; set; }

    /// <summary>
    /// Origin mode - when true, cursor positioning is relative to scroll region.
    /// </summary>
    public bool OriginMode { get; set; }

    /// <summary>
    /// Auto-wrap mode - when true, cursor wraps to next line at right edge.
    /// </summary>
    public bool AutoWrapMode { get; set; } = true;

    /// <summary>
    /// Top of scroll region (0-based, inclusive).
    /// </summary>
    public int ScrollTop { get; set; }

    /// <summary>
    /// Bottom of scroll region (0-based, inclusive).
    /// </summary>
    public int ScrollBottom { get; set; }

    /// <summary>
    /// Tab stops array. True indicates a tab stop at that column.
    /// </summary>
    public bool[] TabStops { get; set; } = Array.Empty<bool>();

    /// <summary>
    /// Window properties (title, icon name).
    /// </summary>
    public WindowProperties WindowProperties { get; set; } = new();

    /// <summary>
    /// Title stack for push/pop operations.
    /// </summary>
    public List<string> TitleStack { get; set; } = new();

    /// <summary>
    /// Icon name stack for push/pop operations.
    /// </summary>
    public List<string> IconNameStack { get; set; } = new();

    /// <summary>
    /// Character set state.
    /// </summary>
    public CharacterSetState CharacterSets { get; set; } = new();

    /// <summary>
    /// UTF-8 mode enabled.
    /// </summary>
    public bool Utf8Mode { get; set; } = true;

    /// <summary>
    /// Saved private modes for XTSAVE/XTRESTORE.
    /// </summary>
    public Dictionary<int, bool> SavedPrivateModes { get; set; } = new();

    /// <summary>
    /// Current SGR attributes for new characters.
    /// </summary>
    public SgrAttributes CurrentSgrState { get; set; } = SgrAttributes.Default;

    /// <summary>
    /// Current character protection attribute.
    /// </summary>
    public bool CurrentCharacterProtection { get; set; }

    /// <summary>
    /// Terminal dimensions (columns).
    /// </summary>
    public int Cols { get; set; }

    /// <summary>
    /// Terminal dimensions (rows).
    /// </summary>
    public int Rows { get; set; }

    /// <summary>
    /// Creates a new terminal state with the specified dimensions.
    /// </summary>
    /// <param name="cols">Number of columns</param>
    /// <param name="rows">Number of rows</param>
    public TerminalState(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
        CursorX = 0;
        CursorY = 0;
        ScrollTop = 0;
        ScrollBottom = rows - 1;
        InitializeTabStops(cols);
    }

    /// <summary>
    /// Initializes tab stops at every 8th column.
    /// </summary>
    /// <param name="cols">Number of columns</param>
    private void InitializeTabStops(int cols)
    {
        TabStops = new bool[cols];
        for (int i = 8; i < cols; i += 8)
        {
            TabStops[i] = true;
        }
    }

    /// <summary>
    /// Clamps the cursor position to stay within terminal bounds.
    /// Respects origin mode and scroll region boundaries.
    /// </summary>
    public void ClampCursor()
    {
        CursorX = Math.Max(0, Math.Min(Cols - 1, CursorX));
        
        if (OriginMode)
        {
            CursorY = Math.Max(ScrollTop, Math.Min(ScrollBottom, CursorY));
        }
        else
        {
            CursorY = Math.Max(0, Math.Min(Rows - 1, CursorY));
        }
        
        WrapPending = false;
    }

    /// <summary>
    /// Sets origin mode and homes the cursor.
    /// </summary>
    /// <param name="enable">True to enable origin mode</param>
    public void SetOriginMode(bool enable)
    {
        OriginMode = enable;
        // VT100/xterm behavior: home the cursor when toggling origin mode
        CursorX = 0;
        CursorY = enable ? ScrollTop : 0;
        WrapPending = false;
        ClampCursor();
    }

    /// <summary>
    /// Sets auto-wrap mode.
    /// </summary>
    /// <param name="enable">True to enable auto-wrap mode</param>
    public void SetAutoWrapMode(bool enable)
    {
        AutoWrapMode = enable;
        if (!enable)
        {
            WrapPending = false;
        }
    }

    /// <summary>
    /// Handles cursor positioning when writing beyond the right edge.
    /// Implements wrap pending semantics.
    /// </summary>
    /// <returns>True if a line wrap occurred</returns>
    public bool HandleRightEdgeWrite()
    {
        if (CursorX >= Cols - 1)
        {
            if (AutoWrapMode)
            {
                WrapPending = true;
                return false; // Don't wrap yet, wait for next character
            }
            else
            {
                // Stay at right edge
                CursorX = Cols - 1;
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Handles wrap pending state when writing a printable character.
    /// </summary>
    /// <returns>True if a line wrap was performed</returns>
    public bool HandleWrapPending()
    {
        if (WrapPending)
        {
            WrapPending = false;
            // Move to beginning of next line
            CursorX = 0;
            if (CursorY < Rows - 1)
            {
                CursorY++;
                return true;
            }
            else
            {
                // At bottom - need to scroll (will be handled by caller)
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Advances the cursor after writing a character.
    /// </summary>
    public void AdvanceCursor()
    {
        CursorX++;
        if (CursorX >= Cols)
        {
            HandleRightEdgeWrite();
        }
    }

    /// <summary>
    /// Resets the terminal state to initial values.
    /// </summary>
    public void Reset()
    {
        CursorX = 0;
        CursorY = 0;
        SavedCursor = null;
        CursorStyle = 1;
        CursorVisible = true;
        WrapPending = false;
        ApplicationCursorKeys = false;
        OriginMode = false;
        AutoWrapMode = true;
        ScrollTop = 0;
        ScrollBottom = Rows - 1;
        InitializeTabStops(Cols);
        WindowProperties = new WindowProperties();
        TitleStack.Clear();
        IconNameStack.Clear();
        CharacterSets = new CharacterSetState();
        Utf8Mode = true;
        SavedPrivateModes.Clear();
        CurrentSgrState = SgrAttributes.Default;
        CurrentCharacterProtection = false;
    }
}