# Implementation Plan

- [x] 1. Set up xterm extension foundation and core interfaces
  - Create new message types for xterm-specific sequences in TerminalEmulationTypes.ts
  - Extend ParserHandlers interface to support new message types
  - Set up testing framework configuration for property-based testing with fast-check
  - _Requirements: 9.1, 9.2, 10.1_

- [x] 1.1 Define xterm message type interfaces
  - Write TypeScript interfaces for OSC title management messages
  - Write TypeScript interfaces for DEC private mode messages
  - Write TypeScript interfaces for device query/response messages
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 3.2_

- [x] 1.2 Write property test for message type validation
  - **Property 15: State integrity during operations**
  - **Validates: Requirements 10.4**

- [x] 1.3 Extend ParserHandlers interface
  - Add handler methods for new OSC message types
  - Add handler methods for extended CSI sequences
  - Add handler methods for device query responses
  - _Requirements: 9.2, 10.1_

- [x] 2. Implement OSC sequence parsing for window management
  - Extend OSC parser to handle title management sequences (OSC 0, 1, 2, 21)
  - Add validation for OSC parameter parsing
  - Implement UTF-8 handling for title strings
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 2.1 Parse OSC title setting sequences
  - Implement parsing for OSC 0 (set window title and icon name)
  - Implement parsing for OSC 1 (set icon name)
  - Implement parsing for OSC 2 (set window title)
  - _Requirements: 1.1, 1.2, 1.3_

- [x] 2.2 Write property test for OSC title parsing
  - **Property 1: OSC title setting consistency**
  - **Validates: Requirements 1.1**

- [x] 2.3 Write property test for OSC icon name isolation
  - **Property 2: OSC icon name isolation**
  - **Validates: Requirements 1.2**

- [x] 2.4 Write property test for OSC window title isolation
  - **Property 3: OSC window title isolation**
  - **Validates: Requirements 1.3**

- [x] 2.5 Parse OSC title query sequences
  - Implement parsing for OSC 21 (query window title)
  - Add response formatting for title queries
  - _Requirements: 1.4_

- [x] 2.6 Write property test for title query round-trip
  - **Property 4: Title query round-trip**
  - **Validates: Requirements 1.4**

- [x] 3. Implement DEC private mode parsing for cursor management
  - Extend CSI parser to handle DECSET/DECRST sequences
  - Add support for cursor visibility modes (25)
  - Add support for application cursor key mode (1)
  - _Requirements: 3.1, 3.2, 4.1, 4.2_

- [x] 3.1 Parse DEC private mode sequences
  - Implement DECSET parsing for mode activation
  - Implement DECRST parsing for mode deactivation
  - Add validation for mode number ranges
  - _Requirements: 3.1, 3.2, 4.1, 4.2_

- [x] 3.2 Write property test for application cursor key mode
  - **Property 8: Application cursor key mode**
  - **Validates: Requirements 3.3**

- [x] 3.3 Parse DECSCUSR cursor style sequences
  - Implement parsing for cursor style control (CSI Ps SP q)
  - Add validation for cursor style parameters
  - _Requirements: 4.3_

- [x] 4. Implement StatefulTerminal extensions for window and cursor state
  - Add window properties state management (title, icon name)
  - Add cursor state management (visibility, style, application mode)
  - Implement cursor save/restore with extended state
  - _Requirements: 1.1, 1.2, 1.3, 3.4, 4.1, 4.2, 4.3_

- [x] 4.1 Add window properties state
  - Create WindowProperties interface and state management
  - Implement title and icon name storage and retrieval
  - Add methods for querying current window properties
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 4.2 Add enhanced cursor state management
  - Extend cursor state to include visibility and style
  - Implement application cursor key mode tracking
  - Add cursor state validation and bounds checking
  - _Requirements: 3.1, 3.2, 4.1, 4.2, 4.3_

- [x] 4.3 Write property test for cursor state round-trip
  - **Property 9: Cursor state round-trip**
  - **Validates: Requirements 3.4**

