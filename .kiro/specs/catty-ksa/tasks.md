# Implementation Plan: caTTY KSA Terminal Emulator

## Overview

This implementation plan creates a C# terminal emulator for the KSA game engine through incremental development. The plan prioritizes getting a minimal working MVP with real shell process integration as quickly as possible to validate the entire technology stack end-to-end. Each major task number represents a working program milestone that can be validated by the user.

Based on fresh analysis of the TypeScript implementation complexity, the most complex areas have been identified and broken down appropriately:

**High Complexity Areas (requiring granular breakdown):**
- **SGR (Select Graphic Rendition) parsing**: 556 lines - 12+ different SGR message types, complex color parsing (8-bit, 24-bit RGB), colon/semicolon separators, enhanced modes, underline styles
- **CSI sequence parsing**: 437 lines - State machine with 8+ states, parameter parsing, private mode indicators, complex cursor operations
- **StatefulTerminal integration**: 936 lines - Complex state management, multiple subsystems coordination, event handling
- **Buffer operations**: 455 lines - Character insertion/deletion, scrolling operations, line manipulation, content preservation
- **Parser state machine**: 541 lines - UTF-8 handling, multi-byte sequences, state transitions, escape sequence detection
- **Alternate screen buffer management**: Dual buffer system, state isolation, scrollback interaction

**Medium Complexity Areas (moderate breakdown):**
- **OSC sequence parsing**: 155 lines - String termination handling, multiple OSC types (title, clipboard, hyperlinks)
- **Terminal mode management**: Multiple DEC modes, application cursor keys, bracketed paste
- **Scrollback buffer**: 56 lines - Circular buffer management, viewport tracking
- **Process management**: Bidirectional data flow, lifecycle management, error handling

**Lower Complexity Areas (minimal breakdown):**
- **Basic data structures**: Cell, Cursor, Screen buffer
- **Control character handling**: Standard C0 controls
- **ImGui integration**: Display rendering, input handling

The task breakdown reflects this complexity analysis while maintaining MVP focus on getting a working terminal as quickly as possible. Key improvements include:

1. **More granular StatefulTerminal breakdown** - Added separate state management task (1.4)
2. **Enhanced parser complexity handling** - Added UTF-8 support as separate task (2.3-2.4) 
3. **Better buffer operations granularity** - Split line/character operations (4.11-4.13)
4. **Improved property test coverage** - Added property test for line/character operations (4.13)
5. **Earlier UTF-8 handling** - Moved from section 6 to section 2 for better dependency flow

## Tasks

- [ ] 1. Create minimal working end-to-end terminal with real shell process
- [ ] 1.1 Set up solution structure and all projects
  - Create caTTY-cs.sln solution file
  - Create caTTY.Core class library project with .NET 10 target
  - Create caTTY.TestApp console project
  - Create caTTY.ImGui class library project (placeholder)
  - Create caTTY.GameMod project (placeholder)
  - Configure all project properties and references
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 1.2 Implement minimal data structures
  - Create Cell struct with character only (no attributes yet)
  - Create basic IScreenBuffer interface
  - Create ScreenBuffer class with simple 2D char array
  - Create ICursor interface with row/col properties
  - Create Cursor class with basic position tracking
  - _Requirements: 7.1, 7.3, 8.1_

- [ ] 1.3 Create minimal terminal emulator core
  - Create ITerminalEmulator interface with Write method
  - Create TerminalEmulator class with screen buffer and cursor
  - Implement Write(ReadOnlySpan<byte>) for raw data processing
  - Add basic printable character handling (ASCII only)
  - Handle newlines and carriage returns for basic line discipline
  - _Requirements: 9.1, 10.1, 10.2, 23.1, 23.2_

- [ ] 1.4 Create basic terminal state management
  - Create TerminalState class with cursor position and basic modes
  - Add SGR state tracking (minimal - just current attributes)
  - Implement basic terminal dimensions and bounds checking
  - Add wrap pending state for line overflow handling
  - _Requirements: 8.1, 8.2, 12.3_

- [ ] 1.5 Implement process management
  - Create IProcessManager interface
  - Create ProcessManager class using System.Diagnostics.Process
  - Add shell spawning (PowerShell for Windows)
  - Implement bidirectional data flow (process ↔ terminal)
  - Add basic error handling and process lifecycle management
  - _Requirements: 27.1, 27.2, 27.3, 28.1, 28.2_

