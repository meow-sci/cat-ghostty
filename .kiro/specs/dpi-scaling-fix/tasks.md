# Implementation Plan: DPI Scaling Fix

## Overview

This implementation plan addresses the DPI scaling issue where the GameMod version of caTTY displays incorrect character spacing and font sizing compared to the standalone TestApp. The solution adds configurable character metrics to the shared TerminalController while maintaining backward compatibility.

The approach uses configuration injection to allow different contexts (TestApp vs GameMod) to provide appropriate metrics without changing core rendering logic. The GameMod will use compensated metrics (typically half-size for 2.0x DPI scaling) while the TestApp continues using standard metrics.

## Tasks

- [x] 1. Create configuration infrastructure
- [x] 1.1 Create TerminalRenderingConfig class
  - Create `caTTY.ImGui/Configuration/TerminalRenderingConfig.cs`
  - Add properties for FontSize, CharacterWidth, LineHeight, AutoDetectDpiScaling, DpiScalingFactor
  - Implement factory methods `CreateForTestApp()` and `CreateForGameMod(float dpiScale)`
  - Add `Validate()` method with bounds checking for all metrics
  - Add XML documentation for all public members
  - _Requirements: 2.1, 2.2, 2.4, 2.5_

- [x] 1.2 Write unit tests for TerminalRenderingConfig
  - Test factory methods produce correct values
  - Test validation logic with valid and invalid inputs
  - Test bounds checking for all metric properties
  - _Requirements: 2.5, 6.2, 6.3_

- [x] 1.3 Create DPI context detection utility
  - Create `caTTY.ImGui/Configuration/DpiContextDetector.cs`
  - Add `ExecutionContext` enum (TestApp, GameMod, Unknown)
  - Implement `DetectExecutionContext()` method using assembly inspection
  - Implement `DetectDpiScaling()` method using ImGui context and system fallbacks
  - Add `DetectAndCreateConfig()` method that combines detection and config creation
  - Add comprehensive logging for debugging DPI detection
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

- [x] 1.4 Write property test for DPI context detection
  - **Property 1: Context Detection and Configuration**
  - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 3.1, 3.2, 3.3**

- [x] 2. Enhance TerminalController with configuration support
- [x] 2.1 Add configuration support to TerminalController
  - Add new constructor accepting `TerminalRenderingConfig` parameter
  - Maintain existing constructors for backward compatibility (delegate to new constructor with default config)
  - Replace hardcoded `_fontSize`, `_charWidth`, `_lineHeight` fields with config-based values
  - Add `UpdateRenderingConfig(TerminalRenderingConfig)` method for runtime updates
  - Add private `LogConfiguration()` method for debugging output
  - Add read-only properties to expose current metrics for debugging
  - _Requirements: 2.1, 2.2, 2.3, 4.3, 4.4, 5.1, 6.1, 6.5_

- [x] 2.2 Write property test for configuration acceptance and application
  - **Property 2: Configuration Acceptance and Application**
  - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**

- [x] 2.3 Update character positioning calculations
  - Modify `RenderTerminalContent()` to use config-based metrics
  - Update `RenderCell()` method to use `_config.CharacterWidth` and `_config.LineHeight`
  - Update `RenderCursor()` method to use config-based positioning
  - Ensure all character positioning calculations are consistent
  - _Requirements: 2.3, 3.5_

- [x] 2.4 Write property test for character grid alignment
  - **Property 6: Character Grid Alignment Consistency**
  - **Validates: Requirements 3.5**

- [x] 2.5 Add runtime configuration update support
  - Implement validation in `UpdateRenderingConfig()` method
  - Ensure immediate application of new metrics to subsequent rendering
  - Maintain cursor position accuracy during metric changes
  - Add logging for runtime configuration changes
  - _Requirements: 5.1, 5.2, 5.3, 5.4_

- [x] 2.6 Write property test for runtime configuration updates
  - **Property 4: Runtime Configuration Updates**
  - **Validates: Requirements 5.1, 5.2, 5.3, 5.4**

- [ ] 3. Update TestApp and GameMod to use new configuration
- [ ] 3.1 Update TestApp to use explicit configuration
  - Modify `TerminalTestApp.cs` to create `TerminalRenderingConfig.CreateForTestApp()`
  - Pass configuration to TerminalController constructor
  - Verify TestApp continues to work without visual changes
  - Add logging to confirm TestApp is using correct metrics
  - _Requirements: 3.1, 4.1_

- [ ] 3.2 Update GameMod to use DPI-compensated configuration
  - Modify `TerminalMod.cs` to create `TerminalRenderingConfig.CreateForGameMod(2.0f)`
  - Pass configuration to TerminalController constructor
  - Add DPI scaling detection and logging for debugging
  - Ensure GameMod uses compensated metrics for proper character spacing
  - _Requirements: 3.2, 3.3, 4.2_

