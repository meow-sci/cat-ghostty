/**
 * Terminal - Main terminal emulator class.
 * 
 * This is the primary interface for the headless terminal emulator.
 * It coordinates all terminal components and provides the public API.
 */

import { ScreenBuffer } from './ScreenBuffer.js';
import { ScrollbackBuffer } from './ScrollbackBuffer.js';
import { AlternateScreenManager } from './AlternateScreenManager.js';
import { Parser, type ParserHandlers } from './Parser.js';
import type { OscCommand } from './osc/OscParser.js';
import type { 
  CursorState, 
  Attributes, 
  Line, 
  Cell,
  Color 
} from './types.js';
import { UnderlineStyle } from './types.js';
import type { GhosttyVtInstance } from '../ghostty-vt.js';

/**
 * Configuration for terminal initialization.
 */
export interface TerminalConfig {
  /** Number of columns (width) */
  cols: number;
  
  /** Number of rows (height) */
  rows: number;
  
  /** Maximum scrollback buffer size */
  scrollback: number;
}

/**
 * Event handlers for terminal events.
 */
export interface TerminalEvents {
  /** Bell character received */
  onBell?: () => void;
  
  /** Window title change requested */
  onTitleChange?: (title: string) => void;
  
  /** Clipboard operation requested */
  onClipboard?: (content: string) => void;
  
  /** Data output to be sent to shell */
  onDataOutput?: (data: Uint8Array) => void;
  
  /** Terminal resized */
  onResize?: (cols: number, rows: number) => void;
  
  /** Terminal state changed (for rendering updates) */
  onStateChange?: () => void;
}

/**
 * Terminal modes that affect behavior.
 */
interface TerminalModes {
  /** Auto-wrap mode - wrap cursor to next line at right edge */
  autoWrap: boolean;
  
  /** Cursor visibility mode */
  cursorVisible: boolean;
  
  /** Application cursor keys mode */
  applicationCursorKeys: boolean;
  
  /** Bracketed paste mode */
  bracketedPaste: boolean;
}

/**
 * Scroll region definition.
 */
interface ScrollRegion {
  /** Top row (inclusive, 0-based) */
  top: number;
  
  /** Bottom row (inclusive, 0-based) */
  bottom: number;
}

/**
 * Main terminal emulator class.
 * Provides a complete VT100/xterm-compatible terminal emulation.
 */
export class Terminal {
  private readonly config: TerminalConfig;
  private readonly events: TerminalEvents;
  private readonly wasmInstance?: GhosttyVtInstance;
  
  // Core components
  private readonly screenManager: AlternateScreenManager;
  private readonly scrollback: ScrollbackBuffer;
  private readonly parser: Parser;
  
  // Terminal state
  private viewportOffset: number = 0;
  private modes: TerminalModes;
  private scrollRegion?: ScrollRegion;
  private tabStops: Set<number>;
  
  // Dirty row tracking for optimized rendering
  private dirtyRows: Set<number> = new Set();
  private lastCursorRow: number = 0;
  
  /**
   * Creates a new terminal instance.
   * @param config Terminal configuration
   * @param events Event handlers
   * @param wasmInstance Optional WASM instance for advanced features
   */
  constructor(
    config: TerminalConfig, 
    events: Partial<TerminalEvents> = {},
    wasmInstance?: GhosttyVtInstance
  ) {
    // Validate configuration
    if (config.cols < 1 || config.cols > 1000) {
      throw new Error(`Invalid cols: ${config.cols}. Must be between 1 and 1000.`);
    }
    if (config.rows < 1 || config.rows > 1000) {
      throw new Error(`Invalid rows: ${config.rows}. Must be between 1 and 1000.`);
    }
    if (config.scrollback < 0) {
      throw new Error(`Invalid scrollback: ${config.scrollback}. Must be non-negative.`);
    }
    
    this.config = config;
    this.events = events;
    this.wasmInstance = wasmInstance;
    
    // Initialize core components
    this.screenManager = new AlternateScreenManager(config.cols, config.rows);
    this.scrollback = new ScrollbackBuffer(config.scrollback);
    
    // Initialize terminal modes
    this.modes = {
      autoWrap: true,
      cursorVisible: true,
      applicationCursorKeys: false,
      bracketedPaste: false,
    };
    
    // Initialize tab stops (every 8 columns by default)
    this.tabStops = new Set<number>();
    for (let col = 8; col < config.cols; col += 8) {
      this.tabStops.add(col);
    }
    
    // Create parser with handlers
    const parserHandlers: ParserHandlers = {
      onPrintable: this.handlePrintable.bind(this),
      onLineFeed: this.handleLineFeed.bind(this),
      onCarriageReturn: this.handleCarriageReturn.bind(this),
      onBackspace: this.handleBackspace.bind(this),
      onTab: this.handleTab.bind(this),
      onBell: this.handleBell.bind(this),
      onShiftIn: this.handleShiftIn.bind(this),
      onShiftOut: this.handleShiftOut.bind(this),
      onCsi: this.handleCsi.bind(this),
      onSgrAttributes: this.handleSgrAttributes.bind(this),
      onTitleChange: this.handleTitleChange.bind(this),
      onHyperlink: this.handleHyperlink.bind(this),
      onClipboard: this.handleClipboard.bind(this),
      onCommand: this.handleOscCommand.bind(this),
      onEscape: this.handleEscape.bind(this),
    };
    
    this.parser = new Parser(parserHandlers, wasmInstance);
  }
  
