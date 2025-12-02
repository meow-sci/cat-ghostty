import { describe, it, expect, vi } from 'vitest';
import { CsiHandler } from '../CsiHandler';
import { ScreenBuffer } from '../ScreenBuffer';
import type { CursorState } from '../types';

describe('CsiHandler', () => {
  function createTestSetup() {
    const buffer = new ScreenBuffer(80, 24);
    const cursor: CursorState = { row: 0, col: 0, visible: true, blinking: false };
    const actions = {
      setScrollRegion: vi.fn(),
      setTabStop: vi.fn(),
      clearTabStop: vi.fn(),
      getTabStops: vi.fn(() => new Set([8, 16, 24, 32, 40, 48, 56, 64, 72])),
    };
    const handler = new CsiHandler(buffer, cursor, actions);
    return { buffer, cursor, actions, handler };
  }
  
  describe('Cursor Movement', () => {
    it('should move cursor up (CUU - A)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 10;
      cursor.col = 5;
      
      handler.handle([5], '', 'A'.charCodeAt(0));
      
      expect(cursor.row).toBe(5);
      expect(cursor.col).toBe(5); // Column unchanged
    });
    
    it('should not move cursor above top (CUU - A)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 2;
      
      handler.handle([5], '', 'A'.charCodeAt(0));
      
      expect(cursor.row).toBe(0);
    });
    
    it('should move cursor down (CUD - B)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 5;
      cursor.col = 10;
      
      handler.handle([3], '', 'B'.charCodeAt(0));
      
      expect(cursor.row).toBe(8);
      expect(cursor.col).toBe(10); // Column unchanged
    });
    
    it('should not move cursor below bottom (CUD - B)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 22;
      
      handler.handle([5], '', 'B'.charCodeAt(0));
      
      expect(cursor.row).toBe(23); // Max row for 24-row buffer
    });
    
    it('should move cursor forward (CUF - C)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 5;
      cursor.col = 10;
      
      handler.handle([5], '', 'C'.charCodeAt(0));
      
      expect(cursor.row).toBe(5); // Row unchanged
      expect(cursor.col).toBe(15);
    });
    
    it('should not move cursor past right edge (CUF - C)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.col = 75;
      
      handler.handle([10], '', 'C'.charCodeAt(0));
      
      expect(cursor.col).toBe(79); // Max col for 80-col buffer
    });
    
    it('should move cursor backward (CUB - D)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 5;
      cursor.col = 10;
      
      handler.handle([3], '', 'D'.charCodeAt(0));
      
      expect(cursor.row).toBe(5); // Row unchanged
      expect(cursor.col).toBe(7);
    });
    
    it('should not move cursor past left edge (CUB - D)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.col = 2;
      
      handler.handle([5], '', 'D'.charCodeAt(0));
      
      expect(cursor.col).toBe(0);
    });
    
    it('should move cursor to absolute column (CHA - G)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 5;
      cursor.col = 10;
      
      handler.handle([20], '', 'G'.charCodeAt(0));
      
      expect(cursor.row).toBe(5); // Row unchanged
      expect(cursor.col).toBe(19); // 1-based to 0-based
    });
    
    it('should move cursor to position (CUP - H)', () => {
      const { cursor, handler } = createTestSetup();
      
      handler.handle([10, 20], '', 'H'.charCodeAt(0));
      
      expect(cursor.row).toBe(9); // 1-based to 0-based
      expect(cursor.col).toBe(19); // 1-based to 0-based
    });
    
    it('should handle CUP with default parameters', () => {
      const { cursor, handler } = createTestSetup();
      cursor.row = 10;
      cursor.col = 20;
      
      handler.handle([0], '', 'H'.charCodeAt(0));
      
      expect(cursor.row).toBe(0);
      expect(cursor.col).toBe(0);
    });
  });
  
  describe('Erase Operations', () => {
    it('should erase from cursor to end of display (ED - J, mode 0)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill buffer with 'X'
      for (let row = 0; row < 24; row++) {
        for (let col = 0; col < 80; col++) {
          buffer.setCell(row, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        }
      }
      
      cursor.row = 10;
      cursor.col = 40;
      
      handler.handle([0], '', 'J'.charCodeAt(0));
      
      // Check that cells from cursor to end are cleared
      expect(buffer.getCell(10, 40).char).toBe(' ');
      expect(buffer.getCell(10, 79).char).toBe(' ');
      expect(buffer.getCell(23, 0).char).toBe(' ');
      
      // Check that cells before cursor are unchanged
      expect(buffer.getCell(10, 39).char).toBe('X');
      expect(buffer.getCell(0, 0).char).toBe('X');
    });
    
    it('should erase from start to cursor (ED - J, mode 1)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill buffer with 'X'
      for (let row = 0; row < 24; row++) {
        for (let col = 0; col < 80; col++) {
          buffer.setCell(row, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        }
      }
      
      cursor.row = 10;
      cursor.col = 40;
      
      handler.handle([1], '', 'J'.charCodeAt(0));
      
      // Check that cells from start to cursor are cleared
      expect(buffer.getCell(0, 0).char).toBe(' ');
      expect(buffer.getCell(10, 40).char).toBe(' ');
      
      // Check that cells after cursor are unchanged
      expect(buffer.getCell(10, 41).char).toBe('X');
      expect(buffer.getCell(23, 79).char).toBe('X');
    });
    
    it('should erase entire display (ED - J, mode 2)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill buffer with 'X'
      for (let row = 0; row < 24; row++) {
        for (let col = 0; col < 80; col++) {
          buffer.setCell(row, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        }
      }
      
      handler.handle([2], '', 'J'.charCodeAt(0));
      
      // Check that entire buffer is cleared
      expect(buffer.getCell(0, 0).char).toBe(' ');
      expect(buffer.getCell(23, 79).char).toBe(' ');
    });
    
    it('should erase from cursor to end of line (EL - K, mode 0)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill line with 'X'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(10, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 10;
      cursor.col = 40;
      
      handler.handle([0], '', 'K'.charCodeAt(0));
      
      // Check that cells from cursor to end of line are cleared
      expect(buffer.getCell(10, 40).char).toBe(' ');
      expect(buffer.getCell(10, 79).char).toBe(' ');
      
      // Check that cells before cursor are unchanged
      expect(buffer.getCell(10, 39).char).toBe('X');
    });
    
    it('should erase from start of line to cursor (EL - K, mode 1)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill line with 'X'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(10, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 10;
      cursor.col = 40;
      
      handler.handle([1], '', 'K'.charCodeAt(0));
      
      // Check that cells from start to cursor are cleared
      expect(buffer.getCell(10, 0).char).toBe(' ');
      expect(buffer.getCell(10, 40).char).toBe(' ');
      
      // Check that cells after cursor are unchanged
      expect(buffer.getCell(10, 41).char).toBe('X');
    });
    
    it('should erase entire line (EL - K, mode 2)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill line with 'X'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(10, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 10;
      
      handler.handle([2], '', 'K'.charCodeAt(0));
      
      // Check that entire line is cleared
      expect(buffer.getCell(10, 0).char).toBe(' ');
      expect(buffer.getCell(10, 79).char).toBe(' ');
    });
  });
  
  describe('Scrolling', () => {
    it('should scroll up (SU - S)', () => {
      const { buffer, handler } = createTestSetup();
      
      // Fill first line with 'A', second with 'B'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(0, col, { char: 'A', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        buffer.setCell(1, col, { char: 'B', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      handler.handle([1], '', 'S'.charCodeAt(0));
      
      // First line should now have 'B'
      expect(buffer.getCell(0, 0).char).toBe('B');
      // Second line should be empty
      expect(buffer.getCell(1, 0).char).toBe(' ');
    });
    
    it('should scroll down (SD - T)', () => {
      const { buffer, handler } = createTestSetup();
      
      // Fill first line with 'A'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(0, col, { char: 'A', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      handler.handle([1], '', 'T'.charCodeAt(0));
      
      // First line should now be empty
      expect(buffer.getCell(0, 0).char).toBe(' ');
      // Second line should have 'A'
      expect(buffer.getCell(1, 0).char).toBe('A');
    });
  });
  
  describe('Character and Line Operations', () => {
    it('should insert characters (ICH - @)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill line with 'X'
      for (let col = 0; col < 80; col++) {
        buffer.setCell(10, col, { char: 'X', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 10;
      cursor.col = 5;
      
      handler.handle([3], '', '@'.charCodeAt(0));
      
      // Check that 3 spaces were inserted at cursor position
      expect(buffer.getCell(10, 5).char).toBe(' ');
      expect(buffer.getCell(10, 6).char).toBe(' ');
      expect(buffer.getCell(10, 7).char).toBe(' ');
      // Original content shifted right
      expect(buffer.getCell(10, 8).char).toBe('X');
    });
    
    it('should delete characters (DCH - P)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill line with different characters
      for (let col = 0; col < 10; col++) {
        buffer.setCell(10, col, { char: String(col), width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 10;
      cursor.col = 2;
      
      handler.handle([2], '', 'P'.charCodeAt(0));
      
      // Characters at positions 2 and 3 should be deleted
      // Position 2 should now have character '4'
      expect(buffer.getCell(10, 2).char).toBe('4');
      expect(buffer.getCell(10, 3).char).toBe('5');
    });
    
    it('should insert lines (IL - L)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill rows with identifiable content
      for (let col = 0; col < 80; col++) {
        buffer.setCell(5, col, { char: 'A', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        buffer.setCell(6, col, { char: 'B', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 5;
      
      handler.handle([2], '', 'L'.charCodeAt(0));
      
      // Two blank lines should be inserted at row 5
      expect(buffer.getCell(5, 0).char).toBe(' ');
      expect(buffer.getCell(6, 0).char).toBe(' ');
      // Original content shifted down
      expect(buffer.getCell(7, 0).char).toBe('A');
      expect(buffer.getCell(8, 0).char).toBe('B');
    });
    
    it('should delete lines (DL - M)', () => {
      const { buffer, cursor, handler } = createTestSetup();
      
      // Fill rows with identifiable content
      for (let col = 0; col < 80; col++) {
        buffer.setCell(5, col, { char: 'A', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        buffer.setCell(6, col, { char: 'B', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
        buffer.setCell(7, col, { char: 'C', width: 1, fg: { type: 'default' }, bg: { type: 'default' }, bold: false, italic: false, underline: 0, inverse: false, strikethrough: false });
      }
      
      cursor.row = 5;
      
      handler.handle([2], '', 'M'.charCodeAt(0));
      
      // Two lines should be deleted at row 5
      // Row 5 should now have 'C'
      expect(buffer.getCell(5, 0).char).toBe('C');
      // Rows 6 and 7 should be blank
      expect(buffer.getCell(6, 0).char).toBe(' ');
    });
  });
  
  describe('Scroll Region', () => {
    it('should set scroll region (DECSTBM - r)', () => {
      const { cursor, actions, handler } = createTestSetup();
      
      handler.handle([5, 20], '', 'r'.charCodeAt(0));
      
      expect(actions.setScrollRegion).toHaveBeenCalledWith(4, 19); // 1-based to 0-based
      expect(cursor.row).toBe(0); // Cursor moves to home
      expect(cursor.col).toBe(0);
    });
    
    it('should reset scroll region with no parameters', () => {
      const { cursor, actions, handler } = createTestSetup();
      
      handler.handle([0], '', 'r'.charCodeAt(0));
      
      expect(actions.setScrollRegion).toHaveBeenCalledWith(0, 23);
      expect(cursor.row).toBe(0);
      expect(cursor.col).toBe(0);
    });
  });
  
  describe('Tab Operations', () => {
    it('should move to next tab stop (CHT - I)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.col = 5;
      
      handler.handle([1], '', 'I'.charCodeAt(0));
      
      expect(cursor.col).toBe(8); // Next tab stop
    });
    
    it('should move multiple tab stops forward (CHT - I)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.col = 5;
      
      handler.handle([2], '', 'I'.charCodeAt(0));
      
      expect(cursor.col).toBe(16); // Two tab stops forward
    });
    
    it('should move to previous tab stop (CBT - Z)', () => {
      const { cursor, handler } = createTestSetup();
      cursor.col = 20;
      
      handler.handle([1], '', 'Z'.charCodeAt(0));
      
      expect(cursor.col).toBe(16); // Previous tab stop
    });
    
    it('should clear tab stop at cursor (TBC - g, mode 0)', () => {
      const { cursor, actions, handler } = createTestSetup();
      cursor.col = 8;
      
      handler.handle([0], '', 'g'.charCodeAt(0));
      
      expect(actions.clearTabStop).toHaveBeenCalledWith(8);
    });
    
    it('should clear all tab stops (TBC - g, mode 3)', () => {
      const { actions, handler } = createTestSetup();
      
      handler.handle([3], '', 'g'.charCodeAt(0));
      
      expect(actions.clearTabStop).toHaveBeenCalledWith(-1);
    });
  });
});