- [ ] 3.3 Add automatic detection fallback option
  - Create alternative initialization path using `DpiContextDetector.DetectAndCreateConfig()`
  - Add this as a third option for users who prefer automatic detection
  - Document when to use explicit vs automatic configuration
  - _Requirements: 3.4, 4.4_

- [ ] 3.4 Write property test for backward compatibility
  - **Property 5: Backward Compatibility and API Stability**
  - **Validates: Requirements 4.1, 4.2, 4.3, 4.4, 4.5**

- [ ] 4. Add comprehensive validation and error handling
- [ ] 4.1 Implement metric validation and bounds checking
  - Add comprehensive validation in `TerminalRenderingConfig.Validate()`
  - Add validation in `TerminalController.UpdateRenderingConfig()`
  - Implement proper exception handling with descriptive messages
  - Add fallback strategies for invalid configurations
  - _Requirements: 2.5, 5.5, 6.2, 6.3_

- [ ] 4.2 Write property test for metric validation
  - **Property 3: Metric Validation and Bounds Checking**
  - **Validates: Requirements 2.5, 5.5, 6.2, 6.3**

- [ ] 4.3 Add comprehensive logging and debugging support
  - Enhance `LogConfiguration()` with detailed DPI context information
  - Add debug properties to expose current metrics
  - Add logging for configuration changes and validation failures
  - Ensure logging doesn't crash application if console output fails
  - _Requirements: 1.5, 6.1, 6.4, 6.5_

- [ ] 4.4 Write property test for debug information and logging
  - **Property 8: Debug Information and Logging**
  - **Validates: Requirements 1.5, 6.1, 6.4, 6.5**

- [ ] 5. Integration testing and validation
- [ ] 5.1 Test TestApp with new configuration system
  - **USER VALIDATION REQUIRED**: Run TestApp and verify character spacing remains correct
  - Verify that TestApp uses standard metrics (16.0f font, 9.6f width, 18.0f height)
  - Check console output for correct configuration logging
  - Test that existing TestApp code works without modifications
  - _Requirements: 3.1, 4.1_

- [ ] 5.2 Test GameMod with DPI-compensated configuration
  - **USER VALIDATION REQUIRED**: Run GameMod and verify character spacing is now correct
  - Verify that GameMod uses compensated metrics (8.0f font, 4.8f width, 9.0f height for 2.0x scaling)
  - Check console output for DPI detection and configuration logging
  - Compare character alignment between TestApp and GameMod to ensure consistency
  - _Requirements: 3.2, 3.3, 4.2_

- [ ] 5.3 Test runtime configuration updates
  - **USER VALIDATION REQUIRED**: Test runtime metric updates in both TestApp and GameMod
  - Verify that character spacing changes immediately when metrics are updated
  - Test that cursor position remains accurate after metric changes
  - Verify that invalid runtime updates are rejected with appropriate errors
  - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

- [ ] 5.4 Write property test for configuration override capability
  - **Property 7: Configuration Override Capability**
  - **Validates: Requirements 3.4**

- [ ] 6. Documentation and cleanup
- [ ] 6.1 Update XML documentation
  - Add comprehensive XML documentation to all new classes and methods
  - Document configuration options and when to use each approach
  - Add examples of TestApp vs GameMod configuration usage
  - Document DPI scaling detection and compensation logic
  - _Requirements: All requirements for maintainability_

- [ ] 6.2 Add configuration usage examples
  - Create example code showing explicit configuration for TestApp
  - Create example code showing DPI-compensated configuration for GameMod
  - Create example code showing automatic detection usage
  - Document troubleshooting steps for DPI scaling issues
  - _Requirements: Developer experience and maintainability_

- [ ] 7. Final validation and testing
- [ ] 7.1 Comprehensive integration testing
  - **USER VALIDATION REQUIRED**: Final validation that both TestApp and GameMod render correctly
  - Verify that character spacing is consistent and properly aligned in both contexts
  - Test with different terminal content (text, colors, cursor positioning)
  - Confirm that the shared TerminalController works correctly in both deployment targets
  - _Requirements: All requirements_

- [ ] 7.2 Performance validation
  - Verify that configuration changes don't impact rendering performance
  - Test that runtime metric updates don't cause frame rate drops
  - Ensure that DPI detection doesn't add significant startup time
  - _Requirements: Performance and user experience_

- [ ] 8. Checkpoint - DPI scaling fix complete
  - Both TestApp and GameMod render with correct character spacing and alignment
  - Configuration system allows fine-tuning of metrics for different DPI contexts
  - Backward compatibility maintained for existing code
  - Comprehensive testing validates all requirements

## Notes

- Each task references specific requirements for traceability
- The implementation maintains backward compatibility while adding new configuration capabilities
- DPI scaling compensation is configurable and can be adjusted for different scaling factors
- Comprehensive logging and debugging support helps diagnose DPI-related issues