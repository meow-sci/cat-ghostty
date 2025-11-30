import type { KeyboardEvent } from "react";
import type { KeyEvent } from "../pure/KeyEvent";

export function toKeyEvent(event: KeyboardEvent<HTMLInputElement>): KeyEvent {
  return {
    _type: "KeyEvent",
    code: event.code,
    shiftKey: event.shiftKey,
    altKey: event.altKey,
    metaKey: event.metaKey,
    ctrlKey: event.ctrlKey,
    key: event.key,
  };
}
