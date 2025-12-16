/**
 * CSI messages (non-SGR). SGR (final "m") is treated as opaque and not parsed here.
 */
export interface CsiBase {
  _type: string;
  raw: string;
}

// =============================================================================
// ESC Types (non-CSI)
// =============================================================================

export interface EscBase {
  _type: string;
  raw: string;
}

/**
 * DECSC (ESC 7): Save cursor.
 */
export interface EscSaveCursor extends EscBase {
  _type: "esc.saveCursor";
}

/**
 * DECRC (ESC 8): Restore cursor.
 */
export interface EscRestoreCursor extends EscBase {
  _type: "esc.restoreCursor";
}

export type EscMessage = EscSaveCursor | EscRestoreCursor;

export interface CsiCursorUp extends CsiBase {
  _type: "csi.cursorUp";
  count: number;
}

export interface CsiCursorDown extends CsiBase {
  _type: "csi.cursorDown";
  count: number;
}

export interface CsiCursorForward extends CsiBase {
  _type: "csi.cursorForward";
  count: number;
}

export interface CsiCursorBackward extends CsiBase {
  _type: "csi.cursorBackward";
  count: number;
}

export interface CsiCursorNextLine extends CsiBase {
  _type: "csi.cursorNextLine";
  count: number;
}

export interface CsiCursorPrevLine extends CsiBase {
  _type: "csi.cursorPrevLine";
  count: number;
}

export interface CsiCursorHorizontalAbsolute extends CsiBase {
  _type: "csi.cursorHorizontalAbsolute";
  column: number;
}

export interface CsiCursorPosition extends CsiBase {
  _type: "csi.cursorPosition";
  row: number;
  column: number;
}

export type CsiEraseInDisplayMode = 0 | 1 | 2 | 3;

export interface CsiEraseInDisplay extends CsiBase {
  _type: "csi.eraseInDisplay";
  mode: CsiEraseInDisplayMode;
}

export type CsiEraseInLineMode = 0 | 1 | 2;

export interface CsiEraseInLine extends CsiBase {
  _type: "csi.eraseInLine";
  mode: CsiEraseInLineMode;
}

export interface CsiScrollUp extends CsiBase {
  _type: "csi.scrollUp";
  lines: number;
}

export interface CsiScrollDown extends CsiBase {
  _type: "csi.scrollDown";
  lines: number;
}

export interface CsiSetScrollRegion extends CsiBase {
  _type: "csi.setScrollRegion";
  top?: number;
  bottom?: number;
}

export interface CsiSaveCursorPosition extends CsiBase {
  _type: "csi.saveCursorPosition";
}

export interface CsiRestoreCursorPosition extends CsiBase {
  _type: "csi.restoreCursorPosition";
}

export interface CsiDecModeSet extends CsiBase {
  _type: "csi.decModeSet";
  modes: number[];
}

export interface CsiDecModeReset extends CsiBase {
  _type: "csi.decModeReset";
  modes: number[];
}

export interface CsiSetCursorStyle extends CsiBase {
  _type: "csi.setCursorStyle";
  style: number;
}

export interface CsiUnknown extends CsiBase {
  _type: "csi.unknown";
  private: boolean;
  params: number[];
  intermediate: string;
  final: string;
}

export type CsiMessage =
  | CsiCursorUp
  | CsiCursorDown
  | CsiCursorForward
  | CsiCursorBackward
  | CsiCursorNextLine
  | CsiCursorPrevLine
  | CsiCursorHorizontalAbsolute
  | CsiCursorPosition
  | CsiEraseInDisplay
  | CsiEraseInLine
  | CsiScrollUp
  | CsiScrollDown
  | CsiSetScrollRegion
  | CsiSaveCursorPosition
  | CsiRestoreCursorPosition
  | CsiDecModeSet
  | CsiDecModeReset
  | CsiSetCursorStyle
  | CsiUnknown;

// =============================================================================
// SGR (Select Graphic Rendition) Types
// =============================================================================

/**
 * Base interface for all SGR messages.
 */
export interface SgrBase {
  _type: string;
}

/**
 * SGR 0: Reset all attributes to default.
 */
export interface SgrReset extends SgrBase {
  _type: "sgr.reset";
}

