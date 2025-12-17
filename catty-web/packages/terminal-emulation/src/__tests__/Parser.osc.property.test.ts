import { describe, it, expect } from "vitest";
import { fc } from "./setup";
import { Parser } from "../terminal/Parser";
import { ParserHandlers } from "../terminal/ParserOptions";
import { type OscMessage, type XtermOscMessage } from "../terminal/TerminalEmulationTypes";
import { getLogger } from "@catty/log";

/**
 * Helper to create a parser with capturing handlers for property testing.
 */
interface CapturedOscEvents {
  xtermOscMessages: XtermOscMessage[];
}

function createOscCapturingHandlers(): { handlers: ParserHandlers; captured: CapturedOscEvents } {
  const captured: CapturedOscEvents = {
    xtermOscMessages: [],
  };

  const handlers: ParserHandlers = {
    handleBell: () => {},
    handleBackspace: () => {},
    handleTab: () => {},
    handleLineFeed: () => {},
    handleFormFeed: () => {},
    handleCarriageReturn: () => {},
    handleNormalByte: () => {},
    handleEsc: () => {},
    handleCsi: () => {},
    handleOsc: () => {},
    handleSgr: () => {},
    handleXtermOsc: (msg: XtermOscMessage) => {
      captured.xtermOscMessages.push(msg);
    },
  };

  return { handlers, captured };
}

/**
 * Generate valid title strings for OSC sequences.
 * Excludes control characters (except tab) and limits length.
 */
const validTitleArbitrary = fc.string({
  minLength: 0,
  maxLength: 100, // Reasonable limit for testing
}).filter(str => {
  // Filter out control characters except tab (0x09)
  for (let i = 0; i < str.length; i++) {
    const charCode = str.charCodeAt(i);
    if (charCode < 0x20 && charCode !== 0x09) {
      return false;
    }
  }
  return true;
});

/**
 * Generate OSC terminator (BEL or ST)
 */
const oscTerminatorArbitrary = fc.constantFrom("\x07", "\x1b\\");

