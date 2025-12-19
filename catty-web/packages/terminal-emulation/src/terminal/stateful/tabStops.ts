export interface TabStopState {
  cursorX: number;
  wrapPending: boolean;
  tabStops: boolean[];
}

export function initializeTabStops(cols: number): boolean[] {
  return Array.from({ length: cols }, (_, i) => i % 8 === 0);
}

export function setTabStopAtCursor(state: Pick<TabStopState, "cursorX" | "tabStops">, cols: number): void {
  if (state.cursorX < 0 || state.cursorX >= cols) {
    return;
  }
  state.tabStops[state.cursorX] = true;
}

export function clearTabStopAtCursor(state: Pick<TabStopState, "cursorX" | "tabStops">, cols: number): void {
  if (state.cursorX < 0 || state.cursorX >= cols) {
    return;
  }
  state.tabStops[state.cursorX] = false;
}

export function clearAllTabStops(state: Pick<TabStopState, "tabStops">, cols: number): void {
  state.tabStops = Array.from({ length: cols }, () => false);
}

export function cursorForwardTab(state: TabStopState, cols: number, count: number): void {
  const n = Math.max(1, count);

  if (state.cursorX < 0) {
    state.cursorX = 0;
  }

  for (let i = 0; i < n; i += 1) {
    let nextStop = -1;
    for (let x = state.cursorX + 1; x < cols; x += 1) {
      if (state.tabStops[x]) {
        nextStop = x;
        break;
      }
    }
    state.cursorX = nextStop === -1 ? (cols - 1) : nextStop;
  }

  state.wrapPending = false;
}

export function cursorBackwardTab(state: TabStopState, count: number): void {
  const n = Math.max(1, count);

  if (state.cursorX < 0) {
    state.cursorX = 0;
  }

  for (let i = 0; i < n; i += 1) {
    let prevStop = -1;
    for (let x = state.cursorX - 1; x >= 0; x -= 1) {
      if (state.tabStops[x]) {
        prevStop = x;
        break;
      }
    }
    state.cursorX = prevStop === -1 ? 0 : prevStop;
  }

  state.wrapPending = false;
}
