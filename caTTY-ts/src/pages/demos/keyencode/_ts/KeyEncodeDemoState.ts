import { atom } from "nanostores";

import type { GhosttyVtInstance } from "../../../../ts/ghostty-vt";
import { loadWasm } from "../../../../ts/terminal/wasm/LoadWasm";

export const $wasm = atom<GhosttyVtInstance | null>(null);
loadWasm().then(o => $wasm.set(o));
