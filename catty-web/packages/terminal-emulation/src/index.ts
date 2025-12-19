// Parser exports
export { Parser } from "./terminal/Parser";
export { parseOsc, validateOscParameters } from "./terminal/ParseOsc";
export type { ParserOptions } from "./terminal/ParserOptions";
export type {
  HandleBell,
  HandleBackspace,
  HandleTab,
  HandleLineFeed,
  HandleFormFeed,
  HandleCarriageReturn,
  HandleNormalByte,
  HandleCsi,
  HandleEsc,
  HandleOsc,
  HandleSgr,
  HandleXtermOsc,
} from "./terminal/ParserHandlers";
export type { ParserHandlers } from "./terminal/ParserOptions";

// Terminal emulation types
export type {
  // ESC types
  EscBase,
  EscMessage,
  EscSaveCursor,
  EscRestoreCursor,
  // CSI types
  CsiBase,
  CsiMessage,
  CsiCursorUp,
  CsiCursorDown,
  CsiCursorForward,
  CsiCursorBackward,
  CsiCursorNextLine,
  CsiCursorPrevLine,
  CsiCursorHorizontalAbsolute,
  CsiCursorPosition,
  CsiEraseInDisplayMode,
  CsiEraseInDisplay,
  CsiEraseInLineMode,
  CsiEraseInLine,
  CsiScrollUp,
  CsiScrollDown,
  CsiSetScrollRegion,
  CsiSaveCursorPosition,
  CsiRestoreCursorPosition,
  CsiDecModeSet,
  CsiDecModeReset,
  CsiSetCursorStyle,
  CsiUnknown,
  // SGR types
  SgrBase,
  SgrSequence,
  SgrMessage,
  SgrReset,
  SgrBold,
  SgrFaint,
  SgrItalic,
  SgrUnderline,
  SgrUnderlineStyle,
  SgrSlowBlink,
  SgrRapidBlink,
  SgrInverse,
  SgrHidden,
  SgrStrikethrough,
  SgrFont,
  SgrFraktur,
  SgrDoubleUnderline,
  SgrNormalIntensity,
  SgrNotItalic,
  SgrNotUnderlined,
  SgrNotBlinking,
  SgrProportionalSpacing,
  SgrNotInverse,
  SgrNotHidden,
  SgrNotStrikethrough,
  SgrColorType,
  SgrNamedColor,
  SgrForegroundColor,
  SgrDefaultForeground,
  SgrBackgroundColor,
  SgrDefaultBackground,
  SgrDisableProportionalSpacing,
  SgrFramed,
  SgrEncircled,
  SgrOverlined,
  SgrNotFramed,
  SgrNotOverlined,
  SgrUnderlineColor,
  SgrDefaultUnderlineColor,
  SgrIdeogram,
  SgrSuperscript,
  SgrSubscript,
  SgrNotSuperscriptSubscript,
  SgrUnknown,

  // OSC types
  OscMessage,

  // Xterm extension types
  OscBase,
  OscSetTitleAndIcon,
  OscSetIconName,
  OscSetWindowTitle,
  OscQueryWindowTitle,
  XtermOscMessage,
  CsiDeviceAttributesPrimary,
  CsiDeviceAttributesSecondary,
  CsiCursorPositionReport,
  CsiTerminalSizeQuery,
  CsiMouseReportingMode,
  XtermCsiMessage,
  DcsBase,
  DcsMessage,
  XtermDcsMessage,
} from "./terminal/TerminalEmulationTypes";

export { StatefulTerminal, type ScreenSnapshot } from "./terminal/StatefulTerminal";
export { type TerminalTraceChunk, formatTerminalTraceLine } from "./terminal/TerminalTrace";
export { traceSettings } from "./terminal/traceSettings";

// Theme system exports
export { 
  ThemeManager, 
  DEFAULT_DARK_THEME,
  type TerminalTheme,
  type TerminalColorPalette 
} from "./terminal/TerminalTheme";

// SGR styling exports
export {
  SgrStyleManager,
  createDefaultSgrState,
  type SgrState
} from "./terminal/SgrStyleManager";

export {
  processSgrMessages,
  ansiCodeToNamedColor,
  applyInverseVideo
} from "./terminal/SgrStateProcessor";

// DOM style management exports
export {
  DomStyleManager,
  CellClassManager,
} from "./terminal/DomStyleManager";

// Color resolution exports
export {
  ColorResolver,
  Color256Palette,
  ANSI_COLOR_VARIABLES
} from "./terminal/ColorResolver";
// Input exports
export * from "./input/InputTypes";
export * from "./input/InputEncoder";
export * from "./input/ScrollLogic";
