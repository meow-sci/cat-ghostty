import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("Scrollback", () => {
  it("should append scrolled-off lines to scrollback on primary screen", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2, scrollbackLimit: 100 });

    terminal.pushPtyText("a\r\nb\r\nc\r\n");

    expect(terminal.getScrollbackRowCount()).toBe(2);

    const viewport = terminal.getViewportRows(0, 2);
    expect(viewport[0][0].ch).toBe("a");
    expect(viewport[1][0].ch).toBe("b");

    const snap = terminal.getSnapshot();
    expect(snap.cells[0][0].ch).toBe("c");
  });

  it("should not append scrollback while alternate screen is active", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2, scrollbackLimit: 100 });

    terminal.pushPtyText("one\r\ntwo\r\nthree\r\n");
    const baseline = terminal.getScrollbackRowCount();
    expect(baseline).toBeGreaterThan(0);

    terminal.pushPtyText("\x1b[?47h");
    expect(terminal.isAlternateScreenActive()).toBe(true);

    terminal.pushPtyText("alt1\r\nalt2\r\nalt3\r\n");
    expect(terminal.getScrollbackRowCount()).toBe(baseline);
  });

  it("should clear scrollback with ED 3 (CSI 3 J)", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 2, scrollbackLimit: 100 });

    terminal.pushPtyText("a\r\nb\r\nc\r\n");
    expect(terminal.getScrollbackRowCount()).toBe(2);

    terminal.pushPtyText("\x1b[3J");
    expect(terminal.getScrollbackRowCount()).toBe(0);
  });
});