- [x] 4.4 Implement message handlers in StatefulTerminal
  - Add handleOscTitle method for window property updates
  - Add handleDecMode method for cursor mode changes
  - Add handleCursorStyle method for DECSCUSR processing
  - _Requirements: 1.1, 1.2, 1.3, 3.1, 3.2, 4.1, 4.2, 4.3_

- [x] 5. Checkpoint - Ensure all tests pass for window and cursor management
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Implement alternate screen buffer support
  - Create AlternateScreenManager class for buffer management
  - Add screen buffer switching logic (DECSET/DECRST 47, 1047, 1049)
  - Implement buffer content preservation during switches
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

- [x] 6.1 Create alternate screen buffer data structures
  - Implement ScreenBuffer interface and management
  - Create AlternateScreenManager class
  - Add buffer switching state tracking
  - _Requirements: 2.1, 2.2, 2.7_

- [x] 6.2 Write property test for alternate screen buffer switching
  - **Property 5: Alternate screen buffer switching**
  - **Validates: Requirements 2.1**

- [x] 6.3 Write property test for screen buffer round-trip
  - **Property 6: Screen buffer round-trip**
  - **Validates: Requirements 2.2**

- [x] 6.4 Write property test for buffer content preservation
  - **Property 7: Buffer content preservation**
  - **Validates: Requirements 2.7**

- [x] 6.5 Implement buffer switching logic
  - Add DECSET 47 handler for basic alternate screen
  - Add DECSET 1047 handler with cursor save/restore
  - Add DECSET 1049 handler with cursor save and screen clear
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6_

- [x] 6.6 Integrate alternate screen with existing terminal operations
  - Update cursor movement to work with current buffer
  - Update text output to write to current buffer
  - Update screen clearing to affect current buffer only
  - _Requirements: 2.7_

- [x] 7. Implement TerminalController extensions for UI integration
  - Add window title management to TerminalController
  - Implement cursor appearance updates in display layer
  - Add keyboard input handling for application cursor keys
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 3.3, 4.1, 4.2, 4.3_

- [x] 7.1 Add window management to TerminalController
  - Implement setTitle and setIconName methods
  - Add DOM title updates when window properties change
  - Implement title query response generation
  - _Requirements: 1.1, 1.2, 1.3, 1.4_

- [x] 7.2 Add cursor appearance management
  - Update cursor rendering to use style information
  - Implement cursor visibility toggling in display
  - Add cursor style CSS classes for different appearances
  - _Requirements: 4.1, 4.2, 4.3_

- [x] 7.3 Implement application cursor key handling
  - Modify keyboard input encoding for application mode
  - Add SS3 sequence generation for arrow keys
  - Update key event handlers to check cursor key mode
  - _Requirements: 3.1, 3.2, 3.3_

- [ ] 8. Implement device query and response system
  - Add device attributes query parsing and response
  - Implement cursor position report functionality
  - Add terminal size query support
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 8.1 Parse device query sequences
  - Implement Device Attributes (DA1/DA2) parsing
  - Add Cursor Position Report (CPR) request parsing
  - Implement terminal size query parsing
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 8.2 Write property test for cursor position query round-trip
  - **Property 11: Cursor position query round-trip**
  - **Validates: Requirements 6.2**

- [x] 8.3 Implement query response generation
  - Add device attributes response formatting
  - Implement cursor position response generation
  - Add terminal size response formatting
  - _Requirements: 6.1, 6.2, 6.3, 6.5_

- [x] 8.4 Integrate query responses with TerminalController
  - Add response transmission to application
  - Implement response queuing for multiple queries
  - Add error handling for malformed queries
  - _Requirements: 6.4, 6.5_

- [ ] 9. Implement scroll region management
  - Add scroll region parsing and validation
  - Implement region-constrained scrolling behavior
  - Add scroll region state management
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 9.1 Parse scroll region control sequences
  - Implement DECSTBM (Set Top and Bottom Margins) parsing
  - Add scroll region parameter validation
  - Implement scroll region reset functionality
  - _Requirements: 7.1, 7.4, 7.5_

