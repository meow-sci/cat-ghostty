# Project Structure

## Root Layout

### TypeScript caTTY implementation

```
- catty-web: pnpm workspace
  - catty-web/app: astro app for the TerminalController (glue) and display
  - catty-web/node-pty: backend websocket server for pty connection to real shells
  - catty-web/packages/log: logging helper
  - catty-web/packages/tsconfig: shared TypeScript config
  - catty-web/packages/terminal-emulation: all headless terminal logic (parsers, stateful terminal)
```

#### Key Locations

```
- catty-web\packages\terminal-emulation\src\terminal\Parser.ts: entrypoint for terminal emulation parsing
- catty-web\packages\terminal-emulation\src\terminal\StatefulTerminal.ts: entrypoint for stateful terminal
- catty-web\node-pty\src\BackendServer.ts: the backend websocket server for pty shells
- catty-web\app\src\ts\terminal\TerminalController.ts: the web/DOM specific controller for glue code to headless terminal logic
```

### C# caTTY implementation

```
- catty-ksa/: .NET solution root
  - catty-ksa/caTTY.Core/: headless terminal logic (Class Library)
    - Terminal/: core terminal emulation (Parser, StatefulTerminal)
    - Input/: keyboard input encoding
    - Types/: shared data structures and enums
    - Utils/: utility functions and helpers
  - catty-ksa/caTTY.ImGui/: ImGui display controller (Class Library)
    - Controllers/: TerminalController for ImGui integration
    - Rendering/: ImGui-specific rendering logic
    - Input/: ImGui input event handling
  - catty-ksa/caTTY.TestApp/: standalone console application for development
  - catty-ksa/caTTY.GameMod/: game mod build target (references Core + ImGui)
  - catty-ksa/Tests/: unit and property-based tests
```

#### Key Locations

```
- catty-ksa/caTTY.Core/Terminal/Parser.cs: entrypoint for terminal emulation parsing
- catty-ksa/caTTY.Core/Terminal/StatefulTerminal.cs: entrypoint for stateful terminal
- catty-ksa/caTTY.ImGui/Controllers/TerminalController.cs: ImGui display controller
- catty-ksa/caTTY.TestApp/Program.cs: standalone test application entry point
```


## Code Organization Principles

### Headless Design

All business logic must be:
- 100% TypeScript / C#
- Headless parts (no DOM/browser APIs or ImGui code)
- Testable in isolation
- Framework-agnostic

### Display controller / glue

- For TypeScript, there is a TerminalController which is the web/DOM display specific bridge and glue code from the headless code
- For C#, there will be a TerminalController which is the ImGui display specific bridge and glue code from the headless Core library

### C# Project Architecture

#### Multi-Target Build Strategy

- **caTTY.Core**: Pure C# headless logic, no external dependencies
- **caTTY.ImGui**: ImGui integration layer, references game DLLs
- **Build Targets**:
  - Development: Console app (`caTTY.TestApp`) for standalone testing
  - Production: Game mod DLL (`caTTY.GameMod`) for KSA integration

#### Dependency Flow

```
caTTY.TestApp ──┐
                ├─→ caTTY.ImGui ──→ caTTY.Core
caTTY.GameMod ──┘
```

#### Game DLL Integration

```xml
<!-- In caTTY.ImGui.csproj -->
<ItemGroup>
  <Reference Include="KSA.ImGui">
    <HintPath>C:\Program Files\Kitten Space Agency\KSA.ImGui.dll</HintPath>
  </Reference>
  <Reference Include="KSA.Graphics">
    <HintPath>C:\Program Files\Kitten Space Agency\KSA.Graphics.dll</HintPath>
  </Reference>
</ItemGroup>
```

### Web/Astro Page Components

- Astro pages (`.astro`) are minimal entry points
- React components (`_ts/` directories) contain UI logic
- Prefix with `_` for non-routable directories
- CSS files prefixed with `_` for component-specific styles
