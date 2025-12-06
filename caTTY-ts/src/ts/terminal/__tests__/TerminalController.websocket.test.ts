/**
 * Integration tests for TerminalController WebSocket functionality.
 * These tests verify the complete integration of WebSocket communication.
 */

import { describe, it, expect, beforeAll, vi } from 'vitest';
import { readFile } from 'fs/promises';
import { join } from 'path';
import type { GhosttyVtInstance } from '../../ghostty-vt.js';
import { Terminal } from '../Terminal.js';
import { TerminalController } from '../TerminalController.js';

// Mock DOM globals for Node.js testing
const mockDocument = {
  createElement: vi.fn(() => ({
    id: '',
    textContent: '',
    style: {},
    classList: {
      add: vi.fn(),
      remove: vi.fn(),
    },
    appendChild: vi.fn(),
  })),
  createDocumentFragment: vi.fn(() => ({
    appendChild: vi.fn(),
    childNodes: [],
  })),
  querySelector: vi.fn(() => null),
  head: {
    appendChild: vi.fn(),
  },
};

const mockWindow = {
  location: {
    protocol: 'http:',
  },
};

// Set up global mocks
(global as any).document = mockDocument;
(global as any).window = mockWindow;

describe('TerminalController WebSocket Integration Tests', () => {
  let wasmInstance: GhosttyVtInstance;
  
  beforeAll(async () => {
    // Load WASM instance for testing
    const wasmPath = join(__dirname, '../../../../public/ghostty-vt.wasm');
    const wasmBytes = await readFile(wasmPath);
    
    const wasmModule = await WebAssembly.instantiate(wasmBytes, {
      env: {
        log: (ptr: number, len: number) => {
          const wasmInstance: GhosttyVtInstance = wasmModule.instance as unknown as any;
          const bytes = new Uint8Array(wasmInstance.exports.memory.buffer, ptr, len);
          const text = new TextDecoder().decode(bytes);
          console.log('[wasm]', text);
        }
      }
    });
    
    wasmInstance = wasmModule.instance as unknown as any;
  });
  
  /**
   * Creates a test terminal and controller setup.
   */
  function createTestSetup() {
    const terminal = new Terminal(
      { cols: 80, rows: 24, scrollback: 1000 },
      {},
      wasmInstance
    );
    
    // Create mock DOM elements
    const inputElement = {
      addEventListener: () => {},
      removeEventListener: () => {},
      setAttribute: () => {},
      focus: () => {},
    } as unknown as HTMLInputElement;
    
    const displayElement = {
      addEventListener: () => {},
      removeEventListener: () => {},
      setAttribute: () => {},
      appendChild: vi.fn(),
      removeChild: vi.fn(),
      replaceChild: vi.fn(),
      innerHTML: '',
      style: {},
      classList: {
        add: () => {},
        remove: () => {},
        contains: () => false,
        toggle: () => false,
      },
      getBoundingClientRect: () => ({
        left: 0,
        top: 0,
        width: 640,
        height: 384,
        right: 640,
        bottom: 384,
        x: 0,
        y: 0,
        toJSON: () => {},
      }),
    } as unknown as HTMLElement;
    
    const controller = new TerminalController({
      terminal,
      inputElement,
      displayElement,
      wasmInstance,
    });
    
    return { terminal, controller };
  }
  
  it('should establish WebSocket connection and handle data flow', () => {
    const { terminal, controller } = createTestSetup();
    
    // Mock WebSocket
    const mockWebSocket = {
      readyState: 0,
      onopen: null as any,
      onmessage: null as any,
      onclose: null as any,
      onerror: null as any,
      send: vi.fn(),
      close: vi.fn(),
    };
    
    const originalWebSocket = (global as any).WebSocket;
    const MockWebSocketConstructor = function(this: any, url: string) {
      return mockWebSocket;
    };
    (MockWebSocketConstructor as any).OPEN = 1;
    (global as any).WebSocket = MockWebSocketConstructor;
    
    try {
      // Connect to WebSocket
      controller.connectWebSocket('ws://localhost:3000');
      expect(controller.getConnectionState()).toBe('connecting');
      
      // Simulate connection open
      mockWebSocket.readyState = 1;
      if (mockWebSocket.onopen) {
        mockWebSocket.onopen({} as Event);
      }
      expect(controller.getConnectionState()).toBe('connected');
      
      // Test data flow from WebSocket to terminal
      const testOutput = 'Hello from PTY\r\n';
      if (mockWebSocket.onmessage) {
        mockWebSocket.onmessage({ data: testOutput } as MessageEvent);
      }
      
      // Verify terminal received the data (cursor should have moved)
      const cursor = terminal.getCursor();
      expect(cursor.row).toBeGreaterThan(0);
      
      // Test data flow from terminal to WebSocket
      const testInput = new Uint8Array([65, 66, 67]); // "ABC"
      controller.handleDataOutput(testInput);
      
      // Verify data was sent through WebSocket
      expect(mockWebSocket.send).toHaveBeenCalled();
      expect(mockWebSocket.send.mock.calls[0][0]).toBeInstanceOf(ArrayBuffer);
      
      // Disconnect
      controller.disconnectWebSocket();
      expect(controller.getConnectionState()).toBe('disconnected');
      
    } finally {
      (global as any).WebSocket = originalWebSocket;
    }
  });
  
  it('should fall back to SampleShell on connection failure', () => {
    const { terminal, controller } = createTestSetup();
    
    // Mock WebSocket that fails
    const mockWebSocket = {
      readyState: 0,
      onopen: null as any,
      onmessage: null as any,
      onclose: null as any,
      onerror: null as any,
      send: vi.fn(),
      close: vi.fn(),
    };
    
    const originalWebSocket = (global as any).WebSocket;
    (global as any).WebSocket = function(this: any, url: string) {
      return mockWebSocket;
    };
    
    try {
      // Set up shell backend for fallback
      const shellBackendCalled = vi.fn();
      controller.setShellBackend(shellBackendCalled);
      
      // Connect
      controller.connectWebSocket('ws://localhost:3000');
      
      // Simulate connection error
      if (mockWebSocket.onerror) {
        mockWebSocket.onerror({} as Event);
      }
      
      // Verify error state
      expect(controller.getConnectionState()).toBe('error');
      
      // Verify fallback works
      const testData = new Uint8Array([65, 66, 67]);
      controller.handleDataOutput(testData);
      expect(shellBackendCalled).toHaveBeenCalledWith(testData);
      
    } finally {
      (global as any).WebSocket = originalWebSocket;
      controller.disconnectWebSocket();
    }
  });
  
  it('should handle page unload cleanup', () => {
    const { controller } = createTestSetup();
    
    // Mock WebSocket
    const mockWebSocket = {
      readyState: 1,
      onopen: null as any,
      onmessage: null as any,
      onclose: null as any,
      onerror: null as any,
      send: vi.fn(),
      close: vi.fn(),
    };
    
    const originalWebSocket = (global as any).WebSocket;
    const MockWebSocketConstructor = function(this: any, url: string) {
      return mockWebSocket;
    };
    (MockWebSocketConstructor as any).OPEN = 1;
    (global as any).WebSocket = MockWebSocketConstructor;
    
    try {
      // Connect
      controller.connectWebSocket('ws://localhost:3000');
      mockWebSocket.readyState = 1;
      if (mockWebSocket.onopen) {
        mockWebSocket.onopen({} as Event);
      }
      
      // Verify connected
      expect(controller.getConnectionState()).toBe('connected');
      
      // Simulate page unload by calling unmount
      controller.unmount();
      
      // Verify WebSocket was closed
      expect(mockWebSocket.close).toHaveBeenCalled();
      expect(controller.getConnectionState()).toBe('disconnected');
      
    } finally {
      (global as any).WebSocket = originalWebSocket;
    }
  });
});
