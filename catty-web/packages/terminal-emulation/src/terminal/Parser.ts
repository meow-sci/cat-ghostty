import { getLogger } from "@catty/log";

import { ParserOptions } from "./ParserOptions";
import { parseSgr, parseSgrParamsAndSeparators } from "./ParseSgr";
import { parseCsi } from "./ParseCsi";


type State = "normal" | "esc" | "csi" | "osc" | "osc_esc";


export class Parser {

  private handlers: ParserOptions["handlers"];
  private log: ReturnType<typeof getLogger>;
  private emitNormalBytesDuringEscapeSequence: boolean;
  private processC0ControlsDuringEscapeSequence: boolean;

  private state: State = "normal";
  private escapeSequence: number[] = [];
  private csiSequence: string = "";

  constructor(options: ParserOptions) {
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

    // Always add byte to the escape sequence and csi sequence
    this.csiSequence += String.fromCharCode(byte);
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
        this.csiSequence = "";
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
      const raw = bytesToString(this.escapeSequence);
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
    const raw = bytesToString(this.escapeSequence);

    // Stub only (no OSC parsing here).
    this.log.debug(`OSC (opaque, ${terminator}): ${raw}`);
    this.handlers.handleOsc(raw);

    this.resetEscapeState();
    return;
  }

  private finishCsiSequence(): void {
    const raw = bytesToString(this.escapeSequence);
    const finalByte = this.escapeSequence[this.escapeSequence.length - 1];

    // CSI SGR: parse using the standalone SGR parser
    if (finalByte === 0x6d) {
      const { params, separators } = parseSgrParamsAndSeparators(raw);
      const sgrMessages = parseSgr(params, separators);
      this.handlers.handleSgr(sgrMessages);
      this.resetEscapeState();
      return;
    }

    const msg = parseCsi(this.escapeSequence, raw);
    this.handlers.handleCsi(msg);
    this.resetEscapeState();
    return;
  }

  private resetEscapeState(): void {
    this.state = "normal";
    this.escapeSequence = [];
    this.csiSequence = "";
    return;
  }

}



function bytesToString(bytes: number[]): string {
  let out = "";
  for (let i = 0; i < bytes.length; i++) {
    out += String.fromCharCode(bytes[i]);
  }
  return out;
}