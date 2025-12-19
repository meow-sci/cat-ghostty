import {
  type StatefulTerminal,
  type ScreenSnapshot,
  type TerminalTraceChunk,
  formatTerminalTraceLine,
  SgrStyleManager,
  ThemeManager,
  DEFAULT_DARK_THEME,
  type SgrState
} from "@catty/terminal-emulation";

type InputMode = "cooked" | "raw";

type MouseTrackingMode = "off" | "click" | "button" | "any";

type MouseTrackingDecMode = 1000 | 1002 | 1003;

type CellMetrics = {
  cellWidthPx: number;
  cellHeightPx: number;
};

function isNode(value: unknown): value is Node {
  return typeof value === "object" && value !== null && value instanceof Node;
}

function isWheelOverElement(el: HTMLElement, ev: WheelEvent): boolean {
  // Prefer hit-testing by coordinates; some browsers can mis-target wheel events.
  if (typeof document.elementFromPoint === "function") {
    const hit = document.elementFromPoint(ev.clientX, ev.clientY);
    if (hit) {
      return el.contains(hit);
    }
  }

  // Fallback: use the event target.
  if (isNode(ev.target)) {
    return el.contains(ev.target);
  }

  return false;
}

type WheelDirection = "up" | "down";

type MouseMoveSample = {
  clientX: number;
  clientY: number;
  buttons: number;
  shiftKey: boolean;
  altKey: boolean;
  ctrlKey: boolean;
};

type WheelSample = {
  clientX: number;
  clientY: number;
  deltaY: number;
  deltaMode: number;
  shiftKey: boolean;
  altKey: boolean;
  ctrlKey: boolean;
};

function wheelDirectionFromDelta(deltaY: number): WheelDirection {
  return deltaY < 0 ? "up" : "down";
}

function wheelScrollLinesFromDelta(deltaY: number, deltaMode: number, rows: number): number {
  if (!Number.isFinite(deltaY) || deltaY === 0) {
    return 0;
  }
  const sign = deltaY < 0 ? -1 : 1;
  const abs = Math.abs(deltaY);
  const max = Math.max(1, rows * 3);

  // 0: pixels, 1: lines, 2: pages
  if (deltaMode === 2) {
    return sign * Math.max(1, rows);
  }
  if (deltaMode === 1) {
    return sign * clampInt(Math.round(abs), 1, max);
  }

  // Pixel-based (trackpads): a small fraction of a line per pixel.
  return sign * clampInt(Math.round(abs / 40), 1, max);
}

function wheelNotchesFromDelta(deltaY: number, deltaMode: number): number {
  const abs = Math.abs(deltaY);
  if (deltaMode === 2) {
    // Page-based wheels are already coarse.
    return clampInt(Math.round(abs), 1, 10);
  }
  if (deltaMode === 1) {
    // Line-based: treat each line as one notch (clamped).
    return clampInt(Math.round(abs), 1, 10);
  }

  // Pixel-based (trackpads): ~100px is roughly one "notch".
  return clampInt(Math.round(abs / 100), 1, 10);
}

function altScreenWheelSequenceFromDelta(
  deltaY: number,
  deltaMode: number,
  rows: number,
  applicationCursorKeys: boolean
): string {
  const direction = wheelDirectionFromDelta(deltaY);
  const lines = wheelScrollLinesFromDelta(deltaY, deltaMode, rows);
  if (lines === 0) {
    return "";
  }

  // Prefer line-wise scrolling via arrow keys, but if the delta is effectively
  // a full page, use PageUp/PageDown to avoid sending very long sequences.
  const absLines = Math.abs(lines);
  if (absLines >= rows) {
    const pages = clampInt(Math.round(absLines / Math.max(1, rows)), 1, 10);
    const seq = direction === "up" ? "\x1b[5~" : "\x1b[6~";
    return seq.repeat(pages);
  }

  const arrow = direction === "up"
    ? (applicationCursorKeys ? "\x1bOA" : "\x1b[A")
    : (applicationCursorKeys ? "\x1bOB" : "\x1b[B");
  return arrow.repeat(clampInt(absLines, 1, rows * 3));
}

function clampInt(v: number, min: number, max: number): number {
  if (!Number.isFinite(v)) {
    return min;
  }
  return Math.max(min, Math.min(max, Math.trunc(v)));
}

// Wheel throttling
//
// Trackpads can generate extremely high-frequency wheel events, which can make
// scrolling feel overly sensitive (too many rows/inputs per second).
//
// We already coalesce wheel events via rAF; additionally, we cap how often we
// *act* on the coalesced delta by requiring a minimum number of rAF frames
// between flushes. This keeps tests deterministic (they stub rAF) while still
// providing a configurable max "polling" rate.
const WHEEL_MAX_FLUSH_HZ = 10;
const WHEEL_ASSUMED_RAF_HZ = 60;
const WHEEL_MIN_RAF_FRAMES_BETWEEN_FLUSH = clampInt(
  Math.round(WHEEL_ASSUMED_RAF_HZ / Math.max(1, WHEEL_MAX_FLUSH_HZ)),
  1,
  10
);

export interface TerminalControllerOptions {
  terminal: StatefulTerminal;
  displayElement: HTMLElement;
  inputElement: HTMLInputElement;
  traceElement?: HTMLPreElement;
  cols?: number;
  rows?: number;
  websocketUrl?: string;
}

export class TerminalController {
  private readonly terminal: StatefulTerminal;
  private readonly displayElement: HTMLElement;
  private readonly inputElement: HTMLInputElement;
  private readonly traceElement: HTMLPreElement | null;
  private readonly sgrStyleManager: SgrStyleManager;
  private readonly themeManager: ThemeManager;

  // Render cache for fast in-place repaint.
  private cellCols = 0;
  private cellRows = 0;
  private cellSpans: Array<HTMLSpanElement> | null = null;
  private cellRenderedText: string[] = [];
  private cellRenderedClassName: string[] = [];
  private overlayElement: HTMLDivElement | null = null;
  private echoLayerElement: HTMLDivElement | null = null;
  private cursorElement: HTMLDivElement | null = null;

  private websocket: WebSocket | null = null;
  private repaintScheduled = false;
  private repaintReschedule = false;
  private lastSnapshot: ScreenSnapshot | null = null;

  private traceScheduled = false;
  private readonly traceChunks: TerminalTraceChunk[] = [];
  private traceRenderedCount = 0;

  // For PTY-backed shells (bash/zsh), we must forward keystrokes immediately.
  // Local "cooked" line editing breaks readline/zle features like Tab completion.
  private inputMode: InputMode = "raw";
  private applicationCursorKeys = false;
  private bracketedPasteEnabled = false;

  // Scrollback viewport (primary screen only). This is the 0-based row index
  // into the combined (scrollback + current screen) buffer of the top visible row.
  // At the bottom, this equals terminal.getScrollbackRowCount().
  private viewportTopRow = 0;
  private lastScrollbackRowCount = 0;
  private lastAlternateScreenActive = false;

