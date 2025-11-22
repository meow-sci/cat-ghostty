using System.Runtime.InteropServices;

namespace dotnet_exe_link_libghostty;


internal static class GhosttyKey
{
  internal enum GhosttyKeyAction
  {
    /** Key was released */
    GHOSTTY_KEY_ACTION_RELEASE = 0,
    /** Key was pressed */
    GHOSTTY_KEY_ACTION_PRESS = 1,
    /** Key is being repeated (held down) */
    GHOSTTY_KEY_ACTION_REPEAT = 2,
  }

  [Flags]
  internal enum GhosttyMods : ushort
  {
    /** Shift key is pressed */
    GHOSTTY_MODS_SHIFT = (1 << 0),
    /** Control key is pressed */
    GHOSTTY_MODS_CTRL = (1 << 1),
    /** Alt/Option key is pressed */
    GHOSTTY_MODS_ALT = (1 << 2),
    /** Super/Command/Windows key is pressed */
    GHOSTTY_MODS_SUPER = (1 << 3),
    /** Caps Lock is active */
    GHOSTTY_MODS_CAPS_LOCK = (1 << 4),
    /** Num Lock is active */
    GHOSTTY_MODS_NUM_LOCK = (1 << 5),

    /**
     * Right shift is pressed (0 = left, 1 = right).
     * Only meaningful when GHOSTTY_MODS_SHIFT is set.
     */
    GHOSTTY_MODS_SHIFT_SIDE = (1 << 6),
    /**
     * Right ctrl is pressed (0 = left, 1 = right).
     * Only meaningful when GHOSTTY_MODS_CTRL is set.
     */
    GHOSTTY_MODS_CTRL_SIDE = (1 << 7),
    /**
     * Right alt is pressed (0 = left, 1 = right).
     * Only meaningful when GHOSTTY_MODS_ALT is set.
     */
    GHOSTTY_MODS_ALT_SIDE = (1 << 8),
    /**
     * Right super is pressed (0 = left, 1 = right).
     * Only meaningful when GHOSTTY_MODS_SUPER is set.
     */
    GHOSTTY_MODS_SUPER_SIDE = (1 << 9),
  }

  internal enum GhosttyOptionAsAlt
  {
    GHOSTTY_OPTION_AS_ALT_FALSE = 0,
    GHOSTTY_OPTION_AS_ALT_TRUE = 1,
    GHOSTTY_OPTION_AS_ALT_LEFT = 2,
    GHOSTTY_OPTION_AS_ALT_RIGHT = 3,
  }

  internal enum GhosttyKeyEncoderOption
  {
    GHOSTTY_KEY_ENCODER_OPT_CURSOR_KEY_APPLICATION = 0,
    GHOSTTY_KEY_ENCODER_OPT_KEYPAD_KEY_APPLICATION = 1,
    GHOSTTY_KEY_ENCODER_OPT_IGNORE_KEYPAD_WITH_NUMLOCK = 2,
    GHOSTTY_KEY_ENCODER_OPT_ALT_ESC_PREFIX = 3,
    GHOSTTY_KEY_ENCODER_OPT_MODIFY_OTHER_KEYS_STATE_2 = 4,
    GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS = 5,
    GHOSTTY_KEY_ENCODER_OPT_MACOS_OPTION_AS_ALT = 6,
  }

