
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

    nint NULL_VALUE = (nint)null;
    // nint NULL_VALUE = IntPtr.Zero;

    var utf8text = "c";
    // var utf8text = "\x03";

    GhosttyKeyAction action = GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS;
    GhosttyMods mods = GhosttyMods.GHOSTTY_MODS_CTRL;
    GhosttyKeyCode keyCode = GhosttyKeyCode.GHOSTTY_KEY_C;

    byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_ALL;
    // byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_DISABLED;
    // byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_DISAMBIGUATE;

    Console.WriteLine($"kittyFlags [{kittyFlags}]");


    IntPtr raw;
    GhosttyResult res = GhosttyKey.ghostty_key_encoder_new(NULL_VALUE, out raw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_encoder_new failed: {res}");
    using var encoder = new GhosttyKeyEncoderHandle(raw);

    Console.WriteLine("✓ Created key encoder successfully");

    // Enable Kitty keyboard protocol with all features

    Console.WriteLine($"Setting Kitty flags to {kittyFlags}");
    unsafe
    {
      ghostty_key_encoder_setopt(
        encoder.DangerousGetHandle(),
        GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS,
        new IntPtr(&kittyFlags)
      );
    }
    Console.WriteLine("✓ Enabled Kitty keyboard protocol");


    // Create key event for Ctrl+C press
    IntPtr keyEventRaw;
    res = GhosttyKey.ghostty_key_event_new(NULL_VALUE, out keyEventRaw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS) throw new Exception($"ghostty_key_event_new failed: {res}");
    using var keyEvent = new GhosttyKeyEventHandle(keyEventRaw);

    Console.WriteLine("✓ Created key event");

    // Configure key event for Ctrl+C
    GhosttyKey.ghostty_key_event_set_action(keyEvent.DangerousGetHandle(), action);
    GhosttyKey.ghostty_key_event_set_key(keyEvent.DangerousGetHandle(), keyCode);
    GhosttyKey.ghostty_key_event_set_mods(keyEvent.DangerousGetHandle(), mods);


    // Set UTF-8 text for the key (Ctrl+C typically produces ETX character 0x03)
    byte[] utf8bytes = Encoding.UTF8.GetBytes(utf8text);
    var len = (uint) utf8bytes.Length;
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

    // Encode the key event
    // First, query the required buffer size
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
      // No output needed
      buffer = new byte[0];
      uint required = requiredSize.ToUInt32();
      Console.WriteLine($"requiredSize: {required}");
      writtenSize = requiredSize;
    }
    else if (res == GhosttyResult.GHOSTTY_OUT_OF_MEMORY)
    {
      Console.WriteLine($"out of memory branc, req size {(int)requiredSize.ToUInt32()}");
      // Allocate buffer of required size
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

    Console.WriteLine("\n✓ Demo completed successfully");
    return 0;
  }
}
