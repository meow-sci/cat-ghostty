/**
 * Property-based tests for CSS hash generation determinism
 * **Feature: xterm-extensions, Property 17: CSS hash generation determinism**
 * **Validates: Requirements 4.1, 4.2**
 */

import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { SgrStyleManager } from '../terminal/SgrStyleManager';

// Generator for CSS strings
const cssPropertyArb = fc.oneof(
  fc.record({
    property: fc.constant('color'),
    value: fc.oneof(
      fc.constant('red'),
      fc.constant('blue'),
      fc.constant('var(--terminal-color-red)'),
      fc.string({ minLength: 7, maxLength: 7 }).map(s => `#${s}`)
    )
  }),
  fc.record({
    property: fc.constant('background-color'),
    value: fc.oneof(
      fc.constant('black'),
      fc.constant('white'),
      fc.constant('var(--terminal-color-black)'),
      fc.string({ minLength: 7, maxLength: 7 }).map(s => `#${s}`)
    )
  }),
  fc.record({
    property: fc.constant('font-weight'),
    value: fc.constantFrom('normal', 'bold', '100', '400', '700')
  }),
  fc.record({
    property: fc.constant('font-style'),
    value: fc.constantFrom('normal', 'italic', 'oblique')
  }),
  fc.record({
    property: fc.constant('text-decoration'),
    value: fc.constantFrom('none', 'underline', 'line-through', 'overline')
  })
);

const cssStringArb = fc.array(cssPropertyArb, { minLength: 0, maxLength: 10 })
  .map(properties => 
    properties.map(prop => `${prop.property}: ${prop.value}`).join('; ')
  );

describe('CSS Generation Determinism Property Tests', () => {
  it('Property 17: CSS hash generation determinism - Same CSS string should always generate same hash', () => {
    const manager = new SgrStyleManager();
    
    fc.assert(
      fc.property(cssStringArb, (cssString) => {
        // Generate hash multiple times for the same CSS string
        const hash1 = manager.hashCssString(cssString);
        const hash2 = manager.hashCssString(cssString);
        const hash3 = manager.hashCssString(cssString);
        
        // All hashes should be identical
        expect(hash1).toBe(hash2);
        expect(hash2).toBe(hash3);
        expect(hash1).toBe(hash3);
        
        // Hash should be a string
        expect(typeof hash1).toBe('string');
        
        // Hash should be consistent
        expect(hash1).toBe(hash2);
      }),
      { numRuns: 100 }
    );
  });

  it('Property 17: CSS hash generation determinism - Different CSS strings should generate different hashes', () => {
    const manager = new SgrStyleManager();
    
    fc.assert(
      fc.property(cssStringArb, cssStringArb, (css1, css2) => {
        // Skip if strings are identical
        fc.pre(css1 !== css2);
        
        const hash1 = manager.hashCssString(css1);
        const hash2 = manager.hashCssString(css2);
        
        // Different CSS strings should produce different hashes (with very high probability)
        expect(hash1).not.toBe(hash2);
      }),
      { numRuns: 100 }
    );
  });

  it('Property 17: CSS hash generation determinism - SgrStyleManager should generate consistent class names', () => {
    fc.assert(
      fc.property(cssStringArb, (cssString) => {
        // Skip empty CSS strings as they might be handled specially
        fc.pre(cssString.trim().length > 0);
        
        const styleManager = new SgrStyleManager();
        
        // Mock the generateCssForSgr method to return our test CSS
        const originalGenerate = styleManager.generateCssForSgr;
        styleManager.generateCssForSgr = () => cssString;
        
        try {
          // Generate style class multiple times
          const class1 = styleManager.getStyleClass({} as any);
          const class2 = styleManager.getStyleClass({} as any);
          
          // Should generate the same class name
          expect(class1).toBe(class2);
          
          // Class name should follow expected format
          expect(class1).toMatch(/^sgr-[a-f0-9]+$/);
          
          // Extract hash from class name and verify it's consistent
          const hashPart1 = class1.replace('sgr-', '');
          const hashPart2 = class2.replace('sgr-', '');
          expect(hashPart1).toBe(hashPart2);
          
        } finally {
          // Restore original method
          styleManager.generateCssForSgr = originalGenerate;
        }
      }),
      { numRuns: 50 }
    );
  });

  it('Property 17: CSS hash generation determinism - Hash should be deterministic across multiple calls', () => {
    const manager = new SgrStyleManager();
    
    fc.assert(
      fc.property(cssStringArb, (cssString) => {
        // Generate hash multiple times for the same string
        const hash1 = manager.hashCssString(cssString);
        const hash2 = manager.hashCssString(cssString);
        const hash3 = manager.hashCssString(cssString);
        
        // All should produce the same hash
        expect(hash1).toBe(hash2);
        expect(hash2).toBe(hash3);
      }),
      { numRuns: 100 }
    );
  });

  it('Property 17: CSS hash generation determinism - Empty and whitespace-only strings should be handled consistently', () => {
    const manager = new SgrStyleManager();
    
    const emptyHash1 = manager.hashCssString('');
    const emptyHash2 = manager.hashCssString('');
    expect(emptyHash1).toBe(emptyHash2);
    
    const spaceHash1 = manager.hashCssString('   ');
    const spaceHash2 = manager.hashCssString('   ');
    expect(spaceHash1).toBe(spaceHash2);
    
    // Empty and spaces should produce different hashes
    expect(emptyHash1).not.toBe(spaceHash1);
  });

  it('Property 17: CSS hash generation determinism - Case sensitivity should be preserved in hashing', () => {
    const manager = new SgrStyleManager();
    
    const lowerHash = manager.hashCssString('color: red');
    const upperHash = manager.hashCssString('COLOR: RED');
    const mixedHash = manager.hashCssString('Color: Red');
    
    // Different cases should produce different hashes
    expect(lowerHash).not.toBe(upperHash);
    expect(upperHash).not.toBe(mixedHash);
    expect(lowerHash).not.toBe(mixedHash);
  });

  it('Property 17: CSS hash generation determinism - Whitespace differences should affect hash', () => {
    const manager = new SgrStyleManager();
    
    const normalHash = manager.hashCssString('color: red; font-weight: bold');
    const extraSpaceHash = manager.hashCssString('color:  red;  font-weight:  bold');
    const noSpaceHash = manager.hashCssString('color:red;font-weight:bold');
    
    // Different whitespace should produce different hashes
    expect(normalHash).not.toBe(extraSpaceHash);
    expect(extraSpaceHash).not.toBe(noSpaceHash);
    expect(normalHash).not.toBe(noSpaceHash);
  });

  it('Property 17: CSS hash generation determinism - Hash hex representation should be consistent', () => {
    const manager = new SgrStyleManager();
    
    fc.assert(
      fc.property(cssStringArb, (cssString) => {
        const hash = manager.hashCssString(cssString);
        
        // Hash should be consistent
        const hash2 = manager.hashCssString(cssString);
        expect(hash).toBe(hash2);
        
        // Hash should be valid hexadecimal
        expect(hash).toMatch(/^[0-9a-f]+$/);
        
        // Should be a string
        expect(typeof hash).toBe('string');
      }),
      { numRuns: 100 }
    );
  });
});