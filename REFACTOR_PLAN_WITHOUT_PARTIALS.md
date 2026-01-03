# Refactor Plan (No `partial` classes) — catty-ksa

## Why a second plan
This plan achieves the same end goals as [REFACTOR_PLAN.md](REFACTOR_PLAN.md) but **explicitly avoids `partial` classes**.

Rationale (per your concern): `partial` splits are convenient, but they can:
- make text-based navigation harder (definition “spreads” across files)
- obscure execution flow for humans/LLMs that aren’t using an AST indexer

So this plan uses **composition** and **feature-focused helper types** (small classes) instead.

## Goals / Constraints
- **Primary goal:** reduce major hotspot files to **~≤ 500 LOC** when practical.
- **Hard constraint:** **NO business logic changes**.
  - Only rearrange code (move methods/types, split files, extract helpers).
  - When code must move across class boundaries, the new method should be a *verbatim move* with only mechanical signature adjustments.
- **Navigability:** code should be discoverable by:
  - predictable folders
  - consistent naming (`*Handler`, `*Operations`, `*Native`, `*Factory`)
  - minimal “jumping” (clear call chain from façade → feature class)
- **Incremental execution:** each task is small and should fit an AI/LLM context window.

## What “no partials” implies (design rules)
To keep refactors safe and navigable:

1. **Use a façade + feature classes pattern**
   - Keep the existing “main” class (e.g., `TerminalEmulator`, `Parser`, `ProcessManager`, `SessionManager`, `TerminalController`) as an orchestrator.
   - Move cohesive clusters of methods into **new internal sealed classes**.

2. **Prefer explicit dependencies over reaching into private fields**
   - Feature classes should take dependencies via constructor parameters.
   - Avoid making previously-private fields `internal` just to enable extraction.

3. **Use “context” objects only when dependency lists get large**
   - For large surfaces, introduce `internal sealed class TerminalEmulatorContext` / `ParserContext` that holds the already-existing dependencies.
   - This is still a rearrangement: the context just passes references.

4. **Keep execution flow obvious**
   - Each public/entry method calls exactly one feature method, e.g.:
     - `TerminalEmulator.Resize(...)` → `_resize.Resize(...)`
     - `Parser.PushByte(...)` → `_engine.PushByte(...)`
   - Feature methods may call other feature methods only via their owning façade (or via shared context) to keep the call graph discoverable.

5. **Avoid clever dispatch tables initially**
   - Large `switch` blocks (CSI dispatch) can be moved as-is into dedicated handler classes.
   - Later, optional refactors can replace switches with dictionaries—but that’s riskier and should be a separate phase.

## Hotspots (largest files observed)
### caTTY.Core
- `caTTY.Core/Terminal/TerminalEmulator.cs` (~2151)
- `caTTY.Core/Terminal/ProcessManager.cs` (~825)
- `caTTY.Core/Terminal/TerminalParserHandlers.cs` (~784)
- `caTTY.Core/Terminal/SessionManager.cs` (~723)
- `caTTY.Core/Parsing/Parser.cs` (~604)
- Also large: `caTTY.Core/Parsing/SgrParser.cs` (~846), `CsiParser.cs` (~667)

### caTTY.Display
- `caTTY.Display/Controllers/TerminalController.cs` (~4363)

## Validation commands (recommended after each task)
- `dotnet build catty-ksa/caTTY.Core/caTTY.Core.csproj`
- `dotnet test catty-ksa/caTTY.Core.Tests/caTTY.Core.Tests.csproj`
- `dotnet test catty-ksa/caTTY.Display.Tests/caTTY.Display.Tests.csproj` (when touching Display)

---

# Target End-State Layout (No partials)

## caTTY.Core/Terminal
Keep the existing public façade types, but extract feature classes into a predictable subfolder:

