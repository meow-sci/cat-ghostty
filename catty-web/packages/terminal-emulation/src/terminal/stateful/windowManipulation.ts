import type { TerminalState } from "./state";

export interface DebugLogger {
  isLevelEnabled(level: "debug"): boolean;
  debug(message: string): void;
}

export interface WindowManipulationActions {
  setWindowTitle(title: string): void;
  setIconName(iconName: string): void;
}

/**
 * Handle window manipulation sequences (CSI Ps t)
 * Implements title/icon name stack operations for vi compatibility
 */
export function handleWindowManipulation(
  state: TerminalState,
  log: DebugLogger,
  actions: WindowManipulationActions,
  operation: number,
  params: number[],
): void {
  switch (operation) {
    case 22:
      // Push title/icon name to stack
      if (params.length >= 1) {
        const subOperation = params[0];
        if (subOperation === 1) {
          // CSI 22;1t - Push icon name to stack
          state.iconNameStack.push(state.windowProperties.iconName);
          if (log.isLevelEnabled("debug")) {
            log.debug(`Pushed icon name to stack: "${state.windowProperties.iconName}"`);
          }
        } else if (subOperation === 2) {
          // CSI 22;2t - Push window title to stack
          state.titleStack.push(state.windowProperties.title);
          if (log.isLevelEnabled("debug")) {
            log.debug(`Pushed window title to stack: "${state.windowProperties.title}"`);
          }
        }
      }
      return;

    case 23:
      // Pop title/icon name from stack
      if (params.length >= 1) {
        const subOperation = params[0];
        if (subOperation === 1) {
          // CSI 23;1t - Pop icon name from stack
          const poppedIconName = state.iconNameStack.pop();
          if (poppedIconName !== undefined) {
            actions.setIconName(poppedIconName);
            if (log.isLevelEnabled("debug")) {
              log.debug(`Popped icon name from stack: "${poppedIconName}"`);
            }
          } else {
            if (log.isLevelEnabled("debug")) {
              log.debug("Attempted to pop icon name from empty stack");
            }
          }
        } else if (subOperation === 2) {
          // CSI 23;2t - Pop window title from stack
          const poppedTitle = state.titleStack.pop();
          if (poppedTitle !== undefined) {
            actions.setWindowTitle(poppedTitle);
            if (log.isLevelEnabled("debug")) {
              log.debug(`Popped window title from stack: "${poppedTitle}"`);
            }
          } else {
            if (log.isLevelEnabled("debug")) {
              log.debug("Attempted to pop window title from empty stack");
            }
          }
        }
      }
      return;

    default:
      // Other window manipulation commands - gracefully ignore
      if (log.isLevelEnabled("debug")) {
        log.debug(
          `Window manipulation operation ${operation} with params ${JSON.stringify(params)} - gracefully ignored`,
        );
      }
      return;
  }
}
