import type { EscMessage } from "../../TerminalEmulationTypes";

import type { TerminalActions } from "../actions";

export function handleEsc(actions: TerminalActions, msg: EscMessage): void {
  switch (msg._type) {
    case "esc.saveCursor":
      actions.saveCursorPosition();
      return;

    case "esc.restoreCursor":
      actions.restoreCursorPosition();
      return;

    case "esc.designateCharacterSet":
      // Designate character set to the specified G slot
      actions.designateCharacterSet(msg.slot, msg.charset);
      return;

    case "esc.reverseIndex": {
      // RI (ESC M): move cursor up; if at top margin, scroll region down.
      actions.setWrapPending(false);
      const cursorY = actions.getCursorY();
      const scrollTop = actions.getScrollTop();

      if (cursorY <= scrollTop) {
        actions.setCursorY(scrollTop);
        actions.scrollDownInRegion(1);
        return;
      }

      actions.setCursorY(Math.max(scrollTop, cursorY - 1));
      return;
    }

    case "esc.index":
      actions.lineFeed();
      return;

    case "esc.nextLine":
      actions.carriageReturn();
      actions.lineFeed();
      return;

    case "esc.horizontalTabSet":
      actions.setTabStopAtCursor();
      return;

    case "esc.resetToInitialState":
      actions.resetToInitialState();
      return;
  }
}
