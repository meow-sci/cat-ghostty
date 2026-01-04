---

# Phase 2: SessionManager Refactoring

**Target:** `caTTY.Core/Terminal/SessionManager.cs`
**Strategy:** Extract session lifecycle, switching, and dimension tracking

## Task 2.1: Extract Dimension Tracker

**Goal:** Isolate terminal dimension management

**Steps:**
1. Create folder: `caTTY.Core/Terminal/Sessions/`
2. Create file: `caTTY.Core/Terminal/Sessions/SessionDimensionTracker.cs`
3. Move these methods (preserve exact logic):
   - `UpdateLastKnownTerminalDimensions(int cols, int rows)`
   - `GetDefaultLaunchOptionsSnapshot()`
   - `UpdateDefaultLaunchOptions(ProcessLaunchOptions launchOptions)`
   - `CloneLaunchOptions(ProcessLaunchOptions options)`
4. Create instance in `SessionManager`, delegate calls

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.1: Extract session dimension tracker

- Created caTTY.Core/Terminal/Sessions/ folder
- Created SessionDimensionTracker.cs
- Extracted UpdateLastKnownTerminalDimensions method
- Extracted GetDefaultLaunchOptionsSnapshot method
- Extracted UpdateDefaultLaunchOptions method
- Extracted CloneLaunchOptions method
- SessionManager delegates to tracker
- All tests pass"
```

## Task 2.2: Extract Terminal Session Factory

**Goal:** Isolate TerminalSession creation and wiring

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/TerminalSessionFactory.cs`
2. Extract session creation logic from `CreateSessionAsync`:
   - `TerminalEmulator` instantiation
   - `ProcessManager` creation
   - Event subscription
   - `TerminalSession` construction
3. Keep exact event subscription order
4. Update `CreateSessionAsync` to call factory

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.2: Extract terminal session factory

- Created TerminalSessionFactory.cs
- Extracted TerminalEmulator instantiation logic
- Extracted ProcessManager creation logic
- Extracted event subscription logic
- Extracted TerminalSession construction
- Preserved exact event subscription order
- All tests pass"
```

## Task 2.3: Extract Session Creator

**Goal:** Move session creation logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionCreator.cs`
2. Move `CreateSessionAsync` body into `SessionCreator.CreateSessionAsync`
3. Keep lock acquisition in `SessionManager`
4. Pass locked state as parameters
5. Preserve exact validation and error handling

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.3: Extract session creator

- Created SessionCreator.cs
- Extracted CreateSessionAsync logic
- Lock acquisition remains in SessionManager
- Preserved exact validation and error handling
- All tests pass"
```

## Task 2.4: Extract Session Switcher

**Goal:** Move session switching logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionSwitcher.cs`
2. Move methods:
   - `SwitchToSession(Guid sessionId)`
   - `SwitchToNextSession()`
   - `SwitchToPreviousSession()`
3. Preserve exact active session tracking and event raising

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.4: Extract session switcher

- Created SessionSwitcher.cs
- Extracted SwitchToSession method
- Extracted SwitchToNextSession method
- Extracted SwitchToPreviousSession method
- Preserved active session tracking and events
- All tests pass"
```

## Task 2.5: Extract Session Closer

**Goal:** Move session closing logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionCloser.cs`
2. Move `CloseSessionAsync(Guid sessionId, CancellationToken cancellationToken)`
3. Preserve exact cleanup order and active session switching logic

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.5: Extract session closer

- Created SessionCloser.cs
- Extracted CloseSessionAsync method
- Preserved cleanup order and session switching
- All tests pass"
```

## Task 2.6: Extract Session Restarter

**Goal:** Move session restart logic

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionRestarter.cs`
2. Move `RestartSessionAsync(Guid sessionId, ProcessLaunchOptions?, CancellationToken)`
3. Preserve exact restart sequence

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.6: Extract session restarter

- Created SessionRestarter.cs
- Extracted RestartSessionAsync method
- Preserved exact restart sequence
- All tests pass"
```

## Task 2.7: Extract Session Event Bridge

**Goal:** Move event handlers

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionEventBridge.cs`
2. Move event handler methods:
   - `OnSessionStateChanged`
   - `OnSessionTitleChanged`
   - `OnSessionProcessExited`
3. Keep subscription points unchanged

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.7: Extract session event bridge