- [ ] 9.2 Write property test for scroll region containment
  - **Property 12: Scroll region containment**
  - **Validates: Requirements 7.1**

- [ ] 9.3 Implement region-constrained scrolling
  - Update scrolling logic to respect region boundaries
  - Modify text output to handle region scrolling
  - Add cursor movement validation within regions
  - _Requirements: 7.1, 7.2, 7.3_

- [ ] 10. Implement basic mouse reporting foundation
  - Add mouse reporting mode state management
  - Implement basic mouse event capture in TerminalController
  - Add mouse event sequence generation
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [ ] 10.1 Parse mouse reporting mode sequences
  - Implement DECSET 1000 (basic mouse reporting) parsing
  - Add DECSET 1002 (button event tracking) parsing
  - Add DECSET 1003 (any event tracking) parsing
  - _Requirements: 5.1, 5.2, 5.3_

- [ ] 10.2 Write property test for mouse event reporting
  - **Property 10: Mouse event reporting**
  - **Validates: Requirements 5.4**

- [ ] 10.3 Implement mouse event capture
  - Add mouse event listeners to TerminalController
  - Implement coordinate translation to terminal coordinates
  - Add mouse event filtering based on reporting mode
  - _Requirements: 5.4, 5.5, 5.6_

- [ ] 10.4 Generate mouse event sequences
  - Implement mouse report sequence formatting
  - Add coordinate clamping for boundary conditions
  - Implement button state tracking for event generation
  - _Requirements: 5.4, 5.6_

- [x] 11. Implement UTF-8 and character set support
  - Add UTF-8 mode state management
  - Implement character set designation parsing
  - Add multi-byte character sequence handling
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [x] 11.1 Parse character set control sequences
  - Implement character set designation sequences
  - Add UTF-8 mode enable/disable parsing
  - Implement character set query sequences
  - _Requirements: 8.1, 8.5_

- [x] 11.2 Write property test for UTF-8 processing
  - **Property 13: UTF-8 processing correctness**
  - **Validates: Requirements 8.2**

- [x] 11.3 Implement multi-byte character handling
  - Add UTF-8 sequence validation and decoding
  - Implement character set switching logic
  - Add graceful handling of invalid sequences
  - _Requirements: 8.2, 8.3, 8.4_

- [ ] 12. Add comprehensive error handling and validation
  - Implement robust error handling for all new sequences
  - Add parameter validation for all xterm extensions
  - Implement graceful degradation for unsupported features
  - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_

- [ ] 12.1 Add sequence validation
  - Implement parameter range checking for all sequences
  - Add malformed sequence detection and handling
  - Implement timeout handling for incomplete sequences
  - _Requirements: 10.1, 10.3_

- [ ] 12.2 Add state consistency validation
  - Implement state integrity checks during transitions
  - Add bounds checking for all coordinate operations
  - Implement resource limit enforcement
  - _Requirements: 10.2, 10.3_

- [ ] 12.3 Write property test for backward compatibility
  - **Property 14: Backward compatibility preservation**
  - **Validates: Requirements 9.4**

- [ ] 13. Final integration and testing
  - Integrate all xterm extensions with existing codebase
  - Run comprehensive test suite including property-based tests
  - Validate performance impact and optimize if needed
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 13.1 Complete integration testing
  - Test all xterm extensions with existing terminal functionality
  - Verify UI updates work correctly with new features
  - Test interaction between different xterm features
  - _Requirements: 9.1, 9.2, 9.3, 9.4_

- [ ] 13.2 Performance validation and optimization
  - Measure parsing performance impact of new sequences
  - Validate memory usage with alternate screen buffers
  - Optimize critical paths if performance degradation detected
  - _Requirements: 9.5_

- [x] 14. Implement terminal theme system and SGR styling with CSS generation
  - Create terminal theme system with CSS variable-based color management
  - Set up CSS-based SGR styling system using xxh3 hashing for style management
  - Implement foreground and background color support as initial MVP
  - Create DOM manipulation system for dynamic style injection
  - _Requirements: 3.4, 4.1, 4.2_

