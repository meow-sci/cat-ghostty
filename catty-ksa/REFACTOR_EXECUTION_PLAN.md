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
