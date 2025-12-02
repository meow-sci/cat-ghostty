/**
 * CSI (Control Sequence Introducer) handler for terminal escape sequences.
 * Processes CSI sequences and performs the corresponding terminal actions.
 */

import type { ScreenBuffer } from './ScreenBuffer.js';
import type { CursorState } from './types.js';

/**
 * Interface for CSI handler actions.
 * These callbacks are invoked when CSI sequences require actions beyond buffer manipulation.
 */
export interface CsiHandlerActions {
  /** Set scroll region */
  setScrollRegion?: (top: number, bottom: number) => void;
  
  /** Set tab stop at current cursor position */
  setTabStop?: (col: number) => void;
  
  /** Clear tab stop(s) */
  clearTabStop?: (mode: number) => void;
  
  /** Get current tab stops */
  getTabStops?: () => Set<number>;
}

/**
 * Handles CSI (Control Sequence Introducer) sequences.
 * Implements cursor movement, erase operations, scrolling, and more.
 */
export class CsiHandler {
  private buffer: ScreenBuffer;
  private cursor: CursorState;
  private actions: CsiHandlerActions;
  private scrollRegion: { top: number; bottom: number } | null = null;
  
  constructor(
    buffer: ScreenBuffer,
    cursor: CursorState,
    actions: CsiHandlerActions = {}
  ) {
    this.buffer = buffer;
    this.cursor = cursor;
    this.actions = actions;
  }
  
  /**
   * Update the buffer and cursor references.
   * Used when switching between alternate screens.
   */
  updateReferences(buffer: ScreenBuffer, cursor: CursorState): void {
    this.buffer = buffer;
    this.cursor = cursor;
  }
  
  /**
   * Set the scroll region.
   */
  setScrollRegion(top: number, bottom: number): void {
    this.scrollRegion = { top, bottom };
  }
  
  /**
   * Get the current scroll region.
   */
  getScrollRegion(): { top: number; bottom: number } | null {
    return this.scrollRegion;
  }
  
  /**
   * Handle a CSI sequence.
   * @param params Array of numeric parameters
   * @param intermediates Intermediate characters
   * @param final Final character code
   */
  handle(params: number[], intermediates: string, final: number): void {
    // Ignore private sequences (those with intermediates starting with ?, >, etc.)
    if (intermediates.length > 0 && (intermediates[0] === '?' || intermediates[0] === '>')) {
      // These are private/DEC sequences - ignore for now
      return;
    }
    
    const finalChar = String.fromCharCode(final);
    
    switch (finalChar) {
      // Cursor movement
      case 'A': // CUU - Cursor Up
        this.cursorUp(params[0] || 1);
        break;
        
      case 'B': // CUD - Cursor Down
        this.cursorDown(params[0] || 1);
        break;
        
      case 'C': // CUF - Cursor Forward
        this.cursorForward(params[0] || 1);
        break;
        
      case 'D': // CUB - Cursor Backward
        this.cursorBackward(params[0] || 1);
        break;
        
      case 'E': // CNL - Cursor Next Line
        this.cursorNextLine(params[0] || 1);
        break;
        
      case 'F': // CPL - Cursor Previous Line
        this.cursorPreviousLine(params[0] || 1);
        break;
        
      case 'G': // CHA - Cursor Horizontal Absolute
        this.cursorHorizontalAbsolute(params[0] || 1);
        break;
        
      case 'H': // CUP - Cursor Position
      case 'f': // HVP - Horizontal and Vertical Position
        this.cursorPosition(params[0] || 1, params[1] || 1);
        break;
        
      // Erase operations
      case 'J': // ED - Erase in Display
        this.eraseInDisplay(params[0] || 0);
        break;
        
      case 'K': // EL - Erase in Line
        this.eraseInLine(params[0] || 0);
        break;
        
      // Scrolling
      case 'S': // SU - Scroll Up
        this.scrollUp(params[0] || 1);
        break;
        
      case 'T': // SD - Scroll Down
        this.scrollDown(params[0] || 1);
        break;
        
      // Character/line insertion and deletion
      case '@': // ICH - Insert Character
        this.insertCharacter(params[0] || 1);
        break;
        
      case 'P': // DCH - Delete Character
        this.deleteCharacter(params[0] || 1);
        break;
        
      case 'L': // IL - Insert Line
        this.insertLine(params[0] || 1);
        break;
        
      case 'M': // DL - Delete Line
        this.deleteLine(params[0] || 1);
        break;
        
      // Scroll region
      case 'r': // DECSTBM - Set Top and Bottom Margins
        this.setTopAndBottomMargins(params[0] || 1, params[1] || this.buffer.getRows());
        break;
        
      // Tab operations
      case 'I': // CHT - Cursor Horizontal Tab
        this.cursorHorizontalTab(params[0] || 1);
        break;
        
      case 'Z': // CBT - Cursor Backward Tab
        this.cursorBackwardTab(params[0] || 1);
        break;
        
      case 'g': // TBC - Tab Clear
        this.tabClear(params[0] || 0);
        break;
        
      default:
        // Unknown CSI sequence - ignore
        break;
    }
  }
  
  // Cursor movement methods
  
  private cursorUp(n: number): void {
    this.cursor.row = Math.max(0, this.cursor.row - n);
  }
  
  private cursorDown(n: number): void {
    this.cursor.row = Math.min(this.buffer.getRows() - 1, this.cursor.row + n);
  }
  
  private cursorForward(n: number): void {
    this.cursor.col = Math.min(this.buffer.getCols() - 1, this.cursor.col + n);
  }
  
  private cursorBackward(n: number): void {
    this.cursor.col = Math.max(0, this.cursor.col - n);
  }
  
