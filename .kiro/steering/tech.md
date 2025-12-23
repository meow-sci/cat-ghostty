---
inclusion: always
---

# C# version Technology Stack

## Build System & Framework

- **.NET 10** - Latest LTS version with C# 13 language features
- **MSBuild** - Standard .NET build system with multi-target support
- **NUnit 4.x** - Testing framework with property-based testing via FsCheck.NUnit
- **Game Integration** - KSA game custom BRUTAL ImGui framework

## Project Structure Pattern

Multi-project solution using **Class Library + Console App** pattern:

```
catty-ksa.sln
├── caTTY.Core/           # Headless terminal logic (Class Library)
├── caTTY.ImGui/          # ImGui display controller (Class Library) 
├── caTTY.ImGui.Playground/ # ImGui rendering experiments (Console App)
├── caTTY.TestApp/        # Standalone BRUTAL ImGui test app (Console App with GLFW window)
└── caTTY.GameMod/        # Game mod DLL output target
```

## Game Integration Requirements

- **Game Install Path**: `C:\Program Files\Kitten Space Agency\`
- **Required DLLs**: GLFW, Vulkan, BRUTAL ImGui framework
- **Build Target**: Game mod DLL for production, console app for development
- **Reference Pattern**: Local file references to game DLLs in .csproj
- **Reference Project**: See `KsaExampleMod/` folder for complete working example of KSA game mod structure

## Code Style & Architecture

- **Immutability First**: Prefer `readonly` structs and immutable classes
- **Pure Functions**: Stateless methods wherever possible (following TypeScript design)
- **Null Safety**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
- **Memory Efficiency**: Use `Span<T>` and `ReadOnlySpan<T>` for byte processing
- **Error Handling**: Use Result<T> pattern instead of exceptions for expected failures

## Performance Optimization - Render Loop Allocation Minimization

**CRITICAL PERFORMANCE REQUIREMENT**: The C#/ImGui implementation MUST minimize allocations during the render loop and hot paths to ensure smooth game performance.

### Hot Path Optimization Rules

- **Render Loop**: Code that executes every frame (Update/Render methods) MUST avoid allocations
- **Input Processing**: Keyboard/mouse event handling MUST use pre-allocated buffers and object pooling
- **Screen Buffer Access**: Terminal screen queries MUST use span-based access patterns
- **String Operations**: Text rendering MUST avoid string concatenation and temporary string creation
- **Collection Operations**: Avoid LINQ, temporary collections, and boxing in hot paths

### Memory Architecture Patterns

- **Pre-allocation Strategy**: Allocate long-lived objects during initialization, reuse during runtime
- **Object Pooling**: Use `ArrayPool<T>` and custom object pools for frequently used temporary objects
- **Span-Based APIs**: Use `ReadOnlySpan<char>` and `Span<T>` for all hot path data access
- **Struct Optimization**: Use `readonly struct` for value types passed frequently
- **Buffer Reuse**: Maintain reusable buffers for string building, character conversion, and data processing

### Acceptable Allocation Areas

- **Initialization**: Object creation during terminal setup and configuration is acceptable
- **Long-Lived Objects**: Class instances, buffers, and caches that persist for the application lifetime
- **Infrequent Operations**: Resize operations, configuration changes, and error handling
- **Background Processing**: Non-render-loop operations like process I/O and logging

### Implementation Guidelines

```csharp
// GOOD: Pre-allocated, reusable buffer
private readonly StringBuilder _stringBuilder = new(1024);
private readonly char[] _renderBuffer = new char[4096];

public void RenderHotPath()
{
    _stringBuilder.Clear(); // Reuse existing capacity
    // Use spans for data access
    ReadOnlySpan<Cell> row = screenBuffer.GetRow(rowIndex);
    // Process without allocations
}

