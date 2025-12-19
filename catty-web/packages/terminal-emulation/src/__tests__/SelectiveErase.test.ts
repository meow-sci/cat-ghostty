import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";

function rowText(terminal: StatefulTerminal, row: number): string {
  const snapshot = terminal.getSnapshot();
  return snapshot.cells[row].map((cell) => cell.ch).join("");
}

describe("Selective erase (DECSED/DECSEL)", () => {
  it("DECSEL (CSI ? Ps K) preserves protected cells (Ps=2)", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2 });

    terminal.pushPtyText("ABCDE");
    terminal.pushPtyText("\x1b[46m");
    terminal.pushPtyText("\x1b[2\"q");
    terminal.pushPtyText("FGH");
    terminal.pushPtyText("\x1b[0m");
    terminal.pushPtyText("\x1b[0\"q");
    terminal.pushPtyText("IJ");

    terminal.pushPtyText("\x1b[41m\r\x1b[?2K");

    const snapshot = terminal.getSnapshot();
    expect(rowText(terminal, 0)).toBe("     FGH  ");

    // Protected cells keep their content and protection.
    expect(snapshot.cells[0][5].ch).toBe("F");
    expect(snapshot.cells[0][5].isProtected).toBe(true);

    // Erased cells are blanks with current SGR and are unprotected.
    expect(snapshot.cells[0][0].ch).toBe(" ");
    expect(snapshot.cells[0][0].isProtected).toBe(false);
    const bg = snapshot.cells[0][0].sgrState?.backgroundColor;
    expect(bg?.type).toBe("named");
    if (bg?.type === "named") {
      expect(bg.color).toBe("red");
    }

    // Protected region retains the earlier cyan background.
    const protectedBg = snapshot.cells[0][5].sgrState?.backgroundColor;
    expect(protectedBg?.type).toBe("named");
    if (protectedBg?.type === "named") {
      expect(protectedBg.color).toBe("cyan");
    }
  });

  it("DECSEL (CSI ? 0 K) erases from cursor to end selectively", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2 });

    terminal.pushPtyText("ABCDE");
    terminal.pushPtyText("F\x1b[2\"qGH\x1b[0\"qIJ");

    // Cursor to column 4 (0-indexed): after \r then 4C.
    terminal.pushPtyText("\r\x1b[4C\x1b[?K");

    const snapshot = terminal.getSnapshot();
    // From column 4 onward, unprotected cells are erased, protected remain.
    expect(rowText(terminal, 0)).toBe("ABCD  GH  ");

    expect(snapshot.cells[0][4].ch).toBe(" ");
    expect(snapshot.cells[0][5].ch).toBe(" ");
    expect(snapshot.cells[0][6].ch).toBe("G");
    expect(snapshot.cells[0][6].isProtected).toBe(true);
    expect(snapshot.cells[0][7].ch).toBe("H");
    expect(snapshot.cells[0][7].isProtected).toBe(true);
  });

  it("DECSEL (CSI ? 1 K) erases from start to cursor selectively", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2 });

    terminal.pushPtyText("AB");
    terminal.pushPtyText("\x1b[2\"qCDE\x1b[0\"qFGHIJ");

    // Cursor to column 6: after \r then 6C.
    terminal.pushPtyText("\r\x1b[6C\x1b[?1K");

    const snapshot = terminal.getSnapshot();
    // Start..cursor (0..6) selectively erased: protected CDE remain.
    expect(rowText(terminal, 0)).toBe("  CDE  HIJ");

    expect(snapshot.cells[0][2].ch).toBe("C");
    expect(snapshot.cells[0][2].isProtected).toBe(true);
    expect(snapshot.cells[0][0].isProtected).toBe(false);
  });

  it("DECSED (CSI ? Ps J) preserves protected cells (Ps=2)", () => {
    const terminal = new StatefulTerminal({ cols: 6, rows: 2 });

    // Row 0: 2 unprotected, 2 protected, 2 unprotected
    terminal.pushPtyText("AA");
    terminal.pushPtyText("\x1b[2\"qBB\x1b[0\"qCC");

    // Row 1: 1 protected, 5 unprotected
    terminal.pushPtyText("\r\n\x1b[2\"qD\x1b[0\"qEEEEE");

    terminal.pushPtyText("\x1b[41m\x1b[?2J");

    const snapshot = terminal.getSnapshot();
    expect(rowText(terminal, 0)).toBe("  BB  ");
    expect(rowText(terminal, 1)).toBe("D     ");

    expect(snapshot.cells[0][2].isProtected).toBe(true);
    expect(snapshot.cells[1][0].isProtected).toBe(true);

    // Erased cells take current SGR and are unprotected.
    const bg = snapshot.cells[0][0].sgrState?.backgroundColor;
    expect(bg?.type).toBe("named");
    if (bg?.type === "named") {
      expect(bg.color).toBe("red");
    }
    expect(snapshot.cells[0][0].isProtected).toBe(false);
  });

  it("DECSED (CSI ? 0 J) erases from cursor to end selectively", () => {
    const terminal = new StatefulTerminal({ cols: 6, rows: 2 });

    terminal.pushPtyText("AAAAAA");
    terminal.pushPtyText("\r\n\x1b[2\"qBB\x1b[0\"qCCCC");

    // Cursor row 0, col 3
    terminal.pushPtyText("\x1b[1;4H\x1b[?J");

    expect(rowText(terminal, 0)).toBe("AAA   ");
    // Row 1 should be fully selectively erased except protected BB.
    expect(rowText(terminal, 1)).toBe("BB    ");
  });

  it("DECSED (CSI ? 1 J) erases from start to cursor selectively", () => {
    const terminal = new StatefulTerminal({ cols: 6, rows: 2 });

    terminal.pushPtyText("\x1b[2\"qAA\x1b[0\"qBBBB");
    terminal.pushPtyText("\r\nCCCCCC");

    // Cursor at row 1, col 2 (1-indexed: 2;3H).
    terminal.pushPtyText("\x1b[2;3H\x1b[?1J");

    // Row 0 start..cursor includes entire row 0; protected AA remain.
    expect(rowText(terminal, 0)).toBe("AA    ");
    // Row 1 start..cursor (0..2) erased.
    expect(rowText(terminal, 1)).toBe("   CCC");
  });
});
