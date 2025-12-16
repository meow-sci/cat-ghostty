import { describe, it, expect } from "vitest";

import { Parser } from "../terminal/Parser";
import { ParserHandlers } from "../terminal/ParserOptions";
import { type CsiMessage, type EscMessage, type SgrMessage } from "../terminal/TerminalEmulationTypes";
import { getLogger } from "@catty/log";

/**
 * Helper to create a parser with capturing handlers for testing.
 */
interface CapturedEvents {
  normalBytes: number[];
  normalText: string;
  escMessages: EscMessage[];
  csiMessages: CsiMessage[];
  sgrMessages: SgrMessage[][];
  oscSequences: string[];
  bells: number;
  backspaces: number;
  tabs: number;
  lineFeeds: number;
  formFeeds: number;
  carriageReturns: number;
}

function createCapturingHandlers(): { handlers: ParserHandlers; captured: CapturedEvents } {
  const captured: CapturedEvents = {
    normalBytes: [],
    normalText: "",
    escMessages: [],
    csiMessages: [],
    sgrMessages: [],
    oscSequences: [],
    bells: 0,
    backspaces: 0,
    tabs: 0,
    lineFeeds: 0,
    formFeeds: 0,
    carriageReturns: 0,
  };

  const handlers: ParserHandlers = {
    handleBell: () => {
      captured.bells++;
    },
    handleBackspace: () => {
      captured.backspaces++;
    },
    handleTab: () => {
      captured.tabs++;
    },
    handleLineFeed: () => {
      captured.lineFeeds++;
    },
    handleFormFeed: () => {
      captured.formFeeds++;
    },
    handleCarriageReturn: () => {
      captured.carriageReturns++;
    },
    handleNormalByte: (byte: number) => {
      captured.normalBytes.push(byte);
      // For single-byte ASCII, convert directly
      // For UTF-8, we'll need to decode the full sequence
      if (byte < 0x80) {
        captured.normalText += String.fromCharCode(byte);
      } else {
        // Multi-byte UTF-8 character - store raw for now
        // In real implementation, you'd accumulate and decode
        captured.normalText += `[0x${byte.toString(16)}]`;
      }
    },
    handleEsc: (msg: EscMessage) => {
      captured.escMessages.push(msg);
    },
    handleCsi: (msg: CsiMessage) => {
      captured.csiMessages.push(msg);
    },
    handleOsc: (raw: string) => {
      captured.oscSequences.push(raw);
    },
    handleSgr: (messages: SgrMessage[]) => {
      captured.sgrMessages.push(messages);
    },
  };

  return { handlers, captured };
}

