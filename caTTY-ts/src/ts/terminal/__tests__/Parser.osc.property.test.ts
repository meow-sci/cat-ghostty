/**
 * Property-based tests for OSC parsing in Parser.
 * These tests verify that OSC sequences are correctly parsed and trigger appropriate actions.
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fc from 'fast-check';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { Parser, type ParserHandlers } from '../Parser.js';
import { type Attributes, UnderlineStyle } from '../types.js';

describe('Parser OSC Property Tests', () => {
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
   * Feature: headless-terminal-emulator, Property 22: OSC parsing triggers appropriate actions
   * For any valid OSC sequence, parsing it should either emit the appropriate event or update the appropriate state
   * Validates: Requirements 7.1
   */
  it('Property 22: OSC parsing triggers appropriate actions', () => {
    fc.assert(
      fc.property(
        // Generate different types of OSC sequences
        fc.oneof(
          // OSC 0/2 - Window title sequences
          fc.record({
            type: fc.constant('title' as const),
            command: fc.constantFrom(0, 2),
            title: fc.string({ minLength: 1, maxLength: 50 }).filter(s => !s.includes('\x07') && !s.includes('\x1B'))
          }),
          // OSC 8 - Hyperlink sequences
          fc.record({
            type: fc.constant('hyperlink' as const),
            command: fc.constant(8),
            url: fc.webUrl(),
            id: fc.option(fc.string({ minLength: 1, maxLength: 20 }).filter(s => !s.includes(';') && !s.includes(':') && !s.includes('\x07') && !s.includes('\x1B')))
          }),
          // OSC 52 - Clipboard sequences
          fc.record({
            type: fc.constant('clipboard' as const),
            command: fc.constant(52),
            content: fc.string({ minLength: 1, maxLength: 30 }).filter(s => !s.includes('\x07') && !s.includes('\x1B'))
          }),
          // Unknown OSC sequences (should be ignored)
          fc.record({
            type: fc.constant('unknown' as const),
            command: fc.integer({ min: 100, max: 999 }),
            data: fc.string({ minLength: 0, maxLength: 20 }).filter(s => !s.includes('\x07') && !s.includes('\x1B'))
          })
        ),
        fc.constantFrom(0x07, 0x1B), // Terminator: BEL or ESC (for ST)
        (oscSpec, terminator) => {
          // Track events that were emitted
          let titleChangeEmitted = false;
          let emittedTitle: string | undefined;
          let hyperlinkEmitted = false;
          let emittedUrl: string | undefined;
          let emittedId: string | undefined;
          let clipboardEmitted = false;
          let emittedClipboard: string | undefined;
          let legacyOscCalled = false;
          let legacyCommand: number | undefined;
          let legacyData: string | undefined;

          const handlers: ParserHandlers = {
            onTitleChange: (title: string) => {
              titleChangeEmitted = true;
              emittedTitle = title;
            },
            onHyperlink: (url: string, id?: string) => {
              hyperlinkEmitted = true;
              emittedUrl = url;
              emittedId = id;
            },
            onClipboard: (content: string) => {
              clipboardEmitted = true;
              emittedClipboard = content;
            },
            onOsc: (command: number, data: string) => {
              legacyOscCalled = true;
              legacyCommand = command;
              legacyData = data;
            }
          };

          const parser = new Parser(handlers, wasmInstance);

          // Build OSC sequence based on type
          let oscData: string;
          switch (oscSpec.type) {
            case 'title':
              oscData = `${oscSpec.command};${oscSpec.title}`;
              break;
            case 'hyperlink':
              const params = oscSpec.id ? `id=${oscSpec.id}` : '';
              oscData = `${oscSpec.command};${params};${oscSpec.url}`;
              break;
            case 'clipboard':
              const base64Content = btoa(oscSpec.content);
              oscData = `${oscSpec.command};c;${base64Content}`;
              break;
            case 'unknown':
              oscData = `${oscSpec.command};${oscSpec.data}`;
              break;
          }

          // Create complete OSC sequence: ESC ] <data> <terminator>
          let sequence: Uint8Array;
          if (terminator === 0x07) {
            // BEL terminator
            sequence = new Uint8Array([
              0x1B, 0x5D, // ESC ]
              ...new TextEncoder().encode(oscData),
              0x07 // BEL
            ]);
          } else {
            // ESC \ (ST) terminator - just ESC for now (parser handles it)
            sequence = new Uint8Array([
              0x1B, 0x5D, // ESC ]
              ...new TextEncoder().encode(oscData),
              0x1B // ESC (ST terminator)
            ]);
          }

          // Parse the sequence
          parser.parse(sequence);

          // Verify appropriate actions were triggered based on OSC type
          switch (oscSpec.type) {
            case 'title':
              // Title change events should be emitted for OSC 0/2
              expect(titleChangeEmitted).toBe(true);
              expect(emittedTitle).toBe(oscSpec.title);
              
              // Legacy handler should also be called
              expect(legacyOscCalled).toBe(true);
              expect(legacyCommand).toBe(oscSpec.command);
              expect(legacyData).toBe(oscSpec.title);
              
              // Other events should not be emitted
              expect(hyperlinkEmitted).toBe(false);
              expect(clipboardEmitted).toBe(false);
              break;

            case 'hyperlink':
              // Hyperlink events should be emitted for OSC 8
              expect(hyperlinkEmitted).toBe(true);
              expect(emittedUrl).toBe(oscSpec.url);
              // Handle both null and undefined for optional id
              if (oscSpec.id === null) {
                expect(emittedId).toBeUndefined();
              } else {
                expect(emittedId).toBe(oscSpec.id);
              }
              
              // Legacy handler should also be called
              expect(legacyOscCalled).toBe(true);
              expect(legacyCommand).toBe(8);
              
              // Other events should not be emitted
              expect(titleChangeEmitted).toBe(false);
              expect(clipboardEmitted).toBe(false);
              break;

            case 'clipboard':
              // Clipboard events should be emitted for OSC 52
              expect(clipboardEmitted).toBe(true);
              expect(emittedClipboard).toBe(oscSpec.content);
              
              // Legacy handler should also be called
              expect(legacyOscCalled).toBe(true);
              expect(legacyCommand).toBe(52);
              
              // Other events should not be emitted
              expect(titleChangeEmitted).toBe(false);
              expect(hyperlinkEmitted).toBe(false);
              break;

            case 'unknown':
              // Unknown OSC sequences should not emit specific events
              expect(titleChangeEmitted).toBe(false);
              expect(hyperlinkEmitted).toBe(false);
              expect(clipboardEmitted).toBe(false);
              
              // But legacy handler should still be called
              expect(legacyOscCalled).toBe(true);
              expect(legacyCommand).toBe(oscSpec.command);
              expect(legacyData).toBe(oscSpec.data);
              break;
          }

          // Verify parser doesn't crash or throw errors
          expect(() => parser.dispose()).not.toThrow();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test: OSC sequences with various terminators work correctly
   */
  it('Property 22 (terminator handling): OSC sequences work with both BEL and ST terminators', () => {
    fc.assert(
      fc.property(
        // Generate simple title OSC sequences
        fc.record({
          command: fc.constantFrom(0, 2),
          title: fc.string({ minLength: 1, maxLength: 20 }).filter(s => !s.includes('\x07') && !s.includes('\x1B'))
        }),
        (oscSpec) => {
          // Test with BEL terminator
          let belTitleEmitted = false;
          let belEmittedTitle: string | undefined;
          
          const belHandlers: ParserHandlers = {
            onTitleChange: (title: string) => {
              belTitleEmitted = true;
              belEmittedTitle = title;
            }
          };

          const belParser = new Parser(belHandlers, wasmInstance);
          const belSequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode(`${oscSpec.command};${oscSpec.title}`),
            0x07 // BEL
          ]);
          belParser.parse(belSequence);

          // Test with ESC terminator (ST)
          let stTitleEmitted = false;
          let stEmittedTitle: string | undefined;
          
          const stHandlers: ParserHandlers = {
            onTitleChange: (title: string) => {
              stTitleEmitted = true;
              stEmittedTitle = title;
            }
          };

          const stParser = new Parser(stHandlers, wasmInstance);
          const stSequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode(`${oscSpec.command};${oscSpec.title}`),
            0x1B // ESC (ST terminator)
          ]);
          stParser.parse(stSequence);

          // Both terminators should work and produce the same result
          expect(belTitleEmitted).toBe(true);
          expect(stTitleEmitted).toBe(true);
          expect(belEmittedTitle).toBe(oscSpec.title);
          expect(stEmittedTitle).toBe(oscSpec.title);

          belParser.dispose();
          stParser.dispose();
        }
      ),
      { numRuns: 50 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 23: OSC 8 hyperlink association
   * For any OSC 8 sequence with URL, all subsequently written characters should have that URL associated until the hyperlink is cleared
   * Validates: Requirements 7.3
   */
  it('Property 23: OSC 8 hyperlink association', () => {
    fc.assert(
      fc.property(
        // Generate hyperlink URLs and text to write
        fc.record({
          url: fc.webUrl(),
          id: fc.option(fc.string({ minLength: 1, maxLength: 20 }).filter(s => !s.includes(';') && !s.includes(':') && !s.includes('\x07') && !s.includes('\x1B'))),
          textBeforeLink: fc.string({ minLength: 1, maxLength: 10 }).filter(s => s.length > 0),
          textWithLink: fc.string({ minLength: 1, maxLength: 10 }).filter(s => s.length > 0),
          textAfterLink: fc.string({ minLength: 1, maxLength: 10 }).filter(s => s.length > 0)
        }),
        (testData) => {
          // Track current attributes and written characters
          let currentAttributes: Attributes = {
            fg: { type: 'default' },
            bg: { type: 'default' },
            bold: false,
            italic: false,
            underline: UnderlineStyle.None,
            inverse: false,
            strikethrough: false
          };
          
          const writtenCharacters: Array<{ char: string; attributes: Attributes }> = [];
          
          const handlers: ParserHandlers = {
            onHyperlink: (url: string, id?: string) => {
              // When hyperlink is set, update current attributes
              currentAttributes = {
                ...currentAttributes,
                url: url || undefined // Empty URL clears the hyperlink
              };
            },
            onPrintable: (char: string) => {
              // When character is written, it should inherit current attributes
              writtenCharacters.push({
                char,
                attributes: { ...currentAttributes }
              });
            }
          };

          const parser = new Parser(handlers, wasmInstance);

          // Step 1: Write some text before setting hyperlink
          const beforeLinkBytes = new TextEncoder().encode(testData.textBeforeLink);
          parser.parse(beforeLinkBytes);

          // Step 2: Set hyperlink with OSC 8 sequence
          const params = testData.id ? `id=${testData.id}` : '';
          const oscData = `8;${params};${testData.url}`;
          const hyperlinkSequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode(oscData),
            0x07 // BEL
          ]);
          parser.parse(hyperlinkSequence);

          // Step 3: Write text that should have the hyperlink
          const withLinkBytes = new TextEncoder().encode(testData.textWithLink);
          parser.parse(withLinkBytes);

          // Step 4: Clear hyperlink with empty OSC 8 sequence
          const clearSequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode('8;;'),
            0x07 // BEL
          ]);
          parser.parse(clearSequence);

          // Step 5: Write text that should not have the hyperlink
          const afterLinkBytes = new TextEncoder().encode(testData.textAfterLink);
          parser.parse(afterLinkBytes);

          // Verify the results
          const beforeLinkChars = writtenCharacters.slice(0, testData.textBeforeLink.length);
          const withLinkChars = writtenCharacters.slice(
            testData.textBeforeLink.length, 
            testData.textBeforeLink.length + testData.textWithLink.length
          );
          const afterLinkChars = writtenCharacters.slice(
            testData.textBeforeLink.length + testData.textWithLink.length
          );

          // Characters before hyperlink should not have URL
          beforeLinkChars.forEach((charData, i) => {
            expect(charData.char).toBe(testData.textBeforeLink[i]);
            expect(charData.attributes.url).toBeUndefined();
          });

          // Characters with hyperlink should have the URL
          withLinkChars.forEach((charData, i) => {
            expect(charData.char).toBe(testData.textWithLink[i]);
            expect(charData.attributes.url).toBe(testData.url);
          });

          // Characters after clearing hyperlink should not have URL
          afterLinkChars.forEach((charData, i) => {
            expect(charData.char).toBe(testData.textAfterLink[i]);
            expect(charData.attributes.url).toBeUndefined();
          });

          parser.dispose();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 24: Unknown OSC sequences are ignored
   * For any invalid or unknown OSC sequence, the terminal should continue processing without error
   * Validates: Requirements 7.5
   */
  it('Property 24: Unknown OSC sequences are ignored', () => {
    fc.assert(
      fc.property(
        // Generate unknown OSC commands (avoiding known ones: 0, 2, 8, 52)
        fc.integer({ min: 3, max: 999 }).filter(n => n !== 8 && n !== 52),
        fc.string({ minLength: 0, maxLength: 50 }).filter(s => !s.includes('\x07') && !s.includes('\x1B')),
        fc.constantFrom(0x07, 0x1B), // Terminator: BEL or ESC
        (unknownCommand, data, terminator) => {
          // Track that no specific events are emitted for unknown OSC sequences
          let titleChangeEmitted = false;
          let hyperlinkEmitted = false;
          let clipboardEmitted = false;
          let legacyOscCalled = false;
          let legacyCommand: number | undefined;
          let legacyData: string | undefined;

          const handlers: ParserHandlers = {
            onTitleChange: (title: string) => {
              titleChangeEmitted = true;
            },
            onHyperlink: (url: string, id?: string) => {
              hyperlinkEmitted = true;
            },
            onClipboard: (content: string) => {
              clipboardEmitted = true;
            },
            onOsc: (command: number, data: string) => {
              legacyOscCalled = true;
              legacyCommand = command;
              legacyData = data;
            }
          };

          const parser = new Parser(handlers, wasmInstance);

          // Create unknown OSC sequence
          const oscData = `${unknownCommand};${data}`;
          let sequence: Uint8Array;
          if (terminator === 0x07) {
            // BEL terminator
            sequence = new Uint8Array([
              0x1B, 0x5D, // ESC ]
              ...new TextEncoder().encode(oscData),
              0x07 // BEL
            ]);
          } else {
            // ESC \ (ST) terminator
            sequence = new Uint8Array([
              0x1B, 0x5D, // ESC ]
              ...new TextEncoder().encode(oscData),
              0x1B // ESC (ST terminator)
            ]);
          }

          // Parse the unknown OSC sequence - should not throw
          expect(() => parser.parse(sequence)).not.toThrow();

          // Unknown OSC sequences should not trigger specific events
          expect(titleChangeEmitted).toBe(false);
          expect(hyperlinkEmitted).toBe(false);
          expect(clipboardEmitted).toBe(false);

          // But the legacy OSC handler should still be called for unknown sequences
          expect(legacyOscCalled).toBe(true);
          expect(legacyCommand).toBe(unknownCommand);
          expect(legacyData).toBe(data);

          // Parser should remain in a valid state after processing unknown OSC
          expect(() => parser.reset()).not.toThrow();
          expect(() => parser.dispose()).not.toThrow();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test: Malformed OSC sequences are handled gracefully
   */
  it('Property 22 (error handling): Malformed OSC sequences do not crash the parser', () => {
    fc.assert(
      fc.property(
        // Generate potentially malformed OSC data
        fc.oneof(
          fc.string({ minLength: 0, maxLength: 50 }), // Random strings
          fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 0, maxLength: 20 }).map(arr => 
            String.fromCharCode(...arr.filter(n => n !== 0x07 && n !== 0x1B)) // Avoid terminators
          ),
          fc.constantFrom('', ';;;', '999999', 'invalid;data;here', '0', '2', '8', '52')
        ),
        (malformedData) => {
          const handlers: ParserHandlers = {
            onTitleChange: () => {},
            onHyperlink: () => {},
            onClipboard: () => {},
            onOsc: () => {}
          };

          const parser = new Parser(handlers, wasmInstance);

          // Create OSC sequence with potentially malformed data
          const sequence = new Uint8Array([
            0x1B, 0x5D, // ESC ]
            ...new TextEncoder().encode(malformedData),
            0x07 // BEL
          ]);

          // The key property: parsing should not throw an error
          expect(() => parser.parse(sequence)).not.toThrow();
          
          // Parser should remain in a valid state
          expect(() => parser.reset()).not.toThrow();
          expect(() => parser.dispose()).not.toThrow();
        }
      ),
      { numRuns: 100 }
    );
  });
});