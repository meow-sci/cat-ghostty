# Implementation Plan: Font Selection UI

## Overview

This implementation plan adds a user-friendly font selection interface to the caTTY terminal emulator. The solution extends the existing CaTTYFontManager with a centralized font registry that maps user-friendly display names to technical font names, handles fonts with varying style availability, and provides an ImGui menu interface for immediate font switching.

The approach builds on the existing font configuration system by adding a registry layer that knows about available fonts and their variants, plus UI integration that allows users to select fonts by display name with immediate application.

## Tasks

**CONSOLE OUTPUT REQUIREMENTS**: All unit tests and property-based tests MUST strive to have no stdout/stderr output under normal conditions to reduce verbosity of console output. Tests should only produce output when:
- A test fails and diagnostic information is needed
- Explicit debugging is enabled via environment variables or test flags
- Critical errors occur that require immediate attention

This requirement applies to all test tasks throughout the implementation plan.

- [x] 1. Create font registry infrastructure
- [x] 1.1 Create FontFamilyDefinition data structure
  - Create `caTTY.Display/Configuration/FontFamilyDefinition.cs`
  - Add properties for DisplayName, FontBaseName, HasRegular, HasBold, HasItalic, HasBoldItalic
  - Add ToString() method for debugging output showing display name and available variants
  - Add XML documentation for all public members
  - _Requirements: 1.1, 1.2, 1.3, 5.4_

- [x] 1.2 Write unit tests for FontFamilyDefinition
  - Test property initialization and default values
  - Test ToString() method output format with different variant combinations
  - Test that HasRegular defaults to true for all font families
  - _Requirements: 1.3, 1.5_

- [x] 1.3 Enhance CaTTYFontManager with font registry
  - Add private static Dictionary<string, FontFamilyDefinition> _fontRegistry field
  - Add private static bool _registryInitialized field
  - Add InitializeFontRegistry() method called from LoadFonts()
  - Add RegisterFontFamily() helper method for adding font families to registry
  - Add GetAvailableFontFamilies() method returning read-only list of display names
  - Add GetFontFamilyDefinition() method for looking up font family by display name
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 5.1, 5.2, 5.3_

- [x] 1.4 Add hardcoded font family definitions
  - Register "Jet Brains Mono" -> "JetBrainsMonoNerdFontMono" with all 4 variants
  - Register "Space Mono" -> "SpaceMonoNerdFontMono" with all 4 variants  
  - Register "Hack" -> "HackNerdFontMono" with all 4 variants
  - Register "Pro Font" -> "ProFontWindowsNerdFontMono" with Regular only
  - Register "Proggy Clean" -> "ProggyCleanNerdFontMono" with Regular only
  - Register "Shure Tech Mono" -> "ShureTechMonoNerdFontMono" with Regular only
  - Register "Departure Mono" -> "DepartureMonoNerdFont" with Regular only
  - Add logging for each registered font family
  - _Requirements: 5.2, 5.3, 5.4, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [x] 1.5 Write property test for font registry completeness
  - **Property 1: Font Registry Completeness and Accuracy**
  - **Validates: Requirements 1.1, 1.2, 1.3, 1.4, 5.2, 5.3, 5.4, 9.1-9.7, 10.1-10.7**

- [x] 2. Add font configuration generation with variant fallback
- [x] 2.1 Add CreateFontConfigForFamily method to CaTTYFontManager
  - Accept displayName and optional fontSize parameters
  - Look up FontFamilyDefinition from registry
  - Create TerminalFontConfig with appropriate font names based on variant availability
  - Use fallback to Regular variant for missing Bold/Italic/BoldItalic variants
  - Return default configuration if font family not found in registry
  - Add comprehensive logging for font configuration generation
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1, 4.2_