describe("Parser", () => {

  describe("ESC sequences", () => {
    it("should emit DECSC (ESC 7) as esc.saveCursor", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b7"));

      expect(captured.escMessages).toHaveLength(1);
      expect(captured.escMessages[0]._type).toBe("esc.saveCursor");
      expect(captured.escMessages[0].raw).toBe("\x1b7");
    });

    it("should emit DECRC (ESC 8) as esc.restoreCursor", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b8"));

      expect(captured.escMessages).toHaveLength(1);
      expect(captured.escMessages[0]._type).toBe("esc.restoreCursor");
      expect(captured.escMessages[0].raw).toBe("\x1b8");
    });
  });

  describe("plain text", () => {
    it("should parse simple ASCII text", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello, World!"));

      expect(captured.normalText).toBe("Hello, World!");
      expect(captured.normalBytes).toHaveLength(13);
    });

    it("should parse text with spaces and punctuation", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("The quick brown fox jumps over the lazy dog."));

      expect(captured.normalText).toBe("The quick brown fox jumps over the lazy dog.");
    });

    it("should parse numbers and symbols", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("12345 !@#$%^&*()"));

      expect(captured.normalText).toBe("12345 !@#$%^&*()");
    });

    it("should handle empty input", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from(""));

      expect(captured.normalText).toBe("");
      expect(captured.normalBytes).toHaveLength(0);
    });
  });

  describe("C0 control characters", () => {
    it("should handle bell (BEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\x07b"));

      expect(captured.normalText).toBe("ab");
      expect(captured.bells).toBe(1);
    });

    it("should handle backspace (BS)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("abc\x08d"));

      expect(captured.normalText).toBe("abcd");
      expect(captured.backspaces).toBe(1);
    });

    it("should handle tab (HT)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\tb"));

      expect(captured.normalText).toBe("ab");
      expect(captured.tabs).toBe(1);
    });

    it("should handle line feed (LF)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("line1\nline2"));

      expect(captured.normalText).toBe("line1line2");
      expect(captured.lineFeeds).toBe(1);
    });

    it("should handle carriage return (CR)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("hello\rworld"));

      expect(captured.normalText).toBe("helloworld");
      expect(captured.carriageReturns).toBe(1);
    });

    it("should handle CR+LF sequence", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("line1\r\nline2"));

      expect(captured.normalText).toBe("line1line2");
      expect(captured.carriageReturns).toBe(1);
      expect(captured.lineFeeds).toBe(1);
    });

    it("should handle multiple control characters", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x07\x07\x07"));

      expect(captured.bells).toBe(3);
    });
  });

  describe("SGR sequences", () => {
    it("should parse reset SGR (ESC[0m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.reset");
    });

    it("should parse empty SGR as reset (ESC[m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.reset");
    });

    it("should parse bold SGR (ESC[1m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
    });

    it("should parse foreground color (ESC[31m - red)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[31m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "named", color: "red" });
      }
    });

    it("should parse background color (ESC[44m - blue)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[44m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.backgroundColor");
      if (msg._type === "sgr.backgroundColor") {
        expect(msg.color).toEqual({ type: "named", color: "blue" });
      }
    });

    it("should parse 256-color foreground (ESC[38;5;11m - bright yellow)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;5;11m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 11 });
      }
    });

    it("should parse true color foreground (ESC[38;2;255;255;0m - yellow RGB)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;2;255;255;0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "rgb", r: 255, g: 255, b: 0 });
      }
    });

    it("should parse true color background (ESC[48;2;0;128;255m - light blue RGB)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[48;2;0;128;255m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.backgroundColor");
      if (msg._type === "sgr.backgroundColor") {
        expect(msg.color).toEqual({ type: "rgb", r: 0, g: 128, b: 255 });
      }
    });

    it("should parse combined SGR (ESC[1;31;47m - bold red on white)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1;31;47m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(3);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[0][1]._type).toBe("sgr.foregroundColor");
      expect(captured.sgrMessages[0][2]._type).toBe("sgr.backgroundColor");
    });

    it("should parse underline styles (ESC[4:3m - curly underline)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:3m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underline");
      if (msg._type === "sgr.underline") {
        expect(msg.style).toBe("curly");
      }
    });

    it("should parse underline color (ESC[58;2;255;0;0m - red underline)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[58;2;255;0;0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underlineColor");
      if (msg._type === "sgr.underlineColor") {
        expect(msg.color).toEqual({ type: "rgb", r: 255, g: 0, b: 0 });
      }
    });

    it("should parse bright foreground colors (ESC[91m - bright red)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[91m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "named", color: "brightRed" });
      }
    });
  });

  describe("SGR with text", () => {
    it("should parse colored text then reset (ESC[38;2;255;255;0mhiESC[0m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;2;255;255;0mhi\x1b[0m"));

      expect(captured.normalText).toBe("hi");
      expect(captured.sgrMessages).toHaveLength(2);

      // First SGR: set yellow foreground
      const fgMsg = captured.sgrMessages[0][0];
      expect(fgMsg._type).toBe("sgr.foregroundColor");
      if (fgMsg._type === "sgr.foregroundColor") {
        expect(fgMsg.color).toEqual({ type: "rgb", r: 255, g: 255, b: 0 });
      }

      // Second SGR: reset
      expect(captured.sgrMessages[1][0]._type).toBe("sgr.reset");
    });

    it("should parse bold text (ESC[1mbold textESC[0m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1mbold text\x1b[0m"));

      expect(captured.normalText).toBe("bold text");
      expect(captured.sgrMessages).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[1][0]._type).toBe("sgr.reset");
    });

    it("should parse multiple style changes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Red "error" then green "success"
      parser.pushBytes(Buffer.from("\x1b[31merror\x1b[0m \x1b[32msuccess\x1b[0m"));

      expect(captured.normalText).toBe("error success");
      expect(captured.sgrMessages).toHaveLength(4);
    });

    it("should parse italic and strikethrough", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3mitalic\x1b[0m \x1b[9mstrike\x1b[0m"));

      expect(captured.normalText).toBe("italic strike");
      expect(captured.sgrMessages).toHaveLength(4);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.italic");
      expect(captured.sgrMessages[2][0]._type).toBe("sgr.strikethrough");
    });
  });

  describe("CSI sequences", () => {
    it("should parse cursor up (ESC[A)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor up with count (ESC[5A)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(5);
      }
    });

    it("should parse cursor down (ESC[B)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3B"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorDown");
      if (captured.csiMessages[0]._type === "csi.cursorDown") {
        expect(captured.csiMessages[0].count).toBe(3);
      }
    });

    it("should parse cursor forward (ESC[C)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[10C"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorForward");
    });

    it("should parse cursor backward (ESC[D)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2D"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorBackward");
    });

    it("should parse cursor position (ESC[row;colH)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[10;20H"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPosition");
      if (captured.csiMessages[0]._type === "csi.cursorPosition") {
        expect(captured.csiMessages[0].row).toBe(10);
        expect(captured.csiMessages[0].column).toBe(20);
      }
    });

    it("should parse erase in display (ESC[2J - clear screen)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(2);
      }
    });

    it("should parse erase in line (ESC[K)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[K"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInLine");
      if (captured.csiMessages[0]._type === "csi.eraseInLine") {
        expect(captured.csiMessages[0].mode).toBe(0);
      }
    });

    it("should parse scroll up (ESC[S)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3S"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollUp");
    });

    it("should parse scroll down (ESC[T)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2T"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollDown");
    });

    it("should parse DEC private mode set (ESC[?25h - show cursor)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?25h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(25);
      }
    });

    it("should parse DEC private mode reset (ESC[?25l - hide cursor)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?25l"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeReset");
      if (captured.csiMessages[0]._type === "csi.decModeReset") {
        expect(captured.csiMessages[0].modes).toContain(25);
      }
    });

    it("should parse alternate screen buffer (ESC[?1049h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?1049h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(1049);
      }
    });

    it("should parse save cursor position (ESC[s)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[s"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.saveCursorPosition");
    });

    it("should parse restore cursor position (ESC[u)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[u"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.restoreCursorPosition");
    });

    it("should parse unknown CSI as csi.unknown", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[999z"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.unknown");
    });
  });

  describe("OSC sequences", () => {
    it("should parse OSC with BEL terminator (ESC]0;titleBEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Window Title\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("0;My Window Title");
    });

    it("should parse OSC with ST terminator (ESC]0;titleESC\\)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Window Title\x1b\\"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("0;My Window Title");
    });

    it("should parse OSC 52 clipboard (ESC]52;c;base64BEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]52;c;SGVsbG8=\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("52;c;SGVsbG8=");
    });

    it("should parse OSC 8 hyperlink (ESC]8;;urlBEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;;https://example.com\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("8;;https://example.com");
    });
  });

  describe("mixed sequences", () => {
    it("should parse text with CSI cursor movement", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello\x1b[5Aworld"));

      expect(captured.normalText).toBe("Helloworld");
      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
    });

    it("should parse colored text with OSC title", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;Title\x07\x1b[31mred text\x1b[0m"));

      expect(captured.normalText).toBe("red text");
      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.sgrMessages).toHaveLength(2);
    });

    it("should parse complex terminal output (htop-like)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Simulate clearing screen, positioning, and colored output
      const htopLikeOutput = [
        "\x1b[2J", // Clear screen
        "\x1b[H", // Home position
        "\x1b[1;32mCPU:\x1b[0m ", // Bold green "CPU:"
        "\x1b[42m          \x1b[0m", // Green progress bar
        " 50%",
        "\n",
        "\x1b[1;34mMem:\x1b[0m ", // Bold blue "Mem:"
        "\x1b[44m     \x1b[0m", // Blue progress bar
        " 25%",
      ].join("");

      parser.pushBytes(Buffer.from(htopLikeOutput));

      expect(captured.normalText).toBe("CPU:            50%Mem:       25%");
      expect(captured.csiMessages.length).toBeGreaterThan(0);
      expect(captured.sgrMessages.length).toBeGreaterThan(0);
      expect(captured.lineFeeds).toBe(1);
    });

    it("should parse shell prompt with colors", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Typical bash prompt: user@host:path$
      const prompt =
        "\x1b[1;32muser\x1b[0m@\x1b[1;34mhost\x1b[0m:\x1b[1;36m~/projects\x1b[0m$ ";

      parser.pushBytes(Buffer.from(prompt));

      expect(captured.normalText).toBe("user@host:~/projects$ ");
      // 6 SGR escape sequences: 3 sets (each with bold+color) + 3 resets
      expect(captured.sgrMessages.length).toBe(6);
    });

    it("should handle alternating text and escape sequences", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\x1b[1mb\x1b[0mc\x1b[31md\x1b[0me"));

      expect(captured.normalText).toBe("abcde");
      expect(captured.sgrMessages).toHaveLength(4);
    });
  });

  describe("edge cases", () => {
    it("should handle incomplete escape sequence at end of input", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Send first part
      parser.pushBytes(Buffer.from("hello\x1b["));

      expect(captured.normalText).toBe("hello");
      expect(captured.sgrMessages).toHaveLength(0);
      expect(captured.csiMessages).toHaveLength(0);

      // Complete the sequence
      parser.pushBytes(Buffer.from("31mworld"));

      expect(captured.normalText).toBe("helloworld");
      expect(captured.sgrMessages).toHaveLength(1);
    });

    it("should handle split escape sequence across pushes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b"));
      parser.pushBytes(Buffer.from("["));
      parser.pushBytes(Buffer.from("3"));
      parser.pushBytes(Buffer.from("8"));
      parser.pushBytes(Buffer.from(";"));
      parser.pushBytes(Buffer.from("2"));
      parser.pushBytes(Buffer.from(";"));
      parser.pushBytes(Buffer.from("255;128;64m"));
      parser.pushBytes(Buffer.from("test"));

      expect(captured.normalText).toBe("test");
      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "rgb", r: 255, g: 128, b: 64 });
      }
    });

    it("should handle multiple SGR params with trailing semicolon", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Trailing semicolon means empty param = 0 = reset
      parser.pushBytes(Buffer.from("\x1b[1;m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[0][1]._type).toBe("sgr.reset");
    });

    it("should handle byte-by-byte parsing", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const input = "\x1b[31mtest\x1b[0m";
      for (let i = 0; i < input.length; i++) {
        parser.pushByte(input.charCodeAt(i));
      }

      expect(captured.normalText).toBe("test");
      expect(captured.sgrMessages).toHaveLength(2);
    });

    it("should handle long parameter values", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Large numbers in parameters
      parser.pushBytes(Buffer.from("\x1b[999A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(999);
      }
    });
  });

  describe("real-world terminal output", () => {
    it("should parse ls --color output style", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Simulated ls output: directory in blue, file in default, executable in green
      const lsOutput = [
        "\x1b[1;34mDocuments\x1b[0m  ",
        "readme.txt  ",
        "\x1b[1;32mscript.sh\x1b[0m",
        "\n",
      ].join("");

      parser.pushBytes(Buffer.from(lsOutput));

      expect(captured.normalText).toBe("Documents  readme.txt  script.sh");
      expect(captured.lineFeeds).toBe(1);
    });

    it("should parse git diff output style", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Simulated git diff: red for removed, green for added
      const diffOutput = [
        "\x1b[31m- old line\x1b[0m\n",
        "\x1b[32m+ new line\x1b[0m\n",
      ].join("");

      parser.pushBytes(Buffer.from(diffOutput));

      expect(captured.normalText).toBe("- old line+ new line");
      expect(captured.lineFeeds).toBe(2);
      expect(captured.sgrMessages).toHaveLength(4);
    });

    it("should parse vim status line style", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Simulated vim status: inverse video for mode
      const statusLine = "\x1b[7m-- INSERT --\x1b[0m file.txt [+]";

      parser.pushBytes(Buffer.from(statusLine));

      expect(captured.normalText).toBe("-- INSERT -- file.txt [+]");
      expect(captured.sgrMessages).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.inverse");
    });

    it("should parse progress bar style output", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Progress bar with carriage return to overwrite
      const progress1 = "Downloading: [====      ] 40%\r";
      const progress2 = "Downloading: [========  ] 80%\r";
      const progress3 = "Downloading: [==========] 100%\n";

      parser.pushBytes(Buffer.from(progress1));
      parser.pushBytes(Buffer.from(progress2));
      parser.pushBytes(Buffer.from(progress3));

      expect(captured.carriageReturns).toBe(2);
      expect(captured.lineFeeds).toBe(1);
    });

    it("should parse npm/yarn styled output", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // npm style output with colors
      const npmOutput = [
        "\x1b[32m+\x1b[0m lodash@4.17.21\n",
        "\x1b[33mwarn\x1b[0m deprecated package\n",
        "\x1b[31merror\x1b[0m Failed to install\n",
      ].join("");

      parser.pushBytes(Buffer.from(npmOutput));

      expect(captured.normalText).toBe(
        "+ lodash@4.17.21warn deprecated packageerror Failed to install"
      );
      expect(captured.lineFeeds).toBe(3);
    });
  });

  describe("UTF-8 handling", () => {
    it("should pass through ASCII characters correctly", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello World 123 !@#"));

      expect(captured.normalText).toBe("Hello World 123 !@#");
    });

    it("should receive multi-byte UTF-8 as individual bytes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // "こんにちは" in UTF-8
      const japaneseText = Buffer.from("こんにちは", "utf-8");
      parser.pushBytes(japaneseText);

      // The parser receives individual bytes; the handler accumulates them
      // The byte count should match the UTF-8 encoding
      expect(captured.normalBytes.length).toBe(japaneseText.length);
    });

    it("should handle emoji characters as UTF-8 bytes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // "🎉" is 4 bytes in UTF-8
      const emojiText = Buffer.from("🎉", "utf-8");
      parser.pushBytes(emojiText);

      expect(captured.normalBytes.length).toBe(4);
    });

    it("should handle mixed ASCII and UTF-8", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const mixedText = Buffer.from("Hello 世界!", "utf-8");
      parser.pushBytes(mixedText);

      // "Hello " = 6 bytes, "世界" = 6 bytes, "!" = 1 byte = 13 total
      expect(captured.normalBytes.length).toBe(13);
    });

    it("should handle UTF-8 text with SGR sequences", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      // Red Japanese text
      const coloredJapanese = Buffer.from("\x1b[31mエラー\x1b[0m", "utf-8");
      parser.pushBytes(coloredJapanese);

      expect(captured.sgrMessages).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.foregroundColor");
      // The text "エラー" is 9 bytes in UTF-8
      expect(captured.normalBytes.length).toBe(9);
    });

    it("should handle European characters (Latin-1 supplement)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const europeanText = Buffer.from("Café naïve résumé", "utf-8");
      parser.pushBytes(europeanText);

      // Each accented character is 2 bytes in UTF-8
      expect(captured.normalBytes.length).toBeGreaterThan("Cafe naive resume".length);
    });

    it("should handle Cyrillic text", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const cyrillicText = Buffer.from("Привет мир", "utf-8");
      parser.pushBytes(cyrillicText);

      // Each Cyrillic character is 2 bytes in UTF-8
      expect(captured.normalBytes.length).toBe(cyrillicText.length);
    });

    it("should handle Arabic text (RTL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const arabicText = Buffer.from("مرحبا", "utf-8");
      parser.pushBytes(arabicText);

      expect(captured.normalBytes.length).toBe(arabicText.length);
    });

    it("should handle Chinese text with colors", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const chineseWithColor = Buffer.from("\x1b[38;2;255;0;0m成功\x1b[0m", "utf-8");
      parser.pushBytes(chineseWithColor);

      expect(captured.sgrMessages).toHaveLength(2);
      const fgMsg = captured.sgrMessages[0][0];
      expect(fgMsg._type).toBe("sgr.foregroundColor");
      if (fgMsg._type === "sgr.foregroundColor") {
        expect(fgMsg.color).toEqual({ type: "rgb", r: 255, g: 0, b: 0 });
      }
    });
  });

  // =============================================================================
  // Comprehensive CSI Tests
  // =============================================================================
  describe("CSI sequences - comprehensive", () => {
    // Cursor movement default values
    it("should parse cursor up with no param as count=1 (ESC[A)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor down with no param as count=1 (ESC[B)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[B"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorDown");
      if (captured.csiMessages[0]._type === "csi.cursorDown") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor forward with no param as count=1 (ESC[C)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[C"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorForward");
      if (captured.csiMessages[0]._type === "csi.cursorForward") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor backward with no param as count=1 (ESC[D)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[D"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorBackward");
      if (captured.csiMessages[0]._type === "csi.cursorBackward") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    // Cursor next/prev line
    it("should parse cursor next line (ESC[E)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[E"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorNextLine");
      if (captured.csiMessages[0]._type === "csi.cursorNextLine") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor next line with count (ESC[5E)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5E"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorNextLine");
      if (captured.csiMessages[0]._type === "csi.cursorNextLine") {
        expect(captured.csiMessages[0].count).toBe(5);
      }
    });

    it("should parse cursor previous line (ESC[F)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[F"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPrevLine");
      if (captured.csiMessages[0]._type === "csi.cursorPrevLine") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor previous line with count (ESC[3F)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3F"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPrevLine");
      if (captured.csiMessages[0]._type === "csi.cursorPrevLine") {
        expect(captured.csiMessages[0].count).toBe(3);
      }
    });

    // Cursor horizontal absolute
    it("should parse cursor horizontal absolute (ESC[G)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[G"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorHorizontalAbsolute");
      if (captured.csiMessages[0]._type === "csi.cursorHorizontalAbsolute") {
        expect(captured.csiMessages[0].column).toBe(1);
      }
    });

    it("should parse cursor horizontal absolute with column (ESC[15G)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[15G"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorHorizontalAbsolute");
      if (captured.csiMessages[0]._type === "csi.cursorHorizontalAbsolute") {
        expect(captured.csiMessages[0].column).toBe(15);
      }
    });

    // Cursor position variations
    it("should parse cursor position with f final byte (ESC[10;20f)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[10;20f"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPosition");
      if (captured.csiMessages[0]._type === "csi.cursorPosition") {
        expect(captured.csiMessages[0].row).toBe(10);
        expect(captured.csiMessages[0].column).toBe(20);
      }
    });

    it("should parse cursor home position (ESC[H)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[H"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPosition");
      if (captured.csiMessages[0]._type === "csi.cursorPosition") {
        expect(captured.csiMessages[0].row).toBe(1);
        expect(captured.csiMessages[0].column).toBe(1);
      }
    });

    it("should parse cursor position with only row (ESC[5H)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5H"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorPosition");
      if (captured.csiMessages[0]._type === "csi.cursorPosition") {
        expect(captured.csiMessages[0].row).toBe(5);
        expect(captured.csiMessages[0].column).toBe(1);
      }
    });

    // Erase in display variations
    it("should parse erase from cursor to end of screen (ESC[0J)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(0);
      }
    });

    it("should parse erase from start of screen to cursor (ESC[1J)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(1);
      }
    });

    it("should parse erase entire screen and scrollback (ESC[3J)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(3);
      }
    });

    it("should parse erase in display with no param as mode 0 (ESC[J)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(0);
      }
    });

    // Erase in line variations
    it("should parse erase from cursor to end of line (ESC[0K)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0K"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInLine");
      if (captured.csiMessages[0]._type === "csi.eraseInLine") {
        expect(captured.csiMessages[0].mode).toBe(0);
      }
    });

    it("should parse erase from start of line to cursor (ESC[1K)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1K"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInLine");
      if (captured.csiMessages[0]._type === "csi.eraseInLine") {
        expect(captured.csiMessages[0].mode).toBe(1);
      }
    });

    it("should parse erase entire line (ESC[2K)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2K"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInLine");
      if (captured.csiMessages[0]._type === "csi.eraseInLine") {
        expect(captured.csiMessages[0].mode).toBe(2);
      }
    });

    // Scroll region
    it("should parse set scroll region (ESC[5;20r)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5;20r"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setScrollRegion");
      if (captured.csiMessages[0]._type === "csi.setScrollRegion") {
        expect(captured.csiMessages[0].top).toBe(5);
        expect(captured.csiMessages[0].bottom).toBe(20);
      }
    });

    it("should parse reset scroll region (ESC[r)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[r"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setScrollRegion");
      if (captured.csiMessages[0]._type === "csi.setScrollRegion") {
        expect(captured.csiMessages[0].top).toBeUndefined();
        expect(captured.csiMessages[0].bottom).toBeUndefined();
      }
    });

    // Scroll operations with default
    it("should parse scroll up with no param as 1 line (ESC[S)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[S"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollUp");
      if (captured.csiMessages[0]._type === "csi.scrollUp") {
        expect(captured.csiMessages[0].lines).toBe(1);
      }
    });

    it("should parse scroll down with no param as 1 line (ESC[T)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[T"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollDown");
      if (captured.csiMessages[0]._type === "csi.scrollDown") {
        expect(captured.csiMessages[0].lines).toBe(1);
      }
    });

    // DEC cursor style (DECSCUSR)
    it("should parse set cursor style - blinking block (ESC[ 0 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(0);
      }
    });

    it("should parse set cursor style - blinking block (ESC[ 1 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(1);
      }
    });

    it("should parse set cursor style - steady block (ESC[ 2 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(2);
      }
    });

    it("should parse set cursor style - blinking underline (ESC[ 3 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(3);
      }
    });

    it("should parse set cursor style - steady underline (ESC[ 4 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(4);
      }
    });

    it("should parse set cursor style - blinking bar (ESC[ 5 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(5);
      }
    });

    it("should parse set cursor style - steady bar (ESC[ 6 SP q)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[6 q"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.setCursorStyle");
      if (captured.csiMessages[0]._type === "csi.setCursorStyle") {
        expect(captured.csiMessages[0].style).toBe(6);
      }
    });

    // DEC private modes - common modes
    it("should parse DECCKM - application cursor keys (ESC[?1h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?1h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(1);
      }
    });

    it("should parse DECAWM - auto-wrap mode (ESC[?7h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?7h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(7);
      }
    });

    it("should parse mouse tracking mode (ESC[?1000h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?1000h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(1000);
      }
    });

    it("should parse SGR mouse mode (ESC[?1006h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?1006h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(1006);
      }
    });

    it("should parse bracketed paste mode (ESC[?2004h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?2004h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(2004);
      }
    });

    it("should parse multiple DEC modes in one sequence (ESC[?25;1049h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?25;1049h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(25);
        expect(captured.csiMessages[0].modes).toContain(1049);
      }
    });

    it("should parse DEC mode reset (ESC[?2004l)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?2004l"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeReset");
      if (captured.csiMessages[0]._type === "csi.decModeReset") {
        expect(captured.csiMessages[0].modes).toContain(2004);
      }
    });
  });

  // =============================================================================
  // Comprehensive SGR Tests
  // =============================================================================
  describe("SGR sequences - comprehensive", () => {
    // Text styling attributes
    it("should parse faint/dim (ESC[2m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.faint");
    });

    it("should parse slow blink (ESC[5m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.slowBlink");
    });

    it("should parse rapid blink (ESC[6m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[6m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.rapidBlink");
    });

    it("should parse hidden/conceal (ESC[8m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[8m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.hidden");
    });

    // Font selection
    it("should parse primary font (ESC[10m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[10m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.font");
      if (msg._type === "sgr.font") {
        expect(msg.font).toBe(0);
      }
    });

    it("should parse alternative font 1 (ESC[11m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[11m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.font");
      if (msg._type === "sgr.font") {
        expect(msg.font).toBe(1);
      }
    });

    it("should parse fraktur (ESC[20m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[20m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.fraktur");
    });

    it("should parse double underline (ESC[21m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[21m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.doubleUnderline");
    });

    // Reset attributes
    it("should parse normal intensity (ESC[22m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[22m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.normalIntensity");
    });

    it("should parse not italic (ESC[23m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[23m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notItalic");
    });

    it("should parse not underlined (ESC[24m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[24m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notUnderlined");
    });

    it("should parse not blinking (ESC[25m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[25m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notBlinking");
    });

    it("should parse proportional spacing (ESC[26m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[26m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.proportionalSpacing");
    });

    it("should parse not inverse (ESC[27m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[27m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notInverse");
    });

    it("should parse not hidden (ESC[28m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[28m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notHidden");
    });

    it("should parse not strikethrough (ESC[29m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[29m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notStrikethrough");
    });

    // Standard foreground colors
    it("should parse all standard foreground colors (30-37)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const expectedColors = ["black", "red", "green", "yellow", "blue", "magenta", "cyan", "white"];

      for (let i = 0; i < 8; i++) {
        parser.pushBytes(Buffer.from(`\x1b[${30 + i}m`));
      }

      expect(captured.sgrMessages).toHaveLength(8);
      for (let i = 0; i < 8; i++) {
        const msg = captured.sgrMessages[i][0];
        expect(msg._type).toBe("sgr.foregroundColor");
        if (msg._type === "sgr.foregroundColor") {
          expect(msg.color).toEqual({ type: "named", color: expectedColors[i] });
        }
      }
    });

    // Default colors
    it("should parse default foreground color (ESC[39m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[39m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.defaultForeground");
    });

    // Standard background colors
    it("should parse all standard background colors (40-47)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const expectedColors = ["black", "red", "green", "yellow", "blue", "magenta", "cyan", "white"];

      for (let i = 0; i < 8; i++) {
        parser.pushBytes(Buffer.from(`\x1b[${40 + i}m`));
      }

      expect(captured.sgrMessages).toHaveLength(8);
      for (let i = 0; i < 8; i++) {
        const msg = captured.sgrMessages[i][0];
        expect(msg._type).toBe("sgr.backgroundColor");
        if (msg._type === "sgr.backgroundColor") {
          expect(msg.color).toEqual({ type: "named", color: expectedColors[i] });
        }
      }
    });

    it("should parse default background color (ESC[49m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[49m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.defaultBackground");
    });

    // Bright foreground colors
    it("should parse all bright foreground colors (90-97)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const expectedColors = [
        "brightBlack", "brightRed", "brightGreen", "brightYellow",
        "brightBlue", "brightMagenta", "brightCyan", "brightWhite"
      ];

      for (let i = 0; i < 8; i++) {
        parser.pushBytes(Buffer.from(`\x1b[${90 + i}m`));
      }

      expect(captured.sgrMessages).toHaveLength(8);
      for (let i = 0; i < 8; i++) {
        const msg = captured.sgrMessages[i][0];
        expect(msg._type).toBe("sgr.foregroundColor");
        if (msg._type === "sgr.foregroundColor") {
          expect(msg.color).toEqual({ type: "named", color: expectedColors[i] });
        }
      }
    });

    // Bright background colors
    it("should parse all bright background colors (100-107)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const expectedColors = [
        "brightBlack", "brightRed", "brightGreen", "brightYellow",
        "brightBlue", "brightMagenta", "brightCyan", "brightWhite"
      ];

      for (let i = 0; i < 8; i++) {
        parser.pushBytes(Buffer.from(`\x1b[${100 + i}m`));
      }

      expect(captured.sgrMessages).toHaveLength(8);
      for (let i = 0; i < 8; i++) {
        const msg = captured.sgrMessages[i][0];
        expect(msg._type).toBe("sgr.backgroundColor");
        if (msg._type === "sgr.backgroundColor") {
          expect(msg.color).toEqual({ type: "named", color: expectedColors[i] });
        }
      }
    });

    // 256-color palette
    it("should parse 256-color foreground - standard colors (0-7)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;5;0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 0 });
      }
    });

    it("should parse 256-color foreground - high intensity (8-15)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;5;15m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 15 });
      }
    });

    it("should parse 256-color foreground - 216 color cube (16-231)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;5;196m")); // Bright red in cube

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 196 });
      }
    });

    it("should parse 256-color foreground - grayscale (232-255)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;5;244m")); // Mid gray

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.foregroundColor");
      if (msg._type === "sgr.foregroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 244 });
      }
    });

    it("should parse 256-color background (ESC[48;5;nm)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[48;5;226m")); // Yellow

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.backgroundColor");
      if (msg._type === "sgr.backgroundColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 226 });
      }
    });

    // Underline styles with colon separator
    it("should parse underline style - none (ESC[4:0m) as not underlined", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      // 4:0 means "no underline" which is equivalent to notUnderlined
      expect(msg._type).toBe("sgr.notUnderlined");
    });

    it("should parse underline style - single (ESC[4:1m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:1m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underline");
      if (msg._type === "sgr.underline") {
        expect(msg.style).toBe("single");
      }
    });

    it("should parse underline style - double (ESC[4:2m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:2m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underline");
      if (msg._type === "sgr.underline") {
        expect(msg.style).toBe("double");
      }
    });

    it("should parse underline style - dotted (ESC[4:4m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:4m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underline");
      if (msg._type === "sgr.underline") {
        expect(msg.style).toBe("dotted");
      }
    });

    it("should parse underline style - dashed (ESC[4:5m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[4:5m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underline");
      if (msg._type === "sgr.underline") {
        expect(msg.style).toBe("dashed");
      }
    });

    // Underline color
    it("should parse 256-color underline (ESC[58;5;nm)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[58;5;196m"));

      expect(captured.sgrMessages).toHaveLength(1);
      const msg = captured.sgrMessages[0][0];
      expect(msg._type).toBe("sgr.underlineColor");
      if (msg._type === "sgr.underlineColor") {
        expect(msg.color).toEqual({ type: "indexed", index: 196 });
      }
    });

    it("should parse default underline color (ESC[59m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[59m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.defaultUnderlineColor");
    });

    // Framing and overlining
    it("should parse framed (ESC[51m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[51m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.framed");
    });

    it("should parse encircled (ESC[52m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[52m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.encircled");
    });

    it("should parse overlined (ESC[53m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[53m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.overlined");
    });

    it("should parse not framed (ESC[54m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[54m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notFramed");
    });

    it("should parse not overlined (ESC[55m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[55m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notOverlined");
    });

    // Superscript/subscript
    it("should parse superscript (ESC[73m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[73m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.superscript");
    });

    it("should parse subscript (ESC[74m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[74m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.subscript");
    });

    it("should parse not superscript/subscript (ESC[75m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[75m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.notSuperscriptSubscript");
    });

    // Complex combined sequences
    it("should parse complex combined SGR (bold + italic + underline + red fg)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1;3;4;31m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(4);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[0][1]._type).toBe("sgr.italic");
      expect(captured.sgrMessages[0][2]._type).toBe("sgr.underline");
      expect(captured.sgrMessages[0][3]._type).toBe("sgr.foregroundColor");
    });

    it("should parse SGR with RGB fg and bg", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[38;2;255;128;0;48;2;0;0;128m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(2);

      const fgMsg = captured.sgrMessages[0][0];
      expect(fgMsg._type).toBe("sgr.foregroundColor");
      if (fgMsg._type === "sgr.foregroundColor") {
        expect(fgMsg.color).toEqual({ type: "rgb", r: 255, g: 128, b: 0 });
      }

      const bgMsg = captured.sgrMessages[0][1];
      expect(bgMsg._type).toBe("sgr.backgroundColor");
      if (bgMsg._type === "sgr.backgroundColor") {
        expect(bgMsg.color).toEqual({ type: "rgb", r: 0, g: 0, b: 128 });
      }
    });

    it("should handle unknown SGR codes gracefully", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[999m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.unknown");
    });
  });

  // =============================================================================
  // Comprehensive OSC Tests
  // =============================================================================
  describe("OSC sequences - comprehensive", () => {
    // OSC 0: Set icon name and window title
    it("should parse OSC 0 - set icon name and window title (BEL terminator)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Terminal Title\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;My Terminal Title\x07");
    });

    it("should parse OSC 0 - set icon name and window title (ST terminator)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Terminal Title\x1b\\"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;My Terminal Title\x1b\\");
    });

    it("should parse OSC 0 with empty title", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;\x07");
    });

    // OSC 1: Set icon name only
    it("should parse OSC 1 - set icon name only", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]1;IconName\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]1;IconName\x07");
    });

    // OSC 2: Set window title only
    it("should parse OSC 2 - set window title only", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]2;Window Title\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]2;Window Title\x07");
    });

    // OSC 4: Set/query color palette
    it("should parse OSC 4 - set color palette entry", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]4;1;rgb:ff/00/00\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]4;1;rgb:ff/00/00\x07");
    });

    it("should parse OSC 4 - query color palette entry", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]4;1;?\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]4;1;?\x07");
    });

    // OSC 7: Set current working directory
    it("should parse OSC 7 - set current working directory", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]7;file:///Users/user/projects\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]7;file:///Users/user/projects\x07");
    });

    it("should parse OSC 7 with encoded spaces", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]7;file:///Users/user/My%20Projects\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]7;file:///Users/user/My%20Projects\x07");
    });

    // OSC 8: Hyperlinks
    it("should parse OSC 8 - hyperlink with URL", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;;https://example.com\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]8;;https://example.com\x07");
    });

    it("should parse OSC 8 - hyperlink with id parameter", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;id=link1;https://example.com\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]8;id=link1;https://example.com\x07");
    });

    it("should parse OSC 8 - hyperlink close (empty URL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;;\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]8;;\x07");
    });

    it("should parse OSC 8 - hyperlink with multiple parameters", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;id=mylink:title=Click me;https://example.com/page\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]8;id=mylink:title=Click me;https://example.com/page\x07");
    });

    // OSC 9: iTerm2 growl notification (legacy)
    it("should parse OSC 9 - notification", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]9;Build completed!\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]9;Build completed!\x07");
    });

    // OSC 10: Set/query foreground color
    it("should parse OSC 10 - set foreground color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]10;rgb:ff/ff/ff\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]10;rgb:ff/ff/ff\x07");
    });

    it("should parse OSC 10 - query foreground color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]10;?\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]10;?\x07");
    });

    // OSC 11: Set/query background color
    it("should parse OSC 11 - set background color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]11;rgb:00/00/00\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]11;rgb:00/00/00\x07");
    });

    it("should parse OSC 11 - query background color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]11;?\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]11;?\x07");
    });

    // OSC 12: Set/query cursor color
    it("should parse OSC 12 - set cursor color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]12;rgb:00/ff/00\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]12;rgb:00/ff/00\x07");
    });

    // OSC 52: Clipboard operations
    it("should parse OSC 52 - clipboard with base64 data", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const base64Data = Buffer.from("Hello World").toString("base64");
      parser.pushBytes(Buffer.from(`\x1b]52;c;${base64Data}\x07`));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe(`\x1b]52;c;${base64Data}\x07`);
    });

    it("should parse OSC 52 - clipboard query", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]52;c;?\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]52;c;?\x07");
    });

    it("should parse OSC 52 - primary selection (p)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const base64Data = Buffer.from("Selected text").toString("base64");
      parser.pushBytes(Buffer.from(`\x1b]52;p;${base64Data}\x07`));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe(`\x1b]52;p;${base64Data}\x07`);
    });

    it("should parse OSC 52 - multiple selections (pc)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const base64Data = Buffer.from("Data").toString("base64");
      parser.pushBytes(Buffer.from(`\x1b]52;pc;${base64Data}\x07`));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe(`\x1b]52;pc;${base64Data}\x07`);
    });

    // OSC 104: Reset color palette
    it("should parse OSC 104 - reset all colors", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]104\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]104\x07");
    });

    it("should parse OSC 104 - reset specific color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]104;1\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]104;1\x07");
    });

    // OSC 110: Reset foreground color
    it("should parse OSC 110 - reset foreground color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]110\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]110\x07");
    });

    // OSC 111: Reset background color
    it("should parse OSC 111 - reset background color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]111\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]111\x07");
    });

    // OSC 112: Reset cursor color
    it("should parse OSC 112 - reset cursor color", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]112\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]112\x07");
    });

    // OSC 133: Shell integration / semantic prompts (FinalTerm)
    it("should parse OSC 133 - prompt start (A)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]133;A\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]133;A\x07");
    });

    it("should parse OSC 133 - command start (B)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]133;B\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]133;B\x07");
    });

    it("should parse OSC 133 - command executed (C)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]133;C\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]133;C\x07");
    });

    it("should parse OSC 133 - command finished (D)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]133;D;0\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]133;D;0\x07");
    });

    // OSC 1337: iTerm2 proprietary sequences
    it("should parse OSC 1337 - iTerm2 set user var", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const base64Value = Buffer.from("value").toString("base64");
      parser.pushBytes(Buffer.from(`\x1b]1337;SetUserVar=myvar=${base64Value}\x07`));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe(`\x1b]1337;SetUserVar=myvar=${base64Value}\x07`);
    });

    it("should parse OSC 1337 - iTerm2 report cell size", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]1337;ReportCellSize\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]1337;ReportCellSize\x07");
    });

    // Edge cases
    it("should handle OSC with special characters in title", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;user@host:~/path/to/dir$\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;user@host:~/path/to/dir$\x07");
    });

    it("should handle multiple consecutive OSC sequences", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;Title1\x07\x1b]0;Title2\x07\x1b]0;Title3\x07"));

      expect(captured.oscSequences).toHaveLength(3);
      expect(captured.oscSequences[0]).toBe("\x1b]0;Title1\x07");
      expect(captured.oscSequences[1]).toBe("\x1b]0;Title2\x07");
      expect(captured.oscSequences[2]).toBe("\x1b]0;Title3\x07");
    });

    it("should handle OSC followed by text", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;Title\x07Hello World"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;Title\x07");
      // "Hello World" should be received as normal bytes
      expect(captured.normalBytes.length).toBeGreaterThan(0);
    });

    it("should handle OSC with very long content", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      const longTitle = "A".repeat(1000);
      parser.pushBytes(Buffer.from(`\x1b]0;${longTitle}\x07`));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe(`\x1b]0;${longTitle}\x07`);
    });

    it("should handle OSC split across multiple pushes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My "));
      parser.pushBytes(Buffer.from("Terminal "));
      parser.pushBytes(Buffer.from("Title\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;My Terminal Title\x07");
    });

    it("should handle OSC with ST split across pushes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;Title\x1b"));
      parser.pushBytes(Buffer.from("\\"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toBe("\x1b]0;Title\x1b\\");
    });
  });
});