- [x] 14.1 Create terminal theme system foundation
  - Define TerminalTheme and TerminalColorPalette interfaces
  - Create ThemeManager class for theme management and CSS variable generation
  - Implement default dark theme with standard terminal colors
  - Add theme application functionality that injects CSS variables into DOM
  - _Requirements: 3.4, 4.1_

- [x] 14.2 Set up SGR styling infrastructure
  - Install xxh3-ts package for CSS hash generation
  - Create SgrStyleManager class for CSS generation and caching
  - Implement in-memory cache for known style blocks
  - Add color resolution system that maps ANSI colors to CSS variables
  - _Requirements: 3.4, 4.1_

- [x] 14.3 Implement CSS generation for SGR styles
  - Create generateCssForSgr function that converts SGR state to CSS strings
  - Implement xxh3 hashing of CSS strings for unique class names
  - Add CSS class generation with hash-based naming
  - Integrate theme-aware color resolution for standard ANSI colors
  - _Requirements: 3.4, 4.1, 4.2_

- [x] 14.4 Create DOM style management system
  - Implement dynamic <style> tag creation and management
  - Add in-memory cache to avoid DOM queries for existing styles
  - Create updateCellClasses function for applying styles to terminal cells
  - Add CSS variable injection system for theme application
  - _Requirements: 4.1, 4.2_

- [x] 14.5 Implement foreground color support
  - Add support for standard 16 ANSI colors (30-37, 90-97) using CSS variables
  - Implement 256-color palette support (38;5;n) with direct color values
  - Add 24-bit RGB color support (38;2;r;g;b) with direct color values
  - Create color resolution logic that chooses between CSS variables and direct values
  - _Requirements: 3.4, 4.1_

- [x] 14.6 Implement background color support
  - Add support for standard 16 ANSI background colors (40-47, 100-107) using CSS variables
  - Implement 256-color background palette support (48;5;n) with direct color values
  - Add 24-bit RGB background color support (48;2;r;g;b) with direct color values
  - Ensure background colors work consistently with theme system
  - _Requirements: 3.4, 4.2_

- [x] 14.7 Integrate SGR styling with terminal rendering
  - Update TerminalController to use SgrStyleManager for cell styling
  - Modify cell rendering to apply generated CSS classes from SGR state
  - Connect ScreenCell sgrState to actual CSS class application in repaint method
  - Implement style reset functionality when SGR attributes change
  - Add theme switching capability that updates existing styled cells
  - _Requirements: 4.1, 4.2_

- [x] 14.11 Connect SGR styling to TerminalController cell rendering
  - Import SgrStyleManager into TerminalController
  - Create SgrStyleManager instance in TerminalController constructor
  - Update repaint method to check each cell's sgrState property
  - Apply SGR CSS classes to terminal cell spans using SgrStyleManager.getStyleClass()
  - Ensure cells without SGR state use default styling
  - _Requirements: 4.1, 4.2_

- [x] 14.12 Initialize terminal theme system in TerminalController
  - Import ThemeManager and apply default dark theme on initialization
  - Ensure CSS variables for terminal colors are injected into DOM
  - Verify theme-aware color resolution works with SGR styling
  - Add theme switching capability for future extensibility
  - _Requirements: 3.4, 4.1_

- [x] 14.13 Test SGR styling integration end-to-end
  - Create test that sends SGR sequences through terminal and verifies CSS classes are applied
  - Test foreground colors (standard 16 colors, 256-color palette, 24-bit RGB)
  - Test background colors (standard 16 colors, 256-color palette, 24-bit RGB)
  - Test text styling (bold, italic, underline, etc.) appears correctly in DOM
  - Verify theme color resolution works with CSS variables
  - _Requirements: 3.4, 4.1, 4.2_

- [x] 14.8 Write property test for SGR color consistency
  - **Property 16: SGR color application consistency**
  - **Validates: Requirements 3.4, 4.1**

