import { getLogger } from "@catty/log";

import { GhosttyVtInstance } from "../ghostty-vt";
import { HandleBell, HandleBackspace, HandleTab, HandleLineFeed, HandleFormFeed, HandleCarriageReturn, HandleNormalByte, HandleCsi, HandleOsc, HandleSgr } from "./ParserHandlers";

export interface ParserHandlers {
  handleBell: HandleBell;
  handleBackspace: HandleBackspace;
  handleTab: HandleTab;
  handleLineFeed: HandleLineFeed;
  handleFormFeed: HandleFormFeed;
  handleCarriageReturn: HandleCarriageReturn;
  handleNormalByte: HandleNormalByte;

  /** Non-SGR CSI handler hook */
  handleCsi: HandleCsi;

  /** Opaque stubs (buffered, not parsed) */
  handleOsc: HandleOsc;
  handleSgr: HandleSgr;
}

export interface ParserOptions {
  wasm: GhosttyVtInstance;

  log: ReturnType<typeof getLogger>;

  /** Whether to emit normal bytes during escape sequences (undefined behavior), default=false */
  emitNormalBytesDuringEscapeSequence?: boolean;

  /** Whether to process C0 controls during escape sequences (common terminal behavior), default=true */
  processC0ControlsDuringEscapeSequence?: boolean;

  handlers: ParserHandlers;
}