- `caTTY.Core/Terminal/TerminalEmulator.cs` (façade, ctor wiring, public surface)
- `caTTY.Core/Terminal/Emulator/`
  - `TerminalInputProcessor.cs` (Write/Flush)
  - `TerminalResizeService.cs`
  - `TerminalViewportScroller.cs`
  - `TerminalC0Controls.cs` (LF/CR/BS/TAB/BEL/SI/SO/Index/ReverseIndex)
  - `TerminalCursorOperations.cs` (cursor positioning + save/restore)
  - `TerminalScreenOperations.cs` (erase/scroll/scroll region)
  - `TerminalInsertDeleteOperations.cs` (IL/DL/ICH/DCH/ECH + insert-mode helpers)
  - `TerminalTabStops.cs`
  - `TerminalModeOperations.cs` (DECSET/DECRST + alternate screen)
  - `TerminalOscOperations.cs` (title/icon/window manipulation/clipboard/hyperlink/color queries)
  - `TerminalCharsetOperations.cs`
  - `TerminalEventEmitter.cs` (event raising + response emission)

## caTTY.Core/Parsing
- `caTTY.Core/Parsing/Parser.cs` (façade)
- `caTTY.Core/Parsing/ParserEngine.cs` (state machine implementation moved from `Parser`)
- `caTTY.Core/Parsing/ParserEngineContext.cs` (buffers/state/dependencies)
- `caTTY.Core/Parsing/ParserEngine.*.cs` is **not allowed** (no partials) so instead:
  - `ParserNormalState.cs`, `ParserEscapeState.cs`, `ParserCsiState.cs`, `ParserOscState.cs`, `ParserDcsState.cs`, `ParserControlStringState.cs`, `ParserRpcHandler.cs`
  - Each implements a tiny interface like `IParserStateHandler` or exposes `HandleByte(...)`.

## caTTY.Core/Terminal parsing bridge
- `caTTY.Core/Terminal/TerminalParserHandlers.cs` (façade implementing `IParserHandlers`)
- `caTTY.Core/Terminal/ParserHandlers/`
  - `CsiHandler.cs` (contains the big CSI switch moved as-is)
  - `EscHandler.cs`
  - `OscHandler.cs`
  - `DcsHandler.cs` (incl DECRQSS)
  - `SgrHandler.cs` (apply + tracing)

## caTTY.Core/Terminal process management
- `caTTY.Core/Terminal/ProcessManager.cs` (façade)
- `caTTY.Core/Terminal/Process/`
  - `ConPtyNative.cs` (constants, structs, `DllImport`s)
  - `ShellCommandResolver.cs` (Resolve* methods)
  - `ConPtyIoPump.cs` (ReadOutputAsync)
  - `ProcessCleanup.cs` (cleanup helpers)

## caTTY.Core/Terminal session management
- `caTTY.Core/Terminal/SessionManager.cs` (façade)
- `caTTY.Core/Terminal/Sessions/`
  - `TerminalSessionFactory.cs` (creates `TerminalEmulator`, `ProcessManager`, RPC wiring)
  - `SessionCollection.cs` (optional: manages `_sessions`, `_sessionOrder`, active id)

## caTTY.Display
- Split pure types out of the controller file:
  - `caTTY.Display/Controllers/LayoutConstants.cs`
  - `caTTY.Display/Controllers/TerminalSettings.cs`
- For the remaining huge controller:
  - `caTTY.Display/Controllers/TerminalController.cs` (façade)
  - `caTTY.Display/Controllers/TerminalUi/`
    - `TerminalFonts.cs`
    - `TerminalMouseTracking.cs`
    - `TerminalSelection.cs`
    - `TerminalResizeCoordinator.cs`
    - `TerminalRenderer.cs`
    - `TerminalInputTranslator.cs`
    - `TerminalTabsUi.cs`

---

# Incremental Task List (No partials)

## Phase 0 — Baseline / Safety

### Task 0.1 — Record a baseline
- Run the validation commands.
- Note any failing/flaky tests (do not fix in this refactor).

---

## Phase 1 — ProcessManager.cs (best first extraction)
This file is ideal for no-partial extraction because the interop layer can be cleanly moved.

### Task 1.1 — Extract Win32/ConPTY interop into `ConPtyNative.cs`
- Add new file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyNative.cs`.
- Move from `caTTY.Core/Terminal/ProcessManager.cs`:
  - `EXTENDED_STARTUPINFO_PRESENT`, `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
  - all `[DllImport]` declarations
  - `COORD`, `STARTUPINFOEX`, `STARTUPINFO`, `PROCESS_INFORMATION`
