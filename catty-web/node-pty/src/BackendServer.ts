import { WebSocketServer, WebSocket } from 'ws';
import { IPty, spawn } from '@lydell/node-pty';
import * as os from 'os';
import * as child_process from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

export interface BackendServerConfig {
  port: number;
  shell?: string;
}

export class BackendServer {
  private wss: WebSocketServer | null = null;
  private connections: Map<WebSocket, IPty> = new Map();
  private config: BackendServerConfig;

  constructor(config: BackendServerConfig) {
    this.config = config;
  }

  private findWindowsShell(): string {
    console.log('Finding Windows shell...');
    // Try to find PowerShell in common locations
    const possiblePaths = [
      'powershell.exe', // Try PATH first
      path.join(process.env.WINDIR || 'C:\\Windows', 'System32', 'WindowsPowerShell', 'v1.0', 'powershell.exe'),
      path.join(process.env.WINDIR || 'C:\\Windows', 'System32', 'cmd.exe'),
      'C:\\Windows\\System32\\WindowsPowerShell\\v1.0\\powershell.exe',
      'C:\\Windows\\System32\\cmd.exe'
    ];

    console.log('Trying Windows shell paths:', possiblePaths);

    for (const shellPath of possiblePaths) {
      try {
        console.log(`Checking shell path: ${shellPath}`);
        // For simple names like 'powershell.exe', try to find them in PATH
        if (!path.isAbsolute(shellPath)) {
          try {
            child_process.execSync(`where ${shellPath}`, { stdio: 'ignore' });
            console.log(`Found shell in PATH: ${shellPath}`);
            return shellPath;
          } catch {
            console.log(`Shell not found in PATH: ${shellPath}`);
            continue;
          }
        } else {
          // For absolute paths, check if file exists
          if (fs.existsSync(shellPath)) {
            console.log(`Found shell at: ${shellPath}`);
            return shellPath;
          } else {
            console.log(`Shell not found at: ${shellPath}`);
          }
        }
      } catch (error) {
        console.log(`Error checking shell ${shellPath}:`, error);
        continue;
      }
    }

    throw new Error('No suitable shell found on Windows. Tried: ' + possiblePaths.join(', '));
  }

  private findUnixShell(): string {
    console.log('Finding Unix shell...');
    // Try user's preferred shell first
    if (process.env.SHELL) {
      console.log(`Trying user's preferred shell: ${process.env.SHELL}`);
      try {
        child_process.execSync(`which ${process.env.SHELL}`, { stdio: 'ignore' });
        console.log(`Found user's shell: ${process.env.SHELL}`);
        return process.env.SHELL;
      } catch {
        console.log(`User's shell not found: ${process.env.SHELL}`);
        // Fall through to try other shells
      }
    } else {
      console.log('No SHELL environment variable set');
    }

    // Try common shells in order of preference
    const commonShells = ['zsh', 'bash', 'sh'];
    console.log('Trying common shells:', commonShells);
    for (const shell of commonShells) {
      try {
        console.log(`Checking shell: ${shell}`);
        child_process.execSync(`which ${shell}`, { stdio: 'ignore' });
        console.log(`Found shell: ${shell}`);
        return shell;
      } catch {
        console.log(`Shell not found: ${shell}`);
        continue;
      }
    }

    throw new Error('No suitable shell found. Tried: ' + commonShells.join(', '));
  }

  async start(): Promise<void> {
    return new Promise((resolve, reject) => {
      try {
        this.wss = new WebSocketServer({ port: this.config.port });

        this.wss.on('listening', () => {
          console.log(`WebSocket server listening on port ${this.config.port}`);
          resolve();
        });

        this.wss.on('error', (error) => {
          console.error('WebSocket server error:', error);
          reject(error);
        });

        this.wss.on('connection', (ws, request) => {
          console.log(`request.url ${request.url}`);
          this.handleConnection(ws, new URL(`http://localhost${request.url ?? ""}`));
        });
      } catch (error) {
        reject(error);
      }
    });
  }

  async stop(): Promise<void> {
    return new Promise((resolve) => {
      // Close all active connections
      for (const [ws, pty] of this.connections.entries()) {
        try {
          pty.kill();
        } catch (error) {
          console.error('Error killing PTY:', error);
        }
        try {
          ws.close();
        } catch (error) {
          console.error('Error closing WebSocket:', error);
        }
      }
      this.connections.clear();

      // Close the WebSocket server
      if (this.wss) {
        this.wss.close(() => {
          console.log('WebSocket server stopped');
          resolve();
        });
      } else {
        resolve();
      }
    });
  }

