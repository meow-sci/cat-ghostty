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



- [ ] 6. Add OSC sequences and advanced features
- [x] 6.1 Create OSC sequence parser infrastructure
  - Create OscParser class for OSC sequences
  - Add OSC sequence detection (ESC ] command ST)
  - Parse OSC command numbers and parameters
  - Handle string termination with ST or BEL
  - Define robustness rules
    - Ignore/skip malformed OSC without breaking the stream
    - Cap maximum OSC payload length to prevent memory blowups
  - **CRITICAL CODE ORGANIZATION**: Create dedicated OscParser class
    - Extract OSC parsing logic into caTTY.Core/Parsing/OscParser.cs
    - Create IOscParser interface for testability
    - OscParser should handle all OSC sequence parsing and command extraction
    - OscParser should not exceed 250 lines (excluding comments)
    - Main Parser should delegate OSC parsing to OscParser instance
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseOsc.ts and Parser.ts OSC handling to ensure C# implementation provides identical OSC parsing behavior and robustness
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParseOsc.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/Parser.ts
  - _Requirements: 13.1_

- [x] 6.2 Implement window title OSC sequences
  - Add OSC 0 and OSC 2 (set window title) sequence handling
  - Emit title change events with new title text
  - Add title state tracking in terminal
  - Handle empty titles and title reset
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts window title handling to ensure C# implementation provides identical title change behavior and event emission
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts
  - _Requirements: 13.2_

- [x] 6.3 Add clipboard OSC sequences
  - Add OSC 52 (clipboard) sequence handling
  - Emit clipboard events for game integration
  - Parse clipboard data and selection targets
  - Handle base64 encoded clipboard content
  - Define safety limits
    - Cap decoded clipboard size
    - Ignore invalid base64 payloads gracefully
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts clipboard handling to ensure C# implementation provides identical clipboard sequence processing and safety limits
  - _Requirements: 13.4_

- [x] 6.4 Write property test for OSC parsing and event emission
  - **Property 23: OSC parsing and event emission**
  - **Validates: Requirements 13.1, 13.2, 13.4**

- [x] 6.5 Implement hyperlink OSC sequences
  - Add OSC 8 (hyperlink) sequence parsing
  - Associate URLs with character ranges
  - Add hyperlink state to cell attributes
  - Handle hyperlink start/end sequences
  - Define association model
    - Track current hyperlink URL as state and apply to subsequent written cells
    - Clear hyperlink state on OSC 8 ;; ST
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts hyperlink handling to ensure C# implementation provides identical URL association and character range tracking
  - _Requirements: 13.3_

- [x] 6.6 Write property test for OSC hyperlink association
  - **Property 24: OSC hyperlink association**
  - **Validates: Requirements 13.3**

- [x] 6.7 Add unknown OSC sequence handling
  - Implement graceful handling of unknown OSC sequences
  - Log unknown sequences for debugging
  - Continue processing without errors
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts unknown sequence handling to ensure C# implementation provides identical graceful handling behavior
  - _Requirements: 13.5_

- [x] 6.8 Write property test for unknown OSC sequence handling
  - **Property 25: Unknown OSC sequence handling**
  - **Validates: Requirements 13.5**

- [x] 6.9 Add character set support
  - Implement character set state model
    - Track G0/G1/G2/G3 designations
    - Track active GL/GR mappings (at least GL via SI/SO)
  - Implement character set designation sequences
    - ESC ( X designate G0
    - ESC ) X designate G1
    - ESC * X designate G2
    - ESC + X designate G3
  - Handle shift-in (SI) and shift-out (SO) characters
    - Switch active GL between G0 and G1
  - Add DEC Special Graphics character set mapping
    - Map bytes/chars for line-drawing glyphs used by TUIs
    - Ensure mapping is bypassed when UTF-8 mode is enabled
  - Create character set mapping tables
    - Unit-test a small representative subset of mappings
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/charset.ts to ensure C# implementation provides identical character set designation, switching, and DEC Special Graphics mapping
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/charset.ts
  - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5_

- [x] 6.10 Test and validate advanced features
  - **USER VALIDATION REQUIRED**: Test OSC sequences and UTF-8 (including vim)
  - Verify window title changes work
  - Test UTF-8 and wide character display
  - Validate character set switching
  - Document any advanced feature issues

- [ ] 6.11 Checkpoint - OSC sequences and character sets working
  - Advanced terminal features function correctly
  - UTF-8 and character sets work properly



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