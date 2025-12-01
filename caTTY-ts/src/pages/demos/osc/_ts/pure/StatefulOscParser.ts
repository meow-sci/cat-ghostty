import type { GhosttyVtInstance } from "../../../../../ts/ghostty-vt";

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
      throw new Error(`ghostty_sgr_new failed with result ${result}`);
    }

    const ptr = new DataView(this.getBuffer()).getUint32(ptrPtr, true);
    return ptr;
  }

  getBuffer() {
    return this.wasm.exports.memory.buffer;
  }
}