- Created SessionEventBridge.cs
- Extracted OnSessionStateChanged handler
- Extracted OnSessionTitleChanged handler
- Extracted OnSessionProcessExited handler
- Subscription points unchanged
- All tests pass"
```

## Task 2.8: Extract Session Logging

**Goal:** Move logging helpers

**Steps:**
1. Create file: `caTTY.Core/Terminal/Sessions/SessionLogging.cs`
2. Move:
   - `LogSessionLifecycleEvent`
   - `IsDebugLoggingEnabled`
3. Keep format strings identical

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 2.8: Extract session logging

- Created SessionLogging.cs
- Extracted LogSessionLifecycleEvent method
- Extracted IsDebugLoggingEnabled method
- Format strings preserved identically
- All tests pass"
```

**Phase 2 Complete:** `SessionManager.cs` should be facade ~200-300 LOC

---

# Phase 3: TerminalParserHandlers Refactoring

**Target:** `caTTY.Core/Terminal/TerminalParserHandlers.cs`
**Strategy:** Extract message type handlers

## Task 3.1: Extract SGR Handler

**Goal:** Isolate SGR handling

**Steps:**
1. Create folder: `caTTY.Core/Terminal/ParserHandlers/`
2. Create file: `caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs`
3. Move methods:
   - `HandleSgrSequence(SgrSequence sequence)`
   - `TraceSgrSequence(...)`
   - `FormatColor(Color? color)`
   - `ExtractSgrParameters(string rawSequence)`
4. Keep `TerminalParserHandlers.HandleSgr` as delegator

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.1: Extract SGR handler

- Created caTTY.Core/Terminal/ParserHandlers/ folder
- Created SgrHandler.cs
- Extracted HandleSgrSequence method
- Extracted TraceSgrSequence method
- Extracted FormatColor method
- Extracted ExtractSgrParameters method
- TerminalParserHandlers.HandleSgr delegates to handler
- All tests pass"
```

## Task 3.2: Extract DCS Handler

**Goal:** Isolate DCS handling

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/DcsHandler.cs`
2. Move methods:
   - `HandleDecrqss(DcsMessage message)`
   - `ExtractDecrqssPayload(string raw)`
   - `GenerateSgrStateResponse()`
3. Keep `HandleDcs` as delegator

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.2: Extract DCS handler

- Created DcsHandler.cs
- Extracted HandleDecrqss method
- Extracted ExtractDecrqssPayload method
- Extracted GenerateSgrStateResponse method
- HandleDcs delegates to handler
- All tests pass"
```

## Task 3.3: Extract OSC Handler

**Goal:** Isolate OSC handling

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/OscHandler.cs`
2. Move methods:
   - `HandleOsc(OscMessage message)`
   - `HandleXtermOsc(XtermOscMessage message)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.3: Extract OSC handler

- Created OscHandler.cs
- Extracted HandleOsc method
- Extracted HandleXtermOsc method
- All tests pass"
```

## Task 3.4: Extract CSI Dispatcher

**Goal:** Create CSI routing infrastructure

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDispatcher.cs`
2. Move `HandleCsi(CsiMessage message)` switch statement
3. Keep case bodies inline initially (will extract in next tasks)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.4: Extract CSI dispatcher

- Created CsiDispatcher.cs
- Extracted HandleCsi switch statement
- Case bodies remain inline for now
- All tests pass"
```

## Task 3.5: Extract CSI Cursor Handler

**Goal:** Extract cursor-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiCursorHandler.cs`
2. Extract cursor movement cases from CSI switch
3. Update dispatcher to delegate cursor cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.5: Extract CSI cursor handler

- Created CsiCursorHandler.cs
- Extracted cursor movement CSI cases
- Updated CsiDispatcher to delegate cursor cases
- All tests pass"
```

## Task 3.6: Extract CSI Erase Handler

**Goal:** Extract erase-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiEraseHandler.cs`
2. Extract erase cases (ED, EL, etc.)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.6: Extract CSI erase handler

- Created CsiEraseHandler.cs
- Extracted erase-related CSI cases (ED, EL, etc.)
- Updated CsiDispatcher to delegate erase cases
- All tests pass"
```

## Task 3.7: Extract CSI Scroll Handler

