import { describe, it, expect } from "vitest";
import { parseSgr } from "../terminal/ParseSgr";

describe("parseSgr", () => {
  describe("reset", () => {
    it("should parse empty params as reset", () => {
      const messages = parseSgr([], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.reset");
    });

    it("should parse [0] as reset", () => {
      const messages = parseSgr([0], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.reset");
    });
  });

  describe("text styles", () => {
    it("should parse bold (1)", () => {
      const messages = parseSgr([1], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.bold");
    });

    it("should parse faint (2)", () => {
      const messages = parseSgr([2], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.faint");
    });

    it("should parse italic (3)", () => {
      const messages = parseSgr([3], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.italic");
    });

    it("should parse underline (4)", () => {
      const messages = parseSgr([4], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.underline");
      if (messages[0]._type === "sgr.underline") {
        expect(messages[0].style).toBe("single");
      }
    });

    it("should parse underline with style 4:3 (curly)", () => {
      const messages = parseSgr([4, 3], [":"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.underline");
      if (messages[0]._type === "sgr.underline") {
        expect(messages[0].style).toBe("curly");
      }
    });

    it("should parse underline off 4:0", () => {
      const messages = parseSgr([4, 0], [":"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notUnderlined");
    });

    it("should parse inverse (7)", () => {
      const messages = parseSgr([7], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.inverse");
    });

    it("should parse strikethrough (9)", () => {
      const messages = parseSgr([9], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.strikethrough");
    });
  });

  describe("reset styles", () => {
    it("should parse normal intensity (22)", () => {
      const messages = parseSgr([22], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.normalIntensity");
    });

    it("should parse not italic (23)", () => {
      const messages = parseSgr([23], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notItalic");
    });

    it("should parse not underlined (24)", () => {
      const messages = parseSgr([24], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notUnderlined");
    });

    it("should parse not inverse (27)", () => {
      const messages = parseSgr([27], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notInverse");
    });

    it("should parse not strikethrough (29)", () => {
      const messages = parseSgr([29], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notStrikethrough");
    });
  });

  describe("standard foreground colors (30-37)", () => {
    it("should parse black foreground (30)", () => {
      const messages = parseSgr([30], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "black" });
      }
    });

    it("should parse red foreground (31)", () => {
      const messages = parseSgr([31], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "red" });
      }
    });

    it("should parse white foreground (37)", () => {
      const messages = parseSgr([37], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "white" });
      }
    });
  });

  describe("standard background colors (40-47)", () => {
    it("should parse black background (40)", () => {
      const messages = parseSgr([40], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.backgroundColor");
      if (messages[0]._type === "sgr.backgroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "black" });
      }
    });

    it("should parse red background (41)", () => {
      const messages = parseSgr([41], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.backgroundColor");
      if (messages[0]._type === "sgr.backgroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "red" });
      }
    });
  });

  describe("256-color mode", () => {
    it("should parse 256-color foreground (38;5;n)", () => {
      const messages = parseSgr([38, 5, 11], [";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "indexed", index: 11 });
      }
    });

    it("should parse 256-color background (48;5;n)", () => {
      const messages = parseSgr([48, 5, 196], [";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.backgroundColor");
      if (messages[0]._type === "sgr.backgroundColor") {
        expect(messages[0].color).toEqual({ type: "indexed", index: 196 });
      }
    });

    it("should parse 256-color with colon separator (38:5:n)", () => {
      const messages = parseSgr([38, 5, 11], [":", ":"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "indexed", index: 11 });
      }
    });
  });

  describe("true color mode (24-bit)", () => {
    it("should parse true color foreground (38;2;r;g;b)", () => {
      const messages = parseSgr([38, 2, 255, 128, 0], [";", ";", ";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "rgb", r: 255, g: 128, b: 0 });
      }
    });

    it("should parse true color background (48;2;r;g;b)", () => {
      const messages = parseSgr([48, 2, 0, 128, 255], [";", ";", ";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.backgroundColor");
      if (messages[0]._type === "sgr.backgroundColor") {
        expect(messages[0].color).toEqual({ type: "rgb", r: 0, g: 128, b: 255 });
      }
    });

    it("should parse true color with colon separator (38:2:r:g:b)", () => {
      const messages = parseSgr([38, 2, 255, 0, 0], [":", ":", ":", ":"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "rgb", r: 255, g: 0, b: 0 });
      }
    });
  });

  describe("default colors", () => {
    it("should parse default foreground (39)", () => {
      const messages = parseSgr([39], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.defaultForeground");
    });

    it("should parse default background (49)", () => {
      const messages = parseSgr([49], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.defaultBackground");
    });
  });

  describe("bright colors (90-97, 100-107)", () => {
    it("should parse bright red foreground (91)", () => {
      const messages = parseSgr([91], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      if (messages[0]._type === "sgr.foregroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "brightRed" });
      }
    });

    it("should parse bright yellow background (103)", () => {
      const messages = parseSgr([103], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.backgroundColor");
      if (messages[0]._type === "sgr.backgroundColor") {
        expect(messages[0].color).toEqual({ type: "named", color: "brightYellow" });
      }
    });
  });

  describe("underline color", () => {
    it("should parse underline color 256 (58;5;n)", () => {
      const messages = parseSgr([58, 5, 196], [";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.underlineColor");
      if (messages[0]._type === "sgr.underlineColor") {
        expect(messages[0].color).toEqual({ type: "indexed", index: 196 });
      }
    });

    it("should parse underline color RGB (58;2;r;g;b)", () => {
      const messages = parseSgr([58, 2, 255, 0, 0], [";", ";", ";", ";"]);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.underlineColor");
      if (messages[0]._type === "sgr.underlineColor") {
        expect(messages[0].color).toEqual({ type: "rgb", r: 255, g: 0, b: 0 });
      }
    });

    it("should parse default underline color (59)", () => {
      const messages = parseSgr([59], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.defaultUnderlineColor");
    });
  });

  describe("overline", () => {
    it("should parse overline (53)", () => {
      const messages = parseSgr([53], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.overlined");
    });

    it("should parse not overlined (55)", () => {
      const messages = parseSgr([55], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.notOverlined");
    });
  });

  describe("combined sequences", () => {
    it("should parse multiple attributes (1;31;47)", () => {
      const messages = parseSgr([1, 31, 47], [";", ";"]);
      expect(messages).toHaveLength(3);
      expect(messages[0]._type).toBe("sgr.bold");
      expect(messages[1]._type).toBe("sgr.foregroundColor");
      expect(messages[2]._type).toBe("sgr.backgroundColor");
    });

    it("should parse bold + bright red (1;91)", () => {
      const messages = parseSgr([1, 91], [";"]);
      expect(messages).toHaveLength(2);
      expect(messages[0]._type).toBe("sgr.bold");
      expect(messages[1]._type).toBe("sgr.foregroundColor");
      if (messages[1]._type === "sgr.foregroundColor") {
        expect(messages[1].color).toEqual({ type: "named", color: "brightRed" });
      }
    });

    it("should parse reset at end (31;0)", () => {
      const messages = parseSgr([31, 0], [";"]);
      expect(messages).toHaveLength(2);
      expect(messages[0]._type).toBe("sgr.foregroundColor");
      expect(messages[1]._type).toBe("sgr.reset");
    });

    it("should parse complex sequence (0;1;4:3;38;5;11)", () => {
      const messages = parseSgr([0, 1, 4, 3, 38, 5, 11], [";", ";", ":", ";", ";", ";"]);
      expect(messages).toHaveLength(4);
      expect(messages[0]._type).toBe("sgr.reset");
      expect(messages[1]._type).toBe("sgr.bold");
      expect(messages[2]._type).toBe("sgr.underline");
      if (messages[2]._type === "sgr.underline") {
        expect(messages[2].style).toBe("curly");
      }
      expect(messages[3]._type).toBe("sgr.foregroundColor");
      if (messages[3]._type === "sgr.foregroundColor") {
        expect(messages[3].color).toEqual({ type: "indexed", index: 11 });
      }
    });
  });

  describe("unknown parameters", () => {
    it("should emit unknown for unrecognized parameter", () => {
      const messages = parseSgr([99], []);
      expect(messages).toHaveLength(1);
      expect(messages[0]._type).toBe("sgr.unknown");
      if (messages[0]._type === "sgr.unknown") {
        expect(messages[0].params).toEqual([99]);
      }
    });
  });
});