  private mouseTrackingMode: MouseTrackingMode = "off";
  private readonly mouseTrackingDecModesEnabled = new Set<MouseTrackingDecMode>();
  private mouseSgrEncodingEnabled = false;
  private cellMetrics: CellMetrics | null = null;
  private lastMouseButton: 0 | 1 | 2 | null = null;
  private lastMouseCell: { x1: number; y1: number } | null = null;
  private pointerOverDisplay = false;

  // High-frequency input coalescing (mousemove / wheel).
  private pendingMouseMove: MouseMoveSample | null = null;
  private mouseMoveRaf: number | null = null;
  private pendingWheel: WheelSample | null = null;
  private wheelRaf: number | null = null;
  private wheelThrottleFramesRemaining = 0;

  private cookedLine = "";
  private cookedCursorIndex = 0;
  private suppressCookedEchoRemaining: string | null = null;
  private readonly debugEsc: boolean;

  private readonly removeListeners: Array<() => void> = [];
  private lastWindowTitle = "";

  constructor(options: TerminalControllerOptions) {
    this.terminal = options.terminal;
    this.displayElement = options.displayElement;
    this.inputElement = options.inputElement;
    this.traceElement = options.traceElement ?? null;
    this.sgrStyleManager = new SgrStyleManager();
    
    // Initialize theme system with default dark theme
    this.themeManager = new ThemeManager(DEFAULT_DARK_THEME);
    this.themeManager.applyTheme(DEFAULT_DARK_THEME);

    this.debugEsc = new URLSearchParams(window.location.search).has("debugEsc");

    // Ensure display container is positioned for absolute cell spans
    this.displayElement.style.position = this.displayElement.style.position || "relative";

    const unsubscribe = this.terminal.onUpdate((snapshot) => {
      this.lastSnapshot = snapshot;

      const alternateActive = this.terminal.isAlternateScreenActive();

      // Maintain scrollback viewport:
      // - If we're at the bottom, follow output.
      // - If the user has scrolled up, keep the viewport stable.
      if (alternateActive) {
        this.viewportTopRow = 0;
      } else {
        const scrollbackRows = this.terminal.getScrollbackRowCount();

        // Leaving a full-screen TUI (alternate screen) should restore the primary
        // screen with the prompt/cursor visible at the bottom.
        if (this.lastAlternateScreenActive) {
          this.viewportTopRow = scrollbackRows;
        } else {
        const wasFollowing = this.viewportTopRow === this.lastScrollbackRowCount;
        this.viewportTopRow = wasFollowing ? scrollbackRows : clampInt(this.viewportTopRow, 0, scrollbackRows);
        }

        this.lastScrollbackRowCount = scrollbackRows;
      }

      this.lastAlternateScreenActive = alternateActive;

      this.scheduleRepaint();

      // Update DOM title when window properties change
      this.updateDomTitle(snapshot.windowProperties.title);
    });
    this.removeListeners.push(unsubscribe);

    const unsubscribeChunks = this.terminal.onChunk((chunk) => {
      this.traceChunks.push(chunk);
      this.scheduleTraceRender();
    });
    this.removeListeners.push(unsubscribeChunks);

    const unsubscribeDecModes = this.terminal.onDecMode((ev) => {
      // These are "real world" sequences apps send to terminals.
      // - 1049/1047/47: alternate screen buffers
      // - 1: application cursor keys
      // - 2004: bracketed paste mode
      const modes = new Set(ev.modes);

      if (modes.has(1)) {
        this.applicationCursorKeys = ev.action === "set";
      }

      if (modes.has(2004)) {
        this.bracketedPasteEnabled = ev.action === "set";
      }

      // Mouse tracking modes
      // 1000: click tracking
      // 1002: button-event tracking (drag)
      // 1003: any-event tracking (motion)
      for (const m of [1000, 1002, 1003] as const) {
        if (!modes.has(m)) {
          continue;
        }

        if (ev.action === "set") {
          this.mouseTrackingDecModesEnabled.add(m);
        } else {
          this.mouseTrackingDecModesEnabled.delete(m);
        }
      }

      if (this.mouseTrackingDecModesEnabled.has(1003)) {
        this.mouseTrackingMode = "any";
      } else if (this.mouseTrackingDecModesEnabled.has(1002)) {
        this.mouseTrackingMode = "button";
      } else if (this.mouseTrackingDecModesEnabled.has(1000)) {
        this.mouseTrackingMode = "click";
      } else {
        this.mouseTrackingMode = "off";
      }

      // Mouse encoding
      // 1006: SGR (recommended)
      if (modes.has(1006)) {
        this.mouseSgrEncodingEnabled = ev.action === "set";
      }

      // Always remain in raw mode for PTY-backed sessions.
      // (Full-screen apps toggling the alt screen should not affect input semantics.)
    });
    this.removeListeners.push(unsubscribeDecModes);

    // Subscribe to terminal responses (device queries, etc.)
    const unsubscribeResponses = this.terminal.onResponse((response) => {
      this.sendResponseToApplication(response);
    });
    this.removeListeners.push(unsubscribeResponses);

    this.setupInputHandlers();

    this.lastScrollbackRowCount = this.terminal.getScrollbackRowCount();
    this.viewportTopRow = this.terminal.isAlternateScreenActive() ? 0 : this.lastScrollbackRowCount;
    this.lastAlternateScreenActive = this.terminal.isAlternateScreenActive();

    const cols = options.cols ?? this.terminal.cols;
    const rows = options.rows ?? this.terminal.rows;
    const websocketUrl = options.websocketUrl ?? this.defaultWebsocketUrl(cols, rows);

    this.connect(websocketUrl, cols, rows);

    // Initial paint
    this.lastSnapshot = this.terminal.getSnapshot();
    this.repaint();
    this.renderTrace();
  }

  public clearTrace(): void {
    this.traceChunks.length = 0;
    this.traceRenderedCount = 0;
    this.traceScheduled = false;
    if (this.traceElement) {
      this.traceElement.textContent = "";
    }
  }

  public getTraceChunks(): ReadonlyArray<TerminalTraceChunk> {
    return this.traceChunks;
  }

  public dispose(): void {
    for (const fn of this.removeListeners) fn();
    this.removeListeners.length = 0;

    if (this.websocket) {
      try {
        this.websocket.close();
      } catch {
        // ignore
      }
      this.websocket = null;
    }
  }

  // Window management methods

  /**
   * Set the window title (OSC 2)
   */
  public setTitle(title: string): void {
    this.terminal.setWindowTitle(title);
  }

  /**
   * Set the icon name (OSC 1)
   */
  public setIconName(name: string): void {
    this.terminal.setIconName(name);
  }

  /**
   * Get the current window title
   */
  public getTitle(): string {
    return this.terminal.getWindowTitle();
  }

