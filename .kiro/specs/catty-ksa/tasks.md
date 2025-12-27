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

- [-] 1. Create minimal working end-to-end terminal with real shell process
- [x] 1.1 Set up solution structure and all projects
  - Create caTTY-cs.sln solution file
  - Create caTTY.Core class library project with .NET 10 target
  - Create caTTY.TestApp console project (BRUTAL ImGui application with GLFW window)
  - Create caTTY.ImGui class library project (placeholder)
  - Create caTTY.ImGui.Playground console project (placeholder)
  - Create caTTY.GameMod project (placeholder)
  - Add caTTY.Core.Tests project (NUnit + FsCheck.NUnit) for core logic testing
- Add caTTY.ImGui.Tests project (NUnit + FsCheck.NUnit) for ImGui controller testing
  - Add shared build config (Directory.Build.props or equivalent)
    - Enable nullable, treat warnings as errors, XML docs
    - Set LangVersion and TargetFramework defaults
  - Add repo-level .editorconfig for C# formatting consistency
  - Wire up project references to match desired dependency flow
    - caTTY.TestApp → caTTY.ImGui → caTTY.Core (TestApp uses BRUTAL ImGui)
    - caTTY.ImGui → caTTY.Core
    - caTTY.ImGui.Playground → (KSA DLLs only, no caTTY dependencies)
    - caTTY.GameMod → caTTY.ImGui → caTTY.Core
    - caTTY.Core.Tests → caTTY.Core
    - caTTY.ImGui.Tests → caTTY.ImGui
  - Add minimal solution folders matching intended layout (Core/ImGui/TestApp/GameMod/Tests)
  - Configure basic build/run commands for local dev (dotnet build/test/run)
  - **Reference**: Use `KsaExampleMod/` folder as template for KSA game mod project structure
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 1.2 Implement minimal data structures
  - Create Cell struct with character only (no attributes yet)
    - Define a default/empty cell value (e.g. space)
    - Decide how to represent “unset” vs “space” (usually just space)
  - Create basic IScreenBuffer interface
    - Include clear operations needed by CSI erase modes later
    - Define how cells are accessed (Get/Set with bounds behavior)
  - Create ScreenBuffer class with simple 2D char array
    - Initialize all cells to the default/empty cell
    - Add helpers to clear a row/region efficiently
  - Create ICursor interface with row/col properties
  - Create Cursor class with basic position tracking
    - Add clamp helper to keep cursor within bounds
    - Add save/restore storage (even if unused until ESC/CSI tasks)
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/screenTypes.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/state.ts
  - _Requirements: 7.1, 7.3, 8.1_

- [x] 1.3 Create minimal terminal emulator core
  - Create ITerminalEmulator interface with Write method
    - Expose cols/rows (or Width/Height) so UI and tests can query size
    - Expose a way to read the current screen snapshot for rendering
  - Create TerminalEmulator class with screen buffer and cursor
  - Implement Write(ReadOnlySpan<byte>) for raw data processing
    - Ensure Write can be called with partial chunks and in rapid succession
  - Add basic printable character handling (ASCII only)
    - Decide how to handle DEL (0x7f) and other non-printables (usually ignore)
    - Decide replacement behavior for unsupported bytes (ignore vs replacement char)
  - Handle newlines and carriage returns for basic line discipline
    - LF: move down (and scroll if at bottom)
    - CR: move to column 0
    - CRLF: treat as CR then LF
    - Keep behavior consistent with future scrollback work
    - Ensure cursor is clamped after movement
  - Add minimal event/callback hooks
    - ScreenUpdated event (or equivalent) for UI refresh
    - ResponseEmitted event for device query replies (wired in 1.5)
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/StatefulTerminal.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts
  - _Requirements: 9.1, 10.1, 10.2, 23.1, 23.2_

- [x] 1.4 Create ImGui playground application structure
  - Create caTTY.ImGui.Playground console application project
  - Add KSA game DLL references (same as caTTY.ImGui and caTTY.GameMod)
  - Set up basic ImGui initialization and window management
  - Create placeholder Program.cs with ImGui context setup
  - Add project to solution and configure build dependencies
  - **Reference**: Use `KsaExampleMod/modone.csproj` for KSA DLL reference patterns
  - _Requirements: 31.1, 5.3_

