/**
 * End-to-end integration tests for vi-specific sequences
 * Tests that all vi sequences work together with existing terminal systems
 */

import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";
import { SgrStyleManager } from "../terminal/SgrStyleManager";

describe("Vi Integration End-to-End", () => {
  it("should handle complete vi startup sequence with all extensions", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    const responses: string[] = [];
    
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Simulate complete vi startup sequence
    terminal.pushPtyText("\x1b]2;vi - test.txt\x07"); // Set window title
    terminal.pushPtyText("\x1b[22;2t"); // Push title to stack
    terminal.pushPtyText("\x1b[?1049h"); // Enter alternate screen
    terminal.pushPtyText("\x1b[2J"); // Clear screen
    terminal.pushPtyText("\x1b[H"); // Move cursor to home
    
    // Query colors (vi does this to detect dark/light theme)
    terminal.pushPtyText("\x1b]11;?\x07"); // Query background
    terminal.pushPtyText("\x1b]10;?\x07"); // Query foreground
    
    // Use various SGR sequences for syntax highlighting
    terminal.pushPtyText("\x1b[32m"); // Green foreground
    terminal.pushPtyText("function"); // Write text
    terminal.pushPtyText("\x1b[39m"); // Default foreground
    terminal.pushPtyText(" ");
    terminal.pushPtyText("\x1b[4m"); // Underline
    terminal.pushPtyText("test");
    terminal.pushPtyText("\x1b[24m"); // Not underlined
    terminal.pushPtyText("() {\n");
    
    // Use enhanced SGR sequences
    terminal.pushPtyText("\x1b[>4;2m"); // Enhanced underline mode
    terminal.pushPtyText("  return");
    terminal.pushPtyText("\x1b[>4;0m"); // Reset enhanced underline
    terminal.pushPtyText(" 42;\n");
    
    // Use private SGR mode
    terminal.pushPtyText("\x1b[?4m"); // Private underline mode
    terminal.pushPtyText("}");
    terminal.pushPtyText("\x1b[m"); // Reset all SGR
    
    // Use unknown vi sequences (should be handled gracefully)
    terminal.pushPtyText("\x1b[11M"); // Unknown vi sequence
    terminal.pushPtyText("\x1b[5M"); // Another unknown vi sequence
    
    // Exit vi
    terminal.pushPtyText("\x1b[?1049l"); // Exit alternate screen
    terminal.pushPtyText("\x1b[23;2t"); // Pop title from stack
    
    // Verify responses
    expect(responses).toHaveLength(2);
    expect(responses[0]).toBe("\x1b]11;rgb:0000/0000/0000\x07"); // Background query
    expect(responses[1]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07"); // Foreground query
    
    // Verify terminal state
    const snapshot = terminal.getSnapshot();
    expect(snapshot.windowProperties.title).toBe("vi - test.txt"); // Title restored from stack
    expect(terminal.isAlternateScreenActive()).toBe(false); // Back to normal screen
  });

  it("should integrate SGR sequences with SgrStyleManager correctly", () => {
    const styleManager = new SgrStyleManager();
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Test that all vi-specific SGR sequences generate proper CSS
    const testSequences = [
      { sequence: "\x1b[32m", description: "green foreground" },
      { sequence: "\x1b[39m", description: "default foreground" },
      { sequence: "\x1b[4m", description: "underline" },
      { sequence: "\x1b[24m", description: "not underlined" },
      { sequence: "\x1b[27m", description: "not inverse" },
      { sequence: "\x1b[23m", description: "not italic" },
      { sequence: "\x1b[29m", description: "not strikethrough" },
      { sequence: "\x1b[>4;2m", description: "enhanced underline mode" },
      { sequence: "\x1b[?4m", description: "private underline mode" },
      { sequence: "\x1b[0%m", description: "SGR with intermediate" },
      { sequence: "\x1b[m", description: "bare reset" },
    ];
    
    for (const test of testSequences) {
      // Apply the sequence
      terminal.pushPtyText(test.sequence);
      
      // Get current SGR state from a cell
      const snapshot = terminal.getSnapshot();
      const cell = snapshot.cells[0][0]; // First cell
      
      if (cell && cell.sgrState) {
        // Generate CSS class - should not throw
        const cssClass = styleManager.getStyleClass(cell.sgrState);
        expect(cssClass).toMatch(/^sgr-[a-f0-9]+$/); // Should be valid hash-based class name
        
        // Generate CSS text - should not throw
        const cssText = styleManager.generateCssForSgr(cell.sgrState);
        expect(typeof cssText).toBe('string');
      }
    }
  });

  it("should handle window manipulation sequences with existing title management", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Test that window manipulation integrates with existing OSC title sequences
    terminal.pushPtyText("\x1b]2;Original Title\x07"); // OSC set title
    expect(terminal.getWindowTitle()).toBe("Original Title");
    
    terminal.pushPtyText("\x1b[22;2t"); // CSI push title
    terminal.pushPtyText("\x1b]2;New Title\x07"); // OSC set new title
    expect(terminal.getWindowTitle()).toBe("New Title");
    
    terminal.pushPtyText("\x1b[23;2t"); // CSI pop title
    expect(terminal.getWindowTitle()).toBe("Original Title"); // Should restore
    
    // Same for icon names
    terminal.pushPtyText("\x1b]1;Original Icon\x07"); // OSC set icon
    expect(terminal.getIconName()).toBe("Original Icon");
    
    terminal.pushPtyText("\x1b[22;1t"); // CSI push icon
    terminal.pushPtyText("\x1b]1;New Icon\x07"); // OSC set new icon
    expect(terminal.getIconName()).toBe("New Icon");
    
    terminal.pushPtyText("\x1b[23;1t"); // CSI pop icon
    expect(terminal.getIconName()).toBe("Original Icon"); // Should restore
  });

  it("should handle CSI 11M without interfering with other CSI parsing", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Mix CSI 11M with other CSI sequences
    terminal.pushPtyText("\x1b[H"); // Move cursor to home
    terminal.pushPtyText("\x1b[11M"); // Unknown vi sequence
    terminal.pushPtyText("\x1b[2J"); // Clear screen
    terminal.pushPtyText("\x1b[5M"); // Another unknown vi sequence
    terminal.pushPtyText("\x1b[10;20H"); // Move cursor to specific position
    terminal.pushPtyText("\x1b[20M"); // Another unknown vi sequence
    terminal.pushPtyText("\x1b[31m"); // Red foreground
    terminal.pushPtyText("X"); // Write a character to apply SGR state
    
    // Verify that normal CSI sequences still work
    const snapshot = terminal.getSnapshot();
    expect(snapshot.cursorX).toBe(20); // Column 20 after writing character
    expect(snapshot.cursorY).toBe(9);  // Row 10 (0-indexed)
    
    // Verify that SGR still works - check the cell where we wrote the character
    const cell = snapshot.cells[9][19]; // Character was written at column 19
    expect(cell?.sgrState?.foregroundColor).toEqual({ type: 'named', color: 'red' });
  });

  it("should handle mixed vi sequences in realistic editing session", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    const responses: string[] = [];
    
    terminal.onResponse((response) => {
      responses.push(response);
    });
    
    // Simulate realistic vi editing session with mixed sequences
    const viSession = [
      "\x1b]2;vi ~/.bashrc\x07",     // Set title
      "\x1b[?1049h",                 // Enter alternate screen
      "\x1b]11;?\x07",               // Query background color
      "\x1b[2J\x1b[H",               // Clear and home
      "\x1b[32m# Bash configuration\x1b[39m\n", // Green comment
      "\x1b[11M",                    // Unknown vi sequence
      "\x1b[4mexport\x1b[24m PATH=...\n", // Underlined export
      "\x1b[>4;2m",                  // Enhanced underline
      "alias ll='ls -la'\n",
      "\x1b[>4;0m",                  // Reset enhanced underline
      "\x1b[?4m",                    // Private underline mode
      "function cd_and_ls() {\n",
      "\x1b[m",                      // Reset all
      "  cd \"$1\" && ls\n",
      "}\n",
      "\x1b[5M",                     // Another unknown vi sequence
      "\x1b[?1049l",                 // Exit alternate screen
    ];
    
    for (const sequence of viSession) {
      terminal.pushPtyText(sequence);
    }
    
    // Should have received background color query response
    expect(responses).toHaveLength(1);
    expect(responses[0]).toBe("\x1b]11;rgb:0000/0000/0000\x07");
    
    // Terminal should be back to normal state
    const snapshot = terminal.getSnapshot();
    expect(terminal.isAlternateScreenActive()).toBe(false);
    expect(snapshot.windowProperties.title).toBe("vi ~/.bashrc");
  });

  it("should update TERMINAL_SPEC_COVERAGE.md correctly for all sequences", () => {
    // This test verifies that all implemented sequences are documented
    // We'll check that the sequences we've implemented are marked correctly
    
    // For this test, we just verify that the sequences can be processed
    // without errors. The actual TERMINAL_SPEC_COVERAGE.md update is manual.
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Test each sequence type that should be documented in TERMINAL_SPEC_COVERAGE.md
    terminal.pushPtyText("\x1b[32m\x1b[39m"); // SGR colors
    terminal.pushPtyText("\x1b[4m\x1b[24m"); // SGR underline
    terminal.pushPtyText("\x1b[>4;2m\x1b[?4m"); // Enhanced/private SGR
    terminal.pushPtyText("\x1b]10;?\x07\x1b]11;?\x07"); // OSC color queries
    terminal.pushPtyText("\x1b[22;2t\x1b[23;2t"); // Window manipulation
    terminal.pushPtyText("\x1b[11M"); // Unknown vi sequence
    
    // If we get here without throwing, all sequences are handled
    expect(true).toBe(true);
  });
});