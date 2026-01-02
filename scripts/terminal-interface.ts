/**
 * Terminal Interface Module
 * 
 * Handles terminal I/O operations for the RPC client application.
 * Writes escape sequences to stdout and reads responses from stdin.
 * Provides command-line argument parsing functionality.
 * 
 * Requirements: 7.4, 7.5
 */

import * as process from 'process';
import * as readline from 'readline';

/**
 * Command-line argument structure
 */
export interface CliArgs {
  command?: string;
  commandId?: number;
  parameters?: number[];
  help?: boolean;
  verbose?: boolean;
}

/**
 * Response data structure for query commands
 */
export interface RpcResponse {
  commandId: number;
  version: number;
  responseType: 'R' | 'E';
  data?: number[] | undefined;
  error?: string | undefined;
  raw: string;
}

/**
 * Terminal interface for RPC communication
 */
export class TerminalInterface {
  private responseTimeout: number = 5000; // 5 second timeout for responses
  private verbose: boolean = false;

  constructor(verbose: boolean = false) {
    this.verbose = verbose;
  }

  /**
   * Writes an RPC escape sequence to stdout
   * @param sequence The formatted escape sequence to send
   */
  public writeSequence(sequence: string): void {
    if (this.verbose) {
      console.error(`[DEBUG] Sending sequence: ${JSON.stringify(sequence)}`);
      console.error(`[DEBUG] Hex bytes: ${Array.from(sequence).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}`);
    }
    
    // Write the sequence to stdout for the terminal emulator to process
    process.stdout.write(sequence);
  }

  /**
   * Reads a response from stdin with timeout
   * @returns Promise that resolves with the response or rejects on timeout
   */
  public async readResponse(): Promise<RpcResponse> {
    return new Promise((resolve, reject) => {
      const rl = readline.createInterface({
        input: process.stdin,
        output: process.stdout,
        terminal: false
      });

      let responseReceived = false;
      
      // Set up timeout
      const timeout = setTimeout(() => {
        if (!responseReceived) {
          responseReceived = true;
          rl.close();
          reject(new Error(`Response timeout after ${this.responseTimeout}ms`));
        }
      }, this.responseTimeout);

      // Listen for data on stdin
      rl.on('line', (line: string) => {
        if (responseReceived) return;
        
        if (this.verbose) {
          console.error(`[DEBUG] Received line: ${JSON.stringify(line)}`);
        }

        // Look for RPC response sequences in the line
        const response = this.parseResponseSequence(line);
        if (response) {
          responseReceived = true;
          clearTimeout(timeout);
          rl.close();
          resolve(response);
        }
      });

      rl.on('error', (error) => {
        if (!responseReceived) {
          responseReceived = true;
          clearTimeout(timeout);
          rl.close();
          reject(error);
        }
      });
    });
  }

