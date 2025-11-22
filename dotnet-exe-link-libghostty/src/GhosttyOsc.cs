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

internal enum GhosttyOscCommandData
{
	GHOSTTY_OSC_DATA_INVALID = 0,
	GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR = 1,
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