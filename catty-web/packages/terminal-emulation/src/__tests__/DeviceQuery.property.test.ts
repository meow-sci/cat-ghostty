import { describe, it, expect } from "vitest";
import { fc } from "./setup";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

/**
 * Generate valid cursor positions for property-based testing.
 */
const validCursorPositionArbitrary = (cols: number, rows: number) => fc.record({
  x: fc.integer({ min: 0, max: cols - 1 }),
  y: fc.integer({ min: 0, max: rows - 1 }),
});

/**
 * Generate valid terminal dimensions for testing.
 */
const terminalDimensionsArbitrary = fc.record({
  cols: fc.integer({ min: 1, max: 200 }),
  rows: fc.integer({ min: 1, max: 100 }),
});

describe("Device Query Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 11: Cursor position query round-trip**
   * **Validates: Requirements 6.2**
   * 
   * Property: For any cursor position, setting it then querying via cursor position 
   * report should return the same coordinates.
   */
  it("Property 11: Cursor position query round-trip returns same coordinates", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary.chain(dimensions => 
          fc.tuple(
            fc.constant(dimensions),
            validCursorPositionArbitrary(dimensions.cols, dimensions.rows)
          )
        ),
        ([dimensions, position]) => {
          // Create terminal with specified dimensions
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
          });

          // Set cursor position using CSI sequence (1-indexed)
          const csiSetPosition = `\x1b[${position.y + 1};${position.x + 1}H`;
          terminal.pushPtyText(csiSetPosition);

          // Verify cursor was set correctly
          const snapshot = terminal.getSnapshot();
          expect(snapshot.cursorX).toBe(position.x);
          expect(snapshot.cursorY).toBe(position.y);

          // Query cursor position using CPR request
          const cprRequest = "\x1b[6n";
          
          // Track responses sent back to the application
          let responseReceived: string | null = null;
          const terminalWithResponse = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
            onResponse: (response: string) => {
              responseReceived = response;
            },
          });

          // Set the same position
          terminalWithResponse.pushPtyText(csiSetPosition);
          
          // Send CPR request
          terminalWithResponse.pushPtyText(cprRequest);

          // Verify response was generated
          expect(responseReceived).not.toBeNull();
          
          if (responseReceived !== null) {
            // Parse the CPR response: ESC [ row ; col R
            // Response uses 1-indexed coordinates
            const cprPattern = /^\x1b\[(\d+);(\d+)R$/;
            const match = cprPattern.exec(responseReceived);
            
            expect(match).not.toBeNull();
            
            if (match !== null) {
              const reportedRow = Number.parseInt(match[1], 10);
              const reportedCol = Number.parseInt(match[2], 10);
              
              // Round-trip property: reported position should match set position
              // Convert from 1-indexed to 0-indexed for comparison
              expect(reportedRow - 1).toBe(position.y);
              expect(reportedCol - 1).toBe(position.x);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test for cursor position report format validation.
   */
  it("Property 11b: Cursor position report format is always valid", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary.chain(dimensions => 
          fc.tuple(
            fc.constant(dimensions),
            validCursorPositionArbitrary(dimensions.cols, dimensions.rows)
          )
        ),
        ([dimensions, position]) => {
          let responseReceived: string | null = null;
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
            onResponse: (response: string) => {
              responseReceived = response;
            },
          });

          // Set cursor position
          const csiSetPosition = `\x1b[${position.y + 1};${position.x + 1}H`;
          terminal.pushPtyText(csiSetPosition);

          // Send CPR request
          terminal.pushPtyText("\x1b[6n");

          // Verify response format
          expect(responseReceived).not.toBeNull();
          
          if (responseReceived !== null) {
            // Response must match CPR format: ESC [ row ; col R
            const cprPattern = /^\x1b\[(\d+);(\d+)R$/;
            expect(responseReceived).toMatch(cprPattern);
            
            const match = cprPattern.exec(responseReceived);
            if (match !== null) {
              const reportedRow = Number.parseInt(match[1], 10);
              const reportedCol = Number.parseInt(match[2], 10);
              
              // Reported coordinates must be positive (1-indexed)
              expect(reportedRow).toBeGreaterThan(0);
              expect(reportedCol).toBeGreaterThan(0);
              
              // Reported coordinates must be within terminal bounds (1-indexed)
              expect(reportedRow).toBeLessThanOrEqual(dimensions.rows);
              expect(reportedCol).toBeLessThanOrEqual(dimensions.cols);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for multiple consecutive cursor position queries.
   */
  it("Property 11c: Multiple cursor position queries return consistent results", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary.chain(dimensions => 
          fc.tuple(
            fc.constant(dimensions),
            validCursorPositionArbitrary(dimensions.cols, dimensions.rows),
            fc.integer({ min: 2, max: 5 }) // Number of queries to perform
          )
        ),
        ([dimensions, position, numQueries]) => {
          const responses: string[] = [];
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
            onResponse: (response: string) => {
              responses.push(response);
            },
          });

          // Set cursor position once
          const csiSetPosition = `\x1b[${position.y + 1};${position.x + 1}H`;
          terminal.pushPtyText(csiSetPosition);

          // Query multiple times
          for (let i = 0; i < numQueries; i++) {
            terminal.pushPtyText("\x1b[6n");
          }

          // All responses should be identical
          expect(responses.length).toBe(numQueries);
          
          if (responses.length > 0) {
            const firstResponse = responses[0];
            for (const response of responses) {
              expect(response).toBe(firstResponse);
            }
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});
