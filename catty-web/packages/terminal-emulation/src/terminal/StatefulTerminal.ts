import { getLogger } from "@catty/log";

import type { TerminalTraceChunk, TraceControlName } from "./TerminalTrace";
import { traceSettings } from "./traceSettings";
import { Parser } from "./Parser";
import type { DcsMessage, EscMessage, CsiMessage, OscMessage, SgrSequence, XtermOscMessage } from "./TerminalEmulationTypes";
import { createDefaultSgrState, type SgrState } from "./SgrStyleManager";
import { processSgrMessages } from "./SgrStateProcessor";

import { AlternateScreenManager } from "./stateful/alternateScreen";
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
  setTabStopAtCursor,
} from "./stateful/tabStops";

import * as cursorOps from "./stateful/cursor";
import * as windowManipulation from "./stateful/windowManipulation";
import * as sgrModes from "./stateful/sgrModes";
import * as alternateScreenOps from "./stateful/alternateScreenOps";
import * as resetOps from "./stateful/reset";

import * as bufferOps from "./stateful/bufferOps";

import type { TerminalActions } from "./stateful/actions";
import { handleCsi as handleCsiDispatch } from "./stateful/handlers/csi";
import { handleEsc as handleEscDispatch } from "./stateful/handlers/esc";
import { handleXtermOsc as handleXtermOscDispatch } from "./stateful/handlers/osc";
import { handleDcs as handleDcsDispatch } from "./stateful/handlers/dcs";

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
      mapRowParamToCursorY: (row: number) => cursorOps.mapRowParamToCursorY(this.state, row),

      clampCursor: () => this.clampCursor(),
      setOriginMode: (enable: boolean) => this.setOriginMode(enable),
      setAutoWrapMode: (enable: boolean) => this.setAutoWrapMode(enable),
      setCursorVisibility: (visible: boolean) => this.setCursorVisibility(visible),
      setApplicationCursorKeys: (enable: boolean) => this.setApplicationCursorKeys(enable),
      setUtf8Mode: (enable: boolean) => this.setUtf8Mode(enable),

      getScrollTop: () => this.scrollTop,
      getScrollBottom: () => this.scrollBottom,
      designateCharacterSet: (slot: "G0" | "G1" | "G2" | "G3", charset: string) => {
        this.designateCharacterSet(slot, charset);
      },
      lineFeed: () => this.lineFeed(),
      carriageReturn: () => this.carriageReturn(),
      setTabStopAtCursor: () => setTabStopAtCursor(this.state, this.cols),
      resetToInitialState: () => this.reset(),

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

      savePrivateMode: (modes: number[]) => this.savePrivateMode(modes),
      restorePrivateMode: (modes: number[]) => this.restorePrivateMode(modes),

      softReset: () => this.softReset(),
      setCursorStyle: (style: number) => this.setCursorStyle(style),

      switchToAlternateScreen: () => this.switchToAlternateScreen(),
      switchToAlternateScreenWithCursorSave: () => this.switchToAlternateScreenWithCursorSave(),
      switchToAlternateScreenWithCursorSaveAndClear: () => this.switchToAlternateScreenWithCursorSaveAndClear(),
      switchToPrimaryScreen: () => this.switchToPrimaryScreen(),
      switchToPrimaryScreenWithCursorRestore: () => this.switchToPrimaryScreenWithCursorRestore(),

      getWindowTitle: () => this.windowProperties.title,
      setWindowTitle: (title: string) => this.setWindowTitle(title),
      setIconName: (iconName: string) => this.setIconName(iconName),
      setTitleAndIcon: (title: string) => this.setTitleAndIcon(title),
      getCurrentSgrState: () => ({ ...this.currentSgrState }),

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
          if (traceSettings.enabled) {
            this.emitChunk({ _type: "trace.dcs", implemented: msg.implemented, cursorX: this._cursorX, cursorY: this._cursorY, msg });
          }
          handleDcsDispatch(this.actions, msg);
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
    windowManipulation.handleWindowManipulation(
      this.state,
      this.log,
      {
        setWindowTitle: (title: string) => this.setWindowTitle(title),
        setIconName: (iconName: string) => this.setIconName(iconName),
      },
      operation,
      params,
    );
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

  private savePrivateMode(modes: number[]): void {
    for (const mode of modes) {
      let enabled = false;
      switch (mode) {
        case 1: // DECCKM
          enabled = this.applicationCursorKeys;
          break;
        case 6: // DECOM
          enabled = this.originMode;
          break;
        case 7: // DECAWM
          enabled = this.autoWrapMode;
          break;
        case 25: // DECTCEM
          enabled = this.cursorVisible;
          break;
        case 47:
        case 1047:
        case 1049:
          enabled = this.isAlternateScreenActive();
          break;
        default:
          continue;
      }
      this.state.savedPrivateModes.set(mode, enabled);
    }
  }

  private restorePrivateMode(modes: number[]): void {
    for (const mode of modes) {
      const enabled = this.state.savedPrivateModes.get(mode);
      if (enabled === undefined) {
        continue;
      }

      switch (mode) {
        case 1: // DECCKM
          this.setApplicationCursorKeys(enabled);
          break;
        case 6: // DECOM
          this.setOriginMode(enabled);
          break;
        case 7: // DECAWM
          this.setAutoWrapMode(enabled);
          break;
        case 25: // DECTCEM
          this.setCursorVisibility(enabled);
          break;
        case 47:
        case 1047:
        case 1049:
          if (enabled) {
            if (!this.isAlternateScreenActive()) {
              if (mode === 1049) {
                this.switchToAlternateScreenWithCursorSaveAndClear();
              } else if (mode === 1047) {
                this.switchToAlternateScreenWithCursorSave();
              } else {
                this.switchToAlternateScreen();
              }
            }
          } else {
            if (this.isAlternateScreenActive()) {
              if (mode === 1049 || mode === 1047) {
                this.switchToPrimaryScreenWithCursorRestore();
              } else {
                this.switchToPrimaryScreen();
              }
            }
          }
          break;
      }
    }
  }

  // Alternate screen buffer switching methods

  /**
   * DECSET 47: Switch to alternate screen buffer
   */
  private switchToAlternateScreen(): void {
    alternateScreenOps.switchToAlternateScreen(this.state, this.alternateScreenManager);
  }

  /**
   * DECSET 1047: Save cursor and switch to alternate screen buffer
   */
  private switchToAlternateScreenWithCursorSave(): void {
    alternateScreenOps.switchToAlternateScreenWithCursorSave(this.state, this.alternateScreenManager);
  }

  /**
   * DECSET 1049: Save cursor, switch to alternate screen, and clear it
   */
  private switchToAlternateScreenWithCursorSaveAndClear(): void {
    alternateScreenOps.switchToAlternateScreenWithCursorSaveAndClear(this.state, this.alternateScreenManager);
  }

  /**
   * DECRST 47: Switch back to normal screen buffer
   */
  private switchToPrimaryScreen(): void {
    alternateScreenOps.switchToPrimaryScreen(this.state, this.alternateScreenManager);
  }

  /**
   * DECRST 1047/1049: Switch to normal screen and restore cursor
   */
  private switchToPrimaryScreenWithCursorRestore(): void {
    alternateScreenOps.switchToPrimaryScreenWithCursorRestore(this.state, this.alternateScreenManager, () => this.clampCursor());
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
    resetOps.hardReset(this.state, this.cols, this.rows);
    this.emitUpdate();
  }

  private softReset(): void {
    resetOps.softReset(this.state, this.cols, this.rows);
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
    cursorOps.setOriginMode(this.state, this.cols, this.rows, enable);
  }

  private setAutoWrapMode(enable: boolean): void {
    cursorOps.setAutoWrapMode(this.state, enable);
  }

  private clampCursor(): void {
    cursorOps.clampCursor(this.state, this.cols, this.rows);
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
    handleEscDispatch(this.actions, msg);
  }

  private handleXtermOsc(msg: XtermOscMessage): void {
    handleXtermOscDispatch(this.actions, msg);
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
    sgrModes.handleEnhancedSgrMode(this.currentSgrState, this.log, params);
  }

  /**
   * Handle private SGR sequences with ? prefix (e.g., CSI ? 4 m)
   * These are typically used for private/experimental features.
   */
  private handlePrivateSgrMode(params: number[]): void {
    sgrModes.handlePrivateSgrMode(this.currentSgrState, this.log, params);
  }

  /**
   * Handle SGR sequences with intermediate characters (e.g., CSI 0 % m)
   * These are used for special SGR attribute resets or modifications.
   */
  private handleSgrWithIntermediate(params: number[], intermediate: string): void {
    const updated = sgrModes.handleSgrWithIntermediate(this.log, params, intermediate);
    if (updated) {
      this.currentSgrState = updated;
    }
  }

  /**
   * Handle unknown vi sequences (e.g., CSI 11M)
   * These sequences appear in vi usage but are not part of standard terminal specifications.
   * We gracefully acknowledge them without implementing specific behavior.
   */
  private handleUnknownViSequence(sequenceNumber: number): void {
    sgrModes.handleUnknownViSequence(this.log, sequenceNumber);
  }

}
