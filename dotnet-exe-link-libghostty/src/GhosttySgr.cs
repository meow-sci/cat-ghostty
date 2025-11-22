using System.Runtime.InteropServices;

namespace dotnet_exe_link_libghostty;

/// <summary>
/// SGR (Select Graphic Rendition) attribute tags.
/// These identify the type of an SGR attribute in a tagged union.
/// </summary>
internal enum GhosttySgrAttributeTag
{
	GHOSTTY_SGR_ATTR_UNSET = 0,
	GHOSTTY_SGR_ATTR_UNKNOWN = 1,
	GHOSTTY_SGR_ATTR_BOLD = 2,
	GHOSTTY_SGR_ATTR_RESET_BOLD = 3,
	GHOSTTY_SGR_ATTR_ITALIC = 4,
	GHOSTTY_SGR_ATTR_RESET_ITALIC = 5,
	GHOSTTY_SGR_ATTR_FAINT = 6,
	GHOSTTY_SGR_ATTR_UNDERLINE = 7,
	GHOSTTY_SGR_ATTR_RESET_UNDERLINE = 8,
	GHOSTTY_SGR_ATTR_UNDERLINE_COLOR = 9,
	GHOSTTY_SGR_ATTR_UNDERLINE_COLOR_256 = 10,
	GHOSTTY_SGR_ATTR_RESET_UNDERLINE_COLOR = 11,
	GHOSTTY_SGR_ATTR_OVERLINE = 12,
	GHOSTTY_SGR_ATTR_RESET_OVERLINE = 13,
	GHOSTTY_SGR_ATTR_BLINK = 14,
	GHOSTTY_SGR_ATTR_RESET_BLINK = 15,
	GHOSTTY_SGR_ATTR_INVERSE = 16,
	GHOSTTY_SGR_ATTR_RESET_INVERSE = 17,
	GHOSTTY_SGR_ATTR_INVISIBLE = 18,
	GHOSTTY_SGR_ATTR_RESET_INVISIBLE = 19,
	GHOSTTY_SGR_ATTR_STRIKETHROUGH = 20,
	GHOSTTY_SGR_ATTR_RESET_STRIKETHROUGH = 21,
	GHOSTTY_SGR_ATTR_DIRECT_COLOR_FG = 22,
	GHOSTTY_SGR_ATTR_DIRECT_COLOR_BG = 23,
	GHOSTTY_SGR_ATTR_BG_8 = 24,
	GHOSTTY_SGR_ATTR_FG_8 = 25,
	GHOSTTY_SGR_ATTR_RESET_FG = 26,
	GHOSTTY_SGR_ATTR_RESET_BG = 27,
	GHOSTTY_SGR_ATTR_BRIGHT_BG_8 = 28,
	GHOSTTY_SGR_ATTR_BRIGHT_FG_8 = 29,
	GHOSTTY_SGR_ATTR_BG_256 = 30,
	GHOSTTY_SGR_ATTR_FG_256 = 31,
}

/// <summary>
/// Underline style types.
/// </summary>
public enum GhosttySgrUnderline
{
	GHOSTTY_SGR_UNDERLINE_NONE = 0,
	GHOSTTY_SGR_UNDERLINE_SINGLE = 1,
	GHOSTTY_SGR_UNDERLINE_DOUBLE = 2,
	GHOSTTY_SGR_UNDERLINE_CURLY = 3,
	GHOSTTY_SGR_UNDERLINE_DOTTED = 4,
	GHOSTTY_SGR_UNDERLINE_DASHED = 5,
}

/// <summary>
/// RGB color value.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct GhosttyColorRgb
{
	public byte r; // Red component (0-255)
	public byte g; // Green component (0-255)
	public byte b; // Blue component (0-255)

	public override string ToString() => $"rgb({r}, {g}, {b})";
}

/// <summary>
/// Palette color index (0-255).
/// </summary>
internal struct GhosttyColorPaletteIndex
{
	public byte Value;

	public GhosttyColorPaletteIndex(byte value)
	{
		Value = value;
	}

	public override string ToString() => Value.ToString();

	public static implicit operator byte(GhosttyColorPaletteIndex index) => index.Value;
	public static implicit operator GhosttyColorPaletteIndex(byte value) => new GhosttyColorPaletteIndex(value);
}

/// <summary>
/// Unknown SGR attribute data.
/// Contains the full parameter list and the partial list where parsing encountered an unknown or invalid sequence.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttySgrUnknown
{
	public IntPtr full_ptr;
	public UIntPtr full_len;
	public IntPtr partial_ptr;
	public UIntPtr partial_len;
}

