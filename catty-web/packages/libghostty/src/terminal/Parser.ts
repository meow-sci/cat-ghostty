import { GhosttyVtInstance } from "../ghostty-vt";
import { HandleBell, HandleBackspace, HandleTab, HandleLineFeed, HandleFormFeed, HandleCarriageReturn, HandleNormalByte } from "./ParserHandlers";

import { getLogger } from "@catty/log";


type State = "normal" | "esc" | "csi" | "osc";

interface ParserOptions {

  wasm: GhosttyVtInstance;

  log: ReturnType<typeof getLogger>;

  /** Whether to emit normal bytes during escape sequences (undefined behavior), default=false */
  emitNormalBytesDuringEscapeSequence?: boolean;

  /** Whether to process C0 controls during escape sequences (common terminal behavior), default=true */
  processC0ControlsDuringEscapeSequence?: boolean;

  handlers: {
    handleBell: HandleBell;
    handleBackspace: HandleBackspace;
    handleTab: HandleTab;
    handleLineFeed: HandleLineFeed;
    handleFormFeed: HandleFormFeed;
    handleCarriageReturn: HandleCarriageReturn;
    handleNormalByte: HandleNormalByte;
  };

}

export class Parser {

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

      case "osc": {

        // Optional: still execute C0 controls while parsing CSI (common terminal behavior).
        if (byte < 0x20 && byte !== 0x1b && this.processC0ControlsDuringEscapeSequence && this.handleC0ExceptEscape(byte)) {
          return;
        }


        // Important: OSC can be terminated by BEL, so don't globally treat BEL as "ring bell".
        return this.handleOscByte(byte);
      }

      case "csi": {
        // Optional: still execute C0 controls while parsing CSI (common terminal behavior).
        if (byte < 0x20 && byte !== 0x1b && this.processC0ControlsDuringEscapeSequence && this.handleC0ExceptEscape(byte)) {
          return;
        }

        if (byte === 0x1b) {
          return this.startEscapeSequence(byte);
        }

        return this.handleCsiByte(byte);
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
    if (byte < 0x20 || byte > 0x7e) {
      this.log.warn(`OSC: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    this.escapeSequence.push(byte);
    return;
  }

  private handleCsiByte(byte: number): void {

    // guard against bytes outside the allowed CSI byte range (0x20 - 0x7E)
    if (byte < 0x20 || byte > 0x7e) {
      this.log.warn(`CSI: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    this.escapeSequence.push(byte);

    return;
  }

  // handle ESC byte, accumulate into escapeSequence
  private handleEscapeByte(byte: number): void {

    // guard against bytes outside the allowed ESC byte range (0x20 - 0x7E)
    if (byte < 0x20 || byte > 0x7e) {
      this.log.warn(`ESC: byte out of range 0x${byte.toString(16)}`);
      return this.maybeEmitNormalByteDuringEscapeSequence(byte);
    }

    this.escapeSequence.push(byte);
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

}
