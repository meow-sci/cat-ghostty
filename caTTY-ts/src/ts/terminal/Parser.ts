/**
 * Parser for terminal escape sequences and control characters.
 * Implements a state machine for parsing VT100/xterm-compatible sequences.
 */

/**
 * Handlers for parser actions.
 * These callbacks are invoked when the parser encounters various terminal actions.
 */
export interface ParserHandlers {
  /** Handle a printable character */
  onPrintable?: (char: string) => void;
  
  /** Handle line feed (LF, 0x0A) */
  onLineFeed?: () => void;
  
  /** Handle carriage return (CR, 0x0D) */
  onCarriageReturn?: () => void;
  
  /** Handle backspace (BS, 0x08) */
  onBackspace?: () => void;
  
  /** Handle horizontal tab (HT, 0x09) */
  onTab?: () => void;
  
  /** Handle bell (BEL, 0x07) */
  onBell?: () => void;
  
  /** Handle CSI sequence */
  onCsi?: (params: number[], intermediates: string, final: number) => void;
  
  /** Handle OSC sequence */
  onOsc?: (command: number, data: string) => void;
  
  /** Handle escape sequence */
  onEscape?: (intermediates: string, final: number) => void;
}

/**
 * Parser state machine states.
 * Based on the VT100/xterm state machine for parsing escape sequences.
 */
export enum ParserState {
  /** Normal text processing state */
  Ground,
  
  /** ESC received, waiting for next character */
  Escape,
  
  /** ESC + intermediate character(s) received */
  EscapeIntermediate,
  
  /** CSI sequence started (ESC [) */
  CsiEntry,
  
  /** CSI parameter bytes (0-9, ;) */
  CsiParam,
  
  /** CSI intermediate bytes (space through /) */
  CsiIntermediate,
  
  /** CSI sequence is invalid, ignore until final byte */
  CsiIgnore,
  
  /** OSC sequence string data */
  OscString,
  
  /** UTF-8 multi-byte sequence in progress */
  Utf8
}

/**
 * Parser for terminal input sequences.
 * Processes raw byte input and converts it to terminal actions.
 */
export class Parser {
  /** Current parser state */
  private state: ParserState = ParserState.Ground;
  
  /** Buffer for collecting CSI parameters */
  private csiParams: number[] = [];
  
  /** Buffer for collecting CSI intermediate characters */
  private csiIntermediates: string = '';
  
  /** Buffer for collecting OSC string data */
  private oscData: string = '';
  
  /** OSC command number */
  private oscCommand: number = 0;
  
  /** Buffer for UTF-8 multi-byte sequence */
  private utf8Buffer: number[] = [];
  
  /** Expected number of bytes in current UTF-8 sequence */
  private utf8Expected: number = 0;
  
  /** Buffer for escape intermediate characters */
  private escapeIntermediates: string = '';
  
  /** Handlers for parser actions */
  private handlers: ParserHandlers;
  
  /**
   * Create a new parser with the specified handlers.
   * @param handlers Callbacks for parser actions
   */
  constructor(handlers: ParserHandlers = {}) {
    this.handlers = handlers;
  }
  
  /**
   * Parse a chunk of input data.
   * @param data Raw byte data to parse
   */
  parse(data: Uint8Array): void {
    for (let i = 0; i < data.length; i++) {
      const byte = data[i];
      this.processByte(byte);
    }
  }
  
