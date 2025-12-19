import { describe, it, expect } from "vitest";
import { Parser } from "../terminal/Parser";
import type { CsiMessage, SgrSequence, EscMessage, XtermOscMessage } from "../terminal/TerminalEmulationTypes";
import { getLogger } from "@catty/log";

describe("Implemented Field", () => {
  it("should mark implemented CSI commands correctly", () => {
    const capturedCsi: CsiMessage[] = [];
    
    const parser = new Parser({
      handlers: {
        handleBell: () => {},
        handleBackspace: () => {},
        handleTab: () => {},
        handleShiftIn: () => {},
        handleShiftOut: () => {},
        handleLineFeed: () => {},
        handleFormFeed: () => {},
        handleCarriageReturn: () => {},
        handleNormalByte: () => {},
        handleEsc: () => {},
        handleCsi: (msg: CsiMessage) => { capturedCsi.push(msg); },
        handleOsc: () => {},
        handleSgr: () => {},
        handleXtermOsc: () => {},
        handleDcs: () => {},
      },
      log: getLogger(),
    });

    // Test implemented CSI command (cursor up)
    parser.pushBytes(new TextEncoder().encode("\x1b[5A"));
    expect(capturedCsi).toHaveLength(1);
    expect(capturedCsi[0]._type).toBe("csi.cursorUp");
    expect(capturedCsi[0].implemented).toBe(true);

    // Test implemented CSI command (scroll down)
    capturedCsi.length = 0;
    parser.pushBytes(new TextEncoder().encode("\x1b[3T"));
    expect(capturedCsi).toHaveLength(1);
    expect(capturedCsi[0]._type).toBe("csi.scrollDown");
    expect(capturedCsi[0].implemented).toBe(true);

    // Test unknown CSI command
    capturedCsi.length = 0;
    parser.pushBytes(new TextEncoder().encode("\x1b[99Z"));
    expect(capturedCsi).toHaveLength(1);
    expect(capturedCsi[0]._type).toBe("csi.unknown");
    expect(capturedCsi[0].implemented).toBe(false);
  });

  it("should mark SGR commands as implemented", () => {
    const capturedSgr: SgrSequence[] = [];
    
    const parser = new Parser({
      handlers: {
        handleBell: () => {},
        handleBackspace: () => {},
        handleTab: () => {},
        handleShiftIn: () => {},
        handleShiftOut: () => {},
        handleLineFeed: () => {},
        handleFormFeed: () => {},
        handleCarriageReturn: () => {},
        handleNormalByte: () => {},
        handleEsc: () => {},
        handleCsi: () => {},
        handleOsc: () => {},
        handleSgr: (msg: SgrSequence) => { capturedSgr.push(msg); },
        handleXtermOsc: () => {},
        handleDcs: () => {},
      },
      log: getLogger(),
    });

    // Test SGR bold command
    parser.pushBytes(new TextEncoder().encode("\x1b[1m"));
    expect(capturedSgr).toHaveLength(1);
    expect(capturedSgr[0].implemented).toBe(true);
    expect(capturedSgr[0].messages).toHaveLength(1);
    expect(capturedSgr[0].messages[0]._type).toBe("sgr.bold");
    expect(capturedSgr[0].messages[0].implemented).toBe(true);

    // Test SGR reset command
    capturedSgr.length = 0;
    parser.pushBytes(new TextEncoder().encode("\x1b[0m"));
    expect(capturedSgr).toHaveLength(1);
    expect(capturedSgr[0].implemented).toBe(true);
    expect(capturedSgr[0].messages).toHaveLength(1);
    expect(capturedSgr[0].messages[0]._type).toBe("sgr.reset");
    expect(capturedSgr[0].messages[0].implemented).toBe(true);
  });

  it("should mark ESC commands as implemented", () => {
    const capturedEsc: EscMessage[] = [];
    
    const parser = new Parser({
      handlers: {
        handleBell: () => {},
        handleBackspace: () => {},
        handleTab: () => {},
        handleShiftIn: () => {},
        handleShiftOut: () => {},
        handleLineFeed: () => {},
        handleFormFeed: () => {},
        handleCarriageReturn: () => {},
        handleNormalByte: () => {},
        handleEsc: (msg: EscMessage) => { capturedEsc.push(msg); },
        handleCsi: () => {},
        handleOsc: () => {},
        handleSgr: () => {},
        handleXtermOsc: () => {},
        handleDcs: () => {},
      },
      log: getLogger(),
    });

    // Test ESC save cursor
    parser.pushBytes(new TextEncoder().encode("\x1b7"));
    expect(capturedEsc).toHaveLength(1);
    expect(capturedEsc[0]._type).toBe("esc.saveCursor");
    expect(capturedEsc[0].implemented).toBe(true);

    // Test ESC restore cursor
    capturedEsc.length = 0;
    parser.pushBytes(new TextEncoder().encode("\x1b8"));
    expect(capturedEsc).toHaveLength(1);
    expect(capturedEsc[0]._type).toBe("esc.restoreCursor");
    expect(capturedEsc[0].implemented).toBe(true);
  });

  it("should mark OSC commands as implemented", () => {
    const capturedXtermOsc: XtermOscMessage[] = [];
    
    const parser = new Parser({
      handlers: {
        handleBell: () => {},
        handleBackspace: () => {},
        handleTab: () => {},
        handleShiftIn: () => {},
        handleShiftOut: () => {},
        handleLineFeed: () => {},
        handleFormFeed: () => {},
        handleCarriageReturn: () => {},
        handleNormalByte: () => {},
        handleEsc: () => {},
        handleCsi: () => {},
        handleOsc: () => {},
        handleSgr: () => {},
        handleXtermOsc: (msg: XtermOscMessage) => { capturedXtermOsc.push(msg); },
        handleDcs: () => {},
      },
      log: getLogger(),
    });

    // Test OSC set window title
    parser.pushBytes(new TextEncoder().encode("\x1b]2;Test Title\x07"));
    expect(capturedXtermOsc).toHaveLength(1);
    expect(capturedXtermOsc[0]._type).toBe("osc.setWindowTitle");
    expect(capturedXtermOsc[0].implemented).toBe(true);
  });
});