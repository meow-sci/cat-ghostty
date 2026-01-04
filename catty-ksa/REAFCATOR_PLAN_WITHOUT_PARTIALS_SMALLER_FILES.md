# Refactor Plan (No `partial`, **More Granular Types**) — catty-ksa

This plan is the “smaller files” variant of [REFACTOR_PLAN_WITHOUT_PARTIALS.md](REFACTOR_PLAN_WITHOUT_PARTIALS.md).

## Goals / Constraints
- Target **very small files** (often **150–350 LOC**; hard cap still **~≤ 500 LOC**).
- **No `partial` classes**.
- **No business logic changes**.
  - Moves/extractions must preserve order of operations, conditionals, and side-effects.
  - Any signature changes must be strictly mechanical (e.g., turning a method into a helper method that takes explicit parameters instead of reading private fields).
- **Execution flow must remain obvious**:
  - façade method → one delegating call → feature implementation.

## Style rules for the granular approach
1. **Prefer “operation-group” classes over “god feature classes”**
   - Example: `TerminalCursorMovementOps` and `TerminalCursorSaveRestoreOps` are separate.
2. **Name classes after the thing you’d search for**
   - e.g., “Erase”, “ScrollRegion”, “DecMode”, “OscClipboard”.
3. **Avoid cross-calling between feature classes**
   - Feature classes should not call each other directly.
   - If necessary, calls go through the façade (`TerminalEmulator`) or a shared context.
4. **Use small context objects sparingly**
   - If a feature needs many shared dependencies, create:
     - `TerminalEmulatorContext`
     - `ProcessManagerContext`
     - `ParserEngineContext`
   - Context should hold *existing* objects, not new behavior.

## Validation (after each task)
- `dotnet build catty-ksa/caTTY.Core/caTTY.Core.csproj`
- `dotnet test catty-ksa/caTTY.Core.Tests/caTTY.Core.Tests.csproj`
- Display touched: `dotnet test catty-ksa/caTTY.Display.Tests/caTTY.Display.Tests.csproj`

---

# Proposed Granular Layout

## caTTY.Core/Terminal — TerminalEmulator split
Keep `TerminalEmulator.cs` as façade; extract small ops classes into `Terminal/EmulatorOps/`.

### Façade
- `caTTY.Core/Terminal/TerminalEmulator.cs`
  - ctor wiring
  - public entrypoints: `Write(...)`, `Resize(...)`, `Dispose()`, scroll viewport APIs
  - owns instances of ops classes (private readonly fields)

### Ops folder (granular)
Create folder: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/`

Suggested files/classes (each intentionally small):

**Input / pipeline**
- `TerminalInputOps.cs` — `Write(ReadOnlySpan<byte>)`, `Write(string)`, `FlushIncompleteSequences()`

**Viewport / scrollback**
- `TerminalViewportOps.cs` — `ScrollViewportUp/Down/ToTop/ToBottom`, `IsAutoScrollEnabled`, `ViewportOffset`

**Resize**
- `TerminalResizeOps.cs` — `Resize(...)` (and only resize-related helpers)

**C0 controls / simple controls**
- `TerminalBellOps.cs` — bell event
- `TerminalBackspaceOps.cs`
- `TerminalTabOps.cs` — tab movement + tab stops initialization hooks if any
- `TerminalCarriageReturnOps.cs`
- `TerminalLineFeedOps.cs`
- `TerminalIndexOps.cs` — index / reverse index
- `TerminalShiftOps.cs` — SI/SO

**Cursor**
- `TerminalCursorMovementOps.cs` — `MoveCursorUp/Down/Forward/Backward`, `SetCursorPosition`, `SetCursorColumn`
- `TerminalCursorSaveRestoreOps.cs` — DEC + ANSI save/restore
- `TerminalCursorStyleOps.cs` — cursor style set/get

**Erase/Clear**
- `TerminalEraseInDisplayOps.cs` — `ClearDisplay`
- `TerminalEraseInLineOps.cs` — `ClearLine`
- `TerminalSelectiveEraseInDisplayOps.cs` — `ClearDisplaySelective`
- `TerminalSelectiveEraseInLineOps.cs` — `ClearLineSelective`

**Scrolling / regions**
- `TerminalScrollOps.cs` — `ScrollScreenUp/Down`
- `TerminalScrollRegionOps.cs` — `SetScrollRegion`

**Insert/Delete**
- `TerminalInsertLinesOps.cs` — `InsertLinesInRegion`
- `TerminalDeleteLinesOps.cs` — `DeleteLinesInRegion`
- `TerminalInsertCharsOps.cs` — `InsertCharactersInLine`
- `TerminalDeleteCharsOps.cs` — `DeleteCharactersInLine`
- `TerminalEraseCharsOps.cs` — `EraseCharactersInLine`
- `TerminalInsertModeOps.cs` — `SetInsertMode` + `ShiftCharactersRight`

**Modes**
- `TerminalDecModeOps.cs` — `SetDecMode` switch (minus alt-screen if you want it separate)
- `TerminalAlternateScreenOps.cs` — `HandleAlternateScreenMode`
- `TerminalPrivateModesOps.cs` — `SavePrivateModes`, `RestorePrivateModes`
- `TerminalBracketedPasteOps.cs` — `WrapPasteContent(...)`, `IsBracketedPasteModeEnabled`

**OSC-related**
- `TerminalOscTitleIconOps.cs` — set/get title/icon + stacks
- `TerminalOscWindowManipulationOps.cs` — CSI Ps t window manipulation handler
- `TerminalOscClipboardOps.cs` — OSC 52
- `TerminalOscHyperlinkOps.cs` — OSC 8
- `TerminalOscColorQueryOps.cs` — OSC 10/11 queries + palette helpers

**Charsets**
- `TerminalCharsetDesignationOps.cs` — `DesignateCharacterSet`
- `TerminalCharsetTranslationOps.cs` — `TranslateCharacter`, `GenerateCharacterSetQueryResponse`

**Events**
- `TerminalResponseOps.cs` — `EmitResponse`, `OnResponseEmitted(...)`
- `TerminalScreenUpdateOps.cs` — `OnScreenUpdated`
- `TerminalTitleIconEventsOps.cs` — title/icon events
- `TerminalClipboardEventsOps.cs` — clipboard event

**Guards**
- `TerminalDisposedGuard.cs` — `ThrowIfDisposed`

> Yes this is a lot of files; the benefit is searchability and small-context refactor tasks.

---

## caTTY.Core/Terminal — TerminalParserHandlers split
Keep `TerminalParserHandlers.cs` as the `IParserHandlers` implementation façade and move logic into ultra-specific handlers.

Create folder: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/`

