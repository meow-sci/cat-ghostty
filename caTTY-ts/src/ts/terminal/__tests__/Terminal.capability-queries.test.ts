/**
 * Tests for terminal capability query responses.
 * These tests verify that the terminal correctly responds to capability queries
 * from applications like viu, htop, etc.
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import { Terminal, type TerminalConfig } from '../Terminal.js';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';

describe('Terminal Capability Query Responses', () => {
  let wasmInstance: GhosttyVtInstance;
  
  beforeEach(async () => {
    // Load WASM instance for tests
    const wasmPath = join(__dirname, '../../../../public/ghostty-vt.wasm');
    const wasmBytes = await readFile(wasmPath);
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

  describe('Device Status Report (DSR)', () => {
    it('should respond to status query (DSR 5) with OK status', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send status query: CSI 5 n
      terminal.write('\x1B[5n');
      
      // Should respond with CSI 0 n (terminal OK)
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B[0n');
      
      terminal.dispose();
    });

    it('should respond to cursor position query (DSR 6) with current position', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Move cursor to a specific position
      terminal.write('\x1B[10;20H'); // Move to row 10, col 20 (1-based)
      
      // Send cursor position query: CSI 6 n
      terminal.write('\x1B[6n');
      
      // Should respond with CSI 10;20 R (1-based coordinates)
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B[10;20R');
      
      terminal.dispose();
    });

    it('should report cursor position at origin correctly', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Cursor should be at origin (0,0 internally, 1,1 in report)
      terminal.write('\x1B[6n');
      
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B[1;1R');
      
      terminal.dispose();
    });
  });

  describe('Primary Device Attributes (DA1)', () => {
    it('should respond to primary DA query with terminal capabilities', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send primary DA query: CSI c
      terminal.write('\x1B[c');
      
      // Should respond with CSI ? ... c format
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toMatch(/^\x1B\[\?[\d;]+c$/);
      expect(response).toBe('\x1B[?1;2;6;22c');
      
      terminal.dispose();
    });
  });

  describe('Secondary Device Attributes (DA2)', () => {
    it('should respond to secondary DA query with terminal type and version', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send secondary DA query: CSI > c
      terminal.write('\x1B[>c');
      
      // Should respond with CSI > Pp;Pv;Pc c format
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toMatch(/^\x1B\[>[\d;]+c$/);
      expect(response).toBe('\x1B[>41;0;0c');
      
      terminal.dispose();
    });
  });

  describe('Kitty Graphics Protocol Query', () => {
    it('should respond to graphics capability query with OK', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send Kitty graphics query: ESC_G a=q ESC\
      terminal.write('\x1B_Ga=q\x1B\\');
      
      // Should respond with ESC_G OK ESC\
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B_GOK\x1B\\');
      
      terminal.dispose();
    });

    it('should echo back image ID in graphics query response', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send Kitty graphics query with image ID: ESC_G i=31,a=q ESC\
      terminal.write('\x1B_Gi=31,a=q\x1B\\');
      
      // Should respond with ESC_G i=31;OK ESC\
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B_Gi=31;OK\x1B\\');
      
      terminal.dispose();
    });

    it('should respond to viu-style graphics query', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      let capturedOutput: Uint8Array | undefined;
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          capturedOutput = data;
        }
      }, wasmInstance);
      
      // Send viu-style query: ESC_G i=31,s=1,v=1,a=q,t=d,f=24;AAAA ESC\
      terminal.write('\x1B_Gi=31,s=1,v=1,a=q,t=d,f=24;AAAA\x1B\\');
      
      // Should respond with ESC_G i=31;OK ESC\
      expect(capturedOutput).toBeDefined();
      const response = new TextDecoder().decode(capturedOutput);
      expect(response).toBe('\x1B_Gi=31;OK\x1B\\');
      
      terminal.dispose();
    });
  });

  describe('Multiple queries in sequence', () => {
    it('should respond to multiple capability queries correctly', () => {
      const config: TerminalConfig = { cols: 80, rows: 24, scrollback: 1000 };
      const responses: string[] = [];
      
      const terminal = new Terminal(config, {
        onDataOutput: (data) => {
          const response = new TextDecoder().decode(data);
          responses.push(response);
        }
      }, wasmInstance);
      
      // Send multiple queries like viu does
      terminal.write('\x1B_Gi=31,a=q\x1B\\'); // Kitty graphics query
      terminal.write('\x1B[c'); // Primary DA
      
      // Should have received two responses
      expect(responses.length).toBe(2);
      expect(responses[0]).toBe('\x1B_Gi=31;OK\x1B\\');
      expect(responses[1]).toBe('\x1B[?1;2;6;22c');
      
      terminal.dispose();
    });
  });
});
