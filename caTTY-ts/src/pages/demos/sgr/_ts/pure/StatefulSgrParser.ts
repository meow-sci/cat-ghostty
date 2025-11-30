import type { GhosttyVtInstance } from "../../../../../ts/ghostty-vt";
import { SgrAttributeTags } from "../../../../../ts/terminal/sgr/SgrAttributeTags";
import { SgrUnderlineStyles } from "../../../../../ts/terminal/sgr/SgrUnderlineStyles";

interface PreParsedSequence {
  params: number[];
  separators: string[];
}

export class StatefulSgrParser {

  private wasm: GhosttyVtInstance;
  // private parserPtr: number;

  constructor(wasm: GhosttyVtInstance) {
    this.wasm = wasm;
    // parserPtr = this.initParser();
  }

  initParser(): number {

    // Create key encoder
    const ptrPtr = this.wasm.exports.ghostty_wasm_alloc_opaque();
    const result = this.wasm.exports.ghostty_sgr_new(0, ptrPtr);

    if (result !== 0) {
      throw new Error(`ghostty_sgr_new failed with result ${result}`);
    }

    const ptr = new DataView(this.getBuffer()).getUint32(ptrPtr, true);
    return ptr;
  }

  parse(sequence: string) {
    const preParsed = this.preParse(sequence);
    return this.parseWithGhostty(preParsed);
  }

  private preParse(sequence: string): PreParsedSequence {

    // Parse the raw sequence into parameters and separators.
    // this is all native text work, no ghostty involvement
    const params: number[] = [];
    const separators: string[] = [];

    let currentNum = '';

    for (let i = 0; i < sequence.length; i++) {
      const char = sequence[i];

      if (char === ':' || char === ';') {
        if (currentNum) {
          const num = parseInt(currentNum, 10);
          if (isNaN(num) || num < 0 || num > 65535) {
            throw new Error(`Invalid parameter: ${currentNum}`);
          }
          params.push(num);
          separators.push(char);
          currentNum = '';
        }
      } else if (char >= '0' && char <= '9') {
        currentNum += char;
      } else if (char !== ' ' && char !== '\t' && char !== '\n') {
        throw new Error(`Invalid character in sequence: '${char}'`);
      }
    }

    // Don't forget the last number
    if (currentNum) {
      const num = parseInt(currentNum, 10);
      if (isNaN(num) || num < 0 || num > 65535) {
        throw new Error(`Invalid parameter: ${currentNum}`);
      }
      params.push(num);
    }

    if (params.length === 0) {
      throw new Error("Error: No parameters found in sequence")
    }

    return { params, separators };
  }

