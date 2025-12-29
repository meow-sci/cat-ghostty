# Requirements Document

## Introduction
C# terminal emulator translating TypeScript caTTY for KSA game integration. VT100/xterm-compatible with ImGui display, headless architecture.

## Glossary
- **Terminal_Emulator**: Terminal software processing sequences, maintaining screen state
- **KSA_Game_Engine**: Kitten Space Agency engine with GLFW, Vulkan, BRUTAL ImGui
- **ImGui_Controller**: Display controller bridging headless logic to ImGui
- **Screen_Buffer**: 2D array of terminal content and attributes
- **SGR/OSC/CSI**: ANSI sequences for styling/features/cursor control
- **Alternate_Screen**: Secondary buffer for fullscreen apps
- **Scrollback**: Historical lines scrolled off screen
- **Game_Mod**: DLL integrating with KSA engine
- **Headless_Core**: Pure C# logic without UI dependencies

## Requirements

### R1: KSA Game Integration
**User Story:** As a KSA player, I want terminal emulator in-game for command-line access.
**Acceptance Criteria:**
1. WHEN game mod loads THEN Terminal_Emulator SHALL initialize in KSA context
2. WHEN terminal opens/closes THEN SHALL display/hide ImGui window, maintain processes
3. WHEN game shuts down THEN SHALL dispose all resources/processes
4. WHEN terminal active THEN SHALL integrate with game input system

### R2: Headless Architecture
**User Story:** As a developer, I want headless C# architecture matching TypeScript for testable core.
**Acceptance Criteria:**
1. WHEN core implemented THEN Core_Library SHALL have no ImGui/game dependencies
2. WHEN processing input THEN Core_Library SHALL use pure C#
3. WHEN state changes THEN Core_Library SHALL notify via events/callbacks
4. WHEN ImGui controller implemented THEN SHALL depend only on Core + ImGui

### R3: TypeScript Compatibility
**User Story:** As a developer, I want C# mirroring TypeScript for consistent features/behaviors.
**Acceptance Criteria:**
1. WHEN processing sequences THEN C# Parser SHALL handle same escapes as TypeScript
2. WHEN screen/cursor/scrolling/alternate screen operations THEN C# SHALL match TypeScript behavior

### R4: Efficient Processing
**User Story:** As a developer, I want efficient C# byte processing with minimal allocation.
**Acceptance Criteria:**
1. WHEN processing input THEN SHALL use ReadOnlySpan&lt;byte&gt;
2. WHEN parsing escapes THEN SHALL minimize allocations using Span&lt;char&gt;
3. WHEN managing buffers THEN SHALL use efficient structures, ArrayPool&lt;T&gt;

### R5: Multi-Project Structure
**User Story:** As a developer, I want solution for standalone test app, game mod, ImGui playground.
**Acceptance Criteria:**
1. WHEN built THEN SHALL produce standalone console app, game mod DLL, ImGui playground
2. WHEN core built THEN SHALL create library with no external deps
3. WHEN tests run THEN SHALL execute against headless core

### R6: KSA DLL Integration
**User Story:** As a developer, I want KSA DLL integration for ImGui framework/graphics.
**Acceptance Criteria:**
1. WHEN ImGui controller built THEN SHALL reference KSA DLLs from standard path
2. WHEN rendering THEN SHALL use BRUTAL ImGui framework
3. WHEN mod loads THEN SHALL register with game mod system
4. WHEN processing input THEN SHALL integrate with the game's input handling system and avoid conflicts with non-terminal game input
5. WHEN managing resources THEN SHALL use the game's graphics context / memory lifecycle appropriately

### R7: Screen Buffer Management
**User Story:** As a user, I want configurable screen buffer with grid layout.
**Acceptance Criteria:**
1. WHEN initialized THEN SHALL create buffer with specified width/height (1x1 to 1000x1000)
2. WHEN dimensions change THEN SHALL resize, preserve content where possible
3. WHILE active THEN SHALL maintain cells with character + SGR attributes
4. WHEN reading a cell THEN SHALL expose character + fg/bg colors + style attributes (and hyperlink when present)

### R8: Cursor Management
**User Story:** As a user, I want cursor tracking for text input positioning.
**Acceptance Criteria:**
1. WHEN initialized THEN SHALL set cursor to row 0, column 0
2. WHEN character written THEN SHALL update cursor to next column
3. WHEN cursor at right edge THEN SHALL wrap if auto-wrap enabled
4. WHEN movement sequence received THEN SHALL update position per sequence
5. WHEN cursor visibility is toggled THEN SHALL track visibility state for rendering

### R9: Character Processing
**User Story:** As a user, I want printable character processing for text output.
**Acceptance Criteria:**
1. WHEN printable ASCII received THEN SHALL write to cursor position
2. WHEN character written THEN SHALL apply current SGR attributes
3. WHEN UTF-8 multi-byte received THEN SHALL decode/display correctly
4. WHEN wide character (CJK) received THEN SHALL occupy two cells

