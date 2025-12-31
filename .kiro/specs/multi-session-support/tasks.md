# Implementation Plan: Multi-Session Support

## Overview

This implementation plan transforms the caTTY terminal from single-session to multi-session support by introducing a session management layer. The approach maintains the existing headless terminal architecture while adding session orchestration, state isolation, and resource management. Implementation follows an incremental approach, building core session infrastructure first, then integrating with the UI layer, and finally adding advanced features like persistence and error handling.

## Tasks

- [x] 1. Create core session infrastructure
  - Create TerminalSession class with session state management
  - Create SessionManager class for session lifecycle coordination
  - Create event argument classes for session events
  - _Requirements: 1.1, 1.2, 1.3, 1.5_

- [x] 1.1 Write property test for session lifecycle management
  - **Property 1: Session Lifecycle Management**
  - **Validates: Requirements 1.1, 1.2, 1.3, 1.5, 2.1, 2.4**

- [x] 2. Implement session creation and initialization
  - [x] 2.1 Implement SessionManager.CreateSessionAsync method
    - Initialize new TerminalEmulator and ProcessManager instances
    - Assign unique session IDs and generate session titles
    - Handle session creation failures gracefully
    - _Requirements: 2.1, 2.2, 2.3_

  - [x] 2.2 Write property test for session creation and initialization
    - **Property 2: Session Creation and Initialization**
    - **Validates: Requirements 2.2, 2.3, 2.5**

  - [x] 2.3 Implement automatic shell process startup for new sessions
    - Start shell process automatically after session creation
    - Make newly created session the active session
    - _Requirements: 2.4, 2.5_

- [x] 3. Implement session switching and management
  - [x] 3.1 Implement SessionManager.SwitchToSession method
    - Deactivate current session and activate target session
    - Update active session tracking
    - Handle invalid session ID requests
    - _Requirements: 3.2, 4.3_

  - [x] 3.2 Write property test for session switching behavior
    - **Property 4: Session Switching Behavior**
    - **Validates: Requirements 3.2, 4.3**

  - [x] 3.3 Implement next/previous session navigation
    - Add SwitchToNextSession and SwitchToPreviousSession methods
    - Handle tab order management
    - _Requirements: 12.3_

- [x] 4. Implement session closure and cleanup
  - [x] 4.1 Implement SessionManager.CloseSessionAsync method
    - Terminate shell processes and dispose resources
    - Handle last session protection logic
    - Update active session when closing current session
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [x] 4.2 Write property test for resource cleanup on session closure
    - **Property 5: Resource Cleanup on Session Closure**
    - **Validates: Requirements 1.4, 4.1, 4.2, 10.1, 10.2**

  - [x] 4.3 Write property test for last session protection
    - **Property 6: Last Session Protection**
    - **Validates: Requirements 4.4, 4.5**

- [ ] 5. Checkpoint - Ensure core session management tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 6. Integrate SessionManager with TerminalController
  - [x] 6.1 Modify TerminalController to use SessionManager
    - Replace single terminal/process with SessionManager instance
    - Update constructor to initialize SessionManager
    - Wire up session manager events
    - _Requirements: 1.1, 1.5_

  - [x] 6.2 Update TerminalController.RenderTabArea method
    - Display tabs for all active sessions
    - Show active session with different styling
    - Handle tab click events for session switching
    - Add context menu for tab operations
    - _Requirements: 3.1, 3.2, 3.4_

  - [x] 6.3 Write property test for tab management consistency
    - **Property 3: Tab Management Consistency**
    - **Validates: Requirements 3.1, 3.4**

  - [x] 6.4 Update TerminalController.RenderTerminalCanvas method
    - Render terminal content for active session only
    - Handle case when no sessions exist
    - Route terminal resize to active session
    - _Requirements: 5.1, 8.1_

- [x] 7. Implement input routing and state isolation
  - [x] 7.1 Update input handling to route to active session
    - NOTE this might already be working, check implementation
    - Route keyboard input to active session ProcessManager
    - Route mouse events to active session Terminal
    - Handle mouse wheel scrolling for active session
    - _Requirements: 8.1, 8.2, 8.4, 8.5_

  - [x] 7.2 Write property test for input routing to active session
    - **Property 8: Input Routing to Active Session**
    - **Validates: Requirements 5.5, 8.1, 8.2, 8.4, 8.5**

  - [x] 7.3 Implement session state isolation
    - NOTE this might already be working, check implementation
    - Ensure terminal buffer, cursor, and scrollback are preserved per session
    - Verify process state isolation between sessions
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [x] 7.4 Write property test for session state isolation
    - **Property 7: Session State Isolation**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**

