import type { GhosttyVtInstance } from "../../../../../ts/ghostty-vt";
import { OscTypeNames } from "../../../../../ts/terminal/osc/OscTypeNames";

export class StatefulOscParser {

  private wasm: GhosttyVtInstance;
  private parserPtr: number;

  constructor(wasm: GhosttyVtInstance) {
    this.wasm = wasm;
    this.parserPtr = this.initParser();
  }

  initParser(): number {

    // Create key encoder
    const ptrPtr = this.wasm.exports.ghostty_wasm_alloc_opaque();
    const result = this.wasm.exports.ghostty_osc_new(0, ptrPtr);

    if (result !== 0) {
      throw new Error(`ghostty_osc_new failed with result ${result}`);
    }

    const ptr = new DataView(this.getBuffer()).getUint32(ptrPtr, true);
    return ptr;
  }

  parse(sequence: string, terminator: number = 0x07): string {
    const exp = this.wasm.exports;
    const dv = new DataView(this.getBuffer());

    // Reset parser state
    exp.ghostty_osc_reset(this.parserPtr);

    // Feed bytes (UTF-8) for the OSC body (e.g. "0;New Title")
    const encoder = new TextEncoder();
    const bytes = encoder.encode(sequence);
    for (let i = 0; i < bytes.length; i++) {
      exp.ghostty_osc_next(this.parserPtr, bytes[i]);
    }

    // Finalize and get command
    const cmdPtr = exp.ghostty_osc_end(this.parserPtr, terminator);
    if (!cmdPtr) {
      return [
        `Type: INVALID (0)`,
        `Note: No command parsed (check input and terminator).`
      ].join("\n");
    }

    const type = exp.ghostty_osc_command_type(cmdPtr);
    const typeName = OscTypeNames[type] ?? `UNKNOWN(${type})`;

    // Try known data extractions similar to C#/C examples
    const outSlot = exp.ghostty_wasm_alloc_opaque();
    let lines: string[] = [];
    try {
      lines.push(`Type: ${typeName} (${type})`);

      // CHANGE_WINDOW_TITLE (1)
      if (type === 1 /* GHOSTTY_OSC_COMMAND_CHANGE_WINDOW_TITLE */) {
        if (exp.ghostty_osc_command_data(cmdPtr, 1 /* GHOSTTY_OSC_DATA_CHANGE_WINDOW_TITLE_STR */, outSlot)) {
          const cstrPtr = new DataView(this.getBuffer()).getUint32(outSlot, true);
          const title = cstrPtr ? this.readCString(cstrPtr) : "";
          lines.push(`Title: ${title}`);
        } else {
          lines.push(`Data: <unavailable>`);
        }
      }
      // REPORT_PWD (8)
      else if (type === 8 /* GHOSTTY_OSC_COMMAND_REPORT_PWD */) {
        // Attempt extraction if supported: GHOSTTY_OSC_DATA_REPORT_PWD_STR = 3
        if (exp.ghostty_osc_command_data(cmdPtr, 3, outSlot)) {
          const cstrPtr = dv.getUint32(outSlot, true);
          const pwd = cstrPtr ? this.readCString(cstrPtr) : "";
          lines.push(`PWD: ${pwd}`);
        } else {
          lines.push(`PWD: <unavailable>`);
        }
      }
      // CLIPBOARD_CONTENTS (7)
      else if (type === 7 /* GHOSTTY_OSC_COMMAND_CLIPBOARD_CONTENTS */) {
        // Attempt extraction if supported: GHOSTTY_OSC_DATA_CLIPBOARD_STR = 4
        if (exp.ghostty_osc_command_data(cmdPtr, 4, outSlot)) {
          const cstrPtr = dv.getUint32(outSlot, true);
          const text = cstrPtr ? this.readCString(cstrPtr) : "";
          lines.push(`Clipboard: ${text}`);
        } else {
          lines.push(`Clipboard: <unavailable>`);
        }
      }
      // COLOR_OPERATION (10)
      else if (type === 10 /* GHOSTTY_OSC_COMMAND_COLOR_OPERATION */) {
        lines.push(`Color operation: parsed (details not exposed via command_data)`);
      }
      else if (type === 0 /* INVALID */) {
        lines.push(`Note: Invalid command`);
      }
      else {
        lines.push(`Data: (no extractor implemented)`);
      }
    } finally {
      // Free our temporary out pointer slot
      exp.ghostty_wasm_free_opaque(outSlot);
    }

    // Include echo of input for clarity
    lines.push(`Input: ${sequence}`);

    return lines.join("\n");
  }

  getBuffer() {
    return this.wasm.exports.memory.buffer;
  }

  private readCString(ptr: number): string {
    const buf = this.getBuffer();
    const dv = new DataView(buf);
    let len = 0;
    while (dv.getUint8(ptr + len) !== 0) len++;
    const bytes = new Uint8Array(buf, ptr, len);
    return new TextDecoder("utf-8").decode(bytes);
  }
}
