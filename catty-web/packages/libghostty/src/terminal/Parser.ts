import { GhosttyVtInstance } from "../ghostty-vt";
import { HandleBell, HandleBackspace, HandleTab, HandleLineFeed, HandleFormFeed, HandleCarriageReturn } from "./ParserHandlers";

type State = "normal" | "escaped";


interface ParserOptions {

  wasm: GhosttyVtInstance;

  handlers: {
    handleBell: HandleBell;
    handleBackspace: HandleBackspace;
    handleTab: HandleTab;
    handleLineFeed: HandleLineFeed;
    handleFormFeed: HandleFormFeed;
    handleCarriageReturn: HandleCarriageReturn;
  };

}


export class Parser {

  private wasm: GhosttyVtInstance;
  private handlers: ParserOptions["handlers"];


  private state: State = "normal";
  private escapeSequence: number[] = [];

  constructor(options: ParserOptions) {
    this.wasm = options.wasm;
    this.handlers = options.handlers;
  }

  public pushBytes(data: Uint8Array): void {
    for (let i = 0; i < data.length; i++) {
      const byte = data[i];
      this.pushByte(byte);
    }
  }

  public pushByte(data: number): void {
    this.processByte(data);
  }

  private processByte(byte: number): void {

    if (byte === 0x1b) {
      // start of a control code
      this.state = "escaped";
      this.escapeSequence.push(byte);
      return;
    }


    // Handle control characters (0x00-0x1F, 0x7F) in most states
    if (byte < 0x20 || byte === 0x7F) {
      this.handleControl(byte);
      return;
    }

  }

  private start
}
