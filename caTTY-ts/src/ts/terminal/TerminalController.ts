/**
 * TerminalController - Handles DOM integration and user interaction.
 * 
 * This class coordinates between the headless Terminal model and the DOM,
 * handling keyboard input, mouse events, focus management, and rendering.
 */

import type { Terminal } from './Terminal.js';
import type { GhosttyVtInstance } from '../ghostty-vt.js';
import type { KeyEvent } from '../../pages/demos/keyencode/_ts/pure/KeyEvent.js';
import { KeyCodeMap } from './keyencode/KeyCodeMap.js';
import { Renderer } from './Renderer.js';

/**
 * Configuration for TerminalController initialization.
 */
export interface ControllerConfig {
  /** The headless terminal instance */
  terminal: Terminal;
  
  /** Input element for capturing keyboard events */
  inputElement: HTMLInputElement;
  
  /** Display element for rendering terminal output */
  displayElement: HTMLElement;
  
  /** WASM instance for key encoding and other features */
  wasmInstance: GhosttyVtInstance;
}

/**
 * Selection state for mouse selection.
 */
interface SelectionState {
  /** Whether selection is active */
  active: boolean;
  
  /** Start position */
  start: { row: number; col: number } | null;
  
  /** End position */
  end: { row: number; col: number } | null;
}

/**
 * TerminalController handles DOM events and coordinates between model and view.
 */
export class TerminalController {
  private readonly terminal: Terminal;
  private readonly inputElement: HTMLInputElement;
  private readonly displayElement: HTMLElement;
  private readonly wasmInstance: GhosttyVtInstance;
  private readonly renderer: Renderer;
  
  // Event listeners for cleanup
  private readonly eventListeners: Array<() => void> = [];
  
  // Selection state
  private selection: SelectionState = {
    active: false,
    start: null,
    end: null,
  };
  
  // Focus state
  private focused: boolean = false;
  
  // Key encoder state
  private keyEncoderPtr: number | null = null;
  
  /**
   * Creates a new TerminalController instance.
   * @param config Controller configuration
   */
  constructor(config: ControllerConfig) {
    this.terminal = config.terminal;
    this.inputElement = config.inputElement;
    this.displayElement = config.displayElement;
    this.wasmInstance = config.wasmInstance;
    
    // Initialize renderer
    this.renderer = new Renderer(this.displayElement);
    
    // Set up initial state
    this.setupInitialState();
    
    // Initialize key encoder
    this.initializeKeyEncoder();
  }
  
  /**
   * Sets up initial DOM state and properties.
   */
  private setupInitialState(): void {
    // Configure input element
    this.inputElement.setAttribute('autocomplete', 'off');
    this.inputElement.setAttribute('autocorrect', 'off');
    this.inputElement.setAttribute('autocapitalize', 'off');
    this.inputElement.setAttribute('spellcheck', 'false');
    
    // Configure display element
    this.displayElement.setAttribute('tabindex', '0');
    this.displayElement.style.fontFamily = 'monospace';
    this.displayElement.style.whiteSpace = 'pre';
    this.displayElement.style.overflow = 'hidden';
    this.displayElement.style.cursor = 'text';
    this.displayElement.style.outline = 'none'; // Remove default focus outline
    
    // Add CSS classes for focus states
    this.displayElement.classList.add('terminal-display');
    
    // Set up focus-related styles
    this.setupFocusStyles();
  }
  
  /**
   * Mounts the controller by adding event listeners.
   */
  mount(): void {
    this.setupEventListeners();
    
    // Auto-focus on initialization
    this.focus();
  }
  
  /**
   * Unmounts the controller by removing event listeners and cleaning up resources.
   */
  unmount(): void {
    // Remove all event listeners
    this.eventListeners.forEach(removeListener => removeListener());
    this.eventListeners.length = 0;
    
    // Clear selection
    this.clearSelection();
    
    // Clean up key encoder
    this.cleanupKeyEncoder();
  }
  
