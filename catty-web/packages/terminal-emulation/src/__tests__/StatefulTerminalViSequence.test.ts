import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("StatefulTerminal Vi Sequence Handling", () => {
  it("should treat CSI 11M as delete lines (DL)", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });

    // Fill rows 1..23 with markers so we can see the effect.
    terminal.pushPtyText("\x1b[1;1H");
    for (let i = 1; i <= 23; i++) {
      terminal.pushPtyText(`L${i.toString().padStart(2, "0")}`);
      terminal.pushPtyText("\r\n");
    }

    // Vi Ctrl+D style: set scroll region to 1..23, home, delete 11 lines.
    terminal.pushPtyText("\x1b[1;23r");
    terminal.pushPtyText("\x1b[H");
    terminal.pushPtyText("\x1b[11M");

    const snapshot = terminal.getSnapshot();
    // After deleting 11 lines at the top, old line 12 becomes new line 1.
    const row1 = snapshot.cells[0].slice(0, 3).map(c => c.ch).join("");
    const row12 = snapshot.cells[11].slice(0, 3).map(c => c.ch).join("");
    expect(row1).toBe("L12");
    expect(row12).toBe("L23");
  });

  it("should handle multiple DL sequences without affecting cursor state", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial cursor position
    terminal.pushPtyText("\x1b[5;10H"); // Move cursor to row 5, column 10
    expect(terminal.cursorX).toBe(9); // 0-indexed
    expect(terminal.cursorY).toBe(4); // 0-indexed
    
    // Process multiple DL sequences
    terminal.pushPtyText("\x1b[11M");
    terminal.pushPtyText("\x1b[5M");
    terminal.pushPtyText("\x1b[20M");
    
    // Cursor position should remain unchanged
    expect(terminal.cursorX).toBe(9);
    expect(terminal.cursorY).toBe(4);
  });

  it("should handle vi sequences mixed with normal sequences", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Mix vi sequences with normal cursor movement
    terminal.pushPtyText("\x1b[11M");  // DL
    terminal.pushPtyText("\x1b[A");    // Cursor up
    terminal.pushPtyText("\x1b[5M");   // DL
    terminal.pushPtyText("\x1b[C");    // Cursor right
    
    // Normal sequences should work, vi sequences should be ignored
    expect(terminal.cursorX).toBe(1); // Moved right once
    expect(terminal.cursorY).toBe(0); // At top (can't go up from 0)
  });

  it("should handle edge case vi sequences", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Test edge cases
    terminal.pushPtyText("\x1b[0M");    // Zero parameter
    terminal.pushPtyText("\x1b[999M");  // Large parameter
    
    // Terminal should remain functional
    expect(terminal.cursorX).toBe(0);
    expect(terminal.cursorY).toBe(0);
  });
});