### High-level handler set
- `EscHandler.cs` — `HandleEsc` switch only
- `DcsHandler.cs` — DCS + DECRQSS
- `OscHandler.cs` — OSC + Xterm OSC mapping
- `SgrHandler.cs` — apply SGR + tracing
- `CsiDispatcher.cs` — top-level CSI dispatch `switch (message.Type)` OR delegate to grouped handlers

### CSI grouping (more granular)
Either:
- Keep one `switch` in `CsiDispatcher` but move the *case bodies* into helpers, or
- Split by message family into separate handler classes:

Suggested classes:
- `CsiCursorHandler.cs`
- `CsiEraseHandler.cs`
- `CsiScrollHandler.cs`
- `CsiInsertDeleteHandler.cs`
- `CsiDecModeHandler.cs`
- `CsiDeviceQueryHandler.cs`
- `CsiWindowManipulationHandler.cs`

Rule: **do not replace switch with dictionary yet**. Keep the case-to-action mapping identical.

---

## caTTY.Core/Terminal — ProcessManager split
Create folder: `catty-ksa/caTTY.Core/Terminal/Process/`

Granular types:
- `ConPtyNative.cs` — all interop declarations/structs/constants
- `PipePair.cs` — tiny struct/class to hold read/write handles
- `PipeFactory.cs` — creates pipes (thin wrapper around native)
- `StartupInfoBuilder.cs` — creates `STARTUPINFOEX` + attribute list
- `AttributeListBuilder.cs` — alloc/init/update/delete attribute list (isolated unsafe-ish logic)
- `ShellCommandResolver.cs` — resolve shell path/args
- `ProcessLauncher.cs` — wraps `CreateProcessW` call + error handling
- `ConPtySession.cs` — holds `IntPtr _pseudoConsole` and resizes/closes it
- `ConPtyInputWriter.cs` — `Write(...)` methods
- `ConPtyOutputPump.cs` — `ReadOutputAsync`
- `ProcessCleanup.cs` — cleanup sequence helpers

`ProcessManager.cs` remains a façade orchestrating these.

---

## caTTY.Core/Terminal — SessionManager split
Create folder: `catty-ksa/caTTY.Core/Terminal/Sessions/`

Granular types:
- `SessionRegistry.cs` — owns `_sessions`, `_sessionOrder`, active id; exposes safe snapshots
- `SessionDimensionTracker.cs` — last-known size + default options sync
- `SessionCreator.cs` — logic from `CreateSessionAsync` (except event raising)
- `SessionCloser.cs` — logic from `CloseSessionAsync`
- `SessionRestarter.cs` — logic from `RestartSessionAsync`
- `SessionSwitcher.cs` — switching logic
- `SessionEventBridge.cs` — `OnSessionStateChanged`, `OnSessionTitleChanged`, `OnSessionProcessExited`
- `SessionLogging.cs` — quiet logging helpers
- `TerminalSessionFactory.cs` — creates `TerminalEmulator`, `ProcessManager`, and RPC wiring

`SessionManager.cs` becomes an orchestrator delegating to these.

---

