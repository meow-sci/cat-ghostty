  /**
 * DOM Style Manager
 * 
 * Handles dynamic style tag creation and management for terminal styling.
 * Provides utilities for CSS variable injection and cell class management.
 */

/**
 * Manages dynamic style injection and CSS variable management
 */
export class DomStyleManager {
  private static instance: DomStyleManager | null = null;
  private readonly styleTagCache = new Map<string, HTMLStyleElement>();
  private readonly injectedStyles = new Set<string>();

  private constructor() {
    // Singleton pattern
  }

  /**
   * Get the singleton instance
   */
  public static getInstance(): DomStyleManager {
    if (!DomStyleManager.instance) {
      DomStyleManager.instance = new DomStyleManager();
    }
    return DomStyleManager.instance;
  }

  /**
   * Create or get an existing style tag with the given ID
   * @param id Unique identifier for the style tag
   * @returns HTMLStyleElement or null if not in browser environment
   */
  public getOrCreateStyleTag(id: string): HTMLStyleElement | null {
    if (typeof document === 'undefined') {
      return null; // Not in browser environment
    }

    // Check cache first
    if (this.styleTagCache.has(id)) {
      return this.styleTagCache.get(id)!;
    }

    // Try to find existing tag
    let styleTag = document.getElementById(id) as HTMLStyleElement;
    
    if (!styleTag) {
      // Create new style tag
      styleTag = document.createElement('style');
      styleTag.id = id;
      document.head.appendChild(styleTag);
    }

    // Cache the tag
    this.styleTagCache.set(id, styleTag);
    return styleTag;
  }

  /**
   * Inject CSS text into a style tag
   * @param styleTagId ID of the style tag
   * @param cssText CSS text to inject
   * @param append Whether to append or replace existing content
   */
  public injectCss(styleTagId: string, cssText: string, append: boolean = false): void {
    const styleTag = this.getOrCreateStyleTag(styleTagId);
    if (!styleTag) {
      return;
    }

    if (append) {
      styleTag.textContent = (styleTag.textContent || '') + cssText;
    } else {
      styleTag.textContent = cssText;
    }
  }

  /**
   * Add a CSS rule to a style tag if it hasn't been added before
   * @param styleTagId ID of the style tag
   * @param cssRule CSS rule to add
   * @returns true if rule was added, false if it already existed
   */
  public addCssRule(styleTagId: string, cssRule: string): boolean {
    const ruleKey = `${styleTagId}:${cssRule}`;
    
    if (this.injectedStyles.has(ruleKey)) {
      return false; // Already exists
    }

    this.injectCss(styleTagId, cssRule + '\n', true);
    this.injectedStyles.add(ruleKey);
    return true;
  }

  /**
   * Remove a style tag and clear its cache
   * @param id ID of the style tag to remove
   */
  public removeStyleTag(id: string): void {
    const styleTag = this.styleTagCache.get(id);
    if (styleTag && styleTag.parentNode) {
      styleTag.parentNode.removeChild(styleTag);
    }
    
    this.styleTagCache.delete(id);
    
    // Remove related injected styles from cache
    for (const key of this.injectedStyles) {
      if (key.startsWith(`${id}:`)) {
        this.injectedStyles.delete(key);
      }
    }
  }

  /**
   * Clear all cached style tags and injected styles
   */
  public clearCache(): void {
    this.styleTagCache.clear();
    this.injectedStyles.clear();
  }

  /**
   * Update CSS classes on a DOM element, removing old classes and adding new ones
   * @param element DOM element to update
   * @param newClasses Array of new class names to apply
   * @param classPrefix Prefix to identify which classes to remove (e.g., 'sgr-')
   */
  public updateElementClasses(
    element: HTMLElement, 
    newClasses: string[], 
    classPrefix: string = ''
  ): void {
    // Remove existing classes with the specified prefix
    if (classPrefix) {
      const existingClasses = Array.from(element.classList).filter(cls => 
        cls.startsWith(classPrefix)
      );
      existingClasses.forEach(cls => element.classList.remove(cls));
    }

    // Add new classes
    newClasses.forEach(cls => {
      if (cls && !element.classList.contains(cls)) {
        element.classList.add(cls);
      }
    });
  }

  /**
   * Create CSS variables object from key-value pairs
   * @param variables Object with CSS variable names and values
   * @returns CSS text with :root selector
   */
  public createCssVariables(variables: Record<string, string>): string {
    const cssRules = Object.entries(variables)
      .map(([name, value]) => `  ${name}: ${value};`)
      .join('\n');
    
    return `:root {\n${cssRules}\n}`;
  }

  /**
   * Inject CSS variables into the DOM
   * @param variables Object with CSS variable names and values
   * @param styleTagId ID for the style tag (defaults to 'css-variables')
   */
  public injectCssVariables(
    variables: Record<string, string>, 
    styleTagId: string = 'css-variables'
  ): void {
    const cssText = this.createCssVariables(variables);
    this.injectCss(styleTagId, cssText);
  }

  /**
   * Check if we're in a browser environment
   */
  public isBrowserEnvironment(): boolean {
    return typeof document !== 'undefined' && typeof window !== 'undefined';
  }
}

/**
 * Utility functions for cell class management
 */
export class CellClassManager {
  /**
   * Update classes for a terminal cell element
   * @param element Cell element to update
   * @param sgrClass SGR style class name
   * @param additionalClasses Additional classes to apply
   */
  public static updateCellClasses(
    element: HTMLElement,
    sgrClass: string | null,
    additionalClasses: string[] = []
  ): void {
    const domManager = DomStyleManager.getInstance();
    
    // Collect all classes to apply
    const newClasses: string[] = [];
    
    if (sgrClass) {
      newClasses.push(sgrClass);
    }
    
    newClasses.push(...additionalClasses);
    
    // Update classes, removing old SGR classes
    domManager.updateElementClasses(element, newClasses, 'sgr-');
  }

  /**
   * Reset cell to default styling
   * @param element Cell element to reset
   */
  public static resetCellClasses(element: HTMLElement): void {
    const domManager = DomStyleManager.getInstance();
    domManager.updateElementClasses(element, [], 'sgr-');
  }

  /**
   * Apply multiple style classes to a cell
   * @param element Cell element
   * @param styleClasses Array of style class names
   */
  public static applyStyleClasses(element: HTMLElement, styleClasses: string[]): void {
    const domManager = DomStyleManager.getInstance();
    domManager.updateElementClasses(element, styleClasses, 'sgr-');
  }
}