  internal enum GhosttyKeyCode
  {
    GHOSTTY_KEY_UNIDENTIFIED = 0,
    GHOSTTY_KEY_BACKQUOTE,
    GHOSTTY_KEY_BACKSLASH,
    GHOSTTY_KEY_BRACKET_LEFT,
    GHOSTTY_KEY_BRACKET_RIGHT,
    GHOSTTY_KEY_COMMA,
    GHOSTTY_KEY_DIGIT_0,
    GHOSTTY_KEY_DIGIT_1,
    GHOSTTY_KEY_DIGIT_2,
    GHOSTTY_KEY_DIGIT_3,
    GHOSTTY_KEY_DIGIT_4,
    GHOSTTY_KEY_DIGIT_5,
    GHOSTTY_KEY_DIGIT_6,
    GHOSTTY_KEY_DIGIT_7,
    GHOSTTY_KEY_DIGIT_8,
    GHOSTTY_KEY_DIGIT_9,
    GHOSTTY_KEY_EQUAL,
    GHOSTTY_KEY_INTL_BACKSLASH,
    GHOSTTY_KEY_INTL_RO,
    GHOSTTY_KEY_INTL_YEN,
    GHOSTTY_KEY_A,
    GHOSTTY_KEY_B,
    GHOSTTY_KEY_C,
    GHOSTTY_KEY_D,
    GHOSTTY_KEY_E,
    GHOSTTY_KEY_F,
    GHOSTTY_KEY_G,
    GHOSTTY_KEY_H,
    GHOSTTY_KEY_I,
    GHOSTTY_KEY_J,
    GHOSTTY_KEY_K,
    GHOSTTY_KEY_L,
    GHOSTTY_KEY_M,
    GHOSTTY_KEY_N,
    GHOSTTY_KEY_O,
    GHOSTTY_KEY_P,
    GHOSTTY_KEY_Q,
    GHOSTTY_KEY_R,
    GHOSTTY_KEY_S,
    GHOSTTY_KEY_T,
    GHOSTTY_KEY_U,
    GHOSTTY_KEY_V,
    GHOSTTY_KEY_W,
    GHOSTTY_KEY_X,
    GHOSTTY_KEY_Y,
    GHOSTTY_KEY_Z,
    GHOSTTY_KEY_MINUS,
    GHOSTTY_KEY_PERIOD,
    GHOSTTY_KEY_QUOTE,
    GHOSTTY_KEY_SEMICOLON,
    GHOSTTY_KEY_SLASH,

    GHOSTTY_KEY_ALT_LEFT,
    GHOSTTY_KEY_ALT_RIGHT,
    GHOSTTY_KEY_BACKSPACE,
    GHOSTTY_KEY_CAPS_LOCK,
    GHOSTTY_KEY_CONTEXT_MENU,
    GHOSTTY_KEY_CONTROL_LEFT,
    GHOSTTY_KEY_CONTROL_RIGHT,
    GHOSTTY_KEY_ENTER,
    GHOSTTY_KEY_META_LEFT,
    GHOSTTY_KEY_META_RIGHT,
    GHOSTTY_KEY_SHIFT_LEFT,
    GHOSTTY_KEY_SHIFT_RIGHT,
    GHOSTTY_KEY_SPACE,
    GHOSTTY_KEY_TAB,
    GHOSTTY_KEY_CONVERT,
    GHOSTTY_KEY_KANA_MODE,
    GHOSTTY_KEY_NON_CONVERT,

    GHOSTTY_KEY_DELETE,
    GHOSTTY_KEY_END,
    GHOSTTY_KEY_HELP,
    GHOSTTY_KEY_HOME,
    GHOSTTY_KEY_INSERT,
    GHOSTTY_KEY_PAGE_DOWN,
    GHOSTTY_KEY_PAGE_UP,

    GHOSTTY_KEY_ARROW_DOWN,
    GHOSTTY_KEY_ARROW_LEFT,
    GHOSTTY_KEY_ARROW_RIGHT,
    GHOSTTY_KEY_ARROW_UP,

