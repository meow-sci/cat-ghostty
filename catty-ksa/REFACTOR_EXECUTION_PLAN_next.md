



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