  /**
   * Sets up all event listeners.
   */
  private setupEventListeners(): void {
    // Keyboard events
    const handleKeyDown = this.handleKeyDown.bind(this);
    this.inputElement.addEventListener('keydown', handleKeyDown);
    this.eventListeners.push(() => this.inputElement.removeEventListener('keydown', handleKeyDown));
    
    // Paste events
    const handlePaste = this.handlePaste.bind(this);
    this.inputElement.addEventListener('paste', handlePaste);
    this.eventListeners.push(() => this.inputElement.removeEventListener('paste', handlePaste));
    
    // Focus events
    const handleFocus = this.handleFocus.bind(this);
    const handleBlur = this.handleBlur.bind(this);
    this.inputElement.addEventListener('focus', handleFocus);
    this.inputElement.addEventListener('blur', handleBlur);
    this.eventListeners.push(() => {
      this.inputElement.removeEventListener('focus', handleFocus);
      this.inputElement.removeEventListener('blur', handleBlur);
    });
    
    // Click on display element should focus input
    const handleDisplayClick = this.handleDisplayClick.bind(this);
    this.displayElement.addEventListener('click', handleDisplayClick);
    this.eventListeners.push(() => this.displayElement.removeEventListener('click', handleDisplayClick));
    
    // Mouse events for selection
    const handleMouseDown = this.handleMouseDown.bind(this);
    const handleMouseMove = this.handleMouseMove.bind(this);
    const handleMouseUp = this.handleMouseUp.bind(this);
    
    this.displayElement.addEventListener('mousedown', handleMouseDown);
    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    
    this.eventListeners.push(() => {
      this.displayElement.removeEventListener('mousedown', handleMouseDown);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    });
    
    // Copy events
    const handleCopy = this.handleCopy.bind(this);
    document.addEventListener('copy', handleCopy);
    this.eventListeners.push(() => document.removeEventListener('copy', handleCopy));
    
    // Terminal state change events
    const handleStateChange = this.handleStateChange.bind(this);
    // Note: This would need to be implemented in Terminal class to emit state change events
    // For now, we'll handle rendering manually when needed
  }
  
  /**
   * Focuses the input element.
   */
  focus(): void {
    this.inputElement.focus();
  }
  
  /**
   * Gets the current focus state.
   */
  isFocused(): boolean {
    return this.focused;
  }
  
  /**
   * Sets up focus-related CSS styles.
   */
  private setupFocusStyles(): void {
    // Add basic focus styles via JavaScript
    // In a real application, these would typically be in a CSS file
    const style = document.createElement('style');
    style.textContent = `
      .terminal-display {
        border: 2px solid transparent;
        transition: border-color 0.2s ease;
      }
      
      .terminal-focused {
        border-color: #007acc;
      }
      
      .terminal-blurred {
        border-color: #cccccc;
      }
      
      .terminal-display:focus {
        outline: none;
      }
    `;
    
    // Only add the style if it doesn't already exist
    if (!document.querySelector('#terminal-controller-styles')) {
      style.id = 'terminal-controller-styles';
      document.head.appendChild(style);
    }
  }
  
  /**
   * Clears the current selection.
   */
  private clearSelection(): void {
    this.selection = {
      active: false,
      start: null,
      end: null,
    };
    this.updateSelectionDisplay();
  }
  
  /**
   * Updates the visual display of the selection.
   */
  private updateSelectionDisplay(): void {
    // This would update CSS classes or styles to highlight selected cells
    // Implementation depends on the rendering strategy
    // For now, we'll just track the selection state and trigger a render
    this.render();
  }
  
  /**
   * Calculates the terminal cell position from mouse coordinates.
   */
  private getCellPositionFromMouse(event: MouseEvent): { row: number; col: number } | null {
    const rect = this.displayElement.getBoundingClientRect();
    const x = event.clientX - rect.left;
    const y = event.clientY - rect.top;
    
    // Calculate character dimensions
    // This is a simplified calculation - in a real implementation,
    // you'd measure the actual character size from the rendered font
    const charWidth = 8; // Approximate monospace character width
    const charHeight = 16; // Approximate line height
    
    const col = Math.floor(x / charWidth);
    const row = Math.floor(y / charHeight);
    
    // Validate bounds
    const config = this.terminal.getConfig();
    if (row >= 0 && row < config.rows && col >= 0 && col < config.cols) {
      return { row, col };
    }
    
    return null;
  }
  
  /**
   * Gets the current selection range.
   */
  getSelection(): SelectionState {
    return { ...this.selection };
  }
  