- [ ] 1.6 Create console test application
  - Implement Program.cs with terminal and process integration
  - Add simple console output for terminal content display
  - Create input loop for sending keystrokes to shell
  - Add basic commands to test shell interaction (ls, echo, etc.)
  - Display raw terminal output in console
  - _Requirements: 26.1, 26.2, 26.3, 26.4_

- [ ] 1.7 Test and validate console application
  - **USER VALIDATION REQUIRED**: Run test application and verify shell works
  - Test basic shell commands (ls, dir, echo)
  - Verify bidirectional data flow
  - Confirm process cleanup on exit
  - Document any issues found during testing

- [ ] 1.8 Create minimal ImGui controller
  - Create ITerminalController interface
  - Create ImGuiTerminalController class (basic implementation)
  - Add placeholder KSA game DLL references
  - Implement basic ImGui window with text display
  - Add minimal keyboard input handling
  - _Requirements: 16.1, 17.1, 18.1_

- [ ] 1.9 Create game mod entry point
  - Implement game mod initialization in caTTY.GameMod
  - Add mod lifecycle management (load/unload)
  - Integrate terminal controller with game mod
  - Add basic resource cleanup
  - Build game mod DLL output
  - _Requirements: 1.1, 1.4, 5.2_

- [ ] 1.10 Test and validate game mod integration
  - **USER VALIDATION REQUIRED**: Load mod in KSA game and verify it works
  - Test ImGui window display in game
  - Verify shell process works within game context
  - Test basic terminal interaction in game
  - Confirm mod unloads cleanly
  - Document any game integration issues

- [ ] 1.11 Checkpoint - End-to-end MVP working
  - Both console test app and game mod working with real shell
  - User has validated both deployment targets work
  - Ready to add more terminal features

- [ ] 2. Add basic escape sequence parsing and control characters
- [ ] 2.1 Add basic control character handling
  - Handle backspace (BS), tab (HT), bell (BEL)
  - Add basic cursor movement for control characters
  - Implement simple tab stops (every 8 columns)
  - Add event emission for bell character
  - _Requirements: 10.3, 10.4, 10.5, 19.1, 19.2_

- [ ] 2.2 Create escape sequence parser state machine
  - Create parser state enum (Ground, Escape, CsiEntry, CsiParam, etc.)
  - Implement basic state machine for escape sequence detection
  - Add escape sequence buffer for partial sequences
  - Handle ESC character and basic sequence detection
  - _Requirements: 11.1, 12.1, 13.1_

- [ ] 2.3 Add UTF-8 decoding support to parser
  - Implement UTF-8 multi-byte sequence detection
  - Add UTF-8 buffer for partial sequences across Write calls
  - Handle UTF-8 validation and error recovery
  - Integrate UTF-8 decoding with character processing
  - _Requirements: 9.3, 9.4_

- [ ] 2.4 Write property test for UTF-8 character handling
  - **Property 16: UTF-8 character handling**
  - **Validates: Requirements 9.3, 9.4**

- [ ] 2.5 Implement CSI parameter parsing
  - Create CSI parameter parsing logic
  - Handle numeric parameters and separators (semicolon/colon)
  - Add parameter validation and bounds checking
  - Support private mode indicators (? prefix)
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 2.6 Add basic cursor movement CSI sequences
  - Implement cursor up (CSI A), down (CSI B) sequences
  - Add cursor forward (CSI C), backward (CSI D) sequences
  - Add cursor position (CSI H) sequence handling
  - Update cursor position with bounds checking
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [ ] 2.7 Write property test for cursor movement sequences
  - **Property 13: Cursor movement sequences**
  - **Validates: Requirements 8.4, 11.1, 11.2, 11.3, 11.4, 11.5**

- [ ] 2.8 Add basic screen clearing CSI sequences
  - Implement erase in display (CSI J) sequence handling
  - Add erase in line (CSI K) sequence handling
  - Implement clearing logic for different erase modes (0, 1, 2)
  - Update screen buffer with cleared cells
  - _Requirements: 11.6, 11.7_

- [ ] 2.9 Write property test for screen clearing operations
  - **Property 19: Screen clearing operations**
  - **Validates: Requirements 11.6, 11.7**

- [ ] 2.10 Integrate escape sequence parsing into terminal
  - Add parser to terminal emulator
  - Update Write method to detect and parse escape sequences
  - Handle partial sequences across multiple Write calls
  - Test escape sequences with shell commands
  - _Requirements: 11.1, 12.1, 13.1_

- [ ] 2.11 Test and validate enhanced terminal functionality
  - **USER VALIDATION REQUIRED**: Test escape sequences work with shell
  - Verify cursor movement commands work (clear, cursor positioning)
  - Test with applications that use escape sequences
  - Validate both console app and game mod still work
  - Document any parsing issues found

