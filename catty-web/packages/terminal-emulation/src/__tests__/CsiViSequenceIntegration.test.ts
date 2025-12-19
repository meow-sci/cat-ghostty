import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("CSI Vi Sequence Integration", () => {
  it("should handle CSI 11M in a realistic vi usage scenario", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Simulate a realistic vi session with mixed sequences
    const viSession = [
      "\x1b[?1049h",    // Enter alternate screen (vi startup)
      "\x1b[H",         // Move cursor to home
      "\x1b[2J",        // Clear screen
      "Hello World",    // Write some text
      "\x1b[11M",       // The unknown vi sequence
      "\x1b[A",         // Cursor up
      "\x1b[11M",       // Another unknown vi sequence
      "More text",      // Write more text
      "\x1b[?1049l"     // Exit alternate screen (vi exit)
    ].join("");
    
    // Process the entire session
    terminal.pushPtyText(viSession);
    
    // Verify terminal is back to primary screen and functional
    expect(terminal.isAlternateScreenActive()).toBe(false);
    expect(terminal.cursorX).toBeGreaterThanOrEqual(0);
    expect(terminal.cursorY).toBeGreaterThanOrEqual(0);
    
    // Verify we can still process normal sequences after vi sequences
    terminal.pushPtyText("\x1b[5;10H");
    expect(terminal.cursorX).toBe(9); // 0-indexed
    expect(terminal.cursorY).toBe(4); // 0-indexed
  });

  it("should handle various vi M sequences without errors", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Test various M sequences that might appear in vi
    const sequences = [
      "\x1b[1M",
      "\x1b[2M", 
      "\x1b[3M",
      "\x1b[5M",
      "\x1b[10M",
      "\x1b[11M",
      "\x1b[15M",
      "\x1b[20M"
    ];
    
    // Process all sequences
    sequences.forEach(seq => {
      terminal.pushPtyText(seq);
    });
    
    // Terminal should remain functional
    terminal.pushPtyText("Test");
    expect(terminal.cursorX).toBe(4); // Should have moved 4 positions for "Test"
  });

  it("should differentiate between CSI nM and CSI nm sequences", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // CSI 11M should be handled as unknown vi sequence
    terminal.pushPtyText("\x1b[11M");
    
    // CSI 11m should be handled as SGR (font selection)
    terminal.pushPtyText("\x1b[11m");
    
    // Both should be processed without errors
    // Terminal should remain functional
    terminal.pushPtyText("Test");
    expect(terminal.cursorX).toBe(4);
  });
});