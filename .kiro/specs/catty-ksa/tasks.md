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



- [ ] 8. Add comprehensive testing and TypeScript compatibility
- [ ] 8.1 Create comprehensive unit test suite
  - Add unit tests for all core terminal components matching TypeScript coverage
  - Create tests for ImGui controller integration
  - Add process management unit tests
  - Implement error condition and edge case tests
  - **CRITICAL**: Match TypeScript test coverage with 42+ test files covering all parser types, terminal behaviors, and advanced features
  - **Compare with TypeScript implementation**: Review all TypeScript test files in catty-web/packages/terminal-emulation/src/terminal/__tests__/ to ensure C# test suite provides equivalent or better coverage for all terminal functionality
  - Add parser state integrity tests (matching Parser.state.property.test.ts)
  - Add comprehensive CSI sequence tests (matching Parser.csi.test.ts)
  - Add SGR parsing tests with color variants (matching Parser.sgr.test.ts)
  - Add OSC sequence tests including hyperlinks (matching Parser.osc.property.test.ts)
  - Add DCS handling tests (matching DcsHandling.test.ts)
  - Add cursor positioning tests (matching CursorPositioning.test.ts)
  - Add alternate screen tests (matching AlternateScreen.test.ts)
  - Add scrollback tests (matching Scrollback.test.ts)
  - Add tab stop control tests (matching TabStopControls.test.ts)
  - Add device query tests (matching DeviceQuery.property.test.ts)
  - Add window manipulation tests (matching WindowManipulation.test.ts)
  - Add UTF-8 processing tests (matching Utf8Processing.property.test.ts)
  - Add selection and text extraction tests
  - Add character set handling tests
  - Add enhanced SGR mode tests (matching EnhancedSgrMode.test.ts)
  - Add selective erase tests (matching SelectiveErase.test.ts)
  - Add insert/delete character tests (matching InsertDeleteChars.test.ts)
  - _Requirements: 30.1_

- [ ] 8.2 Implement property-based test suite
  - Create property tests for all identified correctness properties
  - Add FsCheck.NUnit integration and configuration
  - Set up test generators for terminal data and sequences
  - Configure minimum 100 iterations per property test
  - **CRITICAL**: Ensure broad coverage matching TypeScript property tests
  - **Compare with TypeScript implementation**: Review all TypeScript property test files in catty-web/packages/terminal-emulation/src/terminal/__tests__/ to ensure C# property tests provide equivalent or better coverage for all correctness properties
  - Add parser state integrity properties (matching StatefulTerminal.cursor.property.test.ts)
  - Add cursor behavior properties with round-trip validation
  - Add color consistency properties (matching SgrColorConsistency.property.test.ts)
  - Add CSS generation determinism properties (matching CssGenerationDeterminism.property.test.ts)
  - Add application cursor key properties (matching ApplicationCursorKeys.property.test.ts)
  - Add theme color resolution properties (matching ThemeColorResolution.property.test.ts)
  - Add Vi sequence properties (matching ViSequenceProperties.property.test.ts)
  - Add device query response properties
  - Add UTF-8 processing properties with wide character handling
  - Add scrollback buffer properties with circular array validation
  - Add alternate screen isolation properties
  - Add terminal state consistency properties during error conditions
  - Add memory allocation and performance properties
  - _Requirements: 30.2_

- [ ] 8.3 Write remaining property tests for core functionality
  - **Property 1: Event notification consistency**
  - **Property 7: Screen buffer initialization**
  - **Property 9: Cell data integrity**
  - **Property 10: Terminal size constraints**
  - **Property 11: Cursor initialization and advancement**
  - **Property 15: Character processing with attributes**
  - **Property 17: Line wrapping behavior**
  - **Property 18: Control character processing**
  - **Property 28: Scrollback access**
  - **Property 31: Alternate screen initialization**
  - **Property 32: Font configuration acceptance and application**
  - **Property 33: Context detection and default configuration**
  - **Property 34: Runtime font configuration updates**
  - **Property 35: Font style selection consistency**
  - **Property 36: Line and character insertion/deletion**
  - **Validates: Requirements 2.3, 7.1, 7.3, 7.4, 7.5, 8.1, 8.2, 9.1, 9.2, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5, 14.5, 15.5, 22.1, 22.2, 22.3, 22.4, 22.5, 32.1, 32.2, 32.3, 32.4, 32.5, 33.1, 33.2, 33.3, 33.4, 33.5, 34.1, 34.2, 34.3, 34.4, 34.5**

