import { getLogger } from "@catty/log";
import type { CsiMessage, EscMessage, OscMessage, SgrSequence } from "@catty/terminal-emulation";
import { Parser } from "@catty/terminal-emulation";

import type { TerminalTraceChunk, TraceControlName } from "./TerminalTrace";

export type DecModeEvent = {
  action: "set" | "reset";
  raw: string;
  modes: number[];
};

export interface ScreenCell {
  ch: string;
}

export interface ScreenSnapshot {
  cols: number;
  rows: number;
  cursorX: number;
  cursorY: number;
  cursorStyle: number;
  cursorVisible: boolean;
  cells: ReadonlyArray<ReadonlyArray<ScreenCell>>;
}

export interface StatefulTerminalOptions {
  cols: number;
  rows: number;
  onUpdate?: (snapshot: ScreenSnapshot) => void;
  onChunk?: (chunk: TerminalTraceChunk) => void;
}

type UpdateListener = (snapshot: ScreenSnapshot) => void;
type DecModeListener = (ev: DecModeEvent) => void;
type ChunkListener = (chunk: TerminalTraceChunk) => void;

function createCellGrid(cols: number, rows: number): ScreenCell[][] {
  return Array.from({ length: rows }, () =>
    Array.from({ length: cols }, () => ({ ch: " " })),
  );
}

type XY = [number, number];

export class StatefulTerminal {
  public readonly cols: number;
  public readonly rows: number;

  private readonly log = getLogger();
  private readonly parser: Parser;

  private cursorX = 0;
  private cursorY = 0;
  private savedCursor: XY | null = null;
  private cursorStyle = 1;
  private cursorVisible = true;
  private wrapPending = false;

  private readonly cells: ScreenCell[][];
  private readonly updateListeners = new Set<UpdateListener>();
  private readonly decModeListeners = new Set<DecModeListener>();
  private readonly chunkListeners = new Set<ChunkListener>();

