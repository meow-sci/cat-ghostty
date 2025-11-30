import type { GhosttyVtInstance } from "../../ghostty-vt";


export async function loadWasm(): Promise<GhosttyVtInstance> {
  try {
    // Load the wasm module - adjust path as needed
    const response = await fetch('/caTTY/ghostty-vt.wasm');
    const wasmBytes = await response.arrayBuffer();

    // Instantiate the wasm module
    const wasmModule = await WebAssembly.instantiate(wasmBytes, {
      env: {
        // Logging function for wasm module
        log: (ptr: number, len: number) => {
          const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any
          const bytes = new Uint8Array(wasmInstance.exports.memory.buffer, ptr, len);
          const text = new TextDecoder().decode(bytes);
          console.log('[wasm]', text);
        }
      }
    });

    const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
    return wasmInstance;

  } catch (e) {
    console.error('Failed to load WASM:', e);
    if (window.location.protocol === 'file:') {
      throw new Error('Cannot load WASM from file:// protocol. Please serve via HTTP (see README)');
    }
    throw e;
  }
}
