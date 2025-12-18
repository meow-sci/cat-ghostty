/**
 * End-to-end integration tests for SGR styling system
 * Tests that SGR sequences are properly processed through the terminal
 * and result in correct CSS classes being applied to DOM elements.
 * 
 * **Feature: xterm-extensions, Task 14.13: Test SGR styling integration end-to-end**
 * **Validates: Requirements 3.4, 4.1, 4.2**
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { StatefulTerminal } from '../terminal/StatefulTerminal';
import { SgrStyleManager, createDefaultSgrState } from '../terminal/SgrStyleManager';
import { ThemeManager, DEFAULT_DARK_THEME } from '../terminal/TerminalTheme';
import { DomStyleManager } from '../terminal/DomStyleManager';

// Mock DOM environment for testing
class MockDocument {
  private elements = new Map<string, MockElement>();
  private _head = new MockElement('head');

  getElementById(id: string): MockElement | null {
    return this.elements.get(id) || null;
  }

  createElement(tagName: string): MockElement {
    return new MockElement(tagName);
  }

  appendChild(_element: MockElement): void {
    // Mock implementation
  }

  get head(): MockElement {
    return this._head;
  }
}

class MockElement {
  public id = '';
  public textContent = '';
  public classList = new MockClassList();
  public style = new MockStyle();
  public parentNode: MockElement | null = null;
  private children: MockElement[] = [];

  constructor(public tagName: string) {}

  appendChild(child: MockElement): void {
    child.parentNode = this;
    this.children.push(child);
  }

  removeChild(child: MockElement): void {
    const index = this.children.indexOf(child);
    if (index >= 0) {
      this.children.splice(index, 1);
      child.parentNode = null;
    }
  }
}

class MockClassList {
  private classes = new Set<string>();

  add(className: string): void {
    this.classes.add(className);
  }

  remove(className: string): void {
    this.classes.delete(className);
  }

  contains(className: string): boolean {
    return this.classes.has(className);
  }

  *[Symbol.iterator]() {
    yield* this.classes;
  }
}

class MockStyle {
  public position = '';
  public left = '';
  public top = '';
}

describe('SGR Styling Integration End-to-End Tests', () => {
  let terminal: StatefulTerminal;
  let sgrStyleManager: SgrStyleManager;
  let themeManager: ThemeManager;
  let mockDocument: MockDocument;
  let originalDocument: any;

  beforeEach(() => {
    // Mock DOM environment
    mockDocument = new MockDocument();
    originalDocument = (global as any).document;
    (global as any).document = mockDocument;

    // Initialize components
    terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    sgrStyleManager = new SgrStyleManager();
    themeManager = new ThemeManager(DEFAULT_DARK_THEME);
    
    // Apply theme to set up CSS variables
    themeManager.applyTheme(DEFAULT_DARK_THEME);
  });

  afterEach(() => {
    // Restore original document
    (global as any).document = originalDocument;
    
    // Clear caches
    sgrStyleManager.clearCache();
    DomStyleManager.getInstance().clearCache();
  });

  describe('Standard 16 ANSI Colors', () => {
    it('should apply foreground colors correctly through terminal processing', () => {
      // Test standard foreground colors (30-37)
      const colorTests = [
        { code: 30, color: 'black', sequence: '\x1b[30m' },
        { code: 31, color: 'red', sequence: '\x1b[31m' },
        { code: 32, color: 'green', sequence: '\x1b[32m' },
        { code: 33, color: 'yellow', sequence: '\x1b[33m' },
        { code: 34, color: 'blue', sequence: '\x1b[34m' },
        { code: 35, color: 'magenta', sequence: '\x1b[35m' },
        { code: 36, color: 'cyan', sequence: '\x1b[36m' },
        { code: 37, color: 'white', sequence: '\x1b[37m' },
      ];

      colorTests.forEach(({ code: _code, color, sequence }) => {
        // Reset terminal state
        terminal.reset();
        
        // Send SGR sequence followed by a character
        terminal.pushPtyText(sequence + 'A');
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('A');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.foregroundColor).toEqual({
          type: 'named',
          color: color
        });

        // Generate CSS class and verify it contains correct color
        const cssClass = sgrStyleManager.getStyleClass(cell.sgrState!);
        expect(cssClass).toMatch(/^sgr-[a-f0-9]+$/);
        
        // Verify CSS generation includes the color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        expect(css).toContain(`color: var(--terminal-color-${color})`);
      });
    });

    it('should apply background colors correctly through terminal processing', () => {
      // Test standard background colors (40-47)
      const colorTests = [
        { code: 40, color: 'black', sequence: '\x1b[40m' },
        { code: 41, color: 'red', sequence: '\x1b[41m' },
        { code: 42, color: 'green', sequence: '\x1b[42m' },
        { code: 43, color: 'yellow', sequence: '\x1b[43m' },
        { code: 44, color: 'blue', sequence: '\x1b[44m' },
        { code: 45, color: 'magenta', sequence: '\x1b[45m' },
        { code: 46, color: 'cyan', sequence: '\x1b[46m' },
        { code: 47, color: 'white', sequence: '\x1b[47m' },
      ];

      colorTests.forEach(({ code: _code, color, sequence }) => {
        // Reset terminal state
        terminal.reset();
        
        // Send SGR sequence followed by a character
        terminal.pushPtyText(sequence + 'B');
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('B');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.backgroundColor).toEqual({
          type: 'named',
          color: color
        });

        // Generate CSS class and verify it contains correct background color
        const cssClass = sgrStyleManager.getStyleClass(cell.sgrState!);
        expect(cssClass).toMatch(/^sgr-[a-f0-9]+$/);
        
        // Verify CSS generation includes the background color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        expect(css).toContain(`background-color: var(--terminal-color-${color})`);
      });
    });

    it('should apply bright colors correctly through terminal processing', () => {
      // Test bright foreground colors (90-97)
      const brightColorTests = [
        { code: 90, color: 'brightBlack', sequence: '\x1b[90m' },
        { code: 91, color: 'brightRed', sequence: '\x1b[91m' },
        { code: 92, color: 'brightGreen', sequence: '\x1b[92m' },
        { code: 93, color: 'brightYellow', sequence: '\x1b[93m' },
        { code: 94, color: 'brightBlue', sequence: '\x1b[94m' },
        { code: 95, color: 'brightMagenta', sequence: '\x1b[95m' },
        { code: 96, color: 'brightCyan', sequence: '\x1b[96m' },
        { code: 97, color: 'brightWhite', sequence: '\x1b[97m' },
      ];

      brightColorTests.forEach(({ code: _code, color, sequence }) => {
        // Reset terminal state
        terminal.reset();
        
        // Send SGR sequence followed by a character
        terminal.pushPtyText(sequence + 'C');
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('C');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.foregroundColor).toEqual({
          type: 'named',
          color: color
        });

        // Verify CSS generation includes the bright color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        expect(css).toContain(`color: var(--terminal-color-${color.replace(/([A-Z])/g, '-$1').toLowerCase()})`);
      });
    });
  });

  describe('256-Color Palette', () => {
    it('should apply 256-color foreground colors correctly', () => {
      // Test various 256-color indices
      const colorIndices = [16, 51, 196, 226, 255];

      colorIndices.forEach(index => {
        // Reset terminal state
        terminal.reset();
        
        // Send 256-color SGR sequence: ESC[38;5;{index}m
        terminal.pushPtyText(`\x1b[38;5;${index}mD`);
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('D');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.foregroundColor).toEqual({
          type: 'indexed',
          index: index
        });

        // Generate CSS and verify it contains RGB color for extended palette
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        if (index >= 16) {
          // Extended colors should use RGB values
          expect(css).toMatch(/color: rgb\(\d+, \d+, \d+\)/);
        } else {
          // Standard colors should use CSS variables
          expect(css).toMatch(/color: var\(--terminal-color-[\w-]+\)/);
        }
      });
    });

    it('should apply 256-color background colors correctly', () => {
      // Test various 256-color background indices
      const colorIndices = [16, 88, 160, 208, 255];

      colorIndices.forEach(index => {
        // Reset terminal state
        terminal.reset();
        
        // Send 256-color background SGR sequence: ESC[48;5;{index}m
        terminal.pushPtyText(`\x1b[48;5;${index}mE`);
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('E');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.backgroundColor).toEqual({
          type: 'indexed',
          index: index
        });

        // Generate CSS and verify it contains correct background color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        if (index >= 16) {
          // Extended colors should use RGB values
          expect(css).toMatch(/background-color: rgb\(\d+, \d+, \d+\)/);
        } else {
          // Standard colors should use CSS variables
          expect(css).toMatch(/background-color: var\(--terminal-color-[\w-]+\)/);
        }
      });
    });
  });

  describe('24-bit RGB Colors', () => {
    it('should apply 24-bit RGB foreground colors correctly', () => {
      // Test various RGB color combinations
      const rgbTests = [
        { r: 255, g: 0, b: 0, name: 'red' },
        { r: 0, g: 255, b: 0, name: 'green' },
        { r: 0, g: 0, b: 255, name: 'blue' },
        { r: 255, g: 255, b: 0, name: 'yellow' },
        { r: 128, g: 64, b: 192, name: 'purple' },
      ];

      rgbTests.forEach(({ r, g, b, name: _name }) => {
        // Reset terminal state
        terminal.reset();
        
        // Send 24-bit RGB SGR sequence: ESC[38;2;r;g;bm
        terminal.pushPtyText(`\x1b[38;2;${r};${g};${b}mF`);
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('F');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.foregroundColor).toEqual({
          type: 'rgb',
          r: r,
          g: g,
          b: b
        });

        // Generate CSS and verify it contains exact RGB color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        expect(css).toContain(`color: rgb(${r}, ${g}, ${b})`);
      });
    });

    it('should apply 24-bit RGB background colors correctly', () => {
      // Test various RGB background color combinations
      const rgbTests = [
        { r: 64, g: 128, b: 192, name: 'blue-gray' },
        { r: 255, g: 128, b: 64, name: 'orange' },
        { r: 128, g: 255, b: 128, name: 'light-green' },
      ];

      rgbTests.forEach(({ r, g, b, name: _name }) => {
        // Reset terminal state
        terminal.reset();
        
        // Send 24-bit RGB background SGR sequence: ESC[48;2;r;g;bm
        terminal.pushPtyText(`\x1b[48;2;${r};${g};${b}mG`);
        
        // Get the snapshot and verify SGR state
        const snapshot = terminal.getSnapshot();
        const cell = snapshot.cells[0][0];
        
        expect(cell.ch).toBe('G');
        expect(cell.sgrState).toBeDefined();
        expect(cell.sgrState!.backgroundColor).toEqual({
          type: 'rgb',
          r: r,
          g: g,
          b: b
        });

        // Generate CSS and verify it contains exact RGB background color
        const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
        expect(css).toContain(`background-color: rgb(${r}, ${g}, ${b})`);
      });
    });
  });

  describe('Text Styling Attributes', () => {
    it('should apply bold styling correctly through terminal processing', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send bold SGR sequence: ESC[1m
      terminal.pushPtyText('\x1b[1mH');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('H');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.bold).toBe(true);

      // Generate CSS and verify it contains bold styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('font-weight: bold');
    });

    it('should apply italic styling correctly through terminal processing', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send italic SGR sequence: ESC[3m
      terminal.pushPtyText('\x1b[3mI');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('I');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.italic).toBe(true);

      // Generate CSS and verify it contains italic styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('font-style: italic');
    });

    it('should apply underline styling correctly through terminal processing', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send underline SGR sequence: ESC[4m
      terminal.pushPtyText('\x1b[4mJ');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('J');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.underline).toBe(true);
      expect(cell.sgrState!.underlineStyle).toBe('single');

      // Generate CSS and verify it contains underline styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('text-decoration: underline');
    });

    it('should apply strikethrough styling correctly through terminal processing', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send strikethrough SGR sequence: ESC[9m
      terminal.pushPtyText('\x1b[9mK');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('K');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.strikethrough).toBe(true);

      // Generate CSS and verify it contains strikethrough styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('text-decoration: line-through');
    });

    it('should apply combined text styling correctly', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send combined SGR sequence: ESC[1;3;4m (bold + italic + underline)
      terminal.pushPtyText('\x1b[1;3;4mL');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('L');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.bold).toBe(true);
      expect(cell.sgrState!.italic).toBe(true);
      expect(cell.sgrState!.underline).toBe(true);

      // Generate CSS and verify it contains all styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('font-weight: bold');
      expect(css).toContain('font-style: italic');
      expect(css).toContain('text-decoration: underline');
    });
  });

  describe('Complex SGR Sequences', () => {
    it('should handle complex combined SGR sequences correctly', () => {
      // Reset terminal state
      terminal.reset();
      
      // Send complex SGR sequence: bold + red foreground + blue background
      terminal.pushPtyText('\x1b[1;31;44mM');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('M');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.bold).toBe(true);
      expect(cell.sgrState!.foregroundColor).toEqual({
        type: 'named',
        color: 'red'
      });
      expect(cell.sgrState!.backgroundColor).toEqual({
        type: 'named',
        color: 'blue'
      });

      // Generate CSS and verify it contains all styling
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('font-weight: bold');
      expect(css).toContain('color: var(--terminal-color-red)');
      expect(css).toContain('background-color: var(--terminal-color-blue)');
    });

    it('should handle SGR reset correctly', () => {
      // Reset terminal state
      terminal.reset();
      
      // Apply styling, then reset
      terminal.pushPtyText('\x1b[1;31mN\x1b[0mO');
      
      // Get the snapshots and verify SGR states
      const snapshot = terminal.getSnapshot();
      
      // First character should have styling
      const styledCell = snapshot.cells[0][0];
      expect(styledCell.ch).toBe('N');
      expect(styledCell.sgrState!.bold).toBe(true);
      expect(styledCell.sgrState!.foregroundColor).toEqual({
        type: 'named',
        color: 'red'
      });

      // Second character should have default styling
      const resetCell = snapshot.cells[0][1];
      expect(resetCell.ch).toBe('O');
      expect(resetCell.sgrState).toEqual(createDefaultSgrState());
    });
  });

  describe('Theme Color Resolution', () => {
    it('should resolve theme colors correctly with CSS variables', () => {
      // Reset terminal state
      terminal.reset();
      
      // Apply a standard color that should use CSS variables
      terminal.pushPtyText('\x1b[31mP');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('P');
      expect(cell.sgrState).toBeDefined();
      expect(cell.sgrState!.foregroundColor).toEqual({
        type: 'named',
        color: 'red'
      });

      // Generate CSS and verify it uses CSS variable from theme
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('color: var(--terminal-color-red)');

      // Verify the theme manager has the correct CSS variable
      const themeCss = themeManager.generateCssVariables();
      expect(themeCss).toContain('--terminal-color-red: #AA0000');
    });

    it('should handle theme switching correctly', () => {
      // Create a custom theme with different colors
      const customTheme = {
        name: 'Custom Test Theme',
        type: 'light' as const,
        colors: {
          ...DEFAULT_DARK_THEME.colors,
          red: '#FF0000', // Different red color
        }
      };

      // Apply custom theme
      themeManager.applyTheme(customTheme);

      // Reset terminal state
      terminal.reset();
      
      // Apply red color
      terminal.pushPtyText('\x1b[31mQ');
      
      // Get the snapshot and verify SGR state
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0];
      
      expect(cell.ch).toBe('Q');
      expect(cell.sgrState!.foregroundColor).toEqual({
        type: 'named',
        color: 'red'
      });

      // CSS should still use the same variable name
      const css = sgrStyleManager.generateCssForSgr(cell.sgrState!);
      expect(css).toContain('color: var(--terminal-color-red)');

      // But theme CSS should have the new color value
      const themeCss = themeManager.generateCssVariables();
      expect(themeCss).toContain('--terminal-color-red: #FF0000');
    });
  });

  describe('CSS Class Generation and Caching', () => {
    it('should generate consistent CSS class names for identical SGR states', () => {
      // Reset terminal state
      terminal.reset();
      
      // Apply the same styling to multiple characters
      terminal.pushPtyText('\x1b[1;31mRST');
      
      // Get the snapshot
      const snapshot = terminal.getSnapshot();
      
      // All three characters should have the same SGR state
      const cell1 = snapshot.cells[0][0];
      const cell2 = snapshot.cells[0][1];
      const cell3 = snapshot.cells[0][2];
      
      expect(cell1.ch).toBe('R');
      expect(cell2.ch).toBe('S');
      expect(cell3.ch).toBe('T');

      // Generate CSS classes for each cell
      const class1 = sgrStyleManager.getStyleClass(cell1.sgrState!);
      const class2 = sgrStyleManager.getStyleClass(cell2.sgrState!);
      const class3 = sgrStyleManager.getStyleClass(cell3.sgrState!);

      // All should have the same class name (caching working)
      expect(class1).toBe(class2);
      expect(class2).toBe(class3);
      expect(class1).toMatch(/^sgr-[a-f0-9]+$/);
    });

    it('should generate different CSS class names for different SGR states', () => {
      // Reset terminal state
      terminal.reset();
      
      // Apply different styling to different characters
      terminal.pushPtyText('\x1b[1mU\x1b[31mV\x1b[44mW');
      
      // Get the snapshot
      const snapshot = terminal.getSnapshot();
      
      const cell1 = snapshot.cells[0][0]; // Bold
      const cell2 = snapshot.cells[0][1]; // Bold + Red
      const cell3 = snapshot.cells[0][2]; // Bold + Red + Blue background
      
      // Generate CSS classes for each cell
      const class1 = sgrStyleManager.getStyleClass(cell1.sgrState!);
      const class2 = sgrStyleManager.getStyleClass(cell2.sgrState!);
      const class3 = sgrStyleManager.getStyleClass(cell3.sgrState!);

      // All should have different class names
      expect(class1).not.toBe(class2);
      expect(class2).not.toBe(class3);
      expect(class1).not.toBe(class3);
      
      // All should follow the naming pattern
      expect(class1).toMatch(/^sgr-[a-f0-9]+$/);
      expect(class2).toMatch(/^sgr-[a-f0-9]+$/);
      expect(class3).toMatch(/^sgr-[a-f0-9]+$/);
    });
  });

  describe('DOM Integration Simulation', () => {
    it('should simulate DOM element class updates correctly', () => {
      // Create mock DOM elements
      const mockCell1 = new MockElement('span');
      const mockCell2 = new MockElement('span');
      
      // Reset terminal state
      terminal.reset();
      
      // Apply styling and get SGR states
      terminal.pushPtyText('\x1b[1;31mX\x1b[0mY');
      
      const snapshot = terminal.getSnapshot();
      const styledCell = snapshot.cells[0][0];
      const defaultCell = snapshot.cells[0][1];
      
      // Update mock DOM elements with SGR styling
      sgrStyleManager.updateCellClasses(mockCell1 as any, styledCell.sgrState!);
      sgrStyleManager.updateCellClasses(mockCell2 as any, defaultCell.sgrState!);
      
      // Verify styled cell has SGR class
      const styledClass = sgrStyleManager.getStyleClass(styledCell.sgrState!);
      expect(mockCell1.classList.contains(styledClass)).toBe(true);
      
      // Verify default cell has no SGR classes
      const hasAnySgrClass = Array.from(mockCell2.classList).some(cls => cls.startsWith('sgr-'));
      expect(hasAnySgrClass).toBe(false);
    });
  });
});