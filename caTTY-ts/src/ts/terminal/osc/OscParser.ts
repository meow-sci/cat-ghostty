/**
 * OSC (Operating System Command) parser wrapper for libghostty-vt.
 * Provides a TypeScript interface for parsing OSC sequences using the WASM library.
 */

import type { GhosttyVtInstance } from '../../ghostty-vt.js';

/**
 * OSC command types supported by the parser.
 * These correspond to the GhosttyOscCommandType enum in the C API.
 */
export enum OscCommandType {
  Invalid = 0,
  ChangeWindowTitle = 1,
  ChangeWindowIcon = 2,
  PromptStart = 3,
  PromptEnd = 4,
  EndOfInput = 5,
  EndOfCommand = 6,
  ClipboardContents = 7,
  ReportPwd = 8,
  MouseShape = 9,
  ColorOperation = 10,
  KittyColorProtocol = 11,
  ShowDesktopNotification = 12,
  HyperlinkStart = 13,
  HyperlinkEnd = 14,
  ConemuSleep = 15,
  ConemuShowMessageBox = 16,
  ConemuChangeTabTitle = 17,
  ConemuProgressReport = 18,
  ConemuWaitInput = 19,
  ConemuGuimacro = 20,
}

/**
 * OSC command data types for extracting specific data from commands.
 * These correspond to the GhosttyOscCommandData enum in the C API.
 */
export enum OscCommandData {
  Invalid = 0,
  ChangeWindowTitleStr = 1,
}

/**
 * Parsed OSC command result.
 */
export interface OscCommand {
  /** The type of OSC command */
  type: OscCommandType;
  /** Raw command data (if available) */
  data?: string;
  /** Parsed title data (for title commands) */
  title?: string;
  /** Parsed hyperlink data (for hyperlink commands) */
  hyperlink?: {
    url: string;
    id?: string;
  };
  /** Parsed clipboard data (for clipboard commands) */
  clipboard?: string;
}

/**
 * Events that can be emitted by the OSC parser.
 */
export interface OscEvents {
  /** Emitted when a title change command is parsed (OSC 0/2) */
  onTitleChange?: (title: string) => void;
  /** Emitted when a hyperlink command is parsed (OSC 8) */
  onHyperlink?: (url: string, id?: string) => void;
  /** Emitted when a clipboard command is parsed (OSC 52) */
  onClipboard?: (content: string) => void;
  /** Emitted for any parsed OSC command */
  onCommand?: (command: OscCommand) => void;
}

/**
 * OSC parser wrapper for libghostty-vt.
 * Provides a high-level TypeScript interface for parsing OSC sequences.
 */
export class OscParser {
  private wasmInstance: GhosttyVtInstance;
  private parserPtr: number;
  private events: OscEvents;

  /**
   * Create a new OSC parser.
   * @param wasmInstance The WASM instance containing libghostty-vt
   * @param events Event handlers for parsed commands
   */
  constructor(wasmInstance: GhosttyVtInstance, events: OscEvents = {}) {
    this.wasmInstance = wasmInstance;
    this.events = events;
    this.parserPtr = this.initParser();
  }

  /**
   * Initialize the WASM parser instance.
   * @returns Pointer to the parser instance
   */
  private initParser(): number {
    const ptrPtr = this.wasmInstance.exports.ghostty_wasm_alloc_opaque();
    const result = this.wasmInstance.exports.ghostty_osc_new(0, ptrPtr);

    if (result !== 0) {
      this.wasmInstance.exports.ghostty_wasm_free_opaque(ptrPtr);
      throw new Error(`ghostty_osc_new failed with result ${result}`);
    }

    const ptr = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(ptrPtr, true);
    this.wasmInstance.exports.ghostty_wasm_free_opaque(ptrPtr);
    return ptr;
  }