  /**
   * Get the current icon name
   */
  public getIconName(): string {
    return this.terminal.getIconName();
  }

  /**
   * Switch to a new terminal theme
   * @param theme The theme to apply
   */
  public setTheme(theme: import("@catty/terminal-emulation").TerminalTheme): void {
    this.themeManager.applyTheme(theme);
    // Trigger a repaint to apply theme changes to existing styled cells
    this.scheduleRepaint();
  }

  /**
   * Get the current active theme
   */
  public getCurrentTheme(): import("@catty/terminal-emulation").TerminalTheme {
    return this.themeManager.getCurrentTheme();
  }

  /**
   * Generate title query response (OSC 21)
   * Returns the response string that should be sent back to the application
   */
  public generateTitleQueryResponse(titleType: "window" | "icon"): string {
    const title = titleType === "window"
      ? this.terminal.getWindowTitle()
      : this.terminal.getIconName();

    // OSC 21 response format: OSC L <title> ST
    // where L is the title type identifier
    // ST is String Terminator (ESC \)
    return `\x1b]L${title}\x1b\\`;
  }

  /**
   * Check if an SGR state is the default (unstyled) state
   */
  private isDefaultSgrState(sgrState: SgrState): boolean {
    return !sgrState.bold &&
           !sgrState.faint &&
           !sgrState.italic &&
           !sgrState.underline &&
           !sgrState.blink &&
           !sgrState.inverse &&
           !sgrState.hidden &&
           !sgrState.strikethrough &&
           sgrState.foregroundColor === null &&
           sgrState.backgroundColor === null &&
           sgrState.underlineColor === null &&
           sgrState.font === 0 &&
           sgrState.underlineStyle === null;
  }

  /**
   * Update the DOM document title when terminal window title changes
   */
  private updateDomTitle(title: string): void {
    // Only update if the title has actually changed to avoid unnecessary DOM updates
    if (title !== this.lastWindowTitle) {
      this.lastWindowTitle = title;

      // Update the browser document title
      if (title && title.length > 0) {
        document.title = title;
      }
    }
  }

  /**
   * Send a response back to the application through the websocket.
   * Used for device queries (DA, CPR, terminal size, etc.)
   */
  private sendResponseToApplication(response: string): void {
    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      // Queue responses if websocket is not ready
      // For now, we just drop them - in a production system you might want to queue
      console.warn("Cannot send response: websocket not connected", response);
      return;
    }

