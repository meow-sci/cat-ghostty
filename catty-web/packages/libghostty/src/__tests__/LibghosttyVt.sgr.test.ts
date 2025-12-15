import { describe, it, expect } from 'vitest';

import { loadWasmForTest } from './util/loadWasmForTest';
import { getLogger } from '@catty/log';
import { parseSgrWithWasm } from '../terminal/sgr';


describe('libghostty-vt', () => {
  describe('sgr', () => {
    it('foreground color ', async () => {


      const wasm = await loadWasmForTest();

      // const buf = Buffer.from("\x1b[38;5;11;m");
      // const buf = Buffer.from("38;5;11");
      // const buf = Buffer.from("\x1b[38;2;255;0;0;m");
      // const buf = Buffer.from("38;2;255;0;0");
      const result = parseSgrWithWasm(getLogger(), wasm, "58;2;255;0;0");
      // const result = parseSgrWithWasm(getLogger(), wasm, "0");

      // overline
      // const result = parseSgrWithWasm(getLogger(), wasm, "55");

      console.log('SGR parse result:', JSON.stringify(result));



      // \x1b[38;5;11;mhi\x1b[0m

    });

  });
});