import { createDefaultSgrState, type SgrState } from "../SgrStyleManager";

export interface DebugLogger {
  isLevelEnabled(level: "debug"): boolean;
  debug(message: string): void;
}

/**
 * Handle enhanced SGR sequences with > prefix (e.g., CSI > 4 ; 2 m)
 * These are typically used for advanced terminal features.
 */
export function handleEnhancedSgrMode(currentSgrState: SgrState, log: DebugLogger, params: number[]): void {
  if (params.length >= 2 && params[0] === 4) {
    // Enhanced underline mode: CSI > 4 ; n m
    const underlineType = params[1];

    if (underlineType >= 0 && underlineType <= 5) {
      // Valid enhanced underline mode - update SGR state
      switch (underlineType) {
        case 0:
          // No underline
          currentSgrState.underline = false;
          currentSgrState.underlineStyle = null;
          break;
        case 1:
          // Single underline
          currentSgrState.underline = true;
          currentSgrState.underlineStyle = "single";
          break;
        case 2:
          // Double underline
          currentSgrState.underline = true;
          currentSgrState.underlineStyle = "double";
          break;
        case 3:
          // Curly underline
          currentSgrState.underline = true;
          currentSgrState.underlineStyle = "curly";
          break;
        case 4:
          // Dotted underline
          currentSgrState.underline = true;
          currentSgrState.underlineStyle = "dotted";
          break;
        case 5:
          // Dashed underline
          currentSgrState.underline = true;
          currentSgrState.underlineStyle = "dashed";
          break;
      }

      if (log.isLevelEnabled("debug")) {
        log.debug(`Enhanced underline mode set: type=${underlineType}, style=${currentSgrState.underlineStyle}`);
      }
      return;
    }

    // Invalid underline type - gracefully ignore
    if (log.isLevelEnabled("debug")) {
      log.debug(`Invalid enhanced underline type: ${underlineType}, ignoring`);
    }
    return;
  }

  // Other enhanced modes not yet supported - gracefully ignore
  if (log.isLevelEnabled("debug")) {
    log.debug(`Enhanced SGR mode received: ${JSON.stringify({ params })}, not implemented`);
  }
}

/**
 * Handle private SGR sequences with ? prefix (e.g., CSI ? 4 m)
 * These are typically used for private/experimental features.
 */
export function handlePrivateSgrMode(currentSgrState: SgrState, log: DebugLogger, params: number[]): void {
  // Handle specific private SGR modes
  if (params.length === 1 && params[0] === 4) {
    // Private underline mode (?4m) - enable underline
    currentSgrState.underline = true;
    currentSgrState.underlineStyle = "single";

    if (log.isLevelEnabled("debug")) {
      log.debug("Private underline mode (?4m) enabled");
    }
    return;
  }

  // For other private modes, gracefully ignore
  if (log.isLevelEnabled("debug")) {
    log.debug(`Private SGR mode received: ${JSON.stringify({ params })}, not implemented`);
  }
}

/**
 * Handle SGR sequences with intermediate characters (e.g., CSI 0 % m)
 * These are used for special SGR attribute resets or modifications.
 */
export function handleSgrWithIntermediate(
  log: DebugLogger,
  params: number[],
  intermediate: string,
): SgrState | null {
  // Handle specific intermediate character sequences
  if (intermediate === "%") {
    // CSI 0 % m - Reset specific attributes
    if (params.length === 1 && params[0] === 0) {
      // Reset all SGR attributes (similar to SGR 0)
      log.debug("SGR reset with % intermediate");
      return createDefaultSgrState();
    }
  }

  // For other intermediate sequences, gracefully ignore
  if (log.isLevelEnabled("debug")) {
    log.debug(`SGR with intermediate received: ${JSON.stringify({ params, intermediate })}`);
  }

  return null;
}

/**
 * Handle unknown vi sequences (e.g., CSI 11M)
 * These sequences appear in vi usage but are not part of standard terminal specifications.
 * We gracefully acknowledge them without implementing specific behavior.
 */
export function handleUnknownViSequence(log: DebugLogger, sequenceNumber: number): void {
  // Log the sequence for debugging purposes
  if (log.isLevelEnabled("debug")) {
    log.debug(`Unknown vi sequence received: CSI ${sequenceNumber}M`);
  }

  // Gracefully acknowledge - no specific action needed
  // The sequence is parsed and handled without causing errors
}
