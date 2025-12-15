import { getLogger } from "@catty/log";
import { GhosttyVtInstance } from "../ghostty-vt";
import { Attributes, SgrAttributeTags, UnderlineStyle } from "./SgrTypes";

const DEFAULT_SEPARATORS = [";", ":"];

/**
   * Parse SGR parameters using libghostty-vt WASM and return attributes.
   * This delegates all SGR parsing to libghostty-vt without validating separator format.
   * 
   * @param rawSgrSequence SGR sequence string (e.g. "38;5;11" or "4:3;38;5;11")
   */
export function parseSgrWithWasm(log: ReturnType<typeof getLogger>, wasm: GhosttyVtInstance, rawSgrSequence: string): Attributes | null {

  // rawSgrSequence must be turned into two arrays
  // params: number[] = the numbers in the sequence
  // separators: string[] = the separators between each number. will be either ';' or ':' characters for each entry.  will be one less than params.length

  const params: number[] = [];
  const separators: string[] = [];
  let currentNum: string = ''; // string based buffer to build up a number by character

  for (let i = 0; i < rawSgrSequence.length; i++) {
    const char = rawSgrSequence[i];

    if (char === ':' || char === ';') {
      if (currentNum) {
        const num = parseInt(currentNum, 10);
        if (isNaN(num) || num < 0 || num > 65535) {
          log.error(`Invalid parameter: ${currentNum}`);
        }
        params.push(num);
        separators.push(char);
        currentNum = '';
      }
    } else if (char >= '0' && char <= '9') {
      currentNum += char;
    } else if (char !== ' ' && char !== '\t' && char !== '\n') {
      log.error(`Invalid character in sequence: '${char}'`);
    }
  }

  // handle final number
  if (currentNum) {
    const num = parseInt(currentNum, 10);
    if (isNaN(num) || num < 0 || num > 65535) {
      throw new Error(`Invalid parameter: ${currentNum}`);
    }
    params.push(num);
  }

  const attributes: Attributes = {
    fg: { type: 'default' },
    bg: { type: 'default' },
    bold: false,
    italic: false,
    underline: UnderlineStyle.None,
    underlineColor: { type: 'default' },
    inverse: false,
    strikethrough: false
  };

  try {
    // Create SGR parser
    const ptrPtr = wasm.exports.ghostty_wasm_alloc_opaque();
    const result = wasm.exports.ghostty_sgr_new(0, ptrPtr);

    if (result !== 0) {
      throw new Error(`ghostty_sgr_new failed with result ${result}`);
    }

    const parserPtr = new DataView(wasm.exports.memory.buffer).getUint32(ptrPtr, true);

    // Allocate and set parameters
    const paramsPtr = wasm.exports.ghostty_wasm_alloc_u16_array(params.length);
    const paramsView = new Uint16Array(wasm.exports.memory.buffer, paramsPtr, params.length);
    params.forEach((p, i) => {
      paramsView[i] = p;
      console.log(`paramsView[${i}] = ${p}`);
    });

    // Allocate and set separators
    let sepsPtr = 0;
    if (separators.length > 0) {
      sepsPtr = wasm.exports.ghostty_wasm_alloc_u8_array(separators.length);
      const sepsView = new Uint8Array(wasm.exports.memory.buffer, sepsPtr, separators.length);
      separators.forEach((s, i) => {
        sepsView[i] = s.charCodeAt(0);
        console.log(`sepsView[${i}] = ${s.charCodeAt(0)}`);
      });
    }

    // Set parameters in parser
    const setResult = wasm.exports.ghostty_sgr_set_params(
      parserPtr,
      paramsPtr,
      sepsPtr,
      params.length
    );

    if (setResult !== 0) {
      throw new Error(`ghostty_sgr_set_params failed with result ${setResult}`);
    }

    // Iterate through attributes
    const attrPtr = wasm.exports.ghostty_wasm_alloc_sgr_attribute();

    while (wasm.exports.ghostty_sgr_next(parserPtr, attrPtr)) {
      let tag: number = wasm.exports.ghostty_sgr_attribute_tag(attrPtr);

      const valuePtr = wasm.exports.ghostty_sgr_attribute_value(attrPtr);

      console.log(`tag [${tag}]`)

      switch (tag) {
        case SgrAttributeTags.BOLD:
          attributes.bold = true;
          break;

        case SgrAttributeTags.RESET_BOLD:
          attributes.bold = false;
          break;

        case SgrAttributeTags.ITALIC:
          attributes.italic = true;
          break;

        case SgrAttributeTags.RESET_ITALIC:
          attributes.italic = false;
          break;

        case SgrAttributeTags.INVERSE:
          attributes.inverse = true;
          break;

        case SgrAttributeTags.RESET_INVERSE:
          attributes.inverse = false;
          break;

        case SgrAttributeTags.STRIKETHROUGH:
          attributes.strikethrough = true;
          break;

        case SgrAttributeTags.RESET_STRIKETHROUGH:
          attributes.strikethrough = false;
          break;

        case SgrAttributeTags.UNDERLINE: {
          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 4);
          const style = view.getUint32(0, true);
          if (style >= 0 && style <= 5) {
            attributes.underline = style as UnderlineStyle;
          }
          break;
        }

        case SgrAttributeTags.UNDERLINE_COLOR_256: {

          const rPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = wasm.exports.ghostty_wasm_alloc_u8();

          wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const wasmBuffer = wasm.exports.memory.buffer;

          const r = new Uint8Array(wasmBuffer, rPtr, 1)[0];
          const g = new Uint8Array(wasmBuffer, gPtr, 1)[0];
          const b = new Uint8Array(wasmBuffer, bPtr, 1)[0];

          const output = `Foreground RGB = (${r}, ${g}, ${b})`;
          console.log(`output [${output}]`);

          wasm.exports.ghostty_wasm_free_u8(rPtr);
          wasm.exports.ghostty_wasm_free_u8(gPtr);
          wasm.exports.ghostty_wasm_free_u8(bPtr);


          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 4);
          const style = view.getUint32(0, true);
          if (style >= 0 && style <= 5) {
            attributes.underline = style as UnderlineStyle;
          }
          break;
        }

        case SgrAttributeTags.RESET_UNDERLINE:
          attributes.underline = UnderlineStyle.None;
          break;

        case SgrAttributeTags.FG_8:
        case SgrAttributeTags.BRIGHT_FG_8: {
          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 1);
          const color = view.getUint8(0);
          // WORKAROUND: WASM library appears to swap FG/BG tags for 8-color mode
          // This is likely a bug in the WASM build. Swap them back here.
          attributes.fg = { type: 'indexed', index: color };
          break;
        }

        case SgrAttributeTags.BG_8:
        case SgrAttributeTags.BRIGHT_BG_8: {
          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 1);
          const color = view.getUint8(0);
          // WORKAROUND: WASM library appears to swap FG/BG tags for 8-color mode
          // This is likely a bug in the WASM build. Swap them back here.
          attributes.bg = { type: 'indexed', index: color };
          break;
        }

        case SgrAttributeTags.FG_256: {
          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 1);
          const color = view.getUint8(0);
          attributes.fg = { type: 'indexed', index: color };
          break;
        }

        case SgrAttributeTags.BG_256: {
          const view = new DataView(wasm.exports.memory.buffer, valuePtr, 1);
          const color = view.getUint8(0);
          attributes.bg = { type: 'indexed', index: color };
          break;
        }

        case SgrAttributeTags.DIRECT_COLOR_FG: {
          // Extract RGB components
          const rPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = wasm.exports.ghostty_wasm_alloc_u8();

          wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const r = new Uint8Array(wasm.exports.memory.buffer, rPtr, 1)[0];
          const g = new Uint8Array(wasm.exports.memory.buffer, gPtr, 1)[0];
          const b = new Uint8Array(wasm.exports.memory.buffer, bPtr, 1)[0];

          attributes.fg = { type: 'rgb', r, g, b };

          wasm.exports.ghostty_wasm_free_u8(rPtr);
          wasm.exports.ghostty_wasm_free_u8(gPtr);
          wasm.exports.ghostty_wasm_free_u8(bPtr);
          break;
        }

        case SgrAttributeTags.DIRECT_COLOR_BG: {
          // Extract RGB components
          const rPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = wasm.exports.ghostty_wasm_alloc_u8();

          wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const r = new Uint8Array(wasm.exports.memory.buffer, rPtr, 1)[0];
          const g = new Uint8Array(wasm.exports.memory.buffer, gPtr, 1)[0];
          const b = new Uint8Array(wasm.exports.memory.buffer, bPtr, 1)[0];

          attributes.bg = { type: 'rgb', r, g, b };

          wasm.exports.ghostty_wasm_free_u8(rPtr);
          wasm.exports.ghostty_wasm_free_u8(gPtr);
          wasm.exports.ghostty_wasm_free_u8(bPtr);
          break;
        }

        case SgrAttributeTags.RESET_FG:
          attributes.fg = { type: 'default' };
          break;

        case SgrAttributeTags.RESET_BG:
          attributes.bg = { type: 'default' };
          break;

        case SgrAttributeTags.UNSET:
          // Reset all attributes
          attributes.fg = { type: 'default' };
          attributes.bg = { type: 'default' };
          attributes.bold = false;
          attributes.italic = false;
          attributes.underline = UnderlineStyle.None;
          attributes.inverse = false;
          attributes.strikethrough = false;
          break;
      }
    }

    // Clean up
    wasm.exports.ghostty_wasm_free_sgr_attribute(attrPtr);
    wasm.exports.ghostty_wasm_free_u16_array(paramsPtr, params.length);
    if (sepsPtr !== 0) {
      wasm.exports.ghostty_wasm_free_u8_array(sepsPtr, separators.length);
    }
    wasm.exports.ghostty_sgr_free(parserPtr);
    wasm.exports.ghostty_wasm_free_opaque(ptrPtr);

  } catch (e) {
    // If parsing fails, return null to indicate failure
    log.warn(`SGR parsing failed: ${(e as Error).message}`);
    console.warn(e);
    return null;
  }

  return attributes;
}
