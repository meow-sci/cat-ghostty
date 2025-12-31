# Design Document

## Overview

This design outlines a comprehensive TOML-based terminal theming system for the caTTY terminal emulator. The system will replace the current hardcoded theme approach with a flexible, file-based theme management system that allows users to load and switch between terminal color themes defined in TOML configuration files. Additionally, the design includes global opacity control and UI simplification to create a cleaner terminal experience.

The implementation will be built on top of the existing `TerminalTheme.cs` infrastructure in the `caTTY.Display` project, extending it with TOML file loading capabilities, runtime theme switching, and enhanced UI controls.

## Architecture

### Component Overview

The theming system follows a layered architecture:

1. **Theme Loading Layer**: Handles TOML file discovery, parsing, and validation
2. **Theme Management Layer**: Manages theme lifecycle, persistence, and state
3. **UI Integration Layer**: Provides menu controls for theme selection and settings
4. **Rendering Layer**: Applies themes and opacity to terminal display

### Key Components

- **TomlThemeLoader**: Discovers and parses TOML theme files from the filesystem
- **ThemeManager** (Enhanced): Extended to support TOML themes and persistence
- **TerminalController** (Enhanced): Updated UI with theme menu and settings
- **OpacityManager**: Manages global terminal opacity settings
- **ThemeConfiguration**: Handles theme persistence and application state

## Components and Interfaces

### TomlThemeLoader

```csharp
public static class TomlThemeLoader
{
    public static List<TerminalTheme> LoadThemesFromDirectory(string themesDirectory);
    public static TerminalTheme? LoadThemeFromFile(string filePath);
    private static TerminalColorPalette ParseColorPalette(TomlDocument tomlDocument);
    private static float4 ParseHexColor(string hexColor);
    private static string GetThemeDisplayName(string filePath);
}
```

**Responsibilities:**
- Discover TOML files in the TerminalThemes directory
- Parse TOML structure using Samboy063.Tomlet library and validate required sections
- Convert hex color values to float4 format
- Handle parsing errors gracefully
- Extract theme display names from filenames

**TOML Library Integration:**
Uses the existing Samboy063.Tomlet library reference from KSA assemblies. The library provides TOML parsing capabilities through its API for reading TOML documents and accessing nested values.

### Enhanced ThemeManager

```csharp
public static class ThemeManager
{
    // Existing properties and methods remain unchanged
    public static TerminalTheme CurrentTheme { get; private set; }
    public static readonly TerminalTheme DefaultTheme; // Updated with Adventure.toml values
    
    // New TOML theme support
    public static List<TerminalTheme> AvailableThemes { get; private set; }
    public static void InitializeThemes();
    public static void ApplyTheme(string themeName);
    public static void RefreshAvailableThemes();
    
    // Theme persistence
    public static void SaveThemePreference(string themeName);
    public static string? LoadThemePreference();
    
    // Events
    public static event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
}
```

**Responsibilities:**
- Initialize and manage the collection of available themes
- Load TOML themes on startup using TomlThemeLoader
- Provide fallback to default theme when TOML files are unavailable
- Persist user theme selection across sessions
- Notify components when themes change

### OpacityManager

```csharp
public static class OpacityManager
{
    public static float GlobalOpacity { get; private set; } = 1.0f;
    
    public static void SetOpacity(float opacity);
    public static void SaveOpacityPreference(float opacity);
    public static float LoadOpacityPreference();
    
    public static event EventHandler<OpacityChangedEventArgs>? OpacityChanged;
}
```

**Responsibilities:**
- Manage global terminal opacity setting
- Validate opacity values (0.0 to 1.0 range)
- Persist opacity preference across sessions
- Notify rendering components when opacity changes

### Enhanced TerminalController

The existing `TerminalController` will be updated with:

```csharp
// New menu rendering methods
private void RenderThemeMenu();
private void RenderSettingsMenu();

// Simplified UI rendering
private void RenderTerminalCanvas(); // Replaces complex tab/info rendering

// Theme integration
private void OnThemeChanged(object sender, ThemeChangedEventArgs e);
private void OnOpacityChanged(object sender, OpacityChangedEventArgs e);
```

**UI Changes:**
- Add "Theme" menu with theme selection options
- Add "Settings" menu with opacity slider
- Remove tab bar and info display from main rendering area
- Apply opacity to all terminal canvas rendering operations

### ThemeConfiguration

```csharp
public class ThemeConfiguration
{
    public string? SelectedThemeName { get; set; }
    public float GlobalOpacity { get; set; } = 1.0f;
    
    public static ThemeConfiguration Load();
    public void Save();
    
    private static string GetConfigFilePath();
}
```

