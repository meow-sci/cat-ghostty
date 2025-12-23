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
├── caTTY.TestApp/        # Standalone console test app
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

## Testing Conventions

- **Per-Project Tests**: `caTTY.Core.Tests/`, `caTTY.ImGui.Tests/` with `<ClassName>Tests.cs` naming
- **Property Tests**: Use FsCheck.NUnit for property-based testing
- **Test Categories**: `[Category("Unit")]`, `[Category("Property")]`, `[Category("Integration")]`
- **Headless Testing**: All core logic testable without ImGui dependencies

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
