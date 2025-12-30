# Requirements Document

## Introduction

The caTTY terminal emulator currently has a font configuration system that supports different font families and styles, but users must manually specify font names in code. This enhancement adds a user-friendly font selection interface that allows users to choose from available font families through a menu system. The system needs to handle fonts with varying style availability - some fonts have Regular, Bold, Italic, and BoldItalic variants, while others only have a Regular variant.

## Glossary

- **Font_Family**: A group of related fonts sharing the same base name (e.g., "Jet Brains Mono")
- **Font_Variants**: The available styles within a font family (Regular, Bold, Italic, BoldItalic)
- **Display_Name**: User-friendly name shown in the UI (e.g., "Jet Brains Mono" instead of "JetBrainsMonoNerdFontMono")
- **Font_Registry**: Centralized system that knows about available fonts and their variants
- **CaTTYFontManager**: Existing font management system that loads fonts from files
- **Font_Selection_UI**: User interface menu for selecting font families
- **Variant_Fallback**: Logic to handle fonts with missing style variants by falling back to Regular

## Requirements

### Requirement 1: Font Family Registry

**User Story:** As a developer, I want a centralized registry of available font families, so that the system knows which fonts are available and their variants.

#### Acceptance Criteria

1. THE CaTTYFontManager SHALL maintain a registry of available font families with their display names
2. THE registry SHALL map display names to font file names (e.g., "Jet Brains Mono" → "JetBrainsMonoNerdFontMono-Regular")
3. THE registry SHALL track which variants are available for each font family
4. THE registry SHALL be hardcoded with the well-defined font list provided
5. THE registry SHALL support fonts with different variant availability (4 variants vs Regular-only)

### Requirement 2: Font Variant Detection and Fallback

**User Story:** As a user, I want the system to handle fonts with missing style variants gracefully, so that font selection works regardless of variant availability.

#### Acceptance Criteria

1. WHEN a font family has all four variants THEN THE system SHALL use the appropriate variant for each style
2. WHEN a font family has only Regular variant THEN THE system SHALL use Regular for all styles (Bold, Italic, BoldItalic)
3. THE system SHALL detect available variants automatically based on the registry
4. THE system SHALL apply fallback logic transparently without user intervention
5. THE system SHALL log which variants are being used for debugging purposes

### Requirement 3: Font Selection User Interface

**User Story:** As a user, I want a menu to select font families by display name, so that I can easily change the terminal font without editing code.

#### Acceptance Criteria

1. THE terminal display window SHALL include a font selection menu
2. THE menu SHALL show user-friendly display names (not technical font names)
3. WHEN a font is selected THEN THE system SHALL immediately update the terminal font configuration
4. THE menu SHALL be accessible through the existing ImGui interface
5. THE selected font SHALL persist for the current session

### Requirement 4: Dynamic Font Configuration Updates

**User Story:** As a user, I want font changes to apply immediately, so that I can see the effect of different fonts without restarting.

#### Acceptance Criteria

1. WHEN a font is selected from the menu THEN THE system SHALL create a new TerminalFontConfig with appropriate variants
2. THE system SHALL call UpdateFontConfig() to apply the new configuration immediately
3. THE terminal SHALL re-render with the new font without requiring restart
4. THE character metrics SHALL be recalculated for the new font
5. THE cursor position SHALL remain accurate after font changes

### Requirement 5: Font Registry Initialization

**User Story:** As a developer, I want the font registry to be initialized with the available fonts, so that the selection menu shows the correct options.

#### Acceptance Criteria

1. THE CaTTYFontManager SHALL initialize the font registry with hardcoded font family definitions
2. THE registry SHALL include display names: "Jet Brains Mono", "Pro Font", "Proggy Clean", "Shure Tech Mono", "Space Mono", "Departure Mono", "Hack"
3. THE registry SHALL map display names to font file base names
4. THE registry SHALL specify which variants are available for each font family
5. THE initialization SHALL happen during font loading process

### Requirement 6: Font Selection State Management

**User Story:** As a user, I want the font selection to remember the current font, so that the menu shows which font is currently active.

#### Acceptance Criteria

1. THE font selection menu SHALL indicate which font is currently selected
2. THE system SHALL track the current font family selection
3. WHEN the menu is opened THEN THE current font SHALL be highlighted or marked
4. THE font selection state SHALL be maintained during the session
5. THE system SHALL handle cases where the current font is not in the registry

### Requirement 7: Error Handling and Validation

**User Story:** As a user, I want the font selection to handle errors gracefully, so that invalid selections don't crash the terminal.

#### Acceptance Criteria

1. WHEN an invalid font is selected THEN THE system SHALL log an error and maintain the current font
2. WHEN font loading fails THEN THE system SHALL fall back to the previous working font
3. THE system SHALL validate font selections before applying them
4. THE system SHALL handle missing font files gracefully
5. THE system SHALL provide user feedback for font selection errors

### Requirement 8: Integration with Existing Font System

**User Story:** As a developer, I want the font selection UI to integrate seamlessly with the existing font configuration system, so that existing functionality continues to work.

#### Acceptance Criteria

1. THE font selection SHALL use the existing TerminalFontConfig class
2. THE font selection SHALL work with the existing UpdateFontConfig() method
3. THE font selection SHALL not break existing explicit font configuration
4. THE font selection SHALL work in both TestApp and GameMod contexts
5. THE font selection SHALL maintain backward compatibility with existing code

### Requirement 9: Font Display Name Mapping

**User Story:** As a user, I want to see friendly font names in the selection menu, so that I can easily identify fonts without knowing technical names.

#### Acceptance Criteria

1. THE system SHALL display "Jet Brains Mono" instead of "JetBrainsMonoNerdFontMono-Regular"
2. THE system SHALL display "Pro Font" instead of "ProFontWindowsNerdFontMono-Regular"
3. THE system SHALL display "Proggy Clean" instead of "ProggyCleanNerdFontMono-Regular"
4. THE system SHALL display "Shure Tech Mono" instead of "ShureTechMonoNerdFontMono-Regular"
5. THE system SHALL display "Space Mono" instead of "SpaceMonoNerdFontMono-Regular"
6. THE system SHALL display "Departure Mono" instead of "DepartureMonoNerdFont-Regular"
7. THE system SHALL display "Hack" instead of "HackNerdFontMono-Regular"

### Requirement 10: Font Variant Availability Handling

**User Story:** As a developer, I want the system to know which fonts have which variants available, so that fallback logic works correctly.

#### Acceptance Criteria

1. THE system SHALL know that Jet Brains Mono has Regular, Bold, Italic, BoldItalic variants
2. THE system SHALL know that Space Mono has Regular, Bold, Italic, BoldItalic variants  
3. THE system SHALL know that Hack has Regular, Bold, Italic, BoldItalic variants
4. THE system SHALL know that Pro Font has only Regular variant
5. THE system SHALL know that Proggy Clean has only Regular variant
6. THE system SHALL know that Shure Tech Mono has only Regular variant
7. THE system SHALL know that Departure Mono has only Regular variant