- [ ] 8.4 Write TypeScript compatibility tests
  - **Property 2: TypeScript compatibility for escape sequences**
  - **Property 3: TypeScript compatibility for screen operations**
  - **Property 4: TypeScript compatibility for cursor operations**
  - **Property 5: TypeScript compatibility for scrollback behavior**
  - **Property 6: TypeScript compatibility for alternate screen**
  - **CRITICAL**: Ensure behavioral compatibility with TypeScript reference implementation
  - Add escape sequence parsing compatibility tests comparing C# and TypeScript results
  - Add screen operation compatibility tests validating identical state transitions
  - Add cursor operation compatibility tests ensuring identical positioning logic
  - Add scrollback behavior compatibility tests matching TypeScript scrolling semantics
  - Add alternate screen compatibility tests validating buffer switching behavior
  - Add SGR parsing compatibility tests ensuring identical color and style handling
  - Add OSC sequence compatibility tests matching TypeScript OSC processing
  - Add control character compatibility tests validating identical responses
  - Add terminal mode compatibility tests ensuring identical mode handling
  - Add character set compatibility tests matching TypeScript charset behavior
  - Add UTF-8 processing compatibility tests with wide character handling
  - Add device query compatibility tests ensuring identical response generation
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

- [ ] 8.5 Add integration tests for game mod functionality
  - Create integration tests for game mod loading/unloading
  - Add tests for ImGui integration within game context
  - Test process management integration
  - Validate resource cleanup during mod lifecycle
  - Define a realistic test strategy
    - Prefer headless tests for Core; keep game-mod “integration tests” as smoke/manual harness if game APIs cannot be loaded in CI
  - _Requirements: 30.1_

- [ ] 8.6 Create performance and memory tests
  - Add performance benchmarks for terminal operations
  - Create memory allocation and garbage collection tests
  - Add stress tests for large data processing
  - Implement rendering performance validation
  - **CRITICAL**: Add comprehensive performance testing matching TypeScript benchmarks
  - Add parser performance tests with large escape sequence streams
  - Add screen buffer performance tests with frequent updates
  - Add scrollback performance tests with large history buffers
  - Add ImGui rendering performance tests with complex styling
  - Add memory allocation pattern tests to minimize GC pressure
  - Add UTF-8 processing performance tests with wide characters
  - Add concurrent access performance tests for multi-threaded scenarios
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 8.7 Add comprehensive test coverage validation
  - **CRITICAL**: Ensure test coverage matches or exceeds TypeScript implementation
  - Validate all 42+ TypeScript test file equivalents are implemented in C#
  - Ensure all parser types have comprehensive test coverage (CSI, SGR, OSC, DCS, ESC)
  - Validate all terminal behaviors have property-based test coverage
  - Ensure all advanced features have integration test coverage
  - Add test coverage metrics and reporting
  - Validate compatibility test coverage against TypeScript reference
  - Ensure performance test coverage for all critical paths
  - Add test documentation explaining coverage strategy and test organization
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 8.8 Final comprehensive testing and validation
  - **USER VALIDATION REQUIRED**: Final end-to-end testing
  - Test all features in both console app and game mod
  - Verify performance is acceptable
  - Test with various shell applications and commands
  - Document final validation results
  - **CRITICAL BUILD QUALITY REQUIREMENTS**:
    - **ENTIRE SOLUTION MUST COMPILE WITH ZERO WARNINGS AND ZERO ERRORS**
    - **ENTIRE TEST SUITE MUST PASS WITH ZERO FAILURES**
    - Verify all projects compile successfully with `TreatWarningsAsErrors=true`
    - Ensure all nullable reference type warnings are resolved
    - Confirm all XML documentation warnings are addressed
    - Validate all unit tests pass consistently
    - Verify all property-based tests pass across multiple runs (minimum 100 iterations each)
    - Ensure all integration tests pass reliably
    - Confirm all TypeScript compatibility tests pass
    - Validate all performance tests meet benchmarks
    - Verify clean build with `dotnet build --configuration Release --verbosity normal`
    - Confirm clean test run with `dotnet test --configuration Release --verbosity normal`

- [ ] 8.9 Final checkpoint - Complete tested terminal implementation
  - All features working and thoroughly tested
  - Both deployment targets fully validated by user
  - **ZERO WARNINGS AND ZERO ERRORS** in entire solution
  - **ZERO TEST FAILURES** in entire test suite
  - Ready for production deployment



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