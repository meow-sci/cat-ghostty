using System.Text;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Renders the terminal state to the console using ANSI escape sequences.
/// Implements a simple full-screen redraw strategy for MVP.
/// </summary>
public class ConsoleView
{
    private readonly TerminalState _state;
    
    public ConsoleView(TerminalState state)
    {
        _state = state;
    }
    
    /// <summary>
    /// Renders the entire terminal state to the console.
    /// Uses ANSI sequences to position cursor and apply colors/styles.
    /// </summary>
    public void Render()
    {
        var output = new StringBuilder();
        
        // Hide cursor during rendering
        output.Append("\x1b[?25l");
        
        // Move to top-left
        output.Append("\x1b[H");
        
        TerminalCell? lastAttr = null;
        
        for (int row = 0; row < TerminalState.Height; row++)
        {
            for (int col = 0; col < TerminalState.Width; col++)
            {
                var cell = _state.GetCell(row, col);
                
                // Only emit SGR sequences when attributes change
                if (!lastAttr.HasValue || !AttributesEqual(lastAttr.Value, cell))
                {
                    output.Append(BuildSgrSequence(cell));
                    lastAttr = cell;
                }
                
                output.Append(cell.Character);
            }
        }
        
        // Reset attributes
        output.Append("\x1b[0m");
        
        // Position cursor at the terminal cursor location
        output.Append($"\x1b[{_state.CursorRow + 1};{_state.CursorColumn + 1}H");
        
        // Show cursor
        output.Append("\x1b[?25h");
        
        Console.Write(output.ToString());
        Console.Out.Flush();
    }
    
    private bool AttributesEqual(TerminalCell a, TerminalCell b)
    {
        return a.Bold == b.Bold &&
               a.Faint == b.Faint &&
               a.Italic == b.Italic &&
               a.Underline == b.Underline &&
               a.Blink == b.Blink &&
               a.Inverse == b.Inverse &&
               a.Invisible == b.Invisible &&
               a.Strikethrough == b.Strikethrough &&
               a.Overline == b.Overline &&
               a.Fg8Color == b.Fg8Color &&
               a.Bg8Color == b.Bg8Color &&
               a.Fg256Color == b.Fg256Color &&
               a.Bg256Color == b.Bg256Color &&
               Equals(a.FgRgbColor, b.FgRgbColor) &&
               Equals(a.BgRgbColor, b.BgRgbColor) &&
               Equals(a.UnderlineColor, b.UnderlineColor) &&
               a.UnderlineColor256 == b.UnderlineColor256;
    }
    
    private string BuildSgrSequence(TerminalCell cell)
    {
        var sgr = new List<string>();
        
        // Always start with reset if we're at default state
        bool hasAnyAttribute = cell.Bold || cell.Faint || cell.Italic || 
                               cell.Underline != GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_NONE ||
                               cell.Blink || cell.Inverse || cell.Invisible || 
                               cell.Strikethrough || cell.Overline ||
                               cell.Fg8Color.HasValue || cell.Bg8Color.HasValue ||
                               cell.Fg256Color.HasValue || cell.Bg256Color.HasValue ||
                               cell.FgRgbColor.HasValue || cell.BgRgbColor.HasValue;
        
        if (!hasAnyAttribute)
        {
            return "\x1b[0m"; // Reset all
        }
        
        // Text attributes
        if (cell.Bold) sgr.Add("1");
        if (cell.Faint) sgr.Add("2");
        if (cell.Italic) sgr.Add("3");
        
        // Underline
        switch (cell.Underline)
        {
            case GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_SINGLE:
                sgr.Add("4");
                break;
            case GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DOUBLE:
                sgr.Add("4:2");
                break;
            case GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_CURLY:
                sgr.Add("4:3");
                break;
            case GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DOTTED:
                sgr.Add("4:4");
                break;
            case GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DASHED:
                sgr.Add("4:5");
                break;
        }
        
        if (cell.Blink) sgr.Add("5");
        if (cell.Inverse) sgr.Add("7");
        if (cell.Invisible) sgr.Add("8");
        if (cell.Strikethrough) sgr.Add("9");
        
        // Foreground color
        if (cell.FgRgbColor.HasValue)
        {
            var rgb = cell.FgRgbColor.Value;
            sgr.Add($"38;2;{rgb.r};{rgb.g};{rgb.b}");
        }
        else if (cell.Fg256Color.HasValue)
        {
            sgr.Add($"38;5;{cell.Fg256Color.Value}");
        }
        else if (cell.Fg8Color.HasValue)
        {
            byte color = cell.Fg8Color.Value;
            if (color < 8)
                sgr.Add($"{30 + color}");
            else if (color < 16)
                sgr.Add($"{90 + (color - 8)}");
        }
        
        // Background color
        if (cell.BgRgbColor.HasValue)
        {
            var rgb = cell.BgRgbColor.Value;
            sgr.Add($"48;2;{rgb.r};{rgb.g};{rgb.b}");
        }
        else if (cell.Bg256Color.HasValue)
        {
            sgr.Add($"48;5;{cell.Bg256Color.Value}");
        }
        else if (cell.Bg8Color.HasValue)
        {
            byte color = cell.Bg8Color.Value;
            if (color < 8)
                sgr.Add($"{40 + color}");
            else if (color < 16)
                sgr.Add($"{100 + (color - 8)}");
        }
        
        // Underline color
        if (cell.UnderlineColor.HasValue)
        {
            var rgb = cell.UnderlineColor.Value;
            sgr.Add($"58;2;{rgb.r};{rgb.g};{rgb.b}");
        }
        else if (cell.UnderlineColor256.HasValue)
        {
            sgr.Add($"58;5;{cell.UnderlineColor256.Value}");
        }
        
        if (sgr.Count == 0)
            return "\x1b[0m";
        
        return $"\x1b[{string.Join(";", sgr)}m";
    }
    
    /// <summary>
    /// Initializes the console for terminal emulation.
    /// </summary>
    public void Initialize()
    {
        Console.Clear();
        Console.CursorVisible = true;
        
        // Try to set console to support ANSI sequences
        if (OperatingSystem.IsWindows())
        {
            // Enable virtual terminal processing on Windows
            try
            {
                var stdout = GetStdHandle(-11);
                GetConsoleMode(stdout, out uint mode);
                mode |= 0x0004; // ENABLE_VIRTUAL_TERMINAL_PROCESSING
                SetConsoleMode(stdout, mode);
            }
            catch
            {
                // Silently fail if we can't enable VT processing
            }
        }
    }
    
    /// <summary>
    /// Restores the console to normal state.
    /// </summary>
    public void Cleanup()
    {
        Console.Write("\x1b[0m"); // Reset attributes
        Console.Write("\x1b[?25h"); // Show cursor
        Console.Clear();
    }
    
    // Windows API imports for enabling VT processing
    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
    
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}
