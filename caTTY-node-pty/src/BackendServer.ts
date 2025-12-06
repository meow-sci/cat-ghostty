import { WebSocketServer, WebSocket } from 'ws';
import { IPty, spawn } from '@lydell/node-pty';
import * as os from 'os';

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

        this.wss.on('connection', (ws) => {
          this.handleConnection(ws);
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

  private handleConnection(ws: WebSocket): void {
    const connectionId = Math.random().toString(36).substring(7);
    console.log(`New WebSocket connection (ID: ${connectionId})`);

    try {
      // Determine the appropriate shell for the OS
      const shell = this.config.shell || (os.platform() === 'win32' ? 'powershell.exe' : 'bash');
      
      // Spawn PTY process with default dimensions
      const pty = spawn(shell, [], {
        name: 'xterm-color',
        cols: 80,
        rows: 40,
        cwd: process.env.HOME || process.env.USERPROFILE || process.cwd(),
        env: process.env as { [key: string]: string }
      });

      // Store the connection
      this.connections.set(ws, pty);

      console.log(`PTY spawned with PID ${pty.pid} for shell: ${shell} (Connection ID: ${connectionId})`);

      // Set up event handlers
      this.setupPtyHandlers(ws, pty, connectionId);
      this.setupWebSocketHandlers(ws, pty, connectionId);

    } catch (error) {
      console.error(`Error spawning PTY (Connection ID: ${connectionId}):`, error);
      
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
      console.log(`PTY exited with code ${exitCode}, signal ${signal}`);
      
      // Close the associated WebSocket connection
      if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
        try {
          ws.close();
          console.log('WebSocket closed due to PTY exit');
        } catch (error) {
          console.error('Error closing WebSocket on PTY exit:', error);
        }
      }
      
      // Remove from connection map
      this.connections.delete(ws);
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
        } catch (e) {
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
      console.log(`WebSocket connection closed (Connection ID: ${connectionId})`);
      
      // Get the associated PTY
      const pty = this.connections.get(ws);
      if (pty) {
        try {
          // Terminate the PTY process
          pty.kill();
          console.log(`PTY process terminated for closed connection (Connection ID: ${connectionId})`);
        } catch (error) {
          console.error(`Error terminating PTY on close (Connection ID: ${connectionId}):`, error);
        }
        
        // Remove from connection map
        this.connections.delete(ws);
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
