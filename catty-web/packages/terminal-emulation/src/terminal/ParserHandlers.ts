import type { 
  CsiMessage, 
  DcsMessage,
  EscMessage, 
  OscMessage, 
  SgrSequence,
  XtermOscMessage
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
// ECMA-48 / VT / Xterm Control Strings
// =============================================================================

export type HandleDcs = (msg: DcsMessage) => void;

// =============================================================================
// Xterm Extension Handlers
// =============================================================================

export type HandleXtermOsc = (msg: XtermOscMessage) => void;
