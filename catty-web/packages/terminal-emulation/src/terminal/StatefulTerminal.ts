import { getLogger } from "@catty/log";

import type { TerminalTraceChunk, TraceControlName } from "./TerminalTrace";
import { traceSettings } from "./traceSettings";
import { Parser } from "./Parser";
import type { DcsMessage, EscMessage, CsiMessage, OscMessage, SgrSequence, XtermOscMessage } from "./TerminalEmulationTypes";
import { createDefaultSgrState, type SgrState } from './SgrStyleManager';
import { processSgrMessages } from './SgrStateProcessor';

import { AlternateScreenManager } from "./stateful/alternateScreen";
import { createCellGrid } from "./stateful/screenGrid";
import {
  clearScrollback as clearScrollbackState,
  getViewportRows as getViewportRowsState,
  pushScrollbackRow as pushScrollbackRowState,
} from "./stateful/scrollback";

import { createInitialTerminalState, type CharacterSetState, type TerminalState } from "./stateful/state";
import {
  designateCharacterSet as designateCharacterSetState,
  generateCharacterSetQueryResponse as generateCharacterSetQueryResponseState,
  getCharacterSet as getCharacterSetState,
  getCurrentCharacterSet as getCurrentCharacterSetState,
  isUtf8Mode as isUtf8ModeState,
  setUtf8Mode as setUtf8ModeState,
  switchCharacterSet as switchCharacterSetState,
  translateCharacter as translateCharacterState,
} from "./stateful/charset";
import {
  clearAllTabStops,
  clearTabStopAtCursor,
  cursorBackwardTab,
  cursorForwardTab,
  initializeTabStops,
  setTabStopAtCursor,
} from "./stateful/tabStops";

import * as bufferOps from "./stateful/bufferOps";

import {
  generateBackgroundColorResponse,
  generateForegroundColorResponse,
} from "./stateful/responses";

import type { TerminalActions } from "./stateful/actions";
import { handleCsi as handleCsiDispatch } from "./stateful/handlers/csi";

import type {
  CursorState,
  DecModeEvent,
  ScreenCell,
  ScreenSnapshot,
  StatefulTerminalOptions,
  WindowProperties,
} from "./stateful/screenTypes";

export type {
  CursorState,
  DecModeEvent,
  ScreenBuffer,
  ScreenCell,
  ScreenSnapshot,
  StatefulTerminalOptions,
  WindowProperties,
} from "./stateful/screenTypes";

export { AlternateScreenManager } from "./stateful/alternateScreen";

type UpdateListener = (snapshot: ScreenSnapshot) => void;
type DecModeListener = (ev: DecModeEvent) => void;
type ChunkListener = (chunk: TerminalTraceChunk) => void;
type ResponseListener = (response: string) => void;

function clampInt(v: number, min: number, max: number): number {
  if (!Number.isFinite(v)) {
    return min;
  }
  return Math.max(min, Math.min(max, Math.trunc(v)));
}

type XY = [number, number];

export class StatefulTerminal {
  public readonly cols: number;
  public readonly rows: number;

  private readonly log = getLogger();
  private readonly parser: Parser;
  private readonly actions: TerminalActions;

  private state: TerminalState;

  private get _cursorX(): number {
    return this.state.cursorX;
  }

  private set _cursorX(v: number) {
    this.state.cursorX = v;
  }

  public get cursorX(): number {
    return this._cursorX;
  }

  private get _cursorY(): number {
    return this.state.cursorY;
  }

  private set _cursorY(v: number) {
    this.state.cursorY = v;
  }

  public get cursorY(): number {
    return this._cursorY;
  }

  private get savedCursor(): XY | null {
    return this.state.savedCursor;
  }

  private set savedCursor(v: XY | null) {
    this.state.savedCursor = v;
  }

  private get cursorStyle(): number {
    return this.state.cursorStyle;
  }

  private set cursorStyle(v: number) {
    this.state.cursorStyle = v;
  }

  private get cursorVisible(): boolean {
    return this.state.cursorVisible;
  }

  private set cursorVisible(v: boolean) {
    this.state.cursorVisible = v;
  }

  private get wrapPending(): boolean {
    return this.state.wrapPending;
  }