- Keep namespace `caTTY.Core.Terminal` **or** use `caTTY.Core.Terminal.Process` (pick one and be consistent).
  - If you change namespace, update references in `ProcessManager.cs`.
- Ensure all moved members remain `internal`/`private` equivalent (usually `internal static` inside `ConPtyNative`).
- Build + run core tests.

### Task 1.2 — Extract shell resolution into `ShellCommandResolver.cs`
- Add new file: `catty-ksa/caTTY.Core/Terminal/Process/ShellCommandResolver.cs`.
- Move these methods verbatim:
  - `ResolveShellCommand`, `ResolveAutoShell`, `ResolveWsl`, `ResolvePowerShell`, `ResolvePowerShellCore`, `ResolveCmd`, `ResolveCustomShell`, `FindExecutableInPath`
- Replace calls inside `ProcessManager.StartAsync(...)` with `ShellCommandResolver.ResolveShellCommand(options)`.
- Build + run core tests.

### Task 1.3 — Extract output pump into `ConPtyIoPump.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyIoPump.cs`.
- Move `ReadOutputAsync(...)` into a method on `ConPtyIoPump`.
- `ProcessManager` should own an instance `private readonly ConPtyIoPump _ioPump;` created in ctor.
  - If there’s no ctor, initialize field inline.
- Pass required dependencies explicitly:
  - `_outputReadHandle`, `ProcessId` getter, callbacks for `OnDataReceived`/`OnProcessError`.
- Keep method body identical except replacing field accesses with parameters.
- Build + run core tests.

### Task 1.4 — Extract cleanup helpers into `ProcessCleanup.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ProcessCleanup.cs`.
- Move:
  - `CleanupProcess`, `CleanupPseudoConsole`, `CleanupHandles`
- Convert them to static helpers that accept the handles and return updated values **or** instance helpers on a small `ProcessCleanup` class.
- Keep the operations order identical.
- Build + run core tests.

**Checkpoint:** `ProcessManager.cs` should drop well below ~500 LOC.

---

## Phase 2 — SessionManager.cs (extract session creation + RPC wiring)

### Task 2.1 — Introduce `TerminalSessionFactory`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/TerminalSessionFactory.cs`.
- Move the *exact* RPC wiring block currently inside `SessionManager.CreateSessionAsync(...)` into the factory.
  - Router, response generator, output buffer list, handler, registry registration.
- Factory API suggestion:
  - `internal sealed class TerminalSessionFactory`
  - `public TerminalSession Create(Guid id, string title, ProcessLaunchOptions launchOptions)`
  - It returns a fully wired `TerminalSession`.
- `SessionManager.CreateSessionAsync(...)` becomes:
  - compute effective launch options (unchanged)
  - call factory to create session
  - wire session events (unchanged)
  - initialize + activate (unchanged)
- Build + core tests.

### Task 2.2 — Extract launch option cloning to `ProcessLaunchOptionsExtensions.cs` (optional)
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/ProcessLaunchOptionsExtensions.cs`.
- Move `CloneLaunchOptions` verbatim into an extension method.
- Only update call sites.
- Build + core tests.

### Task 2.3 — Extract session switching logic to `SessionSwitcher`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionSwitcher.cs`.
- Move the logic of:
  - `SwitchToSession`, `SwitchToNextSession`, `SwitchToPreviousSession`
- `SessionManager` delegates to `SessionSwitcher`.
- Keep locking behavior identical (do not change lock granularity/order).
- Build + core tests.

**Checkpoint:** `SessionManager.cs` should be ≤500-ish or close.

---

## Phase 3 — TerminalParserHandlers.cs (split by sequence family)
This is a clean no-partial extraction: keep `TerminalParserHandlers` implementing `IParserHandlers`, but move the big logic into specialized handler classes.

### Task 3.1 — Extract SGR handling to `SgrHandler`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs`.
- Move:
  - `HandleSgrSequence` implementation
  - tracing helpers: `TraceSgrSequence`, `FormatColor`, `ExtractSgrParameters`
- `TerminalParserHandlers.HandleSgr(...)` delegates to `_sgrHandler.HandleSgr(...)`.
- Build + core tests.

