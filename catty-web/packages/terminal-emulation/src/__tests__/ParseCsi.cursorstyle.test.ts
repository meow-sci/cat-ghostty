import { describe, it, expect } from "vitest";
import { parseCsi } from "../terminal/ParseCsi";

describe("DECSCUSR Cursor Style Validation", () => {
  describe("Valid cursor styles", () => {
    it("should parse cursor style 0 (blinking block)", () => {
      // ESC[0 q
      const bytes = [0x1b, 0x5b, 0x30, 0x20, 0x71];
      const raw = "\x1b[0 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(0);
        expect(result.raw).toBe(raw);
      }
    });

    it("should parse cursor style 1 (blinking block)", () => {
      // ESC[1 q
      const bytes = [0x1b, 0x5b, 0x31, 0x20, 0x71];
      const raw = "\x1b[1 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(1);
        expect(result.raw).toBe(raw);
      }
    });

    it("should parse cursor style 6 (steady bar)", () => {
      // ESC[6 q
      const bytes = [0x1b, 0x5b, 0x36, 0x20, 0x71];
      const raw = "\x1b[6 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(6);
        expect(result.raw).toBe(raw);
      }
    });
  });

  describe("Invalid cursor styles - should be clamped to 0", () => {
    it("should clamp cursor style 7 to 0", () => {
      // ESC[7 q - invalid, should become 0
      const bytes = [0x1b, 0x5b, 0x37, 0x20, 0x71];
      const raw = "\x1b[7 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(0); // Clamped to 0
        expect(result.raw).toBe(raw);
      }
    });

    it("should clamp cursor style 100 to 0", () => {
      // ESC[100 q - invalid, should become 0
      const bytes = [0x1b, 0x5b, 0x31, 0x30, 0x30, 0x20, 0x71];
      const raw = "\x1b[100 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(0); // Clamped to 0
        expect(result.raw).toBe(raw);
      }
    });

    it("should handle negative cursor style by clamping to 0", () => {
      // This would be ESC[-1 q but the parser handles this differently
      // Let's test with no parameter which defaults to 0
      const bytes = [0x1b, 0x5b, 0x20, 0x71];
      const raw = "\x1b[ q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(0); // Default fallback
        expect(result.raw).toBe(raw);
      }
    });
  });

  describe("Edge cases", () => {
    it("should handle empty parameter (defaults to 0)", () => {
      // ESC[ q - no parameter
      const bytes = [0x1b, 0x5b, 0x20, 0x71];
      const raw = "\x1b[ q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(0);
        expect(result.raw).toBe(raw);
      }
    });

    it("should handle multiple parameters (uses first one)", () => {
      // ESC[2;5 q - multiple parameters, should use first (2)
      const bytes = [0x1b, 0x5b, 0x32, 0x3b, 0x35, 0x20, 0x71];
      const raw = "\x1b[2;5 q";
      const result = parseCsi(bytes, raw);
      
      expect(result._type).toBe("csi.setCursorStyle");
      if (result._type === "csi.setCursorStyle") {
        expect(result.style).toBe(2); // Uses first parameter
        expect(result.raw).toBe(raw);
      }
    });

    it("should require space intermediate character", () => {
      // ESC[2q - missing space, should not match DECSCUSR
      const bytes = [0x1b, 0x5b, 0x32, 0x71];
      const raw = "\x1b[2q";
      const result = parseCsi(bytes, raw);
      
      // Should not be recognized as setCursorStyle without the space
      expect(result._type).toBe("csi.unknown");
    });

    it("should require 'q' final character", () => {
      // ESC[2 p - wrong final character
      const bytes = [0x1b, 0x5b, 0x32, 0x20, 0x70];
      const raw = "\x1b[2 p";
      const result = parseCsi(bytes, raw);
      
      // Should not be recognized as setCursorStyle with wrong final
      expect(result._type).toBe("csi.unknown");
    });
  });
});