  private parseWithGhostty(preParsed: PreParsedSequence) {

    const parserPtr = this.initParser();

    const { params, separators } = preParsed;

    // Allocate and set parameters
    const paramsPtr = this.wasm.exports.ghostty_wasm_alloc_u16_array(params.length);
    const paramsView = new Uint16Array(this.getBuffer(), paramsPtr, params.length);
    params.forEach((p, i) => paramsView[i] = p);

    // Allocate and set separators (or use null if empty)
    let sepsPtr = 0;
    if (separators.length > 0) {
      sepsPtr = this.wasm.exports.ghostty_wasm_alloc_u8_array(separators.length);
      const sepsView = new Uint8Array(this.getBuffer(), sepsPtr, separators.length);
      separators.forEach((s, i) => sepsView[i] = s.charCodeAt(0));
    }

    // Set parameters in parser
    const setResult = this.wasm.exports.ghostty_sgr_set_params(
      parserPtr,
      paramsPtr,
      sepsPtr,
      params.length
    );

    if (setResult !== 0) {
      throw new Error(`ghostty_sgr_set_params failed with result ${setResult}`);
    }

    // DONE, I THINK... NEED TO PULL OUT THE DATA.
    // FOR NOW, THIS IS JUST FOR HUMAN DISPLAY


    // Build output
    let output = 'Parsing SGR sequence:\n';
    output += 'ESC[';
    params.forEach((p, i) => {
      if (i > 0) output += separators[i - 1];
      output += p;
    });
    output += 'm\n\n';

    // Iterate through attributes
    const attrPtr = this.wasm.exports.ghostty_wasm_alloc_sgr_attribute();
    let count = 0;

    while (this.wasm.exports.ghostty_sgr_next(parserPtr, attrPtr)) {
      count++;

      // Use the new ghostty_sgr_attribute_tag getter function
      const tag: number = this.wasm.exports.ghostty_sgr_attribute_tag(attrPtr);

      // Use ghostty_sgr_attribute_value to get a pointer to the value union
      const valuePtr = this.wasm.exports.ghostty_sgr_attribute_value(attrPtr);

      output += `Attribute ${count}: `;

      switch (tag) {
        case SgrAttributeTags.UNDERLINE: {
          const view = new DataView(this.getBuffer(), valuePtr, 4);
          const style = view.getUint32(0, true);
          output += `Underline style = ${SgrUnderlineStyles[style] || `unknown(${style})`}\n`;
          break;
        }

        case SgrAttributeTags.DIRECT_COLOR_FG: {
          // Use ghostty_color_rgb_get to extract RGB components
          const rPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = this.wasm.exports.ghostty_wasm_alloc_u8();

          this.wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const r = new Uint8Array(this.getBuffer(), rPtr, 1)[0];
          const g = new Uint8Array(this.getBuffer(), gPtr, 1)[0];
          const b = new Uint8Array(this.getBuffer(), bPtr, 1)[0];

          output += `Foreground RGB = (${r}, ${g}, ${b})\n`;

          this.wasm.exports.ghostty_wasm_free_u8(rPtr);
          this.wasm.exports.ghostty_wasm_free_u8(gPtr);
          this.wasm.exports.ghostty_wasm_free_u8(bPtr);
          break;
        }

        case SgrAttributeTags.DIRECT_COLOR_BG: {
          // Use ghostty_color_rgb_get to extract RGB components
          const rPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = this.wasm.exports.ghostty_wasm_alloc_u8();

          this.wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const r = new Uint8Array(this.getBuffer(), rPtr, 1)[0];
          const g = new Uint8Array(this.getBuffer(), gPtr, 1)[0];
          const b = new Uint8Array(this.getBuffer(), bPtr, 1)[0];

          output += `Background RGB = (${r}, ${g}, ${b})\n`;

          this.wasm.exports.ghostty_wasm_free_u8(rPtr);
          this.wasm.exports.ghostty_wasm_free_u8(gPtr);
          this.wasm.exports.ghostty_wasm_free_u8(bPtr);
          break;
        }

        case SgrAttributeTags.UNDERLINE_COLOR: {
          // Use ghostty_color_rgb_get to extract RGB components
          const rPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const gPtr = this.wasm.exports.ghostty_wasm_alloc_u8();
          const bPtr = this.wasm.exports.ghostty_wasm_alloc_u8();

          this.wasm.exports.ghostty_color_rgb_get(valuePtr, rPtr, gPtr, bPtr);

          const r = new Uint8Array(this.getBuffer(), rPtr, 1)[0];
          const g = new Uint8Array(this.getBuffer(), gPtr, 1)[0];
          const b = new Uint8Array(this.getBuffer(), bPtr, 1)[0];

          output += `Underline color RGB = (${r}, ${g}, ${b})\n`;

          this.wasm.exports.ghostty_wasm_free_u8(rPtr);
          this.wasm.exports.ghostty_wasm_free_u8(gPtr);
          this.wasm.exports.ghostty_wasm_free_u8(bPtr);
          break;
        }

        case SgrAttributeTags.FG_8:
        case SgrAttributeTags.BG_8:
        case SgrAttributeTags.FG_256:
        case SgrAttributeTags.BG_256:
        case SgrAttributeTags.UNDERLINE_COLOR_256: {
          const view = new DataView(this.getBuffer(), valuePtr, 1);
          const color = view.getUint8(0);
          const colorType = tag === SgrAttributeTags.FG_8 ? 'Foreground 8-color' :
            tag === SgrAttributeTags.BG_8 ? 'Background 8-color' :
              tag === SgrAttributeTags.FG_256 ? 'Foreground 256-color' :
                tag === SgrAttributeTags.BG_256 ? 'Background 256-color' :
                  'Underline 256-color';
          output += `${colorType} = ${color}\n`;
          break;
        }

        case SgrAttributeTags.BOLD:
          output += 'Bold\n';
          break;

        case SgrAttributeTags.ITALIC:
          output += 'Italic\n';
          break;

        case SgrAttributeTags.UNSET:
          output += 'Reset all attributes\n';
          break;

        case SgrAttributeTags.UNKNOWN:
          output += 'Unknown attribute\n';
          break;

        default:
          output += `Other attribute (tag=${getTagName(tag)})\n`;
          break;
      }
    }

    output += `\nTotal attributes parsed: ${count}`;

    this.wasm.exports.ghostty_wasm_free_sgr_attribute(attrPtr);
    this.wasm.exports.ghostty_sgr_free(parserPtr);

    return output;

  }

  getBuffer() {
    return this.wasm.exports.memory.buffer;
  }
}

function getTagName(tag: number) {
  for (const [name, value] of Object.entries(SgrAttributeTags)) {
    if (value === tag) return name;
  }
  return `UNKNOWN(${tag})`;
}
