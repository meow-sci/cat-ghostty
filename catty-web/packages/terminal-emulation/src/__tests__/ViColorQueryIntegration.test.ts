import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";
import { getLogger } from "@catty/log";

describe("Vi Color Query Integration", () => {
  it("should handle vi startup color query sequence correctly", () => {
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
    });

    const responses: string[] = [];
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Simulate vi startup sequence - query background and foreground colors
    // This is what vi does to determine if it's running in a dark or light terminal
    terminal.pushPtyText("\x1b]11;?\x07"); // Query background color
    terminal.pushPtyText("\x1b]10;?\x07"); // Query foreground color

    // Should get two responses
    expect(responses).toHaveLength(2);
    
    // Background should be black (dark theme default)
    expect(responses[0]).toBe("\x1b]11;rgb:0000/0000/0000\x07");
    
    // Foreground should be light gray (dark theme default)
    expect(responses[1]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07");
  });

  it("should handle color queries after SGR color changes", () => {
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
    });

    const responses: string[] = [];
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Set some colors like vi might do for syntax highlighting
    terminal.pushPtyText("\x1b[32m"); // Green foreground
    terminal.pushPtyText("\x1b[44m"); // Blue background
    
    // Query the colors
    terminal.pushPtyText("\x1b]10;?\x07"); // Query foreground
    terminal.pushPtyText("\x1b]11;?\x07"); // Query background

    expect(responses).toHaveLength(2);
    
    // Should return the SGR-set colors, not theme defaults
    expect(responses[0]).toBe("\x1b]10;rgb:0000/aaaa/0000\x07"); // Green
    expect(responses[1]).toBe("\x1b]11;rgb:0000/0000/aaaa\x07"); // Blue
  });

  it("should handle color queries with 24-bit RGB colors", () => {
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
    });

    const responses: string[] = [];
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Set 24-bit RGB colors
    terminal.pushPtyText("\x1b[38;2;255;128;64m"); // Orange foreground
    terminal.pushPtyText("\x1b[48;2;32;64;128m");  // Dark blue background
    
    // Query the colors
    terminal.pushPtyText("\x1b]10;?\x07");
    terminal.pushPtyText("\x1b]11;?\x07");

    expect(responses).toHaveLength(2);
    
    // Should return the RGB colors converted to 16-bit format
    expect(responses[0]).toBe("\x1b]10;rgb:ffff/8080/4040\x07"); // Orange
    expect(responses[1]).toBe("\x1b]11;rgb:2020/4040/8080\x07"); // Dark blue
  });

  it("should handle color queries mixed with other terminal operations", () => {
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
    });

    const responses: string[] = [];
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Simulate a complex vi session with mixed operations
    terminal.pushPtyText("\x1b]2;vi - test.txt\x07"); // Set title
    terminal.pushPtyText("\x1b[?1049h"); // Switch to alternate screen
    terminal.pushPtyText("\x1b[31m"); // Red foreground
    terminal.pushPtyText("Hello World"); // Write some text
    terminal.pushPtyText("\x1b]10;?\x07"); // Query foreground color
    terminal.pushPtyText("\x1b[H"); // Move cursor to home
    terminal.pushPtyText("\x1b]11;?\x07"); // Query background color
    terminal.pushPtyText("\x1b[?1049l"); // Switch back to normal screen

    // Should only get responses for color queries
    expect(responses).toHaveLength(2);
    expect(responses[0]).toBe("\x1b]10;rgb:aaaa/0000/0000\x07"); // Red foreground
    expect(responses[1]).toBe("\x1b]11;rgb:0000/0000/0000\x07"); // Default background
    
    // Verify terminal state is correct
    const snapshot = terminal.getSnapshot();
    expect(snapshot.cursorX).toBe(0); // Cursor should be at home position
    expect(snapshot.cursorY).toBe(0);
    expect(snapshot.windowProperties.title).toBe("vi - test.txt"); // Title should be set
  });

  it("should handle malformed color queries gracefully", () => {
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
    });

    const responses: string[] = [];
    terminal.onResponse((response) => {
      responses.push(response);
    });

    // Send various malformed sequences
    terminal.pushPtyText("\x1b]10?\x07"); // Missing semicolon
    terminal.pushPtyText("\x1b]10;?extra\x07"); // Extra text after ?
    terminal.pushPtyText("\x1b]10;?\x1b\\"); // ST termination
    terminal.pushPtyText("\x1b]99;?\x07"); // Invalid OSC command

    // Should only respond to valid sequences
    expect(responses).toHaveLength(1);
    expect(responses[0]).toBe("\x1b]10;rgb:aaaa/aaaa/aaaa\x07"); // ST termination should work
  });
});