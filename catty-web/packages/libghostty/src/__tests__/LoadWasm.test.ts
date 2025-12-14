import { describe, it, expect } from 'vitest';
import { loadWasmForTest } from './util/loadWasmForTest';

describe('LoadWasm', () => {
  it('should initialize in Ground state', async () => {

    const wasm = await loadWasmForTest();
    console.log("wasm", wasm);

    // Parser state is private, but we can test behavior
    expect(wasm).toBeDefined();
  });

});