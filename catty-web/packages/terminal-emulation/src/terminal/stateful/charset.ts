import type { CharacterSetKey, CharacterSetState } from "./state";

export interface CharsetTerminalState {
  characterSets: CharacterSetState;
  utf8Mode: boolean;
}

const DEC_SPECIAL_GRAPHICS_MAP: Readonly<Record<number, string>> = Object.freeze({
  0x60: "\u25C6", // ` -> ◆ (diamond)
  0x61: "\u2592", // a -> ▒ (checkerboard)
  0x62: "\u2409", // b -> ␉ (HT symbol)
  0x63: "\u240C", // c -> ␌ (FF symbol)
  0x64: "\u240D", // d -> ␍ (CR symbol)
  0x65: "\u240A", // e -> ␊ (LF symbol)
  0x66: "\u00B0", // f -> ° (degree)
  0x67: "\u00B1", // g -> ± (plus-minus)
  0x68: "\u2424", // h -> ␤ (NL symbol)
  0x69: "\u240B", // i -> ␋ (VT symbol)
  0x6a: "\u2518", // j -> ┘ (lower right corner)
  0x6b: "\u2510", // k -> ┐ (upper right corner)
  0x6c: "\u250c", // l -> ┌ (upper left corner)
  0x6d: "\u2514", // m -> └ (lower left corner)
  0x6e: "\u253c", // n -> ┼ (crossing lines)
  0x6f: "\u23BA", // o -> ⎺ (scan line 1)
  0x70: "\u23BB", // p -> ⎻ (scan line 3)
  0x71: "\u2500", // q -> ─ (horizontal line)
  0x72: "\u23BC", // r -> ⎼ (scan line 7)
  0x73: "\u23BD", // s -> ⎽ (scan line 9)
  0x74: "\u251C", // t -> ├ (left tee)
  0x75: "\u2524", // u -> ┤ (right tee)
  0x76: "\u2534", // v -> ┴ (bottom tee)
  0x77: "\u252C", // w -> ┬ (top tee)
  0x78: "\u2502", // x -> │ (vertical line)
  0x79: "\u2264", // y -> ≤ (less than or equal)
  0x7a: "\u2265", // z -> ≥ (greater than or equal)
  0x7b: "\u03C0", // { -> π (pi)
  0x7c: "\u2260", // | -> ≠ (not equal)
  0x7d: "\u00A3", // } -> £ (pound sterling)
  0x7e: "\u00B7", // ~ -> · (middle dot)
});

export function designateCharacterSet(
  state: CharsetTerminalState,
  slot: CharacterSetKey,
  charset: string,
): void {
  state.characterSets[slot] = charset;
}

export function getCharacterSet(state: CharsetTerminalState, slot: CharacterSetKey): string {
  return state.characterSets[slot];
}

export function getCurrentCharacterSet(state: CharsetTerminalState): string {
  return state.characterSets[state.characterSets.current];
}

export function switchCharacterSet(state: CharsetTerminalState, slot: CharacterSetKey): void {
  state.characterSets.current = slot;
}

export function setUtf8Mode(state: CharsetTerminalState, enabled: boolean): void {
  state.utf8Mode = enabled;
}

export function isUtf8Mode(state: CharsetTerminalState): boolean {
  return state.utf8Mode;
}

/**
 * Generate character set query response.
 * Format: CSI ? 26 ; charset ST
 */
export function generateCharacterSetQueryResponse(state: CharsetTerminalState): string {
  const charset = state.utf8Mode ? "utf-8" : getCurrentCharacterSet(state);
  return `\x1b[?26;${charset}\x1b\\`;
}

/**
 * Translate a character according to the current character set.
 * Handles special character sets like DEC Special Graphics.
 */
export function translateCharacter(state: CharsetTerminalState, ch: string): string {
  if (state.utf8Mode) {
    return ch;
  }

  const currentCharset = getCurrentCharacterSet(state);

  if (currentCharset === "0") {
    const code = ch.charCodeAt(0);
    return DEC_SPECIAL_GRAPHICS_MAP[code] || ch;
  }

  return ch;
}
