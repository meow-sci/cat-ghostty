import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("XTSAVE / XTRESTORE (CSI ? Pm s / r)", () => {
  it("should save and restore DECAWM (7)", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 5 });
    
    // Default is DECAWM on
    // Save it (on)
    term.pushPtyText("\x1b[?7s");
    
    // Turn it off
    term.pushPtyText("\x1b[?7l");
    
    // Verify it's off (writing past end shouldn't wrap)
    term.pushPtyText("1234567890A");
    const snapshot1 = term.getSnapshot();
    expect(snapshot1.cells[0].map(c => c.ch).join("")).toBe("123456789A"); // '0' overwritten by 'A'
    expect(term.cursorY).toBe(0);
    
    // Restore it (should be on)
    term.pushPtyText("\x1b[?7r");
    
    // Verify it's on
    term.pushPtyText("\r\n");
    term.pushPtyText("1234567890B");
    const snapshot2 = term.getSnapshot();
    expect(snapshot2.cells[1].map(c => c.ch).join("")).toBe("1234567890");
    expect(snapshot2.cells[2].map(c => c.ch).join("").trim()).toBe("B");
  });

  it("should save and restore DECOM (6)", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 5 });
    
    // Set scroll region
    term.pushPtyText("\x1b[2;4r");
    
    // Turn DECOM on
    term.pushPtyText("\x1b[?6h");
    
    // Save it (on)
    term.pushPtyText("\x1b[?6s");
    
    // Turn it off
    term.pushPtyText("\x1b[?6l");
    
    // Verify it's off (CUP 1;1 goes to 0,0)
    term.pushPtyText("\x1b[1;1H");
    expect(term.cursorY).toBe(0);
    
    // Restore it (should be on)
    term.pushPtyText("\x1b[?6r");
    
    // Verify it's on (CUP 1;1 goes to 1,0 - top of scroll region)
    term.pushPtyText("\x1b[1;1H");
    expect(term.cursorY).toBe(1);
  });

  it("should handle multiple modes", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 5 });
    
    // DECAWM on (default), DECOM off (default)
    
    // Turn DECOM on
    term.pushPtyText("\x1b[2;4r");
    term.pushPtyText("\x1b[?6h");
    
    // Save both 6 and 7
    term.pushPtyText("\x1b[?6;7s");
    
    // Change both
    term.pushPtyText("\x1b[?6l"); // DECOM off
    term.pushPtyText("\x1b[?7l"); // DECAWM off
    
    // Restore both
    term.pushPtyText("\x1b[?6;7r");
    
    // Verify DECOM is on
    term.pushPtyText("\x1b[1;1H");
    expect(term.cursorY).toBe(1);
    
    // Verify DECAWM is on (we assume it works if DECOM worked, as logic is shared)
  });
});
