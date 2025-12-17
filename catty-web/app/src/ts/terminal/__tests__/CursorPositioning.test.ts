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
});
