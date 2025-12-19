import { createDefaultSgrState, type SgrState } from "../SgrStyleManager";

import type { ScreenCell } from "./screenTypes";

export const DEFAULT_SGR_STATE: Readonly<SgrState> = Object.freeze(createDefaultSgrState());

export function createCellGrid(cols: number, rows: number): ScreenCell[][] {
  return Array.from({ length: rows }, () =>
    Array.from({ length: cols }, () => ({ ch: " ", sgrState: DEFAULT_SGR_STATE, isProtected: false })),
  );
}

export function cloneScreenCell(cell: ScreenCell): ScreenCell {
  return {
    ch: cell.ch,
    sgrState: cell.sgrState ? { ...cell.sgrState } : undefined,
    isProtected: cell.isProtected,
  };
}

export function cloneScreenRow(row: ReadonlyArray<ScreenCell>): ScreenCell[] {
  return row.map(cloneScreenCell);
}