// BAD: Allocates every frame
public void RenderBadExample()
{
    var text = string.Join("", cells.Select(c => c.Character)); // LINQ + string allocation
    var colors = new List<Color>(); // New collection every frame
}
```

### Memory vs Performance Trade-offs

- **Acceptable RAM Usage**: Sacrifice memory space to reduce allocations (larger pre-allocated buffers)
- **Cache-Friendly Design**: Structure data for sequential access patterns
- **Buffer Sizing**: Over-allocate buffers to avoid resize operations during runtime
- **Object Lifetime**: Prefer longer object lifetimes over frequent create/dispose cycles

## Testing Conventions

- **Per-Project Tests**: `caTTY.Core.Tests/`, `caTTY.ImGui.Tests/` with `<ClassName>Tests.cs` naming
- **Property Tests**: Use FsCheck.NUnit for property-based testing
- **Test Categories**: `[Category("Unit")]`, `[Category("Property")]`, `[Category("Integration")]`
- **Headless Testing**: All core logic testable without ImGui dependencies

### Comprehensive Test Coverage Requirements

**CRITICAL**: The C# implementation MUST match or exceed the TypeScript test coverage. The TypeScript version has 42 test files covering:

#### Core Parser Testing (Match TypeScript Coverage)
- **Parser State Integrity**: Property-based tests for parser state consistency during mixed sequence operations
- **CSI Sequence Parsing**: Complete coverage of cursor movement, screen clearing, scrolling, and mode sequences
- **SGR Parsing**: Comprehensive color parsing (8-bit, 24-bit RGB), text styling, underline variants
- **OSC Sequence Parsing**: Window title, clipboard, hyperlinks, and unknown sequence handling
- **DCS Handling**: Device Control String parsing and response generation
- **ESC Sequences**: Save/restore cursor, character set designation, and other non-CSI sequences

#### Terminal Behavior Testing (Match TypeScript Coverage)
- **Cursor Operations**: Position tracking, wrapping, visibility, application mode, save/restore
- **Screen Buffer Operations**: Initialization, resizing, cell integrity, bounds checking
- **Scrollback Management**: Buffer operations, viewport tracking, auto-scroll behavior
- **Alternate Screen**: Buffer switching, state isolation, scrollback isolation
- **Character Processing**: UTF-8 handling, wide characters, control characters
- **Line Operations**: Insertion, deletion, character operations, content preservation

#### Advanced Feature Testing (Match TypeScript Coverage)
- **Tab Stop Controls**: Default stops, custom stops, clearing operations
- **Window Manipulation**: Title changes, resize operations, query responses
- **Device Queries**: Status reports, capability queries, response generation
- **Color Systems**: Theme resolution, color consistency, enhanced SGR modes
- **Selection Operations**: Text extraction, line handling, wide character support
- **Hyperlink Support**: URL association, range tracking, state management

#### Property-Based Testing Requirements
- **Minimum 100 iterations** per property test (due to randomization)
- **State Integrity Properties**: Parser state, terminal state, buffer consistency
- **Round-Trip Properties**: Save/restore operations, serialization/parsing
- **Compatibility Properties**: Behavior matching with TypeScript implementation
- **Error Handling Properties**: Graceful handling of malformed input
- **Performance Properties**: Memory allocation patterns, buffer efficiency

#### Integration Testing Requirements
- **End-to-End Workflows**: Complete terminal sessions with real shell processes
- **ImGui Integration**: Rendering accuracy, input handling, focus management
- **Game Engine Integration**: Resource management, lifecycle handling, error recovery
- **Process Management**: Shell spawning, data flow, cleanup operations

#### Test Organization Structure
```
caTTY.Core.Tests/
├── Unit/
│   ├── Parser/
│   │   ├── CsiParserTests.cs
│   │   ├── SgrParserTests.cs
│   │   ├── OscParserTests.cs
│   │   └── EscParserTests.cs
│   ├── Terminal/
│   │   ├── TerminalEmulatorTests.cs
│   │   ├── ScreenBufferTests.cs
│   │   ├── CursorTests.cs
│   │   └── ScrollbackTests.cs
│   └── Features/
│       ├── AlternateScreenTests.cs
│       ├── TabStopTests.cs
│       └── CharacterSetTests.cs
├── Property/
│   ├── ParserStateProperties.cs
│   ├── TerminalBehaviorProperties.cs
│   ├── CompatibilityProperties.cs
│   └── PerformanceProperties.cs
└── Integration/
    ├── EndToEndTests.cs
    └── ProcessIntegrationTests.cs

