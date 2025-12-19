import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("Less Screen Repaint Issue", () => {
  it("should correctly handle line feeds at the bottom of the screen", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Switch to alternate screen buffer (like less does)
    terminal.pushPtyText("\x1b[?1049h");
    expect(terminal.isAlternateScreenActive()).toBe(true);
    
    // Move to line 24 (last line)
    terminal.pushPtyText("\x1b[24;1H");
    
    let snapshot = terminal.getSnapshot();
    expect(snapshot.cursorY).toBe(23); // 0-indexed
    
    // Write content and do line feed
    terminal.pushPtyText("test");
    snapshot = terminal.getSnapshot();
    expect(snapshot.cells[23].slice(0, 4).map(c => c.ch).join("")).toBe("test");
    
    // Line feed should scroll the content up
    terminal.pushPtyText("\n");
    snapshot = terminal.getSnapshot();
    
    // After scrolling, "test" should have moved to line 23
    expect(snapshot.cells[22].slice(0, 4).map(c => c.ch).join("")).toBe("test");
    // Line 24 should be cleared
    expect(snapshot.cells[23].slice(0, 4).map(c => c.ch).join("").trim()).toBe("");
    // Cursor should be at line 24 (ready for new content)
    expect(snapshot.cursorY).toBe(23);
  });

  it("should support less-style back-page repaint using ESC[H + ESC M", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Switch to alternate screen
    terminal.pushPtyText("\x1b[?1049h");
    
    // This mimics a common less strategy when paging backwards:
    // repeatedly insert a line at the top using RI (ESC M) and then print the new line.
    // If RI is missing, all lines overwrite row 1 and the old page remains on screen.

    for (let n = 23; n >= 1; n--) {
      terminal.pushPtyText("\x1b[H");     // home
      terminal.pushPtyText("\x1bM");     // RI: scroll down within region
      terminal.pushPtyText(`line ${n}`);
      terminal.pushPtyText("\r\n");
    }

    const snapshot = terminal.getSnapshot();
    expect(snapshot.cells[0].slice(0, 6).map(c => c.ch).join("")).toBe("line 1");
    expect(snapshot.cells[1].slice(0, 6).map(c => c.ch).join("")).toBe("line 2");
    expect(snapshot.cells[22].slice(0, 7).map(c => c.ch).join("")).toBe("line 23");
  });

  it("should show correct behavior without the problematic ESC[H", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Switch to alternate screen
    terminal.pushPtyText("\x1b[?1049h");
    
    // Move to home position
    terminal.pushPtyText("\x1b[H");
    
    // Write lines without the problematic ESC[H
    terminal.pushPtyText("line 1\r\n");
    terminal.pushPtyText("line 2\r\n");
    terminal.pushPtyText("line 3\r\n");
    
    const snapshot = terminal.getSnapshot();
    
    // All lines should be preserved
    expect(snapshot.cells[0].slice(0, 6).map(c => c.ch).join("")).toBe("line 1");
    expect(snapshot.cells[1].slice(0, 6).map(c => c.ch).join("")).toBe("line 2");
    expect(snapshot.cells[2].slice(0, 6).map(c => c.ch).join("")).toBe("line 3");
    expect(snapshot.cursorY).toBe(3); // Cursor on line 4
  });
});