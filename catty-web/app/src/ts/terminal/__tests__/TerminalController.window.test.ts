import { describe, it, expect, beforeEach } from "vitest";
import { TerminalController } from "../TerminalController";
import { StatefulTerminal } from "@catty/terminal-emulation";

describe("TerminalController Window Management", () => {
  let terminal: StatefulTerminal;
  let controller: TerminalController;
  let displayElement: HTMLElement;
  let inputElement: HTMLInputElement;
  let wsSends: unknown[];
  let rafQueue: Array<(t: number) => void>;

  beforeEach(() => {
    wsSends = [];
    rafQueue = [];

    // Deterministic rAF for tests (TerminalController throttles mousemove/wheel via rAF).
    (globalThis as any).requestAnimationFrame = (cb: (t: number) => void) => {
      rafQueue.push(cb);
      return rafQueue.length;
    };

    class MockWebSocket {
      static CONNECTING = 0;
      static OPEN = 1;
      static CLOSING = 2;
      static CLOSED = 3;

      public readyState = MockWebSocket.OPEN;
      public onopen: (() => void) | null = null;
      public onmessage: ((ev: MessageEvent) => void) | null = null;

      constructor(_url: string) {
        // Immediately "open" so key handling can send.
        queueMicrotask(() => this.onopen?.());
      }

      send(data: unknown): void {
        wsSends.push(data);
      }

      close(): void {
        this.readyState = MockWebSocket.CLOSED;
      }
    }

    // Ensure a deterministic WebSocket in jsdom.
    (globalThis as any).WebSocket = MockWebSocket;

    // Create DOM elements
    displayElement = document.createElement("div");
    inputElement = document.createElement("input");

    // Deterministic geometry for mouse coordinate mapping in jsdom.
    // (TerminalController maps clientX/Y -> cell coords using getBoundingClientRect + cached cell metrics.)
    (displayElement as any).getBoundingClientRect = () => ({
      left: 0,
      top: 0,
      width: 800,
      height: 480,
      right: 800,
      bottom: 480,
      x: 0,
      y: 0,
      toJSON: () => ({}),
    });

    // Create terminal and controller
    terminal = new StatefulTerminal({ cols: 80, rows: 24 });
    controller = new TerminalController({
      terminal,
      displayElement,
      inputElement,
      websocketUrl: "ws://localhost:9999", // Non-existent to avoid actual connection
    });

    // Force deterministic cell metrics in tests.
    (controller as any).cellMetrics = { cellWidthPx: 10, cellHeightPx: 20 };
  });

  async function flushRaf(): Promise<void> {
    // Run all queued frames. (Nested scheduling is possible, so loop.)
    while (rafQueue.length > 0) {
      const batch = rafQueue;
      rafQueue = [];
      for (const cb of batch) {
        cb(0);
      }
      // Allow microtasks to settle between frames.
      await Promise.resolve();
    }
  }

  function dispatchPaste(text: string): void {
    const ev = new Event("paste", { bubbles: true, cancelable: true }) as unknown as ClipboardEvent;
    const clipboardData = {
      getData: (type: string) => (type === "text/plain" ? text : ""),
    };
    Object.defineProperty(ev, "clipboardData", {
      value: clipboardData as unknown as DataTransfer,
    });
    inputElement.dispatchEvent(ev);
  }

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

  it("should send F10 as an xterm function-key escape sequence", () => {
    const ev = new KeyboardEvent("keydown", { key: "F10", bubbles: true, cancelable: true });
    inputElement.dispatchEvent(ev);

    // F10 should map to CSI 21 ~
    expect(wsSends).toContain("\x1b[21~");
  });

  it("should send raw paste without bracketed paste by default", () => {
    dispatchPaste("hello");
    expect(wsSends[wsSends.length - 1]).toBe("hello");
  });

  it("should wrap paste in bracketed paste markers when DECSET 2004 is enabled", () => {
    terminal.pushPtyText("\x1b[?2004h");
    dispatchPaste("hello");
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[200~hello\x1b[201~");
  });

  it("should stop wrapping paste when DECRST 2004 is received", () => {
    terminal.pushPtyText("\x1b[?2004h");
    dispatchPaste("hello");
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[200~hello\x1b[201~");

    terminal.pushPtyText("\x1b[?2004l");
    dispatchPaste("bye");
    expect(wsSends[wsSends.length - 1]).toBe("bye");
  });

  it("should send SGR mouse press/release when mouse mode is enabled (htop click)", () => {
    // Enable mouse click tracking + SGR encoding.
    terminal.pushPtyText("\x1b[?1000h\x1b[?1006h");

    const down = new MouseEvent("mousedown", { clientX: 15, clientY: 25, button: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(down);
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<0;2;2M");

    const up = new MouseEvent("mouseup", { clientX: 15, clientY: 25, button: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(up);
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<0;2;2m");
  });

  it("should send SGR mouse motion when 1002 is enabled and a button is held", async () => {
    terminal.pushPtyText("\x1b[?1002h\x1b[?1006h");

    const down = new MouseEvent("mousedown", { clientX: 15, clientY: 25, button: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(down);
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<0;2;2M");

    // Move to the next cell to produce a motion report.
    const move = new MouseEvent("mousemove", { clientX: 25, clientY: 25, buttons: 1, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(move);

    await flushRaf();

    // Motion adds 32 to the button code.
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<32;3;2M");
  });

  it("should not send motion when 1002 is enabled but no button is held", async () => {
    terminal.pushPtyText("\x1b[?1002h\x1b[?1006h");

    const start = wsSends.length;
    const move = new MouseEvent("mousemove", { clientX: 25, clientY: 25, buttons: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(move);
    await flushRaf();
    expect(wsSends.length).toBe(start);
  });

  it("should send SGR mouse motion when 1003 is enabled even with no buttons", async () => {
    terminal.pushPtyText("\x1b[?1003h\x1b[?1006h");

    const move = new MouseEvent("mousemove", { clientX: 25, clientY: 25, buttons: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(move);

    await flushRaf();

    // Motion with no buttons uses base button 3, plus 32 for motion.
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<35;3;2M");
  });

  it("should send SGR wheel reports when mouse mode is enabled", async () => {
    terminal.pushPtyText("\x1b[?1000h\x1b[?1006h");

    const wheelUp = new WheelEvent("wheel", { clientX: 15, clientY: 25, deltaY: -100, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(wheelUp);
    await flushRaf();
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<64;2;2M");

    const wheelDown = new WheelEvent("wheel", { clientX: 15, clientY: 25, deltaY: 100, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(wheelDown);
    await flushRaf();
    expect(wsSends[wsSends.length - 1]).toBe("\x1b[<65;2;2M");
  });

  it("should not send mouse reports when mouse mode is disabled", async () => {
    const start = wsSends.length;
    const down = new MouseEvent("mousedown", { clientX: 15, clientY: 25, button: 0, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(down);
    expect(wsSends.length).toBe(start);

    const wheelUp = new WheelEvent("wheel", { clientX: 15, clientY: 25, deltaY: -100, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(wheelUp);
    await flushRaf();
    // Real terminals scroll the viewport (scrollback) locally when mouse
    // reporting is disabled; they do not send arrow/page keys to the application.
    expect(wsSends.length).toBe(start);
  });

  it("should translate wheel to keys in alternate screen when mouse mode is disabled (bat/less)", async () => {
    // bat switches into alt screen and typically enables application cursor keys.
    terminal.pushPtyText("\x1b[?1049h\x1b[?1h");

    const start = wsSends.length;

    const wheelUp = new WheelEvent("wheel", { clientX: 15, clientY: 25, deltaY: -100, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(wheelUp);
    await flushRaf();

    // With deltaY=-100 and pixel mode, the controller converts to ~3 lines.
    expect(wsSends.length).toBe(start + 1);
    expect(wsSends[wsSends.length - 1]).toBe("\x1bOA\x1bOA\x1bOA");

    const wheelDown = new WheelEvent("wheel", { clientX: 15, clientY: 25, deltaY: 100, bubbles: true, cancelable: true });
    displayElement.dispatchEvent(wheelDown);
    await flushRaf();

    expect(wsSends.length).toBe(start + 2);
    expect(wsSends[wsSends.length - 1]).toBe("\x1bOB\x1bOB\x1bOB");
  });
});
