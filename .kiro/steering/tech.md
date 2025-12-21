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
├── caTTY.TestApp/        # Standalone console test app
└── caTTY.GameMod/        # Game mod DLL output target
```

## Game Integration Requirements

- **Game Install Path**: `C:\Program Files\Kitten Space Agency\`
- **Required DLLs**: GLFW, Vulkan, BRUTAL ImGui framework
- **Build Target**: Game mod DLL for production, console app for development
- **Reference Pattern**: Local file references to game DLLs in .csproj

## Code Style & Architecture

- **Immutability First**: Prefer `readonly` structs and immutable classes
- **Pure Functions**: Stateless methods wherever possible (following TypeScript design)
- **Null Safety**: Enable nullable reference types (`<Nullable>enable</Nullable>`)
- **Memory Efficiency**: Use `Span<T>` and `ReadOnlySpan<T>` for byte processing
- **Error Handling**: Use Result<T> pattern instead of exceptions for expected failures

## Testing Conventions

- **Unit Tests**: `Tests/` directory with `<ClassName>Tests.cs` naming
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

## Memory Management

- **Byte Handling**: Use `ReadOnlySpan<byte>` for terminal data processing
- **String Processing**: Minimize allocations with `Span<char>` operations  
- **Buffer Pooling**: Use `ArrayPool<T>` for temporary buffers
- **Disposal Pattern**: Implement `IDisposable` for resource cleanup

## Translation Strategy

- **1:1 Mapping**: Direct translation from TypeScript classes to C# classes as much as possible.  Switch to C# conventions where appropriate.
- **Type Adaptation**: `string` → `ReadOnlySpan<char>`, `Uint8Array` → `ReadOnlySpan<byte>`
- **State Management**: Maintain immutable state pattern from TypeScript version
- **API Consistency**: Keep method signatures as close as possible to TypeScript

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
