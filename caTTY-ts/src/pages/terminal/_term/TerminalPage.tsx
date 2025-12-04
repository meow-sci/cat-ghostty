import { Suspense, useLayoutEffect, useRef } from 'react';
import { loadWasm } from '../../../ts/terminal/wasm/LoadWasm.js';
import { Terminal } from '../../../ts/terminal/Terminal.js';
import { TerminalController } from '../../../ts/terminal/TerminalController.js';
import { SampleShell } from './SampleShell.js';
import type { GhosttyVtInstance } from '../../../ts/ghostty-vt.js';

// Wrapper to use WASM resource with Suspense
function wrapWasmLoader() {
  let status: 'pending' | 'success' | 'error' = 'pending';
  let result: GhosttyVtInstance | null = null;
  let error: Error | null = null;

  const promise = loadWasm()
    .then((wasm) => {
      status = 'success';
      result = wasm;
    })
    .catch((err) => {
      status = 'error';
      error = err instanceof Error ? err : new Error('Failed to load WASM');
    });

  return {
    read(): GhosttyVtInstance {
      if (status === 'pending') throw promise;
      if (status === 'error') throw error;
      return result!;
    },
  };
}

const wasmResource = wrapWasmLoader();

interface TerminalViewProps {
  wasmInstance: GhosttyVtInstance;
}

function TerminalView({ wasmInstance }: TerminalViewProps) {
  const displayRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  useLayoutEffect(() => {
    let terminal: Terminal | null = null;
    let controller: TerminalController | null = null;
    let shell: SampleShell | null = null;

    // Refs are guaranteed to be populated now
    if (!displayRef.current || !inputRef.current) {
      console.error('Refs not available - this should not happen');
      return;
    }

    // Create Terminal instance
    terminal = new Terminal(
      {
        cols: 80,
        rows: 24,
        scrollback: 1000,
      },
      {
        onBell: () => {
          console.log('Bell!');
        },
        onTitleChange: (title: string) => {
          document.title = title;
        },
        onClipboard: (content: string) => {
          console.log('Clipboard:', content);
        },
        onDataOutput: (data: Uint8Array) => {
          if (shell) {
            shell.processInput(data);
          }
        },
        onResize: (cols: number, rows: number) => {
          console.log('Resized:', cols, rows);
        },
        onStateChange: () => {
          if (controller) {
            controller.render();
          }
        },
      },
      wasmInstance
    );

    // Create shell simulation
    shell = new SampleShell({
      onOutput: (output: string) => {
        if (terminal) {
          terminal.write(output);
        }
      },
    });

    // Create TerminalController instance
    controller = new TerminalController({
      terminal,
      inputElement: inputRef.current,
      displayElement: displayRef.current,
      wasmInstance,
    });

    // Mount and render
    controller.mount();
    controller.render();

    // Write welcome message and initial prompt
    terminal.write('Welcome to caTTY Terminal Emulator!\r\n');
    terminal.write('This is a demo terminal with SampleShell.\r\n');
    terminal.write('Available commands: ls, echo\r\n');
    terminal.write('\r\n');
    terminal.write('$ ');

    // Cleanup on unmount
    return () => {
      controller?.unmount();
      terminal?.dispose();
      shell?.reset();
    };
  }, [wasmInstance]);

  return (
    <>
      <div ref={displayRef} id="display"></div>
      <input
        ref={inputRef}
        id="input"
        type="text"
        autoFocus
        autoComplete="off"
        autoCorrect="off"
        autoCapitalize="off"
        spellCheck={false}
      />
    </>
  );
}

function TerminalLoader() {
  const wasmInstance = wasmResource.read();
  return <TerminalView wasmInstance={wasmInstance} />;
}

export function TerminalPage() {
  return (
    <main id="root">
      <Suspense
        fallback={
          <div style={{ padding: '2rem', textAlign: 'center' }}>
            Loading terminal...
          </div>
        }
      >
        <TerminalLoader />
      </Suspense>
    </main>
  );
}
