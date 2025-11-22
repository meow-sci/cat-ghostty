using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttyKey;

namespace dotnet_exe_link_libghostty.Terminal;

/// <summary>
/// Safe handle for managing GhosttyKeyEncoder lifecycle
/// </summary>
sealed class GhosttyKeyEncoderHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private GhosttyKeyEncoderHandle() : base(true) { }

    public GhosttyKeyEncoderHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        GhosttyKey.ghostty_key_encoder_free(handle);
        return true;
    }
}

/// <summary>
/// Safe handle for managing GhosttyKeyEvent lifecycle
/// </summary>
sealed class GhosttyKeyEventHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    private GhosttyKeyEventHandle() : base(true) { }

    public GhosttyKeyEventHandle(IntPtr handle) : base(true)
    {
        SetHandle(handle);
    }

    protected override bool ReleaseHandle()
    {
        GhosttyKey.ghostty_key_event_free(handle);
        return true;
    }
}

/// <summary>
/// Handles keyboard input from the console and encodes it to terminal sequences
/// using libghostty-vt's key encoder with Kitty keyboard protocol.
/// </summary>
public class InputController : IDisposable
{
    private readonly GhosttyKeyEncoderHandle _encoder;
    private bool _disposed;
    
    public InputController()
    {
        IntPtr NULL_VALUE = IntPtr.Zero;
        IntPtr encoderRaw;
        var res = GhosttyKey.ghostty_key_encoder_new(NULL_VALUE, out encoderRaw);
        
        if (res != GhosttyResult.GHOSTTY_SUCCESS || encoderRaw == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create key encoder: {res}");
        }
        
        _encoder = new GhosttyKeyEncoderHandle(encoderRaw);
        
        // Configure encoder with Kitty protocol flags
        unsafe
        {
            byte flags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_DISAMBIGUATE;
            GhosttyKey.ghostty_key_encoder_setopt(
                _encoder.DangerousGetHandle(),
                GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS,
                new IntPtr(&flags)
            );
        }
    }
    
    /// <summary>
    /// Reads a key from the console and encodes it to a terminal sequence.
    /// Returns null if no input is available or encoding fails.
    /// </summary>
    public byte[]? ReadAndEncodeKey()
    {
        if (_disposed)
            return null;
        
        if (!Console.KeyAvailable)
            return null;
        
        var keyInfo = Console.ReadKey(intercept: true);
        return EncodeKey(keyInfo);
    }
    
