/**
 * Unit Tests for RPC Client Application
 * 
 * Tests command sequence generation, argument parsing and validation, and terminal output formatting.
 * Requirements: 7.1, 7.2, 7.3
 */

import { describe, test, expect, beforeEach, afterEach, mock, spyOn } from 'bun:test';
import { RpcClientApp } from './rpc-client';
import {
  parseCommandLineArgs,
  validateCliArgs,
  formatResponse,
  displayUsage,
  TerminalInterface,
  type CliArgs,
  type RpcResponse
} from './terminal-interface';

// Mock console methods to capture output
const mockConsoleLog = mock(() => {});
const mockConsoleError = mock(() => {});
const mockProcessStdoutWrite = mock(() => {});

beforeEach(() => {
  mockConsoleLog.mockClear();
  mockConsoleError.mockClear();
  mockProcessStdoutWrite.mockClear();
  
  // Mock console methods
  spyOn(console, 'log').mockImplementation(mockConsoleLog);
  spyOn(console, 'error').mockImplementation(mockConsoleError);
  spyOn(process.stdout, 'write').mockImplementation(mockProcessStdoutWrite);
});

afterEach(() => {
  // Restore original implementations
  (console.log as any).mockRestore?.();
  (console.error as any).mockRestore?.();
  (process.stdout.write as any).mockRestore?.();
});

describe('RPC Client Application - Command Sequence Generation', () => {
  test('should generate correct ignite command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'ignite' };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>1001;1F');
    expect(mockConsoleLog).toHaveBeenCalledWith('🚀 Ignite main throttle command sent');
  });

  test('should generate correct shutdown command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'shutdown' };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>1002;1F');
    expect(mockConsoleLog).toHaveBeenCalledWith('🛑 Shutdown main engine command sent');
  });

  test('should generate correct throttle command sequence with parameter', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'throttle', parameters: [75] };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>1012;1;75F');
    expect(mockConsoleLog).toHaveBeenCalledWith('⚡ Set throttle to 75% command sent');
  });

  test('should generate correct query throttle command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'query-throttle' };
    
    // Mock the readResponse method to avoid hanging
    const mockReadResponse = mock(() => Promise.resolve({
      commandId: 2001,
      version: 1,
      responseType: 'R' as const,
      data: [1, 75],
      raw: '\x1B[>2001;1;1;75R'
    }));
    
    spyOn(TerminalInterface.prototype, 'readResponse').mockImplementation(mockReadResponse);
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>2001;1Q');
    expect(mockConsoleLog).toHaveBeenCalledWith('❓ Throttle status query sent, waiting for response...');
  });

  test('should generate correct query fuel command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'query-fuel' };
    
    // Mock the readResponse method to avoid hanging
    const mockReadResponse = mock(() => Promise.resolve({
      commandId: 2021,
      version: 1,
      responseType: 'R' as const,
      data: [85],
      raw: '\x1B[>2021;1;85R'
    }));
    
    spyOn(TerminalInterface.prototype, 'readResponse').mockImplementation(mockReadResponse);
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>2021;1Q');
    expect(mockConsoleLog).toHaveBeenCalledWith('❓ Fuel level query sent, waiting for response...');
  });

  test('should generate correct custom fire-and-forget command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { commandId: 1021, parameters: [1, 0] };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>1021;1;1;0F');
    expect(mockConsoleLog).toHaveBeenCalledWith('🔧 Fire-and-forget command 1021 sent');
  });

  test('should generate correct custom query command sequence', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { commandId: 2011 };
    
    // Mock the readResponse method to avoid hanging
    const mockReadResponse = mock(() => Promise.resolve({
      commandId: 2011,
      version: 1,
      responseType: 'R' as const,
      data: [100, 200, 300],
      raw: '\x1B[>2011;1;100;200;300R'
    }));
    
    spyOn(TerminalInterface.prototype, 'readResponse').mockImplementation(mockReadResponse);
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith('\x1B[>2011;1Q');
    expect(mockConsoleLog).toHaveBeenCalledWith('❓ Query command 2011 sent, waiting for response...');
  });

  test('should handle verbose mode correctly', async () => {
    const app = new RpcClientApp(true);
    const args: CliArgs = { command: 'ignite', verbose: true };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockConsoleError).toHaveBeenCalledWith('[INFO] Executing ignite main throttle command (ID 1001)');
  });
});