caTTY.ImGui.Tests/
├── Unit/
│   ├── TerminalControllerTests.cs
│   ├── ImGuiRendererTests.cs
│   └── InputHandlerTests.cs
└── Integration/
    ├── GameIntegrationTests.cs
    └── ResourceManagementTests.cs
```

#### Test Coverage Metrics
- **Parser Coverage**: All escape sequence types from TypeScript implementation
- **Feature Coverage**: All terminal features with property-based validation
- **Edge Case Coverage**: Malformed input, boundary conditions, error states
- **Compatibility Coverage**: Behavior verification against TypeScript reference
- **Performance Coverage**: Memory allocation, GC pressure, rendering performance

## Build Configuration

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>13.0</LangVersion>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
</PropertyGroup>
```

## Build Quality Requirements

**CRITICAL BUILD STANDARDS**: The entire solution MUST maintain zero warnings and zero errors at all times.

### Compilation Requirements
- **Zero Warnings**: The entire solution MUST compile with no warnings
- **Zero Errors**: The entire solution MUST compile with no errors
- **Treat Warnings as Errors**: All projects MUST have `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- **Nullable Reference Types**: All projects MUST have `<Nullable>enable</Nullable>`
- **Documentation**: All public APIs MUST have XML documentation to avoid documentation warnings

### Test Suite Requirements
- **Zero Test Failures**: The entire test suite MUST pass with no errors
- **All Tests Must Run**: No tests should be skipped due to compilation issues
- **Property Test Stability**: All property-based tests MUST pass consistently across multiple runs
- **Integration Test Reliability**: All integration tests MUST pass reliably in CI/CD environments

### Code Quality Standards
- **No Compiler Warnings**: Address all CS warnings immediately
- **No Nullable Warnings**: Properly handle all nullable reference type scenarios
- **No Obsolete API Usage**: Avoid deprecated APIs and methods
- **No Unreachable Code**: Remove or fix any unreachable code warnings
- **No Unused Variables**: Clean up all unused variable warnings

### Continuous Integration Requirements
- **Build Verification**: Every commit MUST compile successfully
- **Test Verification**: Every commit MUST pass all tests
- **Warning Monitoring**: CI MUST fail on any new warnings
- **Coverage Verification**: Test coverage MUST meet minimum thresholds

## KSA Game Mod Reference Structure

The `KsaExampleMod/` folder provides a complete working reference for KSA game mod projects:

### Key Reference Files
- **`KsaExampleMod/modone.csproj`**: Complete .csproj with KSA DLL references, build targets, and asset copying
- **`KsaExampleMod/mod.toml`**: Mod metadata file required by KSA mod system
- **`KsaExampleMod/Class1.cs`**: Example mod implementation with StarMap attributes
- **`KsaExampleMod/Patcher.cs`**: Harmony patching example for game integration

### Project Structure Pattern
```
KsaExampleMod/
├── modone.csproj          # Project file with KSA references
├── modone.sln             # Solution file
├── mod.toml               # Mod metadata (required by KSA)
├── Class1.cs              # Main mod class with StarMap attributes
├── Patcher.cs             # Harmony patching utilities
├── Fonts/                 # Asset folder (copied to output)
│   └── JetBrainsMono-Regular.ttf
├── bin/                   # Build output
└── obj/                   # Build intermediates
```

### Critical KSA Integration Elements
- **KSA DLL References**: Uses `$(KSAFolder)` property for DLL paths
- **StarMap.API**: Required NuGet package for mod attributes
- **Lib.Harmony**: Required for runtime patching
- **Asset Copying**: Custom MSBuild targets for mod.toml and assets
- **StarMap Attributes**: Required attributes for KSA mod system entry points

## BRUTAL ImGui Font Setup

- **Font Loading**: TTF files in `Content/` folder are automatically loaded by BRUTAL ImGui
- **Font Naming**: Font name matches filename without `.ttf` extension (e.g., `Content/Hack.ttf` → `"Hack"`)
- **Font Usage**: Use Push/Pop pattern with `FontManager.Fonts.TryGetValue()` and `ImGui.PushFont()`
- **Reference Documentation**: See `BRUTAL_IMGUI_NOTES.md` for complete font setup examples and code patterns
- **Preferred Font**: HackNerdFontMono-Regular

## BRUTAL ImGui Console Application Requirements

**CRITICAL**: BRUTAL ImGui console applications must be run from their project directory as the working directory.

- **Working Directory**: Must be the `.csproj` folder (e.g., `catty-ksa/caTTY.ImGui.Playground/`)
- **Content Path**: The `Content/` folder must be relative to the current working directory
- **Execution**: Run `dotnet run` from inside the project folder, not from solution root
- **Example**: 
  ```bash
  cd catty-ksa/caTTY.ImGui.Playground/
  dotnet run
  ```
- **Reason**: BRUTAL ImGui loads fonts and assets from `./Content/` relative to `Environment.CurrentDirectory`

## Windows ConPTY (Pseudoconsole) Requirements

**CRITICAL**: The C# version uses Windows ConPTY exclusively for PTY functionality. No fallback to basic process redirection.

- **Platform Support**: **Windows 10 version 1809+ only** - ConPTY is required, no cross-platform support
- **PTY Implementation**: Uses Microsoft's official ConPTY APIs following their documentation exactly
- **Core APIs**: `CreatePseudoConsole`, `ResizePseudoConsole`, `ClosePseudoConsole`
- **Communication**: Pipe-based I/O using `CreatePipe`, `ReadFile`, `WriteFile` (not stream redirection)
- **Process Creation**: Uses `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE` for proper PTY attachment
- **Error Handling**: `ConPtyException` with Win32 error codes for ConPTY-specific failures
- **Resource Management**: Proper cleanup of ConPTY handles and pipes following Microsoft's lifecycle guidelines
- **Testing**: Platform-aware tests that detect ConPTY availability and skip appropriately on unsupported systems

### ConPTY Benefits Over Process Redirection
- **True terminal emulation**: Child processes see a real terminal environment
- **Proper terminal size reporting**: Applications can query dimensions correctly via console APIs
- **Better TUI compatibility**: Works with complex terminal applications that require PTY features
- **Signal handling**: Ctrl+C and other terminal signals work properly
- **Terminal control sequences**: Full support for escape sequences and terminal modes
- **Resizing support**: Dynamic terminal resizing via `ResizePseudoConsole`

- **Byte Handling**: Use `ReadOnlySpan<byte>` for terminal data processing
- **String Processing**: Minimize allocations with `Span<char>` operations  
- **Buffer Pooling**: Use `ArrayPool<T>` for temporary buffers
- **Disposal Pattern**: Implement `IDisposable` for resource cleanup

## Translation Strategy

- **1:1 Mapping**: Direct translation from TypeScript classes to C# classes as much as possible.  Switch to C# conventions where appropriate.
- **Type Adaptation**: `string` → `ReadOnlySpan<char>`, `Uint8Array` → `ReadOnlySpan<byte>`
- **State Management**: Maintain immutable state pattern from TypeScript version
- **API Consistency**: Keep method signatures as close as possible to TypeScript

## Git Commit Requirements

**CRITICAL WORKFLOW REQUIREMENT**: After completing each subtask, you MUST provide a properly formatted git commit message in your response as a summary.

### Git Commit Message Format

```
[task-id] type: description (80 characters or less)

