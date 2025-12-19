import { describe, it, expect } from "vitest";

import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("Device Status Report (DSR)", () => {
  it("should respond to CSI 5 n with CSI 0 n", () => {
    const responses: string[] = [];
    const terminal = new StatefulTerminal({
      cols: 10,
      rows: 5,
      onResponse: (response: string) => {
        responses.push(response);
      },
    });

    terminal.pushPtyText("\x1b[5n");

    expect(responses).toEqual(["\x1b[0n"]);
  });
});
