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



- [ ] 4. Add scrolling, scrollback, and screen management
- [x] 4.1 Create scrollback buffer infrastructure
  - Create IScrollbackBuffer interface
  - Create ScrollbackBuffer class with circular array
  - Add methods for adding lines and querying history
  - Implement size management and line reuse
  - Define what a stored scrollback line contains
    - Preserve characters and attributes (not just chars)
    - Ensure line length always equals cols for simple rendering
  - **CRITICAL CODE ORGANIZATION**: Create dedicated ScrollbackManager class
    - Extract scrollback logic into caTTY.Core/Managers/ScrollbackManager.cs
    - Create IScrollbackManager interface for testability
    - ScrollbackManager should handle all scrollback buffer operations and viewport management
    - ScrollbackManager should not exceed 250 lines (excluding comments)
    - TerminalEmulator should delegate scrollback operations to ScrollbackManager instance
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/scrollback.ts to ensure C# scrollback buffer provides identical circular buffer behavior and line preservation
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/scrollback.ts
  - _Requirements: 14.1, 14.2, 14.5_

- [x] 4.2 Implement basic scrolling operations
  - Add ScrollUp and ScrollDown methods to ScreenBuffer
  - Move scrolled content to scrollback buffer
  - Handle content preservation during scrolling
  - Add bounds checking for scroll operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts scrolling operations to ensure C# implementation provides identical scrolling behavior and content preservation
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts
  - _Requirements: 11.8, 11.9, 14.1_

- [ ] 4.3 Add scroll sequences to CSI parser
  - Implement scroll up (CSI S) and scroll down (CSI T) sequences
  - Add parameter parsing for scroll line counts
  - Integrate scrolling with screen buffer operations
  - Update screen content with scrolling operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts scroll sequence handling to ensure C# implementation provides identical scroll sequence behavior
  - _Requirements: 11.8, 11.9_

- [ ] 4.4 Write property test for scrollback buffer management
  - **Property 26: Scrollback buffer management**
  - **Validates: Requirements 14.1, 14.2**

- [ ] 4.5 Write property test for screen scrolling operations
  - **Property 20: Screen scrolling operations**
  - **Validates: Requirements 11.8, 11.9**

- [ ] 4.6 Implement scroll region management
  - Add scroll region state to terminal (top/bottom boundaries)
  - Implement set scroll region (CSI r) sequence
  - Restrict scrolling operations to defined scroll region
  - Handle cursor movement within and outside scroll regions
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts scroll region implementation to ensure C# provides identical scroll region behavior and cursor interaction
  - _Requirements: Requirement 10 from original spec_

- [ ] 4.7 Add viewport management for scrollback navigation
  - Create viewport offset tracking
  - Add methods for scrolling through history
  - Implement auto-scroll when new content arrives
  - Add viewport bounds checking
  - Define auto-follow rules
    - If user scrolls up, disable auto-follow until they return to bottom
    - New output should not yank viewport while user is reviewing history
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/scrollback.ts viewport management to ensure C# implementation provides identical auto-scroll and viewport behavior
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/scrollback.ts
  - _Requirements: 14.3, 14.4_

- [ ] 4.8 Write property test for viewport and auto-scroll behavior
  - **Property 27: Viewport and auto-scroll behavior**
  - **Validates: Requirements 14.3, 14.4**

- [ ] 4.9 Implement screen buffer resizing
  - Add Resize method with content preservation
  - Handle width/height changes intelligently
  - Preserve cursor position during resize
  - Update scrollback during resize operations
  - Define resize policy (simple, MVP-friendly)
    - Height change: preserve top-to-bottom rows where possible
    - Width change: truncate/pad each row; do not attempt complex reflow
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts resize operations to ensure C# implementation provides equivalent content preservation during resize
  - _Requirements: 7.2, 21.5_

- [ ] 4.10 Write property test for screen buffer resize preservation
  - **Property 8: Screen buffer resize preservation**
  - **Validates: Requirements 7.2**

- [ ] 4.11 Add line insertion and deletion operations
  - Implement insert line (CSI L) sequence with content shifting
  - Add delete line (CSI M) sequence with scrolling behavior
  - Handle scroll region boundaries during line operations
  - Update cursor position appropriately after operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts line insertion/deletion to ensure C# implementation provides identical line operation behavior and content shifting
  - _Requirements: 22.1, 22.2_

- [ ] 4.12 Add character insertion and deletion operations
  - Implement insert character (CSI @) sequence with line shifting
  - Add delete character (CSI P) sequence with content preservation
  - Handle character operations at line boundaries
  - Maintain SGR attributes during character operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts character insertion/deletion to ensure C# implementation provides identical character operation behavior and attribute preservation
  - _Requirements: 22.3, 22.4, 22.5_

- [ ] 4.13 Write property test for line and character operations
  - **Property 32: Line and character insertion/deletion**
  - **Validates: Requirements 22.1, 22.2, 22.3, 22.4, 22.5**

- [ ] 4.14 Test and validate scrolling functionality
  - **USER VALIDATION REQUIRED**: Test scrollback works in both apps
  - Verify long command output scrolls correctly
  - Test viewport navigation and auto-scroll
  - Validate resize handling preserves content
  - Document any scrolling issues

- [ ] 4.15 Checkpoint - Scrolling and screen management working
  - Terminal handles scrolling and scrollback correctly
  - Screen resizing works properly



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