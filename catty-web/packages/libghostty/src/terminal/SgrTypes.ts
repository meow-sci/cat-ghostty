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

export const SgrAttributeTags: Record<string, number> = {
  UNSET: 0,
  UNKNOWN: 1,
  BOLD: 2,
  RESET_BOLD: 3,
  ITALIC: 4,
  RESET_ITALIC: 5,
  FAINT: 6,
  UNDERLINE: 7,
  RESET_UNDERLINE: 8,
  UNDERLINE_COLOR: 9,
  UNDERLINE_COLOR_256: 10,
  RESET_UNDERLINE_COLOR: 11,
  OVERLINE: 12,
  RESET_OVERLINE: 13,
  BLINK: 14,
  RESET_BLINK: 15,
  INVERSE: 16,
  RESET_INVERSE: 17,
  INVISIBLE: 18,
  RESET_INVISIBLE: 19,
  STRIKETHROUGH: 20,
  RESET_STRIKETHROUGH: 21,
  DIRECT_COLOR_FG: 22,
  DIRECT_COLOR_BG: 23,
  BG_8: 24,
  FG_8: 25,
  RESET_FG: 26,
  RESET_BG: 27,
  BRIGHT_BG_8: 28,
  BRIGHT_FG_8: 29,
  BG_256: 30,
  FG_256: 31
};

export type SgrAttributeTags = typeof SgrAttributeTags;
