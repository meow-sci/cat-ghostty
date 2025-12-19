import type { SgrState } from "../SgrStyleManager";

import type { ScreenBuffer, ScreenCell } from "./screenTypes";
import type { TerminalState } from "./state";

export interface BufferOpsContext {
  cols: number;
  rows: number;
  getCells: () => ScreenCell[][];
  getCurrentBuffer: () => ScreenBuffer;
  isAlternateScreenActive: () => boolean;
  pushScrollbackRow: (row: ReadonlyArray<ScreenCell>) => void;
  clearScrollback: () => void;
}

export function makeBlankCellWithCurrentSgr(currentSgrState: SgrState, isProtected: boolean): ScreenCell {
  return { ch: " ", sgrState: { ...currentSgrState }, isProtected };
}

export function insertCharsInLine(state: TerminalState, ctx: BufferOpsContext, count: number): void {
  if (count <= 0) {
    return;
  }
  if (state.cursorY < 0 || state.cursorY >= ctx.rows) {
    return;
  }
  if (state.cursorX < 0 || state.cursorX >= ctx.cols) {
    return;
  }

  const n = Math.min(count, ctx.cols - state.cursorX);
  if (n <= 0) {
    return;
  }

  state.wrapPending = false;

  const cells = ctx.getCells();
  const row = cells[state.cursorY];
  for (let x = ctx.cols - 1; x >= state.cursorX + n; x -= 1) {
    row[x] = row[x - n];
  }
  for (let x = 0; x < n; x += 1) {
    row[state.cursorX + x] = makeBlankCellWithCurrentSgr(
      state.currentSgrState,
      state.currentCharacterProtection === "protected",
    );
  }
}

export function deleteCharsInLine(state: TerminalState, ctx: BufferOpsContext, count: number): void {
  if (count <= 0) {
    return;
  }
  if (state.cursorY < 0 || state.cursorY >= ctx.rows) {
    return;
  }
  if (state.cursorX < 0 || state.cursorX >= ctx.cols) {
    return;
  }

  const n = Math.min(count, ctx.cols - state.cursorX);
  if (n <= 0) {
    return;
  }

  state.wrapPending = false;

  const cells = ctx.getCells();
  const row = cells[state.cursorY];
  for (let x = state.cursorX; x < ctx.cols - n; x += 1) {
    row[x] = row[x + n];
  }
  for (let x = ctx.cols - n; x < ctx.cols; x += 1) {
    row[x] = makeBlankCellWithCurrentSgr(state.currentSgrState, false);
  }
}

export function scrollUp(state: TerminalState, ctx: BufferOpsContext, lines: number): void {
  state.wrapPending = false;
  const n = Math.max(0, Math.min(lines, ctx.rows));
  if (n === 0) {
    return;
  }

  const currentBuffer = ctx.getCurrentBuffer();
  const primaryAndFullScreen =
    !ctx.isAlternateScreenActive() && state.scrollTop === 0 && state.scrollBottom === ctx.rows - 1;

  for (let i = 0; i < n; i++) {
    const removed = currentBuffer.cells.shift();
    if (removed && primaryAndFullScreen) {
      ctx.pushScrollbackRow(removed);
    }
    const sgrState = { ...state.currentSgrState };
    currentBuffer.cells.push(Array.from({ length: ctx.cols }, () => ({ ch: " ", sgrState, isProtected: false })));
  }
}

export function putChar(state: TerminalState, ctx: BufferOpsContext, ch: string): void {
  if (ctx.cols <= 0 || ctx.rows <= 0) {
    return;
  }

  if (state.cursorY < 0 || state.cursorY >= ctx.rows) {
    return;
  }

  if (state.cursorX < 0) {
    state.cursorX = 0;
  }

  // Common terminal behavior (DECAWM autowrap): writing a printable char in
  // the last column sets a pending-wrap flag. The *next* printable char
  // triggers the wrap to column 0 of the next row (with scrolling).
  if (state.autoWrapMode && state.wrapPending) {
    state.cursorX = 0;
    state.cursorY += 1;
    if (state.cursorY >= ctx.rows) {
      scrollUp(state, ctx, 1);
      state.cursorY = ctx.rows - 1;
    }
    state.wrapPending = false;
  }

  if (state.cursorX >= ctx.cols) {
    // Best-effort clamp; cursorX should normally remain in-bounds.
    state.cursorX = ctx.cols - 1;
  }

  const cells = ctx.getCells();
  const cell = cells[state.cursorY][state.cursorX];
  cell.ch = ch;
  cell.sgrState = { ...state.currentSgrState };
  cell.isProtected = state.currentCharacterProtection === "protected";

  if (state.cursorX === ctx.cols - 1) {
    if (state.autoWrapMode) {
      state.wrapPending = true;
    }
    return;
  }

  state.cursorX += 1;
}