  /**
   * Gets the terminal configuration.
   */
  getConfig(): TerminalConfig {
    return { ...this.config };
  }
  
  /**
   * Gets the current cursor state.
   */
  getCursor(): CursorState {
    return { ...this.screenManager.getCurrentCursor() };
  }
  
  /**
   * Gets a line from the screen buffer.
   * @param row Row index (0-based)
   */
  getLine(row: number): Line {
    return this.screenManager.getCurrentBuffer().getLine(row);
  }
  
  /**
   * Gets a line from the scrollback buffer.
   * @param index Scrollback index (0 = oldest line)
   */
  getScrollbackLine(index: number): Line | undefined {
    return this.scrollback.get(index);
  }
  
  /**
   * Gets the current scrollback buffer size.
   */
  getScrollbackSize(): number {
    return this.scrollback.getSize();
  }
  
  /**
   * Gets the current viewport offset.
   */
  getViewportOffset(): number {
    return this.viewportOffset;
  }
  
  /**
   * Sets the viewport offset for scrollback viewing.
   * @param offset Offset from bottom (0 = bottom, positive = scrolled up)
   */
  setViewportOffset(offset: number): void {
    const maxOffset = this.scrollback.getSize();
    this.viewportOffset = Math.max(0, Math.min(offset, maxOffset));
    this.emitStateChange();
  }
  
  /**
   * Gets the set of rows that have been modified since the last render.
   * Used by the renderer for optimized incremental rendering.
   * @returns Set of dirty row indices
   */
  getDirtyRows(): Set<number> {
    return new Set(this.dirtyRows);
  }
  
  /**
   * Clears the dirty row tracking.
   * Should be called by the renderer after rendering dirty rows.
   */
  clearDirtyRows(): void {
    this.dirtyRows.clear();
  }
  
  // Placeholder methods for parser handlers - will be implemented in subsequent tasks
  
  private handlePrintable(char: string): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    const attributes = this.screenManager.getCurrentAttributes();
    
