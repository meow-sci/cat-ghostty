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