- [x] 1.5 Implement basic ImGui rendering experiments
  - Create simple character grid rendering using ImGui text functions
  - Experiment with different approaches to terminal-like display
    - Fixed-width font rendering
    - Character cell positioning and alignment
    - Grid layout with consistent spacing
  - Add basic color experiments (foreground/background)
  - Test different ImGui text rendering methods for performance
  - Document findings and preferred approaches
  - _Requirements: 31.2, 31.3_

- [x] 1.6 Add text styling experiments to playground
  - Implement bold, italic, underline text rendering experiments
  - Test different approaches to text attribute application
  - Experiment with cursor display techniques
    - Block cursor, underline cursor, beam cursor
    - Cursor blinking and visibility states
  - Add interactive controls to toggle different styling options
  - Document styling capabilities and limitations
  - _Requirements: 31.4, 31.5_

- [x] 1.7 Test and validate playground functionality
  - **USER VALIDATION REQUIRED**: Run playground and verify ImGui rendering works
  - Test different text styling combinations
  - Verify color rendering accuracy
  - Validate cursor display techniques
  - Document any rendering issues or limitations found
  - _Requirements: 31.1, 31.2, 31.3, 31.4, 31.5_

- [x] 1.8 Create basic terminal state management
  - Create TerminalState class with cursor position and basic modes
    - Track tab stops and scroll region placeholders for later tasks
  - Add SGR state tracking (minimal - just current attributes)
    - Ensure default attribute state is well-defined and resettable
  - Implement basic terminal dimensions and bounds checking
    - Decide what happens on writes beyond right edge (wrapPending groundwork)
  - Add wrap pending state for line overflow handling
    - Define semantics: “write at last col sets wrapPending; next printable triggers LF+CR then write”
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/state.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/cursor.ts
  - _Requirements: 8.1, 8.2, 12.3_

- [x] 1.9 Implement process management using Windows ConPTY
  - **CRITICAL**: Use Windows ConPTY (Pseudoconsole) exclusively - no fallback to System.Diagnostics.Process redirection
  - **Platform Requirement**: Windows 10 version 1809+ only - throw PlatformNotSupportedException on other platforms
  - Create IProcessManager interface
  - Create ProcessManager class using Windows ConPTY APIs
    - Add P/Invoke declarations for CreatePseudoConsole, ResizePseudoConsole, ClosePseudoConsole
    - Add P/Invoke declarations for CreatePipe, ReadFile, WriteFile, process creation APIs
    - Implement proper ConPTY handle and pipe resource management
  - Define a ProcessLaunchOptions model
    - Shell selection (pwsh/powershell/cmd) and arguments
    - Working directory, env vars, initial cols/rows for ConPTY creation
  - Add shell spawning using ConPTY
    - Create pseudoconsole with CreatePseudoConsole API
    - Use PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE for process creation
    - Implement pipe-based I/O (not stream redirection)
  - Implement ConPTY output read loops
    - Use ReadFile on ConPTY output pipe
    - Non-blocking/async reads with proper Win32 error handling
    - Forward output bytes to terminal Write(ReadOnlySpan<byte>)
  - Implement ConPTY input write path for user input
    - Use WriteFile to ConPTY input pipe
    - Send encoded bytes directly to ConPTY
    - Proper error handling for broken pipes
  - Support terminal-generated responses (query replies) back to ConPTY input
    - Add OnTerminalResponse event/callback hook
    - Ensure response writes do not interleave incorrectly with user input
  - Add ConPTY process lifecycle management
    - Start/Stop/Dispose semantics with proper ConPTY cleanup
    - Detect exit, capture exit code, raise ProcessExited event
    - Cancellation tokens for read loops
  - Add ConPTY-specific error handling
    - Create ConPtyException with Win32 error codes
    - Report ConPTY creation failures, pipe errors, process start failures
    - Handle broken pipe / exited process during writes
  - Implement true terminal resizing
    - Use ResizePseudoConsole API for dynamic terminal size changes
    - Proper terminal dimension reporting to child processes
  - **Reference**: Follow Microsoft's ConPTY documentation exactly: https://learn.microsoft.com/en-us/windows/console/creating-a-pseudoconsole-session
  - TypeScript reference: catty-web/node-pty/src/BackendServer.ts (for interface design only)
  - TypeScript reference: catty-web/node-pty/src/server.ts (for interface design only)
  - _Requirements: 27.1, 27.2, 27.3, 28.1, 28.2_

