import { describe, it, expect } from "vitest";
import { parseCsi } from "../terminal/ParseCsi";

describe("CSI Delete/Insert Line Parsing", () => {
  it("should parse CSI 11M as delete lines", () => {
    const bytes = [0x1b, 0x5b, 0x31, 0x31, 0x4d]; // ESC [ 1 1 M
    const raw = "\x1b[11M";

    const result = parseCsi(bytes, raw);

    expect(result._type).toBe("csi.deleteLines");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(true);

    if (result._type === "csi.deleteLines") {
      expect(result.count).toBe(11);
    }
  });

  it("should parse CSI M without parameters as delete 1 line", () => {
    const bytes = [0x1b, 0x5b, 0x4d]; // ESC [ M
    const raw = "\x1b[M";

    const result = parseCsi(bytes, raw);

    expect(result._type).toBe("csi.deleteLines");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(true);

    if (result._type === "csi.deleteLines") {
      expect(result.count).toBe(1);
    }
  });

  it("should parse CSI 5L as insert lines", () => {
    const bytes = [0x1b, 0x5b, 0x35, 0x4c]; // ESC [ 5 L
    const raw = "\x1b[5L";

    const result = parseCsi(bytes, raw);

    expect(result._type).toBe("csi.insertLines");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(true);

    if (result._type === "csi.insertLines") {
      expect(result.count).toBe(5);
    }
  });

  it("should handle CSI with multiple parameters and M as unknown", () => {
    // CSI 11;5M sequence: ESC [ 1 1 ; 5 M
    const bytes = [0x1b, 0x5b, 0x31, 0x31, 0x3b, 0x35, 0x4d]; // ESC [ 1 1 ; 5 M
    const raw = "\x1b[11;5M";
    
    const result = parseCsi(bytes, raw);
    
    // Should fall back to unknown since multiple parameters
    expect(result._type).toBe("csi.unknown");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
  });

  it("should handle CSI with private prefix and M as unknown", () => {
    // CSI ?11M sequence: ESC [ ? 1 1 M
    const bytes = [0x1b, 0x5b, 0x3f, 0x31, 0x31, 0x4d]; // ESC [ ? 1 1 M
    const raw = "\x1b[?11M";
    
    const result = parseCsi(bytes, raw);
    
    // Should fall back to unknown since private prefix
    expect(result._type).toBe("csi.unknown");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
  });

  it("should handle negative parameters gracefully", () => {
    // This would be malformed, but let's test graceful handling
    const bytes = [0x1b, 0x5b, 0x2d, 0x31, 0x4d]; // ESC [ - 1 M
    const raw = "\x1b[-1M";
    
    const result = parseCsi(bytes, raw);
    
    // Should fall back to unknown since negative parameter
    expect(result._type).toBe("csi.unknown");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
  });
});