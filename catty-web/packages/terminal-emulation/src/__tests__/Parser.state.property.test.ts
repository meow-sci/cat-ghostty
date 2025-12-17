import { describe, it, expect } from "vitest";
import { fc } from "./setup";
import { Parser } from "../terminal/Parser";
import { ParserHandlers } from "../terminal/ParserOptions";
import { 
  type CsiMessage, 
  type EscMessage, 
  type OscMessage, 
  type SgrSequence,
  type XtermOscMessage 
} from "../terminal/TerminalEmulationTypes";
import { getLogger } from "@catty/log";

/**
 * Helper to create a parser with capturing handlers for state integrity testing.
 */
interface CapturedParserState {
  normalBytes: number[];
  csiMessages: CsiMessage[];
  escMessages: EscMessage[];
  oscMessages: OscMessage[];
  sgrSequences: SgrSequence[];
  xtermOscMessages: XtermOscMessage[];
  controlCalls: {
    bell: number;
    backspace: number;
    tab: number;
    lineFeed: number;
    formFeed: number;
    carriageReturn: number;
  };
}

function createStateCapturingHandlers(): { handlers: ParserHandlers; captured: CapturedParserState } {
  const captured: CapturedParserState = {
    normalBytes: [],
    csiMessages: [],
    escMessages: [],
    oscMessages: [],
    sgrSequences: [],
    xtermOscMessages: [],
    controlCalls: {
      bell: 0,
      backspace: 0,
      tab: 0,
      lineFeed: 0,
      formFeed: 0,
      carriageReturn: 0,
    },
  };

  const handlers: ParserHandlers = {
    handleBell: () => { captured.controlCalls.bell++; },
    handleBackspace: () => { captured.controlCalls.backspace++; },
    handleTab: () => { captured.controlCalls.tab++; },
    handleLineFeed: () => { captured.controlCalls.lineFeed++; },
    handleFormFeed: () => { captured.controlCalls.formFeed++; },
    handleCarriageReturn: () => { captured.controlCalls.carriageReturn++; },
    handleNormalByte: (byte: number) => { captured.normalBytes.push(byte); },
    handleEsc: (msg: EscMessage) => { captured.escMessages.push(msg); },
    handleCsi: (msg: CsiMessage) => { captured.csiMessages.push(msg); },
    handleOsc: (msg: OscMessage) => { captured.oscMessages.push(msg); },
    handleSgr: (msg: SgrSequence) => { captured.sgrSequences.push(msg); },
    handleXtermOsc: (msg: XtermOscMessage) => { captured.xtermOscMessages.push(msg); },
  };

  return { handlers, captured };
}

/**
 * Generate valid xterm control sequences for testing state integrity.
 */
const validXtermSequenceArbitrary = fc.oneof(
  // OSC sequences
  fc.record({
    type: fc.constant("osc" as const),
    command: fc.constantFrom("0", "1", "2", "21"),
    title: fc.string({ minLength: 0, maxLength: 50 }).filter(str => {
      // Filter out control characters except tab
      for (let i = 0; i < str.length; i++) {
        const charCode = str.charCodeAt(i);
        if (charCode < 0x20 && charCode !== 0x09) {
          return false;
        }
      }
      return true;
    }),
    terminator: fc.constantFrom("\x07", "\x1b\\"),
  }),
  
  // ESC sequences (save/restore cursor)
  fc.record({
    type: fc.constant("esc" as const),
    command: fc.constantFrom("7", "8"), // DECSC/DECRC
  }),
  
  // CSI sequences (DEC modes)
  fc.record({
    type: fc.constant("csi" as const),
    command: fc.constantFrom("h", "l"), // DECSET/DECRST
    mode: fc.constantFrom("1", "25", "47", "1000", "1047", "1049"),
    private: fc.constant(true),
  }),
  
  // Normal printable characters
  fc.record({
    type: fc.constant("normal" as const),
    char: fc.integer({ min: 0x20, max: 0x7E }),
  }),
  
  // Control characters
  fc.record({
    type: fc.constant("control" as const),
    code: fc.constantFrom(0x07, 0x08, 0x09, 0x0A, 0x0C, 0x0D), // BEL, BS, TAB, LF, FF, CR
  })
);

/**
 * Convert a sequence descriptor to actual bytes.
 */
function sequenceToBytes(seq: any): Uint8Array {
  switch (seq.type) {
    case "osc":
      if (seq.command === "21") {
        // Query sequences don't have title parameter
        return Buffer.from(`\x1b]${seq.command}${seq.terminator}`);
      } else {
        return Buffer.from(`\x1b]${seq.command};${seq.title}${seq.terminator}`);
      }
    
    case "esc":
      return Buffer.from(`\x1b${seq.command}`);
    
    case "csi":
      return Buffer.from(`\x1b[?${seq.mode}${seq.command}`);
    
    case "normal":
      return new Uint8Array([seq.char]);
    
    case "control":
      return new Uint8Array([seq.code]);
    
    default:
      return new Uint8Array([]);
  }
}

