import { describe, it, expect } from "vitest";
import { fc } from "./setup";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

/**
 * Generate valid UTF-8 multi-byte sequences for testing.
 * Uses fc.string() which generates strings with various Unicode characters.
 */
const validUtf8StringArbitrary = fc.string({ minLength: 1, maxLength: 20 });

/**
 * Generate character set identifiers commonly used in terminals.
 */
const characterSetArbitrary = fc.oneof(
  fc.constant("B"), // ASCII
  fc.constant("0"), // DEC Special Graphics
  fc.constant("A"), // UK
  fc.constant("4"), // Dutch
  fc.constant("5"), // Finnish
  fc.constant("C"), // Finnish (alternate)
  fc.constant("R"), // French
  fc.constant("Q"), // French Canadian
  fc.constant("K"), // German
  fc.constant("Y"), // Italian
  fc.constant("E"), // Norwegian/Danish
  fc.constant("6"), // Norwegian/Danish (alternate)
  fc.constant("Z"), // Spanish
  fc.constant("H"), // Swedish
  fc.constant("7"), // Swedish (alternate)
  fc.constant("="), // Swiss
);

/**
 * Generate G slot identifiers.
 */
const gSlotArbitrary = fc.oneof(
  fc.constant("G0" as const),
  fc.constant("G1" as const),
  fc.constant("G2" as const),
  fc.constant("G3" as const),
);

