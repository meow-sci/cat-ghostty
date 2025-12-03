/**
 * Integration tests for Parser character set handling.
 */

import { describe, it, expect } from 'vitest';
import { Parser } from '../Parser.js';
import { CharacterSet } from '../types.js';

describe('Parser Character Set Integration', () => {
  describe('character set designation sequences', () => {
    it('should handle ESC ( sequences for G0 designation', () => {
      const printedChars: string[] = [];
      const parser = new Parser({
        onPrintable: (char) => printedChars.push(char)
      });
      
      // ESC ( 0 - designate DEC Special Graphics to G0
      const sequence = new Uint8Array([0x1B, 0x28, 0x30]);
      parser.parse(sequence);
      
      // Verify character set was designated
      const manager = parser.getCharacterSetManager();
      expect(manager.getState().g0).toBe(CharacterSet.DECSpecialGraphics);
      
      // Test that line drawing characters are now mapped
      parser.parse(new Uint8Array([0x71])); // 'q'
      expect(printedChars).toEqual(['─']); // Should be horizontal line
    });
    
    it('should handle ESC ) sequences for G1 designation', () => {
      const printedChars: string[] = [];
      const parser = new Parser({
        onPrintable: (char) => printedChars.push(char)
      });
      
      // ESC ) 0 - designate DEC Special Graphics to G1
      const sequence = new Uint8Array([0x1B, 0x29, 0x30]);
      parser.parse(sequence);
      
      // Verify character set was designated
      const manager = parser.getCharacterSetManager();
      expect(manager.getState().g1).toBe(CharacterSet.DECSpecialGraphics);
      
      // Initially G0 is active, so 'q' should be normal
      parser.parse(new Uint8Array([0x71])); // 'q'
      expect(printedChars).toEqual(['q']);
      
      // Shift out to G1
      parser.parse(new Uint8Array([0x0E])); // SO
      
      // Now 'q' should be mapped
      parser.parse(new Uint8Array([0x71])); // 'q'
      expect(printedChars).toEqual(['q', '─']);
    });
    
    it('should handle ESC * and ESC + sequences for G2 and G3', () => {
      const parser = new Parser();
      
      // ESC * > - designate DEC Technical to G2
      parser.parse(new Uint8Array([0x1B, 0x2A, 0x3E]));
      
      // ESC + A - designate UK to G3
      parser.parse(new Uint8Array([0x1B, 0x2B, 0x41]));
      
      const manager = parser.getCharacterSetManager();
      expect(manager.getState().g2).toBe(CharacterSet.DECTechnical);
      expect(manager.getState().g3).toBe(CharacterSet.UK);
    });
  });
  
  describe('shift in/out control characters', () => {
    it('should handle SI (0x0F) control character', () => {
      const shiftInCalls: number[] = [];
      const parser = new Parser({
        onShiftIn: () => shiftInCalls.push(1)
      });
      
      // Send SI
      parser.parse(new Uint8Array([0x0F]));
      
      expect(shiftInCalls).toHaveLength(1);
      expect(parser.getCharacterSetManager().getState().activeGL).toBe('g0');
    });
    
    it('should handle SO (0x0E) control character', () => {
      const shiftOutCalls: number[] = [];
      const parser = new Parser({
        onShiftOut: () => shiftOutCalls.push(1)
      });
      
      // Send SO
      parser.parse(new Uint8Array([0x0E]));
      
      expect(shiftOutCalls).toHaveLength(1);
      expect(parser.getCharacterSetManager().getState().activeGL).toBe('g1');
    });
  });
  
  describe('character mapping integration', () => {
    it('should map characters according to active character set', () => {
      const printedChars: string[] = [];
      const parser = new Parser({
        onPrintable: (char) => printedChars.push(char)
      });
      
      // Set up DEC Special Graphics in G0 and G1
      parser.parse(new Uint8Array([0x1B, 0x28, 0x30])); // ESC ( 0
      parser.parse(new Uint8Array([0x1B, 0x29, 0x42])); // ESC ) B (ASCII)
      
      // Test with G0 active (DEC Special Graphics)
      parser.parse(new Uint8Array([0x71, 0x78, 0x6C])); // 'qxl'
      expect(printedChars).toEqual(['─', '│', '┌']);
      
      // Switch to G1 (ASCII)
      parser.parse(new Uint8Array([0x0E])); // SO
      
      // Same characters should now be ASCII
      parser.parse(new Uint8Array([0x71, 0x78, 0x6C])); // 'qxl'
      expect(printedChars).toEqual(['─', '│', '┌', 'q', 'x', 'l']);
      
      // Switch back to G0 (DEC Special Graphics)
      parser.parse(new Uint8Array([0x0F])); // SI
      
      // Should be line drawing again
      parser.parse(new Uint8Array([0x71])); // 'q'
      expect(printedChars).toEqual(['─', '│', '┌', 'q', 'x', 'l', '─']);
    });
    
    it('should handle complex line drawing sequences', () => {
      const printedChars: string[] = [];
      const parser = new Parser({
        onPrintable: (char) => printedChars.push(char)
      });
      
      // Set up DEC Special Graphics
      parser.parse(new Uint8Array([0x1B, 0x28, 0x30])); // ESC ( 0
      
      // Draw a simple box: ┌─┐
      //                    │ │
      //                    └─┘
      const boxChars = 'lqk\r\nx x\r\nmqj';
      parser.parse(new TextEncoder().encode(boxChars));
      
      const expected = ['┌', '─', '┐', '│', ' ', '│', '└', '─', '┘'];
      expect(printedChars).toEqual(expected);
    });
  });
  
  describe('reset behavior', () => {
    it('should reset character sets when parser is reset', () => {
      const parser = new Parser();
      
      // Set up non-default state
      parser.parse(new Uint8Array([0x1B, 0x28, 0x30])); // ESC ( 0 (DEC Special Graphics to G0)
      parser.parse(new Uint8Array([0x0E])); // SO (switch to G1)
      
      const manager = parser.getCharacterSetManager();
      expect(manager.getState().g0).toBe(CharacterSet.DECSpecialGraphics);
      expect(manager.getState().activeGL).toBe('g1');
      
      // Reset parser
      parser.reset();
      
      // Character sets should be back to default
      expect(manager.getState().g0).toBe(CharacterSet.ASCII);
      expect(manager.getState().g1).toBe(CharacterSet.ASCII);
      expect(manager.getState().activeGL).toBe('g0');
    });
  });
  
  describe('edge cases', () => {
    it('should handle unknown character set designations gracefully', () => {
      const parser = new Parser();
      const initialState = parser.getCharacterSetManager().getState();
      
      // Unknown character set
      parser.parse(new Uint8Array([0x1B, 0x28, 0x5A])); // ESC ( Z
      
      // State should be unchanged
      expect(parser.getCharacterSetManager().getState()).toEqual(initialState);
    });
    
    it('should handle malformed designation sequences gracefully', () => {
      const parser = new Parser();
      const initialState = parser.getCharacterSetManager().getState();
      
      // Malformed sequence (unknown intermediate)
      parser.parse(new Uint8Array([0x1B, 0x3F, 0x42])); // ESC ? B
      
      // State should be unchanged
      expect(parser.getCharacterSetManager().getState()).toEqual(initialState);
    });
    
    it('should not map non-ASCII characters', () => {
      const printedChars: string[] = [];
      const parser = new Parser({
        onPrintable: (char) => printedChars.push(char)
      });
      
      // Set up DEC Special Graphics
      parser.parse(new Uint8Array([0x1B, 0x28, 0x30])); // ESC ( 0
      
      // Unicode characters should pass through unchanged
      const unicodeText = 'π∑∆';
      parser.parse(new TextEncoder().encode(unicodeText));
      
      expect(printedChars).toEqual(['π', '∑', '∆']);
    });
  });
});