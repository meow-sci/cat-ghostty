# RPC Command Builder Module

The RPC Command Builder module provides functions to format ESC [ > Pn ; Pv ; Pc sequences for the Terminal Sequence RPC system. It validates command IDs and parameter ranges according to the RPC protocol specification.

## Requirements Fulfilled

- **7.1**: Create functions to format ESC [ > Pn ; Pv ; Pc sequences
- **7.2**: Validate command IDs and parameter ranges  
- **7.3**: Support fire-and-forget and query command types

## Usage

### Basic Import

```typescript
import {
  createIgniteMainThrottleCommand,
  createShutdownMainEngineCommand,
  createSetThrottleCommand,
  createGetThrottleStatusQuery,
  createGetFuelLevelQuery,
  formatRpcSequence,
  CommandType,
  PredefinedCommands
} from './rpc-command-builder';
```

### Fire-and-Forget Commands

Fire-and-forget commands execute immediately without expecting a response:

```typescript
// Predefined commands
const igniteCmd = createIgniteMainThrottleCommand();
// Result: "\x1B[>1001;1F"

const shutdownCmd = createShutdownMainEngineCommand();
// Result: "\x1B[>1002;1F"

const throttleCmd = createSetThrottleCommand(75);
// Result: "\x1B[>1012;1;75F"

// Custom fire-and-forget command
const customCmd = createFireAndForgetCommand(1021, [1, 0]);
// Result: "\x1B[>1021;1;1;0F"
```

### Query Commands

Query commands request information and expect a response:

```typescript
// Predefined queries
const throttleStatusQuery = createGetThrottleStatusQuery();
// Result: "\x1B[>2001;1Q"

const fuelLevelQuery = createGetFuelLevelQuery();
// Result: "\x1B[>2021;1Q"

// Custom query command
const customQuery = createQueryCommand(2011);
// Result: "\x1B[>2011;1Q"
```

### Advanced Formatting

For full control over command parameters:

```typescript
const advancedCmd = formatRpcSequence({
  commandId: 1012,
  version: 1,
  commandType: CommandType.FIRE_AND_FORGET,
  additionalParams: [50, 25]
});
// Result: "\x1B[>1012;1;50;25F"
```

## Protocol Format

The RPC system uses the private use area format: **ESC [ > Pn ; Pv ; Pc**

- **ESC [ >**: Private use area prefix (0x1B 0x5B 0x3E)
- **Pn**: Command ID parameter (1000-9999 range)
- **Pv**: Protocol version (currently 1)
- **Pc**: Command type final character (F/Q/R/E)

### Command ID Ranges

| Range | Purpose | Examples |
|-------|---------|----------|
| 1000-1999 | Fire-and-forget commands | Engine control, navigation |
| 2000-2999 | Query commands | Status queries, sensor readings |
| 3000-8999 | Reserved for future use | - |
| 9000-9999 | System/error responses | Error handling |

### Command Type Final Characters

- **'F' (0x46)**: Fire-and-forget command
- **'Q' (0x51)**: Query command  
- **'R' (0x52)**: Response
- **'E' (0x45)**: Error response

## Predefined Commands

### Engine Control (1001-1010)
- `IGNITE_MAIN_THROTTLE: 1001`
- `SHUTDOWN_MAIN_ENGINE: 1002`

### Navigation (1011-1020)
- `SET_HEADING: 1011`
- `SET_THROTTLE: 1012`

### Systems (1021-1030)
- `TOGGLE_LIGHTS: 1021`
- `ACTIVATE_RCS: 1022`

### Engine Queries (2001-2010)
- `GET_THROTTLE_STATUS: 2001`
- `GET_ENGINE_TEMP: 2002`

### Navigation Queries (2011-2020)
- `GET_POSITION: 2011`
- `GET_VELOCITY: 2012`

### System Queries (2021-2030)
- `GET_FUEL_LEVEL: 2021`
- `GET_BATTERY_LEVEL: 2022`

## Validation Functions

The module provides comprehensive validation:

```typescript
// Validate command ID
const idValidation = validateCommandId(1001);
// Returns: { isValid: true }

// Validate command type
const typeValidation = validateCommandType('F');
// Returns: { isValid: true }

// Validate command ID/type matching
const matchValidation = validateCommandIdTypeMatch(1001, 'F');
// Returns: { isValid: true }

// Check command type
const isFireAndForget = isFireAndForgetCommand(1001); // true
const isQuery = isQueryCommand(2001); // true
const expectedType = getExpectedCommandType(1001); // 'F'
```

## Error Handling

All functions validate input parameters and throw descriptive errors for invalid inputs:

```typescript
try {
  const invalidCmd = formatRpcSequence({
    commandId: 999, // Invalid range
    commandType: 'F'
  });
} catch (error) {
  console.error(error.message); // "Invalid command ID: Command ID must be in range 1000-9999"
}
```

## Example Sequences

Based on the design document examples:

**Fire-and-Forget Commands:**
- Ignite Main Engine: `ESC [ > 1001 ; 1 ; F`
- Shutdown Engine: `ESC [ > 1002 ; 1 ; F`  
- Set Throttle 75%: `ESC [ > 1012 ; 1 ; 75 F`

**Query Commands:**
- Get Throttle Status: `ESC [ > 2001 ; 1 ; Q`
- Get Fuel Level: `ESC [ > 2021 ; 1 ; Q`

**Response Examples:**
- Throttle Status Response: `ESC [ > 2001 ; 1 ; 1 ; 75 R` (enabled, 75%)
- Error Response: `ESC [ > 9999 ; 1 ; E`

## Testing

Run the test suite to verify functionality:

```bash
cd scripts
bun run rpc-command-builder.test.ts
```

Run the demonstration:

```bash
cd scripts  
bun run rpc-demo.ts
```

## Integration with Terminal Emulator

To use these commands with the terminal emulator, write the generated sequences to `process.stdout`:

```typescript
import { createIgniteMainThrottleCommand } from './rpc-command-builder';

// Send ignite command to terminal
const igniteSequence = createIgniteMainThrottleCommand();
process.stdout.write(igniteSequence);
```

The terminal emulator will detect the private use area sequence and route it to the appropriate RPC handler for execution.