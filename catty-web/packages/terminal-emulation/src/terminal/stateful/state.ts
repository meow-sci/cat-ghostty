import { createDefaultSgrState, type SgrState } from "../SgrStyleManager";

import type { AlternateScreenManager } from "./alternateScreen";
import type { ScreenCell, WindowProperties } from "./screenTypes";
import { initializeTabStops } from "./tabStops";

export type CursorXY = [number, number];

export type CharacterSetKey = "G0" | "G1" | "G2" | "G3";

export interface CharacterSetState {
  G0: string;
  G1: string;
  G2: string;
  G3: string;
  current: CharacterSetKey;
}

export interface TerminalState {
  cursorX: number;
  cursorY: number;

  savedCursor: CursorXY | null;
  cursorStyle: number;
  cursorVisible: boolean;
  wrapPending: boolean;
  applicationCursorKeys: boolean;

  scrollbackLimit: number;
  scrollback: ScreenCell[][];

  originMode: boolean;
  autoWrapMode: boolean;

  scrollTop: number;
  scrollBottom: number;

  tabStops: boolean[];

  windowProperties: WindowProperties;
  titleStack: string[];
  iconNameStack: string[];

  characterSets: CharacterSetState;
  utf8Mode: boolean;

  currentSgrState: SgrState;
  currentCharacterProtection: "unprotected" | "protected";

  alternateScreenManager: AlternateScreenManager;

  updateBatchDepth: number;
  updateDirty: boolean;
}

export interface CreateInitialTerminalStateOptions {
  cols: number;
  rows: number;
  scrollbackLimit: number;
  alternateScreenManager: AlternateScreenManager;
}

export function createInitialTerminalState(options: CreateInitialTerminalStateOptions): TerminalState {
  return {
    cursorX: 0,
    cursorY: 0,

    savedCursor: null,
    cursorStyle: 1,
    cursorVisible: true,
    wrapPending: false,
    applicationCursorKeys: false,

    scrollbackLimit: options.scrollbackLimit,
    scrollback: [],

    originMode: false,
    autoWrapMode: true,

    scrollTop: 0,
    scrollBottom: options.rows - 1,

    tabStops: initializeTabStops(options.cols),

    windowProperties: {
      title: "",
      iconName: "",
    },
    titleStack: [],
    iconNameStack: [],

    characterSets: {
      G0: "B",
      G1: "B",
      G2: "B",
      G3: "B",
      current: "G0",
    },
    utf8Mode: true,

    currentSgrState: createDefaultSgrState(),
    currentCharacterProtection: "unprotected",

    alternateScreenManager: options.alternateScreenManager,

    updateBatchDepth: 0,
    updateDirty: false,
  };
}
