import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("Enhanced SGR Mode Implementation", () => {
  
  describe("Enhanced underline mode (CSI > 4 ; n m)", () => {
    it("should set double underline with CSI > 4 ; 2 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Send enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;2m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("double");
    });

    it("should set curly underline with CSI > 4 ; 3 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Send enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;3m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("curly");
    });

    it("should set dotted underline with CSI > 4 ; 4 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Send enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;4m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("dotted");
    });

    it("should set dashed underline with CSI > 4 ; 5 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Send enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;5m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("dashed");
    });

    it("should turn off underline with CSI > 4 ; 0 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // First set underline
      terminal.pushPtyText("\x1b[4m");
      expect(terminal.getCurrentSgrState().underline).toBe(true);
      
      // Then turn it off with enhanced mode
      terminal.pushPtyText("\x1b[>4;0m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(false);
      expect(sgrState.underlineStyle).toBe(null);
    });

    it("should set single underline with CSI > 4 ; 1 m", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Send enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;1m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("single");
    });

    it("should gracefully ignore invalid underline types", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Get initial state
      const initialState = terminal.getCurrentSgrState();
      
      // Send invalid enhanced underline mode sequence
      terminal.pushPtyText("\x1b[>4;99m");
      
      // State should remain unchanged
      const finalState = terminal.getCurrentSgrState();
      expect(finalState).toEqual(initialState);
    });

    it("should gracefully ignore other enhanced modes", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Get initial state
      const initialState = terminal.getCurrentSgrState();
      
      // Send unsupported enhanced mode sequence
      terminal.pushPtyText("\x1b[>5;1m");
      
      // State should remain unchanged
      const finalState = terminal.getCurrentSgrState();
      expect(finalState).toEqual(initialState);
    });

    it("should work in combination with other SGR sequences", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Set bold and enhanced curly underline
      terminal.pushPtyText("\x1b[1m\x1b[>4;3m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.bold).toBe(true);
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("curly");
    });

    it("should override regular underline style", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Set regular underline first
      terminal.pushPtyText("\x1b[4m");
      expect(terminal.getCurrentSgrState().underlineStyle).toBe("single");
      
      // Override with enhanced underline
      terminal.pushPtyText("\x1b[>4;2m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(true);
      expect(sgrState.underlineStyle).toBe("double");
    });

    it("should be overridden by regular underline reset", () => {
      const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
      
      // Set enhanced underline
      terminal.pushPtyText("\x1b[>4;3m");
      expect(terminal.getCurrentSgrState().underline).toBe(true);
      
      // Reset with regular SGR 24
      terminal.pushPtyText("\x1b[24m");
      
      const sgrState = terminal.getCurrentSgrState();
      expect(sgrState.underline).toBe(false);
      expect(sgrState.underlineStyle).toBe(null);
    });
  });
});