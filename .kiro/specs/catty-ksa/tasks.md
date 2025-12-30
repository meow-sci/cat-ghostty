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




## Missing CSI Sequence Implementation Tasks

The following tasks implement CSI sequences that are present in the TypeScript reference implementation but missing from the C# version. These were identified through comparison of the TypeScript and C# CSI handlers.

- [x] 9. Implement cursor save/restore (ANSI style) sequences
- [x] 9.1 Add ANSI cursor save/restore parsing to CsiParser
  - Add parsing for CSI s (save cursor) and CSI u (restore cursor) sequences
  - Create CsiMessage types for cursor save/restore operations
  - Add parameter validation and bounds checking
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for ANSI cursor save/restore parsing
  - _Requirements: Terminal compatibility, cursor state management_

- [x] 9.2 Implement cursor save/restore functionality in TerminalEmulator
  - Add SaveCursorPositionAnsi and RestoreCursorPositionAnsi methods to TerminalEmulator
  - Implement cursor position storage separate from DEC save/restore (ESC 7/8)
  - Add cursor state validation and bounds checking on restore
  - Integrate with existing cursor management system
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts cursor save/restore handling
  - _Requirements: Cursor position persistence, state isolation_

- [x] 9.3 Add CSI cursor save/restore handlers to TerminalParserHandlers
  - Add case handlers for "csi.saveCursorPosition" and "csi.restoreCursorPosition"
  - Wire up to TerminalEmulator save/restore methods
  - Add comprehensive unit tests for ANSI cursor save/restore
  - Test interaction with DEC cursor save/restore (should be independent)
  - _Requirements: Parser integration, handler completeness_

- [x] 10. Implement DEC soft reset sequence
- [x] 10.1 Add DEC soft reset parsing to CsiParser
  - Add parsing for CSI ! p (DECSTR - DEC Soft Terminal Reset) sequence
  - Create CsiMessage type for soft reset operation
  - Add intermediate character handling for '!' prefix
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for DECSTR parsing
  - _Requirements: DEC compatibility, terminal reset functionality_

- [x] 10.2 Implement soft reset functionality in TerminalEmulator
  - Add SoftReset method to TerminalEmulator
  - Reset terminal modes to initial state (cursor visibility, auto-wrap, etc.)
  - Reset SGR attributes to defaults
  - Clear tab stops and restore default tab stops
  - Reset character sets to defaults
  - Do NOT clear screen buffer or cursor position (key difference from hard reset)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts soft reset implementation
  - _Requirements: Partial terminal state reset, mode restoration_

- [x] 10.3 Add CSI soft reset handler and testing
  - Add case handler for "csi.decSoftReset" in TerminalParserHandlers
  - Wire up to TerminalEmulator SoftReset method
  - Add comprehensive unit tests for soft reset behavior
  - Test that screen content and cursor position are preserved
  - Test that modes and attributes are properly reset
  - _Requirements: Parser integration, reset behavior validation_

- [-] 11. Implement insert mode (IRM) sequence
- [x] 11.1 Add insert mode parsing and state tracking
  - Add parsing for CSI 4 h (set insert mode) and CSI 4 l (reset insert mode) sequences
  - Add InsertMode property to terminal mode state tracking
  - Create CsiMessage type for insert mode operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for IRM parsing
  - _Requirements: Terminal mode management, text insertion behavior_

- [x] 11.2 Implement insert mode character writing behavior
  - Modify character writing logic to respect insert mode state
  - When insert mode active: shift existing characters right before writing new character
  - When insert mode inactive: overwrite existing characters (default behavior)
  - Add bounds checking and line overflow handling for insert mode
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts insert mode handling
  - _Requirements: Character insertion, line management_

- [-] 11.3 Add insert mode handler and testing
  - Add case handler for "csi.insertMode" in TerminalParserHandlers
  - Wire up to terminal mode state management
  - Add comprehensive unit tests for insert mode behavior
  - Test character insertion vs overwrite behavior
  - Test line overflow and bounds checking in insert mode
  - _Requirements: Mode switching, insertion behavior validation_

