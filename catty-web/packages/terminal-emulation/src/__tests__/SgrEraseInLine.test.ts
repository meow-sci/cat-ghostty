import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("SGR Erase In Line", () => {
  it("should apply current SGR state to erased content", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 3 });

    // Set background color (cyan) and foreground color (black)
    terminal.pushPtyText("\x1b[30m\x1b[46m");

    // Write some text
    terminal.pushPtyText("Command");

    // Erase from cursor to end of line (ESC[K)
    terminal.pushPtyText("\x1b[K");

    const snapshot = terminal.getSnapshot();

    // Check that the text "Command" has the SGR attributes
    expect(snapshot.cells[0][0].ch).toBe("C");
    expect(snapshot.cells[0][0].sgrState?.backgroundColor?.type).toBe("named");
    if (snapshot.cells[0][0].sgrState?.backgroundColor?.type === "named") {
      expect(snapshot.cells[0][0].sgrState?.backgroundColor?.color).toBe("cyan");
    }
    expect(snapshot.cells[0][0].sgrState?.foregroundColor?.type).toBe("named");
    if (snapshot.cells[0][0].sgrState?.foregroundColor?.type === "named") {
      expect(snapshot.cells[0][0].sgrState?.foregroundColor?.color).toBe("black");
    }

    // Check that the erased area (after "Command") also has the SGR attributes
    expect(snapshot.cells[0][7].ch).toBe(" ");
    expect(snapshot.cells[0][7].sgrState?.backgroundColor?.type).toBe("named");
    if (snapshot.cells[0][7].sgrState?.backgroundColor?.type === "named") {
      expect(snapshot.cells[0][7].sgrState?.backgroundColor?.color).toBe("cyan");
    }
    expect(snapshot.cells[0][7].sgrState?.foregroundColor?.type).toBe("named");
    if (snapshot.cells[0][7].sgrState?.foregroundColor?.type === "named") {
      expect(snapshot.cells[0][7].sgrState?.foregroundColor?.color).toBe("black");
    }
  });

  it("should apply SGR state when erasing entire line", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 3 });

    // Write some text first
    terminal.pushPtyText("Hello");

    // Move cursor to beginning and set background color
    terminal.pushPtyText("\r\x1b[41m");

    // Erase entire line (ESC[2K)
    terminal.pushPtyText("\x1b[2K");

    const snapshot = terminal.getSnapshot();

    // Check that the entire line has the red background
    for (let x = 0; x < 10; x++) {
      expect(snapshot.cells[0][x].ch).toBe(" ");
      expect(snapshot.cells[0][x].sgrState?.backgroundColor?.type).toBe("named");
      const color = snapshot.cells[0][x].sgrState?.backgroundColor;
      if (color && color.type === "named") {
        expect(color.color).toBe("red");
      }
    }
  });

  it("should apply SGR state when erasing from beginning to cursor", () => {
    const terminal = new StatefulTerminal({ cols: 10, rows: 3 });

    // Write some text and move cursor to middle
    terminal.pushPtyText("Hello\x1b[3D");

    // Set background color and erase from beginning to cursor (ESC[1K)
    terminal.pushPtyText("\x1b[42m\x1b[1K");

    const snapshot = terminal.getSnapshot();

    // Check that cells from beginning to cursor have green background
    for (let x = 0; x <= 2; x++) { // cursor is at position 2 (after moving 3 left from 5)
      expect(snapshot.cells[0][x].ch).toBe(" ");
      expect(snapshot.cells[0][x].sgrState?.backgroundColor?.type).toBe("named");
      const color = snapshot.cells[0][x].sgrState?.backgroundColor;
      if (color && color.type === "named") {
        expect(color.color).toBe("green");
      }
    }

    // Check that remaining cells still have original content
    expect(snapshot.cells[0][3].ch).toBe("l");
    expect(snapshot.cells[0][4].ch).toBe("o");
  });
});