- [x] 14.9 Write property test for CSS generation determinism
  - **Property 17: CSS hash generation determinism**
  - **Validates: Requirements 4.1, 4.2**

- [x] 14.10 Write property test for theme color resolution
  - **Property 18: Theme color resolution consistency**
  - **Validates: Requirements 3.4, 4.1**

- [ ] 15. Implement missing vi-specific terminal sequences
  - Add support for SGR sequences causing vi visual artifacts
  - Implement window manipulation sequences for terminal resizing
  - Add support for additional SGR modes and color queries
  - _Requirements: 1.1, 3.4, 4.1, 6.1_

- [x] 15.1 Implement SGR sequence extensions for vi compatibility
  - Add support for SGR 2 (dim/faint text) in ParseSgr.ts - ALREADY IMPLEMENTED
  - Add support for SGR 27 (reverse video off) in ParseSgr.ts - ALREADY IMPLEMENTED
  - Add support for SGR 23 (not italic) in ParseSgr.ts - ALREADY IMPLEMENTED
  - Add support for SGR 29 (not strikethrough) in ParseSgr.ts - ALREADY IMPLEMENTED
  - Add support for SGR 0%m (reset specific attributes) parsing - COMPLETED
  - Add support for SGR ?4m (unknown mode) with graceful handling - COMPLETED
  - _Requirements: 3.4, 4.1_

- [x] 15.2 Implement window manipulation CSI sequences
  - Add parsing for CSI 22;2t (push window title to stack) - COMPLETED (parsed, acknowledged)
  - Add parsing for CSI 22;1t (push icon name to stack) - COMPLETED (parsed, acknowledged)
  - Add parsing for CSI 23;2t (pop window title from stack) - COMPLETED (parsed, acknowledged)
  - Add parsing for CSI 23;1t (pop icon name from stack) - COMPLETED (parsed, acknowledged)
  - Implement title/icon name stack management in StatefulTerminal - NOT IMPLEMENTED (gracefully ignored)
  - _Requirements: 1.1, 1.2, 1.3, 6.3_

- [x] 15.3 Implement OSC color query sequences
  - Add parsing for OSC 11;? (query default background color) - COMPLETED
  - Add parsing for OSC 10;? (query default foreground color) - COMPLETED
  - Implement color query response generation with current theme colors - COMPLETED
  - Add BEL termination support for OSC sequences - COMPLETED
  - _Requirements: 1.4, 6.1, 6.5_

- [x] 15.4 Add enhanced SGR >4;2m sequence support
  - Research and implement SGR >4;2m sequence (likely cursor or display mode) - COMPLETED
  - Add parsing support in ParseSgr.ts with parameter validation - COMPLETED
  - Implement state management for this mode in StatefulTerminal - COMPLETED
  - Add graceful fallback if mode is not fully supported - COMPLETED
  - _Requirements: 3.4, 4.1_

- [ ] 15.5 Write property tests for vi-specific sequences
  - **Property 19: SGR attribute reset consistency**
  - Test that SGR 27, 23, 29 properly reset their corresponding attributes
  - **Validates: Requirements 3.4**

- [ ] 15.6 Write property tests for window manipulation
  - **Property 20: Window title stack operations**
  - Test that push/pop operations maintain title stack integrity
  - **Validates: Requirements 1.1, 6.3**

- [ ] 15.7 Write property tests for color queries
  - **Property 21: Color query response consistency**
  - Test that color queries return values consistent with current theme
  - **Validates: Requirements 1.4, 6.1**

- [ ] 15.8 Integrate vi-specific sequences with existing systems
  - Update SgrStyleManager to handle new SGR modes (dim, reverse-off, etc.)
  - Ensure window manipulation sequences work with existing title management
  - Test color queries return proper theme-aware color values
  - Add comprehensive error handling for malformed vi sequences
  - _Requirements: 3.4, 4.1, 1.1, 6.1_

- [ ] 16. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.