    // Determine character width
    let charWidth = 1;
    if (char.length > 0) {
      const codePoint = char.codePointAt(0);
      if (codePoint !== undefined) {
        // Check for wide characters (CJK and emojis)
        // This is a simplified check - in practice, you'd use a proper Unicode width library
        if (
          // CJK ranges
          (codePoint >= 0x1100 && codePoint <= 0x115F) || // Hangul Jamo
          (codePoint >= 0x2E80 && codePoint <= 0x2EFF) || // CJK Radicals Supplement
          (codePoint >= 0x2F00 && codePoint <= 0x2FDF) || // Kangxi Radicals
          (codePoint >= 0x3000 && codePoint <= 0x303F) || // CJK Symbols and Punctuation
          (codePoint >= 0x3040 && codePoint <= 0x309F) || // Hiragana
          (codePoint >= 0x30A0 && codePoint <= 0x30FF) || // Katakana
          (codePoint >= 0x3100 && codePoint <= 0x312F) || // Bopomofo
          (codePoint >= 0x3130 && codePoint <= 0x318F) || // Hangul Compatibility Jamo
          (codePoint >= 0x3190 && codePoint <= 0x319F) || // Kanbun
          (codePoint >= 0x31A0 && codePoint <= 0x31BF) || // Bopomofo Extended
          (codePoint >= 0x31C0 && codePoint <= 0x31EF) || // CJK Strokes
          (codePoint >= 0x31F0 && codePoint <= 0x31FF) || // Katakana Phonetic Extensions
          (codePoint >= 0x3200 && codePoint <= 0x32FF) || // Enclosed CJK Letters and Months
          (codePoint >= 0x3300 && codePoint <= 0x33FF) || // CJK Compatibility
          (codePoint >= 0x3400 && codePoint <= 0x4DBF) || // CJK Unified Ideographs Extension A
          (codePoint >= 0x4E00 && codePoint <= 0x9FFF) || // CJK Unified Ideographs
          (codePoint >= 0xA000 && codePoint <= 0xA48F) || // Yi Syllables
          (codePoint >= 0xA490 && codePoint <= 0xA4CF) || // Yi Radicals
          (codePoint >= 0xAC00 && codePoint <= 0xD7AF) || // Hangul Syllables
          (codePoint >= 0xF900 && codePoint <= 0xFAFF) || // CJK Compatibility Ideographs
          (codePoint >= 0xFE10 && codePoint <= 0xFE1F) || // Vertical Forms
          (codePoint >= 0xFE30 && codePoint <= 0xFE4F) || // CJK Compatibility Forms
          (codePoint >= 0xFF00 && codePoint <= 0xFFEF) || // Halfwidth and Fullwidth Forms
          (codePoint >= 0x20000 && codePoint <= 0x2A6DF) || // CJK Unified Ideographs Extension B
          (codePoint >= 0x2A700 && codePoint <= 0x2B73F) || // CJK Unified Ideographs Extension C
          (codePoint >= 0x2B740 && codePoint <= 0x2B81F) || // CJK Unified Ideographs Extension D
          (codePoint >= 0x2B820 && codePoint <= 0x2CEAF) || // CJK Unified Ideographs Extension E
          (codePoint >= 0x2CEB0 && codePoint <= 0x2EBEF) || // CJK Unified Ideographs Extension F
          // Emoji ranges (most emojis are wide)
          (codePoint >= 0x1F000 && codePoint <= 0x1F02F) || // Mahjong Tiles
          (codePoint >= 0x1F0A0 && codePoint <= 0x1F0FF) || // Playing Cards
          (codePoint >= 0x1F100 && codePoint <= 0x1F64F) || // Enclosed Alphanumeric Supplement + Emoticons
          (codePoint >= 0x1F680 && codePoint <= 0x1F6FF) || // Transport and Map Symbols
          (codePoint >= 0x1F700 && codePoint <= 0x1F77F) || // Alchemical Symbols
          (codePoint >= 0x1F780 && codePoint <= 0x1F7FF) || // Geometric Shapes Extended
          (codePoint >= 0x1F800 && codePoint <= 0x1F8FF) || // Supplemental Arrows-C
          (codePoint >= 0x1F900 && codePoint <= 0x1F9FF) || // Supplemental Symbols and Pictographs
          (codePoint >= 0x1FA00 && codePoint <= 0x1FA6F) || // Chess Symbols
          (codePoint >= 0x1FA70 && codePoint <= 0x1FAFF)    // Symbols and Pictographs Extended-A
        ) {
          charWidth = 2;
        }
      }
    }
    
    // Check if character fits on current line
    if (cursor.col + charWidth > this.config.cols) {
      if (this.modes.autoWrap) {
        // Auto-wrap to next line
        this.moveCursorToNextLine();
      } else {
        // Stay at right edge, don't write character
        return;
      }
    }
    
    // Create cell with character and current attributes
    const cell: Cell = {
      char: char,
      width: charWidth,
      fg: attributes.fg,
      bg: attributes.bg,
      bold: attributes.bold,
      italic: attributes.italic,
      underline: attributes.underline,
      inverse: attributes.inverse,
      strikethrough: attributes.strikethrough,
      url: attributes.url,
    };
    
    // Write cell to buffer
    const newCursor = this.screenManager.getCurrentCursor();
    buffer.setCell(newCursor.row, newCursor.col, cell);
    
    // For wide characters, write continuation cell
    if (charWidth === 2 && newCursor.col + 1 < this.config.cols) {
      const continuationCell: Cell = {
        char: '',
        width: 0,
        fg: attributes.fg,
        bg: attributes.bg,
        bold: attributes.bold,
        italic: attributes.italic,
        underline: attributes.underline,
        inverse: attributes.inverse,
        strikethrough: attributes.strikethrough,
        url: attributes.url,
      };
      buffer.setCell(newCursor.row, newCursor.col + 1, continuationCell);
    }
    
    // Advance cursor
    this.advanceCursor(charWidth);
    
    // Mark row as dirty for rendering
    this.dirtyRows.add(newCursor.row);
    
