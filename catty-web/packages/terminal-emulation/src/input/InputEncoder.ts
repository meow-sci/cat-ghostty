import type { TerminalKeyEvent, TerminalModifierKeys, EncodeKeyOptions, MouseTrackingMode, MouseTrackingDecMode } from "./InputTypes";

export function resolveMouseTrackingMode(enabledModes: Set<MouseTrackingDecMode>): MouseTrackingMode {
  if (enabledModes.has(1003)) {
    return "any";
  } else if (enabledModes.has(1002)) {
    return "button";
  } else if (enabledModes.has(1000)) {
    return "click";
  } else {
    return "off";
  }
}

export function encodeCtrlKey(e: TerminalKeyEvent): string | null {
  if (!e.ctrlKey || e.metaKey) {
    return null;
  }

  // Ctrl+C and Ctrl+D are the main ones that matter for shells.
  const k = e.key;
  if (k === "c" || k === "C") {
    return String.fromCharCode(0x03);
  }
  if (k === "d" || k === "D") {
    return String.fromCharCode(0x04);
  }

  // Generic Ctrl+<letter> mapping.
  if (k.length === 1) {
    const ch = k.toUpperCase();
    const code = ch.charCodeAt(0);
    if (code >= 0x40 && code <= 0x5f) {
      return String.fromCharCode(code - 0x40);
    }
    if (code >= 0x41 && code <= 0x5a) {
      return String.fromCharCode(code - 0x40);
    }
  }

  return null;
}

export function xtermModifierParam(e: TerminalKeyEvent): number {
  // xterm modifier encoding: 1 + (shift?1) + (alt?2) + (ctrl?4)
  // https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
  let mod = 1;
  if (e.shiftKey) mod += 1;
  if (e.altKey) mod += 2;
  if (e.ctrlKey) mod += 4;
  return mod;
}

export function encodeKeyDownToTerminalBytes(e: TerminalKeyEvent, opts: EncodeKeyOptions): string | null {

  if (e.metaKey) {
    // Let browser/OS shortcuts work.
    return null;
  }

  const ctrl = encodeCtrlKey(e);

  if (ctrl) {
    return ctrl;
  }

  switch (e.key) {
    case "Enter":
      return "\r";
    case "Backspace":
      // Most shells in raw mode expect DEL (0x7f) for backspace.
      return "\x7f";
    case "Tab":
      return "\t";
    case "Escape":
      return "\x1b";
    case "ArrowUp":
      return opts.applicationCursorKeys ? "\x1bOA" : "\x1b[A";
    case "ArrowDown":
      return opts.applicationCursorKeys ? "\x1bOB" : "\x1b[B";
    case "ArrowRight":
      return opts.applicationCursorKeys ? "\x1bOC" : "\x1b[C";
    case "ArrowLeft":
      return opts.applicationCursorKeys ? "\x1bOD" : "\x1b[D";
    case "Home":
      return "\x1b[H";
    case "End":
      return "\x1b[F";
    case "Delete":
      return "\x1b[3~";
    case "Insert":
      return "\x1b[2~";
    case "PageUp":
      return "\x1b[5~";
    case "PageDown":
      return "\x1b[6~";

    // Function keys (xterm-compatible)
    // Common mappings:
    // - F1-F4: SS3 P/Q/R/S (or CSI 1;M P/Q/R/S with modifiers)
    // - F5-F12: CSI 15/17/18/19/20/21/23/24 ~ (with optional ;M)
    case "F1":
    case "F2":
    case "F3":
    case "F4": {
      const mod = xtermModifierParam(e);
      const final = e.key === "F1" ? "P" : e.key === "F2" ? "Q" : e.key === "F3" ? "R" : "S";
      if (mod === 1) {
        return "\x1bO" + final;
      }
      return `\x1b[1;${mod}${final}`;
    }

    case "F5":
    case "F6":
    case "F7":
    case "F8":
    case "F9":
    case "F10":
    case "F11":
    case "F12": {
      const code =
        e.key === "F5" ? 15 :
        e.key === "F6" ? 17 :
        e.key === "F7" ? 18 :
        e.key === "F8" ? 19 :
        e.key === "F9" ? 20 :
        e.key === "F10" ? 21 :
        e.key === "F11" ? 23 :
        24;

      const mod = xtermModifierParam(e);
      if (mod === 1) {
        return `\x1b[${code}~`;
      }
      return `\x1b[${code};${mod}~`;
    }
  }

  // Ignore non-text keys (Shift, Alt, etc)
  if (e.key.length !== 1) {
    return null;
  }

  // Alt as ESC prefix (best-effort). On macOS option often produces a different
  // character already, so only do this when the produced key is a plain ASCII.
  if (e.altKey) {
    const code = e.key.charCodeAt(0);
    if (code >= 0x20 && code <= 0x7e) {
      return "\x1b" + e.key;
    }
  }

  return e.key;
}

export function encodeMouseMotion(button: 0 | 1 | 2 | 3, x1: number, y1: number, mods: TerminalModifierKeys, sgrEncoding: boolean): string {
  const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);
  // Motion reports add 32.
  const b = button + modBits + 32;

  if (sgrEncoding) {
    return `\x1b[<${b};${x1};${y1}M`;
  }

  const cx = 32 + Math.max(1, Math.min(223, x1));
  const cy = 32 + Math.max(1, Math.min(223, y1));
  const cb = 32 + Math.max(0, Math.min(255, b));
  return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
}

export function encodeMouseWheel(direction: "up" | "down", x1: number, y1: number, mods: TerminalModifierKeys, sgrEncoding: boolean): string {
  const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);

  // xterm wheel: buttons 64/65 (press only)
  const wheelButton = direction === "up" ? 64 : 65;
  const b = wheelButton + modBits;

  if (sgrEncoding) {
    return `\x1b[<${b};${x1};${y1}M`;
  }

  const cx = 32 + Math.max(1, Math.min(223, x1));
  const cy = 32 + Math.max(1, Math.min(223, y1));
  const cb = 32 + Math.max(0, Math.min(255, b));
  return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
}

export function encodeMousePress(button: 0 | 1 | 2, x1: number, y1: number, mods: TerminalModifierKeys, sgrEncoding: boolean): string {
  const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);
  const b = button + modBits;

  if (sgrEncoding) {
    return `\x1b[<${b};${x1};${y1}M`;
  }

  // X10 fallback encoding (limited to 223 for coordinates).
  const cx = 32 + Math.max(1, Math.min(223, x1));
  const cy = 32 + Math.max(1, Math.min(223, y1));
  const cb = 32 + Math.max(0, Math.min(255, b));
  return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
}

export function encodeMouseRelease(button: 0 | 1 | 2, x1: number, y1: number, mods: TerminalModifierKeys, sgrEncoding: boolean): string {
  const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);

  if (sgrEncoding) {
    // In SGR mode, xterm uses final 'm' to indicate release.
    const b = button + modBits;
    return `\x1b[<${b};${x1};${y1}m`;
  }

  // In classic mode, use button 3 (release) + modifiers.
  const cb = 32 + (3 + modBits);
  const cx = 32 + Math.max(1, Math.min(223, x1));
  const cy = 32 + Math.max(1, Math.min(223, y1));
  return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
}