**Responsibilities:**
- Serialize/deserialize theme preferences to JSON configuration file
- Provide default values when configuration doesn't exist
- Handle configuration file access errors gracefully

## Data Models

### TOML Theme File Structure

The TOML theme files follow this standardized structure:

```toml
[colors.normal]
black = '#040404'
red = '#d84a33'
green = '#5da602'
yellow = '#eebb6e'
blue = '#417ab3'
magenta = '#e5c499'
cyan = '#bdcfe5'
white = '#dbded8'

[colors.bright]
black = '#685656'
red = '#d76b42'
green = '#99b52c'
yellow = '#ffb670'
blue = '#97d7ef'
magenta = '#aa7900'
cyan = '#bdcfe5'
white = '#e4d5c7'

[colors.primary]
background = '#040404'
foreground = '#feffff'

[colors.cursor]
cursor = '#feffff'
text = '#000000'

[colors.selection]
background = '#606060'
text = '#ffffff'
```

### Enhanced TerminalTheme Structure

The existing `TerminalTheme` struct will be extended to support TOML-loaded themes:

```csharp
public readonly struct TerminalTheme
{
    public string Name { get; }
    public ThemeType Type { get; }
    public TerminalColorPalette Colors { get; }
    public CursorConfig Cursor { get; }
    public ThemeSource Source { get; } // New: indicates if theme is built-in or TOML-loaded
    public string? FilePath { get; } // New: path to TOML file for loaded themes
}

public enum ThemeSource
{
    BuiltIn,
    TomlFile
}
```

### Event Arguments

