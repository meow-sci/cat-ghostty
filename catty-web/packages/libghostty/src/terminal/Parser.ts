import { getLogger } from "@catty/log";

import { GhosttyVtInstance } from "../ghostty-vt";
import { CsiMessage, CsiSetCursorStyle, CsiDecModeSet, CsiDecModeReset, CsiCursorUp, CsiCursorDown, CsiCursorForward, CsiCursorBackward, CsiCursorNextLine, CsiCursorPrevLine, CsiCursorHorizontalAbsolute, CsiCursorPosition, CsiEraseInDisplayMode, CsiEraseInDisplay, CsiEraseInLineMode, CsiEraseInLine, CsiScrollUp, CsiScrollDown, CsiSetScrollRegion, CsiSaveCursorPosition, CsiRestoreCursorPosition, CsiUnknown } from "./TerminalEmulationTypes";
import { ParserOptions } from "./ParserOptions";


type State = "normal" | "esc" | "csi" | "osc" | "osc_esc";


export class Parser {

  // @ts-ignore
  private wasm: GhosttyVtInstance;
  private handlers: ParserOptions["handlers"];
  private log: ReturnType<typeof getLogger>;
  private emitNormalBytesDuringEscapeSequence: boolean;
  private processC0ControlsDuringEscapeSequence: boolean;

  private state: State = "normal";
  private escapeSequence: number[] = [];

  constructor(options: ParserOptions) {
    this.wasm = options.wasm;
    this.handlers = options.handlers;
    this.log = options.log;
    this.emitNormalBytesDuringEscapeSequence = options.emitNormalBytesDuringEscapeSequence ?? false;
    this.processC0ControlsDuringEscapeSequence = options.processC0ControlsDuringEscapeSequence ?? true;
  }

  public pushBytes(data: Uint8Array): void {
    for (let i = 0; i < data.length; i++) {
      const byte = data[i];
      this.pushByte(byte);
    }
  }

  public pushByte(data: number): void {
    this.processByte(data);
  }

  private processByte(byte: number): void {

    if (byte < 0 || byte > 255) {
      // TODO: FIXME: what about utf-8 ... ? are those just multi byte sequences?
      this.log.warn(`Ignoring out-of-range byte: ${byte}`);
      return;
    }

    switch (this.state) {

      case "normal": {

        // C0 controls (including BEL/BS/TAB/LF/CR) execute immediately in normal mode
        if (byte < 0x20 && byte !== 0x1b && this.handleC0ExceptEscape(byte)) {
          return;
        };

        if (byte === 0x1b) {
          return this.startEscapeSequence(byte);
        }

        return this.handleNormalByte(byte);
      }

      case "esc": {
        // Optional: still execute C0 controls while parsing ESC sequences (common terminal behavior).
        if (byte < 0x20 && byte !== 0x1b && this.processC0ControlsDuringEscapeSequence && this.handleC0ExceptEscape(byte)) {
          return;
        };

        return this.handleEscapeByte(byte);
      }

      case "csi": {
        // Optional: still execute C0 controls while parsing CSI (common terminal behavior).
        if (byte < 0x20 && byte !== 0x1b && this.processC0ControlsDuringEscapeSequence && this.handleC0ExceptEscape(byte)) {
          return;
        }

        return this.handleCsiByte(byte);
      }

      case "osc": {
        // cannot process C0 controls inside OSC except for BEL (0x07) and ESC (0x1b) which are terminators
        return this.handleOscByte(byte);
      }

      case "osc_esc": {
        return this.handleOscEscByte(byte);
      }
    }

  }

  private maybeEmitNormalByteDuringEscapeSequence(byte: number): void {
    if (this.emitNormalBytesDuringEscapeSequence) {
      this.handleNormalByte(byte);
    }
  }

