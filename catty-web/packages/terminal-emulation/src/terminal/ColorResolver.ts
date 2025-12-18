/**
 * Color Resolver
 * 
 * Utilities for resolving different color types to CSS values.
 * Handles ANSI colors, 256-color palette, and RGB colors.
 */

import type { SgrColorType, SgrNamedColor } from './TerminalEmulationTypes';

/**
 * Standard 16 ANSI color names mapped to CSS variable names
 */
export const ANSI_COLOR_VARIABLES: Record<SgrNamedColor, string> = {
  black: 'var(--terminal-color-black)',
  red: 'var(--terminal-color-red)',
  green: 'var(--terminal-color-green)',
  yellow: 'var(--terminal-color-yellow)',
  blue: 'var(--terminal-color-blue)',
  magenta: 'var(--terminal-color-magenta)',
  cyan: 'var(--terminal-color-cyan)',
  white: 'var(--terminal-color-white)',
  brightBlack: 'var(--terminal-color-bright-black)',
  brightRed: 'var(--terminal-color-bright-red)',
  brightGreen: 'var(--terminal-color-bright-green)',
  brightYellow: 'var(--terminal-color-bright-yellow)',
  brightBlue: 'var(--terminal-color-bright-blue)',
  brightMagenta: 'var(--terminal-color-bright-magenta)',
  brightCyan: 'var(--terminal-color-bright-cyan)',
  brightWhite: 'var(--terminal-color-bright-white)',
};

/**
 * 256-color palette generator
 * Generates the standard xterm 256-color palette
 */
export class Color256Palette {
  private static readonly STANDARD_COLORS = [
    // Standard 16 colors (0-15) - use CSS variables
    'var(--terminal-color-black)',
    'var(--terminal-color-red)',
    'var(--terminal-color-green)',
    'var(--terminal-color-yellow)',
    'var(--terminal-color-blue)',
    'var(--terminal-color-magenta)',
    'var(--terminal-color-cyan)',
    'var(--terminal-color-white)',
    'var(--terminal-color-bright-black)',
    'var(--terminal-color-bright-red)',
    'var(--terminal-color-bright-green)',
    'var(--terminal-color-bright-yellow)',
    'var(--terminal-color-bright-blue)',
    'var(--terminal-color-bright-magenta)',
    'var(--terminal-color-bright-cyan)',
    'var(--terminal-color-bright-white)',
  ];

  /**
   * Get color value for 256-color palette index
   * @param index Color index (0-255)
   * @returns CSS color value
   */
  public static getColor(index: number): string {
    // Standard 16 colors (0-15)
    if (index >= 0 && index <= 15) {
      return this.STANDARD_COLORS[index];
    }

    // 216 color cube (16-231)
    if (index >= 16 && index <= 231) {
      return this.getCubeColor(index - 16);
    }

    // Grayscale ramp (232-255)
    if (index >= 232 && index <= 255) {
      return this.getGrayscaleColor(index - 232);
    }

    // Invalid index, return default
    return 'var(--terminal-foreground)';
  }

  /**
   * Generate color from 6x6x6 color cube
   * @param cubeIndex Index in the color cube (0-215)
   * @returns RGB color string
   */
  private static getCubeColor(cubeIndex: number): string {
    const r = Math.floor(cubeIndex / 36);
    const g = Math.floor((cubeIndex % 36) / 6);
    const b = cubeIndex % 6;

    const toColorValue = (n: number) => n === 0 ? 0 : 55 + n * 40;

    return `rgb(${toColorValue(r)}, ${toColorValue(g)}, ${toColorValue(b)})`;
  }

  /**
   * Generate grayscale color
   * @param grayIndex Index in grayscale ramp (0-23)
   * @returns RGB color string
   */
  private static getGrayscaleColor(grayIndex: number): string {
    const gray = 8 + grayIndex * 10;
    return `rgb(${gray}, ${gray}, ${gray})`;
  }
}

/**
 * Color resolver for different SGR color types
 */
export class ColorResolver {
  /**
   * Resolve SGR color type to CSS color value
   * @param colorType SGR color specification
   * @returns CSS color value
   */
  public static resolve(colorType: SgrColorType): string {
    switch (colorType.type) {
      case 'named':
        return this.resolveNamedColor(colorType.color);
      case 'indexed':
        return this.resolveIndexedColor(colorType.index);
      case 'rgb':
        return this.resolveRgbColor(colorType.r, colorType.g, colorType.b);
      default:
        return 'var(--terminal-foreground)';
    }
  }

  /**
   * Resolve named ANSI color to CSS variable
   */
  private static resolveNamedColor(color: SgrNamedColor): string {
    return ANSI_COLOR_VARIABLES[color] || 'var(--terminal-foreground)';
  }

  /**
   * Resolve indexed color (256-color palette)
   */
  private static resolveIndexedColor(index: number): string {
    return Color256Palette.getColor(index);
  }

  /**
   * Resolve RGB color to CSS rgb() value
   */
  private static resolveRgbColor(r: number, g: number, b: number): string {
    // Clamp values to valid range
    const clampedR = Math.max(0, Math.min(255, r));
    const clampedG = Math.max(0, Math.min(255, g));
    const clampedB = Math.max(0, Math.min(255, b));
    
    return `rgb(${clampedR}, ${clampedG}, ${clampedB})`;
  }

  /**
   * Convert ANSI color code to SgrColorType
   * @param colorCode ANSI color code (30-37, 40-47, 90-97, 100-107)
   * @param isForeground Whether this is a foreground color
   * @returns SgrColorType or null if invalid
   */
  public static ansiCodeToColorType(colorCode: number): SgrColorType | null {
    // Standard foreground colors (30-37)
    if (colorCode >= 30 && colorCode <= 37) {
      const colors: SgrNamedColor[] = [
        'black', 'red', 'green', 'yellow',
        'blue', 'magenta', 'cyan', 'white'
      ];
      return { type: 'named', color: colors[colorCode - 30] };
    }

    // Bright foreground colors (90-97)
    if (colorCode >= 90 && colorCode <= 97) {
      const colors: SgrNamedColor[] = [
        'brightBlack', 'brightRed', 'brightGreen', 'brightYellow',
        'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
      ];
      return { type: 'named', color: colors[colorCode - 90] };
    }

    // Standard background colors (40-47)
    if (colorCode >= 40 && colorCode <= 47) {
      const colors: SgrNamedColor[] = [
        'black', 'red', 'green', 'yellow',
        'blue', 'magenta', 'cyan', 'white'
      ];
      return { type: 'named', color: colors[colorCode - 40] };
    }

    // Bright background colors (100-107)
    if (colorCode >= 100 && colorCode <= 107) {
      const colors: SgrNamedColor[] = [
        'brightBlack', 'brightRed', 'brightGreen', 'brightYellow',
        'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
      ];
      return { type: 'named', color: colors[colorCode - 100] };
    }

    return null;
  }

  /**
   * Create indexed color type
   * @param index Color index (0-255)
   * @returns SgrColorType for indexed color
   */
  public static createIndexedColor(index: number): SgrColorType {
    return { type: 'indexed', index };
  }

  /**
   * Create RGB color type
   * @param r Red component (0-255)
   * @param g Green component (0-255)
   * @param b Blue component (0-255)
   * @returns SgrColorType for RGB color
   */
  public static createRgbColor(r: number, g: number, b: number): SgrColorType {
    return { type: 'rgb', r, g, b };
  }

  /**
   * Create named color type
   * @param color Named color
   * @returns SgrColorType for named color
   */
  public static createNamedColor(color: SgrNamedColor): SgrColorType {
    return { type: 'named', color };
  }
}