    try {
      this.websocket.send(response);
    } catch (error) {
      console.error("Failed to send response to application:", error);
    }
  }

  private defaultWebsocketUrl(cols: number, rows: number): string {
    const host = window.location.hostname || "localhost";
    return `ws://${host}:4444?cols=${cols}&rows=${rows}`;
  }

  private setInputMode(mode: InputMode): void {
    if (this.inputMode === mode) {
      return;
    }

    this.inputMode = mode;
    // Prevent stale buffered input when switching modes.
    this.inputElement.value = "";

    // Reset local cooked editor state on transitions.
    this.cookedLine = "";
    this.cookedCursorIndex = 0;
    this.suppressCookedEchoRemaining = null;
    this.scheduleRepaint();
  }

  private connect(url: string, cols: number, rows: number): void {
    this.websocket = new WebSocket(url);

    this.websocket.onopen = () => {
      try {
        this.websocket?.send(JSON.stringify({ type: "resize", cols, rows }));
      } catch {
        // ignore
      }
    };

    this.websocket.onmessage = (ev) => {
      if (typeof ev.data === "string") {
        if (this.debugEsc) {
          debugLogIncoming(ev.data);
        }
        const text = this.applyCookedEchoSuppression(ev.data);
        if (text.length > 0) {
          this.terminal.pushPtyText(text);
        }
        return;
      }

      if (ev.data instanceof ArrayBuffer) {
        const text = new TextDecoder().decode(new Uint8Array(ev.data));
        if (this.debugEsc) {
          debugLogIncoming(text);
        }
        const filtered = this.applyCookedEchoSuppression(text);
        if (filtered.length > 0) {
          this.terminal.pushPtyText(filtered);
        }
        return;
      }

      // Blob fallback
      if (ev.data instanceof Blob) {
        ev.data.arrayBuffer().then((buf) => {
          const text = new TextDecoder().decode(new Uint8Array(buf));
          if (this.debugEsc) {
            debugLogIncoming(text);
          }
          const filtered = this.applyCookedEchoSuppression(text);
          if (filtered.length > 0) {
            this.terminal.pushPtyText(filtered);
          }
        }).catch(() => {
          // ignore
        });
      }
    };
  }


  private applyCookedEchoSuppression(text: string): string {
    const remaining = this.suppressCookedEchoRemaining;
    if (!remaining || remaining.length === 0) {
      return text;
    }

    // Disable echo suppression entirely for zsh and other modern shells
    // Modern shells often rewrite the command line with syntax highlighting
    // which should not be suppressed as it's not traditional echo
    // 
    // The original echo suppression was designed for simple shells that
    // just echo back the exact command, but zsh does sophisticated rewriting
    // with cursor movements and styling that we want to preserve
    this.suppressCookedEchoRemaining = null;
    return text;
  }

  private setupInputHandlers(): void {

    // Keep focus on the input so keyboard works (click anywhere in display).
    const focusInput = (e?: Event) => {
      // Avoid text selection / focus oddities when clicking rendered spans.
      e?.preventDefault?.();
      try {
        // TS lib typing for preventScroll can vary; keep it best-effort.
        (this.inputElement as any).focus?.({ preventScroll: true });
      } catch {
        this.inputElement.focus();
      }
    };

    // Use click (explicit requirement) + pointerdown (more reliable on some platforms).
    this.displayElement.addEventListener("click", focusInput);
    this.removeListeners.push(() => this.displayElement.removeEventListener("click", focusInput));

    this.displayElement.addEventListener("pointerdown", focusInput);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointerdown", focusInput));

    // Track whether the pointer is currently over the terminal. Some wheel events
    // (notably on trackpads) can be dispatched with surprising targets; this lets
    // us reliably suppress page scrolling when the user scrolls over the terminal.
    const onPointerEnter = () => {
      this.pointerOverDisplay = true;
    };
    const onPointerLeave = () => {
      this.pointerOverDisplay = false;
    };
    this.displayElement.addEventListener("pointerenter", onPointerEnter);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointerenter", onPointerEnter));
    this.displayElement.addEventListener("pointerleave", onPointerLeave);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointerleave", onPointerLeave));
    this.displayElement.addEventListener("mouseenter", onPointerEnter);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mouseenter", onPointerEnter));
    this.displayElement.addEventListener("mouseleave", onPointerLeave);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mouseleave", onPointerLeave));

    // Mouse reporting (xterm). Minimum for htop: left-click press/release.
    const onPointerDown = (e: PointerEvent) => {
      focusInput(e);
      this.handleMouseDown(e);
    };
    const onPointerUp = (e: PointerEvent) => {
      focusInput(e);
      this.handleMouseUp(e);
    };
    const onPointerMove = (e: PointerEvent) => {
      this.handleMouseMove(e);
    };
    const onWheel = (e: WheelEvent) => {
      // If we already handled this via a capture listener, don't double-send.
      if (e.defaultPrevented) {
        return;
      }
      this.handleWheel(e);
    };

    this.displayElement.addEventListener("pointerdown", onPointerDown);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointerdown", onPointerDown));

    this.displayElement.addEventListener("pointerup", onPointerUp);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointerup", onPointerUp));

    this.displayElement.addEventListener("pointermove", onPointerMove);
    this.removeListeners.push(() => this.displayElement.removeEventListener("pointermove", onPointerMove));

    this.displayElement.addEventListener("wheel", onWheel, { passive: false });
    this.removeListeners.push(() => this.displayElement.removeEventListener("wheel", onWheel));

    // On some browsers / trackpads, wheel events may not reliably target the
    // terminal element even when the pointer is over it (leading to page scroll).
    // Capture at the window level and route to the terminal when appropriate.
    const onWheelCapture = (e: WheelEvent) => {
      if (e.defaultPrevented) {
        return;
      }
      const overTerminal = this.pointerOverDisplay || isWheelOverElement(this.displayElement, e);
      if (!overTerminal) {
        return;
      }

      // Always suppress page scrolling when scrolling over the terminal.
      // (If mouse reporting is enabled, we also forward wheel events to the app.)
      e.preventDefault();
      e.stopPropagation();

      this.handleWheel(e);
    };
    window.addEventListener("wheel", onWheelCapture, { passive: false, capture: true });
    this.removeListeners.push(() => window.removeEventListener("wheel", onWheelCapture, { capture: true }));

    // Fallback for environments without PointerEvent (or where it is flaky).
    const onMouseDown = (e: MouseEvent) => {
      focusInput(e);
      this.handleMouseDown(e);
    };
    const onMouseUp = (e: MouseEvent) => {
      focusInput(e);
      this.handleMouseUp(e);
    };
    const onMouseMove = (e: MouseEvent) => {
      this.handleMouseMove(e);
    };
    this.displayElement.addEventListener("mousedown", onMouseDown);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mousedown", onMouseDown));
    this.displayElement.addEventListener("mouseup", onMouseUp);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mouseup", onMouseUp));

    this.displayElement.addEventListener("mousemove", onMouseMove);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mousemove", onMouseMove));

    const onKeyDown = (e: KeyboardEvent) => {

      if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
        return;
      }

      // Cooked (default): local line buffer in the <input>, send only on Enter.
      if (this.inputMode === "cooked") {

        // Allow Ctrl+C / Ctrl+D to be sent immediately.
        const ctrl = encodeCtrlKey(e);

        if (ctrl) {
          e.preventDefault();
          this.inputElement.value = "";
          this.websocket.send(ctrl);
          return;
        }

        // Local editor keys
        if (e.key === "Enter") {
          e.preventDefault();

          const line = this.cookedLine;
          this.cookedLine = "";
          this.cookedCursorIndex = 0;
          this.inputElement.value = "";

          // Set up smart echo suppression that won't interfere with syntax highlighting
          this.suppressCookedEchoRemaining = line;
          this.websocket.send(line + "\r");
          this.scheduleRepaint();
          return;
        }

        if (e.key === "Backspace") {
          if (this.cookedCursorIndex > 0) {
            const before = this.cookedLine.slice(0, this.cookedCursorIndex - 1);
            const after = this.cookedLine.slice(this.cookedCursorIndex);
            this.cookedLine = before + after;
            this.cookedCursorIndex -= 1;
            this.scheduleRepaint();
          }
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "Delete") {
          if (this.cookedCursorIndex < this.cookedLine.length) {
            const before = this.cookedLine.slice(0, this.cookedCursorIndex);
            const after = this.cookedLine.slice(this.cookedCursorIndex + 1);
            this.cookedLine = before + after;
            this.scheduleRepaint();
          }
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "ArrowLeft") {
          this.cookedCursorIndex = Math.max(0, this.cookedCursorIndex - 1);
          this.scheduleRepaint();
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "ArrowRight") {
          this.cookedCursorIndex = Math.min(this.cookedLine.length, this.cookedCursorIndex + 1);
          this.scheduleRepaint();
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "Home") {
          this.cookedCursorIndex = 0;
          this.scheduleRepaint();
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "End") {
          this.cookedCursorIndex = this.cookedLine.length;
          this.scheduleRepaint();
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (e.key === "Tab") {
          // Keep it simple: insert a literal tab.
          this.insertCookedText("\t");
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        if (!e.metaKey && !e.ctrlKey && e.key.length === 1) {
          this.insertCookedText(e.key);
          e.preventDefault();
          this.inputElement.value = "";
          return;
        }

        // Ignore other keys in cooked mode.
        return;
      }

      // Raw: send keystrokes immediately.
      const encoded = encodeKeyDownToTerminalBytes(e, { applicationCursorKeys: this.applicationCursorKeys });
      if (!encoded) {
        return;
      }

      e.preventDefault();
      // Keep the input empty in raw mode (it is just a focus/IME surface).
      this.inputElement.value = "";
      this.websocket.send(encoded);
    };

    this.inputElement.addEventListener("keydown", onKeyDown);
    this.removeListeners.push(() => this.inputElement.removeEventListener("keydown", onKeyDown));

    const onPaste = (e: ClipboardEvent) => {
      if (this.inputMode === "cooked") {
        if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
          return;
        }
        const text = e.clipboardData?.getData("text/plain");
        if (!text) {
          return;
        }
        e.preventDefault();
        this.insertCookedText(text);
        this.inputElement.value = "";
        return;
      }

      if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
        return;
      }
      const text = e.clipboardData?.getData("text/plain");
      if (!text) {
        return;
      }
      e.preventDefault();
      this.inputElement.value = "";
      const payload = this.bracketedPasteEnabled
        ? `\x1b[200~${text}\x1b[201~`
        : text;
      this.websocket.send(payload);
    };

    this.inputElement.addEventListener("paste", onPaste);
    this.removeListeners.push(() => this.inputElement.removeEventListener("paste", onPaste));

    // Autofocus on mount
    queueMicrotask(() => this.inputElement.focus());
  }

  private getCellMetrics(): CellMetrics {
    if (this.cellMetrics) {
      return this.cellMetrics;
    }

    // Best-effort measurement using CSS-relative units used for positioning.
    const probe = document.createElement("div");
    probe.style.position = "absolute";
    probe.style.visibility = "hidden";
    probe.style.pointerEvents = "none";
    probe.style.width = "1ch";
    probe.style.height = "1lh";
    this.displayElement.appendChild(probe);

    const r = probe.getBoundingClientRect();
    probe.remove();

    const cellWidthPx = Number.isFinite(r.width) && r.width > 0 ? r.width : 8;
    const cellHeightPx = Number.isFinite(r.height) && r.height > 0 ? r.height : 16;

    this.cellMetrics = { cellWidthPx, cellHeightPx };
    return this.cellMetrics;
  }

  private eventToCell(ev: { clientX: number; clientY: number }): { x1: number; y1: number } | null {
    const rect = this.displayElement.getBoundingClientRect();
    const { cellWidthPx, cellHeightPx } = this.getCellMetrics();
    if (cellWidthPx <= 0 || cellHeightPx <= 0) {
      return null;
    }

    const dx = ev.clientX - rect.left;
    const dy = ev.clientY - rect.top;
    if (!Number.isFinite(dx) || !Number.isFinite(dy)) {
      return null;
    }

    const x0 = Math.floor(dx / cellWidthPx);
    const y0 = Math.floor(dy / cellHeightPx);

    // Clamp to terminal size.
    const x1 = Math.max(1, Math.min(this.terminal.cols, x0 + 1));
    const y1 = Math.max(1, Math.min(this.terminal.rows, y0 + 1));
    return { x1, y1 };
  }

  private mouseEnabled(): boolean {
    return this.mouseTrackingMode !== "off";
  }

  private mouseButtonFromEvent(ev: { button: number }): 0 | 1 | 2 | null {
    if (ev.button === 0) return 0;
    if (ev.button === 1) return 1;
    if (ev.button === 2) return 2;
    return null;
  }

  private mouseButtonFromButtons(buttons: number): 0 | 1 | 2 | null {
    // https://developer.mozilla.org/en-US/docs/Web/API/MouseEvent/buttons
    // 1: primary (left), 2: secondary (right), 4: auxiliary (middle)
    if ((buttons & 1) !== 0) return 0;
    if ((buttons & 4) !== 0) return 1;
    if ((buttons & 2) !== 0) return 2;
    return null;
  }

  private encodeMouseMotion(button: 0 | 1 | 2 | 3, x1: number, y1: number, mods: { shift: boolean; alt: boolean; ctrl: boolean }): string {
    const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);
    // Motion reports add 32.
    const b = button + modBits + 32;

    if (this.mouseSgrEncodingEnabled) {
      return `\x1b[<${b};${x1};${y1}M`;
    }

    const cx = 32 + Math.max(1, Math.min(223, x1));
    const cy = 32 + Math.max(1, Math.min(223, y1));
    const cb = 32 + Math.max(0, Math.min(255, b));
    return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
  }

  private encodeMouseWheel(direction: "up" | "down", x1: number, y1: number, mods: { shift: boolean; alt: boolean; ctrl: boolean }): string {
    const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);

    // xterm wheel: buttons 64/65 (press only)
    const wheelButton = direction === "up" ? 64 : 65;
    const b = wheelButton + modBits;

    if (this.mouseSgrEncodingEnabled) {
      return `\x1b[<${b};${x1};${y1}M`;
    }

    const cx = 32 + Math.max(1, Math.min(223, x1));
    const cy = 32 + Math.max(1, Math.min(223, y1));
    const cb = 32 + Math.max(0, Math.min(255, b));
    return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
  }

  private encodeMousePress(button: 0 | 1 | 2, x1: number, y1: number, mods: { shift: boolean; alt: boolean; ctrl: boolean }): string {
    const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);
    const b = button + modBits;

    if (this.mouseSgrEncodingEnabled) {
      return `\x1b[<${b};${x1};${y1}M`;
    }

    // X10 fallback encoding (limited to 223 for coordinates).
    const cx = 32 + Math.max(1, Math.min(223, x1));
    const cy = 32 + Math.max(1, Math.min(223, y1));
    const cb = 32 + Math.max(0, Math.min(255, b));
    return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
  }

  private encodeMouseRelease(button: 0 | 1 | 2, x1: number, y1: number, mods: { shift: boolean; alt: boolean; ctrl: boolean }): string {
    const modBits = (mods.shift ? 4 : 0) + (mods.alt ? 8 : 0) + (mods.ctrl ? 16 : 0);

    if (this.mouseSgrEncodingEnabled) {
      // In SGR mode, xterm uses final 'm' to indicate release.
      const b = button + modBits;
      return `\x1b[<${b};${x1};${y1}m`;
    }

    // In classic mode, use button 3 (release) + modifiers.
    const cb = 32 + (3 + modBits);
    const cx = 32 + Math.max(1, Math.min(223, x1));
    const cy = 32 + Math.max(1, Math.min(223, y1));
    return `\x1b[M${String.fromCharCode(cb)}${String.fromCharCode(cx)}${String.fromCharCode(cy)}`;
  }

  private handleMouseDown(ev: MouseEvent | PointerEvent): void {
    if (!this.mouseEnabled()) {
      return;
    }
    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      return;
    }

    const button = this.mouseButtonFromEvent(ev);
    if (button === null) {
      return;
    }

    const pos = this.eventToCell(ev);
    if (!pos) {
      return;
    }

    ev.preventDefault();

    try {
      if ("pointerId" in ev) {
        this.displayElement.setPointerCapture(ev.pointerId);
      }
    } catch {
      // ignore
    }

    this.lastMouseButton = button;
    this.lastMouseCell = pos;

    const seq = this.encodeMousePress(button, pos.x1, pos.y1, {
      shift: ev.shiftKey,
      alt: ev.altKey,
      ctrl: ev.ctrlKey,
    });
    this.websocket.send(seq);
  }

  private handleMouseUp(ev: MouseEvent | PointerEvent): void {
    if (!this.mouseEnabled()) {
      return;
    }
    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      return;
    }

    const button = this.lastMouseButton ?? this.mouseButtonFromEvent(ev);
    if (button === null) {
      return;
    }

    const pos = this.eventToCell(ev);
    if (!pos) {
      return;
    }

    ev.preventDefault();

    try {
      if ("pointerId" in ev) {
        this.displayElement.releasePointerCapture(ev.pointerId);
      }
    } catch {
      // ignore
    }

    this.lastMouseButton = null;
    this.lastMouseCell = pos;

    const seq = this.encodeMouseRelease(button, pos.x1, pos.y1, {
      shift: ev.shiftKey,
      alt: ev.altKey,
      ctrl: ev.ctrlKey,
    });
    this.websocket.send(seq);
  }

  private handleMouseMove(ev: MouseEvent | PointerEvent): void {
    if (!this.mouseEnabled()) {
      return;
    }
    if (this.mouseTrackingMode === "click") {
      return;
    }
    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      return;
    }

    const buttons = typeof (ev as { buttons?: number }).buttons === "number" ? (ev as { buttons: number }).buttons : 0;

    this.pendingMouseMove = {
      clientX: ev.clientX,
      clientY: ev.clientY,
      buttons,
      shiftKey: ev.shiftKey,
      altKey: ev.altKey,
      ctrlKey: ev.ctrlKey,
    };

    if (this.mouseMoveRaf !== null) {
      return;
    }

    this.mouseMoveRaf = requestAnimationFrame(() => {
      this.mouseMoveRaf = null;
      this.flushMouseMove();
    });
  }

  private flushMouseMove(): void {
    const sample = this.pendingMouseMove;
    this.pendingMouseMove = null;
    if (!sample) {
      return;
    }
    if (!this.mouseEnabled()) {
      return;
    }
    if (this.mouseTrackingMode === "click") {
      return;
    }
    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      return;
    }

    const pos = this.eventToCell(sample);
    if (!pos) {
      return;
    }

    if (this.lastMouseCell && this.lastMouseCell.x1 === pos.x1 && this.lastMouseCell.y1 === pos.y1) {
      return;
    }

    const pressed = this.lastMouseButton ?? this.mouseButtonFromButtons(sample.buttons);
    if (this.mouseTrackingMode === "button" && pressed === null) {
      return;
    }

    // For motion with no pressed buttons (1003), xterm reports button=3.
    const button: 0 | 1 | 2 | 3 = pressed ?? 3;

    this.lastMouseCell = pos;

    const seq = this.encodeMouseMotion(button, pos.x1, pos.y1, {
      shift: sample.shiftKey,
      alt: sample.altKey,
      ctrl: sample.ctrlKey,
    });
    this.websocket.send(seq);
  }

  private handleWheel(ev: WheelEvent): void {
    // Coalesce high-frequency trackpad wheel events.
    if (this.pendingWheel) {
      this.pendingWheel.clientX = ev.clientX;
      this.pendingWheel.clientY = ev.clientY;
      this.pendingWheel.deltaY += ev.deltaY;
      this.pendingWheel.deltaMode = ev.deltaMode;
      this.pendingWheel.shiftKey = ev.shiftKey;
      this.pendingWheel.altKey = ev.altKey;
      this.pendingWheel.ctrlKey = ev.ctrlKey;
    } else {
      this.pendingWheel = {
        clientX: ev.clientX,
        clientY: ev.clientY,
        deltaY: ev.deltaY,
        deltaMode: ev.deltaMode,
        shiftKey: ev.shiftKey,
        altKey: ev.altKey,
        ctrlKey: ev.ctrlKey,
      };
    }

    if (this.wheelRaf !== null) {
      return;
    }

    this.wheelRaf = requestAnimationFrame(() => {
      this.wheelRaf = null;
      this.tickWheelFlush();
    });
  }

  private tickWheelFlush(): void {
    if (WHEEL_MIN_RAF_FRAMES_BETWEEN_FLUSH > 1 && this.wheelThrottleFramesRemaining > 0) {
      this.wheelThrottleFramesRemaining -= 1;
      if (this.pendingWheel) {
        this.wheelRaf = requestAnimationFrame(() => {
          this.wheelRaf = null;
          this.tickWheelFlush();
        });
      }
      return;
    }

    this.flushWheelNow();

    if (WHEEL_MIN_RAF_FRAMES_BETWEEN_FLUSH > 1) {
      this.wheelThrottleFramesRemaining = WHEEL_MIN_RAF_FRAMES_BETWEEN_FLUSH - 1;
    }

    if (this.pendingWheel && this.wheelRaf === null) {
      this.wheelRaf = requestAnimationFrame(() => {
        this.wheelRaf = null;
        this.tickWheelFlush();
      });
    }
  }

  private flushWheelNow(): void {
    const sample = this.pendingWheel;
    this.pendingWheel = null;
    if (!sample) {
      return;
    }
    if (!Number.isFinite(sample.deltaY) || sample.deltaY === 0) {
      return;
    }

    // If the app hasn't enabled mouse reporting, behave like a real terminal:
    // - In the primary screen: scroll the terminal viewport (scrollback) locally.
    // - In the alternate screen: many full-screen TUIs rely on wheel->key
    //   translation (xterm's alternateScroll behavior).
    if (!this.mouseEnabled()) {
      if (this.terminal.isAlternateScreenActive()) {
        if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
          return;
        }
        const seq = altScreenWheelSequenceFromDelta(
          sample.deltaY,
          sample.deltaMode,
          this.terminal.rows,
          this.applicationCursorKeys
        );
        if (seq.length > 0) {
          this.websocket.send(seq);
        }
      } else {
        const lines = wheelScrollLinesFromDelta(sample.deltaY, sample.deltaMode, this.terminal.rows);
        this.scrollViewportBy(lines);
      }
      return;
    }

    if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
      return;
    }

    const direction = wheelDirectionFromDelta(sample.deltaY);

    const pos = this.eventToCell(sample);
    if (!pos) {
      return;
    }

    const notches = wheelNotchesFromDelta(sample.deltaY, sample.deltaMode);
    this.lastMouseCell = pos;

    const one = this.encodeMouseWheel(direction, pos.x1, pos.y1, {
      shift: sample.shiftKey,
      alt: sample.altKey,
      ctrl: sample.ctrlKey,
    });

    // Send N "wheel clicks" for large deltas.
    this.websocket.send(one.repeat(notches));
  }

  private scrollViewportBy(lines: number): void {
    if (!Number.isFinite(lines) || lines === 0) {
      return;
    }
    if (this.terminal.isAlternateScreenActive()) {
      return;
    }

    const scrollbackRows = this.terminal.getScrollbackRowCount();
    const next = clampInt(this.viewportTopRow + Math.trunc(lines), 0, scrollbackRows);
    if (next === this.viewportTopRow) {
      return;
    }
    this.viewportTopRow = next;
    this.lastScrollbackRowCount = scrollbackRows;
    this.scheduleRepaint();
  }

  private insertCookedText(text: string): void {
    if (text.length === 0) {
      return;
    }
    const before = this.cookedLine.slice(0, this.cookedCursorIndex);
    const after = this.cookedLine.slice(this.cookedCursorIndex);
    this.cookedLine = before + text + after;
    this.cookedCursorIndex += text.length;
    this.scheduleRepaint();
  }

  private scheduleRepaint(): void {
    if (this.repaintScheduled) {
      // Output is still streaming; postpone paint to avoid showing transient
      // intermediate states (e.g. Vim showcmd briefly writing "^[").
      this.repaintReschedule = true;
      return;
    }

    this.repaintScheduled = true;
    requestAnimationFrame(() => {
      this.repaintScheduled = false;

      if (this.repaintReschedule) {
        this.repaintReschedule = false;
        this.scheduleRepaint();
        return;
      }

      this.repaint();
    });
  }

  private scheduleTraceRender(): void {
    if (!this.traceElement) {
      return;
    }
    if (this.traceScheduled) {
      return;
    }
    this.traceScheduled = true;

    requestAnimationFrame(() => {
      this.traceScheduled = false;
      this.renderTrace();
    });
  }

  private renderTrace(): void {
    const el = this.traceElement;
    if (!el) {
      return;
    }

    const start = this.traceRenderedCount;
    if (start >= this.traceChunks.length) {
      return;
    }

    const frag = document.createDocumentFragment();
    for (let i = start; i < this.traceChunks.length; i++) {
      const line = formatTerminalTraceLine(this.traceChunks[i], i);
      frag.appendChild(document.createTextNode(line + "\n"));
    }
    el.appendChild(frag);
    this.traceRenderedCount = this.traceChunks.length;
  }

  private repaint(): void {
    const snapshot = this.lastSnapshot;
    if (!snapshot) {
      return;
    }

    const alternateActive = this.terminal.isAlternateScreenActive();
    const scrollbackRows = alternateActive ? 0 : this.terminal.getScrollbackRowCount();
    const atBottom = alternateActive ? true : this.viewportTopRow === scrollbackRows;

    this.ensureDisplayDom(snapshot.cols, snapshot.rows);

    const spans = this.cellSpans;
    if (!spans) {
      return;
    }

    // Update cell spans in-place.
    const viewportRows = alternateActive
      ? snapshot.cells
      : this.terminal.getViewportRows(this.viewportTopRow, snapshot.rows);

    for (let y = 0; y < snapshot.rows; y++) {
      const row = viewportRows[y];
      for (let x = 0; x < snapshot.cols; x++) {
        const idx = y * snapshot.cols + x;
        const cell = row?.[x] ?? null;

        const next = this.renderStateForCell(cell);

        if (this.cellRenderedText[idx] !== next.text) {
          spans[idx].textContent = next.text;
          this.cellRenderedText[idx] = next.text;
        }

        if (this.cellRenderedClassName[idx] !== next.className) {
          spans[idx].className = next.className;
          this.cellRenderedClassName[idx] = next.className;
        }
      }
    }

    // Cooked-mode local echo overlay.
    const echoLayer = this.echoLayerElement;
    if (echoLayer) {
      if (atBottom && this.inputMode === "cooked" && this.cookedLine.length > 0) {
        const startX = snapshot.cursorX;
        const startY = snapshot.cursorY;
        const echoFrag = document.createDocumentFragment();

        for (let i = 0; i < this.cookedLine.length; i++) {
          const ch = this.cookedLine[i];
          const pos = addOffset(startX, startY, i, snapshot.cols);
          if (!pos) {
            break;
          }
          const [x, y] = pos;
          if (y >= snapshot.rows) {
            break;
          }

          const span = document.createElement("span");
          span.className = "terminal-echo";
          span.textContent = ch;
          span.style.left = `${x}ch`;
          span.style.top = `${y}lh`;
          echoFrag.appendChild(span);
        }

        echoLayer.replaceChildren(echoFrag);
      } else {
        echoLayer.replaceChildren();
      }
    }

    // Cursor overlay (respects DECTCEM cursor visibility)
    const cursor = this.cursorElement;
    if (cursor) {
      if (!atBottom || !snapshot.cursorVisible) {
        cursor.style.display = "none";
      } else {
        const cursorOffset = this.inputMode === "cooked" ? this.cookedCursorIndex : 0;
        const cursorPos = addOffset(snapshot.cursorX, snapshot.cursorY, cursorOffset, snapshot.cols);
        if (!cursorPos) {
          cursor.style.display = "none";
        } else {
          const [cx, cy] = cursorPos;
          if (cy < 0 || cy >= snapshot.rows) {
            cursor.style.display = "none";
          } else {
            const className = cursorClassNameForStyle(snapshot.cursorStyle);
            if (cursor.className !== className) {
              cursor.className = className;
            }
            cursor.style.left = `${cx}ch`;
            cursor.style.top = `${cy}lh`;
            cursor.style.display = "block";
          }
        }
      }
    }
  }

  private ensureDisplayDom(cols: number, rows: number): void {
    if (
      this.cellSpans &&
      this.overlayElement &&
      this.echoLayerElement &&
      this.cursorElement &&
      this.cellCols === cols &&
      this.cellRows === rows
    ) {
      return;
    }

    this.cellCols = cols;
    this.cellRows = rows;

    const spanCount = cols * rows;
    this.cellSpans = [];
    this.cellSpans.length = spanCount;
    this.cellRenderedText = Array.from({ length: spanCount }, () => "");
    this.cellRenderedClassName = Array.from({ length: spanCount }, () => "terminal-cell");

    const frag = document.createDocumentFragment();

    for (let y = 0; y < rows; y++) {
      for (let x = 0; x < cols; x++) {
        const idx = y * cols + x;
        const span = document.createElement("span");
        span.className = "terminal-cell";
        span.style.left = `${x}ch`;
        span.style.top = `${y}lh`;
        this.cellSpans[idx] = span;
        frag.appendChild(span);
      }
    }

    const overlay = document.createElement("div");
    overlay.style.position = "absolute";
    overlay.style.left = "0";
    overlay.style.top = "0";
    overlay.style.width = "100%";
    overlay.style.height = "100%";
    overlay.style.pointerEvents = "none";

    const echoLayer = document.createElement("div");
    echoLayer.style.position = "absolute";
    echoLayer.style.left = "0";
    echoLayer.style.top = "0";
    echoLayer.style.width = "100%";
    echoLayer.style.height = "100%";
    overlay.appendChild(echoLayer);

    const cursor = document.createElement("div");
    cursor.className = cursorClassNameForStyle(1);
    cursor.style.display = "none";
    overlay.appendChild(cursor);

    this.overlayElement = overlay;
    this.echoLayerElement = echoLayer;
    this.cursorElement = cursor;

    // Rebuild display root.
    this.displayElement.textContent = "";
    this.displayElement.appendChild(frag);
    this.displayElement.appendChild(overlay);
  }

  private renderStateForCell(cell: { ch: string; sgrState?: SgrState | null } | null): { text: string; className: string } {
    if (!cell) {
      return { text: "", className: "terminal-cell" };
    }

    const sgrState = cell.sgrState ?? null;

    // Default spaces are the common case; render as empty to avoid drawing and
    // avoid creating unnecessary text nodes. (Styled spaces still need a NBSP
    // so backgrounds/attributes paint.)
    const isSpace = cell.ch === " ";
    const isDefault = !sgrState || this.isDefaultSgrState(sgrState);

    if (isSpace && isDefault) {
      return { text: "", className: "terminal-cell" };
    }

    const sgrClass = sgrState && !this.isDefaultSgrState(sgrState)
      ? this.sgrStyleManager.getStyleClass(sgrState)
      : "";

    const className = sgrClass.length > 0 ? `terminal-cell ${sgrClass}` : "terminal-cell";
    const text = isSpace ? "\u00A0" : cell.ch;
    return { text, className };
  }
}