  /**
   * Parses an RPC response sequence from terminal output
   * @param line The line of text that may contain a response sequence
   * @returns Parsed response object or null if no valid response found
   */
  private parseResponseSequence(line: string): RpcResponse | null {
    // Look for ESC [ > Pn ; Pv ; [data ;] R or ESC [ > 9999 ; 1 ; E patterns
    const responseRegex = /\x1B\[>(\d+);(\d+);([^RE]*?)([RE])/g;
    
    let match;
    while ((match = responseRegex.exec(line)) !== null) {
      const [fullMatch, commandIdStr, versionStr, dataStr, responseType] = match;
      
      if (!commandIdStr || !versionStr || !responseType) {
        continue; // Skip invalid matches
      }
      
      const commandId = parseInt(commandIdStr, 10);
      const version = parseInt(versionStr, 10);
      
      if (this.verbose) {
        console.error(`[DEBUG] Parsed response: commandId=${commandId}, version=${version}, type=${responseType}, data="${dataStr}"`);
      }

      // Parse data parameters if present
      let data: number[] | undefined;
      let error: string | undefined;
      
      if (responseType === 'R' && dataStr && dataStr.trim()) {
        // Parse response data parameters
        const dataParams = dataStr.trim().split(';').filter(p => p.length > 0);
        data = dataParams.map(p => parseInt(p, 10)).filter(n => !isNaN(n));
      } else if (responseType === 'E') {
        // Error response
        error = commandId === 9999 ? 'Command timeout or general error' : `Error for command ${commandId}`;
      }

      return {
        commandId,
        version,
        responseType: responseType as 'R' | 'E',
        data,
        error,
        raw: fullMatch
      };
    }

    return null;
  }

  /**
   * Sets the response timeout in milliseconds
   * @param timeoutMs Timeout in milliseconds
   */
  public setResponseTimeout(timeoutMs: number): void {
    this.responseTimeout = timeoutMs;
  }

  /**
   * Enables or disables verbose logging
   * @param enabled Whether to enable verbose logging
   */
  public setVerbose(enabled: boolean): void {
    this.verbose = enabled;
  }
}

/**
 * Parses command-line arguments
 * @param args Array of command-line arguments (typically process.argv.slice(2))
 * @returns Parsed arguments object
 */
export function parseCommandLineArgs(args: string[]): CliArgs {
  const result: CliArgs = {};
  
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    
    switch (arg) {
      case '--help':
      case '-h':
        result.help = true;
        break;
        
      case '--verbose':
      case '-v':
        result.verbose = true;
        break;
        
      case '--command-id':
      case '-c':
        if (i + 1 < args.length) {
          const nextArg = args[i + 1];
          if (nextArg) {
            const commandId = parseInt(nextArg, 10);
            if (!isNaN(commandId)) {
              result.commandId = commandId;
            }
          }
          i++; // Skip next argument
        }
        break;
        
      case '--parameters':
      case '-p':
        if (i + 1 < args.length) {
          const nextArg = args[i + 1];
          if (nextArg) {
            const params = nextArg.split(',').map(p => parseInt(p.trim(), 10)).filter(n => !isNaN(n));
            if (params.length > 0) {
              result.parameters = params;
            }
          }
          i++; // Skip next argument
        }
        break;
        
      default:
        // If it's not a flag and we don't have a command yet, treat it as the command
        if (arg && !arg.startsWith('-') && !result.command) {
          result.command = arg;
        } else if (arg && !arg.startsWith('-') && result.command) {
          // If we already have a command, treat additional arguments as parameters
          if (!result.parameters) {
            result.parameters = [];
          }
          const param = parseInt(arg, 10);
          if (!isNaN(param)) {
            result.parameters.push(param);
          }
        }
        break;
    }
  }
  
  return result;
}

/**
 * Displays usage instructions for the RPC client application
 */
export function displayUsage(): void {
  console.log('RPC Client Application - Terminal Sequence RPC System');
  console.log('');
  console.log('USAGE:');
  console.log('  node rpc-client.js <command> [options]');
  console.log('');
  console.log('COMMANDS:');
  console.log('  ignite              Send ignite main throttle command (ID 1001)');
  console.log('  shutdown            Send shutdown main engine command (ID 1002)');
  console.log('  throttle <percent>  Set throttle to specified percentage (ID 1012)');
  console.log('  query-throttle      Query current throttle status (ID 2001)');
  console.log('  query-fuel          Query current fuel level (ID 2021)');
  console.log('  send <id> [params]  Send custom command with specified ID and parameters');
  console.log('');
  console.log('OPTIONS:');
  console.log('  -h, --help          Show this help message');
  console.log('  -v, --verbose       Enable verbose output with debug information');
  console.log('  -c, --command-id    Specify command ID for custom commands');
  console.log('  -p, --parameters    Comma-separated list of parameters for custom commands');
  console.log('');
  console.log('EXAMPLES:');
  console.log('  node rpc-client.js ignite');
  console.log('  node rpc-client.js shutdown --verbose');
  console.log('  node rpc-client.js throttle 75');
  console.log('  node rpc-client.js query-throttle');
  console.log('  node rpc-client.js send 1021 --parameters 1,0');
  console.log('  node rpc-client.js --command-id 2011 --verbose');
  console.log('');
  console.log('PROTOCOL:');
  console.log('  The client sends escape sequences in the format: ESC [ > Pn ; Pv ; Pc');
  console.log('  - Pn: Command ID (1000-1999 for fire-and-forget, 2000-2999 for queries)');
  console.log('  - Pv: Protocol version (currently 1)');
  console.log('  - Pc: Command type (F=fire-and-forget, Q=query, R=response, E=error)');
  console.log('');
  console.log('INTEGRATION:');
  console.log('  Run this client inside a terminal emulator that supports the RPC system.');
  console.log('  The terminal will process the escape sequences and communicate with the game.');
}

