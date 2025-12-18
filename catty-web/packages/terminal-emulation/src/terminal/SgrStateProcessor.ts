/**
 * SGR State Processor
 * 
 * Processes SGR messages and updates SGR state accordingly.
 * Handles the conversion from SGR escape sequences to styling state.
 */

import type { SgrMessage, SgrNamedColor } from './TerminalEmulationTypes';
import type { SgrState } from './SgrStyleManager';
import { createDefaultSgrState } from './SgrStyleManager';

/**
 * Process SGR messages and update the current SGR state
 * @param currentState Current SGR state
 * @param messages Array of SGR messages to process
 * @returns Updated SGR state
 */
export function processSgrMessages(currentState: SgrState, messages: SgrMessage[]): SgrState {
  // Create a copy of the current state to avoid mutation
  const newState: SgrState = structuredClone(currentState);
  
  for (const message of messages) {
    processSgrMessage(newState, message);
  }
  
  return newState;
}

/**
 * Process a single SGR message and update the state
 */
function processSgrMessage(state: SgrState, message: SgrMessage): void {
  switch (message._type) {
    case 'sgr.reset':
      // Reset all attributes to default
      Object.assign(state, createDefaultSgrState());
      break;
      
    case 'sgr.bold':
      state.bold = true;
      break;
      
    case 'sgr.faint':
      state.faint = true;
      break;
      
    case 'sgr.italic':
      state.italic = true;
      break;
      
    case 'sgr.underline':
      state.underline = true;
      state.underlineStyle = message.style;
      break;
      
    case 'sgr.slowBlink':
    case 'sgr.rapidBlink':
      state.blink = true;
      break;
      
    case 'sgr.inverse':
      state.inverse = true;
      break;
      
    case 'sgr.hidden':
      state.hidden = true;
      break;
      
    case 'sgr.strikethrough':
      state.strikethrough = true;
      break;
      
    case 'sgr.font':
      state.font = message.font;
      break;
      
    case 'sgr.normalIntensity':
      state.bold = false;
      state.faint = false;
      break;
      
    case 'sgr.notItalic':
      state.italic = false;
      break;
      
    case 'sgr.notUnderlined':
      state.underline = false;
      state.underlineStyle = null;
      break;
      
    case 'sgr.notBlinking':
      state.blink = false;
      break;
      
    case 'sgr.notInverse':
      state.inverse = false;
      break;
      
    case 'sgr.notHidden':
      state.hidden = false;
      break;
      
    case 'sgr.notStrikethrough':
      state.strikethrough = false;
      break;
      
    case 'sgr.foregroundColor':
      state.foregroundColor = message.color;
      break;
      
    case 'sgr.defaultForeground':
      state.foregroundColor = null;
      break;
      
    case 'sgr.backgroundColor':
      state.backgroundColor = message.color;
      break;
      
    case 'sgr.defaultBackground':
      state.backgroundColor = null;
      break;
      
    case 'sgr.underlineColor':
      state.underlineColor = message.color;
      break;
      
    case 'sgr.defaultUnderlineColor':
      state.underlineColor = null;
      break;
      
    // Handle other SGR types that don't affect basic styling
    case 'sgr.doubleUnderline':
      state.underline = true;
      state.underlineStyle = 'double';
      break;
      
    case 'sgr.fraktur':
    case 'sgr.proportionalSpacing':
    case 'sgr.disableProportionalSpacing':
    case 'sgr.framed':
    case 'sgr.encircled':
    case 'sgr.overlined':
    case 'sgr.notFramed':
    case 'sgr.notOverlined':
    case 'sgr.ideogram':
    case 'sgr.superscript':
    case 'sgr.subscript':
    case 'sgr.notSuperscriptSubscript':
      // These are not commonly supported or implemented
      // Ignore for now
      break;
      
    case 'sgr.enhancedMode':
      // Enhanced SGR mode with > prefix (e.g., CSI > 4 ; 2 m)
      // For now, gracefully ignore these advanced modes
      break;
      
    case 'sgr.privateMode':
      // Private SGR mode with ? prefix (e.g., CSI ? 4 m)
      // For now, gracefully ignore these private modes
      break;
      
    case 'sgr.withIntermediate':
      // SGR with intermediate characters (e.g., CSI 0 % m)
      // Handle specific cases like reset with %
      if (message.intermediate === '%' && message.params.length === 1 && message.params[0] === 0) {
        // Reset all attributes (similar to SGR 0)
        Object.assign(state, createDefaultSgrState());
      }
      // For other intermediate sequences, gracefully ignore
      break;
      
    case 'sgr.unknown':
      // Unknown SGR parameters - ignore gracefully
      break;
  }
}

/**
 * Convert ANSI color code to named color
 * @param colorCode ANSI color code (30-37, 40-47, 90-97, 100-107)
 * @returns Named color or null if invalid
 */
export function ansiCodeToNamedColor(colorCode: number): SgrNamedColor | null {
  // Standard foreground colors (30-37)
  if (colorCode >= 30 && colorCode <= 37) {
    const colors: SgrNamedColor[] = [
      'black', 'red', 'green', 'yellow',
      'blue', 'magenta', 'cyan', 'white'
    ];
    return colors[colorCode - 30];
  }
  
  // Bright foreground colors (90-97)
  if (colorCode >= 90 && colorCode <= 97) {
    const colors: SgrNamedColor[] = [
      'brightBlack', 'brightRed', 'brightGreen', 'brightYellow',
      'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
    ];
    return colors[colorCode - 90];
  }
  
  // Standard background colors (40-47)
  if (colorCode >= 40 && colorCode <= 47) {
    const colors: SgrNamedColor[] = [
      'black', 'red', 'green', 'yellow',
      'blue', 'magenta', 'cyan', 'white'
    ];
    return colors[colorCode - 40];
  }
  
  // Bright background colors (100-107)
  if (colorCode >= 100 && colorCode <= 107) {
    const colors: SgrNamedColor[] = [
      'brightBlack', 'brightRed', 'brightGreen', 'brightYellow',
      'brightBlue', 'brightMagenta', 'brightCyan', 'brightWhite'
    ];
    return colors[colorCode - 100];
  }
  
  return null;
}

/**
 * Apply inverse video effect to SGR state
 * Swaps foreground and background colors
 */
export function applyInverseVideo(state: SgrState): SgrState {
  if (!state.inverse) {
    return state;
  }
  
  const newState = { ...state };
  
  // Swap foreground and background colors
  const tempFg = newState.foregroundColor;
  newState.foregroundColor = newState.backgroundColor;
  newState.backgroundColor = tempFg;
  
  // If no colors are set, use default terminal colors
  if (!newState.foregroundColor && !newState.backgroundColor) {
    newState.foregroundColor = { type: 'named', color: 'black' };
    newState.backgroundColor = { type: 'named', color: 'white' };
  } else if (!newState.foregroundColor) {
    newState.foregroundColor = { type: 'named', color: 'black' };
  } else if (!newState.backgroundColor) {
    newState.backgroundColor = { type: 'named', color: 'white' };
  }
  
  return newState;
}