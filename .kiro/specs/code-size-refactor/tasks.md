# Implementation Plan: Code Size Refactor

## Overview

This implementation plan follows the specific, concrete steps from the original refactor plan document. Each task contains exact instructions for what to move, where to move it, and how to preserve functionality.

## Validation Commands (after each task)
- `dotnet build catty-ksa/caTTY.Core/caTTY.Core.csproj`
- `dotnet test catty-ksa/caTTY.Core.Tests/caTTY.Core.Tests.csproj`
- Display touched: `dotnet test catty-ksa/caTTY.Display.Tests/caTTY.Display.Tests.csproj`

## Tasks

- [ ] **Phase 0 — Baseline**

  - [ ] 0.1 Confirm tests/builds
    - Run validation commands above
    - Note failures without fixing them
    - Document current state for comparison

- [ ] **Phase 1 — ProcessManager (granular extraction)**

  - [ ] 1.1 Add `ConPtyNative.cs`
    - Create folder: `catty-ksa/caTTY.Core/Terminal/Process/`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyNative.cs`
    - Move these members from `caTTY.Core/Terminal/ProcessManager.cs` into `ConPtyNative` (keep signatures identical; only qualify with `internal static` as needed):
      - **Constants:**
        - `EXTENDED_STARTUPINFO_PRESENT`
        - `PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE`
      - **P/Invoke methods:**
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
      - **Structs:**
        - `COORD`
        - `STARTUPINFOEX`
        - `STARTUPINFO`
        - `PROCESS_INFORMATION`
    - Update `ProcessManager` to reference `ConPtyNative.*` instead of private members
    - Build + core tests

  - [ ] 1.2 Add `ShellCommandResolver.cs`
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
    - Update `ProcessManager.StartAsync(...)` to call `ShellCommandResolver.ResolveShellCommand(options)`
    - Build + core tests

  - [ ] 1.3 Add `AttributeListBuilder.cs`
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
      - ensure the cleanup paths call `AttributeListBuilder.FreeAttributeList(...)` in the same places the old code did
    - Build + core tests

  - [ ] 1.4 Add `StartupInfoBuilder.cs`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/StartupInfoBuilder.cs`
    - Extract the creation of `STARTUPINFOEX` into a single helper:
      - Move this logic out of `StartAsync(...)`:
        - `var startupInfo = new STARTUPINFOEX();`
        - `startupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();`
      - New API (example): `internal static ConPtyNative.STARTUPINFOEX Create()`
    - Update `ProcessManager.StartAsync(...)` to call `StartupInfoBuilder.Create()`
    - Build + core tests

  - [ ] 1.5 Add `ConPtyOutputPump.cs`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyOutputPump.cs`
    - Move the body of `ReadOutputAsync(CancellationToken)` into `ConPtyOutputPump.ReadOutputAsync(...)`
      - Keep the loop, `ReadFile` call, error handling, and `Task.Delay(1, ...)` identical
      - Pass dependencies explicitly (example): output handle, `Func<int?> getProcessId`, and callbacks `onDataReceived`, `onProcessError`
    - Keep `ProcessManager.ReadOutputAsync` as a thin delegator calling the new pump
    - Build + core tests

  - [ ] 1.6 Add `ConPtyInputWriter.cs`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/ConPtyInputWriter.cs`
    - Move `ProcessManager.Write(ReadOnlySpan<byte>)` and `ProcessManager.Write(string)` into `ConPtyInputWriter`
      - Pass `_inputWriteHandle` and disposal/locking dependencies explicitly
    - Keep the `IProcessManager` surface on `ProcessManager` unchanged; `ProcessManager.Write(...)` delegates
    - Build + core tests

  - [ ] 1.7 Add `ProcessCleanup.cs`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/ProcessCleanup.cs`
    - Move these methods from `ProcessManager` into `ProcessCleanup`:
      - `CleanupProcess`
      - `CleanupPseudoConsole`
      - `CleanupHandles`
    - Keep `ProcessManager` as the owner of state; `ProcessCleanup` should operate on explicit parameters (handles, process, cancellation token source) and return updated values if needed
    - Build + core tests

  - [ ] 1.8 Move process event raisers
    - Add file: `catty-ksa/caTTY.Core/Terminal/Process/ProcessEvents.cs`
    - Move these methods from `ProcessManager`:
      - `OnProcessExited`
      - `OnDataReceived`
      - `OnProcessError`
    - Keep event signatures unchanged. If needed, expose them as internal instance methods on `ProcessEvents` that take the `ProcessManager` instance (or callbacks) to raise events
    - Build + core tests

  - [ ] 1.9 Keep disposal guard tiny
    - Keep `ThrowIfDisposed` in `ProcessManager.cs` (small and close to public surface)
    - If `ProcessManager.cs` is still too large, move only `ThrowIfDisposed` into `ProcessDisposedGuard.cs`
    - Build + core tests

- [ ] **Phase 2 — SessionManager (granular extraction)**

  - [ ] 2.1 Add `SessionDimensionTracker`
    - Create folder: `catty-ksa/caTTY.Core/Terminal/Sessions/`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionDimensionTracker.cs`
    - Move these members out of `caTTY.Core/Terminal/SessionManager.cs`:
      - `UpdateLastKnownTerminalDimensions(int cols, int rows)`
      - `GetDefaultLaunchOptionsSnapshot()`
      - `UpdateDefaultLaunchOptions(ProcessLaunchOptions launchOptions)`
      - `CloneLaunchOptions(ProcessLaunchOptions options)`
    - Keep `SessionManager` public surface unchanged; delegate to `SessionDimensionTracker`
    - Build + core tests

  - [ ] 2.2 Add `TerminalSessionFactory`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/TerminalSessionFactory.cs`
    - Move the "create a new `TerminalSession` and wire events" portion from `CreateSessionAsync(...)` into the factory
      - Do not change lock semantics: acquire the same locks in `SessionManager` before calling the factory (or pass in already-validated data)
      - Keep event subscription order identical
    - Build + core tests

  - [ ] 2.3 Add `SessionCreator`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionCreator.cs`
    - Move the body of `CreateSessionAsync(...)` into `SessionCreator.CreateSessionAsync(...)`
      - Keep the `SessionManager` method as a delegator that:
        - validates state and disposal
        - acquires the same locks
        - calls into `SessionCreator`
    - Build + core tests

  - [ ] 2.4 Add `SessionSwitcher`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionSwitcher.cs`
    - Move these methods from `SessionManager`:
      - `SwitchToSession(Guid sessionId)`
      - `SwitchToNextSession()`
      - `SwitchToPreviousSession()`
    - Ensure active-session bookkeeping and event raising is preserved
    - Build + core tests

  - [ ] 2.5 Add `SessionCloser`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionCloser.cs`
    - Move `CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)`
    - Build + core tests

  - [ ] 2.6 Add `SessionRestarter`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionRestarter.cs`
    - Move `RestartSessionAsync(Guid sessionId, ProcessLaunchOptions? launchOptions = null, CancellationToken cancellationToken = default)`
    - Build + core tests

  - [ ] 2.7 Add `SessionEventBridge`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionEventBridge.cs`
    - Move these event handlers from `SessionManager`:
      - `OnSessionStateChanged`
      - `OnSessionTitleChanged`
      - `OnSessionProcessExited`
    - Keep subscription/unsubscription in the same places as today; only relocate handler bodies
    - Build + core tests

  - [ ] 2.8 Add `SessionLogging`
    - Add file: `catty-ksa/caTTY.Core/Terminal/Sessions/SessionLogging.cs`
    - Move these methods:
      - `LogSessionLifecycleEvent`
      - `IsDebugLoggingEnabled`
    - Keep formatting identical
    - Build + core tests

  - [ ] 2.9 "Other" session methods
    - Keep these in `SessionManager.cs` unless the file is still too large:
      - `ApplyFontConfigToAllSessions(object fontConfig)`
      - `TriggerTerminalResizeForAllSessions(float newCharacterWidth, float newLineHeight, (float width, float height) windowSize)`
      - `GenerateSessionTitle()`
      - `ThrowIfDisposed()` and `Dispose()`
    - If needed, extract them into `SessionBulkOps.cs` and `SessionDisposedGuard.cs`
    - Build + core tests

