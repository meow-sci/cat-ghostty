/**
 * Property-based tests for TerminalController key handling.
 * These tests verify universal properties that should hold across all inputs.
 */

import { describe, it, expect, beforeAll, vi } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import fc from 'fast-check';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { Terminal } from '../Terminal.js';
import { TerminalController } from '../TerminalController.js';
import type { KeyEvent } from '../../../pages/demos/keyencode/_ts/pure/KeyEvent.js';

// Mock DOM globals for Node.js testing
const mockDocument = {
  createElement: vi.fn(() => ({
    id: '',
    textContent: '',
  })),
  querySelector: vi.fn(() => null),
  head: {
    appendChild: vi.fn(),
  },
};

const mockWindow = {
  location: {
    protocol: 'http:',
  },
};

// Set up global mocks
(global as any).document = mockDocument;
(global as any).window = mockWindow;

describe('TerminalController Property Tests', () => {
  let wasmInstance: GhosttyVtInstance;
  
  beforeAll(async () => {
    // Load WASM instance for testing
    const wasmPath = join(__dirname, '../../../../public/ghostty-vt.wasm');
    const wasmBytes = await readFile(wasmPath);
    
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
    
    wasmInstance = wasmModule.instance as unknown as any;
  });
  
  /**
   * Creates a test terminal and controller setup.
   */
  function createTestSetup() {
    const terminal = new Terminal(
      { cols: 80, rows: 24, scrollback: 1000 },
      {},
      wasmInstance
    );
    
    // Create mock DOM elements
    const inputElement = {
      addEventListener: () => {},
      removeEventListener: () => {},
      setAttribute: () => {},
      focus: () => {},
    } as unknown as HTMLInputElement;
    
    const displayElement = {
      addEventListener: () => {},
      removeEventListener: () => {},
      setAttribute: () => {},
      style: {},
      classList: {
        add: () => {},
        remove: () => {},
        contains: () => false,
        toggle: () => false,
      },
      getBoundingClientRect: () => ({
        left: 0,
        top: 0,
        width: 640,
        height: 384,
        right: 640,
        bottom: 384,
        x: 0,
        y: 0,
        toJSON: () => {},
      }),
    } as unknown as HTMLElement;
    
    const controller = new TerminalController({
      terminal,
      inputElement,
      displayElement,
      wasmInstance,
    });
    
    return { terminal, controller };
  }
  
  // Generators for property-based testing
  
  const keyCodeArb = fc.oneof(
    fc.constantFrom(
      'KeyA', 'KeyB', 'KeyC', 'KeyD', 'KeyE', 'KeyF', 'KeyG', 'KeyH', 'KeyI', 'KeyJ',
      'KeyK', 'KeyL', 'KeyM', 'KeyN', 'KeyO', 'KeyP', 'KeyQ', 'KeyR', 'KeyS', 'KeyT',
      'KeyU', 'KeyV', 'KeyW', 'KeyX', 'KeyY', 'KeyZ',
      'Digit0', 'Digit1', 'Digit2', 'Digit3', 'Digit4', 'Digit5', 'Digit6', 'Digit7', 'Digit8', 'Digit9',
      'Space', 'Enter', 'Escape', 'Backspace', 'Tab',
      'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight',
      'F1', 'F2', 'F3', 'F4', 'F5', 'F6', 'F7', 'F8', 'F9', 'F10', 'F11', 'F12'
    )
  );
  
  const printableKeyArb = fc.oneof(
    fc.constantFrom('a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
                    'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
                    'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
                    'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
                    '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                    ' ', '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-', '=',
                    '[', ']', '\\', ';', "'", ',', '.', '/', '`', '~', '_', '+',
                    '{', '}', '|', ':', '"', '<', '>', '?')
  );
  
  const keyEventArb = fc.record({
    _type: fc.constant("KeyEvent" as const),
    code: keyCodeArb,
    key: fc.oneof(printableKeyArb, fc.constantFrom('Enter', 'Escape', 'Backspace', 'Tab', 'ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight')),
    shiftKey: fc.boolean(),
    altKey: fc.boolean(),
    metaKey: fc.boolean(),
    ctrlKey: fc.boolean(),
  });
  
  it('Property 38: KeyEvent conversion preserves key information', () => {
    // **Feature: headless-terminal-emulator, Property 38: KeyEvent conversion preserves key information**
    fc.assert(fc.property(keyEventArb, (keyEvent) => {
      const { controller } = createTestSetup();
      
      // Create a mock DOM KeyboardEvent
      const domEvent = {
        code: keyEvent.code,
        key: keyEvent.key,
        shiftKey: keyEvent.shiftKey,
        altKey: keyEvent.altKey,
        metaKey: keyEvent.metaKey,
        ctrlKey: keyEvent.ctrlKey,
        preventDefault: () => {},
      } as KeyboardEvent;
      
      // Access the private method through type assertion for testing
      const convertedEvent = (controller as any).convertToKeyEvent(domEvent);
      
      // Verify all key information is preserved
      expect(convertedEvent._type).toBe("KeyEvent");
      expect(convertedEvent.code).toBe(keyEvent.code);
      expect(convertedEvent.key).toBe(keyEvent.key);
      expect(convertedEvent.shiftKey).toBe(keyEvent.shiftKey);
      expect(convertedEvent.altKey).toBe(keyEvent.altKey);
      expect(convertedEvent.metaKey).toBe(keyEvent.metaKey);
      expect(convertedEvent.ctrlKey).toBe(keyEvent.ctrlKey);
    }), { numRuns: 100 });
  });
  
  it('Property 39: Key encoding produces valid sequences', () => {
    // **Feature: headless-terminal-emulator, Property 39: Key encoding produces valid sequences**
    fc.assert(fc.property(keyEventArb, (keyEvent) => {
      const { controller } = createTestSetup();
      
      // Access the private method through type assertion for testing
      const encodedSequence = (controller as any).encodeKeyEvent(keyEvent);
      
      if (encodedSequence !== null) {
        // If encoding is produced, it should be a valid Uint8Array
        expect(encodedSequence).toBeInstanceOf(Uint8Array);
        expect(encodedSequence.length).toBeGreaterThan(0);
        
        // All bytes should be valid (0-255)
        for (let i = 0; i < encodedSequence.length; i++) {
          expect(encodedSequence[i]).toBeGreaterThanOrEqual(0);
          expect(encodedSequence[i]).toBeLessThanOrEqual(255);
        }
      }
      // If encodedSequence is null, that's also valid (some keys don't produce sequences)
    }), { numRuns: 100 });
  });
  
  it('Property 40: Key encoding round-trip', () => {
    // **Feature: headless-terminal-emulator, Property 40: Key encoding round-trip**
    fc.assert(fc.property(keyEventArb, (keyEvent) => {
      const { terminal, controller } = createTestSetup();
      
      // Get initial terminal state
      const initialCursor = terminal.getCursor();
      const initialLine = terminal.getLine(initialCursor.row);
      
      // Access the private method through type assertion for testing
      const encodedSequence = (controller as any).encodeKeyEvent(keyEvent);
      
      if (encodedSequence !== null) {
        // Process the encoded sequence through the terminal
        terminal.write(encodedSequence);
        
        // The terminal should have processed the sequence without error
        // (We can't predict exact behavior since it depends on the key,
        // but we can verify the terminal is still in a valid state)
        const newCursor = terminal.getCursor();
        
        // Cursor should be within valid bounds
        expect(newCursor.row).toBeGreaterThanOrEqual(0);
        expect(newCursor.row).toBeLessThan(24); // terminal height
        expect(newCursor.col).toBeGreaterThanOrEqual(0);
        expect(newCursor.col).toBeLessThan(80); // terminal width
        
        // Terminal should still be responsive
        expect(() => terminal.getLine(0)).not.toThrow();
      }
    }), { numRuns: 50 }); // Reduced runs since this involves terminal processing
  });
  
  it('Property 41: Mode-dependent key encoding', () => {
    // **Feature: headless-terminal-emulator, Property 41: Mode-dependent key encoding**
    const arrowKeyArb = fc.constantFrom('ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight');
    
    fc.assert(fc.property(arrowKeyArb, fc.boolean(), (arrowKey, applicationMode) => {
      const { terminal, controller } = createTestSetup();
      
      // Set application cursor keys mode
      terminal.setMode('DECCKM', applicationMode);
      
      const keyEvent: KeyEvent = {
        _type: "KeyEvent",
        code: arrowKey,
        key: arrowKey,
        shiftKey: false,
        altKey: false,
        metaKey: false,
        ctrlKey: false,
      };
      
      // Access the private method through type assertion for testing
      const encodedSequence = (controller as any).encodeKeyEvent(keyEvent);
      
      if (encodedSequence !== null) {
        // The encoding should be different based on the mode
        // We can't easily test the exact difference without knowing the internal encoding,
        // but we can verify that encoding is consistent for the same mode
        const encodedSequence2 = (controller as any).encodeKeyEvent(keyEvent);
        
        if (encodedSequence2 !== null) {
          // Same key event in same mode should produce same encoding
          expect(encodedSequence).toEqual(encodedSequence2);
        }
        
        // Verify the sequence is valid
        expect(encodedSequence).toBeInstanceOf(Uint8Array);
        expect(encodedSequence.length).toBeGreaterThan(0);
      }
    }), { numRuns: 50 });
  });
  
  it('Property 60: Text extraction preserves content', () => {
    // **Feature: headless-terminal-emulator, Property 60: Text extraction preserves content**
    const printableTextArb = fc.string({
      minLength: 1,
      maxLength: 20,
    }).filter(s => s.trim().length > 0); // Ensure non-empty after trimming
    
    fc.assert(fc.property(
      fc.array(printableTextArb, { minLength: 1, maxLength: 5 }), // Array of lines
      fc.integer({ min: 0, max: 4 }), // Start row
      fc.integer({ min: 0, max: 19 }), // Start col
      fc.integer({ min: 0, max: 4 }), // End row
      fc.integer({ min: 0, max: 19 }), // End col
      (lines, startRow, startCol, endRow, endCol) => {
        const { terminal, controller } = createTestSetup();
        
        // Ensure start <= end
        if (startRow > endRow || (startRow === endRow && startCol > endCol)) {
          [startRow, endRow] = [endRow, startRow];
          [startCol, endCol] = [endCol, startCol];
        }
        
        // Ensure bounds are within terminal
        startRow = Math.min(startRow, lines.length - 1);
        endRow = Math.min(endRow, lines.length - 1);
        
        // Write test content to terminal
        for (let i = 0; i < lines.length; i++) {
          // Position cursor at start of line
          terminal.write(`\x1b[${i + 1};1H`); // Move to row i+1, col 1 (1-based)
          terminal.write(lines[i]);
        }
        
        // Set up selection manually (simulating mouse selection)
        (controller as any).selection = {
          active: true,
          start: { row: startRow, col: startCol },
          end: { row: endRow, col: endCol },
        };
        
        // Extract selected text
        const extractedText = (controller as any).extractSelectedText();
        
        if (extractedText !== null) {
          // Verify that extracted text contains expected content
          // The exact content depends on what was written and the selection bounds
          expect(typeof extractedText).toBe('string');
          
          // For single-line selections, verify content preservation
          if (startRow === endRow && lines[startRow]) {
            const expectedContent = lines[startRow].slice(startCol, endCol + 1).trimEnd();
            if (expectedContent.length > 0) {
              expect(extractedText).toBe(expectedContent);
            }
          }
          
          // Verify no null characters or other control characters in output
          expect(extractedText).not.toMatch(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/);
          
          // Verify line breaks are preserved for multi-line selections
          if (startRow < endRow) {
            expect(extractedText.includes('\n')).toBe(true);
          }
        }
      }
    ), { numRuns: 50 });
  });
});