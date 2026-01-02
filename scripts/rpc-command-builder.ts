/**
 * RPC Command Builder Module
 * 
 * Provides functions to format ESC [ > Pn ; Pv ; Pc sequences for the Terminal Sequence RPC system.
 * Validates command IDs and parameter ranges according to the RPC protocol specification.
 * 
 * Requirements: 7.1, 7.2, 7.3
 */

// RPC Protocol Constants
const ESC_PREFIX = '\x1B[>';
const PROTOCOL_VERSION = 1;

// Command Type Final Characters
export const CommandType = {
  FIRE_AND_FORGET: 'F',
  QUERY: 'Q', 
  RESPONSE: 'R',
  ERROR: 'E'
} as const;

export type CommandTypeValue = typeof CommandType[keyof typeof CommandType];

// Command ID Ranges
export const CommandRange = {
  FIRE_AND_FORGET_MIN: 1000,
  FIRE_AND_FORGET_MAX: 1999,
  QUERY_MIN: 2000,
  QUERY_MAX: 2999,
  RESERVED_MIN: 3000,
  RESERVED_MAX: 8999,
  SYSTEM_MIN: 9000,
  SYSTEM_MAX: 9999
} as const;

// Predefined Command IDs
export const PredefinedCommands = {
  // Engine Control (1001-1010)
  IGNITE_MAIN_THROTTLE: 1001,
  SHUTDOWN_MAIN_ENGINE: 1002,
  
  // Navigation (1011-1020)
  SET_HEADING: 1011,
  SET_THROTTLE: 1012,
  
  // Systems (1021-1030)
  TOGGLE_LIGHTS: 1021,
  ACTIVATE_RCS: 1022,
  
  // Engine Queries (2001-2010)
  GET_THROTTLE_STATUS: 2001,
  GET_ENGINE_TEMP: 2002,
  
  // Navigation Queries (2011-2020)
  GET_POSITION: 2011,
  GET_VELOCITY: 2012,
  
  // System Queries (2021-2030)
  GET_FUEL_LEVEL: 2021,
  GET_BATTERY_LEVEL: 2022
} as const;

/**
 * Validation result for RPC command parameters
 */
export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

/**
 * RPC command parameters
 */
export interface RpcCommandParams {
  commandId: number;
  version?: number;
  commandType: CommandTypeValue;
  additionalParams?: number[] | undefined;
}

/**
 * Validates a command ID against the RPC protocol specification
 * @param commandId The command ID to validate (Pn parameter)
 * @returns Validation result with error message if invalid
 */
export function validateCommandId(commandId: number): ValidationResult {
  // Check if command ID is a valid integer
  if (!Number.isInteger(commandId)) {
    return { isValid: false, error: 'Command ID must be an integer' };
  }
  
  // Check if command ID is in valid range (1000-9999)
  if (commandId < 1000 || commandId > 9999) {
    return { isValid: false, error: 'Command ID must be in range 1000-9999' };
  }
  
  return { isValid: true };
}

/**
 * Validates command type final character
 * @param commandType The command type final character (Pc parameter)
 * @returns Validation result with error message if invalid
 */
export function validateCommandType(commandType: string): ValidationResult {
  const validTypes = Object.values(CommandType);
  
  if (!validTypes.includes(commandType as CommandTypeValue)) {
    return { 
      isValid: false, 
      error: `Command type must be one of: ${validTypes.join(', ')}` 
    };
  }
  
  // Ensure final character is in range 0x40-0x7E (private use area compliance)
  const charCode = commandType.charCodeAt(0);
  if (charCode < 0x40 || charCode > 0x7E) {
    return { 
      isValid: false, 
      error: 'Command type final character must be in range 0x40-0x7E' 
    };
  }
  
  return { isValid: true };
}

/**
 * Validates that command ID matches the expected command type
 * @param commandId The command ID (Pn parameter)
 * @param commandType The command type (Pc parameter)
 * @returns Validation result with error message if mismatch
 */
export function validateCommandIdTypeMatch(commandId: number, commandType: CommandTypeValue): ValidationResult {
  const isFireAndForget = commandId >= CommandRange.FIRE_AND_FORGET_MIN && 
                         commandId <= CommandRange.FIRE_AND_FORGET_MAX;
  const isQuery = commandId >= CommandRange.QUERY_MIN && 
                 commandId <= CommandRange.QUERY_MAX;
  
  if (isFireAndForget && commandType !== CommandType.FIRE_AND_FORGET) {
    return { 
      isValid: false, 
      error: `Fire-and-forget command ID ${commandId} must use command type 'F'` 
    };
  }
  
  if (isQuery && commandType !== CommandType.QUERY) {
    return { 
      isValid: false, 
      error: `Query command ID ${commandId} must use command type 'Q'` 
    };
  }
  
  return { isValid: true };
}

/**
 * Validates protocol version parameter
 * @param version The protocol version (Pv parameter)
 * @returns Validation result with error message if invalid
 */
export function validateProtocolVersion(version: number): ValidationResult {
  if (!Number.isInteger(version) || version < 1) {
    return { isValid: false, error: 'Protocol version must be a positive integer' };
  }
  
  if (version !== PROTOCOL_VERSION) {
    return { 
      isValid: false, 
      error: `Unsupported protocol version ${version}. Current version is ${PROTOCOL_VERSION}` 
    };
  }
  
  return { isValid: true };
}

/**
 * Validates additional parameters for RPC commands
 * @param additionalParams Array of additional numeric parameters
 * @returns Validation result with error message if invalid
 */