### R10: Control Characters
**User Story:** As a user, I want control character processing for cursor/formatting control.
**Acceptance Criteria:**
1. WHEN LF/CR/BS/HT/BEL received THEN SHALL implement: LF→next line; CR→column 0; BS→one column left (if possible); HT→next tab stop; BEL→emit bell event

### R11: CSI Sequences
**User Story:** As a user, I want CSI escape sequences for cursor/screen control.
**Acceptance Criteria:**
1. WHEN CSI A/B/C/D/H received THEN SHALL move cursor per parameters
2. WHEN CSI J/K received THEN SHALL clear screen/line portions per parameter
3. WHEN CSI S/T received THEN SHALL scroll screen up/down by specified lines

### R12: SGR Parsing
**User Story:** As a user, I want native C# SGR parsing for text styling.
**Acceptance Criteria:**
1. WHEN SGR sequence received THEN SHALL parse using C# native logic
2. WHEN SGR parsed THEN SHALL update current text attributes state
3. WHEN reset SGR (CSI 0 m) received THEN SHALL reset all attributes
4. WHEN color/style SGR received THEN SHALL apply fg/bg colors + bold/italic/underline (and other supported styles) to subsequent character writes

### R13: OSC Parsing
**User Story:** As a user, I want native C# OSC parsing for advanced features.
**Acceptance Criteria:**
1. WHEN OSC sequence received THEN SHALL parse using C# native logic
2. WHEN OSC 0/2 (title) received THEN SHALL emit title change event
3. WHEN OSC 8 (hyperlink) received THEN SHALL associate URL with characters
4. WHEN OSC 52 (clipboard) received THEN SHALL emit clipboard event
5. WHEN unknown OSC received THEN SHALL ignore it without error

### R14: Scrolling/Scrollback
**User Story:** As a user, I want scrolling/scrollback for reviewing output.
**Acceptance Criteria:**
1. WHEN content scrolls off top THEN SHALL add to scrollback buffer
2. WHEN scrollback exceeds max THEN SHALL remove oldest lines
3. WHEN terminal scrolled THEN SHALL maintain viewport offset with auto-scroll
4. WHEN scrollback is queried THEN SHALL provide access to historical lines (with correct line breaks and wide-char handling)

### R15: Alternate Screen
**User Story:** As a user, I want alternate screen for fullscreen apps (vim).
**Acceptance Criteria:**
1. WHEN alternate activated/deactivated THEN SHALL switch/restore buffers
2. WHILE in alternate mode THEN SHALL not add to scrollback
3. WHEN switching buffers THEN SHALL preserve cursor/attributes independently
4. WHEN alternate is activated THEN SHALL clear it to default state

### R16: Input Handling
**User Story:** As a player, I want ImGui input handling for keystroke conversion.
**Acceptance Criteria:**
1. WHEN key pressed in ImGui window THEN SHALL capture keyboard event
2. WHEN event captured THEN SHALL convert to terminal escapes using C#
3. WHEN special keys pressed THEN SHALL encode per terminal mode
4. WHEN sequences are encoded THEN SHALL send them to the headless terminal core for processing

### R17: ImGui Rendering
**User Story:** As a player, I want ImGui rendering for terminal output display.
**Acceptance Criteria:**
1. WHEN terminal state changes THEN SHALL update ImGui display
2. WHEN rendering screen THEN SHALL use ImGui text rendering with styling
3. WHEN rendering cells THEN SHALL apply ImGui colors for fg/bg/attributes
4. WHEN rendering wide characters THEN SHALL ensure correct visual width and selection/cursor alignment

### R18: Focus Management
**User Story:** As a player, I want ImGui focus management for proper input capture.
**Acceptance Criteria:**
1. WHEN terminal window clicked THEN SHALL focus terminal for input
2. WHEN window loses/gains focus THEN SHALL indicate state visually
3. WHILE focused THEN SHALL capture keyboard input in game context

### R19: Tab Stops
**User Story:** As a user, I want tab stop support for correct tab alignment.
**Acceptance Criteria:**
1. WHEN initialized THEN SHALL set default tab stops every 8 columns
2. WHEN tab received THEN SHALL move cursor to next tab stop
3. WHEN set/clear tab sequences received THEN SHALL modify tabs per parameter

### R20: Terminal Modes
**User Story:** As a user, I want terminal modes for application behavior control.
**Acceptance Criteria:**
1. WHEN auto-wrap enabled/disabled THEN SHALL wrap/keep cursor at edge
2. WHEN cursor visibility changed THEN SHALL update visibility state
3. WHEN app cursor keys enabled THEN SHALL encode arrows differently
4. WHEN bracketed paste enabled THEN SHALL wrap pasted content with escapes

### R21: Event Emission
**User Story:** As a developer, I want terminal events for external game actions.
**Acceptance Criteria:**
1. WHEN bell/title/clipboard/resize occurs THEN SHALL emit appropriate events

