# Implementation Plan: Window Design

## Overview

This implementation plan transforms the current basic ImGui terminal window layout into a structured, feature-rich interface with menu bar, tab area, settings area, and terminal canvas. The approach focuses on refactoring the existing `TerminalController.Render()` method while preserving all current terminal functionality.

## Tasks

- [x] 1. Create layout constants and helper structures
  - Define layout constants for heights, spacing, and dimensions
  - Create TerminalSettings class for future multi-terminal support
  - Add helper methods for layout calculations
  - _Requirements: 7.2, 7.3, 8.5_

- [x] 1.1 Write property test for layout constants validation
  - **Property 1: Layout constants are within reasonable ranges**
  - **Validates: Requirements 7.2, 7.3**

- [x] 2. Implement menu bar rendering
  - [x] 2.1 Create RenderMenuBar() method with ImGui menu widgets
    - Implement File menu with New Terminal, Close Terminal, Exit options
    - Implement Edit menu with Copy, Paste, Select All options  
    - Implement View menu with zoom controls
    - Use ImGui.BeginMenuBar() and ImGui.MenuItem() APIs
    - _Requirements: 1.1, 1.2, 1.3, 8.1_

  - [x] 2.2 Manual validation: Verify menu bar structure
    - **Developer validation**: Confirm menu items exist and are properly labeled
    - **Developer validation**: Verify disabled states for unavailable options
    - **Developer validation**: Test menu dropdowns open and close correctly
    - _Requirements: 1.2, 8.1_

  - [x] 2.3 Write property test for menu bar layout
    - **Property 2: Menu bar spans full width and appears at top**
    - **Validates: Requirements 1.4, 1.5**

- [x] 3. Implement tab area rendering
  - [x] 3.1 Create RenderTabArea() method
    - Display single tab representing current terminal instance
    - Add "+" button on right edge for future multi-terminal support
    - Implement proper spacing and sizing calculations
    - _Requirements: 2.1, 2.2, 2.3, 2.5_

  - [x] 3.2 Manual validation: Verify tab area components
    - **Developer validation**: Confirm single tab is displayed with correct label
    - **Developer validation**: Verify add button is present and positioned correctly
    - **Developer validation**: Test tooltip appears on add button hover
    - _Requirements: 2.2, 2.3, 2.4_

  - [x] 3.3 Write property test for tab area layout
    - **Property 3: Tab area maintains consistent height and full width**
    - **Validates: Requirements 2.1, 2.5**

- [ ] 4. Implement settings area rendering
  - [ ] 4.1 Create RenderSettingsArea() method
    - Move terminal info display from top of window to settings area
    - Add font size slider control
    - Implement proper horizontal layout for controls
    - _Requirements: 3.1, 3.2, 3.4, 5.1, 5.2_

  - [ ] 4.2 Manual validation: Verify settings area controls
    - **Developer validation**: Confirm font size slider is present and functional
    - **Developer validation**: Verify terminal info is displayed correctly
    - **Developer validation**: Test settings area contains expected widgets
    - _Requirements: 3.2, 3.4, 5.2_

  - [ ] 4.3 Write property test for settings functionality
    - **Property 4: Settings changes apply to current terminal instance**
    - **Validates: Requirements 3.3, 5.3**

- [ ] 5. Refactor terminal canvas rendering
  - [ ] 5.1 Create RenderTerminalCanvas() method
    - Extract existing terminal rendering logic into RenderTerminalContentInternal()
    - Implement HandleCanvasResize() for canvas-specific sizing
    - Calculate available space after header areas
    - _Requirements: 4.1, 4.3, 4.5_

  - [ ] 5.2 Write property test for canvas space utilization
    - **Property 5: Terminal canvas uses remaining space efficiently**
    - **Validates: Requirements 4.3, 4.5**

  - [ ] 5.3 Write property test for terminal functionality preservation
    - **Property 6: Terminal operations work exactly as before redesign**
    - **Validates: Requirements 4.2, 4.4, 6.2, 6.3**

- [ ] 6. Integrate new layout into main Render() method
  - [ ] 6.1 Refactor TerminalController.Render() method
    - Replace existing layout with structured approach
    - Call RenderMenuBar(), RenderTabArea(), RenderSettingsArea(), RenderTerminalCanvas() in sequence
    - Preserve existing focus management and input handling
    - _Requirements: 1.5, 2.1, 3.1, 4.1_

  - [ ] 6.2 Write property test for hierarchical layout order
    - **Property 1: Layout elements appear in correct hierarchical order**
    - **Validates: Requirements 1.5, 2.1, 3.1, 4.1**

- [ ] 7. Implement helper methods and error handling
  - [ ] 7.1 Create ShowNotImplementedMessage() method
    - Handle clicks on future multi-terminal features
    - Provide user feedback for not-yet-implemented functionality
    - _Requirements: 2.4, 6.4_

  - [ ] 7.2 Add layout calculation error handling
    - Handle invalid window dimensions gracefully
    - Provide fallback values for layout calculations
    - Add logging for debugging layout issues
    - _Requirements: 7.1, 7.5_

  - [ ] 7.3 Manual validation: Verify error handling behavior
    - **Developer validation**: Test invalid window size handling
    - **Developer validation**: Verify fallback behavior for layout failures
    - **Developer validation**: Confirm graceful degradation when ImGui calls fail
    - _Requirements: 7.1_

