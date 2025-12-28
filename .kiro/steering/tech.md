---
inclusion: always
---

# Technology Stack

## C# Stack
- **.NET 10** + C# 13, **MSBuild**, **NUnit 4.x** + FsCheck.NUnit, **BRUTAL ImGui**
- **Projects**: Core (headless), Display (ImGui), TestApp (dev), GameMod (prod), Tests
- **KSA Integration**: `C:\Program Files\Kitten Space Agency\`, see `KsaExampleMod/` reference
- **Code Style**: Immutable, pure functions, nullable types, `Span<T>`, Result<T> pattern

## Code Organization (CRITICAL)
**Size Limits**: Classes <400 lines, methods <50 lines, <10 public methods, <5 classes/file
**Parser Decomposition**: Main Parser (300), CsiParser (200), SgrParser (300), OscParser (250), EscParser (200), DcsParser (150), Utf8Decoder (150)
**State Managers**: ScreenBufferManager (300), CursorManager (200), ScrollbackManager (250), AlternateScreenManager (200), ModeManager (250), AttributeManager (200)
**Refactoring**: MUST refactor before adding features when limits exceeded

## Performance (CRITICAL)
**Hot Path Rules**: Render loop/input MUST avoid allocations, use pre-allocated buffers, `ArrayPool<T>`, `ReadOnlySpan<T>`
**Memory Strategy**: Pre-allocate at init, object pooling, span-based APIs, struct optimization, buffer reuse
**Acceptable Allocations**: Initialization, long-lived objects, infrequent operations, background processing

## Testing (CRITICAL)
**Coverage**: MUST match TypeScript's 42 test files - parser state, CSI/SGR/OSC/DCS/ESC sequences, terminal behavior, cursor ops, screen buffer, scrollback, alternate screen, UTF-8, line ops, tab stops, window manipulation, device queries, colors, selection, hyperlinks
**Property Tests**: Min 100 iterations, state integrity, round-trip, compatibility, error handling, performance
**Organization**: Unit/, Property/, Integration/ folders with focused test classes
**Categories**: `[Category("Unit/Property/Integration")]`, headless testing without ImGui deps

## Build Requirements (CRITICAL)
**Zero Tolerance**: MUST compile with zero warnings/errors, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<Nullable>enable</Nullable>`
**Config**: .NET 10, C# 13, XML docs for public APIs, all tests pass consistently
**CI**: Every commit MUST build and pass all tests

## KSA Integration
**Reference**: `KsaExampleMod/` - modone.csproj (KSA DLL refs), mod.toml (metadata), StarMap attributes, Harmony patching
**DLL Path**: `$(KSAFolder)` property, StarMap.API + Lib.Harmony NuGet packages

## BRUTAL ImGui
**Fonts**: TTF in `Content/` folder, name = filename without .ttf, Push/Pop pattern with FontManager
**Working Dir**: MUST run `dotnet run` from project folder (not solution root) for `./Content/` access
**TestApp**: MUST run `dotnet run` from `caTTY.TestApp/` directory for correct Content/ path resolution
**Preferred**: HackNerdFontMono-Regular, configurable sizing 8-72f, see `BRUTAL_IMGUI_NOTES.md`

## Windows ConPTY (CRITICAL)
**Exclusive**: Windows 10 1809+ only, no fallback to process redirection
**APIs**: `CreatePseudoConsole`, `ResizePseudoConsole`, `ClosePseudoConsole`, pipe-based I/O
**Benefits**: True terminal emulation, proper size reporting, TUI compatibility, signal handling
**Error Handling**: `ConPtyException` with Win32 codes, proper resource cleanup

## Translation Strategy
**1:1 Mapping**: Direct TypeScript → C# translation, adapt types (`string` → `ReadOnlySpan<char>`, `Uint8Array` → `ReadOnlySpan<byte>`)
**Consistency**: Maintain immutable state pattern, keep method signatures similar

## Git Commits (CRITICAL)
**Format**: `[task-id] type: description` (≤80 chars) + `## Changes Made` + bullet points with file names
**Example**: `[1.1] feat: set up solution structure with per-project tests`
**Workflow**: After each task/subtask, include the commit message in the response.

## TypeScript Stack
- **Build**: Astro 5.x, pnpm, TypeScript 5.x strict, Vitest
- **Frontend**: React 19.x, Nanostores (atom/map, useStore, persistent, logger)
- **WASM**: ghostty-vt.wasm, dependency injection, allocate→use→free pattern
- **Commands**: From `catty-web/`: `pnpm install/tsc/test/run-pty/run-web`
- **Testing**: `__tests__/` dirs, `<FileName>.test.ts`, Vitest API, fast-check for properties
- **Backend**: @lydell/node-pty for PTY shells
