import { CsiMessage, SgrMessage } from "./TerminalEmulationTypes";

export type HandleBell = () => void;
export type HandleBackspace = () => void;
export type HandleTab = () => void;
export type HandleLineFeed = () => void;
export type HandleFormFeed = () => void;
export type HandleCarriageReturn = () => void;
export type HandleNormalByte = (byte: number) => void;
export type HandleCsi = (msg: CsiMessage) => void;
export type HandleOsc = (raw: string) => void;
export type HandleSgr = (messages: SgrMessage[]) => void;