  private handleConnection(ws: WebSocket, url: URL): void {

    const cols = parseInt(url.searchParams.get('cols') ?? "80");
    const rows = parseInt(url.searchParams.get('rows') ?? "25");

    const connectionId = Math.random().toString(36).substring(7);
    const timestamp = new Date().toISOString();
    console.log(`Requested terminal size: ${cols} cols, ${rows} rows`);
    console.log(`[${timestamp}] New WebSocket connection (ID: ${connectionId})`);
    console.log(`[${timestamp}] Active connections before: ${this.connections.size}`);

    try {
      const isWindows = os.platform() === 'win32';
      
      console.log(`[${timestamp}] Platform detected: ${isWindows ? 'Windows' : 'Unix/Mac'}`);
      console.log(`[${timestamp}] os.platform() = '${os.platform()}'`);
      console.log(`[${timestamp}] Config shell: ${this.config.shell || 'not set'}`);
      console.log(`[${timestamp}] Environment variables:`);
      console.log(`[${timestamp}]   WINDIR: ${process.env.WINDIR || 'not set'}`);
      console.log(`[${timestamp}]   SHELL: ${process.env.SHELL || 'not set'}`);
      console.log(`[${timestamp}]   HOME: ${process.env.HOME || 'not set'}`);
      console.log(`[${timestamp}]   USERPROFILE: ${process.env.USERPROFILE || 'not set'}`);
      
      let shell: string;
      let shellArgs: string[] = [];
      let term: string;
      let cwd: string;
      let env: { [key: string]: string };

      if (isWindows) {
        console.log(`[${timestamp}] Entering Windows configuration branch`);
        // Windows-specific configuration
        if (this.config.shell) {
          console.log(`[${timestamp}] Using configured shell: ${this.config.shell}`);
          shell = this.config.shell;
        } else {
          console.log(`[${timestamp}] No configured shell, finding Windows shell...`);
          shell = this.findWindowsShell();
        }
        
        // For PowerShell, add startup parameters
        if (shell.toLowerCase().includes('powershell')) {
          shellArgs = ['-NoLogo', '-NoProfile'];
          console.log(`[${timestamp}] PowerShell detected, adding args: ${shellArgs.join(' ')}`);
        }
        
        term = 'xterm-256color';
        cwd = process.env.USERPROFILE || process.env.HOME || process.cwd();
        
        // Windows environment setup
        env = {
          ...process.env,
          TERM: term,
          TERM_PROGRAM: 'caTTY',
          COLORTERM: 'truecolor',
          // Windows-specific environment variables
          FORCE_COLOR: '1',
          CLICOLOR_FORCE: '1'
        } as { [key: string]: string };
        
        // Remove npm-related environment variables that might interfere
        for (const key of Object.keys(env)) {
          if (key.toLowerCase().startsWith('npm_')) {
            delete env[key];
          }
        }
        
      } else {
        console.log(`[${timestamp}] Entering Unix/Mac configuration branch`);
        // Mac/Unix-specific configuration
        if (this.config.shell) {
          console.log(`[${timestamp}] Using configured shell: ${this.config.shell}`);
          shell = this.config.shell;
        } else {
          console.log(`[${timestamp}] No configured shell, finding Unix shell...`);
          shell = this.findUnixShell();
        }
        
        term = 'xterm-256color';
        cwd = process.env.HOME || process.cwd();
        
        // Unix environment setup
        env = {
          ...process.env,
          TERM: term,
          TERM_PROGRAM: 'caTTY',
          COLORTERM: 'truecolor',
          // Unix-specific environment variables
          FORCE_COLOR: '1',
          CLICOLOR_FORCE: '1'
        } as { [key: string]: string };
        
        // Remove npm-related environment variables
        for (const key of Object.keys(env)) {
          if (key.toLowerCase().startsWith('npm_')) {
            delete env[key];
          }
        }
      }

      console.log(`[${timestamp}] Final configuration:`);
      console.log(`[${timestamp}]   shell: ${shell}`);
      console.log(`[${timestamp}]   shellArgs: [${shellArgs.join(', ')}]`);
      console.log(`[${timestamp}]   term: ${term}`);
      console.log(`[${timestamp}]   cwd: ${cwd}`);
      console.log(`[${timestamp}]   cols: ${cols}, rows: ${rows}`);

      // Spawn PTY process with platform-specific configuration
      const pty = spawn(shell, shellArgs, {
        name: term,
        cols,
        rows,
        cwd,
        env
      });

      // Store the connection
      this.connections.set(ws, pty);

      console.log(`[${timestamp}] PTY spawned with PID ${pty.pid} for shell: ${shell} ${shellArgs.join(' ')} (Connection ID: ${connectionId})`);
      console.log(`[${timestamp}] Active connections after: ${this.connections.size}`);

      // Set up event handlers
      this.setupPtyHandlers(ws, pty, connectionId);
      this.setupWebSocketHandlers(ws, pty, connectionId);

    } catch (error) {
      console.error(`[${timestamp}] Error spawning PTY (Connection ID: ${connectionId}):`, error);

      // Send error message to client if possible
      try {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify({
            type: 'error',
            message: 'Failed to spawn shell process'
          }));
        }
      } catch (sendError) {
        console.error('Error sending error message to client:', sendError);
      }

