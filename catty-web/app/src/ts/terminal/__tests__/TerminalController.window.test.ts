import { describe, it, expect, beforeEach } from "vitest";
import { TerminalController } from "../TerminalController";
import { StatefulTerminal } from "@catty/terminal-emulation";

describe("TerminalController Window Management", () => {
  let terminal: StatefulTerminal;
  let controller: TerminalController;
  let displayElement: HTMLElement;
  let inputElement: HTMLInputElement;

  beforeEach(() => {
    // Create DOM elements
    displayElement = document.createElement("div");
    inputElement = document.createElement("input");

    // Create terminal and controller
    terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    controller = new TerminalController({
      terminal,
      displayElement,
      inputElement,
      websocketUrl: "ws://localhost:9999", // Non-existent to avoid actual connection
    });
  });

  it("should set window title", () => {
    controller.setTitle("Test Title");
    expect(controller.getTitle()).toBe("Test Title");
  });

  it("should set icon name", () => {
    controller.setIconName("Test Icon");
    expect(controller.getIconName()).toBe("Test Icon");
  });

  it("should update DOM title when terminal title changes", () => {
    const originalTitle = document.title;

    // Set title via terminal
    terminal.setWindowTitle("New Terminal Title");

    // Give time for update to propagate
    expect(document.title).toBe("New Terminal Title");

    // Restore original title
    document.title = originalTitle;
  });

  it("should generate title query response for window title", () => {
    controller.setTitle("Query Test");
    const response = controller.generateTitleQueryResponse("window");

    // OSC 21 response format: ESC ] L <title> ESC \
    expect(response).toBe("\x1b]LQuery Test\x1b\\");
  });

  it("should generate title query response for icon name", () => {
    controller.setIconName("Icon Query Test");
    const response = controller.generateTitleQueryResponse("icon");

    expect(response).toBe("\x1b]LIcon Query Test\x1b\\");
  });
});
