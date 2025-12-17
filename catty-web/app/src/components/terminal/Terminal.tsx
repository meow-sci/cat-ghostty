import { useEffect, useMemo, useRef, useState } from "react";

import { StatefulTerminal } from "../../ts/terminal/StatefulTerminal";
import { TerminalController } from "../../ts/terminal/TerminalController";

export function Terminal() {

  const terminalRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const traceRef = useRef<HTMLPreElement | null>(null);
  const [size, _setSize] = useState<[number, number]>(() => [85, 24]);

  useEffect(() => {

    const displayElement = terminalRef.current;
    const inputElement = inputRef.current;
    const traceElement = traceRef.current;

    if (!displayElement || !inputElement) {
      return;
    }

    const terminal = new StatefulTerminal({ cols: size[0], rows: size[1] });
    const controller = new TerminalController({
      terminal,
      displayElement,
      inputElement,
      traceElement: traceElement ?? undefined,
      cols: size[0],
      rows: size[1],
    });

    return () => {
      controller.dispose();
    };
  }, []);

  const style = useMemo<React.CSSProperties>(() => ({
    "--terminal-width": size[0],
    "--terminal-height": size[1],
  } as React.CSSProperties), [size])

  return (
    <>
      <div id="terminal" ref={terminalRef} style={style} />
      <pre id="trace" ref={traceRef} style={style} />
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