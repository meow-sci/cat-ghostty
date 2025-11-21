using System;
using System.Runtime.InteropServices;

internal enum GhosttyResult : int
{
	GHOSTTY_SUCCESS = 0,
	GHOSTTY_OUT_OF_MEMORY = -1,
	GHOSTTY_INVALID_VALUE = -2
}

internal enum GhosttyOscCommandType : int
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

internal enum GhosttyOscCommandData : int
{
	GHOSTTY_OSC_DATA_INVALID = 0,
	GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR = 1,
}

internal static class Native
{
	private const string DLL_NAME = "lib/ghostty-vt.dll";

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyResult ghostty_osc_new(IntPtr allocator, out IntPtr parser);

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_osc_free(IntPtr parser);

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_osc_next(IntPtr parser, byte b);

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr ghostty_osc_end(IntPtr parser, byte terminator);

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyOscCommandType ghostty_osc_command_type(IntPtr command);

	[DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static extern bool ghostty_osc_command_data(IntPtr command, GhosttyOscCommandData data, out IntPtr outPtr);
}

public static class Program
{
	public static int Main(string[] args)
	{
		try
		{
			IntPtr parser;
			var res = Native.ghostty_osc_new(IntPtr.Zero, out parser);
			if (res != GhosttyResult.GHOSTTY_SUCCESS || parser == IntPtr.Zero)
			{
				Console.Error.WriteLine("Failed to create OSC parser: " + res);
				return 1;
			}

			// Feed bytes representing '0;hello' to the parser
			Native.ghostty_osc_next(parser, (byte)'0');
			Native.ghostty_osc_next(parser, (byte)';');
			var titleStr = "hello world 🔥";
			foreach (var ch in titleStr)
			{
				Native.ghostty_osc_next(parser, (byte)ch);
			}

			// Finalize parsing
			IntPtr command = Native.ghostty_osc_end(parser, 0);
			var type = Native.ghostty_osc_command_type(command);
			Console.WriteLine("Command type: " + (int)type + " (" + type + ")");

			// Extract title string if available
			if (Native.ghostty_osc_command_data(command, GhosttyOscCommandData.GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR, out IntPtr titlePtr))
			{
				var extracted = Marshal.PtrToStringAnsi(titlePtr);
				Console.WriteLine("Extracted title: " + extracted);
			}
			else
			{
				Console.WriteLine("Failed to extract title");
			}

			Native.ghostty_osc_free(parser);
			return 0;
		}
		catch (DllNotFoundException e)
		{
			Console.Error.WriteLine("Native library not found. Make sure lib/ghostty-vt.dll is copied to the output folder. Error: " + e.Message);
			return 1;
		}
	}
}

