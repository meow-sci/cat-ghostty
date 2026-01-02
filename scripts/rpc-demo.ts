/**
 * RPC Command Builder Demonstration
 * 
 * Shows how to use the RPC command builder module to create terminal escape sequences
 * for communicating with the game through the Terminal Sequence RPC system.
 */

import {
  createIgniteMainThrottleCommand,
  createShutdownMainEngineCommand,
  createSetThrottleCommand,
  createGetThrottleStatusQuery,
  createGetFuelLevelQuery,
  createFireAndForgetCommand,
  createQueryCommand,
  formatRpcSequence,
  PredefinedCommands,
  CommandType
} from './rpc-command-builder.js';

console.log('=== RPC Command Builder Demonstration ===\n');

// Demonstrate predefined fire-and-forget commands
console.log('🚀 Fire-and-Forget Commands:');
console.log('These commands execute immediately without expecting a response.\n');

const igniteCmd = createIgniteMainThrottleCommand();
console.log(`Ignite Main Throttle (ID 1001):`);
console.log(`  Sequence: ${JSON.stringify(igniteCmd)}`);
console.log(`  Hex bytes: ${Array.from(igniteCmd).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

const shutdownCmd = createShutdownMainEngineCommand();
console.log(`Shutdown Main Engine (ID 1002):`);
console.log(`  Sequence: ${JSON.stringify(shutdownCmd)}`);
console.log(`  Hex bytes: ${Array.from(shutdownCmd).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

const throttleCmd = createSetThrottleCommand(75);
console.log(`Set Throttle to 75% (ID 1012):`);
console.log(`  Sequence: ${JSON.stringify(throttleCmd)}`);
console.log(`  Hex bytes: ${Array.from(throttleCmd).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

// Demonstrate query commands
console.log('❓ Query Commands:');
console.log('These commands request information and expect a response.\n');

const throttleStatusQuery = createGetThrottleStatusQuery();
console.log(`Get Throttle Status (ID 2001):`);
console.log(`  Sequence: ${JSON.stringify(throttleStatusQuery)}`);
console.log(`  Hex bytes: ${Array.from(throttleStatusQuery).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

const fuelLevelQuery = createGetFuelLevelQuery();
console.log(`Get Fuel Level (ID 2021):`);
console.log(`  Sequence: ${JSON.stringify(fuelLevelQuery)}`);
console.log(`  Hex bytes: ${Array.from(fuelLevelQuery).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

// Demonstrate custom command creation
console.log('🔧 Custom Commands:');
console.log('Create custom commands using the builder functions.\n');

const customFireAndForget = createFireAndForgetCommand(1021, [1, 0]); // Toggle lights on
console.log(`Custom Fire-and-Forget (Toggle Lights On):`);
console.log(`  Sequence: ${JSON.stringify(customFireAndForget)}`);
console.log(`  Hex bytes: ${Array.from(customFireAndForget).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

const customQuery = createQueryCommand(2011); // Get position
console.log(`Custom Query (Get Position):`);
console.log(`  Sequence: ${JSON.stringify(customQuery)}`);
console.log(`  Hex bytes: ${Array.from(customQuery).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

// Demonstrate advanced formatting
console.log('⚙️ Advanced Formatting:');
console.log('Use formatRpcSequence for full control over command parameters.\n');

const advancedCmd = formatRpcSequence({
  commandId: 1012,
  version: 1,
  commandType: CommandType.FIRE_AND_FORGET,
  additionalParams: [50, 25] // Set throttle with additional parameters
});
console.log(`Advanced Command (Set Throttle with Extra Params):`);
console.log(`  Sequence: ${JSON.stringify(advancedCmd)}`);
console.log(`  Hex bytes: ${Array.from(advancedCmd).map(c => '0x' + c.charCodeAt(0).toString(16).padStart(2, '0')).join(' ')}\n`);

// Show protocol breakdown
console.log('📋 Protocol Format Breakdown:');
console.log('ESC [ > Pn ; Pv ; [additional params ;] Pc');
console.log('  ESC [ >    : Private use area prefix (0x1B 0x5B 0x3E)');
console.log('  Pn         : Command ID (1000-9999)');
console.log('  Pv         : Protocol version (currently 1)');
console.log('  Pc         : Command type final character (F/Q/R/E)');
console.log('  ; params   : Optional additional parameters\n');

// Show command ID ranges
console.log('🎯 Command ID Ranges:');
console.log('  1000-1999  : Fire-and-forget commands');
console.log('  2000-2999  : Query commands');
console.log('  3000-8999  : Reserved for future use');
console.log('  9000-9999  : System/error responses\n');

console.log('✅ RPC Command Builder demonstration complete!');
console.log('Use these functions in your client application to send commands to the terminal emulator.');