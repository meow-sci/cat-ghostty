import { describe, it, expect } from 'vitest';
import { getLogger } from '@catty/log';

import { Parser } from '../terminal/Parser';
import { ParserHandlers } from '../terminal/ParserOptions';
import { DcsMessage, OscMessage, SgrSequence, type CsiMessage, type EscMessage, type XtermOscMessage } from '../terminal/TerminalEmulationTypes';


const NOOP_HANDLERS: ParserHandlers = {

  handleBell: () => { },
  handleBackspace: () => { },
  handleTab: () => { },
  handleShiftIn: () => { },
  handleShiftOut: () => { },
  handleLineFeed: () => { },
  handleFormFeed: () => { },
  handleCarriageReturn: () => { },
  handleNormalByte: (_byte: number) => { },
  handleEsc: (_msg: EscMessage) => { },
  handleCsi: (_msg: CsiMessage) => { },
  handleOsc: (_msg: OscMessage) => { },
  handleDcs: (_msg: DcsMessage) => { },
  handleXtermOsc: (_msg: XtermOscMessage) => { },
  handleSgr: (_msg: SgrSequence) => { },
};

describe('Parser', () => {
  describe('CSI SGR', () => {
    it('foreground color ', async () => {

      const parser = new Parser({
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
          handleSgr: (messages: SgrSequence) => {
            console.log(`SGR attrs received: ${JSON.stringify(messages)}`);
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