  /**
   * Process a single byte through the state machine.
   * @param byte The byte to process
   */
  private processByte(byte: number): void {
    // Handle UTF-8 continuation bytes in Utf8 state
    if (this.state === ParserState.Utf8) {
      this.handleUtf8Byte(byte);
      return;
    }
    
    // Check for UTF-8 multi-byte sequence start
    if (this.state === ParserState.Ground && byte >= 0x80) {
      this.startUtf8Sequence(byte);
      return;
    }
    
    // Handle control characters (0x00-0x1F, 0x7F) in most states
    if (byte < 0x20 || byte === 0x7F) {
      this.handleControl(byte);
      return;
    }
    
    // State-specific processing
    switch (this.state) {
      case ParserState.Ground:
        this.handleGround(byte);
        break;
        
      case ParserState.Escape:
        this.handleEscape(byte);
        break;
        
      case ParserState.EscapeIntermediate:
        this.handleEscapeIntermediate(byte);
        break;
        
      case ParserState.CsiEntry:
        this.handleCsiEntry(byte);
        break;
        
      case ParserState.CsiParam:
        this.handleCsiParam(byte);
        break;
        
      case ParserState.CsiIntermediate:
        this.handleCsiIntermediate(byte);
        break;
        
      case ParserState.CsiIgnore:
        this.handleCsiIgnore(byte);
        break;
        
      case ParserState.OscString:
        this.handleOscString(byte);
        break;
    }
  }
  
  /**
   * Handle a byte in Ground state (normal printable characters).
   */
  private handleGround(byte: number): void {
    // Printable ASCII character
    const char = String.fromCharCode(byte);
    this.handlePrintable(char);
  }
  
  /**
   * Handle control characters (0x00-0x1F, 0x7F).
   */
  private handleControl(byte: number): void {
    switch (byte) {
      case 0x1B: // ESC
        this.enterEscape();
        break;
        
      case 0x0A: // LF (Line Feed)
        this.handleLineFeed();
        break;
        
      case 0x0D: // CR (Carriage Return)
        this.handleCarriageReturn();
        break;
        
      case 0x08: // BS (Backspace)
        this.handleBackspace();
        break;
        
      case 0x09: // HT (Horizontal Tab)
        this.handleTab();
        break;
        
      case 0x07: // BEL (Bell)
        this.handleBell();
        break;
        
      case 0x00: // NUL - ignore
        break;
        
      default:
        // Other control characters - ignore for now
        break;
    }
  }
  
  /**
   * Enter Escape state.
   */
  private enterEscape(): void {
    this.state = ParserState.Escape;
    this.escapeIntermediates = '';
  }
  
  /**
   * Handle a byte in Escape state.
   */
  private handleEscape(byte: number): void {
    switch (byte) {
      case 0x5B: // [ - CSI
        this.enterCsi();
        break;
        
      case 0x5D: // ] - OSC
        this.enterOsc();
        break;
        
      case 0x20: // Space through / are intermediates
      case 0x21:
      case 0x22:
      case 0x23:
      case 0x24:
      case 0x25:
      case 0x26:
      case 0x27:
      case 0x28:
      case 0x29:
      case 0x2A:
      case 0x2B:
      case 0x2C:
      case 0x2D:
      case 0x2E:
      case 0x2F:
        this.escapeIntermediates += String.fromCharCode(byte);
        this.state = ParserState.EscapeIntermediate;
        break;
        
      default:
        // Final byte - execute escape sequence
        this.executeEscape(byte);
        this.state = ParserState.Ground;
        break;
    }
  }
  
  /**
   * Handle a byte in EscapeIntermediate state.
   */
  private handleEscapeIntermediate(byte: number): void {
    if (byte >= 0x20 && byte <= 0x2F) {
      // More intermediate bytes
      this.escapeIntermediates += String.fromCharCode(byte);
    } else {
      // Final byte
      this.executeEscape(byte);
      this.state = ParserState.Ground;
    }
  }
  
  /**
   * Enter CSI state.
   */
  private enterCsi(): void {
    this.state = ParserState.CsiEntry;
    this.csiParams = [];
    this.csiIntermediates = '';
  }
  
