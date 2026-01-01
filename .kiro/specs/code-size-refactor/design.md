# Design Document

## Overview

This design outlines a systematic refactoring approach to decompose oversized files in the caTTY C# codebase into smaller, maintainable components. The refactoring will preserve all existing functionality while optimizing for AI/LLM tooling and developer productivity.

The approach focuses on logical decomposition based on single responsibility principle, clear separation of concerns, and maintaining existing architectural patterns. All public APIs will remain unchanged, ensuring zero breaking changes.

## Architecture

### Current State Analysis

**Critical Files Requiring Refactoring:**
- `TerminalController.cs` (4979 lines) - Display layer controller with mixed responsibilities
- `TerminalEmulator.cs` (2465 lines) - Core terminal logic with multiple concerns
- `ProcessManager.cs` (947 lines) - Process management with ConPTY integration
- `TerminalParserHandlers.cs` (917 lines) - Parser event handlers
- `SgrParser.cs` (879 lines) - SGR sequence parsing logic

**Architectural Principles to Preserve:**
- Headless Core library with no UI dependencies
- Display library with ImGui integration
- Manager pattern for state management
- Parser state machine integrity
- Immutable data patterns and Result<T> usage

### Refactoring Strategy

**Three-Phase Approach:**
1. **Extraction Phase** - Extract logical components into separate files
2. **Interface Phase** - Define clear interfaces between components  
3. **Integration Phase** - Wire components together maintaining existing behavior

**Size Targets:**
- Ideal: ≤200 lines per file
- Acceptable: ≤500 lines per file
- Maximum: ≤1000 lines per file

## Components and Interfaces

### Phase 1: TerminalController Decomposition

**Current Responsibilities (4979 lines):**
- Layout management and UI rendering
- Font management and character metrics
- Input handling and focus management
- Window resize handling
- Session management integration
- Theme and configuration management
- Mouse input processing

**Proposed Decomposition:**

```csharp
// Core controller (≤300 lines)
public class TerminalController : ITerminalController, IDisposable
{
    private readonly ITerminalLayoutManager _layoutManager;
    private readonly ITerminalFontManager _fontManager;
    private readonly ITerminalInputHandler _inputHandler;
    private readonly ITerminalResizeHandler _resizeHandler;
    private readonly ITerminalRenderingEngine _renderingEngine;
}

// Layout management (≤250 lines)
public class TerminalLayoutManager : ITerminalLayoutManager
{
    // Menu bar, tab area, settings area layout logic
    // Window size calculations and constraints
}

// Font management (≤400 lines)  
public class TerminalFontManager : ITerminalFontManager
{
    // Font loading, selection, and character metrics
    // Font configuration and validation
}

// Input handling (≤300 lines)
public class TerminalInputHandler : ITerminalInputHandler
{
    // Keyboard input processing
    // Focus management and input capture
}

// Resize handling (≤200 lines)
public class TerminalResizeHandler : ITerminalResizeHandler
{
    // Window resize detection and processing
    // Terminal dimension calculations
}

// Rendering engine (≤400 lines)
public class TerminalRenderingEngine : ITerminalRenderingEngine
{
    // ImGui rendering logic
    // Theme application and visual effects
}
```

### Phase 2: TerminalEmulator Decomposition

**Current Responsibilities (2465 lines):**
- Terminal state management
- Character processing and display
- Cursor management integration
- Scrolling and viewport management
- Window manipulation handling
- Color and attribute management
- Response emission

**Proposed Decomposition:**

```csharp
// Core emulator (≤300 lines)
public class TerminalEmulator : ITerminalEmulator, ICursorPositionProvider
{
    private readonly ITerminalCharacterProcessor _characterProcessor;
    private readonly ITerminalScrollHandler _scrollHandler;
    private readonly ITerminalWindowHandler _windowHandler;
    private readonly ITerminalResponseHandler _responseHandler;
}

// Character processing (≤400 lines)
public class TerminalCharacterProcessor : ITerminalCharacterProcessor
{
    // Character writing and wide character handling
    // Tab stops and character positioning
}

// Scroll handling (≤300 lines)
public class TerminalScrollHandler : ITerminalScrollHandler
{
    // Viewport scrolling and screen scrolling
    // Scroll region management
}

// Window handling (≤300 lines)
public class TerminalWindowHandler : ITerminalWindowHandler
{
    // Window manipulation operations
    // Title and icon management
    // Clipboard integration
}

// Response handling (≤200 lines)
public class TerminalResponseHandler : ITerminalResponseHandler
{
    // Device response generation
    // Event emission and notifications
}
```

### Phase 3: Parser and Manager Decomposition

