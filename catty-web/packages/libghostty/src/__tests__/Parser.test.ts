import { describe, it, expect, beforeEach } from "vitest";

import { loadWasmForTest } from "./util/loadWasmForTest";
import { Parser } from "../terminal/Parser";
import { ParserHandlers } from "../terminal/ParserOptions";
import { type CsiMessage, type SgrMessage } from "../terminal/TerminalEmulationTypes";
import { getLogger } from "@catty/log";

/**
 * Helper to create a parser with capturing handlers for testing.
 */
interface CapturedEvents {
  normalBytes: number[];
  normalText: string;
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
  let wasm: Awaited<ReturnType<typeof loadWasmForTest>>;

  beforeEach(async () => {
    wasm = await loadWasmForTest();
  });

  describe("plain text", () => {
    it("should parse simple ASCII text", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello, World!"));

      expect(captured.normalText).toBe("Hello, World!");
      expect(captured.normalBytes).toHaveLength(13);
    });

    it("should parse text with spaces and punctuation", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("The quick brown fox jumps over the lazy dog."));

      expect(captured.normalText).toBe("The quick brown fox jumps over the lazy dog.");
    });

    it("should parse numbers and symbols", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("12345 !@#$%^&*()"));

      expect(captured.normalText).toBe("12345 !@#$%^&*()");
    });

    it("should handle empty input", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from(""));

      expect(captured.normalText).toBe("");
      expect(captured.normalBytes).toHaveLength(0);
    });
  });

  describe("C0 control characters", () => {
    it("should handle bell (BEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\x07b"));

      expect(captured.normalText).toBe("ab");
      expect(captured.bells).toBe(1);
    });

    it("should handle backspace (BS)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("abc\x08d"));

      expect(captured.normalText).toBe("abcd");
      expect(captured.backspaces).toBe(1);
    });

    it("should handle tab (HT)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\tb"));

      expect(captured.normalText).toBe("ab");
      expect(captured.tabs).toBe(1);
    });

    it("should handle line feed (LF)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("line1\nline2"));

      expect(captured.normalText).toBe("line1line2");
      expect(captured.lineFeeds).toBe(1);
    });

    it("should handle carriage return (CR)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("hello\rworld"));

      expect(captured.normalText).toBe("helloworld");
      expect(captured.carriageReturns).toBe(1);
    });

    it("should handle CR+LF sequence", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("line1\r\nline2"));

      expect(captured.normalText).toBe("line1line2");
      expect(captured.carriageReturns).toBe(1);
      expect(captured.lineFeeds).toBe(1);
    });

    it("should handle multiple control characters", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x07\x07\x07"));

      expect(captured.bells).toBe(3);
    });
  });

  describe("SGR sequences", () => {
    it("should parse reset SGR (ESC[0m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.reset");
    });

    it("should parse empty SGR as reset (ESC[m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.reset");
    });

    it("should parse bold SGR (ESC[1m)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(1);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
    });

    it("should parse foreground color (ESC[31m - red)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1;31;47m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(3);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[0][1]._type).toBe("sgr.foregroundColor");
      expect(captured.sgrMessages[0][2]._type).toBe("sgr.backgroundColor");
    });

    it("should parse underline styles (ESC[4:3m - curly underline)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[1mbold text\x1b[0m"));

      expect(captured.normalText).toBe("bold text");
      expect(captured.sgrMessages).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[1][0]._type).toBe("sgr.reset");
    });

    it("should parse multiple style changes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      // Red "error" then green "success"
      parser.pushBytes(Buffer.from("\x1b[31merror\x1b[0m \x1b[32msuccess\x1b[0m"));

      expect(captured.normalText).toBe("error success");
      expect(captured.sgrMessages).toHaveLength(4);
    });

    it("should parse italic and strikethrough", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(1);
      }
    });

    it("should parse cursor up with count (ESC[5A)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[5A"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
      if (captured.csiMessages[0]._type === "csi.cursorUp") {
        expect(captured.csiMessages[0].count).toBe(5);
      }
    });

    it("should parse cursor down (ESC[B)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3B"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorDown");
      if (captured.csiMessages[0]._type === "csi.cursorDown") {
        expect(captured.csiMessages[0].count).toBe(3);
      }
    });

    it("should parse cursor forward (ESC[C)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[10C"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorForward");
    });

    it("should parse cursor backward (ESC[D)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2D"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorBackward");
    });

    it("should parse cursor position (ESC[row;colH)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2J"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInDisplay");
      if (captured.csiMessages[0]._type === "csi.eraseInDisplay") {
        expect(captured.csiMessages[0].mode).toBe(2);
      }
    });

    it("should parse erase in line (ESC[K)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[K"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.eraseInLine");
      if (captured.csiMessages[0]._type === "csi.eraseInLine") {
        expect(captured.csiMessages[0].mode).toBe(0);
      }
    });

    it("should parse scroll up (ESC[S)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[3S"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollUp");
    });

    it("should parse scroll down (ESC[T)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[2T"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.scrollDown");
    });

    it("should parse DEC private mode set (ESC[?25h - show cursor)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?25h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(25);
      }
    });

    it("should parse DEC private mode reset (ESC[?25l - hide cursor)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?25l"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeReset");
      if (captured.csiMessages[0]._type === "csi.decModeReset") {
        expect(captured.csiMessages[0].modes).toContain(25);
      }
    });

    it("should parse alternate screen buffer (ESC[?1049h)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?1049h"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.decModeSet");
      if (captured.csiMessages[0]._type === "csi.decModeSet") {
        expect(captured.csiMessages[0].modes).toContain(1049);
      }
    });

    it("should parse save cursor position (ESC[s)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[s"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.saveCursorPosition");
    });

    it("should parse restore cursor position (ESC[u)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[u"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.restoreCursorPosition");
    });

    it("should parse unknown CSI as csi.unknown", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[999z"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.unknown");
    });
  });

  describe("OSC sequences", () => {
    it("should parse OSC with BEL terminator (ESC]0;titleBEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Window Title\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("0;My Window Title");
    });

    it("should parse OSC with ST terminator (ESC]0;titleESC\\)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;My Window Title\x1b\\"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("0;My Window Title");
    });

    it("should parse OSC 52 clipboard (ESC]52;c;base64BEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]52;c;SGVsbG8=\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("52;c;SGVsbG8=");
    });

    it("should parse OSC 8 hyperlink (ESC]8;;urlBEL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]8;;https://example.com\x07"));

      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.oscSequences[0]).toContain("8;;https://example.com");
    });
  });

  describe("mixed sequences", () => {
    it("should parse text with CSI cursor movement", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello\x1b[5Aworld"));

      expect(captured.normalText).toBe("Helloworld");
      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.cursorUp");
    });

    it("should parse colored text with OSC title", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]0;Title\x07\x1b[31mred text\x1b[0m"));

      expect(captured.normalText).toBe("red text");
      expect(captured.oscSequences).toHaveLength(1);
      expect(captured.sgrMessages).toHaveLength(2);
    });

    it("should parse complex terminal output (htop-like)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("a\x1b[1mb\x1b[0mc\x1b[31md\x1b[0me"));

      expect(captured.normalText).toBe("abcde");
      expect(captured.sgrMessages).toHaveLength(4);
    });
  });

  describe("edge cases", () => {
    it("should handle incomplete escape sequence at end of input", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      // Trailing semicolon means empty param = 0 = reset
      parser.pushBytes(Buffer.from("\x1b[1;m"));

      expect(captured.sgrMessages).toHaveLength(1);
      expect(captured.sgrMessages[0]).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.bold");
      expect(captured.sgrMessages[0][1]._type).toBe("sgr.reset");
    });

    it("should handle byte-by-byte parsing", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      const input = "\x1b[31mtest\x1b[0m";
      for (let i = 0; i < input.length; i++) {
        parser.pushByte(input.charCodeAt(i));
      }

      expect(captured.normalText).toBe("test");
      expect(captured.sgrMessages).toHaveLength(2);
    });

    it("should handle long parameter values", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      // Simulated vim status: inverse video for mode
      const statusLine = "\x1b[7m-- INSERT --\x1b[0m file.txt [+]";

      parser.pushBytes(Buffer.from(statusLine));

      expect(captured.normalText).toBe("-- INSERT -- file.txt [+]");
      expect(captured.sgrMessages).toHaveLength(2);
      expect(captured.sgrMessages[0][0]._type).toBe("sgr.inverse");
    });

    it("should parse progress bar style output", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("Hello World 123 !@#"));

      expect(captured.normalText).toBe("Hello World 123 !@#");
    });

    it("should receive multi-byte UTF-8 as individual bytes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      // "こんにちは" in UTF-8
      const japaneseText = Buffer.from("こんにちは", "utf-8");
      parser.pushBytes(japaneseText);

      // The parser receives individual bytes; the handler accumulates them
      // The byte count should match the UTF-8 encoding
      expect(captured.normalBytes.length).toBe(japaneseText.length);
    });

    it("should handle emoji characters as UTF-8 bytes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      // "🎉" is 4 bytes in UTF-8
      const emojiText = Buffer.from("🎉", "utf-8");
      parser.pushBytes(emojiText);

      expect(captured.normalBytes.length).toBe(4);
    });

    it("should handle mixed ASCII and UTF-8", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      const mixedText = Buffer.from("Hello 世界!", "utf-8");
      parser.pushBytes(mixedText);

      // "Hello " = 6 bytes, "世界" = 6 bytes, "!" = 1 byte = 13 total
      expect(captured.normalBytes.length).toBe(13);
    });

    it("should handle UTF-8 text with SGR sequences", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      const europeanText = Buffer.from("Café naïve résumé", "utf-8");
      parser.pushBytes(europeanText);

      // Each accented character is 2 bytes in UTF-8
      expect(captured.normalBytes.length).toBeGreaterThan("Cafe naive resume".length);
    });

    it("should handle Cyrillic text", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      const cyrillicText = Buffer.from("Привет мир", "utf-8");
      parser.pushBytes(cyrillicText);

      // Each Cyrillic character is 2 bytes in UTF-8
      expect(captured.normalBytes.length).toBe(cyrillicText.length);
    });

    it("should handle Arabic text (RTL)", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

      const arabicText = Buffer.from("مرحبا", "utf-8");
      parser.pushBytes(arabicText);

      expect(captured.normalBytes.length).toBe(arabicText.length);
    });

    it("should handle Chinese text with colors", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ wasm, handlers, log: getLogger() });

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
});
