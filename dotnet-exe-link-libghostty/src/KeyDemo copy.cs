
// using System.Runtime.InteropServices;
// using System.Text;
// using static dotnet_exe_link_libghostty.GhosttyKey;

// namespace dotnet_exe_link_libghostty;

// public static class KeyDemoProgram2
// {
//     public static int Run(string[] args)
//     {
//         Console.WriteLine("=== Ghostty Key Encoder Demo ===");
//         Console.WriteLine("This demo mirrors the C example from key.h\n");
        
//         // Create encoder
//         IntPtr encoder;
//         GhosttyResult result = ghostty_key_encoder_new(IntPtr.Zero, out encoder);
//         if (result != GhosttyResult.GHOSTTY_SUCCESS)
//         {
//             Console.Error.WriteLine($"Failed to create key encoder: {result}");
//             return 1;
//         }
//         Console.WriteLine("✓ Created key encoder");
        
//         // Create key event
//         IntPtr keyEvent;
//         result = ghostty_key_event_new(IntPtr.Zero, out keyEvent);
//         if (result != GhosttyResult.GHOSTTY_SUCCESS)
//         {
//             Console.Error.WriteLine($"Failed to create key event: {result}");
//             ghostty_key_encoder_free(encoder);
//             return 1;
//         }
//         Console.WriteLine("✓ Created key event\n");
        
//         // Demo 1: Legacy encoding (without Kitty protocol)
//         Console.WriteLine("--- LEGACY ENCODING (Default) ---\n");
        
//         EncodeAndDisplay(encoder, keyEvent, "Ctrl+C", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_C);
//             ghostty_key_event_set_mods(ev, GhosttyMods.GHOSTTY_MODS_CTRL);
//             SetUtf8Text(ev, "\x03"); // Ctrl+C produces ETX (0x03)
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Arrow Up", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_ARROW_UP);
//             ghostty_key_event_set_mods(ev, 0);
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "F1", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_F1);
//             ghostty_key_event_set_mods(ev, 0);
//         });
        
//         // Demo 2: Kitty keyboard protocol (all features enabled)
//         Console.WriteLine("--- KITTY KEYBOARD PROTOCOL (All Features) ---\n");
        
//         // Enable Kitty keyboard protocol with all features
//         byte kittyFlags = (byte)GhosttyKittyKeyFlags.GHOSTTY_KITTY_KEY_ALL;
//         IntPtr flagsPtr = Marshal.AllocHGlobal(1);
//         Marshal.WriteByte(flagsPtr, kittyFlags);
//         ghostty_key_encoder_setopt(encoder, GhosttyKeyEncoderOption.GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS, flagsPtr);
//         Marshal.FreeHGlobal(flagsPtr);
//         Console.WriteLine("✓ Enabled Kitty keyboard protocol (all features)\n");
        
//         EncodeAndDisplay(encoder, keyEvent, "Ctrl+C (press)", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_C);
//             ghostty_key_event_set_mods(ev, GhosttyMods.GHOSTTY_MODS_CTRL);
//             SetUtf8Text(ev, "\x03");
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Ctrl+C (release)", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_RELEASE);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_C);
//             ghostty_key_event_set_mods(ev, GhosttyMods.GHOSTTY_MODS_CTRL);
//             SetUtf8Text(ev, "\x03");
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Arrow Up", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_ARROW_UP);
//             ghostty_key_event_set_mods(ev, 0);
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Shift+F1", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_F1);
//             ghostty_key_event_set_mods(ev, GhosttyMods.GHOSTTY_MODS_SHIFT);
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Ctrl+Shift+A", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_A);
//             ghostty_key_event_set_mods(ev, GhosttyMods.GHOSTTY_MODS_CTRL | GhosttyMods.GHOSTTY_MODS_SHIFT);
//             SetUtf8Text(ev, "\x01"); // Ctrl+A produces SOH (0x01)
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Letter 'a' (press)", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_PRESS);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_A);
//             ghostty_key_event_set_mods(ev, 0);
//             SetUtf8Text(ev, "a");
//         });
        
//         EncodeAndDisplay(encoder, keyEvent, "Letter 'a' (release)", ev =>
//         {
//             ghostty_key_event_set_action(ev, GhosttyKeyAction.GHOSTTY_KEY_ACTION_RELEASE);
//             ghostty_key_event_set_key(ev, GhosttyKeyCode.GHOSTTY_KEY_A);
//             ghostty_key_event_set_mods(ev, 0);
//             SetUtf8Text(ev, "a");
//         });
        
//         // Cleanup
//         ghostty_key_event_free(keyEvent);
//         ghostty_key_encoder_free(encoder);
//         Console.WriteLine("✓ Cleanup complete");
//         Console.WriteLine("\nDemo complete! This matches the C example workflow from key.h");
        
//         return 0;
//     }
    
//     private static void EncodeAndDisplay(IntPtr encoder, IntPtr keyEvent, string description, Action<IntPtr> configure)
//     {
//         Console.WriteLine($"Encoding: {description}");
        
//         // Configure the key event
//         configure(keyEvent);
        
//         // Encode the key event
//         byte[] buffer = new byte[128];
//         UIntPtr written;
//         GhosttyResult result;
        
//         unsafe
//         {
//             fixed (byte* bufPtr = buffer)
//             {
//                 result = ghostty_key_encoder_encode(
//                     encoder, 
//                     keyEvent, 
//                     (IntPtr)bufPtr, 
//                     (UIntPtr)buffer.Length, 
//                     out written
//                 );
//             }
//         }
        
//         if (result != GhosttyResult.GHOSTTY_SUCCESS)
//         {
//             Console.Error.WriteLine($"  ✗ Failed to encode: {result}");
//             return;
//         }
        
//         // Display the encoded sequence
//         int bytesWritten = (int)written;
//         if (bytesWritten == 0)
//         {
//             Console.WriteLine("  (no output - this key doesn't produce an escape sequence in this mode)");
//         }
//         else
//         {
//             Console.Write($"  Hex: ");
//             for (int i = 0; i < bytesWritten; i++)
//             {
//                 Console.Write($"{buffer[i]:X2} ");
//             }
//             Console.WriteLine();
            
//             Console.Write($"  Escaped: ");
//             for (int i = 0; i < bytesWritten; i++)
//             {
//                 byte b = buffer[i];
//                 if (b == 0x1B) // ESC
//                     Console.Write("\\x1b");
//                 else if (b >= 32 && b < 127)
//                     Console.Write((char)b);
//                 else
//                     Console.Write($"\\x{b:x2}");
//             }
//             Console.WriteLine();
//         }
//         Console.WriteLine();
//     }
    
//     private static void SetUtf8Text(IntPtr keyEvent, string text)
//     {
//         byte[] utf8Bytes = Encoding.UTF8.GetBytes(text);
//         IntPtr utf8Ptr = Marshal.AllocHGlobal(utf8Bytes.Length);
//         Marshal.Copy(utf8Bytes, 0, utf8Ptr, utf8Bytes.Length);
//         ghostty_key_event_set_utf8(keyEvent, utf8Ptr, (UIntPtr)utf8Bytes.Length);
//         Marshal.FreeHGlobal(utf8Ptr);
//     }
// }
