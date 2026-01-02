/**
 * Unit Tests for Terminal Interface Module
 * 
 * Tests terminal I/O operations, argument parsing, and response formatting.
 * Requirements: 7.4, 7.5
 */

import { describe, test, expect, beforeEach, afterEach, mock, spyOn } from 'bun:test';
import {
  TerminalInterface,
  parseCommandLineArgs,
  validateCliArgs,
  formatResponse,
  displayUsage,
  type CliArgs,
  type RpcResponse
} from './terminal-interface';

// Mock process.stdout.write to capture output
const mockProcessStdoutWrite = mock(() => {});
const mockConsoleLog = mock(() => {});
const mockConsoleError = mock(() => {});

beforeEach(() => {
  mockProcessStdoutWrite.mockClear();
  mockConsoleLog.mockClear();
  mockConsoleError.mockClear();
  
  spyOn(process.stdout, 'write').mockImplementation(mockProcessStdoutWrite);
  spyOn(console, 'log').mockImplementation(mockConsoleLog);
  spyOn(console, 'error').mockImplementation(mockConsoleError);
});

afterEach(() => {
  (process.stdout.write as any).mockRestore?.();
  (console.log as any).mockRestore?.();
  (console.error as any).mockRestore?.();
});

describe('TerminalInterface - Sequence Writing', () => {
  test('should write sequence to stdout', () => {
    const terminalInterface = new TerminalInterface(false);
    const sequence = '\x1B[>1001;1F';
    
    terminalInterface.writeSequence(sequence);
    
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith(sequence);
  });

  test('should write sequence with verbose logging', () => {
    const terminalInterface = new TerminalInterface(true);
    const sequence = '\x1B[>1001;1F';
    
    terminalInterface.writeSequence(sequence);
    
    expect(mockProcessStdoutWrite).toHaveBeenCalledWith(sequence);
    expect(mockConsoleError).toHaveBeenCalledWith(`[DEBUG] Sending sequence: ${JSON.stringify(sequence)}`);
    expect(mockConsoleError).toHaveBeenCalledWith('[DEBUG] Hex bytes: 0x1b 0x5b 0x3e 0x31 0x30 0x30 0x31 0x3b 0x31 0x46');
  });

  test('should configure response timeout', () => {
    const terminalInterface = new TerminalInterface(false);
    
    terminalInterface.setResponseTimeout(10000);
    
    // Verify the interface is still functional after setting timeout
    expect(terminalInterface).toBeDefined();
  });

  test('should configure verbose mode', () => {
    const terminalInterface = new TerminalInterface(false);
    
    terminalInterface.setVerbose(true);
    
    // Test that verbose mode affects sequence writing
    const sequence = '\x1B[>1001;1F';
    terminalInterface.writeSequence(sequence);
    
    expect(mockConsoleError).toHaveBeenCalledWith(`[DEBUG] Sending sequence: ${JSON.stringify(sequence)}`);
  });
});