- [ ] 2.12 Checkpoint - Basic escape sequence parsing working
  - Terminal handles basic CSI sequences and control characters
  - Both deployment targets validated by user

- [ ] 3. Add comprehensive SGR (text styling) support
- [ ] 3.1 Create SGR data structures and color system
  - Create Color union type (default, indexed, RGB)
  - Create SgrAttributes struct with all text styling properties
  - Add UnderlineStyle enum (none, single, double, curly, dotted, dashed)
  - Update Cell struct to include full SGR attributes
  - _Requirements: 12.2, 12.3, 12.4, 12.5_

- [ ] 3.2 Implement SGR parameter parsing (basic colors and styles)
  - Create SgrParser class for parsing SGR parameters
  - Add support for both semicolon and colon separators
  - Parse basic text styles (bold, italic, underline, strikethrough)
  - Handle standard 8-color foreground/background (30-37, 40-47)
  - _Requirements: 12.1, 12.2, 12.4, 12.5_

- [ ] 3.3 Add extended color parsing (256-color and RGB)
  - Implement 256-color parsing (38;5;n, 48;5;n)
  - Add 24-bit RGB color parsing (38;2;r;g;b, 48;2;r;g;b)
  - Handle colon-separated color formats (38:2:r:g:b)
  - Add bright color support (90-97, 100-107)
  - _Requirements: 12.1, 12.4_

- [ ] 3.4 Implement advanced SGR features
  - Add underline color support (58, 59)
  - Implement underline style subparameters (4:n)
  - Handle enhanced SGR modes (CSI > 4 ; n m)
  - Add private SGR modes (CSI ? 4 m)
  - _Requirements: 12.1, 12.2_

- [ ] 3.5 Create SGR state processor
  - Create SgrState class to track current attributes
  - Implement SGR message processing logic
  - Handle attribute reset and individual attribute clearing
  - Add inverse video processing
  - _Requirements: 12.2, 12.3_

- [ ] 3.6 Integrate SGR parsing into CSI parser
  - Add SGR sequence handling to CsiParser for 'm' command
  - Update terminal to track current SGR state
  - Apply attributes to characters written after SGR changes
  - Handle SGR reset (CSI 0 m) to restore defaults
  - _Requirements: 12.1, 12.2, 12.3_

- [ ] 3.7 Write property test for SGR parsing and application
  - **Property 21: SGR parsing and application**
  - **Validates: Requirements 12.1, 12.2, 12.4, 12.5**

- [ ] 3.8 Write property test for SGR reset behavior
  - **Property 22: SGR reset behavior**
  - **Validates: Requirements 12.3**

- [ ] 3.9 Update display to show colors and styles
  - Enhance console test app to display colors (if possible)
  - Update ImGui controller to render colors and text styles
  - Test with shell commands that produce colored output
  - Verify SGR attributes are applied correctly
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

- [ ] 4. Add scrolling, scrollback, and screen management
- [ ] 4.1 Create scrollback buffer infrastructure
  - Create IScrollbackBuffer interface
  - Create ScrollbackBuffer class with circular array
  - Add methods for adding lines and querying history
  - Implement size management and line reuse
  - _Requirements: 14.1, 14.2, 14.5_

- [ ] 4.2 Implement basic scrolling operations
  - Add ScrollUp and ScrollDown methods to ScreenBuffer
  - Move scrolled content to scrollback buffer
  - Handle content preservation during scrolling
  - Add bounds checking for scroll operations
  - _Requirements: 11.8, 11.9, 14.1_

- [ ] 4.3 Add scroll sequences to CSI parser
  - Implement scroll up (CSI S) and scroll down (CSI T) sequences
  - Add parameter parsing for scroll line counts
  - Integrate scrolling with screen buffer operations
  - Update screen content with scrolling operations
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
  - _Requirements: Requirement 10 from original spec_

- [ ] 4.7 Add viewport management for scrollback navigation
  - Create viewport offset tracking
  - Add methods for scrolling through history
  - Implement auto-scroll when new content arrives
  - Add viewport bounds checking
  - _Requirements: 14.3, 14.4_

- [ ] 4.8 Write property test for viewport and auto-scroll behavior
  - **Property 27: Viewport and auto-scroll behavior**
  - **Validates: Requirements 14.3, 14.4**

- [ ] 4.9 Implement screen buffer resizing
  - Add Resize method with content preservation
  - Handle width/height changes intelligently
  - Preserve cursor position during resize
  - Update scrollback during resize operations
  - _Requirements: 7.2, 21.5_

