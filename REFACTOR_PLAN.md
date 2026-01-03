# Refactor Plan (catty-ksa)

## Goals / Constraints
- **Primary goal:** reduce major hotspot files to **~≤ 500 LOC per file** (C# is verbose, so this is a soft target but we should bias toward it).
- **Hard constraint:** **NO business logic changes**.
  - Only rearrange code (move methods/types, split files, rename *files* / folders, introduce partials, extract helpers that are pure delegations).
  - Public APIs should remain stable unless a change is *strictly mechanical* (e.g., `partial` keyword) and does not affect consumers.
- **Navigability goal:** arrange code so it’s easy to find related behavior (co-locate similar code, consistent naming, predictable locations).
- **Incremental execution:** every task below is intended to be small enough that an AI/LLM agent can complete it in a tight context window.

## Starting Point (from .kiro/steering/structure.md)
- `caTTY.Core` is the headless terminal engine (parsing, state, tracing, types).
- `caTTY.Display` is the ImGui-facing controller layer (input/render glue).
- Parser design principle: decompose by sequence type (CSI/SGR/OSC/ESC/DCS/UTF-8), keep it testable and framework-agnostic.

## Hotspots (largest files observed)
> Line counts are approximate and may differ slightly depending on tooling.

### caTTY.Core
- `caTTY.Core/Terminal/TerminalEmulator.cs` (~2.5k LOC): terminal core + huge surface area (cursor, screen ops, OSC/CSI helpers, modes, responses, clipboard, etc.)
- `caTTY.Core/Terminal/ProcessManager.cs` (~0.95k LOC): ConPTY lifecycle + P/Invoke + IO loop + shell resolution
- `caTTY.Core/Terminal/TerminalParserHandlers.cs` (~0.93k LOC): parser-to-terminal bridge + massive CSI switch + SGR tracing + DECRQSS helpers
- `caTTY.Core/Terminal/SessionManager.cs` (~0.85k LOC): session lifecycle, tab order, restart, plus inline RPC wiring
- `caTTY.Core/Parsing/SgrParser.cs` (~0.88k LOC): SGR parse + apply + helpers
- `caTTY.Core/Parsing/CsiParser.cs` (~0.67k LOC)
- `caTTY.Core/Parsing/Parser.cs` (~0.68k LOC)
- `caTTY.Core/Managers/ScreenBufferManager.cs` (~0.54k LOC) (borderline)

### caTTY.Display
- `caTTY.Display/Controllers/TerminalController.cs` (~5.0k LOC): layout constants + settings + controller implementation (render, input, selection, fonts, mouse tracking, resize handling, tab/session UI)

## Refactor Strategy (behavior-preserving)

### Preferred technique: **partial classes + file splits**
This repo already has large, cohesive classes with many related methods. Converting those classes to `partial` and moving method groups into themed files is the lowest-risk way to:
- keep all fields private as-is (no new interfaces needed)
- preserve method bodies exactly (cut/paste only)
- avoid changing construction or call graphs

### Secondary technique (optional): “extracted helper” classes
Only when a partial split still leaves a file >500 LOC *or* when code is obviously a standalone concern (e.g., Win32 interop structs/constants). Helpers should:
- be `internal` and live adjacent to the caller
- keep signatures minimal
- contain **no new logic** (only moved logic)

### Validation principle
After **each task**:
- build the solution (or at least the touched project)
- run the most relevant tests (see “Validation Commands”)

## Proposed Target Layout (end state)
This is the *intended* arrangement once all tasks are completed.

### caTTY.Core/Terminal
- Keep folder, keep namespaces, but split large types into multiple files:
  - `TerminalEmulator.cs` (small public surface + ctor)
  - `TerminalEmulator.*.cs` partials grouped by concern
  - `TerminalParserHandlers.cs` (thin entry points) + `TerminalParserHandlers.*.cs`
  - `SessionManager.cs` (thin public API surface) + `SessionManager.*.cs`
  - `ProcessManager.cs` (thin public API surface) + `ProcessManager.*.cs`

### caTTY.Core/Parsing
- `Parser.cs` + `Parser.*.cs` (partial, grouped by state: Normal/Escape/CSI/OSC/DCS/ControlString/RPC/UTF8)
- `SgrParser.cs` split into `SgrParser.*.cs` (parsing vs application vs helpers)
- `CsiParser.cs` split into `CsiParser.*.cs` if still needed

### caTTY.Display/Controllers
- Split top-level types and the `TerminalController` monster file:
  - `LayoutConstants.cs`
  - `TerminalSettings.cs`
  - `TerminalController.cs` + `TerminalController.*.cs` partials (rendering, input, mouse, selection, fonts, resize)

## Validation Commands (recommended)
- Build core: `dotnet build catty-ksa/caTTY.Core/caTTY.Core.csproj`
- Core tests: `dotnet test catty-ksa/caTTY.Core.Tests/caTTY.Core.Tests.csproj`
- Display tests (if touched): `dotnet test catty-ksa/caTTY.Display.Tests/caTTY.Display.Tests.csproj`

---

# Incremental Task List

## Phase 0 — Prep / Safety Net

### Task 0.1 — Establish baseline
**Goal:** ensure we can detect accidental behavior changes.
- Run the validation commands above and save the output (or at least confirm green).
- Identify any tests that are flaky; mark them as “do not use for refactor gating” (do not change them yet).

### Task 0.2 — Agree on naming conventions for partial splits
**Decision:** Use `TypeName.Area.cs` naming.
- Example: `TerminalEmulator.Cursor.cs`, `Parser.Osc.cs`, `ProcessManager.Interop.cs`.
- Keep namespaces identical to current (`namespace caTTY.Core.Terminal;`, etc.).

---

## Phase 1 — TerminalEmulator.cs (core hotspot)
### Current responsibilities (observed)
- Parsing pipeline wiring (`ParserOptions`, `TerminalParserHandlers`, optional RPC wiring)
- Input entry points: `Write(ReadOnlySpan<byte>)`, `Write(string)`, `FlushIncompleteSequences()`
- Resize logic + scrollback integration
- Viewport scrolling APIs
- Cursor movement + cursor save/restore (DEC + ANSI)
- Screen operations: clear display/line, selective erase, scroll region
- Tab stops + line/char insert/delete/erase
- Mode toggles (DECSET/DECRST, bracketed paste, origin, auto-wrap, alt screen)
- OSC-related helpers: title/icon stacks, clipboard OSC 52, hyperlinks OSC 8, color queries
- Responses back to shell (device responses)

### Target split (suggested files)
- `TerminalEmulator.cs` — fields/ctor + public surface entry points + basic events
- `TerminalEmulator.Input.cs` — `Write(...)`, `FlushIncompleteSequences()`
- `TerminalEmulator.Resize.cs` — `Resize(...)` and related helpers
- `TerminalEmulator.Viewport.cs` — `ScrollViewport*`, `IsAutoScrollEnabled`, `ViewportOffset`
- `TerminalEmulator.Cursors.cs` — cursor movement, save/restore, cursor style
- `TerminalEmulator.ScreenOps.cs` — clear/erase, scrolling, scroll region
- `TerminalEmulator.Tabs.cs` — tab stop operations
- `TerminalEmulator.InsertDelete.cs` — insert/delete lines/chars, erase chars, insert mode helpers
- `TerminalEmulator.Modes.cs` — `SetDecMode`, alternate screen handling, private mode save/restore
- `TerminalEmulator.Osc.cs` — title/icon, window manipulation, clipboard, hyperlink, color queries
- `TerminalEmulator.Charsets.cs` — charset designation/translation/UTF-8 mode hooks
- `TerminalEmulator.Events.cs` — `On*` event raisers + `EmitResponse`
- `TerminalEmulator.Guards.cs` — `ThrowIfDisposed`, `_disposed` handling

### Task 1.1 — Convert TerminalEmulator to partial (no moves yet)
- Edit `caTTY.Core/Terminal/TerminalEmulator.cs`: change `public class TerminalEmulator` to `public partial class TerminalEmulator`.
- Build `caTTY.Core`.

### Task 1.2 — Move viewport scrolling API
- Create `caTTY.Core/Terminal/TerminalEmulator.Viewport.cs` containing the `partial class TerminalEmulator`.
- Move methods/properties:
  - `ScrollViewportUp`, `ScrollViewportDown`, `ScrollViewportToTop`, `ScrollViewportToBottom`
  - `IsAutoScrollEnabled`, `ViewportOffset`
- Ensure `using` directives are minimal; remove unused usings.
- Build + run core tests.

### Task 1.3 — Move resize logic
- Create `TerminalEmulator.Resize.cs`.
- Move `Resize(int width, int height)` as-is (no behavior change).
- Build + core tests.

### Task 1.4 — Move OSC/window/clipboard/hyperlink helpers
- Create `TerminalEmulator.Osc.cs`.
- Move methods:
  - Title/icon setters/getters: `SetWindowTitle`, `SetIconName`, `SetTitleAndIcon`, `GetWindowTitle`, `GetIconName`
  - Color query helpers: `GetCurrentForegroundColor`, `GetCurrentBackgroundColor`, and their private palette helpers
  - `HandleWindowManipulation`, `HandleClipboard`, `HandleHyperlink`
- Build + core tests.

### Task 1.5 — Move cursor movement + save/restore
- Create `TerminalEmulator.Cursors.cs`.
- Move methods:
  - `MoveCursorUp/Down/Forward/Backward`, `SetCursorPosition`, `SetCursorColumn`
  - `SaveCursorPosition`, `RestoreCursorPosition`, `SaveCursorPositionAnsi`, `RestoreCursorPositionAnsi`
  - `SetCursorStyle(int)`, `SetCursorStyle(CursorStyle)`
- Build + core tests.

### Task 1.6 — Move screen erase operations
- Create `TerminalEmulator.ScreenOps.cs`.
- Move methods:
  - `ClearDisplay`, `ClearLine`, `ClearDisplaySelective`, `ClearLineSelective`
  - `ScrollScreenUp`, `ScrollScreenDown`, `SetScrollRegion`
- Build + core tests.

### Task 1.7 — Move insert/delete/erase/insert-mode helpers
- Create `TerminalEmulator.InsertDelete.cs`.
- Move methods:
  - `InsertLinesInRegion`, `DeleteLinesInRegion`
  - `InsertCharactersInLine`, `DeleteCharactersInLine`, `EraseCharactersInLine`
  - `SetInsertMode`, `ShiftCharactersRight`
- Build + core tests.

### Task 1.8 — Move tab stop operations
- Create `TerminalEmulator.Tabs.cs`.
- Move methods:
  - `SetTabStopAtCursor`, `ClearTabStopAtCursor`, `ClearAllTabStops`
  - `CursorForwardTab`, `CursorBackwardTab`
- Build + core tests.

### Task 1.9 — Move modes + alternate screen
- Create `TerminalEmulator.Modes.cs`.
- Move methods:
  - `SetDecMode`, `HandleAlternateScreenMode`
  - `SavePrivateModes`, `RestorePrivateModes`
  - `WrapPasteContent(...)`, `IsBracketedPasteModeEnabled`
- Build + core tests.

### Task 1.10 — Move charset operations
- Create `TerminalEmulator.Charsets.cs`.
- Move methods:
  - `DesignateCharacterSet`, `HandleShiftIn`, `HandleShiftOut`, `TranslateCharacter`, `GenerateCharacterSetQueryResponse`
- Build + core tests.

### Task 1.11 — Move event raisers + guards
- Create `TerminalEmulator.Events.cs` and `TerminalEmulator.Guards.cs`.
- Move:
  - `EmitResponse`, `OnScreenUpdated`, `OnResponseEmitted(...)`, `OnBell`, `OnTitleChanged`, `OnIconNameChanged`, `OnClipboardRequest`
  - `ThrowIfDisposed`
- Build + core tests.

### Task 1.12 — Optional: isolate C0 control handlers
- If still large, create `TerminalEmulator.C0.cs` and move:
  - `HandleLineFeed`, `HandleCarriageReturn`, `HandleBackspace`, `HandleTab`, `HandleBell`, `HandleIndex`, `HandleReverseIndex`
- Build + core tests.

---

## Phase 2 — TerminalParserHandlers.cs
### Current responsibilities (observed)
- Implements `IParserHandlers` and maps parsed messages to `TerminalEmulator` operations
- Large `HandleCsi` switch (cursor movement, erase, modes, device queries, insert/delete)
- SGR application + SGR tracing + DECRQSS response generation
- OSC handling for title/icon/clipboard/hyperlink/color queries

### Target split
- `TerminalParserHandlers.cs` — ctor + trivial forwards + small dispatch entrypoints
- `TerminalParserHandlers.C0.cs` — C0 handlers (bell/bs/tab/lf/cr/si/so)
- `TerminalParserHandlers.Esc.cs` — ESC mapping
- `TerminalParserHandlers.Csi.cs` — CSI mapping (may be split further)
- `TerminalParserHandlers.Csi.Cursor.cs`, `.Csi.Erase.cs`, `.Csi.Modes.cs`, `.Csi.DeviceQueries.cs`, `.Csi.InsertDelete.cs`
- `TerminalParserHandlers.Sgr.cs` — `HandleSgr`, `HandleSgrSequence`, tracing helpers
- `TerminalParserHandlers.Osc.cs` — `HandleOsc`, `HandleXtermOsc`
- `TerminalParserHandlers.Dcs.cs` — `HandleDcs`, `HandleDecrqss`, payload extraction, response generation

### Task 2.1 — Convert to partial
- Change `internal class TerminalParserHandlers` to `internal partial class TerminalParserHandlers`.
- Build.

### Task 2.2 — Move SGR handling + tracing
- Create `TerminalParserHandlers.Sgr.cs`.
- Move:
  - `HandleSgr`, `HandleSgrSequence`
  - `TraceSgrSequence`, `FormatColor`, `ExtractSgrParameters`
- Build + core tests.

### Task 2.3 — Move DCS/DECRQSS handling
- Create `TerminalParserHandlers.Dcs.cs`.
- Move:
  - `HandleDcs`, `HandleDecrqss`, `ExtractDecrqssPayload`, `GenerateSgrStateResponse`
- Build + core tests.

### Task 2.4 — Move Xterm OSC handling
- Create `TerminalParserHandlers.Osc.cs`.
- Move:
  - `HandleOsc`, `HandleXtermOsc`
- Build + core tests.

### Task 2.5 — Split CSI handling by concern
- Keep the public `HandleCsi(CsiMessage)` signature.
- Introduce private partial helper methods (pure extraction, no logic changes), e.g.:
  - `HandleCsiCursorOps`, `HandleCsiEraseOps`, `HandleCsiModes`, `HandleCsiDeviceQueries`, `HandleCsiInsertDelete`
- First task: create `TerminalParserHandlers.Csi.Cursor.cs` and move cursor-related cases.
- Repeat with additional CSI partial files until each is ≤500 LOC.
- Build + core tests after each extraction.

---

## Phase 3 — Parser.cs (escape state machine)
### Current responsibilities (observed)
- Core byte-driven parser state machine
- Delegation to specialized parsers (CSI/ESC/OSC/DCS/SGR)
- UTF-8 decode + tracing hooks
- Optional RPC detection/parse/dispatch

### Target split
- `Parser.cs` — ctor + `PushBytes/PushByte` + state switch
- `Parser.Normal.cs` — normal state + `HandleNormalState`, `HandleNormalByte`, C0 handling
- `Parser.Escape.cs` — ESC state + `HandleEscapeState`, `HandleEscapeByte`, `StartEscapeSequence`
- `Parser.Csi.cs` — CSI state + `HandleCsiState`, `HandleCsiByte`, `FinishCsiSequence`
- `Parser.Osc.cs` — OSC state methods
- `Parser.Dcs.cs` — DCS state methods + `FinishDcsSequence`
- `Parser.ControlString.cs` — SOS/PM/APC handling
- `Parser.Rpc.cs` — `IsRpcHandlingEnabled`, `TryHandleRpcSequence`
- `Parser.Helpers.cs` — `ResetEscapeState`, `MaybeEmitNormalByteDuringEscapeSequence`, `BytesToString`

### Task 3.1 — Convert Parser to partial
- Change `public class Parser` to `public partial class Parser`.
- Build.

### Task 3.2+ — Split one state at a time
- Create each partial file above and move only the relevant methods.
- Keep method bodies unchanged.
- Build + core tests after each state move.

---

## Phase 4 — ProcessManager.cs (ConPTY)
### Current responsibilities (observed)
- Process lifecycle (`StartAsync`, `StopAsync`, `Dispose`)
- ConPTY handle + pipe lifecycle
- Output pump (`ReadOutputAsync`) and events
- Shell resolution (`ResolveShellCommand`, `ResolveWsl`, etc.)
- Win32 P/Invoke declarations + structs

### Target split
- `ProcessManager.cs` — public API surface + fields
- `ProcessManager.StartStop.cs` — `StartAsync`, `StopAsync`, `Dispose`
- `ProcessManager.IO.cs` — `ReadOutputAsync`, `Write(...)`, event raisers
- `ProcessManager.ShellResolution.cs` — all `Resolve*` and `FindExecutableInPath`
- `ProcessManager.Interop.cs` — constants, `DllImport` declarations, `COORD/STARTUPINFOEX/...` structs
- `ProcessManager.Cleanup.cs` — `CleanupProcess`, `CleanupPseudoConsole`, `CleanupHandles`, guard methods

### Task 4.1 — Convert ProcessManager to partial
- Change `public class ProcessManager` to `public partial class ProcessManager`.
- Build.

### Task 4.2 — Move Win32 interop
- Create `ProcessManager.Interop.cs`.
- Move constants, `DllImport` declarations, and structs.
- Build + core tests (especially process tests if available).

### Task 4.3 — Move shell resolution
- Create `ProcessManager.ShellResolution.cs`.
- Move `ResolveShellCommand` and all `Resolve*` helpers.
- Build + core tests.

### Task 4.4 — Move IO pump and events
- Create `ProcessManager.IO.cs`.
- Move `ReadOutputAsync`, `Write(...)` overloads, `OnDataReceived`, `OnProcessError`, `OnProcessExited`.
- Build + core tests.

### Task 4.5 — Move cleanup + guards
- Create `ProcessManager.Cleanup.cs`.
- Move cleanup methods and `ThrowIfDisposed`.
- Build + core tests.

---

## Phase 5 — SessionManager.cs
### Current responsibilities (observed)
- Session collection + tab order + active session switching
- Create/close/restart sessions
- Tracks “last known terminal dimensions” for consistent sizing
- Inline wiring of RPC router/handler/registry during session creation
- Debug logging utilities and event handlers

### Target split
- `SessionManager.cs` — fields/ctor + public surface (thin)
- `SessionManager.Options.cs` — default launch options + dimension tracking
- `SessionManager.Create.cs` — `CreateSessionAsync` + cleanup
- `SessionManager.Switching.cs` — `SwitchToSession`, next/prev
- `SessionManager.Close.cs` — `CloseSessionAsync`
- `SessionManager.Restart.cs` — `RestartSessionAsync`
- `SessionManager.Events.cs` — `OnSession*` handlers
- `SessionManager.Logging.cs` — `LogSessionLifecycleEvent`, `IsDebugLoggingEnabled`
- `SessionManager.Dispose.cs` — `Dispose`, `ThrowIfDisposed`
- Optional helper: `TerminalSessionFactory` (internal) to hold the RPC wiring + `TerminalEmulator` + `ProcessManager` instantiation

### Task 5.1 — Convert SessionManager to partial
- Change `public class SessionManager` to `public partial class SessionManager`.
- Build.

### Task 5.2 — Split create session
- Create `SessionManager.Create.cs` and move `CreateSessionAsync` + any directly related private helpers.
- Build + core tests.

### Task 5.3 — Extract RPC wiring into a helper (optional but high value)
**Goal:** reduce cognitive load in `CreateSessionAsync` without changing behavior.
- Create `caTTY.Core/Terminal/TerminalSessionFactory.cs` (or `SessionManager.RpcWiring.cs` if you prefer partial-only).
- Move the exact RPC wiring block currently inside `CreateSessionAsync` (router/response generator/buffer/handler/registry registration) into a method:
  - e.g. `internal static IRpcHandler CreateDefaultRpcHandler(out List<byte[]> outputBuffer)`
- Keep the call site behavior identical.
- Build + core tests.

### Task 5.4 — Split switching
- Create `SessionManager.Switching.cs`.
- Move `SwitchToSession`, `SwitchToNextSession`, `SwitchToPreviousSession`.
- Build + core tests.

### Task 5.5 — Split close
- Create `SessionManager.Close.cs`.
- Move `CloseSessionAsync`.
- Build + core tests.

### Task 5.6 — Split restart
- Create `SessionManager.Restart.cs`.
- Move `RestartSessionAsync`.
- Build + core tests.

### Task 5.7 — Split logging + event handlers + dispose
- Create `SessionManager.Events.cs`, `SessionManager.Logging.cs`, `SessionManager.Dispose.cs`.
- Move the corresponding methods.
- Build + core tests.

---

## Phase 6 — SgrParser.cs / CsiParser.cs / ScreenBufferManager.cs (secondary core hotspots)

### Task 6.1 — SgrParser: convert to partial
- Change `public class SgrParser` to `public partial class SgrParser`.
- Build.

### Task 6.2 — SgrParser: split parsing vs applying
- Create `SgrParser.Parse.cs` and move:
  - `ParseSgrSequence`, `TryParseParameters`, and parsing helpers like `ParseSgrParamsAndSeparators`
- Create `SgrParser.Apply.cs` and move:
  - `ApplyAttributes`, `ApplySingleMessage`, and any message-application helpers
- Move internal helper types (`SgrParseContext`, `SgrParseResult`) into `SgrParser.InternalTypes.cs`.
- Build + core tests.

### Task 6.3 — CsiParser: split by message families (if still >500)
- Convert to partial, then split into `CsiParser.Cursor.cs`, `CsiParser.Modes.cs`, `CsiParser.Erase.cs`, etc. by extracting helper methods.
- Build + core tests.

### Task 6.4 — ScreenBufferManager: optional split
- Only if it stays above ~500 LOC after core refactors.
- Prefer partial split by operation type (scrolling, resize, row/cell access, integration hooks).

---

## Phase 7 — caTTY.Display TerminalController.cs (UI hotspot)
### Current responsibilities (observed)
- Contains 3 top-level types: `LayoutConstants`, `TerminalSettings`, and `TerminalController`
- `TerminalController` includes font loading, rendering, input buffering, mouse tracking, selection, resize debounce, session tabs, config UI

### Target split
- `caTTY.Display/Controllers/LayoutConstants.cs`
- `caTTY.Display/Controllers/TerminalSettings.cs`
- `caTTY.Display/Controllers/TerminalController.cs` (partial)
  - `TerminalController.Rendering.cs`
  - `TerminalController.Input.cs`
  - `TerminalController.Mouse.cs`
  - `TerminalController.Selection.cs`
  - `TerminalController.Fonts.cs`
  - `TerminalController.Resize.cs`
  - `TerminalController.TabsSessions.cs`
  - `TerminalController.SettingsUi.cs` (if present)

### Task 7.1 — Split out LayoutConstants
- Create `LayoutConstants.cs` and move the static class.
- Ensure namespace remains `caTTY.Display.Controllers`.
- Build display projects + display tests.

### Task 7.2 — Split out TerminalSettings
- Create `TerminalSettings.cs` and move the class.
- Build + tests.

### Task 7.3 — Convert TerminalController to partial
- Change `public class TerminalController` to `public partial class TerminalController`.
- Build.

### Task 7.4+ — Split TerminalController by concern (one file per task)
- For each partial file, move a cohesive slice:
  - Mouse tracking fields + methods → `TerminalController.Mouse.cs`
  - Selection state + selection rendering → `TerminalController.Selection.cs`
  - Font loading + font selection → `TerminalController.Fonts.cs`
  - Resize debounce + terminal resize triggering → `TerminalController.Resize.cs`
  - Rendering loops/draw helpers → `TerminalController.Rendering.cs`
  - Input buffering/key handling/paste → `TerminalController.Input.cs`
- After each move: build + display tests.

---

## Optional Phase — Large Test Files
Several test/property files exceed 500 LOC. Consider splitting them later for maintainability, but keep them **out of the critical path** unless they are actively blocking refactors.

---

# Completion Criteria
- All targeted hotspot files are ≤ ~500 LOC (or as close as practical without contorting the design).
- No changes in terminal behavior (verified by tests + manual quick sanity runs).
- New file layout is predictable and consistent across Core and Display.
