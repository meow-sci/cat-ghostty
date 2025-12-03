/**
 * Core data structures for the terminal emulator.
 * These types define the fundamental building blocks of terminal state.
 */

/**
 * Color representation for terminal cells.
 * Supports default colors, 256-color palette, and true color (RGB).
 */
export type Color =
  | { type: 'default' }
  | { type: 'indexed'; index: number }  // 0-255
  | { type: 'rgb'; r: number; g: number; b: number };

/**
 * Underline style options for text.
 * Matches terminal SGR underline capabilities.
 */
export enum UnderlineStyle {
  None = 0,
  Single = 1,
  Double = 2,
  Curly = 3,
  Dotted = 4,
  Dashed = 5
}

/**
 * A single character position in the terminal grid.
 * Contains the character and all its visual attributes.
 */
export interface Cell {
  /** The character (may be empty for wide char continuation) */
  char: string;
  
  /** Character width (1 for normal, 2 for wide, 0 for continuation) */
  width: number;
  
  /** Foreground color */
  fg: Color;
  
  /** Background color */
  bg: Color;
  
  /** Bold text attribute */
  bold: boolean;
  
  /** Italic text attribute */
  italic: boolean;
  
  /** Underline style */
  underline: UnderlineStyle;
  
  /** Inverse/reverse video attribute */
  inverse: boolean;
  
  /** Strikethrough attribute */
  strikethrough: boolean;
  
  /** Hyperlink URL (OSC 8) */
  url?: string;
}

/**
 * A single line in the terminal.
 * Contains an array of cells and metadata about line wrapping.
 */
export interface Line {
  /** Array of cells in this line */
  cells: Cell[];
  
  /** True if this line was wrapped from the previous line */
  wrapped: boolean;
}

/**
 * Cursor position and appearance state.
 */
export interface CursorState {
  /** Current row (0-based) */
  row: number;
  
  /** Current column (0-based) */
  col: number;
  
  /** Whether the cursor is visible */
  visible: boolean;
  
  /** Whether the cursor is blinking */
  blinking: boolean;
}

/**
 * Character set identifiers.
 * Represents the different character sets that can be designated to G0-G3.
 */
export enum CharacterSet {
  /** US ASCII (default) */
  ASCII = 'B',
  
  /** DEC Special Character and Line Drawing Set */
  DECSpecialGraphics = '0',
  
  /** DEC Alternate Character ROM Standard Character Set */
  DECAlternate = 'A',
  
  /** DEC Alternate Character ROM Special Character Set */
  DECAlternateSpecial = '1',
  
  /** DEC Technical Character Set */
  DECTechnical = '>',
  
  /** United Kingdom (UK) */
  UK = 'A'
}

/**
 * Character set state tracking G0-G3 designations and active set.
 */
export interface CharacterSetState {
  /** G0 character set (default active) */
  g0: CharacterSet;
  
  /** G1 character set */
  g1: CharacterSet;
  
  /** G2 character set */
  g2: CharacterSet;
  
  /** G3 character set */
  g3: CharacterSet;
  
  /** Currently active character set (GL - left side) */
  activeGL: 'g0' | 'g1';
  
  /** Currently active character set (GR - right side) */
  activeGR: 'g2' | 'g3';
}

/**
 * Current text attributes for new characters.
 * These attributes are applied to characters as they are written.
 */
export interface Attributes {
  /** Foreground color */
  fg: Color;
  
  /** Background color */
  bg: Color;
  
  /** Bold text attribute */
  bold: boolean;
  
  /** Italic text attribute */
  italic: boolean;
  
  /** Underline style */
  underline: UnderlineStyle;
  
  /** Inverse/reverse video attribute */
  inverse: boolean;
  
  /** Strikethrough attribute */
  strikethrough: boolean;
  
  /** Hyperlink URL (OSC 8) */
  url?: string;
}
