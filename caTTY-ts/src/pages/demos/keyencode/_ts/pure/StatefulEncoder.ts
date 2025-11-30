import { KeyCodeMap } from "../../../../../ts/terminal/keyencode/KeyCodeMap";
import { formatHex } from "./formatHex";
import { formatString } from "./formatString";
import { getUnshiftedCodepoint } from "./getUnshiftedCodepoint";
import type { GhosttyVtInstance } from "../../../../../ts/ghostty-vt";
import type { KeyEvent } from "./KeyEvent";
import type { KeyEncoderResult } from "./KeyEncoderResult";

export class StatefulEncoder {

  wasm: GhosttyVtInstance;
  _kittyFlags: number;
  encoderPtr: number;

  constructor(wasm: GhosttyVtInstance, initialKittyFlags: number) {
    this.wasm = wasm;
    this._kittyFlags = initialKittyFlags;
    this.encoderPtr = this.initEncoder();
    this.updateEncoderFlags();
  }

  set kittyFlags(o: number) {
    this._kittyFlags = o;
    this.updateEncoderFlags();
  }

  get kittyFlags(): number {
    return this._kittyFlags;
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

  encodeKeyEvent(event: KeyEvent): KeyEncoderResult | null {

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