- [x] 1.10 Create standalone BRUTAL ImGui test application
  - Create Program.cs with BRUTAL ImGui initialization and GLFW window setup
  - Add KSA game DLL references (same as caTTY.ImGui and caTTY.GameMod)
  - Initialize standalone ImGui context using BRUTAL ImGui framework
  - Create main application loop with ImGui rendering
  - Integrate terminal emulator and process manager
  - Add ImGui terminal controller for display and input handling
  - Use the same ImGui controller code that the game mod will use
  - Add proper resource cleanup and disposal
  - **Reference**: Use `catty-ksa/caTTY.ImGui.Playground/caTTY.ImGui.Playground.csproj` for KSA DLL reference patterns and how to setup the project code to run the Glfw window, Vulkan and BRUTAL ImGui code
  - **Reference**: Use existing ImGui playground experiments for ImGui setup patterns
  - _Requirements: 26.1, 26.2, 26.3, 26.4, 26.5_

- [x] 1.11 Test and validate BRUTAL ImGui test application
  - **USER VALIDATION REQUIRED**: Run test application and verify GLFW window opens with ImGui terminal
  - Test basic shell commands (ls, dir, echo) in the ImGui terminal window
  - Verify bidirectional data flow between shell and ImGui display
  - Test keyboard input handling through ImGui
  - Test terminal rendering with colors and text styling
  - Confirm process cleanup on application exit
  - Validate that the same ImGui controller code works in standalone context
  - Document any issues found during testing

- [x] 1.12 Create shared ImGui controller for both TestApp and GameMod
  - Create ITerminalController interface
  - Create ImGuiTerminalController class (shared implementation for both TestApp and GameMod)
  - Add KSA game DLL references to caTTY.ImGui project
  - Define the controller data flow boundaries
    - Subscribe to terminal updates and request redraw
    - Emit user input bytes/strings to ProcessManager
  - Implement basic ImGui window with text display
    - Render a fixed-size grid view (rows x cols) using terminal snapshot
    - Render cursor (even if minimal: invert cell or draw rect)
  - Add minimal keyboard input handling
    - Text input for printable characters
    - Enter, Backspace, Tab
    - Arrow keys (basic) mapped to escape sequences
  - Implement focus gating
    - Only capture keyboard input when terminal window focused
  - Implement minimal scrollback viewing in UI (optional for MVP)
    - Render only viewport rows provided by terminal
  - This controller will be used by both the standalone TestApp and the GameMod
  - TypeScript reference: catty-web/app/src/ts/terminal/TerminalController.ts
  - TypeScript reference: catty-web/app/src/components/terminal/Terminal.tsx
  - TypeScript reference: catty-web/app/src/components/terminal/TerminalPage.tsx
  - _Requirements: 16.1, 17.1, 18.1_

- [x] 1.12.1 Add font configuration system to ImGui controller
  - Create TerminalFontConfig class with font family and style settings
    - Support Regular, Bold, Italic, and BoldItalic font variants
    - Include configurable font size with validation (8.0f to 72.0f)
    - Add factory methods for TestApp and GameMod contexts
  - Create FontContextDetector utility for automatic context detection
    - Detect TestApp vs GameMod execution environment
    - Provide appropriate default font configurations
  - Update ImGuiTerminalController to accept font configuration
    - Add constructor overload with TerminalFontConfig parameter
    - Maintain backward compatibility with existing constructor
    - Load fonts from ImGui font system based on configuration
    - Calculate character metrics from loaded fonts
  - Implement runtime font configuration updates
    - Add UpdateFontConfig method to ITerminalController interface
    - Support font changes without restarting application
    - Recalculate character metrics after font changes
  - Add font loading and validation
    - Validate font names and sizes during configuration
    - Fall back to available fonts when specified fonts are unavailable
    - Log font configuration for debugging purposes
  - Implement font style selection for character rendering
    - Select appropriate font variant based on SGR attributes
    - Use Bold, Italic, or BoldItalic fonts for styled characters
    - Ensure consistent font application across all character rendering
  - _Requirements: 32.1, 32.2, 32.3, 32.4, 32.5, 33.1, 33.2, 33.3, 33.4, 33.5, 34.1, 34.2, 34.3, 34.4, 34.5_