  /**
   * Checks if there is an active selection.
   */
  hasSelection(): boolean {
    return this.selection.active && 
           this.selection.start !== null && 
           this.selection.end !== null;
  }
  
  /**
   * Gets the normalized selection range (start <= end).
   */
  private getNormalizedSelection(): { start: { row: number; col: number }; end: { row: number; col: number } } | null {
    if (!this.hasSelection() || !this.selection.start || !this.selection.end) {
      return null;
    }
    
    const start = this.selection.start;
    const end = this.selection.end;
    
    // Normalize so start is always before end
    if (start.row > end.row || (start.row === end.row && start.col > end.col)) {
      return { start: end, end: start };
    }
    
    return { start, end };
  }
  
  /**
   * Extracts text from the current selection.
   */
  private extractSelectedText(): string | null {
    const selection = this.getNormalizedSelection();
    if (!selection) {
      return null;
    }
    
    const lines: string[] = [];
    
    for (let row = selection.start.row; row <= selection.end.row; row++) {
      const line = this.terminal.getLine(row);
      if (!line) {
        continue;
      }
      
      let lineText = '';
      const startCol = row === selection.start.row ? selection.start.col : 0;
      const endCol = row === selection.end.row ? selection.end.col : line.cells.length - 1;
      
      for (let col = startCol; col <= endCol && col < line.cells.length; col++) {
        const cell = line.cells[col];
        if (cell) {
          // Handle wide characters properly
          if (cell.width === 2) {
            // This is the first cell of a wide character
            lineText += cell.char;
          } else if (cell.width === 0) {
            // This is a continuation cell of a wide character, skip it
            continue;
          } else {
            // Normal character
            lineText += cell.char;
          }
        } else {
          // Empty cell
          lineText += ' ';
        }
      }
      
      // Trim trailing spaces from each line
      lineText = lineText.trimEnd();
      lines.push(lineText);
    }
    
    // Join lines with newlines, but don't add trailing newline
    return lines.join('\n');
  }
  
  /**
   * Copies the current selection to the clipboard programmatically.
   * This is an alternative to the copy event handler for programmatic copying.
   */
  async copySelection(): Promise<boolean> {
    if (!this.hasSelection()) {
      return false;
    }
    
    const selectedText = this.extractSelectedText();
    if (!selectedText) {
      return false;
    }
    
    try {
      // Use the modern Clipboard API if available
      if (navigator.clipboard && navigator.clipboard.writeText) {
        await navigator.clipboard.writeText(selectedText);
        return true;
      } else {
        // Fallback for older browsers
        // Create a temporary textarea element
        const textarea = document.createElement('textarea');
        textarea.value = selectedText;
        textarea.style.position = 'fixed';
        textarea.style.opacity = '0';
        document.body.appendChild(textarea);
        
        try {
          textarea.select();
          const success = document.execCommand('copy');
          return success;
        } finally {
          document.body.removeChild(textarea);
        }
      }
    } catch (error) {
      console.error('Failed to copy to clipboard:', error);
      return false;
    }
  }
  
  // Placeholder methods for event handlers - will be implemented in subsequent subtasks
  
  private handleKeyDown(event: KeyboardEvent): void {
    // Prevent default browser behavior for most keys
    if (this.shouldPreventDefault(event)) {
      event.preventDefault();
    }
    
    // Convert DOM KeyboardEvent to our KeyEvent structure
    const keyEvent = this.convertToKeyEvent(event);
    
    // Encode the key event using libghostty-vt
    const encodedSequence = this.encodeKeyEvent(keyEvent);
    
    if (encodedSequence) {
      // Send encoded sequence to shell backend
      this.terminal.sendInput(encodedSequence);
    }
  }
  
  private handlePaste(event: ClipboardEvent): void {
    event.preventDefault();
    
    // Get pasted text from clipboard
    const pastedText = event.clipboardData?.getData('text/plain');
    if (!pastedText) {
      return;
    }
    
    // Check if bracketed paste mode is enabled
    const bracketedPasteEnabled = this.terminal.getMode('2004');
    
    if (bracketedPasteEnabled) {
      // Wrap pasted content with bracketed paste sequences
      const wrappedText = '\x1b[200~' + pastedText + '\x1b[201~';
      this.terminal.sendInput(wrappedText);
    } else {
      // Send pasted text directly
      this.terminal.sendInput(pastedText);
    }
  }
  