  /**
   * Handle a byte in CsiEntry state.
   */
  private handleCsiEntry(byte: number): void {
    if (byte >= 0x30 && byte <= 0x39) {
      // Digit - start parameter
      this.csiParams.push(byte - 0x30);
      this.state = ParserState.CsiParam;
    } else if (byte === 0x3B) {
      // Semicolon - empty parameter
      this.csiParams.push(0);
      this.state = ParserState.CsiParam;
    } else if (byte >= 0x3C && byte <= 0x3F) {
      // Private marker - ignore sequence
      this.state = ParserState.CsiIgnore;
    } else if (byte >= 0x20 && byte <= 0x2F) {
      // Intermediate byte
      this.csiIntermediates += String.fromCharCode(byte);
      this.state = ParserState.CsiIntermediate;
    } else if (byte >= 0x40 && byte <= 0x7E) {
      // Final byte
      this.executeCsi(byte);
      this.state = ParserState.Ground;
    }
  }
  
  /**
   * Handle a byte in CsiParam state.
   */
  private handleCsiParam(byte: number): void {
    if (byte >= 0x30 && byte <= 0x39) {
      // Digit - add to current parameter
      const lastIdx = this.csiParams.length - 1;
      this.csiParams[lastIdx] = this.csiParams[lastIdx] * 10 + (byte - 0x30);
    } else if (byte === 0x3B) {
      // Semicolon - start new parameter
      this.csiParams.push(0);
    } else if (byte >= 0x3C && byte <= 0x3F) {
      // Invalid - ignore sequence
      this.state = ParserState.CsiIgnore;
    } else if (byte >= 0x20 && byte <= 0x2F) {
      // Intermediate byte
      this.csiIntermediates += String.fromCharCode(byte);
      this.state = ParserState.CsiIntermediate;
    } else if (byte >= 0x40 && byte <= 0x7E) {
      // Final byte
      this.executeCsi(byte);
      this.state = ParserState.Ground;
    }
  }
  
  /**
   * Handle a byte in CsiIntermediate state.
   */
  private handleCsiIntermediate(byte: number): void {
    if (byte >= 0x20 && byte <= 0x2F) {
      // More intermediate bytes
      this.csiIntermediates += String.fromCharCode(byte);
    } else if (byte >= 0x40 && byte <= 0x7E) {
      // Final byte
      this.executeCsi(byte);
      this.state = ParserState.Ground;
    } else {
      // Invalid - ignore
      this.state = ParserState.CsiIgnore;
    }
  }
  
  /**
   * Handle a byte in CsiIgnore state.
   */
  private handleCsiIgnore(byte: number): void {
    if (byte >= 0x40 && byte <= 0x7E) {
      // Final byte - return to ground
      this.state = ParserState.Ground;
    }
    // Otherwise keep ignoring
  }
  
  /**
   * Enter OSC state.
   */
  private enterOsc(): void {
    this.state = ParserState.OscString;
    this.oscData = '';
    this.oscCommand = 0;
  }
  
  /**
   * Handle a byte in OscString state.
   */
  private handleOscString(byte: number): void {
    if (byte === 0x07) {
      // BEL terminates OSC
      this.executeOsc();
      this.state = ParserState.Ground;
    } else if (byte === 0x1B) {
      // ESC might be start of ST (ESC \)
      // For now, treat as terminator
      this.executeOsc();
      this.state = ParserState.Ground;
    } else {
      // Accumulate OSC data
      this.oscData += String.fromCharCode(byte);
    }
  }
  
  /**
   * Start a UTF-8 multi-byte sequence.
   */
  private startUtf8Sequence(byte: number): void {
    this.utf8Buffer = [byte];
    
    // Determine expected sequence length
    if ((byte & 0xE0) === 0xC0) {
      // 2-byte sequence (110xxxxx)
      this.utf8Expected = 2;
    } else if ((byte & 0xF0) === 0xE0) {
      // 3-byte sequence (1110xxxx)
      this.utf8Expected = 3;
    } else if ((byte & 0xF8) === 0xF0) {
      // 4-byte sequence (11110xxx)
      this.utf8Expected = 4;
    } else {
      // Invalid UTF-8 start byte - emit replacement character
      this.handlePrintable('\uFFFD');
      return;
    }
    
    this.state = ParserState.Utf8;
  }
  
