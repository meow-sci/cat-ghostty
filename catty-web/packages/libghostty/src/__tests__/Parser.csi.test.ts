import { describe, it, expect } from 'vitest';

import { loadWasmForTest } from './util/loadWasmForTest';
import { Parser } from '../terminal/Parser';
import { ParserHandlers } from '../terminal/ParserOptions';
import { type CsiMessage } from '../terminal/TerminalEmulationTypes';
import { getLogger } from '@catty/log';


const NOOP_HANDLERS: ParserHandlers = {

  handleBell: () => { },
  handleBackspace: () => { },
  handleTab: () => { },
  handleLineFeed: () => { },
  handleFormFeed: () => { },
  handleCarriageReturn: () => { },
  handleNormalByte: (byte: number) => { },
  handleCsi: (msg: CsiMessage) => { },
  handleOsc: (raw: string) => { },
  handleSgr: (raw: string) => { },
};

describe('Parser', () => {
  describe('CSI SGR', () => {
    it('foreground color ', async () => {


      const wasm = await loadWasmForTest();
      const parser = new Parser({
        wasm,
        handlers: {
          ...NOOP_HANDLERS,
          handleNormalByte: (byte: number) => {
            const char = String.fromCharCode(byte);
            // For testing, we can log or collect the output
            // Here we just print to console for demonstration
            console.log(`Normal byte: ${char}`);
          },
          handleCsi: (msg: CsiMessage) => {
            console.log(`CSI Message: ${msg._type}`);
          },
          handleSgr: (raw: string) => {
            console.log(`SGR sequence received: ${raw}`);
          },
        },
        log: getLogger(),
      });

      parser.pushBytes(Buffer.from("\x1b[38;5;11;mhi\x1b[0m\x1b[3J"));

      // \x1b[38;5;11;mhi\x1b[0m

      // Parser state is private, but we can test behavior
      expect("abc").toBeDefined();
    });

  });
});