  private handleFocus(): void {
    this.focused = true;
    
    // Update visual state to indicate focus
    this.displayElement.classList.add('terminal-focused');
    this.displayElement.classList.remove('terminal-blurred');
    
    // Emit state change for potential cursor visibility updates
    this.render();
  }
  
  private handleBlur(): void {
    this.focused = false;
    
    // Update visual state to indicate blur
    this.displayElement.classList.add('terminal-blurred');
    this.displayElement.classList.remove('terminal-focused');
    
    // Emit state change for potential cursor visibility updates
    this.render();
  }
  
  private handleDisplayClick(event: MouseEvent): void {
    // Focus the input element when display is clicked
    this.focus();
    
    // Prevent default to avoid any unwanted selection behavior
    event.preventDefault();
  }
  
  private handleMouseDown(event: MouseEvent): void {
    // Only handle left mouse button
    if (event.button !== 0) {
      return;
    }
    
    // Focus the terminal
    this.focus();
    
    // Clear any existing selection
    this.clearSelection();
    
    // Calculate cell position from mouse coordinates
    const cellPos = this.getCellPositionFromMouse(event);
    if (cellPos) {
      // Start new selection
      this.selection = {
        active: true,
        start: cellPos,
        end: cellPos,
      };
      
      this.updateSelectionDisplay();
    }
    
    // Prevent default to avoid text selection
    event.preventDefault();
  }
  
  private handleMouseMove(event: MouseEvent): void {
    // Only handle if selection is active
    if (!this.selection.active || !this.selection.start) {
      return;
    }
    
    // Calculate cell position from mouse coordinates
    const cellPos = this.getCellPositionFromMouse(event);
    if (cellPos) {
      // Update selection end
      this.selection.end = cellPos;
      this.updateSelectionDisplay();
    }
  }
  
  private handleMouseUp(event: MouseEvent): void {
    // Only handle left mouse button
    if (event.button !== 0) {
      return;
    }
    
    // If we have an active selection, finalize it
    if (this.selection.active && this.selection.start && this.selection.end) {
      // Selection is now complete but remains active for copying
      this.updateSelectionDisplay();
    }
  }
  
  private handleCopy(event: ClipboardEvent): void {
    // Only handle copy if we have an active selection
    if (!this.hasSelection()) {
      return;
    }
    
    // Extract text from selection
    const selectedText = this.extractSelectedText();
    if (selectedText) {
      // Place text on clipboard
      event.clipboardData?.setData('text/plain', selectedText);
      event.preventDefault();
    }
  }
  
  private handleStateChange(): void {
    // Handle terminal state changes for rendering updates
    this.render();
  }
  
  /**
   * Renders the current terminal state to the display element.
   * Uses the Renderer class for optimized incremental rendering.
   */
  render(): void {
    this.renderer.render(this.terminal);
  }
  
  // Key encoding helper methods
  
  /**
   * Initializes the WASM key encoder.
   */
  private initializeKeyEncoder(): void {
    try {
      const ptrPtr = this.wasmInstance.exports.ghostty_wasm_alloc_opaque();
      const result = this.wasmInstance.exports.ghostty_key_encoder_new(0, ptrPtr);
      
      if (result !== 0) {
        throw new Error(`ghostty_key_encoder_new failed with result ${result}`);
      }
      
      this.keyEncoderPtr = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(ptrPtr, true);
      
      // Free the temporary pointer
      this.wasmInstance.exports.ghostty_wasm_free_opaque(ptrPtr);
      
    } catch (error) {
      console.error('Failed to initialize key encoder:', error);
      this.keyEncoderPtr = null;
    }
  }
  
  /**
   * Cleans up the WASM key encoder.
   */
  private cleanupKeyEncoder(): void {
    if (this.keyEncoderPtr !== null) {
      this.wasmInstance.exports.ghostty_key_encoder_free(this.keyEncoderPtr);
      this.keyEncoderPtr = null;
    }
  }
  
