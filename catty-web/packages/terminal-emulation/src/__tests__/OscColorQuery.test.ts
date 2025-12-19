import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";
import { getLogger } from "@catty/log";

describe("OSC Color Query Sequences for vi compatibility", () => {
  describe("OSC 10;? (query foreground color)", () => {
    it("should generate correct foreground color response with default colors", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send OSC 10;? query with BEL termination
      terminal.pushPtyText("\x1b]10;?\x07");

      // Should respond with default foreground color in rgb:rrrr/gggg/bbbb format
      expect(responseReceived).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
    });

    it("should generate correct foreground color response with SGR foreground set", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Set foreground color to red (SGR 31)
      terminal.pushPtyText("\x1b[31m");
      
      // Send OSC 10;? query
      terminal.pushPtyText("\x1b]10;?\x07");

      // Should respond with red color in rgb format
      expect(responseReceived).toBe("\x1b]10;rgb:aaaa/0000/0000\x07");
    });

    it("should handle OSC 10;? with ST termination", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send OSC 10;? query with ST termination (ESC \)
      terminal.pushPtyText("\x1b]10;?\x1b\\");

      // Should respond with default foreground color
      expect(responseReceived).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
    });

    it("should handle 24-bit RGB foreground color", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Set 24-bit RGB foreground color (SGR 38;2;255;128;64)
      terminal.pushPtyText("\x1b[38;2;255;128;64m");
      
      // Send OSC 10;? query
      terminal.pushPtyText("\x1b]10;?\x07");

      // Should respond with the RGB color converted to 16-bit format
      expect(responseReceived).toBe("\x1b]10;rgb:ffff/8080/4040\x07");
    });

    it("should handle 256-color palette foreground", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Set 256-color palette foreground (SGR 38;5;196 = bright red)
      terminal.pushPtyText("\x1b[38;5;196m");
      
      // Send OSC 10;? query
      terminal.pushPtyText("\x1b]10;?\x07");

      // Should respond with the indexed color converted to RGB format
      // Color 196 in 256-color palette is RGB(255, 0, 0)
      expect(responseReceived).toBe("\x1b]10;rgb:ffff/0000/0000\x07");
    });
  });

  describe("OSC 11;? (query background color)", () => {
    it("should generate correct background color response with default colors", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send OSC 11;? query with BEL termination
      terminal.pushPtyText("\x1b]11;?\x07");

      // Should respond with default background color (black)
      expect(responseReceived).toBe("\x1b]11;rgb:0000/0000/0000\x07");
    });

    it("should generate correct background color response with SGR background set", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Set background color to blue (SGR 44)
      terminal.pushPtyText("\x1b[44m");
      
      // Send OSC 11;? query
      terminal.pushPtyText("\x1b]11;?\x07");

      // Should respond with blue color in rgb format
      expect(responseReceived).toBe("\x1b]11;rgb:0000/0000/aaaa\x07");
    });

    it("should handle OSC 11;? with ST termination", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send OSC 11;? query with ST termination (ESC \)
      terminal.pushPtyText("\x1b]11;?\x1b\\");

      // Should respond with default background color
      expect(responseReceived).toBe("\x1b]11;rgb:0000/0000/0000\x07");
    });

    it("should handle 24-bit RGB background color", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Set 24-bit RGB background color (SGR 48;2;64;128;255)
      terminal.pushPtyText("\x1b[48;2;64;128;255m");
      
      // Send OSC 11;? query
      terminal.pushPtyText("\x1b]11;?\x07");

      // Should respond with the RGB color converted to 16-bit format
      expect(responseReceived).toBe("\x1b]11;rgb:4040/8080/ffff\x07");
    });
  });

  describe("Error handling and edge cases", () => {
    it("should handle malformed OSC color query gracefully", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send malformed OSC sequence (missing semicolon)
      terminal.pushPtyText("\x1b]10?\x07");

      // Should not generate a response for malformed sequence
      expect(responseReceived).toBe("");
    });

    it("should handle OSC color query with extra parameters", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send OSC with extra parameters (should be ignored)
      terminal.pushPtyText("\x1b]10;?;extra\x07");

      // Should still respond correctly (extra parameters ignored)
      expect(responseReceived).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
    });

    it("should handle incomplete OSC sequence gracefully", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      let responseReceived = "";
      terminal.onResponse((response) => {
        responseReceived = response;
      });

      // Send incomplete OSC sequence (no terminator)
      terminal.pushPtyText("\x1b]10;?");

      // Should not generate a response for incomplete sequence
      expect(responseReceived).toBe("");
    });

    it("should handle multiple color queries in sequence", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      const responses: string[] = [];
      terminal.onResponse((response) => {
        responses.push(response);
      });

      // Send multiple color queries
      terminal.pushPtyText("\x1b]10;?\x07\x1b]11;?\x07");

      // Should generate two responses
      expect(responses).toHaveLength(2);
      expect(responses[0]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
      expect(responses[1]).toBe("\x1b]11;rgb:0000/0000/0000\x07");
    });
  });

  describe("vi compatibility scenarios", () => {
    it("should handle vi's typical color query sequence", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      const responses: string[] = [];
      terminal.onResponse((response) => {
        responses.push(response);
      });

      // Simulate vi querying both foreground and background colors
      // This is what vi typically does when starting up
      terminal.pushPtyText("\x1b]11;?\x07"); // Query background first
      terminal.pushPtyText("\x1b]10;?\x07"); // Then query foreground

      expect(responses).toHaveLength(2);
      expect(responses[0]).toBe("\x1b]11;rgb:0000/0000/0000\x07"); // Background
      expect(responses[1]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07"); // Foreground
    });

    it("should work correctly after setting colors via SGR", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      const responses: string[] = [];
      terminal.onResponse((response) => {
        responses.push(response);
      });

      // Set some colors like vi might do
      terminal.pushPtyText("\x1b[32m"); // Green foreground
      terminal.pushPtyText("\x1b[40m"); // Black background (explicit)
      
      // Query colors
      terminal.pushPtyText("\x1b]10;?\x07");
      terminal.pushPtyText("\x1b]11;?\x07");

      expect(responses).toHaveLength(2);
      expect(responses[0]).toBe("\x1b]10;rgb:0000/aaaa/0000\x07"); // Green foreground
      expect(responses[1]).toBe("\x1b]11;rgb:0000/0000/0000\x07"); // Black background
    });

    it("should handle color queries mixed with other OSC sequences", () => {
      const terminal = new StatefulTerminal({
        cols: 80,
        rows: 24,
      });

      const responses: string[] = [];
      terminal.onResponse((response) => {
        responses.push(response);
      });

      // Mix color queries with title setting (common in vi)
      terminal.pushPtyText("\x1b]2;vi - test.txt\x07"); // Set title
      terminal.pushPtyText("\x1b]11;?\x07"); // Query background
      terminal.pushPtyText("\x1b]0;vi session\x07"); // Set title and icon
      terminal.pushPtyText("\x1b]10;?\x07"); // Query foreground

      // Should only get responses for color queries
      expect(responses).toHaveLength(2);
      expect(responses[0]).toBe("\x1b]11;rgb:0000/0000/0000\x07");
      expect(responses[1]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
    });
  });
});