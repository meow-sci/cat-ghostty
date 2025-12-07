/**
 * Property-based tests for Kitty Graphics Protocol parsing.
 * These tests verify that graphics commands are correctly parsed and processed.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { KittyGraphicsParser } from '../graphics/KittyGraphicsParser.js';

describe('KittyGraphicsParser Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 75: Graphics command parsing
   * For any valid Kitty graphics escape sequence, the parser should extract the action and payload correctly
   * Validates: Requirements 26.1
   */
  it('Property 75: Graphics command parsing', () => {
    fc.assert(
      fc.property(
        // Generate different types of graphics commands
        fc.oneof(
          // Transmission command
          fc.record({
            action: fc.constant('t' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            format: fc.option(fc.constantFrom('24', '32', '100'), { nil: undefined }),
            width: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            height: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            more: fc.option(fc.boolean(), { nil: undefined }),
          }),
          // Display command
          fc.record({
            action: fc.constant('d' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            placementId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            x: fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
            y: fc.option(fc.integer({ min: 0, max: 200 }), { nil: undefined }),
            rows: fc.option(fc.integer({ min: 1, max: 100 }), { nil: undefined }),
            cols: fc.option(fc.integer({ min: 1, max: 100 }), { nil: undefined }),
            sourceX: fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }),
            sourceY: fc.option(fc.integer({ min: 0, max: 2000 }), { nil: undefined }),
            sourceWidth: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            sourceHeight: fc.option(fc.integer({ min: 1, max: 2000 }), { nil: undefined }),
            zIndex: fc.option(fc.integer({ min: -100, max: 100 }), { nil: undefined }),
            unicodePlaceholder: fc.option(fc.integer({ min: 0x1F000, max: 0x1F999 }), { nil: undefined }),
          }),
          // Delete command
          fc.record({
            action: fc.constant('D' as const),
            imageId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
            placementId: fc.option(fc.integer({ min: 1, max: 1000 }), { nil: undefined }),
          })
        ),
        fc.string({ maxLength: 100 }), // payload
        (command, payload) => {
        const parser = new KittyGraphicsParser();
        
        // Build control data string from command
        const controlPairs: string[] = [];
        
        // Add action
        controlPairs.push(`a=${command.action}`);
        
        // Add optional parameters based on command type
        if ('imageId' in command && command.imageId !== undefined) {
          controlPairs.push(`i=${command.imageId}`);
        }
        if ('placementId' in command && command.placementId !== undefined) {
          controlPairs.push(`p=${command.placementId}`);
        }
        if ('format' in command && command.format !== undefined) {
          controlPairs.push(`f=${command.format}`);
        }
        if ('width' in command && command.width !== undefined) {
          controlPairs.push(`s=${command.width}`);
        }
        if ('height' in command && command.height !== undefined) {
          controlPairs.push(`v=${command.height}`);
        }
        if ('x' in command && command.x !== undefined) {
          controlPairs.push(`x=${command.x}`);
        }
        if ('y' in command && command.y !== undefined) {
          controlPairs.push(`y=${command.y}`);
        }
        if ('rows' in command && command.rows !== undefined) {
          controlPairs.push(`r=${command.rows}`);
        }
        if ('cols' in command && command.cols !== undefined) {
          controlPairs.push(`c=${command.cols}`);
        }
        if ('sourceX' in command && command.sourceX !== undefined) {
          controlPairs.push(`X=${command.sourceX}`);
        }
        if ('sourceY' in command && command.sourceY !== undefined) {
          controlPairs.push(`Y=${command.sourceY}`);
        }
        if ('sourceWidth' in command && command.sourceWidth !== undefined) {
          controlPairs.push(`w=${command.sourceWidth}`);
        }
        if ('sourceHeight' in command && command.sourceHeight !== undefined) {
          controlPairs.push(`h=${command.sourceHeight}`);
        }
        if ('more' in command && command.more !== undefined) {
          controlPairs.push(`m=${command.more ? '1' : '0'}`);
        }
        if ('zIndex' in command && command.zIndex !== undefined) {
          controlPairs.push(`z=${command.zIndex}`);
        }
        if ('unicodePlaceholder' in command && command.unicodePlaceholder !== undefined) {
          controlPairs.push(`U=${command.unicodePlaceholder}`);
        }
        
        const controlData = controlPairs.join(',');
        const sequence = `${controlData};${payload}`;
        
        // Parse the sequence
        const result = parser.parseGraphicsCommand(sequence);
        
        // Verify parsing succeeded
        expect(result).not.toBeNull();
        if (!result) return;
        
        // Verify action is correct
        expect(result.params.action).toBe(command.action);
        
        // Verify payload is correct
        expect(result.payload).toBe(payload);
        
        // Verify all provided parameters are parsed correctly
        if ('imageId' in command && command.imageId !== undefined) {
          expect(result.params.imageId).toBe(command.imageId);
        }
        if ('placementId' in command && command.placementId !== undefined) {
          expect(result.params.placementId).toBe(command.placementId);
        }
        if ('format' in command && command.format !== undefined) {
          expect(result.params.format).toBe(command.format);
        }
        if ('width' in command && command.width !== undefined) {
          expect(result.params.width).toBe(command.width);
        }
        if ('height' in command && command.height !== undefined) {
          expect(result.params.height).toBe(command.height);
        }
        if ('x' in command && command.x !== undefined) {
          expect(result.params.x).toBe(command.x);
        }
        if ('y' in command && command.y !== undefined) {
          expect(result.params.y).toBe(command.y);
        }
        if ('rows' in command && command.rows !== undefined) {
          expect(result.params.rows).toBe(command.rows);
        }
        if ('cols' in command && command.cols !== undefined) {
          expect(result.params.cols).toBe(command.cols);
        }
        if ('sourceX' in command && command.sourceX !== undefined) {
          expect(result.params.sourceX).toBe(command.sourceX);
        }
        if ('sourceY' in command && command.sourceY !== undefined) {
          expect(result.params.sourceY).toBe(command.sourceY);
        }
        if ('sourceWidth' in command && command.sourceWidth !== undefined) {
          expect(result.params.sourceWidth).toBe(command.sourceWidth);
        }
        if ('sourceHeight' in command && command.sourceHeight !== undefined) {
          expect(result.params.sourceHeight).toBe(command.sourceHeight);
        }
        if ('more' in command && command.more !== undefined) {
          expect(result.params.more).toBe(command.more);
        }
        if ('zIndex' in command && command.zIndex !== undefined) {
          expect(result.params.zIndex).toBe(command.zIndex);
        }
        if ('unicodePlaceholder' in command && command.unicodePlaceholder !== undefined) {
          expect(result.params.unicodePlaceholder).toBe(command.unicodePlaceholder);
        }
      }),
      { numRuns: 100 }
    );
  });

  it('Property 75: Graphics command parsing handles invalid sequences gracefully', () => {
    fc.assert(
      fc.property(
        fc.string({ maxLength: 100 }),
        (invalidSequence) => {
          const parser = new KittyGraphicsParser();
          
          // Parse invalid sequence (no action parameter)
          const result = parser.parseGraphicsCommand(invalidSequence);
          
          // Should return null for invalid sequences
          // (sequences without an action parameter)
          if (!invalidSequence.includes('a=t') && 
              !invalidSequence.includes('a=d') && 
              !invalidSequence.includes('a=D')) {
            expect(result).toBeNull();
          }
        }),
      { numRuns: 100 }
    );
  });
});
