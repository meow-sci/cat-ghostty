/**
 * AlternateScreenManager manages dual screen buffers for the terminal.
 * It handles switching between primary and alternate screen buffers,
 * which is used by full-screen applications like vim and less.
 */

import { ScreenBuffer } from './ScreenBuffer.js';
import type { CursorState, Attributes } from './types.js';
import { UnderlineStyle } from './types.js';

/**
 * Complete state for a single screen (buffer + cursor + attributes).
 */
interface ScreenState {
  buffer: ScreenBuffer;
  cursor: CursorState;
  attributes: Attributes;
  savedCursor?: CursorState;
}

/**
 * Creates default cursor state at origin.
 */
function createDefaultCursor(): CursorState {
  return {
    row: 0,
    col: 0,
    visible: true,
    blinking: false,
  };
}

/**
 * Creates default text attributes.
 */
function createDefaultAttributes(): Attributes {
  return {
    fg: { type: 'default' },
    bg: { type: 'default' },
    bold: false,
    italic: false,
    underline: UnderlineStyle.None,
    inverse: false,
    strikethrough: false,
  };
}

/**
 * AlternateScreenManager handles switching between primary and alternate screen buffers.
 * The alternate screen is used by full-screen applications and does not contribute to scrollback.
 */
export class AlternateScreenManager {
  private primary: ScreenState;
  private alternate: ScreenState;
  private current: 'primary' | 'alternate';
  private readonly cols: number;
  private readonly rows: number;

  /**
   * Creates a new AlternateScreenManager with the specified dimensions.
   * @param cols Number of columns
   * @param rows Number of rows
   */
  constructor(cols: number, rows: number) {
    this.cols = cols;
    this.rows = rows;
    this.current = 'primary';

    // Initialize primary screen
    this.primary = {
      buffer: new ScreenBuffer(cols, rows),
      cursor: createDefaultCursor(),
      attributes: createDefaultAttributes(),
    };

    // Initialize alternate screen (will be cleared when first activated)
    this.alternate = {
      buffer: new ScreenBuffer(cols, rows),
      cursor: createDefaultCursor(),
      attributes: createDefaultAttributes(),
    };
  }

  /**
   * Switches to the alternate screen buffer.
   * The alternate buffer is cleared to default state.
   */
  switchToAlternate(): void {
    if (this.current === 'alternate') {
      return; // Already in alternate mode
    }

    this.current = 'alternate';
    
    // Clear alternate buffer and reset state
    this.alternate.buffer.clear();
    this.alternate.cursor = createDefaultCursor();
    this.alternate.attributes = createDefaultAttributes();
    this.alternate.savedCursor = undefined;
  }

  /**
   * Switches back to the primary screen buffer.
   * The primary buffer state is restored exactly as it was.
   */
  switchToPrimary(): void {
    if (this.current === 'primary') {
      return; // Already in primary mode
    }

    this.current = 'primary';
  }

  /**
   * Gets the currently active screen buffer.
   */
  getCurrentBuffer(): ScreenBuffer {
    return this.current === 'primary' ? this.primary.buffer : this.alternate.buffer;
  }

  /**
   * Gets the currently active cursor state.
   */
  getCurrentCursor(): CursorState {
    return this.current === 'primary' ? this.primary.cursor : this.alternate.cursor;
  }

  /**
   * Sets the cursor state for the current screen.
   */
  setCurrentCursor(cursor: CursorState): void {
    if (this.current === 'primary') {
      this.primary.cursor = cursor;
    } else {
      this.alternate.cursor = cursor;
    }
  }

  /**
   * Gets the currently active attributes.
   */
  getCurrentAttributes(): Attributes {
    return this.current === 'primary' ? this.primary.attributes : this.alternate.attributes;
  }

  /**
   * Sets the attributes for the current screen.
   */
  setCurrentAttributes(attributes: Attributes): void {
    if (this.current === 'primary') {
      this.primary.attributes = attributes;
    } else {
      this.alternate.attributes = attributes;
    }
  }

  /**
   * Returns whether the current screen is the alternate screen.
   */
  isAlternateScreen(): boolean {
    return this.current === 'alternate';
  }

  /**
   * Gets the primary screen buffer (for testing/inspection).
   */
  getPrimaryBuffer(): ScreenBuffer {
    return this.primary.buffer;
  }

  /**
   * Gets the alternate screen buffer (for testing/inspection).
   */
  getAlternateBuffer(): ScreenBuffer {
    return this.alternate.buffer;
  }

  /**
   * Gets the primary cursor state (for testing/inspection).
   */
  getPrimaryCursor(): CursorState {
    return this.primary.cursor;
  }

  /**
   * Gets the alternate cursor state (for testing/inspection).
   */
  getAlternateCursor(): CursorState {
    return this.alternate.cursor;
  }

  /**
   * Resizes both screen buffers.
   */
  resize(newCols: number, newRows: number): void {
    this.primary.buffer.resize(newCols, newRows);
    this.alternate.buffer.resize(newCols, newRows);
    (this as any).cols = newCols;
    (this as any).rows = newRows;
  }
}
