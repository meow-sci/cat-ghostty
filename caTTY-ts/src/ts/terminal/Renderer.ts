/**
 * Renderer - Renders terminal state to HTML.
 * 
 * This class is responsible for converting the headless terminal state
 * into a visual HTML representation using absolute-positioned spans.
 */

import type { Terminal } from './Terminal.js';
import type { Line, Cell, CursorState, Color } from './types.js';
import { UnderlineStyle } from './types.js';

/**
 * Cached cell data for incremental rendering.
 */
interface CachedCell {
  char: string;
  fg: Color;
  bg: Color;
  bold: boolean;
  italic: boolean;
  underline: UnderlineStyle;
  inverse: boolean;
  strikethrough: boolean;
  url?: string;
  width: number;
}

/**
 * Renderer class for converting terminal state to HTML.
 */
export class Renderer {
  private readonly displayElement: HTMLElement;
  
  // Cache for incremental rendering
  private cellCache: Map<string, CachedCell> = new Map();
  private lastCursorPosition: { row: number; col: number; visible: boolean } | null = null;
  private lineElements: Map<number, HTMLElement> = new Map();
  private cellElements: Map<string, HTMLElement> = new Map();
  private cursorElement: HTMLElement | null = null;
  
  /**
   * Creates a new Renderer instance.
   * @param displayElement The HTML element to render into
   */
  constructor(displayElement: HTMLElement) {
    this.displayElement = displayElement;
    
    // Set up initial display element styles
    this.setupDisplayElement();
  }
  
  /**
   * Sets up the display element with required styles.
   */
  private setupDisplayElement(): void {
    // Ensure the display element has the correct base styles
    this.displayElement.style.position = 'relative';
    this.displayElement.style.fontFamily = 'monospace';
    this.displayElement.style.whiteSpace = 'pre';
    this.displayElement.style.overflow = 'hidden';
    this.displayElement.style.lineHeight = '1';
  }
  
  /**
   * Renders the current terminal state to the display element.
   * Uses incremental rendering to only update changed cells.
   * @param terminal The terminal instance to render
   */
  render(terminal: Terminal): void {
    // Get terminal configuration
    const config = terminal.getConfig();
    const cursor = terminal.getCursor();
    
    // Use document fragment for batching DOM updates
    const fragment = document.createDocumentFragment();
    const updatedElements: HTMLElement[] = [];
    
    // Track which cells have been updated
    const updatedCells = new Set<string>();
    
    // Render each line incrementally
    for (let row = 0; row < config.rows; row++) {
      const line = terminal.getLine(row);
      
      // Get or create line element
      let lineElement = this.lineElements.get(row);
      if (!lineElement) {
        lineElement = this.createLineElement(row);
        this.lineElements.set(row, lineElement);
        fragment.appendChild(lineElement);
      }
      
      // Update cells in this line
      for (let col = 0; col < line.cells.length; col++) {
        const cell = line.cells[col];
        const cellKey = `${row},${col}`;
        
        // Skip continuation cells (width === 0)
        if (cell.width === 0) {
          updatedCells.add(cellKey);
          continue;
        }
        
        // Check if cell has changed
        const cachedCell = this.cellCache.get(cellKey);
        if (cachedCell && this.cellsEqual(cachedCell, cell)) {
          updatedCells.add(cellKey);
          continue;
        }
        
        // Cell has changed, update it
        let cellElement = this.cellElements.get(cellKey);
        if (!cellElement) {
          cellElement = this.renderCell(cell, col);
          this.cellElements.set(cellKey, cellElement);
          lineElement.appendChild(cellElement);
        } else {
          // Update existing cell element
          this.updateCellElement(cellElement, cell);
        }
        
        // Update cache
        this.cellCache.set(cellKey, this.cloneCell(cell));
        updatedCells.add(cellKey);
      }
    }
    
    // Remove cells that no longer exist
    for (const [key, element] of this.cellElements.entries()) {
      if (!updatedCells.has(key)) {
        element.remove();
        this.cellElements.delete(key);
        this.cellCache.delete(key);
      }
    }
    
    // Append fragment if it has new elements
    if (fragment.childNodes.length > 0) {
      this.displayElement.appendChild(fragment);
    }
    
    // Update cursor if it has changed
    if (this.cursorHasChanged(cursor)) {
      if (this.cursorElement) {
        this.cursorElement.remove();
      }
      this.cursorElement = this.renderCursor(cursor);
      this.displayElement.appendChild(this.cursorElement);
      this.lastCursorPosition = { row: cursor.row, col: cursor.col, visible: cursor.visible };
    }
  }
  
  /**
   * Creates a line element container.
   */
  private createLineElement(row: number): HTMLElement {
    const lineElement = document.createElement('div');
    lineElement.style.position = 'absolute';
    lineElement.style.top = `${row}em`;
    lineElement.style.left = '0';
    lineElement.style.height = '1em';
    lineElement.style.whiteSpace = 'pre';
    return lineElement;
  }
  
