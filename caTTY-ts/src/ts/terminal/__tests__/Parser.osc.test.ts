/**
 * Integration tests for OSC parsing in the main Parser class.
 * These tests verify that OSC sequences are correctly parsed and events are emitted.
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { Parser } from '../Parser.js';

describe('Parser OSC Integration', () => {
    let wasmInstance: GhosttyVtInstance;

    beforeAll(async () => {
        // Load WASM file from filesystem
        const wasmPath = join(__dirname, '../../../../public/ghostty-vt.wasm');
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

        wasmInstance = wasmModule.instance as unknown as any;
    });

    it('should parse OSC 0 sequence and emit title change event', () => {
        let emittedTitle: string | undefined;
        
        const parser = new Parser({
            onTitleChange: (title) => {
                emittedTitle = title;
            }
        }, wasmInstance);
        
        // Send OSC 0 sequence: ESC ] 0 ; title BEL
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('0;Test Window Title'),
            0x07 // BEL
        ]);
        
        parser.parse(sequence);
        
        expect(emittedTitle).toBe('Test Window Title');
        
        parser.dispose();
    });

    it('should parse OSC 8 sequence and emit hyperlink event', () => {
        let emittedUrl: string | undefined;
        let emittedId: string | undefined;
        
        const parser = new Parser({
            onHyperlink: (url, id) => {
                emittedUrl = url;
                emittedId = id;
            }
        }, wasmInstance);
        
        // Send OSC 8 sequence: ESC ] 8 ; id=link123 ; https://example.com BEL
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('8;id=link123;https://example.com'),
            0x07 // BEL
        ]);
        
        parser.parse(sequence);
        
        expect(emittedUrl).toBe('https://example.com');
        expect(emittedId).toBe('link123');
        
        parser.dispose();
    });

    it('should parse OSC 52 sequence and emit clipboard event', () => {
        let emittedContent: string | undefined;
        
        const parser = new Parser({
            onClipboard: (content) => {
                emittedContent = content;
            }
        }, wasmInstance);
        
        const testText = 'Hello Clipboard';
        const base64Text = btoa(testText);
        
        // Send OSC 52 sequence: ESC ] 52 ; c ; base64data BEL
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode(`52;c;${base64Text}`),
            0x07 // BEL
        ]);
        
        parser.parse(sequence);
        
        expect(emittedContent).toBe(testText);
        
        parser.dispose();
    });

    it('should handle OSC sequence terminated with ESC \\', () => {
        let emittedTitle: string | undefined;
        
        const parser = new Parser({
            onTitleChange: (title) => {
                emittedTitle = title;
            }
        }, wasmInstance);
        
        // Send OSC 0 sequence: ESC ] 0 ; title ESC \
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('0;ST Terminated Title'),
            0x1B // ESC (ST terminator will be handled by escape state)
        ]);
        
        parser.parse(sequence);
        
        expect(emittedTitle).toBe('ST Terminated Title');
        
        parser.dispose();
    });

    it('should call both new OSC events and legacy onOsc handler', () => {
        let emittedTitle: string | undefined;
        let legacyCommand: number | undefined;
        let legacyData: string | undefined;
        
        const parser = new Parser({
            onTitleChange: (title) => {
                emittedTitle = title;
            },
            onOsc: (command, data) => {
                legacyCommand = command;
                legacyData = data;
            }
        }, wasmInstance);
        
        // Send OSC 0 sequence
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('0;Legacy Test'),
            0x07 // BEL
        ]);
        
        parser.parse(sequence);
        
        // Both new and legacy handlers should be called
        expect(emittedTitle).toBe('Legacy Test');
        expect(legacyCommand).toBe(0);
        expect(legacyData).toBe('Legacy Test');
        
        parser.dispose();
    });

    it('should handle malformed OSC sequences gracefully', () => {
        const parser = new Parser({}, wasmInstance);
        
        // Send malformed OSC sequence
        const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('invalid;malformed;sequence'),
            0x07 // BEL
        ]);
        
        // Should not throw an error
        expect(() => parser.parse(sequence)).not.toThrow();
        
        parser.dispose();
    });
});