- [x] 1.13 Create game mod entry point
  - Implement game mod initialization in caTTY.GameMod
    - Define a minimal mod API surface (init/update/draw/dispose)
    - Ensure the mod does not block the game loop (no synchronous reads)
  - Add mod lifecycle management (load/unload)
    - Guard against double-init and double-dispose
  - Integrate terminal controller with game mod
    - Decide how the terminal window is toggled (keybind)
    - Ensure input is routed only when terminal is visible/focused
  - Add basic resource cleanup
    - Dispose process manager, controller, and any subscriptions
  - Build game mod DLL output
    - Document required DLL references and expected install path assumptions
  - **Reference**: Follow `KsaExampleMod/Class1.cs` and `KsaExampleMod/Patcher.cs` patterns for StarMap attribute-based implementation
  - _Requirements: 1.1, 1.4, 5.2_

- [x] 1.14 Test and validate both TestApp and GameMod integration
  - **USER VALIDATION REQUIRED**: Test standalone BRUTAL ImGui TestApp works correctly
  - **USER VALIDATION REQUIRED**: Load mod in KSA game and verify it works
  - Verify both applications use the same ImGui controller and rendering code
  - Test ImGui window display in both standalone app and game
  - Verify shell process works in both contexts
  - Test basic terminal interaction in both applications
  - Confirm both applications dispose resources cleanly on exit
  - Document any integration issues or differences between contexts

- [x] 1.15 Checkpoint - End-to-end MVP working
  - Both standalone BRUTAL ImGui TestApp and game mod working with real shell
  - User has validated both deployment targets work with the same shared ImGui controller
  - Ready to add more terminal features

- [x] 2. Add basic escape sequence parsing and control characters
- [x] 2.1 Add basic control character handling
  - Handle backspace (BS), tab (HT), bell (BEL)
  - Add basic cursor movement for control characters
  - Implement simple tab stops (every 8 columns)
  - Add event emission for bell character
  - _Requirements: 10.3, 10.4, 10.5, 19.1, 19.2_

- [x] 2.2 Create escape sequence parser state machine
  - Create parser state enum (Ground, Escape, CsiEntry, CsiParam, etc.)
  - Implement basic state machine for escape sequence detection
  - Add escape sequence buffer for partial sequences
  - Handle ESC character and basic sequence detection
  - Specify behavior for C0 controls during escape parsing
    - Optionally allow BEL/BS/TAB/LF/CR while in ESC/CSI parsing (matching TS)
  - Specify termination and abort rules
    - Abort control strings on CAN (0x18) / SUB (0x1a)
    - Handle OSC terminators BEL and ST (ESC \\)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/Parser.ts state machine logic and ensure C# implementation matches or improves upon the TypeScript behavior for state transitions and sequence detection
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/Parser.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParserOptions.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParserHandlers.ts
  - _Requirements: 11.1, 12.1, 13.1_

- [x] 2.3 Add UTF-8 decoding support to parser
  - Implement UTF-8 multi-byte sequence detection
  - Add UTF-8 buffer for partial sequences across Write calls
  - Handle UTF-8 validation and error recovery
  - Integrate UTF-8 decoding with character processing
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/Parser.ts UTF-8 handling and ensure C# implementation provides equivalent or better UTF-8 processing capabilities
  - _Requirements: 9.3, 9.4_

