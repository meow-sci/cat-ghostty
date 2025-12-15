import { CsiMessage, CsiSetCursorStyle, CsiDecModeSet, CsiDecModeReset, CsiCursorUp, CsiCursorDown, CsiCursorForward, CsiCursorBackward, CsiCursorNextLine, CsiCursorPrevLine, CsiCursorHorizontalAbsolute, CsiCursorPosition, CsiEraseInDisplayMode, CsiEraseInDisplay, CsiEraseInLineMode, CsiEraseInLine, CsiScrollUp, CsiScrollDown, CsiSetScrollRegion, CsiSaveCursorPosition, CsiRestoreCursorPosition, CsiUnknown } from "./TerminalEmulationTypes";

export function parseCsi(bytes: number[], raw: string): CsiMessage {
  // bytes: ESC [ ... final
  const finalByte = bytes[bytes.length - 1];
  const final = String.fromCharCode(finalByte);

  let paramsText = "";
  let intermediate = "";

  for (let i = 2; i < bytes.length - 1; i++) {
    const b = bytes[i];
    if (b >= 0x30 && b <= 0x3f) {
      paramsText += String.fromCharCode(b);
      continue;
    }
    if (b >= 0x20 && b <= 0x2f) {
      intermediate += String.fromCharCode(b);
      continue;
    }
  }

  const parsed = parseCsiParams(paramsText);
  const isPrivate = parsed.private;
  const params = parsed.params;

  // DECSCUSR: CSI Ps SP q
  if (final === "q" && intermediate === " ") {
    const style = getParam(params, 0, 0);
    const msg: CsiSetCursorStyle = { _type: "csi.setCursorStyle", raw, style };
    return msg;
  }

  // DEC private modes: CSI ? Pm h / l
  if (isPrivate && (final === "h" || final === "l")) {
    const modes = params.slice();
    if (final === "h") {
      const msg: CsiDecModeSet = { _type: "csi.decModeSet", raw, modes };
      return msg;
    }
    const msg: CsiDecModeReset = { _type: "csi.decModeReset", raw, modes };
    return msg;
  }

  if (final === "A") {
    const msg: CsiCursorUp = { _type: "csi.cursorUp", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "B") {
    const msg: CsiCursorDown = { _type: "csi.cursorDown", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "C") {
    const msg: CsiCursorForward = { _type: "csi.cursorForward", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "D") {
    const msg: CsiCursorBackward = { _type: "csi.cursorBackward", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "E") {
    const msg: CsiCursorNextLine = { _type: "csi.cursorNextLine", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "F") {
    const msg: CsiCursorPrevLine = { _type: "csi.cursorPrevLine", raw, count: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "G") {
    const msg: CsiCursorHorizontalAbsolute = { _type: "csi.cursorHorizontalAbsolute", raw, column: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "H" || final === "f") {
    const msg: CsiCursorPosition = {
      _type: "csi.cursorPosition",
      raw,
      row: getParam(params, 0, 1),
      column: getParam(params, 1, 1),
    };
    return msg;
  }

  if (final === "J") {
    const modeValue = getParam(params, 0, 0);
    const mode: CsiEraseInDisplayMode = (modeValue === 0 || modeValue === 1 || modeValue === 2 || modeValue === 3) ? modeValue : 0;
    const msg: CsiEraseInDisplay = { _type: "csi.eraseInDisplay", raw, mode };
    return msg;
  }

  if (final === "K") {
    const modeValue = getParam(params, 0, 0);
    const mode: CsiEraseInLineMode = (modeValue === 0 || modeValue === 1 || modeValue === 2) ? modeValue : 0;
    const msg: CsiEraseInLine = { _type: "csi.eraseInLine", raw, mode };
    return msg;
  }

  if (final === "S") {
    const msg: CsiScrollUp = { _type: "csi.scrollUp", raw, lines: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "T" && params.length <= 1 && !isPrivate) {
    const msg: CsiScrollDown = { _type: "csi.scrollDown", raw, lines: getParam(params, 0, 1) };
    return msg;
  }

  if (final === "r") {
    const top = params.length >= 1 ? params[0] : undefined;
    const bottom = params.length >= 2 ? params[1] : undefined;
    const msg: CsiSetScrollRegion = { _type: "csi.setScrollRegion", raw, top, bottom };
    return msg;
  }

  if (final === "s") {
    const msg: CsiSaveCursorPosition = { _type: "csi.saveCursorPosition", raw };
    return msg;
  }

  if (final === "u") {
    const msg: CsiRestoreCursorPosition = { _type: "csi.restoreCursorPosition", raw };
    return msg;
  }

  const unknown: CsiUnknown = {
    _type: "csi.unknown",
    raw,
    private: isPrivate,
    params,
    intermediate,
    final,
  };

  return unknown;
}


function parseCsiParams(paramText: string): { private: boolean; params: number[] } {
  let isPrivate = false;
  let text = paramText;

  if (text.startsWith("?")) {
    isPrivate = true;
    text = text.slice(1);
  }

  const params: number[] = [];

  if (text.length === 0) {
    return { private: isPrivate, params };
  }

  const parts = text.split(";");
  for (let i = 0; i < parts.length; i++) {
    const part = parts[i];
    if (part.length === 0) {
      continue;
    }
    const n = Number.parseInt(part, 10);
    if (!Number.isFinite(n)) {
      continue;
    }
    params.push(n);
  }

  return { private: isPrivate, params };
}

function getParam(params: number[], index: number, fallback: number): number {
  const v = params[index];
  if (v === undefined) {
    return fallback;
  }
  return v;
}