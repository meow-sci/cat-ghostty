/**
 * Unit tests for OSC parser.
 * These tests verify that the OSC parser correctly integrates with libghostty-vt.
 */

import { describe, it, expect, beforeAll } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { OscParser, OscCommandType } from '../osc/OscParser.js';

describe('OscParser', () => {
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

    it('should parse OSC 0 (window title) command', () => {
        const parser = new OscParser(wasmInstance);
        
        const result = parser.parse('0;Test Title');
        
        expect(result.type).toBe(OscCommandType.ChangeWindowTitle);
        expect(result.title).toBe('Test Title');
        
        parser.dispose();
    });

    it('should parse OSC 2 (window title) command', () => {
        const parser = new OscParser(wasmInstance);
        
        const result = parser.parse('2;Another Title');
        
        expect(result.type).toBe(OscCommandType.ChangeWindowTitle);
        expect(result.title).toBe('Another Title');
        
        parser.dispose();
    });

    it('should parse OSC 8 (hyperlink) command', () => {
        const parser = new OscParser(wasmInstance);
        
        const result = parser.parse('8;id=test123;https://example.com');
        
        expect(result.type).toBe(OscCommandType.HyperlinkStart);
        expect(result.hyperlink?.url).toBe('https://example.com');
        expect(result.hyperlink?.id).toBe('test123');
        
        parser.dispose();
    });

    it('should parse OSC 8 (hyperlink) command without id', () => {
        const parser = new OscParser(wasmInstance);
        
        const result = parser.parse('8;;https://example.com');
        
        expect(result.type).toBe(OscCommandType.HyperlinkStart);
        expect(result.hyperlink?.url).toBe('https://example.com');
        expect(result.hyperlink?.id).toBeUndefined();
        
        parser.dispose();
    });

    it('should parse OSC 52 (clipboard) command', () => {
        const parser = new OscParser(wasmInstance);
        const testText = 'Hello World';
        const base64Text = btoa(testText);
        
        const result = parser.parse(`52;c;${base64Text}`);
        
        expect(result.type).toBe(OscCommandType.ClipboardContents);
        expect(result.clipboard).toBe(testText);
        
        parser.dispose();
    });

    it('should handle invalid OSC commands', () => {
        const parser = new OscParser(wasmInstance);
        
        const result = parser.parse('999;invalid');
        
        expect(result.type).toBe(OscCommandType.Invalid);
        
        parser.dispose();
    });

    it('should emit title change events', () => {
        let emittedTitle: string | undefined;
        
        const parser = new OscParser(wasmInstance, {
            onTitleChange: (title) => {
                emittedTitle = title;
            }
        });
        
        parser.parse('0;Event Test Title');
        
        expect(emittedTitle).toBe('Event Test Title');
        
        parser.dispose();
    });

    it('should emit hyperlink events', () => {
        let emittedUrl: string | undefined;
        let emittedId: string | undefined;
        
        const parser = new OscParser(wasmInstance, {
            onHyperlink: (url, id) => {
                emittedUrl = url;
                emittedId = id;
            }
        });
        
        parser.parse('8;id=link123;https://test.com');
        
        expect(emittedUrl).toBe('https://test.com');
        expect(emittedId).toBe('link123');
        
        parser.dispose();
    });

    it('should emit clipboard events', () => {
        let emittedContent: string | undefined;
        
        const parser = new OscParser(wasmInstance, {
            onClipboard: (content) => {
                emittedContent = content;
            }
        });
        
        const testText = 'Clipboard Test';
        const base64Text = btoa(testText);
        parser.parse(`52;c;${base64Text}`);
        
        expect(emittedContent).toBe(testText);
        
        parser.dispose();
    });

    it('should reset parser state', () => {
        const parser = new OscParser(wasmInstance);
        
        // Parse a command
        const result1 = parser.parse('0;First Title');
        expect(result1.title).toBe('First Title');
        
        // Reset and parse another
        parser.reset();
        const result2 = parser.parse('0;Second Title');
        expect(result2.title).toBe('Second Title');
        
        parser.dispose();
    });
});