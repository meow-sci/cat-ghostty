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
