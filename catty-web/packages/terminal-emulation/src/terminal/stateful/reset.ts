import { createDefaultSgrState } from "../SgrStyleManager";

import { createCellGrid } from "./screenGrid";
import type { TerminalState } from "./state";
import { initializeTabStops } from "./tabStops";

export function hardReset(state: TerminalState, cols: number, rows: number): void {
  // Hard reset (best-effort): return to primary buffer, clear screens, and reset modes/state.
  state.alternateScreenManager.switchToPrimary();

  const primary = state.alternateScreenManager.getPrimaryBuffer();
  primary.cells = createCellGrid(cols, rows);
  primary.cursorX = 0;
  primary.cursorY = 0;
  primary.savedCursor = null;
  primary.wrapPending = false;

  const alternate = state.alternateScreenManager.getAlternateBuffer();
  alternate.cells = createCellGrid(cols, rows);
  alternate.cursorX = 0;
  alternate.cursorY = 0;
  alternate.savedCursor = null;
  alternate.wrapPending = false;

  state.cursorX = 0;
  state.cursorY = 0;
  state.savedCursor = null;
  state.wrapPending = false;
  state.cursorStyle = 1;
  state.cursorVisible = true;
  state.applicationCursorKeys = false;

  state.originMode = false;
  state.autoWrapMode = true;

  state.scrollTop = 0;
  state.scrollBottom = rows - 1;

  state.currentSgrState = createDefaultSgrState();

  state.scrollback = [];
  state.currentCharacterProtection = "unprotected";

  state.windowProperties = {
    title: "",
    iconName: "",
  };

  state.characterSets = {
    G0: "B",
    G1: "B",
    G2: "B",
    G3: "B",
    current: "G0",
  };

  state.utf8Mode = true;
  state.tabStops = initializeTabStops(cols);

  // Clear title/icon name stacks
  state.titleStack = [];
  state.iconNameStack = [];
}

export function softReset(state: TerminalState, cols: number, rows: number): void {
  // DECSTR (soft reset): reset modes/state without clearing the screen.
  state.cursorX = 0;
  state.cursorY = 0;
  state.savedCursor = null;
  state.wrapPending = false;
  state.cursorStyle = 1;
  state.cursorVisible = true;
  state.applicationCursorKeys = false;

  state.originMode = false;
  state.autoWrapMode = true;

  state.scrollTop = 0;
  state.scrollBottom = rows - 1;

  state.currentCharacterProtection = "unprotected";

  state.currentSgrState = createDefaultSgrState();

  state.characterSets = {
    G0: "B",
    G1: "B",
    G2: "B",
    G3: "B",
    current: "G0",
  };

  state.utf8Mode = true;
  state.tabStops = initializeTabStops(cols);
}
