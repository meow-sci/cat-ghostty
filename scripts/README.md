# RPC Client Application

A command-line interface for sending RPC commands to the terminal emulator using the Terminal Sequence RPC system.

## Requirements

- [Bun](https://bun.sh/) runtime
- Terminal emulator with RPC system support

## Installation

```bash
cd scripts
bun install
```

## Usage

### Basic Commands

```bash
# Show help
bun run rpc-client.ts --help

# Send ignite main throttle command
bun run rpc-client.ts ignite

# Send shutdown main engine command  
bun run rpc-client.ts shutdown

# Set throttle to 75%
bun run rpc-client.ts throttle 75

# Query current throttle status
bun run rpc-client.ts query-throttle

# Query fuel level
bun run rpc-client.ts query-fuel
```

### Custom Commands

```bash
# Send custom fire-and-forget command
bun run rpc-client.ts send --command-id 1021 --parameters 1,0

# Send custom query command
bun run rpc-client.ts --command-id 2011

# Send command with verbose output
bun run rpc-client.ts ignite --verbose
```

### Command Line Options

- `-h, --help` - Show help message
- `-v, --verbose` - Enable verbose output with debug information
- `-c, --command-id` - Specify command ID for custom commands
- `-p, --parameters` - Comma-separated list of parameters for custom commands

## Protocol

The client sends escape sequences in the format: `ESC [ > Pn ; Pv ; Pc`

- **Pn**: Command ID (1000-1999 for fire-and-forget, 2000-2999 for queries)
- **Pv**: Protocol version (currently 1)
- **Pc**: Command type (F=fire-and-forget, Q=query, R=response, E=error)

### Command ID Ranges

| Range | Purpose | Examples |
|-------|---------|----------|
| 1001-1010 | Engine Control | IgniteMainThrottle (1001), ShutdownMainEngine (1002) |
| 1011-1020 | Navigation | SetHeading (1011), SetThrottle (1012) |
| 1021-1030 | Systems | ToggleLights (1021), ActivateRCS (1022) |
| 2001-2010 | Engine Queries | GetThrottleStatus (2001), GetEngineTemp (2002) |
| 2011-2020 | Navigation Queries | GetPosition (2011), GetVelocity (2012) |
| 2021-2030 | System Queries | GetFuelLevel (2021), GetBatteryLevel (2022) |

## Examples

### Basic unix shell echo

```bash
echo -ne '\e[>1001;1F'
```

### Fire-and-Forget Commands

```bash
# Ignite main engine
bun run rpc-client.ts ignite
# Output: 🚀 Ignite main throttle command sent

# Set throttle to 50%
bun run rpc-client.ts throttle 50
# Output: ⚡ Set throttle to 50% command sent

# Toggle lights on (custom command)
bun run rpc-client.ts send --command-id 1021 --parameters 1
# Output: 🔧 Fire-and-forget command 1021 sent
```

### Query Commands

```bash
# Query throttle status
bun run rpc-client.ts query-throttle
# Output: ❓ Throttle status query sent, waiting for response...
#         ✅ Response from command 2001:
#           Throttle: enabled, 75%

# Query fuel level
bun run rpc-client.ts query-fuel
# Output: ❓ Fuel level query sent, waiting for response...
#         ✅ Response from command 2021:
#           Fuel level: 85%
```

### Verbose Mode

```bash
bun run rpc-client.ts ignite --verbose
# Output: [INFO] Executing ignite main throttle command (ID 1001)
#         [DEBUG] Sending sequence: "\u001b[>1001;1F"
#         [DEBUG] Hex bytes: 0x1b 0x5b 0x3e 0x31 0x30 0x30 0x31 0x3b 0x31 0x46
#         🚀 Ignite main throttle command sent
```

## Integration

This client application is designed to work with terminal emulators that support the Terminal Sequence RPC system. When run inside such a terminal:

1. The client sends properly formatted escape sequences to stdout
2. The terminal emulator detects and processes these RPC sequences
3. The terminal communicates with the game to execute the requested actions
4. For query commands, the terminal sends responses back through the PTY

## Development

### Running Tests

```bash
bun test
```

### Building

The application runs directly with Bun, no build step required.

### Demo

```bash
# Run the RPC command builder demonstration
bun run rpc-demo.ts
```

## Files

- `rpc-client.ts` - Main client application
- `rpc-command-builder.ts` - RPC sequence formatting utilities
- `terminal-interface.ts` - Terminal I/O and argument parsing
- `rpc-demo.ts` - Demonstration of RPC command building
- `rpc-command-builder.test.ts` - Unit tests for command builder

## Error Handling

The client provides clear error messages for common issues:

- Invalid command IDs (must be 1000-9999)
- Invalid parameter ranges (throttle 0-100%)
- Missing required parameters
- Command/parameter type mismatches
- Response timeouts for query commands

## Requirements Validation

This client application fulfills the following requirements:

- **7.1**: Demonstrates sending fire-and-forget commands using proper escape sequence format
- **7.2**: Outputs `ESC [ > 1001 ; 1 ; F` when sending ignite engine command
- **7.3**: Includes examples of both fire-and-forget and query commands
- **7.4**: Integrates seamlessly with terminal emulator's RPC system when run in terminal
- **7.5**: Provides clear usage instructions and command-line interface