**SgrParser Decomposition (879 lines):**
```csharp
// Core SGR parser (≤200 lines)
public class SgrParser : ISgrParser
{
    private readonly ISgrSequenceProcessor _sequenceProcessor;
    private readonly ISgrAttributeProcessor _attributeProcessor;
}

// Sequence processing (≤300 lines)
public class SgrSequenceProcessor : ISgrSequenceProcessor
{
    // SGR sequence parsing and validation
}

// Attribute processing (≤400 lines)
public class SgrAttributeProcessor : ISgrAttributeProcessor
{
    // Color processing and attribute application
}
```

**ProcessManager Decomposition (947 lines):**
```csharp
// Core process manager (≤200 lines)
public class ProcessManager : IProcessManager
{
    private readonly IConPtyManager _conPtyManager;
    private readonly IProcessLifecycleManager _lifecycleManager;
}

// ConPTY management (≤400 lines)
public class ConPtyManager : IConPtyManager
{
    // ConPTY creation, resizing, and cleanup
}

// Process lifecycle (≤350 lines)
public class ProcessLifecycleManager : IProcessLifecycleManager
{
    // Process startup, monitoring, and termination
}
```

## Data Models

### Interface Definitions

```csharp
// Terminal Controller Interfaces
public interface ITerminalController : IDisposable
{
    void Render();
    void Update(float deltaTime);
    bool ShouldCaptureInput();
    void ForceFocus();
    (int width, int height) GetTerminalDimensions();
}

public interface ITerminalLayoutManager
{
    void RenderLayout();
    float2 CalculateContentArea();
    void UpdateLayoutConstraints(LayoutConstants constants);
}

public interface ITerminalFontManager
{
    void LoadFonts();
    ImFontPtr SelectFont(SgrAttributes attributes);
    void CalculateCharacterMetrics();
    CharacterMetrics GetCharacterMetrics();
}

public interface ITerminalInputHandler
{
    void ProcessInput();
    void ManageInputCapture();
    bool ShouldCaptureInput();
}

public interface ITerminalResizeHandler
{
    void HandleWindowResize();
    (int cols, int rows)? CalculateTerminalDimensions(float2 availableSize);
    void ApplyTerminalDimensions(int cols, int rows);
}

public interface ITerminalRenderingEngine
{
    void RenderTerminalContent();
    void RenderFocusIndicators();
    void ApplyTheme(TerminalTheme theme);
}

// Terminal Emulator Interfaces
public interface ITerminalCharacterProcessor
{
    void WriteCharacterAtCursor(char character);
    void HandleTab();
    void HandleBackspace();
    void HandleLineFeed();
    void HandleCarriageReturn();
}

public interface ITerminalScrollHandler
{
    void ScrollViewportUp(int lines);
    void ScrollViewportDown(int lines);
    void ScrollScreenUp(int lines);
    void ScrollScreenDown(int lines);
    void SetScrollRegion(int? top, int? bottom);
}

public interface ITerminalWindowHandler
{
    void HandleWindowManipulation(int operation, int[] parameters);
    void SetWindowTitle(string title);
    void SetIconName(string iconName);
    string GetWindowTitle();
    string GetIconName();
}

public interface ITerminalResponseHandler
{
    void EmitResponse(string responseText);
    void HandleBell();
    (byte Red, byte Green, byte Blue) GetCurrentForegroundColor();
    (byte Red, byte Green, byte Blue) GetCurrentBackgroundColor();
}
```

### Configuration Models

```csharp
// Decomposed configuration classes
public class LayoutConfiguration
{
    public float MenuBarHeight { get; set; }
    public float TabAreaHeight { get; set; }
    public float SettingsAreaHeight { get; set; }
}

public class FontConfiguration
{
    public string RegularFontName { get; set; }
    public string BoldFontName { get; set; }
    public string ItalicFontName { get; set; }
    public string BoldItalicFontName { get; set; }
    public float FontSize { get; set; }
}

public class InputConfiguration
{
    public bool CaptureGameInput { get; set; }
    public TimeSpan FocusDebounceTime { get; set; }
}
```

## Correctness Properties

Let me analyze the acceptance criteria for testability:

<function_calls>
<invoke name="prework">
<parameter name="featureName">code-size-refactor

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

After analyzing the acceptance criteria, I've identified several redundant properties that can be consolidated:

**Property Reflection:**
- Properties 1.2 and 1.3 both test file size constraints and can be combined into a comprehensive file size property
- Properties 3.1, 3.3, and 3.4 all test API/signature preservation and can be combined
- Properties 4.4 and 4.5 both test minimal test changes and can be combined
- Properties 7.1, 7.2, 7.3, and 7.5 all test namespace organization and can be combined
- Properties 8.1, 8.2, and 8.3 all test documentation preservation and can be combined