  /**
   * Parse an OSC sequence and return the parsed command.
   * @param sequence The OSC sequence data (without ESC] prefix or terminator)
   * @param terminator The terminating byte (0x07 for BEL, 0x5C for ST)
   * @returns The parsed OSC command
   */
  parse(sequence: string, terminator: number = 0x07): OscCommand {
    // Reset parser state
    this.wasmInstance.exports.ghostty_osc_reset(this.parserPtr);

    // Feed bytes to the parser
    const encoder = new TextEncoder();
    const bytes = encoder.encode(sequence);
    for (let i = 0; i < bytes.length; i++) {
      this.wasmInstance.exports.ghostty_osc_next(this.parserPtr, bytes[i]);
    }

    // Finalize parsing and get command
    const cmdPtr = this.wasmInstance.exports.ghostty_osc_end(this.parserPtr, terminator);
    if (!cmdPtr) {
      return { type: OscCommandType.Invalid };
    }

    const type = this.wasmInstance.exports.ghostty_osc_command_type(cmdPtr) as OscCommandType;
    const command: OscCommand = { type };

    // Extract data based on command type
    switch (type) {
      case OscCommandType.ChangeWindowTitle:
      case OscCommandType.ChangeWindowIcon:
        command.title = this.extractTitleData(cmdPtr);
        if (command.title && this.events.onTitleChange) {
          this.events.onTitleChange(command.title);
        }
        break;

      case OscCommandType.HyperlinkStart:
      case OscCommandType.HyperlinkEnd:
        // For hyperlink commands, we need to parse the sequence manually
        // since libghostty-vt doesn't provide specific extractors for hyperlink data
        command.hyperlink = this.parseHyperlinkData(sequence);
        if (command.hyperlink && this.events.onHyperlink) {
          this.events.onHyperlink(command.hyperlink.url, command.hyperlink.id);
        }
        break;

      case OscCommandType.ClipboardContents:
        // For clipboard commands, parse the sequence manually
        command.clipboard = this.parseClipboardData(sequence);
        if (command.clipboard && this.events.onClipboard) {
          this.events.onClipboard(command.clipboard);
        }
        break;

      default:
        // For other commands, store the raw data
        command.data = sequence;
        break;
    }

    // Emit general command event
    if (this.events.onCommand) {
      this.events.onCommand(command);
    }

    return command;
  }

  /**
   * Extract title data from a parsed command.
   * @param cmdPtr Pointer to the parsed command
   * @returns The extracted title string, or undefined if extraction failed
   */
  private extractTitleData(cmdPtr: number): string | undefined {
    const outSlot = this.wasmInstance.exports.ghostty_wasm_alloc_opaque();
    try {
      const success = this.wasmInstance.exports.ghostty_osc_command_data(
        cmdPtr,
        OscCommandData.ChangeWindowTitleStr,
        outSlot
      );

      if (success) {
        const cstrPtr = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(outSlot, true);
        return cstrPtr ? this.readCString(cstrPtr) : undefined;
      }
    } finally {
      this.wasmInstance.exports.ghostty_wasm_free_opaque(outSlot);
    }
    return undefined;
  }

  /**
   * Parse hyperlink data from an OSC 8 sequence.
   * OSC 8 format: 8;params;URI where params can contain id=value
   * @param sequence The OSC sequence data
   * @returns Parsed hyperlink data
   */
  private parseHyperlinkData(sequence: string): { url: string; id?: string } | undefined {
    // OSC 8 format: 8;params;URI
    const parts = sequence.split(';');
    if (parts.length < 3 || parts[0] !== '8') {
      return undefined;
    }

    const params = parts[1];
    const url = parts.slice(2).join(';'); // URI might contain semicolons

    let id: string | undefined;
    if (params) {
      // Parse id parameter from params (format: id=value)
      const idMatch = params.match(/id=([^:]+)/);
      if (idMatch) {
        id = idMatch[1];
      }
    }

    return { url, id };
  }

  /**
   * Parse clipboard data from an OSC 52 sequence.
   * OSC 52 format: 52;c;base64data
   * @param sequence The OSC sequence data
   * @returns Parsed clipboard content
   */
  private parseClipboardData(sequence: string): string | undefined {
    // OSC 52 format: 52;c;base64data
    const parts = sequence.split(';');
    if (parts.length < 3 || parts[0] !== '52') {
      return undefined;
    }

    const clipboard = parts[1];
    const data = parts[2];

    // Only handle 'c' (clipboard) for now
    if (clipboard !== 'c') {
      return undefined;
    }

    try {
      // Decode base64 data
      return atob(data);
    } catch (e) {
      // Invalid base64 data
      return undefined;
    }
  }

  /**
   * Read a null-terminated C string from WASM memory.
   * @param ptr Pointer to the string in WASM memory
   * @returns The decoded string
   */
  private readCString(ptr: number): string {
    const buffer = this.wasmInstance.exports.memory.buffer;
    const view = new DataView(buffer);
    let length = 0;
    
    // Find string length
    while (view.getUint8(ptr + length) !== 0) {
      length++;
    }
    
    // Read string bytes
    const bytes = new Uint8Array(buffer, ptr, length);
    return new TextDecoder('utf-8').decode(bytes);
  }

  /**
   * Reset the parser to its initial state.
   */
  reset(): void {
    this.wasmInstance.exports.ghostty_osc_reset(this.parserPtr);
  }

  /**
   * Free the parser resources.
   * Call this when the parser is no longer needed.
   */
  dispose(): void {
    if (this.parserPtr) {
      this.wasmInstance.exports.ghostty_osc_free(this.parserPtr);
      this.parserPtr = 0;
    }
  }
}