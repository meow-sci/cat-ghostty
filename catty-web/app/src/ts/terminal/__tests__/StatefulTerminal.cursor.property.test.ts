import { describe, it, expect } from "vitest";
import { fc } from "./setup";
import { StatefulTerminal, type CursorState } from "../StatefulTerminal";

/**
 * Generate valid cursor state for property-based testing.
 */
const validCursorStateArbitrary = (cols: number, rows: number) => fc.record({
  x: fc.integer({ min: 0, max: cols - 1 }),
  y: fc.integer({ min: 0, max: rows - 1 }),
  visible: fc.boolean(),
  style: fc.integer({ min: 0, max: 6 }), // Valid DECSCUSR style values
  applicationMode: fc.boolean(),
  wrapPending: fc.boolean(),
});

/**
 * Generate valid terminal dimensions for testing.
 */
const terminalDimensionsArbitrary = fc.record({
  cols: fc.integer({ min: 1, max: 200 }),
  rows: fc.integer({ min: 1, max: 100 }),
});

describe("StatefulTerminal Cursor State Property-Based Tests", () => {
  /**
   * **Feature: xterm-extensions, Property 9: Cursor state round-trip**
   * **Validates: Requirements 3.4**
   * 
   * Property: For any cursor state (position, attributes, wrap state), saving then 
   * restoring should produce identical state.
   */
  it("Property 9: Cursor state round-trip preserves all state components", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary.chain(dimensions => 
          fc.tuple(
            fc.constant(dimensions),
            validCursorStateArbitrary(dimensions.cols, dimensions.rows)
          )
        ),
        ([dimensions, originalState]) => {
          // Create terminal with specified dimensions
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
          });

          // Set the cursor state using individual methods to ensure proper validation
          terminal.restoreCursorState(originalState);

          // Save the current cursor state
          const savedState = terminal.saveCursorState();

          // Modify the terminal state to something different
          // For coordinates, ensure they stay within bounds
          const differentState: CursorState = {
            x: Math.min(dimensions.cols - 1, originalState.x === 0 ? Math.min(1, dimensions.cols - 1) : 0),
            y: Math.min(dimensions.rows - 1, originalState.y === 0 ? Math.min(1, dimensions.rows - 1) : 0),
            visible: !originalState.visible,
            style: originalState.style === 0 ? 1 : 0,
            applicationMode: !originalState.applicationMode,
            wrapPending: !originalState.wrapPending,
          };
          terminal.restoreCursorState(differentState);

          // Verify the state was actually changed (at least some property should be different)
          const modifiedState = terminal.getCursorState();
          // For 1x1 terminals, coordinates can't change, but other properties should
          const hasChanges = 
            modifiedState.visible !== savedState.visible ||
            modifiedState.style !== savedState.style ||
            modifiedState.applicationMode !== savedState.applicationMode ||
            modifiedState.wrapPending !== savedState.wrapPending ||
            (dimensions.cols > 1 && modifiedState.x !== savedState.x) ||
            (dimensions.rows > 1 && modifiedState.y !== savedState.y);
          
          expect(hasChanges).toBe(true);

          // Restore the saved state
          terminal.restoreCursorState(savedState);

          // Get the final state
          const restoredState = terminal.getCursorState();

          // Round-trip property: restored state should match original saved state
          expect(restoredState.x).toBe(savedState.x);
          expect(restoredState.y).toBe(savedState.y);
          expect(restoredState.visible).toBe(savedState.visible);
          expect(restoredState.style).toBe(savedState.style);
          expect(restoredState.applicationMode).toBe(savedState.applicationMode);
          expect(restoredState.wrapPending).toBe(savedState.wrapPending);

          // Complete state equality
          expect(restoredState).toEqual(savedState);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Additional property test for cursor state bounds validation.
   */
  it("Property 9b: Cursor state restoration validates and clamps coordinates", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary,
        fc.record({
          x: fc.integer({ min: -50, max: 300 }), // Include out-of-bounds values
          y: fc.integer({ min: -50, max: 150 }),
          visible: fc.boolean(),
          style: fc.integer({ min: -10, max: 20 }), // Include invalid style values
          applicationMode: fc.boolean(),
          wrapPending: fc.boolean(),
        }),
        (dimensions, inputState) => {
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
          });

          // Restore potentially out-of-bounds state
          terminal.restoreCursorState(inputState);

          // Get the actual state after validation
          const actualState = terminal.getCursorState();

          // Bounds validation property: coordinates should be clamped to valid ranges
          expect(actualState.x).toBeGreaterThanOrEqual(0);
          expect(actualState.x).toBeLessThan(dimensions.cols);
          expect(actualState.y).toBeGreaterThanOrEqual(0);
          expect(actualState.y).toBeLessThan(dimensions.rows);

          // Style validation: should preserve valid styles, handle invalid ones gracefully
          if (inputState.style >= 0 && inputState.style <= 6) {
            expect(actualState.style).toBe(inputState.style);
          }

          // Boolean properties should be preserved exactly
          expect(actualState.visible).toBe(inputState.visible);
          expect(actualState.applicationMode).toBe(inputState.applicationMode);
          expect(actualState.wrapPending).toBe(inputState.wrapPending);

          // Consistency property: multiple saves/restores should be stable
          const firstSave = terminal.saveCursorState();
          terminal.restoreCursorState(firstSave);
          const secondSave = terminal.saveCursorState();
          
          expect(firstSave).toEqual(secondSave);
        }
      ),
      { numRuns: 100 }
    );
  });

  /**
   * Property test for cursor state consistency during terminal operations.
   */
  it("Property 9c: Cursor state operations maintain internal consistency", () => {
    fc.assert(
      fc.property(
        terminalDimensionsArbitrary,
        fc.array(fc.oneof(
          fc.record({ op: fc.constant("setVisibility" as const), value: fc.boolean() }),
          fc.record({ op: fc.constant("setStyle" as const), value: fc.integer({ min: 0, max: 6 }) }),
          fc.record({ op: fc.constant("setApplicationKeys" as const), value: fc.boolean() }),
          fc.record({ op: fc.constant("saveRestore" as const) }),
        ), { minLength: 1, maxLength: 20 }),
        (dimensions, operations) => {
          const terminal = new StatefulTerminal({
            cols: dimensions.cols,
            rows: dimensions.rows,
          });

          let savedStates: CursorState[] = [];

          // Apply operations and track state changes
          for (const op of operations) {
            const stateBefore = terminal.getCursorState();

            switch (op.op) {
              case "setVisibility":
                terminal.setCursorVisibility(op.value);
                const stateAfterVisibility = terminal.getCursorState();
                expect(stateAfterVisibility.visible).toBe(op.value);
                // Other properties should remain unchanged
                expect(stateAfterVisibility.x).toBe(stateBefore.x);
                expect(stateAfterVisibility.y).toBe(stateBefore.y);
                expect(stateAfterVisibility.style).toBe(stateBefore.style);
                expect(stateAfterVisibility.applicationMode).toBe(stateBefore.applicationMode);
                break;

              case "setStyle":
                terminal.setCursorStyle(op.value);
                const stateAfterStyle = terminal.getCursorState();
                expect(stateAfterStyle.style).toBe(op.value);
                // Other properties should remain unchanged
                expect(stateAfterStyle.x).toBe(stateBefore.x);
                expect(stateAfterStyle.y).toBe(stateBefore.y);
                expect(stateAfterStyle.visible).toBe(stateBefore.visible);
                expect(stateAfterStyle.applicationMode).toBe(stateBefore.applicationMode);
                break;

              case "setApplicationKeys":
                terminal.setApplicationCursorKeys(op.value);
                const stateAfterAppKeys = terminal.getCursorState();
                expect(stateAfterAppKeys.applicationMode).toBe(op.value);
                // Other properties should remain unchanged
                expect(stateAfterAppKeys.x).toBe(stateBefore.x);
                expect(stateAfterAppKeys.y).toBe(stateBefore.y);
                expect(stateAfterAppKeys.visible).toBe(stateBefore.visible);
                expect(stateAfterAppKeys.style).toBe(stateBefore.style);
                break;

              case "saveRestore":
                const saved = terminal.saveCursorState();
                savedStates.push(saved);
                
                // Modify state
                terminal.setCursorVisibility(!saved.visible);
                
                // Restore
                terminal.restoreCursorState(saved);
                
                // Should match saved state
                const restored = terminal.getCursorState();
                expect(restored).toEqual(saved);
                break;
            }

            // Invariant: cursor coordinates should always be within bounds
            const currentState = terminal.getCursorState();
            expect(currentState.x).toBeGreaterThanOrEqual(0);
            expect(currentState.x).toBeLessThan(dimensions.cols);
            expect(currentState.y).toBeGreaterThanOrEqual(0);
            expect(currentState.y).toBeLessThan(dimensions.rows);
          }

          // Final consistency check: all saved states should still be restorable
          for (const savedState of savedStates) {
            terminal.restoreCursorState(savedState);
            const currentState = terminal.getCursorState();
            expect(currentState).toEqual(savedState);
          }
        }
      ),
      { numRuns: 100 }
    );
  });
});