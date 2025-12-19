import type { SgrColorType, SgrNamedColor } from "../TerminalEmulationTypes";
import type { SgrState } from "../SgrStyleManager";

/**
 * Generate Device Attributes (Primary DA) response.
 * Reports terminal type and supported features.
 * Format: CSI ? 1 ; 2 c (VT100 with Advanced Video Option)
 */
export function generateDeviceAttributesPrimaryResponse(): string {
  // Report as VT100 with Advanced Video Option
  // This is a minimal response that most applications will accept
  return "\x1b[?1;2c";
}

/**
 * Generate Device Attributes (Secondary DA) response.
 * Reports terminal version and firmware level.
 * Format: CSI > 0 ; version ; 0 c
 */
export function generateDeviceAttributesSecondaryResponse(): string {
  // Report as VT100 compatible terminal, version 0
  return "\x1b[>0;0;0c";
}

/**
 * Generate Cursor Position Report (CPR) response.
 * Reports current cursor position to the application.
 * Format: CSI row ; col R (1-indexed coordinates)
 */
export function generateCursorPositionReport(cursorX: number, cursorY: number): string {
  // Convert from 0-indexed to 1-indexed coordinates
  const row = cursorY + 1;
  const col = cursorX + 1;
  return `\x1b[${row};${col}R`;
}

/**
 * Generate Device Status Report (DSR) "ready" response.
 * Format: CSI 0 n
 */
export function generateDeviceStatusReportResponse(): string {
  return "\x1b[0n";
}

/**
 * Generate Terminal Size Query response.
 * Reports terminal dimensions in characters.
 * Format: CSI 8 ; rows ; cols t
 */
export function generateTerminalSizeResponse(rows: number, cols: number): string {
  return `\x1b[8;${rows};${cols}t`;
}

/**
 * Generate response for OSC 10;? (query foreground color)
 */
export function generateForegroundColorResponse(currentSgrState: SgrState): string {
  // Get the current effective foreground color
  const effectiveColor = getEffectiveForegroundColor(currentSgrState);

  // Convert to OSC format: OSC 10 ; rgb:rrrr/gggg/bbbb BEL
  return `\x1b]10;${effectiveColor}\x07`;
}

/**
 * Generate response for OSC 11;? (query background color)
 */
export function generateBackgroundColorResponse(currentSgrState: SgrState): string {
  // Get the current effective background color
  const effectiveColor = getEffectiveBackgroundColor(currentSgrState);

  // Convert to OSC format: OSC 11 ; rgb:rrrr/gggg/bbbb BEL
  return `\x1b]11;${effectiveColor}\x07`;
}

/**
 * Get the effective foreground color (SGR override or theme default)
 */
export function getEffectiveForegroundColor(currentSgrState: SgrState): string {
  if (currentSgrState.foregroundColor) {
    return convertSgrColorToOscFormat(currentSgrState.foregroundColor);
  }

  // Return theme default foreground color
  // Use the CSS variable value or fallback to standard light gray
  return "rgb:aaaa/aaaa/aaaa"; // Default light gray
}

/**
 * Get the effective background color (SGR override or theme default)
 */
export function getEffectiveBackgroundColor(currentSgrState: SgrState): string {
  if (currentSgrState.backgroundColor) {
    return convertSgrColorToOscFormat(currentSgrState.backgroundColor);
  }

  // Return theme default background color
  // Use the CSS variable value or fallback to standard black
  return "rgb:0000/0000/0000"; // Default black
}

/**
 * Convert SGR color type to OSC rgb format
 */
export function convertSgrColorToOscFormat(colorType: SgrColorType): string {
  switch (colorType.type) {
    case "rgb": {
      // Convert 8-bit RGB values to 16-bit hex format for OSC
      const r16 = Math.round((colorType.r / 255) * 65535).toString(16).padStart(4, "0");
      const g16 = Math.round((colorType.g / 255) * 65535).toString(16).padStart(4, "0");
      const b16 = Math.round((colorType.b / 255) * 65535).toString(16).padStart(4, "0");
      return `rgb:${r16}/${g16}/${b16}`;
    }

    case "indexed":
      // Convert indexed color to RGB then to OSC format
      return convertIndexedColorToOscFormat(colorType.index);

    case "named":
      // Convert named color to RGB then to OSC format
      return convertNamedColorToOscFormat(colorType.color);

    default:
      // Fallback to default colors
      return "rgb:aaaa/aaaa/aaaa";
  }
}

