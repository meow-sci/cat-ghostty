/**
 * CSI messages (non-SGR). SGR (final "m") is treated as opaque and not parsed here.
 */
export interface CsiBase {
  _type: string;
  raw: string;
  implemented: boolean;
}

// =============================================================================
// OSC Types (opaque)
// =============================================================================

export interface OscMessage {
  _type: "osc";
  raw: string;
  terminator: "BEL" | "ST";
  implemented: boolean;
}

// =============================================================================
// Xterm OSC Extension Types
// =============================================================================

export interface OscBase {
  _type: string;
  raw: string;
  terminator: "BEL" | "ST";
  implemented: boolean;
}

/**
 * OSC 0: Set window title and icon name
 */
export interface OscSetTitleAndIcon extends OscBase {
  _type: "osc.setTitleAndIcon";
  title: string;
}

/**
 * OSC 1: Set icon name
 */
export interface OscSetIconName extends OscBase {
  _type: "osc.setIconName";
  iconName: string;
}

/**
 * OSC 2: Set window title
 */
export interface OscSetWindowTitle extends OscBase {
  _type: "osc.setWindowTitle";
  title: string;
}

/**
 * OSC 21: Query window title
 */
export interface OscQueryWindowTitle extends OscBase {
  _type: "osc.queryWindowTitle";
}

/**
 * OSC 10;?: Query default foreground color
 */
export interface OscQueryForegroundColor extends OscBase {
  _type: "osc.queryForegroundColor";
}

/**
 * OSC 11;?: Query default background color
 */
export interface OscQueryBackgroundColor extends OscBase {
  _type: "osc.queryBackgroundColor";
}

export type XtermOscMessage = 
  | OscSetTitleAndIcon
  | OscSetIconName
  | OscSetWindowTitle
  | OscQueryWindowTitle
  | OscQueryForegroundColor
  | OscQueryBackgroundColor;

// =============================================================================
// ESC Types (non-CSI)
// =============================================================================

