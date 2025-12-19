import { createDefaultSgrState, type SgrState } from "../SgrStyleManager";

import type { ScreenBuffer, ScreenCell } from "./screenTypes";

const DEFAULT_SGR_STATE: Readonly<SgrState> = Object.freeze(createDefaultSgrState());

function createCellGrid(cols: number, rows: number): ScreenCell[][] {
  return Array.from({ length: rows }, () =>
    Array.from({ length: cols }, () => ({ ch: " ", sgrState: DEFAULT_SGR_STATE, isProtected: false })),
  );
}

export class AlternateScreenManager {
  private primaryBuffer: ScreenBuffer;
  private alternateBuffer: ScreenBuffer;
  private currentBuffer: "primary" | "alternate" = "primary";
  private readonly cols: number;
  private readonly rows: number;

  constructor(cols: number, rows: number) {
    this.cols = cols;
    this.rows = rows;

    // Initialize primary buffer with empty cells
    this.primaryBuffer = {
      cells: createCellGrid(cols, rows),
      cursorX: 0,
      cursorY: 0,
      savedCursor: null,
      wrapPending: false,
    };

    // Initialize alternate buffer with empty cells
    this.alternateBuffer = {
      cells: createCellGrid(cols, rows),
      cursorX: 0,
      cursorY: 0,
      savedCursor: null,
      wrapPending: false,
    };
  }

  public switchToPrimary(): void {
    this.currentBuffer = "primary";
  }

  public switchToAlternate(): void {
    this.currentBuffer = "alternate";
  }

  public getCurrentBuffer(): ScreenBuffer {
    return this.currentBuffer === "primary" ? this.primaryBuffer : this.alternateBuffer;
  }

  public isAlternateActive(): boolean {
    return this.currentBuffer === "alternate";
  }

  public getPrimaryBuffer(): ScreenBuffer {
    return this.primaryBuffer;
  }

  public getAlternateBuffer(): ScreenBuffer {
    return this.alternateBuffer;
  }

  public clearAlternateBuffer(): void {
    this.alternateBuffer.cells = createCellGrid(this.cols, this.rows);
    this.alternateBuffer.cursorX = 0;
    this.alternateBuffer.cursorY = 0;
    this.alternateBuffer.wrapPending = false;
  }
}