**Goal:** Extract scroll-related CSI cases

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiScrollHandler.cs`
2. Extract scroll region, scroll up/down cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.7: Extract CSI scroll handler

- Created CsiScrollHandler.cs
- Extracted scroll region CSI cases
- Extracted scroll up/down CSI cases
- All tests pass"
```

## Task 3.8: Extract CSI Insert/Delete Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiInsertDeleteHandler.cs`
2. Extract insert/delete lines/chars cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.8: Extract CSI insert/delete handler

- Created CsiInsertDeleteHandler.cs
- Extracted insert/delete lines CSI cases
- Extracted insert/delete chars CSI cases
- All tests pass"
```

## Task 3.9: Extract CSI DEC Mode Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDecModeHandler.cs`
2. Extract DEC mode setting cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.9: Extract CSI DEC mode handler

- Created CsiDecModeHandler.cs
- Extracted DEC mode setting CSI cases
- All tests pass"
```

## Task 3.10: Extract CSI Device Query Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiDeviceQueryHandler.cs`
2. Extract device status, cursor position query cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.10: Extract CSI device query handler

- Created CsiDeviceQueryHandler.cs
- Extracted device status CSI cases
- Extracted cursor position query cases
- All tests pass"
```

## Task 3.11: Extract CSI Window Manipulation Handler

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/CsiWindowManipulationHandler.cs`
2. Extract window ops cases

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.11: Extract CSI window manipulation handler

- Created CsiWindowManipulationHandler.cs
- Extracted window manipulation CSI cases
- All tests pass"
```

## Task 3.12: Extract C0 and ESC Handlers

**Goal:** Extract simple control handlers

**Steps:**
1. Create file: `caTTY.Core/Terminal/ParserHandlers/C0Handler.cs`
2. Move: `HandleBell`, `HandleBackspace`, `HandleTab`, `HandleLineFeed`, `HandleFormFeed`, `HandleCarriageReturn`, `HandleShiftIn`, `HandleShiftOut`, `HandleNormalByte`
3. Create file: `caTTY.Core/Terminal/ParserHandlers/EscHandler.cs`
4. Move: `HandleEsc(EscMessage message)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 3.12: Extract C0 and ESC handlers

- Created C0Handler.cs
- Extracted HandleBell, HandleBackspace, HandleTab
- Extracted HandleLineFeed, HandleFormFeed, HandleCarriageReturn
- Extracted HandleShiftIn, HandleShiftOut, HandleNormalByte
- Created EscHandler.cs
- Extracted HandleEsc method
- All tests pass"
```

**Phase 3 Complete:** `TerminalParserHandlers.cs` should be facade ~100-150 LOC

---

# Phase 4: TerminalEmulator Refactoring

**Target:** `caTTY.Core/Terminal/TerminalEmulator.cs` (~2500 LOC)
**Strategy:** Extract operation groups into EmulatorOps/ folder
**CRITICAL:** This is the largest refactoring - proceed incrementally, test after EACH file

## Task 4.1: Create Infrastructure

**Goal:** Set up folder structure

**Steps:**
1. Create folder: `caTTY.Core/Terminal/EmulatorOps/`
2. Plan facade structure (keep all public APIs in `TerminalEmulator.cs`)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.1: Create EmulatorOps infrastructure

- Created caTTY.Core/Terminal/EmulatorOps/ folder
- Prepared for operation extraction
- All tests pass"
```

## Task 4.2: Extract Viewport Operations

**Goal:** First extraction - lowest risk area

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalViewportOps.cs`
2. Move methods:
   - `ScrollViewportUp(int lines)`
   - `ScrollViewportDown(int lines)`
   - `ScrollViewportToTop()`
   - `ScrollViewportToBottom()`
3. Create instance in `TerminalEmulator`, delegate public methods

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.2: Extract viewport operations

- Created TerminalViewportOps.cs
- Extracted ScrollViewportUp method
- Extracted ScrollViewportDown method
- Extracted ScrollViewportToTop method
- Extracted ScrollViewportToBottom method
- TerminalEmulator delegates to viewport ops
- All tests pass"
```

## Task 4.3: Extract Resize Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalResizeOps.cs`
2. Move: `Resize(int cols, int rows)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.3: Extract resize operations

- Created TerminalResizeOps.cs
- Extracted Resize method
- All tests pass"
```