function makeControlCharsVisible(text: string): string {
  return text
    .replaceAll("\x1b", "<ESC>")
    .replaceAll("\r", "<CR>")
    .replaceAll("\n", "<LF>\n")
    .replaceAll("\t", "<TAB>")
    .replaceAll("\x07", "<BEL>");
}

function addOffset(startX: number, startY: number, offset: number, cols: number): [number, number] | null {
  if (cols <= 0) {
    return null;
  }
  const total = startX + offset;
  const x = ((total % cols) + cols) % cols;
  const y = startY + Math.floor(total / cols);
  return [x, y];
}

function cursorClassNameForStyle(style: number): string {
  // DECSCUSR mapping (CSI Ps SP q)
  // 0 or 1 = blinking block
  // 2 = steady block
  // 3 = blinking underline
  // 4 = steady underline
  // 5 = blinking bar
  // 6 = steady bar
  const normalized = style === 0 ? 1 : style;

  const base = "terminal-cursor";

  if (normalized === 1) return `${base} terminal-cursor--block terminal-cursor--blink`;
  if (normalized === 2) return `${base} terminal-cursor--block`;
  if (normalized === 3) return `${base} terminal-cursor--underline terminal-cursor--blink`;
  if (normalized === 4) return `${base} terminal-cursor--underline`;
  if (normalized === 5) return `${base} terminal-cursor--bar terminal-cursor--blink`;
  if (normalized === 6) return `${base} terminal-cursor--bar`;

  return `${base} terminal-cursor--block terminal-cursor--blink`;
}

