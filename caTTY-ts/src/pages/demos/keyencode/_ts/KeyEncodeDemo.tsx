import { useLayoutEffect, type KeyboardEvent } from "react";
import { atom } from "nanostores";
import { useStore } from "@nanostores/react";

import type { GhosttyVtInstance } from "../../../../ts/ghostty-vt";
import { StatefulEncoder } from "./pure/StatefulEncoder";
import type { KeyEvent } from "./pure/KeyEvent";
import { toKeyEvent } from "./ui/toKeyEvent";
import type { KeyEncoderResult } from "./pure/KeyEncoderResult";

// liar liar pants on fire
let encoder: StatefulEncoder = null as any;

const $output = atom<string>("");

export interface KeyEncodeDemoProps {
  wasm: GhosttyVtInstance;
}

export function KeyEncodeDemo(props: KeyEncodeDemoProps) {

  const { wasm } = props;

  useLayoutEffect(() => {
    if (!encoder) {
      encoder = new StatefulEncoder(wasm, getKittyFlags());
    }
  }, []);

  const output = useStore($output);

  return (
    <main>
      <section id="input-section">
        <input
          id="input"
          type="text"
          autoFocus
          value=""
          autoComplete="off"
          autoCorrect="off"
          autoCapitalize="off"
          spellCheck="false"
          onKeyDown={onKeyDown}
        />
        <section id="checkboxes">
          <label><input type="checkbox" id="flag_disambiguate" defaultChecked onChange={updateKittyFlagsOnEncoder} /> Disambiguate</label>
          <label><input type="checkbox" id="flag_report_events" defaultChecked onChange={updateKittyFlagsOnEncoder} /> Report Events</label>
          <label><input type="checkbox" id="flag_report_alternates" defaultChecked onChange={updateKittyFlagsOnEncoder} /> Report Alternates</label>
          <label><input type="checkbox" id="flag_report_all_as_escapes" defaultChecked onChange={updateKittyFlagsOnEncoder} /> Report All As Escapes</label>
          <label><input type="checkbox" id="flag_report_text" defaultChecked onChange={updateKittyFlagsOnEncoder} /> Report Text</label>
        </section>
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

function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {

  // Allow modifier keys to be pressed without clearing input
  // Only prevent default for keys we want to capture
  if (event.key !== 'Tab' && event.key !== 'F5') {
    event.preventDefault();
  }

  displayEncoding(toKeyEvent(event));
}


function displayEncoding(event: KeyEvent) {

  // do actual encoding using stateful encoder + wasm
  const encoded = encoder.encodeKeyEvent(event);

  // show results
  $output.set(buildHumanOutput(event, encoded));
}


function buildHumanOutput(event: KeyEvent, encoded: KeyEncoderResult | null) {

  const actionName = "press";

  let output = `Action: ${actionName}\n`;
  output += `Key: ${event.key} (code: ${event.code})\n`;
  output += `Modifiers: `;
  const mods = [];

  if (event.shiftKey) mods.push('Shift');
  if (event.ctrlKey) mods.push('Ctrl');
  if (event.altKey) mods.push('Alt');
  if (event.metaKey) mods.push('Meta');

  output += mods.length ? mods.join('+') : 'none';
  output += '\n';

  // Show Kitty flags state
  const flags = [];
  if ((document.getElementById('flag_disambiguate')! as HTMLInputElement).checked) flags.push('Disambiguate');
  if ((document.getElementById('flag_report_events')! as HTMLInputElement).checked) flags.push('Report Events');
  if ((document.getElementById('flag_report_alternates')! as HTMLInputElement).checked) flags.push('Report Alternates');
  if ((document.getElementById('flag_report_all_as_escapes')! as HTMLInputElement).checked) flags.push('Report All As Escapes');
  if ((document.getElementById('flag_report_text')! as HTMLInputElement).checked) flags.push('Report Text');

  output += 'Kitty Flags:\n';
  if (flags.length) {
    flags.forEach(flag => output += `  - ${flag}\n`);
  } else {
    output += '  - none\n';
  }
  output += '\n';

  if (encoded) {
    output += `Encoded ${encoded.bytes.length} bytes\n`;
    output += `Hex: ${encoded.hex}\n`;
    output += `String: ${encoded.string}`;
  } else {
    output += 'No encoding for this key event';
  }

  return output;
}

function getKittyFlags(): number {
  let flags = 0;
  if ((document.getElementById('flag_disambiguate')! as HTMLInputElement).checked) flags |= 0x01;
  if ((document.getElementById('flag_report_events')! as HTMLInputElement).checked) flags |= 0x02;
  if ((document.getElementById('flag_report_alternates')! as HTMLInputElement).checked) flags |= 0x04;
  if ((document.getElementById('flag_report_all_as_escapes')! as HTMLInputElement).checked) flags |= 0x08;
  if ((document.getElementById('flag_report_text')! as HTMLInputElement).checked) flags |= 0x10;
  return flags;
}

function updateKittyFlagsOnEncoder() {
  encoder.kittyFlags = getKittyFlags();
}