    /// <summary>
    /// Encodes a ConsoleKeyInfo to a terminal escape sequence.
    /// </summary>
    private byte[]? EncodeKey(ConsoleKeyInfo keyInfo)
    {
        IntPtr NULL_VALUE = IntPtr.Zero;
        
        // Create key event
        IntPtr keyEventRaw;
        var res = GhosttyKey.ghostty_key_event_new(NULL_VALUE, out keyEventRaw);
        if (res != GhosttyResult.GHOSTTY_SUCCESS)
            return null;
        
        using var keyEvent = new GhosttyKeyEventHandle(keyEventRaw);
        
        // Set action to PRESS
        GhosttyKey.ghostty_key_event_set_action(keyEvent.DangerousGetHandle(), GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
        
        // Convert ConsoleKey to GhosttyKeyCode
        var keyCode = ConvertToGhosttyKeyCode(keyInfo.Key);
        GhosttyKey.ghostty_key_event_set_key(keyEvent.DangerousGetHandle(), keyCode);
        
        // Convert modifiers
        var mods = ConvertModifiers(keyInfo.Modifiers);
        GhosttyKey.ghostty_key_event_set_mods(keyEvent.DangerousGetHandle(), mods);
        
        // Set UTF-8 text if printable character
        if (keyInfo.KeyChar != '\0')
        {
            byte[] utf8bytes = Encoding.UTF8.GetBytes(new[] { keyInfo.KeyChar });
            var len = (uint)utf8bytes.Length;
            unsafe
            {
                fixed (byte* textPtr = utf8bytes)
                {
                    GhosttyKey.ghostty_key_event_set_utf8(
                        keyEvent.DangerousGetHandle(),
                        new IntPtr(textPtr),
                        new UIntPtr(len)
                    );
                }
            }
        }
        
        // Set unshifted codepoint
        uint unshiftedCodepoint = DeriveUnshiftedCodepoint(keyCode);
        if (unshiftedCodepoint != 0)
        {
            GhosttyKey.ghostty_key_event_set_unshifted_codepoint(keyEvent.DangerousGetHandle(), unshiftedCodepoint);
        }
        
        // Encode the key event
        UIntPtr requiredSize;
        res = GhosttyKey.ghostty_key_encoder_encode(
            _encoder.DangerousGetHandle(),
            keyEvent.DangerousGetHandle(),
            NULL_VALUE,
            UIntPtr.Zero,
            out requiredSize
        );
        
        byte[] buffer;
        UIntPtr writtenSize;
        
        if (res == GhosttyResult.GHOSTTY_SUCCESS)
        {
            buffer = new byte[0];
            writtenSize = requiredSize;
        }
        else if (res == GhosttyResult.GHOSTTY_OUT_OF_MEMORY)
        {
            buffer = new byte[(int)requiredSize.ToUInt32()];
            unsafe
            {
                fixed (byte* bufPtrRaw = buffer)
                {
                    var bufPtr = new IntPtr(bufPtrRaw);
                    res = GhosttyKey.ghostty_key_encoder_encode(
                        _encoder.DangerousGetHandle(),
                        keyEvent.DangerousGetHandle(),
                        bufPtr,
                        requiredSize,
                        out writtenSize
                    );
                }
            }
        }
        else
        {
            return null;
        }
        
        if (res != GhosttyResult.GHOSTTY_SUCCESS)
            return null;
        
        int written = (int)writtenSize.ToUInt32();
        if (written > 0 && written <= buffer.Length)
        {
            var result = new byte[written];
            Array.Copy(buffer, result, written);
            return result;
        }
        
        return null;
    }
    
    private GhosttyKeyCode ConvertToGhosttyKeyCode(ConsoleKey key)
    {
        return key switch
        {
            ConsoleKey.A => GhosttyKeyCode.GHOSTTY_KEY_A,
            ConsoleKey.B => GhosttyKeyCode.GHOSTTY_KEY_B,
            ConsoleKey.C => GhosttyKeyCode.GHOSTTY_KEY_C,
            ConsoleKey.D => GhosttyKeyCode.GHOSTTY_KEY_D,
            ConsoleKey.E => GhosttyKeyCode.GHOSTTY_KEY_E,
            ConsoleKey.F => GhosttyKeyCode.GHOSTTY_KEY_F,
            ConsoleKey.G => GhosttyKeyCode.GHOSTTY_KEY_G,
            ConsoleKey.H => GhosttyKeyCode.GHOSTTY_KEY_H,
            ConsoleKey.I => GhosttyKeyCode.GHOSTTY_KEY_I,
            ConsoleKey.J => GhosttyKeyCode.GHOSTTY_KEY_J,
            ConsoleKey.K => GhosttyKeyCode.GHOSTTY_KEY_K,
            ConsoleKey.L => GhosttyKeyCode.GHOSTTY_KEY_L,
            ConsoleKey.M => GhosttyKeyCode.GHOSTTY_KEY_M,
            ConsoleKey.N => GhosttyKeyCode.GHOSTTY_KEY_N,
            ConsoleKey.O => GhosttyKeyCode.GHOSTTY_KEY_O,
            ConsoleKey.P => GhosttyKeyCode.GHOSTTY_KEY_P,
            ConsoleKey.Q => GhosttyKeyCode.GHOSTTY_KEY_Q,
            ConsoleKey.R => GhosttyKeyCode.GHOSTTY_KEY_R,
            ConsoleKey.S => GhosttyKeyCode.GHOSTTY_KEY_S,
            ConsoleKey.T => GhosttyKeyCode.GHOSTTY_KEY_T,
            ConsoleKey.U => GhosttyKeyCode.GHOSTTY_KEY_U,
            ConsoleKey.V => GhosttyKeyCode.GHOSTTY_KEY_V,
            ConsoleKey.W => GhosttyKeyCode.GHOSTTY_KEY_W,
            ConsoleKey.X => GhosttyKeyCode.GHOSTTY_KEY_X,
            ConsoleKey.Y => GhosttyKeyCode.GHOSTTY_KEY_Y,
            ConsoleKey.Z => GhosttyKeyCode.GHOSTTY_KEY_Z,
            ConsoleKey.D0 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_0,
            ConsoleKey.D1 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_1,
            ConsoleKey.D2 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_2,
            ConsoleKey.D3 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_3,
            ConsoleKey.D4 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_4,
            ConsoleKey.D5 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_5,
            ConsoleKey.D6 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_6,
            ConsoleKey.D7 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_7,
            ConsoleKey.D8 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_8,
            ConsoleKey.D9 => GhosttyKeyCode.GHOSTTY_KEY_DIGIT_9,
            ConsoleKey.Spacebar => GhosttyKeyCode.GHOSTTY_KEY_SPACE,
            ConsoleKey.Enter => GhosttyKeyCode.GHOSTTY_KEY_ENTER,
            ConsoleKey.Escape => GhosttyKeyCode.GHOSTTY_KEY_ESCAPE,
            ConsoleKey.Backspace => GhosttyKeyCode.GHOSTTY_KEY_BACKSPACE,
            ConsoleKey.Tab => GhosttyKeyCode.GHOSTTY_KEY_TAB,
            ConsoleKey.Delete => GhosttyKeyCode.GHOSTTY_KEY_DELETE,
            ConsoleKey.Home => GhosttyKeyCode.GHOSTTY_KEY_HOME,
            ConsoleKey.End => GhosttyKeyCode.GHOSTTY_KEY_END,
            ConsoleKey.PageUp => GhosttyKeyCode.GHOSTTY_KEY_PAGE_UP,
            ConsoleKey.PageDown => GhosttyKeyCode.GHOSTTY_KEY_PAGE_DOWN,
            ConsoleKey.UpArrow => GhosttyKeyCode.GHOSTTY_KEY_ARROW_UP,
            ConsoleKey.DownArrow => GhosttyKeyCode.GHOSTTY_KEY_ARROW_DOWN,
            ConsoleKey.LeftArrow => GhosttyKeyCode.GHOSTTY_KEY_ARROW_LEFT,
            ConsoleKey.RightArrow => GhosttyKeyCode.GHOSTTY_KEY_ARROW_RIGHT,
            ConsoleKey.F1 => GhosttyKeyCode.GHOSTTY_KEY_F1,
            ConsoleKey.F2 => GhosttyKeyCode.GHOSTTY_KEY_F2,
            ConsoleKey.F3 => GhosttyKeyCode.GHOSTTY_KEY_F3,
            ConsoleKey.F4 => GhosttyKeyCode.GHOSTTY_KEY_F4,
            ConsoleKey.F5 => GhosttyKeyCode.GHOSTTY_KEY_F5,
            ConsoleKey.F6 => GhosttyKeyCode.GHOSTTY_KEY_F6,
            ConsoleKey.F7 => GhosttyKeyCode.GHOSTTY_KEY_F7,
            ConsoleKey.F8 => GhosttyKeyCode.GHOSTTY_KEY_F8,
            ConsoleKey.F9 => GhosttyKeyCode.GHOSTTY_KEY_F9,
            ConsoleKey.F10 => GhosttyKeyCode.GHOSTTY_KEY_F10,
            ConsoleKey.F11 => GhosttyKeyCode.GHOSTTY_KEY_F11,
            ConsoleKey.F12 => GhosttyKeyCode.GHOSTTY_KEY_F12,
            ConsoleKey.Insert => GhosttyKeyCode.GHOSTTY_KEY_INSERT,
            ConsoleKey.OemMinus => GhosttyKeyCode.GHOSTTY_KEY_MINUS,
            ConsoleKey.OemPlus => GhosttyKeyCode.GHOSTTY_KEY_EQUAL,
            ConsoleKey.Oem4 => GhosttyKeyCode.GHOSTTY_KEY_BRACKET_LEFT,
            ConsoleKey.Oem6 => GhosttyKeyCode.GHOSTTY_KEY_BRACKET_RIGHT,
            ConsoleKey.Oem5 => GhosttyKeyCode.GHOSTTY_KEY_BACKSLASH,
            ConsoleKey.Oem1 => GhosttyKeyCode.GHOSTTY_KEY_SEMICOLON,
            ConsoleKey.Oem7 => GhosttyKeyCode.GHOSTTY_KEY_QUOTE,
            ConsoleKey.OemComma => GhosttyKeyCode.GHOSTTY_KEY_COMMA,
            ConsoleKey.OemPeriod => GhosttyKeyCode.GHOSTTY_KEY_PERIOD,
            ConsoleKey.Oem2 => GhosttyKeyCode.GHOSTTY_KEY_SLASH,
            ConsoleKey.Oem3 => GhosttyKeyCode.GHOSTTY_KEY_BACKQUOTE,
            _ => GhosttyKeyCode.GHOSTTY_KEY_UNIDENTIFIED
        };
    }
    
    private GhosttyMods ConvertModifiers(ConsoleModifiers modifiers)
    {
        GhosttyMods mods = 0;
        
        if ((modifiers & ConsoleModifiers.Shift) != 0)
            mods |= GhosttyMods.GHOSTTY_MODS_SHIFT;
        
        if ((modifiers & ConsoleModifiers.Control) != 0)
            mods |= GhosttyMods.GHOSTTY_MODS_CTRL;
        
        if ((modifiers & ConsoleModifiers.Alt) != 0)
            mods |= GhosttyMods.GHOSTTY_MODS_ALT;
        
        return mods;
    }
    
    private uint DeriveUnshiftedCodepoint(GhosttyKeyCode keyCode)
    {
        // Letter keys (A-Z) -> lowercase letters (a-z)
        if (keyCode >= GhosttyKeyCode.GHOSTTY_KEY_A && keyCode <= GhosttyKeyCode.GHOSTTY_KEY_Z)
        {
            int offset = keyCode - GhosttyKeyCode.GHOSTTY_KEY_A;
            return (uint)('a' + offset);
        }

        // Digit keys (0-9) -> the digit itself
        if (keyCode >= GhosttyKeyCode.GHOSTTY_KEY_DIGIT_0 && keyCode <= GhosttyKeyCode.GHOSTTY_KEY_DIGIT_9)
        {
            int offset = keyCode - GhosttyKeyCode.GHOSTTY_KEY_DIGIT_0;
            return (uint)('0' + offset);
        }

        // Symbol keys -> unshifted character
        return keyCode switch
        {
            GhosttyKeyCode.GHOSTTY_KEY_SPACE => (uint)' ',
            GhosttyKeyCode.GHOSTTY_KEY_MINUS => (uint)'-',
            GhosttyKeyCode.GHOSTTY_KEY_EQUAL => (uint)'=',
            GhosttyKeyCode.GHOSTTY_KEY_BRACKET_LEFT => (uint)'[',
            GhosttyKeyCode.GHOSTTY_KEY_BRACKET_RIGHT => (uint)']',
            GhosttyKeyCode.GHOSTTY_KEY_BACKSLASH => (uint)'\\',
            GhosttyKeyCode.GHOSTTY_KEY_SEMICOLON => (uint)';',
            GhosttyKeyCode.GHOSTTY_KEY_QUOTE => (uint)'\'',
            GhosttyKeyCode.GHOSTTY_KEY_BACKQUOTE => (uint)'`',
            GhosttyKeyCode.GHOSTTY_KEY_COMMA => (uint)',',
            GhosttyKeyCode.GHOSTTY_KEY_PERIOD => (uint)'.',
            GhosttyKeyCode.GHOSTTY_KEY_SLASH => (uint)'/',
            _ => 0
        };
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            _encoder?.Dispose();
            _disposed = true;
        }
    }
}
