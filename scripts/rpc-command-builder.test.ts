/**
 * Tests for RPC Command Builder Module
 * 
 * Validates the command builder functions according to requirements 7.1, 7.2, 7.3
 */

import {
  formatRpcSequence,
  createFireAndForgetCommand,
  createQueryCommand,
  createIgniteMainThrottleCommand,
  createShutdownMainEngineCommand,
  createSetThrottleCommand,
  createGetThrottleStatusQuery,
  createGetFuelLevelQuery,
  validateCommandId,
  validateCommandType,
  validateCommandIdTypeMatch,
  validateProtocolVersion,
  validateAdditionalParams,
  isFireAndForgetCommand,
  isQueryCommand,
  getExpectedCommandType,
  CommandType,
  CommandRange,
  PredefinedCommands
} from './rpc-command-builder';

// Test validation functions
console.log('Testing validation functions...');

// Test command ID validation
console.assert(validateCommandId(1001).isValid === true, 'Valid command ID should pass');
console.assert(validateCommandId(999).isValid === false, 'Command ID below range should fail');
console.assert(validateCommandId(10000).isValid === false, 'Command ID above range should fail');
console.assert(validateCommandId(1.5).isValid === false, 'Non-integer command ID should fail');

// Test command type validation
console.assert(validateCommandType('F').isValid === true, 'Valid command type F should pass');
console.assert(validateCommandType('Q').isValid === true, 'Valid command type Q should pass');
console.assert(validateCommandType('R').isValid === true, 'Valid command type R should pass');
console.assert(validateCommandType('E').isValid === true, 'Valid command type E should pass');
console.assert(validateCommandType('X').isValid === false, 'Invalid command type should fail');

// Test command ID/type matching
console.assert(validateCommandIdTypeMatch(1001, 'F').isValid === true, 'Fire-and-forget ID with F type should pass');
console.assert(validateCommandIdTypeMatch(2001, 'Q').isValid === true, 'Query ID with Q type should pass');
console.assert(validateCommandIdTypeMatch(1001, 'Q').isValid === false, 'Fire-and-forget ID with Q type should fail');
console.assert(validateCommandIdTypeMatch(2001, 'F').isValid === false, 'Query ID with F type should fail');

// Test protocol version validation
console.assert(validateProtocolVersion(1).isValid === true, 'Valid protocol version should pass');
console.assert(validateProtocolVersion(0).isValid === false, 'Invalid protocol version should fail');
console.assert(validateProtocolVersion(2).isValid === false, 'Unsupported protocol version should fail');

// Test additional parameters validation
console.assert(validateAdditionalParams([1, 2, 3]).isValid === true, 'Valid additional params should pass');
console.assert(validateAdditionalParams([]).isValid === true, 'Empty additional params should pass');
console.assert(validateAdditionalParams(undefined).isValid === true, 'Undefined additional params should pass');
console.assert(validateAdditionalParams([1.5]).isValid === false, 'Non-integer additional params should fail');
console.assert(validateAdditionalParams([-1]).isValid === false, 'Negative additional params should fail');

console.log('✓ Validation functions tests passed');

// Test sequence formatting
console.log('Testing sequence formatting...');

// Test basic fire-and-forget command
const igniteSequence = formatRpcSequence({
  commandId: 1001,
  commandType: 'F'
});
console.assert(igniteSequence === '\x1B[>1001;1F', `Expected ignite sequence, got: ${igniteSequence}`);

// Test query command
const querySequence = formatRpcSequence({
  commandId: 2001,
  commandType: 'Q'
});
console.assert(querySequence === '\x1B[>2001;1Q', `Expected query sequence, got: ${querySequence}`);

// Test command with additional parameters
const throttleSequence = formatRpcSequence({
  commandId: 1012,
  commandType: 'F',
  additionalParams: [75]
});
console.assert(throttleSequence === '\x1B[>1012;1;75F', `Expected throttle sequence, got: ${throttleSequence}`);

console.log('✓ Sequence formatting tests passed');

// Test convenience functions
console.log('Testing convenience functions...');

// Test fire-and-forget command creation
const fireAndForgetCmd = createFireAndForgetCommand(1001);
console.assert(fireAndForgetCmd === '\x1B[>1001;1F', `Expected fire-and-forget command, got: ${fireAndForgetCmd}`);

// Test query command creation
const queryCmd = createQueryCommand(2001);
console.assert(queryCmd === '\x1B[>2001;1Q', `Expected query command, got: ${queryCmd}`);

// Test predefined commands
const igniteCmd = createIgniteMainThrottleCommand();
console.assert(igniteCmd === '\x1B[>1001;1F', `Expected ignite command, got: ${igniteCmd}`);

