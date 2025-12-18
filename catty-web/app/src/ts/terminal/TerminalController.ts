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

  private websocket: WebSocket | null = null;
  private repaintScheduled = false;
  private lastSnapshot: ScreenSnapshot | null = null;

  private traceScheduled = false;
  private readonly traceChunks: TerminalTraceChunk[] = [];
  private traceRenderedCount = 0;

  private inputMode: InputMode = "cooked";
  private applicationCursorKeys = false;

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
      // We use them as a proxy to decide whether we should behave like a raw-key terminal.
      // - 1049/1047/47: alternate screen buffers
      // - 1: application cursor keys
      const modes = new Set(ev.modes);

      if (modes.has(1)) {
        this.applicationCursorKeys = ev.action === "set";
      }

      if (modes.has(47) || modes.has(1047) || modes.has(1049)) {
        this.setInputMode(ev.action === "set" ? "raw" : "cooked");
      }
    });
    this.removeListeners.push(unsubscribeDecModes);

    // Subscribe to terminal responses (device queries, etc.)
    const unsubscribeResponses = this.terminal.onResponse((response) => {
      this.sendResponseToApplication(response);
    });
    this.removeListeners.push(unsubscribeResponses);

    this.setupInputHandlers();

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

    // Best-effort: suppress an exact prefix match of the last cooked line we sent.
    // This avoids local-echo overlay + driver echo duplicating characters.
    let i = 0;
    const n = Math.min(text.length, remaining.length);
    while (i < n && text[i] === remaining[i]) {
      i++;
    }

    if (i === 0) {
      // If the next output doesn't look like the echo, stop trying.
      this.suppressCookedEchoRemaining = null;
      return text;
    }

    const nextRemaining = remaining.slice(i);
    this.suppressCookedEchoRemaining = nextRemaining.length > 0 ? nextRemaining : null;
    return text.slice(i);
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

          // Suppress the PTY's echo of the line we are about to send.
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
      this.websocket.send(text);
    };

    this.inputElement.addEventListener("paste", onPaste);
    this.removeListeners.push(() => this.inputElement.removeEventListener("paste", onPaste));

    // Autofocus on mount
    queueMicrotask(() => this.inputElement.focus());
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
      return;
    }
    this.repaintScheduled = true;

    requestAnimationFrame(() => {
      this.repaintScheduled = false;
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

    // Full repaint for MVP.
    this.displayElement.textContent = "";

    const frag = document.createDocumentFragment();

    for (let y = 0; y < snapshot.rows; y++) {
      const row = snapshot.cells[y];
      for (let x = 0; x < snapshot.cols; x++) {
        
        const cell = row[x];
        
        if (!cell || cell.ch === " ") {
          continue;
        }

        const span = document.createElement("span");
        span.className = "terminal-cell";
        span.textContent = cell.ch;
        span.style.position = "absolute";
        span.style.left = `${x}ch`;
        span.style.top = `${y}lh`;

        // Apply SGR styling if the cell has sgrState
        if (cell.sgrState) {
          const sgrClass = this.sgrStyleManager.getStyleClass(cell.sgrState);
          span.className += ` ${sgrClass}`;
        }

        frag.appendChild(span);
      }
    }

    // Cooked-mode local echo overlay.
    if (this.inputMode === "cooked" && this.cookedLine.length > 0) {
      const startX = snapshot.cursorX;
      const startY = snapshot.cursorY;

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
        frag.appendChild(span);
      }
    }

    // Cursor overlay (respects DECTCEM cursor visibility)
    if (snapshot.cursorVisible) {
      const cursorOffset = this.inputMode === "cooked" ? this.cookedCursorIndex : 0;
      const cursorPos = addOffset(snapshot.cursorX, snapshot.cursorY, cursorOffset, snapshot.cols);
      if (cursorPos) {
        const [cx, cy] = cursorPos;
        if (cy >= 0 && cy < snapshot.rows) {
          const cursor = document.createElement("div");
          cursor.className = cursorClassNameForStyle(snapshot.cursorStyle);
          cursor.style.left = `${cx}ch`;
          cursor.style.top = `${cy}lh`;
          frag.appendChild(cursor);
        }
      }
    }

    this.displayElement.appendChild(frag);
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

  // Focus on the sequences relevant to cursor visibility + alt screen.
  const _interesting = [
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
  ];

  let found = true;

  // let found = false;
  // for (const seq of interesting) {
  //   if (text.includes(seq)) {
  //     found = true;
  //     break;
  //   }
  // }

  if (!found) {
    return;
  }

  // Make control bytes visible in console output.
  const visible = makeControlCharsVisible(text);
  console.log("[catty debugEsc] incoming:", visible);
}