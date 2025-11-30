// Map W3C KeyboardEvent.code values to Ghostty key codes
// Based on include/ghostty/vt/key/event.h
export const KeyCodeMap = {
  
  // Writing System Keys
  'Backquote': 1,          // GHOSTTY_KEY_BACKQUOTE
  'Backslash': 2,          // GHOSTTY_KEY_BACKSLASH
  'BracketLeft': 3,        // GHOSTTY_KEY_BRACKET_LEFT
  'BracketRight': 4,       // GHOSTTY_KEY_BRACKET_RIGHT
  'Comma': 5,              // GHOSTTY_KEY_COMMA
  'Digit0': 6,             // GHOSTTY_KEY_DIGIT_0
  'Digit1': 7,             // GHOSTTY_KEY_DIGIT_1
  'Digit2': 8,             // GHOSTTY_KEY_DIGIT_2
  'Digit3': 9,             // GHOSTTY_KEY_DIGIT_3
  'Digit4': 10,            // GHOSTTY_KEY_DIGIT_4
  'Digit5': 11,            // GHOSTTY_KEY_DIGIT_5
  'Digit6': 12,            // GHOSTTY_KEY_DIGIT_6
  'Digit7': 13,            // GHOSTTY_KEY_DIGIT_7
  'Digit8': 14,            // GHOSTTY_KEY_DIGIT_8
  'Digit9': 15,            // GHOSTTY_KEY_DIGIT_9
  'Equal': 16,             // GHOSTTY_KEY_EQUAL
  'IntlBackslash': 17,     // GHOSTTY_KEY_INTL_BACKSLASH
  'IntlRo': 18,            // GHOSTTY_KEY_INTL_RO
  'IntlYen': 19,           // GHOSTTY_KEY_INTL_YEN
  'KeyA': 20,              // GHOSTTY_KEY_A
  'KeyB': 21,              // GHOSTTY_KEY_B
  'KeyC': 22,              // GHOSTTY_KEY_C
  'KeyD': 23,              // GHOSTTY_KEY_D
  'KeyE': 24,              // GHOSTTY_KEY_E
  'KeyF': 25,              // GHOSTTY_KEY_F
  'KeyG': 26,              // GHOSTTY_KEY_G
  'KeyH': 27,              // GHOSTTY_KEY_H
  'KeyI': 28,              // GHOSTTY_KEY_I
  'KeyJ': 29,              // GHOSTTY_KEY_J
  'KeyK': 30,              // GHOSTTY_KEY_K
  'KeyL': 31,              // GHOSTTY_KEY_L
  'KeyM': 32,              // GHOSTTY_KEY_M
  'KeyN': 33,              // GHOSTTY_KEY_N
  'KeyO': 34,              // GHOSTTY_KEY_O
  'KeyP': 35,              // GHOSTTY_KEY_P
  'KeyQ': 36,              // GHOSTTY_KEY_Q
  'KeyR': 37,              // GHOSTTY_KEY_R
  'KeyS': 38,              // GHOSTTY_KEY_S
  'KeyT': 39,              // GHOSTTY_KEY_T
  'KeyU': 40,              // GHOSTTY_KEY_U
  'KeyV': 41,              // GHOSTTY_KEY_V
  'KeyW': 42,              // GHOSTTY_KEY_W
  'KeyX': 43,              // GHOSTTY_KEY_X
  'KeyY': 44,              // GHOSTTY_KEY_Y
  'KeyZ': 45,              // GHOSTTY_KEY_Z
  'Minus': 46,             // GHOSTTY_KEY_MINUS
  'Period': 47,            // GHOSTTY_KEY_PERIOD
  'Quote': 48,             // GHOSTTY_KEY_QUOTE
  'Semicolon': 49,         // GHOSTTY_KEY_SEMICOLON
  'Slash': 50,             // GHOSTTY_KEY_SLASH

  // Functional Keys
  'AltLeft': 51,           // GHOSTTY_KEY_ALT_LEFT
  'AltRight': 52,          // GHOSTTY_KEY_ALT_RIGHT
  'Backspace': 53,         // GHOSTTY_KEY_BACKSPACE
  'CapsLock': 54,          // GHOSTTY_KEY_CAPS_LOCK
  'ContextMenu': 55,       // GHOSTTY_KEY_CONTEXT_MENU
  'ControlLeft': 56,       // GHOSTTY_KEY_CONTROL_LEFT
  'ControlRight': 57,      // GHOSTTY_KEY_CONTROL_RIGHT
  'Enter': 58,             // GHOSTTY_KEY_ENTER
  'MetaLeft': 59,          // GHOSTTY_KEY_META_LEFT
  'MetaRight': 60,         // GHOSTTY_KEY_META_RIGHT
  'ShiftLeft': 61,         // GHOSTTY_KEY_SHIFT_LEFT
  'ShiftRight': 62,        // GHOSTTY_KEY_SHIFT_RIGHT
  'Space': 63,             // GHOSTTY_KEY_SPACE
  'Tab': 64,               // GHOSTTY_KEY_TAB
  'Convert': 65,           // GHOSTTY_KEY_CONVERT
  'KanaMode': 66,          // GHOSTTY_KEY_KANA_MODE
  'NonConvert': 67,        // GHOSTTY_KEY_NON_CONVERT

  // Control Pad Section
  'Delete': 68,            // GHOSTTY_KEY_DELETE
  'End': 69,               // GHOSTTY_KEY_END
  'Help': 70,              // GHOSTTY_KEY_HELP
  'Home': 71,              // GHOSTTY_KEY_HOME
  'Insert': 72,            // GHOSTTY_KEY_INSERT
  'PageDown': 73,          // GHOSTTY_KEY_PAGE_DOWN
  'PageUp': 74,            // GHOSTTY_KEY_PAGE_UP

  // Arrow Pad Section
  'ArrowDown': 75,         // GHOSTTY_KEY_ARROW_DOWN
  'ArrowLeft': 76,         // GHOSTTY_KEY_ARROW_LEFT
  'ArrowRight': 77,        // GHOSTTY_KEY_ARROW_RIGHT
  'ArrowUp': 78,           // GHOSTTY_KEY_ARROW_UP

  // Numpad Section
  'NumLock': 79,           // GHOSTTY_KEY_NUM_LOCK
  'Numpad0': 80,           // GHOSTTY_KEY_NUMPAD_0
  'Numpad1': 81,           // GHOSTTY_KEY_NUMPAD_1
  'Numpad2': 82,           // GHOSTTY_KEY_NUMPAD_2
  'Numpad3': 83,           // GHOSTTY_KEY_NUMPAD_3
  'Numpad4': 84,           // GHOSTTY_KEY_NUMPAD_4
  'Numpad5': 85,           // GHOSTTY_KEY_NUMPAD_5
  'Numpad6': 86,           // GHOSTTY_KEY_NUMPAD_6
  'Numpad7': 87,           // GHOSTTY_KEY_NUMPAD_7
  'Numpad8': 88,           // GHOSTTY_KEY_NUMPAD_8
  'Numpad9': 89,           // GHOSTTY_KEY_NUMPAD_9
  'NumpadAdd': 90,         // GHOSTTY_KEY_NUMPAD_ADD
  'NumpadBackspace': 91,   // GHOSTTY_KEY_NUMPAD_BACKSPACE
  'NumpadClear': 92,       // GHOSTTY_KEY_NUMPAD_CLEAR
  'NumpadClearEntry': 93,  // GHOSTTY_KEY_NUMPAD_CLEAR_ENTRY
  'NumpadComma': 94,       // GHOSTTY_KEY_NUMPAD_COMMA
  'NumpadDecimal': 95,     // GHOSTTY_KEY_NUMPAD_DECIMAL
  'NumpadDivide': 96,      // GHOSTTY_KEY_NUMPAD_DIVIDE
  'NumpadEnter': 97,       // GHOSTTY_KEY_NUMPAD_ENTER
  'NumpadEqual': 98,       // GHOSTTY_KEY_NUMPAD_EQUAL
  'NumpadMemoryAdd': 99,   // GHOSTTY_KEY_NUMPAD_MEMORY_ADD
  'NumpadMemoryClear': 100,// GHOSTTY_KEY_NUMPAD_MEMORY_CLEAR
  'NumpadMemoryRecall': 101,// GHOSTTY_KEY_NUMPAD_MEMORY_RECALL
  'NumpadMemoryStore': 102,// GHOSTTY_KEY_NUMPAD_MEMORY_STORE
  'NumpadMemorySubtract': 103,// GHOSTTY_KEY_NUMPAD_MEMORY_SUBTRACT
  'NumpadMultiply': 104,   // GHOSTTY_KEY_NUMPAD_MULTIPLY
  'NumpadParenLeft': 105,  // GHOSTTY_KEY_NUMPAD_PAREN_LEFT
  'NumpadParenRight': 106, // GHOSTTY_KEY_NUMPAD_PAREN_RIGHT
  'NumpadSubtract': 107,   // GHOSTTY_KEY_NUMPAD_SUBTRACT
  'NumpadSeparator': 108,  // GHOSTTY_KEY_NUMPAD_SEPARATOR
  'NumpadUp': 109,         // GHOSTTY_KEY_NUMPAD_UP
  'NumpadDown': 110,       // GHOSTTY_KEY_NUMPAD_DOWN
  'NumpadRight': 111,      // GHOSTTY_KEY_NUMPAD_RIGHT
  'NumpadLeft': 112,       // GHOSTTY_KEY_NUMPAD_LEFT
  'NumpadBegin': 113,      // GHOSTTY_KEY_NUMPAD_BEGIN
  'NumpadHome': 114,       // GHOSTTY_KEY_NUMPAD_HOME
  'NumpadEnd': 115,        // GHOSTTY_KEY_NUMPAD_END
  'NumpadInsert': 116,     // GHOSTTY_KEY_NUMPAD_INSERT
  'NumpadDelete': 117,     // GHOSTTY_KEY_NUMPAD_DELETE
  'NumpadPageUp': 118,     // GHOSTTY_KEY_NUMPAD_PAGE_UP
  'NumpadPageDown': 119,   // GHOSTTY_KEY_NUMPAD_PAGE_DOWN

  // Function Section
  'Escape': 120,           // GHOSTTY_KEY_ESCAPE
  'F1': 121,               // GHOSTTY_KEY_F1
  'F2': 122,               // GHOSTTY_KEY_F2
  'F3': 123,               // GHOSTTY_KEY_F3
  'F4': 124,               // GHOSTTY_KEY_F4
  'F5': 125,               // GHOSTTY_KEY_F5
  'F6': 126,               // GHOSTTY_KEY_F6
  'F7': 127,               // GHOSTTY_KEY_F7
  'F8': 128,               // GHOSTTY_KEY_F8
  'F9': 129,               // GHOSTTY_KEY_F9
  'F10': 130,              // GHOSTTY_KEY_F10
  'F11': 131,              // GHOSTTY_KEY_F11
  'F12': 132,              // GHOSTTY_KEY_F12
  'F13': 133,              // GHOSTTY_KEY_F13
  'F14': 134,              // GHOSTTY_KEY_F14
  'F15': 135,              // GHOSTTY_KEY_F15
  'F16': 136,              // GHOSTTY_KEY_F16
  'F17': 137,              // GHOSTTY_KEY_F17
  'F18': 138,              // GHOSTTY_KEY_F18
  'F19': 139,              // GHOSTTY_KEY_F19
  'F20': 140,              // GHOSTTY_KEY_F20
  'F21': 141,              // GHOSTTY_KEY_F21
  'F22': 142,              // GHOSTTY_KEY_F22
  'F23': 143,              // GHOSTTY_KEY_F23
  'F24': 144,              // GHOSTTY_KEY_F24
  'F25': 145,              // GHOSTTY_KEY_F25
  'Fn': 146,               // GHOSTTY_KEY_FN
  'FnLock': 147,           // GHOSTTY_KEY_FN_LOCK
  'PrintScreen': 148,      // GHOSTTY_KEY_PRINT_SCREEN
  'ScrollLock': 149,       // GHOSTTY_KEY_SCROLL_LOCK
  'Pause': 150,            // GHOSTTY_KEY_PAUSE

  // Media Keys
  'BrowserBack': 151,      // GHOSTTY_KEY_BROWSER_BACK
  'BrowserFavorites': 152, // GHOSTTY_KEY_BROWSER_FAVORITES
  'BrowserForward': 153,   // GHOSTTY_KEY_BROWSER_FORWARD
  'BrowserHome': 154,      // GHOSTTY_KEY_BROWSER_HOME
  'BrowserRefresh': 155,   // GHOSTTY_KEY_BROWSER_REFRESH
  'BrowserSearch': 156,    // GHOSTTY_KEY_BROWSER_SEARCH
  'BrowserStop': 157,      // GHOSTTY_KEY_BROWSER_STOP
  'Eject': 158,            // GHOSTTY_KEY_EJECT
  'LaunchApp1': 159,       // GHOSTTY_KEY_LAUNCH_APP_1
  'LaunchApp2': 160,       // GHOSTTY_KEY_LAUNCH_APP_2
  'LaunchMail': 161,       // GHOSTTY_KEY_LAUNCH_MAIL
  'MediaPlayPause': 162,   // GHOSTTY_KEY_MEDIA_PLAY_PAUSE
  'MediaSelect': 163,      // GHOSTTY_KEY_MEDIA_SELECT
  'MediaStop': 164,        // GHOSTTY_KEY_MEDIA_STOP
  'MediaTrackNext': 165,   // GHOSTTY_KEY_MEDIA_TRACK_NEXT
  'MediaTrackPrevious': 166,// GHOSTTY_KEY_MEDIA_TRACK_PREVIOUS
  'Power': 167,            // GHOSTTY_KEY_POWER
  'Sleep': 168,            // GHOSTTY_KEY_SLEEP
  'AudioVolumeDown': 169,  // GHOSTTY_KEY_AUDIO_VOLUME_DOWN
  'AudioVolumeMute': 170,  // GHOSTTY_KEY_AUDIO_VOLUME_MUTE
  'AudioVolumeUp': 171,    // GHOSTTY_KEY_AUDIO_VOLUME_UP
  'WakeUp': 172,           // GHOSTTY_KEY_WAKE_UP

  // Legacy, Non-standard, and Special Keys
  'Copy': 173,             // GHOSTTY_KEY_COPY
  'Cut': 174,              // GHOSTTY_KEY_CUT
  'Paste': 175,            // GHOSTTY_KEY_PASTE
};