## Task 4.4: Extract Cursor Movement Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCursorMovementOps.cs`
2. Move:
   - `MoveCursorUp(int count)`
   - `MoveCursorDown(int count)`
   - `MoveCursorForward(int count)`
   - `MoveCursorBackward(int count)`
   - `SetCursorPosition(int row, int col)`
   - `SetCursorColumn(int col)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.4: Extract cursor movement operations

- Created TerminalCursorMovementOps.cs
- Extracted MoveCursorUp, MoveCursorDown
- Extracted MoveCursorForward, MoveCursorBackward
- Extracted SetCursorPosition, SetCursorColumn
- All tests pass"
```

## Task 4.5: Extract Cursor Save/Restore Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCursorSaveRestoreOps.cs`
2. Move:
   - `SaveCursorPosition()`
   - `RestoreCursorPosition()`
   - `SaveCursorPositionAnsi()`
   - `RestoreCursorPositionAnsi()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.5: Extract cursor save/restore operations

- Created TerminalCursorSaveRestoreOps.cs
- Extracted SaveCursorPosition, RestoreCursorPosition
- Extracted SaveCursorPositionAnsi, RestoreCursorPositionAnsi
- All tests pass"
```

## Task 4.6: Extract Cursor Style Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCursorStyleOps.cs`
2. Move:
   - `SetCursorStyle(int style)`
   - `SetCursorStyle(CursorStyle style)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.6: Extract cursor style operations

- Created TerminalCursorStyleOps.cs
- Extracted SetCursorStyle(int) method
- Extracted SetCursorStyle(CursorStyle) method
- All tests pass"
```

## Task 4.7: Extract Erase In Display Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalEraseInDisplayOps.cs`
2. Move: `ClearDisplay(int mode)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.7: Extract erase in display operations

- Created TerminalEraseInDisplayOps.cs
- Extracted ClearDisplay method
- All tests pass"
```

## Task 4.8: Extract Erase In Line Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalEraseInLineOps.cs`
2. Move: `ClearLine(int mode)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.8: Extract erase in line operations

- Created TerminalEraseInLineOps.cs
- Extracted ClearLine method
- All tests pass"
```

## Task 4.9: Extract Selective Erase In Display Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInDisplayOps.cs`
2. Move: `ClearDisplaySelective(int mode)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.9: Extract selective erase in display operations

- Created TerminalSelectiveEraseInDisplayOps.cs
- Extracted ClearDisplaySelective method
- All tests pass"
```

## Task 4.10: Extract Selective Erase In Line Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalSelectiveEraseInLineOps.cs`
2. Move: `ClearLineSelective(int mode)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.10: Extract selective erase in line operations

- Created TerminalSelectiveEraseInLineOps.cs
- Extracted ClearLineSelective method
- All tests pass"
```

## Task 4.11: Extract Scroll Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalScrollOps.cs`
2. Move:
   - `ScrollScreenUp(int lines)`
   - `ScrollScreenDown(int lines)`
   - `HandleReverseIndex()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.11: Extract scroll operations

- Created TerminalScrollOps.cs
- Extracted ScrollScreenUp, ScrollScreenDown
- Extracted HandleReverseIndex
- All tests pass"
```

## Task 4.12: Extract Scroll Region Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalScrollRegionOps.cs`
2. Move: `SetScrollRegion(int top, int bottom)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.12: Extract scroll region operations

- Created TerminalScrollRegionOps.cs
- Extracted SetScrollRegion method
- All tests pass"
```

## Task 4.13: Extract Insert Lines Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalInsertLinesOps.cs`
2. Move: `InsertLinesInRegion(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.13: Extract insert lines operations

- Created TerminalInsertLinesOps.cs
- Extracted InsertLinesInRegion method
- All tests pass"
```

## Task 4.14: Extract Delete Lines Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalDeleteLinesOps.cs`
2. Move: `DeleteLinesInRegion(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.14: Extract delete lines operations

- Created TerminalDeleteLinesOps.cs
- Extracted DeleteLinesInRegion method
- All tests pass"
```

## Task 4.15: Extract Insert Characters Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalInsertCharsOps.cs`
2. Move: `InsertCharactersInLine(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.15: Extract insert characters operations

- Created TerminalInsertCharsOps.cs
- Extracted InsertCharactersInLine method
- All tests pass"
```

## Task 4.16: Extract Delete Characters Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalDeleteCharsOps.cs`
2. Move: `DeleteCharactersInLine(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.16: Extract delete characters operations

- Created TerminalDeleteCharsOps.cs
- Extracted DeleteCharactersInLine method
- All tests pass"
```

