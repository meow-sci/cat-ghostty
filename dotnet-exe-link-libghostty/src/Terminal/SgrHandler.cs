using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttySgr;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Safe handle for managing GhosttySgrParser lifecycle
/// </summary>
sealed class GhosttySgrParserHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private GhosttySgrParserHandle() : base(true) { }

    public GhosttySgrParserHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        GhosttySgr.ghostty_sgr_free(handle);
        return true;
    }
}

/// <summary>
/// Handles SGR (Select Graphic Rendition) sequences using libghostty-vt.
/// SGR sequences control text attributes like colors, bold, italic, underline.
/// </summary>
public class SgrHandler : IDisposable
{
    private readonly GhosttySgrParserHandle _parser;
    private readonly TerminalState _terminalState;
    private bool _disposed;
    
    public SgrHandler(TerminalState terminalState)
    {
        _terminalState = terminalState;
        
        IntPtr NULL_VALUE = IntPtr.Zero;
        IntPtr parserRaw;
        var res = GhosttySgr.ghostty_sgr_new(NULL_VALUE, out parserRaw);
        
        if (res != GhosttyResult.GHOSTTY_SUCCESS || parserRaw == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create SGR parser: {res}");
        }
        
        _parser = new GhosttySgrParserHandle(parserRaw);
    }
    
    /// <summary>
    /// Parses and applies SGR parameters to the current terminal attributes.
    /// </summary>
    public void Handle(ushort[] parameters, byte[] separators)
    {
        if (_disposed || parameters == null || parameters.Length == 0)
        {
            // Empty SGR (CSI m) means reset all attributes
            if (parameters == null || parameters.Length == 0)
            {
                ResetAttributes();
            }
            return;
        }
        
        try
        {
            // Reset parser
            GhosttySgr.ghostty_sgr_reset(_parser.DangerousGetHandle());
            
            // Set parameters
            var res = GhosttySgr.ghostty_sgr_set_params(
                _parser.DangerousGetHandle(),
                parameters,
                separators,
                new UIntPtr((uint)parameters.Length)
            );
            
            if (res != GhosttyResult.GHOSTTY_SUCCESS)
            {
                return;
            }
            
            // Process all attributes
            while (ghostty_sgr_next(_parser.DangerousGetHandle(), out GhosttySgrAttribute attr))
            {
                ApplyAttribute(attr);
            }
        }
        catch (Exception)
        {
            // Silently ignore parser errors in MVP
        }
    }
    
    private void ApplyAttribute(GhosttySgrAttribute attr)
    {
        var current = _terminalState.CurrentAttributes;
        
        switch (attr.tag)
        {
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BOLD:
                current.Bold = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BOLD:
                current.Bold = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FAINT:
                current.Faint = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_ITALIC:
                current.Italic = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_ITALIC:
                current.Italic = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE:
                current.Underline = attr.value.underline;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_UNDERLINE:
                current.Underline = GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_NONE;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR:
                current.UnderlineColor = attr.value.underline_color;
                current.UnderlineColor256 = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR_256:
                current.UnderlineColor256 = attr.value.underline_color_256;
                current.UnderlineColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_UNDERLINE_COLOR:
                current.UnderlineColor = null;
                current.UnderlineColor256 = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_OVERLINE:
                current.Overline = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_OVERLINE:
                current.Overline = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BLINK:
                current.Blink = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BLINK:
                current.Blink = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_INVERSE:
                current.Inverse = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_INVERSE:
                current.Inverse = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_INVISIBLE:
                current.Invisible = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_INVISIBLE:
                current.Invisible = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_STRIKETHROUGH:
                current.Strikethrough = true;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_STRIKETHROUGH:
                current.Strikethrough = false;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_FG:
                current.FgRgbColor = attr.value.direct_color_fg;
                current.Fg8Color = null;
                current.Fg256Color = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_BG:
                current.BgRgbColor = attr.value.direct_color_bg;
                current.Bg8Color = null;
                current.Bg256Color = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_8:
                current.Fg8Color = attr.value.fg_8;
                current.Fg256Color = null;
                current.FgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_8:
                current.Bg8Color = attr.value.bg_8;
                current.Bg256Color = null;
                current.BgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_FG_8:
                current.Fg8Color = (byte)(attr.value.bright_fg_8 + 8); // Bright colors are 8-15
                current.Fg256Color = null;
                current.FgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_BG_8:
                current.Bg8Color = (byte)(attr.value.bright_bg_8 + 8);
                current.Bg256Color = null;
                current.BgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_256:
                current.Fg256Color = attr.value.fg_256;
                current.Fg8Color = null;
                current.FgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_256:
                current.Bg256Color = attr.value.bg_256;
                current.Bg8Color = null;
                current.BgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_FG:
                current.Fg8Color = null;
                current.Fg256Color = null;
                current.FgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BG:
                current.Bg8Color = null;
                current.Bg256Color = null;
                current.BgRgbColor = null;
                break;
                
            case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNSET:
                ResetAttributes();
                break;
        }
        
        _terminalState.CurrentAttributes = current;
    }
    
    private void ResetAttributes()
    {
        _terminalState.CurrentAttributes = new TerminalCell();
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _parser?.Dispose();
            _disposed = true;
        }
    }
}
