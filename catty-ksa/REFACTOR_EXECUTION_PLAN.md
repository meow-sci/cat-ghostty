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
