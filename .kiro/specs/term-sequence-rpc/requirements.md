# Requirements Document

## Introduction

The Terminal Sequence RPC system enables shell programs to communicate with the game through custom terminal escape sequences. This allows CLI/TUI programs running in the terminal emulator to invoke game actions and query game state without polluting the core VT100/xterm-compliant terminal emulator.

## Glossary

- **RPC_System**: The terminal sequence-based remote procedure call mechanism
- **Private_Sequence**: ESC [ > format escape sequences using private use area parameters
- **Command_ID**: Pn parameter that identifies specific RPC commands (1000-1999 for fire-and-forget, 2000-2999 for queries)
- **Version_Parameter**: Pv parameter indicating RPC protocol version (currently 1)
- **Final_Character**: Pc parameter indicating command type (F=fire-and-forget, Q=query, R=response, E=error)
- **Game_Host**: The game-side terminal emulator that processes PTY and custom sequences
- **Shell_Client**: CLI/TUI programs that send custom sequences to invoke game actions
- **Core_Emulator**: The VT100/xterm-compliant terminal emulation engine
- **RPC_Handler**: Separate classes that process private use area sequences without affecting core emulation
- **Fire_And_Forget**: RPC calls that send commands without expecting responses
- **Client_Application**: JavaScript example application that demonstrates sending RPC commands to the terminal

## Requirements

### Requirement 1: Private Use Area Escape Sequence Protocol

**User Story:** As a shell program developer, I want to send private use area escape sequences to invoke game actions, so that I can control the game from CLI/TUI applications without conflicting with standard terminal sequences.

#### Acceptance Criteria

1. WHEN a shell client sends a private use sequence, THE RPC_System SHALL parse the ESC [ > prefix and extract command parameters
2. THE Custom_Sequence SHALL follow the format: ESC [ > Pn ; Pv ; Pc where Pn is command ID, Pv is version, and Pc is the final character
3. WHEN the sequence format is invalid or uses non-private parameters, THE RPC_System SHALL ignore the sequence and continue normal terminal processing
4. WHEN a valid private sequence is detected, THE Core_Emulator SHALL delegate processing to the RPC_Handler without affecting VT100/xterm compliance
5. THE RPC_System SHALL use unique parameter combinations (Pn values) to identify different game commands
6. THE final character (Pc) SHALL be in the range 0x40-0x7E to comply with private use area standards

### Requirement 2: Fire-and-Forget Command Execution

**User Story:** As a shell program, I want to send fire-and-forget commands to the game, so that I can trigger actions like igniting engines without waiting for responses.

#### Acceptance Criteria

1. WHEN a fire-and-forget sequence ESC [ > Pn ; 1 ; C is received, THE RPC_System SHALL invoke the corresponding game action immediately
2. WHEN the game action completes, THE RPC_System SHALL NOT send any response to the shell client
3. WHEN Pn is 1001 (IgniteMainThrottle command), THE Game_Host SHALL call rocket.SetEnum(VehicleEngine.MainIgnite)
4. WHEN Pn is 1002 (ShutdownMainEngine command), THE Game_Host SHALL call rocket.SetEnum(VehicleEngine.MainShutdown)
5. WHEN an unknown Pn parameter is received, THE RPC_System SHALL log the error and continue processing
6. THE final character C SHALL be 'F' (0x46) to indicate fire-and-forget commands

### Requirement 3: Query-Response Communication

**User Story:** As a shell program, I want to query game state and receive responses, so that I can display current throttle status or other game information.

#### Acceptance Criteria

1. WHEN a query sequence ESC [ > Pn ; 1 ; Q is received, THE RPC_System SHALL process the query and send a response back to the shell client
2. WHEN querying throttle status (Pn = 2001), THE RPC_System SHALL return the current engine state as a structured response
3. WHEN the response is ready, THE Game_Host SHALL send the data back through the PTY using ESC [ > Pn ; 1 ; R format
4. WHEN a query times out, THE RPC_System SHALL send an error response after a reasonable timeout period using ESC [ > 9999 ; 1 ; E
5. THE response data SHALL be encoded in additional parameters following the standard Pn ; Pv ; Pc format
6. THE final character Q SHALL indicate query commands, R SHALL indicate responses, E SHALL indicate errors

### Requirement 4: Clean Architecture Separation

**User Story:** As a terminal emulator maintainer, I want RPC functionality separated from core emulation, so that the VT100/xterm compliance is not compromised.

#### Acceptance Criteria

1. WHEN private use sequences are detected, THE Core_Emulator SHALL delegate to RPC_Handler classes without modification to core parsing logic
2. WHEN standard terminal sequences are processed, THE Core_Emulator SHALL function identically whether RPC is enabled or disabled
3. THE RPC_Handler SHALL be implemented as separate classes that do not inherit from or modify core emulator components
4. WHEN RPC functionality is disabled, THE Core_Emulator SHALL ignore private use sequences and process only standard sequences
5. THE RPC_System SHALL be optional and removable without affecting core terminal functionality

### Requirement 5: Game Integration Interface

**User Story:** As a game developer, I want a clean interface to register RPC commands, so that I can easily add new game actions without modifying RPC parsing logic.

#### Acceptance Criteria

1. THE RPC_System SHALL provide a registration interface for adding new commands with Command_ID ranges
2. WHEN registering a command, THE Game_Host SHALL specify the Command_ID (Pn), parameter types, and handler function
3. WHEN a registered command is invoked, THE RPC_System SHALL call the appropriate handler with parsed parameters
4. THE registration interface SHALL support both fire-and-forget (1000-1999) and query-response (2000-2999) command ranges
5. WHEN the game starts, THE RPC_System SHALL initialize with a default set of vehicle control commands

### Requirement 6: Error Handling and Logging

**User Story:** As a developer, I want comprehensive error handling and logging, so that I can debug RPC communication issues.

#### Acceptance Criteria

1. WHEN sequence parsing fails, THE RPC_System SHALL log the error with the invalid sequence content
2. WHEN a command handler throws an exception, THE RPC_System SHALL catch it and log the error without crashing the terminal
3. WHEN an unknown command is received, THE RPC_System SHALL log a warning with the command name
4. WHEN query responses time out, THE RPC_System SHALL log the timeout and send an error response
5. THE logging SHALL be configurable and separate from core terminal tracing

### Requirement 7: Client Application Example

**User Story:** As a developer, I want a simple JavaScript client application example, so that I can understand how to send RPC commands from shell programs.

#### Acceptance Criteria

1. THE Client_Application SHALL demonstrate sending fire-and-forget commands using proper escape sequence format
2. WHEN the client sends an ignite engine command, THE application SHALL output ESC [ > 1001 ; 1 ; F to stdout
3. THE Client_Application SHALL include examples of both fire-and-forget and query commands
4. WHEN running in a terminal, THE Client_Application SHALL integrate seamlessly with the terminal emulator's RPC system
5. THE Client_Application SHALL provide clear usage instructions and command-line interface

### Requirement 8: Security and Validation

**User Story:** As a system administrator, I want RPC commands to be validated and secured, so that malicious shell programs cannot compromise the game.

#### Acceptance Criteria

1. WHEN processing RPC commands, THE RPC_System SHALL validate all parameters before invoking game actions
2. WHEN a command would cause unsafe game state changes, THE RPC_System SHALL reject the command and log a security warning
3. THE RPC_System SHALL implement rate limiting to prevent command flooding
4. WHEN rate limits are exceeded, THE RPC_System SHALL ignore subsequent commands for a cooldown period
5. THE RPC_System SHALL provide configuration options for enabling/disabling specific command categories