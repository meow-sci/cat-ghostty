import { getLogger } from "@catty/log";

import type { ParserOptions } from "./ParserOptions";
import { parseSgr, parseSgrParamsAndSeparators } from "./ParseSgr";
import { parseCsi } from "./ParseCsi";
import { parseOsc } from "./ParseOsc";
import type { EscMessage, OscMessage, SgrSequence } from "./TerminalEmulationTypes";


type State = "normal" | "esc" | "csi" | "osc" | "osc_esc";


export class Parser {

  private handlers: ParserOptions["handlers"];
  private log: ReturnType<typeof getLogger>;
  private emitNormalBytesDuringEscapeSequence: boolean;
  private processC0ControlsDuringEscapeSequence: boolean;

  private state: State = "normal";
  private escapeSequence: number[] = [];
  private csiSequence: string = "";

  // UTF-8 decoding state
  private utf8Buffer: number[] = [];
  private utf8ExpectedLength = 0;

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

      // DECSC / DECRC
      if (byte === 0x37 || byte === 0x38) {
        this.escapeSequence.push(byte);
        const raw = bytesToString(this.escapeSequence);
        const msg: EscMessage = byte === 0x37
          ? { _type: "esc.saveCursor", raw, implemented: true }
          : { _type: "esc.restoreCursor", raw, implemented: true };
        this.handlers.handleEsc(msg);
        this.resetEscapeState();
        return;
      }

      // Character set designation: ESC ( X, ESC ) X, ESC * X, ESC + X
      // These are two-byte sequences after ESC
      if (byte === 0x28 || byte === 0x29 || byte === 0x2a || byte === 0x2b) {
        this.escapeSequence.push(byte);
        // Need one more byte for the character set identifier
        return;
      }
    }

    // Second byte after ESC for character set designation
    if (this.escapeSequence.length === 2) {
      const firstByte = this.escapeSequence[1];
      if (firstByte === 0x28 || firstByte === 0x29 || firstByte === 0x2a || firstByte === 0x2b) {
        this.escapeSequence.push(byte);
        const raw = bytesToString(this.escapeSequence);
        
        // Determine which G slot is being designated
        let slot: "G0" | "G1" | "G2" | "G3";
        if (firstByte === 0x28) slot = "G0";
        else if (firstByte === 0x29) slot = "G1";
        else if (firstByte === 0x2a) slot = "G2";
        else slot = "G3";
        
        const charset = String.fromCharCode(byte);
        const msg: EscMessage = {
          _type: "esc.designateCharacterSet",
          raw,
          slot,
          charset,
          implemented: true
        };
        this.handlers.handleEsc(msg);
        this.resetEscapeState();
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
    // Handle UTF-8 multi-byte sequences
    if (this.handleUtf8Byte(byte)) {
      return;
    }
    
    // Single ASCII byte
    this.handlers.handleNormalByte(byte);
    return;
  }

  private handleUtf8Byte(byte: number): boolean {
    // If we're not in a UTF-8 sequence and this is ASCII, let it pass through
    if (this.utf8ExpectedLength === 0 && byte < 0x80) {
      return false; // Not a UTF-8 sequence, handle as normal ASCII
    }

    // Start of a new UTF-8 sequence
    if (this.utf8ExpectedLength === 0) {
      if ((byte & 0xE0) === 0xC0) {
        // 2-byte sequence: 110xxxxx
        this.utf8ExpectedLength = 2;
      } else if ((byte & 0xF0) === 0xE0) {
        // 3-byte sequence: 1110xxxx
        this.utf8ExpectedLength = 3;
      } else if ((byte & 0xF8) === 0xF0) {
        // 4-byte sequence: 11110xxx
        this.utf8ExpectedLength = 4;
      } else {
        // Invalid UTF-8 start byte, treat as single byte
        this.handlers.handleNormalByte(byte);
        return true;
      }
      
      this.utf8Buffer = [byte];
      return true;
    }

    // Continuation byte in UTF-8 sequence
    if ((byte & 0xC0) !== 0x80) {
      // Invalid continuation byte, flush buffer and start over
      this.flushUtf8Buffer();
      return this.handleUtf8Byte(byte); // Retry with this byte
    }

    this.utf8Buffer.push(byte);

    // Check if we have a complete UTF-8 sequence
    if (this.utf8Buffer.length === this.utf8ExpectedLength) {
      this.decodeUtf8Sequence();
    }

    return true;
  }

  private decodeUtf8Sequence(): void {
    try {
      const utf8Array = new Uint8Array(this.utf8Buffer);
      const decoded = new TextDecoder('utf-8', { fatal: true }).decode(utf8Array);
      
      // Send each Unicode character as its code point
      for (let i = 0; i < decoded.length; i++) {
        const codePoint = decoded.codePointAt(i);
        if (codePoint !== undefined) {
          this.handlers.handleNormalByte(codePoint);
          // Skip the next character if this was a surrogate pair
          if (codePoint > 0xFFFF) {
            i++;
          }
        }
      }
    } catch (error) {
      // Decoding failed, treat each byte as a separate character
      this.flushUtf8Buffer();
    }

    // Reset UTF-8 state
    this.utf8Buffer = [];
    this.utf8ExpectedLength = 0;
  }

  private flushUtf8Buffer(): void {
    // Send each byte in the buffer as a separate character (fallback)
    for (const b of this.utf8Buffer) {
      this.handlers.handleNormalByte(b);
    }
    this.utf8Buffer = [];
    this.utf8ExpectedLength = 0;
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

    const msg: OscMessage = { _type: "osc", raw, terminator, implemented: false };
    
    // Try to parse as xterm OSC extension
    const xtermMsg = parseOsc(msg);
    if (xtermMsg) {
      this.log.debug(`OSC (xterm, ${terminator}): ${raw}`);
      this.handlers.handleXtermOsc(xtermMsg);
    } else {
      // Fall back to generic OSC handling
      this.log.debug(`OSC (opaque, ${terminator}): ${raw}`);
      this.handlers.handleOsc(msg);
    }

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
      const sgr: SgrSequence = { _type: "sgr", implemented: false, raw, messages: sgrMessages };
      this.handlers.handleSgr(sgr);
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