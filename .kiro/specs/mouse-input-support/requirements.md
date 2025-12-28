# Mouse Input Support Requirements

## Introduction

This document specifies the requirements for implementing comprehensive mouse input support in the catty-ksa C# terminal emulator. The implementation must achieve feature parity with the TypeScript reference implementation, supporting mouse tracking modes, coordinate conversion, selection functionality, and terminal escape sequence generation.

## Glossary

- **Mouse_Tracking_Mode**: Terminal mode that determines which mouse events are reported to applications (off, click, button, any)
- **Mouse_Encoding**: Format for encoding mouse events in terminal escape sequences (standard or SGR)
- **Cell_Coordinates**: Terminal grid position (row, column) corresponding to pixel coordinates
- **Selection_Range**: Contiguous text region selected by mouse drag operations
- **Mouse_Event**: User interaction (press, release, move, wheel) with coordinate and modifier information
- **Terminal_Focus**: State where terminal window captures mouse and keyboard input
- **Coordinate_Conversion**: Translation between screen pixels and terminal cell positions
- **Mouse_Capture**: ImGui mechanism to receive mouse events during drag operations
- **Wheel_Accumulation**: Buffering fractional wheel deltas for smooth scrolling behavior

## Requirements

### R1: Mouse Tracking Mode Management
**User Story:** As a terminal application, I want to enable different mouse tracking modes so that I can receive appropriate mouse events.
**Acceptance Criteria:**
1. WHEN mouse tracking mode 1000 (click) is enabled THEN Terminal SHALL report mouse press and release events only
2. WHEN mouse tracking mode 1002 (button) is enabled THEN Terminal SHALL report press, release, and drag events
3. WHEN mouse tracking mode 1003 (any) is enabled THEN Terminal SHALL report all mouse events including motion
4. WHEN mouse tracking is disabled THEN Terminal SHALL handle mouse events locally for selection and scrolling
5. WHEN multiple tracking modes are set THEN Terminal SHALL use the highest numbered mode (1003 > 1002 > 1000)

### R2: Mouse Encoding Format Support
**User Story:** As a terminal application, I want to choose mouse encoding format so that I can parse mouse events correctly.
**Acceptance Criteria:**
1. WHEN SGR encoding (mode 1006) is enabled THEN Terminal SHALL use SGR format for mouse event sequences
2. WHEN SGR encoding is disabled THEN Terminal SHALL use standard X10/X11 format for mouse events
3. WHEN encoding mouse coordinates THEN Terminal SHALL handle coordinates beyond 223 correctly in SGR mode
4. WHEN generating escape sequences THEN Terminal SHALL include modifier keys (shift, alt, ctrl) in encoding
5. WHEN mouse button is released THEN Terminal SHALL generate appropriate release sequence for the encoding format

### R3: Coordinate Conversion System
**User Story:** As a developer, I want accurate coordinate conversion so that mouse events map correctly to terminal cells.
**Acceptance Criteria:**
1. WHEN mouse position is received THEN Terminal SHALL convert screen pixels to 1-based terminal coordinates
2. WHEN terminal is resized THEN Terminal SHALL update coordinate conversion metrics immediately
3. WHEN mouse is outside terminal bounds THEN Terminal SHALL clamp coordinates to valid terminal range
4. WHEN character metrics change THEN Terminal SHALL recalculate coordinate conversion factors
5. WHEN coordinate conversion fails THEN Terminal SHALL handle gracefully without crashing

### R4: Mouse Event Detection and Processing
**User Story:** As a user, I want mouse clicks and movements to be detected so that applications can respond to mouse input.
**Acceptance Criteria:**
1. WHEN left mouse button is pressed THEN Terminal SHALL detect press event with correct coordinates
2. WHEN right mouse button is pressed THEN Terminal SHALL detect press event with button identifier 2
3. WHEN middle mouse button is pressed THEN Terminal SHALL detect press event with button identifier 1
4. WHEN mouse is moved while button pressed THEN Terminal SHALL detect drag events in appropriate tracking modes
5. WHEN mouse is moved without button pressed THEN Terminal SHALL detect motion events in mode 1003 only

