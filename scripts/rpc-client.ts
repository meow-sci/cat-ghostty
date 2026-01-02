#!/usr/bin/env node

/**
 * RPC Client Application - Main Entry Point
 * 
 * A command-line interface for sending RPC commands to the terminal emulator.
 * Demonstrates how to use the Terminal Sequence RPC system from shell programs.
 * 
 * Requirements: 7.1, 7.2, 7.3, 7.4, 7.5
 */

import * as process from 'process';
import {
  createIgniteMainThrottleCommand,
  createShutdownMainEngineCommand,
  createSetThrottleCommand,
  createGetThrottleStatusQuery,
  createGetFuelLevelQuery,
  createFireAndForgetCommand,
  createQueryCommand,
  PredefinedCommands,
  isFireAndForgetCommand,
  isQueryCommand,
  getExpectedCommandType,
  CommandType
} from './rpc-command-builder.js';
import {
  TerminalInterface,
  parseCommandLineArgs,
  displayUsage,
  validateCliArgs,
  formatResponse,
  type CliArgs
} from './terminal-interface.js';

/**
 * Main application class for the RPC client
 */
class RpcClientApp {
  private terminalInterface: TerminalInterface;
  private verbose: boolean = false;

  constructor(verbose: boolean = false) {
    this.verbose = verbose;
    this.terminalInterface = new TerminalInterface(verbose);
  }

  /**
   * Executes the main application logic
   * @param args Command-line arguments
   * @returns Exit code (0 for success, 1 for error)
   */
  public async run(args: CliArgs): Promise<number> {
    try {
      // Handle help request
      if (args.help) {
        displayUsage();
        return 0;
      }

      // Validate arguments
      const validation = validateCliArgs(args);
      if (!validation.isValid) {
        console.error(`❌ Error: ${validation.error}`);
        console.error('Use --help for usage information.');
        return 1;
      }

      // Execute the requested command
      if (args.command) {
        return await this.executeCommand(args);
      } else if (args.commandId !== undefined) {
        return await this.executeCustomCommand(args.commandId, args.parameters);
      } else {
        console.error('❌ Error: No command specified.');
        console.error('Use --help for usage information.');
        return 1;
      }
    } catch (error) {
      console.error(`❌ Unexpected error: ${error instanceof Error ? error.message : String(error)}`);
      return 1;
    }
  }

  /**
   * Executes a named command
   * @param args Command-line arguments
   * @returns Exit code
   */
  private async executeCommand(args: CliArgs): Promise<number> {
    const command = args.command!;

    try {
      switch (command) {
        case 'ignite':
          return await this.executeIgniteCommand();

        case 'shutdown':
          return await this.executeShutdownCommand();

        case 'throttle':
          if (!args.parameters || args.parameters.length === 0) {
            console.error('❌ Error: Throttle command requires a percentage parameter.');
            return 1;
          }
          const throttlePercent = args.parameters[0];
          if (throttlePercent === undefined) {
            console.error('❌ Error: Invalid throttle percentage.');
            return 1;
          }
          return await this.executeThrottleCommand(throttlePercent);

        case 'query-throttle':
          return await this.executeQueryThrottleCommand();

        case 'query-fuel':
          return await this.executeQueryFuelCommand();

        case 'send':
          if (args.commandId === undefined) {
            console.error('❌ Error: Send command requires a command ID (--command-id).');
            return 1;
          }
          return await this.executeCustomCommand(args.commandId, args.parameters);

        default:
          console.error(`❌ Error: Unknown command: ${command}`);
          return 1;
      }
    } catch (error) {
      console.error(`❌ Command execution failed: ${error instanceof Error ? error.message : String(error)}`);
      return 1;
    }
  }

  /**
   * Executes the ignite main throttle command
   * @returns Exit code
   */
  private async executeIgniteCommand(): Promise<number> {
    if (this.verbose) {
      console.error('[INFO] Executing ignite main throttle command (ID 1001)');
    }

    const sequence = createIgniteMainThrottleCommand();
    this.terminalInterface.writeSequence(sequence);

    console.log('🚀 Ignite main throttle command sent');
    return 0;
  }

  /**
   * Executes the shutdown main engine command
   * @returns Exit code
   */
  private async executeShutdownCommand(): Promise<number> {
    if (this.verbose) {
      console.error('[INFO] Executing shutdown main engine command (ID 1002)');
    }

    const sequence = createShutdownMainEngineCommand();
    this.terminalInterface.writeSequence(sequence);

    console.log('🛑 Shutdown main engine command sent');
    return 0;
  }