function encodeCtrlKey(e: KeyboardEvent): string | null {
  if (!e.ctrlKey || e.metaKey) {
    return null;
  }

  // Ctrl+C and Ctrl+D are the main ones that matter for shells.
  const k = e.key;
  if (k === "c" || k === "C") {
    return String.fromCharCode(0x03);
  }
  if (k === "d" || k === "D") {
    return String.fromCharCode(0x04);
  }

  // Generic Ctrl+<letter> mapping.
  if (k.length === 1) {
    const ch = k.toUpperCase();
    const code = ch.charCodeAt(0);
    if (code >= 0x40 && code <= 0x5f) {
      return String.fromCharCode(code - 0x40);
    }
    if (code >= 0x41 && code <= 0x5a) {
      return String.fromCharCode(code - 0x40);
    }
  }

  return null;
}

interface EncodeKeyOptions {
  applicationCursorKeys: boolean;
}

function xtermModifierParam(e: KeyboardEvent): number {
  // xterm modifier encoding: 1 + (shift?1) + (alt?2) + (ctrl?4)
  // https://invisible-island.net/xterm/ctlseqs/ctlseqs.html
  let mod = 1;
  if (e.shiftKey) mod += 1;
  if (e.altKey) mod += 2;
  if (e.ctrlKey) mod += 4;
  return mod;
}