- [ ] **Phase 3 — TerminalParserHandlers (granular extraction)**

  - [ ] 3.1 Add `SgrHandler`
    - Create folder: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/`
    - Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs`
    - Move these methods from `caTTY.Core/Terminal/TerminalParserHandlers.cs`:
      - `HandleSgr(SgrSequence sequence)` (body delegates to `SgrHandler`)
      - `HandleSgrSequence(SgrSequence sequence)`
      - `TraceSgrSequence(...)`
      - `FormatColor(Color? color)`
      - `ExtractSgrParameters(string rawSequence)`
    - Keep the SGR apply order and tracing content identical
    - Build + core tests

  - [ ] 3.2 Add `DcsHandler`
    - Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/DcsHandler.cs`
    - Move these methods:
      - `HandleDcs(DcsMessage message)` (delegate)
      - `HandleDecrqss(DcsMessage message)`
      - `ExtractDecrqssPayload(string raw)`
      - `GenerateSgrStateResponse()`
    - Build + core tests

  - [ ] 3.3 Add `OscHandler`
    - Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/OscHandler.cs`
    - Move these methods:
      - `HandleOsc(OscMessage message)`
      - `HandleXtermOsc(XtermOscMessage message)`
    - Build + core tests

  - [ ] 3.4 Add CSI family handlers
    - Add file: `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDispatcher.cs`
    - Keep `TerminalParserHandlers.HandleCsi(CsiMessage message)` as a delegator to `CsiDispatcher.HandleCsi(...)`
    - Split the CSI switch-case bodies into family handlers (one file per family):
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiCursorHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiEraseHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiScrollHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiInsertDeleteHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDecModeHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiDeviceQueryHandler.cs`
      - `catty-ksa/caTTY.Core/Terminal/ParserHandlers/CsiWindowManipulationHandler.cs`
    - Rule: keep the top-level mapping identical (same cases call the same terminal ops in the same order)
    - Build + core tests after each handler extraction

  - [ ] 3.5 Split C0/ESC façade handlers
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
    - Move `HandleEsc(EscMessage message)`
    - Build + core tests

- [ ] **Phase 4 — TerminalEmulator (granular ops extraction)**
  *Approach: extract the smallest, least-coupled areas first to reduce risk*

  - [ ] 4.1 Add `TerminalViewportOps`
    - Create folder: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalViewportOps.cs`
    - Move these façade methods from `TerminalEmulator` into `TerminalViewportOps` (keep signatures):
      - `ScrollViewportUp`
      - `ScrollViewportDown`
      - `ScrollViewportToTop`
      - `ScrollViewportToBottom`
    - Keep `TerminalEmulator` methods as 1-line delegators: `_viewportOps.ScrollViewportUp(...);` etc.
    - Build + core tests

  - [ ] 4.2 Add `TerminalResizeOps`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalResizeOps.cs`
    - Move:
      - `Resize(int cols, int rows)`
    - Keep façade delegate
    - Build + core tests

  - [ ] 4.3 Add cursor ops (3 files)
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
    - Build + core tests after each file extraction

  - [ ] 4.4 Add erase/clear ops (4 files)
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalEraseInDisplayOps.cs`
      - Move `ClearDisplay`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalEraseInLineOps.cs`
      - Move `ClearLine`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInDisplayOps.cs`
      - Move `ClearDisplaySelective`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInLineOps.cs`
      - Move `ClearLineSelective`
    - Build + core tests after each file extraction

  - [ ] 4.5 Add scroll/region ops (2 files)
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalScrollOps.cs`
      - Move:
        - `ScrollScreenUp`
        - `ScrollScreenDown`
        - `HandleReverseIndex`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalScrollRegionOps.cs`
      - Move `SetScrollRegion`
    - Build + core tests after each file extraction

  - [ ] 4.6 Add insert/delete ops (6 files)
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
    - Build + core tests after each file extraction

  - [ ] 4.7 Add modes/alt-screen ops (4 files)
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
    - Build + core tests after each file extraction

  - [ ] 4.8 Add OSC ops (5 files)
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
    - Build + core tests after each file extraction

  - [ ] 4.9 Add charset ops (2 files)
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCharsetDesignationOps.cs`
      - Move `DesignateCharacterSet`
    - Add file: `catty-ksa/caTTY.Core/Terminal/EmulatorOps/TerminalCharsetTranslationOps.cs`
      - Move:
        - `HandleShiftIn`
        - `HandleShiftOut`
        - `TranslateCharacter`
        - `GenerateCharacterSetQueryResponse`
    - Build + core tests after each file extraction

  - [ ] 4.10 Add C0 control ops
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
    - Build + core tests after each file extraction

  - [ ] 4.11 Add response/screen/event ops
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
    - Build + core tests

  - [ ] 4.12 Add misc façade helpers
    - Keep `ThrowIfDisposed` in `TerminalEmulator.cs` unless you're still above target size; if needed, move it to `TerminalDisposedGuard.cs`
    - Keep `Dispose()` in façade
    - Keep `Write(ReadOnlySpan<byte>)`, `Write(string)`, and `FlushIncompleteSequences()` in façade **unless** they're primarily parser plumbing; if you move them:
      - Add `TerminalInputOps.cs` and move:
        - `Write(ReadOnlySpan<byte>)`
        - `Write(string)`
        - `FlushIncompleteSequences()`
        - `SetRpcEnabled(bool enabled)`
    - Move reset helpers into `TerminalResetOps.cs`:
      - `ResetToInitialState`
      - `SoftReset`
    - Build + core tests