/**
 * Validates command-line arguments
 * @param args Parsed command-line arguments
 * @returns Validation result with error message if invalid
 */
export function validateCliArgs(args: CliArgs): { isValid: boolean; error?: string } {
  // If help is requested, that's always valid
  if (args.help) {
    return { isValid: true };
  }
  
  // If command ID is specified, validate it
  if (args.commandId !== undefined) {
    if (args.commandId < 1000 || args.commandId > 9999) {
      return { isValid: false, error: 'Command ID must be in range 1000-9999' };
    }
  }
  
  // If parameters are specified, validate them
  if (args.parameters !== undefined) {
    for (const param of args.parameters) {
      if (param < 0) {
        return { isValid: false, error: 'Parameters must be non-negative integers' };
      }
    }
  }
  
  // Validate specific commands
  if (args.command) {
    switch (args.command) {
      case 'ignite':
      case 'shutdown':
      case 'query-throttle':
      case 'query-fuel':
        // These commands don't need additional validation
        break;
        
      case 'throttle':
        // Throttle command needs a percentage parameter
        if (!args.parameters || args.parameters.length === 0) {
          return { isValid: false, error: 'Throttle command requires a percentage parameter (0-100)' };
        }
        const throttlePercent = args.parameters[0];
        if (throttlePercent !== undefined && (throttlePercent < 0 || throttlePercent > 100)) {
          return { isValid: false, error: 'Throttle percentage must be between 0 and 100' };
        }
        break;
        
      case 'send':
        // Send command needs a command ID
        if (args.commandId === undefined) {
          return { isValid: false, error: 'Send command requires a command ID (--command-id or -c)' };
        }
        break;
        
      default:
        return { isValid: false, error: `Unknown command: ${args.command}` };
    }
  }
  
  return { isValid: true };
}

/**
 * Formats response data for display
 * @param response The RPC response to format
 * @returns Formatted string representation of the response
 */
export function formatResponse(response: RpcResponse): string {
  if (response.responseType === 'E') {
    return `❌ Error: ${response.error || 'Unknown error'}`;
  }
  
  let result = `✅ Response from command ${response.commandId}:`;
  
  if (response.data && response.data.length > 0) {
    // Format specific known responses
    switch (response.commandId) {
      case 2001: // Throttle status
        if (response.data.length >= 2) {
          const enabled = response.data[0] === 1 ? 'enabled' : 'disabled';
          const percentage = response.data[1];
          result += `\n  Throttle: ${enabled}, ${percentage}%`;
        } else {
          result += `\n  Data: ${response.data.join(', ')}`;
        }
        break;
        
      case 2021: // Fuel level
        if (response.data.length >= 1) {
          const fuelPercent = response.data[0];
          result += `\n  Fuel level: ${fuelPercent}%`;
        } else {
          result += `\n  Data: ${response.data.join(', ')}`;
        }
        break;
        
      default:
        result += `\n  Data: ${response.data.join(', ')}`;
        break;
    }
  } else {
    result += '\n  No data returned';
  }
  
  return result;
}