  constructor(options: StatefulTerminalOptions) {

    this.cols = options.cols;
    this.rows = options.rows;

    this.cells = createCellGrid(this.cols, this.rows);

    if (options.onUpdate) {
      this.updateListeners.add(options.onUpdate);
    }

    if (options.onChunk) {
      this.chunkListeners.add(options.onChunk);
    }

    this.parser = new Parser({
      log: this.log,
      emitNormalBytesDuringEscapeSequence: false,
      processC0ControlsDuringEscapeSequence: true,
      handlers: {
        handleBell: () => {
          this.emitControlChunk("BEL", 0x07);
        },
        handleBackspace: () => {
          this.emitControlChunk("BS", 0x08);
          this.backspace();
          this.emitUpdate();
        },
        handleTab: () => {
          this.emitControlChunk("TAB", 0x09);
          this.tab();
          this.emitUpdate();
        },
        handleLineFeed: () => {
          this.emitControlChunk("LF", 0x0a);
          this.lineFeed();
          this.emitUpdate();
        },
        handleFormFeed: () => {
          this.emitControlChunk("FF", 0x0c);
          this.clear();
          this.emitUpdate();
        },
        handleCarriageReturn: () => {
          this.emitControlChunk("CR", 0x0d);
          this.carriageReturn();
          this.emitUpdate();
        },
        handleNormalByte: (byte: number) => {
          this.emitChunk({
            _type: "trace.normalByte",
            cursorX: this.cursorX,
            cursorY: this.cursorY,
            byte,
          });
          this.writePrintableByte(byte);
          this.emitUpdate();
        },
        handleEsc: (msg: EscMessage) => {
          this.emitChunk({ _type: "trace.esc", cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleEsc(msg);
          this.emitUpdate();
        },
        handleCsi: (msg: CsiMessage) => {
          this.emitChunk({ _type: "trace.csi", cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleCsi(msg);
          this.emitUpdate();
        },
        handleOsc: (msg: OscMessage) => {
          this.emitChunk({ _type: "trace.osc", cursorX: this.cursorX, cursorY: this.cursorY, msg });
        },
        handleSgr: (msg: SgrSequence) => {
          this.emitChunk({ _type: "trace.sgr", cursorX: this.cursorX, cursorY: this.cursorY, msg });
          // ignore styling for MVP
        },
      },
    });
  }

  public onUpdate(listener: UpdateListener): () => void {
    this.updateListeners.add(listener);
    return () => this.updateListeners.delete(listener);
  }

  public onDecMode(listener: DecModeListener): () => void {
    this.decModeListeners.add(listener);
    return () => this.decModeListeners.delete(listener);
  }

  public onChunk(listener: ChunkListener): () => void {
    this.chunkListeners.add(listener);
    return () => this.chunkListeners.delete(listener);
  }

  public getSnapshot(): ScreenSnapshot {
    return {
      cols: this.cols,
      rows: this.rows,
      cursorX: this.cursorX,
      cursorY: this.cursorY,
      cursorStyle: this.cursorStyle,
      cursorVisible: this.cursorVisible,
      cells: this.cells,
    };
  }

  public pushPtyText(text: string): void {
    const bytes = new TextEncoder().encode(text);
    this.parser.pushBytes(bytes);
  }

  public reset(): void {
    this.cursorX = 0;
    this.cursorY = 0;
    this.savedCursor = null;
    this.wrapPending = false;
    this.clear();
    this.emitUpdate();
  }

  private emitUpdate(): void {
    const snapshot = this.getSnapshot();
    for (const listener of this.updateListeners) {
      listener(snapshot);
    }
  }

  private emitDecMode(ev: DecModeEvent): void {
    for (const listener of this.decModeListeners) {
      listener(ev);
    }
  }

  private emitChunk(chunk: TerminalTraceChunk): void {
    for (const listener of this.chunkListeners) {
      listener(chunk);
    }
  }

  private emitControlChunk(name: TraceControlName, byte: number): void {
    this.emitChunk({ _type: "trace.control", cursorX: this.cursorX, cursorY: this.cursorY, name, byte });
  }

  private writePrintableByte(byte: number): void {
    if (byte < 0x20 || byte === 0x7f) {
      return;
    }

    const ch = String.fromCharCode(byte);
    this.putChar(ch);
  }

  private putChar(ch: string): void {
    if (this.cols <= 0 || this.rows <= 0) {
      return;
    }

    if (this.cursorY < 0 || this.cursorY >= this.rows) {
      return;
    }

    if (this.cursorX < 0) {
      this.cursorX = 0;
    }

    // Common terminal behavior (DECAWM autowrap): writing a printable char in
    // the last column sets a pending-wrap flag. The *next* printable char
    // triggers the wrap to column 0 of the next row (with scrolling).
    if (this.wrapPending) {
      this.cursorX = 0;
      this.cursorY += 1;
      if (this.cursorY >= this.rows) {
        this.scrollUp(1);
        this.cursorY = this.rows - 1;
      }
      this.wrapPending = false;
    }

    if (this.cursorX >= this.cols) {
      // Best-effort clamp; cursorX should normally remain in-bounds.
      this.cursorX = this.cols - 1;
    }

    this.cells[this.cursorY][this.cursorX].ch = ch;

    if (this.cursorX === this.cols - 1) {
      this.wrapPending = true;
      return;
    }

    this.cursorX += 1;
  }

  private carriageReturn(): void {
    this.cursorX = 0;
    this.wrapPending = false;
  }

  private lineFeed(): void {
    this.cursorY += 1;
    if (this.cursorY >= this.rows) {
      this.scrollUp(1);
      this.cursorY = this.rows - 1;
    }
    this.wrapPending = false;
  }

  private backspace(): void {
    this.wrapPending = false;
    if (this.cursorX > 0) {
      this.cursorX -= 1;
      this.cells[this.cursorY][this.cursorX].ch = " ";
      return;
    }
  }

  private tab(): void {
    this.wrapPending = false;
    const next = Math.min(this.cols - 1, ((Math.floor(this.cursorX / 8) + 1) * 8));
    while (this.cursorX < next) {
      this.putChar(" ");
    }
  }

  private clear(): void {
    for (let y = 0; y < this.rows; y++) {
      for (let x = 0; x < this.cols; x++) {
        this.cells[y][x].ch = " ";
      }
    }
    this.cursorX = 0;
    this.cursorY = 0;
    this.wrapPending = false;
  }

  private scrollUp(lines: number): void {
    this.wrapPending = false;
    const n = Math.max(0, Math.min(lines, this.rows));
    if (n === 0) {
      return;
    }

    for (let i = 0; i < n; i++) {
      this.cells.shift();
      this.cells.push(Array.from({ length: this.cols }, () => ({ ch: " " })));
    }
  }

  private clampCursor(): void {
    this.cursorX = Math.max(0, Math.min(this.cols - 1, this.cursorX));
    this.cursorY = Math.max(0, Math.min(this.rows - 1, this.cursorY));
    this.wrapPending = false;
  }

  private clearLine(mode: 0 | 1 | 2): void {
    this.wrapPending = false;
    const y = this.cursorY;
    if (y < 0 || y >= this.rows) {
      return;
    }

    if (mode === 0) {
      for (let x = this.cursorX; x < this.cols; x++) this.cells[y][x].ch = " ";
      return;
    }

    if (mode === 1) {
      for (let x = 0; x <= this.cursorX; x++) this.cells[y][x].ch = " ";
      return;
    }

    for (let x = 0; x < this.cols; x++) this.cells[y][x].ch = " ";
  }

  private clearDisplay(mode: 0 | 1 | 2 | 3): void {
    this.wrapPending = false;
    if (mode === 2 || mode === 3) {
      this.clear();
      return;
    }

    if (mode === 0) {
      // from cursor to end
      this.clearLine(0);
      for (let y = this.cursorY + 1; y < this.rows; y++) {
        for (let x = 0; x < this.cols; x++) this.cells[y][x].ch = " ";
      }
      return;
    }

    if (mode === 1) {
      // from start to cursor
      for (let y = 0; y < this.cursorY; y++) {
        for (let x = 0; x < this.cols; x++) this.cells[y][x].ch = " ";
      }
      this.clearLine(1);
      return;
    }
  }

  private handleCsi(msg: CsiMessage): void {
    switch (msg._type) {
      case "csi.cursorUp":
        this.cursorY -= Math.max(1, msg.count);
        this.clampCursor();
        return;
      case "csi.cursorDown":
        this.cursorY += Math.max(1, msg.count);
        this.clampCursor();
        return;
      case "csi.cursorForward":
        this.cursorX += Math.max(1, msg.count);
        this.clampCursor();
        return;
      case "csi.cursorBackward":
        this.cursorX -= Math.max(1, msg.count);
        this.clampCursor();
        return;
      case "csi.cursorNextLine":
        this.cursorY += Math.max(1, msg.count);
        this.cursorX = 0;
        this.clampCursor();
        return;
      case "csi.cursorPrevLine":
        this.cursorY -= Math.max(1, msg.count);
        this.cursorX = 0;
        this.clampCursor();
        return;
      case "csi.cursorHorizontalAbsolute":
        this.cursorX = Math.max(0, Math.min(this.cols - 1, msg.column - 1));
        this.wrapPending = false;
        return;
      case "csi.cursorPosition":
        this.cursorY = Math.max(0, Math.min(this.rows - 1, msg.row - 1));
        this.cursorX = Math.max(0, Math.min(this.cols - 1, msg.column - 1));
        this.wrapPending = false;
        return;
      case "csi.eraseInLine":
        this.clearLine(msg.mode);
        return;
      case "csi.eraseInDisplay":
        this.clearDisplay(msg.mode);
        return;
      case "csi.scrollUp":
        this.scrollUp(msg.lines);
        return;
      case "csi.saveCursorPosition":
        this.savedCursor = [this.cursorX, this.cursorY];
        return;
      case "csi.restoreCursorPosition":
        if (this.savedCursor) {
          this.cursorX = this.savedCursor[0];
          this.cursorY = this.savedCursor[1];
          this.clampCursor();
        }
        return;

      case "csi.decModeSet":
        // DECTCEM (CSI ? 25 h): show cursor
        if (msg.modes.includes(25)) {
          this.cursorVisible = true;
        }
        this.emitDecMode({ action: "set", raw: msg.raw, modes: msg.modes });
        return;

      case "csi.decModeReset":
        // DECTCEM (CSI ? 25 l): hide cursor
        if (msg.modes.includes(25)) {
          this.cursorVisible = false;
        }
        this.emitDecMode({ action: "reset", raw: msg.raw, modes: msg.modes });
        return;

      case "csi.setCursorStyle":
        // DECSCUSR (CSI Ps SP q)
        // Ps:
        // 0 or 1 = blinking block
        // 2 = steady block
        // 3 = blinking underline
        // 4 = steady underline
        // 5 = blinking bar
        // 6 = steady bar
        this.cursorStyle = msg.style;
        return;

      // ignored (for MVP)
      case "csi.scrollDown":
      case "csi.setScrollRegion":
      case "csi.unknown":
        return;
    }
  }

  private handleEsc(msg: EscMessage): void {
    switch (msg._type) {
      case "esc.saveCursor":
        this.savedCursor = [this.cursorX, this.cursorY];
        return;
      case "esc.restoreCursor":
        if (this.savedCursor) {
          this.cursorX = this.savedCursor[0];
          this.cursorY = this.savedCursor[1];
          this.clampCursor();
        }
        return;
    }
  }
}
