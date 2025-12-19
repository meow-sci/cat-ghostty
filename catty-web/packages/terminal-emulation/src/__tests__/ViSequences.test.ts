import { describe, it, expect } from "vitest";
import { getLogger } from "@catty/log";

import { Parser } from "../terminal/Parser";
import type { CsiMessage, XtermOscMessage, SgrSequence } from "../terminal/TerminalEmulationTypes";
import { type ParserHandlers } from "../terminal/ParserOptions";

interface CapturedEvents {
  csiMessages: CsiMessage[];
  xtermOscMessages: XtermOscMessage[];
  sgrSequences: SgrSequence[];
}

function createCapturingHandlers(): { handlers: ParserHandlers; captured: CapturedEvents } {
  const captured: CapturedEvents = {
    csiMessages: [],
    xtermOscMessages: [],
    sgrSequences: [],
  };

  const handlers: ParserHandlers = {
    handleBell: () => {},
    handleBackspace: () => {},
    handleTab: () => {},
    handleLineFeed: () => {},
    handleFormFeed: () => {},
    handleCarriageReturn: () => {},
    handleNormalByte: () => {},
    handleEsc: () => {},
    handleCsi: (msg: CsiMessage) => {
      captured.csiMessages.push(msg);
    },
    handleOsc: () => {},
    handleSgr: (seq: SgrSequence) => {
      captured.sgrSequences.push(seq);
    },
    handleXtermOsc: (msg: XtermOscMessage) => {
      captured.xtermOscMessages.push(msg);
    },
  };

  return { handlers, captured };
}

describe("Vi-specific terminal sequences", () => {
  
  describe("Enhanced SGR sequences", () => {
    it("should parse CSI > 4 ; 2 m as enhanced SGR mode", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[>4;2m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[>4;2m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.enhancedMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.enhancedMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([4, 2]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(true);
      }
    });

    it("should parse CSI > 4 ; 0 m as enhanced underline off", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[>4;0m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.enhancedMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.enhancedMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([4, 0]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(true);
      }
    });

    it("should parse CSI > 4 ; 3 m as enhanced curly underline", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[>4;3m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.enhancedMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.enhancedMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([4, 3]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(true);
      }
    });

    it("should gracefully handle invalid enhanced underline types", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[>4;99m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.enhancedMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.enhancedMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([4, 99]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(false);
      }
    });

    it("should gracefully handle other enhanced modes", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[>5;1m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.enhancedMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.enhancedMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([5, 1]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(false);
      }
    });
  });

  describe("Private SGR sequences", () => {
    it("should parse CSI ? 4 m as private SGR mode", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[?4m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[?4m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.privateMode");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.privateMode") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([4]);
        expect(captured.sgrSequences[0].messages[0].implemented).toBe(true);
      }
    });
  });

  describe("SGR with intermediate characters", () => {
    it("should parse CSI 0 % m as SGR with intermediate", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[0%m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[0%m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.withIntermediate");
      if (captured.sgrSequences[0].messages[0]._type === "sgr.withIntermediate") {
        expect(captured.sgrSequences[0].messages[0].params).toEqual([0]);
        expect(captured.sgrSequences[0].messages[0].intermediate).toBe("%");
      }
    });
  });

  describe("Window manipulation sequences", () => {
    it("should parse CSI 22 ; 2 t as window manipulation", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[22;2t"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.windowManipulation");
      expect(captured.csiMessages[0].raw).toBe("\x1b[22;2t");
      if (captured.csiMessages[0]._type === "csi.windowManipulation") {
        expect(captured.csiMessages[0].operation).toBe(22);
        expect(captured.csiMessages[0].params).toEqual([2]);
      }
    });

    it("should parse CSI 22 ; 1 t as window manipulation", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[22;1t"));

      expect(captured.csiMessages).toHaveLength(1);
      expect(captured.csiMessages[0]._type).toBe("csi.windowManipulation");
      expect(captured.csiMessages[0].raw).toBe("\x1b[22;1t");
      if (captured.csiMessages[0]._type === "csi.windowManipulation") {
        expect(captured.csiMessages[0].operation).toBe(22);
        expect(captured.csiMessages[0].params).toEqual([1]);
      }
    });
  });

  describe("Color query sequences", () => {
    it("should parse OSC 10 ; ? as foreground color query", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]10;?\x07"));

      expect(captured.xtermOscMessages).toHaveLength(1);
      expect(captured.xtermOscMessages[0]._type).toBe("osc.queryForegroundColor");
      expect(captured.xtermOscMessages[0].raw).toBe("\x1b]10;?\x07");
      expect(captured.xtermOscMessages[0].terminator).toBe("BEL");
    });

    it("should parse OSC 11 ; ? as background color query", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b]11;?\x07"));

      expect(captured.xtermOscMessages).toHaveLength(1);
      expect(captured.xtermOscMessages[0]._type).toBe("osc.queryBackgroundColor");
      expect(captured.xtermOscMessages[0].raw).toBe("\x1b]11;?\x07");
      expect(captured.xtermOscMessages[0].terminator).toBe("BEL");
    });
  });

  describe("Standard SGR sequences (should still work)", () => {
    it("should still parse CSI 27 m as not inverse", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[27m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[27m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.notInverse");
    });

    it("should still parse CSI 23 m as not italic", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[23m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[23m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.notItalic");
    });

    it("should still parse CSI 29 m as not strikethrough", async () => {
      const { handlers, captured } = createCapturingHandlers();
      const parser = new Parser({ handlers, log: getLogger() });

      parser.pushBytes(Buffer.from("\x1b[29m"));

      expect(captured.sgrSequences).toHaveLength(1);
      expect(captured.sgrSequences[0].raw).toBe("\x1b[29m");
      expect(captured.sgrSequences[0].messages).toHaveLength(1);
      expect(captured.sgrSequences[0].messages[0]._type).toBe("sgr.notStrikethrough");
    });
  });
});