### R22: Character Insert/Delete
**User Story:** As a user, I want character insert/delete for line editing.
**Acceptance Criteria:**
1. WHEN insert/delete char/line sequences received THEN SHALL modify content appropriately

### R23: Clean API
**User Story:** As a developer, I want clean API for easy integration/testing.
**Acceptance Criteria:**
1. WHEN creating terminal THEN SHALL accept width/height/scrollback parameters
2. WHEN writing data THEN SHALL accept string or ReadOnlySpan&lt;byte&gt;
3. WHEN disposing THEN SHALL implement IDisposable, clean up resources

### R24: Character Sets
**User Story:** As a user, I want character set selection for line-drawing chars.
**Acceptance Criteria:**
1. WHEN charset designation/SI/SO received THEN SHALL track/switch charsets
2. WHEN DEC Special Graphics active THEN SHALL map chars to line-drawing

### R25: Selection/Copying
**User Story:** As a player, I want ImGui selection/copying for terminal content.
**Acceptance Criteria:**
1. WHEN user drags mouse THEN SHALL track selection range using ImGui input
2. WHEN selection active THEN SHALL highlight selected cells, extract text on copy

### R26: Standalone Test App
**User Story:** As a developer, I want standalone BRUTAL ImGui test app for development/debug.
**Acceptance Criteria:**
1. WHEN test app starts THEN SHALL initialize standalone BRUTAL ImGui with GLFW
2. WHEN displaying terminal THEN SHALL use same ImGui controller/rendering

### R27: Process Management
**User Story:** As a developer, I want process management for shell integration.
**Acceptance Criteria:**
1. WHEN terminal needs shell THEN SHALL spawn process using System.Diagnostics.Process
2. WHEN shell spawned THEN SHALL configure with terminal dimensions
3. WHEN shell outputs/receives THEN SHALL forward data bidirectionally

### R28: Bidirectional Data Flow
**User Story:** As a developer, I want bidirectional data flow between terminal/shell.
**Acceptance Criteria:**
1. WHEN terminal receives/sends input THEN SHALL forward to/from shell process
2. WHEN terminal resized THEN SHALL update shell process dimensions
3. WHEN shell exits THEN SHALL notify terminal, clean up resources

### R29: Resource Lifecycle
**User Story:** As a developer, I want proper resource lifecycle for cleanup.
**Acceptance Criteria:**
1. WHEN shell no longer needed THEN SHALL terminate process cleanly
2. WHEN terminal disposed THEN SHALL clean up all processes/resources
3. WHEN game mod unloaded THEN SHALL ensure all terminal resources disposed

### R30: Testing
**User Story:** As a developer, I want comprehensive testing for reliability/correctness.
**Acceptance Criteria:**
1. WHEN core tested THEN SHALL include unit tests for all major components
2. WHEN behavior validated THEN SHALL include property tests using FsCheck.NUnit
3. WHEN escape parsing tested THEN SHALL verify TypeScript compatibility

### R31: ImGui Playground
**User Story:** As a developer, I want ImGui playground for rendering experiments.
**Acceptance Criteria:**
1. WHEN playground starts THEN SHALL initialize standalone ImGui using KSA DLLs
2. WHEN experimenting THEN SHALL test colors/styling/grid/cursor approaches independently
3. WHEN playground runs THEN SHALL NOT depend on terminal emulator core logic

### R32: Font Configuration
**User Story:** As a developer, I want configurable fonts for different deployment contexts.
**Acceptance Criteria:**
1. THE TerminalController SHALL accept font config specifying family/styles at init
2. THE Controller SHALL support separate names for Regular/Bold/Italic/BoldItalic
3. THE Controller SHALL validate fonts available, derive character metrics

### R33: Context-Aware Font Defaults
**User Story:** As a developer, I want context-aware font defaults for TestApp/GameMod.
**Acceptance Criteria:**
1. WHEN running in TestApp/GameMod THEN SHALL use appropriate defaults
2. THE Controller SHALL detect execution context automatically
3. THE Controller SHALL maintain consistent rendering across contexts

### R34: Runtime Font Updates
**User Story:** As a developer, I want runtime font config updates without restart.
**Acceptance Criteria:**
1. THE TerminalController SHALL provide methods to update font config at runtime
2. WHEN font config changed THEN SHALL immediately apply new fonts to rendering
3. THE Controller SHALL recalculate character metrics, maintain cursor accuracy

### R35: Debug Tracing
**User Story:** As a developer, I want optional debug tracing for terminal analysis.
**Acceptance Criteria:**
1. THE Core SHALL provide SQLite-based tracing system for escape sequences and printable text
2. WHEN tracing disabled (default) THEN SHALL have minimal performance overhead (~1-2ns per call)
3. WHEN tracing enabled THEN SHALL log terminal data to SQLite database with timestamps
4. THE tracing system SHALL fail gracefully without breaking terminal functionality