  private set wrapPending(v: boolean) {
    this.state.wrapPending = v;
  }

  private get applicationCursorKeys(): boolean {
    return this.state.applicationCursorKeys;
  }

  private set applicationCursorKeys(v: boolean) {
    this.state.applicationCursorKeys = v;
  }

  private get scrollback(): ScreenCell[][] {
    return this.state.scrollback;
  }

  private set scrollback(v: ScreenCell[][]) {
    this.state.scrollback = v;
  }

  // DEC private modes
  // DECOM (CSI ? 6 h/l): origin mode (cursor addressing relative to scroll region)
  private get originMode(): boolean {
    return this.state.originMode;
  }

  private set originMode(v: boolean) {
    this.state.originMode = v;
  }

  // DECAWM (CSI ? 7 h/l): auto-wrap mode
  private get autoWrapMode(): boolean {
    return this.state.autoWrapMode;
  }

  private set autoWrapMode(v: boolean) {
    this.state.autoWrapMode = v;
  }

  // Scroll region (DECSTBM)
  private get scrollTop(): number {
    return this.state.scrollTop;
  }

  private set scrollTop(v: number) {
    this.state.scrollTop = v;
  }

  private get scrollBottom(): number {
    return this.state.scrollBottom;
  }

  private set scrollBottom(v: number) {
    this.state.scrollBottom = v;
  }

  // Tab stops (HTS/TAB). Defaults to every 8 columns.
  private get tabStops(): boolean[] {
    return this.state.tabStops;
  }

  private set tabStops(v: boolean[]) {
    this.state.tabStops = v;
  }

  // Window properties state
  private get windowProperties(): WindowProperties {
    return this.state.windowProperties;
  }

  private set windowProperties(v: WindowProperties) {
    this.state.windowProperties = v;
  }

  // Character set state
  private get characterSets(): CharacterSetState {
    return this.state.characterSets;
  }

  private set characterSets(v: CharacterSetState) {
    this.state.characterSets = v;
  }

  private get titleStack(): string[] {
    return this.state.titleStack;
  }

  private set titleStack(v: string[]) {
    this.state.titleStack = v;
  }

  private get iconNameStack(): string[] {
    return this.state.iconNameStack;
  }

  private set iconNameStack(v: string[]) {
    this.state.iconNameStack = v;
  }

  // UTF-8 mode state (DECSET/DECRST 2027)
  private get utf8Mode(): boolean {
    return this.state.utf8Mode;
  }

  private set utf8Mode(v: boolean) {
    this.state.utf8Mode = v;
  }

  // SGR state management
  // NOTE: must never reference DEFAULT_SGR_STATE directly, because we mutate currentSgrState.
  private get currentSgrState(): SgrState {
    return this.state.currentSgrState;
  }

  private set currentSgrState(v: SgrState) {
    this.state.currentSgrState = v;
  }

  // DECSCA (CSI Ps " q) character protection attribute.
  private get currentCharacterProtection(): "unprotected" | "protected" {
    return this.state.currentCharacterProtection;
  }

  private set currentCharacterProtection(v: "unprotected" | "protected") {
    this.state.currentCharacterProtection = v;
  }

  // Alternate screen buffer management
  private get alternateScreenManager(): AlternateScreenManager {
    return this.state.alternateScreenManager;
  }

  private readonly updateListeners = new Set<UpdateListener>();
  private readonly decModeListeners = new Set<DecModeListener>();
  private readonly chunkListeners = new Set<ChunkListener>();
  private readonly responseListeners = new Set<ResponseListener>();

  // Update batching
  // When processing a burst of bytes we want to avoid emitting intermediate
  // snapshots that can cause visible flicker (e.g. transient status-line writes).
  private get updateBatchDepth(): number {
    return this.state.updateBatchDepth;
  }

  private set updateBatchDepth(v: number) {
    this.state.updateBatchDepth = v;
  }

  private get updateDirty(): boolean {
    return this.state.updateDirty;
  }

  private set updateDirty(v: boolean) {
    this.state.updateDirty = v;
  }

