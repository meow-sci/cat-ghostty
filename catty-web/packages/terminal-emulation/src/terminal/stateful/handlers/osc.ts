import type { XtermOscMessage } from "../../TerminalEmulationTypes";

import type { TerminalActions } from "../actions";

import { generateBackgroundColorResponse, generateForegroundColorResponse } from "../responses";

export function handleXtermOsc(actions: TerminalActions, msg: XtermOscMessage): void {
  switch (msg._type) {
    case "osc.setTitleAndIcon":
      // OSC 0: Set both window title and icon name
      actions.setTitleAndIcon(msg.title);
      return;

    case "osc.setIconName":
      // OSC 1: Set icon name only
      actions.setIconName(msg.iconName);
      return;

    case "osc.setWindowTitle":
      // OSC 2: Set window title only
      actions.setWindowTitle(msg.title);
      return;

    case "osc.queryWindowTitle": {
      // OSC 21: Query window title
      // Respond with OSC ] L <title> ST (ESC \\)
      const title = actions.getWindowTitle();
      actions.emitResponse(`\x1b]L${title}\x1b\\`);
      return;
    }

    case "osc.queryForegroundColor":
      // OSC 10;?: Query default foreground color
      // Respond with current theme foreground color
      actions.emitResponse(generateForegroundColorResponse(actions.getCurrentSgrState()));
      return;

    case "osc.queryBackgroundColor":
      // OSC 11;?: Query default background color
      // Respond with current theme background color
      actions.emitResponse(generateBackgroundColorResponse(actions.getCurrentSgrState()));
      return;
  }
}
