import type { GhosttyVtInstance } from "../../ghostty-vt";
import { loadWasm } from "./LoadWasm";

let cached: GhosttyVtInstance | null = null;
let promise: Promise<GhosttyVtInstance> | null = null;

export async function wasm(): Promise<GhosttyVtInstance> {
  if (cached) {
    return cached;
  }
  if (!promise) {
    promise = loadWasm();
  }
  return promise;
}