  private cursorNextLine(n: number): void {
    this.cursor.row = Math.min(this.buffer.getRows() - 1, this.cursor.row + n);
    this.cursor.col = 0;
  }
  
  private cursorPreviousLine(n: number): void {
    this.cursor.row = Math.max(0, this.cursor.row - n);
    this.cursor.col = 0;
  }
  
  private cursorHorizontalAbsolute(col: number): void {
    // CSI parameters are 1-based, convert to 0-based
    this.cursor.col = Math.max(0, Math.min(this.buffer.getCols() - 1, col - 1));
  }
  
  private cursorPosition(row: number, col: number): void {
    // CSI parameters are 1-based, convert to 0-based
    this.cursor.row = Math.max(0, Math.min(this.buffer.getRows() - 1, row - 1));
    this.cursor.col = Math.max(0, Math.min(this.buffer.getCols() - 1, col - 1));
  }
  
  // Erase operations
  
  private eraseInDisplay(mode: number): void {
    const rows = this.buffer.getRows();
    const cols = this.buffer.getCols();
    
    switch (mode) {
      case 0: // Erase from cursor to end of display
        this.buffer.clearRegion(this.cursor.row, this.cursor.row, this.cursor.col, cols - 1);
        if (this.cursor.row < rows - 1) {
          this.buffer.clearRegion(this.cursor.row + 1, rows - 1, 0, cols - 1);
        }
        break;
        
      case 1: // Erase from start of display to cursor
        if (this.cursor.row > 0) {
          this.buffer.clearRegion(0, this.cursor.row - 1, 0, cols - 1);
        }
        this.buffer.clearRegion(this.cursor.row, this.cursor.row, 0, this.cursor.col);
        break;
        
      case 2: // Erase entire display
      case 3: // Erase entire display including scrollback (treat as 2 for now)
        this.buffer.clear();
        break;
    }
  }
  
  private eraseInLine(mode: number): void {
    const cols = this.buffer.getCols();
    
    switch (mode) {
      case 0: // Erase from cursor to end of line
        this.buffer.clearRegion(this.cursor.row, this.cursor.row, this.cursor.col, cols - 1);
        break;
        
      case 1: // Erase from start of line to cursor
        this.buffer.clearRegion(this.cursor.row, this.cursor.row, 0, this.cursor.col);
        break;
        
      case 2: // Erase entire line
        this.buffer.clearRegion(this.cursor.row, this.cursor.row, 0, cols - 1);
        break;
    }
  }
  
  // Scrolling operations
  
  private scrollUp(n: number): void {
    this.buffer.scrollUp(n, this.scrollRegion || undefined);
  }
  
  private scrollDown(n: number): void {
    this.buffer.scrollDown(n, this.scrollRegion || undefined);
  }
  
  // Character and line insertion/deletion
  
  private insertCharacter(n: number): void {
    this.buffer.insertCells(this.cursor.row, this.cursor.col, n);
  }
  
  private deleteCharacter(n: number): void {
    this.buffer.deleteCells(this.cursor.row, this.cursor.col, n);
  }
  
  private insertLine(n: number): void {
    this.buffer.insertLines(this.cursor.row, n);
  }
  
  private deleteLine(n: number): void {
    this.buffer.deleteLines(this.cursor.row, n);
  }
  
  // Scroll region
  
  private setTopAndBottomMargins(top: number, bottom: number): void {
    // Parameters are 1-based, convert to 0-based
    const topRow = Math.max(0, top - 1);
    const bottomRow = Math.min(this.buffer.getRows() - 1, bottom - 1);
    
    if (topRow < bottomRow) {
      this.scrollRegion = { top: topRow, bottom: bottomRow };
      this.actions.setScrollRegion?.(topRow, bottomRow);
    } else {
      // Invalid region - reset to full screen
      this.scrollRegion = null;
      this.actions.setScrollRegion?.(0, this.buffer.getRows() - 1);
    }
    
    // Move cursor to home position (top-left of screen, not scroll region)
    this.cursor.row = 0;
    this.cursor.col = 0;
  }
  
  // Tab operations
  
  private cursorHorizontalTab(n: number): void {
    const tabStops = this.actions.getTabStops?.() || new Set<number>();
    const cols = this.buffer.getCols();
    
    let currentCol = this.cursor.col;
    for (let i = 0; i < n; i++) {
      // Find next tab stop
      let found = false;
      for (let col = currentCol + 1; col < cols; col++) {
        if (tabStops.has(col)) {
          currentCol = col;
          found = true;
          break;
        }
      }
      
      if (!found) {
        // No more tab stops, go to end of line
        currentCol = cols - 1;
        break;
      }
    }
    
    this.cursor.col = currentCol;
  }
  
  private cursorBackwardTab(n: number): void {
    const tabStops = this.actions.getTabStops?.() || new Set<number>();
    
    let currentCol = this.cursor.col;
    for (let i = 0; i < n; i++) {
      // Find previous tab stop
      let found = false;
      for (let col = currentCol - 1; col >= 0; col--) {
        if (tabStops.has(col)) {
          currentCol = col;
          found = true;
          break;
        }
      }
      
      if (!found) {
        // No more tab stops, go to start of line
        currentCol = 0;
        break;
      }
    }
    
    this.cursor.col = currentCol;
  }
  
  private tabClear(mode: number): void {
    switch (mode) {
      case 0: // Clear tab stop at current position
        this.actions.clearTabStop?.(this.cursor.col);
        break;
        
      case 3: // Clear all tab stops
        this.actions.clearTabStop?.(-1);
        break;
    }
  }
}