export function carriageReturn(state: TerminalState): void {
  state.cursorX = 0;
  state.wrapPending = false;
}

export function lineFeed(state: TerminalState, ctx: BufferOpsContext): void {
  state.cursorY += 1;

  // Check if we've moved past the bottom of the scroll region
  if (state.cursorY > state.scrollBottom) {
    // Scroll only within the scroll region
    scrollUpInRegion(state, ctx, 1);
    state.cursorY = state.scrollBottom;
  }

  state.wrapPending = false;
}

export function backspace(state: TerminalState): void {
  state.wrapPending = false;
  if (state.cursorX > 0) {
    state.cursorX -= 1;
    return;
  }
}

export function clear(state: TerminalState, ctx: BufferOpsContext): void {
  const cells = ctx.getCells();
  for (let y = 0; y < ctx.rows; y++) {
    for (let x = 0; x < ctx.cols; x++) {
      cells[y][x].ch = " ";
      cells[y][x].sgrState = { ...state.currentSgrState };
      cells[y][x].isProtected = false;
    }
  }
  state.cursorX = 0;
  state.cursorY = 0;
  state.wrapPending = false;
}

export function clearLine(state: TerminalState, ctx: BufferOpsContext, mode: 0 | 1 | 2): void {
  state.wrapPending = false;
  const y = state.cursorY;
  if (y < 0 || y >= ctx.rows) {
    return;
  }

  const cells = ctx.getCells();

  if (mode === 0) {
    for (let x = state.cursorX; x < ctx.cols; x++) {
      cells[y][x].ch = " ";
      cells[y][x].sgrState = { ...state.currentSgrState };
      cells[y][x].isProtected = false;
    }
    return;
  }

  if (mode === 1) {
    for (let x = 0; x <= state.cursorX; x++) {
      cells[y][x].ch = " ";
      cells[y][x].sgrState = { ...state.currentSgrState };
      cells[y][x].isProtected = false;
    }
    return;
  }

  for (let x = 0; x < ctx.cols; x++) {
    cells[y][x].ch = " ";
    cells[y][x].sgrState = { ...state.currentSgrState };
    cells[y][x].isProtected = false;
  }
}

export function clearLineSelective(state: TerminalState, ctx: BufferOpsContext, mode: 0 | 1 | 2): void {
  state.wrapPending = false;
  const y = state.cursorY;
  if (y < 0 || y >= ctx.rows) {
    return;
  }

  const cells = ctx.getCells();
  const shouldErase = (cell: ScreenCell): boolean => cell.isProtected !== true;

  if (mode === 0) {
    for (let x = state.cursorX; x < ctx.cols; x++) {
      const cell = cells[y][x];
      if (!shouldErase(cell)) {
        continue;
      }
      cell.ch = " ";
      cell.sgrState = { ...state.currentSgrState };
      cell.isProtected = false;
    }
    return;
  }

  if (mode === 1) {
    for (let x = 0; x <= state.cursorX; x++) {
      const cell = cells[y][x];
      if (!shouldErase(cell)) {
        continue;
      }
      cell.ch = " ";
      cell.sgrState = { ...state.currentSgrState };
      cell.isProtected = false;
    }
    return;
  }

  for (let x = 0; x < ctx.cols; x++) {
    const cell = cells[y][x];
    if (!shouldErase(cell)) {
      continue;
    }
    cell.ch = " ";
    cell.sgrState = { ...state.currentSgrState };
    cell.isProtected = false;
  }
}