## Task 4.17: Extract Erase Characters Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalEraseCharsOps.cs`
2. Move: `EraseCharactersInLine(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.17: Extract erase characters operations

- Created TerminalEraseCharsOps.cs
- Extracted EraseCharactersInLine method
- All tests pass"
```

## Task 4.18: Extract Insert Mode Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalInsertModeOps.cs`
2. Move:
   - `SetInsertMode(bool enabled)`
   - `ShiftCharactersRight(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.18: Extract insert mode operations

- Created TerminalInsertModeOps.cs
- Extracted SetInsertMode method
- Extracted ShiftCharactersRight method
- All tests pass"
```

## Task 4.19: Extract DEC Mode Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalDecModeOps.cs`
2. Move: `SetDecMode(int mode, bool enabled)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.19: Extract DEC mode operations

- Created TerminalDecModeOps.cs
- Extracted SetDecMode method
- All tests pass"
```

## Task 4.20: Extract Alternate Screen Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalAlternateScreenOps.cs`
2. Move: `HandleAlternateScreenMode(bool enabled)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.20: Extract alternate screen operations

- Created TerminalAlternateScreenOps.cs
- Extracted HandleAlternateScreenMode method
- All tests pass"
```

## Task 4.21: Extract Private Modes Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalPrivateModesOps.cs`
2. Move:
   - `SavePrivateModes(int[] modes)`
   - `RestorePrivateModes(int[] modes)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.21: Extract private modes operations

- Created TerminalPrivateModesOps.cs
- Extracted SavePrivateModes method
- Extracted RestorePrivateModes method
- All tests pass"
```

## Task 4.22: Extract Bracketed Paste Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalBracketedPasteOps.cs`
2. Move:
   - `WrapPasteContent(string pasteContent)`
   - `WrapPasteContent(ReadOnlySpan<char> pasteContent)`
   - `IsBracketedPasteModeEnabled()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.22: Extract bracketed paste operations

- Created TerminalBracketedPasteOps.cs
- Extracted WrapPasteContent(string) method
- Extracted WrapPasteContent(ReadOnlySpan<char>) method
- Extracted IsBracketedPasteModeEnabled method
- All tests pass"
```

## Task 4.23: Extract OSC Title/Icon Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscTitleIconOps.cs`
2. Move:
   - `SetWindowTitle(string title)`
   - `SetIconName(string name)`
   - `SetTitleAndIcon(string text)`
   - `GetWindowTitle()`
   - `GetIconName()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.23: Extract OSC title/icon operations

- Created TerminalOscTitleIconOps.cs
- Extracted SetWindowTitle, SetIconName, SetTitleAndIcon
- Extracted GetWindowTitle, GetIconName
- All tests pass"
```

## Task 4.24: Extract OSC Window Manipulation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscWindowManipulationOps.cs`
2. Move: `HandleWindowManipulation(int operation, int[] params)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.24: Extract OSC window manipulation operations

- Created TerminalOscWindowManipulationOps.cs
- Extracted HandleWindowManipulation method
- All tests pass"
```

## Task 4.25: Extract OSC Clipboard Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscClipboardOps.cs`
2. Move:
   - `HandleClipboard(string data)`
   - `OnClipboardRequest()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.25: Extract OSC clipboard operations

- Created TerminalOscClipboardOps.cs
- Extracted HandleClipboard method
- Extracted OnClipboardRequest method
- All tests pass"
```

## Task 4.26: Extract OSC Hyperlink Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscHyperlinkOps.cs`
2. Move: `HandleHyperlink(string params, string uri)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.26: Extract OSC hyperlink operations

- Created TerminalOscHyperlinkOps.cs
- Extracted HandleHyperlink method
- All tests pass"
```

## Task 4.27: Extract OSC Color Query Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscColorQueryOps.cs`
2. Move:
   - `GetCurrentForegroundColor()`
   - `GetCurrentBackgroundColor()`
   - `GetDefaultForegroundColor()`
   - `GetDefaultBackgroundColor()`
   - `GetNamedColorRgb(int colorIndex)`
   - `GetIndexedColorRgb(int index)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.27: Extract OSC color query operations

- Created TerminalOscColorQueryOps.cs
- Extracted color query methods
- Extracted palette helper methods
- All tests pass"
```

