# Implementation Plan: caTTY KSA Terminal Emulator

## Overview

C# terminal emulator for KSA game engine via incremental MVP development. Each major task = working program milestone.

**PLATFORM**: Windows ConPTY exclusively (Win10 1809+). No cross-platform/fallback.
No fallback to stdout/stderr process redirection; follow Microsoft ConPTY docs.

**COMPLEXITY BREAKDOWN** (from TypeScript analysis):
- **High**: SGR parsing (556L), CSI parsing (437L), StatefulTerminal (936L), Buffer ops (455L), Parser state (541L), Alternate screen
- **Medium**: OSC parsing (155L), Terminal modes, Scrollback (56L), Process mgmt  
- **Low**: Basic structures, Control chars, ImGui integration

**DRIVERS**: SGR color formats, CSI state/params, UTF-8 stream decode/recovery, dual-buffer semantics.

**KEY IMPROVEMENTS**: ImGui playground (1.4-1.7), granular StatefulTerminal (1.8), UTF-8 separation (2.3-2.4), buffer ops split (4.11-4.13), line/char ops property test (4.13), earlier UTF-8 handling

## Tasks

**IMPORTANT**: After completing each subtask, you MUST provide a properly formatted git commit message in your response as a summary. Use the format: `[task-id] type: description` (e.g., `[1.1] feat: set up solution structure`), followed by a blank line, then "## Changes Made" with bullet points of specific changes.

**CONSOLE OUTPUT REQUIREMENTS**: All unit tests and property-based tests MUST strive to have no stdout/stderr output under normal conditions to reduce verbosity of console output. Tests should only produce output when:
- A test fails and diagnostic information is needed
- Explicit debugging is enabled via environment variables or test flags
- Critical errors occur that require immediate attention

This requirement applies to all test tasks throughout the implementation plan.



- [ ] 5. Add alternate screen buffer and advanced terminal modes
- [x] 5.1 Create alternate screen buffer infrastructure
  - NOTE: some of this was implemented by other work streams.  Analyze what is in place and fix, augment or replace it with a proper implementation as necessary based on the spec and task design.
  - Create AlternateScreenManager class
  - Implement separate primary and alternate screen buffers
  - Add buffer switching methods (activate/deactivate)
  - Preserve cursor and attributes independently per buffer
  - **CRITICAL CODE ORGANIZATION**: Create dedicated AlternateScreenManager class
    - Extract alternate screen logic into caTTY.Core/Managers/AlternateScreenManager.cs
    - Create IAlternateScreenManager interface for testability
    - AlternateScreenManager should handle all buffer switching and state isolation
    - AlternateScreenManager should not exceed 200 lines (excluding comments)
    - TerminalEmulator should delegate alternate screen operations to AlternateScreenManager instance
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreen.ts to ensure C# implementation provides identical alternate screen buffer management and state isolation
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreen.ts
  - _Requirements: 15.1, 15.2, 15.4_

- [x] 5.2 Implement alternate screen isolation
  - NOTE: some of this was implemented by other work streams.  Analyze what is in place and fix, augment or replace it with a proper implementation as necessary based on the spec and task design.
  - Ensure alternate screen doesn't add to scrollback
  - Clear alternate buffer on activation
  - Handle buffer switching with proper state preservation
  - Maintain separate cursor positions per buffer
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreen.ts isolation behavior to ensure C# implementation provides identical scrollback isolation and state preservation
  - _Requirements: 15.3, 15.5_

- [x] 5.3 Add alternate screen control sequences
  - NOTE: some of this was implemented by other work streams.  Analyze what is in place and fix, augment or replace it with a proper implementation as necessary based on the spec and task design.
  - Implement DEC private mode sequences for alternate screen
  - Add alternate screen activation/deactivation sequences
  - Handle mode switching in CSI parser
  - Test buffer switching with state preservation
  - Ensure correct semantics for 47/1047/1049
    - 1047/1049 preserve/restore cursor as specified
    - 1049 clears alternate screen on entry
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreenOps.ts and handlers/csi.ts to ensure C# implementation provides identical alternate screen control sequence behavior
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreenOps.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts
  - _Requirements: 15.1, 15.2, 15.5_

- [x] 5.4 Write property test for alternate screen buffer switching
  - **Property 29: Alternate screen buffer switching**
  - **Validates: Requirements 15.1, 15.2, 15.4**

- [x] 5.5 Write property test for alternate screen scrollback isolation
  - **Property 30: Alternate screen scrollback isolation**
  - **Validates: Requirements 15.3**

- [x] 5.6 Implement terminal mode management
  - NOTE: some of this was implemented by other work streams.  Analyze what is in place and fix, augment or replace it with a proper implementation as necessary based on the spec and task design.
  - Create terminal mode state tracking
  - Add auto-wrap mode with line wrapping behavior
  - Implement cursor visibility mode tracking
  - Add application cursor keys mode
  - Add origin mode (DECOM) state tracking
  - Add UTF-8 mode (DECSET/DECRST 2027) state tracking
  - Add cursor style tracking (DECSCUSR)
  - Add save/restore private modes (CSI ? s / CSI ? r) state tracking
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts and cursor.ts mode management to ensure C# implementation provides identical terminal mode behavior and state tracking
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/cursor.ts
  - _Requirements: 20.1, 20.2, 20.3, 20.4_