## caTTY.Core/Parsing — Parser split
Avoid partials by moving state machine into an engine and splitting by state into small classes.

Create folder: `catty-ksa/caTTY.Core/Parsing/Engine/`

Files:
- `ParserEngine.cs` — owns state enum and delegates to handlers
- `ParserEngineContext.cs` — buffers/state/dependencies
- `NormalStateHandler.cs`
- `EscapeStateHandler.cs`
- `CsiStateHandler.cs`
- `OscStateHandler.cs`
- `DcsStateHandler.cs`
- `ControlStringStateHandler.cs`
- `RpcSequenceHandler.cs`
- `Utf8DecoderAdapter.cs` (optional wrapper to keep decoding calls consistent)

`Parser.cs` remains public façade.

---

## caTTY.Core/Parsing — SgrParser and CsiParser
More granular split:

- `SgrParser.cs` (façade)
- `Sgr/`
  - `SgrParamTokenizer.cs` (parses params + separators)
  - `SgrMessageParser.cs` (produces `SgrMessage[]`)
  - `SgrAttributeApplier.cs` (apply messages)
  - `SgrColorParsers.cs` (rgb/indexed/named helpers)
  - `SgrInternalTypes.cs` (`SgrParseContext`, `SgrParseResult`)

- `CsiParser.cs` (façade)
- `Csi/`
  - `CsiTokenizer.cs`
  - `CsiMessageFactory.cs`
  - `CsiParamParsers.cs`

---

## caTTY.Display — TerminalController split
Split types + introduce granular UI subsystems.

- `Controllers/LayoutConstants.cs`
- `Controllers/TerminalSettings.cs`
- `Controllers/TerminalController.cs` (façade)
- `Controllers/TerminalUi/`:
  - `TerminalUiFonts.cs`
  - `TerminalUiSelection.cs`
  - `TerminalUiMouseTracking.cs`
  - `TerminalUiResize.cs`
  - `TerminalUiInput.cs`
  - `TerminalUiRender.cs`
  - `TerminalUiTabs.cs`
  - `TerminalUiSettingsPanel.cs` (if applicable)

---

# Incremental Task List (Granular, No partials)

## Phase 0 — Baseline
### Task 0.1 — Confirm tests/builds
- Run validation commands.
- Note failures without fixing.

---

## Phase 1 — ProcessManager (granular extraction)
### Task 1.1 — Add `ConPtyNative.cs`
- Create folder: `catty-ksa/caTTY.Core/Terminal/Process/`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyNative.cs`
- Move these members from `caTTY.Core/Terminal/ProcessManager.cs` into `ConPtyNative` (keep signatures identical; only qualify with `internal static` as needed):
  - Constants:
    - `EXTENDED_STARTUPINFO_PRESENT`
    - `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
  - P/Invoke methods:
    - `CreatePseudoConsole`
    - `ResizePseudoConsole`
    - `ClosePseudoConsole`
    - `CreatePipe`
    - `CloseHandle`
    - `InitializeProcThreadAttributeList`
    - `UpdateProcThreadAttribute`
    - `DeleteProcThreadAttributeList`
    - `CreateProcessW`
    - `ReadFile`
    - `WriteFile`
  - Structs:
    - `COORD`
    - `STARTUPINFOEX`
    - `STARTUPINFO`
    - `PROCESS_INFORMATION`
- Update `ProcessManager` to reference `ConPtyNative.*` instead of private members.
- Build + core tests.

### Task 1.2 — Add `ShellCommandResolver.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ShellCommandResolver.cs`
- Move these methods from `ProcessManager` into `ShellCommandResolver` (keep logic identical):
  - `ResolveShellCommand`
  - `ResolveAutoShell`
  - `ResolveWsl`
  - `ResolvePowerShell`
  - `ResolvePowerShellCore`
  - `ResolveCmd`
  - `ResolveCustomShell`
  - `FindExecutableInPath`
- Update `ProcessManager.StartAsync(...)` to call `ShellCommandResolver.ResolveShellCommand(options)`.
- Build + core tests.

### Task 1.3 — Add `AttributeListBuilder.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/AttributeListBuilder.cs`
- Extract the attribute-list block inside `StartAsync(...)` into **one** helper that preserves ordering and error handling:
  - New API (example shape; keep it simple):
    - `internal static IntPtr CreateAttributeListWithPseudoConsole(IntPtr pseudoConsole)`
    - `internal static void FreeAttributeList(IntPtr attributeList)`
- Move the following logic (verbatim, just relocated) into `AttributeListBuilder`:
  - Probe-size call to `InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref size)`
  - `Marshal.AllocHGlobal(size)`
  - `InitializeProcThreadAttributeList(attributeList, 1, 0, ref size)` + failure path
  - `UpdateProcThreadAttribute(... PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE ...)` + failure path
  - `DeleteProcThreadAttributeList` + `Marshal.FreeHGlobal`