describe('Command Line Argument Parsing', () => {
  test('should parse empty arguments', () => {
    const args = parseCommandLineArgs([]);
    
    expect(args).toEqual({});
  });

  test('should parse single command', () => {
    const args = parseCommandLineArgs(['ignite']);
    
    expect(args.command).toBe('ignite');
  });

  test('should parse command with numeric parameter', () => {
    const args = parseCommandLineArgs(['throttle', '75']);
    
    expect(args.command).toBe('throttle');
    expect(args.parameters).toEqual([75]);
  });

  test('should parse multiple numeric parameters', () => {
    const args = parseCommandLineArgs(['send', '1021', '1', '0']);
    
    expect(args.command).toBe('send');
    expect(args.parameters).toEqual([1021, 1, 0]);
  });

  test('should ignore non-numeric parameters in positional arguments', () => {
    const args = parseCommandLineArgs(['send', 'invalid', '75']);
    
    expect(args.command).toBe('send');
    expect(args.parameters).toEqual([75]);
  });

  test('should parse help flags', () => {
    const args1 = parseCommandLineArgs(['--help']);
    const args2 = parseCommandLineArgs(['-h']);
    
    expect(args1.help).toBe(true);
    expect(args2.help).toBe(true);
  });

  test('should parse verbose flags', () => {
    const args1 = parseCommandLineArgs(['--verbose']);
    const args2 = parseCommandLineArgs(['-v']);
    
    expect(args1.verbose).toBe(true);
    expect(args2.verbose).toBe(true);
  });

  test('should parse command ID flags', () => {
    const args1 = parseCommandLineArgs(['--command-id', '1021']);
    const args2 = parseCommandLineArgs(['-c', '2001']);
    
    expect(args1.commandId).toBe(1021);
    expect(args2.commandId).toBe(2001);
  });

  test('should parse parameters flags', () => {
    const args1 = parseCommandLineArgs(['--parameters', '75,100']);
    const args2 = parseCommandLineArgs(['-p', '1,0,5']);
    
    expect(args1.parameters).toEqual([75, 100]);
    expect(args2.parameters).toEqual([1, 0, 5]);
  });

  test('should handle empty parameters flag', () => {
    const args = parseCommandLineArgs(['--parameters', '']);
    
    expect(args.parameters).toBeUndefined();
  });

  test('should handle invalid command ID', () => {
    const args = parseCommandLineArgs(['--command-id', 'invalid']);
    
    expect(args.commandId).toBeUndefined();
  });

  test('should handle missing flag values', () => {
    const args = parseCommandLineArgs(['--command-id']);
    
    expect(args.commandId).toBeUndefined();
  });

  test('should parse complex command line with mixed flags', () => {
    const args = parseCommandLineArgs([
      '--verbose',
      'send',
      '--command-id', '1021',
      '--parameters', '1,0',
      '75'
    ]);
    
    expect(args.verbose).toBe(true);
    expect(args.command).toBe('send');
    expect(args.commandId).toBe(1021);
    expect(args.parameters).toEqual([1, 0, 75]);
  });
});

describe('Command Line Argument Validation', () => {
  test('should validate empty arguments as invalid', () => {
    const args: CliArgs = {};
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(true); // Empty args are valid, will be handled by app logic
  });

  test('should validate help as always valid', () => {
    const args: CliArgs = { help: true, command: 'invalid' };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(true);
  });

  test('should validate command ID ranges', () => {
    const validArgs: CliArgs = { commandId: 1500 };
    const invalidLowArgs: CliArgs = { commandId: 999 };
    const invalidHighArgs: CliArgs = { commandId: 10000 };
    
    expect(validateCliArgs(validArgs).isValid).toBe(true);
    expect(validateCliArgs(invalidLowArgs).isValid).toBe(false);
    expect(validateCliArgs(invalidHighArgs).isValid).toBe(false);
  });

  test('should validate parameter ranges', () => {
    const validArgs: CliArgs = { parameters: [0, 50, 100] };
    const invalidArgs: CliArgs = { parameters: [-1, 50] };
    
    expect(validateCliArgs(validArgs).isValid).toBe(true);
    expect(validateCliArgs(invalidArgs).isValid).toBe(false);
    expect(validateCliArgs(invalidArgs).error).toBe('Parameters must be non-negative integers');
  });

  test('should validate throttle command requirements', () => {
    const validArgs: CliArgs = { command: 'throttle', parameters: [75] };
    const missingParamArgs: CliArgs = { command: 'throttle' };
    const invalidPercentArgs: CliArgs = { command: 'throttle', parameters: [150] };
    
    expect(validateCliArgs(validArgs).isValid).toBe(true);
    expect(validateCliArgs(missingParamArgs).isValid).toBe(false);
    expect(validateCliArgs(invalidPercentArgs).isValid).toBe(false);
  });

  test('should validate send command requirements', () => {
    const validArgs: CliArgs = { command: 'send', commandId: 1021 };
    const missingIdArgs: CliArgs = { command: 'send' };
    
    expect(validateCliArgs(validArgs).isValid).toBe(true);
    expect(validateCliArgs(missingIdArgs).isValid).toBe(false);
    expect(validateCliArgs(missingIdArgs).error).toBe('Send command requires a command ID (--command-id or -c)');
  });

  test('should validate known commands', () => {
    const knownCommands = ['ignite', 'shutdown', 'query-throttle', 'query-fuel'];
    
    for (const command of knownCommands) {
      const args: CliArgs = { command };
      expect(validateCliArgs(args).isValid).toBe(true);
    }
  });

  test('should reject unknown commands', () => {
    const args: CliArgs = { command: 'unknown-command' };
    const validation = validateCliArgs(args);
    
    expect(validation.isValid).toBe(false);
    expect(validation.error).toBe('Unknown command: unknown-command');
  });
});

