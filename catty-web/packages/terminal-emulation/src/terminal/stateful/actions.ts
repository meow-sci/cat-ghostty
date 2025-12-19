import type { DecModeEvent } from "./screenTypes";
import type { SgrState } from "../SgrStyleManager";

/**
 * Narrow internal interface for protocol handlers (CSI/ESC/OSC).
 *
 * The goal is to allow extracted handler modules to drive terminal behavior
 * without reaching into `StatefulTerminal` internals or `TerminalState` fields.
 */
export interface TerminalActions {
  // Introspection / cursor state
  getCols(): number;
  getRows(): number;
  getCursorX(): number;
  getCursorY(): number;

  setCursorX(x: number): void;
  setCursorY(y: number): void;
  setWrapPending(wrapPending: boolean): void;

  mapRowParamToCursorY(row: number): number;

  // Cursor/mode actions
  clampCursor(): void;
  setOriginMode(enable: boolean): void;
  setAutoWrapMode(enable: boolean): void;
  setCursorVisibility(visible: boolean): void;
  setApplicationCursorKeys(enable: boolean): void;
  setUtf8Mode(enable: boolean): void;

  // ESC helpers
  getScrollTop(): number;
  getScrollBottom(): number;
  designateCharacterSet(slot: "G0" | "G1" | "G2" | "G3", charset: string): void;
  lineFeed(): void;
  carriageReturn(): void;
  setTabStopAtCursor(): void;
  resetToInitialState(): void;

  // Tabs
  cursorForwardTab(count: number): void;
  cursorBackwardTab(count: number): void;
  clearTabStopAtCursor(): void;
  clearAllTabStops(): void;

  // DECSCA character protection
  setCharacterProtection(isProtected: boolean): void;

  // Buffer ops
  clearLine(mode: 0 | 1 | 2): void;
  clearLineSelective(mode: 0 | 1 | 2): void;
  clearDisplay(mode: 0 | 1 | 2 | 3): void;
  clearDisplaySelective(mode: 0 | 1 | 2 | 3): void;

  insertCharsInLine(count: number): void;
  deleteCharsInLine(count: number): void;
  eraseCharacters(count: number): void;

  setScrollRegion(top?: number, bottom?: number): void;
  scrollUpInRegion(lines: number): void;
  scrollDownInRegion(lines: number): void;
  deleteLinesInRegion(count: number): void;
  insertLinesInRegion(count: number): void;

  // Cursor save/restore
  saveCursorPosition(): void;
  restoreCursorPosition(): void;

  // XTSAVE/XTRESTORE
  savePrivateMode(modes: number[]): void;
  restorePrivateMode(modes: number[]): void;

  // Screen/mode ops
  softReset(): void;
  setCursorStyle(style: number): void;

  // Alternate screen buffer modes
  switchToAlternateScreen(): void;
  switchToAlternateScreenWithCursorSave(): void;
  switchToAlternateScreenWithCursorSaveAndClear(): void;
  switchToPrimaryScreen(): void;
  switchToPrimaryScreenWithCursorRestore(): void;

  // OSC helpers
  getWindowTitle(): string;
  setWindowTitle(title: string): void;
  setIconName(iconName: string): void;
  setTitleAndIcon(title: string): void;
  getCurrentSgrState(): SgrState;

  // Queries & misc CSI helpers
  generateCharacterSetQueryResponse(): string;
  handleWindowManipulation(operation: number, params: number[]): void;
  handleEnhancedSgrMode(params: number[]): void;
  handlePrivateSgrMode(params: number[]): void;
  handleSgrWithIntermediate(params: number[], intermediate: string): void;
  handleUnknownViSequence(sequenceNumber: number): void;

  // Output events
  emitResponse(response: string): void;
  emitDecMode(ev: DecModeEvent): void;
}
