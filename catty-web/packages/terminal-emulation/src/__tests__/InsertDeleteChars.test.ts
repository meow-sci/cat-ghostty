import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";

function rowToString(term: StatefulTerminal, row = 0): string {
  const snapshot = term.getSnapshot();
  return snapshot.cells[row].map((c) => c.ch).join("");
}

describe("CSI ICH/DCH", () => {
  it("should insert blank characters (CSI Ps @) shifting existing content right", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 1 });

    term.pushPtyText("ABCDE");
    term.pushPtyText("\x1b[3G"); // column 3 (1-based)
    term.pushPtyText("\x1b[2@");

    expect(rowToString(term)).toBe("AB  CDE   ");
  });

  it("should delete characters (CSI Ps P) shifting remaining content left", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 1 });

    term.pushPtyText("ABCDE");
    term.pushPtyText("\x1b[3G");
    term.pushPtyText("\x1b[2@");

    term.pushPtyText("\x1b[3G");
    term.pushPtyText("\x1b[2P");

    expect(rowToString(term)).toBe("ABCDE     ");
  });

  it("should clamp delete count at end of line", () => {
    const term = new StatefulTerminal({ cols: 10, rows: 1 });

    term.pushPtyText("ABCDEFGHIJ");
    term.pushPtyText("\x1b[9G");
    term.pushPtyText("\x1b[5P");

    expect(rowToString(term)).toBe("ABCDEFGH  ");
  });

  it("should insert blanks using current SGR state", () => {
    const term = new StatefulTerminal({ cols: 8, rows: 1 });

    term.pushPtyText("\x1b[31mABCDE");
    term.pushPtyText("\x1b[3G\x1b[2@");

    const snap = term.getSnapshot();
    // Inserted blanks are at columns 3 and 4 (0-based indices 2 and 3)
    expect(snap.cells[0][2].ch).toBe(" ");
    expect(snap.cells[0][3].ch).toBe(" ");
    expect(snap.cells[0][2].sgrState?.foregroundColor).not.toBeNull();
    expect(snap.cells[0][3].sgrState?.foregroundColor).not.toBeNull();
  });
});