- [x] 5.7 Add cursor wrapping and line overflow handling
  - NOTE: some of this was implemented by other work streams.  Analyze what is in place and fix, augment or replace it with a proper implementation as necessary based on the spec and task design.
  - Implement auto-wrap behavior when cursor reaches right edge
  - Add line overflow handling based on auto-wrap mode
  - Update character writing to respect wrapping settings
  - Handle wide character wrapping correctly
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts and cursor.ts wrapping behavior to ensure C# implementation provides identical cursor wrapping and line overflow handling
  - _Requirements: 8.3, 9.5, 20.1_

- [x] 5.8 Write property test for cursor wrapping behavior
  - **Property 12: Cursor wrapping behavior**
  - **Validates: Requirements 8.3**

- [x] 5.9 Add bracketed paste mode support
  - Implement bracketed paste mode state tracking
  - Add paste sequence wrapping for bracketed paste
  - Handle mode switching sequences
  - Prepare for future paste integration
  - Define the exact DECSET/DECRST sequences
    - CSI ? 2004 h enable, CSI ? 2004 l disable
    - When enabled, wrap paste payload with ESC[200~ and ESC[201~
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts bracketed paste mode handling to ensure C# implementation provides identical paste mode behavior
  - _Requirements: 20.5_

- [x] 5.10 Write property test for cursor visibility tracking
  - **Property 14: Cursor visibility tracking**
  - **Validates: Requirements 8.5**

- [ ] 5.11 Test and validate alternate screen and modes
  - **USER VALIDATION REQUIRED**: Test full-screen apps (less)
  - Verify alternate screen works correctly
  - Test terminal mode switching
  - Validate cursor wrapping and visibility
  - Document any mode handling issues

- [ ] 5.12 Checkpoint - Alternate screen and terminal modes working
  - Full-screen applications work correctly
  - Terminal modes function properly



## Notes

- All tasks are required for comprehensive implementation from the start
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation at working program milestones
- Property tests validate universal correctness properties using FsCheck.NUnit
- Unit tests validate specific examples and edge cases
- The implementation follows the TypeScript version as a reference for behavior compatibility
- Each major task number (1-8) results in a working program with incrementally more features
- Subtasks are kept small to optimize AI/LLM context window usage
- Complex areas identified from TypeScript analysis have been broken down into granular subtasks

## CRITICAL CODE ORGANIZATION REQUIREMENTS

**MANDATORY REFACTORING**: The current Parser class (600+ lines) and future large classes MUST be decomposed to maintain code quality:

### Parser Decomposition (Tasks 2.18)
- **Current Issue**: Parser.cs has grown to over 600 lines and handles multiple responsibilities
- **Required Action**: Break into specialized parsers (CsiParser, SgrParser, OscParser, EscParser, DcsParser, Utf8Decoder)
- **Benefit**: Improved testability, maintainability, and adherence to single responsibility principle

### State Management Decomposition (Task 2.19)
- **Proactive Measure**: Prevent TerminalEmulator and related state classes from becoming monolithic
- **Required Action**: Create focused managers (ScreenBufferManager, CursorManager, ScrollbackManager, etc.)
- **Benefit**: Clear separation of concerns and easier unit testing

### Ongoing Vigilance
- **Monitor Class Sizes**: Any class exceeding 400 lines requires immediate refactoring
- **Enforce Interfaces**: All managers and parsers must implement focused interfaces
- **Maintain Tests**: Each decomposed component must have comprehensive unit tests

This refactoring is essential for long-term maintainability and follows industry best practices for complex terminal emulation software.

## CRITICAL BUILD AND TEST REQUIREMENTS

**ZERO TOLERANCE POLICY**: The implementation MUST maintain the highest quality standards throughout development:

### Build Quality Standards
- **ENTIRE SOLUTION MUST COMPILE WITH ZERO WARNINGS AND ZERO ERRORS**
- **ENTIRE TEST SUITE MUST PASS WITH ZERO FAILURES**
- All projects configured with `TreatWarningsAsErrors=true`
- All projects configured with `Nullable=enable`
- All public APIs must have XML documentation
- No obsolete API usage allowed
- No unreachable code allowed
- No unused variables allowed

### Test Quality Standards
- All unit tests must pass consistently
- All property-based tests must pass across multiple runs (minimum 100 iterations)
- All integration tests must pass reliably
- All TypeScript compatibility tests must pass
- All performance tests must meet established benchmarks
- Test coverage must meet minimum thresholds for all components

### Continuous Validation
- Every task completion must result in a clean build (zero warnings/errors)
- Every task completion must result in passing tests (zero failures)
- Any warnings or test failures must be addressed immediately before proceeding
- Build and test validation must be performed after each significant change