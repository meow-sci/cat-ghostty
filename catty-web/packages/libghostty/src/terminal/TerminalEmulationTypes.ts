/**
 * CSI messages (non-SGR). SGR (final "m") is treated as opaque and not parsed here.
 */
export interface CsiBase {
  _type: string;
  raw: string;
}

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