- [ ] 4.10 Write property test for screen buffer resize preservation
  - **Property 8: Screen buffer resize preservation**
  - **Validates: Requirements 7.2**

- [ ] 4.11 Add line insertion and deletion operations
  - Implement insert line (CSI L) sequence with content shifting
  - Add delete line (CSI M) sequence with scrolling behavior
  - Handle scroll region boundaries during line operations
  - Update cursor position appropriately after operations
  - _Requirements: 22.1, 22.2_

- [ ] 4.12 Add character insertion and deletion operations
  - Implement insert character (CSI @) sequence with line shifting
  - Add delete character (CSI P) sequence with content preservation
  - Handle character operations at line boundaries
  - Maintain SGR attributes during character operations
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

- [ ] 5. Add alternate screen buffer and advanced terminal modes
- [ ] 5.1 Create alternate screen buffer infrastructure
  - Create AlternateScreenManager class
  - Implement separate primary and alternate screen buffers
  - Add buffer switching methods (activate/deactivate)
  - Preserve cursor and attributes independently per buffer
  - _Requirements: 15.1, 15.2, 15.4_

- [ ] 5.2 Implement alternate screen isolation
  - Ensure alternate screen doesn't add to scrollback
  - Clear alternate buffer on activation
  - Handle buffer switching with proper state preservation
  - Maintain separate cursor positions per buffer
  - _Requirements: 15.3, 15.5_

- [ ] 5.3 Add alternate screen control sequences
  - Implement DEC private mode sequences for alternate screen
  - Add alternate screen activation/deactivation sequences
  - Handle mode switching in CSI parser
  - Test buffer switching with state preservation
  - _Requirements: 15.1, 15.2, 15.5_

- [ ] 5.4 Write property test for alternate screen buffer switching
  - **Property 29: Alternate screen buffer switching**
  - **Validates: Requirements 15.1, 15.2, 15.4**

- [ ] 5.5 Write property test for alternate screen scrollback isolation
  - **Property 30: Alternate screen scrollback isolation**
  - **Validates: Requirements 15.3**

- [ ] 5.6 Implement terminal mode management
  - Create terminal mode state tracking
  - Add auto-wrap mode with line wrapping behavior
  - Implement cursor visibility mode tracking
  - Add application cursor keys mode
  - _Requirements: 20.1, 20.2, 20.3, 20.4_

- [ ] 5.7 Add cursor wrapping and line overflow handling
  - Implement auto-wrap behavior when cursor reaches right edge
  - Add line overflow handling based on auto-wrap mode
  - Update character writing to respect wrapping settings
  - Handle wide character wrapping correctly
  - _Requirements: 8.3, 9.5, 20.1_

- [ ] 5.8 Write property test for cursor wrapping behavior
  - **Property 12: Cursor wrapping behavior**
  - **Validates: Requirements 8.3**

- [ ] 5.9 Add bracketed paste mode support
  - Implement bracketed paste mode state tracking
  - Add paste sequence wrapping for bracketed paste
  - Handle mode switching sequences
  - Prepare for future paste integration
  - _Requirements: 20.5_

- [ ] 5.10 Write property test for cursor visibility tracking
  - **Property 14: Cursor visibility tracking**
  - **Validates: Requirements 8.5**

- [ ] 5.11 Test and validate alternate screen and modes
  - **USER VALIDATION REQUIRED**: Test full-screen apps (vim, less)
  - Verify alternate screen works correctly
  - Test terminal mode switching
  - Validate cursor wrapping and visibility
  - Document any mode handling issues

- [ ] 5.12 Checkpoint - Alternate screen and terminal modes working
  - Full-screen applications work correctly
  - Terminal modes function properly

- [ ] 6. Add OSC sequences and advanced features
- [ ] 6.1 Create OSC sequence parser infrastructure
  - Create OscParser class for OSC sequences
  - Add OSC sequence detection (ESC ] command ST)
  - Parse OSC command numbers and parameters
  - Handle string termination with ST or BEL
  - _Requirements: 13.1_

- [ ] 6.2 Implement window title OSC sequences
  - Add OSC 0 and OSC 2 (set window title) sequence handling
  - Emit title change events with new title text
  - Add title state tracking in terminal
  - Handle empty titles and title reset
  - _Requirements: 13.2_

- [ ] 6.3 Add clipboard OSC sequences
  - Add OSC 52 (clipboard) sequence handling
  - Emit clipboard events for game integration
  - Parse clipboard data and selection targets
  - Handle base64 encoded clipboard content
  - _Requirements: 13.4_

