# Implementation Plan: Font Configuration System

## Overview

This implementation plan creates a flexible font configuration system for the caTTY terminal emulator. The current implementation uses hardcoded font settings, but different deployment contexts (TestApp vs GameMod) may benefit from different font families, styles, and sizes. Since the KSA game renders directly with GLFW and Vulkan (bypassing Windows DPI scaling), the solution focuses on configurable font selection rather than DPI compensation.

The approach uses configuration injection to allow different contexts to specify appropriate fonts while maintaining backward compatibility with existing code. The system supports separate font variants for regular, bold, italic, and bold+italic text styling.

## Tasks

**CONSOLE OUTPUT REQUIREMENTS**: All unit tests and property-based tests MUST strive to have no stdout/stderr output under normal conditions to reduce verbosity of console output. Tests should only produce output when:
- A test fails and diagnostic information is needed
- Explicit debugging is enabled via environment variables or test flags
- Critical errors occur that require immediate attention

This requirement applies to all test tasks throughout the implementation plan.

- [x] 1. Create font configuration infrastructure
- [x] 1.1 Create TerminalFontConfig class
  - Create `caTTY.Display/Configuration/TerminalFontConfig.cs`
  - Add properties for RegularFontName, BoldFontName, ItalicFontName, BoldItalicFontName, FontSize, AutoDetectContext
  - Implement factory methods `CreateForTestApp()` and `CreateForGameMod()`
  - Add `Validate()` method with bounds checking for font size and null checking for font names
  - Add XML documentation for all public members
  - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.4_

- [x] 1.2 Write unit tests for TerminalFontConfig
  - Test factory methods produce correct font names and sizes
  - Test validation logic with valid and invalid inputs
  - Test bounds checking for font size and null checking for font names
  - _Requirements: 2.4, 2.5, 6.2, 6.3_

- [x] 1.3 Create font context detection utility
  - Create `caTTY.Display/Configuration/FontContextDetector.cs`
  - Add `ExecutionContext` enum (TestApp, GameMod, Unknown)
  - Implement `DetectExecutionContext()` method using assembly inspection
  - Add `DetectAndCreateConfig()` method that combines detection and config creation
  - Add comprehensive logging for debugging font context detection
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [x] 1.4 Write property test for font context detection
  - **Property 2: Context Detection and Default Configuration**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4**

- [x] 2. Enhance TerminalController with font configuration support
- [x] 2.1 Add font configuration support to TerminalController
  - Add new constructor accepting `TerminalFontConfig` parameter
  - Maintain existing constructors for backward compatibility (delegate to new constructor with default config)
  - Add private fields for `_regularFont`, `_boldFont`, `_italicFont`, `_boldItalicFont` (ImFontPtr)
  - Add `LoadFonts()` method to load fonts from ImGui font system by name
  - Add `FindFont(string fontName)` method to locate fonts in ImGui font atlas
  - Add `CalculateCharacterMetrics()` method to derive character width/height from loaded fonts
  - Add `UpdateFontConfig(TerminalFontConfig)` method for runtime updates
  - Add private `LogFontConfiguration()` method for debugging output
  - Add read-only properties to expose current font configuration for debugging
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 4.3, 4.4, 5.1, 6.1, 6.5_

- [x] 2.2 Write property test for font configuration acceptance and application
  - **Property 1: Font Configuration Acceptance and Application**
  - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 2.1, 2.2**

- [x] 2.3 Update character rendering to use font styles
  - Modify `RenderCell()` method to select appropriate font based on SGR attributes
  - Add `SelectFont(SgrAttributes)` method to choose between regular, bold, italic, bold+italic fonts
  - Use ImGui.PushFont()/PopFont() pattern for each character rendering
  - Ensure font selection is consistent across all character rendering operations
  - _Requirements: 1.3, font style rendering consistency_

- [x] 2.4 Write property test for font style selection
  - **Property 6: Font Style Selection Consistency**
  - **Validates: Requirements 1.3, character rendering consistency**

- [x] 2.5 Add character metrics calculation from fonts
  - Implement `CalculateCharacterMetrics()` to measure actual font dimensions
  - Use ImGui.CalcTextSize() with test characters to determine character width and line height
  - Update character positioning calculations to use calculated metrics instead of hardcoded values
  - Ensure metrics are recalculated when fonts are changed
  - _Requirements: 2.3, character positioning accuracy_

- [ ] 2.6 Write property test for character metrics calculation
  - **Property 7: Character Metrics Calculation**
  - **Validates: Requirements 2.3, character positioning accuracy**

- [ ] 2.7 Add runtime font configuration update support
  - Implement validation in `UpdateFontConfig()` method
  - Ensure immediate font reloading and metrics recalculation
  - Maintain cursor position accuracy during font changes
  - Add logging for runtime configuration changes
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [ ] 2.8 Write property test for runtime font configuration updates
  - **Property 4: Runtime Font Configuration Updates**
  - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**

- [ ] 3. Update TestApp and GameMod to use new font configuration
- [ ] 3.1 Update TestApp to use explicit font configuration
  - Modify `TerminalTestApp.cs` to create `TerminalFontConfig.CreateForTestApp()`
  - Pass font configuration to TerminalController constructor
  - Verify TestApp continues to work without visual changes
  - Add logging to confirm TestApp is using correct fonts
  - _Requirements: 3.1, 4.1_

