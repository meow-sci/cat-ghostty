import { describe, it, expect } from "vitest";
import { StatefulTerminal } from "../terminal/StatefulTerminal";

describe("Window Manipulation", () => {
  it("should handle title stack operations (push/pop window title)", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial title
    terminal.pushPtyText("\x1b]2;Initial Title\x07");
    expect(terminal.getWindowTitle()).toBe("Initial Title");
    
    // Push title to stack
    terminal.pushPtyText("\x1b[22;2t");
    
    // Change title
    terminal.pushPtyText("\x1b]2;New Title\x07");
    expect(terminal.getWindowTitle()).toBe("New Title");
    
    // Pop title from stack
    terminal.pushPtyText("\x1b[23;2t");
    expect(terminal.getWindowTitle()).toBe("Initial Title");
  });

  it("should handle icon name stack operations (push/pop icon name)", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial icon name
    terminal.pushPtyText("\x1b]1;Initial Icon\x07");
    expect(terminal.getIconName()).toBe("Initial Icon");
    
    // Push icon name to stack
    terminal.pushPtyText("\x1b[22;1t");
    
    // Change icon name
    terminal.pushPtyText("\x1b]1;New Icon\x07");
    expect(terminal.getIconName()).toBe("New Icon");
    
    // Pop icon name from stack
    terminal.pushPtyText("\x1b[23;1t");
    expect(terminal.getIconName()).toBe("Initial Icon");
  });

  it("should handle empty stack gracefully", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial title
    terminal.pushPtyText("\x1b]2;Test Title\x07");
    expect(terminal.getWindowTitle()).toBe("Test Title");
    
    // Try to pop from empty stack - should not change title
    terminal.pushPtyText("\x1b[23;2t");
    expect(terminal.getWindowTitle()).toBe("Test Title");
    
    // Same for icon name
    terminal.pushPtyText("\x1b]1;Test Icon\x07");
    expect(terminal.getIconName()).toBe("Test Icon");
    
    // Try to pop from empty stack - should not change icon name
    terminal.pushPtyText("\x1b[23;1t");
    expect(terminal.getIconName()).toBe("Test Icon");
  });

  it("should handle multiple push/pop operations", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial title and push multiple times
    terminal.pushPtyText("\x1b]2;Title 1\x07");
    terminal.pushPtyText("\x1b[22;2t"); // Push "Title 1"
    
    terminal.pushPtyText("\x1b]2;Title 2\x07");
    terminal.pushPtyText("\x1b[22;2t"); // Push "Title 2"
    
    terminal.pushPtyText("\x1b]2;Title 3\x07");
    expect(terminal.getWindowTitle()).toBe("Title 3");
    
    // Pop in reverse order
    terminal.pushPtyText("\x1b[23;2t"); // Should restore "Title 2"
    expect(terminal.getWindowTitle()).toBe("Title 2");
    
    terminal.pushPtyText("\x1b[23;2t"); // Should restore "Title 1"
    expect(terminal.getWindowTitle()).toBe("Title 1");
    
    // Stack should now be empty
    terminal.pushPtyText("\x1b[23;2t"); // Should not change title
    expect(terminal.getWindowTitle()).toBe("Title 1");
  });

  it("should clear stacks on reset", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set title and push to stack
    terminal.pushPtyText("\x1b]2;Test Title\x07");
    terminal.pushPtyText("\x1b[22;2t");
    
    // Set icon and push to stack
    terminal.pushPtyText("\x1b]1;Test Icon\x07");
    terminal.pushPtyText("\x1b[22;1t");
    
    // Reset terminal
    terminal.reset();
    
    // Stacks should be cleared, so popping should not restore anything
    terminal.pushPtyText("\x1b]2;New Title\x07");
    terminal.pushPtyText("\x1b]1;New Icon\x07");
    
    terminal.pushPtyText("\x1b[23;2t"); // Should not change title
    terminal.pushPtyText("\x1b[23;1t"); // Should not change icon
    
    expect(terminal.getWindowTitle()).toBe("New Title");
    expect(terminal.getIconName()).toBe("New Icon");
  });

  it("should ignore unknown window manipulation operations", () => {
    const terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    
    // Set initial title
    terminal.pushPtyText("\x1b]2;Test Title\x07");
    expect(terminal.getWindowTitle()).toBe("Test Title");
    
    // Send unknown window manipulation sequence
    terminal.pushPtyText("\x1b[99;1t");
    
    // Title should remain unchanged
    expect(terminal.getWindowTitle()).toBe("Test Title");
  });

  it("should mark title stack operations as implemented", () => {
    // Create a simple handler to capture CSI messages
    let capturedCsiMessage: any = null;
    
    const terminal = new StatefulTerminal({
      cols: 80,
      rows: 24,
      onChunk: (chunk) => {
        if (chunk._type === "trace.csi") {
          capturedCsiMessage = chunk.msg;
        }
      }
    });
    
    // Test title stack operations - these should be marked as implemented
    terminal.pushPtyText("\x1b[22;2t"); // Push window title
    expect(capturedCsiMessage).not.toBeNull();
    expect(capturedCsiMessage._type).toBe("csi.windowManipulation");
    expect(capturedCsiMessage.implemented).toBe(true);
    
    capturedCsiMessage = null;
    terminal.pushPtyText("\x1b[22;1t"); // Push icon name
    expect(capturedCsiMessage).not.toBeNull();
    expect(capturedCsiMessage._type).toBe("csi.windowManipulation");
    expect(capturedCsiMessage.implemented).toBe(true);
    
    capturedCsiMessage = null;
    terminal.pushPtyText("\x1b[23;2t"); // Pop window title
    expect(capturedCsiMessage).not.toBeNull();
    expect(capturedCsiMessage._type).toBe("csi.windowManipulation");
    expect(capturedCsiMessage.implemented).toBe(true);
    
    capturedCsiMessage = null;
    terminal.pushPtyText("\x1b[23;1t"); // Pop icon name
    expect(capturedCsiMessage).not.toBeNull();
    expect(capturedCsiMessage._type).toBe("csi.windowManipulation");
    expect(capturedCsiMessage.implemented).toBe(true);
    
    // Test unknown window manipulation operation - should not be implemented
    capturedCsiMessage = null;
    terminal.pushPtyText("\x1b[99;1t"); // Unknown operation
    expect(capturedCsiMessage).not.toBeNull();
    expect(capturedCsiMessage._type).toBe("csi.windowManipulation");
    expect(capturedCsiMessage.implemented).toBe(false);
  });
});