    GHOSTTY_KEY_NUM_LOCK,
    GHOSTTY_KEY_NUMPAD_0,
    GHOSTTY_KEY_NUMPAD_1,
    GHOSTTY_KEY_NUMPAD_2,
    GHOSTTY_KEY_NUMPAD_3,
    GHOSTTY_KEY_NUMPAD_4,
    GHOSTTY_KEY_NUMPAD_5,
    GHOSTTY_KEY_NUMPAD_6,
    GHOSTTY_KEY_NUMPAD_7,
    GHOSTTY_KEY_NUMPAD_8,
    GHOSTTY_KEY_NUMPAD_9,
    GHOSTTY_KEY_NUMPAD_ADD,
    GHOSTTY_KEY_NUMPAD_BACKSPACE,
    GHOSTTY_KEY_NUMPAD_CLEAR,
    GHOSTTY_KEY_NUMPAD_CLEAR_ENTRY,
    GHOSTTY_KEY_NUMPAD_COMMA,
    GHOSTTY_KEY_NUMPAD_DECIMAL,
    GHOSTTY_KEY_NUMPAD_DIVIDE,
    GHOSTTY_KEY_NUMPAD_ENTER,
    GHOSTTY_KEY_NUMPAD_EQUAL,
    GHOSTTY_KEY_NUMPAD_MEMORY_ADD,
    GHOSTTY_KEY_NUMPAD_MEMORY_CLEAR,
    GHOSTTY_KEY_NUMPAD_MEMORY_RECALL,
    GHOSTTY_KEY_NUMPAD_MEMORY_STORE,
    GHOSTTY_KEY_NUMPAD_MEMORY_SUBTRACT,
    GHOSTTY_KEY_NUMPAD_MULTIPLY,
    GHOSTTY_KEY_NUMPAD_PAREN_LEFT,
    GHOSTTY_KEY_NUMPAD_PAREN_RIGHT,
    GHOSTTY_KEY_NUMPAD_SUBTRACT,
    GHOSTTY_KEY_NUMPAD_SEPARATOR,
    GHOSTTY_KEY_NUMPAD_UP,
    GHOSTTY_KEY_NUMPAD_DOWN,
    GHOSTTY_KEY_NUMPAD_RIGHT,
    GHOSTTY_KEY_NUMPAD_LEFT,
    GHOSTTY_KEY_NUMPAD_BEGIN,
    GHOSTTY_KEY_NUMPAD_HOME,
    GHOSTTY_KEY_NUMPAD_END,
    GHOSTTY_KEY_NUMPAD_INSERT,
    GHOSTTY_KEY_NUMPAD_DELETE,
    GHOSTTY_KEY_NUMPAD_PAGE_UP,
    GHOSTTY_KEY_NUMPAD_PAGE_DOWN,

    GHOSTTY_KEY_ESCAPE,
    GHOSTTY_KEY_F1,
    GHOSTTY_KEY_F2,
    GHOSTTY_KEY_F3,
    GHOSTTY_KEY_F4,
    GHOSTTY_KEY_F5,
    GHOSTTY_KEY_F6,
    GHOSTTY_KEY_F7,
    GHOSTTY_KEY_F8,
    GHOSTTY_KEY_F9,
    GHOSTTY_KEY_F10,
    GHOSTTY_KEY_F11,
    GHOSTTY_KEY_F12,
    GHOSTTY_KEY_F13,
    GHOSTTY_KEY_F14,
    GHOSTTY_KEY_F15,
    GHOSTTY_KEY_F16,
    GHOSTTY_KEY_F17,
    GHOSTTY_KEY_F18,
    GHOSTTY_KEY_F19,
    GHOSTTY_KEY_F20,
    GHOSTTY_KEY_F21,
    GHOSTTY_KEY_F22,
    GHOSTTY_KEY_F23,
    GHOSTTY_KEY_F24,
    GHOSTTY_KEY_F25,
    GHOSTTY_KEY_FN,
    GHOSTTY_KEY_FN_LOCK,
    GHOSTTY_KEY_PRINT_SCREEN,
    GHOSTTY_KEY_SCROLL_LOCK,
    GHOSTTY_KEY_PAUSE,