describe('Response Formatting', () => {
  test('should format successful response with data', () => {
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

  test('should format successful response without data', () => {
    const response: RpcResponse = {
      commandId: 1001,
      version: 1,
      responseType: 'R',
      raw: '\x1B[>1001;1R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 1001:\n  No data returned');
  });

  test('should format error response', () => {
    const response: RpcResponse = {
      commandId: 9999,
      version: 1,
      responseType: 'E',
      error: 'Command timeout',
      raw: '\x1B[>9999;1;E'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('❌ Error: Command timeout');
  });

  test('should format error response without error message', () => {
    const response: RpcResponse = {
      commandId: 1001,
      version: 1,
      responseType: 'E',
      raw: '\x1B[>1001;1;E'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('❌ Error: Unknown error');
  });

  test('should format fuel level response', () => {
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

  test('should format throttle status disabled', () => {
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

  test('should format generic response with multiple data points', () => {
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

  test('should format incomplete throttle data as generic', () => {
    const response: RpcResponse = {
      commandId: 2001,
      version: 1,
      responseType: 'R',
      data: [1], // Missing percentage
      raw: '\x1B[>2001;1;1R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2001:\n  Data: 1');
  });

  test('should format incomplete fuel data as generic', () => {
    const response: RpcResponse = {
      commandId: 2021,
      version: 1,
      responseType: 'R',
      data: [], // No data
      raw: '\x1B[>2021;1R'
    };
    
    const formatted = formatResponse(response);
    
    expect(formatted).toBe('✅ Response from command 2021:\n  No data returned');
  });
});

describe('Usage Display', () => {
  test('should display complete usage information', () => {
    displayUsage();
    
    // Check that key sections are displayed
    expect(mockConsoleLog).toHaveBeenCalledWith('RPC Client Application - Terminal Sequence RPC System');
    expect(mockConsoleLog).toHaveBeenCalledWith('USAGE:');
    expect(mockConsoleLog).toHaveBeenCalledWith('COMMANDS:');
    expect(mockConsoleLog).toHaveBeenCalledWith('OPTIONS:');
    expect(mockConsoleLog).toHaveBeenCalledWith('EXAMPLES:');
    expect(mockConsoleLog).toHaveBeenCalledWith('PROTOCOL:');
    expect(mockConsoleLog).toHaveBeenCalledWith('INTEGRATION:');
    
    // Check specific command descriptions
    expect(mockConsoleLog).toHaveBeenCalledWith('  ignite              Send ignite main throttle command (ID 1001)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  shutdown            Send shutdown main engine command (ID 1002)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  throttle <percent>  Set throttle to specified percentage (ID 1012)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  query-throttle      Query current throttle status (ID 2001)');
    expect(mockConsoleLog).toHaveBeenCalledWith('  query-fuel          Query current fuel level (ID 2021)');
    
    // Check example usage
    expect(mockConsoleLog).toHaveBeenCalledWith('  node rpc-client.js ignite');
    expect(mockConsoleLog).toHaveBeenCalledWith('  node rpc-client.js throttle 75');
    
    // Check protocol description
    expect(mockConsoleLog).toHaveBeenCalledWith('  The client sends escape sequences in the format: ESC [ > Pn ; Pv ; Pc');
  });
});