- [x] 12. Implement window manipulation sequences
- [x] 12.1 Add window manipulation parsing to CsiParser
  - Add parsing for CSI Ps t sequences (window manipulation)
  - Support common operations: minimize (2), restore (1), resize (8), query size (18)
  - Create CsiMessage type with operation and parameter handling
  - Add parameter validation for different operation types
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for window manipulation parsing
  - _Requirements: Window control, parameter validation_

- [x] 12.2 Implement window manipulation functionality
  - Add HandleWindowManipulation method to TerminalEmulator
  - Implement title stack operations (push/pop title) for vi compatibility
  - Add window size query responses (report current terminal dimensions)
  - Handle unsupported operations gracefully (ignore minimize/restore in game context)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts window manipulation handling
  - _Requirements: Window state management, vi compatibility_

- [x] 12.3 Add window manipulation handler and testing
  - Add case handler for "csi.windowManipulation" in TerminalParserHandlers
  - Wire up to TerminalEmulator window manipulation methods
  - Add comprehensive unit tests for supported operations
  - Test title stack operations and size queries
  - Test graceful handling of unsupported operations
  - _Requirements: Parser integration, operation handling_

- [ ] 13. Implement enhanced SGR mode sequences
- [ ] 13.1 Add enhanced SGR parsing to CsiParser
  - Add parsing for CSI > Ps m sequences (enhanced SGR with > prefix)
  - Support enhanced underline styles and color modes
  - Create CsiMessage type for enhanced SGR operations
  - Add prefix character handling and parameter validation
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for enhanced SGR parsing
  - _Requirements: Advanced text styling, SGR extensions_

- [ ] 13.2 Implement enhanced SGR functionality in SgrParser
  - Add HandleEnhancedSgrMode method to SgrParser
  - Support enhanced underline styles (curly, dotted, dashed)
  - Add enhanced color mode processing
  - Integrate with existing SGR attribute system
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts for enhanced SGR handling
  - _Requirements: Extended styling capabilities, attribute management_

- [ ] 13.3 Add enhanced SGR handler and testing
  - Add case handler for "csi.enhancedSgrMode" in TerminalParserHandlers
  - Wire up to SgrParser enhanced mode handling
  - Add comprehensive unit tests for enhanced SGR features
  - Test enhanced underline styles and color modes
  - Test integration with standard SGR sequences
  - _Requirements: Parser integration, enhanced styling validation_

- [ ] 14. Implement private SGR mode sequences
- [ ] 14.1 Add private SGR parsing to CsiParser
  - Add parsing for CSI ? Ps m sequences (private SGR with ? prefix)
  - Support private SGR modes for terminal-specific features
  - Create CsiMessage type for private SGR operations
  - Add prefix character handling and mode validation
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for private SGR parsing
  - _Requirements: Terminal-specific features, private mode handling_

- [ ] 14.2 Implement private SGR functionality
  - Add HandlePrivateSgrMode method to SgrParser
  - Support private SGR modes (implementation-specific features)
  - Add graceful handling of unknown private modes
  - Integrate with existing SGR system without conflicts
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts private SGR handling
  - _Requirements: Private mode support, graceful degradation_

- [ ] 14.3 Add private SGR handler and testing
  - Add case handler for "csi.privateSgrMode" in TerminalParserHandlers
  - Wire up to SgrParser private mode handling
  - Add comprehensive unit tests for private SGR modes
  - Test graceful handling of unknown private modes
  - Test isolation from standard SGR sequences
  - _Requirements: Parser integration, private mode validation_

- [ ] 15. Implement SGR with intermediate characters
- [ ] 15.1 Add SGR intermediate character parsing to CsiParser
  - Add parsing for CSI Ps <intermediate> m sequences (e.g., CSI 0 % m)
  - Support intermediate characters in SGR sequences
  - Create CsiMessage type with intermediate character handling
  - Add intermediate character validation and processing
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for SGR intermediate handling
  - _Requirements: Extended SGR syntax, intermediate character support_