function encodeKeyDownToTerminalBytes(e: KeyboardEvent, opts: EncodeKeyOptions): string | null {

  if (e.metaKey) {
    // Let browser/OS shortcuts work.
    return null;
  }

  const ctrl = encodeCtrlKey(e);

  if (ctrl) {
    return ctrl;
  }

  switch (e.key) {
    case "Enter":
      return "\r";
    case "Backspace":
      // Most shells in raw mode expect DEL (0x7f) for backspace.
      return "\x7f";
    case "Tab":
      return "\t";
    case "Escape":
      return "\x1b";
    case "ArrowUp":
      return opts.applicationCursorKeys ? "\x1bOA" : "\x1b[A";
    case "ArrowDown":
      return opts.applicationCursorKeys ? "\x1bOB" : "\x1b[B";
    case "ArrowRight":
      return opts.applicationCursorKeys ? "\x1bOC" : "\x1b[C";
    case "ArrowLeft":
      return opts.applicationCursorKeys ? "\x1bOD" : "\x1b[D";
    case "Home":
      return "\x1b[H";
    case "End":
      return "\x1b[F";
    case "Delete":
      return "\x1b[3~";
    case "Insert":
      return "\x1b[2~";
    case "PageUp":
      return "\x1b[5~";
    case "PageDown":
      return "\x1b[6~";

    // Function keys (xterm-compatible)
    // Common mappings:
    // - F1-F4: SS3 P/Q/R/S (or CSI 1;M P/Q/R/S with modifiers)
    // - F5-F12: CSI 15/17/18/19/20/21/23/24 ~ (with optional ;M)
    case "F1":
    case "F2":
    case "F3":
    case "F4": {
      const mod = xtermModifierParam(e);
      const final = e.key === "F1" ? "P" : e.key === "F2" ? "Q" : e.key === "F3" ? "R" : "S";
      if (mod === 1) {
        return "\x1bO" + final;
      }
      return `\x1b[1;${mod}${final}`;
    }

    case "F5":
    case "F6":
    case "F7":
    case "F8":
    case "F9":
    case "F10":
    case "F11":
    case "F12": {
      const code =
        e.key === "F5" ? 15 :
        e.key === "F6" ? 17 :
        e.key === "F7" ? 18 :
        e.key === "F8" ? 19 :
        e.key === "F9" ? 20 :
        e.key === "F10" ? 21 :
        e.key === "F11" ? 23 :
        24;

      const mod = xtermModifierParam(e);
      if (mod === 1) {
        return `\x1b[${code}~`;
      }
      return `\x1b[${code};${mod}~`;
    }
  }

  // Ignore non-text keys (Shift, Alt, etc)
  if (e.key.length !== 1) {
    return null;
  }

  // Alt as ESC prefix (best-effort). On macOS option often produces a different
  // character already, so only do this when the produced key is a plain ASCII.
  if (e.altKey) {
    const code = e.key.charCodeAt(0);
    if (code >= 0x20 && code <= 0x7e) {
      return "\x1b" + e.key;
    }
  }

  return e.key;
}



function debugLogIncoming(text: string): void {

  // Only log if something interesting is present to keep noise down.
  if (!text.includes("\x1b[?")) {
    return;
  }

  // Focus on the sequences relevant to cursor visibility + alt screen + input modes.
  const interesting = [
    "\x1b[?25l",
    "\x1b[?25h",
    "\x1b[?1049h",
    "\x1b[?1049l",
    "\x1b[?1047h",
    "\x1b[?1047l",
    "\x1b[?47h",
    "\x1b[?47l",
    "\x1b[?1h",
    "\x1b[?1l",
    "\x1b[?2004h",
    "\x1b[?2004l",
    "\x1b[?1000h",
    "\x1b[?1000l",
    "\x1b[?1002h",
    "\x1b[?1002l",
    "\x1b[?1003h",
    "\x1b[?1003l",
    "\x1b[?1006h",
    "\x1b[?1006l",
  ];

  let found = false;
  for (const seq of interesting) {
    if (text.includes(seq)) {
      found = true;
      break;
    }
  }

  if (!found) {
    return;
  }

  // Make control bytes visible in console output.
  const visible = makeControlCharsVisible(text);
  console.log("[catty debugEsc] incoming:", visible);
}