## Task 4.28: Extract Charset Designation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCharsetDesignationOps.cs`
2. Move: `DesignateCharacterSet(char designator, char charset)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.28: Extract charset designation operations

- Created TerminalCharsetDesignationOps.cs
- Extracted DesignateCharacterSet method
- All tests pass"
```

## Task 4.29: Extract Charset Translation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCharsetTranslationOps.cs`
2. Move:
   - `HandleShiftIn()`
   - `HandleShiftOut()`
   - `TranslateCharacter(char c)`
   - `GenerateCharacterSetQueryResponse()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.29: Extract charset translation operations

- Created TerminalCharsetTranslationOps.cs
- Extracted HandleShiftIn, HandleShiftOut
- Extracted TranslateCharacter
- Extracted GenerateCharacterSetQueryResponse
- All tests pass"
```

## Task 4.30: Extract Line Feed Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalLineFeedOps.cs`
2. Move: `HandleLineFeed()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.30: Extract line feed operations

- Created TerminalLineFeedOps.cs
- Extracted HandleLineFeed method
- All tests pass"
```

## Task 4.31: Extract Index Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalIndexOps.cs`
2. Move: `HandleIndex()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.31: Extract index operations

- Created TerminalIndexOps.cs
- Extracted HandleIndex method
- All tests pass"
```

## Task 4.32: Extract Carriage Return Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCarriageReturnOps.cs`
2. Move: `HandleCarriageReturn()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.32: Extract carriage return operations

- Created TerminalCarriageReturnOps.cs
- Extracted HandleCarriageReturn method
- All tests pass"
```

## Task 4.33: Extract Bell Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalBellOps.cs`
2. Move:
   - `HandleBell()`
   - `OnBell()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.33: Extract bell operations

- Created TerminalBellOps.cs
- Extracted HandleBell method
- Extracted OnBell event raiser
- All tests pass"
```

## Task 4.34: Extract Backspace Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalBackspaceOps.cs`
2. Move: `HandleBackspace()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.34: Extract backspace operations

- Created TerminalBackspaceOps.cs
- Extracted HandleBackspace method
- All tests pass"
```

## Task 4.35: Extract Tab Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalTabOps.cs`
2. Move:
   - `HandleTab()`
   - `SetTabStopAtCursor()`
   - `CursorForwardTab(int count)`
   - `CursorBackwardTab(int count)`
   - `ClearTabStopAtCursor()`
   - `ClearAllTabStops()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.35: Extract tab operations

- Created TerminalTabOps.cs
- Extracted HandleTab method
- Extracted tab stop management methods
- Extracted cursor tab navigation methods
- All tests pass"
```

## Task 4.36: Extract Response Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalResponseOps.cs`
2. Move:
   - `EmitResponse(string response)`
   - `OnResponseEmitted(ResponseEmittedEventArgs e)`
   - `OnResponseEmitted(string response)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.36: Extract response operations

- Created TerminalResponseOps.cs
- Extracted EmitResponse method
- Extracted OnResponseEmitted overloads
- All tests pass"
```

## Task 4.37: Extract Screen Update Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalScreenUpdateOps.cs`
2. Move: `OnScreenUpdated()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.37: Extract screen update operations

- Created TerminalScreenUpdateOps.cs
- Extracted OnScreenUpdated method
- All tests pass"
```

## Task 4.38: Extract Title/Icon Event Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalTitleIconEventsOps.cs`
2. Move:
   - `OnTitleChanged(string title)`
   - `OnIconNameChanged(string name)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.38: Extract title/icon event operations

- Created TerminalTitleIconEventsOps.cs
- Extracted OnTitleChanged method
- Extracted OnIconNameChanged method
- All tests pass"
```

## Task 4.39: Extract Input Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalInputOps.cs`
2. Move:
   - `Write(ReadOnlySpan<byte>)`
   - `Write(string)`
   - `FlushIncompleteSequences()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.39: Extract input operations

- Created TerminalInputOps.cs
- Extracted Write(ReadOnlySpan<byte>) method
- Extracted Write(string) method
- Extracted FlushIncompleteSequences method
- All tests pass"
```

## Task 4.40: Extract Reset Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalResetOps.cs`
2. Move:
   - `ResetToInitialState()`
   - `SoftReset()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.40: Extract reset operations

- Created TerminalResetOps.cs
- Extracted ResetToInitialState method
- Extracted SoftReset method
- All tests pass"
```

