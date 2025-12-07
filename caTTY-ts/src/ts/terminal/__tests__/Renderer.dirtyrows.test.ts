/**
 * Tests for Renderer dirty row optimization.
 * 
 * Verifies that the Renderer only renders rows that have been marked as dirty.
 * 
 * @vitest-environment jsdom
 */

import { describe, it, expect, beforeEach } from 'vitest';
import { Terminal } from '../Terminal.js';
import { Renderer } from '../Renderer.js';
import type { TerminalConfig } from '../Terminal.js';

describe('Renderer Dirty Row Optimization', () => {
  let terminal: Terminal;
  let displayElement: HTMLElement;
  let renderer: Renderer;
  
  const config: TerminalConfig = {
    cols: 80,
    rows: 24,
    scrollback: 100,
  };

  beforeEach(() => {
    terminal = new Terminal(config);
    displayElement = document.createElement('div');
    renderer = new Renderer(displayElement);
  });

  it('should not render when no rows are dirty', () => {
    // Initial render
    renderer.render(terminal);
    
    // Clear dirty rows
    terminal.clearDirtyRows();
    
    // Get initial child count
    const initialChildCount = displayElement.childNodes.length;
    
    // Render again with no dirty rows
    renderer.render(terminal);
    
    // Child count should remain the same (no new elements added)
    expect(displayElement.childNodes.length).toBe(initialChildCount);
  });

  it('should only render dirty rows after writing a character', () => {
    // Write a character on row 0
    terminal.write('A');
    
    // Render
    renderer.render(terminal);
    
    // Verify row 0 was rendered (should have line element)
    const lineElements = displayElement.querySelectorAll('div[style*="position: absolute"]');
    expect(lineElements.length).toBeGreaterThan(0);
    
    // Clear dirty rows
    terminal.clearDirtyRows();
    
    // Write a character on row 5
    terminal.write('\x1b[6;1H'); // Move to row 6, col 1
    terminal.write('B');
    
    // Get dirty rows before render
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.has(5)).toBe(true);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
  });

  it('should render all rows when scrolling', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    
    // Clear dirty rows
    terminal.clearDirtyRows();
    
    // Write one more line to trigger scrolling
    terminal.write('Scroll line\n');
    
    // Verify all rows are dirty
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBe(config.rows);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
  });

  it('should handle multiple dirty rows efficiently', () => {
    // Write on multiple rows
    terminal.write('Row 0');
    terminal.write('\x1b[6;1H'); // Move to row 6
    terminal.write('Row 5');
    terminal.write('\x1b[11;1H'); // Move to row 11
    terminal.write('Row 10');
    
    // Verify multiple rows are dirty
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.has(0)).toBe(true);
    expect(dirtyRows.has(5)).toBe(true);
    expect(dirtyRows.has(10)).toBe(true);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
    
    // Verify content was rendered correctly
    const line0 = terminal.getLine(0);
    expect(line0.cells[0].char).toBe('R');
    
    const line5 = terminal.getLine(5);
    expect(line5.cells[0].char).toBe('R');
    
    const line10 = terminal.getLine(10);
    expect(line10.cells[0].char).toBe('R');
  });

  it('should clear dirty rows after rendering', () => {
    // Write some content
    terminal.write('Hello World');
    
    // Verify rows are dirty
    let dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBeGreaterThan(0);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBe(0);
  });

  it('should handle cursor-only changes efficiently', () => {
    // Initial render
    terminal.write('Hello');
    renderer.render(terminal);
    terminal.clearDirtyRows();
    
    // Move cursor within same row (should mark row as dirty)
    terminal.write('\r');
    
    // Verify only one row is dirty
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
  });

  it('should handle erase operations efficiently', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    renderer.render(terminal);
    
    // Position cursor at row 0 (it's already there after filling)
    terminal.write('\x1b[1;1H'); // Move to row 1, col 1 (which is row 0, 0-based)
    terminal.clearDirtyRows();
    
    // Erase from cursor to end of line
    terminal.write('\x1b[0K'); // Erase to end of line
    
    // Verify only one row is dirty (the row where we erased)
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBe(1);
    expect(dirtyRows.has(0)).toBe(true);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
  });

  it('should handle line insertion efficiently', () => {
    // Fill the screen
    for (let i = 0; i < config.rows; i++) {
      terminal.write(`Line ${i}\n`);
    }
    renderer.render(terminal);
    terminal.clearDirtyRows();
    
    // Insert lines at row 10
    terminal.write('\x1b[11;1H'); // Move to row 11, col 1
    terminal.write('\x1b[3L'); // Insert 3 lines
    
    // Verify affected rows are dirty
    const dirtyRows = terminal.getDirtyRows();
    expect(dirtyRows.size).toBeGreaterThanOrEqual(config.rows - 10);
    
    // Render
    renderer.render(terminal);
    
    // Verify dirty rows were cleared
    const dirtyRowsAfter = terminal.getDirtyRows();
    expect(dirtyRowsAfter.size).toBe(0);
  });

  it('should not render if no rows are dirty and cursor has not moved', () => {
    // Initial render
    terminal.write('Hello');
    renderer.render(terminal);
    
    // Clear dirty rows
    terminal.clearDirtyRows();
    
    // Get initial DOM state
    const initialHTML = displayElement.innerHTML;
    
    // Render again (should be a no-op)
    renderer.render(terminal);
    
    // DOM should be unchanged
    expect(displayElement.innerHTML).toBe(initialHTML);
  });
});
