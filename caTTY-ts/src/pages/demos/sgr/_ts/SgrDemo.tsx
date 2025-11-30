import { useLayoutEffect, type ChangeEvent } from "react";
import { atom } from "nanostores";
import { useStore } from "@nanostores/react";

import type { GhosttyVtInstance } from "../../../../ts/ghostty-vt";
import { StatefulSgrParser } from "./pure/StatefulSgrParser";

// liar liar pants on fire
let parser: StatefulSgrParser = null as any;

const $output = atom<string>("");

export interface KeyEncodeDemoProps {
  wasm: GhosttyVtInstance;
}

export function SgrDemo(props: KeyEncodeDemoProps) {

  const { wasm } = props;

  useLayoutEffect(() => {
    if (!parser) {
      parser = new StatefulSgrParser(wasm);
      displaySgrParsingResult((document.getElementById("input")! as HTMLInputElement).value);
    }
  }, []);

  const output = useStore($output);

  return (
    <main>
      <section id="input-section">
        <p><label htmlFor="input">SGR sequence</label></p>
        <input
          id="input"
          type="text"
          autoFocus
          autoComplete="off"
          autoCorrect="off"
          autoCapitalize="off"
          spellCheck="false"
          onChange={onChange}
          defaultValue="4:3;38;2;51;51;51;48;2;170;170;170;58;2;255;97;136"
          style={{ width: "80ch" }}
        />
      </section>
      <section id="output-section">
        <pre id="output">{output}</pre>
      </section>
    </main>
  );

}

//
// UI INTEGRATION
//

function onChange(event: ChangeEvent<HTMLInputElement>) {
  displaySgrParsingResult(event.currentTarget.value);
}


function displaySgrParsingResult(sequence: string) {

  // do actual encoding using stateful encoder + wasm
  const encoded = parser.parse(sequence);

  // show results
  $output.set(encoded);
}