**Phase 4 Complete:** `TerminalEmulator.cs` should be facade ~200-400 LOC with EmulatorOps/ containing ~40 focused classes

**Git Commit (Phase Completion):**
```bash
git add .
git commit -m "Phase 4 Complete: TerminalEmulator refactored

- TerminalEmulator.cs reduced from ~2500 to ~200-400 LOC
- Created 40+ focused operation classes in EmulatorOps/
- All public APIs preserved
- Zero business logic changes
- All ~1500 tests pass"
```

---

# Phase 5: Parser Engine Refactoring

**Target:** `caTTY.Core/Parsing/Parser.cs`
**Strategy:** Extract state machine into engine with state handlers

## Task 5.1: Create Parser Engine Context

**Steps:**
1. Create folder: `caTTY.Core/Parsing/Engine/`
2. Create file: `caTTY.Core/Parsing/Engine/ParserEngineContext.cs`
3. Move parser state fields (buffers, state enum, etc.)

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.1: Create parser engine context

- Created caTTY.Core/Parsing/Engine/ folder
- Created ParserEngineContext.cs
- Extracted parser state fields
- Extracted buffers and state enum
- All tests pass"
```

## Task 5.2: Create Parser Engine

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/ParserEngine.cs`
2. Move `ProcessByte(byte b)` logic
3. Keep `Parser.cs` as facade delegating to engine

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.2: Create parser engine

- Created ParserEngine.cs
- Extracted ProcessByte logic
- Parser.cs delegates to engine
- All tests pass"
```

## Task 5.3: Extract Normal State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/NormalStateHandler.cs`
2. Move: `HandleNormalState(byte b)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.3: Extract normal state handler

- Created NormalStateHandler.cs
- Extracted HandleNormalState method
- All tests pass"
```

## Task 5.4: Extract Escape State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/EscapeStateHandler.cs`
2. Move: `HandleEscapeState(byte b)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.4: Extract escape state handler

- Created EscapeStateHandler.cs
- Extracted HandleEscapeState method
- All tests pass"
```

## Task 5.5: Extract CSI State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/CsiStateHandler.cs`
2. Move:
   - `HandleCsiState(byte b)`
   - `HandleCsiByte(byte b)`
   - `FinishCsiSequence()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.5: Extract CSI state handler

- Created CsiStateHandler.cs
- Extracted HandleCsiState method
- Extracted HandleCsiByte method
- Extracted FinishCsiSequence method
- All tests pass"
```

## Task 5.6: Extract OSC State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/OscStateHandler.cs`
2. Move:
   - `HandleOscState(byte b)`
   - `HandleOscEscapeState(byte b)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.6: Extract OSC state handler

- Created OscStateHandler.cs
- Extracted HandleOscState method
- Extracted HandleOscEscapeState method
- All tests pass"
```

## Task 5.7: Extract DCS State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/DcsStateHandler.cs`
2. Move:
   - `HandleDcsState(byte b)`
   - `HandleDcsEscapeState(byte b)`
   - `FinishDcsSequence(string terminator)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.7: Extract DCS state handler

- Created DcsStateHandler.cs
- Extracted HandleDcsState method
- Extracted HandleDcsEscapeState method
- Extracted FinishDcsSequence method
- All tests pass"
```

## Task 5.8: Extract Control String State Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/ControlStringStateHandler.cs`
2. Move:
   - `HandleControlStringState(byte b)`
   - `HandleControlStringEscapeState(byte b)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.8: Extract control string state handler

- Created ControlStringStateHandler.cs
- Extracted HandleControlStringState method
- Extracted HandleControlStringEscapeState method
- All tests pass"
```

## Task 5.9: Extract RPC Sequence Handler

**Steps:**
1. Create file: `caTTY.Core/Parsing/Engine/RpcSequenceHandler.cs`
2. Move:
   - `IsRpcHandlingEnabled()`
   - `TryHandleRpcSequence()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 5.9: Extract RPC sequence handler

- Created RpcSequenceHandler.cs
- Extracted IsRpcHandlingEnabled method
- Extracted TryHandleRpcSequence method
- All tests pass"
```

**Phase 5 Complete:** `Parser.cs` should be facade ~100-150 LOC

**Git Commit (Phase Completion):**
```bash
git add .
git commit -m "Phase 5 Complete: Parser engine refactored

- Parser.cs reduced to ~100-150 LOC facade
- Created 9 focused state handlers in Engine/
- State machine logic properly separated
- All tests pass"
```

