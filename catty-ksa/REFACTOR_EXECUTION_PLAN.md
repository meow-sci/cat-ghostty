# Refactor Execution Plan - caTTY-cs

**CRITICAL: This is a step-by-step execution plan for breaking down large classes into smaller, maintainable units.**

## Constraints & Rules

### Non-Negotiable Requirements
1. **ZERO business logic changes** - only move code, never modify behavior
2. **Preserve execution order** - all conditionals, loops, and side-effects must remain identical
3. **No partial classes** - use facade pattern with operation classes
4. **Target file size: 150-350 LOC** (hard cap: ~500 LOC)
5. **Build + tests must pass after EVERY task** - use `.\scripts\dotnet-test.ps1`
6. **Git commit after EVERY task** - track progress incrementally

### Validation After Each Task
```bash
# REQUIRED after every task completion:
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1

# If tests pass, commit:
git add .
git commit -m "Task X.Y: <description>

- Bullet point of what was implemented
- Another bullet point
- Etc."
```

### Refactoring Pattern
- Keep original class as **facade** (orchestrator with public API)
- Extract implementation into **operation classes** in subfolder
- Facade delegates to operation classes (1-line calls)
- Operation classes receive dependencies via constructor or method parameters
- Avoid cross-calling between operation classes - call through facade if needed

---

# Phase 0: Baseline Validation

## Task 0.1: Verify Current State
**Goal:** Establish baseline - confirm all tests pass before refactoring begins.

**Steps:**
1. Run full build: `dotnet build`
2. Run all tests: `.\scripts\dotnet-test.ps1`
3. Document any existing failures (do not fix)
4. Confirm test count (~1500 tests)

**Validation:**
```bash
dotnet build
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 0.1: Establish refactoring baseline

- Confirmed all tests pass
- Documented test count: ~1500 tests
- Verified build succeeds
- Ready to begin refactoring"
```

**Success Criteria:**
- All existing tests pass (or document pre-existing failures)
- Baseline established


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

# Completion Criteria

## Success Metrics
- ✅ All major files < 500 LOC (target 150-350 LOC)
- ✅ All tests pass: `.\scripts\dotnet-test.ps1`
- ✅ Full build passes: `dotnet build`
- ✅ Zero business logic changes
- ✅ Improved searchability and navigation
- ✅ Clear separation of concerns
- ✅ ~150-200 focused files vs original ~20-30 large files
- ✅ Complete git history tracking all changes

## Final Validation
```bash
dotnet build
.\scripts\dotnet-test.ps1
```

**Final Git Commit:**
```bash
git add .
git commit -m "Refactoring Complete: All phases finished

Summary of changes:
- ProcessManager: 8 operation classes extracted
- SessionManager: 8 session management classes extracted
- TerminalParserHandlers: 12 handler classes extracted
- TerminalEmulator: 40+ operation classes extracted
- Parser: 9 state handler classes extracted
- TerminalController: 11 UI subsystem classes extracted

Metrics:
- File count: ~20-30 → ~150-200 files
- Largest file: ~2500 LOC → ~300-400 LOC
- Test count: ~1500 tests (all passing)
- Business logic changes: ZERO
- Architecture: Facade pattern with operation classes

Benefits for AI/LLM agents:
- Better context efficiency
- Improved code navigation
- Easier to locate and modify specific functionality
- Clear separation of concerns"
```

---

# Notes for Execution

1. **Never skip validation** - test after EVERY task
2. **Never skip git commits** - commit after EVERY successful validation
3. **Preserve exact logic** - copy-paste code, don't rewrite
4. **Keep commits atomic** - one task = one commit
5. **Use descriptive commit messages** - include task number and bullet points
6. **Document deviations** - if something can't be extracted as planned, document in commit message
7. **Sequential execution required** - complete tasks in order to avoid conflicts
8. **Git history is documentation** - commit messages tell the refactoring story
