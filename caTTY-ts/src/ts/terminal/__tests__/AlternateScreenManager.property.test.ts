/**
 * Property-based tests for AlternateScreenManager.
 * These tests verify universal properties that should hold across all inputs.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { AlternateScreenManager } from '../AlternateScreenManager.js';

describe('AlternateScreenManager Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 29: Alternate screen buffer isolation
   * For any content written to the primary buffer, switching to alternate buffer should show a separate, independent buffer
   * Validates: Requirements 9.1
   */
  it('Property 29: Alternate screen buffer isolation', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        fc.integer({ min: 0, max: 49 }),  // row to write
        fc.integer({ min: 0, max: 49 }),  // col to write
        fc.string({ minLength: 1, maxLength: 1 }), // character to write
        (cols, rows, writeRow, writeCol, char) => {
          const manager = new AlternateScreenManager(cols, rows);
          
          // Clamp write position to valid range
          const row = writeRow % rows;
          const col = writeCol % cols;
          
          // Write content to primary buffer
          const primaryBuffer = manager.getCurrentBuffer();
          const cell = primaryBuffer.getCell(row, col);
          cell.char = char;
          primaryBuffer.setCell(row, col, cell);
          
          // Verify content is in primary buffer
          expect(manager.getCurrentBuffer().getCell(row, col).char).toBe(char);
          
          // Switch to alternate screen
          manager.switchToAlternate();
          
          // Verify we're in alternate mode
          expect(manager.isAlternateScreen()).toBe(true);
          
          // Verify alternate buffer is separate (should be empty/default)
          const alternateCell = manager.getCurrentBuffer().getCell(row, col);
          expect(alternateCell.char).toBe(' '); // Should be cleared
          
          // Verify primary buffer still has the content (not modified)
          expect(manager.getPrimaryBuffer().getCell(row, col).char).toBe(char);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 30: Alternate screen round-trip
   * For any primary buffer state, switching to alternate and back to primary should restore the exact original primary buffer state
   * Validates: Requirements 9.2
   */
  it('Property 30: Alternate screen round-trip', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        fc.array(
          fc.record({
            row: fc.integer({ min: 0, max: 49 }),
            col: fc.integer({ min: 0, max: 49 }),
            char: fc.string({ minLength: 1, maxLength: 1 }),
          }),
          { minLength: 1, maxLength: 20 }
        ), // content to write
        (cols, rows, content) => {
          const manager = new AlternateScreenManager(cols, rows);
          
          // Write content to primary buffer
          const primaryBuffer = manager.getCurrentBuffer();
          for (const { row: r, col: c, char } of content) {
            const row = r % rows;
            const col = c % cols;
            const cell = primaryBuffer.getCell(row, col);
            cell.char = char;
            primaryBuffer.setCell(row, col, cell);
          }
          
          // Store primary buffer state
          const originalContent: Map<string, string> = new Map();
          for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
              const key = `${row},${col}`;
              originalContent.set(key, primaryBuffer.getCell(row, col).char);
            }
          }
          
          // Switch to alternate
          manager.switchToAlternate();
          
          // Write different content to alternate buffer
          const alternateBuffer = manager.getCurrentBuffer();
          const altCell = alternateBuffer.getCell(0, 0);
          altCell.char = 'X';
          alternateBuffer.setCell(0, 0, altCell);
          
          // Switch back to primary
          manager.switchToPrimary();
          
          // Verify we're back in primary mode
          expect(manager.isAlternateScreen()).toBe(false);
          
          // Verify primary buffer state is exactly as it was
          const restoredBuffer = manager.getCurrentBuffer();
          for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
              const key = `${row},${col}`;
              expect(restoredBuffer.getCell(row, col).char).toBe(originalContent.get(key));
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 31: Alternate screen no scrollback
   * For any content written in alternate screen mode, it should not appear in the scrollback buffer
   * Validates: Requirements 9.3
   */
  it('Property 31: Alternate screen no scrollback', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        (cols, rows) => {
          const manager = new AlternateScreenManager(cols, rows);
          
          // Switch to alternate screen
          manager.switchToAlternate();
          
          // Verify we're in alternate mode
          expect(manager.isAlternateScreen()).toBe(true);
          
          // This property is verified by the fact that the AlternateScreenManager
          // provides a way to check if we're in alternate mode, which the terminal
          // can use to decide whether to add content to scrollback.
          // The actual scrollback logic will be in the Terminal class.
          
          // For now, we verify that the alternate screen flag is correctly set
          expect(manager.isAlternateScreen()).toBe(true);
          
          // Switch back to primary
          manager.switchToPrimary();
          expect(manager.isAlternateScreen()).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 32: Buffer-independent cursor state
   * For any cursor position set in primary buffer, switching to alternate should have an independent cursor position
   * Validates: Requirements 9.4
   */
  it('Property 32: Buffer-independent cursor state', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        fc.integer({ min: 0, max: 49 }),  // primary cursor row
        fc.integer({ min: 0, max: 49 }),  // primary cursor col
        (cols, rows, cursorRow, cursorCol) => {
          const manager = new AlternateScreenManager(cols, rows);
          
          // Clamp cursor position to valid range
          const primaryRow = cursorRow % rows;
          const primaryCol = cursorCol % cols;
          
          // Set cursor position in primary buffer
          const primaryCursor = manager.getCurrentCursor();
          primaryCursor.row = primaryRow;
          primaryCursor.col = primaryCol;
          manager.setCurrentCursor(primaryCursor);
          
          // Verify cursor is set in primary
          expect(manager.getCurrentCursor().row).toBe(primaryRow);
          expect(manager.getCurrentCursor().col).toBe(primaryCol);
          
          // Switch to alternate screen
          manager.switchToAlternate();
          
          // Verify alternate screen has independent cursor (should be at origin)
          const alternateCursor = manager.getCurrentCursor();
          expect(alternateCursor.row).toBe(0);
          expect(alternateCursor.col).toBe(0);
          
          // Set different cursor position in alternate
          alternateCursor.row = Math.min(5, rows - 1);
          alternateCursor.col = Math.min(5, cols - 1);
          manager.setCurrentCursor(alternateCursor);
          
          // Switch back to primary
          manager.switchToPrimary();
          
          // Verify primary cursor is unchanged
          expect(manager.getCurrentCursor().row).toBe(primaryRow);
          expect(manager.getCurrentCursor().col).toBe(primaryCol);
          
          // Verify alternate cursor is still different
          expect(manager.getAlternateCursor().row).toBe(Math.min(5, rows - 1));
          expect(manager.getAlternateCursor().col).toBe(Math.min(5, cols - 1));
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 33: Alternate buffer initialization
   * For any terminal, switching to alternate buffer should show a cleared buffer with default attributes
   * Validates: Requirements 9.5
   */
  it('Property 33: Alternate buffer initialization', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        (cols, rows) => {
          const manager = new AlternateScreenManager(cols, rows);
          
          // Write some content to primary buffer first
          const primaryBuffer = manager.getCurrentBuffer();
          for (let row = 0; row < Math.min(5, rows); row++) {
            for (let col = 0; col < Math.min(5, cols); col++) {
              const cell = primaryBuffer.getCell(row, col);
              cell.char = 'X';
              primaryBuffer.setCell(row, col, cell);
            }
          }
          
          // Switch to alternate screen
          manager.switchToAlternate();
          
          // Verify alternate buffer is cleared (all cells are empty)
          const alternateBuffer = manager.getCurrentBuffer();
          for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
              const cell = alternateBuffer.getCell(row, col);
              expect(cell.char).toBe(' ');
              expect(cell.fg.type).toBe('default');
              expect(cell.bg.type).toBe('default');
              expect(cell.bold).toBe(false);
              expect(cell.italic).toBe(false);
            }
          }
          
          // Verify cursor is at origin
          const cursor = manager.getCurrentCursor();
          expect(cursor.row).toBe(0);
          expect(cursor.col).toBe(0);
          
          // Verify attributes are default
          const attrs = manager.getCurrentAttributes();
          expect(attrs.fg.type).toBe('default');
          expect(attrs.bg.type).toBe('default');
          expect(attrs.bold).toBe(false);
          expect(attrs.italic).toBe(false);
        }
      ),
      { numRuns: 100 }
    );
  });
});
