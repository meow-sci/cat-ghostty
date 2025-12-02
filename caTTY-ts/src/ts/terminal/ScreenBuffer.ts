/**
 * ScreenBuffer manages the 2D grid of cells representing the terminal screen.
 * It provides methods for cell access, scrolling, clearing, and resizing.
 */

import type { Cell, Line, Color } from './types.js';
import { UnderlineStyle } from './types.js';

/**
 * Creates a default empty cell with default attributes.
 */
function createEmptyCell(): Cell {
  return {
    char: ' ',
    width: 1,
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
 * ScreenBuffer represents a 2D grid of terminal cells.
 */
export class ScreenBuffer {
  private buffer: Line[];
  private readonly cols: number;
  private readonly rows: number;

  /**
   * Creates a new screen buffer with the specified dimensions.
   * @param cols Number of columns (width)
   * @param rows Number of rows (height)
   */
  constructor(cols: number, rows: number) {
    if (cols < 1 || cols > 1000) {
      throw new Error(`Invalid cols: ${cols}. Must be between 1 and 1000.`);
    }
    if (rows < 1 || rows > 1000) {
      throw new Error(`Invalid rows: ${rows}. Must be between 1 and 1000.`);
    }

    this.cols = cols;
    this.rows = rows;
    this.buffer = [];

    // Initialize buffer with empty cells
    for (let row = 0; row < rows; row++) {
      const cells: Cell[] = [];
      for (let col = 0; col < cols; col++) {
        cells.push(createEmptyCell());
      }
      this.buffer.push({ cells, wrapped: false });
    }
  }

  /**
   * Gets the number of columns in the buffer.
   */
  getCols(): number {
    return this.cols;
  }

  /**
   * Gets the number of rows in the buffer.
   */
  getRows(): number {
    return this.rows;
  }

  /**
   * Gets a cell at the specified position.
   * Returns an empty cell if the position is out of bounds.
   * @param row Row index (0-based)
   * @param col Column index (0-based)
   */
  getCell(row: number, col: number): Cell {
    if (row < 0 || row >= this.rows || col < 0 || col >= this.cols) {
      return createEmptyCell();
    }
    return this.buffer[row].cells[col];
  }

  /**
   * Sets a cell at the specified position.
   * Does nothing if the position is out of bounds.
   * @param row Row index (0-based)
   * @param col Column index (0-based)
   * @param cell The cell to set
   */
  setCell(row: number, col: number, cell: Cell): void {
    if (row < 0 || row >= this.rows || col < 0 || col >= this.cols) {
      return;
    }
    this.buffer[row].cells[col] = cell;
  }

  /**
   * Gets a complete line at the specified row.
   * Returns a line with empty cells if the row is out of bounds.
   * @param row Row index (0-based)
   */
  getLine(row: number): Line {
    if (row < 0 || row >= this.rows) {
      const cells: Cell[] = [];
      for (let col = 0; col < this.cols; col++) {
        cells.push(createEmptyCell());
      }
      return { cells, wrapped: false };
    }
    return this.buffer[row];
  }

  /**
   * Clears the entire buffer, setting all cells to empty.
   */
  clear(): void {
    for (let row = 0; row < this.rows; row++) {
      for (let col = 0; col < this.cols; col++) {
        this.buffer[row].cells[col] = createEmptyCell();
      }
      this.buffer[row].wrapped = false;
    }
  }

  /**
   * Clears a rectangular region of the buffer.
   * @param startRow Starting row (inclusive)
   * @param endRow Ending row (inclusive)
   * @param startCol Starting column (inclusive)
   * @param endCol Ending column (inclusive)
   */
  clearRegion(startRow: number, endRow: number, startCol: number, endCol: number): void {
    // Clamp to valid bounds
    startRow = Math.max(0, Math.min(startRow, this.rows - 1));
    endRow = Math.max(0, Math.min(endRow, this.rows - 1));
    startCol = Math.max(0, Math.min(startCol, this.cols - 1));
    endCol = Math.max(0, Math.min(endCol, this.cols - 1));

    for (let row = startRow; row <= endRow; row++) {
      for (let col = startCol; col <= endCol; col++) {
        this.buffer[row].cells[col] = createEmptyCell();
      }
    }
  }

  /**
   * Scrolls the buffer up by the specified number of lines.
   * Lines scrolled off the top are lost, and new empty lines appear at the bottom.
   * @param lines Number of lines to scroll
   * @param scrollRegion Optional scroll region {top, bottom} (inclusive)
   */
  scrollUp(lines: number, scrollRegion?: { top: number; bottom: number }): void {
    if (lines <= 0) return;

    const top = scrollRegion?.top ?? 0;
    const bottom = scrollRegion?.bottom ?? this.rows - 1;

    // Clamp to valid bounds
    const clampedTop = Math.max(0, Math.min(top, this.rows - 1));
    const clampedBottom = Math.max(0, Math.min(bottom, this.rows - 1));

    if (clampedTop >= clampedBottom) return;

    const regionHeight = clampedBottom - clampedTop + 1;
    const scrollAmount = Math.min(lines, regionHeight);

    // Shift lines up within the region
    for (let i = 0; i < regionHeight - scrollAmount; i++) {
      this.buffer[clampedTop + i] = this.buffer[clampedTop + i + scrollAmount];
    }

    // Fill bottom lines with empty cells
    for (let i = regionHeight - scrollAmount; i < regionHeight; i++) {
      const cells: Cell[] = [];
      for (let col = 0; col < this.cols; col++) {
        cells.push(createEmptyCell());
      }
      this.buffer[clampedTop + i] = { cells, wrapped: false };
    }
  }

  /**
   * Scrolls the buffer down by the specified number of lines.
   * Lines scrolled off the bottom are lost, and new empty lines appear at the top.
   * @param lines Number of lines to scroll
   * @param scrollRegion Optional scroll region {top, bottom} (inclusive)
   */
  scrollDown(lines: number, scrollRegion?: { top: number; bottom: number }): void {
    if (lines <= 0) return;

    const top = scrollRegion?.top ?? 0;
    const bottom = scrollRegion?.bottom ?? this.rows - 1;

    // Clamp to valid bounds
    const clampedTop = Math.max(0, Math.min(top, this.rows - 1));
    const clampedBottom = Math.max(0, Math.min(bottom, this.rows - 1));

    if (clampedTop >= clampedBottom) return;

    const regionHeight = clampedBottom - clampedTop + 1;
    const scrollAmount = Math.min(lines, regionHeight);

    // Shift lines down within the region
    for (let i = regionHeight - 1; i >= scrollAmount; i--) {
      this.buffer[clampedTop + i] = this.buffer[clampedTop + i - scrollAmount];
    }

    // Fill top lines with empty cells
    for (let i = 0; i < scrollAmount; i++) {
      const cells: Cell[] = [];
      for (let col = 0; col < this.cols; col++) {
        cells.push(createEmptyCell());
      }
      this.buffer[clampedTop + i] = { cells, wrapped: false };
    }
  }

  /**
   * Inserts blank lines at the specified row, shifting existing lines down.
   * Lines that scroll off the bottom are lost.
   * @param row Row at which to insert lines
   * @param count Number of lines to insert
   */
  insertLines(row: number, count: number): void {
    if (row < 0 || row >= this.rows || count <= 0) return;

    const linesToMove = Math.min(count, this.rows - row);

    // Shift lines down
    for (let i = this.rows - 1; i >= row + linesToMove; i--) {
      this.buffer[i] = this.buffer[i - linesToMove];
    }

    // Insert blank lines
    for (let i = 0; i < linesToMove; i++) {
      const cells: Cell[] = [];
      for (let col = 0; col < this.cols; col++) {
        cells.push(createEmptyCell());
      }
      this.buffer[row + i] = { cells, wrapped: false };
    }
  }

  /**
   * Deletes lines at the specified row, shifting lines below up.
   * New blank lines appear at the bottom.
   * @param row Row at which to delete lines
   * @param count Number of lines to delete
   */
  deleteLines(row: number, count: number): void {
    if (row < 0 || row >= this.rows || count <= 0) return;

    const linesToDelete = Math.min(count, this.rows - row);

    // Shift lines up
    for (let i = row; i < this.rows - linesToDelete; i++) {
      this.buffer[i] = this.buffer[i + linesToDelete];
    }

    // Fill bottom with blank lines
    for (let i = this.rows - linesToDelete; i < this.rows; i++) {
      const cells: Cell[] = [];
      for (let col = 0; col < this.cols; col++) {
        cells.push(createEmptyCell());
      }
      this.buffer[i] = { cells, wrapped: false };
    }
  }

  /**
   * Inserts blank cells at the specified position, shifting existing cells right.
   * Cells that scroll off the right edge are lost.
   * @param row Row at which to insert cells
   * @param col Column at which to insert cells
   * @param count Number of cells to insert
   */
  insertCells(row: number, col: number, count: number): void {
    if (row < 0 || row >= this.rows || col < 0 || col >= this.cols || count <= 0) {
      return;
    }

    const line = this.buffer[row];
    const cellsToInsert = Math.min(count, this.cols - col);

    // Shift cells right
    for (let i = this.cols - 1; i >= col + cellsToInsert; i--) {
      line.cells[i] = line.cells[i - cellsToInsert];
    }

    // Insert blank cells
    for (let i = 0; i < cellsToInsert; i++) {
      line.cells[col + i] = createEmptyCell();
    }
  }

  /**
   * Deletes cells at the specified position, shifting remaining cells left.
   * New blank cells appear at the right edge.
   * @param row Row at which to delete cells
   * @param col Column at which to delete cells
   * @param count Number of cells to delete
   */
  deleteCells(row: number, col: number, count: number): void {
    if (row < 0 || row >= this.rows || col < 0 || col >= this.cols || count <= 0) {
      return;
    }

    const line = this.buffer[row];
    const cellsToDelete = Math.min(count, this.cols - col);

    // Shift cells left
    for (let i = col; i < this.cols - cellsToDelete; i++) {
      line.cells[i] = line.cells[i + cellsToDelete];
    }

    // Fill right edge with blank cells
    for (let i = this.cols - cellsToDelete; i < this.cols; i++) {
      line.cells[i] = createEmptyCell();
    }
  }

  /**
   * Resizes the buffer, preserving content where possible.
   * Content in the overlapping region is preserved.
   * @param newCols New number of columns
   * @param newRows New number of rows
   */
  resize(newCols: number, newRows: number): void {
    if (newCols < 1 || newCols > 1000) {
      throw new Error(`Invalid cols: ${newCols}. Must be between 1 and 1000.`);
    }
    if (newRows < 1 || newRows > 1000) {
      throw new Error(`Invalid rows: ${newRows}. Must be between 1 and 1000.`);
    }

    const newBuffer: Line[] = [];

    // Copy overlapping content
    for (let row = 0; row < newRows; row++) {
      const cells: Cell[] = [];
      
      for (let col = 0; col < newCols; col++) {
        if (row < this.rows && col < this.cols) {
          // Copy existing cell
          cells.push(this.buffer[row].cells[col]);
        } else {
          // Fill with empty cell
          cells.push(createEmptyCell());
        }
      }
      
      const wrapped = row < this.rows ? this.buffer[row].wrapped : false;
      newBuffer.push({ cells, wrapped });
    }

    // Update buffer and dimensions
    this.buffer = newBuffer;
    (this as any).cols = newCols;
    (this as any).rows = newRows;
  }
}