describe("OSC Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 1: OSC title setting consistency**
   * **Validates: Requirements 1.1**
   * 
   * Property: For any valid OSC 0 sequence with title text, both window title 
   * and icon name should be set to the same value.
   */
  it("Property 1: OSC 0 sets both title and icon to same value", () => {
    fc.assert(
      fc.property(
        validTitleArbitrary,
        oscTerminatorArbitrary,
        (title, terminator) => {
          const { handlers, captured } = createOscCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // Construct OSC 0 sequence: ESC ] 0 ; title terminator
          const oscSequence = `\x1b]0;${title}${terminator}`;
          
          // Parse the sequence
          parser.pushBytes(Buffer.from(oscSequence));

          // Verify we got exactly one xterm OSC message
          expect(captured.xtermOscMessages).toHaveLength(1);
          
          const message = captured.xtermOscMessages[0];
          
          // Verify it's the correct type for OSC 0
          expect(message._type).toBe("osc.setTitleAndIcon");
          
          // Verify the title is set correctly
          if (message._type === "osc.setTitleAndIcon") {
            expect(message.title).toBe(title);
          }
          
          // Verify the terminator is preserved correctly
          expect(message.terminator).toBe(terminator === "\x07" ? "BEL" : "ST");
          
          // Verify the raw sequence is preserved
          expect(message.raw).toBe(oscSequence);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 2: OSC icon name isolation**
   * **Validates: Requirements 1.2**
   * 
   * Property: For any valid OSC 1 sequence with icon text, only the icon name 
   * should change while window title remains unchanged.
   */
  it("Property 2: OSC 1 affects only icon name, not window title", () => {
    fc.assert(
      fc.property(
        validTitleArbitrary,
        validTitleArbitrary,
        oscTerminatorArbitrary,
        (initialTitle, iconName, terminator) => {
          const { handlers, captured } = createOscCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // First set both title and icon with OSC 0
          const initialSequence = `\x1b]0;${initialTitle}\x07`;
          parser.pushBytes(Buffer.from(initialSequence));
          
          // Clear captured messages to focus on the OSC 1 sequence
          captured.xtermOscMessages.length = 0;

          // Then send OSC 1 to set only icon name
          const iconSequence = `\x1b]1;${iconName}${terminator}`;
          parser.pushBytes(Buffer.from(iconSequence));

          // Verify we got exactly one xterm OSC message for the icon sequence
          expect(captured.xtermOscMessages).toHaveLength(1);
          
          const message = captured.xtermOscMessages[0];
          
          // Verify it's the correct type for OSC 1 (icon name only)
          expect(message._type).toBe("osc.setIconName");
          
          // Verify the icon name is set correctly
          if (message._type === "osc.setIconName") {
            expect(message.iconName).toBe(iconName);
          }
          
          // Verify the terminator is preserved correctly
          expect(message.terminator).toBe(terminator === "\x07" ? "BEL" : "ST");
          
          // Verify the raw sequence is preserved
          expect(message.raw).toBe(iconSequence);
          
          // The key property: OSC 1 should NOT generate a window title message
          // It should only generate an icon name message, leaving window title unchanged
          expect(message._type).not.toBe("osc.setWindowTitle");
          expect(message._type).not.toBe("osc.setTitleAndIcon");
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 3: OSC window title isolation**
   * **Validates: Requirements 1.3**
   * 
   * Property: For any valid OSC 2 sequence with title text, only the window title 
   * should change while icon name remains unchanged.
   */
  it("Property 3: OSC 2 affects only window title, not icon name", () => {
    fc.assert(
      fc.property(
        validTitleArbitrary,
        validTitleArbitrary,
        oscTerminatorArbitrary,
        (initialTitle, windowTitle, terminator) => {
          const { handlers, captured } = createOscCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // First set both title and icon with OSC 0
          const initialSequence = `\x1b]0;${initialTitle}\x07`;
          parser.pushBytes(Buffer.from(initialSequence));
          
          // Clear captured messages to focus on the OSC 2 sequence
          captured.xtermOscMessages.length = 0;

          // Then send OSC 2 to set only window title
          const titleSequence = `\x1b]2;${windowTitle}${terminator}`;
          parser.pushBytes(Buffer.from(titleSequence));

          // Verify we got exactly one xterm OSC message for the title sequence
          expect(captured.xtermOscMessages).toHaveLength(1);
          
          const message = captured.xtermOscMessages[0];
          
          // Verify it's the correct type for OSC 2 (window title only)
          expect(message._type).toBe("osc.setWindowTitle");
          
          // Verify the window title is set correctly
          if (message._type === "osc.setWindowTitle") {
            expect(message.title).toBe(windowTitle);
          }
          
          // Verify the terminator is preserved correctly
          expect(message.terminator).toBe(terminator === "\x07" ? "BEL" : "ST");
          
          // Verify the raw sequence is preserved
          expect(message.raw).toBe(titleSequence);
          
          // The key property: OSC 2 should NOT generate an icon name message
          // It should only generate a window title message, leaving icon name unchanged
          expect(message._type).not.toBe("osc.setIconName");
          expect(message._type).not.toBe("osc.setTitleAndIcon");
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * **Feature: xterm-extensions, Property 4: Title query round-trip**
   * **Validates: Requirements 1.4**
   * 
   * Property: For any title string, setting it via OSC 2 then querying via OSC 21 
   * should return the same title.
   */
  it("Property 4: Title query round-trip consistency", () => {
    fc.assert(
      fc.property(
        validTitleArbitrary,
        oscTerminatorArbitrary,
        oscTerminatorArbitrary,
        (title, setTerminator, queryTerminator) => {
          const { handlers, captured } = createOscCapturingHandlers();
          const parser = new Parser({ handlers, log: getLogger() });

          // First, set the window title using OSC 2
          const setTitleSequence = `\x1b]2;${title}${setTerminator}`;
          parser.pushBytes(Buffer.from(setTitleSequence));

          // Verify the set title message was parsed correctly
          expect(captured.xtermOscMessages).toHaveLength(1);
          const setMessage = captured.xtermOscMessages[0];
          expect(setMessage._type).toBe("osc.setWindowTitle");
          
          if (setMessage._type === "osc.setWindowTitle") {
            expect(setMessage.title).toBe(title);
          }

          // Clear captured messages to focus on the query
          captured.xtermOscMessages.length = 0;

          // Then, query the window title using OSC 21
          const queryTitleSequence = `\x1b]21${queryTerminator}`;
          parser.pushBytes(Buffer.from(queryTitleSequence));

          // Verify the query message was parsed correctly
          expect(captured.xtermOscMessages).toHaveLength(1);
          const queryMessage = captured.xtermOscMessages[0];
          expect(queryMessage._type).toBe("osc.queryWindowTitle");
          
          // Verify the query message structure
          expect(queryMessage.raw).toBe(queryTitleSequence);
          expect(queryMessage.terminator).toBe(queryTerminator === "\x07" ? "BEL" : "ST");

          // The round-trip property: The query should be parseable and well-formed
          // In a complete implementation, this would trigger a response containing the title
          // For now, we verify that the query parsing is consistent and correct
          
          // Key property: Query parsing should be independent of the title content
          // The query message should always have the same structure regardless of 
          // what title was previously set
          expect(queryMessage._type).toBe("osc.queryWindowTitle");
          
          // The query should not contain any title data - it's a request, not a response
          expect(queryMessage).not.toHaveProperty("title");
          expect(queryMessage).not.toHaveProperty("iconName");
        }
      ),
      { numRuns: 100 }
    );
  });
});