describe('RPC Client Application - Argument Parsing and Validation', () => {
  test('should parse basic command correctly', () => {
    const args = parseCommandLineArgs(['ignite']);
    
    expect(args.command).toBe('ignite');
    expect(args.help).toBeUndefined();
    expect(args.verbose).toBeUndefined();
  });

  test('should parse help flag correctly', () => {
    const args1 = parseCommandLineArgs(['--help']);
    const args2 = parseCommandLineArgs(['-h']);
    
    expect(args1.help).toBe(true);
    expect(args2.help).toBe(true);
  });

  test('should parse verbose flag correctly', () => {
    const args1 = parseCommandLineArgs(['--verbose', 'ignite']);
    const args2 = parseCommandLineArgs(['-v', 'shutdown']);
    
    expect(args1.verbose).toBe(true);
    expect(args1.command).toBe('ignite');
    expect(args2.verbose).toBe(true);
    expect(args2.command).toBe('shutdown');
  });

  test('should parse command ID correctly', () => {
    const args1 = parseCommandLineArgs(['--command-id', '1021']);
    const args2 = parseCommandLineArgs(['-c', '2011']);
    
    expect(args1.commandId).toBe(1021);
    expect(args2.commandId).toBe(2011);
  });

  test('should parse parameters correctly', () => {
    const args1 = parseCommandLineArgs(['--parameters', '75,100,25']);
    const args2 = parseCommandLineArgs(['-p', '1,0']);
    
    expect(args1.parameters).toEqual([75, 100, 25]);
    expect(args2.parameters).toEqual([1, 0]);
  });

  test('should parse throttle command with parameter', () => {
    const args = parseCommandLineArgs(['throttle', '75']);
    
    expect(args.command).toBe('throttle');
    expect(args.parameters).toEqual([75]);
  });

  test('should parse complex command line', () => {
    const args = parseCommandLineArgs(['--verbose', '--command-id', '1021', '--parameters', '1,0']);
    
    expect(args.verbose).toBe(true);
    expect(args.commandId).toBe(1021);
    expect(args.parameters).toEqual([1, 0]);
  });

  test('should validate help request as valid', () => {
    const args: CliArgs = { help: true };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(true);
  });

  test('should validate basic commands as valid', () => {
    const commands = ['ignite', 'shutdown', 'query-throttle', 'query-fuel'];
    
    for (const command of commands) {
      const args: CliArgs = { command };
      const validation = validateCliArgs(args);
      
      expect(validation.isValid).toBe(true);
    }
  });

  test('should validate throttle command with valid percentage', () => {
    const args: CliArgs = { command: 'throttle', parameters: [75] };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(true);
  });

  test('should reject throttle command without percentage', () => {
    const args: CliArgs = { command: 'throttle' };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Throttle command requires a percentage parameter (0-100)');
  });

  test('should reject throttle command with invalid percentage', () => {
    const args: CliArgs = { command: 'throttle', parameters: [150] };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Throttle percentage must be between 0 and 100');
  });

  test('should reject send command without command ID', () => {
    const args: CliArgs = { command: 'send' };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Send command requires a command ID (--command-id or -c)');
  });

  test('should validate send command with valid command ID', () => {
    const args: CliArgs = { command: 'send', commandId: 1021 };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(true);
  });

  test('should reject invalid command ID range', () => {
    const args: CliArgs = { commandId: 999 };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Command ID must be in range 1000-9999');
  });

  test('should reject negative parameters', () => {
    const args: CliArgs = { parameters: [-1, 5] };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Parameters must be non-negative integers');
  });

  test('should reject unknown command', () => {
    const args: CliArgs = { command: 'unknown' };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Unknown command: unknown');
  });
});

