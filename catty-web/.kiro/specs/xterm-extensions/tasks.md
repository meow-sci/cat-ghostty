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

- [ ] 14. Implement SGR styling system with CSS generation
  - Set up CSS-based styling system using xxh3 hashing for style management
  - Implement foreground and background color support as initial MVP
  - Create DOM manipulation system for dynamic style injection
  - _Requirements: 3.4, 4.1, 4.2_

- [ ] 14.1 Set up SGR styling infrastructure
  - Install xxh3-ts package for CSS hash generation
  - Create SgrStyleManager class for CSS generation and caching
  - Implement in-memory cache for known style blocks
  - _Requirements: 3.4, 4.1_

- [ ] 14.2 Implement CSS generation for SGR styles
  - Create generateCssForSgr function that converts SGR state to CSS strings
  - Implement xxh3 hashing of CSS strings for unique class names
  - Add CSS class generation with hash-based naming
  - _Requirements: 3.4, 4.1, 4.2_

- [ ] 14.3 Create DOM style management system
  - Implement dynamic <style> tag creation and management
  - Add in-memory cache to avoid DOM queries for existing styles
  - Create updateCellClasses function for applying styles to terminal cells
  - _Requirements: 4.1, 4.2_

- [ ] 14.4 Implement foreground color support
  - Add support for standard 16 ANSI colors (30-37, 90-97)
  - Implement 256-color palette support (38;5;n)
  - Add 24-bit RGB color support (38;2;r;g;b)
  - _Requirements: 3.4, 4.1_

- [ ] 14.5 Implement background color support
  - Add support for standard 16 ANSI background colors (40-47, 100-107)
  - Implement 256-color background palette support (48;5;n)
  - Add 24-bit RGB background color support (48;2;r;g;b)
  - _Requirements: 3.4, 4.2_

- [ ] 14.6 Integrate SGR styling with terminal rendering
  - Update TerminalController to use SgrStyleManager for cell styling
  - Modify cell rendering to apply generated CSS classes
  - Implement style reset functionality when SGR attributes change
  - _Requirements: 4.1, 4.2_

- [ ] 14.7 Write property test for SGR color consistency
  - **Property 16: SGR color application consistency**
  - **Validates: Requirements 3.4, 4.1**

- [ ] 14.8 Write property test for CSS generation determinism
  - **Property 17: CSS hash generation determinism**
  - **Validates: Requirements 4.1, 4.2**

- [ ] 15. Final Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.