/**
 * Unit tests for CharacterSetManager.
 */

import { describe, it, expect } from 'vitest';
import { CharacterSetManager } from '../CharacterSetManager.js';
import { CharacterSet } from '../types.js';

describe('CharacterSetManager', () => {
  describe('initialization', () => {
    it('should initialize with default ASCII character sets', () => {
      const manager = new CharacterSetManager();
      const state = manager.getState();
      
      expect(state.g0).toBe(CharacterSet.ASCII);
      expect(state.g1).toBe(CharacterSet.ASCII);
      expect(state.g2).toBe(CharacterSet.ASCII);
      expect(state.g3).toBe(CharacterSet.ASCII);
      expect(state.activeGL).toBe('g0');
      expect(state.activeGR).toBe('g2');
    });
  });
  
  describe('character set designation', () => {
    it('should designate character sets to G0-G3 slots', () => {
      const manager = new CharacterSetManager();
      
      manager.designateCharacterSet(0, CharacterSet.DECSpecialGraphics);
      manager.designateCharacterSet(1, CharacterSet.UK);
      manager.designateCharacterSet(2, CharacterSet.DECAlternate);
      manager.designateCharacterSet(3, CharacterSet.DECTechnical);
      
      const state = manager.getState();
      expect(state.g0).toBe(CharacterSet.DECSpecialGraphics);
      expect(state.g1).toBe(CharacterSet.UK);
      expect(state.g2).toBe(CharacterSet.DECAlternate);
      expect(state.g3).toBe(CharacterSet.DECTechnical);
    });
  });
  
  describe('shift in/out', () => {
    it('should switch between G0 and G1 with SI/SO', () => {
      const manager = new CharacterSetManager();
      
      // Initially G0 is active
      expect(manager.getState().activeGL).toBe('g0');
      
      // Shift out to G1
      manager.shiftOut();
      expect(manager.getState().activeGL).toBe('g1');
      
      // Shift in to G0
      manager.shiftIn();
      expect(manager.getState().activeGL).toBe('g0');
    });
  });
  
  describe('character mapping', () => {
    it('should pass through ASCII characters when ASCII is active', () => {
      const manager = new CharacterSetManager();
      
      expect(manager.mapCharacter('A')).toBe('A');
      expect(manager.mapCharacter('z')).toBe('z');
      expect(manager.mapCharacter('0')).toBe('0');
      expect(manager.mapCharacter(' ')).toBe(' ');
    });
    
    it('should map DEC Special Graphics characters', () => {
      const manager = new CharacterSetManager();
      
      // Designate DEC Special Graphics to G0
      manager.designateCharacterSet(0, CharacterSet.DECSpecialGraphics);
      
      // Test some line drawing characters
      expect(manager.mapCharacter('q')).toBe('─'); // Horizontal line
      expect(manager.mapCharacter('x')).toBe('│'); // Vertical line
      expect(manager.mapCharacter('l')).toBe('┌'); // Upper left corner
      expect(manager.mapCharacter('k')).toBe('┐'); // Upper right corner
      expect(manager.mapCharacter('m')).toBe('└'); // Lower left corner
      expect(manager.mapCharacter('j')).toBe('┘'); // Lower right corner
      expect(manager.mapCharacter('n')).toBe('┼'); // Crossing lines
      
      // Test characters not in the mapping
      expect(manager.mapCharacter('A')).toBe('A');
    });
    
    it('should only map single ASCII characters', () => {
      const manager = new CharacterSetManager();
      manager.designateCharacterSet(0, CharacterSet.DECSpecialGraphics);
      
      // Multi-character strings should pass through
      expect(manager.mapCharacter('qq')).toBe('qq');
      
      // Unicode characters should pass through
      expect(manager.mapCharacter('π')).toBe('π');
      expect(manager.mapCharacter('🚀')).toBe('🚀');
    });
    
    it('should use the active character set for mapping', () => {
      const manager = new CharacterSetManager();
      
      // Set up different character sets
      manager.designateCharacterSet(0, CharacterSet.ASCII);
      manager.designateCharacterSet(1, CharacterSet.DECSpecialGraphics);
      
      // Initially G0 (ASCII) is active
      expect(manager.mapCharacter('q')).toBe('q');
      
      // Switch to G1 (DEC Special Graphics)
      manager.shiftOut();
      expect(manager.mapCharacter('q')).toBe('─');
      
      // Switch back to G0 (ASCII)
      manager.shiftIn();
      expect(manager.mapCharacter('q')).toBe('q');
    });
  });
  
  describe('escape sequence parsing', () => {
    it('should parse G0 designation sequences', () => {
      const manager = new CharacterSetManager();
      
      // ESC ( B - designate ASCII to G0
      manager.parseDesignationSequence('(', 'B'.charCodeAt(0));
      expect(manager.getState().g0).toBe(CharacterSet.ASCII);
      
      // ESC ( 0 - designate DEC Special Graphics to G0
      manager.parseDesignationSequence('(', '0'.charCodeAt(0));
      expect(manager.getState().g0).toBe(CharacterSet.DECSpecialGraphics);
    });
    
    it('should parse G1 designation sequences', () => {
      const manager = new CharacterSetManager();
      
      // ESC ) A - designate UK to G1
      manager.parseDesignationSequence(')', 'A'.charCodeAt(0));
      expect(manager.getState().g1).toBe(CharacterSet.UK);
      
      // ESC ) 0 - designate DEC Special Graphics to G1
      manager.parseDesignationSequence(')', '0'.charCodeAt(0));
      expect(manager.getState().g1).toBe(CharacterSet.DECSpecialGraphics);
    });
    
    it('should parse G2 and G3 designation sequences', () => {
      const manager = new CharacterSetManager();
      
      // ESC * > - designate DEC Technical to G2
      manager.parseDesignationSequence('*', '>'.charCodeAt(0));
      expect(manager.getState().g2).toBe(CharacterSet.DECTechnical);
      
      // ESC + 1 - designate DEC Alternate Special to G3
      manager.parseDesignationSequence('+', '1'.charCodeAt(0));
      expect(manager.getState().g3).toBe(CharacterSet.DECAlternateSpecial);
    });
    
    it('should ignore unknown intermediate characters', () => {
      const manager = new CharacterSetManager();
      const initialState = manager.getState();
      
      // Unknown intermediate should be ignored
      manager.parseDesignationSequence('?', 'B'.charCodeAt(0));
      expect(manager.getState()).toEqual(initialState);
    });
    
    it('should ignore unknown final characters', () => {
      const manager = new CharacterSetManager();
      const initialState = manager.getState();
      
      // Unknown final character should be ignored
      manager.parseDesignationSequence('(', 'Z'.charCodeAt(0));
      expect(manager.getState()).toEqual(initialState);
    });
  });
  
  describe('reset', () => {
    it('should reset all character sets to ASCII', () => {
      const manager = new CharacterSetManager();
      
      // Set up non-default state
      manager.designateCharacterSet(0, CharacterSet.DECSpecialGraphics);
      manager.designateCharacterSet(1, CharacterSet.UK);
      manager.shiftOut();
      
      // Verify non-default state
      expect(manager.getState().g0).toBe(CharacterSet.DECSpecialGraphics);
      expect(manager.getState().g1).toBe(CharacterSet.UK);
      expect(manager.getState().activeGL).toBe('g1');
      
      // Reset
      manager.reset();
      
      // Verify default state
      const state = manager.getState();
      expect(state.g0).toBe(CharacterSet.ASCII);
      expect(state.g1).toBe(CharacterSet.ASCII);
      expect(state.g2).toBe(CharacterSet.ASCII);
      expect(state.g3).toBe(CharacterSet.ASCII);
      expect(state.activeGL).toBe('g0');
      expect(state.activeGR).toBe('g2');
    });
  });
});