### Property 1: File Size Compliance
*For any* refactored codebase, all output files should meet size constraints: ≤200 lines (ideal), ≤500 lines (acceptable), or ≤1000 lines (maximum), and files exceeding 500 lines should be identified as refactoring candidates
**Validates: Requirements 1.1, 1.2, 1.3**

### Property 2: Naming Convention Consistency  
*For any* newly created file during refactoring, the file name and namespace should follow established conventions (caTTY.Core.*, caTTY.Display.*) and logical organization patterns
**Validates: Requirements 1.4**

### Property 3: Interface Definition Completeness
*For any* refactored component, clear public interfaces should be defined and accessible for navigation and dependency analysis
**Validates: Requirements 2.1**

### Property 4: Library Separation Integrity
*For any* refactored codebase, the Core library should maintain no Display dependencies and preserve headless design patterns
**Validates: Requirements 2.3, 2.5**

### Property 5: API Preservation Completeness
*For any* refactored code, all public APIs, method signatures, and dependency relationships should remain identical to the original implementation
**Validates: Requirements 3.1, 3.3, 3.4**

### Property 6: Behavioral Equivalence
*For any* refactored component, executing identical operations should produce identical outcomes and state changes as the original implementation
**Validates: Requirements 3.2, 3.5**

### Property 7: Test Compatibility Preservation
*For any* refactored codebase, all existing unit, property, and integration tests should pass with only import/using statement modifications
**Validates: Requirements 4.4, 4.5**

### Property 8: Architectural Pattern Preservation
*For any* refactored component, existing architectural patterns (manager pattern, parser state machines, immutable data patterns, Result<T> usage) should remain intact
**Validates: Requirements 6.1, 6.2, 6.3, 6.4, 6.5**

### Property 9: Namespace Organization Consistency
*For any* refactored codebase, all namespace references should be consistent, properly organized in logical hierarchies, and free of orphaned references
**Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5**

### Property 10: Documentation Preservation Completeness
*For any* refactored code, all existing XML documentation should be preserved, new classes should have appropriate documentation, and documentation links should remain valid
**Validates: Requirements 8.1, 8.2, 8.3, 8.5**

## Error Handling

### Refactoring Failure Recovery
- **Compilation Errors**: If refactoring introduces compilation errors, automatically revert changes and log specific error details
- **Test Failures**: If existing tests fail after refactoring, identify the specific test failures and provide detailed analysis
- **Dependency Breaks**: If refactoring breaks dependencies, automatically restore original dependency structure

### Validation Checkpoints
- **Pre-Refactoring Validation**: Verify all tests pass and code compiles before starting refactoring
- **Incremental Validation**: After each file refactoring, run affected tests to ensure no regressions
- **Post-Refactoring Validation**: Run full test suite and compilation check after completing refactoring

### Rollback Mechanisms
- **File-Level Rollback**: Ability to rollback individual file refactoring if issues are detected
- **Component-Level Rollback**: Ability to rollback entire component refactoring (e.g., TerminalController decomposition)
- **Full Rollback**: Ability to restore entire codebase to pre-refactoring state

## Testing Strategy

### Dual Testing Approach
The refactoring process requires both **unit tests** and **property-based tests** to ensure comprehensive validation:

**Unit Tests:**
- Verify specific refactoring scenarios and edge cases
- Test individual component extraction and interface creation
- Validate specific file size targets and naming conventions
- Test compilation and basic functionality after refactoring

**Property-Based Tests:**
- Verify universal properties across all refactored components (minimum 100 iterations)
- Test API preservation across randomly generated code structures
- Validate behavioral equivalence with randomized input scenarios
- Test namespace consistency across various refactoring patterns

**Property Test Configuration:**
- Each property test must run minimum 100 iterations due to randomization
- Tests should be tagged with: **Feature: code-size-refactor, Property {number}: {property_text}**
- Property tests focus on universal correctness across all inputs
- Unit tests focus on specific examples, integration points, and edge cases

### Test Categories

**Refactoring Validation Tests:**
- File size compliance verification
- API signature comparison tests
- Behavioral equivalence tests
- Compilation and build verification

**Integration Tests:**
- Full test suite execution after refactoring
- Cross-component interaction validation
- End-to-end functionality verification
- Performance regression testing

**Property-Based Tests:**
- API preservation properties across random code structures
- Namespace consistency properties across various organizations
- Documentation preservation properties across different refactoring patterns
- Behavioral equivalence properties with randomized operations

### Testing Framework Requirements
- Use **NUnit 4.x** for unit and integration tests
- Use **FsCheck.NUnit** for property-based testing
- Maintain existing test organization in Unit/, Property/, Integration/ folders
- All tests must be headless (no ImGui dependencies in Core tests)
- Tests must achieve zero warnings/errors with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`