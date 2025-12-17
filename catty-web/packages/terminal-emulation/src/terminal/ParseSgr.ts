import type {
  SgrReset,
  SgrBold,
  SgrFaint,
  SgrItalic,
  SgrUnderline,
  SgrUnderlineStyle,
  SgrSlowBlink,
  SgrRapidBlink,
  SgrInverse,
  SgrHidden,
  SgrStrikethrough,
  SgrFont,
  SgrFraktur,
  SgrDoubleUnderline,
  SgrNormalIntensity,
  SgrNotItalic,
  SgrNotUnderlined,
  SgrNotBlinking,
  SgrProportionalSpacing,
  SgrNotInverse,
  SgrNotHidden,
  SgrNotStrikethrough,
  SgrForegroundColor,
  SgrDefaultForeground,
  SgrBackgroundColor,
  SgrDefaultBackground,
  SgrDisableProportionalSpacing,
  SgrFramed,
  SgrEncircled,
  SgrOverlined,
  SgrNotFramed,
  SgrNotOverlined,
  SgrUnderlineColor,
  SgrDefaultUnderlineColor,
  SgrIdeogram,
  SgrSuperscript,
  SgrSubscript,
  SgrNotSuperscriptSubscript,
  SgrUnknown,
  SgrColorType,
  SgrNamedColor,
  SgrMessage,
} from "./TerminalEmulationTypes";

/**
 * Parse context for SGR sequences. Tracks current position and separator info.
 */
interface SgrParseContext {
  params: number[];
  separators: string[];
  index: number;
}

/**
 * Named color lookup for standard 8 colors (SGR 30-37, 40-47).
 */
const STANDARD_COLORS: SgrNamedColor[] = [
  "black",
  "red",
  "green",
  "yellow",
  "blue",
  "magenta",
  "cyan",
  "white",
];

/**
 * Named color lookup for bright colors (SGR 90-97, 100-107).
 */
const BRIGHT_COLORS: SgrNamedColor[] = [
  "brightBlack",
  "brightRed",
  "brightGreen",
  "brightYellow",
  "brightBlue",
  "brightMagenta",
  "brightCyan",
  "brightWhite",
];

/**
 * Parse an extended color specification (38;5;n, 38;2;r;g;b, or colon variants).
 * Returns the color and the number of additional params consumed.
 */
function parseExtendedColor(ctx: SgrParseContext): { color: SgrColorType; consumed: number } | null {
  const { params, separators, index } = ctx;

  if (index >= params.length) {
    return null;
  }

  // Look at the next parameter to determine color type
  const colorType = params[index];

  if (colorType === 5) {
    // 256-color mode: 38;5;n or 38:5:n
    if (index + 1 < params.length) {
      const colorIndex = params[index + 1];
      if (colorIndex >= 0 && colorIndex <= 255) {
        return { color: { type: "indexed", index: colorIndex }, consumed: 2 };
      }
    }
    return null;
  }

  if (colorType === 2) {
    // True color mode: 38;2;r;g;b or 38:2:r:g:b (or 38:2::r:g:b with colorspace)
    // The ITU T.416 format includes an optional colorspace ID after the 2
    let rIdx = index + 1;

    // Check if the separator after '2' is a colon - if so, we may have colorspace ID
    if (rIdx < params.length && separators[index] === ":") {
      // Check if next param could be a colorspace ID (empty/0) followed by RGB
      // Format: 38:2:<colorspace>:r:g:b or 38:2::r:g:b (empty colorspace)
      // We need at least 3 more params for RGB
      if (index + 4 <= params.length) {
        // Try parsing with colorspace ID skipped
        const r = params[index + 2];
        const g = params[index + 3];
        const b = params[index + 4];
        if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255) {
          return { color: { type: "rgb", r, g, b }, consumed: 5 };
        }
      }
    }

    // Standard format: 38;2;r;g;b
    if (index + 3 < params.length) {
      const r = params[index + 1];
      const g = params[index + 2];
      const b = params[index + 3];
      if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255) {
        return { color: { type: "rgb", r, g, b }, consumed: 4 };
      }
    }
    return null;
  }

  // Unknown color type
  return null;
}

