# Implementation Plan: TOML Terminal Theming

## Overview

This implementation plan converts the TOML-based terminal theming design into a series of incremental coding tasks. The approach focuses on building the core theme loading infrastructure first, then integrating it with the existing UI, and finally adding the opacity and UI simplification features. Each task builds on previous work and includes comprehensive testing to ensure correctness.

## Tasks

- [ ] 0. Add Tomlyn NuGet package dependency
  - Add Tomlyn package reference to caTTY.Display.csproj
  - Verify package compatibility with existing .NET 10 and C# 13 configuration
  - _Requirements: Infrastructure setup_

- [x] 1 Set up TOML parsing infrastructure and core theme loading
  - Add Tomlyn NuGet package reference to caTTY.Display project
  - Create TomlThemeLoader class with file discovery and parsing methods
  - Implement hex color parsing and validation utilities using Tomlyn's TomlTable API
  - Use `Toml.ToModel()` for parsing TOML content to `TomlTable` objects
  - Use `Toml.TryToModel()` for graceful error handling with diagnostic information
  - _Requirements: 1.1, 1.2, 5.1, 5.2_

- [x] 1.1 Write property test for theme discovery completeness
  - **Property 1: Theme Discovery Completeness**
  - **Validates: Requirements 1.1**

- [x] 1.2 Write property test for TOML parsing consistency
  - **Property 2: TOML Theme Parsing Consistency**
  - **Validates: Requirements 1.2, 5.3, 5.4, 5.5**

- [x] 1.3 Write property test for hex color round-trip conversion
  - **Property 8: Hex Color Parsing Round-Trip**
  - **Validates: Requirements 5.1, 5.2**

- [ ] 2 Enhance ThemeManager with TOML theme support
- [x] 2.01 Enhance ThemeManager with TOML theme support
  - Extend ThemeManager to load and manage TOML themes using Tomlyn
  - Update default theme to use Adventure.toml color values
  - Implement theme collection management and fallback logic
  - Add theme change event notification system
  - Use `TomlTable` dictionary-style access for nested TOML sections
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 6.4_

- [x] 2.1 Write property test for theme validation completeness
  - **Property 3: Theme Validation Completeness**
  - **Validates: Requirements 1.3, 1.4**

- [x] 2.2 Write property test for theme name extraction
  - **Property 4: Theme Name Extraction Consistency**
  - **Validates: Requirements 1.5**

- [x] 2.3 Write property test for theme change notifications
  - **Property 10: Theme Change Notification Consistency**
  - **Validates: Requirements 6.4**

- [x] 3. Implement theme persistence and configuration management
  - Create ThemeConfiguration class for settings persistence
  - Implement JSON-based configuration file handling
  - Add theme preference save/load functionality with error handling
  - _Requirements: 6.1, 6.2, 6.3_

- [x] 3.1 Write property test for theme persistence round-trip
  - **Property 9: Theme Persistence Round-Trip**
  - **Validates: Requirements 6.1**

- [x] 3.2 Write unit tests for configuration error handling
  - Test missing configuration file scenarios
  - Test invalid configuration file scenarios
  - _Requirements: 6.2, 6.3_

- [ ] 4. Checkpoint - Ensure core theme system functionality
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Add theme menu to TerminalController UI
  - Implement RenderThemeMenu method in TerminalController
  - Integrate theme selection with existing menu system
  - Add theme application logic with immediate UI updates
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 5.1 Write property test for theme menu content completeness
  - **Property 6: Theme Menu Content Completeness**
  - **Validates: Requirements 4.2, 4.5**

- [x] 5.2 Write property test for theme application completeness
  - **Property 7: Theme Application Completeness**
  - **Validates: Requirements 4.3, 4.4**

- [ ] 6. Implement global opacity management system
  - Create OpacityManager class with opacity control and persistence
  - Add opacity change event system
  - Implement opacity validation and bounds checking
  - _Requirements: 7.1, 7.4_