export function eraseCharacters(state: TerminalState, ctx: BufferOpsContext, count: number): void {
  state.wrapPending = false;
  const y = state.cursorY;
  if (y < 0 || y >= ctx.rows) {
    return;
  }

  const cells = ctx.getCells();

  // Erase 'count' characters starting from cursor position
  const endX = Math.min(state.cursorX + count, ctx.cols);
  for (let x = state.cursorX; x < endX; x++) {
    cells[y][x].ch = " ";
    cells[y][x].sgrState = { ...state.currentSgrState };
    cells[y][x].isProtected = false;
  }
}

export function setScrollRegion(state: TerminalState, rows: number, top?: number, bottom?: number): void {
  // DECSTBM - Set Top and Bottom Margins
  if (top === undefined && bottom === undefined) {
    // Reset to full screen
    state.scrollTop = 0;
    state.scrollBottom = rows - 1;
  } else {
    // Convert from 1-indexed to 0-indexed and validate bounds
    const newTop = top ? Math.max(0, Math.min(rows - 1, top - 1)) : 0;
    const newBottom = bottom ? Math.max(0, Math.min(rows - 1, bottom - 1)) : rows - 1;

    // Ensure top < bottom
    if (newTop < newBottom) {
      state.scrollTop = newTop;
      state.scrollBottom = newBottom;
    }
  }

  // Move cursor to home position within scroll region
  state.cursorX = 0;
  state.cursorY = state.scrollTop;
  state.wrapPending = false;
}

export function scrollUpInRegion(state: TerminalState, ctx: BufferOpsContext, lines: number): void {
  // Scroll only within the defined scroll region
  if (state.scrollTop === 0 && state.scrollBottom === ctx.rows - 1) {
    // When the scroll region covers the full screen, treat this as a normal
    // terminal scroll and append scrolled-off lines to scrollback (primary only).
    scrollUp(state, ctx, lines);
    return;
  }

  const cells = ctx.getCells();

  for (let i = 0; i < lines; i++) {
    // Move all lines up within the scroll region
    for (let y = state.scrollTop; y < state.scrollBottom; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        cells[y][x] = { ...cells[y + 1][x] };
      }
    }

    // Clear the bottom line of the scroll region
    for (let x = 0; x < ctx.cols; x++) {
      cells[state.scrollBottom][x].ch = " ";
      cells[state.scrollBottom][x].sgrState = { ...state.currentSgrState };
      cells[state.scrollBottom][x].isProtected = false;
    }
  }
}

export function scrollDownInRegion(state: TerminalState, ctx: BufferOpsContext, lines: number): void {
  state.wrapPending = false;
  const n = Math.max(0, Math.min(lines, state.scrollBottom - state.scrollTop + 1));
  if (n === 0) {
    return;
  }

  const cells = ctx.getCells();

  for (let i = 0; i < n; i++) {
    // Move all lines down within the scroll region
    for (let y = state.scrollBottom; y > state.scrollTop; y--) {
      for (let x = 0; x < ctx.cols; x++) {
        cells[y][x] = { ...cells[y - 1][x] };
      }
    }

    // Clear the top line of the scroll region
    for (let x = 0; x < ctx.cols; x++) {
      cells[state.scrollTop][x].ch = " ";
      cells[state.scrollTop][x].sgrState = { ...state.currentSgrState };
      cells[state.scrollTop][x].isProtected = false;
    }
  }
}

export function deleteLinesInRegion(state: TerminalState, ctx: BufferOpsContext, count: number): void {
  state.wrapPending = false;

  // DL/IL affect only when the cursor is within the scroll region.
  if (state.cursorY < state.scrollTop || state.cursorY > state.scrollBottom) {
    return;
  }

  const maxDeletable = state.scrollBottom - state.cursorY + 1;
  const n = Math.max(0, Math.min(count, maxDeletable));
  if (n === 0) {
    return;
  }

  const cells = ctx.getCells();

  // Shift lines up within the region starting at cursorY.
  for (let y = state.cursorY; y <= state.scrollBottom - n; y++) {
    for (let x = 0; x < ctx.cols; x++) {
      cells[y][x] = { ...cells[y + n][x] };
    }
  }

  // Clear the newly exposed bottom lines.
  for (let y = state.scrollBottom - n + 1; y <= state.scrollBottom; y++) {
    for (let x = 0; x < ctx.cols; x++) {
      cells[y][x].ch = " ";
      cells[y][x].sgrState = { ...state.currentSgrState };
      cells[y][x].isProtected = false;
    }
  }
}

