import type { AlternateScreenManager } from "./alternateScreen";
import type { TerminalState } from "./state";

export type ClampCursor = () => void;

/**
 * DECSET 47: Switch to alternate screen buffer
 */
export function switchToAlternateScreen(state: TerminalState, manager: AlternateScreenManager): void {
  if (!manager.isAlternateActive()) {
    // Save current primary buffer state
    const primaryBuffer = manager.getPrimaryBuffer();
    primaryBuffer.cursorX = state.cursorX;
    primaryBuffer.cursorY = state.cursorY;
    primaryBuffer.wrapPending = state.wrapPending;

    // Switch to alternate buffer
    manager.switchToAlternate();
    const alternateBuffer = manager.getCurrentBuffer();

    // Load alternate buffer state
    state.cursorX = alternateBuffer.cursorX;
    state.cursorY = alternateBuffer.cursorY;
    state.wrapPending = alternateBuffer.wrapPending;
  }
}

/**
 * DECSET 1047: Save cursor and switch to alternate screen buffer
 */
export function switchToAlternateScreenWithCursorSave(state: TerminalState, manager: AlternateScreenManager): void {
  // Save cursor position
  state.savedCursor = [state.cursorX, state.cursorY];

  // Switch to alternate screen
  switchToAlternateScreen(state, manager);
}

/**
 * DECSET 1049: Save cursor, switch to alternate screen, and clear it
 */
export function switchToAlternateScreenWithCursorSaveAndClear(state: TerminalState, manager: AlternateScreenManager): void {
  // Save cursor position
  state.savedCursor = [state.cursorX, state.cursorY];

  // Switch to alternate screen
  switchToAlternateScreen(state, manager);

  // Clear the alternate screen buffer
  manager.clearAlternateBuffer();
  state.cursorX = 0;
  state.cursorY = 0;
  state.wrapPending = false;
}

/**
 * DECRST 47: Switch back to normal screen buffer
 */
export function switchToPrimaryScreen(state: TerminalState, manager: AlternateScreenManager): void {
  if (manager.isAlternateActive()) {
    // Save current alternate buffer state
    const alternateBuffer = manager.getAlternateBuffer();
    alternateBuffer.cursorX = state.cursorX;
    alternateBuffer.cursorY = state.cursorY;
    alternateBuffer.wrapPending = state.wrapPending;

    // Switch to primary buffer
    manager.switchToPrimary();
    const primaryBuffer = manager.getCurrentBuffer();

    // Load primary buffer state
    state.cursorX = primaryBuffer.cursorX;
    state.cursorY = primaryBuffer.cursorY;
    state.wrapPending = primaryBuffer.wrapPending;
  }
}

/**
 * DECRST 1047/1049: Switch to normal screen and restore cursor
 */
export function switchToPrimaryScreenWithCursorRestore(
  state: TerminalState,
  manager: AlternateScreenManager,
  clampCursor: ClampCursor,
): void {
  // Switch to primary screen
  switchToPrimaryScreen(state, manager);

  // Restore cursor position
  if (state.savedCursor) {
    state.cursorX = state.savedCursor[0];
    state.cursorY = state.savedCursor[1];
    clampCursor();
  }
}
