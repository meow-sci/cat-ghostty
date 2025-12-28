# Project Structure

## TypeScript Implementation
- `catty-web/`: pnpm workspace
  - `app/`: Astro app with TerminalController (DOM glue)
  - `node-pty/`: WebSocket server for PTY shells
  - `packages/terminal-emulation/`: headless terminal logic
- Key files: `Parser.ts`, `StatefulTerminal.ts`, `BackendServer.ts`, `TerminalController.ts`

### Key Paths
- `catty-web/packages/terminal-emulation/src/terminal/Parser.ts`
- `catty-web/packages/terminal-emulation/src/terminal/StatefulTerminal.ts`
- `catty-web/node-pty/src/BackendServer.ts`
- `catty-web/app/src/ts/terminal/TerminalController.ts`

## C# Implementation
- `catty-ksa/`: .NET solution
  - `caTTY.Core/`: headless terminal (Terminal/, Parsing/, Managers/, Types/)
  - `caTTY.Display/`: ImGui display controller
  - `caTTY.TestApp/`: standalone console app
  - `caTTY.GameMod/`: KSA game mod target
  - Tests: `caTTY.Core.Tests/`, `caTTY.Display.Tests/`

### Key Paths
- `catty-ksa/caTTY.Core/Terminal/TerminalEmulator.cs`
- `catty-ksa/caTTY.Core/Parsing/Parser.cs`
- `catty-ksa/caTTY.Display/Controllers/TerminalController.cs`

## Architecture Principles
- **Headless Design**: Business logic separate from UI (TypeScript/C#, testable, framework-agnostic)
- **Display Controllers**: TerminalController bridges headless logic to UI (DOM/ImGui)
- **Modular Components**: Classes <400 lines, methods <50 lines, focused responsibilities
- **Parser Decomposition**: CsiParser, SgrParser, OscParser, EscParser, DcsParser, Utf8Decoder
- **State Managers**: ScreenBufferManager, CursorManager, ScrollbackManager, AlternateScreenManager, ModeManager, AttributeManager

## Web/Astro Conventions
- Astro pages (`.astro`) are minimal entry points
- React/TS UI logic lives under `_ts/`-style directories
- Prefix `_` for non-routable directories and component-only assets

## C# Build Strategy
- **caTTY.Core**: Pure C# headless logic
- **caTTY.Display**: ImGui integration with KSA DLLs
- **Targets**: TestApp (dev), GameMod (prod)
- **TestApp Usage**: Run `dotnet run` from `caTTY.TestApp/` directory for Content/ path resolution
- **Dependencies**: TestApp/GameMod → Display → Core
- **Reference**: See `KsaExampleMod/` for KSA integration patterns
