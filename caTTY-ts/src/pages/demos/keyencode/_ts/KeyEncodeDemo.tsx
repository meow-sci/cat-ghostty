import { useLayoutEffect, type KeyboardEvent } from "react";
import { atom } from "nanostores";
import { useStore } from "@nanostores/react";

import type { GhosttyVtInstance } from "../../../../ts/ghostty-vt";
import { KeyCodeMap } from "../../../../ts/terminal/keyencode/KeyCodeMap";

// liar liar pants on fire
let encoder: StatefulEncoder = null as any;

const $output = atom<string>("");

export interface KeyEncodeDemoProps {
  wasm: GhosttyVtInstance;
}

export function KeyEncodeDemo(props: KeyEncodeDemoProps) {

  const { wasm } = props;

  useLayoutEffect(() => {
    if (!encoder) {
      encoder = new StatefulEncoder(wasm, getKittyFlags());
    }
  }, []);

  const output = useStore($output);

  return (
    <main>
      <section id="input-section">
        <input
          id="input"
          type="text"
          autoFocus
          value=""
          autoComplete="off"
          autoCorrect="off"
          autoCapitalize="off"
          spellCheck="false"
          onKeyDown={onKeyDown}
        />
        <section id="checkboxes">
          <label><input type="checkbox" id="flag_disambiguate" defaultChecked /> Disambiguate</label>
          <label><input type="checkbox" id="flag_report_events" defaultChecked /> Report Events</label>
          <label><input type="checkbox" id="flag_report_alternates" defaultChecked /> Report Alternates</label>
          <label><input type="checkbox" id="flag_report_all_as_escapes" defaultChecked /> Report All As Escapes</label>
          <label><input type="checkbox" id="flag_report_text" defaultChecked /> Report Text</label>
        </section>
      </section>
      <section id="output-section">
        <pre id="output">{output}</pre>
      </section>
    </main>
  );

}

//
// UI INTEGRATION
//

function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {

  // Allow modifier keys to be pressed without clearing input
  // Only prevent default for keys we want to capture
  if (event.key !== 'Tab' && event.key !== 'F5') {
    event.preventDefault();
  }

  displayEncoding(event);
}


function getKittyFlags(): number {
  let flags = 0;
  if ((document.getElementById('flag_disambiguate')! as HTMLInputElement).checked) flags |= 0x01;
  if ((document.getElementById('flag_report_events')! as HTMLInputElement).checked) flags |= 0x02;
  if ((document.getElementById('flag_report_alternates')! as HTMLInputElement).checked) flags |= 0x04;
  if ((document.getElementById('flag_report_all_as_escapes')! as HTMLInputElement).checked) flags |= 0x08;
  if ((document.getElementById('flag_report_text')! as HTMLInputElement).checked) flags |= 0x10;
  return flags;
}


function displayEncoding(event: KeyboardEvent<HTMLInputElement>) {
  const encoded = encoder.encodeKeyEvent(event);

  const actionName = "press";

  let output = `Action: ${actionName}\n`;
  output += `Key: ${event.key} (code: ${event.code})\n`;
  output += `Modifiers: `;
  const mods = [];

  if (event.shiftKey) mods.push('Shift');
  if (event.ctrlKey) mods.push('Ctrl');
  if (event.altKey) mods.push('Alt');
  if (event.metaKey) mods.push('Meta');

  output += mods.length ? mods.join('+') : 'none';
  output += '\n';

  // Show Kitty flags state
  const flags = [];
  if ((document.getElementById('flag_disambiguate')! as HTMLInputElement).checked) flags.push('Disambiguate');
  if ((document.getElementById('flag_report_events')! as HTMLInputElement).checked) flags.push('Report Events');
  if ((document.getElementById('flag_report_alternates')! as HTMLInputElement).checked) flags.push('Report Alternates');
  if ((document.getElementById('flag_report_all_as_escapes')! as HTMLInputElement).checked) flags.push('Report All As Escapes');
  if ((document.getElementById('flag_report_text')! as HTMLInputElement).checked) flags.push('Report Text');

  output += 'Kitty Flags:\n';
  if (flags.length) {
    flags.forEach(flag => output += `  - ${flag}\n`);
  } else {
    output += '  - none\n';
  }
  output += '\n';

  if (encoded) {
    output += `Encoded ${encoded.bytes.length} bytes\n`;
    output += `Hex: ${encoded.hex}\n`;
    output += `String: ${encoded.string}`;
  } else {
    output += 'No encoding for this key event';
  }

  $output.set(output);
}

