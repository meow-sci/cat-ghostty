import { describe, it, expect, vi } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("StatefulTerminal Vi Sequence Handling", () => {
  it("should handle CSI 11M sequence gracefully", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Mock the log to capture debug messages
    const debugSpy = vi.spyOn(terminal['log'], 'debug');
    const isLevelEnabledSpy = vi.spyOn(terminal['log'], 'isLevelEnabled').mockReturnValue(true);
    
    // Process CSI 11M sequence
    const input = "\x1b[11M";
    terminal.pushPtyText(input);
    
    // Verify that the sequence was handled without errors
    // The terminal should continue to function normally
    expect(terminal.cursorX).toBe(0);
    expect(terminal.cursorY).toBe(0);
    
    // Verify debug logging was called
    expect(debugSpy).toHaveBeenCalledWith("Unknown vi sequence received: CSI 11M");
    
    // Clean up spies
    debugSpy.mockRestore();
    isLevelEnabledSpy.mockRestore();
  });

  it("should handle multiple vi sequences without affecting terminal state", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial cursor position
    terminal.pushPtyText("\x1b[5;10H"); // Move cursor to row 5, column 10
    expect(terminal.cursorX).toBe(9); // 0-indexed
    expect(terminal.cursorY).toBe(4); // 0-indexed
    
    // Process multiple vi sequences
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
    terminal.pushPtyText("\x1b[11M");  // Vi sequence
    terminal.pushPtyText("\x1b[A");    // Cursor up
    terminal.pushPtyText("\x1b[5M");   // Vi sequence
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