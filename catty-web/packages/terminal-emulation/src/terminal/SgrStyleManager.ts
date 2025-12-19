/**
 * SGR Style Manager
 * 
 * Handles CSS generation and caching for SGR (Select Graphic Rendition) styling.
 * Uses xxh3 hashing for unique CSS class names and maintains an in-memory cache.
 */

import type { SgrColorType } from './TerminalEmulationTypes';

import { ColorResolver } from './ColorResolver';
import { CellClassManager, DomStyleManager } from './DomStyleManager';

/**
 * Represents the current SGR state for a terminal cell
 */
export interface SgrState {
  // Text styling
  bold: boolean;
  faint: boolean;
  italic: boolean;
  underline: boolean;
  underlineStyle: "single" | "double" | "curly" | "dotted" | "dashed" | null;
  blink: boolean;
  inverse: boolean;
  hidden: boolean;
  strikethrough: boolean;

  // Colors
  foregroundColor: SgrColorType | null;
  backgroundColor: SgrColorType | null;
  underlineColor: SgrColorType | null;

  // Font
  font: number; // 0 = primary, 1-9 = alternative fonts
}

/**
 * Creates a default/reset SGR state
 */
export function createDefaultSgrState(): SgrState {
  return {
    bold: false,
    faint: false,
    italic: false,
    underline: false,
    underlineStyle: null,
    blink: false,
    inverse: false,
    hidden: false,
    strikethrough: false,
    foregroundColor: null,
    backgroundColor: null,
    underlineColor: null,
    font: 0,
  };
}

/**
 * Cache entry for generated CSS styles
 */
interface StyleCacheEntry {
  cssText: string;
  className: string;
}

/**
 * Manages SGR styling with CSS generation and caching
 */
export class SgrStyleManager {
  private readonly styleCache = new Map<string, StyleCacheEntry>();
  private readonly domStyleCache = new Set<string>(); // Track which styles are in DOM

  constructor() {
    // No theme manager needed for now - colors are resolved via CSS variables
  }

  /**
   * Generate CSS class name and ensure style is available in DOM
   * @param sgrState Current SGR state
   * @returns CSS class name to apply to the cell
   */
  public getStyleClass(sgrState: SgrState): string {

    const cssText = this.generateCssForSgr(sgrState);
    const hash = this.simpleHash(cssText);

    // Check cache first
    const cacheKey = hash;
    let cacheEntry = this.styleCache.get(hash);

    if (!cacheEntry) {
      // Generate hash-based class name
      const className = `sgr-${hash}`;

      cacheEntry = { cssText, className };
      this.styleCache.set(cacheKey, cacheEntry);
    }

    // Ensure style is in DOM
    this.ensureStyleInDom(cacheEntry);

    return cacheEntry.className;
  }

  /**
   * Simple hash function for CSS strings (avoids Buffer dependency)
   */
  private simpleHash(str: string): string {
    let hash = 0;
    if (str.length === 0) return hash.toString(16);
    
    for (let i = 0; i < str.length; i++) {
      const char = str.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32bit integer
    }
    
    return Math.abs(hash).toString(16);
  }

  /**
   * Expose hash function for testing
   */
  public hashCssString(cssString: string): string {
    return this.simpleHash(cssString);
  }

