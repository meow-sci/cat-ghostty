/**
 * Unit tests for WASM loading.
 * These tests verify that the ghostty-vt WASM module can be loaded and instantiated.
 */

import { describe, it, expect } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';

describe('LoadWasm', () => {
    it('should load and instantiate WASM module from filesystem', async () => {
        // Load WASM file from filesystem (relative to test file location)
        const wasmPath = join(__dirname, '../../../../public/ghostty-vt.wasm');
        const wasmBytes = await readFile(wasmPath);
        
        expect(wasmBytes).toBeInstanceOf(Buffer);
        expect(wasmBytes.length).toBeGreaterThan(0);

        // Instantiate the WASM module
        const wasmModule = await WebAssembly.instantiate(wasmBytes, {
            env: {
                log: (ptr: number, len: number) => {
                    const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
                    const bytes = new Uint8Array(wasmInstance.exports.memory.buffer, ptr, len);
                    const text = new TextDecoder().decode(bytes);
                    console.log('[wasm]', text);
                }
            }
        });

        const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
        
        // Verify the instance has the expected exports
        expect(wasmInstance.exports).toBeDefined();
        expect(wasmInstance.exports.memory).toBeInstanceOf(WebAssembly.Memory);
        
        // Verify some key functions exist
        expect(typeof wasmInstance.exports.ghostty_key_encoder_new).toBe('function');
        expect(typeof wasmInstance.exports.ghostty_sgr_new).toBe('function');
        expect(typeof wasmInstance.exports.ghostty_osc_new).toBe('function');
        expect(typeof wasmInstance.exports.ghostty_wasm_alloc_u8_array).toBe('function');
    });
});