  /**
   * Determines if the default browser behavior should be prevented for a key event.
   */
  private shouldPreventDefault(event: KeyboardEvent): boolean {
    // Allow some essential browser shortcuts
    const allowedShortcuts = [
      // Refresh
      { ctrl: true, key: 'r' },
      { ctrl: true, key: 'R' },
      { key: 'F5' },
      
      // DevTools
      { ctrl: true, shift: true, key: 'I' },
      { key: 'F12' },
      
      // Zoom
      { ctrl: true, key: '=' },
      { ctrl: true, key: '+' },
      { ctrl: true, key: '-' },
      { ctrl: true, key: '0' },
      
      // Navigation
      { ctrl: true, key: 'l' }, // Address bar
      { ctrl: true, key: 'L' },
      { ctrl: true, key: 't' }, // New tab
      { ctrl: true, key: 'T' },
      { ctrl: true, key: 'w' }, // Close tab
      { ctrl: true, key: 'W' },
      { ctrl: true, shift: true, key: 'T' }, // Reopen closed tab
      
      // Find
      { ctrl: true, key: 'f' },
      { ctrl: true, key: 'F' },
    ];
    
    // Check if this key combination should be allowed
    for (const shortcut of allowedShortcuts) {
      const ctrlMatch = shortcut.ctrl ? event.ctrlKey : !event.ctrlKey;
      const shiftMatch = shortcut.shift ? event.shiftKey : !event.shiftKey;
      const keyMatch = shortcut.key === event.key || shortcut.key === event.code;
      
      if (ctrlMatch && shiftMatch && keyMatch) {
        return false; // Don't prevent default for allowed shortcuts
      }
    }
    
    // Prevent default for everything else when focused
    return this.focused;
  }
  
  /**
   * Converts a DOM KeyboardEvent to our KeyEvent structure.
   */
  private convertToKeyEvent(event: KeyboardEvent): KeyEvent {
    return {
      _type: "KeyEvent",
      code: event.code,
      key: event.key,
      shiftKey: event.shiftKey,
      altKey: event.altKey,
      metaKey: event.metaKey,
      ctrlKey: event.ctrlKey,
    };
  }
  
