/**
 * Property-based tests for SGR parsing in Parser.
 * These tests verify that SGR sequences are correctly parsed using libghostty-vt.
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fc from 'fast-check';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { Parser, type ParserHandlers } from '../Parser.js';
import { type Attributes, UnderlineStyle } from '../types.js';

describe('Parser SGR Property Tests', () => {
  let wasmInstance: GhosttyVtInstance;

  // Load WASM instance before tests
  beforeAll(async () => {
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

  /**
   * Feature: headless-terminal-emulator, Property 20: SGR parsing updates attributes
   * For any valid SGR sequence, parsing it should update the current text attributes to match the sequence parameters
   * Validates: Requirements 6.1, 6.2
   */
  it('Property 20: SGR parsing updates attributes', () => {
    fc.assert(
      fc.property(
        // Generate SGR parameters (0-255 range covers most common SGR codes)
        fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 1, maxLength: 10 }),
        (params) => {
          // Track current attributes state
          let currentAttributes: Attributes | null = null;

          // Track if SGR handler was called
          let sgrHandlerCalled = false;
          let csiHandlerCalled = false;
          let receivedParams: number[] = [];

          const handlers: ParserHandlers = {
            onCsi: (params: number[], intermediates: string, final: number) => {
              // Track CSI handler calls
              if (final === 0x6D) { // 'm'
                csiHandlerCalled = true;
                receivedParams = [...params];
              }
            },
            onSgrAttributes: (attributes: Attributes) => {
              // Track SGR attribute updates from Parser's built-in SGR parsing
              sgrHandlerCalled = true;
              currentAttributes = { ...attributes };
            }
          };

          const parser = new Parser(handlers, wasmInstance);

          // Create SGR sequence: ESC[<params>m (using semicolon separator)
          const paramString = params.join(';');
          const sgrSequence = `\x1B[${paramString}m`;
          const data = new TextEncoder().encode(sgrSequence);

          // Parse the sequence
          parser.parse(data);

          // Verify CSI handler was called
          expect(csiHandlerCalled).toBe(true);

          // Verify parameters were received (parser may normalize empty params)
          expect(receivedParams.length).toBeGreaterThan(0);

          // Verify SGR handler was called and attributes were parsed
          expect(sgrHandlerCalled).toBe(true);
          expect(currentAttributes).not.toBeNull();

          // Type assertion after null check
          const attrs = currentAttributes!;
          {
            // Verify attributes are valid objects with expected structure
            expect(attrs.fg).toBeDefined();
            expect(attrs.bg).toBeDefined();
            expect(typeof attrs.bold).toBe('boolean');
            expect(typeof attrs.italic).toBe('boolean');
            expect(typeof attrs.inverse).toBe('boolean');
            expect(typeof attrs.strikethrough).toBe('boolean');
            expect(Object.values(UnderlineStyle)).toContain(attrs.underline);

            // Verify color objects have valid structure
            expect(['default', 'indexed', 'rgb']).toContain(attrs.fg.type);
            expect(['default', 'indexed', 'rgb']).toContain(attrs.bg.type);

            if (attrs.fg.type === 'indexed') {
              expect(attrs.fg.index).toBeGreaterThanOrEqual(0);
              expect(attrs.fg.index).toBeLessThanOrEqual(255);
            }
            if (attrs.fg.type === 'rgb') {
              expect(attrs.fg.r).toBeGreaterThanOrEqual(0);
              expect(attrs.fg.r).toBeLessThanOrEqual(255);
              expect(attrs.fg.g).toBeGreaterThanOrEqual(0);
              expect(attrs.fg.g).toBeLessThanOrEqual(255);
              expect(attrs.fg.b).toBeGreaterThanOrEqual(0);
              expect(attrs.fg.b).toBeLessThanOrEqual(255);
            }

            if (attrs.bg.type === 'indexed') {
              expect(attrs.bg.index).toBeGreaterThanOrEqual(0);
              expect(attrs.bg.index).toBeLessThanOrEqual(255);
            }
            if (attrs.bg.type === 'rgb') {
              expect(attrs.bg.r).toBeGreaterThanOrEqual(0);
              expect(attrs.bg.r).toBeLessThanOrEqual(255);
              expect(attrs.bg.g).toBeGreaterThanOrEqual(0);
              expect(attrs.bg.g).toBeLessThanOrEqual(255);
              expect(attrs.bg.b).toBeGreaterThanOrEqual(0);
              expect(attrs.bg.b).toBeLessThanOrEqual(255);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 21: SGR attributes persist across characters
   * For any SGR attributes set, all subsequently written characters should have those attributes until they are changed
   * Validates: Requirements 6.4, 6.5
   */
  it('Property 21: SGR attributes persist across characters', () => {
    fc.assert(
      fc.property(
        // Generate SGR parameters for setting attributes
        fc.record({
          // Generate initial SGR sequence
          initialSgr: fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 1, maxLength: 5 }),
          // Generate characters to write after setting attributes
          characters: fc.array(fc.constantFrom('a', 'b', 'c', 'd', 'e', 'A', 'B', 'C', '1', '2', '3', ' ', 'x', 'y', 'z'), { minLength: 1, maxLength: 10 }),
          // Generate optional second SGR sequence to change attributes
          secondSgr: fc.option(fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 1, maxLength: 3 }))
        }),
        ({ initialSgr, characters, secondSgr }) => {
          // Track attribute changes
          let currentAttributes: Attributes | null = null;
          let attributeHistory: Attributes[] = [];
          let characterAttributes: Attributes[] = [];

          const handlers: ParserHandlers = {
            onSgrAttributes: (attributes: Attributes) => {
              currentAttributes = { ...attributes };
              attributeHistory.push({ ...attributes });
            },
            onPrintable: (char: string) => {
              // When a character is written, it should use the current attributes
              if (currentAttributes) {
                characterAttributes.push({ ...currentAttributes });
              }
            }
          };

          const parser = new Parser(handlers, wasmInstance);

          // Set initial SGR attributes
          const initialSgrSequence = `\x1B[${initialSgr.join(';')}m`;
          parser.parse(new TextEncoder().encode(initialSgrSequence));

          // Verify initial attributes were set
          expect(currentAttributes).not.toBeNull();
          expect(attributeHistory.length).toBeGreaterThan(0);

          const initialAttributes = { ...currentAttributes! };

          // Write characters - they should all use the initial attributes
          for (const char of characters) {
            parser.parse(new TextEncoder().encode(char));
          }

          // Verify all characters got the same attributes as the initial SGR
          expect(characterAttributes.length).toBe(characters.length);
          for (let i = 0; i < characterAttributes.length; i++) {
            const charAttrs = characterAttributes[i];
            
            // All attributes should match the initial SGR attributes
            expect(charAttrs.fg).toEqual(initialAttributes.fg);
            expect(charAttrs.bg).toEqual(initialAttributes.bg);
            expect(charAttrs.bold).toBe(initialAttributes.bold);
            expect(charAttrs.italic).toBe(initialAttributes.italic);
            expect(charAttrs.underline).toBe(initialAttributes.underline);
            expect(charAttrs.inverse).toBe(initialAttributes.inverse);
            expect(charAttrs.strikethrough).toBe(initialAttributes.strikethrough);
            expect(charAttrs.url).toBe(initialAttributes.url);
          }

          // If there's a second SGR sequence, test attribute change persistence
          if (secondSgr) {
            // Reset character tracking
            characterAttributes = [];
            
            // Apply second SGR sequence
            const secondSgrSequence = `\x1B[${secondSgr.join(';')}m`;
            parser.parse(new TextEncoder().encode(secondSgrSequence));

            // Verify attributes changed
            expect(currentAttributes).not.toBeNull();
            const secondAttributes = { ...currentAttributes! };

            // Write more characters - they should use the new attributes
            const moreChars = ['a', 'b', 'c'];
            for (const char of moreChars) {
              parser.parse(new TextEncoder().encode(char));
            }

            // Verify characters after second SGR use the new attributes
            expect(characterAttributes.length).toBe(moreChars.length);
            for (let i = 0; i < characterAttributes.length; i++) {
              const charAttrs = characterAttributes[i];
              
              // All attributes should match the second SGR attributes
              expect(charAttrs.fg).toEqual(secondAttributes.fg);
              expect(charAttrs.bg).toEqual(secondAttributes.bg);
              expect(charAttrs.bold).toBe(secondAttributes.bold);
              expect(charAttrs.italic).toBe(secondAttributes.italic);
              expect(charAttrs.underline).toBe(secondAttributes.underline);
              expect(charAttrs.inverse).toBe(secondAttributes.inverse);
              expect(charAttrs.strikethrough).toBe(secondAttributes.strikethrough);
              expect(charAttrs.url).toBe(secondAttributes.url);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Test that libghostty-vt handles both semicolon and colon separators correctly.
   * This verifies libghostty-vt handles both separator types as documented.
   * Colon separators are used for specific color formats like underline styles.
   */
  it('Property 20 (separator compatibility): SGR parsing works with both semicolon and colon separators', () => {
    fc.assert(
      fc.property(
        // Test cases where colon separators are meaningful
        fc.constantFrom(
          // Underline with style (4:3 = curly underline)
          { params: [4, 3], semicolonSep: ';', colonSep: ':' },
          // Underline with different style (4:1 = single underline)  
          { params: [4, 1], semicolonSep: ';', colonSep: ':' },
          // Multiple parameters with mixed separators
          { params: [1, 4, 2], semicolonSep: ';', colonSep: ';' }
        ),
        (testCase) => {
          const { params, semicolonSep, colonSep } = testCase;

          // Test with semicolon separators
          let semicolonResult: Attributes | null = null;
          const semicolonHandlers: ParserHandlers = {
            onSgrAttributes: (attributes: Attributes) => {
              semicolonResult = { ...attributes };
            }
          };
          const semicolonParser = new Parser(semicolonHandlers, wasmInstance);
          const semicolonSequence = `\x1B[${params.join(semicolonSep)}m`;
          semicolonParser.parse(new TextEncoder().encode(semicolonSequence));

          // Test with colon separators (where appropriate)
          let colonResult: Attributes | null = null;
          const colonHandlers: ParserHandlers = {
            onSgrAttributes: (attributes: Attributes) => {
              colonResult = { ...attributes };
            }
          };
          const colonParser = new Parser(colonHandlers, wasmInstance);
          const colonSequence = `\x1B[${params.join(colonSep)}m`;
          colonParser.parse(new TextEncoder().encode(colonSequence));

          // Both should produce valid results
          expect(semicolonResult).not.toBeNull();
          expect(colonResult).not.toBeNull();

          // Type assertions after null checks
          const semiResult = semicolonResult!;
          const colResult = colonResult!;
          
          // For underline style tests, verify that colon separators work
          if (params[0] === 4) {
            // Both should recognize underline
            expect(semiResult.underline).toBeGreaterThan(UnderlineStyle.None);
            expect(colResult.underline).toBeGreaterThan(UnderlineStyle.None);

            // The colon separator version should properly parse the style
            if (params.length > 1 && colonSep === ':') {
              const expectedStyle = params[1];
              if (expectedStyle >= 0 && expectedStyle <= 5) {
                expect(colResult.underline).toBe(expectedStyle);
              }
            }

            // Note: With semicolon separators, [4, 3] is treated as separate parameters
            // (underline + italic), but with colon separators, [4, 3] is treated as
            // underline with style 3. So we don't compare all attributes directly.
          } else {
            // For non-underline tests, basic attributes should be consistent
            expect(semiResult.bold).toBe(colResult.bold);
            expect(semiResult.italic).toBe(colResult.italic);
            expect(semiResult.inverse).toBe(colResult.inverse);
            expect(semiResult.strikethrough).toBe(colResult.strikethrough);
          }
        }
      ),
      { numRuns: 20 }
    );
  });
});