export function validateAdditionalParams(additionalParams?: number[]): ValidationResult {
  if (!additionalParams) {
    return { isValid: true };
  }
  
  // Check that all additional parameters are valid integers
  for (let i = 0; i < additionalParams.length; i++) {
    const param = additionalParams[i];
    if (param === undefined || !Number.isInteger(param) || param < 0) {
      return { 
        isValid: false, 
        error: `Additional parameter at index ${i} must be a non-negative integer` 
      };
    }
  }
  
  return { isValid: true };
}

/**
 * Formats an RPC command sequence according to ESC [ > Pn ; Pv ; Pc format
 * @param params RPC command parameters
 * @returns Formatted escape sequence string
 * @throws Error if parameters are invalid
 */
export function formatRpcSequence(params: RpcCommandParams): string {
  const { commandId, version = PROTOCOL_VERSION, commandType, additionalParams } = params;
  
  // Validate all parameters
  const commandIdValidation = validateCommandId(commandId);
  if (!commandIdValidation.isValid) {
    throw new Error(`Invalid command ID: ${commandIdValidation.error}`);
  }
  
  const versionValidation = validateProtocolVersion(version);
  if (!versionValidation.isValid) {
    throw new Error(`Invalid protocol version: ${versionValidation.error}`);
  }
  
  const commandTypeValidation = validateCommandType(commandType);
  if (!commandTypeValidation.isValid) {
    throw new Error(`Invalid command type: ${commandTypeValidation.error}`);
  }
  
  const typeMatchValidation = validateCommandIdTypeMatch(commandId, commandType);
  if (!typeMatchValidation.isValid) {
    throw new Error(`Command ID/type mismatch: ${typeMatchValidation.error}`);
  }
  
  const additionalParamsValidation = validateAdditionalParams(additionalParams);
  if (!additionalParamsValidation.isValid) {
    throw new Error(`Invalid additional parameters: ${additionalParamsValidation.error}`);
  }
  
  // Build the sequence: ESC [ > Pn ; Pv ; [additional params ;] Pc
  let sequence = `${ESC_PREFIX}${commandId};${version}`;
  
  // Add additional parameters if provided
  if (additionalParams && additionalParams.length > 0) {
    sequence += `;${additionalParams.join(';')}`;
  }
  
  // Add final character
  sequence += commandType;
  
  return sequence;
}

/**
 * Creates a fire-and-forget command sequence
 * @param commandId Command ID in range 1000-1999
 * @param additionalParams Optional additional parameters
 * @returns Formatted fire-and-forget command sequence
 */
export function createFireAndForgetCommand(commandId: number, additionalParams?: number[] | undefined): string {
  return formatRpcSequence({
    commandId,
    commandType: CommandType.FIRE_AND_FORGET,
    additionalParams: additionalParams || undefined
  });
}

/**
 * Creates a query command sequence
 * @param commandId Command ID in range 2000-2999
 * @param additionalParams Optional additional parameters
 * @returns Formatted query command sequence
 */
export function createQueryCommand(commandId: number, additionalParams?: number[] | undefined): string {
  return formatRpcSequence({
    commandId,
    commandType: CommandType.QUERY,
    additionalParams: additionalParams || undefined
  });
}

/**
 * Creates an ignite main throttle command (ID 1001)
 * @returns Formatted ignite command sequence
 */
export function createIgniteMainThrottleCommand(): string {
  return createFireAndForgetCommand(PredefinedCommands.IGNITE_MAIN_THROTTLE);
}

/**
 * Creates a shutdown main engine command (ID 1002)
 * @returns Formatted shutdown command sequence
 */
export function createShutdownMainEngineCommand(): string {
  return createFireAndForgetCommand(PredefinedCommands.SHUTDOWN_MAIN_ENGINE);
}

/**
 * Creates a set throttle command (ID 1012)
 * @param throttlePercent Throttle percentage (0-100)
 * @returns Formatted set throttle command sequence
 */
export function createSetThrottleCommand(throttlePercent: number): string {
  if (!Number.isInteger(throttlePercent) || throttlePercent < 0 || throttlePercent > 100) {
    throw new Error('Throttle percentage must be an integer between 0 and 100');
  }
  
  return createFireAndForgetCommand(PredefinedCommands.SET_THROTTLE, [throttlePercent]);
}

/**
 * Creates a get throttle status query (ID 2001)
 * @returns Formatted throttle status query sequence
 */
export function createGetThrottleStatusQuery(): string {
  return createQueryCommand(PredefinedCommands.GET_THROTTLE_STATUS);
}

/**
 * Creates a get fuel level query (ID 2021)
 * @returns Formatted fuel level query sequence
 */
export function createGetFuelLevelQuery(): string {
  return createQueryCommand(PredefinedCommands.GET_FUEL_LEVEL);
}

/**
 * Checks if a command ID is a fire-and-forget command
 * @param commandId The command ID to check
 * @returns True if the command ID is in the fire-and-forget range
 */
export function isFireAndForgetCommand(commandId: number): boolean {
  return commandId >= CommandRange.FIRE_AND_FORGET_MIN && 
         commandId <= CommandRange.FIRE_AND_FORGET_MAX;
}

/**
 * Checks if a command ID is a query command
 * @param commandId The command ID to check
 * @returns True if the command ID is in the query range
 */
export function isQueryCommand(commandId: number): boolean {
  return commandId >= CommandRange.QUERY_MIN && 
         commandId <= CommandRange.QUERY_MAX;
}

/**
 * Gets the expected command type for a given command ID
 * @param commandId The command ID
 * @returns The expected command type, or null if command ID is invalid
 */
export function getExpectedCommandType(commandId: number): CommandTypeValue | null {
  if (isFireAndForgetCommand(commandId)) {
    return CommandType.FIRE_AND_FORGET;
  }
  
  if (isQueryCommand(commandId)) {
    return CommandType.QUERY;
  }
  
  return null;
}