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



- [ ] 7. Add comprehensive input handling and selection
- [x] 7.1 Enhance keyboard input handling
  - Improve key-to-sequence conversion in ImGui controller
    - Define a single encoder entrypoint (key event → bytes)
    - Ensure text input and key events do not double-send
  - Add basic navigation key handling
    - Arrow keys, Home/End, PageUp/PageDown, Insert/Delete
  - Add function key handling
    - F1-F12 escape sequences (xterm-compatible)
  - Implement application cursor keys mode
    - Switch arrow-key sequences based on mode state
  - Add modifier key handling
    - Ctrl combinations (Ctrl+C, Ctrl+V, Ctrl+W etc) forwarded correctly
    - Alt/Meta handling for escape-prefixed sequences
    - Shift behavior for navigation keys where applicable
  - Add keypad semantics (minimal)
    - Enter vs Return distinction if available
  - **Compare with TypeScript implementation**: Review catty-web/app/src/ts/terminal/TerminalController.ts keyboard input handling to ensure C# ImGui implementation provides equivalent key encoding and modifier handling
  - TypeScript reference: catty-web/app/src/ts/terminal/TerminalController.ts
  - _Requirements: 16.2, 16.3, 16.4, 16.5_

- [x] 7.2 Add selection and copying support
  - Implement mouse selection in ImGui context
    - Map mouse coords to (row, col) in the terminal grid
  - Add visual selection highlighting
    - Ensure highlight works across viewport/scrollback rows
  - Create text extraction from selected cells
    - Normalize line endings (\n) and trim trailing spaces optionally
    - Respect wrapped lines vs explicit newlines (simple rule acceptable)
  - Integrate with game clipboard system
  - **Compare with TypeScript implementation**: Review catty-web/app/src/ts/terminal/TerminalController.ts selection and copying logic to ensure C# ImGui implementation provides equivalent text extraction and selection behavior
  - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5_

- [ ] 7.3 Enhance focus and window management
  - Improve focus state tracking
  - Add visual focus indicators
  - Handle window focus events properly
  - Integrate with game input system
  - Define input capture priority
    - When terminal is focused, suppress game hotkeys bound to typing
    - When terminal is unfocused/hidden, pass all input through to game
  - **Compare with TypeScript implementation**: Review catty-web/app/src/ts/terminal/TerminalController.ts focus management to ensure C# ImGui implementation provides equivalent focus handling and input priority management
  - _Requirements: 18.2, 18.3, 18.4, 18.5_

- [ ] 7.4 Test and validate input and selection
  - **USER VALIDATION REQUIRED**: Test keyboard input thoroughly
  - Verify special keys work correctly
  - Test mouse selection and copying
  - Validate focus management
  - Document any input handling issues

- [ ] 7.5 Checkpoint - Input handling and selection working
  - Keyboard input works comprehensively
  - Selection and copying function properly



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