    GHOSTTY_KEY_BROWSER_BACK,
    GHOSTTY_KEY_BROWSER_FAVORITES,
    GHOSTTY_KEY_BROWSER_FORWARD,
    GHOSTTY_KEY_BROWSER_HOME,
    GHOSTTY_KEY_BROWSER_REFRESH,
    GHOSTTY_KEY_BROWSER_SEARCH,
    GHOSTTY_KEY_BROWSER_STOP,
    GHOSTTY_KEY_EJECT,
    GHOSTTY_KEY_LAUNCH_APP_1,
    GHOSTTY_KEY_LAUNCH_APP_2,
    GHOSTTY_KEY_LAUNCH_MAIL,
    GHOSTTY_KEY_MEDIA_PLAY_PAUSE,
    GHOSTTY_KEY_MEDIA_SELECT,
    GHOSTTY_KEY_MEDIA_STOP,
    GHOSTTY_KEY_MEDIA_TRACK_NEXT,
    GHOSTTY_KEY_MEDIA_TRACK_PREVIOUS,
    GHOSTTY_KEY_POWER,
    GHOSTTY_KEY_SLEEP,
    GHOSTTY_KEY_AUDIO_VOLUME_DOWN,
    GHOSTTY_KEY_AUDIO_VOLUME_MUTE,
    GHOSTTY_KEY_AUDIO_VOLUME_UP,
    GHOSTTY_KEY_WAKE_UP,

    GHOSTTY_KEY_COPY,
    GHOSTTY_KEY_CUT,
    GHOSTTY_KEY_PASTE,
  }

  // Kitty keyboard protocol flags
  [Flags]
  internal enum GhosttyKittyKeyFlags : byte
  {
    GHOSTTY_KITTY_KEY_DISABLED = 0,
    GHOSTTY_KITTY_KEY_DISAMBIGUATE = (1 << 0),
    GHOSTTY_KITTY_KEY_REPORT_EVENTS = (1 << 1),
    GHOSTTY_KITTY_KEY_REPORT_ALTERNATES = (1 << 2),
    GHOSTTY_KITTY_KEY_REPORT_ALL = (1 << 3),
    GHOSTTY_KITTY_KEY_REPORT_ASSOCIATED = (1 << 4),
    GHOSTTY_KITTY_KEY_ALL = GHOSTTY_KITTY_KEY_DISAMBIGUATE | GHOSTTY_KITTY_KEY_REPORT_EVENTS | GHOSTTY_KITTY_KEY_REPORT_ALTERNATES | GHOSTTY_KITTY_KEY_REPORT_ALL | GHOSTTY_KITTY_KEY_REPORT_ASSOCIATED,
  }

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyResult ghostty_key_encoder_new(IntPtr allocator, out IntPtr encoder);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_encoder_free(IntPtr encoder);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_encoder_setopt(IntPtr encoder, GhosttyKeyEncoderOption option, IntPtr value);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyResult ghostty_key_encoder_encode(
    IntPtr encoder,
    IntPtr @event,
    IntPtr out_buf,
    UIntPtr out_buf_size,
    out UIntPtr out_len
  );

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyResult ghostty_key_event_new(IntPtr allocator, out IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_free(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_action(IntPtr @event, GhosttyKeyAction action);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyKeyAction ghostty_key_event_get_action(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_key(IntPtr @event, GhosttyKeyCode key);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyKeyCode ghostty_key_event_get_key(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_mods(IntPtr @event, GhosttyMods mods);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyMods ghostty_key_event_get_mods(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_consumed_mods(IntPtr @event, GhosttyMods consumed_mods);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern GhosttyMods ghostty_key_event_get_consumed_mods(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_composing(IntPtr @event, [MarshalAs(UnmanagedType.I1)] bool composing);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  [return: MarshalAs(UnmanagedType.I1)]
  internal static extern bool ghostty_key_event_get_composing(IntPtr @event);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_utf8(IntPtr @event, IntPtr utf8, UIntPtr len);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern IntPtr ghostty_key_event_get_utf8(IntPtr @event, out UIntPtr len);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern void ghostty_key_event_set_unshifted_codepoint(IntPtr @event, uint codepoint);

  [DllImport(GhosttyDll.DllName, CallingConvention = CallingConvention.Cdecl)]
  internal static extern uint ghostty_key_event_get_unshifted_codepoint(IntPtr @event);

}