  /**
   * Checks if two cells are equal.
   */
  private cellsEqual(a: CachedCell, b: Cell): boolean {
    return (
      a.char === b.char &&
      a.width === b.width &&
      a.bold === b.bold &&
      a.italic === b.italic &&
      a.underline === b.underline &&
      a.inverse === b.inverse &&
      a.strikethrough === b.strikethrough &&
      a.url === b.url &&
      this.colorsEqual(a.fg, b.fg) &&
      this.colorsEqual(a.bg, b.bg)
    );
  }
  
  /**
   * Checks if two colors are equal.
   */
  private colorsEqual(a: Color, b: Color): boolean {
    if (a.type !== b.type) return false;
    
    switch (a.type) {
      case 'default':
        return true;
      case 'indexed':
        return b.type === 'indexed' && a.index === b.index;
      case 'rgb':
        return b.type === 'rgb' && a.r === b.r && a.g === b.g && a.b === b.b;
    }
  }
  
  /**
   * Clones a cell for caching.
   */
  private cloneCell(cell: Cell): CachedCell {
    return {
      char: cell.char,
      width: cell.width,
      fg: { ...cell.fg } as Color,
      bg: { ...cell.bg } as Color,
      bold: cell.bold,
      italic: cell.italic,
      underline: cell.underline,
      inverse: cell.inverse,
      strikethrough: cell.strikethrough,
      url: cell.url,
    };
  }
  
  /**
   * Updates an existing cell element with new cell data.
   */
  private updateCellElement(element: HTMLElement, cell: Cell): void {
    element.textContent = cell.char || ' ';
    this.applyStyles(element, cell);
  }
  
  /**
   * Checks if the cursor has changed since last render.
   */
  private cursorHasChanged(cursor: CursorState): boolean {
    if (!this.lastCursorPosition) return true;
    
    return (
      this.lastCursorPosition.row !== cursor.row ||
      this.lastCursorPosition.col !== cursor.col ||
      this.lastCursorPosition.visible !== cursor.visible
    );
  }
  
  /**
   * Clears the rendering cache.
   * Call this when the terminal is resized or cleared.
   */
  clearCache(): void {
    this.cellCache.clear();
    this.lastCursorPosition = null;
    this.lineElements.clear();
    this.cellElements.clear();
    this.cursorElement = null;
    this.displayElement.innerHTML = '';
  }
  
  /**
   * Renders a single line to an HTML element.
   * @param line The line to render
   * @param row The row index
   * @returns The HTML element representing the line
   */
  private renderLine(line: Line, row: number): HTMLElement {
    const lineElement = document.createElement('div');
    lineElement.style.position = 'absolute';
    lineElement.style.top = `${row}em`;
    lineElement.style.left = '0';
    lineElement.style.height = '1em';
    lineElement.style.whiteSpace = 'pre';
    
    // Render each cell in the line
    for (let col = 0; col < line.cells.length; col++) {
      const cell = line.cells[col];
      
      // Skip continuation cells (width === 0)
      if (cell.width === 0) {
        continue;
      }
      
      const cellElement = this.renderCell(cell, col);
      lineElement.appendChild(cellElement);
    }
    
    return lineElement;
  }
  
  /**
   * Renders a single cell to an HTML element.
   * @param cell The cell to render
   * @param col The column index
   * @returns The HTML element representing the cell
   */
  private renderCell(cell: Cell, col: number): HTMLElement {
    const span = document.createElement('span');
    span.style.position = 'absolute';
    span.style.left = `${col}ch`;
    
    // Set character content (use space for empty cells)
    span.textContent = cell.char || ' ';
    
    // Apply cell styling
    this.applyStyles(span, cell);
    
    return span;
  }
  
