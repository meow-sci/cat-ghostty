import { DEFAULT_SGR_STATE, cloneScreenRow } from "./screenGrid";
import type { ScreenCell } from "./screenTypes";
import type { TerminalState } from "./state";

function clampInt(v: number, min: number, max: number): number {
  if (!Number.isFinite(v)) {
    return min;
  }
  return Math.max(min, Math.min(max, Math.trunc(v)));
}

export interface GetViewportRowsOptions {
  cols: number;
  cells: ScreenCell[][];
  scrollback: ScreenCell[][];
  isAlternateScreenActive: boolean;
  viewportTopRow: number;
  rows: number;
}

export function getViewportRows(options: GetViewportRowsOptions): ReadonlyArray<ReadonlyArray<ScreenCell>> {
  const nRows = Math.max(0, Math.trunc(options.rows));
  if (nRows === 0) {
    return [];
  }

  if (options.isAlternateScreenActive) {
    // Real-world terminals typically do not show primary scrollback while an
    // alternate screen app is active.
    return options.cells;
  }

  const scrollbackRows = options.scrollback.length;
  const top = clampInt(options.viewportTopRow, 0, scrollbackRows);
  const out: Array<ReadonlyArray<ScreenCell>> = [];
  out.length = nRows;

  for (let i = 0; i < nRows; i++) {
    const globalRow = top + i;
    if (globalRow < scrollbackRows) {
      out[i] = options.scrollback[globalRow];
      continue;
    }
    const screenRow = globalRow - scrollbackRows;
    out[i] = options.cells[screenRow] ??
      Array.from({ length: options.cols }, () => ({ ch: " ", sgrState: DEFAULT_SGR_STATE, isProtected: false }));
  }

  return out;
}

export function pushScrollbackRow(state: TerminalState, row: ReadonlyArray<ScreenCell>): void {
  if (state.scrollbackLimit <= 0) {
    return;
  }

  state.scrollback.push(cloneScreenRow(row));
  if (state.scrollback.length <= state.scrollbackLimit) {
    return;
  }
  state.scrollback = state.scrollback.slice(-state.scrollbackLimit);
}

export function clearScrollback(state: TerminalState): void {
  state.scrollback = [];
}
