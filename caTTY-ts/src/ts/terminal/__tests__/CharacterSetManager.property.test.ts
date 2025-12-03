/**
 * Property-based tests for CharacterSetManager.
 * These tests verify universal properties that should hold across all inputs.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { CharacterSetManager } from '../CharacterSetManager.js';
import { CharacterSet } from '../types.js';

describe('CharacterSetManager Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 56: Character set designation tracking
   * For any character set designation sequence, the terminal should track which character set is designated to which slot (G0-G3)
   * Validates: Requirements 19.1
   */
  it('Property 56: Character set designation tracking', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 0, max: 3 }), // G slot (0-3)
        fc.constantFrom(
          CharacterSet.ASCII,
          CharacterSet.DECSpecialGraphics,
          CharacterSet.UK,
          CharacterSet.DECAlternateSpecial,
          CharacterSet.DECTechnical
        ), // Character set to designate
        (slot, charset) => {
          const manager = new CharacterSetManager();
          
          // Designate character set to slot
          manager.designateCharacterSet(slot, charset);
          
          const state = manager.getState();
          
          // Verify the character set was designated to the correct slot
          switch (slot) {
            case 0:
              expect(state.g0).toBe(charset);
              break;
            case 1:
              expect(state.g1).toBe(charset);
              break;
            case 2:
              expect(state.g2).toBe(charset);
              break;
            case 3:
              expect(state.g3).toBe(charset);
              break;
          }
          
          // Verify other slots remain unchanged from their initial state
          const defaultState = new CharacterSetManager().getState();
          if (slot !== 0) expect(state.g0).toBe(defaultState.g0);
          if (slot !== 1) expect(state.g1).toBe(defaultState.g1);
          if (slot !== 2) expect(state.g2).toBe(defaultState.g2);
          if (slot !== 3) expect(state.g3).toBe(defaultState.g3);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 57: Character set switching
   * For any shift-in or shift-out control character, the active character set should switch between G0 and G1
   * Validates: Requirements 19.2
   */
  it('Property 57: Character set switching', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(
          CharacterSet.ASCII,
          CharacterSet.DECSpecialGraphics,
          CharacterSet.UK,
          CharacterSet.DECAlternateSpecial,
          CharacterSet.DECTechnical
        ), // G0 character set
        fc.constantFrom(
          CharacterSet.ASCII,
          CharacterSet.DECSpecialGraphics,
          CharacterSet.UK,
          CharacterSet.DECAlternateSpecial,
          CharacterSet.DECTechnical
        ), // G1 character set
        fc.array(fc.constantFrom('SI', 'SO'), { minLength: 1, maxLength: 10 }), // Sequence of shift operations
        (g0Charset, g1Charset, shiftSequence) => {
          const manager = new CharacterSetManager();
          
          // Set up different character sets in G0 and G1
          manager.designateCharacterSet(0, g0Charset);
          manager.designateCharacterSet(1, g1Charset);
          
          // Initially G0 should be active
          expect(manager.getState().activeGL).toBe('g0');
          
          let expectedActive = 'g0';
          
          // Apply shift sequence and verify each step
          for (const shift of shiftSequence) {
            if (shift === 'SI') {
              manager.shiftIn();
              expectedActive = 'g0';
            } else if (shift === 'SO') {
              manager.shiftOut();
              expectedActive = 'g1';
            }
            
            expect(manager.getState().activeGL).toBe(expectedActive);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 58: DEC Special Graphics mapping
   * For any character written while DEC Special Graphics is active, it should be mapped to the corresponding line-drawing glyph
   * Validates: Requirements 19.3
   */
  it('Property 58: DEC Special Graphics mapping', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(
          // Characters that have DEC Special Graphics mappings
          'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
          '{', '|', '}', '~', '_', '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i'
        ),
        (char) => {
          const manager = new CharacterSetManager();
          
          // Set up DEC Special Graphics in G0
          manager.designateCharacterSet(0, CharacterSet.DECSpecialGraphics);
          
          // Map the character
          const mappedChar = manager.mapCharacter(char);
          
          // Verify the character was mapped (should be different from input for mapped chars)
          // We know these specific characters have mappings
          const knownMappings: Record<string, string> = {
            'q': '─', 'x': '│', 'l': '┌', 'k': '┐', 'm': '└', 'j': '┘', 'n': '┼',
            't': '├', 'u': '┤', 'v': '┴', 'w': '┬', 'y': '≤', 'z': '≥', '{': 'π',
            '|': '≠', '}': '£', '~': '·', '_': ' ', '`': '◆', 'a': '▒', 'b': '␉',
            'c': '␌', 'd': '␍', 'e': '␊', 'f': '°', 'g': '±', 'h': '␤', 'i': '␋',
            'o': '⎺', 'p': '⎻', 'r': '⎼', 's': '⎽'
          };
          
          if (knownMappings[char]) {
            expect(mappedChar).toBe(knownMappings[char]);
          } else {
            // For unmapped characters, they should pass through unchanged
            expect(mappedChar).toBe(char);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 59: Character set affects written characters
   * For any character written, it should be mapped according to the currently active character set
   * Validates: Requirements 19.4
   */
  it('Property 59: Character set affects written characters', () => {
    fc.assert(
      fc.property(
        fc.constantFrom(
          CharacterSet.ASCII,
          CharacterSet.DECSpecialGraphics,
          CharacterSet.UK,
          CharacterSet.DECAlternateSpecial,
          CharacterSet.DECTechnical
        ), // G0 character set
        fc.constantFrom(
          CharacterSet.ASCII,
          CharacterSet.DECSpecialGraphics,
          CharacterSet.UK,
          CharacterSet.DECAlternateSpecial,
          CharacterSet.DECTechnical
        ), // G1 character set
        fc.constantFrom('g0', 'g1'), // Which character set to activate
        fc.string({ minLength: 1, maxLength: 1 }).filter(c => c.charCodeAt(0) >= 32 && c.charCodeAt(0) <= 126), // Printable ASCII character
        (g0Charset, g1Charset, activeSet, char) => {
          const manager = new CharacterSetManager();
          
          // Set up character sets
          manager.designateCharacterSet(0, g0Charset);
          manager.designateCharacterSet(1, g1Charset);
          
          // Activate the specified character set
          if (activeSet === 'g0') {
            manager.shiftIn();
          } else {
            manager.shiftOut();
          }
          
          // Map the character
          const mappedChar = manager.mapCharacter(char);
          
          // Verify the mapping depends on the active character set
          const expectedCharset = activeSet === 'g0' ? g0Charset : g1Charset;
          
          if (expectedCharset === CharacterSet.DECSpecialGraphics) {
            // For DEC Special Graphics, some characters should be mapped
            const knownMappings: Record<string, string> = {
              'j': '┘', 'k': '┐', 'l': '┌', 'm': '└', 'n': '┼', 'o': '⎺', 'p': '⎻', 
              'q': '─', 'r': '⎼', 's': '⎽', 't': '├', 'u': '┤', 'v': '┴', 'w': '┬', 
              'x': '│', 'y': '≤', 'z': '≥', '{': 'π', '|': '≠', '}': '£', '~': '·',
              '_': ' ', '`': '◆', 'a': '▒', 'b': '␉', 'c': '␌', 'd': '␍', 'e': '␊', 
              'f': '°', 'g': '±', 'h': '␤', 'i': '␋'
            };
            
            if (knownMappings[char]) {
              expect(mappedChar).toBe(knownMappings[char]);
            } else {
              expect(mappedChar).toBe(char);
            }
          } else {
            // For ASCII and other character sets, characters should pass through unchanged
            expect(mappedChar).toBe(char);
          }
          
          // Verify that switching character sets changes the mapping for mapped characters
          if (char === 'q' && g0Charset !== g1Charset) {
            // Switch to the other character set
            if (activeSet === 'g0') {
              manager.shiftOut();
            } else {
              manager.shiftIn();
            }
            
            const newMappedChar = manager.mapCharacter(char);
            const newActiveCharset = activeSet === 'g0' ? g1Charset : g0Charset;
            
            if (newActiveCharset === CharacterSet.DECSpecialGraphics) {
              expect(newMappedChar).toBe('─');
            } else {
              expect(newMappedChar).toBe('q');
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});