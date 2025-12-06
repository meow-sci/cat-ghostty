/**
 * TerminalPage - Full terminal emulator with WebSocket backend integration
 * 
 * This component provides a complete terminal emulator that can connect to either:
 * 1. A real PTY shell via WebSocket (caTTY-node-pty backend)
 * 2. A demonstration SampleShell for testing without a backend
 * 
 * ## WebSocket Integration
 * 
 * The terminal connects to a Node.js backend server (caTTY-node-pty) that manages
 * PTY processes. Each WebSocket connection spawns a new shell process (bash on Unix,
 * powershell.exe on Windows).
 * 
 * ### Backend Setup
 * 
 * 1. Navigate to caTTY-node-pty directory
 * 2. Install dependencies: `pnpm install`
 * 3. Start the server: `pnpm dev` (development) or `pnpm start` (production)
 * 4. Server listens on port 3000 by default (configurable via PORT env variable)
 * 
 * ### WebSocket URL Configuration
 * 
 * The default WebSocket URL is `ws://localhost:4444`. To change it:
 * - Use the URL input field in the connection controls UI
 * - Or modify the default in the `websocketUrl` state initialization
 * 
 * ### Message Protocol
 * 
 * The WebSocket connection uses two types of messages:
 * 
 * 1. **Data Messages** (string or Uint8Array):
 *    - User input from terminal → backend → PTY process
 *    - PTY output → backend → terminal display
 * 
 * 2. **Resize Messages** (JSON):
 *    ```json
 *    { "type": "resize", "cols": 80, "rows": 24 }
 *    ```
 *    - Sent when terminal dimensions change
 *    - Backend updates PTY dimensions accordingly
 * 
 * ### Connection Lifecycle
 * 
 * 1. Page loads → WebSocket connection attempt (if mode is 'websocket')
 * 2. Backend spawns PTY process on connection
 * 3. Bidirectional data flow between terminal and PTY
 * 4. On disconnect: backend terminates PTY, terminal falls back to SampleShell
 * 5. On PTY exit: backend closes WebSocket
 * 
 * ### Troubleshooting
 * 
 * **Connection Failed / Error Status:**
 * - Ensure caTTY-node-pty backend is running (`pnpm dev` in caTTY-node-pty/)
 * - Check that the WebSocket URL matches the backend server address
 * - Verify no firewall is blocking port 3000
 * - Check browser console for detailed error messages
 * 
 * **CORS Issues:**
 * - WebSocket connections don't have CORS restrictions
 * - If using a different origin, ensure the backend allows the connection
 * 
 * **Port Conflicts:**
 * - If port 3000 is in use, set PORT environment variable before starting backend
 * - Update the WebSocket URL in the UI to match the new port
 * 
 * **PTY Spawn Errors:**
 * - Check backend logs for PTY spawn errors
 * - Ensure the shell (bash/powershell) is available on the system
 * - Verify @lydell/node-pty is properly installed in caTTY-node-pty
 * 
 * ### Fallback Behavior
 * 
 * If WebSocket connection fails, the terminal automatically falls back to SampleShell,
 * a demonstration shell that supports basic commands (ls, echo, red, green).
 * Users can manually switch between modes using the connection controls UI.
 */

