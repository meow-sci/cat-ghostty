import { getLogger } from "@catty/log";

import type { HandleBell, HandleBackspace, HandleTab, HandleLineFeed, HandleFormFeed, HandleCarriageReturn, HandleNormalByte, HandleCsi, HandleEsc, HandleOsc, HandleSgr, HandleXtermOsc } from "./ParserHandlers";

export interface ParserHandlers {
  handleBell: HandleBell;
  handleBackspace: HandleBackspace;
  handleTab: HandleTab;
  handleLineFeed: HandleLineFeed;
  handleFormFeed: HandleFormFeed;
  handleCarriageReturn: HandleCarriageReturn;
  handleNormalByte: HandleNormalByte;

  /** Non-CSI ESC handler hook (e.g. ESC 7/8). */
  handleEsc: HandleEsc;

  /** Non-SGR CSI handler hook */
  handleCsi: HandleCsi;

  /** Opaque OSC sequence (buffered, not parsed), includes raw */
  handleOsc: HandleOsc;

  /** Parsed SGR messages (stateless) wrapped with raw sequence */
  handleSgr: HandleSgr;

  /** Xterm OSC extension handler */
  handleXtermOsc: HandleXtermOsc;
}

export interface ParserOptions {

  log: ReturnType<typeof getLogger>;

  /** Whether to emit normal bytes during escape sequences (undefined behavior), default=false */
  emitNormalBytesDuringEscapeSequence?: boolean;

  /** Whether to process C0 controls during escape sequences (common terminal behavior), default=true */
  processC0ControlsDuringEscapeSequence?: boolean;

  handlers: ParserHandlers;
}