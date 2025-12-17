import type { 
  CsiMessage, 
  EscMessage, 
  OscMessage, 
  SgrSequence,
  XtermOscMessage,
  XtermCsiMessage,
  XtermDcsMessage
} from "./TerminalEmulationTypes";

export type HandleBell = () => void;
export type HandleBackspace = () => void;
export type HandleTab = () => void;
export type HandleLineFeed = () => void;
export type HandleFormFeed = () => void;
export type HandleCarriageReturn = () => void;
export type HandleNormalByte = (byte: number) => void;
export type HandleCsi = (msg: CsiMessage) => void;
export type HandleEsc = (msg: EscMessage) => void;
export type HandleOsc = (msg: OscMessage) => void;
export type HandleSgr = (msg: SgrSequence) => void;

// =============================================================================
// Xterm Extension Handlers
// =============================================================================

export type HandleXtermOsc = (msg: XtermOscMessage) => void;
export type HandleXtermCsi = (msg: XtermCsiMessage) => void;
export type HandleXtermDcs = (msg: XtermDcsMessage) => void;