/// <summary>
/// SGR attribute value union.
/// This union contains all possible attribute values. Use the tag field to determine which union member is active.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 64)] // 64 bytes = 8 uint64_t padding
internal struct GhosttySgrAttributeValue
{
	[FieldOffset(0)] public GhosttySgrUnknown unknown;
	[FieldOffset(0)] public GhosttySgrUnderline underline;
	[FieldOffset(0)] public GhosttyColorRgb underline_color;
	[FieldOffset(0)] public GhosttyColorPaletteIndex underline_color_256;
	[FieldOffset(0)] public GhosttyColorRgb direct_color_fg;
	[FieldOffset(0)] public GhosttyColorRgb direct_color_bg;
	[FieldOffset(0)] public GhosttyColorPaletteIndex bg_8;
	[FieldOffset(0)] public GhosttyColorPaletteIndex fg_8;
	[FieldOffset(0)] public GhosttyColorPaletteIndex bright_bg_8;
	[FieldOffset(0)] public GhosttyColorPaletteIndex bright_fg_8;
	[FieldOffset(0)] public GhosttyColorPaletteIndex bg_256;
	[FieldOffset(0)] public GhosttyColorPaletteIndex fg_256;
}

/// <summary>
/// SGR attribute (tagged union).
/// A complete SGR attribute with both its type tag and associated value.
/// Always check the tag field to determine which value union member is valid.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct GhosttySgrAttribute
{
	public GhosttySgrAttributeTag tag;
	public GhosttySgrAttributeValue value;
}

/// <summary>
/// P/Invoke declarations for libghostty-vt SGR parser functions.
/// </summary>
internal static class GhosttySgr
{
	/// <summary>
	/// Create a new SGR parser instance.
	/// </summary>
	/// <param name="allocator">Pointer to the allocator to use for memory management, or NULL to use the default allocator</param>
	/// <param name="parser">Pointer to store the created parser handle</param>
	/// <returns>GHOSTTY_SUCCESS on success, or an error code on failure</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyResult ghostty_sgr_new(IntPtr allocator, out IntPtr parser);

	/// <summary>
	/// Free an SGR parser instance.
	/// </summary>
	/// <param name="parser">The parser handle to free (may be NULL)</param>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_sgr_free(IntPtr parser);

	/// <summary>
	/// Reset an SGR parser instance to the beginning of the parameter list.
	/// </summary>
	/// <param name="parser">The parser handle to reset, must not be NULL</param>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern void ghostty_sgr_reset(IntPtr parser);

	/// <summary>
	/// Set SGR parameters for parsing.
	/// </summary>
	/// <param name="parser">The parser handle, must not be NULL</param>
	/// <param name="params">Array of SGR parameter values</param>
	/// <param name="separators">Optional array of separator characters (';' or ':'), or NULL</param>
	/// <param name="len">Number of parameters (and separators if provided)</param>
	/// <returns>GHOSTTY_SUCCESS on success, or an error code on failure</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttyResult ghostty_sgr_set_params(
		IntPtr parser,
		ushort[] @params,
		byte[]? separators,
		UIntPtr len);

	/// <summary>
	/// Get the next SGR attribute.
	/// Call this function repeatedly until it returns false to process all attributes in the sequence.
	/// </summary>
	/// <param name="parser">The parser handle, must not be NULL</param>
	/// <param name="attr">Pointer to store the next attribute</param>
	/// <returns>true if an attribute was returned, false if no more attributes</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	internal static extern bool ghostty_sgr_next(IntPtr parser, out GhosttySgrAttribute attr);

	/// <summary>
	/// Get the full parameter list from an unknown SGR attribute.
	/// </summary>
	/// <param name="unknown">The unknown attribute data</param>
	/// <param name="ptr">Pointer to store the pointer to the parameter array (may be NULL)</param>
	/// <returns>The length of the full parameter array</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern UIntPtr ghostty_sgr_unknown_full(GhosttySgrUnknown unknown, out IntPtr ptr);

	/// <summary>
	/// Get the partial parameter list from an unknown SGR attribute.
	/// </summary>
	/// <param name="unknown">The unknown attribute data</param>
	/// <param name="ptr">Pointer to store the pointer to the parameter array (may be NULL)</param>
	/// <returns>The length of the partial parameter array</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern UIntPtr ghostty_sgr_unknown_partial(GhosttySgrUnknown unknown, out IntPtr ptr);

	/// <summary>
	/// Get the tag from an SGR attribute.
	/// </summary>
	/// <param name="attr">The SGR attribute</param>
	/// <returns>The attribute tag</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern GhosttySgrAttributeTag ghostty_sgr_attribute_tag(GhosttySgrAttribute attr);

	/// <summary>
	/// Get the value from an SGR attribute.
	/// </summary>
	/// <param name="attr">Pointer to the SGR attribute</param>
	/// <returns>Pointer to the attribute value union</returns>
	[DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
	internal static extern IntPtr ghostty_sgr_attribute_value(ref GhosttySgrAttribute attr);
}