  /**
   * Handle a UTF-8 continuation byte.
   */
  private handleUtf8Byte(byte: number): void {
    // Check if this is a valid continuation byte (10xxxxxx)
    if ((byte & 0xC0) !== 0x80) {
      // Invalid continuation - emit replacement character and process this byte
      this.handlePrintable('\uFFFD');
      this.state = ParserState.Ground;
      this.processByte(byte);
      return;
    }
    
    this.utf8Buffer.push(byte);
    
    // Check if sequence is complete
    if (this.utf8Buffer.length === this.utf8Expected) {
      this.completeUtf8Sequence();
    }
  }
  
  /**
   * Complete a UTF-8 sequence and emit the character.
   */
  private completeUtf8Sequence(): void {
    try {
      // Decode UTF-8 sequence
      const bytes = new Uint8Array(this.utf8Buffer);
      const decoder = new TextDecoder('utf-8', { fatal: true });
      const char = decoder.decode(bytes);
      
      this.handlePrintable(char);
    } catch (e) {
      // Invalid UTF-8 sequence - emit replacement character
      this.handlePrintable('\uFFFD');
    }
    
    this.state = ParserState.Ground;
    this.utf8Buffer = [];
    this.utf8Expected = 0;
  }
  
  /**
   * Execute an escape sequence.
   */
  private executeEscape(final: number): void {
    this.handlers.onEscape?.(this.escapeIntermediates, final);
  }
  
  /**
   * Execute a CSI sequence.
   */
  private executeCsi(final: number): void {
    this.handlers.onCsi?.(this.csiParams, this.csiIntermediates, final);
  }
  
  /**
   * Execute an OSC sequence.
   */
  private executeOsc(): void {
    // Parse OSC command from data
    const semicolonIndex = this.oscData.indexOf(';');
    if (semicolonIndex !== -1) {
      const commandStr = this.oscData.substring(0, semicolonIndex);
      const command = parseInt(commandStr, 10);
      const data = this.oscData.substring(semicolonIndex + 1);
      this.handlers.onOsc?.(command, data);
    } else {
      // No semicolon - treat entire string as command with no data
      const command = parseInt(this.oscData, 10);
      this.handlers.onOsc?.(command, '');
    }
  }
  
  /**
   * Handle a printable character.
   */
  private handlePrintable(char: string): void {
    this.handlers.onPrintable?.(char);
  }
  
  /**
   * Handle line feed control character (LF, 0x0A).
   * Moves the cursor to the next line.
   */
  private handleLineFeed(): void {
    this.handlers.onLineFeed?.();
  }
  
  /**
   * Handle carriage return control character (CR, 0x0D).
   * Moves the cursor to column 0 of the current line.
   */
  private handleCarriageReturn(): void {
    this.handlers.onCarriageReturn?.();
  }
  
  /**
   * Handle backspace control character (BS, 0x08).
   * Moves the cursor one position left if not at column 0.
   */
  private handleBackspace(): void {
    this.handlers.onBackspace?.();
  }
  
  /**
   * Handle horizontal tab control character (HT, 0x09).
   * Moves the cursor to the next tab stop.
   */
  private handleTab(): void {
    this.handlers.onTab?.();
  }
  
  /**
   * Handle bell control character (BEL, 0x07).
   * Triggers an audible or visual bell.
   */
  private handleBell(): void {
    this.handlers.onBell?.();
  }
  
  /**
   * Reset the parser to initial state.
   */
  reset(): void {
    this.state = ParserState.Ground;
    this.csiParams = [];
    this.csiIntermediates = '';
    this.oscData = '';
    this.oscCommand = 0;
    this.utf8Buffer = [];
    this.utf8Expected = 0;
    this.escapeIntermediates = '';
  }
}