- [ ] 6.4 Write property test for OSC parsing and event emission
  - **Property 23: OSC parsing and event emission**
  - **Validates: Requirements 13.1, 13.2, 13.4**

- [ ] 6.5 Implement hyperlink OSC sequences
  - Add OSC 8 (hyperlink) sequence parsing
  - Associate URLs with character ranges
  - Add hyperlink state to cell attributes
  - Handle hyperlink start/end sequences
  - _Requirements: 13.3_

- [ ] 6.6 Write property test for OSC hyperlink association
  - **Property 24: OSC hyperlink association**
  - **Validates: Requirements 13.3**

- [ ] 6.7 Add unknown OSC sequence handling
  - Implement graceful handling of unknown OSC sequences
  - Log unknown sequences for debugging
  - Continue processing without errors
  - _Requirements: 13.5_

- [ ] 6.8 Write property test for unknown OSC sequence handling
  - **Property 25: Unknown OSC sequence handling**
  - **Validates: Requirements 13.5**

- [ ] 6.9 Add character set support
  - Implement character set designation sequences
  - Add DEC Special Graphics character set mapping
  - Handle shift-in (SI) and shift-out (SO) characters
  - Create character set mapping tables
  - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5_

- [ ] 6.10 Test and validate advanced features
  - **USER VALIDATION REQUIRED**: Test OSC sequences and UTF-8
  - Verify window title changes work
  - Test UTF-8 and wide character display
  - Validate character set switching
  - Document any advanced feature issues

- [ ] 6.11 Checkpoint - OSC sequences and character sets working
  - Advanced terminal features function correctly
  - UTF-8 and character sets work properly

- [ ] 7. Add comprehensive input handling and selection
- [ ] 7.1 Enhance keyboard input handling
  - Improve key-to-sequence conversion in ImGui controller
  - Add special key handling (function keys, arrows)
  - Implement application cursor keys mode
  - Add proper modifier key handling
  - _Requirements: 16.2, 16.3, 16.4, 16.5_

- [ ] 7.2 Add selection and copying support
  - Implement mouse selection in ImGui context
  - Add visual selection highlighting
  - Create text extraction from selected cells
  - Integrate with game clipboard system
  - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5_

- [ ] 7.3 Enhance focus and window management
  - Improve focus state tracking
  - Add visual focus indicators
  - Handle window focus events properly
  - Integrate with game input system
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

- [ ] 8. Add comprehensive testing and TypeScript compatibility
- [ ] 8.1 Create comprehensive unit test suite
  - Add unit tests for all core terminal components
  - Create tests for ImGui controller integration
  - Add process management unit tests
  - Implement error condition and edge case tests
  - _Requirements: 30.1_

- [ ] 8.2 Implement property-based test suite
  - Create property tests for all identified correctness properties
  - Add FsCheck.NUnit integration and configuration
  - Set up test generators for terminal data and sequences
  - Configure minimum 100 iterations per property test
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
  - **Property 32: Line and character insertion/deletion**
  - **Validates: Requirements 2.3, 7.1, 7.3, 7.4, 7.5, 8.1, 8.2, 9.1, 9.2, 9.5, 10.1, 10.2, 10.3, 10.4, 10.5, 14.5, 15.5, 22.1, 22.2, 22.3, 22.4, 22.5**

- [ ] 8.4 Write TypeScript compatibility tests
  - **Property 2: TypeScript compatibility for escape sequences**
  - **Property 3: TypeScript compatibility for screen operations**
  - **Property 4: TypeScript compatibility for cursor operations**
  - **Property 5: TypeScript compatibility for scrollback behavior**
  - **Property 6: TypeScript compatibility for alternate screen**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

- [ ] 8.5 Add integration tests for game mod functionality
  - Create integration tests for game mod loading/unloading
  - Add tests for ImGui integration within game context
  - Test process management integration
  - Validate resource cleanup during mod lifecycle
  - _Requirements: 30.1_

- [ ] 8.6 Create performance and memory tests
  - Add performance benchmarks for terminal operations
  - Create memory allocation and garbage collection tests
  - Add stress tests for large data processing
  - Implement rendering performance validation
  - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [ ] 8.7 Final comprehensive testing and validation
  - **USER VALIDATION REQUIRED**: Final end-to-end testing
  - Test all features in both console app and game mod
  - Verify performance is acceptable
  - Test with various shell applications and commands
  - Document final validation results

- [ ] 8.8 Final checkpoint - Complete tested terminal implementation
  - All features working and thoroughly tested
  - Both deployment targets fully validated by user

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