  /**
   * Executes the set throttle command
   * @param percentage Throttle percentage (0-100)
   * @returns Exit code
   */
  private async executeThrottleCommand(percentage: number): Promise<number> {
    if (this.verbose) {
      console.error(`[INFO] Executing set throttle command (ID 1012) with ${percentage}%`);
    }

    const sequence = createSetThrottleCommand(percentage);
    this.terminalInterface.writeSequence(sequence);

    console.log(`⚡ Set throttle to ${percentage}% command sent`);
    return 0;
  }

  /**
   * Executes the query throttle status command
   * @returns Exit code
   */
  private async executeQueryThrottleCommand(): Promise<number> {
    if (this.verbose) {
      console.error('[INFO] Executing query throttle status command (ID 2001)');
    }

    const sequence = createGetThrottleStatusQuery();
    this.terminalInterface.writeSequence(sequence);

    console.log('❓ Throttle status query sent, waiting for response...');

    try {
      const response = await this.terminalInterface.readResponse();
      console.log(formatResponse(response));
      return 0;
    } catch (error) {
      console.error(`❌ Query failed: ${error instanceof Error ? error.message : String(error)}`);
      return 1;
    }
  }

  /**
   * Executes the query fuel level command
   * @returns Exit code
   */
  private async executeQueryFuelCommand(): Promise<number> {
    if (this.verbose) {
      console.error('[INFO] Executing query fuel level command (ID 2021)');
    }

    const sequence = createGetFuelLevelQuery();
    this.terminalInterface.writeSequence(sequence);

    console.log('❓ Fuel level query sent, waiting for response...');

    try {
      const response = await this.terminalInterface.readResponse();
      console.log(formatResponse(response));
      return 0;
    } catch (error) {
      console.error(`❌ Query failed: ${error instanceof Error ? error.message : String(error)}`);
      return 1;
    }
  }

  /**
   * Executes a custom command with the specified ID and parameters
   * @param commandId The command ID to send
   * @param parameters Optional additional parameters
   * @returns Exit code
   */
  private async executeCustomCommand(commandId: number, parameters?: number[]): Promise<number> {
    if (this.verbose) {
      console.error(`[INFO] Executing custom command ID ${commandId} with parameters: ${parameters?.join(', ') || 'none'}`);
    }

    try {
      let sequence: string;
      let isQuery = false;

      // Determine command type based on ID range
      if (isFireAndForgetCommand(commandId)) {
        sequence = createFireAndForgetCommand(commandId, parameters);
        console.log(`🔧 Fire-and-forget command ${commandId} sent`);
      } else if (isQueryCommand(commandId)) {
        sequence = createQueryCommand(commandId, parameters);
        isQuery = true;
        console.log(`❓ Query command ${commandId} sent, waiting for response...`);
      } else {
        console.error(`❌ Error: Command ID ${commandId} is not in a valid range (1000-2999)`);
        return 1;
      }

      this.terminalInterface.writeSequence(sequence);

      // If it's a query command, wait for response
      if (isQuery) {
        try {
          const response = await this.terminalInterface.readResponse();
          console.log(formatResponse(response));
        } catch (error) {
          console.error(`❌ Query failed: ${error instanceof Error ? error.message : String(error)}`);
          return 1;
        }
      }

      return 0;
    } catch (error) {
      console.error(`❌ Command execution failed: ${error instanceof Error ? error.message : String(error)}`);
      return 1;
    }
  }
}

/**
 * Application entry point
 */
async function main(): Promise<void> {
  // Parse command-line arguments
  const args = parseCommandLineArgs(process.argv.slice(2));
  
  // Create and run the application
  const app = new RpcClientApp(args.verbose || false);
  const exitCode = await app.run(args);
  
  // Exit with the appropriate code
  process.exit(exitCode);
}

// Handle unhandled promise rejections
process.on('unhandledRejection', (reason, promise) => {
  console.error('❌ Unhandled promise rejection:', reason);
  process.exit(1);
});

// Handle uncaught exceptions
process.on('uncaughtException', (error) => {
  console.error('❌ Uncaught exception:', error);
  process.exit(1);
});

// Run the application if this file is executed directly
if (require.main === module) {
  main().catch((error) => {
    console.error('❌ Application startup failed:', error);
    process.exit(1);
  });
}

export { RpcClientApp };