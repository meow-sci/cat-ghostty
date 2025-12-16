import { StatefulTerminal, type ScreenSnapshot } from "./StatefulTerminal";

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

    this.setupInputHandlers();

    const cols = options.cols ?? this.terminal.cols;
    const rows = options.rows ?? this.terminal.rows;
    const websocketUrl = options.websocketUrl ?? this.defaultWebsocketUrl();

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

  private defaultWebsocketUrl(): string {
    const host = window.location.hostname || "localhost";
    return `ws://${host}:4444?cols=80&rows=25`;
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
      if (e.key !== "Enter") {
        return;
      }

      e.preventDefault();

      const line = this.inputElement.value;
      this.inputElement.value = "";

      if (!this.websocket || this.websocket.readyState !== WebSocket.OPEN) {
        return;
      }

      // Buffered line send: send full line then CR.
      this.websocket.send(line + "\r");
    };

    this.inputElement.addEventListener("keydown", onKeyDown);
    this.removeListeners.push(() => this.inputElement.removeEventListener("keydown", onKeyDown));

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