    this.emitStateChange();
  }
  
  /**
   * Advances the cursor by the specified number of columns.
   * In auto-wrap mode, cursor can go past the right edge and will wrap on next character.
   */
  private advanceCursor(columns: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    cursor.col += columns;
    this.screenManager.setCurrentCursor(cursor);
  }
  
  /**
   * Moves cursor to the beginning of the next line.
   * Handles scrolling if at bottom of screen.
   */
  private moveCursorToNextLine(): void {
    const cursor = this.screenManager.getCurrentCursor();
    cursor.col = 0;
    cursor.row++;
    
    // Handle scrolling if we're at the bottom
    if (cursor.row >= this.config.rows) {
      this.scrollUp(1);
      cursor.row = this.config.rows - 1;
    }
    
    this.screenManager.setCurrentCursor(cursor);
  }
  
  /**
   * Scrolls the screen up by the specified number of lines.
   * Lines scrolled off the top are added to scrollback (unless in alternate screen).
   */
  private scrollUp(lines: number): void {
    const buffer = this.screenManager.getCurrentBuffer();
    
    // Save lines to scrollback if in primary screen
    if (!this.screenManager.isAlternateScreen()) {
      for (let i = 0; i < lines; i++) {
        const topLine = buffer.getLine(0);
        this.scrollback.push(topLine);
      }
    }
    
    // Scroll the buffer
    const region = this.scrollRegion;
    buffer.scrollUp(lines, region);
    
    // Mark all visible rows as dirty (scrolling affects entire screen)
    for (let row = 0; row < this.config.rows; row++) {
      this.dirtyRows.add(row);
    }
  }
  
  private handleLineFeed(): void {
    const cursor = this.screenManager.getCurrentCursor();
    const oldRow = cursor.row;
    cursor.row++;
    
    // Handle scrolling if we're at the bottom
    if (cursor.row >= this.config.rows) {
      this.scrollUp(1);
      cursor.row = this.config.rows - 1;
    }
    
    // Mark both old and new cursor rows as dirty
    this.dirtyRows.add(oldRow);
    this.dirtyRows.add(cursor.row);
    
    this.screenManager.setCurrentCursor(cursor);
    this.emitStateChange();
  }
  
  private handleCarriageReturn(): void {
    const cursor = this.screenManager.getCurrentCursor();
    cursor.col = 0;
    this.screenManager.setCurrentCursor(cursor);
    // Mark current row as dirty (cursor moved within row)
    this.dirtyRows.add(cursor.row);
    this.emitStateChange();
  }
  
  private handleBackspace(): void {
    const cursor = this.screenManager.getCurrentCursor();
    if (cursor.col > 0) {
      cursor.col--;
      this.screenManager.setCurrentCursor(cursor);
      // Mark current row as dirty (cursor moved within row)
      this.dirtyRows.add(cursor.row);
    }
    this.emitStateChange();
  }
  
  private handleTab(): void {
    const cursor = this.screenManager.getCurrentCursor();
    
    // Find next tab stop
    let nextTabStop = cursor.col + 1;
    
    // Look for the next tab stop
    while (nextTabStop < this.config.cols && !this.tabStops.has(nextTabStop)) {
      nextTabStop++;
    }
    
    // If no tab stop found, go to end of line
    if (nextTabStop >= this.config.cols) {
      nextTabStop = this.config.cols - 1; // Stay at right edge
    }
    
    // Move cursor to tab stop
    cursor.col = nextTabStop;
    this.screenManager.setCurrentCursor(cursor);
    // Mark current row as dirty (cursor moved within row)
    this.dirtyRows.add(cursor.row);
    this.emitStateChange();
  }
  
  private handleBell(): void {
    this.events.onBell?.();
  }
  
  private handleShiftIn(): void {
    // Character set handling - delegated to parser
    this.emitStateChange();
  }
  
  private handleShiftOut(): void {
    // Character set handling - delegated to parser
    this.emitStateChange();
  }
  
  private handleCsi(params: number[], intermediates: string, final: number): void {
    // Ensure at least one parameter (default to 1 for most sequences, 0 for some)
    const param1 = params.length > 0 ? Math.max(1, params[0]) : 1;
    const param2 = params.length > 1 ? Math.max(1, params[1]) : 1;
    
    switch (final) {
      case 0x41: // A - Cursor Up
        this.moveCursor(-param1, 0);
        break;
        
      case 0x42: // B - Cursor Down
        this.moveCursor(param1, 0);
        break;
        
      case 0x43: // C - Cursor Forward (Right)
        this.moveCursor(0, param1);
        break;
        
      case 0x44: // D - Cursor Backward (Left)
        this.moveCursor(0, -param1);
        break;
        
      case 0x48: // H - Cursor Position or Set Tab Stop
        if (params.length === 0) {
          // No parameters - this is a tab stop set (should be ESC H, not CSI H)
          // CSI H with no params is cursor home
          this.setCursorPosition(0, 0);
        } else {
          // Parameters are 1-based, convert to 0-based
          const row = (params.length > 0 ? Math.max(1, params[0]) : 1) - 1;
          const col = (params.length > 1 ? Math.max(1, params[1]) : 1) - 1;
          this.setCursorPosition(row, col);
        }
        break;
        
      case 0x66: // f - Horizontal and Vertical Position (same as H)
        // Parameters are 1-based, convert to 0-based
        const row = (params.length > 0 ? Math.max(1, params[0]) : 1) - 1;
        const col = (params.length > 1 ? Math.max(1, params[1]) : 1) - 1;
        this.setCursorPosition(row, col);
        break;
        
      case 0x4A: // J - Erase in Display
        this.eraseInDisplay(params.length > 0 ? params[0] : 0);
        break;
        
      case 0x4B: // K - Erase in Line
        this.eraseInLine(params.length > 0 ? params[0] : 0);
        break;
        
      case 0x53: // S - Scroll Up
        this.scrollUp(param1);
        break;
        
      case 0x54: // T - Scroll Down
        this.scrollDown(param1);
        break;
        
      case 0x72: // r - Set Scroll Region
        if (params.length >= 2) {
          const top = Math.max(1, params[0]) - 1; // Convert to 0-based
          const bottom = Math.max(1, params[1]) - 1; // Convert to 0-based
          this.setScrollRegion(top, bottom);
        } else {
          // Reset scroll region
          this.scrollRegion = undefined;
        }
        break;
        
      case 0x40: // @ - Insert Character
        this.insertCharacters(param1);
        break;
        
      case 0x50: // P - Delete Character
        this.deleteCharacters(param1);
        break;
        
      case 0x4C: // L - Insert Line
        this.insertLines(param1);
        break;
        
      case 0x4D: // M - Delete Line
        this.deleteLines(param1);
        break;
        
      case 0x68: // h - Set Mode
        if (intermediates === '?') {
          // DEC Private Mode Set
          params.forEach(param => {
            this.setMode(param.toString(), true);
          });
        } else {
          // ANSI Mode Set
          params.forEach(param => {
            this.setMode(param.toString(), true);
          });
        }
        break;
        
      case 0x6C: // l - Reset Mode
        if (intermediates === '?') {
          // DEC Private Mode Reset
          params.forEach(param => {
            this.setMode(param.toString(), false);
          });
        } else {
          // ANSI Mode Reset
          params.forEach(param => {
            this.setMode(param.toString(), false);
          });
        }
        break;
        

        
      case 0x67: // g - Tab Clear
        this.clearTabStop(params.length > 0 ? params[0] : 0);
        break;
        
      // Note: SGR (m) is handled separately in the parser
      
      default:
        // Unknown CSI sequence - ignore
        break;
    }
    
    this.emitStateChange();
  }
  
  /**
   * Moves the cursor by the specified offset.
   * @param deltaRow Row offset (positive = down, negative = up)
   * @param deltaCol Column offset (positive = right, negative = left)
   */
  private moveCursor(deltaRow: number, deltaCol: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const oldRow = cursor.row;
    const newRow = Math.max(0, Math.min(cursor.row + deltaRow, this.config.rows - 1));
    const newCol = Math.max(0, Math.min(cursor.col + deltaCol, this.config.cols - 1));
    
    cursor.row = newRow;
    cursor.col = newCol;
    this.screenManager.setCurrentCursor(cursor);
    
    // Mark both old and new cursor rows as dirty if cursor moved between rows
    if (oldRow !== newRow) {
      this.dirtyRows.add(oldRow);
      this.dirtyRows.add(newRow);
    }
  }
  
  /**
   * Sets the cursor to an absolute position.
   * @param row Target row (0-based)
   * @param col Target column (0-based)
   */
  private setCursorPosition(row: number, col: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const oldRow = cursor.row;
    cursor.row = Math.max(0, Math.min(row, this.config.rows - 1));
    cursor.col = Math.max(0, Math.min(col, this.config.cols - 1));
    this.screenManager.setCurrentCursor(cursor);
    
    // Mark both old and new cursor rows as dirty if cursor moved between rows
    if (oldRow !== cursor.row) {
      this.dirtyRows.add(oldRow);
      this.dirtyRows.add(cursor.row);
    }
  }
  
  /**
   * Erases portions of the display.
   * @param param 0 = cursor to end, 1 = start to cursor, 2 = entire screen
   */
  private eraseInDisplay(param: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    
    switch (param) {
      case 0: // Cursor to end of screen
        // Clear from cursor to end of current line
        buffer.clearRegion(cursor.row, cursor.row, cursor.col, this.config.cols - 1);
        // Clear all lines below cursor
        if (cursor.row + 1 < this.config.rows) {
          buffer.clearRegion(cursor.row + 1, this.config.rows - 1, 0, this.config.cols - 1);
        }
        // Mark affected rows as dirty
        for (let row = cursor.row; row < this.config.rows; row++) {
          this.dirtyRows.add(row);
        }
        break;
        
      case 1: // Start of screen to cursor
        // Clear all lines above cursor
        if (cursor.row > 0) {
          buffer.clearRegion(0, cursor.row - 1, 0, this.config.cols - 1);
        }
        // Clear from start of current line to cursor
        buffer.clearRegion(cursor.row, cursor.row, 0, cursor.col);
        // Mark affected rows as dirty
        for (let row = 0; row <= cursor.row; row++) {
          this.dirtyRows.add(row);
        }
        break;
        
      case 2: // Entire screen
        buffer.clear();
        // Mark all rows as dirty
        for (let row = 0; row < this.config.rows; row++) {
          this.dirtyRows.add(row);
        }
        break;
    }
  }
  
  /**
   * Erases portions of the current line.
   * @param param 0 = cursor to end, 1 = start to cursor, 2 = entire line
   */
  private eraseInLine(param: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    
    switch (param) {
      case 0: // Cursor to end of line
        buffer.clearRegion(cursor.row, cursor.row, cursor.col, this.config.cols - 1);
        break;
        
      case 1: // Start of line to cursor
        buffer.clearRegion(cursor.row, cursor.row, 0, cursor.col);
        break;
        
      case 2: // Entire line
        buffer.clearRegion(cursor.row, cursor.row, 0, this.config.cols - 1);
        break;
    }
    
    // Mark current row as dirty
    this.dirtyRows.add(cursor.row);
  }
  
  /**
   * Scrolls the screen down by the specified number of lines.
   */
  private scrollDown(lines: number): void {
    const buffer = this.screenManager.getCurrentBuffer();
    const region = this.scrollRegion;
    buffer.scrollDown(lines, region);
    
    // Mark all visible rows as dirty (scrolling affects entire screen)
    for (let row = 0; row < this.config.rows; row++) {
      this.dirtyRows.add(row);
    }
  }
  
  /**
   * Sets the scroll region.
   * @param top Top row (0-based, inclusive)
   * @param bottom Bottom row (0-based, inclusive)
   */
  private setScrollRegion(top: number, bottom: number): void {
    // Validate parameters
    if (top >= 0 && bottom < this.config.rows && top <= bottom) {
      this.scrollRegion = { top, bottom };
    }
  }
  
  /**
   * Inserts blank characters at the cursor position.
   * @param count Number of characters to insert
   */
  private insertCharacters(count: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    buffer.insertCells(cursor.row, cursor.col, count);
    
    // Mark current row as dirty
    this.dirtyRows.add(cursor.row);
  }
  
  /**
   * Deletes characters at the cursor position.
   * @param count Number of characters to delete
   */
  private deleteCharacters(count: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    buffer.deleteCells(cursor.row, cursor.col, count);
    
    // Mark current row as dirty
    this.dirtyRows.add(cursor.row);
  }
  
  /**
   * Inserts blank lines at the cursor row.
   * @param count Number of lines to insert
   */
  private insertLines(count: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    buffer.insertLines(cursor.row, count);
    
    // Mark affected rows as dirty (from cursor row to end of screen)
    for (let row = cursor.row; row < this.config.rows; row++) {
      this.dirtyRows.add(row);
    }
  }
  
  /**
   * Deletes lines at the cursor row.
   * @param count Number of lines to delete
   */
  private deleteLines(count: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    const buffer = this.screenManager.getCurrentBuffer();
    buffer.deleteLines(cursor.row, count);
    
    // Mark affected rows as dirty (from cursor row to end of screen)
    for (let row = cursor.row; row < this.config.rows; row++) {
      this.dirtyRows.add(row);
    }
  }
  
  private handleSgrAttributes(attributes: Attributes): void {
    // TODO: Implement SGR attribute handling
    this.screenManager.setCurrentAttributes(attributes);
    this.emitStateChange();
  }
  
  private handleTitleChange(title: string): void {
    this.events.onTitleChange?.(title);
  }
  
  private handleHyperlink(url: string): void {
    // TODO: Implement hyperlink handling
    const attributes = this.screenManager.getCurrentAttributes();
    attributes.url = url || undefined;
    this.screenManager.setCurrentAttributes(attributes);
    this.emitStateChange();
  }
  
  private handleClipboard(content: string): void {
    this.events.onClipboard?.(content);
  }
  
  private handleOscCommand(command: OscCommand): void {
    // Additional OSC command handling if needed
    // The specific OSC commands (title, hyperlink, clipboard) are already
    // handled by their dedicated callbacks (onTitleChange, onHyperlink, onClipboard)
    // This is a catch-all for any other OSC commands
    this.emitStateChange();
  }
  
  private handleEscape(intermediates: string, final: number): void {
    // Handle escape sequences that aren't CSI or OSC
    switch (final) {
      case 0x37: // 7 - Save Cursor (DECSC)
        this.saveCursor();
        break;
        
      case 0x38: // 8 - Restore Cursor (DECRC)
        this.restoreCursor();
        break;
        
      case 0x44: // D - Index (move cursor down, scroll if at bottom)
        this.indexDown();
        break;
        
      case 0x4D: // M - Reverse Index (move cursor up, scroll if at top)
        this.reverseIndex();
        break;
        
      case 0x45: // E - Next Line (CR + LF)
        this.handleCarriageReturn();
        this.handleLineFeed();
        break;
        
      case 0x48: // H - Set Tab Stop (ESC H)
        this.setTabStop();
        break;
        
      default:
        // Unknown escape sequence - ignore
        break;
    }
    
    this.emitStateChange();
  }
  
  /**
   * Sets a terminal mode.
   * @param mode Mode identifier
   * @param enabled Whether to enable or disable the mode
   */
  setMode(mode: string, enabled: boolean): void {
    switch (mode) {
      case 'DECAWM': // Auto-wrap mode
      case '7':
        this.modes.autoWrap = enabled;
        break;
        
      case 'DECTCEM': // Cursor visibility
      case '25':
        this.modes.cursorVisible = enabled;
        const cursor = this.screenManager.getCurrentCursor();
        cursor.visible = enabled;
        this.screenManager.setCurrentCursor(cursor);
        break;
        
      case 'DECCKM': // Application cursor keys
      case '1':
        this.modes.applicationCursorKeys = enabled;
        break;
        
      case '2004': // Bracketed paste mode
        this.modes.bracketedPaste = enabled;
        break;
        
      case '47': // Alternate screen (simple)
      case '1047': // Alternate screen (with clear)
      case '1049': // Alternate screen (with cursor save/restore)
        if (enabled) {
          this.screenManager.switchToAlternate();
          // Mark all rows as dirty since we switched screens
          for (let row = 0; row < this.config.rows; row++) {
            this.dirtyRows.add(row);
          }
        } else {
          this.screenManager.switchToPrimary();
          // Mark all rows as dirty since we switched screens
          for (let row = 0; row < this.config.rows; row++) {
            this.dirtyRows.add(row);
          }
        }
        break;
    }
    
    this.emitStateChange();
  }
  
  /**
   * Gets the current state of a terminal mode.
   * @param mode Mode identifier
   */
  getMode(mode: string): boolean {
    switch (mode) {
      case 'DECAWM':
      case '7':
        return this.modes.autoWrap;
        
      case 'DECTCEM':
      case '25':
        return this.modes.cursorVisible;
        
      case 'DECCKM':
      case '1':
        return this.modes.applicationCursorKeys;
        
      case '2004':
        return this.modes.bracketedPaste;
        
      default:
        return false;
    }
  }
  
  /**
   * Saves the current cursor position and attributes.
   */
  private saveCursor(): void {
    // This would be implemented with saved cursor state in AlternateScreenManager
    // For now, we'll just track that the functionality exists
  }
  
  /**
   * Restores the saved cursor position and attributes.
   */
  private restoreCursor(): void {
    // This would be implemented with saved cursor state in AlternateScreenManager
    // For now, we'll just track that the functionality exists
  }
  
  /**
   * Moves cursor down one line, scrolling if at bottom.
   */
  private indexDown(): void {
    const cursor = this.screenManager.getCurrentCursor();
    if (cursor.row >= this.config.rows - 1) {
      // At bottom, scroll up
      this.scrollUp(1);
    } else {
      // Move down
      cursor.row++;
      this.screenManager.setCurrentCursor(cursor);
    }
  }
  
  /**
   * Moves cursor up one line, scrolling if at top.
   */
  private reverseIndex(): void {
    const cursor = this.screenManager.getCurrentCursor();
    if (cursor.row <= 0) {
      // At top, scroll down
      this.scrollDown(1);
    } else {
      // Move up
      cursor.row--;
      this.screenManager.setCurrentCursor(cursor);
    }
  }
  
  /**
   * Sets a tab stop at the current cursor column.
   */
  private setTabStop(): void {
    const cursor = this.screenManager.getCurrentCursor();
    this.tabStops.add(cursor.col);
  }
  
  /**
   * Clears tab stops according to the parameter.
   * @param param 0 = clear at cursor, 3 = clear all
   */
  private clearTabStop(param: number): void {
    const cursor = this.screenManager.getCurrentCursor();
    
    switch (param) {
      case 0: // Clear tab stop at cursor position
        this.tabStops.delete(cursor.col);
        break;
        
      case 3: // Clear all tab stops
        this.tabStops.clear();
        break;
        
      default:
        // Other values are ignored
        break;
    }
  }
  
  /**
   * Gets the current tab stops (for testing).
   */
  getTabStops(): Set<number> {
    return new Set(this.tabStops);
  }
  
  /**
   * Emits a state change event to notify listeners.
   */
  private emitStateChange(): void {
    this.events.onStateChange?.();
  }
  
  /**
   * Emits a data output event for sending data to the shell.
   * @param data Data to send to the shell
   */
  private emitDataOutput(data: string | Uint8Array): void {
    let bytes: Uint8Array;
    
    if (typeof data === 'string') {
      const encoder = new TextEncoder();
      bytes = encoder.encode(data);
    } else {
      bytes = data;
    }
    
    this.events.onDataOutput?.(bytes);
  }
  
  /**
   * Sends user input data to the shell backend.
   * This is used for keyboard input and other user-generated data.
   * @param data String or byte array to send to the shell
   */
  sendInput(data: string | Uint8Array): void {
    this.emitDataOutput(data);
  }
  
  /**
   * Writes data to the terminal.
   * Accepts either string or Uint8Array input and processes it through the parser.
   * @param data String or byte array to write to the terminal
   */
  write(data: string | Uint8Array): void {
    let bytes: Uint8Array;
    
    if (typeof data === 'string') {
      // Convert string to UTF-8 bytes
      const encoder = new TextEncoder();
      bytes = encoder.encode(data);
    } else {
      // Use byte array directly
      bytes = data;
    }
    
    // Process through parser
    this.parser.parse(bytes);
    
    // Auto-scroll to bottom if new content is written while scrolled
    // This implements the auto-scroll behavior for viewport management
    if (this.viewportOffset > 0) {
      // User was viewing scrollback, but new content arrived
      // Auto-scroll to bottom to show new content
      this.viewportOffset = 0;
    }
  }
  
  /**
   * Resizes the terminal.
   * @param cols New number of columns
   * @param rows New number of rows
   */
  resize(cols: number, rows: number): void {
    // Validate new dimensions
    if (cols < 1 || cols > 1000) {
      throw new Error(`Invalid cols: ${cols}. Must be between 1 and 1000.`);
    }
    if (rows < 1 || rows > 1000) {
      throw new Error(`Invalid rows: ${rows}. Must be between 1 and 1000.`);
    }
    
    // Resize screen buffers
    this.screenManager.resize(cols, rows);
    
    // Update configuration
    (this.config as any).cols = cols;
    (this.config as any).rows = rows;
    
    // Recalculate tab stops for new width
    this.tabStops.clear();
    for (let col = 8; col < cols; col += 8) {
      this.tabStops.add(col);
    }
    
    // Reset scroll region if it's now invalid
    if (this.scrollRegion && this.scrollRegion.bottom >= rows) {
      this.scrollRegion = undefined;
    }
    
    // Emit resize event
    this.events.onResize?.(cols, rows);
    this.emitStateChange();
  }
  
  /**
   * Disposes of the terminal and cleans up resources.
   */
  dispose(): void {
    this.parser.dispose();
    
    // Clear all buffers
    this.scrollback.clear();
    this.screenManager.getPrimaryBuffer().clear();
    this.screenManager.getAlternateBuffer().clear();
    
    // Reset to primary screen
    this.screenManager.switchToPrimary();
    
    // Reset cursor to default position
    const cursor = this.screenManager.getCurrentCursor();
    cursor.row = 0;
    cursor.col = 0;
    cursor.visible = true;
    cursor.blinking = false;
    this.screenManager.setCurrentCursor(cursor);
    
    // Reset attributes to default
    const defaultAttrs = {
      fg: { type: 'default' as const },
      bg: { type: 'default' as const },
      bold: false,
      italic: false,
      underline: UnderlineStyle.None,
      inverse: false,
      strikethrough: false,
    };
    this.screenManager.setCurrentAttributes(defaultAttrs);
  }
}