  private handleOscByte(byte: number): void {

    // guard against bytes outside the allowed OSC byte range (0x20 - 0x7E)
    // special allowances for BEL (0x07) and ESC (0x1b) must be allowed, these are valid terminators for OSC sequences
    if (byte !== 0x07 && byte !== 0x1b && (byte < 0x20 || byte > 0x7e)) {
      this.log.warn(`OSC: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    this.escapeSequence.push(byte);

    // OSC terminators: BEL or ST (ESC \)
    if (byte === 0x07) {
      this.finishOscSequence("BEL");
      return;
    }

    if (byte === 0x1b) {
      this.state = "osc_esc";
      return;
    }

    return;
  }

  private handleOscEscByte(byte: number): void {
    // We just saw an ESC while inside OSC. If next byte is "\" then it's ST terminator.
    // Otherwise, it was a literal ESC in the OSC payload and we continue in OSC.
    this.escapeSequence.push(byte);

    if (byte === 0x5c) {
      this.finishOscSequence("ST");
      return;
    }

    if (byte === 0x07) {
      this.finishOscSequence("BEL");
      return;
    }

    // Continue OSC payload.
    this.state = "osc";
    return;
  }

  private handleCsiByte(byte: number): void {

    // guard against bytes outside the allowed CSI byte range (0x20 - 0x7E)
    if (byte < 0x20 || byte > 0x7e) {
      this.log.warn(`CSI: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    this.escapeSequence.push(byte);

    // CSI final bytes are 0x40-0x7E.
    if (byte >= 0x40 && byte <= 0x7e) {
      this.finishCsiSequence();
      return;
    }

    return;
  }

  // handle ESC byte, accumulate into escapeSequence
  private handleEscapeByte(byte: number): void {

    // guard against bytes outside the allowed ESC byte range (0x20 - 0x7E)
    if (byte < 0x20 || byte > 0x7e) {
      this.log.warn(`ESC: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    // First byte after ESC decides the submode for CSI/OSC.
    if (this.escapeSequence.length === 1) {
      if (byte === 0x5b) {
        this.escapeSequence.push(byte);
        this.state = "csi";
        return;
      }

      if (byte === 0x5d) {
        this.escapeSequence.push(byte);
        this.state = "osc";
        return;
      }
    }

    this.escapeSequence.push(byte);

    // Basic ESC sequence: intermediates 0x20-0x2F, final 0x30-0x7E.
    if (byte >= 0x30 && byte <= 0x7e) {
      const raw = this.bytesToString(this.escapeSequence);
      this.log.debug(`ESC (opaque): ${raw}`);
      this.resetEscapeState();
      return;
    }

    return;
  }

  private handleNormalByte(byte: number): void {
    this.handlers.handleNormalByte(byte);
    return;
  }

  private handleC0ExceptEscape(byte: number): boolean {
    switch (byte) {
      case 0x07: // Bell
        this.handlers.handleBell();
        return true;
      case 0x08: // Backspace
        this.handlers.handleBackspace();
        return true;
      case 0x09: // Tab
        this.handlers.handleTab();
        return true;
      case 0x0a: // Line Feed
        this.handlers.handleLineFeed();
        return true;
      case 0x0c: // Form Feed
        this.handlers.handleFormFeed();
        return true;
      case 0x0d: // Carriage Return
        this.handlers.handleCarriageReturn();
        return true;
    }
    return false;
  }

  private startEscapeSequence(byte: number): void {
    this.state = "esc";
    this.escapeSequence = [byte];
    return;
  }

  private finishOscSequence(terminator: "BEL" | "ST"): void {
    const raw = this.bytesToString(this.escapeSequence);

    // Stub only (no OSC parsing here).
    this.log.debug(`OSC (opaque, ${terminator}): ${raw}`);
    this.handlers.handleOsc(raw);

    this.resetEscapeState();
    return;
  }

  private finishCsiSequence(): void {
    const raw = this.bytesToString(this.escapeSequence);
    const finalByte = this.escapeSequence[this.escapeSequence.length - 1];

    // CSI SGR: keep opaque, delegate later.
    if (finalByte === 0x6d) {
      this.log.debug(`CSI SGR (opaque): ${raw}`);
      this.handlers.handleSgr(raw);
      this.resetEscapeState();
      return;
    }

    const msg = this.parseNonSgrCsi(this.escapeSequence, raw);
    this.handlers.handleCsi(msg);
    this.resetEscapeState();
    return;
  }

  private resetEscapeState(): void {
    this.state = "normal";
    this.escapeSequence = [];
    return;
  }

  private bytesToString(bytes: number[]): string {
    let out = "";
    for (let i = 0; i < bytes.length; i++) {
      out += String.fromCharCode(bytes[i]);
    }
    return out;
  }

  private parseNonSgrCsi(bytes: number[], raw: string): CsiMessage {
    // bytes: ESC [ ... final
    const finalByte = bytes[bytes.length - 1];
    const final = String.fromCharCode(finalByte);

    let paramsText = "";
    let intermediate = "";

    for (let i = 2; i < bytes.length - 1; i++) {
      const b = bytes[i];
      if (b >= 0x30 && b <= 0x3f) {
        paramsText += String.fromCharCode(b);
        continue;
      }
      if (b >= 0x20 && b <= 0x2f) {
        intermediate += String.fromCharCode(b);
        continue;
      }
    }

    const parsed = this.parseCsiParams(paramsText);
    const isPrivate = parsed.private;
    const params = parsed.params;

    // DECSCUSR: CSI Ps SP q
    if (final === "q" && intermediate === " ") {
      const style = this.getParam(params, 0, 0);
      const msg: CsiSetCursorStyle = { _type: "csi.setCursorStyle", raw, style };
      return msg;
    }

    // DEC private modes: CSI ? Pm h / l
    if (isPrivate && (final === "h" || final === "l")) {
      const modes = params.slice();
      if (final === "h") {
        const msg: CsiDecModeSet = { _type: "csi.decModeSet", raw, modes };
        return msg;
      }
      const msg: CsiDecModeReset = { _type: "csi.decModeReset", raw, modes };
      return msg;
    }

    if (final === "A") {
      const msg: CsiCursorUp = { _type: "csi.cursorUp", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "B") {
      const msg: CsiCursorDown = { _type: "csi.cursorDown", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "C") {
      const msg: CsiCursorForward = { _type: "csi.cursorForward", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "D") {
      const msg: CsiCursorBackward = { _type: "csi.cursorBackward", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "E") {
      const msg: CsiCursorNextLine = { _type: "csi.cursorNextLine", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "F") {
      const msg: CsiCursorPrevLine = { _type: "csi.cursorPrevLine", raw, count: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "G") {
      const msg: CsiCursorHorizontalAbsolute = { _type: "csi.cursorHorizontalAbsolute", raw, column: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "H" || final === "f") {
      const msg: CsiCursorPosition = {
        _type: "csi.cursorPosition",
        raw,
        row: this.getParam(params, 0, 1),
        column: this.getParam(params, 1, 1),
      };
      return msg;
    }

    if (final === "J") {
      const modeValue = this.getParam(params, 0, 0);
      const mode: CsiEraseInDisplayMode = (modeValue === 0 || modeValue === 1 || modeValue === 2 || modeValue === 3) ? modeValue : 0;
      const msg: CsiEraseInDisplay = { _type: "csi.eraseInDisplay", raw, mode };
      return msg;
    }

    if (final === "K") {
      const modeValue = this.getParam(params, 0, 0);
      const mode: CsiEraseInLineMode = (modeValue === 0 || modeValue === 1 || modeValue === 2) ? modeValue : 0;
      const msg: CsiEraseInLine = { _type: "csi.eraseInLine", raw, mode };
      return msg;
    }

    if (final === "S") {
      const msg: CsiScrollUp = { _type: "csi.scrollUp", raw, lines: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "T" && params.length <= 1 && !isPrivate) {
      const msg: CsiScrollDown = { _type: "csi.scrollDown", raw, lines: this.getParam(params, 0, 1) };
      return msg;
    }

    if (final === "r") {
      const top = params.length >= 1 ? params[0] : undefined;
      const bottom = params.length >= 2 ? params[1] : undefined;
      const msg: CsiSetScrollRegion = { _type: "csi.setScrollRegion", raw, top, bottom };
      return msg;
    }

    if (final === "s") {
      const msg: CsiSaveCursorPosition = { _type: "csi.saveCursorPosition", raw };
      return msg;
    }

    if (final === "u") {
      const msg: CsiRestoreCursorPosition = { _type: "csi.restoreCursorPosition", raw };
      return msg;
    }

    const unknown: CsiUnknown = {
      _type: "csi.unknown",
      raw,
      private: isPrivate,
      params,
      intermediate,
      final,
    };

    return unknown;
  }

  private parseCsiParams(paramText: string): { private: boolean; params: number[] } {
    let isPrivate = false;
    let text = paramText;

    if (text.startsWith("?")) {
      isPrivate = true;
      text = text.slice(1);
    }

    const params: number[] = [];

    if (text.length === 0) {
      return { private: isPrivate, params };
    }

    const parts = text.split(";");
    for (let i = 0; i < parts.length; i++) {
      const part = parts[i];
      if (part.length === 0) {
        continue;
      }
      const n = Number.parseInt(part, 10);
      if (!Number.isFinite(n)) {
        continue;
      }
      params.push(n);
    }

    return { private: isPrivate, params };
  }

  private getParam(params: number[], index: number, fallback: number): number {
    const v = params[index];
    if (v === undefined) {
      return fallback;
    }
    return v;
  }
}