/**
 * Parse an underline style from SGR 4:n subparameter.
 */
function parseUnderlineStyle(style: number): SgrUnderlineStyle {
  switch (style) {
    case 0:
      return "single"; // 4:0 is treated as single underline (or could be none, but 4 alone is single)
    case 1:
      return "single";
    case 2:
      return "double";
    case 3:
      return "curly";
    case 4:
      return "dotted";
    case 5:
      return "dashed";
    default:
      return "single";
  }
}

export function parseSgrParamsAndSeparators(raw: string): { params: number[]; separators: string[] } {
  // track separators between params because some use a colon for special color handling like "\x1b[4:3m"
  const paramsText = raw.length >= 3 ? raw.slice(2, -1) : "";

  const params: number[] = [];
  const separators: string[] = [];

  let current = "";
  for (let i = 0; i < paramsText.length; i++) {
    const ch = paramsText[i];

    if (ch >= "0" && ch <= "9") {
      current += ch;
      continue;
    }

    if (ch === ";" || ch === ":") {
      params.push(current.length > 0 ? Number.parseInt(current, 10) : 0);
      separators.push(ch);
      current = "";
      continue;
    }

    // Ignore any unexpected characters (private markers / intermediates);
    // SGR params are expected to be digits + ';' / ':' only.
  }

  if (current.length > 0) {
    params.push(Number.parseInt(current, 10));
  } else if (paramsText.endsWith(";") || paramsText.endsWith(":")) {
    params.push(0);
  }

  if (params.length === 0) {
    params.push(0);
  }

  return { params, separators };
}

/**
 * Parse SGR parameters and separators into an array of SgrMessage objects.
 *
 * @param params Array of numeric parameters from the SGR sequence
 * @param separators Array of separator characters (';' or ':') between parameters
 * @returns Array of parsed SGR messages
 */
