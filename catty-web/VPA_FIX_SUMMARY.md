# VPA (Vertical Position Absolute) Implementation

## Problem

The htop alignment issue was caused by the terminal emulator not supporting the **VPA (Vertical Position Absolute)** ANSI escape sequence `CSI Ps d` (e.g., `ESC[11d`).

### Root Cause Analysis

From the trace log at line 647:
```
647  10,1   |  CSI  csi.unknown <ESC>[11d
```

The sequence `ESC[11d` was being treated as `csi.unknown` instead of being recognized as VPA. This caused htop's header row to be written at row 10 instead of row 11, resulting in the CPU bars being overwritten.

### Expected Behavior

- `ESC[11d` should move the cursor to row 11 while preserving the current column
- This is the VPA (Vertical Position Absolute) command, a standard VT100/ANSI sequence
- It's the vertical counterpart to HPA (Horizontal Position Absolute) `ESC[Ps G`

## Solution

Implemented full VPA support across three layers:

### 1. Type Definition (`TerminalEmulationTypes.ts`)

Added new interface for VPA:
```typescript
export interface CsiVerticalPositionAbsolute extends CsiBase {
  _type: "csi.verticalPositionAbsolute";
  row: number;
}
```

### 2. Parser (`ParseCsi.ts`)

Added parsing for the `d` final byte:
```typescript
if (final === "d") {
  const msg: CsiVerticalPositionAbsolute = { 
    _type: "csi.verticalPositionAbsolute", 
    raw, 
    row: getParam(params, 0, 1) 
  };
  return msg;
}
```

### 3. Handler (`StatefulTerminal.ts`)

Added handler to move cursor vertically:
```typescript
case "csi.verticalPositionAbsolute":
  this.cursorY = Math.max(0, Math.min(this.rows - 1, msg.row - 1));
  this.wrapPending = false;
  return;
```

## Testing

### Parser Tests (`Parser.test.ts`)

Added two test cases:
- `ESC[d` - VPA with default parameter (row 1)
- `ESC[11d` - VPA with explicit row parameter

### Integration Tests (`CursorPositioning.test.ts`)

Created comprehensive test suite covering:
- Basic VPA functionality
- Default parameter handling
- Bounds clamping
- Column preservation
- Interaction with HPA (Horizontal Position Absolute)
- Combined positioning scenarios

All tests pass ✓

## Impact

This fix resolves the htop alignment issue and adds proper support for a standard ANSI escape sequence that may be used by other terminal applications. The implementation follows the existing patterns for cursor positioning commands (HPA, CUP, etc.) and maintains consistency with the codebase architecture.

## Files Modified

1. `packages/terminal-emulation/src/terminal/TerminalEmulationTypes.ts` - Added type
2. `packages/terminal-emulation/src/terminal/ParseCsi.ts` - Added parser
3. `app/src/ts/terminal/StatefulTerminal.ts` - Added handler
4. `packages/terminal-emulation/src/__tests__/Parser.test.ts` - Added parser tests
5. `app/src/ts/terminal/__tests__/CursorPositioning.test.ts` - Added integration tests (new file)
