import type { OscMessage, XtermOscMessage } from "./TerminalEmulationTypes";

/**
 * Parse OSC (Operating System Command) sequences for xterm extensions.
 * OSC sequences have the format: ESC ] Ps ; Pt BEL/ST
 * where Ps is the command number and Pt is the text parameter.
 */
export function parseOsc(msg: OscMessage): XtermOscMessage | null {
  const { raw, terminator } = msg;
  
  // OSC sequences start with ESC ] (0x1b 0x5d)
  if (!raw.startsWith('\x1b]')) {
    return null;
  }

  // Extract the payload (everything after ESC ])
  let payload = raw.slice(2);
  
  // Remove terminator from payload
  if (terminator === "BEL") {
    // Remove BEL (0x07) from end
    payload = payload.slice(0, -1);
  } else if (terminator === "ST") {
    // Remove ST (ESC \) from end
    payload = payload.slice(0, -2);
  }

  // Parse command number and text parameter
  const semicolonIndex = payload.indexOf(';');
  if (semicolonIndex === -1) {
    // No semicolon found - could be a query command
    const commandNum = parseInt(payload, 10);
    if (isNaN(commandNum)) {
      return null;
    }
    
    // Handle query commands
    switch (commandNum) {
      case 21:
        return {
          _type: "osc.queryWindowTitle",
          raw,
          terminator,
          implemented: true
        };
      default:
        return null;
    }
  }

  const commandStr = payload.slice(0, semicolonIndex);
  const commandNum = parseInt(commandStr, 10);
  
  if (isNaN(commandNum)) {
    return null;
  }

  const textParam = payload.slice(semicolonIndex + 1);
  
  // Handle color queries (OSC 10;? and OSC 11;?)
  if (textParam === "?") {
    switch (commandNum) {
      case 10:
        return {
          _type: "osc.queryForegroundColor",
          raw,
          terminator,
          implemented: false
        };
      case 11:
        return {
          _type: "osc.queryBackgroundColor",
          raw,
          terminator,
          implemented: false
        };
      default:
        return null;
    }
  }
  
  // Decode UTF-8 text parameter
  const decodedText = decodeUtf8Text(textParam);
  
  // Handle title setting commands
  switch (commandNum) {
    case 0:
      return {
        _type: "osc.setTitleAndIcon",
        title: decodedText,
        raw,
        terminator,
        implemented: true
      };
    
    case 1:
      return {
        _type: "osc.setIconName",
        iconName: decodedText,
        raw,
        terminator,
        implemented: true
      };
    
    case 2:
      return {
        _type: "osc.setWindowTitle",
        title: decodedText,
        raw,
        terminator,
        implemented: true
      };
    
    default:
      return null;
  }
}

/**
 * Decode UTF-8 text from OSC parameter.
 * Handles proper UTF-8 decoding for international characters.
 */
function decodeUtf8Text(text: string): string {
  try {
    // The text is already a string, but we need to ensure proper UTF-8 handling
    // For now, return as-is since JavaScript strings are already UTF-16
    // In a real implementation, we might need more sophisticated UTF-8 handling
    return text;
  } catch (error) {
    // If decoding fails, return the original text
    return text;
  }
}

/**
 * Validate OSC parameter format and ranges.
 */
export function validateOscParameters(commandNum: number, textParam: string): boolean {
  // Command number validation
  if (commandNum < 0 || commandNum > 999) {
    return false;
  }
  
  // Text parameter validation
  if (textParam.length > 1024) {
    // Limit title length to prevent excessive memory usage
    return false;
  }
  
  // Check for control characters that shouldn't be in titles
  for (let i = 0; i < textParam.length; i++) {
    const charCode = textParam.charCodeAt(i);
    if (charCode < 0x20 && charCode !== 0x09) {
      // Allow tab (0x09) but reject other control characters
      return false;
    }
  }
  
  return true;
}