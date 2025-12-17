import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../StatefulTerminal";

describe("Cursor Positioning", () => {
  it("should handle VPA (Vertical Position Absolute) - ESC[d", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Start at row 1, col 1
    expect(terminal.getSnapshot().cursorY).toBe(0);
    expect(terminal.getSnapshot().cursorX).toBe(0);
    
    // Move to column 10
    terminal.pushPtyText("\x1b[10G");
    expect(terminal.getSnapshot().cursorX).toBe(9); // 0-indexed
    expect(terminal.getSnapshot().cursorY).toBe(0);
    
    // Use VPA to move to row 11 (keeping column)
    terminal.pushPtyText("\x1b[11d");
    expect(terminal.getSnapshot().cursorY).toBe(10); // Row 11 (0-indexed)
    expect(terminal.getSnapshot().cursorX).toBe(9); // Column should be preserved
  });

  it("should handle VPA with default parameter (row 1)", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Move to row 10, col 20
    terminal.pushPtyText("\x1b[10;20H");
    expect(terminal.getSnapshot().cursorY).toBe(9);
    expect(terminal.getSnapshot().cursorX).toBe(19);
    
    // VPA with no parameter should move to row 1
    terminal.pushPtyText("\x1b[d");
    expect(terminal.getSnapshot().cursorY).toBe(0); // Row 1 (0-indexed)
    expect(terminal.getSnapshot().cursorX).toBe(19); // Column preserved
  });

  it("should clamp VPA to terminal bounds", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Move to column 50
    terminal.pushPtyText("\x1b[50G");
    expect(terminal.getSnapshot().cursorX).toBe(49);
    
    // Try to move to row 100 (beyond terminal height)
    terminal.pushPtyText("\x1b[100d");
    expect(terminal.getSnapshot().cursorY).toBe(23); // Clamped to last row (0-indexed)
    expect(terminal.getSnapshot().cursorX).toBe(49); // Column preserved
  });

  it("should handle HPA (Horizontal Position Absolute) - ESC[G", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Move to row 5
    terminal.pushPtyText("\x1b[5d");
    expect(terminal.getSnapshot().cursorY).toBe(4);
    expect(terminal.getSnapshot().cursorX).toBe(0);
    
    // Use HPA to move to column 30 (keeping row)
    terminal.pushPtyText("\x1b[30G");
    expect(terminal.getSnapshot().cursorX).toBe(29); // Column 30 (0-indexed)
    expect(terminal.getSnapshot().cursorY).toBe(4); // Row should be preserved
  });

  it("should handle VPA and HPA together for precise positioning", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Use VPA to set row 15
    terminal.pushPtyText("\x1b[15d");
    expect(terminal.getSnapshot().cursorY).toBe(14);
    
    // Use HPA to set column 40
    terminal.pushPtyText("\x1b[40G");
    expect(terminal.getSnapshot().cursorX).toBe(39);
    expect(terminal.getSnapshot().cursorY).toBe(14);
    
    // Verify we can write at this position
    terminal.pushPtyText("X");
    const snapshot = terminal.getSnapshot();
    expect(snapshot.cells[14][39].ch).toBe("X");
  });

  it("should handle erase character (ECH) - ESC[X", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Write some text
    terminal.pushPtyText("Hello World");
    expect(terminal.getSnapshot().cells[0][0].ch).toBe("H");
    expect(terminal.getSnapshot().cells[0][5].ch).toBe(" ");
    expect(terminal.getSnapshot().cells[0][6].ch).toBe("W");
    
    // Move cursor back to position 6 (the 'W')
    terminal.pushPtyText("\x1b[7G");
    expect(terminal.getSnapshot().cursorX).toBe(6);
    
    // Erase 5 characters starting from cursor position
    terminal.pushPtyText("\x1b[5X");
    
    const snapshot = terminal.getSnapshot();
    // Characters at positions 6-10 should be erased (spaces)
    expect(snapshot.cells[0][6].ch).toBe(" ");
    expect(snapshot.cells[0][7].ch).toBe(" ");
    expect(snapshot.cells[0][8].ch).toBe(" ");
    expect(snapshot.cells[0][9].ch).toBe(" ");
    expect(snapshot.cells[0][10].ch).toBe(" ");
    
    // Characters before position 6 should be unchanged
    expect(snapshot.cells[0][0].ch).toBe("H");
    expect(snapshot.cells[0][5].ch).toBe(" ");
  });

  it("should handle erase character with bounds checking", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 5 });
    
    // Write text and move to near end of line
    terminal.pushPtyText("1234567890");
    terminal.pushPtyText("\x1b[8G"); // Move to column 8
    
    // Try to erase 10 characters (should be clamped to line end)
    terminal.pushPtyText("\x1b[10X");
    
    const snapshot = terminal.getSnapshot();
    // Positions 7-9 should be erased (0-indexed)
    expect(snapshot.cells[0][7].ch).toBe(" ");
    expect(snapshot.cells[0][8].ch).toBe(" ");
    expect(snapshot.cells[0][9].ch).toBe(" ");
    
    // Positions before cursor should be unchanged
    expect(snapshot.cells[0][6].ch).toBe("7");
  });

  it("should handle scroll region (DECSTBM) - ESC[r", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 10 });
    
    // Set scroll region from row 3 to row 7 (1-indexed)
    terminal.pushPtyText("\x1b[3;7r");
    
    // Cursor should move to top of scroll region
    expect(terminal.getSnapshot().cursorY).toBe(2); // Row 3 (0-indexed)
    expect(terminal.getSnapshot().cursorX).toBe(0);
    
    // Write some text at the current position
    terminal.pushPtyText("Test");
    
    // The text should be at row 2 (top of scroll region)
    expect(terminal.getSnapshot().cells[2][0].ch).toBe("T");
    expect(terminal.getSnapshot().cells[2][1].ch).toBe("e");
    expect(terminal.getSnapshot().cells[2][2].ch).toBe("s");
    expect(terminal.getSnapshot().cells[2][3].ch).toBe("t");
  });

  it("should reset scroll region with ESC[r (no parameters)", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 10 });
    
    // Set a limited scroll region first
    terminal.pushPtyText("\x1b[3;7r");
    expect(terminal.getSnapshot().cursorY).toBe(2);
    
    // Reset to full screen
    terminal.pushPtyText("\x1b[r");
    expect(terminal.getSnapshot().cursorY).toBe(0); // Should move to top of screen
    expect(terminal.getSnapshot().cursorX).toBe(0);
  });

  it("should handle scroll up within region (SU) - ESC[S", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 10 });
    
    // Set scroll region from row 3 to row 7
    terminal.pushPtyText("\x1b[3;7r");
    
    // Write some simple content
    terminal.pushPtyText("ABC");
    
    // Move to next line in scroll region
    terminal.pushPtyText("\x1b[4;1H");
    terminal.pushPtyText("DEF");
    
    // Scroll up by 1 line
    terminal.pushPtyText("\x1b[1S");
    
    const snapshot = terminal.getSnapshot();
    
    // After scrolling up 1 line, "DEF" should move to row 2 (0-indexed)
    expect(snapshot.cells[2][0].ch).toBe("D");
    expect(snapshot.cells[2][1].ch).toBe("E");
    expect(snapshot.cells[2][2].ch).toBe("F");
    
    // Row 3 should be cleared
    expect(snapshot.cells[3][0].ch).toBe(" ");
  });
});
