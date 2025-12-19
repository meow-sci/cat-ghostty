import type { CsiMessage, CsiSetCursorStyle, CsiDecModeSet, CsiDecModeReset, CsiCursorUp, CsiCursorDown, CsiCursorForward, CsiCursorBackward, CsiCursorNextLine, CsiCursorPrevLine, CsiCursorHorizontalAbsolute, CsiVerticalPositionAbsolute, CsiCursorPosition, CsiEraseInDisplayMode, CsiEraseInDisplay, CsiEraseInLineMode, CsiEraseInLine, CsiScrollUp, CsiScrollDown, CsiSetScrollRegion, CsiSaveCursorPosition, CsiRestoreCursorPosition, CsiUnknown, CsiDeviceAttributesPrimary, CsiDeviceAttributesSecondary, CsiCursorPositionReport, CsiTerminalSizeQuery, CsiCharacterSetQuery, CsiWindowManipulation, CsiInsertMode, CsiEraseCharacter, CsiEnhancedSgrMode, CsiPrivateSgrMode, CsiSgrWithIntermediate, CsiUnknownViSequence } from "./TerminalEmulationTypes";

export function parseCsi(bytes: number[], raw: string): CsiMessage {
  // bytes: ESC [ ... final
  const finalByte = bytes[bytes.length - 1];
  const final = String.fromCharCode(finalByte);

  let paramsText = "";
  let intermediate = "";

  for (let i = 2; i < bytes.length - 1; i++) {
    const b = bytes[i];
    if (b >= 0x30 && b <= 0x3f) {
      paramsText += String.fromCharCode(b);
      continue;
    }
    if (b >= 0x20 && b <= 0x2f) {
      intermediate += String.fromCharCode(b);
      continue;
    }
  }

  const parsed = parseCsiParams(paramsText);
  const isPrivate = parsed.private;
  const params = parsed.params;
  const prefix = parsed.prefix;

  // DECSCUSR: CSI Ps SP q
  if (final === "q" && intermediate === " ") {
    const style = validateCursorStyle(getParam(params, 0, 0));
    const msg: CsiSetCursorStyle = { _type: "csi.setCursorStyle", raw, style, implemented: true };
    return msg;
  }

  // DEC private modes: CSI ? Pm h / l
  if (isPrivate && (final === "h" || final === "l")) {
    const modes = validateDecModes(params);
    if (final === "h") {
      const msg: CsiDecModeSet = { _type: "csi.decModeSet", raw, modes, implemented: true };
      return msg;
    }
    const msg: CsiDecModeReset = { _type: "csi.decModeReset", raw, modes, implemented: true };
    return msg;
  }

  // Standard modes: CSI Pm h / l (non-private)
  if (!isPrivate && !prefix && (final === "h" || final === "l")) {
    // IRM (Insert/Replace Mode): CSI 4 h/l
    if (params.length === 1 && params[0] === 4) {
      const msg: CsiInsertMode = { 
        _type: "csi.insertMode", 
        raw, 
        enable: final === "h",
        implemented: false
      };
      return msg;
    }
  }

  if (final === "A") {
    const msg: CsiCursorUp = { _type: "csi.cursorUp", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "B") {
    const msg: CsiCursorDown = { _type: "csi.cursorDown", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "C") {
    const msg: CsiCursorForward = { _type: "csi.cursorForward", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "D") {
    const msg: CsiCursorBackward = { _type: "csi.cursorBackward", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "E") {
    const msg: CsiCursorNextLine = { _type: "csi.cursorNextLine", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "F") {
    const msg: CsiCursorPrevLine = { _type: "csi.cursorPrevLine", raw, count: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "G") {
    const msg: CsiCursorHorizontalAbsolute = { _type: "csi.cursorHorizontalAbsolute", raw, column: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "d") {
    const msg: CsiVerticalPositionAbsolute = { _type: "csi.verticalPositionAbsolute", raw, row: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "H" || final === "f") {
    const msg: CsiCursorPosition = {
      _type: "csi.cursorPosition",
      raw,
      row: getParam(params, 0, 1),
      column: getParam(params, 1, 1),
      implemented: true
    };
    return msg;
  }

  if (final === "J") {
    const modeValue = getParam(params, 0, 0);
    const mode: CsiEraseInDisplayMode = (modeValue === 0 || modeValue === 1 || modeValue === 2 || modeValue === 3) ? modeValue : 0;
    const msg: CsiEraseInDisplay = { _type: "csi.eraseInDisplay", raw, mode, implemented: true };
    return msg;
  }

  if (final === "K") {
    const modeValue = getParam(params, 0, 0);
    const mode: CsiEraseInLineMode = (modeValue === 0 || modeValue === 1 || modeValue === 2) ? modeValue : 0;
    const msg: CsiEraseInLine = { _type: "csi.eraseInLine", raw, mode, implemented: true };
    return msg;
  }

  if (final === "S") {
    const msg: CsiScrollUp = { _type: "csi.scrollUp", raw, lines: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "T" && params.length <= 1 && !isPrivate) {
    const msg: CsiScrollDown = { _type: "csi.scrollDown", raw, lines: getParam(params, 0, 1), implemented: true };
    return msg;
  }

  if (final === "r") {
    const top = params.length >= 1 ? params[0] : undefined;
    const bottom = params.length >= 2 ? params[1] : undefined;
    const msg: CsiSetScrollRegion = { _type: "csi.setScrollRegion", raw, top, bottom, implemented: true };
    return msg;
  }

  if (final === "s") {
    const msg: CsiSaveCursorPosition = { _type: "csi.saveCursorPosition", raw, implemented: true };
    return msg;
  }

  if (final === "u") {
    const msg: CsiRestoreCursorPosition = { _type: "csi.restoreCursorPosition", raw, implemented: true };
    return msg;
  }

  // Device Attributes queries
  if (final === "c") {
    // Secondary DA: CSI > c or CSI > 0 c
    if (prefix === ">" && (params.length === 0 || (params.length === 1 && params[0] === 0))) {
      const msg: CsiDeviceAttributesSecondary = { 
        _type: "csi.deviceAttributesSecondary", 
        raw,
        implemented: true
      };
      return msg;
    }
    // Primary DA: CSI c or CSI 0 c
    if (!isPrivate && !prefix && (params.length === 0 || (params.length === 1 && params[0] === 0))) {
      const msg: CsiDeviceAttributesPrimary = { 
        _type: "csi.deviceAttributesPrimary", 
        raw,
        implemented: true
      };
      return msg;
    }
  }

  // Cursor Position Report request: CSI 6 n
  // Character Set Query: CSI ? 26 n
  if (final === "n") {
    if (isPrivate && params.length === 1 && params[0] === 26) {
      const msg: CsiCharacterSetQuery = { 
        _type: "csi.characterSetQuery", 
        raw,
        implemented: true
      };
      return msg;
    }
    if (!isPrivate && !prefix && params.length === 1 && params[0] === 6) {
      const msg: CsiCursorPositionReport = { 
        _type: "csi.cursorPositionReport", 
        raw,
        implemented: true
      };
      return msg;
    }
  }

  // Window manipulation and queries: CSI Ps t or CSI Ps ; Ps ; Ps t
  if (final === "t" && !isPrivate && !prefix) {
    if (params.length === 1 && params[0] === 18) {
      const msg: CsiTerminalSizeQuery = { 
        _type: "csi.terminalSizeQuery", 
        raw,
        implemented: true
      };
      return msg;
    }
    
    // Window manipulation commands
    if (params.length >= 1) {
      // Check for title stack operations (vi compatibility)
      const operation = params[0];
      let implemented = false;
      
      // Title stack operations: 22;1t, 22;2t, 23;1t, 23;2t
      if (operation === 22 || operation === 23) {
        if (params.length >= 2) {
          const subOperation = params[1];
          if (subOperation === 1 || subOperation === 2) {
            implemented = true; // We'll handle these with graceful acknowledgment
          }
        }
      }
      
      const msg: CsiWindowManipulation = {
        _type: "csi.windowManipulation",
        raw,
        operation: params[0],
        params: params.slice(1),
        implemented
      };
      return msg;
    }
  }

  // Erase Character: CSI Ps X
  if (final === "X") {
    const msg: CsiEraseCharacter = { 
      _type: "csi.eraseCharacter", 
      raw, 
      count: getParam(params, 0, 1),
      implemented: true
    };
    return msg;
  }

  // Enhanced SGR sequences: CSI > Ps ; Ps m
  if (final === "m" && prefix === ">") {
    const msg: CsiEnhancedSgrMode = {
      _type: "csi.enhancedSgrMode",
      raw,
      params,
      implemented: false
    };
    return msg;
  }

  // Private SGR sequences: CSI ? Ps m
  if (final === "m" && isPrivate) {
    const msg: CsiPrivateSgrMode = {
      _type: "csi.privateSgrMode",
      raw,
      params,
      implemented: false
    };
    return msg;
  }

  // SGR with intermediate characters: CSI Ps % m
  if (final === "m" && intermediate.length > 0) {
    const msg: CsiSgrWithIntermediate = {
      _type: "csi.sgrWithIntermediate",
      raw,
      params,
      intermediate,
      implemented: false
    };
    return msg;
  }

  // Unknown vi sequences: CSI n M (e.g., CSI 11M)
  if (final === "M" && !isPrivate && !prefix && intermediate.length === 0) {
    // Validate that we have exactly one numeric parameter
    if (params.length === 1 && Number.isInteger(params[0]) && params[0] >= 0) {
      const msg: CsiUnknownViSequence = {
        _type: "csi.unknownViSequence",
        raw,
        sequenceNumber: params[0],
        implemented: false // Gracefully acknowledged but not implemented
      };
      return msg;
    }
  }

  const unknown: CsiUnknown = {
    _type: "csi.unknown",
    raw,
    private: isPrivate,
    params,
    intermediate,
    final,
    implemented: false
  };

  return unknown;
}


function parseCsiParams(paramText: string): { private: boolean; params: number[]; prefix?: string } {
  let isPrivate = false;
  let prefix: string | undefined = undefined;
  let text = paramText;

  if (text.startsWith("?")) {
    isPrivate = true;
    text = text.slice(1);
  } else if (text.startsWith(">")) {
    prefix = ">";
    text = text.slice(1);
  }

  const params: number[] = [];

  if (text.length === 0) {
    return { private: isPrivate, params, prefix };
  }

  const parts = text.split(";");
  for (let i = 0; i < parts.length; i++) {
    const part = parts[i];
    if (part.length === 0) {
      continue;
    }
    const n = Number.parseInt(part, 10);
    if (!Number.isFinite(n)) {
      continue;
    }
    params.push(n);
  }

  return { private: isPrivate, params, prefix };
}

function getParam(params: number[], index: number, fallback: number): number {
  const v = params[index];
  if (v === undefined) {
    return fallback;
  }
  return v;
}

/**
 * Validates DEC private mode numbers and filters out invalid ones.
 * Returns only the valid mode numbers within acceptable ranges.
 */
function validateDecModes(params: number[]): number[] {
  const validModes: number[] = [];
  
  for (const mode of params) {
    if (isValidDecModeNumber(mode)) {
      validModes.push(mode);
    }
  }
  
  return validModes;
}

/**
 * Checks if a DEC private mode number is valid.
 * Validates that the mode number is within acceptable ranges for DEC private modes.
 */
function isValidDecModeNumber(mode: number): boolean {
  // Validate mode is a positive integer
  if (!Number.isInteger(mode) || mode < 0) {
    return false;
  }
  
  // DEC private modes can range from 1 to 65535 (16-bit unsigned integer range)
  // This covers all standard and extended xterm DEC private modes
  if (mode > 65535) {
    return false;
  }
  
  return true;
}

/**
 * Validates and normalizes cursor style parameter for DECSCUSR.
 * Valid cursor styles are 0-6:
 * - 0 or 1: Blinking block (default)
 * - 2: Steady block
 * - 3: Blinking underline
 * - 4: Steady underline
 * - 5: Blinking bar
 * - 6: Steady bar
 * 
 * Invalid values are clamped to 0 (default blinking block).
 */
function validateCursorStyle(style: number): number {
  // Validate style is a non-negative integer
  if (!Number.isInteger(style) || style < 0) {
    return 0;
  }
  
  // Valid cursor styles are 0-6
  if (style > 6) {
    return 0;
  }
  
  return style;
}