import type { DcsMessage } from "../../TerminalEmulationTypes";
import type { TerminalActions } from "../actions";

export function handleDcs(actions: TerminalActions, msg: DcsMessage): void {
  // DECRQSS: DCS $ q <request> ST
  // The parser puts intermediates (like $) in the parameters list (or attached to the last parameter).
  // We check if the last parameter ends with '$' and the command is 'q'.
  const lastParam = msg.parameters.length > 0 ? msg.parameters[msg.parameters.length - 1] : "";
  
  if (msg.command === "q" && lastParam.endsWith("$")) {
    handleDecrqss(actions, msg);
    return;
  }
}

function handleDecrqss(actions: TerminalActions, msg: DcsMessage): void {
  // Extract payload.
  // The raw string contains the full sequence.
  // We need to find the 'q' that terminates the header.
  // The header is: DCS [params] [intermediates] Final
  // DCS is ESC P (0x1b 0x50) or 0x90.
  
  let payloadStart = -1;
  
  // Skip DCS initiator
  let i = 0;
  if (msg.raw.charCodeAt(0) === 0x1b && msg.raw.charCodeAt(1) === 0x50) {
    i = 2;
  } else if (msg.raw.charCodeAt(0) === 0x90) {
    i = 1;
  }
  
  // Scan for the Final Byte (0x40-0x7E) which is msg.command ('q')
  for (; i < msg.raw.length; i++) {
    const code = msg.raw.charCodeAt(i);
    if (code >= 0x40 && code <= 0x7e) {
      // Found the command byte. Payload starts after this.
      payloadStart = i + 1;
      break;
    }
  }
  
  if (payloadStart === -1) {
    return; // Should not happen if parser is correct
  }
  
  // Payload ends before the terminator.
  // Terminator is ST (ESC \ or 0x9C).
  let payloadEnd = msg.raw.length;
  if (msg.raw.endsWith("\x1b\\")) {
    payloadEnd -= 2;
  } else if (msg.raw.endsWith("\x9c")) {
    payloadEnd -= 1;
  } else if (msg.raw.endsWith("\x07")) { 
      // Xterm allows BEL as terminator too sometimes? Standard says ST.
  }
  
  const payload = msg.raw.substring(payloadStart, payloadEnd);
  
  // DECRQSS response: DCS <status> $ r <response> ST
  // status: 0 = valid, 1 = invalid
  
  let response = "";
  let valid = false;
  
  if (payload === '"q') { // DECSCA
      // Not implemented yet
      valid = false; 
  } else if (payload === '"p') { // DECSCL
      // DECSCL: conformance level.
      // Let's pretend to be VT400?
      // response = "64;1\"p";
      // valid = true;
      valid = false;
  } else if (payload === 'm') { // SGR
      // Request SGR state.
      const sgr = actions.getCurrentSgrState();
      const parts: string[] = ["0"]; 
      
      if (sgr.bold) parts.push("1");
      if (sgr.faint) parts.push("2");
      if (sgr.italic) parts.push("3");
      if (sgr.underline) parts.push("4");
      if (sgr.blink) parts.push("5"); // Slow blink
      if (sgr.inverse) parts.push("7");
      if (sgr.hidden) parts.push("8");
      if (sgr.strikethrough) parts.push("9");
      
      response = parts.join(";") + "m";
      valid = true;
  } else if (payload === 'r') { // DECSTBM
      // Top/Bottom margins.
      const top = actions.getScrollTop() + 1;
      const bottom = actions.getScrollBottom() + 1; 
      
      response = `${top};${bottom}r`;
      valid = true;
  } else {
      valid = false;
  }
  
  // Xterm: 1 = valid, 0 = invalid.
  
  if (valid) {
      actions.emitResponse(`\x1bP1$r${response}\x1b\\`);
  } else {
      // Invalid request
      actions.emitResponse(`\x1bP0$r${payload}\x1b\\`);
  }
}