- [x] 2.4 Write property test for UTF-8 character handling ✅
  - **Property 16: UTF-8 character handling**
  - **Validates: Requirements 9.3, 9.4**
  - **Status**: COMPLETED - All 5 property tests pass (100 iterations each)
  - **Coverage**: UTF-8 decoding, sequence splitting, wide characters, invalid byte recovery, mixed content
  - **Fixed**: Cursor reference bug in property tests causing false failures

- [x] 2.5 Implement CSI parameter parsing
  - Create CSI parameter parsing logic
  - Handle numeric parameters and separators (semicolon/colon)
  - Add parameter validation and bounds checking
  - Support private mode indicators (? prefix)
  - Support prefix/intermediate parsing
    - Prefix '>' and '?' where applicable
    - Intermediate characters (e.g. space for DECSCUSR, '!' for DECSTR, '"' for DECSCA)
  - Define defaulting rules
    - Empty params default to 0 or 1 depending on command
    - Treat trailing separators as an extra 0 param
  - **CRITICAL CODE ORGANIZATION**: Create dedicated CsiParser class
    - Extract CSI parsing logic from main Parser class into caTTY.Core/Parsing/CsiParser.cs
    - CsiParser should handle all CSI sequence parsing and parameter extraction
    - CsiParser should not exceed 200 lines (excluding comments)
    - Main Parser should delegate CSI parsing to CsiParser instance
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts parameter parsing logic and ensure C# implementation handles all parameter types and edge cases identically to the TypeScript version
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParseCsi.ts
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 2.5.1 Create dedicated UTF-8 decoder class
  - Extract UTF-8 decoding logic from main Parser class into caTTY.Core/Parsing/Utf8Decoder.cs
  - Create IUtf8Decoder interface for testability
  - Implement Utf8Decoder class with focused UTF-8 handling responsibilities
  - Utf8Decoder should handle multi-byte sequence detection, validation, and decoding
  - Utf8Decoder should not exceed 150 lines (excluding comments)
  - Main Parser should delegate UTF-8 processing to Utf8Decoder instance
  - Add comprehensive unit tests for Utf8Decoder in isolation
  - _Requirements: 9.3, 9.4_

- [x] 2.6 Add basic cursor movement CSI sequences
  - Implement cursor up (CSI A), down (CSI B) sequences
  - Add cursor forward (CSI C), backward (CSI D) sequences
  - Add cursor position (CSI H) sequence handling
  - Update cursor position with bounds checking
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts cursor movement handlers and ensure C# implementation provides identical cursor positioning behavior and bounds checking
  - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 2.7 Write property test for cursor movement sequences
  - **Property 13: Cursor movement sequences**
  - **Validates: Requirements 8.4, 11.1, 11.2, 11.3, 11.4, 11.5**

- [x] 2.8 Add basic screen clearing CSI sequences
  - Implement erase in display (CSI J) sequence handling
  - Add erase in line (CSI K) sequence handling
  - Implement clearing logic for different erase modes (0, 1, 2)
  - Update screen buffer with cleared cells
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts erase operations and ensure C# implementation provides identical clearing behavior for all erase modes
  - _Requirements: 11.6, 11.7_

- [x] 2.9 Write property test for screen clearing operations
  - **Property 19: Screen clearing operations**
  - **Validates: Requirements 11.6, 11.7**

- [x] 2.10 Integrate escape sequence parsing into terminal
  - Add parser to terminal emulator
  - Update Write method to detect and parse escape sequences
  - Handle partial sequences across multiple Write calls
  - Test escape sequences with shell commands
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/StatefulTerminal.ts parser integration and ensure C# implementation handles partial sequences and parser state management identically
  - _Requirements: 11.1, 12.1, 13.1_

- [x] 2.11 Add essential ESC (non-CSI) sequences
  - Implement save/restore cursor (ESC 7 / ESC 8)
  - Implement index and reverse index (ESC D / ESC M)
  - Implement next line (ESC E)
  - Implement horizontal tab set at cursor (ESC H)
  - Implement reset to initial state (ESC c)
  - Add character set designation parsing (ESC ( X, ESC ) X, ESC * X, ESC + X)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/esc.ts ESC sequence handlers and ensure C# implementation provides identical behavior for all ESC sequences
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/Parser.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/esc.ts
  - _Requirements: 10.1, 10.2, 24.1, 24.2_

