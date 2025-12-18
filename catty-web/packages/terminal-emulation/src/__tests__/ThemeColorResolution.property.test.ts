/**
 * Property-based tests for theme color resolution consistency
 * **Feature: xterm-extensions, Property 18: Theme color resolution consistency**
 * **Validates: Requirements 3.4, 4.1**
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { ThemeManager, DEFAULT_DARK_THEME, type TerminalTheme, type TerminalColorPalette } from '../terminal/TerminalTheme';
import { ColorResolver, ANSI_COLOR_VARIABLES } from '../terminal/ColorResolver';
import type { SgrNamedColor } from '../terminal/TerminalEmulationTypes';

// Generator for valid hex colors
const hexColorArb = fc.array(fc.integer({ min: 0, max: 15 }), { minLength: 6, maxLength: 6 })
  .map(digits => `#${digits.map(d => d.toString(16)).join('')}`);

// Generator for valid RGB colors
const rgbColorArb = fc.record({
  r: fc.integer({ min: 0, max: 255 }),
  g: fc.integer({ min: 0, max: 255 }),
  b: fc.integer({ min: 0, max: 255 })
}).map(rgb => `rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`);

// Generator for CSS color values
const cssColorArb = fc.oneof(hexColorArb, rgbColorArb);

// Generator for terminal color palette
const colorPaletteArb = fc.record({
  black: cssColorArb,
  red: cssColorArb,
  green: cssColorArb,
  yellow: cssColorArb,
  blue: cssColorArb,
  magenta: cssColorArb,
  cyan: cssColorArb,
  white: cssColorArb,
  brightBlack: cssColorArb,
  brightRed: cssColorArb,
  brightGreen: cssColorArb,
  brightYellow: cssColorArb,
  brightBlue: cssColorArb,
  brightMagenta: cssColorArb,
  brightCyan: cssColorArb,
  brightWhite: cssColorArb,
  foreground: cssColorArb,
  background: cssColorArb,
  cursor: cssColorArb,
  selection: cssColorArb,
}) as fc.Arbitrary<TerminalColorPalette>;

// Generator for terminal themes
const terminalThemeArb = fc.record({
  name: fc.string({ minLength: 1, maxLength: 50 }),
  type: fc.constantFrom('dark', 'light') as fc.Arbitrary<'dark' | 'light'>,
  colors: colorPaletteArb
}) as fc.Arbitrary<TerminalTheme>;

// Generator for ANSI color codes
const ansiColorCodeArb = fc.oneof(
  fc.integer({ min: 0, max: 15 }), // Standard 16 colors
  fc.integer({ min: 16, max: 255 }) // Extended colors
);

// Generator for named colors
const namedColorArb = fc.constantFrom(
  'black', 'red', 'green', 'yellow', 'blue', 'magenta', 'cyan', 'white',
  'brightBlack', 'brightRed', 'brightGreen', 'brightYellow', 
  'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
) as fc.Arbitrary<SgrNamedColor>;

describe('Theme Color Resolution Property Tests', () => {
  it('Property 18: Theme color resolution consistency - CSS variable generation should be deterministic', () => {
    fc.assert(
      fc.property(terminalThemeArb, (theme) => {
        const themeManager = new ThemeManager(theme);
        
        // Generate CSS variables multiple times
        const css1 = themeManager.generateCssVariables();
        const css2 = themeManager.generateCssVariables();
        
        // Should always generate the same CSS
        expect(css1).toBe(css2);
        
        // CSS should be valid
        expect(css1).toMatch(/^:root\s*\{[\s\S]*\}$/);
        
        // Should contain all expected color variables
        Object.keys(theme.colors).forEach(colorName => {
          const expectedVar = `--terminal-color-${colorName.replace(/([A-Z])/g, '-$1').toLowerCase()}`;
          if (colorName === 'foreground' || colorName === 'background' || colorName === 'cursor' || colorName === 'selection') {
            const expectedVar = `--terminal-${colorName}`;
            expect(css1).toContain(expectedVar);
          } else {
            expect(css1).toContain(expectedVar);
          }
        });
      }),
      { numRuns: 50 }
    );
  });

  it('Property 18: Theme color resolution consistency - Named color resolution should map to correct CSS variables', () => {
    fc.assert(
      fc.property(namedColorArb, (namedColor) => {
        const colorType = { type: 'named' as const, color: namedColor };
        const resolvedColor = ColorResolver.resolve(colorType);
        
        // Should resolve to a CSS variable
        expect(resolvedColor).toMatch(/^var\(--terminal-color-[\w-]+\)$/);
        
        // Should match the expected variable name from ANSI_COLOR_VARIABLES
        const expectedVariable = ANSI_COLOR_VARIABLES[namedColor];
        expect(resolvedColor).toBe(expectedVariable);
        
        // Variable name should be consistent
        const variableName = resolvedColor.match(/var\((--terminal-color-[\w-]+)\)/)?.[1];
        expect(variableName).toBeTruthy();
        
        // Should contain the color name (with potential transformations)
        const expectedPattern = namedColor.startsWith('bright') 
          ? namedColor.replace(/([A-Z])/g, '-$1').toLowerCase()
          : namedColor;
        expect(variableName).toContain(expectedPattern);
      }),
      { numRuns: 50 }
    );
  });

  it('Property 18: Theme color resolution consistency - Theme application should be idempotent', () => {
    fc.assert(
      fc.property(terminalThemeArb, (theme) => {
        const themeManager = new ThemeManager();
        
        // Apply theme multiple times
        themeManager.applyTheme(theme);
        const css1 = themeManager.generateCssVariables();
        
        themeManager.applyTheme(theme);
        const css2 = themeManager.generateCssVariables();
        
        themeManager.applyTheme(theme);
        const css3 = themeManager.generateCssVariables();
        
        // All should produce the same result
        expect(css1).toBe(css2);
        expect(css2).toBe(css3);
        
        // Current theme should match applied theme
        const currentTheme = themeManager.getCurrentTheme();
        expect(currentTheme).toEqual(theme);
      }),
      { numRuns: 50 }
    );
  });

  it('Property 18: Theme color resolution consistency - Color resolution should be consistent across theme changes', () => {
    fc.assert(
      fc.property(terminalThemeArb, terminalThemeArb, namedColorArb, (theme1, theme2, namedColor) => {
        // Skip if themes are identical
        fc.pre(JSON.stringify(theme1) !== JSON.stringify(theme2));
                
        const colorType = { type: 'named' as const, color: namedColor };
        
        // Color resolution should be independent of theme manager instance
        const resolved1 = ColorResolver.resolve(colorType);
        const resolved2 = ColorResolver.resolve(colorType);
        
        expect(resolved1).toBe(resolved2);
        
        // Should always resolve to the same CSS variable regardless of theme
        expect(resolved1).toBe(ANSI_COLOR_VARIABLES[namedColor]);
      }),
      { numRuns: 50 }
    );
  });

  it('Property 18: Theme color resolution consistency - Default theme should have valid color values', () => {
    const themeManager = new ThemeManager(DEFAULT_DARK_THEME);
    const css = themeManager.generateCssVariables();
    
    // Should generate valid CSS
    expect(css).toMatch(/^:root\s*\{[\s\S]*\}$/);
    
    // All colors in default theme should be valid hex colors
    Object.values(DEFAULT_DARK_THEME.colors).forEach(color => {
      expect(color).toMatch(/^#[0-9A-Fa-f]{6}$/);
    });
    
    // CSS should contain all expected variables
    expect(css).toContain('--terminal-color-black: #000000');
    expect(css).toContain('--terminal-color-red: #AA0000');
    expect(css).toContain('--terminal-foreground: #AAAAAA');
    expect(css).toContain('--terminal-background: #000000');
  });

  it('Property 18: Theme color resolution consistency - ANSI color code resolution should be deterministic', () => {
    fc.assert(
      fc.property(ansiColorCodeArb, (colorCode) => {
        const themeManager = new ThemeManager();
        
        // Resolve color code multiple times
        const resolved1 = themeManager.resolveColor(colorCode);
        const resolved2 = themeManager.resolveColor(colorCode);
        
        // Should be deterministic
        expect(resolved1).toBe(resolved2);
        
        // Should be a valid CSS value
        expect(typeof resolved1).toBe('string');
        expect(resolved1.length).toBeGreaterThan(0);
        
        // Standard colors (0-7) should resolve to CSS variables
        if (colorCode >= 0 && colorCode <= 7) {
          expect(resolved1).toMatch(/^var\(--terminal-color-[\w-]+\)$/);
        } else {
          // Colors outside standard range should return fallback
          expect(resolved1).toBe('var(--terminal-foreground)');
        }
      }),
      { numRuns: 100 }
    );
  });

  it('Property 18: Theme color resolution consistency - CSS variable names should follow consistent naming convention', () => {
    fc.assert(
      fc.property(colorPaletteArb, (palette) => {
        const theme: TerminalTheme = {
          name: 'Test Theme',
          type: 'dark',
          colors: palette
        };
        
        const themeManager = new ThemeManager(theme);
        const css = themeManager.generateCssVariables();
        
        // Extract all CSS variable names
        const variableMatches = css.match(/--terminal-[\w-]+/g) || [];
        
        // All variables should follow naming convention
        variableMatches.forEach(variable => {
          expect(variable).toMatch(/^--terminal-(color-[\w-]+|foreground|background|cursor|selection)$/);
        });
        
        // Should have the expected number of variables (16 colors + 4 UI colors)
        expect(variableMatches.length).toBe(20);
      }),
      { numRuns: 50 }
    );
  });

  it('Property 18: Theme color resolution consistency - Theme switching should update CSS variables', () => {
    fc.assert(
      fc.property(terminalThemeArb, terminalThemeArb, (theme1, theme2) => {
        // Skip if themes have identical colors
        fc.pre(JSON.stringify(theme1.colors) !== JSON.stringify(theme2.colors));
        
        const themeManager = new ThemeManager(theme1);
        const css1 = themeManager.generateCssVariables();
        
        themeManager.applyTheme(theme2);
        const css2 = themeManager.generateCssVariables();
        
        // CSS should be different when themes are different
        expect(css1).not.toBe(css2);
        
        // Both should be valid CSS
        expect(css1).toMatch(/^:root\s*\{[\s\S]*\}$/);
        expect(css2).toMatch(/^:root\s*\{[\s\S]*\}$/);
        
        // Current theme should be theme2
        expect(themeManager.getCurrentTheme()).toEqual(theme2);
      }),
      { numRuns: 50 }
    );
  });
});