/**
 * Convert indexed color (0-255) to OSC rgb format
 */
export function convertIndexedColorToOscFormat(index: number): string {
  // Standard 16 colors (0-15) - use predefined RGB values
  if (index >= 0 && index <= 15) {
    const standardColors: Array<[number, number, number]> = [
      [0, 0, 0],
      [170, 0, 0],
      [0, 170, 0],
      [170, 85, 0],
      [0, 0, 170],
      [170, 0, 170],
      [0, 170, 170],
      [170, 170, 170],
      [85, 85, 85],
      [255, 85, 85],
      [85, 255, 85],
      [255, 255, 85],
      [85, 85, 255],
      [255, 85, 255],
      [85, 255, 255],
      [255, 255, 255],
    ];

    if (index < standardColors.length) {
      const [r, g, b] = standardColors[index];
      const r16 = Math.round((r / 255) * 65535).toString(16).padStart(4, "0");
      const g16 = Math.round((g / 255) * 65535).toString(16).padStart(4, "0");
      const b16 = Math.round((b / 255) * 65535).toString(16).padStart(4, "0");
      return `rgb:${r16}/${g16}/${b16}`;
    }
  }

  // 216 color cube (16-231)
  if (index >= 16 && index <= 231) {
    const cubeIndex = index - 16;
    const r = Math.floor(cubeIndex / 36);
    const g = Math.floor((cubeIndex % 36) / 6);
    const b = cubeIndex % 6;

    const toColorValue = (n: number) => n === 0 ? 0 : 55 + n * 40;
    const rVal = toColorValue(r);
    const gVal = toColorValue(g);
    const bVal = toColorValue(b);

    const r16 = Math.round((rVal / 255) * 65535).toString(16).padStart(4, "0");
    const g16 = Math.round((gVal / 255) * 65535).toString(16).padStart(4, "0");
    const b16 = Math.round((bVal / 255) * 65535).toString(16).padStart(4, "0");
    return `rgb:${r16}/${g16}/${b16}`;
  }

  // Grayscale ramp (232-255)
  if (index >= 232 && index <= 255) {
    const gray = 8 + (index - 232) * 10;
    const gray16 = Math.round((gray / 255) * 65535).toString(16).padStart(4, "0");
    return `rgb:${gray16}/${gray16}/${gray16}`;
  }

  // Invalid index, return default
  return "rgb:aaaa/aaaa/aaaa";
}

/**
 * Convert named ANSI color to OSC rgb format
 */
export function convertNamedColorToOscFormat(color: SgrNamedColor): string {
  const namedColorRgb: Record<SgrNamedColor, [number, number, number]> = {
    black: [0, 0, 0],
    red: [170, 0, 0],
    green: [0, 170, 0],
    yellow: [170, 85, 0],
    blue: [0, 0, 170],
    magenta: [170, 0, 170],
    cyan: [0, 170, 170],
    white: [170, 170, 170],
    brightBlack: [85, 85, 85],
    brightRed: [255, 85, 85],
    brightGreen: [85, 255, 85],
    brightYellow: [255, 255, 85],
    brightBlue: [85, 85, 255],
    brightMagenta: [255, 85, 255],
    brightCyan: [85, 255, 255],
    brightWhite: [255, 255, 255],
  };

  const [r, g, b] = namedColorRgb[color] ?? [170, 170, 170];
  const r16 = Math.round((r / 255) * 65535).toString(16).padStart(4, "0");
  const g16 = Math.round((g / 255) * 65535).toString(16).padStart(4, "0");
  const b16 = Math.round((b / 255) * 65535).toString(16).padStart(4, "0");
  return `rgb:${r16}/${g16}/${b16}`;
}
