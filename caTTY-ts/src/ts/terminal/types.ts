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

/**
 * Image data stored in the terminal.
 * Represents a decoded image that can be placed multiple times.
 */
export interface ImageData {
  /** Unique identifier for this image */
  id: number;
  
  /** Decoded image data ready for rendering */
  data: ImageBitmap | HTMLImageElement;
  
  /** Image width in pixels */
  width: number;
  
  /** Image height in pixels */
  height: number;
  
  /** Image format */
  format: 'png' | 'jpeg' | 'gif';
}

/**
 * An image placement on the terminal screen.
 * Represents a specific instance of an image at a location.
 */
export interface ImagePlacement {
  /** Unique identifier for this placement */
  placementId: number;
  
  /** ID of the image to display */
  imageId: number;
  
  /** Row position in the terminal grid */
  row: number;
  
  /** Column position in the terminal grid */
  col: number;
  
  /** Width in terminal cells */
  width: number;
  
  /** Height in terminal cells */
  height: number;
  
  /** Source rectangle X offset in pixels (for cropping) */
  sourceX?: number;
  
  /** Source rectangle Y offset in pixels (for cropping) */
  sourceY?: number;
  
  /** Source rectangle width in pixels (for cropping) */
  sourceWidth?: number;
  
  /** Source rectangle height in pixels (for cropping) */
  sourceHeight?: number;
  
  /** Z-index for layering (higher values are on top) */
  zIndex?: number;
  
  /** Unicode placeholder character associated with this placement */
  unicodePlaceholder?: string;
}

/**
 * State tracking for chunked image transmission.
 * Used when an image is transmitted in multiple parts.
 */
export interface TransmissionState {
  /** ID of the image being transmitted */
  imageId: number;
  
  /** Accumulated chunks of image data */
  chunks: Uint8Array[];
  
  /** Image format (png, jpeg, gif) */
  format: string;
  
  /** Expected total size in bytes (if known) */
  expectedSize?: number;
  
  /** Whether transmission is complete */
  complete: boolean;
}

/**
 * Parsed parameters from a Kitty graphics command.
 * Represents the decoded escape sequence parameters.
 */
export interface GraphicsParams {
  /** Action: 't' = transmit, 'd' = display, 'D' = delete */
  action: 't' | 'd' | 'D';
  
  /** Image ID for storage and reference */
  imageId?: number;
  
  /** Placement ID for specific placement reference */
  placementId?: number;
  
  /** Image format (24 = RGB, 32 = RGBA, 100 = PNG, etc.) */
  format?: string;
  
  /** Width in pixels */
  width?: number;
  
  /** Height in pixels */
  height?: number;
  
  /** X offset in pixels */
  x?: number;
  
  /** Y offset in pixels */
  y?: number;
  
  /** Number of rows to occupy */
  rows?: number;
  
  /** Number of columns to occupy */
  cols?: number;
  
  /** Source rectangle X offset */
  sourceX?: number;
  
  /** Source rectangle Y offset */
  sourceY?: number;
  
  /** Source rectangle width */
  sourceWidth?: number;
  
  /** Source rectangle height */
  sourceHeight?: number;
  
  /** Whether more chunks are coming (for chunked transmission) */
  more?: boolean;
  
  /** Unicode placeholder codepoint */
  unicodePlaceholder?: number;
  
  /** Z-index for layering */
  zIndex?: number;
  
  /** Compression type (0 = none, 1 = zlib) */
  compression?: number;
  
  /** Transmission medium (d = direct, f = file, t = temp file, s = shared memory) */
  medium?: string;
}
