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



- [ ] 3. Add comprehensive SGR (text styling) support
- [x] 3.1 Create SGR data structures and color system
  - Create Color union type (default, indexed, RGB)
  - Create SgrAttributes struct with all text styling properties
  - Add UnderlineStyle enum (none, single, double, curly, dotted, dashed)
  - Update Cell struct to include full SGR attributes
  - **CRITICAL CODE ORGANIZATION**: Create dedicated SgrParser class
    - Extract SGR parsing logic into caTTY.Core/Parsing/SgrParser.cs
    - Create ISgrParser interface for testability
    - SgrParser should handle all SGR parameter parsing and attribute processing
    - SgrParser should not exceed 300 lines (excluding comments)
    - Main Parser should delegate SGR parsing to SgrParser instance
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/screenTypes.ts and related SGR type definitions to ensure C# data structures match TypeScript capabilities and attribute handling
  - _Requirements: 12.2, 12.3, 12.4, 12.5_

- [x] 3.2 Implement SGR parameter parsing (basic colors and styles)
  - Create SgrParser class for parsing SGR parameters
  - Add support for both semicolon and colon separators
  - Parse basic text styles (bold, italic, underline, strikethrough)
  - Handle standard 8-color foreground/background (30-37, 40-47)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts SGR parsing logic to ensure C# implementation handles all SGR parameter types and separator formats identically
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts
  - _Requirements: 12.1, 12.2, 12.4, 12.5_

- [x] 3.3 Add extended color parsing (256-color and RGB)
  - Implement 256-color parsing (38;5;n, 48;5;n)
  - Add 24-bit RGB color parsing (38;2;r;g;b, 48;2;r;g;b)
  - Handle colon-separated color formats (38:2:r:g:b)
  - Add bright color support (90-97, 100-107)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts extended color parsing to ensure C# implementation supports all color formats and edge cases identically
  - _Requirements: 12.1, 12.4_

- [x] 3.4 Implement advanced SGR features
  - Add underline color support (58, 59)
  - Implement underline style subparameters (4:n)
  - Handle enhanced SGR modes (CSI > 4 ; n m)
  - Add private SGR modes (CSI ? 4 m)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts advanced SGR features to ensure C# implementation supports all advanced SGR modes and underline variants identically
  - _Requirements: 12.1, 12.2_

- [x] 3.5 Create SGR state processor
  - Create SgrState class to track current attributes
  - Implement SGR message processing logic
  - Handle attribute reset and individual attribute clearing
  - Add inverse video processing
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/SgrStateProcessor.ts and SgrStyleManager.ts to ensure C# SGR state management provides identical attribute tracking and processing behavior
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/SgrStateProcessor.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/SgrStyleManager.ts
  - _Requirements: 12.2, 12.3_

- [x] 3.6 Integrate SGR parsing into CSI parser
  - Add SGR sequence handling to CsiParser for 'm' command
  - Update terminal to track current SGR state
  - Apply attributes to characters written after SGR changes
  - Handle SGR reset (CSI 0 m) to restore defaults
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts SGR integration to ensure C# implementation applies SGR attributes to characters identically
  - _Requirements: 12.1, 12.2, 12.3_

- [x] 3.7 Write property test for SGR parsing and application
  - **Property 21: SGR parsing and application**
  - **Validates: Requirements 12.1, 12.2, 12.4, 12.5**

- [ ] 3.8 Write property test for SGR reset behavior
  - **Property 22: SGR reset behavior**
  - **Validates: Requirements 12.3**

- [ ] 3.9 Update display to show colors and styles
  - Enhance console test app to display colors (if possible)
    - Use ANSI SGR output only if the host console supports it; otherwise skip
  - Update ImGui controller to render colors and text styles
    - Resolve indexed/RGB colors against a theme/default palette
    - Render underline styles conservatively (at least single underline)
  - Test with shell commands that produce colored output
  - Verify SGR attributes are applied correctly
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ColorResolver.ts, TerminalTheme.ts, and DomStyleManager.ts to ensure C# ImGui rendering provides equivalent color resolution and style application
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ColorResolver.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/TerminalTheme.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/DomStyleManager.ts
  - _Requirements: 17.2, 17.3_

- [ ] 3.10 Test and validate color and styling
  - **USER VALIDATION REQUIRED**: Test colored output in both apps
  - Verify colors display correctly in console and game
  - Test with commands like ls --color, colored prompts
  - Validate text styles (bold, italic) if supported
  - Document any color rendering issues

- [ ] 3.11 Checkpoint - Colors and text styling working
  - Terminal displays colored and styled text correctly
  - Both deployment targets show proper color rendering



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