## Changes Made

- Bullet point list of specific changes
- Each change should be concrete and actionable
- Use past tense (e.g., "Added", "Updated", "Fixed")
- Include file names and key modifications
```

### Example Git Commit Message

```
[1.1] feat: set up solution structure with per-project tests

## Changes Made

- Created caTTY-cs.sln with 6 projects and solution folders
- Added Directory.Build.props with .NET 10 and C# 13 configuration
- Created .editorconfig with comprehensive C# formatting rules
- Set up caTTY.Core class library with Terminal/, Input/, Parsing/, Types/, Utils/ folders
- Set up caTTY.ImGui class library with KSA DLL references
- Created caTTY.TestApp console application with placeholder Program.cs
- Set up caTTY.GameMod with StarMap.API and mod.toml configuration
- Added caTTY.Core.Tests and caTTY.ImGui.Tests with NUnit + FsCheck.NUnit
- Configured proper project dependencies and build verification
- Updated README.md with project structure and development workflow
```

### Commit Message Rules

- **Subject Line**: 80 characters or less, use format: `[task-id] type: description`
  - Task ID: Use the exact task number (e.g., "1.1", "2.3", "4.11")
  - Type: Use conventional commit format (feat, fix, refactor, docs, test, etc.)
  - Example: `[1.1] feat: set up solution structure with per-project tests`
- **Blank Line**: Always include a blank line after the subject
- **Body**: Use markdown formatting with "## Changes Made" header
- **Changes List**: Bullet points with specific, concrete changes
- **File References**: Include relevant file names and paths
- **Past Tense**: Use past tense verbs (Added, Updated, Fixed, Created, etc.)

This requirement ensures proper documentation of progress and provides clear commit messages for version control.

# TypeScript version Technology Stack

## Build System & Framework

- **Astro 5.x** - Static site generator with React integration
- **pnpm** - Package manager (always use pnpm, never npm or yarn)
- **TypeScript 5.x** - Strict mode enabled via `astro/tsconfigs/strict`
- **Vitest** - Testing framework with property-based testing support

## Frontend Stack

- **React 19.x** with TypeScript for UI components
- **Nanostores** for state management:
  - Use `atom()` for simple state, `map()` for objects
  - Import `useStore()` from `@nanostores/react` for React integration
  - Use `@nanostores/persistent` for localStorage persistence
  - Enable `@nanostores/logger` in development for debugging

## WebAssembly Integration Rules

- **WASM Library**: `ghostty-vt.wasm` (libghostty-vt)
- **Type Definitions**: `src/ts/ghostty-vt.d.ts`
- **Dependency Injection**: Always pass WASM instance to class constructors that need it
- **Testing**: Load WASM bundle in test setup and provide to instances
- **Memory Management**: Wrappers must handle allocate → use → free pattern
- **API Design**: Hide C API complexity, expose idiomatic TypeScript interfaces

## Working Directory for caTTY-ts

All commands run from `catty-web/` directory:

```bash
pnpm install          # Install dependencies
pnpm tsc              # compile all projects
pnpm test             # test all projects
pnpm run-pty          # run the pty websocket server
pnpm run-web          # run the astro web app
```

## Code Style Requirements

- Use TypeScript strict mode (no `any` types without justification)
- Prefer `const` over `let`, avoid `var`
- Use functional programming patterns where appropriate
- Write pure functions in `src/ts/` (no side effects, no DOM access)
- Use descriptive variable names (avoid single-letter except in loops/lambdas)
- Add JSDoc comments for public APIs and complex logic
- Always use exact package versions in package.json for npm packages

## Testing Conventions

- Place tests in `__tests__/` directories adjacent to source files
- Name test files: `<FileName>.test.ts` or `<FileName>.property.test.ts`
- Use Vitest's `describe`, `it`, `expect` API
- Property-based tests for stateful components (use `fast-check` if needed)
- Always test WASM integration with actual WASM instance

## Deployment Configuration

- **Site URL**: `https://meow.science.fail`
- **Base Path**: `/caTTY/`
- **Trailing Slashes**: Always enforced (configured in `astro.config.ts`)

## Backend

- @lydell/node-pty npm package to manage pty shells.
