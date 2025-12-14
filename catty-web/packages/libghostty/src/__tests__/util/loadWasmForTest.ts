import { readFile } from 'node:fs/promises';
import { join } from 'node:path';

import { GhosttyVtInstance } from '../../ghostty-vt';


export async function loadWasmForTest(): Promise<GhosttyVtInstance> {

  // Load WASM file from filesystem
  const wasmPath = join(__dirname, '../../../public/ghostty-vt.wasm');
  const wasmBytes = await readFile(wasmPath);

  // Instantiate the WASM module
  const wasmModule = await WebAssembly.instantiate(wasmBytes, {
    env: {
      log: (ptr: number, len: number) => {
        const instance: GhosttyVtInstance = wasmModule.instance as unknown as any;
        const bytes = new Uint8Array(instance.exports.memory.buffer, ptr, len);
        const text = new TextDecoder().decode(bytes);
        console.log('[wasm]', text);
      }
    }
  });

  const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
  return wasmInstance;
};
