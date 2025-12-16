import { StatefulTerminal, type ScreenSnapshot } from "./StatefulTerminal";

type InputMode = "cooked" | "raw";

export interface TerminalControllerOptions {
  terminal: StatefulTerminal;
  displayElement: HTMLElement;
  inputElement: HTMLInputElement;
  cols?: number;
  rows?: number;
  websocketUrl?: string;
}

export class TerminalController {
  private readonly terminal: StatefulTerminal;
  private readonly displayElement: HTMLElement;
  private readonly inputElement: HTMLInputElement;

  private websocket: WebSocket | null = null;
  private repaintScheduled = false;
  private lastSnapshot: ScreenSnapshot | null = null;

  private inputMode: InputMode = "cooked";
  private applicationCursorKeys = false;

  private readonly removeListeners: Array<() => void> = [];

  constructor(options: TerminalControllerOptions) {
    this.terminal = options.terminal;
    this.displayElement = options.displayElement;
    this.inputElement = options.inputElement;

    // Ensure display container is positioned for absolute cell spans
    this.displayElement.style.position = this.displayElement.style.position || "relative";

    const unsubscribe = this.terminal.onUpdate((snapshot) => {
      this.lastSnapshot = snapshot;
      this.scheduleRepaint();
    });
    this.removeListeners.push(unsubscribe);

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

    this.setupInputHandlers();

    const cols = options.cols ?? this.terminal.cols;
    const rows = options.rows ?? this.terminal.rows;
    const websocketUrl = options.websocketUrl ?? this.defaultWebsocketUrl(cols, rows);

    this.connect(websocketUrl, cols, rows);

    // Initial paint
    this.lastSnapshot = this.terminal.getSnapshot();
    this.repaint();
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
        this.terminal.pushPtyText(ev.data);
        return;
      }

      if (ev.data instanceof ArrayBuffer) {
        const text = new TextDecoder().decode(new Uint8Array(ev.data));
        this.terminal.pushPtyText(text);
        return;
      }

      // Blob fallback
      if (ev.data instanceof Blob) {
        ev.data.arrayBuffer().then((buf) => {
          const text = new TextDecoder().decode(new Uint8Array(buf));
          this.terminal.pushPtyText(text);
        }).catch(() => {
          // ignore
        });
      }
    };
  }

  private setupInputHandlers(): void {

    // Keep focus on the input so keyboard works.
    const focusOnClick = () => {
      this.inputElement.focus();
    };
    
    this.displayElement.addEventListener("mousedown", focusOnClick);
    this.removeListeners.push(() => this.displayElement.removeEventListener("mousedown", focusOnClick));

    const onKeyDown = (e: KeyboardEvent) => {

      if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
        return;
      }

      // Cooked (default): local line buffer in the <input>, send only on Enter.
      // Minimal MVP: we rely on the <input> for local editing/echo.
      if (this.inputMode === "cooked") {

        // Allow Ctrl+C / Ctrl+D to be sent immediately.
        const ctrl = encodeCtrlKey(e);

        if (ctrl) {
          e.preventDefault();
          this.inputElement.value = "";
          this.websocket.send(ctrl);
          return;
        }

        if (e.key !== "Enter") {
          return;
        }

        e.preventDefault();

        const line = this.inputElement.value;
        this.inputElement.value = "";
        this.websocket.send(line + "\r");
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
      if (this.inputMode !== "raw") {
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

        frag.appendChild(span);
      }
    }

    this.displayElement.appendChild(frag);
  }
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
