using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttyOsc;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Safe handle for managing GhosttyOscParser lifecycle
/// </summary>
sealed class GhosttyOscParserHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private GhosttyOscParserHandle() : base(true) { }

    public GhosttyOscParserHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        GhosttyOsc.ghostty_osc_free(handle);
        return true;
    }
}

/// <summary>
/// Handles OSC (Operating System Command) sequences using libghostty-vt.
/// OSC sequences control window title, clipboard, colors, and other terminal features.
/// </summary>
public class OscHandler : IDisposable
{
    private readonly GhosttyOscParserHandle _parser;
    private readonly TerminalState _terminalState;
    private bool _disposed;
    
    public OscHandler(TerminalState terminalState)
    {
        _terminalState = terminalState;
        
        IntPtr NULL_VALUE = IntPtr.Zero;
        IntPtr parserRaw;
        var res = GhosttyOsc.ghostty_osc_new(NULL_VALUE, out parserRaw);
        
        if (res != GhosttyResult.GHOSTTY_SUCCESS || parserRaw == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create OSC parser: {res}");
        }
        
        _parser = new GhosttyOscParserHandle(parserRaw);
    }
    
    /// <summary>
    /// Parses and handles an OSC sequence string.
    /// The string should not include the OSC introducer or terminator.
    /// </summary>
    public void Handle(string oscString)
    {
        if (_disposed || string.IsNullOrEmpty(oscString))
            return;
        
        try
        {
            // Reset parser for new sequence
            GhosttyOsc.ghostty_osc_reset(_parser.DangerousGetHandle());
            
            // Feed bytes to parser
            byte[] bytes = Encoding.UTF8.GetBytes(oscString);
            foreach (byte b in bytes)
            {
                GhosttyOsc.ghostty_osc_next(_parser.DangerousGetHandle(), b);
            }
            
            // Finalize parsing (BEL terminator)
            IntPtr command = GhosttyOsc.ghostty_osc_end(_parser.DangerousGetHandle(), 0x07);
            if (command == IntPtr.Zero)
            {
                return; // Failed to parse
            }
            
            // Get command type and handle accordingly
            var type = GhosttyOsc.ghostty_osc_command_type(command);
            
            switch (type)
            {
                case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE:
                    HandleWindowTitle(command);
                    break;
                    
                case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_ICON:
                    // Not implemented in MVP
                    break;
                    
                case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_CLIPBOARD_CONTENTS:
                    // Not implemented in MVP (requires clipboard access)
                    break;
                    
                case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_REPORT_PWD:
                    // Not implemented in MVP (would need to store PWD)
                    break;
                    
                case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_COLOR_OPERATION:
                    // Not implemented in MVP (would need color palette management)
                    break;
                    
                default:
                    // Unknown or unsupported command - ignore
                    break;
            }
        }
        catch (Exception)
        {
            // Silently ignore parser errors in MVP
        }
    }
    
    private void HandleWindowTitle(IntPtr command)
    {
        if (GhosttyOsc.ghostty_osc_command_data(
            command,
            GhosttyOscCommandData.GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR,
            out IntPtr titlePtr))
        {
            var title = Marshal.PtrToStringAnsi(titlePtr);
            if (!string.IsNullOrEmpty(title))
            {
                _terminalState.WindowTitle = title;
            }
        }
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
