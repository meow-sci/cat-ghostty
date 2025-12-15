import { useEffect, useRef } from "react";

import { StatefulTerminal } from "../../ts/terminal/StatefulTerminal";
import { TerminalController } from "../../ts/terminal/TerminalController";

export function Terminal() {
  const terminalRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    const displayElement = terminalRef.current;
    const inputElement = inputRef.current;

    if (!displayElement || !inputElement) {
      return;
    }

    const terminal = new StatefulTerminal({ cols: 80, rows: 40 });
    const controller = new TerminalController({
      terminal,
      displayElement,
      inputElement,
      cols: 80,
      rows: 40,
    });

    return () => {
      controller.dispose();
    };
  }, []);

  return (
    <>
      <div id="terminal" ref={terminalRef} />
      <input
        id="input"
        ref={inputRef}
        type="text"
        autoComplete="off"
        autoCorrect="off"
        autoCapitalize="off"
        spellCheck={false}
      />
    </>
  );
}