- [ ] 8. Implement responsive layout behavior
  - [ ] 8.1 Update window resize handling
    - Modify existing HandleWindowResize() to work with new layout
    - Ensure proper space distribution during resize
    - Maintain header area heights during resize
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [ ] 8.2 Write property test for resize stability
    - **Property 7: Layout maintains stability during window resize**
    - **Validates: Requirements 7.2, 7.3, 7.4**

- [ ] 9. Add menu functionality implementation
  - [ ] 9.1 Implement menu action handlers
    - Add CopySelectionToClipboard(), PasteFromClipboard(), SelectAllText() methods
    - Add ResetFontSize(), IncreaseFontSize(), DecreaseFontSize() methods
    - Connect menu items to appropriate actions
    - _Requirements: 1.3, 8.1_

  - [ ] 9.2 Manual validation: Verify menu actions
    - **Developer validation**: Test copy/paste functionality works correctly
    - **Developer validation**: Verify font size adjustment methods function properly
    - **Developer validation**: Confirm menu item enable/disable states are correct
    - _Requirements: 1.3_

- [ ] 10. Checkpoint - Ensure all tests pass and layout works correctly
  - Ensure all property tests pass, ask the user if questions arise.
  - **Developer validation**: Manually verify all ImGui layout areas render correctly

- [ ] 11. Final integration and validation
  - [ ] 11.1 Manual validation: Test complete window rendering pipeline
    - **Developer validation**: Verify all layout areas render correctly together
    - **Developer validation**: Test interaction between different areas
    - **Developer validation**: Validate single terminal instance constraint
    - _Requirements: 6.1, 6.4_

  - [ ] 11.2 Manual validation: Verify complete layout integration
    - **Developer validation**: Test full window rendering from initialization to display
    - **Developer validation**: Verify cross-area interactions (settings affecting canvas)
    - **Developer validation**: Confirm layout stability across different window sizes
    - _Requirements: 6.1, 6.2, 6.3_

  - [ ] 11.3 Write property test for single terminal constraint
    - **Property 8: Exactly one terminal instance is managed**
    - **Validates: Requirements 2.2, 6.1, 6.4**

- [ ] 12. Final checkpoint - Ensure all functionality works as expected
  - Ensure all property tests pass, ask the user if questions arise.
  - **Developer validation**: Manually verify complete window layout and functionality

- [ ] 13. Consolidate font configuration objects
  - [ ] 13.1 Remove font configuration from TerminalSettings class
    - Remove FontSize and FontName properties from TerminalSettings
    - Update TerminalSettings.Validate() to remove font-related validation
    - Update TerminalSettings.Clone() to remove font-related copying
    - _Requirements: 9.1, 9.2, 9.5_

  - [ ] 13.2 Update font size adjustment methods to use TerminalFontConfig directly
    - Modify ResetFontSize(), IncreaseFontSize(), DecreaseFontSize() methods
    - Remove UpdateTerminalSettings() calls and use UpdateFontConfig() directly
    - Eliminate TerminalSettings intermediate object for font changes
    - _Requirements: 9.3, 9.4_

  - [ ] 13.3 Write property test for font configuration consolidation
    - **Property 10: Only TerminalFontConfig stores font configuration**
    - **Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.5**

- [ ] 14. Implement automatic terminal resize on font changes
  - [ ] 14.1 Add terminal resize trigger to UpdateFontConfig method
    - Calculate new terminal dimensions after font metrics change
    - Call terminal resize logic similar to HandleWindowResize
    - Ensure PTY process dimensions are updated
    - _Requirements: 10.1, 10.2, 10.4_

  - [ ] 14.2 Create TriggerTerminalResize helper method
    - Extract terminal resize logic from HandleWindowResize
    - Make it reusable for both window resize and font change scenarios
    - Maintain cursor position accuracy during resize
    - _Requirements: 10.3, 10.5_

  - [ ] 14.3 Write property test for automatic terminal resize
    - **Property 11: Font changes trigger terminal dimension recalculation**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**

- [ ] 15. Final integration and validation of font fixes
  - [ ] 15.1 Manual validation: Test font size changes trigger terminal resize
    - **Developer validation**: Verify zoom in/out immediately recalculates terminal dimensions
    - **Developer validation**: Confirm no manual window resize needed after font changes
    - **Developer validation**: Test cursor position maintained during font-triggered resize
    - _Requirements: 10.1, 10.2, 10.3_

  - [ ] 15.2 Manual validation: Verify single font configuration source
    - **Developer validation**: Confirm only TerminalFontConfig contains font settings
    - **Developer validation**: Verify no duplicate font configuration in TerminalSettings
    - **Developer validation**: Test font changes work correctly with consolidated configuration
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 16. Final checkpoint - Ensure all font configuration fixes work correctly
  - Ensure all property tests pass, ask the user if questions arise.
  - **Developer validation**: Manually verify font size changes and terminal resize behavior

## Notes

- Property tests validate universal correctness properties for non-UI logic
- Manual validation tasks require developer verification of ImGui display functionality
- Unit tests are only used for testable non-UI logic (layout calculations, error handling)
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation with both automated tests and manual verification
- The implementation preserves all existing terminal functionality while adding the new layout structure
- Multi-terminal functionality is explicitly not implemented in this phase - only UI structure preparation