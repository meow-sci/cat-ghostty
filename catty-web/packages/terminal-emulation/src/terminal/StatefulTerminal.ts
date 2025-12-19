import { getLogger } from "@catty/log";

import type { TerminalTraceChunk, TraceControlName } from "./TerminalTrace";
import { Parser } from "./Parser";
import type { EscMessage, CsiMessage, OscMessage, SgrSequence, XtermOscMessage, SgrColorType, SgrNamedColor } from "./TerminalEmulationTypes";
import { createDefaultSgrState, type SgrState } from './SgrStyleManager';
import { processSgrMessages } from './SgrStateProcessor';

export type DecModeEvent = {
  action: "set" | "reset";
  raw: string;
  modes: number[];
};

export interface ScreenCell {
  ch: string;
  sgrState?: SgrState;
}

export interface WindowProperties {
  title: string;
  iconName: string;
}

export interface ScreenBuffer {
  cells: ScreenCell[][];
  cursorX: number;
  cursorY: number;
  savedCursor: [number, number] | null;
  wrapPending: boolean;
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

export interface CursorState {
  x: number;
  y: number;
  visible: boolean;
  style: number;
  applicationMode: boolean;
  wrapPending: boolean;
}

export interface ScreenSnapshot {
  cols: number;
  rows: number;
  cursorX: number;
  cursorY: number;
  cursorStyle: number;
  cursorVisible: boolean;
  cells: ReadonlyArray<ReadonlyArray<ScreenCell>>;
  windowProperties: WindowProperties;
  cursorState: CursorState;
  currentSgrState: SgrState;
}

export interface StatefulTerminalOptions {
  cols: number;
  rows: number;
  onUpdate?: (snapshot: ScreenSnapshot) => void;
  onChunk?: (chunk: TerminalTraceChunk) => void;
  onResponse?: (response: string) => void;
}

type UpdateListener = (snapshot: ScreenSnapshot) => void;
type DecModeListener = (ev: DecModeEvent) => void;
type ChunkListener = (chunk: TerminalTraceChunk) => void;
type ResponseListener = (response: string) => void;

const DEFAULT_SGR_STATE: Readonly<SgrState> = Object.freeze(createDefaultSgrState());

function createCellGrid(cols: number, rows: number): ScreenCell[][] {
  return Array.from({ length: rows }, () =>
    Array.from({ length: cols }, () => ({ ch: " ", sgrState: DEFAULT_SGR_STATE })),
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
  private applicationCursorKeys = false;

  // Scroll region (DECSTBM)
  private scrollTop = 0;
  private scrollBottom: number;

  // Window properties state
  private windowProperties: WindowProperties = {
    title: "",
    iconName: ""
  };

  // Character set state
  private characterSets = {
    G0: "B", // ASCII (default)
    G1: "B", // ASCII (default)
    G2: "B", // ASCII (default)
    G3: "B", // ASCII (default)
    current: "G0" as "G0" | "G1" | "G2" | "G3", // Currently active character set
  };

  // UTF-8 mode state (DECSET/DECRST 2027)
  private utf8Mode = true; // Modern terminals default to UTF-8

  // SGR state management
  // NOTE: must never reference DEFAULT_SGR_STATE directly, because we mutate currentSgrState.
  private currentSgrState: SgrState = createDefaultSgrState();

  // Alternate screen buffer management
  private readonly alternateScreenManager: AlternateScreenManager;

  private readonly updateListeners = new Set<UpdateListener>();
  private readonly decModeListeners = new Set<DecModeListener>();
  private readonly chunkListeners = new Set<ChunkListener>();
  private readonly responseListeners = new Set<ResponseListener>();

  constructor(options: StatefulTerminalOptions) {

    this.cols = options.cols;
    this.rows = options.rows;

    // Initialize scroll region to full screen
    this.scrollBottom = this.rows - 1;

    // Initialize alternate screen manager
    this.alternateScreenManager = new AlternateScreenManager(this.cols, this.rows);

    if (options.onUpdate) {
      this.updateListeners.add(options.onUpdate);
    }

    if (options.onChunk) {
      this.chunkListeners.add(options.onChunk);
    }

    if (options.onResponse) {
      this.responseListeners.add(options.onResponse);
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
          this.lineFeed();
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
            implemented: true,
            cursorX: this.cursorX,
            cursorY: this.cursorY,
            byte,
          });
          this.writePrintableByte(byte);
          this.emitUpdate();
        },
        handleEsc: (msg: EscMessage) => {
          this.emitChunk({ _type: "trace.esc", implemented: msg.implemented, cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleEsc(msg);
          this.emitUpdate();
        },
        handleCsi: (msg: CsiMessage) => {
          this.emitChunk({ _type: "trace.csi", implemented: msg.implemented, cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleCsi(msg);
          this.emitUpdate();
        },
        handleOsc: (msg: OscMessage) => {
          this.emitChunk({ _type: "trace.osc", implemented: msg.implemented, cursorX: this.cursorX, cursorY: this.cursorY, msg });
        },
        handleSgr: (msg: SgrSequence) => {
          this.emitChunk({ _type: "trace.sgr", implemented: msg.implemented, cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleSgr(msg);
          this.emitUpdate();
        },
        handleXtermOsc: (msg: XtermOscMessage) => {
          this.emitChunk({ _type: "trace.osc", implemented: msg.implemented, cursorX: this.cursorX, cursorY: this.cursorY, msg });
          this.handleXtermOsc(msg);
          this.emitUpdate();
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

  public onResponse(listener: ResponseListener): () => void {
    this.responseListeners.add(listener);
    return () => this.responseListeners.delete(listener);
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
      windowProperties: { ...this.windowProperties },
      cursorState: this.getCursorState(),
      currentSgrState: { ...this.currentSgrState },
    };
  }

  // Window properties management methods
  public setWindowTitle(title: string): void {
    this.windowProperties.title = title;
    this.emitUpdate();
  }

  public setIconName(iconName: string): void {
    this.windowProperties.iconName = iconName;
    this.emitUpdate();
  }

  public setTitleAndIcon(title: string): void {
    this.windowProperties.title = title;
    this.windowProperties.iconName = title;
    this.emitUpdate();
  }

  public getWindowTitle(): string {
    return this.windowProperties.title;
  }

  public getIconName(): string {
    return this.windowProperties.iconName;
  }

  public getWindowProperties(): WindowProperties {
    return { ...this.windowProperties };
  }

  // Title/icon name stack management for vi compatibility
  private titleStack: string[] = [];
  private iconNameStack: string[] = [];

  /**
   * Handle window manipulation sequences (CSI Ps t)
   * Implements title/icon name stack operations for vi compatibility
   */
  private handleWindowManipulation(operation: number, params: number[]): void {
    switch (operation) {
      case 22:
        // Push title/icon name to stack
        if (params.length >= 1) {
          const subOperation = params[0];
          if (subOperation === 1) {
            // CSI 22;1t - Push icon name to stack
            this.iconNameStack.push(this.windowProperties.iconName);
            if (this.log.isLevelEnabled("debug")) {
              this.log.debug(`Pushed icon name to stack: "${this.windowProperties.iconName}"`);
            }
          } else if (subOperation === 2) {
            // CSI 22;2t - Push window title to stack
            this.titleStack.push(this.windowProperties.title);
            if (this.log.isLevelEnabled("debug")) {
              this.log.debug(`Pushed window title to stack: "${this.windowProperties.title}"`);
            }
          }
        }
        break;

      case 23:
        // Pop title/icon name from stack
        if (params.length >= 1) {
          const subOperation = params[0];
          if (subOperation === 1) {
            // CSI 23;1t - Pop icon name from stack
            const poppedIconName = this.iconNameStack.pop();
            if (poppedIconName !== undefined) {
              this.setIconName(poppedIconName);
              if (this.log.isLevelEnabled("debug")) {
                this.log.debug(`Popped icon name from stack: "${poppedIconName}"`);
              }
            } else {
              if (this.log.isLevelEnabled("debug")) {
                this.log.debug("Attempted to pop icon name from empty stack");
              }
            }
          } else if (subOperation === 2) {
            // CSI 23;2t - Pop window title from stack
            const poppedTitle = this.titleStack.pop();
            if (poppedTitle !== undefined) {
              this.setWindowTitle(poppedTitle);
              if (this.log.isLevelEnabled("debug")) {
                this.log.debug(`Popped window title from stack: "${poppedTitle}"`);
              }
            } else {
              if (this.log.isLevelEnabled("debug")) {
                this.log.debug("Attempted to pop window title from empty stack");
              }
            }
          }
        }
        break;

      default:
        // Other window manipulation commands - gracefully ignore
        if (this.log.isLevelEnabled("debug")) {
          this.log.debug(`Window manipulation operation ${operation} with params ${JSON.stringify(params)} - gracefully ignored`);
        }
        break;
    }
  }

  // Device query response generation methods

  /**
   * Generate Device Attributes (Primary DA) response.
   * Reports terminal type and supported features.
   * Format: CSI ? 1 ; 2 c (VT100 with Advanced Video Option)
   */
  private generateDeviceAttributesPrimaryResponse(): string {
    // Report as VT100 with Advanced Video Option
    // This is a minimal response that most applications will accept
    return "\x1b[?1;2c";
  }

  /**
   * Generate Device Attributes (Secondary DA) response.
   * Reports terminal version and firmware level.
   * Format: CSI > 0 ; version ; 0 c
   */
  private generateDeviceAttributesSecondaryResponse(): string {
    // Report as VT100 compatible terminal, version 0
    return "\x1b[>0;0;0c";
  }

  /**
   * Generate Cursor Position Report (CPR) response.
   * Reports current cursor position to the application.
   * Format: CSI row ; col R (1-indexed coordinates)
   */
  private generateCursorPositionReport(): string {
    // Convert from 0-indexed to 1-indexed coordinates
    const row = this.cursorY + 1;
    const col = this.cursorX + 1;
    return `\x1b[${row};${col}R`;
  }

  /**
   * Generate Terminal Size Query response.
   * Reports terminal dimensions in characters.
   * Format: CSI 8 ; rows ; cols t
   */
  private generateTerminalSizeResponse(): string {
    return `\x1b[8;${this.rows};${this.cols}t`;
  }

  // Enhanced cursor state management methods
  public getCursorState(): CursorState {
    return {
      x: this.cursorX,
      y: this.cursorY,
      visible: this.cursorVisible,
      style: this.cursorStyle,
      applicationMode: this.applicationCursorKeys,
      wrapPending: this.wrapPending,
    };
  }

  public setCursorVisibility(visible: boolean): void {
    this.cursorVisible = visible;
    this.emitUpdate();
  }

  public setCursorStyle(style: number): void {
    // Validate cursor style parameter (0-6 are valid DECSCUSR values)
    if (style >= 0 && style <= 6) {
      this.cursorStyle = style;
      this.emitUpdate();
    }
  }

  public setApplicationCursorKeys(enabled: boolean): void {
    this.applicationCursorKeys = enabled;
    this.emitUpdate();
  }

  public getApplicationCursorKeys(): boolean {
    return this.applicationCursorKeys;
  }

  // Character set management methods

  /**
   * Designate a character set to a specific G slot
   */
  public designateCharacterSet(slot: "G0" | "G1" | "G2" | "G3", charset: string): void {
    this.characterSets[slot] = charset;
  }

  /**
   * Get the currently designated character set for a slot
   */
  public getCharacterSet(slot: "G0" | "G1" | "G2" | "G3"): string {
    return this.characterSets[slot];
  }

  /**
   * Get the current active character set
   */
  public getCurrentCharacterSet(): string {
    return this.characterSets[this.characterSets.current];
  }

  /**
   * Switch to a different character set slot
   */
  public switchCharacterSet(slot: "G0" | "G1" | "G2" | "G3"): void {
    this.characterSets.current = slot;
  }

  /**
   * Enable or disable UTF-8 mode
   */
  public setUtf8Mode(enabled: boolean): void {
    this.utf8Mode = enabled;
  }

  /**
   * Check if UTF-8 mode is enabled
   */
  public isUtf8Mode(): boolean {
    return this.utf8Mode;
  }

  /**
   * Generate character set query response
   * Format: CSI ? 26 ; charset ST
   */
  private generateCharacterSetQueryResponse(): string {
    // Report current character set
    // For UTF-8 mode, report "utf-8", otherwise report the current G set
    const charset = this.utf8Mode ? "utf-8" : this.getCurrentCharacterSet();
    return `\x1b[?26;${charset}\x1b\\`;
  }

  /**
   * Translate a character according to the current character set.
   * Handles special character sets like DEC Special Graphics.
   * 
   * @param ch The input character
   * @returns The translated character
   */
  private translateCharacter(ch: string): string {
    // If UTF-8 mode is enabled, no translation needed
    if (this.utf8Mode) {
      return ch;
    }

    const currentCharset = this.getCurrentCharacterSet();

    // DEC Special Graphics character set (line drawing characters)
    if (currentCharset === "0") {
      const code = ch.charCodeAt(0);
      // Map ASCII characters to Unicode box drawing characters
      const decSpecialGraphics: Record<number, string> = {
        0x60: "\u25C6", // ` -> ◆ (diamond)
        0x61: "\u2592", // a -> ▒ (checkerboard)
        0x62: "\u2409", // b -> ␉ (HT symbol)
        0x63: "\u240C", // c -> ␌ (FF symbol)
        0x64: "\u240D", // d -> ␍ (CR symbol)
        0x65: "\u240A", // e -> ␊ (LF symbol)
        0x66: "\u00B0", // f -> ° (degree)
        0x67: "\u00B1", // g -> ± (plus-minus)
        0x68: "\u2424", // h -> ␤ (NL symbol)
        0x69: "\u240B", // i -> ␋ (VT symbol)
        0x6A: "\u2518", // j -> ┘ (lower right corner)
        0x6B: "\u2510", // k -> ┐ (upper right corner)
        0x6C: "\u250C", // l -> ┌ (upper left corner)
        0x6D: "\u2514", // m -> └ (lower left corner)
        0x6E: "\u253C", // n -> ┼ (crossing lines)
        0x6F: "\u23BA", // o -> ⎺ (scan line 1)
        0x70: "\u23BB", // p -> ⎻ (scan line 3)
        0x71: "\u2500", // q -> ─ (horizontal line)
        0x72: "\u23BC", // r -> ⎼ (scan line 7)
        0x73: "\u23BD", // s -> ⎽ (scan line 9)
        0x74: "\u251C", // t -> ├ (left tee)
        0x75: "\u2524", // u -> ┤ (right tee)
        0x76: "\u2534", // v -> ┴ (bottom tee)
        0x77: "\u252C", // w -> ┬ (top tee)
        0x78: "\u2502", // x -> │ (vertical line)
        0x79: "\u2264", // y -> ≤ (less than or equal)
        0x7A: "\u2265", // z -> ≥ (greater than or equal)
        0x7B: "\u03C0", // { -> π (pi)
        0x7C: "\u2260", // | -> ≠ (not equal)
        0x7D: "\u00A3", // } -> £ (pound sterling)
        0x7E: "\u00B7", // ~ -> · (middle dot)
      };

      return decSpecialGraphics[code] || ch;
    }

    // For other character sets, return the character as-is
    // In a full implementation, we would handle other character sets here
    return ch;
  }

  public saveCursorState(): CursorState {
    return this.getCursorState();
  }

  public restoreCursorState(state: CursorState): void {
    // Validate and clamp coordinates to screen boundaries
    this.cursorX = Math.max(0, Math.min(this.cols - 1, state.x));
    this.cursorY = Math.max(0, Math.min(this.rows - 1, state.y));
    this.cursorVisible = state.visible;
    this.cursorStyle = state.style;
    this.applicationCursorKeys = state.applicationMode;
    this.wrapPending = state.wrapPending;
    this.emitUpdate();
  }

  // Alternate screen buffer switching methods

  /**
   * DECSET 47: Switch to alternate screen buffer
   */
  private switchToAlternateScreen(): void {
    if (!this.alternateScreenManager.isAlternateActive()) {
      // Save current primary buffer state
      const primaryBuffer = this.alternateScreenManager.getPrimaryBuffer();
      primaryBuffer.cursorX = this.cursorX;
      primaryBuffer.cursorY = this.cursorY;
      primaryBuffer.wrapPending = this.wrapPending;

      // Switch to alternate buffer
      this.alternateScreenManager.switchToAlternate();
      const alternateBuffer = this.alternateScreenManager.getCurrentBuffer();

      // Load alternate buffer state
      this.cursorX = alternateBuffer.cursorX;
      this.cursorY = alternateBuffer.cursorY;
      this.wrapPending = alternateBuffer.wrapPending;
    }
  }

  /**
   * DECSET 1047: Save cursor and switch to alternate screen buffer
   */
  private switchToAlternateScreenWithCursorSave(): void {
    // Save cursor position
    this.savedCursor = [this.cursorX, this.cursorY];

    // Switch to alternate screen
    this.switchToAlternateScreen();
  }

  /**
   * DECSET 1049: Save cursor, switch to alternate screen, and clear it
   */
  private switchToAlternateScreenWithCursorSaveAndClear(): void {
    // Save cursor position
    this.savedCursor = [this.cursorX, this.cursorY];

    // Switch to alternate screen
    this.switchToAlternateScreen();

    // Clear the alternate screen buffer
    this.alternateScreenManager.clearAlternateBuffer();
    this.cursorX = 0;
    this.cursorY = 0;
    this.wrapPending = false;
  }

  /**
   * DECRST 47: Switch back to normal screen buffer
   */
  private switchToPrimaryScreen(): void {
    if (this.alternateScreenManager.isAlternateActive()) {
      // Save current alternate buffer state
      const alternateBuffer = this.alternateScreenManager.getAlternateBuffer();
      alternateBuffer.cursorX = this.cursorX;
      alternateBuffer.cursorY = this.cursorY;
      alternateBuffer.wrapPending = this.wrapPending;

      // Switch to primary buffer
      this.alternateScreenManager.switchToPrimary();
      const primaryBuffer = this.alternateScreenManager.getCurrentBuffer();

      // Load primary buffer state
      this.cursorX = primaryBuffer.cursorX;
      this.cursorY = primaryBuffer.cursorY;
      this.wrapPending = primaryBuffer.wrapPending;
    }
  }

  /**
   * DECRST 1047/1049: Switch to normal screen and restore cursor
   */
  private switchToPrimaryScreenWithCursorRestore(): void {
    // Switch to primary screen
    this.switchToPrimaryScreen();

    // Restore cursor position
    if (this.savedCursor) {
      this.cursorX = this.savedCursor[0];
      this.cursorY = this.savedCursor[1];
      this.clampCursor();
    }
  }

  public isAlternateScreenActive(): boolean {
    return this.alternateScreenManager.isAlternateActive();
  }

  /**
   * Get the current buffer's cells (either primary or alternate)
   */
  private get cells(): ScreenCell[][] {
    return this.alternateScreenManager.getCurrentBuffer().cells;
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
    this.cursorStyle = 1;
    this.cursorVisible = true;
    this.applicationCursorKeys = false;
    this.windowProperties = {
      title: "",
      iconName: ""
    };
    this.characterSets = {
      G0: "B",
      G1: "B",
      G2: "B",
      G3: "B",
      current: "G0",
    };
    this.utf8Mode = true;
    // Clear title/icon name stacks
    this.titleStack = [];
    this.iconNameStack = [];
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
    this.emitChunk({ _type: "trace.control", implemented: true, cursorX: this.cursorX, cursorY: this.cursorY, name, byte });
  }

  private emitResponse(response: string): void {
    for (const listener of this.responseListeners) {
      listener(response);
    }
  }

  private writePrintableByte(byte: number): void {
    if (byte < 0x20 || byte === 0x7f) {
      return;
    }

    let ch: string;

    // Handle Unicode code points (the parser now sends Unicode code points for UTF-8 sequences)
    if (byte > 0xFFFF) {
      // Use String.fromCodePoint for characters outside the BMP (Basic Multilingual Plane)
      ch = String.fromCodePoint(byte);
    } else {
      // Use String.fromCharCode for characters in the BMP
      ch = String.fromCharCode(byte);
    }

    // Apply character set translation if not in UTF-8 mode
    if (!this.utf8Mode) {
      ch = this.translateCharacter(ch);
    }

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

    const cell = this.cells[this.cursorY][this.cursorX];
    cell.ch = ch;
    cell.sgrState = { ...this.currentSgrState };

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

    // Check if we've moved past the bottom of the scroll region
    if (this.cursorY > this.scrollBottom) {
      // Scroll only within the scroll region
      this.scrollUpInRegion(1);
      this.cursorY = this.scrollBottom;
    }

    this.wrapPending = false;
  }

  private backspace(): void {
    this.wrapPending = false;
    if (this.cursorX > 0) {
      this.cursorX -= 1;
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
        this.cells[y][x].sgrState = { ...this.currentSgrState };
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

    const currentBuffer = this.alternateScreenManager.getCurrentBuffer();
    for (let i = 0; i < n; i++) {
      currentBuffer.cells.shift();
      const sgrState = { ...this.currentSgrState };
      currentBuffer.cells.push(Array.from({ length: this.cols }, () => ({ ch: " ", sgrState })));
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
      for (let x = this.cursorX; x < this.cols; x++) {
        this.cells[y][x].ch = " ";
        this.cells[y][x].sgrState = { ...this.currentSgrState };
      }
      return;
    }

    if (mode === 1) {
      for (let x = 0; x <= this.cursorX; x++) {
        this.cells[y][x].ch = " ";
        this.cells[y][x].sgrState = { ...this.currentSgrState };
      }
      return;
    }

    for (let x = 0; x < this.cols; x++) {
      this.cells[y][x].ch = " ";
      this.cells[y][x].sgrState = { ...this.currentSgrState };
    }
  }

  private eraseCharacters(count: number): void {
    this.wrapPending = false;
    const y = this.cursorY;
    if (y < 0 || y >= this.rows) {
      return;
    }

    // Erase 'count' characters starting from cursor position
    const endX = Math.min(this.cursorX + count, this.cols);
    for (let x = this.cursorX; x < endX; x++) {
      this.cells[y][x].ch = " ";
      this.cells[y][x].sgrState = { ...this.currentSgrState };
    }
  }

  private setScrollRegion(top?: number, bottom?: number): void {
    // DECSTBM - Set Top and Bottom Margins
    if (top === undefined && bottom === undefined) {
      // Reset to full screen
      this.scrollTop = 0;
      this.scrollBottom = this.rows - 1;
    } else {
      // Convert from 1-indexed to 0-indexed and validate bounds
      const newTop = top ? Math.max(0, Math.min(this.rows - 1, top - 1)) : 0;
      const newBottom = bottom ? Math.max(0, Math.min(this.rows - 1, bottom - 1)) : this.rows - 1;

      // Ensure top < bottom
      if (newTop < newBottom) {
        this.scrollTop = newTop;
        this.scrollBottom = newBottom;
      }
    }

    // Move cursor to home position within scroll region
    this.cursorX = 0;
    this.cursorY = this.scrollTop;
    this.wrapPending = false;
  }

  private scrollUpInRegion(lines: number): void {
    // Scroll only within the defined scroll region
    for (let i = 0; i < lines; i++) {
      // Move all lines up within the scroll region
      for (let y = this.scrollTop; y < this.scrollBottom; y++) {
        for (let x = 0; x < this.cols; x++) {
          this.cells[y][x] = { ...this.cells[y + 1][x] };
        }
      }

      // Clear the bottom line of the scroll region
      for (let x = 0; x < this.cols; x++) {
        this.cells[this.scrollBottom][x].ch = " ";
        this.cells[this.scrollBottom][x].sgrState = { ...this.currentSgrState };
      }
    }
  }

  private scrollDownInRegion(lines: number): void {
    this.wrapPending = false;
    const n = Math.max(0, Math.min(lines, this.scrollBottom - this.scrollTop + 1));
    if (n === 0) {
      return;
    }

    for (let i = 0; i < n; i++) {
      // Move all lines down within the scroll region
      for (let y = this.scrollBottom; y > this.scrollTop; y--) {
        for (let x = 0; x < this.cols; x++) {
          this.cells[y][x] = { ...this.cells[y - 1][x] };
        }
      }

      // Clear the top line of the scroll region
      for (let x = 0; x < this.cols; x++) {
        this.cells[this.scrollTop][x].ch = " ";
        this.cells[this.scrollTop][x].sgrState = { ...this.currentSgrState };
      }
    }
  }

  private deleteLinesInRegion(count: number): void {
    this.wrapPending = false;

    // DL/IL affect only when the cursor is within the scroll region.
    if (this.cursorY < this.scrollTop || this.cursorY > this.scrollBottom) {
      return;
    }

    const maxDeletable = this.scrollBottom - this.cursorY + 1;
    const n = Math.max(0, Math.min(count, maxDeletable));
    if (n === 0) {
      return;
    }

    // Shift lines up within the region starting at cursorY.
    for (let y = this.cursorY; y <= this.scrollBottom - n; y++) {
      for (let x = 0; x < this.cols; x++) {
        this.cells[y][x] = { ...this.cells[y + n][x] };
      }
    }

    // Clear the newly exposed bottom lines.
    for (let y = this.scrollBottom - n + 1; y <= this.scrollBottom; y++) {
      for (let x = 0; x < this.cols; x++) {
        this.cells[y][x].ch = " ";
        this.cells[y][x].sgrState = { ...this.currentSgrState };
      }
    }
  }

  private insertLinesInRegion(count: number): void {
    this.wrapPending = false;

    if (this.cursorY < this.scrollTop || this.cursorY > this.scrollBottom) {
      return;
    }

    const maxInsertable = this.scrollBottom - this.cursorY + 1;
    const n = Math.max(0, Math.min(count, maxInsertable));
    if (n === 0) {
      return;
    }

    // Shift lines down within the region starting at cursorY.
    for (let y = this.scrollBottom; y >= this.cursorY + n; y--) {
      for (let x = 0; x < this.cols; x++) {
        this.cells[y][x] = { ...this.cells[y - n][x] };
      }
    }

    // Clear the inserted blank lines.
    for (let y = this.cursorY; y < this.cursorY + n; y++) {
      for (let x = 0; x < this.cols; x++) {
        this.cells[y][x].ch = " ";
        this.cells[y][x].sgrState = { ...this.currentSgrState };
      }
    }
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
        for (let x = 0; x < this.cols; x++) {
          this.cells[y][x].ch = " ";
          this.cells[y][x].sgrState = { ...this.currentSgrState };
        }
      }
      return;
    }

    if (mode === 1) {
      // from start to cursor
      for (let y = 0; y < this.cursorY; y++) {
        for (let x = 0; x < this.cols; x++) {
          this.cells[y][x].ch = " ";
          this.cells[y][x].sgrState = { ...this.currentSgrState };
        }
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
      case "csi.verticalPositionAbsolute":
        this.cursorY = Math.max(0, Math.min(this.rows - 1, msg.row - 1));
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

      case "csi.deleteLines":
        this.deleteLinesInRegion(Math.max(1, msg.count));
        return;

      case "csi.insertLines":
        this.insertLinesInRegion(Math.max(1, msg.count));
        return;
      case "csi.scrollUp":
        this.scrollUpInRegion(msg.lines);
        return;

      case "csi.scrollDown":
        this.scrollDownInRegion(msg.lines);
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
          this.setCursorVisibility(true);
        }
        // Application cursor keys (CSI ? 1 h)
        if (msg.modes.includes(1)) {
          this.setApplicationCursorKeys(true);
        }
        // UTF-8 mode (CSI ? 2027 h)
        if (msg.modes.includes(2027)) {
          this.setUtf8Mode(true);
        }
        // Alternate screen buffer modes
        if (msg.modes.includes(47)) {
          // DECSET 47: Switch to alternate screen buffer
          this.switchToAlternateScreen();
        }
        if (msg.modes.includes(1047)) {
          // DECSET 1047: Save cursor and switch to alternate screen buffer
          this.switchToAlternateScreenWithCursorSave();
        }
        if (msg.modes.includes(1049)) {
          // DECSET 1049: Save cursor, switch to alternate screen, and clear it
          this.switchToAlternateScreenWithCursorSaveAndClear();
        }
        this.emitDecMode({ action: "set", raw: msg.raw, modes: msg.modes });
        return;

      case "csi.decModeReset":
        // DECTCEM (CSI ? 25 l): hide cursor
        if (msg.modes.includes(25)) {
          this.setCursorVisibility(false);
        }
        // Application cursor keys (CSI ? 1 l)
        if (msg.modes.includes(1)) {
          this.setApplicationCursorKeys(false);
        }
        // UTF-8 mode (CSI ? 2027 l)
        if (msg.modes.includes(2027)) {
          this.setUtf8Mode(false);
        }
        // Alternate screen buffer modes
        if (msg.modes.includes(47)) {
          // DECRST 47: Switch back to normal screen buffer
          this.switchToPrimaryScreen();
        }
        if (msg.modes.includes(1047)) {
          // DECRST 1047: Switch to normal screen and restore cursor
          this.switchToPrimaryScreenWithCursorRestore();
        }
        if (msg.modes.includes(1049)) {
          // DECRST 1049: Switch to normal screen and restore cursor
          this.switchToPrimaryScreenWithCursorRestore();
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
        this.setCursorStyle(msg.style);
        return;

      // Device query handling
      case "csi.deviceAttributesPrimary":
        // Primary DA query: respond with device attributes
        this.emitResponse(this.generateDeviceAttributesPrimaryResponse());
        return;

      case "csi.deviceAttributesSecondary":
        // Secondary DA query: respond with terminal version
        this.emitResponse(this.generateDeviceAttributesSecondaryResponse());
        return;

      case "csi.cursorPositionReport":
        // CPR query: respond with current cursor position
        this.emitResponse(this.generateCursorPositionReport());
        return;

      case "csi.terminalSizeQuery":
        // Terminal size query: respond with dimensions
        this.emitResponse(this.generateTerminalSizeResponse());
        return;

      case "csi.characterSetQuery":
        // Character set query: respond with current character set
        this.emitResponse(this.generateCharacterSetQueryResponse());
        return;

      case "csi.eraseCharacter":
        this.eraseCharacters(msg.count);
        return;

      case "csi.insertMode":
        // IRM (Insert/Replace Mode) - store the mode but don't implement insertion yet
        // This prevents the sequence from being unknown and potentially causing issues
        return;

      case "csi.windowManipulation":
        // Window manipulation commands - handle title stack operations for vi compatibility
        this.handleWindowManipulation(msg.operation, msg.params);
        return;

      case "csi.setScrollRegion":
        this.setScrollRegion(msg.top, msg.bottom);
        return;

      case "csi.enhancedSgrMode":
        // Enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
        this.handleEnhancedSgrMode(msg.params);
        return;

      case "csi.privateSgrMode":
        // Private SGR sequences with ? prefix (e.g., CSI ? 4 m)
        this.handlePrivateSgrMode(msg.params);
        return;

      case "csi.sgrWithIntermediate":
        // SGR sequences with intermediate characters (e.g., CSI 0 % m)
        this.handleSgrWithIntermediate(msg.params, msg.intermediate);
        return;

      case "csi.unknownViSequence":
        // Unknown vi sequences (e.g., CSI 11M) - gracefully acknowledge but don't implement
        // These sequences appear in vi usage but are not part of standard terminal specifications
        this.handleUnknownViSequence(msg.sequenceNumber);
        return;

      // ignored (for MVP)
      case "csi.mouseReportingMode":
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
      case "esc.designateCharacterSet":
        // Designate character set to the specified G slot
        this.designateCharacterSet(msg.slot, msg.charset);
        return;

      case "esc.reverseIndex": {
        // RI (ESC M): move cursor up; if at top margin, scroll region down.
        this.wrapPending = false;
        if (this.cursorY <= this.scrollTop) {
          this.cursorY = this.scrollTop;
          this.scrollDownInRegion(1);
          return;
        }

        this.cursorY = Math.max(this.scrollTop, this.cursorY - 1);
        return;
      }
    }
  }

  private handleXtermOsc(msg: XtermOscMessage): void {
    switch (msg._type) {
      case "osc.setTitleAndIcon":
        // OSC 0: Set both window title and icon name
        this.setTitleAndIcon(msg.title);
        return;

      case "osc.setIconName":
        // OSC 1: Set icon name only
        this.setIconName(msg.iconName);
        return;

      case "osc.setWindowTitle":
        // OSC 2: Set window title only
        this.setWindowTitle(msg.title);
        return;

      case "osc.queryWindowTitle":
        // OSC 21: Query window title
        // Respond with OSC ] L <title> ST (ESC \\)
        this.emitResponse(`\x1b]L${this.windowProperties.title}\x1b\\`);
        return;

      case "osc.queryForegroundColor":
        // OSC 10;?: Query default foreground color
        // Respond with current theme foreground color
        this.emitResponse(this.generateForegroundColorResponse());
        return;

      case "osc.queryBackgroundColor":
        // OSC 11;?: Query default background color
        // Respond with current theme background color
        this.emitResponse(this.generateBackgroundColorResponse());
        return;
    }
  }

  private handleSgr(msg: SgrSequence): void {
    // Process SGR messages and update current SGR state
    this.currentSgrState = processSgrMessages(this.currentSgrState, msg.messages);
  }

  /**
   * Get the current SGR state
   */
  public getCurrentSgrState(): SgrState {
    return { ...this.currentSgrState };
  }

  /**
   * Reset SGR state to default
   */
  public resetSgrState(): void {
    this.currentSgrState = createDefaultSgrState();
  }

  /**
   * Handle enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
   * These are typically used for advanced terminal features.
   */
  private handleEnhancedSgrMode(params: number[]): void {
    if (params.length >= 2 && params[0] === 4) {
      // Enhanced underline mode: CSI > 4 ; n m
      const underlineType = params[1];
      
      if (underlineType >= 0 && underlineType <= 5) {
        // Valid enhanced underline mode - update SGR state
        switch (underlineType) {
          case 0:
            // No underline
            this.currentSgrState.underline = false;
            this.currentSgrState.underlineStyle = null;
            break;
          case 1:
            // Single underline
            this.currentSgrState.underline = true;
            this.currentSgrState.underlineStyle = 'single';
            break;
          case 2:
            // Double underline
            this.currentSgrState.underline = true;
            this.currentSgrState.underlineStyle = 'double';
            break;
          case 3:
            // Curly underline
            this.currentSgrState.underline = true;
            this.currentSgrState.underlineStyle = 'curly';
            break;
          case 4:
            // Dotted underline
            this.currentSgrState.underline = true;
            this.currentSgrState.underlineStyle = 'dotted';
            break;
          case 5:
            // Dashed underline
            this.currentSgrState.underline = true;
            this.currentSgrState.underlineStyle = 'dashed';
            break;
        }
        
        if (this.log.isLevelEnabled("debug")) {
          this.log.debug(`Enhanced underline mode set: type=${underlineType}, style=${this.currentSgrState.underlineStyle}`);
        }
      } else {
        // Invalid underline type - gracefully ignore
        if (this.log.isLevelEnabled("debug")) {
          this.log.debug(`Invalid enhanced underline type: ${underlineType}, ignoring`);
        }
      }
    } else {
      // Other enhanced modes not yet supported - gracefully ignore
      if (this.log.isLevelEnabled("debug")) {
        this.log.debug(`Enhanced SGR mode received: ${JSON.stringify({ params })}, not implemented`);
      }
    }
  }

  /**
   * Handle private SGR sequences with ? prefix (e.g., CSI ? 4 m)
   * These are typically used for private/experimental features.
   */
  private handlePrivateSgrMode(params: number[]): void {
    // Handle specific private SGR modes
    if (params.length === 1 && params[0] === 4) {
      // Private underline mode (?4m) - enable underline
      this.currentSgrState.underline = true;
      this.currentSgrState.underlineStyle = 'single';
      
      if (this.log.isLevelEnabled("debug")) {
        this.log.debug("Private underline mode (?4m) enabled");
      }
      return;
    }
    
    // For other private modes, gracefully ignore
    if (this.log.isLevelEnabled("debug")) {
      this.log.debug(`Private SGR mode received: ${JSON.stringify({ params })}, not implemented`);
    }
  }

  /**
   * Handle SGR sequences with intermediate characters (e.g., CSI 0 % m)
   * These are used for special SGR attribute resets or modifications.
   */
  private handleSgrWithIntermediate(params: number[], intermediate: string): void {
    // Handle specific intermediate character sequences
    if (intermediate === "%") {
      // CSI 0 % m - Reset specific attributes
      if (params.length === 1 && params[0] === 0) {
        // Reset all SGR attributes (similar to SGR 0)
        this.currentSgrState = createDefaultSgrState();
        this.log.debug("SGR reset with % intermediate");
        return;
      }
    }

    // For other intermediate sequences, gracefully ignore
    if (this.log.isLevelEnabled("debug")) {
      this.log.debug(`SGR with intermediate received: ${JSON.stringify({ params, intermediate })}`);
    }
  }

  /**
   * Handle unknown vi sequences (e.g., CSI 11M)
   * These sequences appear in vi usage but are not part of standard terminal specifications.
   * We gracefully acknowledge them without implementing specific behavior.
   */
  private handleUnknownViSequence(sequenceNumber: number): void {
    // Log the sequence for debugging purposes
    if (this.log.isLevelEnabled("debug")) {
      this.log.debug(`Unknown vi sequence received: CSI ${sequenceNumber}M`);
    }
    
    // Gracefully acknowledge - no specific action needed
    // The sequence is parsed and handled without causing errors
  }

  /**
   * Generate response for OSC 10;? (query foreground color)
   */
  private generateForegroundColorResponse(): string {
    // Get the current effective foreground color
    const effectiveColor = this.getEffectiveForegroundColor();
    
    // Convert to OSC format: OSC 10 ; rgb:rrrr/gggg/bbbb BEL
    return `\x1b]10;${effectiveColor}\x07`;
  }

  /**
   * Generate response for OSC 11;? (query background color)
   */
  private generateBackgroundColorResponse(): string {
    // Get the current effective background color
    const effectiveColor = this.getEffectiveBackgroundColor();
    
    // Convert to OSC format: OSC 11 ; rgb:rrrr/gggg/bbbb BEL
    return `\x1b]11;${effectiveColor}\x07`;
  }

  /**
   * Get the effective foreground color (SGR override or theme default)
   */
  private getEffectiveForegroundColor(): string {
    if (this.currentSgrState.foregroundColor) {
      return this.convertSgrColorToOscFormat(this.currentSgrState.foregroundColor);
    }
    
    // Return theme default foreground color
    // Use the CSS variable value or fallback to standard light gray
    return "rgb:aaaa/aaaa/aaaa"; // Default light gray
  }

  /**
   * Get the effective background color (SGR override or theme default)
   */
  private getEffectiveBackgroundColor(): string {
    if (this.currentSgrState.backgroundColor) {
      return this.convertSgrColorToOscFormat(this.currentSgrState.backgroundColor);
    }
    
    // Return theme default background color
    // Use the CSS variable value or fallback to standard black
    return "rgb:0000/0000/0000"; // Default black
  }

  /**
   * Convert SGR color type to OSC rgb format
   */
  private convertSgrColorToOscFormat(colorType: SgrColorType): string {
    switch (colorType.type) {
      case 'rgb':
        // Convert 8-bit RGB values to 16-bit hex format for OSC
        const r16 = Math.round((colorType.r / 255) * 65535).toString(16).padStart(4, '0');
        const g16 = Math.round((colorType.g / 255) * 65535).toString(16).padStart(4, '0');
        const b16 = Math.round((colorType.b / 255) * 65535).toString(16).padStart(4, '0');
        return `rgb:${r16}/${g16}/${b16}`;
        
      case 'indexed':
        // Convert indexed color to RGB then to OSC format
        return this.convertIndexedColorToOscFormat(colorType.index);
        
      case 'named':
        // Convert named color to RGB then to OSC format
        return this.convertNamedColorToOscFormat(colorType.color);
        
      default:
        // Fallback to default colors
        return "rgb:aaaa/aaaa/aaaa";
    }
  }

  /**
   * Convert indexed color (0-255) to OSC rgb format
   */
  private convertIndexedColorToOscFormat(index: number): string {
    // Standard 16 colors (0-15) - use predefined RGB values
    if (index >= 0 && index <= 15) {
      const standardColors = [
        [0, 0, 0],       // 0: black
        [170, 0, 0],     // 1: red
        [0, 170, 0],     // 2: green
        [170, 85, 0],    // 3: yellow
        [0, 0, 170],     // 4: blue
        [170, 0, 170],   // 5: magenta
        [0, 170, 170],   // 6: cyan
        [170, 170, 170], // 7: white
        [85, 85, 85],    // 8: bright black
        [255, 85, 85],   // 9: bright red
        [85, 255, 85],   // 10: bright green
        [255, 255, 85],  // 11: bright yellow
        [85, 85, 255],   // 12: bright blue
        [255, 85, 255],  // 13: bright magenta
        [85, 255, 255],  // 14: bright cyan
        [255, 255, 255], // 15: bright white
      ];
      
      if (index < standardColors.length) {
        const [r, g, b] = standardColors[index];
        const r16 = Math.round((r / 255) * 65535).toString(16).padStart(4, '0');
        const g16 = Math.round((g / 255) * 65535).toString(16).padStart(4, '0');
        const b16 = Math.round((b / 255) * 65535).toString(16).padStart(4, '0');
        return `rgb:${r16}/${g16}/${b16}`;
      }
    }

    // 216 color cube (16-231)
    if (index >= 16 && index <= 231) {
      const cubeIndex = index - 16;
      const r = Math.floor(cubeIndex / 36);
      const g = Math.floor((cubeIndex % 36) / 6);
      const b = cubeIndex % 6;
      
      const toColorValue = (n: number) => n === 0 ? 0 : 55 + n * 40;
      const rVal = toColorValue(r);
      const gVal = toColorValue(g);
      const bVal = toColorValue(b);
      
      const r16 = Math.round((rVal / 255) * 65535).toString(16).padStart(4, '0');
      const g16 = Math.round((gVal / 255) * 65535).toString(16).padStart(4, '0');
      const b16 = Math.round((bVal / 255) * 65535).toString(16).padStart(4, '0');
      return `rgb:${r16}/${g16}/${b16}`;
    }

    // Grayscale ramp (232-255)
    if (index >= 232 && index <= 255) {
      const gray = 8 + (index - 232) * 10;
      const gray16 = Math.round((gray / 255) * 65535).toString(16).padStart(4, '0');
      return `rgb:${gray16}/${gray16}/${gray16}`;
    }

    // Invalid index, return default
    return "rgb:aaaa/aaaa/aaaa";
  }

  /**
   * Convert named ANSI color to OSC rgb format
   */
  private convertNamedColorToOscFormat(color: SgrNamedColor): string {
    const namedColorRgb: Record<SgrNamedColor, [number, number, number]> = {
      black: [0, 0, 0],
      red: [170, 0, 0],
      green: [0, 170, 0],
      yellow: [170, 85, 0],
      blue: [0, 0, 170],
      magenta: [170, 0, 170],
      cyan: [0, 170, 170],
      white: [170, 170, 170],
      brightBlack: [85, 85, 85],
      brightRed: [255, 85, 85],
      brightGreen: [85, 255, 85],
      brightYellow: [255, 255, 85],
      brightBlue: [85, 85, 255],
      brightMagenta: [255, 85, 255],
      brightCyan: [85, 255, 255],
      brightWhite: [255, 255, 255],
    };

    const [r, g, b] = namedColorRgb[color] || [170, 170, 170]; // Default to white
    const r16 = Math.round((r / 255) * 65535).toString(16).padStart(4, '0');
    const g16 = Math.round((g / 255) * 65535).toString(16).padStart(4, '0');
    const b16 = Math.round((b / 255) * 65535).toString(16).padStart(4, '0');
    return `rgb:${r16}/${g16}/${b16}`;
  }
}
