import type { GhosttyVtInstance } from "../ghostty-vt";


export async function loadWasmFromURL(url: string): Promise<GhosttyVtInstance> {
  try {
    // Load the wasm module - adjust path as needed
    const response = await fetch(url);
    const wasmBytes = await response.arrayBuffer();

    // Instantiate the wasm module
    const wasmModule = await WebAssembly.instantiate(wasmBytes, {
      env: {
        // Logging function for wasm module
        log: (ptr: number, len: number) => {
          const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any
          const bytes = new Uint8Array(wasmInstance.exports.memory.buffer, ptr, len);
          const text = new TextDecoder().decode(bytes);
          console.log(`[wasm]: ${text}`);
        }
      }
    });

    const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
    return wasmInstance;

  } catch (e) {
    console.error('Failed to load WASM:', e);
    throw e;
  }
}
