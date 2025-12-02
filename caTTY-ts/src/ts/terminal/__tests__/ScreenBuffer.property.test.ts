/**
 * Property-based tests for ScreenBuffer.
 * These tests verify universal properties that should hold across all inputs.
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { ScreenBuffer } from '../ScreenBuffer.js';
import { UnderlineStyle } from '../types.js';

describe('ScreenBuffer Property Tests', () => {
  /**
   * Feature: headless-terminal-emulator, Property 1: Buffer initialization creates correct dimensions
   * For any valid width and height parameters, initializing a terminal should create a screen buffer with exactly those dimensions
   * Validates: Requirements 1.1
   */
  it('Property 1: Buffer initialization creates correct dimensions', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 200 }), // cols (reduced for performance)
        fc.integer({ min: 1, max: 200 }), // rows (reduced for performance)
        (cols, rows) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          // Verify dimensions
          expect(buffer.getCols()).toBe(cols);
          expect(buffer.getRows()).toBe(rows);
          
          // Verify corner cells are accessible (sampling instead of checking all)
          expect(buffer.getCell(0, 0)).toBeDefined();
          expect(buffer.getCell(0, cols - 1)).toBeDefined();
          expect(buffer.getCell(rows - 1, 0)).toBeDefined();
          expect(buffer.getCell(rows - 1, cols - 1)).toBeDefined();
          
          // Verify all lines have correct length
          for (let row = 0; row < rows; row++) {
            const line = buffer.getLine(row);
            expect(line.cells).toHaveLength(cols);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 3: Cell structure completeness
   * For any cell in the terminal buffer, it should always have a character value and complete SGR attributes
   * Validates: Requirements 1.3, 1.4
   */
  it('Property 3: Cell structure completeness', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 1, max: 100 }), // cols
        fc.integer({ min: 1, max: 100 }), // rows
        fc.integer({ min: 0, max: 99 }),  // row to check
        fc.integer({ min: 0, max: 99 }),  // col to check
        (cols, rows, checkRow, checkCol) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          // Clamp check positions to valid range
          const row = checkRow % rows;
          const col = checkCol % cols;
          
          const cell = buffer.getCell(row, col);
          
          // Verify cell has all required attributes
          expect(cell.char).toBeDefined();
          expect(typeof cell.char).toBe('string');
          expect(cell.width).toBeDefined();
          expect(typeof cell.width).toBe('number');
          
          // Verify color attributes
          expect(cell.fg).toBeDefined();
          expect(cell.fg.type).toBeDefined();
          expect(cell.bg).toBeDefined();
          expect(cell.bg.type).toBeDefined();
          
          // Verify boolean attributes
          expect(typeof cell.bold).toBe('boolean');
          expect(typeof cell.italic).toBe('boolean');
          expect(typeof cell.inverse).toBe('boolean');
          expect(typeof cell.strikethrough).toBe('boolean');
          
          // Verify underline style
          expect(cell.underline).toBeDefined();
          expect(Object.values(UnderlineStyle)).toContain(cell.underline);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 19: Scroll operations move content correctly
   * For any screen content and scroll up/down sequence with parameter N, content should shift by exactly N lines in the specified direction
   * Validates: Requirements 5.8, 5.9
   */
  it('Property 19: Scroll operations move content correctly', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 5, max: 50 }), // cols
        fc.integer({ min: 5, max: 50 }), // rows
        fc.integer({ min: 1, max: 10 }), // scroll amount
        fc.constantFrom('up', 'down'),   // scroll direction
        (cols, rows, scrollAmount, direction) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          // Fill buffer with identifiable content (row number as character)
          for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
              const cell = buffer.getCell(row, col);
              cell.char = String(row % 10);
              buffer.setCell(row, col, cell);
            }
          }
          
          // Store original content
          const originalContent: string[] = [];
          for (let row = 0; row < rows; row++) {
            originalContent.push(buffer.getLine(row).cells[0].char);
          }
          
          // Perform scroll
          if (direction === 'up') {
            buffer.scrollUp(scrollAmount);
            
            // Verify content shifted up
            const actualScroll = Math.min(scrollAmount, rows);
            for (let row = 0; row < rows - actualScroll; row++) {
              const cell = buffer.getCell(row, 0);
              expect(cell.char).toBe(originalContent[row + actualScroll]);
            }
            
            // Verify bottom lines are empty
            for (let row = rows - actualScroll; row < rows; row++) {
              const cell = buffer.getCell(row, 0);
              expect(cell.char).toBe(' ');
            }
          } else {
            buffer.scrollDown(scrollAmount);
            
            // Verify content shifted down
            const actualScroll = Math.min(scrollAmount, rows);
            for (let row = actualScroll; row < rows; row++) {
              const cell = buffer.getCell(row, 0);
              expect(cell.char).toBe(originalContent[row - actualScroll]);
            }
            
            // Verify top lines are empty
            for (let row = 0; row < actualScroll; row++) {
              const cell = buffer.getCell(row, 0);
              expect(cell.char).toBe(' ');
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 35: Scroll region content isolation
   * For any content outside a scroll region, scrolling within the region should not modify that content
   * Validates: Requirements 10.2
   */
  it('Property 35: Scroll region content isolation', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 10, max: 50 }), // rows
        fc.integer({ min: 0, max: 8 }),   // region top
        fc.integer({ min: 1, max: 5 }),   // region size
        fc.integer({ min: 1, max: 3 }),   // scroll amount
        fc.constantFrom('up', 'down'),    // scroll direction
        (cols, rows, regionTop, regionSize, scrollAmount, direction) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          // Ensure valid scroll region
          const top = regionTop;
          const bottom = Math.min(top + regionSize, rows - 1);
          
          if (top >= bottom) return; // Skip invalid regions
          
          // Fill buffer with identifiable content (row number)
          for (let row = 0; row < rows; row++) {
            for (let col = 0; col < cols; col++) {
              const cell = buffer.getCell(row, col);
              cell.char = String(row % 10);
              buffer.setCell(row, col, cell);
            }
          }
          
          // Store content outside the scroll region
          const contentAbove: string[] = [];
          for (let row = 0; row < top; row++) {
            contentAbove.push(buffer.getLine(row).cells[0].char);
          }
          
          const contentBelow: string[] = [];
          for (let row = bottom + 1; row < rows; row++) {
            contentBelow.push(buffer.getLine(row).cells[0].char);
          }
          
          // Perform scroll within region
          if (direction === 'up') {
            buffer.scrollUp(scrollAmount, { top, bottom });
          } else {
            buffer.scrollDown(scrollAmount, { top, bottom });
          }
          
          // Verify content above region is unchanged
          for (let row = 0; row < top; row++) {
            const cell = buffer.getCell(row, 0);
            expect(cell.char).toBe(contentAbove[row]);
          }
          
          // Verify content below region is unchanged
          for (let row = bottom + 1; row < rows; row++) {
            const cell = buffer.getCell(row, 0);
            expect(cell.char).toBe(contentBelow[row - bottom - 1]);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 51: Character insertion shifts content
   * For any line content and insert character sequence with parameter N, N blank cells should be inserted at cursor position and existing content should shift right
   * Validates: Requirements 17.1, 17.5
   */
  it('Property 51: Character insertion shifts content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 5, max: 20 }),  // rows
        fc.integer({ min: 0, max: 9 }),   // row to insert at
        fc.integer({ min: 0, max: 9 }),   // col to insert at
        fc.integer({ min: 1, max: 5 }),   // number of cells to insert
        (cols, rows, insertRow, insertCol, count) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          const row = insertRow % rows;
          const col = insertCol % cols;
          
          // Fill line with identifiable content
          for (let c = 0; c < cols; c++) {
            const cell = buffer.getCell(row, c);
            cell.char = String(c % 10);
            buffer.setCell(row, c, cell);
          }
          
          // Store original content at and after insertion point
          const originalContent: string[] = [];
          for (let c = col; c < cols; c++) {
            originalContent.push(buffer.getCell(row, c).char);
          }
          
          // Insert cells
          buffer.insertCells(row, col, count);
          
          // Verify blank cells were inserted
          const actualInsert = Math.min(count, cols - col);
          for (let c = col; c < col + actualInsert; c++) {
            expect(buffer.getCell(row, c).char).toBe(' ');
          }
          
          // Verify content shifted right
          for (let c = col + actualInsert; c < cols; c++) {
            const expectedIndex = c - col - actualInsert;
            if (expectedIndex < originalContent.length) {
              expect(buffer.getCell(row, c).char).toBe(originalContent[expectedIndex]);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 52: Character deletion shifts content
   * For any line content and delete character sequence with parameter N, N cells should be deleted at cursor position and remaining content should shift left
   * Validates: Requirements 17.2, 17.5
   */
  it('Property 52: Character deletion shifts content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // cols
        fc.integer({ min: 5, max: 20 }),  // rows
        fc.integer({ min: 0, max: 9 }),   // row to delete from
        fc.integer({ min: 0, max: 9 }),   // col to delete from
        fc.integer({ min: 1, max: 5 }),   // number of cells to delete
        (cols, rows, deleteRow, deleteCol, count) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          const row = deleteRow % rows;
          const col = deleteCol % cols;
          
          // Fill line with identifiable content
          for (let c = 0; c < cols; c++) {
            const cell = buffer.getCell(row, c);
            cell.char = String(c % 10);
            buffer.setCell(row, c, cell);
          }
          
          // Store original content after deletion point
          const originalContent: string[] = [];
          for (let c = col; c < cols; c++) {
            originalContent.push(buffer.getCell(row, c).char);
          }
          
          // Delete cells
          buffer.deleteCells(row, col, count);
          
          // Verify content shifted left
          const actualDelete = Math.min(count, cols - col);
          for (let c = col; c < cols - actualDelete; c++) {
            const expectedIndex = c - col + actualDelete;
            if (expectedIndex < originalContent.length) {
              expect(buffer.getCell(row, c).char).toBe(originalContent[expectedIndex]);
            }
          }
          
          // Verify right edge is blank
          for (let c = cols - actualDelete; c < cols; c++) {
            expect(buffer.getCell(row, c).char).toBe(' ');
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 53: Line insertion shifts content
   * For any screen content and insert line sequence with parameter N, N blank lines should be inserted at cursor row and existing lines should shift down
   * Validates: Requirements 17.3, 17.5
   */
  it('Property 53: Line insertion shifts content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 5, max: 30 }),  // cols
        fc.integer({ min: 10, max: 30 }), // rows
        fc.integer({ min: 0, max: 9 }),   // row to insert at
        fc.integer({ min: 1, max: 5 }),   // number of lines to insert
        (cols, rows, insertRow, count) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          const row = insertRow % rows;
          
          // Fill buffer with identifiable content
          for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
              const cell = buffer.getCell(r, c);
              cell.char = String(r % 10);
              buffer.setCell(r, c, cell);
            }
          }
          
          // Store original content at and after insertion point
          const originalContent: string[] = [];
          for (let r = row; r < rows; r++) {
            originalContent.push(buffer.getLine(r).cells[0].char);
          }
          
          // Insert lines
          buffer.insertLines(row, count);
          
          // Verify blank lines were inserted
          const actualInsert = Math.min(count, rows - row);
          for (let r = row; r < row + actualInsert; r++) {
            expect(buffer.getCell(r, 0).char).toBe(' ');
          }
          
          // Verify content shifted down
          for (let r = row + actualInsert; r < rows; r++) {
            const expectedIndex = r - row - actualInsert;
            if (expectedIndex < originalContent.length) {
              expect(buffer.getCell(r, 0).char).toBe(originalContent[expectedIndex]);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 54: Line deletion shifts content
   * For any screen content and delete line sequence with parameter N, N lines should be deleted at cursor row and remaining lines should shift up
   * Validates: Requirements 17.4, 17.5
   */
  it('Property 54: Line deletion shifts content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 5, max: 30 }),  // cols
        fc.integer({ min: 10, max: 30 }), // rows
        fc.integer({ min: 0, max: 9 }),   // row to delete from
        fc.integer({ min: 1, max: 5 }),   // number of lines to delete
        (cols, rows, deleteRow, count) => {
          const buffer = new ScreenBuffer(cols, rows);
          
          const row = deleteRow % rows;
          
          // Fill buffer with identifiable content
          for (let r = 0; r < rows; r++) {
            for (let c = 0; c < cols; c++) {
              const cell = buffer.getCell(r, c);
              cell.char = String(r % 10);
              buffer.setCell(r, c, cell);
            }
          }
          
          // Store original content after deletion point
          const originalContent: string[] = [];
          for (let r = row; r < rows; r++) {
            originalContent.push(buffer.getLine(r).cells[0].char);
          }
          
          // Delete lines
          buffer.deleteLines(row, count);
          
          // Verify content shifted up
          const actualDelete = Math.min(count, rows - row);
          for (let r = row; r < rows - actualDelete; r++) {
            const expectedIndex = r - row + actualDelete;
            if (expectedIndex < originalContent.length) {
              expect(buffer.getCell(r, 0).char).toBe(originalContent[expectedIndex]);
            }
          }
          
          // Verify bottom is blank
          for (let r = rows - actualDelete; r < rows; r++) {
            expect(buffer.getCell(r, 0).char).toBe(' ');
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Feature: headless-terminal-emulator, Property 2: Resize preserves overlapping content
   * For any terminal with content, resizing should preserve all content that fits within the overlapping region of old and new dimensions
   * Validates: Requirements 1.2
   */
  it('Property 2: Resize preserves overlapping content', () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 10, max: 50 }), // original cols
        fc.integer({ min: 10, max: 50 }), // original rows
        fc.integer({ min: 5, max: 60 }),  // new cols
        fc.integer({ min: 5, max: 60 }),  // new rows
        (oldCols, oldRows, newCols, newRows) => {
          const buffer = new ScreenBuffer(oldCols, oldRows);
          
          // Fill buffer with identifiable content
          for (let row = 0; row < oldRows; row++) {
            for (let col = 0; col < oldCols; col++) {
              const cell = buffer.getCell(row, col);
              cell.char = String((row * oldCols + col) % 10);
              buffer.setCell(row, col, cell);
            }
          }
          
          // Store original content in overlapping region
          const overlapRows = Math.min(oldRows, newRows);
          const overlapCols = Math.min(oldCols, newCols);
          const originalContent: string[][] = [];
          
          for (let row = 0; row < overlapRows; row++) {
            const rowContent: string[] = [];
            for (let col = 0; col < overlapCols; col++) {
              rowContent.push(buffer.getCell(row, col).char);
            }
            originalContent.push(rowContent);
          }
          
          // Resize buffer
          buffer.resize(newCols, newRows);
          
          // Verify dimensions changed
          expect(buffer.getCols()).toBe(newCols);
          expect(buffer.getRows()).toBe(newRows);
          
          // Verify overlapping content is preserved
          for (let row = 0; row < overlapRows; row++) {
            for (let col = 0; col < overlapCols; col++) {
              expect(buffer.getCell(row, col).char).toBe(originalContent[row][col]);
            }
          }
          
          // Verify new cells are empty
          for (let row = 0; row < newRows; row++) {
            for (let col = 0; col < newCols; col++) {
              if (row >= oldRows || col >= oldCols) {
                expect(buffer.getCell(row, col).char).toBe(' ');
              }
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
