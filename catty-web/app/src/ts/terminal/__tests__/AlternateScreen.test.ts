import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../StatefulTerminal";
import { fc } from "./setup";

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

  /**
   * **Feature: xterm-extensions, Property 5: Alternate screen buffer switching**
   * **Validates: Requirements 2.1**
   * 
   * Property: For any terminal state, switching to alternate screen buffer via DECSET 47 
   * should result in using alternate buffer.
   */
  it("Property 5: Alternate screen buffer switching", () => {
    fc.assert(
      fc.property(
        // Generate random terminal dimensions
        fc.integer({ min: 10, max: 200 }),  // cols
        fc.integer({ min: 5, max: 100 }),   // rows
        // Generate random content to write before switching
        fc.array(
          fc.record({
            text: fc.string({ minLength: 0, maxLength: 20 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 0, maxLength: 5 }
        ),
        // Generate random cursor position
        fc.record({
          row: fc.integer({ min: 1, max: 24 }),
          col: fc.integer({ min: 1, max: 80 }),
        }),
        (cols, rows, contentItems, cursorPos) => {
          const terminal = new StatefulTerminal({ cols, rows });

          // Initially, terminal should be on primary screen
          expect(terminal.isAlternateScreenActive()).toBe(false);

          // Write some content to primary screen
          for (const item of contentItems) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Move cursor to a specific position
          const clampedRow = Math.min(cursorPos.row, rows);
          const clampedCol = Math.min(cursorPos.col, cols);
          terminal.pushPtyText(`\x1b[${clampedRow};${clampedCol}H`);

          // Capture primary screen state before switching
          const primaryStateBefore = terminal.getSnapshot();

          // Capture the exact content of primary screen before switching
          const primaryCellsSnapshot: string[][] = [];
          for (let y = 0; y < rows; y++) {
            primaryCellsSnapshot[y] = [];
            for (let x = 0; x < cols; x++) {
              primaryCellsSnapshot[y][x] = primaryStateBefore.cells[y][x].ch;
            }
          }

          // Switch to alternate screen buffer (DECSET 47)
          terminal.pushPtyText("\x1b[?47h");

          // KEY PROPERTY: After DECSET 47, terminal should be using alternate buffer
          expect(terminal.isAlternateScreenActive()).toBe(true);

          // Write a unique marker to alternate screen at a known position
          terminal.pushPtyText("\x1b[1;1H"); // Move to top-left
          terminal.pushPtyText("XALTX"); // Unique marker that won't appear in random content
          
          const alternateWithMarker = terminal.getSnapshot();
          const alternateMarkerChar = alternateWithMarker.cells[0][0].ch;
          expect(alternateMarkerChar).toBe("X"); // Verify marker was written to alternate
          
          // Switch back to primary screen (DECRST 47)
          terminal.pushPtyText("\x1b[?47l");

          // After switching back, should be on primary screen
          expect(terminal.isAlternateScreenActive()).toBe(false);

          // KEY PROPERTY: Primary screen content should be exactly as it was before switching
          const primaryStateAfter = terminal.getSnapshot();
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(primaryStateAfter.cells[y][x].ch).toBe(primaryCellsSnapshot[y][x]);
            }
          }

          // Switch back to alternate to verify it still has the marker
          terminal.pushPtyText("\x1b[?47h");
          expect(terminal.isAlternateScreenActive()).toBe(true);
          
          const alternateStateAgain = terminal.getSnapshot();
          // The marker should still be at position [0][0]
          expect(alternateStateAgain.cells[0][0].ch).toBe("X");
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 6: Screen buffer round-trip**
   * **Validates: Requirements 2.2**
   * 
   * Property: For any terminal state, switching to alternate screen then back to normal 
   * should restore the original screen buffer.
   */
  it("Property 6: Screen buffer round-trip", () => {
    fc.assert(
      fc.property(
        // Generate random terminal dimensions
        fc.integer({ min: 10, max: 200 }),  // cols
        fc.integer({ min: 5, max: 100 }),   // rows
        // Generate random content to write to primary screen
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate random content to write to alternate screen
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        (cols, rows, primaryContent, alternateContent) => {
          const terminal = new StatefulTerminal({ cols, rows });

          // Write content to primary screen
          for (const item of primaryContent) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Capture the complete primary screen state before switching
          const primaryStateBefore = terminal.getSnapshot();
          const primaryCellsSnapshot: string[][] = [];
          const primaryCursorXBefore = primaryStateBefore.cursorX;
          const primaryCursorYBefore = primaryStateBefore.cursorY;
          
          for (let y = 0; y < rows; y++) {
            primaryCellsSnapshot[y] = [];
            for (let x = 0; x < cols; x++) {
              primaryCellsSnapshot[y][x] = primaryStateBefore.cells[y][x].ch;
            }
          }

          // Switch to alternate screen buffer (DECSET 47)
          terminal.pushPtyText("\x1b[?47h");
          expect(terminal.isAlternateScreenActive()).toBe(true);

          // Write different content to alternate screen
          for (const item of alternateContent) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Verify we're still on alternate screen
          expect(terminal.isAlternateScreenActive()).toBe(true);

          // Switch back to primary screen (DECRST 47)
          terminal.pushPtyText("\x1b[?47l");

          // KEY PROPERTY: After round-trip, should be back on primary screen
          expect(terminal.isAlternateScreenActive()).toBe(false);

          // KEY PROPERTY: Primary screen content should be exactly as it was before switching
          const primaryStateAfter = terminal.getSnapshot();
          
          // Verify all cell content is preserved
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(primaryStateAfter.cells[y][x].ch).toBe(primaryCellsSnapshot[y][x]);
            }
          }

          // Verify cursor position is preserved
          expect(primaryStateAfter.cursorX).toBe(primaryCursorXBefore);
          expect(primaryStateAfter.cursorY).toBe(primaryCursorYBefore);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 7: Buffer content preservation**
   * **Validates: Requirements 2.7**
   * 
   * Property: For any content written to one screen buffer, switching to the other buffer 
   * and back should preserve the original content unchanged.
   */
  it("Property 7: Buffer content preservation", () => {
    fc.assert(
      fc.property(
        // Generate random terminal dimensions
        fc.integer({ min: 10, max: 200 }),  // cols
        fc.integer({ min: 5, max: 100 }),   // rows
        // Generate random content for primary screen (first write)
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate random content for alternate screen
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate additional content for primary screen (second write)
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        // Generate additional content for alternate screen (second write)
        fc.array(
          fc.record({
            text: fc.string({ minLength: 1, maxLength: 30 }).filter(str => {
              // Filter out control characters except tab
              for (let i = 0; i < str.length; i++) {
                const charCode = str.charCodeAt(i);
                if (charCode < 0x20 && charCode !== 0x09) {
                  return false;
                }
              }
              return true;
            }),
            addNewline: fc.boolean(),
          }),
          { minLength: 1, maxLength: 10 }
        ),
        (cols, rows, primaryContent1, alternateContent1, primaryContent2, alternateContent2) => {
          const terminal = new StatefulTerminal({ cols, rows });

          // Phase 1: Write initial content to primary screen
          for (const item of primaryContent1) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Capture primary screen state after first write
          const primarySnapshot1 = terminal.getSnapshot();
          const primaryCells1: string[][] = [];
          for (let y = 0; y < rows; y++) {
            primaryCells1[y] = [];
            for (let x = 0; x < cols; x++) {
              primaryCells1[y][x] = primarySnapshot1.cells[y][x].ch;
            }
          }

          // Phase 2: Switch to alternate and write content
          terminal.pushPtyText("\x1b[?47h");
          expect(terminal.isAlternateScreenActive()).toBe(true);

          for (const item of alternateContent1) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Capture alternate screen state after first write
          const alternateSnapshot1 = terminal.getSnapshot();
          const alternateCells1: string[][] = [];
          for (let y = 0; y < rows; y++) {
            alternateCells1[y] = [];
            for (let x = 0; x < cols; x++) {
              alternateCells1[y][x] = alternateSnapshot1.cells[y][x].ch;
            }
          }

          // Phase 3: Switch back to primary and verify preservation
          terminal.pushPtyText("\x1b[?47l");
          expect(terminal.isAlternateScreenActive()).toBe(false);

          const primarySnapshot2 = terminal.getSnapshot();
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(primarySnapshot2.cells[y][x].ch).toBe(primaryCells1[y][x]);
            }
          }

          // Phase 4: Write more content to primary screen
          for (const item of primaryContent2) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Capture primary screen state after second write
          const primarySnapshot3 = terminal.getSnapshot();
          const primaryCells2: string[][] = [];
          for (let y = 0; y < rows; y++) {
            primaryCells2[y] = [];
            for (let x = 0; x < cols; x++) {
              primaryCells2[y][x] = primarySnapshot3.cells[y][x].ch;
            }
          }

          // Phase 5: Switch to alternate and verify its content is still preserved
          terminal.pushPtyText("\x1b[?47h");
          expect(terminal.isAlternateScreenActive()).toBe(true);

          const alternateSnapshot2 = terminal.getSnapshot();
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(alternateSnapshot2.cells[y][x].ch).toBe(alternateCells1[y][x]);
            }
          }

          // Phase 6: Write more content to alternate screen
          for (const item of alternateContent2) {
            terminal.pushPtyText(item.text);
            if (item.addNewline) {
              terminal.pushPtyText("\r\n");
            }
          }

          // Capture alternate screen state after second write
          const alternateSnapshot3 = terminal.getSnapshot();
          const alternateCells2: string[][] = [];
          for (let y = 0; y < rows; y++) {
            alternateCells2[y] = [];
            for (let x = 0; x < cols; x++) {
              alternateCells2[y][x] = alternateSnapshot3.cells[y][x].ch;
            }
          }

          // Phase 7: Switch back to primary and verify its updated content is preserved
          terminal.pushPtyText("\x1b[?47l");
          expect(terminal.isAlternateScreenActive()).toBe(false);

          const primarySnapshot4 = terminal.getSnapshot();
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(primarySnapshot4.cells[y][x].ch).toBe(primaryCells2[y][x]);
            }
          }

          // Phase 8: Final verification - switch to alternate one more time
          terminal.pushPtyText("\x1b[?47h");
          expect(terminal.isAlternateScreenActive()).toBe(true);

          const alternateSnapshot4 = terminal.getSnapshot();
          for (let y = 0; y < rows; y++) {
            for (let x = 0; x < cols; x++) {
              expect(alternateSnapshot4.cells[y][x].ch).toBe(alternateCells2[y][x]);
            }
          }

          // KEY PROPERTY: Each buffer independently preserves its content across multiple switches
          // - Primary buffer content is preserved when switching to alternate and back
          // - Alternate buffer content is preserved when switching to primary and back
          // - Multiple round-trips maintain independent buffer states
        }
      ),
      { numRuns: 100 }
    );
  });
});
