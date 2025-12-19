import type { TerminalTraceChunk } from "../TerminalTrace";
import type { SgrState } from "../SgrStyleManager";

export type DecModeEvent = {
  action: "set" | "reset";
  raw: string;
  modes: number[];
};

export interface ScreenCell {
  ch: string;
  sgrState?: SgrState;
  isProtected?: boolean;
}

export interface WindowProperties {
  title: string;
  iconName: string;
}

export interface ScreenBuffer {
  cells: ScreenCell[][];
  cursorX: number;
  cursorY: number;
  savedCursor: [number, number] | null;
  wrapPending: boolean;
}

export interface CursorState {
  x: number;
  y: number;
  visible: boolean;
  style: number;
  applicationMode: boolean;
  wrapPending: boolean;
}

export interface ScreenSnapshot {
  cols: number;
  rows: number;
  cursorX: number;
  cursorY: number;
  cursorStyle: number;
  cursorVisible: boolean;
  cells: ReadonlyArray<ReadonlyArray<ScreenCell>>;
  windowProperties: WindowProperties;
  cursorState: CursorState;
  currentSgrState: SgrState;
}

export interface StatefulTerminalOptions {
  cols: number;
  rows: number;
  onUpdate?: (snapshot: ScreenSnapshot) => void;
  onChunk?: (chunk: TerminalTraceChunk) => void;
  onResponse?: (response: string) => void;
  scrollbackLimit?: number;
}
