import type { DecModeEvent } from "./screenTypes";

/**
 * Narrow internal interface for protocol handlers (CSI/ESC/OSC).
 *
 * The goal is to allow extracted handler modules to drive terminal behavior
 * without reaching into `StatefulTerminal` internals or `TerminalState` fields.
 */
export interface TerminalActions {
  // Cursor/mode actions
  clampCursor(): void;
  setOriginMode(enable: boolean): void;
  setAutoWrapMode(enable: boolean): void;

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

  // Output events
  emitResponse(response: string): void;
  emitDecMode(ev: DecModeEvent): void;
}
