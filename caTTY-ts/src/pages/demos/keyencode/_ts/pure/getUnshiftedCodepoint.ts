import { type KeyboardEvent } from "react";

export function getUnshiftedCodepoint(event: KeyboardEvent<HTMLInputElement>) {
  // Derive unshifted codepoint from the physical key code
  const code = event.code;

  // Letter keys (KeyA-KeyZ) -> lowercase letters
  if (code.startsWith('Key')) {
    const letter = code.substring(3).toLowerCase();
    return letter.codePointAt(0);
  }

  // Digit keys (Digit0-Digit9) -> the digit itself
  if (code.startsWith('Digit')) {
    const digit = code.substring(5);
    return digit.codePointAt(0);
  }

  // Space
  if (code === 'Space') {
    return ' '.codePointAt(0);
  }

  // Symbol keys -> unshifted character
  const unshiftedSymbols: Record<string, string | undefined> = {
    'Minus': '-', 'Equal': '=', 'BracketLeft': '[', 'BracketRight': ']',
    'Backslash': '\\', 'Semicolon': ';', 'Quote': "'",
    'Backquote': '`', 'Comma': ',', 'Period': '.', 'Slash': '/'
  };

  if (unshiftedSymbols[code]) {
    return unshiftedSymbols[code].codePointAt(0);
  }

  // Fallback: use the produced character's codepoint
  if (event.key.length > 0) {
    return event.key.codePointAt(0) || 0;
  }

  return 0;
}
