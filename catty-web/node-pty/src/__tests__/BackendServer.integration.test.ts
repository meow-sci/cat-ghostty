import { describe, it, expect } from 'vitest';
import { BackendServer } from '../BackendServer.js';
import { WebSocket } from 'ws';

describe('BackendServer Integration Tests', () => {
  it('should handle complete connection lifecycle', async () => {
    const testPort = 13000 + Math.floor(Math.random() * 1000);
    const server = new BackendServer({ port: testPort });
    
    try {
      // Start server
      await server.start();

      // Connect client
      const client = new WebSocket(`ws://localhost:${testPort}`);
      
      await new Promise<void>((resolve, reject) => {
        client.on('open', () => resolve());
        client.on('error', reject);
        setTimeout(() => reject(new Error('Connection timeout')), 5000);
      });

      // Verify connection is open
      expect(client.readyState).toBe(WebSocket.OPEN);

      // Close client
      client.close();
      
      // Wait for cleanup
      await new Promise(resolve => setTimeout(resolve, 200));

    } finally {
      await server.stop();
    }
  });

  it('should handle bidirectional data flow', async () => {
    const testPort = 13000 + Math.floor(Math.random() * 1000);
    const server = new BackendServer({ port: testPort });
    
    try {
      await server.start();

      const client = new WebSocket(`ws://localhost:${testPort}`);
      const receivedData: string[] = [];
      
      await new Promise<void>((resolve, reject) => {
        client.on('open', () => resolve());
        client.on('error', reject);
        setTimeout(() => reject(new Error('Connection timeout')), 5000);
      });

      // Set up data listener
      const dataPromise = new Promise<void>((resolve) => {
        client.on('message', (data: Buffer) => {
          receivedData.push(data.toString());
          if (receivedData.join('').includes('INTEGRATION_TEST')) {
            resolve();
          }
        });
        setTimeout(resolve, 2000);
      });

      // Send command
      client.send('echo "INTEGRATION_TEST"\n');

      // Wait for response
      await dataPromise;

      // Verify we received data
      expect(receivedData.length).toBeGreaterThan(0);

      client.close();
      await new Promise(resolve => setTimeout(resolve, 200));

    } finally {
      await server.stop();
    }
  });

  it('should handle error scenarios gracefully', async () => {
    const testPort = 13000 + Math.floor(Math.random() * 1000);
    const server = new BackendServer({ port: testPort });
    
    try {
      await server.start();

      const client = new WebSocket(`ws://localhost:${testPort}`);
      
      await new Promise<void>((resolve, reject) => {
        client.on('open', () => resolve());
        client.on('error', reject);
        setTimeout(() => reject(new Error('Connection timeout')), 5000);
      });

      // Send invalid resize message
      client.send(JSON.stringify({ type: 'resize', cols: -1, rows: -1 }));

      // Wait a bit
      await new Promise(resolve => setTimeout(resolve, 200));

      // Connection should still be open
      expect(client.readyState).toBe(WebSocket.OPEN);

      client.close();
      await new Promise(resolve => setTimeout(resolve, 200));

    } finally {
      await server.stop();
    }
  });
});
