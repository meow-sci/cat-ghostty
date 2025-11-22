
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
  public static int Run(string[] args)
  {
    Console.WriteLine("=== Ghostty Key Encoder Demo ===");

    IntPtr raw;
    GhosttyResult res = GhosttyKey.ghostty_key_encoder_new(IntPtr.Zero, out raw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_encoder_new failed: {res}");
    using var encoder = new GhosttyKeyEncoderHandle(raw);

    Console.WriteLine("✓ Created key encoder successfully");

    // Enable Kitty keyboard protocol with all features
    byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_ALL;
    // byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_DISABLED;
    Console.WriteLine($"Setting Kitty flags to {kittyFlags}");
    unsafe
    {
      var flags = 31;
      ghostty_key_encoder_setopt(
        encoder.DangerousGetHandle(),
        GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS,
        new IntPtr(&kittyFlags)
        // new IntPtr(&flags)
      );
    }
    Console.WriteLine("✓ Enabled Kitty keyboard protocol");

    // Create key event for Ctrl+C press
    IntPtr keyEventRaw;
    res = GhosttyKey.ghostty_key_event_new(IntPtr.Zero, out keyEventRaw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_event_new failed: {res}");
    using var keyEvent = new GhosttyKeyEventHandle(keyEventRaw);

    Console.WriteLine("✓ Created key event");

    // Configure key event for Ctrl+C
    GhosttyKey.ghostty_key_event_set_action(keyEvent.DangerousGetHandle(), GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
    GhosttyKey.ghostty_key_event_set_key(keyEvent.DangerousGetHandle(), GhosttyKeyCode.GHOSTTY_KEY_C);
    // GhosttyKey.ghostty_key_event_set_mods(keyEvent.DangerousGetHandle(), GhosttyMods.GHOSTTY_MODS_CTRL);
    GhosttyKey.ghostty_key_event_set_mods(keyEvent.DangerousGetHandle(), 0);
    // GhosttyKey.ghostty_key_event_set_action(keyEventRaw, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
    // GhosttyKey.ghostty_key_event_set_key(keyEventRaw, GhosttyKeyCode.GHOSTTY_KEY_C);
    // GhosttyKey.ghostty_key_event_set_key(keyEventRaw, GhosttyKeyCode.GHOSTTY_KEY_Z);
    // GhosttyKey.ghostty_key_event_set_key(keyEventRaw, GhosttyKeyCode.GHOSTTY_KEY_V);
    // GhosttyKey.ghostty_key_event_set_mods(keyEventRaw, GhosttyMods.GHOSTTY_MODS_SHIFT);
    // GhosttyKey.ghostty_key_event_set_key(keyEventRaw, GhosttyKeyCode.GHOSTTY_KEY_Z);
    // Console.WriteLine($"GhosttyMods.GHOSTTY_MODS_CTRL is {(int)GhosttyMods.GHOSTTY_MODS_CTRL}");
    // GhosttyKey.ghostty_key_event_set_mods(keyEventRaw, GhosttyMods.GHOSTTY_MODS_CTRL);
    // ushort mods = 2; // Ctrl
    // GhosttyKey.ghostty_key_event_set_mods(keyEventRaw,  (GhosttyMods) 2);
    // GhosttyKey.ghostty_key_event_set_mods(keyEventRaw,  (GhosttyMods) 0);

    // // Set UTF-8 text for the key (Ctrl+C typically produces ETX character 0x03)
    // byte[] utf8Text = Encoding.UTF8.GetBytes("\x03");
    byte[] utf8Text = Encoding.UTF8.GetBytes("c");
    unsafe
    {
      fixed (byte* textPtr = utf8Text)
      {
        GhosttyKey.ghostty_key_event_set_utf8(
          keyEvent.DangerousGetHandle(), 
          new IntPtr(textPtr), 
          new UIntPtr((uint)utf8Text.Length)
        );
      }
    }

    // Encode the key event
    byte[] buffer = new byte[128];
    UIntPtr writtenSize;
    unsafe
    {
      fixed (byte* bufPtrRaw = buffer)
      {
        Console.WriteLine($"✓ before 1 len={buffer.Length}");

        var bufPtr = new IntPtr(bufPtrRaw);

        res = GhosttyKey.ghostty_key_encoder_encode(
          encoder.DangerousGetHandle(),
          keyEvent.DangerousGetHandle(),
          bufPtr,
          (UIntPtr)buffer.Length,
          out writtenSize
        );
      }
    }

    Console.WriteLine($"✓ Encoded key event, written len: {writtenSize}");


    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_encoder_encode failed: {res}");

    Console.WriteLine($"✓ after");

    // Display the encoded sequence
    int written = (int)writtenSize.ToUInt32();
    Console.WriteLine($"✓ Encoded Ctrl+C to {written} bytes:");
    Console.Write("  Hex: ");
    for (int i = 0; i < written; i++)
    {
      Console.Write($"{buffer[i]:X2} ");
    }
    Console.WriteLine();
    Console.Write("  ASCII: ");
    Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, written).Replace("\x1b", "ESC"));

    // Cleanup
    ghostty_key_event_free(keyEvent.DangerousGetHandle());
    ghostty_key_encoder_free(encoder.DangerousGetHandle());

    Console.WriteLine("\n✓ Demo completed successfully");
    return 0;
  }
}