### R5: Mouse Wheel Event Handling
**User Story:** As a user, I want mouse wheel scrolling to work correctly in both local and application modes.
**Acceptance Criteria:**
1. WHEN mouse tracking is enabled THEN Terminal SHALL send wheel events as mouse button 64/65 sequences
2. WHEN mouse tracking is disabled and in primary screen THEN Terminal SHALL scroll local scrollback buffer
3. WHEN mouse tracking is disabled and in alternate screen THEN Terminal SHALL send arrow key sequences
4. WHEN wheel delta accumulates THEN Terminal SHALL send multiple wheel events for large deltas
5. WHEN wheel events are coalesced THEN Terminal SHALL maintain smooth scrolling behavior

### R6: Selection and Copying Integration
**User Story:** As a user, I want mouse selection to work alongside mouse tracking so that I can copy text when appropriate.
**Acceptance Criteria:**
1. WHEN shift key is held during mouse events THEN Terminal SHALL handle selection locally instead of reporting to application
2. WHEN mouse tracking is disabled THEN Terminal SHALL handle all mouse events for selection
3. WHEN selection is active THEN Terminal SHALL highlight selected text visually
4. WHEN right-click occurs on selection THEN Terminal SHALL copy selected text to clipboard
5. WHEN Ctrl+C is pressed with selection THEN Terminal SHALL copy instead of sending interrupt signal

### R7: Focus and Input Priority Management
**User Story:** As a user, I want mouse input to respect terminal focus so that events go to the correct target.
**Acceptance Criteria:**
1. WHEN terminal has focus THEN Terminal SHALL capture mouse events within terminal bounds
2. WHEN terminal loses focus THEN Terminal SHALL stop mouse event processing and clear selection state
3. WHEN mouse is outside terminal bounds THEN Terminal SHALL not process mouse events
4. WHEN terminal gains focus THEN Terminal SHALL reset mouse state and prepare for input capture
5. WHEN focus changes during drag operation THEN Terminal SHALL complete or cancel the operation gracefully

### R8: Mouse State Management
**User Story:** As a developer, I want consistent mouse state tracking so that drag operations work correctly.
**Acceptance Criteria:**
1. WHEN mouse button is pressed THEN Terminal SHALL track which button is currently pressed
2. WHEN mouse drag begins THEN Terminal SHALL capture mouse input to receive events outside terminal bounds
3. WHEN mouse button is released THEN Terminal SHALL release mouse capture and update state
4. WHEN multiple buttons are pressed THEN Terminal SHALL track all pressed buttons correctly
5. WHEN mouse state becomes inconsistent THEN Terminal SHALL reset to known good state

### R9: Modifier Key Support
**User Story:** As a terminal application, I want modifier key information with mouse events so that I can implement complex interactions.
**Acceptance Criteria:**
1. WHEN shift key is held during mouse event THEN Terminal SHALL include shift modifier in escape sequence
2. WHEN alt key is held during mouse event THEN Terminal SHALL include alt modifier in escape sequence
3. WHEN ctrl key is held during mouse event THEN Terminal SHALL include ctrl modifier in escape sequence
4. WHEN multiple modifiers are held THEN Terminal SHALL combine modifiers correctly in escape sequence
5. WHEN modifier state changes during drag THEN Terminal SHALL report updated modifier state

### R10: Mouse Tracking Mode Transitions
**User Story:** As a terminal application, I want smooth transitions between mouse tracking modes so that mode changes don't cause issues.
**Acceptance Criteria:**
1. WHEN switching from mode 1000 to 1002 THEN Terminal SHALL immediately start reporting drag events
2. WHEN switching from mode 1002 to 1003 THEN Terminal SHALL immediately start reporting motion events
3. WHEN disabling mouse tracking THEN Terminal SHALL complete any ongoing mouse operations
4. WHEN enabling mouse tracking THEN Terminal SHALL reset mouse state and prepare for application events
5. WHEN mode changes during drag operation THEN Terminal SHALL handle transition without losing events

### R11: Error Handling and Recovery
**User Story:** As a developer, I want robust error handling so that mouse input failures don't crash the terminal.
**Acceptance Criteria:**
1. WHEN coordinate conversion fails THEN Terminal SHALL log error and continue with fallback coordinates
2. WHEN mouse capture fails THEN Terminal SHALL log error and continue without capture
3. WHEN escape sequence generation fails THEN Terminal SHALL log error and skip the event
4. WHEN mouse state becomes corrupted THEN Terminal SHALL reset state and log recovery action
5. WHEN ImGui mouse functions throw exceptions THEN Terminal SHALL catch and handle gracefully

