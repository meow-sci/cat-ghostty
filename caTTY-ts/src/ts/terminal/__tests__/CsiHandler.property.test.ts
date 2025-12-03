/**
 * Property-based tests for CSI cursor movement.
 * These tests verify universal properties for cursor movement sequences.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { CsiHandler } from '../CsiHandler.js';
import { ScreenBuffer } from '../ScreenBuffer.js';
import type { CursorState } from '../types.js';

describe('CsiHandler Cursor Movement Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 15: CSI cursor movement correctness
   * For any CSI cursor movement sequence (up, down, left, right) with parameter N, 
   * the cursor should move exactly N positions in the specified direction, bounded by screen edges
   * Validates: Requirements 5.1, 5.2, 5.3, 5.4
   */
  it('Property 15: CSI cursor movement correctness', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 80 }),  // cols
        fc.integer({ min: 10, max: 40 }),  // rows
        fc.integer({ min: 0, max: 79 }),   // initial cursor col
        fc.integer({ min: 0, max: 39 }),   // initial cursor row
        fc.integer({ min: 1, max: 20 }),   // movement amount
        fc.constantFrom('A', 'B', 'C', 'D'), // direction (A=up, B=down, C=forward, D=backward)
        (cols, rows, initialCol, initialRow, amount, direction) => {
          const buffer = new ScreenBuffer(cols, rows);
          const cursor: CursorState = {
            row: initialRow % rows,
            col: initialCol % cols,
            visible: true,
            blinking: false,
          };
          
          const handler = new CsiHandler(buffer, cursor);
          
          // Store initial position
          const startRow = cursor.row;
          const startCol = cursor.col;
          
          // Execute cursor movement
          handler.handle([amount], '', direction.charCodeAt(0));
          
          // Verify cursor moved in correct direction and by correct amount
          switch (direction) {
            case 'A': // Cursor Up
              {
                const expectedRow = Math.max(0, startRow - amount);
                expect(cursor.row).toBe(expectedRow);
                expect(cursor.col).toBe(startCol); // Column unchanged
              }
              break;
              
            case 'B': // Cursor Down
              {
                const expectedRow = Math.min(rows - 1, startRow + amount);
                expect(cursor.row).toBe(expectedRow);
                expect(cursor.col).toBe(startCol); // Column unchanged
              }
              break;
              
            case 'C': // Cursor Forward (right)
              {
                const expectedCol = Math.min(cols - 1, startCol + amount);
                expect(cursor.col).toBe(expectedCol);
                expect(cursor.row).toBe(startRow); // Row unchanged
              }
              break;
              
            case 'D': // Cursor Backward (left)
              {
                const expectedCol = Math.max(0, startCol - amount);
                expect(cursor.col).toBe(expectedCol);
                expect(cursor.row).toBe(startRow); // Row unchanged
              }
              break;
          }
          
          // Verify cursor stays within bounds
          expect(cursor.row).toBeGreaterThanOrEqual(0);
          expect(cursor.row).toBeLessThan(rows);
          expect(cursor.col).toBeGreaterThanOrEqual(0);
          expect(cursor.col).toBeLessThan(cols);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 16: CSI cursor positioning absolute
   * For any CSI cursor position sequence with row R and column C, 
   * the cursor should move to exactly position (R, C)
   * Validates: Requirements 5.5
   */
  it('Property 16: CSI cursor positioning absolute', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 80 }),  // cols
        fc.integer({ min: 10, max: 40 }),  // rows
        fc.integer({ min: 1, max: 100 }),  // target row (1-based)
        fc.integer({ min: 1, max: 100 }),  // target col (1-based)
        (cols, rows, targetRow, targetCol) => {
          const buffer = new ScreenBuffer(cols, rows);
          const cursor: CursorState = {
            row: 5,  // Start at arbitrary position
            col: 5,
            visible: true,
            blinking: false,
          };
          
          const handler = new CsiHandler(buffer, cursor);
          
          // Execute cursor position command (CSI H or CSI f)
          // Test both 'H' (CUP) and 'f' (HVP)
          const finalChar = Math.random() < 0.5 ? 'H' : 'f';
          handler.handle([targetRow, targetCol], '', finalChar.charCodeAt(0));
          
          // Calculate expected position (1-based to 0-based, clamped to bounds)
          const expectedRow = Math.max(0, Math.min(rows - 1, targetRow - 1));
          const expectedCol = Math.max(0, Math.min(cols - 1, targetCol - 1));
          
          // Verify cursor moved to expected position
          expect(cursor.row).toBe(expectedRow);
          expect(cursor.col).toBe(expectedCol);
          
          // Verify cursor stays within bounds
          expect(cursor.row).toBeGreaterThanOrEqual(0);
          expect(cursor.row).toBeLessThan(rows);
          expect(cursor.col).toBeGreaterThanOrEqual(0);
          expect(cursor.col).toBeLessThan(cols);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional test: Verify default parameter behavior
   * When no parameter is provided, cursor movement should default to 1
   */
  it('Property 15 (edge case): Default parameter behavior', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 80 }),  // cols
        fc.integer({ min: 10, max: 40 }),  // rows
        fc.integer({ min: 5, max: 35 }),   // initial cursor row (away from edges)
        fc.integer({ min: 5, max: 75 }),   // initial cursor col (away from edges)
        fc.constantFrom('A', 'B', 'C', 'D'), // direction
        (cols, rows, initialRow, initialCol, direction) => {
          const buffer = new ScreenBuffer(cols, rows);
          const cursor: CursorState = {
            row: initialRow % rows,
            col: initialCol % cols,
            visible: true,
            blinking: false,
          };
          
          const handler = new CsiHandler(buffer, cursor);
          
          // Store initial position
          const startRow = cursor.row;
          const startCol = cursor.col;
          
          // Execute cursor movement with default parameter (0 or empty)
          handler.handle([0], '', direction.charCodeAt(0));
          
          // Verify cursor moved by 1 (default)
          switch (direction) {
            case 'A': // Cursor Up
              expect(cursor.row).toBe(Math.max(0, startRow - 1));
              expect(cursor.col).toBe(startCol);
              break;
              
            case 'B': // Cursor Down
              expect(cursor.row).toBe(Math.min(rows - 1, startRow + 1));
              expect(cursor.col).toBe(startCol);
              break;
              
            case 'C': // Cursor Forward
              expect(cursor.col).toBe(Math.min(cols - 1, startCol + 1));
              expect(cursor.row).toBe(startRow);
              break;
              
            case 'D': // Cursor Backward
              expect(cursor.col).toBe(Math.max(0, startCol - 1));
              expect(cursor.row).toBe(startRow);
              break;
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional test: Verify cursor position with default parameters
   * When row or column is 0 or omitted, it should default to 1 (top-left)
   */
  it('Property 16 (edge case): Default position parameters', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 80 }),  // cols
        fc.integer({ min: 10, max: 40 }),  // rows
        (cols, rows) => {
          const buffer = new ScreenBuffer(cols, rows);
          const cursor: CursorState = {
            row: 10,  // Start at arbitrary position
            col: 10,
            visible: true,
            blinking: false,
          };
          
          const handler = new CsiHandler(buffer, cursor);
          
          // Test with default parameters (0, 0) which should go to (0, 0)
          handler.handle([0, 0], '', 'H'.charCodeAt(0));
          
          // Should move to top-left (0, 0) since 0 defaults to 1 and 1-based becomes 0-based
          expect(cursor.row).toBe(0);
          expect(cursor.col).toBe(0);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional test: Verify cursor stays at edge when moving beyond bounds
   */
  it('Property 15 (edge case): Cursor clamping at boundaries', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 80 }),  // cols
        fc.integer({ min: 10, max: 40 }),  // rows
        fc.constantFrom('A', 'B', 'C', 'D'), // direction
        (cols, rows, direction) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          // Start at appropriate edge for each direction and use amount that exceeds bounds
          let startRow: number, startCol: number, amount: number;
          switch (direction) {
            case 'A': // Up - start at bottom
              startRow = rows - 1;
              startCol = Math.floor(cols / 2);
              amount = rows + 10; // More than screen height
              break;
            case 'B': // Down - start at top
              startRow = 0;
              startCol = Math.floor(cols / 2);
              amount = rows + 10; // More than screen height
              break;
            case 'C': // Forward - start at left
              startRow = Math.floor(rows / 2);
              startCol = 0;
              amount = cols + 10; // More than screen width
              break;
            case 'D': // Backward - start at right
              startRow = Math.floor(rows / 2);
              startCol = cols - 1;
              amount = cols + 10; // More than screen width
              break;
            default:
              startRow = 0;
              startCol = 0;
              amount = 100;
          }
          
          const cursor: CursorState = {
            row: startRow,
            col: startCol,
            visible: true,
            blinking: false,
          };
          
          const handler = new CsiHandler(buffer, cursor);
          
          // Execute large movement that should exceed bounds
          handler.handle([amount], '', direction.charCodeAt(0));
          
          // Verify cursor is clamped at appropriate edge
          switch (direction) {
            case 'A': // Up - should be at top
              expect(cursor.row).toBe(0);
              break;
            case 'B': // Down - should be at bottom
              expect(cursor.row).toBe(rows - 1);
              break;
            case 'C': // Forward - should be at right edge
              expect(cursor.col).toBe(cols - 1);
              break;
            case 'D': // Backward - should be at left edge
              expect(cursor.col).toBe(0);
              break;
          }
          
          // Verify cursor is still within bounds
          expect(cursor.row).toBeGreaterThanOrEqual(0);
          expect(cursor.row).toBeLessThan(rows);
          expect(cursor.col).toBeGreaterThanOrEqual(0);
          expect(cursor.col).toBeLessThan(cols);
        }
      ),
      { numRuns: 100 }
    );
  });
});
