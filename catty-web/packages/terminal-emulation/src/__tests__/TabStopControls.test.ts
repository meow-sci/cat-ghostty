import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";

function rowToString(term: StatefulTerminal, row = 0): string {
  const snapshot = term.getSnapshot();
  return snapshot.cells[row].map((c) => c.ch).join("");
}

describe("Tab stop controls (CHT/CBT/TBC)", () => {
  it("CHT should move to the next tab stop Ps times", () => {
    const term = new StatefulTerminal({ cols: 20, rows: 1 });

    // Default tab stops are every 8 columns (0, 8, 16, ...)
    term.pushPtyText("\r");
    term.pushPtyText("\x1b[2I");

    expect(term.cursorX).toBe(16);

    term.pushPtyText("X");
    expect(rowToString(term).slice(0, 17)).toBe("                X");
  });

  it("CBT should move to the previous tab stop Ps times", () => {
    const term = new StatefulTerminal({ cols: 20, rows: 1 });

    term.pushPtyText("\r\x1b[2I");
    expect(term.cursorX).toBe(16);

    term.pushPtyText("\x1b[1Z");
    expect(term.cursorX).toBe(8);

    term.pushPtyText("\x1b[1Z");
    expect(term.cursorX).toBe(0);
  });

  it("TBC 0 should clear the tab stop at the cursor", () => {
    const term = new StatefulTerminal({ cols: 20, rows: 1 });

    // Move to column 9 (1-based) => cursorX 8, which is a default tab stop.
    term.pushPtyText("\x1b[9G");
    expect(term.cursorX).toBe(8);

    // Clear tab stop at cursor.
    term.pushPtyText("\x1b[0g");

    // From col 8, next tab should now be 16.
    term.pushPtyText("\r\x1b[I");
    expect(term.cursorX).toBe(16);
  });

  it("TBC 3 should clear all tab stops", () => {
    const term = new StatefulTerminal({ cols: 20, rows: 1 });

    term.pushPtyText("\x1b[3g");

    term.pushPtyText("\r\tX");

    // With no tab stops, TAB goes to last column.
    expect(term.cursorX).toBe(19);

    // Writing at last column triggers wrapPending; visible output should include X at the last column.
    expect(rowToString(term)).toBe("                   X");
  });
});
