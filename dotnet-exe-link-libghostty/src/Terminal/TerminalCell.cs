using static dotnet_exe_link_libghostty.GhosttySgr;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Represents a single cell in the terminal screen buffer.
/// Contains a character and all associated text attributes (colors, styles).
/// </summary>
public struct TerminalCell
{
    /// <summary>
    /// The character displayed in this cell. Default is space.
    /// </summary>
    public char Character { get; set; }
    
    /// <summary>
    /// Foreground color (8-color palette index). Null means default.
    /// </summary>
    public byte? Fg8Color { get; set; }
    
    /// <summary>
    /// Background color (8-color palette index). Null means default.
    /// </summary>
    public byte? Bg8Color { get; set; }
    
    /// <summary>
    /// Foreground color (256-color palette index). Null means default.
    /// </summary>
    public byte? Fg256Color { get; set; }
    
    /// <summary>
    /// Background color (256-color palette index). Null means default.
    /// </summary>
    public byte? Bg256Color { get; set; }
    
    /// <summary>
    /// Foreground color (RGB). Null means default.
    /// </summary>
    public GhosttyColorRgb? FgRgbColor { get; set; }
    
    /// <summary>
    /// Background color (RGB). Null means default.
    /// </summary>
    public GhosttyColorRgb? BgRgbColor { get; set; }
    
    /// <summary>
    /// Text is bold/bright.
    /// </summary>
    public bool Bold { get; set; }
    
    /// <summary>
    /// Text is faint/dim.
    /// </summary>
    public bool Faint { get; set; }
    
    /// <summary>
    /// Text is italic.
    /// </summary>
    public bool Italic { get; set; }
    
    /// <summary>
    /// Underline style.
    /// </summary>
    public GhosttySgrUnderline Underline { get; set; }
    
    /// <summary>
    /// Underline color (RGB). Null means default.
    /// </summary>
    public GhosttyColorRgb? UnderlineColor { get; set; }
    
    /// <summary>
    /// Underline color (256-color palette). Null means default.
    /// </summary>
    public byte? UnderlineColor256 { get; set; }
    
    /// <summary>
    /// Text has overline.
    /// </summary>
    public bool Overline { get; set; }
    
    /// <summary>
    /// Text is blinking.
    /// </summary>
    public bool Blink { get; set; }
    
    /// <summary>
    /// Text has inverse video (swap fg/bg).
    /// </summary>
    public bool Inverse { get; set; }
    
    /// <summary>
    /// Text is invisible/hidden.
    /// </summary>
    public bool Invisible { get; set; }
    
    /// <summary>
    /// Text has strikethrough.
    /// </summary>
    public bool Strikethrough { get; set; }
    
    /// <summary>
    /// Creates a new terminal cell with default attributes (space character).
    /// </summary>
    public TerminalCell()
    {
        Character = ' ';
        Underline = GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_NONE;
    }
    
    /// <summary>
    /// Creates a copy of the cell with a different character.
    /// </summary>
    public TerminalCell WithCharacter(char ch)
    {
        var copy = this;
        copy.Character = ch;
        return copy;
    }
}
