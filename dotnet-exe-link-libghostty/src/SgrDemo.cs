using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static dotnet_exe_link_libghostty.GhosttySgr;

namespace dotnet_exe_link_libghostty;

/// <summary>
/// Demo program showcasing Ghostty SGR (Select Graphic Rendition) parser functionality.
/// 
/// This demonstrates how to:
/// - Create and manage SGR parser instances
/// - Parse various SGR parameter sequences
/// - Extract text attributes (bold, italic, colors, underline styles)
/// - Handle different color formats (8-color, 16-color, 256-color, RGB)
/// - Properly manage parser lifecycle with SafeHandles
/// 
/// Run with: dotnet run -- --sgr-demo
/// </summary>
/// 
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

public static class SgrDemoProgram
{
	/// <summary>
	/// Parse SGR sequence string (e.g., "4:3;38;2;255;0;0") into parameters and separators.
	/// </summary>
	private static (ushort[] parameters, byte[] separators) ParseSgrSequence(string sequence)
	{
		var parameters = new List<ushort>();
		var separators = new List<byte>();
		var currentNum = "";

		for (int i = 0; i < sequence.Length; i++)
		{
			char ch = sequence[i];
			if (ch == ';' || ch == ':')
			{
				if (currentNum.Length > 0)
				{
					if (ushort.TryParse(currentNum, out ushort value))
					{
						parameters.Add(value);
					}
					currentNum = "";
				}
				separators.Add((byte)ch);
			}
			else if (char.IsDigit(ch))
			{
				currentNum += ch;
			}
		}

		// Don't forget the last number
		if (currentNum.Length > 0)
		{
			if (ushort.TryParse(currentNum, out ushort value))
			{
				parameters.Add(value);
			}
		}

		return (parameters.ToArray(), separators.ToArray());
	}

	/// <summary>
	/// Get a friendly name for an SGR attribute tag.
	/// </summary>
	private static string GetAttributeName(GhosttySgrAttributeTag tag)
	{
		return tag switch
		{
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNSET => "Unset",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNKNOWN => "Unknown",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BOLD => "Bold",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BOLD => "Reset Bold",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_ITALIC => "Italic",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_ITALIC => "Reset Italic",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FAINT => "Faint",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE => "Underline",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_UNDERLINE => "Reset Underline",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR => "Underline Color (RGB)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR_256 => "Underline Color (256)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_UNDERLINE_COLOR => "Reset Underline Color",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_OVERLINE => "Overline",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_OVERLINE => "Reset Overline",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BLINK => "Blink",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BLINK => "Reset Blink",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_INVERSE => "Inverse",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_INVERSE => "Reset Inverse",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_INVISIBLE => "Invisible",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_INVISIBLE => "Reset Invisible",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_STRIKETHROUGH => "Strikethrough",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_STRIKETHROUGH => "Reset Strikethrough",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_FG => "Foreground Color (RGB)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_BG => "Background Color (RGB)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_8 => "Background Color (8)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_8 => "Foreground Color (8)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_FG => "Reset Foreground",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_RESET_BG => "Reset Background",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_BG_8 => "Bright Background Color (8)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_FG_8 => "Bright Foreground Color (8)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_256 => "Background Color (256)",
			GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_256 => "Foreground Color (256)",
			_ => $"Unknown({(int)tag})"
		};
	}

	/// <summary>
	/// Get a friendly name for an underline style.
	/// </summary>
	private static string GetUnderlineName(GhosttySgrUnderline underline)
	{
		return underline switch
		{
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_NONE => "None",
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_SINGLE => "Single",
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DOUBLE => "Double",
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_CURLY => "Curly",
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DOTTED => "Dotted",
			GhosttySgrUnderline.GHOSTTY_SGR_UNDERLINE_DASHED => "Dashed",
			_ => $"Unknown({(int)underline})"
		};
	}