export interface EscBase {
  _type: string;
  raw: string;
  implemented: boolean;
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

/**
 * Character set designation sequences (ESC ( X, ESC ) X, ESC * X, ESC + X)
 * where X is the character set identifier
 */
export interface EscDesignateCharacterSet extends EscBase {
  _type: "esc.designateCharacterSet";
  slot: "G0" | "G1" | "G2" | "G3"; // Which character set slot to designate
  charset: string; // Character set identifier (e.g., "B" for ASCII, "0" for DEC Special Graphics)
}

/**
 * RI (ESC M): Reverse Index.
 * Move cursor up one line; if already at the top margin, scroll the scroll-region down.
 */
export interface EscReverseIndex extends EscBase {
  _type: "esc.reverseIndex";
}

/**
 * IND (ESC D): Index.
 * Move cursor down one line; if already at bottom margin, scroll the scroll-region up.
 */
export interface EscIndex extends EscBase {
  _type: "esc.index";
}

/**
 * NEL (ESC E): Next Line.
 * Like CR+LF (move to first column, then index).
 */
export interface EscNextLine extends EscBase {
  _type: "esc.nextLine";
}

/**
 * HTS (ESC H): Horizontal Tab Set.
 * Set a tab stop at the current column.
 */
export interface EscHorizontalTabSet extends EscBase {
  _type: "esc.horizontalTabSet";
}

/**
 * RIS (ESC c): Reset to Initial State.
 */
export interface EscResetToInitialState extends EscBase {
  _type: "esc.resetToInitialState";
}

export type EscMessage =
  | EscSaveCursor
  | EscRestoreCursor
  | EscDesignateCharacterSet
  | EscReverseIndex
  | EscIndex
  | EscNextLine
  | EscHorizontalTabSet
  | EscResetToInitialState;

// =============================================================================
// DCS Types (Device Control String)
// =============================================================================

export interface DcsBase {
  _type: string;
  raw: string;
  terminator: "ST" | "ESC\\";
  implemented: boolean;
}

/**
 * Generic DCS message for device-specific control
 */
export interface DcsMessage extends DcsBase {
  _type: "dcs";
  command: string;
  parameters: string[];
}

export type XtermDcsMessage = DcsMessage;

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

export interface CsiVerticalPositionAbsolute extends CsiBase {
  _type: "csi.verticalPositionAbsolute";
  row: number;
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

/**
 * Delete Lines (DL)
 * CSI Ps M — Delete Ps lines starting at the cursor line within the scroll region.
 */
export interface CsiDeleteLines extends CsiBase {
  _type: "csi.deleteLines";
  count: number;
}

/**
 * Insert Lines (IL)
 * CSI Ps L — Insert Ps blank lines at the cursor line within the scroll region.
 */
export interface CsiInsertLines extends CsiBase {
  _type: "csi.insertLines";
  count: number;
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

/**
 * DECSTR (CSI ! p): Soft terminal reset.
 */
export interface CsiDecSoftReset extends CsiBase {
  _type: "csi.decSoftReset";
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

// =============================================================================
// Xterm Device Query/Response Types
// =============================================================================

/**
 * Device Attributes Query (Primary DA)
 */
export interface CsiDeviceAttributesPrimary extends CsiBase {
  _type: "csi.deviceAttributesPrimary";
}

/**
 * Device Attributes Query (Secondary DA)
 */
export interface CsiDeviceAttributesSecondary extends CsiBase {
  _type: "csi.deviceAttributesSecondary";
}

/**
 * Cursor Position Report Request
 */
export interface CsiCursorPositionReport extends CsiBase {
  _type: "csi.cursorPositionReport";
}

/**
 * Terminal Size Query
 */
export interface CsiTerminalSizeQuery extends CsiBase {
  _type: "csi.terminalSizeQuery";
}

/**
 * Mouse Reporting Mode Control
 */
export interface CsiMouseReportingMode extends CsiBase {
  _type: "csi.mouseReportingMode";
  mode: number; // 1000, 1002, 1003, etc.
  enable: boolean; // true for DECSET, false for DECRST
}

/**
 * Character Set Query Request
 * CSI ? 2 6 n - Query character set
 */
export interface CsiCharacterSetQuery extends CsiBase {
  _type: "csi.characterSetQuery";
}

/**
 * Window Manipulation Commands
 * CSI Ps ; Ps ; Ps t
 */
export interface CsiWindowManipulation extends CsiBase {
  _type: "csi.windowManipulation";
  operation: number;
  params: number[];
}

/**
 * Insert/Replace Mode Control
 * CSI 4 h/l - Set/Reset Insert Mode
 */
export interface CsiInsertMode extends CsiBase {
  _type: "csi.insertMode";
  enable: boolean;
}

/**
 * Erase Character
 * CSI Ps X - Erase Ps characters from cursor position
 */
export interface CsiEraseCharacter extends CsiBase {
  _type: "csi.eraseCharacter";
  count: number;
}

/**
 * Insert Character (ICH)
 * CSI Ps @ - Insert Ps blank characters at cursor position (shift right)
 */
export interface CsiInsertChars extends CsiBase {
  _type: "csi.insertChars";
  count: number;
}

/**
 * Delete Character (DCH)
 * CSI Ps P - Delete Ps characters at cursor position (shift left)
 */
export interface CsiDeleteChars extends CsiBase {
  _type: "csi.deleteChars";
  count: number;
}

/**
 * Enhanced SGR Mode Control
 * CSI > Ps ; Ps m - Enhanced SGR sequences with > prefix
 */
export interface CsiEnhancedSgrMode extends CsiBase {
  _type: "csi.enhancedSgrMode";
  params: number[];
}

/**
 * Private SGR Mode Control  
 * CSI ? Ps m - Private SGR sequences with ? prefix
 */
export interface CsiPrivateSgrMode extends CsiBase {
  _type: "csi.privateSgrMode";
  params: number[];
}

/**
 * SGR with Intermediate Characters
 * CSI Ps % m - SGR sequences with intermediate characters
 */
export interface CsiSgrWithIntermediate extends CsiBase {
  _type: "csi.sgrWithIntermediate";
  params: number[];
  intermediate: string;
}

/**
 * CSI 11 M - Unknown vi sequence (likely delete/insert related)
 * This sequence appears in vi usage but is not part of standard terminal specifications.
 * We handle it gracefully by parsing and acknowledging it.
 */
export interface CsiUnknownViSequence extends CsiBase {
  _type: "csi.unknownViSequence";
  sequenceNumber: number; // The number before M (e.g., 11 in CSI 11M)
}

export type XtermCsiMessage =
  | CsiDeviceAttributesPrimary
  | CsiDeviceAttributesSecondary
  | CsiCursorPositionReport
  | CsiTerminalSizeQuery
  | CsiMouseReportingMode
  | CsiCharacterSetQuery
  | CsiWindowManipulation
  | CsiInsertMode
  | CsiEraseCharacter
  | CsiEnhancedSgrMode
  | CsiPrivateSgrMode
  | CsiSgrWithIntermediate
  | CsiUnknownViSequence;

export type CsiMessage =
  | CsiCursorUp
  | CsiCursorDown
  | CsiCursorForward
  | CsiCursorBackward
  | CsiCursorNextLine
  | CsiCursorPrevLine
  | CsiCursorHorizontalAbsolute
  | CsiVerticalPositionAbsolute
  | CsiCursorPosition
  | CsiEraseInDisplay
  | CsiEraseInLine
  | CsiInsertChars
  | CsiDeleteChars
  | CsiDeleteLines
  | CsiInsertLines
  | CsiScrollUp
  | CsiScrollDown
  | CsiSetScrollRegion
  | CsiSaveCursorPosition
  | CsiRestoreCursorPosition
  | CsiDecModeSet
  | CsiDecModeReset
  | CsiDecSoftReset
  | CsiSetCursorStyle
  | CsiUnknown
  | XtermCsiMessage;

// =============================================================================
// SGR (Select Graphic Rendition) Types
// =============================================================================

/**
 * Base interface for all SGR messages.
 */
export interface SgrBase {
  _type: string;
  implemented: boolean;
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
 * Enhanced SGR mode with > prefix (e.g., CSI > 4 ; 2 m)
 */
export interface SgrEnhancedMode extends SgrBase {
  _type: "sgr.enhancedMode";
  params: number[];
}

/**
 * Private SGR mode with ? prefix (e.g., CSI ? 4 m)
 */
export interface SgrPrivateMode extends SgrBase {
  _type: "sgr.privateMode";
  params: number[];
}

/**
 * SGR with intermediate characters (e.g., CSI 0 % m)
 */
export interface SgrWithIntermediate extends SgrBase {
  _type: "sgr.withIntermediate";
  params: number[];
  intermediate: string;
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
  | SgrEnhancedMode
  | SgrPrivateMode
  | SgrWithIntermediate
  | SgrUnknown;

/**
 * Wrapper for a complete SGR escape sequence (CSI ... m) including the original raw bytes.
 * The individual SGR messages remain stateless; the raw sequence is attached here.
 */
export interface SgrSequence {
  _type: "sgr";
  implemented: boolean;
  raw: string;
  messages: SgrMessage[];
}