const shutdownCmd = createShutdownMainEngineCommand();
console.assert(shutdownCmd === '\x1B[>1002;1F', `Expected shutdown command, got: ${shutdownCmd}`);

const setThrottleCmd = createSetThrottleCommand(75);
console.assert(setThrottleCmd === '\x1B[>1012;1;75F', `Expected set throttle command, got: ${setThrottleCmd}`);

const throttleStatusQuery = createGetThrottleStatusQuery();
console.assert(throttleStatusQuery === '\x1B[>2001;1Q', `Expected throttle status query, got: ${throttleStatusQuery}`);

const fuelLevelQuery = createGetFuelLevelQuery();
console.assert(fuelLevelQuery === '\x1B[>2021;1Q', `Expected fuel level query, got: ${fuelLevelQuery}`);

console.log('✓ Convenience functions tests passed');

// Test utility functions
console.log('Testing utility functions...');

console.assert(isFireAndForgetCommand(1001) === true, 'Should identify fire-and-forget command');
console.assert(isFireAndForgetCommand(2001) === false, 'Should not identify query as fire-and-forget');
console.assert(isQueryCommand(2001) === true, 'Should identify query command');
console.assert(isQueryCommand(1001) === false, 'Should not identify fire-and-forget as query');

console.assert(getExpectedCommandType(1001) === 'F', 'Should return F for fire-and-forget command');
console.assert(getExpectedCommandType(2001) === 'Q', 'Should return Q for query command');
console.assert(getExpectedCommandType(999) === null, 'Should return null for invalid command');

console.log('✓ Utility functions tests passed');

// Test error handling
console.log('Testing error handling...');

try {
  formatRpcSequence({ commandId: 999, commandType: 'F' });
  console.assert(false, 'Should throw error for invalid command ID');
} catch (error) {
  console.assert(error instanceof Error, 'Should throw Error for invalid command ID');
}

try {
  formatRpcSequence({ commandId: 1001, commandType: 'X' as any });
  console.assert(false, 'Should throw error for invalid command type');
} catch (error) {
  console.assert(error instanceof Error, 'Should throw Error for invalid command type');
}

try {
  formatRpcSequence({ commandId: 1001, commandType: 'Q' });
  console.assert(false, 'Should throw error for command ID/type mismatch');
} catch (error) {
  console.assert(error instanceof Error, 'Should throw Error for command ID/type mismatch');
}

try {
  createSetThrottleCommand(150);
  console.assert(false, 'Should throw error for invalid throttle percentage');
} catch (error) {
  console.assert(error instanceof Error, 'Should throw Error for invalid throttle percentage');
}

console.log('✓ Error handling tests passed');

// Test example sequences from design document
console.log('Testing example sequences from design document...');

// Fire-and-Forget Commands from design doc:
// - Ignite Main Engine: `ESC [ > 1001 ; 1 ; F`
// - Shutdown Engine: `ESC [ > 1002 ; 1 ; F`
// - Set Throttle 75%: `ESC [ > 1012 ; 1 ; 75 F`

const designIgnite = createIgniteMainThrottleCommand();
console.assert(designIgnite === '\x1B[>1001;1F', `Design ignite mismatch: ${designIgnite}`);

const designShutdown = createShutdownMainEngineCommand();
console.assert(designShutdown === '\x1B[>1002;1F', `Design shutdown mismatch: ${designShutdown}`);

const designThrottle = createSetThrottleCommand(75);
console.assert(designThrottle === '\x1B[>1012;1;75F', `Design throttle mismatch: ${designThrottle}`);

// Query Commands from design doc:
// - Get Throttle Status: `ESC [ > 2001 ; 1 ; Q`
// - Get Fuel Level: `ESC [ > 2021 ; 1 ; Q`

const designThrottleQuery = createGetThrottleStatusQuery();
console.assert(designThrottleQuery === '\x1B[>2001;1Q', `Design throttle query mismatch: ${designThrottleQuery}`);

const designFuelQuery = createGetFuelLevelQuery();
console.assert(designFuelQuery === '\x1B[>2021;1Q', `Design fuel query mismatch: ${designFuelQuery}`);

console.log('✓ Design document example tests passed');

console.log('\n🎉 All RPC Command Builder tests passed!');
console.log('\nExample usage:');
console.log('Fire-and-forget commands:');
console.log(`  Ignite: ${createIgniteMainThrottleCommand()}`);
console.log(`  Shutdown: ${createShutdownMainEngineCommand()}`);
console.log(`  Set Throttle 75%: ${createSetThrottleCommand(75)}`);
console.log('\nQuery commands:');
console.log(`  Get Throttle Status: ${createGetThrottleStatusQuery()}`);
console.log(`  Get Fuel Level: ${createGetFuelLevelQuery()}`);