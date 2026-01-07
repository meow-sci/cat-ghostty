# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

caTTY-cs is a C# terminal emulator for the Kitten Space Agency (KSA) game engine, providing full VT100/xterm-compatible terminal emulation. The project is structured as a .NET 10 solution with a headless core library and game-specific display layer.

## Build and Test Commands

### Building
```bash
dotnet build                    # Build entire solution
dotnet build caTTY.Core         # Build core library only
dotnet build caTTY.GameMod      # Build game mod (outputs to caTTY.GameMod/dist/)
```

### Testing

**CRITICAL: Always use the PowerShell test script, never `dotnet test` directly.**

The test suite contains ~1500 tests that produce massive stdout output from subshells that cannot be suppressed. Direct use of `dotnet test` will bloat context with thousands of lines of output.

```bash
.\scripts\dotnet-test.ps1                            # Run all tests (REQUIRED METHOD)
.\scripts\dotnet-test.ps1 -Filter "Category=Unit"    # Run filtered tests
```

The script runs dotnet test, saves results to `.testresults/results.trx`, then automatically parses failures with `test-errors.ts`. It outputs YAML-formatted results showing test counts on success or detailed failure information on error. Exit code 0 = all tests passed, 1 = failures detected.

### Running
```bash
dotnet run --project caTTY.TestApp                   # Run standalone console app
```

## Architecture

### Project Dependencies
```
caTTY.Core (headless, no dependencies)
    ↑
    ├── caTTY.TestApp (console app)
    ├── caTTY.Core.Tests (unit & property tests)
    └── caTTY.Display (ImGui integration, depends on KSA DLLs)
            ↑
            ├── caTTY.Display.Playground
            ├── caTTY.Display.Tests
            └── caTTY.GameMod (final game mod DLL, includes mod.toml)
```

### Core Architecture (caTTY.Core)

The terminal emulator follows a multi-stage parsing and execution pipeline:

**Parsing Pipeline:**
- Input bytes → `ParserEngine` → escape sequence parsers (`CsiParser`, `EscParser`, `OscParser`, `DcsParser`)
- Parsers emit typed messages: `CsiMessage`, `EscMessage`, `OscMessage`, `DcsMessage`
- Messages routed to execution handlers in `TerminalEmulator`

**Key Components:**
- `TerminalEmulator` (2500 LOC): Main orchestrator, handles all escape sequences and terminal state
  - Currently a large monolith, planned refactor to extract operation classes (see REAFCATOR_PLAN_WITHOUT_PARTIALS_SMALLER_FILES.md)
- `ProcessManager`: Manages Windows ConPTY shell processes (PowerShell, cmd)
- `CustomShellPtyBridge`: Adapter for custom shell implementations via `ICustomShell` interface
- `DualScreenBuffer`: Manages primary and alternate screen buffers
- `ScreenBuffer`: Grid of cells with cursor, scrollback, and scroll regions

**Important Subsystems:**
- `Input/`: Keyboard encoding, bracketed paste mode
- `Parsing/`: Escape sequence parsers (CSI, OSC, ESC, DCS)
- `Terminal/`: Core terminal logic, process management, custom shell support
- `Rpc/`: RPC mechanisms for game integration (both CSI and OSC-based)
- `Tracing/`: SQLite-based logging for debugging (disabled by default, see caTTY.Core/Tracing/README.md)
- `Types/`: Data structures (Cell, Cursor, ScreenBuffer, messages)
- `Utils/`: Character classification, UTF-8 handling

**Custom RPC Mechanisms:**
- **CSI RPC**: Command sequences via CSI private-use functions (commands 1000+)
  - Entry points: `Rpc/RpcHandler.cs`, `Rpc/RpcCommandRouter.cs`, `Rpc/GameActionRegistry.cs`
  - Format: CSI-based with fire-and-forget or query patterns
- **OSC RPC**: Uses OSC sequences in private-use range (1000+) for KSA game integration
  - OSC 1010: JSON action commands (e.g., `ESC ] 1010 ; {"action":"engine_ignite"} BEL`)
  - Entry points:
    - `Parsing/OscParser.cs`: Marks OSC ≥1000 as `osc.private` type
    - `Terminal/ParserHandlers/OscHandler.cs`: Standard OSC handling, delegates private commands to RPC layer
    - `Rpc/IOscRpcHandler.cs`: RPC interface
    - `Rpc/OscRpcHandler.cs`: JSON parsing and KSA action dispatch
    - `Terminal/TerminalParserHandlers.cs`: Wires `OscRpcHandler` into OSC pipeline
  - Why OSC instead of DCS: Windows ConPTY filters DCS sequences but passes OSC through

### Display Layer (caTTY.Display)

