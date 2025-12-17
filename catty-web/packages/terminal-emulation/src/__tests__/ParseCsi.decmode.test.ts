import { describe, it, expect } from "vitest";
import { parseCsi } from "../terminal/ParseCsi";

describe("DEC Mode Parsing and Validation", () => {
  describe("DECSET (mode set)", () => {
    it("should parse valid single DEC mode", () => {
      // ESC[?25h - show cursor
      const bytes = [0x1b, 0x5b, 0x3f, 0x32, 0x35, 0x68];
      const raw = "\x1b[?25h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([25]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should parse multiple valid DEC modes", () => {
      // ESC[?25;1049h - show cursor and alternate screen
      const bytes = [0x1b, 0x5b, 0x3f, 0x32, 0x35, 0x3b, 0x31, 0x30, 0x34, 0x39, 0x68];
      const raw = "\x1b[?25;1049h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([25, 1049]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should handle invalid parameter characters gracefully", () => {
      // ESC[?-1;25h - the '-' is an intermediate character, so this becomes ESC[?1;25h
      const bytes = [0x1b, 0x5b, 0x3f, 0x2d, 0x31, 0x3b, 0x32, 0x35, 0x68];
      const raw = "\x1b[?-1;25h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([1, 25]); // '-' is treated as intermediate, so we get 1 and 25
        expect(result.raw).toBe(raw);
      }
    });

    it("should filter out mode numbers exceeding 65535", () => {
      // ESC[?70000;25h - invalid large mode with valid mode
      const bytes = [0x1b, 0x5b, 0x3f, 0x37, 0x30, 0x30, 0x30, 0x30, 0x3b, 0x32, 0x35, 0x68];
      const raw = "\x1b[?70000;25h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([25]); // Only valid mode should remain
        expect(result.raw).toBe(raw);
      }
    });

    it("should filter out mode numbers exceeding valid range", () => {
      // ESC[?-1;70000h - '-' becomes intermediate, 70000 exceeds range
      const bytes = [0x1b, 0x5b, 0x3f, 0x2d, 0x31, 0x3b, 0x37, 0x30, 0x30, 0x30, 0x30, 0x68];
      const raw = "\x1b[?-1;70000h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([1]); // Only mode 1 is valid (70000 filtered out)
        expect(result.modes).not.toContain(70000);
        expect(result.raw).toBe(raw);
      }
    });
  });

  describe("DECRST (mode reset)", () => {
    it("should parse valid single DEC mode reset", () => {
      // ESC[?25l - hide cursor
      const bytes = [0x1b, 0x5b, 0x3f, 0x32, 0x35, 0x6c];
      const raw = "\x1b[?25l";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeReset");
      if (result._type === "csi.decModeReset") {
        expect(result.modes).toEqual([25]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should parse multiple valid DEC mode resets", () => {
      // ESC[?1000;1002l - disable mouse reporting modes
      const bytes = [0x1b, 0x5b, 0x3f, 0x31, 0x30, 0x30, 0x30, 0x3b, 0x31, 0x30, 0x30, 0x32, 0x6c];
      const raw = "\x1b[?1000;1002l";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeReset");
      if (result._type === "csi.decModeReset") {
        expect(result.modes).toEqual([1000, 1002]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should apply same validation as DECSET", () => {
      // ESC[?-5;1047;80000l - '-' becomes intermediate, 80000 exceeds range
      const bytes = [0x1b, 0x5b, 0x3f, 0x2d, 0x35, 0x3b, 0x31, 0x30, 0x34, 0x37, 0x3b, 0x38, 0x30, 0x30, 0x30, 0x30, 0x6c];
      const raw = "\x1b[?-5;1047;80000l";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeReset");
      if (result._type === "csi.decModeReset") {
        expect(result.modes).toEqual([5, 1047]); // 5 and 1047 are valid (80000 filtered out)
        expect(result.modes).not.toContain(80000);
        expect(result.raw).toBe(raw);
      }
    });
  });

  describe("Edge cases", () => {
    it("should handle zero as valid mode", () => {
      // ESC[?0h - mode 0 should be valid
      const bytes = [0x1b, 0x5b, 0x3f, 0x30, 0x68];
      const raw = "\x1b[?0h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([0]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should handle maximum valid mode (65535)", () => {
      // ESC[?65535h - maximum valid mode
      const bytes = [0x1b, 0x5b, 0x3f, 0x36, 0x35, 0x35, 0x33, 0x35, 0x68];
      const raw = "\x1b[?65535h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([65535]);
        expect(result.raw).toBe(raw);
      }
    });

    it("should handle empty mode list", () => {
      // ESC[?h - no modes specified
      const bytes = [0x1b, 0x5b, 0x3f, 0x68];
      const raw = "\x1b[?h";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.decModeSet");
      if (result._type === "csi.decModeSet") {
        expect(result.modes).toEqual([]);
        expect(result.raw).toBe(raw);
      }
    });
  });
});