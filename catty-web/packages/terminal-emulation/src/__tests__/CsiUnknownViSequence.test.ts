import { describe, it, expect } from "vitest";
import { parseCsi } from "../terminal/ParseCsi";

describe("CSI Unknown Vi Sequence Parsing", () => {
  it("should parse CSI 11M as unknown vi sequence", () => {
    // CSI 11M sequence: ESC [ 1 1 M
    const bytes = [0x1b, 0x5b, 0x31, 0x31, 0x4d]; // ESC [ 1 1 M
    const raw = "\x1b[11M";
    
    const result = parseCsi(bytes, raw);
    
    expect(result._type).toBe("csi.unknownViSequence");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
    
    if (result._type === "csi.unknownViSequence") {
      expect(result.sequenceNumber).toBe(11);
    }
  });

  it("should parse CSI 5M as unknown vi sequence", () => {
    // CSI 5M sequence: ESC [ 5 M
    const bytes = [0x1b, 0x5b, 0x35, 0x4d]; // ESC [ 5 M
    const raw = "\x1b[5M";
    
    const result = parseCsi(bytes, raw);
    
    expect(result._type).toBe("csi.unknownViSequence");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
    
    if (result._type === "csi.unknownViSequence") {
      expect(result.sequenceNumber).toBe(5);
    }
  });

  it("should handle CSI M without parameters as unknown", () => {
    // CSI M sequence without parameters: ESC [ M
    const bytes = [0x1b, 0x5b, 0x4d]; // ESC [ M
    const raw = "\x1b[M";
    
    const result = parseCsi(bytes, raw);
    
    // Should fall back to unknown since no parameters
    expect(result._type).toBe("csi.unknown");
    expect(result.raw).toBe(raw);
    expect(result.implemented).toBe(false);
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