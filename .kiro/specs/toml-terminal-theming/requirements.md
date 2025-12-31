# Requirements Document

## Introduction

This specification defines a TOML-based terminal theming system for the caTTY terminal emulator. The system will replace the current hardcoded theme approach with a flexible, file-based theme management system that allows users to load and switch between terminal color themes defined in TOML configuration files.

## Glossary

- **Theme_Manager**: Component responsible for loading, managing, and applying terminal themes
- **TOML_Theme_File**: Configuration file in TOML format containing terminal color definitions
- **Theme_Menu**: User interface component for selecting and applying themes
- **Default_Theme**: Fallback theme defined in code when no TOML files are available
- **Terminal_Controller**: UI component that integrates theme management with the terminal display
- **Settings_Menu**: User interface component for configuring terminal settings including opacity
- **Opacity_Control**: Slider control for adjusting global terminal transparency
- **Terminal_Canvas**: The row/column rendering area where terminal content is displayed

## Requirements

### Requirement 1: TOML Theme File Loading

**User Story:** As a developer, I want the system to load theme definitions from TOML files, so that themes can be easily configured and distributed.

#### Acceptance Criteria

1. WHEN the Theme_Manager initializes, THE system SHALL discover TOML files in the TerminalThemes directory relative to the assembly location
2. WHEN a TOML theme file is found, THE Theme_Manager SHALL parse the file and create a theme object with the defined colors
3. WHEN parsing a TOML file, THE system SHALL validate that all required color sections exist (colors.normal, colors.bright, colors.primary, colors.cursor, colors.selection)
4. WHEN a TOML file has invalid format or missing required sections, THE system SHALL log an error and skip that theme file
5. WHEN determining the theme display name, THE system SHALL use the filename without the .toml extension

### Requirement 2: Default Theme Fallback

**User Story:** As a user, I want the system to have a working default theme, so that the terminal remains functional even when no theme files are available.

#### Acceptance Criteria

1. WHEN no TOML theme files are found in the TerminalThemes directory, THE system SHALL use the Default_Theme defined in code
2. WHEN the Default_Theme is used, THE system SHALL use the color values from the Adventure.toml theme as the baseline
3. WHEN the system starts, THE Default_Theme SHALL be applied initially before any user theme selection
4. WHEN all TOML theme files fail to load, THE system SHALL fall back to the Default_Theme

### Requirement 3: Runtime Theme Discovery

**User Story:** As a developer, I want themes to be discovered at runtime using the assembly location, so that themes are portable with the application.

#### Acceptance Criteria

1. WHEN determining the theme directory path, THE system SHALL use Assembly.GetExecutingAssembly().Location to find the DLL directory
2. WHEN constructing the theme path, THE system SHALL append "TerminalThemes/" to the assembly directory path
3. WHEN the TerminalThemes directory does not exist, THE system SHALL handle the missing directory gracefully and use the Default_Theme
4. WHEN new theme files are added to the TerminalThemes directory, THE system SHALL be able to discover them on the next application restart

### Requirement 4: Theme Management Interface

**User Story:** As a user, I want to select and apply different terminal themes through a menu interface, so that I can customize the terminal appearance.

#### Acceptance Criteria

1. WHEN the terminal UI is displayed, THE system SHALL provide a theme selection menu
2. WHEN the theme menu is opened, THE system SHALL display all available themes including loaded TOML themes and the Default_Theme
3. WHEN a user selects a theme from the menu, THE system SHALL apply the selected theme immediately to the terminal display
4. WHEN a theme is applied, THE system SHALL update all terminal colors including foreground, background, cursor, selection, and ANSI colors
5. WHEN the theme menu displays theme names, THE system SHALL show user-friendly names derived from the TOML filenames

### Requirement 5: TOML Color Format Support

**User Story:** As a theme creator, I want to define colors using standard hex color format in TOML files, so that themes are easy to create and maintain.

#### Acceptance Criteria

1. WHEN parsing color values from TOML files, THE system SHALL support hex color format (e.g., '#ff6188')
2. WHEN a hex color is parsed, THE system SHALL convert it to the internal float4 color representation
3. WHEN the TOML file contains the colors.normal section, THE system SHALL map the values to standard ANSI colors (black, red, green, yellow, blue, magenta, cyan, white)
4. WHEN the TOML file contains the colors.bright section, THE system SHALL map the values to bright ANSI colors
5. WHEN the TOML file contains colors.primary, colors.cursor, and colors.selection sections, THE system SHALL map these to the corresponding terminal UI colors

### Requirement 6: Theme Persistence and State Management

**User Story:** As a user, I want my selected theme to be remembered across application sessions, so that I don't have to reselect my preferred theme every time.

#### Acceptance Criteria

1. WHEN a user selects a theme, THE system SHALL store the theme selection in application configuration
2. WHEN the application starts, THE system SHALL load the previously selected theme if it is still available
3. WHEN a previously selected theme file is no longer available, THE system SHALL fall back to the Default_Theme
4. WHEN the theme state changes, THE system SHALL notify any dependent components that need to update their rendering

### Requirement 7: Global Terminal Opacity Control

**User Story:** As a user, I want to control the overall opacity of the terminal display, so that I can achieve transparency effects and customize the visual appearance.

#### Acceptance Criteria

1. WHEN the terminal UI is displayed, THE system SHALL provide a Settings menu with an opacity slider control
2. WHEN the opacity slider is adjusted, THE system SHALL apply the opacity value to all terminal content rendered to the row/column canvas
3. WHEN opacity is changed, THE system SHALL update the terminal display immediately without requiring a restart
4. WHEN the opacity setting is modified, THE system SHALL persist the value across application sessions
5. WHEN the terminal renders content, THE system SHALL apply the global opacity to all painted elements including text, background, and cursor

### Requirement 8: Simplified Terminal UI Layout

**User Story:** As a user, I want a clean terminal interface without distracting UI elements, so that I can focus on the terminal content.

#### Acceptance Criteria

1. WHEN the terminal window is displayed, THE system SHALL render only the terminal row/column canvas content
2. WHEN the UI is rendered, THE system SHALL NOT display tab bars or info displays within the terminal window
3. WHEN the terminal content is painted, THE system SHALL use the full available window space for the terminal canvas
4. WHEN menus are accessed, THE system SHALL provide theme and settings options through appropriate menu controls
5. WHEN the simplified layout is active, THE system SHALL maintain all terminal functionality while removing visual clutter

### Requirement 9: Error Handling and Validation

**User Story:** As a developer, I want robust error handling for theme loading, so that invalid theme files don't crash the application.

#### Acceptance Criteria

1. WHEN a TOML file cannot be parsed due to syntax errors, THE system SHALL log the error and continue loading other themes
2. WHEN a TOML file is missing required color sections, THE system SHALL log a validation error and skip that theme
3. WHEN a hex color value is invalid or malformed, THE system SHALL log an error and use a fallback color value
4. WHEN file system access fails during theme discovery, THE system SHALL handle the exception gracefully and use the Default_Theme
5. WHEN theme loading encounters any errors, THE system SHALL ensure the terminal remains functional with at least the Default_Theme available