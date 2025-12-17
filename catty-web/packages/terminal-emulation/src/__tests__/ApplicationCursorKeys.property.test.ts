import { describe, it, expect } from "vitest";
import { fc } from "./setup";

/**
 * Mock KeyboardEvent for testing keyboard encoding
 */
class MockKeyboardEvent {
  constructor(
    public key: string,
    public altKey: boolean = false,
    public ctrlKey: boolean = false,
    public metaKey: boolean = false,
    public shiftKey: boolean = false
  ) {}
}

/**
 * Encode keyboard events to terminal bytes (simplified version for testing)
 * This is based on the encodeKeyDownToTerminalBytes function from TerminalController
 */
function encodeKeyDownToTerminalBytes(e: MockKeyboardEvent, opts: { applicationCursorKeys: boolean }): string | null {
  if (e.metaKey) {
    return null;
  }

  switch (e.key) {
    case "Enter":
      return "\r";
    case "Backspace":
      return "\x7f";
    case "Tab":
      return "\t";
    case "Escape":
      return "\x1b";
    case "ArrowUp":
      return opts.applicationCursorKeys ? "\x1bOA" : "\x1b[A";
    case "ArrowDown":
      return opts.applicationCursorKeys ? "\x1bOB" : "\x1b[B";
    case "ArrowRight":
      return opts.applicationCursorKeys ? "\x1bOC" : "\x1b[C";
    case "ArrowLeft":
      return opts.applicationCursorKeys ? "\x1bOD" : "\x1b[D";
    case "Home":
      return "\x1b[H";
    case "End":
      return "\x1b[F";
    case "Delete":
      return "\x1b[3~";
    case "Insert":
      return "\x1b[2~";
    case "PageUp":
      return "\x1b[5~";
    case "PageDown":
      return "\x1b[6~";
  }

  if (e.key.length !== 1) {
    return null;
  }

  if (e.altKey) {
    const code = e.key.charCodeAt(0);
    if (code >= 0x20 && code <= 0x7e) {
      return "\x1b" + e.key;
    }
  }

  return e.key;
}

/**
 * Generate arrow key names for testing
 */
const arrowKeyArbitrary = fc.constantFrom("ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight");

/**
 * Generate modifier key combinations
 */
const modifierArbitrary = fc.record({
  altKey: fc.boolean(),
  ctrlKey: fc.boolean(),
  metaKey: fc.boolean(),
  shiftKey: fc.boolean(),
});

describe("Application Cursor Keys Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 8: Application cursor key mode**
   * **Validates: Requirements 3.3**
   * 
   * Property: For any arrow key input when application cursor keys are enabled, 
   * the output should be SS3 sequences instead of CSI sequences.
   */
  it("Property 8: Application cursor key mode produces SS3 sequences", () => {
    fc.assert(
      fc.property(
        arrowKeyArbitrary,
        modifierArbitrary,
        (arrowKey, modifiers) => {
          // Skip meta key combinations as they are handled differently
          if (modifiers.metaKey) {
            return;
          }

          const keyEvent = new MockKeyboardEvent(
            arrowKey,
            modifiers.altKey,
            modifiers.ctrlKey,
            modifiers.metaKey,
            modifiers.shiftKey
          );

          // Test with application cursor keys disabled (normal mode)
          const normalModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: false });
          
          // Test with application cursor keys enabled
          const appModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: true });

          // Both should produce valid results for arrow keys
          expect(normalModeResult).not.toBeNull();
          expect(appModeResult).not.toBeNull();

          if (normalModeResult && appModeResult) {
            // Key property: Normal mode should produce CSI sequences (ESC [)
            expect(normalModeResult.startsWith("\x1b[")).toBe(true);
            
            // Key property: Application mode should produce SS3 sequences (ESC O)
            expect(appModeResult.startsWith("\x1bO")).toBe(true);

            // The sequences should be different
            expect(normalModeResult).not.toBe(appModeResult);

            // Both sequences should be exactly 3 characters long
            expect(normalModeResult.length).toBe(3);
            expect(appModeResult.length).toBe(3);

            // The final character should be the same (A, B, C, or D)
            const normalFinal = normalModeResult.charAt(2);
            const appFinal = appModeResult.charAt(2);
            expect(normalFinal).toBe(appFinal);

            // Verify the correct mapping for each arrow key
            switch (arrowKey) {
              case "ArrowUp":
                expect(normalFinal).toBe("A");
                expect(normalModeResult).toBe("\x1b[A");
                expect(appModeResult).toBe("\x1bOA");
                break;
              case "ArrowDown":
                expect(normalFinal).toBe("B");
                expect(normalModeResult).toBe("\x1b[B");
                expect(appModeResult).toBe("\x1bOB");
                break;
              case "ArrowRight":
                expect(normalFinal).toBe("C");
                expect(normalModeResult).toBe("\x1b[C");
                expect(appModeResult).toBe("\x1bOC");
                break;
              case "ArrowLeft":
                expect(normalFinal).toBe("D");
                expect(normalModeResult).toBe("\x1b[D");
                expect(appModeResult).toBe("\x1bOD");
                break;
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test to verify non-arrow keys are unaffected by application cursor key mode
   */
  it("Property 8b: Non-arrow keys unaffected by application cursor key mode", () => {
    fc.assert(
      fc.property(
        fc.constantFrom("Enter", "Backspace", "Tab", "Escape", "Home", "End", "Delete", "Insert", "PageUp", "PageDown"),
        modifierArbitrary,
        (keyName, modifiers) => {
          // Skip meta key combinations
          if (modifiers.metaKey) {
            return;
          }

          const keyEvent = new MockKeyboardEvent(
            keyName,
            modifiers.altKey,
            modifiers.ctrlKey,
            modifiers.metaKey,
            modifiers.shiftKey
          );

          // Test with application cursor keys disabled
          const normalModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: false });
          
          // Test with application cursor keys enabled
          const appModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: true });

          // Key property: Non-arrow keys should produce identical results regardless of application cursor key mode
          expect(normalModeResult).toBe(appModeResult);

          // Both should be valid (non-null) for these keys
          expect(normalModeResult).not.toBeNull();
          expect(appModeResult).not.toBeNull();
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for printable character handling
   */
  it("Property 8c: Printable characters unaffected by application cursor key mode", () => {
    fc.assert(
      fc.property(
        fc.integer({ min: 0x20, max: 0x7e }).map(code => String.fromCharCode(code)),
        fc.boolean(), // altKey
        (char, altKey) => {
          const keyEvent = new MockKeyboardEvent(char, altKey, false, false, false);

          // Test with application cursor keys disabled
          const normalModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: false });
          
          // Test with application cursor keys enabled
          const appModeResult = encodeKeyDownToTerminalBytes(keyEvent, { applicationCursorKeys: true });

          // Key property: Printable characters should be identical regardless of application cursor key mode
          expect(normalModeResult).toBe(appModeResult);

          if (altKey) {
            // Alt+character should produce ESC prefix
            expect(normalModeResult).toBe("\x1b" + char);
            expect(appModeResult).toBe("\x1b" + char);
          } else {
            // Regular character should be passed through unchanged
            expect(normalModeResult).toBe(char);
            expect(appModeResult).toBe(char);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});