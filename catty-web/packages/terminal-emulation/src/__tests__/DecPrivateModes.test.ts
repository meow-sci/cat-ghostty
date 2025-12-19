import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";

function rowToString(term: StatefulTerminal, row = 0): string {
  const snapshot = term.getSnapshot();
  return snapshot.cells[row].map((c) => c.ch).join("");
}

describe("DEC private modes (DECAWM/DECOM)", () => {
  it("DECAWM off (CSI ? 7 l) should not wrap at last column", () => {
    const term = new StatefulTerminal({ cols: 3, rows: 2 });

    term.pushPtyText("\x1b[?7l");
    term.pushPtyText("abcd");

    expect(rowToString(term, 0)).toBe("abd");
    expect(rowToString(term, 1)).toBe("   ");
    expect(term.cursorX).toBe(2);
    expect(term.cursorY).toBe(0);
  });

  it("DECAWM on (default / CSI ? 7 h) should wrap on next printable", () => {
    const term = new StatefulTerminal({ cols: 3, rows: 2 });

    term.pushPtyText("abcd");

    expect(rowToString(term, 0)).toBe("abc");
    expect(rowToString(term, 1)).toBe("d  ");
    expect(term.cursorX).toBe(1);
    expect(term.cursorY).toBe(1);
  });

  it("DECOM on (CSI ? 6 h) should make CUP/VPA relative to scroll region", () => {
    const term = new StatefulTerminal({ cols: 5, rows: 5 });

    // Set scroll region to rows 2..4 (1-indexed). Cursor moves to top margin.
    term.pushPtyText("\x1b[2;4r");
    expect(term.cursorY).toBe(1);

    // With origin mode OFF, CUP 1;1 is absolute (top-left of screen).
    term.pushPtyText("\x1b[1;1H");
    term.pushPtyText("A");
    expect(term.getSnapshot().cells[0][0].ch).toBe("A");

    // Enable origin mode, which homes to the top margin.
    term.pushPtyText("\x1b[?6h");
    expect(term.cursorY).toBe(1);

    // CUP 1;1 should now address the top margin.
    term.pushPtyText("\x1b[1;1H");
    term.pushPtyText("B");
    expect(term.getSnapshot().cells[1][0].ch).toBe("B");

    // VPA 1 should also address the top margin (keeping column).
    term.pushPtyText("\x1b[3G");
    term.pushPtyText("\x1b[1d");
    expect(term.cursorY).toBe(1);
    expect(term.cursorX).toBe(2);

    // Cursor up should clamp inside the scroll region in origin mode.
    term.pushPtyText("\x1b[99A");
    expect(term.cursorY).toBe(1);

    // Disabling origin mode homes to row 0.
    term.pushPtyText("\x1b[?6l");
    expect(term.cursorY).toBe(0);
  });
});