- [x] 2.2 Add GetCurrentFontFamily method to CaTTYFontManager
  - Accept TerminalFontConfig parameter
  - Match RegularFontName against registered font base names
  - Return display name of matching font family
  - Return null if no match found in registry
  - Add logging for font family detection results
  - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 2.3 Write property test for font configuration generation
  - **Property 2: Font Configuration Generation with Variant Fallback**
  - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5**

- [x] 2.4 Write property test for current font family detection
  - **Property 4: Current Font Family Detection**
  - **Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

- [x] 2.5 Write unit tests for font configuration generation
  - Test CreateFontConfigForFamily with fonts having all 4 variants
  - Test CreateFontConfigForFamily with fonts having only Regular variant
  - Test fallback behavior for unknown font families
  - Test GetCurrentFontFamily with various TerminalFontConfig inputs
  - Test edge cases (null inputs, empty strings, invalid configurations)
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 6.1, 6.2, 6.3, 6.4, 6.5_

- [-] 3. Add font selection UI to TerminalController
- [x] 3.1 Add font selection state to TerminalController
  - Add private string _currentFontFamily field (default to "Hack")
  - Add InitializeCurrentFontFamily() method to detect current font from configuration
  - Call InitializeCurrentFontFamily() in constructor after font configuration is set
  - Add logging for current font family initialization
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

- [x] 3.2 Add font selection menu to Render method
  - Add RenderFontSelectionMenu() method called from Render()
  - Use ImGui.BeginMenuBar() and ImGui.BeginMenu("Font") for menu structure
  - Iterate through CaTTYFontManager.GetAvailableFontFamilies() for menu items
  - Use ImGui.MenuItem() with selection state to show current font
  - Call SelectFontFamily() when menu item is clicked
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

- [x] 3.3 Add SelectFontFamily method to TerminalController
  - Accept displayName parameter for selected font family
  - Create new TerminalFontConfig using CaTTYFontManager.CreateFontConfigForFamily()
  - Validate the new configuration using Validate() method
  - Call UpdateFontConfig() to apply the new configuration immediately
  - Update _currentFontFamily field to reflect selection
  - Add comprehensive error handling with logging
  - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 7.1, 7.2, 7.3_

- [x] 3.4 Write property test for font selection UI state consistency
  - **Property 3: Font Selection UI State Consistency**
  - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 6.5**

- [x] 3.5 Write unit tests for font selection UI
  - Test InitializeCurrentFontFamily() with various font configurations
  - Test SelectFontFamily() with valid font families
  - Test SelectFontFamily() with invalid font families (error handling)
  - Test that _currentFontFamily is updated correctly after selection
  - Test integration with existing UpdateFontConfig() method
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 4.1, 4.2, 4.3, 4.4, 4.5, 6.1, 6.2, 6.3, 6.4, 6.5_

- [ ] 4. Add comprehensive error handling and validation
- [ ] 4.1 Add error handling to font registry operations
  - Add try-catch blocks in InitializeFontRegistry() with logging
  - Handle cases where font registry initialization fails
  - Provide safe fallbacks when GetFontFamilyDefinition() returns null
  - Add validation in CreateFontConfigForFamily() for invalid inputs
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 4.2 Add error handling to font selection UI
  - Add try-catch blocks in SelectFontFamily() with comprehensive logging
  - Handle TerminalFontConfig validation failures gracefully
  - Handle UpdateFontConfig() failures by maintaining current configuration
  - Provide user feedback for font selection errors (console logging)
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 4.3 Write property test for error handling and graceful degradation
  - **Property 5: Error Handling and Graceful Degradation**
  - **Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

- [ ] 4.4 Write unit tests for error handling scenarios
  - Test behavior when font registry initialization fails
  - Test behavior when CreateFontConfigForFamily() receives invalid inputs
  - Test behavior when SelectFontFamily() receives non-existent font families
  - Test behavior when UpdateFontConfig() fails during font selection
  - Test that terminal continues to function after font selection errors
  - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

