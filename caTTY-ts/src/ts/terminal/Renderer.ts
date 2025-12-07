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
 * Renderer class for converting terminal state to HTML.
 */
export class Renderer {
  private readonly displayElement: HTMLElement;
  
  // Simple string-based cache for detecting row changes
  private rowSignatures: Map<number, string> = new Map();
  private lastCursorPosition: { row: number; col: number; visible: boolean } | null = null;
  private lineElements: Map<number, HTMLElement> = new Map();
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
   * Uses string-based comparison to detect and render only changed rows.
   * @param terminal The terminal instance to render
   */
  render(terminal: Terminal): void {
    // Get terminal configuration
    const config = terminal.getConfig();
    const cursor = terminal.getCursor();
    
    // Use document fragment for batching DOM updates
    const fragment = document.createDocumentFragment();
    
    // Check all rows for changes using signature comparison
    for (let row = 0; row < config.rows; row++) {
      const line = terminal.getLine(row);
      const signature = this.generateRowSignature(line);
      const previousSignature = this.rowSignatures.get(row);
      
      // Only render if the row has changed
      if (signature !== previousSignature) {
        // Remove old line element if it exists
        const oldLineElement = this.lineElements.get(row);
        if (oldLineElement && oldLineElement.parentNode === this.displayElement) {
          this.displayElement.removeChild(oldLineElement);
        }
        
        // Render new line
        const lineElement = this.renderLine(line, row);
        this.lineElements.set(row, lineElement);
        fragment.appendChild(lineElement);
        
        // Update signature cache
        this.rowSignatures.set(row, signature);
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
   * Generates a unique string signature for a row that includes both content and styling.
   * This signature is used to detect if a row has changed and needs re-rendering.
   * Format: "{style_code}text{style_code}text..."
   * Example: "{fg(255,0,0)}red {fg(0,0,255)}blue"
   */
  private generateRowSignature(line: Line): string {
    let signature = '';
    
    for (const cell of line.cells) {
      // Skip continuation cells (width === 0)
      if (cell.width === 0) {
        continue;
      }
      
      // Build style code
      const styleCode = this.generateCellStyleCode(cell);
      signature += styleCode;
      
      // Add character
      signature += cell.char || ' ';
    }
    
    return signature;
  }
  
  /**
   * Generates a compact style code for a cell.
   * This encodes all visual attributes into a short string.
   */
  private generateCellStyleCode(cell: Cell): string {
    const parts: string[] = [];
    
    // Foreground color
    if (cell.fg.type === 'indexed') {
      parts.push(`f${cell.fg.index}`);
    } else if (cell.fg.type === 'rgb') {
      parts.push(`f(${cell.fg.r},${cell.fg.g},${cell.fg.b})`);
    }
    
    // Background color
    if (cell.bg.type === 'indexed') {
      parts.push(`b${cell.bg.index}`);
    } else if (cell.bg.type === 'rgb') {
      parts.push(`b(${cell.bg.r},${cell.bg.g},${cell.bg.b})`);
    }
    
    // Text attributes
    if (cell.bold) parts.push('B');
    if (cell.italic) parts.push('I');
    if (cell.underline !== UnderlineStyle.None) parts.push(`U${cell.underline}`);
    if (cell.inverse) parts.push('V');
    if (cell.strikethrough) parts.push('S');
    if (cell.url) parts.push(`L${cell.url}`);
    if (cell.width === 2) parts.push('W');
    
    return parts.length > 0 ? `{${parts.join(',')}}` : '';
  }
  
  /**
   * Checks if two cells have equal styles (ignoring char and width).
   * Used for cell batching optimization.
   */
  private cellStylesEqual(a: Cell, b: Cell): boolean {
    return (
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
    this.rowSignatures.clear();
    this.lastCursorPosition = null;
    this.lineElements.clear();
    this.cursorElement = null;
    this.displayElement.innerHTML = '';
  }
  
  /**
   * Renders a single line to an HTML element.
   * Uses cell batching to combine consecutive same-styled cells into single spans.
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
    
    // Batch consecutive cells with identical styling
    let runStart = 0;
    let runText = '';
    let runCell: Cell | null = null;
    
    for (let col = 0; col < line.cells.length; col++) {
      const cell = line.cells[col];
      
      // Skip continuation cells (width === 0) but include them in the run
      if (cell.width === 0) {
        continue;
      }
      
      // Check if this cell can be batched with the current run
      const canBatch = runCell !== null && this.cellStylesEqual(runCell, cell);
      
      if (canBatch) {
        // Add to current run
        runText += cell.char || ' ';
        
        // Handle wide characters - they occupy 2 positions
        if (cell.width === 2) {
          runText += ' '; // Add extra space for wide character
        }
      } else {
        // Style changed or first cell - flush current run
        if (runCell !== null) {
          const runSpan = this.createRunSpan(runCell, runStart, runText);
          lineElement.appendChild(runSpan);
        }
        
        // Start new run
        runStart = col;
        runText = cell.char || ' ';
        runCell = cell;
        
        // Handle wide characters
        if (cell.width === 2) {
          runText += ' ';
        }
      }
    }
    
    // Flush final run
    if (runCell !== null) {
      const runSpan = this.createRunSpan(runCell, runStart, runText);
      lineElement.appendChild(runSpan);
    }
    
    // Ensure line element always has at least one child for proper rendering
    // This handles edge cases where no spans were created
    if (lineElement.childNodes && lineElement.childNodes.length === 0) {
      const emptySpan = document.createElement('span');
      emptySpan.textContent = '\u00A0'; // Non-breaking space to ensure visibility
      emptySpan.style.display = 'inline-block';
      emptySpan.style.width = '1ch';
      lineElement.appendChild(emptySpan);
    }
    
    return lineElement;
  }
  
  /**
   * Creates a span element for a run of consecutive same-styled cells.
   * This is used for cell batching optimization.
   * @param cell The cell containing the style to apply
   * @param col The starting column position
   * @param text The concatenated text from all cells in the run
   * @returns The HTML element representing the run
   */
  private createRunSpan(cell: Cell, col: number, text: string): HTMLElement {
    const span = document.createElement('span');
    span.style.position = 'absolute';
    span.style.left = `${col}ch`;
    
    // Set the concatenated text content
    span.textContent = text;
    
    // Apply cell styling
    this.applyStyles(span, cell);
    
    return span;
  }
  
  /**
   * Applies CSS styles to a cell element based on cell attributes.
   * @param element The HTML element to style
   * @param cell The Cell containing attribute information
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
    if (element.hasAttribute && element.hasAttribute('data-url')) {
      element.removeAttribute('data-url');
    }
    
    // Apply foreground color
    let fgColor = this.colorToCSS(cell.fg);
    
    // WORKAROUND: If foreground is black (index 0) and background is default/black,
    // treat foreground as default (white) to avoid invisible text
    if (cell.fg.type === 'indexed' && cell.fg.index === 0 && 
        (cell.bg.type === 'default' || (cell.bg.type === 'indexed' && cell.bg.index === 0))) {
      fgColor = null; // Use default color (white)
    }
    
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