/**
 * SGR 1: Bold or increased intensity.
 */
export interface SgrBold extends SgrBase {
  _type: "sgr.bold";
}

/**
 * SGR 2: Faint, decreased intensity, or dim.
 */
export interface SgrFaint extends SgrBase {
  _type: "sgr.faint";
}

/**
 * SGR 3: Italic.
 */
export interface SgrItalic extends SgrBase {
  _type: "sgr.italic";
}

/**
 * Underline style for SGR 4 and SGR 4:n sequences.
 */
export type SgrUnderlineStyle = "single" | "double" | "curly" | "dotted" | "dashed";

/**
 * SGR 4: Underline. SGR 4:0 = none, 4:1 = single, 4:2 = double, 4:3 = curly, 4:4 = dotted, 4:5 = dashed.
 */
export interface SgrUnderline extends SgrBase {
  _type: "sgr.underline";
  style: SgrUnderlineStyle;
}

/**
 * SGR 5: Slow blink (less than 150 per minute).
 */
export interface SgrSlowBlink extends SgrBase {
  _type: "sgr.slowBlink";
}

/**
 * SGR 6: Rapid blink (150+ per minute).
 */
export interface SgrRapidBlink extends SgrBase {
  _type: "sgr.rapidBlink";
}

/**
 * SGR 7: Reverse video / invert.
 */
export interface SgrInverse extends SgrBase {
  _type: "sgr.inverse";
}

/**
 * SGR 8: Conceal / hidden.
 */
export interface SgrHidden extends SgrBase {
  _type: "sgr.hidden";
}

/**
 * SGR 9: Crossed-out / strikethrough.
 */
export interface SgrStrikethrough extends SgrBase {
  _type: "sgr.strikethrough";
}

/**
 * SGR 10-19: Font selection (10 = primary, 11-19 = alternative fonts).
 */
export interface SgrFont extends SgrBase {
  _type: "sgr.font";
  font: number; // 0 = primary, 1-9 = alternative fonts
}

/**
 * SGR 20: Fraktur (Gothic) - rarely supported.
 */
export interface SgrFraktur extends SgrBase {
  _type: "sgr.fraktur";
}

/**
 * SGR 21: Doubly underlined (or not bold on some terminals).
 */
export interface SgrDoubleUnderline extends SgrBase {
  _type: "sgr.doubleUnderline";
}

/**
 * SGR 22: Normal intensity (neither bold nor faint).
 */
export interface SgrNormalIntensity extends SgrBase {
  _type: "sgr.normalIntensity";
}

/**
 * SGR 23: Not italic, not fraktur.
 */
export interface SgrNotItalic extends SgrBase {
  _type: "sgr.notItalic";
}

/**
 * SGR 24: Not underlined.
 */
export interface SgrNotUnderlined extends SgrBase {
  _type: "sgr.notUnderlined";
}

/**
 * SGR 25: Not blinking.
 */
export interface SgrNotBlinking extends SgrBase {
  _type: "sgr.notBlinking";
}

/**
 * SGR 26: Proportional spacing (ITU T.61/T.416).
 */
export interface SgrProportionalSpacing extends SgrBase {
  _type: "sgr.proportionalSpacing";
}

/**
 * SGR 27: Not reversed.
 */
export interface SgrNotInverse extends SgrBase {
  _type: "sgr.notInverse";
}

/**
 * SGR 28: Reveal (not hidden).
 */
export interface SgrNotHidden extends SgrBase {
  _type: "sgr.notHidden";
}

/**
 * SGR 29: Not crossed out.
 */
export interface SgrNotStrikethrough extends SgrBase {
  _type: "sgr.notStrikethrough";
}

/**
 * Color types for foreground/background colors.
 */
export type SgrColorType =
  | { type: "named"; color: SgrNamedColor }
  | { type: "indexed"; index: number }
  | { type: "rgb"; r: number; g: number; b: number };

/**
 * Named colors for SGR 30-37, 40-47, 90-97, 100-107.
 */
export type SgrNamedColor =
  | "black"
  | "red"
  | "green"
  | "yellow"
  | "blue"
  | "magenta"
  | "cyan"
  | "white"
  | "brightBlack"
  | "brightRed"
  | "brightGreen"
  | "brightYellow"
  | "brightBlue"
  | "brightMagenta"
  | "brightCyan"
  | "brightWhite";

