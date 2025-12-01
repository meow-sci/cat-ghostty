import { useLayoutEffect, type ChangeEvent } from "react";
import { atom } from "nanostores";
import { useStore } from "@nanostores/react";

import type { GhosttyVtInstance } from "../../../../ts/ghostty-vt";
import { StatefulOscParser } from "./pure/StatefulOscParser";

// liar liar pants on fire
let parser: StatefulOscParser = null as any;

const $output = atom<string>("");

export interface KeyEncodeDemoProps {
  wasm: GhosttyVtInstance;
}

export function OscDemo(props: KeyEncodeDemoProps) {

  const { wasm } = props;

  useLayoutEffect(() => {
    if (!parser) {
      parser = new StatefulOscParser(wasm);
      displaySgrParsingResult((document.getElementById("input")! as HTMLInputElement).value);
    }
  }, []);

  const output = useStore($output);

  return (
    <main>
      <section id="input-section">
        <p><label htmlFor="input">OSC body (e.g. 0;New Title)</label></p>
        <input
          id="input"
          type="text"
          autoFocus
          autoComplete="off"
          autoCorrect="off"
          autoCapitalize="off"
          spellCheck="false"
          onChange={onChange}
          defaultValue="0;hello world"
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
