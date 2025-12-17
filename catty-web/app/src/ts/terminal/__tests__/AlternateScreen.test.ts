import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../StatefulTerminal";

describe("Alternate Screen Buffer", () => {
  it("should switch to alternate screen and back", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Write some text to primary screen
    terminal.pushPtyText("Primary screen content");
    const primarySnapshot = terminal.getSnapshot();
    expect(primarySnapshot.cells[0][0].ch).toBe("P");
    
    // Switch to alternate screen (DECSET 47)
    terminal.pushPtyText("\x1b[?47h");
    expect(terminal.isAlternateScreenActive()).toBe(true);
    
    // Write to alternate screen
    terminal.pushPtyText("Alternate screen");
    const alternateSnapshot = terminal.getSnapshot();
    expect(alternateSnapshot.cells[0][0].ch).toBe("A");
    
    // Switch back to primary screen (DECRST 47)
    terminal.pushPtyText("\x1b[?47l");
    expect(terminal.isAlternateScreenActive()).toBe(false);
    
    // Verify primary screen content is preserved
    const restoredSnapshot = terminal.getSnapshot();
    expect(restoredSnapshot.cells[0][0].ch).toBe("P");
  });

  it("should save and restore cursor with DECSET 1047", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Move cursor and write text
    terminal.pushPtyText("\x1b[5;10H"); // Move to row 5, col 10
    terminal.pushPtyText("Test");
    
    const beforeSwitch = terminal.getSnapshot();
    expect(beforeSwitch.cursorX).toBe(13); // After "Test" (10 + 4 - 1)
    expect(beforeSwitch.cursorY).toBe(4); // Row 5 (0-indexed)
    
    // Switch to alternate with cursor save (DECSET 1047)
    terminal.pushPtyText("\x1b[?1047h");
    expect(terminal.isAlternateScreenActive()).toBe(true);
    
    // Move cursor in alternate screen
    terminal.pushPtyText("\x1b[1;1H");
    terminal.pushPtyText("Alt");
    
    // Switch back with cursor restore (DECRST 1047)
    terminal.pushPtyText("\x1b[?1047l");
    expect(terminal.isAlternateScreenActive()).toBe(false);
    
    // Cursor should be restored
    const afterSwitch = terminal.getSnapshot();
    expect(afterSwitch.cursorX).toBe(13);
    expect(afterSwitch.cursorY).toBe(4);
  });

  it("should clear alternate screen with DECSET 1049", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Write to primary screen
    terminal.pushPtyText("Primary");
    
    // Switch to alternate with cursor save and clear (DECSET 1049)
    terminal.pushPtyText("\x1b[?1049h");
    expect(terminal.isAlternateScreenActive()).toBe(true);
    
    // Alternate screen should be clear and cursor at origin
    const alternateSnapshot = terminal.getSnapshot();
    expect(alternateSnapshot.cursorX).toBe(0);
    expect(alternateSnapshot.cursorY).toBe(0);
    expect(alternateSnapshot.cells[0][0].ch).toBe(" ");
    
    // Write to alternate screen
    terminal.pushPtyText("Alternate");
    
    // Switch back (DECRST 1049)
    terminal.pushPtyText("\x1b[?1049l");
    expect(terminal.isAlternateScreenActive()).toBe(false);
    
    // Primary screen content should be preserved
    const primarySnapshot = terminal.getSnapshot();
    expect(primarySnapshot.cells[0][0].ch).toBe("P");
  });

  it("should preserve buffer content independently", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Write to primary
    terminal.pushPtyText("Line 1 Primary\r\n");
    terminal.pushPtyText("Line 2 Primary");
    
    // Switch to alternate
    terminal.pushPtyText("\x1b[?47h");
    terminal.pushPtyText("Line 1 Alternate\r\n");
    terminal.pushPtyText("Line 2 Alternate");
    
    const alternateSnapshot = terminal.getSnapshot();
    expect(alternateSnapshot.cells[0][0].ch).toBe("L");
    expect(alternateSnapshot.cells[1][0].ch).toBe("L");
    
    // Switch back to primary
    terminal.pushPtyText("\x1b[?47l");
    
    const primarySnapshot = terminal.getSnapshot();
    expect(primarySnapshot.cells[0][0].ch).toBe("L");
    expect(primarySnapshot.cells[1][0].ch).toBe("L");
    
    // Verify the content is different
    const primaryLine1 = primarySnapshot.cells[0].map(c => c.ch).join("").trim();
    expect(primaryLine1).toBe("Line 1 Primary");
    
    // Switch back to alternate to verify its content
    terminal.pushPtyText("\x1b[?47h");
    const alternateAgain = terminal.getSnapshot();
    const alternateLine1 = alternateAgain.cells[0].map(c => c.ch).join("").trim();
    expect(alternateLine1).toBe("Line 1 Alternate");
  });
});