import { Suspense, useLayoutEffect, useRef, useState } from 'react';
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
  
  // WebSocket connection configuration
  // Default URL points to caTTY-node-pty backend on localhost:4444
  const [websocketUrl, setWebsocketUrl] = useState('ws://localhost:4444');
  
  // Connection mode: 'websocket' for real PTY, 'sampleshell' for demo
  const [connectionMode, setConnectionMode] = useState<'sampleshell' | 'websocket'>('websocket');
  
  // Connection status tracking for UI feedback
  const [connectionStatus, setConnectionStatus] = useState<'disconnected' | 'connecting' | 'connected' | 'error'>('disconnected');
  
  // Error message for display in UI
  const [errorMessage, setErrorMessage] = useState<string>('');
  
  // Store controller and terminal references for reconnection functionality
  const controllerRef = useRef<TerminalController | null>(null);
  const terminalRef = useRef<Terminal | null>(null);

  /**
   * Reconnect handler - Attempts to re-establish WebSocket connection
   * 
   * This is called when the user clicks the "Reconnect" button after a connection failure.
   * It reuses the existing controller and terminal instances to avoid recreating the UI.
   */
  const handleReconnect = () => {
    if (controllerRef.current && terminalRef.current && connectionMode === 'websocket') {
      setConnectionStatus('connecting');
      setErrorMessage('');
      terminalRef.current.write('\r\nReconnecting to backend server...\r\n');
      
      try {
        // Attempt to establish new WebSocket connection
        controllerRef.current.connectWebSocket(websocketUrl);
        
        // Check connection status after allowing time for connection
        setTimeout(() => {
          if (controllerRef.current) {
            const state = controllerRef.current.getConnectionState();
            setConnectionStatus(state);
            
            if (state === 'connected') {
              terminalRef.current?.write('\x1b[32mReconnected successfully!\x1b[0m\r\n');
              setErrorMessage('');
            } else if (state === 'error') {
              const errMsg = 'Reconnection failed. Backend server may not be running.';
              setErrorMessage(errMsg);
              terminalRef.current?.write('\x1b[31m' + errMsg + '\x1b[0m\r\n');
            }
          }
        }, 500);
      } catch (error) {
        console.error('Reconnection error:', error);
        const errMsg = error instanceof Error ? error.message : 'Unknown reconnection error';
        setConnectionStatus('error');
        setErrorMessage(errMsg);
        terminalRef.current?.write('\x1b[31mReconnection error: ' + errMsg + '\x1b[0m\r\n');
      }
    }
  };

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

    // Create shell simulation (used as fallback or when in sampleshell mode)
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

    // Store references for reconnection
    controllerRef.current = controller;
    terminalRef.current = terminal;

    // Mount and render
    controller.mount();
    controller.render();

    // Handle connection based on selected mode
    if (connectionMode === 'websocket') {
      // WebSocket mode: Connect to caTTY-node-pty backend for real PTY shell
      setConnectionStatus('connecting');
      setErrorMessage('');
      terminal.write('Connecting to backend server at ' + websocketUrl + '...\r\n');
      
      try {
        // Initiate WebSocket connection to backend
        // The controller will handle WebSocket events (open, message, close, error)
        controller.connectWebSocket(websocketUrl);
        
        // Check connection status after allowing time for WebSocket handshake
        // WebSocket connection is asynchronous, so we poll the state
        setTimeout(() => {
          const state = controller.getConnectionState();
          setConnectionStatus(state);
          
          if (state === 'connected') {
            // Successfully connected to backend PTY
            terminal.write('\x1b[32mConnected to real shell!\x1b[0m\r\n');
            setErrorMessage('');
          } else if (state === 'error') {
            // Connection failed - provide helpful error message
            const errMsg = 'Failed to connect to backend server. Check that caTTY-node-pty is running.';
            setErrorMessage(errMsg);
            terminal.write('\x1b[31m' + errMsg + '\x1b[0m\r\n');
            terminal.write('\x1b[33mFalling back to SampleShell.\x1b[0m\r\n');
            terminal.write('Available commands: ls, echo, red, green\r\n');
            terminal.write('\r\n');
            terminal.write('$ ');
          }
        }, 500);
      } catch (error) {
        // Handle synchronous errors during connection attempt
        console.error('WebSocket connection error:', error);
        const errMsg = error instanceof Error ? error.message : 'Unknown connection error';
        setConnectionStatus('error');
        setErrorMessage(errMsg);
        terminal.write('\x1b[31mConnection error: ' + errMsg + '\x1b[0m\r\n');
        terminal.write('\x1b[33mUsing SampleShell.\x1b[0m\r\n');
        terminal.write('Available commands: ls, echo, red, green\r\n');
        terminal.write('\r\n');
        terminal.write('$ ');
      }
    } else {
      // SampleShell mode: Use demonstration shell without backend
      setConnectionStatus('disconnected');
      setErrorMessage('');
      terminal.write('Welcome to caTTY Terminal Emulator!\r\n');
      terminal.write('This is a demo terminal with SampleShell.\r\n');
      terminal.write('Available commands: ls, echo, red, green\r\n');
      terminal.write('\r\n');
      terminal.write('$ ');
    }

    // Cleanup on unmount
    return () => {
      controller?.unmount();
      terminal?.dispose();
      shell?.reset();
    };
  }, [wasmInstance, connectionMode, websocketUrl]);

  return (
    <>
      {/* Connection controls */}
      <div style={{
        position: 'absolute',
        top: '10px',
        right: '10px',
        zIndex: 1000,
        background: 'rgba(0, 0, 0, 0.8)',
        padding: '10px',
        borderRadius: '5px',
        color: '#fff',
        fontSize: '12px',
        fontFamily: 'monospace'
      }}>
        <div style={{ marginBottom: '8px' }}>
          <label style={{ marginRight: '8px' }}>Mode:</label>
          <select 
            value={connectionMode} 
            onChange={(e) => setConnectionMode(e.target.value as 'sampleshell' | 'websocket')}
            style={{
              background: '#333',
              color: '#fff',
              border: '1px solid #555',
              padding: '2px 5px',
              borderRadius: '3px'
            }}
          >
            <option value="websocket">WebSocket (Real PTY)</option>
            <option value="sampleshell">SampleShell (Demo)</option>
          </select>
        </div>
        
        {connectionMode === 'websocket' && (
          <div style={{ marginBottom: '8px' }}>
            <label style={{ marginRight: '8px' }}>URL:</label>
            <input 
              type="text" 
              value={websocketUrl}
              onChange={(e) => setWebsocketUrl(e.target.value)}
              style={{
                background: '#333',
                color: '#fff',
                border: '1px solid #555',
                padding: '2px 5px',
                borderRadius: '3px',
                width: '180px',
                fontSize: '11px'
              }}
            />
          </div>
        )}
        
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span>Status:</span>
          <span style={{
            display: 'inline-block',
            width: '10px',
            height: '10px',
            borderRadius: '50%',
            background: 
              connectionStatus === 'connected' ? '#0f0' :
              connectionStatus === 'connecting' ? '#ff0' :
              connectionStatus === 'error' ? '#f00' :
              '#888'
          }}></span>
          <span>{connectionStatus}</span>
        </div>
        
        {errorMessage && (
          <div style={{
            marginTop: '8px',
            padding: '5px',
            background: 'rgba(255, 0, 0, 0.2)',
            borderRadius: '3px',
            fontSize: '11px',
            maxWidth: '250px',
            wordWrap: 'break-word'
          }}>
            {errorMessage}
          </div>
        )}
        
        {connectionStatus === 'error' && connectionMode === 'websocket' && (
          <button
            onClick={handleReconnect}
            style={{
              marginTop: '8px',
              padding: '5px 10px',
              background: '#0066cc',
              color: '#fff',
              border: 'none',
              borderRadius: '3px',
              cursor: 'pointer',
              fontSize: '11px',
              width: '100%'
            }}
            onMouseOver={(e) => e.currentTarget.style.background = '#0052a3'}
            onMouseOut={(e) => e.currentTarget.style.background = '#0066cc'}
          >
            Reconnect
          </button>
        )}
      </div>
      
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