- [ ] 15.2 Implement SGR intermediate character functionality
  - Add HandleSgrWithIntermediate method to SgrParser
  - Support SGR sequences with intermediate characters
  - Add intermediate character interpretation logic
  - Integrate with existing SGR processing pipeline
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts SGR intermediate handling
  - _Requirements: Extended SGR processing, intermediate character interpretation_

- [ ] 15.3 Add SGR intermediate handler and testing
  - Add case handler for "csi.sgrWithIntermediate" in TerminalParserHandlers
  - Wire up to SgrParser intermediate character handling
  - Add comprehensive unit tests for SGR with intermediate characters
  - Test various intermediate character combinations
  - Test integration with standard SGR sequences
  - _Requirements: Parser integration, intermediate character validation_

- [ ] 17. Implement mouse reporting mode sequences
- [ ] 17.1 Add mouse reporting mode parsing to CsiParser
  - Add parsing for mouse reporting mode sequences (CSI ? 1000 h/l, etc.)
  - Create CsiMessage type for mouse reporting operations
  - Add mode parameter validation and handling
  - Support various mouse reporting modes (basic, extended, SGR)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts for mouse reporting parsing
  - _Requirements: Mouse input support, mode management_

- [ ] 17.2 Implement mouse reporting mode state tracking
  - Add mouse reporting mode state to terminal mode management
  - Track different mouse reporting modes (none, basic, extended, SGR)
  - Add mode switching and validation logic
  - Prepare infrastructure for future mouse input handling
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts mouse reporting handling
  - _Requirements: Mouse mode management, state tracking_

- [ ] 17.3 Add mouse reporting handler and testing
  - Add case handler for "csi.mouseReportingMode" in TerminalParserHandlers
  - Wire up to terminal mouse mode state management
  - Add comprehensive unit tests for mouse reporting modes
  - Test mode switching and state validation
  - Note: Actual mouse input handling deferred to future tasks
  - _Requirements: Parser integration, mode state validation_


- [ ] 18. Implement color query OSC sequences
- [ ] 18.1 Add color query response generation
  - Add GenerateForegroundColorResponse method to DeviceResponses class
  - Add GenerateBackgroundColorResponse method to DeviceResponses class
  - Support RGB color format responses (rgb:rrrr/gggg/bbbb format)
  - Add current SGR state integration for color resolution
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/responses.ts color response generation
  - _Requirements: Color query support, terminal theme integration_

- [ ] 18.2 Implement OSC color query handlers
  - Add case handlers for "osc.queryForegroundColor" and "osc.queryBackgroundColor" in HandleXtermOsc
  - Wire up to DeviceResponses color query methods
  - Add current terminal state color resolution logic
  - Emit proper OSC response format for color queries
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts color query handling
  - _Requirements: OSC handler completeness, color query responses_

- [ ] 18.3 Add comprehensive OSC color query testing
  - Add unit tests for OSC 10;? (query foreground color) sequences
  - Add unit tests for OSC 11;? (query background color) sequences
  - Test color response format and RGB value accuracy
  - Test integration with current SGR state and terminal themes
  - Test proper OSC response emission and formatting
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/__tests__/Parser.test.ts OSC color query tests
  - _Requirements: OSC color query validation, response format verification_




## Notes

- All tasks are required for comprehensive TypeScript compatibility
- Each task references specific requirements for traceability
- Tasks are sized to optimize AI/LLM context window usage
- Complex areas have been broken down into focused subtasks
- The implementation follows the TypeScript version as a reference for behavior compatibility
- Each major task number (9-17) implements a specific missing CSI sequence type
- Task 18 implements missing OSC color query sequences
- Subtasks are kept small and focused on specific implementation aspects
- Property tests and comprehensive unit tests are included for each feature

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