export function parseSgr(params: number[], separators: string[]): SgrMessage[] {
  const messages: SgrMessage[] = [];

  // Empty or single zero param means reset
  if (params.length === 0 || (params.length === 1 && params[0] === 0)) {
    messages.push({ _type: "sgr.reset", implemented: false } as SgrReset);
    return messages;
  }

  const ctx: SgrParseContext = { params, separators, index: 0 };

  while (ctx.index < params.length) {
    const param = params[ctx.index];
    const nextSep = ctx.index < separators.length ? separators[ctx.index] : ";";

    switch (param) {
      case 0:
        messages.push({ _type: "sgr.reset", implemented: false } as SgrReset);
        ctx.index++;
        break;

      case 1:
        messages.push({ _type: "sgr.bold", implemented: false } as SgrBold);
        ctx.index++;
        break;

      case 2:
        messages.push({ _type: "sgr.faint", implemented: false } as SgrFaint);
        ctx.index++;
        break;

      case 3:
        messages.push({ _type: "sgr.italic", implemented: false } as SgrItalic);
        ctx.index++;
        break;

      case 4: {
        // Check for underline style subparameter (4:n)
        if (nextSep === ":" && ctx.index + 1 < params.length) {
          const style = parseUnderlineStyle(params[ctx.index + 1]);
          if (params[ctx.index + 1] === 0) {
            // 4:0 means no underline
            messages.push({ _type: "sgr.notUnderlined", implemented: false } as SgrNotUnderlined);
          } else {
            messages.push({ _type: "sgr.underline", style, implemented: false } as SgrUnderline);
          }
          ctx.index += 2;
        } else {
          messages.push({ _type: "sgr.underline", style: "single", implemented: false } as SgrUnderline);
          ctx.index++;
        }
        break;
      }

      case 5:
        messages.push({ _type: "sgr.slowBlink", implemented: false } as SgrSlowBlink);
        ctx.index++;
        break;

      case 6:
        messages.push({ _type: "sgr.rapidBlink", implemented: false } as SgrRapidBlink);
        ctx.index++;
        break;

      case 7:
        messages.push({ _type: "sgr.inverse", implemented: false } as SgrInverse);
        ctx.index++;
        break;

      case 8:
        messages.push({ _type: "sgr.hidden", implemented: false } as SgrHidden);
        ctx.index++;
        break;

      case 9:
        messages.push({ _type: "sgr.strikethrough", implemented: false } as SgrStrikethrough);
        ctx.index++;
        break;

      case 10:
      case 11:
      case 12:
      case 13:
      case 14:
      case 15:
      case 16:
      case 17:
      case 18:
      case 19:
        messages.push({ _type: "sgr.font", font: param - 10, implemented: false } as SgrFont);
        ctx.index++;
        break;

      case 20:
        messages.push({ _type: "sgr.fraktur", implemented: false } as SgrFraktur);
        ctx.index++;
        break;

      case 21:
        messages.push({ _type: "sgr.doubleUnderline", implemented: false } as SgrDoubleUnderline);
        ctx.index++;
        break;

      case 22:
        messages.push({ _type: "sgr.normalIntensity", implemented: false } as SgrNormalIntensity);
        ctx.index++;
        break;

      case 23:
        messages.push({ _type: "sgr.notItalic", implemented: false } as SgrNotItalic);
        ctx.index++;
        break;

      case 24:
        messages.push({ _type: "sgr.notUnderlined", implemented: false } as SgrNotUnderlined);
        ctx.index++;
        break;

      case 25:
        messages.push({ _type: "sgr.notBlinking", implemented: false } as SgrNotBlinking);
        ctx.index++;
        break;

      case 26:
        messages.push({ _type: "sgr.proportionalSpacing", implemented: false } as SgrProportionalSpacing);
        ctx.index++;
        break;

      case 27:
        messages.push({ _type: "sgr.notInverse", implemented: false } as SgrNotInverse);
        ctx.index++;
        break;

      case 28:
        messages.push({ _type: "sgr.notHidden", implemented: false } as SgrNotHidden);
        ctx.index++;
        break;

      case 29:
        messages.push({ _type: "sgr.notStrikethrough", implemented: false } as SgrNotStrikethrough);
        ctx.index++;
        break;

      // Standard foreground colors (30-37)
      case 30:
      case 31:
      case 32:
      case 33:
      case 34:
      case 35:
      case 36:
      case 37:
        messages.push({
          _type: "sgr.foregroundColor",
          color: { type: "named", color: STANDARD_COLORS[param - 30] },
          implemented: false
        } as SgrForegroundColor);
        ctx.index++;
        break;

      case 38: {
        // Extended foreground color
        ctx.index++;
        const result = parseExtendedColor(ctx);
        if (result) {
          messages.push({ _type: "sgr.foregroundColor", color: result.color, implemented: false } as SgrForegroundColor);
          ctx.index += result.consumed;
        } else {
          // Invalid extended color, emit unknown
          messages.push({ _type: "sgr.unknown", params: [38], implemented: false } as SgrUnknown);
        }
        break;
      }

      case 39:
        messages.push({ _type: "sgr.defaultForeground", implemented: false } as SgrDefaultForeground);
        ctx.index++;
        break;

      // Standard background colors (40-47)
      case 40:
      case 41:
      case 42:
      case 43:
      case 44:
      case 45:
      case 46:
      case 47:
        messages.push({
          _type: "sgr.backgroundColor",
          color: { type: "named", color: STANDARD_COLORS[param - 40] },
          implemented: false
        } as SgrBackgroundColor);
        ctx.index++;
        break;

      case 48: {
        // Extended background color
        ctx.index++;
        const result = parseExtendedColor(ctx);
        if (result) {
          messages.push({ _type: "sgr.backgroundColor", color: result.color, implemented: false } as SgrBackgroundColor);
          ctx.index += result.consumed;
        } else {
          // Invalid extended color, emit unknown
          messages.push({ _type: "sgr.unknown", params: [48], implemented: false } as SgrUnknown);
        }
        break;
      }

      case 49:
        messages.push({ _type: "sgr.defaultBackground", implemented: false } as SgrDefaultBackground);
        ctx.index++;
        break;

      case 50:
        messages.push({ _type: "sgr.disableProportionalSpacing", implemented: false } as SgrDisableProportionalSpacing);
        ctx.index++;
        break;

      case 51:
        messages.push({ _type: "sgr.framed", implemented: false } as SgrFramed);
        ctx.index++;
        break;

      case 52:
        messages.push({ _type: "sgr.encircled", implemented: false } as SgrEncircled);
        ctx.index++;
        break;

      case 53:
        messages.push({ _type: "sgr.overlined", implemented: false } as SgrOverlined);
        ctx.index++;
        break;

      case 54:
        messages.push({ _type: "sgr.notFramed", implemented: false } as SgrNotFramed);
        ctx.index++;
        break;

      case 55:
        messages.push({ _type: "sgr.notOverlined", implemented: false } as SgrNotOverlined);
        ctx.index++;
        break;

      case 58: {
        // Extended underline color
        ctx.index++;
        const result = parseExtendedColor(ctx);
        if (result) {
          messages.push({ _type: "sgr.underlineColor", color: result.color, implemented: false } as SgrUnderlineColor);
          ctx.index += result.consumed;
        } else {
          // Invalid extended color, emit unknown
          messages.push({ _type: "sgr.unknown", params: [58], implemented: false } as SgrUnknown);
        }
        break;
      }

      case 59:
        messages.push({ _type: "sgr.defaultUnderlineColor", implemented: false } as SgrDefaultUnderlineColor);
        ctx.index++;
        break;

      case 60:
        messages.push({ _type: "sgr.ideogram", style: "underline", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 61:
        messages.push({ _type: "sgr.ideogram", style: "doubleUnderline", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 62:
        messages.push({ _type: "sgr.ideogram", style: "overline", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 63:
        messages.push({ _type: "sgr.ideogram", style: "doubleOverline", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 64:
        messages.push({ _type: "sgr.ideogram", style: "stress", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 65:
        messages.push({ _type: "sgr.ideogram", style: "reset", implemented: false } as SgrIdeogram);
        ctx.index++;
        break;

      case 73:
        messages.push({ _type: "sgr.superscript", implemented: false } as SgrSuperscript);
        ctx.index++;
        break;

      case 74:
        messages.push({ _type: "sgr.subscript", implemented: false } as SgrSubscript);
        ctx.index++;
        break;

      case 75:
        messages.push({ _type: "sgr.notSuperscriptSubscript", implemented: false } as SgrNotSuperscriptSubscript);
        ctx.index++;
        break;

      // Bright foreground colors (90-97)
      case 90:
      case 91:
      case 92:
      case 93:
      case 94:
      case 95:
      case 96:
      case 97:
        messages.push({
          _type: "sgr.foregroundColor",
          color: { type: "named", color: BRIGHT_COLORS[param - 90] },
          implemented: false
        } as SgrForegroundColor);
        ctx.index++;
        break;

      // Bright background colors (100-107)
      case 100:
      case 101:
      case 102:
      case 103:
      case 104:
      case 105:
      case 106:
      case 107:
        messages.push({
          _type: "sgr.backgroundColor",
          color: { type: "named", color: BRIGHT_COLORS[param - 100] },
          implemented: false
        } as SgrBackgroundColor);
        ctx.index++;
        break;

      default:
        // Unknown parameter
        messages.push({ _type: "sgr.unknown", params: [param], implemented: false } as SgrUnknown);
        ctx.index++;
        break;
    }
  }

  return messages;
}