### Task 3.2 — Extract DCS/DECRQSS handling to `DcsHandler`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/DcsHandler.cs`.
- Move:
  - `HandleDcs`, `HandleDecrqss`, `ExtractDecrqssPayload`, `GenerateSgrStateResponse`
- `TerminalParserHandlers.HandleDcs(...)` delegates.
- Build + core tests.

### Task 3.3 — Extract OSC handling to `OscHandler`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/OscHandler.cs`.
- Move:
  - `HandleOsc`, `HandleXtermOsc`
- Delegate from `TerminalParserHandlers`.
- Build + core tests.

### Task 3.4 — Extract CSI switch to `CsiHandler` (verbatim move)
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiHandler.cs`.
- Move the entire `switch (message.Type)` from `HandleCsi` into `CsiHandler.HandleCsi(CsiMessage message)`.
- Keep the switch cases identical (no re-ordering, no dictionary refactor).
- Delegate from `TerminalParserHandlers.HandleCsi`.
- Build + core tests.

**Checkpoint:** `TerminalParserHandlers.cs` becomes a small façade.

---

## Phase 4 — TerminalEmulator.cs (largest core hotspot, no partials)
This is the trickiest without partials. The safest approach is:
- keep `TerminalEmulator` public surface
- move cohesive method clusters into **feature classes** under `Terminal/Emulator/`
- update callers (mostly `TerminalParserHandlers` and its helpers) to call the feature classes instead of internal methods

### Task 4.1 — Create feature class skeletons (no moves)
- Add empty internal sealed classes under `catty-ksa/caTTY.Core/Terminal/Emulator/`:
  - `TerminalResizeService`, `TerminalOscOperations`, `TerminalCursorOperations`, `TerminalScreenOperations`, `TerminalModeOperations`, `TerminalInsertDeleteOperations`, `TerminalTabStops`, `TerminalCharsetOperations`, `TerminalViewportScroller`, `TerminalC0Controls`, `TerminalEventEmitter`.
- Each constructor accepts only what it needs (e.g., `TerminalResizeService(IScreenBufferManager, ICursorManager, TerminalState, IScrollbackManager, ILogger)` etc.).
- No behavior change yet.
- Build.

### Task 4.2 — Move viewport methods into `TerminalViewportScroller`
- Move verbatim method bodies:
  - `ScrollViewportUp/Down/ToTop/ToBottom`, `IsAutoScrollEnabled`, `ViewportOffset`
- Replace `TerminalEmulator` implementations with delegation.
- Build + core tests.

### Task 4.3 — Move resize into `TerminalResizeService`
- Move `Resize(int width, int height)` body verbatim.
- Keep all validation + scrollback behavior identical.
- `TerminalEmulator.Resize(...)` delegates.
- Build + core tests.

### Task 4.4 — Move OSC/window/clipboard/hyperlink/color queries into `TerminalOscOperations`
- Move:
  - `HandleWindowManipulation`, `HandleClipboard`, `HandleHyperlink`
  - title/icon setters/getters
  - color query helpers + palette helpers
- Ensure `TerminalParserHandlers.OscHandler` calls `_terminal.Osc.*` (new property) rather than old internal methods.
- Build + core tests.

### Task 4.5 — Move cursor operations into `TerminalCursorOperations`
- Move:
  - cursor move/set methods
  - ANSI/DEC save/restore
  - cursor style setters
- Update `CsiHandler` (or whichever handler calls these) to call `_terminal.CursorOps.*`.
- Build + core tests.

### Task 4.6 — Move screen erase/scroll/regions into `TerminalScreenOperations`
- Move:
  - `ClearDisplay/ClearLine` + selective variants
  - `ScrollScreenUp/Down`, `SetScrollRegion`
- Update handlers to call `_terminal.ScreenOps.*`.
- Build + core tests.

### Task 4.7 — Move insert/delete/tab stop/modes/charset into dedicated feature classes
- `TerminalInsertDeleteOperations`: IL/DL/ICH/DCH/ECH + insert-mode helpers.
- `TerminalTabStops`: tab stop operations.
- `TerminalModeOperations`: `SetDecMode`, alt-screen handling, private mode save/restore, bracketed paste helpers.
- `TerminalCharsetOperations`: charset designation/translation/shift-in/out.
- Update handlers to call the feature classes.
- Build + core tests after each extraction.

### Task 4.8 — Decide what stays inside `TerminalEmulator`
To keep the façade easy to follow:
- Keep ctor, fields, event declarations, `Write(...)` entrypoints, and disposal in `TerminalEmulator.cs`.
- Move event raisers into `TerminalEventEmitter` if it reduces LOC.

**Checkpoint:** `TerminalEmulator.cs` should shrink dramatically, with functionality discoverable under `Terminal/Emulator/*`.

---

## Phase 5 — Parser.cs (state machine extraction)
Without partials, the safest pattern is to move the full implementation into a single `ParserEngine` and keep `Parser` as a thin wrapper.

### Task 5.1 — Introduce `ParserEngine` (no logic changes)
- Add file: `catty-ksa/caTTY.Core/Parsing/ParserEngine.cs`.
- Move all private fields + private methods from `Parser` into `ParserEngine`.
- `Parser` should keep the same public API and forward to `_engine`.
- Build + core tests.

### Task 5.2 — Optional: split engine by state handlers (only if still too large)
If `ParserEngine.cs` remains >500 LOC and you still want the limit:
- Add:
  - `ParserNormalState.cs`, `ParserEscapeState.cs`, `ParserCsiState.cs`, `ParserOscState.cs`, `ParserDcsState.cs`, `ParserControlStringState.cs`, `ParserRpcHandler.cs`
- Each is a small class that operates on a shared `ParserEngineContext` (buffers + current state + dependencies).
- **Important:** keep byte-processing semantics identical.
- Build + core tests after each state extraction.

---

## Phase 6 — SgrParser.cs / CsiParser.cs (secondary parsing hotspots)
These can be split cleanly without partials by extracting helpers.

### Task 6.1 — Split SgrParser into `SgrParser` façade + `SgrParsing` + `SgrApplying`
- Add `SgrParsing.cs`: move parsing-related helpers.
- Add `SgrApplying.cs`: move application logic.
- Keep `SgrParser` public and delegate.
- Build + core tests.

### Task 6.2 — Split CsiParser similarly
- Create `CsiParsing.cs` / `CsiMessages.cs` helper types as needed.
- Keep `CsiParser` public and delegate.
- Build + core tests.

---

## Phase 7 — TerminalController.cs (Display hotspot)
Without partials, prefer a façade controller that delegates to focused UI subsystems.

### Task 7.1 — Move `LayoutConstants` to its own file
- Add `catty-ksa/caTTY.Display/Controllers/LayoutConstants.cs`.
- Move the type verbatim.
- Build display projects + tests.

### Task 7.2 — Move `TerminalSettings` to its own file
- Add `catty-ksa/caTTY.Display/Controllers/TerminalSettings.cs`.
- Move the type verbatim.
- Build + tests.

### Task 7.3 — Introduce UI subsystems (one at a time)
- Add a folder: `catty-ksa/caTTY.Display/Controllers/TerminalUi/`.
- Start with the least-coupled areas:
  - `TerminalFonts` (font loading + selection state)
  - `TerminalSelection` (selection state + helpers)
  - `TerminalMouseTracking` (mouse tracking + encoding)
  - `TerminalResizeCoordinator` (debounce + resize triggering)
  - `TerminalInputTranslator` (keys/paste/input buffer)
  - `TerminalRenderer` (render loop helpers)
- For each subsystem:
  - move a cohesive method cluster verbatim
  - pass dependencies explicitly (config objects, session manager, ImGui pointers)
  - keep `TerminalController` methods delegating 1:1
- Build + display tests after each extraction.

---

# Completion Criteria
- The hotspot files listed above are ≤ ~500 LOC where practical.
- All behavior stays identical (tests pass).
- A reader can find functionality by following:
  - `TerminalEmulator` → `Terminal/Emulator/*`
  - `TerminalParserHandlers` → `Terminal/ParserHandlers/*`
  - `ProcessManager` → `Terminal/Process/*`
  - `TerminalController` → `Controllers/TerminalUi/*`