	/// <summary>
	/// Helper to display attribute value based on its tag.
	/// </summary>
	private static void DisplayAttributeValue(GhosttySgrAttributeTag tag, GhosttySgrAttributeValue value)
	{
		switch (tag)
		{
			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE:
				Console.WriteLine($"    Style: {GetUnderlineName(value.underline)}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR:
				Console.WriteLine($"    Color: {value.underline_color}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNDERLINE_COLOR_256:
				Console.WriteLine($"    Color Index: {value.underline_color_256.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_FG:
				Console.WriteLine($"    Color: {value.direct_color_fg}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_DIRECT_COLOR_BG:
				Console.WriteLine($"    Color: {value.direct_color_bg}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_8:
				Console.WriteLine($"    Color Index: {value.fg_8.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_8:
				Console.WriteLine($"    Color Index: {value.bg_8.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_FG_8:
				Console.WriteLine($"    Color Index: {value.bright_fg_8.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BRIGHT_BG_8:
				Console.WriteLine($"    Color Index: {value.bright_bg_8.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_FG_256:
				Console.WriteLine($"    Color Index: {value.fg_256.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_BG_256:
				Console.WriteLine($"    Color Index: {value.bg_256.Value}");
				break;

			case GhosttySgrAttributeTag.GHOSTTY_SGR_ATTR_UNKNOWN:
				{
					var unknown = value.unknown;
					var fullLen = ghostty_sgr_unknown_full(unknown, out IntPtr fullPtr);
					var partialLen = ghostty_sgr_unknown_partial(unknown, out IntPtr partialPtr);

					Console.Write($"    Full params ({fullLen}): ");
					if (fullLen.ToUInt32() > 0)
					{
						var fullParams = new ushort[fullLen.ToUInt32()];
						Marshal.Copy(fullPtr, (short[])(object)fullParams, 0, (int)fullLen.ToUInt32());
						Console.WriteLine(string.Join(", ", fullParams));
					}
					else
					{
						Console.WriteLine("(none)");
					}

					Console.Write($"    Partial params ({partialLen}): ");
					if (partialLen.ToUInt32() > 0)
					{
						var partialParams = new ushort[partialLen.ToUInt32()];
						Marshal.Copy(partialPtr, (short[])(object)partialParams, 0, (int)partialLen.ToUInt32());
						Console.WriteLine(string.Join(", ", partialParams));
					}
					else
					{
						Console.WriteLine("(none)");
					}
					break;
				}

			default:
				// Attributes without associated data (like bold, italic)
				break;
		}
	}

	/// <summary>
	/// Helper to parse and test a single SGR sequence from a string.
	/// </summary>
	private static void TestSgrSequence(
		GhosttySgrParserHandle parser,
		string testName,
		string sgrSequence)
	{
		Console.WriteLine($"\n--- Test: {testName} ---");
		Console.WriteLine($"  Input: {sgrSequence}");
		Console.WriteLine($"  CSI Sequence: ESC[{sgrSequence}m");

		// Parse the sequence string into parameters and separators
		var (parameters, separators) = ParseSgrSequence(sgrSequence);

		if (parameters.Length == 0)
		{
			Console.WriteLine("  ✗ No parameters parsed from input");
			return;
		}

		// Set parameters for parsing
		var res = ghostty_sgr_set_params(
			parser.DangerousGetHandle(),
			parameters,
			separators.Length > 0 ? separators : null,
			new UIntPtr((uint)parameters.Length));

		if (res != GhosttyResult.GHOSTTY_SUCCESS)
		{
			Console.WriteLine($"  ✗ Failed to set parameters: {res}");
			return;
		}

		// Iterate through attributes
		int attributeCount = 0;
		while (ghostty_sgr_next(parser.DangerousGetHandle(), out GhosttySgrAttribute attr))
		{
			attributeCount++;
			Console.WriteLine($"  Attribute {attributeCount}: {GetAttributeName(attr.tag)}");
			DisplayAttributeValue(attr.tag, attr.value);
		}

		if (attributeCount == 0)
		{
			Console.WriteLine("  (no attributes parsed)");
		}
		else
		{
			Console.WriteLine($"  ✓ Parsed {attributeCount} attribute(s)");
		}
	}

	public static int Run(string[] args)
	{
		Console.WriteLine("=== Ghostty SGR Parser Demo ===");

		IntPtr NULL_VALUE = IntPtr.Zero;

		// Create SGR parser
		IntPtr parserRaw;
		var res = ghostty_sgr_new(NULL_VALUE, out parserRaw);
		if (res != GhosttyResult.GHOSTTY_SUCCESS || parserRaw == IntPtr.Zero)
		{
			Console.Error.WriteLine($"✗ Failed to create SGR parser: {res}");
			return 1;
		}

		using var parser = new GhosttySgrParserHandle(parserRaw);
		Console.WriteLine("✓ Created SGR parser successfully\n");

		// Test the default example from sgr.html
		Console.WriteLine("=== Default Example (from sgr.html) ===");
		TestSgrSequence(parser, "Curly underline with colors", "4:3;38;2;51;51;51;48;2;170;170;170;58;2;255;97;136");

		// Test 1: Simple text styles
		Console.WriteLine("\n=== Simple Text Styles ===");

		TestSgrSequence(parser, "Bold", "1");
		TestSgrSequence(parser, "Italic", "3");
		TestSgrSequence(parser, "Faint", "2");
		TestSgrSequence(parser, "Underline (single)", "4");
		TestSgrSequence(parser, "Blink", "5");
		TestSgrSequence(parser, "Inverse", "7");
		TestSgrSequence(parser, "Invisible", "8");
		TestSgrSequence(parser, "Strikethrough", "9");
		TestSgrSequence(parser, "Reset Bold", "22");
		TestSgrSequence(parser, "Reset Italic", "23");
		TestSgrSequence(parser, "Reset Underline", "24");

		// Test 2: Combined styles
		Console.WriteLine("\n=== Combined Text Styles ===");

		TestSgrSequence(parser, "Bold + Italic", "1;3");
		TestSgrSequence(parser, "Bold + Red foreground", "1;31");
		TestSgrSequence(parser, "Underline + Blue background", "4;44");
		TestSgrSequence(parser, "Bold + Italic + Underline + Red", "1;3;4;31");

		// Test 3: 8-color foreground
		Console.WriteLine("\n=== 8-Color Foreground (30-37) ===");

		TestSgrSequence(parser, "Black foreground", "30");
		TestSgrSequence(parser, "Red foreground", "31");
		TestSgrSequence(parser, "Green foreground", "32");
		TestSgrSequence(parser, "Yellow foreground", "33");
		TestSgrSequence(parser, "Blue foreground", "34");
		TestSgrSequence(parser, "Magenta foreground", "35");
		TestSgrSequence(parser, "Cyan foreground", "36");
		TestSgrSequence(parser, "White foreground", "37");
		TestSgrSequence(parser, "Reset foreground", "39");

		// Test 4: 8-color background
		Console.WriteLine("\n=== 8-Color Background (40-47) ===");

		TestSgrSequence(parser, "Black background", "40");
		TestSgrSequence(parser, "Red background", "41");
		TestSgrSequence(parser, "Green background", "42");
		TestSgrSequence(parser, "Yellow background", "43");
		TestSgrSequence(parser, "Blue background", "44");
		TestSgrSequence(parser, "Magenta background", "45");
		TestSgrSequence(parser, "Cyan background", "46");
		TestSgrSequence(parser, "White background", "47");
		TestSgrSequence(parser, "Reset background", "49");

		// Test 5: Bright colors (90-97, 100-107)
		Console.WriteLine("\n=== Bright Colors (90-97, 100-107) ===");

		TestSgrSequence(parser, "Bright red foreground", "91");
		TestSgrSequence(parser, "Bright green foreground", "92");
		TestSgrSequence(parser, "Bright yellow foreground", "93");
		TestSgrSequence(parser, "Bright blue background", "104");
		TestSgrSequence(parser, "Bright magenta background", "105");

		// Test 6: 256-color mode
		Console.WriteLine("\n=== 256-Color Mode (38;5;n and 48;5;n) ===");

		TestSgrSequence(parser, "256-color foreground (red=196)", "38;5;196");
		TestSgrSequence(parser, "256-color foreground (green=46)", "38;5;46");
		TestSgrSequence(parser, "256-color background (blue=21)", "48;5;21");
		TestSgrSequence(parser, "256-color both (fg=226, bg=235)", "38;5;226;48;5;235");

		// Test 7: RGB (truecolor) mode
		Console.WriteLine("\n=== RGB/Truecolor Mode (38;2;r;g;b and 48;2;r;g;b) ===");

		TestSgrSequence(parser, "RGB foreground (255,0,0)", "38;2;255;0;0");
		TestSgrSequence(parser, "RGB foreground (0,255,0)", "38;2;0;255;0");
		TestSgrSequence(parser, "RGB background (0,0,255)", "48;2;0;0;255");
		TestSgrSequence(parser, "RGB both (fg=255,128,0, bg=0,64,128)", "38;2;255;128;0;48;2;0;64;128");

		// Test 8: Underline styles with colon separator
		Console.WriteLine("\n=== Underline Styles (4:n with colon separator) ===");

		TestSgrSequence(parser, "Single underline", "4:1");
		TestSgrSequence(parser, "Double underline", "4:2");
		TestSgrSequence(parser, "Curly underline", "4:3");
		TestSgrSequence(parser, "Dotted underline", "4:4");
		TestSgrSequence(parser, "Dashed underline", "4:5");

		// Test 9: Underline color
		Console.WriteLine("\n=== Underline Color (58;5;n and 58;2;r;g;b) ===");

		TestSgrSequence(parser, "Underline color 256 (red=196)", "58;5;196");
		TestSgrSequence(parser, "Underline color RGB (255,0,255)", "58;2;255;0;255");
		TestSgrSequence(parser, "Curly underline with color", "4:3;58;5;46");

		// Test 10: Overline
		Console.WriteLine("\n=== Overline ===");

		TestSgrSequence(parser, "Overline", "53");
		TestSgrSequence(parser, "Reset Overline", "55");

		// Test 11: Reset/Clear all (SGR 0)
		Console.WriteLine("\n=== Reset/Clear All ===");

		TestSgrSequence(parser, "Reset all attributes", "0");

		// Test 12: Complex real-world examples
		Console.WriteLine("\n=== Complex Real-World Examples ===");

		TestSgrSequence(parser, "Bold red text on blue background", "1;31;44");
		TestSgrSequence(parser, "Italic green with curly underline", "3;32;4:3");
		TestSgrSequence(parser, "256-color with bold and underline", "1;4;38;5;208;48;5;236");
		TestSgrSequence(parser, "RGB colors with multiple styles", "1;3;4;38;2;255;165;0;48;2;30;30;30");

		// Test 13: Edge cases
		Console.WriteLine("\n=== Edge Cases ===");

		TestSgrSequence(parser, "Empty sequence", "");
		TestSgrSequence(parser, "Multiple resets", "0;0;0");

		Console.WriteLine("\n=== Demo Complete ===");
		Console.WriteLine("✓ All SGR parsing tests completed successfully");

		return 0;
	}
}