- [x] 8. Implement global settings management
  - [x] 8.1 Update font configuration handling for multiple sessions
    - Apply font changes to all sessions simultaneously
    - Trigger terminal resize for all sessions on font size changes
    - Share font resources across sessions
    - _Requirements: 6.1, 6.3, 6.5, 10.3_

  - [x] 8.2 Write property test for global settings propagation
    - **Property 9: Global Settings Propagation**
    - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

  - [x] 8.3 Implement session-specific settings management
    - Maintain separate TerminalSettings for each session
    - Preserve session-specific settings during session switches
    - Update settings area to show active session information
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [x] 8.4 Write property test for session-specific settings isolation
    - **Property 10: Session-Specific Settings Isolation**
    - **Validates: Requirements 7.1, 7.2, 7.4, 7.5**

- [ ] 9. Checkpoint - Ensure session integration tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. Implement menu integration and keyboard shortcuts
  - [x] 10.1 Add session management menu items
    - Add "New Terminal" menu item to File menu
    - Add "Close Terminal" menu item to File menu
    - Add "Next Terminal" and "Previous Terminal" menu items
    - Add "Sessions" menu which contains a entry for each terminal and a checkmark for the current one, allow clicking to switch sessions (use same backing code as the tabs do)
    - _Requirements: 12.1, 12.2, 12.3_

  - [x] 10.2 Implement dynamic menu state management
    - Disable "Close Terminal" when only one session exists
    - Disable navigation items when only one session exists
    - _Requirements: 12.5_

- [ ] 11. Implement process lifecycle and error handling
  - [ ] 11.1 Handle session process exit events
    - Update session state when process exits
    - Display exit codes in tab labels or settings area
    - Allow restarting terminated sessions
    - _Requirements: 9.1, 9.2, 9.3_

  - [ ] 11.2 Write property test for process lifecycle event handling
    - Ensure property tests only have max 2 iterations as real terminals are slow
    - **Property 11: Process Lifecycle Event Handling**
    - **Validates: Requirements 9.1, 9.2**

  - [ ] 11.3 Implement comprehensive error handling
    - Handle session creation failures gracefully
    - Implement resource cleanup error handling
    - Add logging for session lifecycle events
    - _Requirements: 9.4, 9.5_

  - [ ] 11.4 Write property test for session recovery and error handling
    - Ensure property tests only have max 2 iterations as real terminals are slow
    - **Property 12: Session Recovery and Error Handling**
    - **Validates: Requirements 9.3, 9.4, 11.5**

- [ ] 12. Implement resource management and limits
  - [ ] 12.1 Add session count limits and monitoring
    - Implement maximum session limit (default 20)
    - Provide memory usage information
    - Prevent resource exhaustion
    - _Requirements: 10.4, 10.5_

  - [ ] 12.2 Write property test for resource limits and monitoring
    - **Property 13: Resource Limits and Monitoring**
    - **Validates: Requirements 10.4, 10.5**

  - [ ] 12.3 Optimize resource sharing across sessions
    - Share font resources across all sessions
    - Implement efficient resource cleanup
    - _Requirements: 10.3_

- [ ] 13. Implement session persistence
  - [ ] 13.1 Create SessionPersistenceManager class
    - Implement session save/restore functionality
    - Handle session titles and working directories
    - Create JSON-based persistence format
    - _Requirements: 11.1, 11.2_

  - [ ] 13.2 Integrate persistence with SessionManager
    - Save sessions on application close
    - Restore sessions on application start
    - Restore previously active session
    - Handle restoration failures gracefully
    - _Requirements: 11.3, 11.4, 11.5_

  - [ ] 13.3 Write property test for session persistence round-trip
    - **Property 14: Session Persistence Round-Trip**
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.4**

- [ ] 14. Final integration and testing
  - [ ] 14.1 Add comprehensive integration tests
    - Test complete session lifecycle with UI interactions
    - Test session switching with active terminal operations
    - Test error recovery scenarios
    - _Requirements: All requirements_

  - [ ] 14.2 Write unit tests for edge cases
    - Test session creation with invalid parameters
    - Test concurrent session operations
    - Test UI responsiveness with maximum sessions

- [ ] 15. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- Implementation maintains backward compatibility with existing single-session usage
- Global settings (fonts, themes) remain shared while session state is isolated
- Resource management prevents memory leaks and handles cleanup properly

### Quiet Operation Requirements
- **Happy Path Silence**: Normal session operations (create, switch, close) must produce no stdout/stderr output
- **Test Silence**: All tests must run quietly with `QuietOnSuccess = true` for property tests
- **Error Handling**: Use event notifications instead of console logging for error reporting
- **Debug Output**: Only produce output when explicitly enabled through configuration or debug modes

### Real Shell Test Guidelines
- **Limited Iterations**: Tests that launch real shells must use `MaxTest = 2` for property tests
- **Validation Process**: Run test to confirm it works, then mark with `[Ignore]` attribute
- **Ignore Format**: Use `[Ignore("Real shell test - validated and disabled for CI")]`
- **Mock Alternatives**: Create equivalent tests using mocked ProcessManager for regular test runs
- **CI Compatibility**: Real shell tests should not run in continuous integration to avoid environment dependencies