  /**
   * Applies CSS styles to a cell element based on cell attributes.
   * @param element The HTML element to style
   * @param cell The cell containing attribute information
   */
  private applyStyles(element: HTMLElement, cell: Cell): void {
    // Reset all styles first to ensure clean state
    element.style.color = '';
    element.style.backgroundColor = '';
    element.style.fontWeight = '';
    element.style.fontStyle = '';
    element.style.textDecoration = '';
    element.style.cursor = '';
    element.style.width = '';
    element.removeAttribute('data-url');
    
    // Apply foreground color
    const fgColor = this.colorToCSS(cell.fg);
    if (fgColor) {
      element.style.color = fgColor;
    }
    
    // Apply background color
    const bgColor = this.colorToCSS(cell.bg);
    if (bgColor) {
      element.style.backgroundColor = bgColor;
    }
    
    // Apply text attributes
    if (cell.bold) {
      element.style.fontWeight = 'bold';
    }
    
    if (cell.italic) {
      element.style.fontStyle = 'italic';
    }
    
    if (cell.underline !== UnderlineStyle.None) {
      element.style.textDecoration = this.underlineStyleToCSS(cell.underline);
    }
    
    if (cell.strikethrough) {
      element.style.textDecoration = element.style.textDecoration 
        ? `${element.style.textDecoration} line-through`
        : 'line-through';
    }
    
    // Handle inverse video
    if (cell.inverse) {
      // Swap foreground and background colors
      const temp = element.style.color;
      element.style.color = element.style.backgroundColor || '#ffffff';
      element.style.backgroundColor = temp || '#000000';
    }
    
    // Handle hyperlinks
    if (cell.url) {
      element.style.textDecoration = element.style.textDecoration
        ? `${element.style.textDecoration} underline`
        : 'underline';
      element.style.cursor = 'pointer';
      element.setAttribute('data-url', cell.url);
    }
    
    // Handle wide characters
    if (cell.width === 2) {
      element.style.width = '2ch';
    }
  }
  
  /**
   * Converts a Color type to a CSS color string.
   * @param color The color to convert
   * @returns CSS color string or null for default color
   */
  private colorToCSS(color: Color): string | null {
    switch (color.type) {
      case 'default':
        return null;
        
      case 'indexed':
        // Convert indexed color (0-255) to CSS
        return this.indexedColorToCSS(color.index);
        
      case 'rgb':
        return `rgb(${color.r}, ${color.g}, ${color.b})`;
    }
  }
  
  /**
   * Converts an indexed color (0-255) to a CSS color string.
   * @param index Color index (0-255)
   * @returns CSS color string
   */
  private indexedColorToCSS(index: number): string {
    // Standard 16 colors (0-15)
    if (index < 16) {
      const standardColors = [
        '#000000', // Black
        '#800000', // Red
        '#008000', // Green
        '#808000', // Yellow
        '#000080', // Blue
        '#800080', // Magenta
        '#008080', // Cyan
        '#c0c0c0', // White
        '#808080', // Bright Black (Gray)
        '#ff0000', // Bright Red
        '#00ff00', // Bright Green
        '#ffff00', // Bright Yellow
        '#0000ff', // Bright Blue
        '#ff00ff', // Bright Magenta
        '#00ffff', // Bright Cyan
        '#ffffff', // Bright White
      ];
      return standardColors[index];
    }
    
    // 216 color cube (16-231)
    if (index >= 16 && index <= 231) {
      const cubeIndex = index - 16;
      const r = Math.floor(cubeIndex / 36);
      const g = Math.floor((cubeIndex % 36) / 6);
      const b = cubeIndex % 6;
      
      const toRGB = (value: number) => value === 0 ? 0 : 55 + value * 40;
      
      return `rgb(${toRGB(r)}, ${toRGB(g)}, ${toRGB(b)})`;
    }
    
    // Grayscale ramp (232-255)
    if (index >= 232 && index <= 255) {
      const gray = 8 + (index - 232) * 10;
      return `rgb(${gray}, ${gray}, ${gray})`;
    }
    
    // Fallback for out of range
    return '#ffffff';
  }
  
  /**
   * Converts an UnderlineStyle to a CSS text-decoration value.
   * @param style The underline style
   * @returns CSS text-decoration value
   */
  private underlineStyleToCSS(style: UnderlineStyle): string {
    switch (style) {
      case UnderlineStyle.None:
        return 'none';
      case UnderlineStyle.Single:
        return 'underline';
      case UnderlineStyle.Double:
        return 'underline double';
      case UnderlineStyle.Curly:
        return 'underline wavy';
      case UnderlineStyle.Dotted:
        return 'underline dotted';
      case UnderlineStyle.Dashed:
        return 'underline dashed';
      default:
        return 'underline';
    }
  }
  
  /**
   * Renders the cursor to an HTML element.
   * @param cursor The cursor state
   * @returns The HTML element representing the cursor
   */
  private renderCursor(cursor: CursorState): HTMLElement {
    const cursorElement = document.createElement('div');
    cursorElement.className = 'terminal-cursor';
    cursorElement.style.position = 'absolute';
    cursorElement.style.left = `${cursor.col}ch`;
    cursorElement.style.top = `${cursor.row}em`;
    cursorElement.style.width = '1ch';
    cursorElement.style.height = '1em';
    cursorElement.style.backgroundColor = '#ffffff';
    cursorElement.style.opacity = cursor.visible ? '0.5' : '0';
    cursorElement.style.pointerEvents = 'none';
    
    // Add blinking animation if cursor is blinking
    if (cursor.blinking && cursor.visible) {
      cursorElement.style.animation = 'terminal-cursor-blink 1s step-end infinite';
    }
    
    return cursorElement;
  }
}
