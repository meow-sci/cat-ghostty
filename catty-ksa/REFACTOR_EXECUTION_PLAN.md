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