//
// STATEFUL ENCODER GLUE
//

class StatefulEncoder {

  wasm: GhosttyVtInstance;
  kittyFlags: number;
  encoderPtr: number;

  constructor(wasm: GhosttyVtInstance, initialKittyFlags: number) {
    this.wasm = wasm;
    this.kittyFlags = initialKittyFlags;
    this.encoderPtr = this.initEncoder();
    this.updateEncoderFlags();
  }

  initEncoder(): number {

    // Create key encoder
    const encoderPtrPtr = this.wasm.exports.ghostty_wasm_alloc_opaque();
    const result = this.wasm.exports.ghostty_key_encoder_new(0, encoderPtrPtr);

    if (result !== 0) {
      throw new Error(`ghostty_key_encoder_new failed with result ${result}`);
    }

    const ptr = new DataView(this.getBuffer()).getUint32(encoderPtrPtr, true);

    // Set kitty flags based on checkboxes
    return ptr;

  }

  getBuffer() {
    return this.wasm.exports.memory.buffer;
  }

  updateEncoderFlags() {
    if (!this.encoderPtr) return;

    const flags = this.kittyFlags;
    console.log(`kitty flags=${flags}`)
    const flagsPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
    new DataView(this.getBuffer()).setUint8(flagsPtr, flags);
    this.wasm.exports.ghostty_key_encoder_setopt(
      this.encoderPtr,
      5, // GHOSTTY_KEY_ENCODER_OPT_KITTY_FLAGS
      flagsPtr
    );
  }

  encodeKeyEvent(event: KeyboardEvent<HTMLInputElement>) {

    try {
      // Create key event
      const eventPtrPtr = this.wasm.exports.ghostty_wasm_alloc_opaque();
      const result = this.wasm.exports.ghostty_key_event_new(0, eventPtrPtr);

      if (result !== 0) {
        throw new Error(`ghostty_key_event_new failed with result ${result}`);
      }

      const eventPtr = new DataView(this.getBuffer()).getUint32(eventPtrPtr, true);

      const action = 1; // 1 = press, 0 = release, 2 = repeat
      console.log(`wasmInstance.exports.ghostty_key_event_set_action(${eventPtr}, ${action});`)
      this.wasm.exports.ghostty_key_event_set_action(eventPtr, action);

      // Map key code from event.code (preferred, layout-independent)
      let keyCode = KeyCodeMap[event.code] || 0; // GHOSTTY_KEY_UNIDENTIFIED = 0
      console.log(`wasmInstance.exports.ghostty_key_event_set_key(${eventPtr}, ${keyCode});`)
      this.wasm.exports.ghostty_key_event_set_key(eventPtr, keyCode);

      // Map modifiers with left/right side information
      let mods = 0;
      if (event.shiftKey) {
        mods |= 0x01; // GHOSTTY_MODS_SHIFT
        if (event.code === 'ShiftRight') mods |= 0x40; // GHOSTTY_MODS_SHIFT_SIDE
      }
      if (event.ctrlKey) {
        mods |= 0x02; // GHOSTTY_MODS_CTRL
        if (event.code === 'ControlRight') mods |= 0x80; // GHOSTTY_MODS_CTRL_SIDE
      }
      if (event.altKey) {
        mods |= 0x04; // GHOSTTY_MODS_ALT
        if (event.code === 'AltRight') mods |= 0x100; // GHOSTTY_MODS_ALT_SIDE
      }
      if (event.metaKey) {
        mods |= 0x08; // GHOSTTY_MODS_SUPER
        if (event.code === 'MetaRight') mods |= 0x200; // GHOSTTY_MODS_SUPER_SIDE
      }
      console.log(`wasmInstance.exports.ghostty_key_event_set_mods(${eventPtr}, ${mods});`);
      this.wasm.exports.ghostty_key_event_set_mods(eventPtr, mods);

      // Set UTF-8 text from the key event (the actual character produced)
      if (event.key.length === 1) {
        console.log(`event.key=${event.key}`);
        const utf8Bytes = new TextEncoder().encode(event.key);
        const utf8Ptr = this.wasm.exports.ghostty_wasm_alloc_u8_array(utf8Bytes.length);
        new Uint8Array(this.getBuffer()).set(utf8Bytes, utf8Ptr);
        console.log(`wasmInstance.exports.ghostty_key_event_set_utf8(${eventPtr}, ${utf8Ptr}, ${utf8Bytes.length});`);
        this.wasm.exports.ghostty_key_event_set_utf8(eventPtr, utf8Ptr, utf8Bytes.length);
      }

      // Set unshifted codepoint
      const unshiftedCodepoint = getUnshiftedCodepoint(event);
      if (typeof unshiftedCodepoint === "number" && unshiftedCodepoint !== 0) {
        console.log(`wasmInstance.exports.ghostty_key_event_set_unshifted_codepoint(${eventPtr}, ${unshiftedCodepoint});`);
        this.wasm.exports.ghostty_key_event_set_unshifted_codepoint(eventPtr, unshiftedCodepoint);
      }

      // Encode the key event
      const requiredPtr = this.wasm.exports.ghostty_wasm_alloc_usize();
      this.wasm.exports.ghostty_key_encoder_encode(
        this.encoderPtr, eventPtr, 0, 0, requiredPtr
      );

      const required = new DataView(this.getBuffer()).getUint32(requiredPtr, true);

      const bufPtr = this.wasm.exports.ghostty_wasm_alloc_u8_array(required);
      const writtenPtr = this.wasm.exports.ghostty_wasm_alloc_usize();
      const encodeResult = this.wasm.exports.ghostty_key_encoder_encode(
        this.encoderPtr, eventPtr, bufPtr, required, writtenPtr
      );

      if (encodeResult !== 0) {
        return null; // No encoding for this key
      }

      const written = new DataView(this.getBuffer()).getUint32(writtenPtr, true);
      const encoded = new Uint8Array(this.getBuffer()).slice(bufPtr, bufPtr + written);

      return {
        bytes: Array.from(encoded),
        hex: formatHex(encoded),
        string: formatString(encoded)
      };
    } catch (e) {
      console.error('Encoding error:', e);
      return null;
    }
  }

}

