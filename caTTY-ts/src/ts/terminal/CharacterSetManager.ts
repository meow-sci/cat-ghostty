/**
 * Character Set Manager for handling VT100/xterm character set designations and mappings.
 * Manages G0-G3 character set designations and SI/SO switching.
 */

import { CharacterSet, type CharacterSetState } from './types.js';

/**
 * DEC Special Graphics character mapping.
 * Maps ASCII characters to line-drawing glyphs when DEC Special Graphics is active.
 */
const DEC_SPECIAL_GRAPHICS_MAP: Record<string, string> = {
  // Line drawing characters
  'j': '┘',  // Lower right corner
  'k': '┐',  // Upper right corner
  'l': '┌',  // Upper left corner
  'm': '└',  // Lower left corner
  'n': '┼',  // Crossing lines
  'o': '⎺',  // Horizontal line - scan 1
  'p': '⎻',  // Horizontal line - scan 3
  'q': '─',  // Horizontal line
  'r': '⎼',  // Horizontal line - scan 7
  's': '⎽',  // Horizontal line - scan 9
  't': '├',  // Left tee
  'u': '┤',  // Right tee
  'v': '┴',  // Bottom tee
  'w': '┬',  // Top tee
  'x': '│',  // Vertical line
  'y': '≤',  // Less than or equal
  'z': '≥',  // Greater than or equal
  '{': 'π',  // Pi
  '|': '≠',  // Not equal
  '}': '£',  // UK pound sign
  '~': '·',  // Bullet
  
  // Additional special characters
  '_': ' ',  // Blank (space)
  '`': '◆',  // Diamond
  'a': '▒',  // Checkerboard (stipple)
  'b': '␉',  // HT symbol
  'c': '␌',  // FF symbol
  'd': '␍',  // CR symbol
  'e': '␊',  // LF symbol
  'f': '°',  // Degree symbol
  'g': '±',  // Plus/minus
  'h': '␤',  // NL symbol
  'i': '␋'   // VT symbol
};

/**
 * Character Set Manager handles character set designations and character mapping.
 */
export class CharacterSetManager {
  /** Current character set state */
  private state: CharacterSetState;
  
  /**
   * Create a new character set manager with default state.
   */
  constructor() {
    this.state = this.getDefaultState();
  }
  
  /**
   * Get the default character set state.
   */
  private getDefaultState(): CharacterSetState {
    return {
      g0: CharacterSet.ASCII,
      g1: CharacterSet.ASCII,
      g2: CharacterSet.ASCII,
      g3: CharacterSet.ASCII,
      activeGL: 'g0',
      activeGR: 'g2'
    };
  }
  
  /**
   * Reset character sets to default state.
   */
  reset(): void {
    this.state = this.getDefaultState();
  }
  
  /**
   * Designate a character set to one of the G0-G3 slots.
   * @param slot The G slot to designate (0-3)
   * @param charset The character set to designate
   */
  designateCharacterSet(slot: number, charset: CharacterSet): void {
    switch (slot) {
      case 0:
        this.state.g0 = charset;
        break;
      case 1:
        this.state.g1 = charset;
        break;
      case 2:
        this.state.g2 = charset;
        break;
      case 3:
        this.state.g3 = charset;
        break;
    }
  }
  
  /**
   * Handle Shift In (SI) control character.
   * Activates G0 character set for GL (left side).
   */
  shiftIn(): void {
    this.state.activeGL = 'g0';
  }
  
  /**
   * Handle Shift Out (SO) control character.
   * Activates G1 character set for GL (left side).
   */
  shiftOut(): void {
    this.state.activeGL = 'g1';
  }
  
  /**
   * Map a character according to the current active character set.
   * @param char The input character to map
   * @returns The mapped character
   */
  mapCharacter(char: string): string {
    // Only map single ASCII characters
    if (char.length !== 1 || char.charCodeAt(0) > 127) {
      return char;
    }
    
    // Get the active character set for GL (left side, 0x20-0x7F)
    const activeCharset = this.getActiveCharacterSet();
    
    // Apply character set mapping
    switch (activeCharset) {
      case CharacterSet.DECSpecialGraphics:
        return DEC_SPECIAL_GRAPHICS_MAP[char] || char;
        
      case CharacterSet.ASCII:
      default:
        // ASCII and other character sets pass through unchanged for now
        return char;
    }
  }
  
  /**
   * Get the currently active character set for GL (left side).
   */
  private getActiveCharacterSet(): CharacterSet {
    const activeSlot = this.state.activeGL;
    return this.state[activeSlot];
  }
  
  /**
   * Get the current character set state (for testing/debugging).
   */
  getState(): Readonly<CharacterSetState> {
    return { ...this.state };
  }
  
  /**
   * Parse a character set designation escape sequence.
   * Handles sequences like ESC ( B (designate ASCII to G0).
   * @param intermediates The intermediate characters
   * @param final The final character
   */
  parseDesignationSequence(intermediates: string, final: number): void {
    const finalChar = String.fromCharCode(final);
    
    // Determine which G slot based on intermediate character
    let slot = 0;
    if (intermediates.length > 0) {
      const intermediate = intermediates[0];
      switch (intermediate) {
        case '(':  // G0
          slot = 0;
          break;
        case ')':  // G1
          slot = 1;
          break;
        case '*':  // G2
          slot = 2;
          break;
        case '+':  // G3
          slot = 3;
          break;
        default:
          // Unknown intermediate - ignore
          return;
      }
    }
    
    // Map final character to character set
    let charset: CharacterSet;
    switch (finalChar) {
      case 'B':
        charset = CharacterSet.ASCII;
        break;
      case '0':
        charset = CharacterSet.DECSpecialGraphics;
        break;
      case 'A':
        charset = CharacterSet.UK;
        break;
      case '1':
        charset = CharacterSet.DECAlternateSpecial;
        break;
      case '>':
        charset = CharacterSet.DECTechnical;
        break;
      default:
        // Unknown character set - ignore
        return;
    }
    
    this.designateCharacterSet(slot, charset);
  }
}