- [ ] **Phase 5 — Parser engine (granular state handlers)**

  - [ ] 5.1 Introduce `ParserEngine` + `ParserEngineContext`
    - Create folder: `catty-ksa/caTTY.Core/Parsing/Engine/`
    - Add file: `catty-ksa/caTTY.Core/Parsing/Engine/ParserEngineContext.cs`
      - Move/hold existing parsing buffers and state that are currently private fields in `Parser`
    - Add file: `catty-ksa/caTTY.Core/Parsing/Engine/ParserEngine.cs`
      - Move `ProcessByte(byte b)` into the engine
      - Keep `Parser` public surface (`PushBytes`, `PushByte`, `FlushIncompleteSequences`) delegating to the engine
    - Build + core tests

  - [ ] 5.2 Extract one state handler at a time
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
    - Build + core tests after each file extraction

- [ ] **Phase 5b — SgrParser and CsiParser (granular split)**

  - [ ] 5b.1 SgrParser façade + tokenizer
    - Create folder: `catty-ksa/caTTY.Core/Parsing/Sgr/`
    - Extract parameter tokenization into `Sgr/SgrParamTokenizer.cs`
    - Keep `SgrParser.cs` as façade that calls the tokenizer and existing apply logic

  - [ ] 5b.2 Sgr message parsing and apply
    - Extract message construction into `Sgr/SgrMessageParser.cs`
    - Extract apply logic into `Sgr/SgrAttributeApplier.cs`
    - Extract color helpers into `Sgr/SgrColorParsers.cs`
    - Build + core tests after each task

  - [ ] 5b.3 CsiParser façade + tokenizer
    - Create folder: `catty-ksa/caTTY.Core/Parsing/Csi/`
    - Extract byte/param tokenization into `Csi/CsiTokenizer.cs`
    - Extract message building into `Csi/CsiMessageFactory.cs`
    - Extract param parsing helpers into `Csi/CsiParamParsers.cs`
    - Build + core tests after each task