- [ ] 6.1 Write property test for opacity persistence round-trip
  - **Property 12: Opacity Persistence Round-Trip**
  - **Validates: Requirements 7.4**

- [ ] 7. Add Settings menu with opacity control to TerminalController
  - Implement RenderSettingsMenu method with opacity slider
  - Integrate opacity changes with terminal rendering pipeline
  - Apply opacity to all terminal canvas rendering operations
  - _Requirements: 7.1, 7.2, 7.3, 7.5_

- [ ] 7.1 Write property test for opacity application completeness
  - **Property 11: Opacity Application Completeness**
  - **Validates: Requirements 7.2, 7.3, 7.5**

- [ ] 8. Simplify terminal UI layout
  - Remove tab bar and info display from main terminal rendering
  - Implement RenderTerminalCanvas method for clean terminal-only display
  - Update terminal dimension calculations for simplified layout
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 8.1 Write property test for terminal canvas space utilization
  - **Property 13: Terminal Canvas Space Utilization**
  - **Validates: Requirements 8.3**

- [ ] 8.2 Write unit tests for simplified UI layout
  - Test that tab bars and info displays are not rendered
  - Test that menu functionality remains accessible
  - _Requirements: 8.1, 8.2, 8.4, 8.5_

- [ ] 9. Implement comprehensive error handling and resilience
  - Add robust error handling for TOML parsing failures
  - Implement graceful fallback for file system errors
  - Add logging for all error conditions with appropriate detail levels
  - Ensure system remains functional with default theme under all error conditions
  - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

- [ ] 9.1 Write property test for error handling resilience
  - **Property 14: Error Handling Resilience**
  - **Validates: Requirements 9.1, 9.2, 9.3, 9.5**

- [ ] 9.2 Write unit tests for specific error scenarios
  - Test invalid TOML syntax handling
  - Test missing required sections handling
  - Test file system access failures
  - _Requirements: 9.1, 9.2, 9.4_

- [ ] 10. Integration and assembly path resolution
  - Implement assembly location detection using Assembly.GetExecutingAssembly().Location
  - Add TerminalThemes directory path construction logic
  - Integrate theme loading with application startup sequence
  - Test theme discovery in both TestApp and GameMod contexts
  - _Requirements: 3.1, 3.2, 3.3, 3.4_

- [ ] 10.1 Write property test for assembly path resolution
  - **Property 5: Assembly Path Resolution Consistency**
  - **Validates: Requirements 3.1, 3.2**

- [ ] 10.2 Write integration tests for theme system startup
  - Test theme discovery on application startup
  - Test fallback behavior when TerminalThemes directory is missing
  - _Requirements: 3.3, 3.4_

- [ ] 11. Final integration and testing
  - Wire all components together in TerminalController
  - Ensure theme changes propagate correctly to all rendering components
  - Verify opacity and theme settings persist across application restarts
  - Test complete user workflow from theme selection to rendering
  - _Requirements: All requirements integration_

- [ ] 11.1 Write integration tests for complete theme workflow
  - Test end-to-end theme selection and application
  - Test persistence across application sessions
  - Test error recovery scenarios

- [ ] 12. Final checkpoint - Ensure all tests pass and system integration
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- All tasks are required for comprehensive implementation from the start
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties with minimum 100 iterations
- Unit tests validate specific examples and edge cases
- Integration tests verify end-to-end functionality
- The implementation follows the existing caTTY.Display architecture patterns
- **TOML parsing uses the Tomlyn library with TomlTable API for flexible theme structure handling**
- **Tomlyn provides `Toml.ToModel()` for parsing and `Toml.TryToModel()` for error handling with diagnostics**
- All theme files in caTTY.Display.Tests/TerminalThemes/ will be used as test data
- **Test Output**: All unit and property tests must be quiet (no stdout) when completed. Console output may be used temporarily for debugging during development but must be removed before final implementation.