      // Close the WebSocket
      ws.close();
    }
  }

  private setupPtyHandlers(ws: WebSocket, pty: IPty, _connectionId: string): void {
    // Forward PTY output to WebSocket client
    pty.onData((data: string) => {
      try {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(data);
        }
      } catch (error) {
        console.error('Error sending PTY data to WebSocket:', error);
      }
    });

    // Handle PTY exit
    pty.onExit(({ exitCode, signal }) => {
      const timestamp = new Date().toISOString();
      console.log(`[${timestamp}] PTY exited with code ${exitCode}, signal ${signal} (Connection ID: ${_connectionId})`);

      // Close the associated WebSocket connection
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        try {
          ws.close();
          console.log(`[${timestamp}] WebSocket closed due to PTY exit (Connection ID: ${_connectionId})`);
        } catch (error) {
          console.error(`[${timestamp}] Error closing WebSocket on PTY exit:`, error);
        }
      }

      // Remove from connection map
      this.connections.delete(ws);
      console.log(`[${timestamp}] Active connections after PTY exit: ${this.connections.size}`);
    });
  }

  private setupWebSocketHandlers(ws: WebSocket, pty: IPty, connectionId: string): void {
    // Forward WebSocket messages to PTY
    ws.on('message', (data: Buffer | string) => {
      try {
        const message = typeof data === 'string' ? data : data.toString();

        // Check if this is a resize message
        try {
          const parsed = JSON.parse(message);
          if (parsed.type === 'resize' && typeof parsed.cols === 'number' && typeof parsed.rows === 'number') {
            // Validate dimensions
            if (parsed.cols > 0 && parsed.rows > 0 && parsed.cols <= 1000 && parsed.rows <= 1000) {
              pty.resize(parsed.cols, parsed.rows);
              console.log(`PTY resized to ${parsed.cols}x${parsed.rows}`);
              return;
            } else {
              console.error('Invalid resize dimensions:', parsed.cols, parsed.rows);
              return;
            }
          }
        } catch {
          // Not JSON or not a resize message, treat as regular data
        }

        // Regular data - forward to PTY
        pty.write(message);
      } catch (error) {
        console.error('Error writing to PTY:', error);
      }
    });

    // Handle WebSocket close
    ws.on('close', () => {
      const timestamp = new Date().toISOString();
      console.log(`[${timestamp}] WebSocket connection closed (Connection ID: ${connectionId})`);

      // Get the associated PTY
      const pty = this.connections.get(ws);
      if (pty) {
        try {
          // Terminate the PTY process
          pty.kill();
          console.log(`[${timestamp}] PTY process terminated for closed connection (Connection ID: ${connectionId})`);
        } catch (error) {
          console.error(`[${timestamp}] Error terminating PTY on close (Connection ID: ${connectionId}):`, error);
        }

        // Remove from connection map
        this.connections.delete(ws);
        console.log(`[${timestamp}] Active connections after close: ${this.connections.size}`);
      }
    });

    // Handle WebSocket errors
    ws.on('error', (error) => {
      console.error(`WebSocket error (Connection ID: ${connectionId}):`, error);

      // Clean up resources on error
      const pty = this.connections.get(ws);
      if (pty) {
        try {
          pty.kill();
          console.log(`PTY process terminated due to WebSocket error (Connection ID: ${connectionId})`);
        } catch (cleanupError) {
          console.error(`Error cleaning up PTY on WebSocket error (Connection ID: ${connectionId}):`, cleanupError);
        }

        this.connections.delete(ws);
      }

      // Close the WebSocket if still open
      if (ws.readyState === WebSocket.OPEN) {
        try {
          ws.close();
        } catch (closeError) {
          console.error(`Error closing WebSocket (Connection ID: ${connectionId}):`, closeError);
        }
      }
    });
  }
}