- Update `ProcessManager.StartAsync(...)` to:
  - call `AttributeListBuilder.CreateAttributeListWithPseudoConsole(_pseudoConsole)`
  - assign `startupInfo.lpAttributeList`
  - ensure the cleanup paths call `AttributeListBuilder.FreeAttributeList(...)` in the same places the old code did.
- Build + core tests.

### Task 1.4 — Add `StartupInfoBuilder.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/StartupInfoBuilder.cs`
- Extract the creation of `STARTUPINFOEX` into a single helper:
  - Move this logic out of `StartAsync(...)`:
    - `var startupInfo = new STARTUPINFOEX();`
    - `startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();`
  - New API (example): `internal static ConPtyNative.STARTUPINFOEX Create()`
- Update `ProcessManager.StartAsync(...)` to call `StartupInfoBuilder.Create()`.
- Build + core tests.

### Task 1.5 — Add `ConPtyOutputPump.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyOutputPump.cs`
- Move the body of `ReadOutputAsync(CancellationToken)` into `ConPtyOutputPump.ReadOutputAsync(...)`.
  - Keep the loop, `ReadFile` call, error handling, and `Task.Delay(1, ...)` identical.
  - Pass dependencies explicitly (example): output handle, `Func<int?> getProcessId`, and callbacks `onDataReceived`, `onProcessError`.
- Keep `ProcessManager.ReadOutputAsync` as a thin delegator calling the new pump.
- Build + core tests.

### Task 1.6 — Add `ConPtyInputWriter.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyInputWriter.cs`
- Move `ProcessManager.Write(ReadOnlySpan<byte>)` and `ProcessManager.Write(string)` into `ConPtyInputWriter`.
  - Pass `_inputWriteHandle` and disposal/locking dependencies explicitly.
- Keep the `IProcessManager` surface on `ProcessManager` unchanged; `ProcessManager.Write(...)` delegates.
- Build + core tests.

### Task 1.7 — Add `ProcessCleanup.cs`
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ProcessCleanup.cs`
- Move these methods from `ProcessManager` into `ProcessCleanup`:
  - `CleanupProcess`
  - `CleanupPseudoConsole`
  - `CleanupHandles`
- Keep `ProcessManager` as the owner of state; `ProcessCleanup` should operate on explicit parameters (handles, process, cancellation token source) and return updated values if needed.
- Build + core tests.

### Task 1.8 — Move process event raisers
- Add file: `catty-ksa/caTTY.Core/Terminal/Process/ProcessEvents.cs`
- Move these methods from `ProcessManager`:
  - `OnProcessExited`
  - `OnDataReceived`
  - `OnProcessError`
- Keep event signatures unchanged. If needed, expose them as internal instance methods on `ProcessEvents` that take the `ProcessManager` instance (or callbacks) to raise events.
- Build + core tests.

### Task 1.9 — Keep disposal guard tiny
- Keep `ThrowIfDisposed` in `ProcessManager.cs` (small and close to public surface).
- If `ProcessManager.cs` is still too large, move only `ThrowIfDisposed` into `ProcessDisposedGuard.cs`.
- Build + core tests.

---

## Phase 2 — SessionManager (granular extraction)
### Task 2.1 — Add `SessionDimensionTracker`
- Create folder: `catty-ksa/caTTY.Core/Terminal/Sessions/`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionDimensionTracker.cs`
- Move these members out of `caTTY.Core/Terminal/SessionManager.cs`:
  - `UpdateLastKnownTerminalDimensions(int cols, int rows)`
  - `GetDefaultLaunchOptionsSnapshot()`
  - `UpdateDefaultLaunchOptions(ProcessLaunchOptions launchOptions)`
  - `CloneLaunchOptions(ProcessLaunchOptions options)`
- Keep `SessionManager` public surface unchanged; delegate to `SessionDimensionTracker`.
- Build + core tests.

### Task 2.2 — Add `TerminalSessionFactory`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/TerminalSessionFactory.cs`
- Move the “create a new `TerminalSession` and wire events” portion from `CreateSessionAsync(...)` into the factory.
  - Do not change lock semantics: acquire the same locks in `SessionManager` before calling the factory (or pass in already-validated data).
  - Keep event subscription order identical.
- Build + core tests.

### Task 2.3 — Add `SessionCreator`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionCreator.cs`
- Move the body of `CreateSessionAsync(...)` into `SessionCreator.CreateSessionAsync(...)`.
  - Keep the `SessionManager` method as a delegator that:
    - validates state and disposal
    - acquires the same locks
    - calls into `SessionCreator`
- Build + core tests.

### Task 2.4 — Add `SessionSwitcher`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionSwitcher.cs`
- Move these methods from `SessionManager`:
  - `SwitchToSession(Guid sessionId)`
  - `SwitchToNextSession()`
  - `SwitchToPreviousSession()`
- Ensure active-session bookkeeping and event raising is preserved.
- Build + core tests.