describe("UTF-8 Processing Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 13: UTF-8 processing correctness**
   * **Validates: Requirements 8.2**
   * 
   * Property: For any valid multi-byte UTF-8 sequence, enabling UTF-8 mode should 
   * process the sequence correctly without corruption.
   */
  it("Property 13: UTF-8 mode processes multi-byte sequences correctly", () => {
    fc.assert(
      fc.property(
        validUtf8StringArbitrary,
        (utf8String) => {
          // Create terminal with UTF-8 mode enabled (default)
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          // Ensure UTF-8 mode is enabled
          terminal.setUtf8Mode(true);
          expect(terminal.isUtf8Mode()).toBe(true);

          // Get initial cursor position
          const initialX = terminal.getSnapshot().cursorX;
          const initialY = terminal.getSnapshot().cursorY;

          // Push the UTF-8 string to the terminal
          terminal.pushPtyText(utf8String);

          // Get the terminal snapshot
          const snapshot = terminal.getSnapshot();

          // Property 1: Terminal should not crash or corrupt state
          expect(snapshot.cols).toBe(80);
          expect(snapshot.rows).toBe(24);

          // Property 2: Cursor should advance (unless string is empty or only control chars)
          const printableChars = utf8String.replace(/[\x00-\x1f\x7f]/g, "");
          if (printableChars.length > 0) {
            const cursorMoved = 
              snapshot.cursorX !== initialX || 
              snapshot.cursorY !== initialY;
            expect(cursorMoved).toBe(true);
          }

          // Property 3: Characters should be stored in cells
          // Count non-space cells in the terminal (spaces are default)
          let nonSpaceCells = 0;
          for (let y = 0; y < snapshot.rows; y++) {
            for (let x = 0; x < snapshot.cols; x++) {
              if (snapshot.cells[y][x].ch !== " ") {
                nonSpaceCells++;
              }
            }
          }

          // If we had non-space printable characters, we should have non-space cells
          const nonSpaceChars = printableChars.replace(/ /g, "");
          if (nonSpaceChars.length > 0) {
            expect(nonSpaceCells).toBeGreaterThan(0);
          }

          // Property 4: UTF-8 mode state should remain enabled
          expect(terminal.isUtf8Mode()).toBe(true);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for UTF-8 mode toggle behavior.
   */
  it("Property 13b: UTF-8 mode can be toggled without state corruption", () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.record({
            utf8Mode: fc.boolean(),
            text: validUtf8StringArbitrary,
          }),
          { minLength: 1, maxLength: 10 }
        ),
        (operations) => {
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          for (const op of operations) {
            // Set UTF-8 mode
            terminal.setUtf8Mode(op.utf8Mode);
            expect(terminal.isUtf8Mode()).toBe(op.utf8Mode);

            // Process text
            terminal.pushPtyText(op.text);

            // Verify terminal state remains valid
            const snapshot = terminal.getSnapshot();
            expect(snapshot.cols).toBe(80);
            expect(snapshot.rows).toBe(24);
            expect(snapshot.cursorX).toBeGreaterThanOrEqual(0);
            expect(snapshot.cursorX).toBeLessThan(80);
            expect(snapshot.cursorY).toBeGreaterThanOrEqual(0);
            expect(snapshot.cursorY).toBeLessThan(24);

            // UTF-8 mode state should match what we set
            expect(terminal.isUtf8Mode()).toBe(op.utf8Mode);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for character set designation.
   */
  it("Property 13c: Character set designation preserves state correctly", () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.record({
            slot: gSlotArbitrary,
            charset: characterSetArbitrary,
          }),
          { minLength: 1, maxLength: 10 }
        ),
        (designations) => {
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          // Track expected character sets
          const expectedCharsets = {
            G0: "B", // Default ASCII
            G1: "B",
            G2: "B",
            G3: "B",
          };

          for (const designation of designations) {
            // Designate character set
            terminal.designateCharacterSet(designation.slot, designation.charset);
            expectedCharsets[designation.slot] = designation.charset;

            // Verify the designation was applied
            expect(terminal.getCharacterSet(designation.slot)).toBe(designation.charset);

            // Verify other slots remain unchanged
            for (const slot of ["G0", "G1", "G2", "G3"] as const) {
              expect(terminal.getCharacterSet(slot)).toBe(expectedCharsets[slot]);
            }

            // Verify terminal state remains valid
            const snapshot = terminal.getSnapshot();
            expect(snapshot.cols).toBe(80);
            expect(snapshot.rows).toBe(24);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for character set query response.
   */
  it("Property 13d: Character set query generates valid response", () => {
    fc.assert(
      fc.property(
        fc.boolean(), // UTF-8 mode
        gSlotArbitrary,
        characterSetArbitrary,
        (utf8Mode, slot, charset) => {
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          // Set UTF-8 mode
          terminal.setUtf8Mode(utf8Mode);

          // Designate character set
          terminal.designateCharacterSet(slot, charset);
          terminal.switchCharacterSet(slot);

          // Track responses
          const responses: string[] = [];
          terminal.onResponse((response) => {
            responses.push(response);
          });

          // Send character set query: CSI ? 26 n
          terminal.pushPtyText("\x1b[?26n");

          // Should have received exactly one response
          expect(responses.length).toBe(1);

          // Response should be a valid CSI sequence
          const response = responses[0];
          expect(response).toMatch(/^\x1b\[/); // Starts with CSI

          // Response should contain character set information
          if (utf8Mode) {
            expect(response).toContain("utf-8");
          } else {
            expect(response).toContain(charset);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for UTF-8 mode control via DEC private mode.
   */
  it("Property 13e: UTF-8 mode responds to DECSET/DECRST 2027", () => {
    fc.assert(
      fc.property(
        fc.array(fc.boolean(), { minLength: 1, maxLength: 20 }),
        (modeSequence) => {
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          for (const enableUtf8 of modeSequence) {
            if (enableUtf8) {
              // DECSET 2027: Enable UTF-8 mode
              terminal.pushPtyText("\x1b[?2027h");
              expect(terminal.isUtf8Mode()).toBe(true);
            } else {
              // DECRST 2027: Disable UTF-8 mode
              terminal.pushPtyText("\x1b[?2027l");
              expect(terminal.isUtf8Mode()).toBe(false);
            }

            // Verify terminal state remains valid
            const snapshot = terminal.getSnapshot();
            expect(snapshot.cols).toBe(80);
            expect(snapshot.rows).toBe(24);
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for character set designation via ESC sequences.
   */
  it("Property 13f: Character set designation via ESC sequences works correctly", () => {
    fc.assert(
      fc.property(
        fc.array(
          fc.record({
            slot: gSlotArbitrary,
            charset: characterSetArbitrary,
          }),
          { minLength: 1, maxLength: 10 }
        ),
        (designations) => {
          const terminal = new StatefulTerminal({
            cols: 80,
            rows: 24,
          });

          // Track expected character sets
          const expectedCharsets = {
            G0: "B",
            G1: "B",
            G2: "B",
            G3: "B",
          };

          for (const designation of designations) {
            // Generate ESC sequence for character set designation
            let escSequence: string;
            switch (designation.slot) {
              case "G0":
                escSequence = `\x1b(${designation.charset}`;
                break;
              case "G1":
                escSequence = `\x1b)${designation.charset}`;
                break;
              case "G2":
                escSequence = `\x1b*${designation.charset}`;
                break;
              case "G3":
                escSequence = `\x1b+${designation.charset}`;
                break;
            }

            // Send the ESC sequence
            terminal.pushPtyText(escSequence);
            expectedCharsets[designation.slot] = designation.charset;

            // Verify the designation was applied
            expect(terminal.getCharacterSet(designation.slot)).toBe(designation.charset);

            // Verify all slots have expected values
            for (const slot of ["G0", "G1", "G2", "G3"] as const) {
              expect(terminal.getCharacterSet(slot)).toBe(expectedCharsets[slot]);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