/**
 * SGR 30-37, 38, 90-97: Set foreground color.
 */
export interface SgrForegroundColor extends SgrBase {
  _type: "sgr.foregroundColor";
  color: SgrColorType;
}

/**
 * SGR 39: Default foreground color.
 */
export interface SgrDefaultForeground extends SgrBase {
  _type: "sgr.defaultForeground";
}

/**
 * SGR 40-47, 48, 100-107: Set background color.
 */
export interface SgrBackgroundColor extends SgrBase {
  _type: "sgr.backgroundColor";
  color: SgrColorType;
}

/**
 * SGR 49: Default background color.
 */
export interface SgrDefaultBackground extends SgrBase {
  _type: "sgr.defaultBackground";
}

/**
 * SGR 50: Disable proportional spacing.
 */
export interface SgrDisableProportionalSpacing extends SgrBase {
  _type: "sgr.disableProportionalSpacing";
}

/**
 * SGR 51: Framed.
 */
export interface SgrFramed extends SgrBase {
  _type: "sgr.framed";
}

/**
 * SGR 52: Encircled.
 */
export interface SgrEncircled extends SgrBase {
  _type: "sgr.encircled";
}

/**
 * SGR 53: Overlined.
 */
export interface SgrOverlined extends SgrBase {
  _type: "sgr.overlined";
}

/**
 * SGR 54: Not framed, not encircled.
 */
export interface SgrNotFramed extends SgrBase {
  _type: "sgr.notFramed";
}

/**
 * SGR 55: Not overlined.
 */
export interface SgrNotOverlined extends SgrBase {
  _type: "sgr.notOverlined";
}

/**
 * SGR 58: Set underline color.
 */
export interface SgrUnderlineColor extends SgrBase {
  _type: "sgr.underlineColor";
  color: SgrColorType;
}

/**
 * SGR 59: Default underline color.
 */
export interface SgrDefaultUnderlineColor extends SgrBase {
  _type: "sgr.defaultUnderlineColor";
}

/**
 * SGR 60-65: Ideogram attributes (rarely supported).
 */
export interface SgrIdeogram extends SgrBase {
  _type: "sgr.ideogram";
  style: "underline" | "doubleUnderline" | "overline" | "doubleOverline" | "stress" | "reset";
}

/**
 * SGR 73: Superscript.
 */
export interface SgrSuperscript extends SgrBase {
  _type: "sgr.superscript";
}

/**
 * SGR 74: Subscript.
 */
export interface SgrSubscript extends SgrBase {
  _type: "sgr.subscript";
}

/**
 * SGR 75: Neither superscript nor subscript.
 */
export interface SgrNotSuperscriptSubscript extends SgrBase {
  _type: "sgr.notSuperscriptSubscript";
}

/**
 * Unknown or unrecognized SGR parameter.
 */
export interface SgrUnknown extends SgrBase {
  _type: "sgr.unknown";
  params: number[];
}

/**
 * Union of all SGR message types.
 */
export type SgrMessage =
  | SgrReset
  | SgrBold
  | SgrFaint
  | SgrItalic
  | SgrUnderline
  | SgrSlowBlink
  | SgrRapidBlink
  | SgrInverse
  | SgrHidden
  | SgrStrikethrough
  | SgrFont
  | SgrFraktur
  | SgrDoubleUnderline
  | SgrNormalIntensity
  | SgrNotItalic
  | SgrNotUnderlined
  | SgrNotBlinking
  | SgrProportionalSpacing
  | SgrNotInverse
  | SgrNotHidden
  | SgrNotStrikethrough
  | SgrForegroundColor
  | SgrDefaultForeground
  | SgrBackgroundColor
  | SgrDefaultBackground
  | SgrDisableProportionalSpacing
  | SgrFramed
  | SgrEncircled
  | SgrOverlined
  | SgrNotFramed
  | SgrNotOverlined
  | SgrUnderlineColor
  | SgrDefaultUnderlineColor
  | SgrIdeogram
  | SgrSuperscript
  | SgrSubscript
  | SgrNotSuperscriptSubscript
  | SgrUnknown;