### Task 2.5 — Add `SessionCloser`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionCloser.cs`
- Move `CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)`.
- Build + core tests.

### Task 2.6 — Add `SessionRestarter`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionRestarter.cs`
- Move `RestartSessionAsync(Guid sessionId, ProcessLaunchOptions? launchOptions = null, CancellationToken cancellationToken = default)`.
- Build + core tests.

### Task 2.7 — Add `SessionEventBridge`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionEventBridge.cs`
- Move these event handlers from `SessionManager`:
  - `OnSessionStateChanged`
  - `OnSessionTitleChanged`
  - `OnSessionProcessExited`
- Keep subscription/unsubscription in the same places as today; only relocate handler bodies.
- Build + core tests.

### Task 2.8 — Add `SessionLogging`
- Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionLogging.cs`
- Move these methods:
  - `LogSessionLifecycleEvent`
  - `IsDebugLoggingEnabled`
- Keep formatting identical.
- Build + core tests.

### Task 2.9 — “Other” session methods
- Keep these in `SessionManager.cs` unless the file is still too large:
  - `ApplyFontConfigToAllSessions(object fontConfig)`
  - `TriggerTerminalResizeForAllSessions(float newCharacterWidth, float newLineHeight, (float width, float height) windowSize)`
  - `GenerateSessionTitle()`
  - `ThrowIfDisposed()` and `Dispose()`
- If needed, extract them into `SessionBulkOps.cs` and `SessionDisposedGuard.cs`.
- Build + core tests.

---

## Phase 3 — TerminalParserHandlers (granular extraction)
### Task 3.1 — Add `SgrHandler`
- Create folder: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs`
- Move these methods from `caTTY.Core/Terminal/TerminalParserHandlers.cs`:
  - `HandleSgr(SgrSequence sequence)` (body delegates to `SgrHandler`)
  - `HandleSgrSequence(SgrSequence sequence)`
  - `TraceSgrSequence(...)`
  - `FormatColor(Color? color)`
  - `ExtractSgrParameters(string rawSequence)`
- Keep the SGR apply order and tracing content identical.

Build + core tests.

### Task 3.2 — Add `DcsHandler`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/DcsHandler.cs`
- Move these methods:
  - `HandleDcs(DcsMessage message)` (delegate)
  - `HandleDecrqss(DcsMessage message)`
  - `ExtractDecrqssPayload(string raw)`
  - `GenerateSgrStateResponse()`

Build + core tests.

### Task 3.3 — Add `OscHandler`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/OscHandler.cs`
- Move these methods:
  - `HandleOsc(OscMessage message)`
  - `HandleXtermOsc(XtermOscMessage message)`

Build + core tests.

### Task 3.4 — Add CSI family handlers
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDispatcher.cs`
- Keep `TerminalParserHandlers.HandleCsi(CsiMessage message)` as a delegator to `CsiDispatcher.HandleCsi(...)`.
- Split the CSI switch-case bodies into family handlers (one file per family):
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiCursorHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiEraseHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiScrollHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiInsertDeleteHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDecModeHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDeviceQueryHandler.cs`
  - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiWindowManipulationHandler.cs`
- Rule: keep the top-level mapping identical (same cases call the same terminal ops in the same order).

Build + core tests after each handler extraction.

### Task 3.5 — Split C0/ESC façade handlers
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/C0Handler.cs`
- Move these methods:
  - `HandleBell`
  - `HandleBackspace`
  - `HandleTab`
  - `HandleLineFeed`
  - `HandleFormFeed`
  - `HandleCarriageReturn`
  - `HandleShiftIn`
  - `HandleShiftOut`
  - `HandleNormalByte(int codePoint)`
- Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/EscHandler.cs`
- Move `HandleEsc(EscMessage message)`.

Build + core tests.

Build + core tests after each task.

---

## Phase 4 — TerminalEmulator (granular ops extraction)
Approach: extract the smallest, least-coupled areas first to reduce risk.

### Task 4.1 — Add `TerminalViewportOps`
- Create folder: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalViewportOps.cs`
- Move these façade methods from `TerminalEmulator` into `TerminalViewportOps` (keep signatures):
  - `ScrollViewportUp`
  - `ScrollViewportDown`
  - `ScrollViewportToTop`
  - `ScrollViewportToBottom`
- Keep `TerminalEmulator` methods as 1-line delegators: `_viewportOps.ScrollViewportUp(...);` etc.

Build + core tests.

### Task 4.2 — Add `TerminalResizeOps`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalResizeOps.cs`
- Move:
  - `Resize(int cols, int rows)`
- Keep façade delegate.

Build + core tests.

