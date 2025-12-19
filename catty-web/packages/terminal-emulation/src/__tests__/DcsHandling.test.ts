import { describe, it, expect, vi } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("DCS Handling", () => {
  it("should handle DECRQSS for SGR (valid)", () => {
    const onResponse = vi.fn();
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
      onResponse,
    });

    // Request SGR status: DCS $ q m ST
    // \x1bP $ q m \x1b\\
    terminal.pushPtyText("\x1bP$qm\x1b\\");

    // Expect response: DCS 1 $ r 0 m ST (1=valid, 0=reset)
    // Wait, my implementation returns "0" as first part of SGR.
    // And "1" as status (valid).
    // So \x1bP 1 $ r 0 m \x1b\\
    
    expect(onResponse).toHaveBeenCalledWith("\x1bP1$r0m\x1b\\");
  });

  it("should handle DECRQSS for invalid request", () => {
    const onResponse = vi.fn();
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
      onResponse,
    });

    // Request unknown: DCS $ q z ST
    terminal.pushPtyText("\x1bP$qz\x1b\\");

    // Expect response: DCS 0 $ r z ST (0=invalid)
    expect(onResponse).toHaveBeenCalledWith("\x1bP0$rz\x1b\\");
  });

  it("should handle DECRQSS for DECSTBM", () => {
    const onResponse = vi.fn();
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
      onResponse,
    });

    // Request DECSTBM: DCS $ q r ST
    terminal.pushPtyText("\x1bP$qr\x1b\\");

    // Expect response: DCS 1 $ r 1;24 r ST
    // Top=1, Bottom=24 (default)
    expect(onResponse).toHaveBeenCalledWith("\x1bP1$r1;24r\x1b\\");
  });
});
