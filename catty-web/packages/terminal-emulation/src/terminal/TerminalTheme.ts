/**
 * Terminal Theme System
 * 
 * Provides a structured approach to color management using CSS custom properties.
 * Supports standard 16 ANSI colors plus terminal UI colors.
 */

import { DomStyleManager } from "./DomStyleManager";

/**
 * Terminal color palette containing all standard ANSI colors and UI colors
 */
export interface TerminalColorPalette {
  // Standard 16 ANSI colors
  black: string;
  red: string;
  green: string;
  yellow: string;
  blue: string;
  magenta: string;
  cyan: string;
  white: string;
  brightBlack: string;
  brightRed: string;
  brightGreen: string;
  brightYellow: string;
  brightBlue: string;
  brightMagenta: string;
  brightCyan: string;
  brightWhite: string;
  
  // Terminal UI colors
  foreground: string;
  background: string;
  cursor: string;
  selection: string;
}

/**
 * Complete terminal theme definition
 */
export interface TerminalTheme {
  name: string;
  type: "dark" | "light";
  colors: TerminalColorPalette;
}

/**
 * Default dark theme using standard terminal colors
 */
export const DEFAULT_DARK_THEME: TerminalTheme = {
  name: "Default Dark",
  type: "dark",
  colors: {
    // Standard ANSI colors
    black: "#000000",
    red: "#AA0000",
    green: "#00AA00",
    yellow: "#AA5500",
    blue: "#0000AA",
    magenta: "#AA00AA",
    cyan: "#00AAAA",
    white: "#AAAAAA",
    
    // Bright ANSI colors
    brightBlack: "#555555",
    brightRed: "#FF5555",
    brightGreen: "#55FF55",
    brightYellow: "#FFFF55",
    brightBlue: "#5555FF",
    brightMagenta: "#FF55FF",
    brightCyan: "#55FFFF",
    brightWhite: "#FFFFFF",
    
    // Terminal UI colors
    foreground: "#AAAAAA",
    background: "#000000",
    cursor: "#AAAAAA",
    selection: "#444444",
  },
};

/**
 * Maps ANSI color names to CSS variable names
 */
const COLOR_VARIABLE_MAP: Record<keyof TerminalColorPalette, string> = {
  black: "--terminal-color-black",
  red: "--terminal-color-red",
  green: "--terminal-color-green",
  yellow: "--terminal-color-yellow",
  blue: "--terminal-color-blue",
  magenta: "--terminal-color-magenta",
  cyan: "--terminal-color-cyan",
  white: "--terminal-color-white",
  brightBlack: "--terminal-color-bright-black",
  brightRed: "--terminal-color-bright-red",
  brightGreen: "--terminal-color-bright-green",
  brightYellow: "--terminal-color-bright-yellow",
  brightBlue: "--terminal-color-bright-blue",
  brightMagenta: "--terminal-color-bright-magenta",
  brightCyan: "--terminal-color-bright-cyan",
  brightWhite: "--terminal-color-bright-white",
  foreground: "--terminal-foreground",
  background: "--terminal-background",
  cursor: "--terminal-cursor",
  selection: "--terminal-selection",
};

/**
 * Theme manager for handling terminal color themes and CSS variable generation
 */
export class ThemeManager {
  private currentTheme: TerminalTheme;

  constructor(initialTheme: TerminalTheme = DEFAULT_DARK_THEME) {
    this.currentTheme = initialTheme;
  }

  /**
   * Get the current active theme
   */
  public getCurrentTheme(): TerminalTheme {
    return this.currentTheme;
  }

  /**
   * Apply a new theme and generate CSS variables
   */
  public applyTheme(theme: TerminalTheme): void {
    this.currentTheme = theme;
    this.injectCssVariables();
  }

  /**
   * Generate CSS custom properties string for the current theme
   */
  public generateCssVariables(theme: TerminalTheme = this.currentTheme): string {
    const cssRules: string[] = [];
    
    for (const [colorName, colorValue] of Object.entries(theme.colors)) {
      const cssVariable = COLOR_VARIABLE_MAP[colorName as keyof TerminalColorPalette];
      if (cssVariable) {
        cssRules.push(`  ${cssVariable}: ${colorValue};`);
      }
    }
    
    return `:root {\n${cssRules.join('\n')}\n}`;
  }

  /**
   * Resolve a color by ANSI color code to CSS variable or direct value
   * @param colorCode ANSI color code (0-15 for standard colors)
   * @param isBright Whether this is a bright color variant
   * @returns CSS variable reference or direct color value
   */
  public resolveColor(colorCode: number, isBright: boolean = false): string {
    // Map ANSI color codes to color names
    const colorNames = [
      "black", "red", "green", "yellow", 
      "blue", "magenta", "cyan", "white"
    ];
    
    if (colorCode >= 0 && colorCode <= 7) {
      const colorName = colorNames[colorCode];
      const fullColorName = isBright ? `bright${colorName.charAt(0).toUpperCase()}${colorName.slice(1)}` : colorName;
      const cssVariable = COLOR_VARIABLE_MAP[fullColorName as keyof TerminalColorPalette];
      return `var(${cssVariable})`;
    }
    
    // For colors outside standard range, return a fallback
    return "var(--terminal-foreground)";
  }

  /**
   * Inject CSS variables into the DOM
   */
  private injectCssVariables(): void {
    // Check if we're in a browser environment
    if (typeof document === 'undefined') {
      return;
    }

    const cssText = this.generateCssVariables();
    DomStyleManager.getInstance().injectCss('terminal-theme-variables', cssText);
  }
}