```csharp
public class ThemeChangedEventArgs : EventArgs
{
    public TerminalTheme PreviousTheme { get; }
    public TerminalTheme NewTheme { get; }
}

public class OpacityChangedEventArgs : EventArgs
{
    public float PreviousOpacity { get; }
    public float NewOpacity { get; }
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to eliminate redundancy:

- Properties 5.3, 5.4, and 5.5 (TOML section mapping) can be combined into a single comprehensive property about TOML structure validation
- Properties 7.2, 7.3, and 7.5 (opacity application) can be combined into a comprehensive opacity behavior property
- Properties 9.1, 9.2, and 9.3 (error handling) can be combined into a comprehensive error resilience property

### Core Properties

**Property 1: Theme Discovery Completeness**
*For any* directory containing TOML files with .toml extensions, the theme discovery process should find and return all valid theme files in that directory
**Validates: Requirements 1.1**

**Property 2: TOML Theme Parsing Consistency**
*For any* valid TOML theme file with all required color sections, parsing should successfully create a theme object with all colors correctly mapped
**Validates: Requirements 1.2, 5.3, 5.4, 5.5**

**Property 3: Theme Validation Completeness**
*For any* TOML file missing required color sections (colors.normal, colors.bright, colors.primary, colors.cursor, colors.selection), the validation should reject the file and log an appropriate error
**Validates: Requirements 1.3, 1.4**

**Property 4: Theme Name Extraction Consistency**
*For any* file path ending with .toml extension, the display name extraction should return the filename without the .toml extension
**Validates: Requirements 1.5**

**Property 5: Assembly Path Resolution Consistency**
*For any* assembly location, the theme directory path construction should correctly append "TerminalThemes/" to the assembly directory path
**Validates: Requirements 3.1, 3.2**

**Property 6: Theme Menu Content Completeness**
*For any* set of available themes (including TOML-loaded and built-in themes), the theme menu should display all themes with their correct display names
**Validates: Requirements 4.2, 4.5**

**Property 7: Theme Application Completeness**
*For any* theme selection, applying the theme should update all terminal color properties including foreground, background, cursor, selection, and all 16 ANSI colors
**Validates: Requirements 4.3, 4.4**

**Property 8: Hex Color Parsing Round-Trip**
*For any* valid hex color string (e.g., '#ff6188'), parsing to float4 and converting back should preserve the color values within acceptable precision
**Validates: Requirements 5.1, 5.2**

**Property 9: Theme Persistence Round-Trip**
*For any* theme selection, saving the preference and loading it back should return the same theme name
**Validates: Requirements 6.1**

**Property 10: Theme Change Notification Consistency**
*For any* theme change operation, all registered event handlers should be notified with the correct previous and new theme information
**Validates: Requirements 6.4**

**Property 11: Opacity Application Completeness**
*For any* opacity value between 0.0 and 1.0, applying the opacity should affect all terminal rendering elements (text, background, cursor) and update the display immediately
**Validates: Requirements 7.2, 7.3, 7.5**

**Property 12: Opacity Persistence Round-Trip**
*For any* opacity value, saving the preference and loading it back should return the same opacity value within acceptable precision
**Validates: Requirements 7.4**

**Property 13: Terminal Canvas Space Utilization**
*For any* window size, the terminal canvas should utilize the full available space after accounting for menu bar height
**Validates: Requirements 8.3**

**Property 14: Error Handling Resilience**
*For any* combination of invalid TOML files, malformed hex colors, or file system errors, the system should continue functioning with at least the default theme available
**Validates: Requirements 9.1, 9.2, 9.3, 9.5**

## Error Handling

### TOML File Processing Errors

**Invalid TOML Syntax**: When TOML files contain syntax errors, the system will log the specific error and filename, then continue processing other theme files. The invalid file will be skipped and not appear in the theme menu.

**Missing Required Sections**: Theme files missing any of the required color sections (colors.normal, colors.bright, colors.primary, colors.cursor, colors.selection) will be rejected during validation. The system will log which sections are missing and skip the file.

**Invalid Hex Colors**: Malformed hex color values (e.g., invalid characters, wrong length) will trigger a warning log entry. The system will use a fallback color (white for foreground colors, black for background colors) and continue processing the theme.

### File System Errors

**Missing TerminalThemes Directory**: If the TerminalThemes directory doesn't exist, the system will log an informational message and use only the built-in default theme.

**File Access Permissions**: If theme files cannot be read due to permission issues, the system will log the error and skip those specific files while processing any accessible files.

**Assembly Location Detection Failure**: If the assembly location cannot be determined, the system will fall back to using the current working directory and log a warning.

### Runtime Errors

**Theme Application Failures**: If applying a selected theme fails (e.g., due to corrupted theme data), the system will revert to the default theme and log the error.

**Configuration Persistence Failures**: If theme preferences cannot be saved or loaded, the system will log the error and continue with default settings.

### Error Recovery Strategy

All error conditions follow a consistent recovery pattern:
1. Log the specific error with context information
2. Continue processing other valid items when possible
3. Fall back to safe defaults (built-in default theme)
4. Ensure the terminal remains functional regardless of theme system failures

## Testing Strategy

### Dual Testing Approach

The testing strategy employs both unit tests and property-based tests to ensure comprehensive coverage:

**Unit Tests**: Focus on specific examples, edge cases, and error conditions including:
- Loading specific known theme files (Adventure.toml, Monokai Pro.toml)
- Testing with empty directories and missing files
- Validating specific hex color conversions
- Testing UI menu interactions with known theme sets
- Error scenarios with malformed TOML files

**Property-Based Tests**: Verify universal properties across all inputs including:
- Theme discovery with randomly generated directory structures
- TOML parsing with randomly generated valid theme files
- Hex color parsing with randomly generated valid hex values
- Theme application with randomly generated theme configurations
- Error handling with randomly generated invalid inputs

### Property-Based Testing Configuration

Each property test will run a minimum of 100 iterations to ensure comprehensive input coverage. Tests will be tagged with references to their corresponding design properties:

- **Feature: toml-terminal-theming, Property 1**: Theme Discovery Completeness
- **Feature: toml-terminal-theming, Property 2**: TOML Theme Parsing Consistency
- **Feature: toml-terminal-theming, Property 3**: Theme Validation Completeness
- And so forth for all 14 properties

### Testing Framework

The implementation will use **NUnit 4.x** with **FsCheck.NUnit** for property-based testing, following the established C# testing patterns in the caTTY project. **TOML parsing will utilize the existing Samboy063.Tomlet library reference from KSA assemblies.** Tests will be organized in the `caTTY.Display.Tests` project with the following structure:

```
caTTY.Display.Tests/
├── Unit/
│   ├── TomlThemeLoaderTests.cs
│   ├── ThemeManagerTests.cs
│   ├── OpacityManagerTests.cs
│   └── ThemeConfigurationTests.cs
├── Property/
│   ├── ThemeDiscoveryProperties.cs
│   ├── TomlParsingProperties.cs
│   ├── ColorConversionProperties.cs
│   └── ThemePersistenceProperties.cs
└── Integration/
    ├── ThemeSystemIntegrationTests.cs
    └── UIIntegrationTests.cs
```

### Test Data Management

Property tests will use smart generators that:
- Generate valid TOML theme files with all required sections
- Create invalid TOML files with specific types of errors (missing sections, syntax errors, invalid colors)
- Generate valid hex color values in various formats (#RGB, #RRGGBB)
- Create directory structures with varying numbers of theme files
- Generate edge cases like very long filenames, special characters, and Unicode content

Unit tests will use the existing theme files in `caTTY.Display.Tests/TerminalThemes/` as known good test data, ensuring compatibility with the current theme collection.

<function_calls>
<invoke name="prework">
<parameter name="featureName">toml-terminal-theming