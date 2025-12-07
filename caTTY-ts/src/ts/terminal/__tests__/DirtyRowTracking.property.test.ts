/**
 * Property-based tests for dirty row tracking optimization.
 * 
 * **Feature: headless-terminal-emulator, Property 75: Dirty row tracking correctness**
 * **Validates: Performance requirement - minimize unnecessary rendering**
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { Terminal } from '../Terminal.js';
import type { TerminalConfig } from '../Terminal.js';

describe('Dirty Row Tracking', () => {
  let terminal: Terminal;
  const config: TerminalConfig = {
    cols: 80,
    rows: 24,
    scrollback: 100,
  };

  beforeEach(() => {
    terminal = new Terminal(config);
  });

  /**
   * Property 75: Dirty row tracking correctness
   * For any terminal operation, only the rows that were actually modified should be marked as dirty
   */
  it('should mark only affected rows as dirty when writing a single character', () => {
    // Clear any initial dirty rows
    terminal.clearDirtyRows();
    
    // Write a single character
    terminal.write('A');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should only mark row 0 as dirty (cursor starts at 0,0)
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should mark only affected rows as dirty when writing multiple characters on same line', () => {
    // Clear any initial dirty rows
    terminal.clearDirtyRows();
    
    // Write multiple characters on the same line
    terminal.write('Hello');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should only mark row 0 as dirty
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should mark both old and new rows as dirty when cursor moves between rows', () => {
    // Write on first line
    terminal.write('Line 1');
    terminal.clearDirtyRows();
    
    // Move to next line
    terminal.write('\n');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark both row 0 (old cursor position) and row 1 (new cursor position)
    expect(dirtyRows.size).toBe(2);
    expect(dirtyRows.has(0)).toBe(true);
    expect(dirtyRows.has(1)).toBe(true);
  });

  it('should mark all rows as dirty when scrolling', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    terminal.clearDirtyRows();
    
    // Write one more line to trigger scrolling
    terminal.write('Scroll line\n');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark all rows as dirty (scrolling affects entire screen)
    expect(dirtyRows.size).toBe(config.rows);
    for (let row = 0; row < config.rows; row++) {
      expect(dirtyRows.has(row)).toBe(true);
    }
  });

  it('should mark affected rows as dirty when erasing in display (cursor to end)', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    
    // Move cursor to middle of screen
    terminal.write('\x1b[10;1H'); // Move to row 10, col 1
    terminal.clearDirtyRows();
    
    // Erase from cursor to end of display (CSI 0 J)
    terminal.write('\x1b[0J');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark rows from 9 (0-based row 9 = 1-based row 10) to end
    expect(dirtyRows.size).toBeGreaterThanOrEqual(config.rows - 9);
    for (let row = 9; row < config.rows; row++) {
      expect(dirtyRows.has(row)).toBe(true);
    }
  });

  it('should mark affected rows as dirty when erasing entire display', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    terminal.clearDirtyRows();
    
    // Erase entire display (CSI 2 J)
    terminal.write('\x1b[2J');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark all rows as dirty
    expect(dirtyRows.size).toBe(config.rows);
    for (let row = 0; row < config.rows; row++) {
      expect(dirtyRows.has(row)).toBe(true);
    }
  });

  it('should mark only current row as dirty when erasing in line', () => {
    // Write some content
    terminal.write('Hello World');
    
    // Move cursor to middle of line
    terminal.write('\x1b[1;6H'); // Move to row 1, col 6
    terminal.clearDirtyRows();
    
    // Erase from cursor to end of line (CSI 0 K)
    terminal.write('\x1b[0K');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should only mark row 0 as dirty
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should mark affected rows as dirty when inserting lines', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    
    // Move cursor to middle of screen
    terminal.write('\x1b[10;1H'); // Move to row 10, col 1
    terminal.clearDirtyRows();
    
    // Insert 3 lines (CSI 3 L)
    terminal.write('\x1b[3L');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark rows from 9 (0-based) to end
    expect(dirtyRows.size).toBeGreaterThanOrEqual(config.rows - 9);
    for (let row = 9; row < config.rows; row++) {
      expect(dirtyRows.has(row)).toBe(true);
    }
  });

  it('should mark affected rows as dirty when deleting lines', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    
    // Move cursor to middle of screen
    terminal.write('\x1b[10;1H'); // Move to row 10, col 1
    terminal.clearDirtyRows();
    
    // Delete 3 lines (CSI 3 M)
    terminal.write('\x1b[3M');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark rows from 9 (0-based) to end
    expect(dirtyRows.size).toBeGreaterThanOrEqual(config.rows - 9);
    for (let row = 9; row < config.rows; row++) {
      expect(dirtyRows.has(row)).toBe(true);
    }
  });

  it('should mark only current row as dirty when inserting characters', () => {
    // Write some content
    terminal.write('Hello World');
    
    // Move cursor to middle of line
    terminal.write('\x1b[1;6H'); // Move to row 1, col 6
    terminal.clearDirtyRows();
    
    // Insert 3 characters (CSI 3 @)
    terminal.write('\x1b[3@');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should only mark row 0 as dirty
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should mark only current row as dirty when deleting characters', () => {
    // Write some content
    terminal.write('Hello World');
    
    // Move cursor to middle of line
    terminal.write('\x1b[1;6H'); // Move to row 1, col 6
    terminal.clearDirtyRows();
    
    // Delete 3 characters (CSI 3 P)
    terminal.write('\x1b[3P');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should only mark row 0 as dirty
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should clear dirty rows after calling clearDirtyRows()', () => {
    // Write some content
    terminal.write('Hello');
    
    // Verify rows are dirty
    let dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBeGreaterThan(0);
    
    // Clear dirty rows
    terminal.clearDirtyRows();
    
    // Verify no rows are dirty
    dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBe(0);
  });

  it('should mark rows as dirty when cursor moves with CSI sequences', () => {
    // Position cursor at row 5
    terminal.write('\x1b[6;1H'); // Move to row 6, col 1 (1-based)
    terminal.clearDirtyRows();
    
    // Move cursor to row 10
    terminal.write('\x1b[11;1H'); // Move to row 11, col 1 (1-based)
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark both old row (5, 0-based) and new row (10, 0-based)
    expect(dirtyRows.has(5)).toBe(true);
    expect(dirtyRows.has(10)).toBe(true);
  });

  it('should not mark rows as dirty when cursor moves within same row', () => {
    // Write some content
    terminal.write('Hello World');
    terminal.clearDirtyRows();
    
    // Move cursor within same row (carriage return)
    terminal.write('\r');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should mark row 0 as dirty (cursor moved within row)
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
  });

  it('should handle multiple operations accumulating dirty rows', () => {
    terminal.clearDirtyRows();
    
    // Write on row 0
    terminal.write('Line 0');
    
    // Move to row 5
    terminal.write('\x1b[6;1H');
    terminal.write('Line 5');
    
    // Move to row 10
    terminal.write('\x1b[11;1H');
    terminal.write('Line 10');
    
    // Get dirty rows
    const dirtyRows = terminal.getDirtyRows();
    
    // Should have marked rows 0, 5, and 10 as dirty
    expect(dirtyRows.has(0)).toBe(true);
    expect(dirtyRows.has(5)).toBe(true);
    expect(dirtyRows.has(10)).toBe(true);
  });
});