//
// PURE FUNCTIONS FOR ENCODER
//

function getUnshiftedCodepoint(event: KeyboardEvent<HTMLInputElement>) {
  // Derive unshifted codepoint from the physical key code
  const code = event.code;

  // Letter keys (KeyA-KeyZ) -> lowercase letters
  if (code.startsWith('Key')) {
    const letter = code.substring(3).toLowerCase();
    return letter.codePointAt(0);
  }

  // Digit keys (Digit0-Digit9) -> the digit itself
  if (code.startsWith('Digit')) {
    const digit = code.substring(5);
    return digit.codePointAt(0);
  }

  // Space
  if (code === 'Space') {
    return ' '.codePointAt(0);
  }

  // Symbol keys -> unshifted character
  const unshiftedSymbols: Record<string, string | undefined> = {
    'Minus': '-', 'Equal': '=', 'BracketLeft': '[', 'BracketRight': ']',
    'Backslash': '\\', 'Semicolon': ';', 'Quote': "'",
    'Backquote': '`', 'Comma': ',', 'Period': '.', 'Slash': '/'
  };

  if (unshiftedSymbols[code]) {
    return unshiftedSymbols[code].codePointAt(0);
  }

  // Fallback: use the produced character's codepoint
  if (event.key.length > 0) {
    return event.key.codePointAt(0) || 0;
  }

  return 0;
}


function formatHex(bytes: Uint8Array<ArrayBuffer>) {
  return Array.from(bytes)
    .map(b => b.toString(16).padStart(2, '0'))
    .join(' ');
}

function formatString(bytes: Uint8Array<ArrayBuffer>) {
  let result = '';
  for (let i = 0; i < bytes.length; i++) {
    if (bytes[i] === 0x1b) {
      result += '\\x1b';
    } else {
      result += String.fromCharCode(bytes[i]);
    }
  }
  return result;
}
