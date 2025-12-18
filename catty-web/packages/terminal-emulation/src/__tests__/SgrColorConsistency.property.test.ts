/**
 * Property-based tests for SGR color consistency
 * **Feature: xterm-extensions, Property 16: SGR color application consistency**
 * **Validates: Requirements 3.4, 4.1**
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { SgrStyleManager, createDefaultSgrState } from '../terminal/SgrStyleManager';
import { ColorResolver } from '../terminal/ColorResolver';
import type { SgrColorType, SgrNamedColor } from '../terminal/TerminalEmulationTypes';

// Generators for different color types
const namedColorArb = fc.constantFrom(
  'black', 'red', 'green', 'yellow', 'blue', 'magenta', 'cyan', 'white',
  'brightBlack', 'brightRed', 'brightGreen', 'brightYellow', 
  'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
) as fc.Arbitrary<SgrNamedColor>;

const indexedColorArb = fc.integer({ min: 0, max: 255 });

const rgbColorArb = fc.record({
  r: fc.integer({ min: 0, max: 255 }),
  g: fc.integer({ min: 0, max: 255 }),
  b: fc.integer({ min: 0, max: 255 })
});

const sgrColorTypeArb = fc.oneof(
  fc.record({ type: fc.constant('named' as const), color: namedColorArb }),
  fc.record({ type: fc.constant('indexed' as const), index: indexedColorArb }),
  fc.record({ type: fc.constant('rgb' as const), r: rgbColorArb.map(c => c.r), g: rgbColorArb.map(c => c.g), b: rgbColorArb.map(c => c.b) })
);

const sgrStateArb = fc.record({
  bold: fc.boolean(),
  faint: fc.boolean(),
  italic: fc.boolean(),
  underline: fc.boolean(),
  underlineStyle: fc.oneof(
    fc.constant(null),
    fc.constantFrom('single', 'double', 'curly', 'dotted', 'dashed')
  ),
  blink: fc.boolean(),
  inverse: fc.boolean(),
  hidden: fc.boolean(),
  strikethrough: fc.boolean(),
  foregroundColor: fc.oneof(fc.constant(null), sgrColorTypeArb),
  backgroundColor: fc.oneof(fc.constant(null), sgrColorTypeArb),
  underlineColor: fc.oneof(fc.constant(null), sgrColorTypeArb),
  font: fc.integer({ min: 0, max: 9 })
});

describe('SGR Color Consistency Property Tests', () => {
  it('Property 16: SGR color application consistency - Color resolution should be deterministic', () => {
    fc.assert(
      fc.property(sgrColorTypeArb, (colorType) => {
        // Resolve the same color multiple times
        const result1 = ColorResolver.resolve(colorType);
        const result2 = ColorResolver.resolve(colorType);
        
        // Should always return the same result
        expect(result1).toBe(result2);
        
        // Result should be a valid CSS color value
        expect(typeof result1).toBe('string');
        expect(result1.length).toBeGreaterThan(0);
        
        // Should match expected patterns based on color type
        switch (colorType.type) {
          case 'named':
            expect(result1).toMatch(/^var\(--terminal-color-[\w-]+\)$/);
            break;
          case 'rgb':
            expect(result1).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
            break;
          case 'indexed':
            // Can be either CSS variable or RGB depending on index
            expect(result1).toMatch(/^(var\(--terminal-color-[\w-]+\)|rgb\(\d+, \d+, \d+\))$/);
            break;
        }
      }),
      { numRuns: 100 }
    );
  });

  it('Property 16: SGR color application consistency - CSS generation should be consistent for same SGR state', () => {
    fc.assert(
      fc.property(sgrStateArb, (sgrState) => {
        const styleManager = new SgrStyleManager();
        
        // Generate CSS for the same state multiple times
        const css1 = styleManager.generateCssForSgr(sgrState);
        const css2 = styleManager.generateCssForSgr(sgrState);
        
        // Should always generate the same CSS
        expect(css1).toBe(css2);
        
        // CSS should be a string
        expect(typeof css1).toBe('string');
        
        // If state has colors, CSS should contain color properties
        if (sgrState.foregroundColor) {
          expect(css1).toMatch(/color:/);
        }
        
        if (sgrState.backgroundColor) {
          expect(css1).toMatch(/background-color:/);
        }
        
        if (sgrState.underlineColor) {
          expect(css1).toMatch(/text-decoration-color:/);
        }
        
        // If state has text styling, CSS should contain appropriate properties
        if (sgrState.bold) {
          expect(css1).toMatch(/font-weight:\s*bold/);
        }
        
        if (sgrState.italic) {
          expect(css1).toMatch(/font-style:\s*italic/);
        }
        
        if (sgrState.underline && sgrState.underlineStyle) {
          expect(css1).toMatch(/text-decoration:\s*underline/);
        }
      }),
      { numRuns: 100 }
    );
  });

  it('Property 16: SGR color application consistency - Style class generation should be deterministic', () => {
    fc.assert(
      fc.property(sgrStateArb, (sgrState) => {
        const styleManager = new SgrStyleManager();
        
        // Generate style class for the same state multiple times
        const class1 = styleManager.getStyleClass(sgrState);
        const class2 = styleManager.getStyleClass(sgrState);
        
        // Should always generate the same class name
        expect(class1).toBe(class2);
        
        // Class name should be a string
        expect(typeof class1).toBe('string');
        
        // Class name should start with 'sgr-' prefix
        expect(class1).toMatch(/^sgr-[a-f0-9]+$/);
      }),
      { numRuns: 100 }
    );
  });

  it('Property 16: SGR color application consistency - Default state should generate empty or minimal CSS', () => {
    const styleManager = new SgrStyleManager();
    const defaultState = createDefaultSgrState();
    
    const css = styleManager.generateCssForSgr(defaultState);
    
    // Default state should generate empty CSS or minimal CSS
    expect(css).toBe('');
  });

  it('Property 16: SGR color application consistency - Named colors should map to correct CSS variables', () => {
    fc.assert(
      fc.property(namedColorArb, (namedColor) => {
        const colorType: SgrColorType = { type: 'named', color: namedColor };
        const result = ColorResolver.resolve(colorType);
        
        // Should be a CSS variable
        expect(result).toMatch(/^var\(--terminal-color-/);
        
        // Should contain the color name (with potential bright prefix)
        const expectedPattern = namedColor.startsWith('bright') 
          ? `--terminal-color-${namedColor.replace(/([A-Z])/g, '-$1').toLowerCase()}`
          : `--terminal-color-${namedColor}`;
        
        expect(result).toContain(expectedPattern);
      }),
      { numRuns: 50 }
    );
  });

  it('Property 16: SGR color application consistency - RGB colors should generate valid RGB CSS', () => {
    fc.assert(
      fc.property(rgbColorArb, (rgb) => {
        const colorType: SgrColorType = { type: 'rgb', r: rgb.r, g: rgb.g, b: rgb.b };
        const result = ColorResolver.resolve(colorType);
        
        // Should be RGB format
        expect(result).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
        
        // Should contain the exact RGB values
        expect(result).toBe(`rgb(${rgb.r}, ${rgb.g}, ${rgb.b})`);
      }),
      { numRuns: 100 }
    );
  });

  it('Property 16: SGR color application consistency - Indexed colors should resolve consistently', () => {
    fc.assert(
      fc.property(indexedColorArb, (index) => {
        const colorType: SgrColorType = { type: 'indexed', index };
        const result1 = ColorResolver.resolve(colorType);
        const result2 = ColorResolver.resolve(colorType);
        
        // Should be deterministic
        expect(result1).toBe(result2);
        
        // Should be valid CSS color
        expect(result1).toMatch(/^(var\(--terminal-color-[\w-]+\)|rgb\(\d+, \d+, \d+\))$/);
        
        // Standard colors (0-15) should use CSS variables
        if (index >= 0 && index <= 15) {
          expect(result1).toMatch(/^var\(--terminal-color-[\w-]+\)$/);
        }
        
        // Color cube and grayscale (16-255) should use RGB
        if (index >= 16 && index <= 255) {
          expect(result1).toMatch(/^rgb\(\d+, \d+, \d+\)$/);
        }
      }),
      { numRuns: 100 }
    );
  });
});