### R12: Performance and Responsiveness
**User Story:** As a user, I want responsive mouse input so that interactions feel smooth and immediate.
**Acceptance Criteria:**
1. WHEN processing mouse events THEN Terminal SHALL complete processing within 1ms for typical events
2. WHEN accumulating wheel events THEN Terminal SHALL use efficient buffering to avoid allocation
3. WHEN converting coordinates THEN Terminal SHALL use cached metrics to avoid repeated calculations
4. WHEN generating escape sequences THEN Terminal SHALL use efficient string building techniques
5. WHEN handling high-frequency events THEN Terminal SHALL coalesce appropriately to maintain performance

### R13: Integration with Existing Systems
**User Story:** As a developer, I want mouse input to integrate cleanly with existing terminal systems.
**Acceptance Criteria:**
1. WHEN mouse events are processed THEN Terminal SHALL integrate with existing focus management system
2. WHEN generating escape sequences THEN Terminal SHALL use existing input encoding infrastructure
3. WHEN handling wheel events THEN Terminal SHALL integrate with existing scroll configuration system
4. WHEN managing selection THEN Terminal SHALL use existing text selection and clipboard systems
5. WHEN tracking mouse state THEN Terminal SHALL integrate with existing terminal state management

### R14: Testing and Validation
**User Story:** As a developer, I want comprehensive testing so that mouse input works reliably across scenarios.
**Acceptance Criteria:**
1. WHEN implementing mouse tracking THEN Tests SHALL verify all tracking modes work correctly
2. WHEN implementing coordinate conversion THEN Tests SHALL verify accuracy across different terminal sizes
3. WHEN implementing escape sequence generation THEN Tests SHALL verify compatibility with TypeScript reference
4. WHEN implementing error handling THEN Tests SHALL verify graceful handling of edge cases
5. WHEN implementing performance optimizations THEN Tests SHALL verify no regression in functionality

## Implementation Tasks

### Task 1: Core Mouse Infrastructure (Small)
- Create `MouseTrackingMode` enum and state management
- Implement coordinate conversion utilities
- Add mouse state tracking fields to TerminalController
- Create basic mouse event detection framework

### Task 2: Mouse Tracking Mode Implementation (Small)
- Implement mode 1000 (click tracking) support
- Add mode switching logic and state validation
- Create escape sequence generation for click events
- Add integration with existing terminal mode management

### Task 3: Extended Mouse Tracking (Medium)
- Implement mode 1002 (button/drag tracking) support
- Implement mode 1003 (any-event tracking) support
- Add mouse motion event detection and processing
- Create comprehensive escape sequence generation

### Task 4: Mouse Encoding Formats (Small)
- Implement standard X10/X11 mouse encoding
- Implement SGR mouse encoding (mode 1006)
- Add coordinate range handling for both formats
- Create encoding format selection logic

### Task 5: Mouse Wheel Integration (Medium)
- Integrate wheel events with mouse tracking modes
- Implement wheel-to-mouse-button conversion (64/65)
- Add wheel event accumulation and coalescing
- Integrate with existing wheel scroll configuration

### Task 6: Selection and Mouse Interaction (Medium)
- Implement shift-key bypass for local selection
- Add selection priority over mouse tracking
- Integrate with existing selection and clipboard systems
- Handle focus changes during selection operations

### Task 7: Mouse Capture and State Management (Small)
- Implement ImGui mouse capture for drag operations
- Add robust mouse state tracking and recovery
- Create mouse capture error handling
- Add state validation and consistency checks

### Task 8: Modifier Key Support (Small)
- Add modifier key detection to mouse events
- Implement modifier encoding in escape sequences
- Add modifier state tracking during drag operations
- Create modifier key combination handling

### Task 9: Error Handling and Edge Cases (Small)
- Add comprehensive error handling for all mouse operations
- Implement graceful degradation for failed operations
- Add logging and diagnostics for mouse input issues
- Create recovery mechanisms for corrupted state

### Task 10: Testing and Validation (Medium)
- Create unit tests for all mouse tracking modes
- Add property tests for coordinate conversion accuracy
- Create integration tests with existing terminal systems
- Add performance tests for high-frequency mouse events

## Architecture Notes

- **Headless Design**: Mouse event processing logic goes in caTTY.Core, ImGui integration in caTTY.Display
- **State Management**: Mouse tracking state integrated with existing terminal mode management
- **Performance**: Use efficient coordinate conversion with cached metrics and minimal allocation
- **Error Recovery**: Robust error handling with state reset capabilities for production stability
- **TypeScript Compatibility**: Maintain 1:1 feature parity with reference implementation escape sequences