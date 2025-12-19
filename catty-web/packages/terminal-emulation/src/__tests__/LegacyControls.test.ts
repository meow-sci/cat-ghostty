import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";
import { createDefaultSgrState } from "../terminal/SgrStyleManager";

function rowToString(term: StatefulTerminal, row = 0): string {
  const snapshot = term.getSnapshot();
  return snapshot.cells[row].map((c) => c.ch).join("");
}

describe("Legacy controls and resets", () => {
  it("should switch invoked charset via SO/SI when UTF-8 mode is disabled", () => {
    const term = new StatefulTerminal({ cols: 5, rows: 1 });

    // Disable UTF-8 mode so charset translation is applied.
    term.pushPtyText("\x1b[?2027l");

    // Designate G1 as DEC Special Graphics, then invoke it via SO.
    term.pushPtyText("\x1b)0");
    term.pushPtyText("\x0eq\x0fq");

    expect(rowToString(term)).toBe("─q   ");
  });

  it("should set a horizontal tab stop (HTS) and tab to it", () => {
    const term = new StatefulTerminal({ cols: 16, rows: 1 });

    term.pushPtyText("abc");
    term.pushPtyText("\x1bH"); // HTS at column 4 (1-based), index 3 (0-based)

    term.pushPtyText("\r\tZ");

    expect(rowToString(term).slice(0, 4)).toBe("abcZ");
  });

  it("should perform IND (ESC D) without carriage return", () => {
    const term = new StatefulTerminal({ cols: 5, rows: 2 });

    term.pushPtyText("A");
    term.pushPtyText("\x1bD");
    term.pushPtyText("B");

    const snap = term.getSnapshot();
    expect(snap.cells[1][1].ch).toBe("B");
  });

  it("should perform NEL (ESC E) as CR+LF", () => {
    const term = new StatefulTerminal({ cols: 5, rows: 2 });

    term.pushPtyText("A");
    term.pushPtyText("\x1bE");
    term.pushPtyText("B");

    const snap = term.getSnapshot();
    expect(snap.cells[1][0].ch).toBe("B");
  });

  it("should soft reset via DECSTR (CSI ! p) without clearing the screen", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 1 });

    term.pushPtyText("HELLO");
    term.pushPtyText("\x1b[31mX");

    const before = term.getSnapshot();
    expect(before.cells[0][5].ch).toBe("X");
    expect(before.cells[0][5].sgrState?.foregroundColor).not.toBeNull();

    term.pushPtyText("\x1b[!p");

    expect(term.cursorX).toBe(0);
    expect(term.cursorY).toBe(0);

    const afterReset = term.getSnapshot();
    expect(afterReset.currentSgrState).toEqual(createDefaultSgrState());

    // Screen contents remain; subsequent text overwrites at the home position.
    term.pushPtyText("Y");
    expect(rowToString(term)).toBe("YELLOX    ");

    const afterWrite = term.getSnapshot();
    expect(afterWrite.cells[0][0].sgrState).toEqual(createDefaultSgrState());
  });

  it("should hard reset via RIS (ESC c) clearing the screen", () => {
    const term = new StatefulTerminal({ cols: 5, rows: 2 });

    term.pushPtyText("HI");
    term.pushPtyText("\x1b[?2027l\x1b)0\x0eq");

    term.pushPtyText("\x1bc");

    expect(term.cursorX).toBe(0);
    expect(term.cursorY).toBe(0);
    expect(rowToString(term, 0)).toBe("     ");
    expect(rowToString(term, 1)).toBe("     ");
  });
});