**Controllers:**
- `TerminalController`: Bridges `TerminalEmulator` with ImGui rendering
- Handles input → terminal routing and terminal output → ImGui display

**Rendering:**
- Uses KSA's Brutal.ImGui bindings
- Manages font rendering, text layout, and ImGui window state

**Dependencies:**
- KSA game DLLs from `C:\Program Files\Kitten Space Agency\` (configurable via `KSAFolder` in Directory.Build.props)
- `Brutal.Core.Common.dll`, `Brutal.ImGui.dll`, `KSA.dll`, etc.

### Game Mod (caTTY.GameMod)

- StarMap.API-based mod with `[StarMapMod]` attribute
- Lifecycle hooks: `[StarMapAllModsLoaded]`, `[StarMapAfterGui]`, `[StarMapUnload]`
- F12 keybind toggles terminal window
- Outputs to `dist/` with mod.toml

## Development Patterns

### Testing Strategy
- **Unit tests**: Per-component tests in matching test projects
- **Property tests**: FsCheck-based in `Property/` folders
- **Test organization**: Mirrors source structure (e.g., `caTTY.Core.Tests/Unit/ProcessManagerTests.cs` → `caTTY.Core/Terminal/ProcessManager.cs`)
- **Test execution**: MUST use `.\scripts\dotnet-test.ps1` to avoid massive stdout bloat

### Refactoring Goals
The codebase is undergoing refactoring to break `TerminalEmulator.cs` (2500 LOC) into smaller operation classes (<500 LOC each). See REAFCATOR_PLAN_WITHOUT_PARTIALS_SMALLER_FILES.md for detailed plan. Key principles:
- No `partial` classes
- Feature classes in `Terminal/EmulatorOps/` (e.g., `TerminalCursorMovementOps.cs`, `TerminalEraseOps.cs`)
- Façade pattern: `TerminalEmulator` delegates to operation classes
- No business logic changes during refactoring

### Code Standards (from Directory.Build.props)
- .NET 10 with C# 13
- Nullable reference types enabled
- Warnings treated as errors (except CS1591 for missing XML docs)
- All projects generate XML documentation

### Tracing
- SQLite-based tracing in `caTTY.Core/Tracing/`
- Disabled by default for performance
- Enable with `TerminalTracer.Enabled = true`
- Database location: `%TEMP%\catty_trace.db` (Windows)

### Tools
- ALWAYS use bun to run .ts scripts

## Common Workflows

### Adding a new escape sequence handler
1. Add parsing logic to appropriate parser (e.g., `CsiParser.cs`)
2. Define message type if needed (e.g., in `Types/CsiMessage.cs`)
3. Add handler in `TerminalEmulator.cs` (or appropriate ops class if refactored)
4. Add unit tests in `caTTY.Core.Tests/`
5. Run tests with `.\scripts\dotnet-test.ps1`

### Adding a new RPC command (OSC-based)
1. Add action constant to `Rpc/OscRpcHandler.cs` `Actions` class
2. Add dispatch case in `OscRpcHandler.DispatchAction()`
3. Add unit tests in `caTTY.Core.Tests/Unit/Rpc/OscRpcHandlerTests.cs`
4. Use from shell: `echo -ne '\e]1010;{"action":"your_action"}\a'`

### Adding a new RPC command (CSI-based)
1. Create command handler implementing `IRpcCommandHandler` in `Rpc/`
2. Register in `GameActionRegistry.RegisterVehicleCommands()` or custom registry
3. Commands 1001-1999 are fire-and-forget, 2001-2999 are queries
4. Add tests in `caTTY.Core.Tests/Unit/Rpc/`

### Testing terminal behavior
1. Use `caTTY.TestApp` for quick console testing
2. Add unit tests in `caTTY.Core.Tests/`
3. For display issues, test with `caTTY.Display.Playground` or full game mod
4. Always run tests via `.\scripts\dotnet-test.ps1`

### Deploying game mod
1. Build: `dotnet build caTTY.GameMod`
2. Copy `caTTY.GameMod/dist/*` to KSA mods folder
3. Launch KSA, press F12 to toggle terminal

## Important Files

- `Directory.Build.props`: Shared build configuration, KSA installation path
- `.editorconfig`: C# formatting rules
- `FEATURE_TRACKING.md`: VT100/xterm feature implementation status
- `caTTY.Core/Tracing/README.md`: Tracing system documentation
- `caTTY.GameMod/README.md`: Game mod installation and usage guide
- `scripts/dotnet-test.ps1`: Test runner that suppresses verbose output and auto-parses results (MUST USE THIS)
- `scripts/test-errors.ts`: TRX parser (automatically invoked by dotnet-test.ps1)