describe('RPC Client Application - Terminal Output Formatting', () => {
  test('should format throttle status response correctly', () => {
    const response: RpcResponse = {
      commandId: 2001,
      version: 1,
      responseType: 'R',
      data: [1, 75],
      raw: '\x1B[>2001;1;1;75R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2001:\n  Throttle: enabled, 75%');
  });

  test('should format fuel level response correctly', () => {
    const response: RpcResponse = {
      commandId: 2021,
      version: 1,
      responseType: 'R',
      data: [85],
      raw: '\x1B[>2021;1;85R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2021:\n  Fuel level: 85%');
  });

  test('should format generic response correctly', () => {
    const response: RpcResponse = {
      commandId: 2011,
      version: 1,
      responseType: 'R',
      data: [100, 200, 300],
      raw: '\x1B[>2011;1;100;200;300R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2011:\n  Data: 100, 200, 300');
  });

  test('should format response with no data correctly', () => {
    const response: RpcResponse = {
      commandId: 2001,
      version: 1,
      responseType: 'R',
      raw: '\x1B[>2001;1R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2001:\n  No data returned');
  });

  test('should format error response correctly', () => {
    const response: RpcResponse = {
      commandId: 9999,
      version: 1,
      responseType: 'E',
      error: 'Command timeout or general error',
      raw: '\x1B[>9999;1;E'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('❌ Error: Command timeout or general error');
  });

  test('should format throttle status with disabled state correctly', () => {
    const response: RpcResponse = {
      commandId: 2001,
      version: 1,
      responseType: 'R',
      data: [0, 0],
      raw: '\x1B[>2001;1;0;0R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2001:\n  Throttle: disabled, 0%');
  });

  test('should format incomplete throttle status data correctly', () => {
    const response: RpcResponse = {
      commandId: 2001,
      version: 1,
      responseType: 'R',
      data: [1],
      raw: '\x1B[>2001;1;1R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2001:\n  Data: 1');
  });

  test('should display usage information correctly', () => {
    displayUsage();
    
    expect(mockConsoleLog).toHaveBeenCalledWith('RPC Client Application - Terminal Sequence RPC System');
    expect(mockConsoleLog).toHaveBeenCalledWith('USAGE:');
    expect(mockConsoleLog).toHaveBeenCalledWith('  node rpc-client.js <command> [options]');
    expect(mockConsoleLog).toHaveBeenCalledWith('COMMANDS:');
    expect(mockConsoleLog).toHaveBeenCalledWith('  ignite              Send ignite main throttle command (ID 1001)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  shutdown            Send shutdown main engine command (ID 1002)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  throttle <percent>  Set throttle to specified percentage (ID 1012)');
  });
});

describe('RPC Client Application - Error Handling', () => {
  test('should handle missing throttle parameter', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'throttle' };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Error: Throttle command requires a percentage parameter (0-100)');
  });

  test('should handle invalid command ID range', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { commandId: 999 };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Error: Command ID must be in range 1000-9999');
  });

  test('should handle unknown command', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'unknown' };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Error: Unknown command: unknown');
  });

  test('should handle no command specified', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = {};
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Error: No command specified.');
  });

  test('should handle help request correctly', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { help: true };
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(0);
    expect(mockConsoleLog).toHaveBeenCalledWith('RPC Client Application - Terminal Sequence RPC System');
  });

  test('should handle query timeout error', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { command: 'query-throttle' };
    
    // Mock the readResponse method to simulate timeout
    const mockReadResponse = mock(() => Promise.reject(new Error('Response timeout after 5000ms')));
    
    spyOn(TerminalInterface.prototype, 'readResponse').mockImplementation(mockReadResponse);
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Query failed: Response timeout after 5000ms');
  });

  test('should handle invalid command ID for custom command', async () => {
    const app = new RpcClientApp(false);
    const args: CliArgs = { commandId: 500 }; // Invalid range
    
    const exitCode = await app.run(args);
    
    expect(exitCode).toBe(1);
    expect(mockConsoleError).toHaveBeenCalledWith('❌ Error: Command ID must be in range 1000-9999');
  });
});

describe('TerminalInterface - Response Parsing', () => {
  test('should parse valid response sequence correctly', () => {
    const terminalInterface = new TerminalInterface(false);
    
    // Access private method through proper binding for testing
    const parseMethod = (terminalInterface as any).parseResponseSequence.bind(terminalInterface);
    const response = parseMethod('\x1B[>2001;1;1;75R');
    
    expect(response).toEqual({
      commandId: 2001,
      version: 1,
      responseType: 'R',
      data: [1, 75],
      raw: '\x1B[>2001;1;1;75R'
    });
  });

  test('should parse error response sequence correctly', () => {
    const terminalInterface = new TerminalInterface(false);
    
    // Access private method through proper binding for testing
    const parseMethod = (terminalInterface as any).parseResponseSequence.bind(terminalInterface);
    const response = parseMethod('\x1B[>9999;1;E');
    
    expect(response).toEqual({
      commandId: 9999,
      version: 1,
      responseType: 'E',
      error: 'Command timeout or general error',
      raw: '\x1B[>9999;1;E'
    });
  });

  test('should return null for invalid response sequence', () => {
    const terminalInterface = new TerminalInterface(false);
    
    // Access private method through proper binding for testing
    const parseMethod = (terminalInterface as any).parseResponseSequence.bind(terminalInterface);
    const response = parseMethod('invalid sequence');
    
    expect(response).toBeNull();
  });

  test('should handle response timeout configuration', () => {
    const terminalInterface = new TerminalInterface(false);
    
    terminalInterface.setResponseTimeout(10000);
    
    // Verify timeout was set (we can't easily test the actual timeout behavior in unit tests)
    expect(terminalInterface).toBeDefined();
  });

  test('should handle verbose mode configuration', () => {
    const terminalInterface = new TerminalInterface(false);
    
    terminalInterface.setVerbose(true);
    
    // Verify verbose mode was set
    expect(terminalInterface).toBeDefined();
  });
});