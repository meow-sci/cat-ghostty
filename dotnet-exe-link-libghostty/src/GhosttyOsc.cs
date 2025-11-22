using System.Runtime.InteropServices;

namespace dotnet_exe_link_libghostty;

internal enum GhosttyResult
{
	GHOSTTY_SUCCESS = 0,
	GHOSTTY_OUT_OF_MEMORY = -1,
	GHOSTTY_INVALID_VALUE = -2
}

internal enum GhosttyOscCommandType
{
	GHOSTTY_OSC_COMMAND_INVALID = 0,
	GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE = 1,
	GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_ICON = 2,
	GHOSTTY_OSC_COMMAND_PROMPT_START = 3,
	GHOSTTY_OSC_COMMAND_PROMPT_END = 4,
	GHOSTTY_OSC_COMMAND_END_OF_INPUT = 5,
	GHOSTTY_OSC_COMMAND_END_OF_COMMAND = 6,
	GHOSTTY_OSC_COMMAND_CLIPBOARD_CONTENTS = 7,
	GHOSTTY_OSC_COMMAND_REPORT_PWD = 8,
	GHOSTTY_OSC_COMMAND_MOUSE_SHAPE = 9,
	GHOSTTY_OSC_COMMAND_COLOR_OPERATION = 10,
	GHOSTTY_OSC_COMMAND_KITTY_COLOR_PROTOCOL = 11,
	GHOSTTY_OSC_COMMAND_SHOW_DESKTOP_NOTIFICATION = 12,
	GHOSTTY_OSC_COMMAND_HYPERLINK_START = 13,
	GHOSTTY_OSC_COMMAND_HYPERLINK_END = 14,
	GHOSTTY_OSC_COMMAND_CONEMU_SLEEP = 15,
	GHOSTTY_OSC_COMMAND_CONEMU_SHOW_MESSAGE_BOX = 16,
	GHOSTTY_OSC_COMMAND_CONEMU_CHANGE_TAB_TITLE = 17,
	GHOSTTY_OSC_COMMAND_CONEMU_PROGRESS_REPORT = 18,
	
	GHOSTTY_OSC_COMMAND_CONEMU_WAIT_INPUT = 19,
	GHOSTTY_OSC_COMMAND_CONEMU_GUIMACRO = 20,
}

/// <summary>
/// Color operation types for OSC 4/5/10-19/104-105/110-119 commands
/// Based on the Ghostty color.zig implementation
/// </summary>
internal enum GhosttyColorOperation
{
	/// OSC 4 - Set/Query ANSI color (palette 0-255 or special 256-259)
	OSC_4 = 4,
	/// OSC 5 - Set/Query Special color (0-7 maps to special colors)
	OSC_5 = 5,
	/// OSC 10 - Set/Query foreground color
	OSC_10 = 10,
	/// OSC 11 - Set/Query background color
	OSC_11 = 11,
	/// OSC 12 - Set/Query cursor color
	OSC_12 = 12,
	/// OSC 13 - Set/Query pointer foreground color
	OSC_13 = 13,
	/// OSC 14 - Set/Query pointer background color
	OSC_14 = 14,
	/// OSC 15 - Set/Query tektronix foreground color
	OSC_15 = 15,
	/// OSC 16 - Set/Query tektronix background color
	OSC_16 = 16,
	/// OSC 17 - Set/Query highlight background color
	OSC_17 = 17,
	/// OSC 18 - Set/Query tektronix cursor color
	OSC_18 = 18,
	/// OSC 19 - Set/Query highlight foreground color
	OSC_19 = 19,
	/// OSC 104 - Reset ANSI color (palette)
	OSC_104 = 104,
	/// OSC 105 - Reset Special color
	OSC_105 = 105,
	/// OSC 110 - Reset foreground color
	OSC_110 = 110,
	/// OSC 111 - Reset background color
	OSC_111 = 111,
	/// OSC 112 - Reset cursor color
	OSC_112 = 112,
	/// OSC 113 - Reset pointer foreground color
	OSC_113 = 113,
	/// OSC 114 - Reset pointer background color
	OSC_114 = 114,
	/// OSC 115 - Reset tektronix foreground color
	OSC_115 = 115,
	/// OSC 116 - Reset tektronix background color
	OSC_116 = 116,
	/// OSC 117 - Reset highlight background color
	OSC_117 = 117,
	/// OSC 118 - Reset tektronix cursor color
	OSC_118 = 118,
	/// OSC 119 - Reset highlight foreground color
	OSC_119 = 119,
}

internal enum GhosttyOscCommandData
{
	GHOSTTY_OSC_DATA_INVALID = 0,
	
	/// Window title string data (for CHANGE_WINDOW_TITLE)
	GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR = 1,
	
	/// Raw OSC body string (the part after the command number)
	/// Available for most OSC commands
	GHOSTTY_OSC_DATA_BODY_STR = 2,
	
	/// PWD path string (for REPORT_PWD)
	GHOSTTY_OSC_DATA_REPORT_PWD_STR = 3,
	
	/// Clipboard selection and data (for CLIPBOARD_CONTENTS)
	GHOSTTY_OSC_DATA_CLIPBOARD_STR = 4,
}

internal static class GhosttyOsc
{

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyResult ghostty_osc_new(IntPtr allocator, out IntPtr parser);

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_osc_free(IntPtr parser);
	
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_osc_reset(IntPtr parser);

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_osc_next(IntPtr parser, byte b);

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr ghostty_osc_end(IntPtr parser, byte terminator);

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyOscCommandType ghostty_osc_command_type(IntPtr command);

	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static extern bool ghostty_osc_command_data(IntPtr command, GhosttyOscCommandData data, out IntPtr outPtr);
}