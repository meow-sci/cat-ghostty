using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttyOsc;

namespace dotnet_exe_link_libghostty;

/// <summary>
/// Demo program showcasing Ghostty OSC (Operating System Command) parser functionality.
/// 
/// This demonstrates how to:
/// - Create and manage OSC parser instances
/// - Parse various OSC sequences byte-by-byte
/// - Extract command types and associated data
/// - Handle different OSC command types (window title, PWD, clipboard, color operations)
/// - Properly manage parser lifecycle with SafeHandles
/// 
/// Run with: dotnet run -- --osc-demo
/// </summary>
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

public static class OscDemoProgram
{
  /// <summary>
  /// Helper to parse and test a single OSC sequence.
  /// </summary>
  private static void TestOscSequence(
    GhosttyOscParserHandle parser,
    string testName,
    string oscSequence,
    byte terminator = 0x07) // Default to BEL terminator
  {
    Console.WriteLine($"\n--- Test: {testName} ---");
    Console.WriteLine($"  Input: {oscSequence}");

    // Reset parser for new sequence
    GhosttyOsc.ghostty_osc_reset(parser.DangerousGetHandle());

    // Feed bytes to parser
    byte[] bytes = Encoding.UTF8.GetBytes(oscSequence);
    foreach (byte b in bytes)
    {
      GhosttyOsc.ghostty_osc_next(parser.DangerousGetHandle(), b);
    }

    // Finalize parsing
    IntPtr command = GhosttyOsc.ghostty_osc_end(parser.DangerousGetHandle(), terminator);
    if (command == IntPtr.Zero)
    {
      Console.WriteLine("  ✗ Failed to parse OSC sequence (null command)");
      return;
    }

    // Get command type
    var type = GhosttyOsc.ghostty_osc_command_type(command);
    Console.WriteLine($"  Command type: {(int)type} ({type})");

    // Try to extract data based on command type
    switch (type)
    {
      case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE:
        if (GhosttyOsc.ghostty_osc_command_data(
          command,
          GhosttyOscCommandData.GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR,
          out IntPtr titlePtr))
        {
          var extracted = Marshal.PtrToStringAnsi(titlePtr);
          Console.WriteLine($"  ✓ Extracted title: '{extracted}'");
        }
        else
        {
          Console.WriteLine("  ✗ Failed to extract title data");
        }
        break;

      case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_REPORT_PWD:
        Console.WriteLine("  ✓ PWD report command recognized");
        // Note: PWD data extraction would require additional GhosttyOscCommandData enum values
        break;

      case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_CLIPBOARD_CONTENTS:
        Console.WriteLine("  ✓ Clipboard contents command recognized");
        // Note: Clipboard data extraction would require additional GhosttyOscCommandData enum values
        break;

      case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_COLOR_OPERATION:
        Console.WriteLine("  ✓ Color operation command recognized");
        // Note: Color operation data extraction would require additional GhosttyOscCommandData enum values
        break;

      case GhosttyOscCommandType.GHOSTTY_OSC_COMMAND_INVALID:
        Console.WriteLine("  ✗ Invalid OSC command");
        break;

      default:
        Console.WriteLine($"  ✓ Command recognized (no data extraction implemented)");
        break;
    }
  }

  public static int Run(string[] args)
  {
    Console.WriteLine("=== Ghostty OSC Parser Demo ===");

    IntPtr NULL_VALUE = IntPtr.Zero;

    // Create OSC parser
    IntPtr parserRaw;
    var res = GhosttyOsc.ghostty_osc_new(NULL_VALUE, out parserRaw);
    if (res != GhosttyResult.GHOSTTY_SUCCESS || parserRaw == IntPtr.Zero)
    {
      Console.Error.WriteLine($"✗ Failed to create OSC parser: {res}");
      return 1;
    }

    using var parser = new GhosttyOscParserHandle(parserRaw);
    Console.WriteLine("✓ Created OSC parser successfully\n");

    // Test 1: Change Window Title (OSC 0 or OSC 2)
    TestOscSequence(
      parser,
      "Change Window Title - Simple",
      "0;Hello World",
      0x07); // BEL terminator

    TestOscSequence(
      parser,
      "Change Window Title - UTF-8",
      "2;Terminal 🚀 Title",
      0x07);

    TestOscSequence(
      parser,
      "Change Window Title - With Path",
      "0;~/projects/my-app",
      0x07);

    // Test 2: Report PWD (OSC 7)
    TestOscSequence(
      parser,
      "Report PWD - Local Path",
      "7;file://hostname/home/user/projects",
      0x07);

    TestOscSequence(
      parser,
      "Report PWD - Root",
      "7;file://localhost/",
      0x07);

    // Test 3: Clipboard Contents (OSC 52)
    TestOscSequence(
      parser,
      "Clipboard Contents - Set",
      "52;c;SGVsbG8gV29ybGQ=", // "Hello World" in base64
      0x07);

    TestOscSequence(
      parser,
      "Clipboard Contents - Query",
      "52;c;?",
      0x07);

    // Test 4: Color Operation (OSC 4, OSC 10-19, OSC 104-119)
    TestOscSequence(
      parser,
      "Color Operation - Set Color 4",
      "4;1;rgb:ff/00/00", // Set color 1 to red
      0x07);

    TestOscSequence(
      parser,
      "Color Operation - Query Foreground",
      "10;?", // Query foreground color
      0x07);

    TestOscSequence(
      parser,
      "Color Operation - Set Background",
      "11;rgb:00/00/00", // Set background to black
      0x07);

    // Test with ST terminator (0x5C after ESC 0x1B)
    // Note: We only pass the final byte of ST to ghostty_osc_end
    TestOscSequence(
      parser,
      "Window Title with ST terminator",
      "0;ST Terminated Title",
      0x5C); // ST terminator

    // Test edge cases
    TestOscSequence(
      parser,
      "Empty Title",
      "0;",
      0x07);

    TestOscSequence(
      parser,
      "Invalid Sequence",
      "999;unknown",
      0x07);

    Console.WriteLine("\n✓ Demo completed successfully");
    return 0;
  }
}