export function insertLinesInRegion(state: TerminalState, ctx: BufferOpsContext, count: number): void {
  state.wrapPending = false;

  if (state.cursorY < state.scrollTop || state.cursorY > state.scrollBottom) {
    return;
  }

  const maxInsertable = state.scrollBottom - state.cursorY + 1;
  const n = Math.max(0, Math.min(count, maxInsertable));
  if (n === 0) {
    return;
  }

  const cells = ctx.getCells();

  // Shift lines down within the region starting at cursorY.
  for (let y = state.scrollBottom; y >= state.cursorY + n; y--) {
    for (let x = 0; x < ctx.cols; x++) {
      cells[y][x] = { ...cells[y - n][x] };
    }
  }

  // Clear the inserted blank lines.
  for (let y = state.cursorY; y < state.cursorY + n; y++) {
    for (let x = 0; x < ctx.cols; x++) {
      cells[y][x].ch = " ";
      cells[y][x].sgrState = { ...state.currentSgrState };
      cells[y][x].isProtected = state.currentCharacterProtection === "protected";
    }
  }
}

export function clearDisplay(state: TerminalState, ctx: BufferOpsContext, mode: 0 | 1 | 2 | 3): void {
  state.wrapPending = false;
  if (mode === 3) {
    // xterm: ED 3 clears the scrollback as well as the screen.
    ctx.clearScrollback();
    clear(state, ctx);
    return;
  }
  if (mode === 2) {
    clear(state, ctx);
    return;
  }

  const cells = ctx.getCells();

  if (mode === 0) {
    // from cursor to end
    clearLine(state, ctx, 0);
    for (let y = state.cursorY + 1; y < ctx.rows; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        cells[y][x].ch = " ";
        cells[y][x].sgrState = { ...state.currentSgrState };
        cells[y][x].isProtected = false;
      }
    }
    return;
  }

  if (mode === 1) {
    // from start to cursor
    for (let y = 0; y < state.cursorY; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        cells[y][x].ch = " ";
        cells[y][x].sgrState = { ...state.currentSgrState };
        cells[y][x].isProtected = false;
      }
    }
    clearLine(state, ctx, 1);
    return;
  }
}

export function clearDisplaySelective(state: TerminalState, ctx: BufferOpsContext, mode: 0 | 1 | 2 | 3): void {
  state.wrapPending = false;

  const cells = ctx.getCells();
  const shouldErase = (cell: ScreenCell): boolean => cell.isProtected !== true;

  if (mode === 3) {
    // xterm: selective ED 3 clears the scrollback as well.
    ctx.clearScrollback();
    // Fall through to selective full-screen erase.
  }

  if (mode === 2 || mode === 3) {
    for (let y = 0; y < ctx.rows; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        const cell = cells[y][x];
        if (!shouldErase(cell)) {
          continue;
        }
        cell.ch = " ";
        cell.sgrState = { ...state.currentSgrState };
        cell.isProtected = false;
      }
    }
    return;
  }

  if (mode === 0) {
    clearLineSelective(state, ctx, 0);
    for (let y = state.cursorY + 1; y < ctx.rows; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        const cell = cells[y][x];
        if (!shouldErase(cell)) {
          continue;
        }
        cell.ch = " ";
        cell.sgrState = { ...state.currentSgrState };
        cell.isProtected = false;
      }
    }
    return;
  }

  if (mode === 1) {
    for (let y = 0; y < state.cursorY; y++) {
      for (let x = 0; x < ctx.cols; x++) {
        const cell = cells[y][x];
        if (!shouldErase(cell)) {
          continue;
        }
        cell.ch = " ";
        cell.sgrState = { ...state.currentSgrState };
        cell.isProtected = false;
      }
    }
    clearLineSelective(state, ctx, 1);
    return;
  }
}