  constructor(options: StatefulTerminalOptions) {

    this.cols = options.cols;
    this.rows = options.rows;

    const scrollbackLimit = clampInt(options.scrollbackLimit ?? 10000, 0, 200000);
    const alternateScreenManager = new AlternateScreenManager(this.cols, this.rows);

    this.state = createInitialTerminalState({
      cols: this.cols,
      rows: this.rows,
      scrollbackLimit,
      alternateScreenManager,
    });

    this.actions = {
      getCols: () => this.cols,
      getRows: () => this.rows,
      getCursorX: () => this._cursorX,
      getCursorY: () => this._cursorY,
      setCursorX: (x: number) => {
        this._cursorX = x;
      },
      setCursorY: (y: number) => {
        this._cursorY = y;
      },
      setWrapPending: (wrapPending: boolean) => {
        this.wrapPending = wrapPending;
      },
      mapRowParamToCursorY: (row: number) => this.mapRowParamToCursorY(row),

      clampCursor: () => this.clampCursor(),
      setOriginMode: (enable: boolean) => this.setOriginMode(enable),
      setAutoWrapMode: (enable: boolean) => this.setAutoWrapMode(enable),
      setCursorVisibility: (visible: boolean) => this.setCursorVisibility(visible),
      setApplicationCursorKeys: (enable: boolean) => this.setApplicationCursorKeys(enable),
      setUtf8Mode: (enable: boolean) => this.setUtf8Mode(enable),

      cursorForwardTab: (count: number) => cursorForwardTab(this.state, this.cols, count),
      cursorBackwardTab: (count: number) => cursorBackwardTab(this.state, count),
      clearTabStopAtCursor: () => clearTabStopAtCursor(this.state, this.cols),
      clearAllTabStops: () => clearAllTabStops(this.state, this.cols),

      setCharacterProtection: (isProtected: boolean) => {
        this.currentCharacterProtection = isProtected ? "protected" : "unprotected";
      },

      clearLine: (mode: 0 | 1 | 2) => this.clearLine(mode),
      clearLineSelective: (mode: 0 | 1 | 2) => this.clearLineSelective(mode),
      clearDisplay: (mode: 0 | 1 | 2 | 3) => this.clearDisplay(mode),
      clearDisplaySelective: (mode: 0 | 1 | 2 | 3) => this.clearDisplaySelective(mode),

      insertCharsInLine: (count: number) => this.insertCharsInLine(count),
      deleteCharsInLine: (count: number) => this.deleteCharsInLine(count),
      eraseCharacters: (count: number) => this.eraseCharacters(count),

      setScrollRegion: (top?: number, bottom?: number) => this.setScrollRegion(top, bottom),
      scrollUpInRegion: (lines: number) => this.scrollUpInRegion(lines),
      scrollDownInRegion: (lines: number) => this.scrollDownInRegion(lines),
      deleteLinesInRegion: (count: number) => this.deleteLinesInRegion(count),
      insertLinesInRegion: (count: number) => this.insertLinesInRegion(count),

      saveCursorPosition: () => {
        this.savedCursor = [this._cursorX, this._cursorY];
      },
      restoreCursorPosition: () => {
        if (this.savedCursor) {
          this._cursorX = this.savedCursor[0];
          this._cursorY = this.savedCursor[1];
          this.clampCursor();
        }
      },

      softReset: () => this.softReset(),
      setCursorStyle: (style: number) => this.setCursorStyle(style),

      switchToAlternateScreen: () => this.switchToAlternateScreen(),
      switchToAlternateScreenWithCursorSave: () => this.switchToAlternateScreenWithCursorSave(),
      switchToAlternateScreenWithCursorSaveAndClear: () => this.switchToAlternateScreenWithCursorSaveAndClear(),
      switchToPrimaryScreen: () => this.switchToPrimaryScreen(),
      switchToPrimaryScreenWithCursorRestore: () => this.switchToPrimaryScreenWithCursorRestore(),

      generateCharacterSetQueryResponse: () => this.generateCharacterSetQueryResponse(),
      handleWindowManipulation: (operation: number, params: number[]) => this.handleWindowManipulation(operation, params),
      handleEnhancedSgrMode: (params: number[]) => this.handleEnhancedSgrMode(params),
      handlePrivateSgrMode: (params: number[]) => this.handlePrivateSgrMode(params),
      handleSgrWithIntermediate: (params: number[], intermediate: string) => this.handleSgrWithIntermediate(params, intermediate),
      handleUnknownViSequence: (sequenceNumber: number) => this.handleUnknownViSequence(sequenceNumber),

      emitResponse: (response: string) => this.emitResponse(response),
      emitDecMode: (ev: DecModeEvent) => this.emitDecMode(ev),
    };

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
          this.requestUpdate();
        },
        handleTab: () => {
          this.emitControlChunk("TAB", 0x09);
          this.tab();
          this.requestUpdate();
        },
        handleShiftIn: () => {
          this.emitControlChunk("SI", 0x0f);
          this.shiftIn();
          this.requestUpdate();
        },
        handleShiftOut: () => {
          this.emitControlChunk("SO", 0x0e);
          this.shiftOut();
          this.requestUpdate();
        },
        handleLineFeed: () => {
          this.emitControlChunk("LF", 0x0a);
          this.lineFeed();
          this.requestUpdate();
        },
        handleFormFeed: () => {
          this.emitControlChunk("FF", 0x0c);
          this.lineFeed();
          this.requestUpdate();
        },
        handleCarriageReturn: () => {
          this.emitControlChunk("CR", 0x0d);
          this.carriageReturn();
          this.requestUpdate();
        },
        handleNormalByte: (byte: number) => {
          if (traceSettings.enabled) {
            this.emitChunk({
              _type: "trace.normalByte",
              implemented: true,
              cursorX: this._cursorX,
              cursorY: this._cursorY,
              byte,
            });
          }
          this.writePrintableByte(byte);
          this.requestUpdate();
        },
        handleEsc: (msg: EscMessage) => {
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.esc", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
          this.handleEsc(msg);
          this.requestUpdate();
        },
        handleCsi: (msg: CsiMessage) => {
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.csi", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
          this.handleCsi(msg);
          this.requestUpdate();
        },
        handleOsc: (msg: OscMessage) => {
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.osc", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
        },
        handleDcs: (msg: DcsMessage) => {
          // For now, DCS is treated as a consumed-but-ignored control string.
          // The critical behavior is that its payload never renders as normal bytes.
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.dcs", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
        },
        handleSgr: (msg: SgrSequence) => {
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.sgr", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
          this.handleSgr(msg);
          this.requestUpdate();
        },
        handleXtermOsc: (msg: XtermOscMessage) => {
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.osc", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
          this.handleXtermOsc(msg);
          this.requestUpdate();
        },
      },
    });
  }

  private shiftIn(): void {
    // SI: invoke G0 into GL
    this.characterSets.current = "G0";
  }

  private shiftOut(): void {
    // SO: invoke G1 into GL
    this.characterSets.current = "G1";
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
      cursorX: this._cursorX,
      cursorY: this._cursorY,
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
    this.requestUpdate();
  }

  public setIconName(iconName: string): void {
    this.windowProperties.iconName = iconName;
    this.requestUpdate();
  }

  public setTitleAndIcon(title: string): void {
    this.windowProperties.title = title;
    this.windowProperties.iconName = title;
    this.requestUpdate();
  }

  private withUpdateBatch<T>(fn: () => T): T {
    this.updateBatchDepth += 1;
    try {
      return fn();
    } finally {
      this.updateBatchDepth -= 1;
      if (this.updateBatchDepth === 0 && this.updateDirty) {
        this.updateDirty = false;
        this.emitUpdate();
      }
    }
  }

  private requestUpdate(): void {
    if (this.updateBatchDepth > 0) {
      this.updateDirty = true;
      return;
    }
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

  // Enhanced cursor state management methods
  public getCursorState(): CursorState {
    return {
      x: this._cursorX,
      y: this._cursorY,
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
    designateCharacterSetState(this.state, slot, charset);
  }

  /**
   * Get the currently designated character set for a slot
   */
  public getCharacterSet(slot: "G0" | "G1" | "G2" | "G3"): string {
    return getCharacterSetState(this.state, slot);
  }

  /**
   * Get the current active character set
   */
  public getCurrentCharacterSet(): string {
    return getCurrentCharacterSetState(this.state);
  }

  /**
   * Switch to a different character set slot
   */
  public switchCharacterSet(slot: "G0" | "G1" | "G2" | "G3"): void {
    switchCharacterSetState(this.state, slot);
  }

  /**
   * Enable or disable UTF-8 mode
   */
  public setUtf8Mode(enabled: boolean): void {
    setUtf8ModeState(this.state, enabled);
  }

  /**
   * Check if UTF-8 mode is enabled
   */
  public isUtf8Mode(): boolean {
    return isUtf8ModeState(this.state);
  }

  /**
   * Generate character set query response
   * Format: CSI ? 26 ; charset ST
   */
  private generateCharacterSetQueryResponse(): string {
    return generateCharacterSetQueryResponseState(this.state);
  }

  /**
   * Translate a character according to the current character set.
   * Handles special character sets like DEC Special Graphics.
   * 
   * @param ch The input character
   * @returns The translated character
   */
  private translateCharacter(ch: string): string {
    return translateCharacterState(this.state, ch);
  }

  public saveCursorState(): CursorState {
    return this.getCursorState();
  }

  public restoreCursorState(state: CursorState): void {
    // Validate and clamp coordinates to screen boundaries
    this._cursorX = Math.max(0, Math.min(this.cols - 1, state.x));
    const y = Math.max(0, Math.min(this.rows - 1, state.y));
    if (this.originMode) {
      this._cursorY = Math.max(this.scrollTop, Math.min(this.scrollBottom, y));
    } else {
      this._cursorY = y;
    }
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
      primaryBuffer.cursorX = this._cursorX;
      primaryBuffer.cursorY = this._cursorY;
      primaryBuffer.wrapPending = this.wrapPending;

      // Switch to alternate buffer
      this.alternateScreenManager.switchToAlternate();
      const alternateBuffer = this.alternateScreenManager.getCurrentBuffer();

      // Load alternate buffer state
      this._cursorX = alternateBuffer.cursorX;
      this._cursorY = alternateBuffer.cursorY;
      this.wrapPending = alternateBuffer.wrapPending;
    }
  }

  /**
   * DECSET 1047: Save cursor and switch to alternate screen buffer
   */
  private switchToAlternateScreenWithCursorSave(): void {
    // Save cursor position
    this.savedCursor = [this._cursorX, this._cursorY];

    // Switch to alternate screen
    this.switchToAlternateScreen();
  }

  /**
   * DECSET 1049: Save cursor, switch to alternate screen, and clear it
   */
  private switchToAlternateScreenWithCursorSaveAndClear(): void {
    // Save cursor position
    this.savedCursor = [this._cursorX, this._cursorY];

    // Switch to alternate screen
    this.switchToAlternateScreen();

    // Clear the alternate screen buffer
    this.alternateScreenManager.clearAlternateBuffer();
    this._cursorX = 0;
    this._cursorY = 0;
    this.wrapPending = false;
  }

  /**
   * DECRST 47: Switch back to normal screen buffer
   */
  private switchToPrimaryScreen(): void {
    if (this.alternateScreenManager.isAlternateActive()) {
      // Save current alternate buffer state
      const alternateBuffer = this.alternateScreenManager.getAlternateBuffer();
      alternateBuffer.cursorX = this._cursorX;
      alternateBuffer.cursorY = this._cursorY;
      alternateBuffer.wrapPending = this.wrapPending;

      // Switch to primary buffer
      this.alternateScreenManager.switchToPrimary();
      const primaryBuffer = this.alternateScreenManager.getCurrentBuffer();

      // Load primary buffer state
      this._cursorX = primaryBuffer.cursorX;
      this._cursorY = primaryBuffer.cursorY;
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
      this._cursorX = this.savedCursor[0];
      this._cursorY = this.savedCursor[1];
      this.clampCursor();
    }
  }

  public isAlternateScreenActive(): boolean {
    return this.alternateScreenManager.isAlternateActive();
  }

  public getScrollbackRowCount(): number {
    return this.scrollback.length;
  }

  public getViewportRows(viewportTopRow: number, rows: number): ReadonlyArray<ReadonlyArray<ScreenCell>> {
    return getViewportRowsState({
      cols: this.cols,
      cells: this.cells,
      scrollback: this.scrollback,
      isAlternateScreenActive: this.isAlternateScreenActive(),
      viewportTopRow,
      rows,
    });
  }

  private pushScrollbackRow(row: ReadonlyArray<ScreenCell>): void {
    pushScrollbackRowState(this.state, row);
  }

  private clearScrollback(): void {
    clearScrollbackState(this.state);
  }

  /**
   * Get the current buffer's cells (either primary or alternate)
   */
  private get cells(): ScreenCell[][] {
    return this.alternateScreenManager.getCurrentBuffer().cells;
  }

  private getBufferOpsContext(): bufferOps.BufferOpsContext {
    return {
      cols: this.cols,
      rows: this.rows,
      getCells: () => this.cells,
      getCurrentBuffer: () => this.alternateScreenManager.getCurrentBuffer(),
      isAlternateScreenActive: () => this.isAlternateScreenActive(),
      pushScrollbackRow: (row: ReadonlyArray<ScreenCell>) => this.pushScrollbackRow(row),
      clearScrollback: () => this.clearScrollback(),
    };
  }

  public pushPtyText(text: string): void {
    this.withUpdateBatch(() => {
      const bytes = new TextEncoder().encode(text);
      this.parser.pushBytes(bytes);
    });
  }

  public reset(): void {
    // Hard reset (best-effort): return to primary buffer, clear screens, and reset modes/state.
    this.alternateScreenManager.switchToPrimary();

    const primary = this.alternateScreenManager.getPrimaryBuffer();
    primary.cells = createCellGrid(this.cols, this.rows);
    primary.cursorX = 0;
    primary.cursorY = 0;
    primary.savedCursor = null;
    primary.wrapPending = false;

    const alternate = this.alternateScreenManager.getAlternateBuffer();
    alternate.cells = createCellGrid(this.cols, this.rows);
    alternate.cursorX = 0;
    alternate.cursorY = 0;
    alternate.savedCursor = null;
    alternate.wrapPending = false;

    this._cursorX = 0;
    this._cursorY = 0;
    this.savedCursor = null;
    this.wrapPending = false;
    this.cursorStyle = 1;
    this.cursorVisible = true;
    this.applicationCursorKeys = false;

    this.originMode = false;
    this.autoWrapMode = true;

    this.scrollTop = 0;
    this.scrollBottom = this.rows - 1;

    this.currentSgrState = createDefaultSgrState();

    this.clearScrollback();
    this.currentCharacterProtection = "unprotected";

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
    this.tabStops = initializeTabStops(this.cols);

    // Clear title/icon name stacks
    this.titleStack = [];
    this.iconNameStack = [];

    this.emitUpdate();
  }

  private softReset(): void {
    // DECSTR (soft reset): reset modes/state without clearing the screen.
    this._cursorX = 0;
    this._cursorY = 0;
    this.savedCursor = null;
    this.wrapPending = false;
    this.cursorStyle = 1;
    this.cursorVisible = true;
    this.applicationCursorKeys = false;

    this.originMode = false;
    this.autoWrapMode = true;

    this.scrollTop = 0;
    this.scrollBottom = this.rows - 1;

    this.currentCharacterProtection = "unprotected";

    this.currentSgrState = createDefaultSgrState();

    this.characterSets = {
      G0: "B",
      G1: "B",
      G2: "B",
      G3: "B",
      current: "G0",
    };

    this.utf8Mode = true;
    this.tabStops = initializeTabStops(this.cols);
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
    if (!traceSettings.enabled) {
      return;
    }
    for (const listener of this.chunkListeners) {
      listener(chunk);
    }
  }

  private emitControlChunk(name: TraceControlName, byte: number): void {
    if (!traceSettings.enabled) {
      return;
    }
    this.emitChunk({ _type: "trace.control", implemented: true, cursorX: this._cursorX, cursorY: this._cursorY, name, byte });
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

  private insertCharsInLine(count: number): void {
    bufferOps.insertCharsInLine(this.state, this.getBufferOpsContext(), count);
  }

  private deleteCharsInLine(count: number): void {
    bufferOps.deleteCharsInLine(this.state, this.getBufferOpsContext(), count);
  }

  private putChar(ch: string): void {
    bufferOps.putChar(this.state, this.getBufferOpsContext(), ch);
  }

  private carriageReturn(): void {
    bufferOps.carriageReturn(this.state);
  }

  private lineFeed(): void {
    bufferOps.lineFeed(this.state, this.getBufferOpsContext());
  }

  private backspace(): void {
    bufferOps.backspace(this.state);
  }

  private tab(): void {
    this.wrapPending = false;

    cursorForwardTab(this.state, this.cols, 1);
  }

  private setOriginMode(enable: boolean): void {
    this.originMode = enable;
    // vt100/xterm behavior: home the cursor when toggling origin mode.
    this._cursorX = 0;
    this._cursorY = enable ? this.scrollTop : 0;
    this.wrapPending = false;
    this.clampCursor();
  }

  private setAutoWrapMode(enable: boolean): void {
    this.autoWrapMode = enable;
    if (!enable) {
      this.wrapPending = false;
    }
  }

  private mapRowParamToCursorY(row1Based: number): number {
    const base = this.originMode ? this.scrollTop : 0;
    return base + (row1Based - 1);
  }

  private clampCursor(): void {
    this._cursorX = Math.max(0, Math.min(this.cols - 1, this._cursorX));
    const y = Math.max(0, Math.min(this.rows - 1, this._cursorY));
    if (this.originMode) {
      this._cursorY = Math.max(this.scrollTop, Math.min(this.scrollBottom, y));
    } else {
      this._cursorY = y;
    }
    this.wrapPending = false;
  }

  private clearLine(mode: 0 | 1 | 2): void {
    bufferOps.clearLine(this.state, this.getBufferOpsContext(), mode);
  }

  private clearLineSelective(mode: 0 | 1 | 2): void {
    bufferOps.clearLineSelective(this.state, this.getBufferOpsContext(), mode);
  }

  private eraseCharacters(count: number): void {
    bufferOps.eraseCharacters(this.state, this.getBufferOpsContext(), count);
  }

  private setScrollRegion(top?: number, bottom?: number): void {
    bufferOps.setScrollRegion(this.state, this.rows, top, bottom);
  }

  private scrollUpInRegion(lines: number): void {
    bufferOps.scrollUpInRegion(this.state, this.getBufferOpsContext(), lines);
  }

  private scrollDownInRegion(lines: number): void {
    bufferOps.scrollDownInRegion(this.state, this.getBufferOpsContext(), lines);
  }

  private deleteLinesInRegion(count: number): void {
    bufferOps.deleteLinesInRegion(this.state, this.getBufferOpsContext(), count);
  }

  private insertLinesInRegion(count: number): void {
    bufferOps.insertLinesInRegion(this.state, this.getBufferOpsContext(), count);
  }

  private clearDisplay(mode: 0 | 1 | 2 | 3): void {
    bufferOps.clearDisplay(this.state, this.getBufferOpsContext(), mode);
  }

  private clearDisplaySelective(mode: 0 | 1 | 2 | 3): void {
    bufferOps.clearDisplaySelective(this.state, this.getBufferOpsContext(), mode);
  }

  private handleCsi(msg: CsiMessage): void {
    handleCsiDispatch(this.actions, msg);
  }

  private handleEsc(msg: EscMessage): void {
    switch (msg._type) {
      case "esc.saveCursor":
        this.savedCursor = [this._cursorX, this._cursorY];
        return;
      case "esc.restoreCursor":
        if (this.savedCursor) {
          this._cursorX = this.savedCursor[0];
          this._cursorY = this.savedCursor[1];
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
        if (this._cursorY <= this.scrollTop) {
          this._cursorY = this.scrollTop;
          this.scrollDownInRegion(1);
          return;
        }

        this._cursorY = Math.max(this.scrollTop, this._cursorY - 1);
        return;
      }

      case "esc.index":
        this.lineFeed();
        return;

      case "esc.nextLine":
        this.carriageReturn();
        this.lineFeed();
        return;

      case "esc.horizontalTabSet":
        setTabStopAtCursor(this.state, this.cols);
        return;

      case "esc.resetToInitialState":
        this.reset();
        return;
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
        this.emitResponse(generateForegroundColorResponse(this.currentSgrState));
        return;

      case "osc.queryBackgroundColor":
        // OSC 11;?: Query default background color
        // Respond with current theme background color
        this.emitResponse(generateBackgroundColorResponse(this.currentSgrState));
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

}
