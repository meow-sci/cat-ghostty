s# Requirements Document

## Introduction

The caTTY terminal emulator has a DPI scaling issue where the GameMod version displays incorrect character spacing and font sizing compared to the standalone TestApp. The TestApp renders correctly with proper character alignment, but the GameMod shows text that appears to be scaled incorrectly, likely by a factor of 2.0 (matching the user's Windows DPI scaling). This issue needs to be resolved by making DPI scaling configurable in the shared ImGui controller code.

## Glossary

- **DPI_Scaling**: Display scaling factor applied by Windows for high-DPI displays
- **TestApp**: Standalone BRUTAL ImGui application that works correctly
- **GameMod**: KSA game mod version that inherits game's DPI context
- **TerminalController**: Shared ImGui controller used by both TestApp and GameMod
- **Character_Metrics**: Font size, character width, and line height values used for terminal rendering
- **Scaling_Configuration**: Configurable parameters to adjust rendering for different DPI contexts

## Requirements

### Requirement 1: DPI Scaling Detection

**User Story:** As a developer, I want the terminal controller to detect DPI scaling differences between TestApp and GameMod contexts, so that rendering can be adjusted appropriately.

#### Acceptance Criteria

1. WHEN the TerminalController is initialized, THE System SHALL detect the current DPI scaling context
2. WHEN running in TestApp context, THE System SHALL identify it as a standalone DPI-aware application
3. WHEN running in GameMod context, THE System SHALL identify it as inheriting game's DPI context
4. THE System SHALL provide a way to query the detected DPI scaling factor
5. THE System SHALL log the detected DPI context for debugging purposes

### Requirement 2: Configurable Character Metrics

**User Story:** As a developer, I want to configure character metrics (font size, width, height) based on the DPI context, so that text renders with correct spacing in both TestApp and GameMod.

#### Acceptance Criteria

1. THE TerminalController SHALL accept configurable character metrics during initialization
2. WHEN no metrics are provided, THE System SHALL use default values appropriate for the detected context
3. WHEN custom metrics are provided, THE System SHALL use those values for all character positioning calculations
4. THE System SHALL support separate configuration of font size, character width, and line height
5. THE System SHALL validate that provided metrics are within reasonable bounds

### Requirement 3: Context-Aware Default Metrics

**User Story:** As a developer, I want the system to automatically choose appropriate default metrics based on the execution context, so that minimal configuration is required for correct rendering.

#### Acceptance Criteria

1. WHEN running in TestApp context, THE System SHALL use standard metrics (fontSize=16.0f, charWidth=9.6f, lineHeight=18.0f)
2. WHEN running in GameMod context, THE System SHALL apply DPI scaling compensation to the metrics
3. WHEN DPI scaling factor is 2.0, THE System SHALL use half-sized metrics to compensate for game scaling
4. THE System SHALL provide a way to override automatic metric selection
5. THE System SHALL maintain consistent character grid alignment regardless of scaling

### Requirement 4: Backward Compatibility

**User Story:** As a developer, I want existing TestApp and GameMod code to continue working without changes, so that the fix doesn't break current functionality.

#### Acceptance Criteria

1. THE existing TestApp SHALL continue to render correctly without code changes
2. THE existing GameMod SHALL render correctly after the fix without requiring initialization changes
3. THE TerminalController constructor SHALL remain compatible with existing calling code
4. WHEN no scaling configuration is provided, THE System SHALL automatically detect and apply appropriate settings
5. THE System SHALL not change the public API of ITerminalController interface

### Requirement 5: Runtime Metric Adjustment

**User Story:** As a developer, I want to adjust character metrics at runtime, so that I can fine-tune rendering without restarting the application.

#### Acceptance Criteria

1. THE TerminalController SHALL provide methods to update character metrics at runtime
2. WHEN metrics are changed, THE System SHALL immediately apply the new values to subsequent rendering
3. THE System SHALL recalculate all character positions based on the new metrics
4. THE System SHALL maintain cursor position accuracy after metric changes
5. THE System SHALL provide validation for runtime metric changes

### Requirement 6: Configuration Validation and Debugging

**User Story:** As a developer, I want comprehensive logging and validation of DPI scaling configuration, so that I can diagnose and fix rendering issues.

#### Acceptance Criteria

1. THE System SHALL log the detected DPI context and chosen metrics during initialization
2. THE System SHALL validate that character metrics produce reasonable terminal dimensions
3. WHEN invalid metrics are detected, THE System SHALL log warnings and use fallback values
4. THE System SHALL provide debug information about character positioning calculations
5. THE System SHALL expose current metrics through read-only properties for debugging