import { getLogger } from "@catty/log";
import type { CsiMessage, SgrMessage } from "@catty/terminal-emulation";
import { Parser } from "@catty/terminal-emulation";

export interface ScreenCell {
  ch: string;
}

export interface ScreenSnapshot {
  cols: number;
  rows: number;
  cursorX: number;
  cursorY: number;
  cells: ReadonlyArray<ReadonlyArray<ScreenCell>>;
}

export interface StatefulTerminalOptions {
  cols: number;
  rows: number;
  onUpdate?: (snapshot: ScreenSnapshot) => void;
}

type UpdateListener = (snapshot: ScreenSnapshot) => void;

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

  private readonly cells: ScreenCell[][];
  private readonly updateListeners = new Set<UpdateListener>();

  constructor(options: StatefulTerminalOptions) {

    this.cols = options.cols;
    this.rows = options.rows;

    this.cells = createCellGrid(this.cols, this.rows);

    if (options.onUpdate) {
      this.updateListeners.add(options.onUpdate);
    }

    this.parser = new Parser({
      log: this.log,
      emitNormalBytesDuringEscapeSequence: false,
      processC0ControlsDuringEscapeSequence: true,
      handlers: {
        handleBell: () => {
          // noop
        },
        handleBackspace: () => {
          this.backspace();
          this.emitUpdate();
        },
        handleTab: () => {
          this.tab();
          this.emitUpdate();
        },
        handleLineFeed: () => {
          this.lineFeed();
          this.emitUpdate();
        },
        handleFormFeed: () => {
          this.clear();
          this.emitUpdate();
        },
        handleCarriageReturn: () => {
          this.carriageReturn();
          this.emitUpdate();
        },
        handleNormalByte: (byte: number) => {
          this.writePrintableByte(byte);
          this.emitUpdate();
        },
        handleCsi: (msg: CsiMessage) => {
          this.handleCsi(msg);
          this.emitUpdate();
        },
        handleOsc: (_raw: string) => {
          // noop (opaque)
        },
        handleSgr: (_messages: SgrMessage[]) => {
          // ignore styling for MVP
        },
      },
    });
  }

  public onUpdate(listener: UpdateListener): () => void {
    this.updateListeners.add(listener);
    return () => this.updateListeners.delete(listener);
  }

  public getSnapshot(): ScreenSnapshot {
    return {
      cols: this.cols,
      rows: this.rows,
      cursorX: this.cursorX,
      cursorY: this.cursorY,
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
    this.clear();
    this.emitUpdate();
  }

  private emitUpdate(): void {
    const snapshot = this.getSnapshot();
    for (const listener of this.updateListeners) {
      listener(snapshot);
    }
  }

  private writePrintableByte(byte: number): void {
    if (byte < 0x20 || byte === 0x7f) {
      return;
    }

    const ch = String.fromCharCode(byte);
    this.putChar(ch);
  }

  private putChar(ch: string): void {
    if (this.cursorY < 0 || this.cursorY >= this.rows) {
      return;
    }

    if (this.cursorX < 0) {
      this.cursorX = 0;
    }

    if (this.cursorX >= this.cols) {
      this.cursorX = 0;
      this.cursorY += 1;
      
      if (this.cursorY >= this.rows) {
        this.scrollUp(1);
        this.cursorY = this.rows - 1;
      }
    }

    this.cells[this.cursorY][this.cursorX].ch = ch;

    this.cursorX += 1;
    if (this.cursorX >= this.cols) {
      this.cursorX = 0;
      this.cursorY += 1;
      if (this.cursorY >= this.rows) {
        this.scrollUp(1);
        this.cursorY = this.rows - 1;
      }
    }
  }

  private carriageReturn(): void {
    this.cursorX = 0;
  }

  private lineFeed(): void {
    this.cursorY += 1;
    if (this.cursorY >= this.rows) {
      this.scrollUp(1);
      this.cursorY = this.rows - 1;
    }
  }

  private backspace(): void {
    if (this.cursorX > 0) {
      this.cursorX -= 1;
      this.cells[this.cursorY][this.cursorX].ch = " ";
      return;
    }
  }

  private tab(): void {
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
  }

  private scrollUp(lines: number): void {
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
  }

  private clearLine(mode: 0 | 1 | 2): void {
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
        return;
      case "csi.cursorPosition":
        this.cursorY = Math.max(0, Math.min(this.rows - 1, msg.row - 1));
        this.cursorX = Math.max(0, Math.min(this.cols - 1, msg.column - 1));
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

      // ignored (for MVP)
      case "csi.scrollDown":
      case "csi.setScrollRegion":
      case "csi.decModeSet":
      case "csi.decModeReset":
      case "csi.setCursorStyle":
      case "csi.unknown":
        return;
    }
  }
}