- [ ] **Phase 6 — Display TerminalController (granular UI subsystems)**

  - [ ] 6.1 Split out `LayoutConstants` and `TerminalSettings`
    - Add file: `catty-ksa/caTTY.Display/Controllers/LayoutConstants.cs`
      - Move `LayoutConstants` (and any related small constants/types)
    - Add file: `catty-ksa/caTTY.Display/Controllers/TerminalSettings.cs`
      - Move `TerminalSettings` including:
        - `Validate()`
        - `Clone()`
    - Keep `TerminalController.cs` as façade wiring these
    - Build + display tests

  - [ ] 6.2 Fonts subsystem (`TerminalUiFonts.cs`)
    - Create folder: `catty-ksa/caTTY.Display/Controllers/TerminalUi/`
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
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

  - [ ] 6.3 Render subsystem (`TerminalUiRender.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiRender.cs`
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

  - [ ] 6.4 Input subsystem (`TerminalUiInput.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
    - Move:
      - `HandleInput`
      - `HandleSpecialKeys`
      - `ShouldCaptureInput`
      - `ManageInputCapture`
      - `ForceFocus`

  - [ ] 6.5 Mouse + selection subsystems (`TerminalUiMouseTracking.cs`, `TerminalUiSelection.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiMouseTracking.cs`
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
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiSelection.cs`
    - Move selection:
      - `SelectAllVisibleContent`
      - `HandleSelectionMouseDown`
      - `HandleSelectionMouseMove`
      - `HandleSelectionMouseUp`
      - `ClearSelection`
      - `CopySelectionToClipboard`
      - `GetCurrentSelection`
      - `SetSelection`

  - [ ] 6.6 Resize subsystem (`TerminalUiResize.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
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

  - [ ] 6.7 Tabs + menu subsystem (`TerminalUiTabs.cs`, `TerminalUiSettingsPanel.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiTabs.cs`
    - Move tab UI:
      - `RenderTabArea`
      - `RenderTerminalCanvas`
      - `CalculateTabAreaHeight`
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
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

  - [ ] 6.8 Event wiring subsystem (`TerminalUiEvents.cs`)
    - Create file: `catty-ksa/caTTY.Display/Controllers/TerminalUi/TerminalUiEvents.cs`
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
    - Build + display tests after each task

## Completion Criteria
- Major hotspot files are reduced to small façade files
- Feature code is discoverable via folder names and class names
- Build/tests remain green throughout