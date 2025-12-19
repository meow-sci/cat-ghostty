export interface TerminalKeyEvent {
  key: string;
  shiftKey: boolean;
  altKey: boolean;
  ctrlKey: boolean;
  metaKey: boolean;
}

export interface TerminalModifierKeys {
  shift: boolean;
  alt: boolean;
  ctrl: boolean;
}

export interface EncodeKeyOptions {
  applicationCursorKeys: boolean;
}

export type WheelDirection = "up" | "down";

export type MouseTrackingMode = "off" | "click" | "button" | "any";

export type MouseTrackingDecMode = 1000 | 1002 | 1003;