### Task 4.3 — Add cursor ops (3 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCursorMovementOps.cs`
  - Move:
    - `MoveCursorUp`
    - `MoveCursorDown`
    - `MoveCursorForward`
    - `MoveCursorBackward`
    - `SetCursorPosition`
    - `SetCursorColumn`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCursorSaveRestoreOps.cs`
  - Move:
    - `SaveCursorPosition`
    - `RestoreCursorPosition`
    - `SaveCursorPositionAnsi`
    - `RestoreCursorPositionAnsi`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCursorStyleOps.cs`
  - Move:
    - `SetCursorStyle(int style)`
    - `SetCursorStyle(CursorStyle style)`

Build + core tests after each file extraction.

### Task 4.4 — Add erase/clear ops (4 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalEraseInDisplayOps.cs`
  - Move `ClearDisplay`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalEraseInLineOps.cs`
  - Move `ClearLine`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInDisplayOps.cs`
  - Move `ClearDisplaySelective`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInLineOps.cs`
  - Move `ClearLineSelective`

Build + core tests after each file extraction.

### Task 4.5 — Add scroll/region ops (2 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalScrollOps.cs`
  - Move:
    - `ScrollScreenUp`
    - `ScrollScreenDown`
    - `HandleReverseIndex`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalScrollRegionOps.cs`
  - Move `SetScrollRegion`

Build + core tests after each file extraction.

### Task 4.6 — Add insert/delete ops (6 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalInsertLinesOps.cs`
  - Move `InsertLinesInRegion`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalDeleteLinesOps.cs`
  - Move `DeleteLinesInRegion`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalInsertCharsOps.cs`
  - Move `InsertCharactersInLine`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalDeleteCharsOps.cs`
  - Move `DeleteCharactersInLine`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalEraseCharsOps.cs`
  - Move `EraseCharactersInLine`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalInsertModeOps.cs`
  - Move:
    - `SetInsertMode`
    - `ShiftCharactersRight`

Build + core tests after each file extraction.

### Task 4.7 — Add modes/alt-screen ops (4 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalDecModeOps.cs`
  - Move `SetDecMode`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalAlternateScreenOps.cs`
  - Move `HandleAlternateScreenMode`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalPrivateModesOps.cs`
  - Move:
    - `SavePrivateModes`
    - `RestorePrivateModes`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalBracketedPasteOps.cs`
  - Move:
    - `WrapPasteContent(string pasteContent)`
    - `WrapPasteContent(ReadOnlySpan<char> pasteContent)`
    - `IsBracketedPasteModeEnabled()`

Build + core tests after each file extraction.

### Task 4.8 — Add OSC ops (5 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalOscTitleIconOps.cs`
  - Move:
    - `SetWindowTitle`
    - `SetIconName`
    - `SetTitleAndIcon`
    - `GetWindowTitle`
    - `GetIconName`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalOscWindowManipulationOps.cs`
  - Move `HandleWindowManipulation`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalOscClipboardOps.cs`
  - Move:
    - `HandleClipboard`
    - `OnClipboardRequest`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalOscHyperlinkOps.cs`
  - Move `HandleHyperlink`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalOscColorQueryOps.cs`
  - Move:
    - `GetCurrentForegroundColor`
    - `GetCurrentBackgroundColor`
    - `GetDefaultForegroundColor`
    - `GetDefaultBackgroundColor`
    - `GetNamedColorRgb`
    - `GetIndexedColorRgb`

Build + core tests after each file extraction.

### Task 4.9 — Add charset ops (2 files)
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCharsetDesignationOps.cs`
  - Move `DesignateCharacterSet`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCharsetTranslationOps.cs`
  - Move:
    - `HandleShiftIn`
    - `HandleShiftOut`
    - `TranslateCharacter`
    - `GenerateCharacterSetQueryResponse`

Build + core tests after each file extraction.

### Task 4.10 — Add C0 control ops
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalLineFeedOps.cs`
  - Move `HandleLineFeed`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalIndexOps.cs`
  - Move `HandleIndex`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCarriageReturnOps.cs`
  - Move `HandleCarriageReturn`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalBellOps.cs`
  - Move:
    - `HandleBell`
    - `OnBell`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalBackspaceOps.cs`
  - Move `HandleBackspace`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalTabOps.cs`
  - Move:
    - `HandleTab`
    - `SetTabStopAtCursor`
    - `CursorForwardTab`
    - `CursorBackwardTab`
    - `ClearTabStopAtCursor`
    - `ClearAllTabStops`

Build + core tests after each file extraction.

### Task 4.11 — Add response/screen/event ops
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalResponseOps.cs`
  - Move:
    - `EmitResponse`
    - `OnResponseEmitted(ResponseEmittedEventArgs e)`
    - `OnResponseEmitted(string response)`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalScreenUpdateOps.cs`
  - Move `OnScreenUpdated`
- Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalTitleIconEventsOps.cs`
  - Move:
    - `OnTitleChanged`
    - `OnIconNameChanged`

Build + core tests.

### Task 4.12 — Add misc façade helpers
- Keep `ThrowIfDisposed` in `TerminalEmulator.cs` unless you’re still above target size; if needed, move it to `TerminalDisposedGuard.cs`.
- Keep `Dispose()` in façade.
- Keep `Write(ReadOnlySpan<byte>)`, `Write(string)`, and `FlushIncompleteSequences()` in façade **unless** they’re primarily parser plumbing; if you move them:
  - Add `TerminalInputOps.cs` and move:
    - `Write(ReadOnlySpan<byte>)`
    - `Write(string)`
    - `FlushIncompleteSequences()`
    - `SetRpcEnabled(bool enabled)`
- Move reset helpers into `TerminalResetOps.cs`:
  - `ResetToInitialState`
  - `SoftReset`
- Build + core tests.

Build + core tests after each extraction.

---

## Phase 5 — Parser engine (granular state handlers)
### Task 5.1 — Introduce `ParserEngine` + `ParserEngineContext`
- Create folder: `catty-ksa/caTTY.Core/Parsing/Engine/`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/ParserEngineContext.cs`
  - Move/hold existing parsing buffers and state that are currently private fields in `Parser`.
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/ParserEngine.cs`
  - Move `ProcessByte(byte b)` into the engine.
  - Keep `Parser` public surface (`PushBytes`, `PushByte`, `FlushIncompleteSequences`) delegating to the engine.

Build + core tests.

### Task 5.2 — Extract one state handler at a time
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/NormalStateHandler.cs`
  - Move `HandleNormalState(byte b)`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/EscapeStateHandler.cs`
  - Move `HandleEscapeState(byte b)`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/CsiStateHandler.cs`
  - Move `HandleCsiState(byte b)`
  - Move `HandleCsiByte(byte b)`
  - Move `FinishCsiSequence()`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/OscStateHandler.cs`
  - Move:
    - `HandleOscState(byte b)`
    - `HandleOscEscapeState(byte b)`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/DcsStateHandler.cs`
  - Move:
    - `HandleDcsState(byte b)`
    - `HandleDcsEscapeState(byte b)`
    - `FinishDcsSequence(string terminator)`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/ControlStringStateHandler.cs`
  - Move:
    - `HandleControlStringState(byte b)`
    - `HandleControlStringEscapeState(byte b)`
- Add file: `catty-ksa/caTTY.Core/Parsing/Engine/RpcSequenceHandler.cs`
  - Move:
    - `IsRpcHandlingEnabled()`
    - `TryHandleRpcSequence()`
- Keep these helpers close to the engine (either in `ParserEngine` or `ParserEngineContext`):
  - `HandleEscapeByte(byte b)`
  - `HandleNormalByte(byte b)`
  - `ResetEscapeState()`
  - `HandleC0ExceptEscape(byte b)`
  - `StartEscapeSequence(byte b)`
  - `MaybeEmitNormalByteDuringEscapeSequence(byte b)`
  - `BytesToString(IEnumerable<byte> bytes)`

Build + core tests after each file extraction.

---

## Phase 5b — SgrParser and CsiParser (granular split)

### Task 5b.1 — SgrParser façade + tokenizer
- Create folder: `catty-ksa/caTTY.Core/Parsing/Sgr/`
- Extract parameter tokenization into `Sgr/SgrParamTokenizer.cs`.
- Keep `SgrParser.cs` as façade that calls the tokenizer and existing apply logic.

### Task 5b.2 — Sgr message parsing and apply
- Extract message construction into `Sgr/SgrMessageParser.cs`.
- Extract apply logic into `Sgr/SgrAttributeApplier.cs`.
- Extract color helpers into `Sgr/SgrColorParsers.cs`.

Build + core tests after each task.

### Task 5b.3 — CsiParser façade + tokenizer
- Create folder: `catty-ksa/caTTY.Core/Parsing/Csi/`
- Extract byte/param tokenization into `Csi/CsiTokenizer.cs`.
- Extract message building into `Csi/CsiMessageFactory.cs`.
- Extract param parsing helpers into `Csi/CsiParamParsers.cs`.

Build + core tests after each task.

Build + core tests after each.

---

## Phase 6 — Display TerminalController (granular UI subsystems)
### Task 6.1 — Split out `LayoutConstants` and `TerminalSettings`

- Add file: `catty-ksa/caTTY.Display/Controllers/LayoutConstants.cs`
  - Move `LayoutConstants` (and any related small constants/types).
- Add file: `catty-ksa/caTTY.Display/Controllers/TerminalSettings.cs`
  - Move `TerminalSettings` including:
    - `Validate()`
    - `Clone()`
- Keep `TerminalController.cs` as façade wiring these.

Build + display tests.

### Task 6.2+ — Extract one UI subsystem per task
- Fonts, input, mouse, selection, resize, render, tabs.

For each subsystem:
- Create file in `catty-ksa/caTTY.Display/Controllers/TerminalUi/`.
- Move the listed methods from `TerminalController` into that file.
- Keep `TerminalController` method as a 1-line delegator (or move the method entirely if it’s private).
- Build + display tests.

Suggested task breakdown (explicit methods from `TerminalController.cs` inventory):

### Task 6.2 — Fonts subsystem (`TerminalUiFonts.cs`)
- Move:
  - `LoadFontSettingsInConstructor`
  - `InitializeCurrentFontFamily`
  - `EnsureFontsLoaded`
  - `LoadFonts`
  - `FindFont`
  - `CalculateCharacterMetrics`
  - `SelectFont`
  - `SetFontSize`
  - `ResetFontSize`
  - `IncreaseFontSize`
  - `DecreaseFontSize`
  - `LoadFontSettings`
  - `SaveFontSettings`
  - `UpdateFontConfig`

### Task 6.3 — Render subsystem (`TerminalUiRender.cs`)
- Move:
  - `Render`
  - `RenderTerminalContent`
  - `RenderCell`
  - `RenderCursor`
  - `RenderUnderline`
  - `RenderStrikethrough`
  - `RenderCurlyUnderline`
  - `RenderDottedUnderline`
  - `RenderDashedUnderline`
  - `PushUIFont`
  - `PushTerminalContentFont`
  - `PushMonospaceFont`
  - `MaybePopFont`

### Task 6.4 — Input subsystem (`TerminalUiInput.cs`)
- Move:
  - `HandleInput`
  - `HandleSpecialKeys`
  - `ShouldCaptureInput`
  - `ManageInputCapture`
  - `ForceFocus`

### Task 6.5 — Mouse + selection subsystems (`TerminalUiMouseTracking.cs`, `TerminalUiSelection.cs`)
- Move mouse tracking:
  - `HandleMouseTrackingForApplications`
  - `HandleMouseInputIntegrated`
  - `HandleMouseInput`
  - `HandleMouseWheelInput`
  - `ProcessMouseWheelScroll`
  - `UpdateCoordinateConverterMetrics`
  - `SyncMouseTrackingConfiguration`
  - `GetMouseCellCoordinates1Based`
  - `GetMouseCellCoordinates`
  - `IsMouseOverTerminal`
  - `EncodeAltScreenWheelAsKeys`
- Move selection:
  - `SelectAllVisibleContent`
  - `HandleSelectionMouseDown`
  - `HandleSelectionMouseMove`
  - `HandleSelectionMouseUp`
  - `ClearSelection`
  - `CopySelectionToClipboard`
  - `GetCurrentSelection`
  - `SetSelection`

### Task 6.6 — Resize subsystem (`TerminalUiResize.cs`)
- Move:
  - `HandleWindowResize`
  - `CalculateTerminalDimensions`
  - `TriggerTerminalResize`
  - `ProcessPendingFontResize`
  - `TriggerTerminalResizeForAllSessions`
  - `ResizeTerminal`
  - `ApplyTerminalDimensionsToAllSessions`
  - `GetTerminalDimensions`
  - `GetCurrentWindowSize`

### Task 6.7 — Tabs + menu subsystem (`TerminalUiTabs.cs`, `TerminalUiSettingsPanel.cs`)
- Move tab UI:
  - `RenderTabArea`
  - `RenderTerminalCanvas`
  - `CalculateTabAreaHeight`
- Move menu/settings rendering:
  - `RenderMenuBar`
  - `RenderFileMenu`
  - `RenderEditMenu`
  - `RenderSessionsMenu`
  - `RenderFontMenu`
  - `SelectFontFamily`
  - `RenderThemeMenu`
  - `ApplySelectedTheme`
  - `RefreshThemes`
  - `RenderSettingsMenu`
  - `RenderShellConfigurationSection`
  - `ApplyShellConfiguration`
  - `ApplyShellConfigurationToSessionManager`
  - `CalculateSettingsAreaHeight`
  - `CalculateHeaderHeight`
  - `CalculateMinHeaderHeight`
  - `CalculateMaxHeaderHeight`
  - `CalculateTerminalCanvasSize`
  - `ValidateWindowSize`
  - `CalculateTerminalCanvasPosition`
  - `CalculateOptimalTerminalDimensions`
  - `ShowNotImplementedMessage`

### Task 6.8 — Event wiring subsystem (`TerminalUiEvents.cs`)
- Move:
  - `OnScreenUpdated`
  - `OnResponseEmitted`
  - `OnThemeChanged`
  - `OnMouseEventGenerated`
  - `OnLocalMouseEvent`
  - `OnMouseProcessingError`
  - `OnMouseInputError`
  - `OnTerminalReset`
  - `OnSessionCreated`
  - `OnSessionClosed`
  - `OnActiveSessionChanged`
  - `OnSessionTitleChanged`

Build + display tests after each task.

Build + display tests after each.

---

# Completion Criteria
- Major hotspot files are reduced to small façade files.
- Feature code is discoverable via folder names and class names.
- Build/tests remain green throughout.
