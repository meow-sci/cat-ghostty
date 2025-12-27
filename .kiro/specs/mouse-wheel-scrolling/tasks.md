# Implementation Plan: Mouse Wheel Scrolling

## Overview

Add mouse wheel scrolling support to the existing caTTY terminal emulator ImGui TerminalController. The implementation leverages the existing ScrollbackManager infrastructure and follows the mouse wheel handling patterns demonstrated in MouseInputExperiments.cs.

## Tasks

- [x] 1. Create mouse wheel scroll configuration
  - Create MouseWheelScrollConfig class with validation
  - Add default configuration factory methods
  - Integrate with existing TerminalController constructor overloads
  - Add runtime configuration update method
  - _Requirements: 4.1, 4.4_

- [x] 1.1 Write unit tests for scroll configuration
  - Test configuration validation and defaults
  - Test invalid configuration rejection
  - Test runtime configuration updates
  - _Requirements: 4.1, 4.4_

- [x] 2. Implement mouse wheel event detection
  - Add HandleMouseWheelInput method to TerminalController
  - Integrate with existing HandleInput method
  - Add focus checking to ignore events when unfocused
  - Add minimum delta threshold to prevent micro-movements
  - _Requirements: 1.1, 1.2, 1.3_

- [x] 2.1 Write property test for focus-based event filtering
  - **Property 6: Focus-based event filtering**
  - **Validates: Requirements 1.3**

- [x] 3. Implement wheel delta accumulation algorithm
  - Add wheel accumulator field to TerminalController
  - Create ProcessMouseWheelScroll method with smooth accumulation
  - Handle fractional deltas and convert to integer line counts
  - Add overflow protection for accumulator
  - _Requirements: 5.1, 5.2_

- [x] 3.1 Write property test for wheel delta accumulation
  - **Property 5: Wheel delta accumulation and line calculation**
  - **Validates: Requirements 5.1, 5.2**

- [x] 4. Integrate with ScrollbackManager
  - Call ScrollbackManager.ScrollUp/ScrollDown with calculated line counts
  - Add error handling for ScrollbackManager integration
  - Ensure proper direction mapping (positive delta = scroll up)
  - Add boundary condition handling
  - _Requirements: 2.1, 2.2, 2.3, 2.4_

- [x] 4.1 Write property test for ScrollbackManager integration
  - **Property 1: Mouse wheel event processing and ScrollbackManager integration**
  - **Validates: Requirements 1.1, 1.2, 2.1, 2.2**

- [x] 4.2 Write property test for boundary condition handling
  - **Property 2: Boundary condition handling at scroll limits**
  - **Validates: Requirements 2.3, 2.4**

- [x] 5. Implement auto-scroll behavior integration
  - Verify auto-scroll state changes work correctly with wheel scrolling
  - Test that ScrollbackManager handles auto-scroll disable/enable
  - Ensure viewport doesn't yank when auto-scroll is disabled
  - Verify auto-scroll re-enables when scrolling to bottom
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 5.1 Write property test for auto-scroll state management
  - **Property 3: Auto-scroll state management with wheel scrolling**
  - **Validates: Requirements 3.1, 3.2**

- [x] 6. Add configuration sensitivity testing
  - Verify different sensitivity values produce correct line counts
  - Test that sensitivity=1 scrolls exactly 1 line per wheel step
  - Test that sensitivity=3 scrolls exactly 3 lines per wheel step
  - Validate configuration clamping to reasonable ranges
  - _Requirements: 4.2, 4.3, 4.4_

- [x] 6.1 Write property test for configuration sensitivity behavior
  - **Property 4: Configuration validation and sensitivity behavior**
  - **Validates: Requirements 4.1, 4.2, 4.3, 4.4**

- [x] 7. Add error handling and robustness
  - Add try-catch around mouse wheel processing
  - Reset accumulator on errors to prevent stuck state
  - Log warnings for wheel processing errors without crashing terminal
  - Validate wheel delta values for NaN/infinity
  - _Requirements: 7.1, 7.3_

- [x] 7.1 Write unit tests for error handling
  - Test error recovery and accumulator reset
  - Test invalid wheel delta handling
  - Test exception handling in wheel processing
  - _Requirements: 7.1, 7.3_

- [x] 8. Integration testing and validation
  - Add debugging output to identify mouse wheel scrolling issues in TestApp
  - Fix focus detection or mouse wheel event processing problems
  - **FIXED: Terminal rendering was bypassing ScrollbackManager viewport**
  - **FIXED: RenderTerminalContent now uses GetViewportRows for proper scrollback integration**
  - **FIXED: Auto-scroll viewport calculation corrected in GetViewportRows method**
  - Test mouse wheel scrolling in TestApp
  - Test mouse wheel scrolling in GameMod context
  - Verify performance with rapid wheel events
  - Test interaction with existing keyboard scrolling
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [x] 8.1 Write integration tests
  - Test wheel scrolling with existing scrollback functionality
  - Test interaction between wheel and keyboard scrolling
  - Test edge cases (empty terminal, full scrollback)
  - _Requirements: 8.1, 8.2, 8.3, 8.4_

- [ ] 9. Documentation and examples
  - Update TerminalController XML documentation
  - Add mouse wheel scrolling example to MouseInputExperiments
  - Document configuration options and defaults
  - Add usage examples for different sensitivity settings

- [x] 10. Checkpoint - Mouse wheel scrolling working
  - Mouse wheel scrolling works in both TestApp and GameMod
  - All property tests pass
  - Integration with existing scrollback system is seamless
  - Configuration and error handling work correctly
  - **FIXED: Auto-scroll to bottom on new content now works correctly**

## Final Resolution

The auto-scroll issue was caused by incorrect viewport calculation in `ScrollbackManager.GetViewportRows()`. The method was showing content starting from the beginning of scrollback when `ViewportOffset = 0` (at bottom), instead of showing the most recent content.

**Root Cause**: When `_viewportOffset = 0` (at bottom), the viewport calculation was:
```csharp
var viewportTop = Math.Max(0, Math.Min(_viewportOffset, scrollbackRows)); // = 0
```
This showed content starting from the oldest scrollback line, not the most recent.

**Fix**: Changed the viewport calculation to:
```csharp
var totalContentRows = scrollbackRows + screenRows;
var viewportStart = Math.Max(0, totalContentRows - requestedRows - _viewportOffset);
```
This correctly shows the most recent content when at bottom (`_viewportOffset = 0`) and earlier content when scrolled up (`_viewportOffset > 0`).

## Notes

- Each task references specific requirements for traceability
- Property tests validate universal correctness properties using FsCheck.NUnit
- Unit tests validate specific examples and edge cases
- The implementation reuses existing ScrollbackManager methods without duplication
- Integration testing ensures compatibility with both TestApp and GameMod contexts