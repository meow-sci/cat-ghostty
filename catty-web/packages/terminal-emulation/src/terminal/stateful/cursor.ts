import type { TerminalState } from "./state";

export function mapRowParamToCursorY(state: TerminalState, row1Based: number): number {
  const base = state.originMode ? state.scrollTop : 0;
  return base + (row1Based - 1);
}

export function clampCursor(state: TerminalState, cols: number, rows: number): void {
  state.cursorX = Math.max(0, Math.min(cols - 1, state.cursorX));
  const y = Math.max(0, Math.min(rows - 1, state.cursorY));
  if (state.originMode) {
    state.cursorY = Math.max(state.scrollTop, Math.min(state.scrollBottom, y));
  } else {
    state.cursorY = y;
  }
  state.wrapPending = false;
}

export function setOriginMode(state: TerminalState, cols: number, rows: number, enable: boolean): void {
  state.originMode = enable;
  // vt100/xterm behavior: home the cursor when toggling origin mode.
  state.cursorX = 0;
  state.cursorY = enable ? state.scrollTop : 0;
  state.wrapPending = false;
  clampCursor(state, cols, rows);
}

export function setAutoWrapMode(state: TerminalState, enable: boolean): void {
  state.autoWrapMode = enable;
  if (!enable) {
    state.wrapPending = false;
  }
}