- [ ] 5. Integration testing and validation
- [ ] 5.1 Test integration with existing font configuration system
  - Verify that font selection works with existing TerminalFontConfig class
  - Test that font selection uses existing UpdateFontConfig() method correctly
  - Verify that explicit font configuration still works alongside font selection
  - Test that font selection works in both TestApp and GameMod contexts
  - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5_

- [ ] 5.2 Write property test for integration with existing font system
  - **Property 6: Integration with Existing Font System**
  - **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5**

- [ ] 5.3 Test font selection in TestApp context
  - **USER VALIDATION REQUIRED**: Run TestApp and verify font selection menu appears
  - Verify that all registered font families appear in the menu with correct display names
  - Test selecting different font families and verify immediate font changes
  - Test that fonts with 4 variants render Bold/Italic/BoldItalic correctly
  - Test that fonts with only Regular variant use Regular for all styles
  - Verify that current font is highlighted correctly in menu
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 8.4_

- [ ] 5.4 Test font selection in GameMod context
  - **USER VALIDATION REQUIRED**: Run GameMod and verify font selection menu works in game
  - Test that font selection doesn't interfere with game input or rendering
  - Verify that font changes apply immediately without affecting game performance
  - Test that font selection persists correctly during game session
  - Verify that all font families work correctly in game context
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 8.4_

- [ ] 5.5 Test font variant fallback behavior
  - **USER VALIDATION REQUIRED**: Test fonts with different variant availability
  - Select "Jet Brains Mono" and verify Bold/Italic/BoldItalic text renders with appropriate variants
  - Select "Pro Font" and verify Bold/Italic/BoldItalic text renders using Regular variant
  - Test other fonts with limited variants (Proggy Clean, Shure Tech Mono, Departure Mono)
  - Verify that fallback behavior is transparent to the user
  - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [ ] 6. Documentation and cleanup
- [ ] 6.1 Update XML documentation
  - Add comprehensive XML documentation to FontFamilyDefinition class
  - Add XML documentation to new CaTTYFontManager methods
  - Add XML documentation to new TerminalController font selection methods
  - Document font registry initialization and font family registration process
  - _Requirements: Developer experience and maintainability_

- [ ] 6.2 Add font selection usage examples
  - Document how to add new font families to the registry
  - Document the font variant fallback logic and when it applies
  - Create examples showing font selection in both TestApp and GameMod contexts
  - Document troubleshooting steps for font selection issues
  - _Requirements: Developer experience and maintainability_

- [ ] 7. Final validation and testing
- [ ] 7.1 Comprehensive integration testing
  - **USER VALIDATION REQUIRED**: Final validation that font selection works correctly in both contexts
  - Test all registered font families in both TestApp and GameMod
  - Verify that font selection doesn't break existing explicit font configuration
  - Test that font selection persists correctly during session
  - Confirm that error handling works correctly for invalid selections
  - _Requirements: All requirements_

- [ ] 7.2 Performance validation
  - Verify that font selection menu doesn't impact rendering performance
  - Test that font switching doesn't cause frame rate drops
  - Ensure that font registry initialization doesn't add significant startup time
  - Test font selection performance with rapid font switching
  - _Requirements: Performance and user experience_

- [ ] 8. Checkpoint - Font selection UI complete
  - Font selection menu appears in both TestApp and GameMod terminal windows
  - All registered font families are selectable with user-friendly display names
  - Font variant fallback works correctly for fonts with limited variants
  - Font changes apply immediately without requiring restart
  - Error handling prevents crashes and maintains terminal functionality
  - Integration with existing font configuration system is seamless

## Notes

- Each task references specific requirements for traceability
- The implementation builds on the existing font configuration system without breaking compatibility
- Font selection provides immediate visual feedback through the terminal rendering
- Comprehensive error handling ensures robust operation even with invalid font selections
- The font registry is hardcoded with the specific font list provided by the user
- Font variant fallback logic handles the different availability patterns transparently