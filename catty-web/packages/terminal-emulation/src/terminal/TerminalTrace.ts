import type { EscMessage, CsiMessage, DcsMessage, OscMessage, XtermOscMessage, SgrSequence } from "./TerminalEmulationTypes";

export interface TraceCursorPosition {
  /** 0-based cursor column */
  cursorX: number;
  /** 0-based cursor row */
  cursorY: number;
  implemented: boolean;
}

export interface TraceNormalByteChunk extends TraceCursorPosition {
  _type: "trace.normalByte";
  byte: number;
}

export type TraceControlName = "BEL" | "BS" | "TAB" | "LF" | "FF" | "CR";

export interface TraceControlChunk extends TraceCursorPosition {
  _type: "trace.control";
  name: TraceControlName;
  byte: number;
}

export interface TraceEscChunk extends TraceCursorPosition {
  _type: "trace.esc";
  msg: EscMessage;
}

export interface TraceCsiChunk extends TraceCursorPosition {
  _type: "trace.csi";
  msg: CsiMessage;
}

export interface TraceOscChunk extends TraceCursorPosition {
  _type: "trace.osc";
  msg: OscMessage | XtermOscMessage;
}

export interface TraceDcsChunk extends TraceCursorPosition {
  _type: "trace.dcs";
  msg: DcsMessage;
}

export interface TraceSgrChunk extends TraceCursorPosition {
  _type: "trace.sgr";
  msg: SgrSequence;
}

export type TerminalTraceChunk =
  | TraceNormalByteChunk
  | TraceControlChunk
  | TraceEscChunk
  | TraceCsiChunk
  | TraceOscChunk
  | TraceDcsChunk
  | TraceSgrChunk;

function byteToHex(byte: number): string {
  return `0x${byte.toString(16).padStart(2, "0")}`;
}

function makeControlCharsVisible(text: string): string {
  return text
    .replaceAll("\x1b", "<ESC>")
    .replaceAll("\r", "<CR>")
    .replaceAll("\n", "<LF>\n")
    .replaceAll("\t", "<TAB>")
    .replaceAll("\x07", "<BEL>");
}

function describePrintableByte(byte: number): string {
  if (byte === 0x20) {
    return "<space>";
  }
  if (byte >= 0x21 && byte <= 0x7e) {
    return JSON.stringify(String.fromCharCode(byte));
  }
  if (byte === 0x7f) {
    return "<DEL>";
  }
  return "";
}

export function formatTerminalTraceLine(chunk: TerminalTraceChunk, index: number): string {
  const row1 = chunk.cursorY + 1;
  const col1 = chunk.cursorX + 1;
  const prefix = `${index.toString().padStart(6, " ")}  ${row1.toString().padStart(2, " ")},${col1.toString().padEnd(2, " ")}  | ${chunk.implemented ? 'Y' : 'N'} `;

  switch (chunk._type) {
    case "trace.normalByte": {
      const desc = describePrintableByte(chunk.byte);
      return `${prefix}BYTE ${byteToHex(chunk.byte)}${desc ? ` ${desc}` : ""}`;
    }

    case "trace.control":
      return `${prefix}CTRL ${chunk.name} ${byteToHex(chunk.byte)}`;

    case "trace.esc":
      return `${prefix}ESC  ${chunk.msg._type} ${makeControlCharsVisible(chunk.msg.raw)}`;

    case "trace.csi":
      return `${prefix}CSI  ${chunk.msg._type} ${makeControlCharsVisible(chunk.msg.raw)}`;

    case "trace.osc": {
      const terminator = 'terminator' in chunk.msg ? chunk.msg.terminator : 'BEL';
      const msgType = chunk.msg._type !== 'osc' ? ` ${chunk.msg._type}` : '';
      return `${prefix}OSC ${terminator}${msgType} ${makeControlCharsVisible(chunk.msg.raw)}`;
    }

    case "trace.dcs": {
      return `${prefix}DCS ${chunk.msg.terminator} ${makeControlCharsVisible(chunk.msg.raw)}`;
    }

    case "trace.sgr":
      return `${prefix}SGR  (${chunk.msg.messages.length}) ${makeControlCharsVisible(chunk.msg.raw)}`;
  }
}
