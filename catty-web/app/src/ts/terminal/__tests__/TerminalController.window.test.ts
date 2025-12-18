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

  it("should apply SGR styling to terminal cells", () => {
    // Send some text with SGR styling
    terminal.pushPtyText("\x1b[31mRed text\x1b[0m");

    // Get the snapshot to trigger repaint
    const snapshot = terminal.getSnapshot();

    // Manually trigger repaint to update DOM
    (controller as any).lastSnapshot = snapshot;
    (controller as any).repaint();

    // Check that spans were created with SGR classes
    const spans = displayElement.querySelectorAll("span.terminal-cell");
    expect(spans.length).toBeGreaterThan(0);

    // Check that at least one span has an SGR class (starts with "sgr-")
    const hasStyleClass = Array.from(spans).some(span => 
      Array.from(span.classList).some(cls => cls.startsWith("sgr-"))
    );
    expect(hasStyleClass).toBe(true);
  });

  it("should initialize theme system with default dark theme", () => {
    // Check that theme manager is initialized
    const currentTheme = controller.getCurrentTheme();
    expect(currentTheme).toBeDefined();
    expect(currentTheme.name).toBe("Default Dark");
    expect(currentTheme.type).toBe("dark");
    expect(currentTheme.colors).toBeDefined();
    expect(currentTheme.colors.foreground).toBe("#AAAAAA");
    expect(currentTheme.colors.background).toBe("#000000");
  });

  it("should allow theme switching", () => {
    // Create a custom theme
    const customTheme = {
      name: "Custom Light",
      type: "light" as const,
      colors: {
        black: "#000000",
        red: "#CC0000",
        green: "#00CC00",
        yellow: "#CC6600",
        blue: "#0000CC",
        magenta: "#CC00CC",
        cyan: "#00CCCC",
        white: "#CCCCCC",
        brightBlack: "#666666",
        brightRed: "#FF6666",
        brightGreen: "#66FF66",
        brightYellow: "#FFFF66",
        brightBlue: "#6666FF",
        brightMagenta: "#FF66FF",
        brightCyan: "#66FFFF",
        brightWhite: "#FFFFFF",
        foreground: "#000000",
        background: "#FFFFFF",
        cursor: "#000000",
        selection: "#CCCCCC",
      },
    };

    // Switch to custom theme
    controller.setTheme(customTheme);

    // Verify theme was applied
    const currentTheme = controller.getCurrentTheme();
    expect(currentTheme.name).toBe("Custom Light");
    expect(currentTheme.type).toBe("light");
    expect(currentTheme.colors.foreground).toBe("#000000");
    expect(currentTheme.colors.background).toBe("#FFFFFF");
  });

  it("should inject CSS variables for theme colors", () => {
    // Check that CSS variables are injected into DOM
    const styleTag = document.getElementById("terminal-theme-variables");
    expect(styleTag).toBeTruthy();
    expect(styleTag?.tagName).toBe("STYLE");
    
    // Check that the style tag contains CSS variables
    const cssContent = styleTag?.textContent || "";
    expect(cssContent).toContain("--terminal-color-black");
    expect(cssContent).toContain("--terminal-color-red");
    expect(cssContent).toContain("--terminal-foreground");
    expect(cssContent).toContain("--terminal-background");
  });
});