  /**
   * Generate CSS text for the given SGR state
   */
  public generateCssForSgr(sgrState: SgrState): string {
    const cssRules: string[] = [];

    // Text styling
    if (sgrState.bold) {
      cssRules.push('font-weight: bold');
    }

    if (sgrState.faint) {
      cssRules.push('opacity: 0.5');
    }

    if (sgrState.italic) {
      cssRules.push('font-style: italic');
    }

    const decorationLines: string[] = [];

    if (sgrState.underline && sgrState.underlineStyle) {
      decorationLines.push('underline');
      switch (sgrState.underlineStyle) {
        case 'single':
          // default
          break;
        case 'double':
          cssRules.push('text-decoration-style: double');
          break;
        case 'curly':
          cssRules.push('text-decoration-style: wavy');
          break;
        case 'dotted':
          cssRules.push('text-decoration-style: dotted');
          break;
        case 'dashed':
          cssRules.push('text-decoration-style: dashed');
          break;
      }
    }

    if (sgrState.blink) {
      cssRules.push('animation: blink 1s step-end infinite');
    }

    if (sgrState.strikethrough) {
      decorationLines.push('line-through');
    }

    if (decorationLines.length > 0) {
      cssRules.push(`text-decoration: ${decorationLines.join(' ')}`);
    }

    if (sgrState.hidden) {
      cssRules.push('visibility: hidden');
    }

    // Colors
    const defaultFg = 'var(--terminal-foreground)';
    const defaultBg = 'var(--terminal-background)';

    let fg = sgrState.foregroundColor ? this.resolveColor(sgrState.foregroundColor) : defaultFg;
    let bg = sgrState.backgroundColor ? this.resolveColor(sgrState.backgroundColor) : defaultBg;

    // Inverse video swaps the effective foreground/background (including defaults).
    if (sgrState.inverse) {
      [fg, bg] = [bg, fg];
    }

    // Only emit defaults when needed (inverse requires both).
    if (sgrState.inverse || sgrState.foregroundColor) {
      cssRules.push(`color: ${fg}`);
    }
    if (sgrState.inverse || sgrState.backgroundColor) {
      cssRules.push(`background-color: ${bg}`);
    }

    if (sgrState.underlineColor) {
      const color = this.resolveColor(sgrState.underlineColor);
      cssRules.push(`text-decoration-color: ${color}`);
    }

    return cssRules.join('; ');
  }

  /**
   * Resolve SGR color to CSS color value
   */
  private resolveColor(colorType: SgrColorType): string {
    return ColorResolver.resolve(colorType);
  }

  /**
   * Ensure the CSS style is available in the DOM
   */
  private ensureStyleInDom(cacheEntry: StyleCacheEntry): void {
    if (typeof document === 'undefined') {
      return; // Not in browser environment
    }

    if (this.domStyleCache.has(cacheEntry.className)) {
      return; // Already in DOM
    }

    // Import DOM manager here to avoid circular dependencies
    const domManager = DomStyleManager.getInstance();

    // Add the new style rule
    const cssRule = `.${cacheEntry.className} { ${cacheEntry.cssText} }`;

    // Add blink animation if needed
    if (cacheEntry.cssText.includes('blink')) {
      const blinkKeyframes = `
@keyframes blink {
  0%, 50% { opacity: 1; }
  51%, 100% { opacity: 0; }
}`;
      domManager.addCssRule('terminal-sgr-styles', blinkKeyframes);
    }

    domManager.addCssRule('terminal-sgr-styles', cssRule);
    this.domStyleCache.add(cacheEntry.className);
  }

  /**
   * Clear all cached styles (useful for theme changes)
   */
  public clearCache(): void {
    this.styleCache.clear();
    this.domStyleCache.clear();

    // Remove DOM style tag using DOM manager
    if (typeof document !== 'undefined') {
      DomStyleManager.getInstance().removeStyleTag('terminal-sgr-styles');
    }
  }

  /**
   * Update cell classes for a terminal cell element
   * @param element DOM element representing the terminal cell
   * @param sgrState Current SGR state for the cell
   */
  public updateCellClasses(element: HTMLElement, sgrState: SgrState): void {

    // Get style class if state is not default
    const styleClass = this.isDefaultState(sgrState) ? null : this.getStyleClass(sgrState);

    // Update cell classes
    CellClassManager.updateCellClasses(element, styleClass);
  }

  /**
   * Check if SGR state is the default (no styling)
   */
  private isDefaultState(sgrState: SgrState): boolean {
    const defaultState = createDefaultSgrState();
    return JSON.stringify(sgrState) === JSON.stringify(defaultState);
  }
}