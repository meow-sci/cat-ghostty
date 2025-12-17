import { useEffect, useMemo, useRef, useState } from "react";

import { StatefulTerminal } from "../../ts/terminal/StatefulTerminal";
import { TerminalController } from "../../ts/terminal/TerminalController";

export function Terminal() {

  const terminalRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);
  const traceRef = useRef<HTMLPreElement | null>(null);
  const controllerRef = useRef<TerminalController | null>(null);
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

    controllerRef.current = controller;

    return () => {
      controller.dispose();
      controllerRef.current = null;
    };
  }, []);

  const style = useMemo<React.CSSProperties>(() => ({
    "--terminal-width": size[0],
    "--terminal-height": size[1],
  } as React.CSSProperties), [size])

  const onClearTrace = () => {
    controllerRef.current?.clearTrace();
  };

  const onCopyRenderedTrace = async () => {
    const text = traceRef.current?.textContent ?? "";
    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      console.error("Failed to copy rendered trace", err);
    }
  };

  const onCopyRawTraceJsonLines = async () => {
    const chunks = controllerRef.current?.getTraceChunks() ?? [];
    let text = "";
    try {
      text = chunks.map((c) => JSON.stringify(c)).join("\n");
    } catch (err) {
      console.error("Failed to stringify raw trace chunks", err);
      return;
    }

    try {
      await navigator.clipboard.writeText(text);
    } catch (err) {
      console.error("Failed to copy raw trace JSON lines", err);
    }
  };

  return (
    <>
      <div id="terminal" ref={terminalRef} style={style} />
      <div id="trace-controls" style={style}>
        <button type="button" onClick={onClearTrace}>
          Clear trace
        </button>
        <button type="button" onClick={onCopyRenderedTrace}>
          Copy rendered
        </button>
        <button type="button" onClick={onCopyRawTraceJsonLines}>
          Copy raw JSON lines
        </button>
      </div>
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