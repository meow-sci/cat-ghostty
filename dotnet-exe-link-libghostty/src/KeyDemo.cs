
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttyKey;

namespace dotnet_exe_link_libghostty;


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

public static class KeyDemoProgram
{
  /// <summary>
  /// Derives the unshifted codepoint from a physical key code.
  /// This represents the character that would be produced by pressing the key without modifiers.
  /// Based on the logic from encode.html's getUnshiftedCodepoint function.
  /// </summary>
  private static uint DeriveUnshiftedCodepoint(GhosttyKeyCode keyCode)
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
      
      // Numpad keys (when not in numlock mode, these act like their unshifted versions)
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_0 => (uint)'0',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_1 => (uint)'1',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_2 => (uint)'2',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_3 => (uint)'3',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_4 => (uint)'4',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_5 => (uint)'5',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_6 => (uint)'6',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_7 => (uint)'7',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_8 => (uint)'8',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_9 => (uint)'9',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_DECIMAL => (uint)'.',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_ADD => (uint)'+',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_SUBTRACT => (uint)'-',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_MULTIPLY => (uint)'*',
      GhosttyKeyCode.GHOSTTY_KEY_NUMPAD_DIVIDE => (uint)'/',
      
      // For other keys (function keys, navigation keys, etc.), return 0
      // as they don't have a meaningful unshifted codepoint
      _ => 0
    };
  }

  /// <summary>
  /// Helper to encode a single key event and display the result.
  /// </summary>
  private static void TestKeyEncoding(
    GhosttyKeyEncoderHandle encoder,
    string testName,
    GhosttyKeyAction action,
    GhosttyKeyCode keyCode,
    GhosttyMods mods,
    string utf8Text)
  {
    Console.WriteLine($"\n--- Test: {testName} ---");

    nint NULL_VALUE = (nint)null;

    // Create key event
    IntPtr keyEventRaw;
    var res = GhosttyKey.ghostty_key_event_new(NULL_VALUE, out keyEventRaw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_event_new failed: {res}");
    using var keyEvent = new GhosttyKeyEventHandle(keyEventRaw);

    // Configure key event
    GhosttyKey.ghostty_key_event_set_action(keyEvent.DangerousGetHandle(), action);
    GhosttyKey.ghostty_key_event_set_key(keyEvent.DangerousGetHandle(), keyCode);
    GhosttyKey.ghostty_key_event_set_mods(keyEvent.DangerousGetHandle(), mods);

    // Set UTF-8 text if provided
    if (!string.IsNullOrEmpty(utf8Text))
    {
      byte[] utf8bytes = Encoding.UTF8.GetBytes(utf8Text);
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
      Console.WriteLine($"  Unshifted codepoint: {unshiftedCodepoint} ('{(char)unshiftedCodepoint}')");
      GhosttyKey.ghostty_key_event_set_unshifted_codepoint(keyEvent.DangerousGetHandle(), unshiftedCodepoint);
    }

    // Encode the key event
    UIntPtr requiredSize;
    res = GhosttyKey.ghostty_key_encoder_encode(
      encoder.DangerousGetHandle(),
      keyEvent.DangerousGetHandle(),
      NULL_VALUE,
      nuint.Zero,
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
            encoder.DangerousGetHandle(),
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
      throw new Exception($"Unexpected result from query: {res}");
    }

    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_encoder_encode failed: {res}");

    // Display the encoded sequence
    int written = (int)writtenSize.ToUInt32();
    if (written > 0)
    {
      Console.Write($"  Hex: ");
      for (int i = 0; i < written; i++)
      {
        Console.Write($"{buffer[i]:X2} ");
      }
      Console.WriteLine();
      Console.Write($"  ASCII: ");
      Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, written).Replace("\x1b", "ESC"));
    }
    else
    {
      Console.WriteLine("  (no output - key event doesn't produce escape sequence)");
    }
  }

  public static int Run(string[] args)
  {
    Console.WriteLine("=== Ghostty Key Encoder Demo ===");

    nint NULL_VALUE = (nint)null;
    byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_ALL;

    // Create encoder
    IntPtr raw;
    GhosttyResult res = GhosttyKey.ghostty_key_encoder_new(NULL_VALUE, out raw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_encoder_new failed: {res}");
    using var encoder = new GhosttyKeyEncoderHandle(raw);

    Console.WriteLine("✓ Created key encoder successfully");

    // Enable Kitty keyboard protocol with all features
    Console.WriteLine($"✓ Enabled Kitty keyboard protocol (flags: {kittyFlags})");
    unsafe
    {
      ghostty_key_encoder_setopt(
        encoder.DangerousGetHandle(),
        GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS,
        new IntPtr(&kittyFlags)
      );
    }

    // Test various key combinations
    TestKeyEncoding(encoder, "Ctrl+C", 
      GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS,
      GhosttyKeyCode.GHOSTTY_KEY_C,
      GhosttyMods.GHOSTTY_MODS_CTRL,
      "c");

    TestKeyEncoding(encoder, "Ctrl+Shift+A",
      GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS,
      GhosttyKeyCode.GHOSTTY_KEY_A,
      GhosttyMods.GHOSTTY_MODS_CTRL | GhosttyMods.GHOSTTY_MODS_SHIFT,
      "A");

    TestKeyEncoding(encoder, "Ctrl+3",
      GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS,
      GhosttyKeyCode.GHOSTTY_KEY_DIGIT_3,
      GhosttyMods.GHOSTTY_MODS_CTRL,
      "3");

    TestKeyEncoding(encoder, "Alt+Enter",
      GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS,
      GhosttyKeyCode.GHOSTTY_KEY_ENTER,
      GhosttyMods.GHOSTTY_MODS_ALT,
      "\r");

    TestKeyEncoding(encoder, "Ctrl+[",
      GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS,
      GhosttyKeyCode.GHOSTTY_KEY_BRACKET_LEFT,
      GhosttyMods.GHOSTTY_MODS_CTRL,
      "[");

    Console.WriteLine("\n✓ Demo completed successfully");
    return 0;
  }
}