  /**
   * Encodes a KeyEvent using libghostty-vt and returns the escape sequence.
   */
  private encodeKeyEvent(keyEvent: KeyEvent): Uint8Array | null {
    if (this.keyEncoderPtr === null) {
      console.warn('Key encoder not initialized');
      return null;
    }
    
    try {
      // Create WASM key event
      const eventPtrPtr = this.wasmInstance.exports.ghostty_wasm_alloc_opaque();
      const result = this.wasmInstance.exports.ghostty_key_event_new(0, eventPtrPtr);
      
      if (result !== 0) {
        throw new Error(`ghostty_key_event_new failed with result ${result}`);
      }
      
      const eventPtr = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(eventPtrPtr, true);
      
      try {
        // Set action (1 = press)
        this.wasmInstance.exports.ghostty_key_event_set_action(eventPtr, 1);
        
        // Map key code
        const keyCode = KeyCodeMap[keyEvent.code] || 0; // GHOSTTY_KEY_UNIDENTIFIED = 0
        this.wasmInstance.exports.ghostty_key_event_set_key(eventPtr, keyCode);
        
        // Map modifiers
        let mods = 0;
        if (keyEvent.shiftKey) {
          mods |= 0x01; // GHOSTTY_MODS_SHIFT
          if (keyEvent.code === 'ShiftRight') mods |= 0x40; // GHOSTTY_MODS_SHIFT_SIDE
        }
        if (keyEvent.ctrlKey) {
          mods |= 0x02; // GHOSTTY_MODS_CTRL
          if (keyEvent.code === 'ControlRight') mods |= 0x80; // GHOSTTY_MODS_CTRL_SIDE
        }
        if (keyEvent.altKey) {
          mods |= 0x04; // GHOSTTY_MODS_ALT
          if (keyEvent.code === 'AltRight') mods |= 0x100; // GHOSTTY_MODS_ALT_SIDE
        }
        if (keyEvent.metaKey) {
          mods |= 0x08; // GHOSTTY_MODS_SUPER
          if (keyEvent.code === 'MetaRight') mods |= 0x200; // GHOSTTY_MODS_SUPER_SIDE
        }
        this.wasmInstance.exports.ghostty_key_event_set_mods(eventPtr, mods);
        
        // Set UTF-8 text if it's a single character
        if (keyEvent.key.length === 1) {
          const utf8Bytes = new TextEncoder().encode(keyEvent.key);
          const utf8Ptr = this.wasmInstance.exports.ghostty_wasm_alloc_u8_array(utf8Bytes.length);
          new Uint8Array(this.wasmInstance.exports.memory.buffer).set(utf8Bytes, utf8Ptr);
          this.wasmInstance.exports.ghostty_key_event_set_utf8(eventPtr, utf8Ptr, utf8Bytes.length);
          // Note: Don't free utf8Ptr here - the key event stores a pointer to it
        }
        
        // Set unshifted codepoint
        const unshiftedCodepoint = this.getUnshiftedCodepoint(keyEvent);
        if (unshiftedCodepoint !== 0) {
          this.wasmInstance.exports.ghostty_key_event_set_unshifted_codepoint(eventPtr, unshiftedCodepoint);
        }
        
        // Encode the key event
        const requiredPtr = this.wasmInstance.exports.ghostty_wasm_alloc_usize();
        this.wasmInstance.exports.ghostty_key_encoder_encode(
          this.keyEncoderPtr, eventPtr, 0, 0, requiredPtr
        );
        
        const required = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(requiredPtr, true);
        this.wasmInstance.exports.ghostty_wasm_free_usize(requiredPtr);
        
        if (required === 0) {
          return null; // No encoding for this key
        }
        
        const bufPtr = this.wasmInstance.exports.ghostty_wasm_alloc_u8_array(required);
        const writtenPtr = this.wasmInstance.exports.ghostty_wasm_alloc_usize();
        const encodeResult = this.wasmInstance.exports.ghostty_key_encoder_encode(
          this.keyEncoderPtr, eventPtr, bufPtr, required, writtenPtr
        );
        
        if (encodeResult !== 0) {
          this.wasmInstance.exports.ghostty_wasm_free_u8_array(bufPtr, required);
          this.wasmInstance.exports.ghostty_wasm_free_usize(writtenPtr);
          return null; // No encoding for this key
        }
        
        const written = new DataView(this.wasmInstance.exports.memory.buffer).getUint32(writtenPtr, true);
        const encoded = new Uint8Array(this.wasmInstance.exports.memory.buffer).slice(bufPtr, bufPtr + written);
        
        // Clean up
        this.wasmInstance.exports.ghostty_wasm_free_u8_array(bufPtr, required);
        this.wasmInstance.exports.ghostty_wasm_free_usize(writtenPtr);
        
        return encoded;
        
      } finally {
        // Clean up event
        this.wasmInstance.exports.ghostty_key_event_free(eventPtr);
        this.wasmInstance.exports.ghostty_wasm_free_opaque(eventPtrPtr);
      }
      
    } catch (error) {
      console.error('Key encoding error:', error);
      return null;
    }
  }
  
  /**
   * Gets the unshifted codepoint for a key event.
   * This is used for proper key encoding in terminal applications.
   */
  private getUnshiftedCodepoint(keyEvent: KeyEvent): number {
    // This is a simplified version - the full implementation would handle
    // keyboard layout mapping more comprehensively
    if (keyEvent.key.length === 1) {
      const codepoint = keyEvent.key.codePointAt(0);
      if (codepoint !== undefined) {
        // For shifted characters, try to get the unshifted version
        if (keyEvent.shiftKey) {
          const shiftMap: Record<string, string> = {
            '!': '1', '@': '2', '#': '3', '$': '4', '%': '5',
            '^': '6', '&': '7', '*': '8', '(': '9', ')': '0',
            '_': '-', '+': '=', '{': '[', '}': ']', '|': '\\',
            ':': ';', '"': "'", '<': ',', '>': '.', '?': '/',
            '~': '`',
          };
          
          const unshifted = shiftMap[keyEvent.key];
          if (unshifted) {
            return unshifted.codePointAt(0) || 0;
          }
          
          // For letters, convert to lowercase
          if (keyEvent.key >= 'A' && keyEvent.key <= 'Z') {
            return keyEvent.key.toLowerCase().codePointAt(0) || 0;
          }
        }
        
        return codepoint;
      }
    }
    
    return 0;
  }
}