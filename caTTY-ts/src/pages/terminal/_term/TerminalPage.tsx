import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { loadWasm } from '../../../ts/terminal/wasm/LoadWasm.js';
import { Terminal } from '../../../ts/terminal/Terminal.js';
import { TerminalController } from '../../../ts/terminal/TerminalController.js';
import { EchoShell } from './EchoShell.js';
import type { GhosttyVtInstance } from '../../../ts/ghostty-vt.js';

export function TerminalPage() {
  const displayRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const terminalRef = useRef<Terminal | null>(null);
  const controllerRef = useRef<TerminalController | null>(null);
  const shellRef = useRef<EchoShell | null>(null);

  // useLayoutEffect(() => {
  //   console.log("displayRef", displayRef);
  //   console.log("inputRef", inputRef);

  // }, []);

  useLayoutEffect(() => {
    let mounted = true;
    let terminal: Terminal | null = null;
    let controller: TerminalController | null = null;
    let shell: EchoShell | null = null;

            console.log("displayRef", displayRef);
        console.log("inputRef", inputRef);


    async function initialize() {
      try {
        console.log("displayRef", displayRef);
        console.log("inputRef", inputRef);
        // Load WASM instance
        const wasmInstance: GhosttyVtInstance = await loadWasm();

        if (!mounted) return;

        // Ensure refs are available
        if (!displayRef.current || !inputRef.current) {
          throw new Error('Display or input element not available');
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
              // Could trigger a visual bell effect here
            },
            onTitleChange: (title: string) => {
              document.title = title;
            },
            onClipboard: (content: string) => {
              console.log('Clipboard:', content);
              // Could implement clipboard write here
            },
            onDataOutput: (data: Uint8Array) => {
              // Send data to shell simulation
              if (shell) {
                shell.processInput(data);
              }
            },
            onResize: (cols: number, rows: number) => {
              console.log('Resized:', cols, rows);
            },
            onStateChange: () => {
              // Trigger re-render when terminal state changes
              if (controller) {
                controller.render();
              }
            },
          },
          wasmInstance
        );

        // Create shell simulation
        shell = new EchoShell((output: string) => {
          // Shell output callback - write to terminal
          if (terminal) {
            terminal.write(output);
          }
        });

        // Create TerminalController instance
        controller = new TerminalController({
          terminal,
          inputElement: inputRef.current,
          displayElement: displayRef.current,
          wasmInstance,
        });

        // Mount the controller
        controller.mount();

        // Store references
        terminalRef.current = terminal;
        controllerRef.current = controller;
        shellRef.current = shell;

        // Initial render
        controller.render();

        // Write welcome message
        terminal.write('Welcome to caTTY Terminal Emulator!\r\n');
        terminal.write('This is a demo terminal with a simple echo shell.\r\n');
        terminal.write('Type "help" for available commands.\r\n');
        terminal.write('\r\n');
        terminal.write('$ ');

        setIsLoading(false);
      } catch (err) {
        console.error('Failed to initialize terminal:', err);
        if (mounted) {
          setError(err instanceof Error ? err.message : 'Failed to initialize terminal');
          setIsLoading(false);
        }
      }
    }

    initialize();

    // Cleanup on unmount
    return () => {
      mounted = false;
      if (controller) {
        controller.unmount();
      }
      if (terminal) {
        terminal.dispose();
      }
      if (shell) {
        shell.reset();
      }
    };
  }, []);

  if (isLoading) {
    return (
      <main id="root">
        <div style={{ padding: '2rem', textAlign: 'center' }}>
          Loading terminal...
        </div>
      </main>
    );
  }

  if (error) {
    return (
      <main id="root">
        <div style={{ padding: '2rem', color: 'red' }}>
          Error: {error}
        </div>
      </main>
    );
  }

  return (
    <main id="root">
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
    </main>
  );
}