- [x] 2.12 Add tab stop CSI sequences
  - Implement cursor forward tab (CSI I) and cursor backward tab (CSI Z)
  - Implement tab clear (CSI g) for clear-at-cursor and clear-all
  - Integrate with tab stop tracking in terminal state
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/tabStops.ts and handlers/csi.ts tab operations to ensure C# implementation provides identical tab stop behavior
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/tabStops.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts
  - _Requirements: 10.4, 11.1_

- [x] 2.13 Add device query sequences and responses (CSI)
  - Implement DA queries (CSI c and CSI > c) and emit appropriate responses
  - Implement DSR queries (CSI 5 n and CSI 6 n) and emit appropriate responses
  - Implement terminal size query (CSI 18 t) and emit appropriate response
  - Ensure responses are routed back to the shell via process stdin
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/responses.ts and handlers/csi.ts device query handling to ensure C# implementation generates identical response sequences
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/responses.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts
  - _Requirements: 11.1, 11.2, 27.1, 27.2_

- [x] 2.14 Add selective erase and character protection
  - Implement selective erase in display/line (CSI ? J / CSI ? K)
  - Implement DECSCA character protection (CSI Ps " q)
  - Ensure protected cells are preserved by selective erase operations
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts selective erase implementation to ensure C# provides identical character protection behavior
  - _Requirements: 11.6, 11.7_

- [x] 2.15 Add DCS and control-string handling
  - Extend parser to recognize DCS (ESC P ... ST) and emit parsed DCS messages
  - Implement DECRQSS (DCS $ q ... ST) minimal support for common requests (SGR "m", scroll region "r")
  - Ensure SOS/PM/APC control strings are safely skipped until ST terminator
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/dcs.ts and Parser.ts DCS handling to ensure C# implementation provides identical DCS processing and response generation
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/dcs.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/Parser.ts
  - _Requirements: 11.1, 13.1_

- [x] 2.16 Test and validate enhanced terminal functionality
  - **USER VALIDATION REQUIRED**: Test escape sequences work with shell
  - Verify cursor movement commands work (clear, cursor positioning)
  - Test with applications that use escape sequences
  - Validate both console app and game mod still work
  - Document any parsing issues found

- [x] 2.17 Checkpoint - Basic escape sequence parsing working
  - Terminal handles basic CSI sequences and control characters
  - Terminal responds to common device/status queries without hanging
  - Both deployment targets validated by user

- [x] 2.18 Decompose Parser class for better maintainability
  - **CRITICAL CODE ORGANIZATION**: Break down the main Parser class which has grown too large
  - Extract EscParser class into caTTY.Core/Parsing/EscParser.cs
    - Handle all ESC sequence parsing (save/restore cursor, character sets, etc.)
    - Create IEscParser interface for testability
    - EscParser should not exceed 200 lines (excluding comments)
  - Extract DcsParser class into caTTY.Core/Parsing/DcsParser.cs
    - Handle all DCS sequence parsing and device control
    - Create IDcsParser interface for testability
    - DcsParser should not exceed 150 lines (excluding comments)
  - Refactor main Parser class to coordinate between specialized parsers
    - Parser should act as state machine coordinator only
    - Parser should delegate to CsiParser, SgrParser, OscParser, EscParser, DcsParser, Utf8Decoder
    - Parser should not exceed 300 lines after refactoring (excluding comments)
  - Add comprehensive unit tests for each specialized parser in isolation
  - Ensure all existing functionality continues to work after decomposition
  - _Requirements: Code organization and maintainability_

- [x] 2.19 Create terminal state management classes
  - **CRITICAL CODE ORGANIZATION**: Break down terminal state management into focused managers
  - Create ScreenBufferManager class in caTTY.Core/Managers/ScreenBufferManager.cs
    - Handle all screen buffer operations (cell access, clearing, resizing)
    - Create IScreenBufferManager interface for testability
    - ScreenBufferManager should not exceed 300 lines (excluding comments)
  - Create CursorManager class in caTTY.Core/Managers/CursorManager.cs
    - Handle all cursor positioning, visibility, and movement operations
    - Create ICursorManager interface for testability
    - CursorManager should not exceed 200 lines (excluding comments)
  - Create ModeManager class in caTTY.Core/Managers/ModeManager.cs
    - Handle all terminal mode state tracking (auto-wrap, cursor keys, etc.)
    - Create IModeManager interface for testability
    - ModeManager should not exceed 250 lines (excluding comments)
  - Create AttributeManager class in caTTY.Core/Managers/AttributeManager.cs
    - Handle all SGR attribute state management and application
    - Create IAttributeManager interface for testability
    - AttributeManager should not exceed 200 lines (excluding comments)
  - Refactor TerminalEmulator to use these focused managers
  - Add comprehensive unit tests for each manager in isolation
  - _Requirements: Code organization and maintainability_

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

- [ ] 3.2 Implement SGR parameter parsing (basic colors and styles)
  - Create SgrParser class for parsing SGR parameters
  - Add support for both semicolon and colon separators
  - Parse basic text styles (bold, italic, underline, strikethrough)
  - Handle standard 8-color foreground/background (30-37, 40-47)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts SGR parsing logic to ensure C# implementation handles all SGR parameter types and separator formats identically
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts
  - _Requirements: 12.1, 12.2, 12.4, 12.5_

- [ ] 3.3 Add extended color parsing (256-color and RGB)
  - Implement 256-color parsing (38;5;n, 48;5;n)
  - Add 24-bit RGB color parsing (38;2;r;g;b, 48;2;r;g;b)
  - Handle colon-separated color formats (38:2:r:g:b)
  - Add bright color support (90-97, 100-107)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts extended color parsing to ensure C# implementation supports all color formats and edge cases identically
  - _Requirements: 12.1, 12.4_

- [ ] 3.4 Implement advanced SGR features
  - Add underline color support (58, 59)
  - Implement underline style subparameters (4:n)
  - Handle enhanced SGR modes (CSI > 4 ; n m)
  - Add private SGR modes (CSI ? 4 m)
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/ParseSgr.ts advanced SGR features to ensure C# implementation supports all advanced SGR modes and underline variants identically
  - _Requirements: 12.1, 12.2_

- [ ] 3.5 Create SGR state processor
  - Create SgrState class to track current attributes
  - Implement SGR message processing logic
  - Handle attribute reset and individual attribute clearing
  - Add inverse video processing
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/SgrStateProcessor.ts and SgrStyleManager.ts to ensure C# SGR state management provides identical attribute tracking and processing behavior
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/SgrStateProcessor.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/SgrStyleManager.ts
  - _Requirements: 12.2, 12.3_

- [ ] 3.6 Integrate SGR parsing into CSI parser
  - Add SGR sequence handling to CsiParser for 'm' command
  - Update terminal to track current SGR state
  - Apply attributes to characters written after SGR changes
  - Handle SGR reset (CSI 0 m) to restore defaults
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts SGR integration to ensure C# implementation applies SGR attributes to characters identically
  - _Requirements: 12.1, 12.2, 12.3_

- [ ] 3.7 Write property test for SGR parsing and application
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

- [ ] 4. Add scrolling, scrollback, and screen management
- [ ] 4.1 Create scrollback buffer infrastructure
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

- [ ] 4.2 Implement basic scrolling operations
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

- [ ] 5. Add alternate screen buffer and advanced terminal modes
- [ ] 5.1 Create alternate screen buffer infrastructure
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

- [ ] 5.2 Implement alternate screen isolation
  - Ensure alternate screen doesn't add to scrollback
  - Clear alternate buffer on activation
  - Handle buffer switching with proper state preservation
  - Maintain separate cursor positions per buffer
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/alternateScreen.ts isolation behavior to ensure C# implementation provides identical scrollback isolation and state preservation
  - _Requirements: 15.3, 15.5_

- [ ] 5.3 Add alternate screen control sequences
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
  - Add origin mode (DECOM) state tracking
  - Add UTF-8 mode (DECSET/DECRST 2027) state tracking
  - Add cursor style tracking (DECSCUSR)
  - Add save/restore private modes (CSI ? s / CSI ? r) state tracking
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts and cursor.ts mode management to ensure C# implementation provides identical terminal mode behavior and state tracking
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/cursor.ts
  - _Requirements: 20.1, 20.2, 20.3, 20.4_

- [ ] 5.7 Add cursor wrapping and line overflow handling
  - Implement auto-wrap behavior when cursor reaches right edge
  - Add line overflow handling based on auto-wrap mode
  - Update character writing to respect wrapping settings
  - Handle wide character wrapping correctly
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/bufferOps.ts and cursor.ts wrapping behavior to ensure C# implementation provides identical cursor wrapping and line overflow handling
  - _Requirements: 8.3, 9.5, 20.1_

- [ ] 5.8 Write property test for cursor wrapping behavior
  - **Property 12: Cursor wrapping behavior**
  - **Validates: Requirements 8.3**

- [ ] 5.9 Add bracketed paste mode support
  - Implement bracketed paste mode state tracking
  - Add paste sequence wrapping for bracketed paste
  - Handle mode switching sequences
  - Prepare for future paste integration
  - Define the exact DECSET/DECRST sequences
    - CSI ? 2004 h enable, CSI ? 2004 l disable
    - When enabled, wrap paste payload with ESC[200~ and ESC[201~
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/csi.ts bracketed paste mode handling to ensure C# implementation provides identical paste mode behavior
  - _Requirements: 20.5_

- [ ] 5.10 Write property test for cursor visibility tracking
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

- [ ] 6. Add OSC sequences and advanced features
- [ ] 6.1 Create OSC sequence parser infrastructure
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

- [ ] 6.2 Implement window title OSC sequences
  - Add OSC 0 and OSC 2 (set window title) sequence handling
  - Emit title change events with new title text
  - Add title state tracking in terminal
  - Handle empty titles and title reset
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts window title handling to ensure C# implementation provides identical title change behavior and event emission
  - TypeScript reference: catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts
  - _Requirements: 13.2_

- [ ] 6.3 Add clipboard OSC sequences
  - Add OSC 52 (clipboard) sequence handling
  - Emit clipboard events for game integration
  - Parse clipboard data and selection targets
  - Handle base64 encoded clipboard content
  - Define safety limits
    - Cap decoded clipboard size
    - Ignore invalid base64 payloads gracefully
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts clipboard handling to ensure C# implementation provides identical clipboard sequence processing and safety limits
  - _Requirements: 13.4_

- [ ] 6.4 Write property test for OSC parsing and event emission
  - **Property 23: OSC parsing and event emission**
  - **Validates: Requirements 13.1, 13.2, 13.4**

- [ ] 6.5 Implement hyperlink OSC sequences
  - Add OSC 8 (hyperlink) sequence parsing
  - Associate URLs with character ranges
  - Add hyperlink state to cell attributes
  - Handle hyperlink start/end sequences
  - Define association model
    - Track current hyperlink URL as state and apply to subsequent written cells
    - Clear hyperlink state on OSC 8 ;; ST
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts hyperlink handling to ensure C# implementation provides identical URL association and character range tracking
  - _Requirements: 13.3_

- [ ] 6.6 Write property test for OSC hyperlink association
  - **Property 24: OSC hyperlink association**
  - **Validates: Requirements 13.3**

- [ ] 6.7 Add unknown OSC sequence handling
  - Implement graceful handling of unknown OSC sequences
  - Log unknown sequences for debugging
  - Continue processing without errors
  - **Compare with TypeScript implementation**: Review catty-web/packages/terminal-emulation/src/terminal/stateful/handlers/osc.ts unknown sequence handling to ensure C# implementation provides identical graceful handling behavior
  - _Requirements: 13.5_

- [ ] 6.8 Write property test for unknown OSC sequence handling
  - **Property 25: Unknown OSC sequence handling**
  - **Validates: Requirements 13.5**

- [ ] 6.9 Add character set support
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

- [ ] 6.10 Test and validate advanced features
  - **USER VALIDATION REQUIRED**: Test OSC sequences and UTF-8 (including vim)
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

- [ ] 7.2 Add selection and copying support
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