describe("Parser State Integrity Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 15: State integrity during operations**
   * **Validates: Requirements 10.4**
   * 
   * Property: For any sequence of save/restore operations, the terminal state should 
   * maintain integrity without corruption.
   */
  it("Property 15: Parser maintains state integrity during mixed sequence operations", () => {
    fc.assert(
      fc.property(
        fc.array(validXtermSequenceArbitrary, { minLength: 1, maxLength: 20 }),
        (sequences) => {
          const { handlers, captured } = createStateCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // Track expected state based on sequence types
          let expectedOscCount = 0;
          let expectedEscCount = 0;
          let expectedCsiCount = 0;
          let expectedNormalBytes = 0;
          let expectedControlCalls = 0;

          // Process each sequence and track expected outcomes
          for (const seq of sequences) {
            const bytes = sequenceToBytes(seq);
            parser.pushBytes(bytes);

            // Update expected counts based on sequence type
            switch (seq.type) {
              case "osc":
                expectedOscCount++;
                break;
              case "esc":
                expectedEscCount++;
                break;
              case "csi":
                expectedCsiCount++;
                break;
              case "normal":
                expectedNormalBytes++;
                break;
              case "control":
                expectedControlCalls++;
                break;
            }
          }

          // Verify state integrity: all messages should be captured correctly
          // The total number of captured messages should match expected counts
          const totalCapturedMessages = 
            captured.csiMessages.length + 
            captured.escMessages.length + 
            captured.oscMessages.length + 
            captured.xtermOscMessages.length;

          const totalExpectedMessages = expectedOscCount + expectedEscCount + expectedCsiCount;

          // Key integrity property: No messages should be lost or duplicated
          expect(totalCapturedMessages).toBe(totalExpectedMessages);

          // Verify normal bytes are captured correctly
          expect(captured.normalBytes.length).toBe(expectedNormalBytes);

          // Verify control calls are captured correctly
          const totalControlCalls = Object.values(captured.controlCalls).reduce((sum, count) => sum + count, 0);
          expect(totalControlCalls).toBe(expectedControlCalls);

          // State integrity property: Each message should have valid structure
          for (const msg of captured.csiMessages) {
            expect(msg).toHaveProperty("_type");
            expect(msg).toHaveProperty("raw");
            expect(typeof msg._type).toBe("string");
            expect(typeof msg.raw).toBe("string");
            expect(msg._type.startsWith("csi.")).toBe(true);
          }

          for (const msg of captured.escMessages) {
            expect(msg).toHaveProperty("_type");
            expect(msg).toHaveProperty("raw");
            expect(typeof msg._type).toBe("string");
            expect(typeof msg.raw).toBe("string");
            expect(msg._type.startsWith("esc.")).toBe(true);
          }

          for (const msg of captured.xtermOscMessages) {
            expect(msg).toHaveProperty("_type");
            expect(msg).toHaveProperty("raw");
            expect(msg).toHaveProperty("terminator");
            expect(typeof msg._type).toBe("string");
            expect(typeof msg.raw).toBe("string");
            expect(msg._type.startsWith("osc.")).toBe(true);
            expect(["BEL", "ST"]).toContain(msg.terminator);
          }

          // Round-trip integrity: Raw sequences should be preserved
          for (const msg of [...captured.csiMessages, ...captured.escMessages, ...captured.xtermOscMessages]) {
            expect(msg.raw).toBeTruthy();
            expect(msg.raw.length).toBeGreaterThan(0);
            // Raw sequence should start with ESC
            expect(msg.raw.charCodeAt(0)).toBe(0x1b);
          }

          // Data integrity: No corruption should occur in message content
          for (const msg of captured.xtermOscMessages) {
            if (msg._type === "osc.setTitleAndIcon" || msg._type === "osc.setWindowTitle") {
              // Title should be extractable from raw sequence
              const titleMatch = msg.raw.match(/\x1b\](?:0|2);([^\x07\x1b]*)/);
              if (titleMatch && msg._type === "osc.setTitleAndIcon") {
                expect((msg as any).title).toBe(titleMatch[1]);
              } else if (titleMatch && msg._type === "osc.setWindowTitle") {
                expect((msg as any).title).toBe(titleMatch[1]);
              }
            } else if (msg._type === "osc.setIconName") {
              // Icon name should be extractable from raw sequence
              const iconMatch = msg.raw.match(/\x1b\]1;([^\x07\x1b]*)/);
              if (iconMatch) {
                expect((msg as any).iconName).toBe(iconMatch[1]);
              }
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test for parser state consistency during error conditions.
   */
  it("Property 15b: Parser maintains consistency with malformed sequences", () => {
    fc.assert(
      fc.property(
        fc.array(fc.oneof(
          // Valid sequences
          validXtermSequenceArbitrary,
          // Malformed sequences that should be handled gracefully
          fc.record({
            type: fc.constant("malformed" as const),
            bytes: fc.array(fc.integer({ min: 0, max: 255 }), { minLength: 1, maxLength: 10 }),
          })
        ), { minLength: 1, maxLength: 15 }),
        (sequences) => {
          const { handlers, captured } = createStateCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // Process sequences, including malformed ones
          for (const seq of sequences) {
            if (seq.type === "malformed") {
              // Push malformed bytes directly
              parser.pushBytes(new Uint8Array(seq.bytes));
            } else {
              const bytes = sequenceToBytes(seq);
              parser.pushBytes(bytes);
            }
          }

          // Key integrity property: Parser should never crash or enter invalid state
          // Even with malformed input, the parser should continue functioning
          
          // Verify that all captured messages have valid structure
          for (const msg of [...captured.csiMessages, ...captured.escMessages, ...captured.xtermOscMessages]) {
            expect(msg).toHaveProperty("_type");
            expect(msg).toHaveProperty("raw");
            expect(typeof msg._type).toBe("string");
            expect(typeof msg.raw).toBe("string");
          }

          // State consistency: Parser should be ready for next valid sequence
          // Test by sending a simple valid sequence after malformed input
          const testSequence = Buffer.from("\x1b]2;test\x07"); // Simple OSC 2
          parser.pushBytes(testSequence);

          // Should have at least one valid xterm OSC message from the test sequence
          const validOscMessages = captured.xtermOscMessages.filter(msg => 
            msg._type === "osc.setWindowTitle" && msg.raw.includes("test")
          );
          expect(validOscMessages.length).toBeGreaterThanOrEqual(1);
        }
      ),
      { numRuns: 100 }
    );
  });
});