---

# Phase 6: Display TerminalController Refactoring

**Target:** `caTTY.Display/Controllers/TerminalController.cs`
**Strategy:** Extract UI subsystems

## Task 6.1: Extract Layout Constants

**Steps:**
1. Create file: `caTTY.Display/Controllers/LayoutConstants.cs`
2. Move `LayoutConstants` class

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.1: Extract layout constants

- Created LayoutConstants.cs
- Extracted LayoutConstants class
- All tests pass"
```

## Task 6.2: Extract Terminal Settings

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalSettings.cs`
2. Move `TerminalSettings` class with `Validate()` and `Clone()`

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.2: Extract terminal settings

- Created TerminalSettings.cs
- Extracted TerminalSettings class
- Extracted Validate method
- Extracted Clone method
- All tests pass"
```

## Task 6.3: Extract Font Subsystem

**Steps:**
1. Create folder: `caTTY.Display/Controllers/TerminalUi/`
2. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
3. Move all font-related methods (load, calculate metrics, size adjustment, etc.)

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.3: Extract font subsystem

- Created caTTY.Display/Controllers/TerminalUi/ folder
- Created TerminalUiFonts.cs
- Extracted font loading methods
- Extracted font metrics calculation
- Extracted font size adjustment methods
- All tests pass"
```

## Task 6.4: Extract Render Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiRender.cs`
2. Move all rendering methods (Render, RenderCell, RenderCursor, underline variants, font stack, etc.)

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.4: Extract render subsystem

- Created TerminalUiRender.cs
- Extracted Render method and helpers
- Extracted cell and cursor rendering
- Extracted underline/strikethrough rendering
- Extracted font stack management
- All tests pass"
```

## Task 6.5: Extract Input Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
2. Move input handling methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.5: Extract input subsystem

- Created TerminalUiInput.cs
- Extracted HandleInput method
- Extracted special key handling
- Extracted input capture management
- All tests pass"
```

## Task 6.6: Extract Mouse Tracking Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiMouseTracking.cs`
2. Move mouse tracking and handling methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.6: Extract mouse tracking subsystem

- Created TerminalUiMouseTracking.cs
- Extracted mouse tracking methods
- Extracted coordinate conversion
- Extracted wheel scroll handling
- All tests pass"
```

## Task 6.7: Extract Selection Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiSelection.cs`
2. Move selection handling methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.7: Extract selection subsystem

- Created TerminalUiSelection.cs
- Extracted selection mouse handlers
- Extracted SelectAllVisibleContent
- Extracted clipboard copy methods
- All tests pass"
```

## Task 6.8: Extract Resize Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
2. Move resize and dimension calculation methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.8: Extract resize subsystem

- Created TerminalUiResize.cs
- Extracted window resize handling
- Extracted dimension calculation methods
- Extracted terminal resize triggers
- All tests pass"
```

## Task 6.9: Extract Tabs Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiTabs.cs`
2. Move tab rendering and tab area methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.9: Extract tabs subsystem

- Created TerminalUiTabs.cs
- Extracted tab area rendering
- Extracted tab height calculations
- All tests pass"
```

## Task 6.10: Extract Settings Panel Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Move menu bar, settings rendering, and configuration methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.10: Extract settings panel subsystem

- Created TerminalUiSettingsPanel.cs
- Extracted menu bar rendering
- Extracted settings menu methods
- Extracted shell configuration UI
- Extracted theme selection UI
- All tests pass"
```

## Task 6.11: Extract Events Subsystem

**Steps:**
1. Create file: `caTTY.Display/Controllers/TerminalUi/TerminalUiEvents.cs`
2. Move event handler methods

**Validation:**
```bash
dotnet build caTTY.Display
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 6.11: Extract events subsystem

- Created TerminalUiEvents.cs
- Extracted terminal event handlers
- Extracted session event handlers
- Extracted mouse event handlers
- All tests pass"
```

**Phase 6 Complete:** `TerminalController.cs` should be facade ~200-300 LOC

**Git Commit (Phase Completion):**
```bash
git add .
git commit -m "Phase 6 Complete: TerminalController refactored

- TerminalController.cs reduced to ~200-300 LOC facade
- Created 11 focused UI subsystem classes in TerminalUi/
- All display functionality preserved
- All tests pass"
```