- [ ] 3.2 Update GameMod to use game-appropriate font configuration
  - Modify `TerminalMod.cs` to create `TerminalFontConfig.CreateForGameMod()`
  - Pass font configuration to TerminalController constructor
  - Add font configuration logging for debugging
  - Ensure GameMod uses appropriate fonts for game context
  - _Requirements: 3.2, 4.2_

- [ ] 3.3 Add automatic detection fallback option
  - Create alternative initialization path using `FontContextDetector.DetectAndCreateConfig()`
  - Add this as a third option for users who prefer automatic detection
  - Document when to use explicit vs automatic configuration
  - _Requirements: 3.4, 4.4_

- [ ] 3.4 Write property test for backward compatibility
  - **Property 5: Backward Compatibility and API Stability**
  - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**

- [ ] 4. Add comprehensive validation and error handling
- [ ] 4.1 Implement font configuration validation and bounds checking
  - Add comprehensive validation in `TerminalFontConfig.Validate()`
  - Add validation in `TerminalController.UpdateFontConfig()`
  - Implement proper exception handling with descriptive messages
  - Add fallback strategies for invalid configurations and missing fonts
  - _Requirements: 1.4, 2.4, 2.5, 5.5, 6.2, 6.3_

- [ ] 4.2 Write property test for font loading and validation
  - **Property 3: Font Loading and Validation**
  - **Validates: Requirements 1.4, 2.4, 2.5, 6.2, 6.3**

- [ ] 4.3 Add comprehensive logging and debugging support
  - Enhance `LogFontConfiguration()` with detailed font loading information
  - Add debug properties to expose current font configuration
  - Add logging for font loading failures and fallback usage
  - Ensure logging doesn't crash application if console output fails
  - _Requirements: 1.5, 6.1, 6.4, 6.5_

- [ ] 4.4 Write property test for debug information and logging
  - **Property 9: Debug Information and Logging**
  - **Validates: Requirements 1.5, 6.1, 6.4, 6.5**

- [ ] 5. Integration testing and validation
- [ ] 5.1 Test TestApp with new font configuration system
  - **USER VALIDATION REQUIRED**: Run TestApp and verify font rendering works correctly
  - Verify that TestApp uses appropriate development fonts
  - Check console output for correct font configuration logging
  - Test that existing TestApp code works without modifications
  - _Requirements: 3.1, 4.1_

- [ ] 5.2 Test GameMod with game-appropriate font configuration
  - **USER VALIDATION REQUIRED**: Run GameMod and verify font rendering works correctly
  - Verify that GameMod uses appropriate game context fonts
  - Check console output for font detection and configuration logging
  - Test font style rendering (bold, italic, bold+italic) in game context
  - _Requirements: 3.2, 4.2_

- [ ] 5.3 Test runtime font configuration updates
  - **USER VALIDATION REQUIRED**: Test runtime font updates in both TestApp and GameMod
  - Verify that font changes are applied immediately
  - Test that character metrics are recalculated correctly
  - Verify that invalid runtime updates are rejected with appropriate errors
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 5.4 Write property test for configuration override capability
  - **Property 8: Configuration Override Capability**
  - **Validates: Requirements 3.4**

- [ ] 6. Documentation and cleanup
- [ ] 6.1 Update XML documentation
  - Add comprehensive XML documentation to all new classes and methods
  - Document font configuration options and when to use each approach
  - Add examples of TestApp vs GameMod font configuration usage
  - Document font loading and fallback logic
  - _Requirements: All requirements for maintainability_

- [ ] 6.2 Add font configuration usage examples
  - Create example code showing explicit font configuration for TestApp
  - Create example code showing game-appropriate font configuration for GameMod
  - Create example code showing automatic detection usage
  - Document troubleshooting steps for font loading issues
  - _Requirements: Developer experience and maintainability_

- [ ] 7. Final validation and testing
- [ ] 7.1 Comprehensive integration testing
  - **USER VALIDATION REQUIRED**: Final validation that both TestApp and GameMod render correctly
  - Verify that font styles (regular, bold, italic, bold+italic) render correctly in both contexts
  - Test with different terminal content (text, colors, cursor positioning, styled text)
  - Confirm that the shared TerminalController works correctly in both deployment targets
  - _Requirements: All requirements_

- [ ] 7.2 Performance validation
  - Verify that font configuration changes don't impact rendering performance
  - Test that runtime font updates don't cause frame rate drops
  - Ensure that font loading doesn't add significant startup time
  - Test font switching performance during character rendering
  - _Requirements: Performance and user experience_

- [ ] 8. Checkpoint - Font configuration system complete
  - Both TestApp and GameMod render with appropriate fonts and proper character styling
  - Font configuration system allows fine-tuning of fonts for different contexts
  - Backward compatibility maintained for existing code
  - Comprehensive testing validates all requirements

## Notes

- Each task references specific requirements for traceability
- The implementation maintains backward compatibility while adding new font configuration capabilities
- Font selection is configurable and can be adjusted for different deployment contexts
- Comprehensive logging and debugging support helps diagnose font-related issues
- Font style support (bold, italic, bold+italic) enhances terminal text rendering capabilities