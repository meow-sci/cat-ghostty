# Continued Refactor Plan - Phase 7+

## Overview

This document outlines the continued refactoring of caTTY-cs to achieve the goal of **all production files ≤500 LOC**. The recent refactor (Phases 1-6) successfully reduced major monolithic classes through the facade + operation classes pattern. This plan addresses the remaining files exceeding 500 LOC.

**Current Status:**
- ✅ Phase 1-6: Completed (TerminalEmulator, TerminalController, Parser, ProcessManager, SessionManager operations extracted)
- 🔄 Phase 7+: Remaining files >500 LOC (11 production files)

**Guiding Principles:**
- ✅ NO functionality changes - logic must remain identical
- ✅ Tests must pass after each task
- ✅ Use `scripts\dotnet-test.ps1` (0 = success, non-zero = failure)
- ✅ DO NOT run `dotnet test` directly (bloats context with stdout)
- ✅ Commit after each task with structured message (80 char subject, task prefix, bullet list body)
- ✅ Maintain facade + operation class pattern
- ✅ Keep files discoverable (clear names matching responsibility)

---

## Files Requiring Refactoring (Priority Order)

### High Priority (>700 LOC)
1. **TerminalEmulator.cs** (1,199 LOC) - Facade initialization complexity
2. **TerminalController.cs** (1,130 LOC) - Facade initialization complexity
3. **TerminalUiSettingsPanel.cs** (989 LOC) - Monolithic menu UI
4. **SgrParser.cs** (879 LOC) - Parser logic not yet decomposed
5. **TerminalUiFonts.cs** (807 LOC) - Complex font management
6. **CsiParser.cs** (739 LOC) - Parser logic not yet decomposed
7. **TerminalUiResize.cs** (707 LOC) - Complex resize calculations

### Medium Priority (550-700 LOC)
8. **ScreenBufferManager.cs** (601 LOC) - Grid manipulation logic
9. **TerminalUiInput.cs** (582 LOC) - Input and focus handling
10. **SessionManager.cs** (552 LOC) - More delegation opportunities
11. **ProcessManager.cs** (539 LOC) - More delegation opportunities



---

## Phase 10: Extract TerminalUiFonts Components

**Goal:** Reduce TerminalUiFonts.cs from 807 LOC to <250 LOC by splitting font management concerns.

**Context:** Font management includes loading fonts, calculating metrics, family selection UI, and persisting configuration. These are distinct responsibilities.

### Task 10.1: Extract FontLoader

**Objective:** Extract font file loading logic into `TerminalUi/Fonts/FontLoader.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
2. Identify font loading logic (font file discovery, loading from disk, fallback handling)
3. Create directory `caTTY.Display/Controllers/TerminalUi/Fonts/`
4. Create `caTTY.Display/Controllers/TerminalUi/Fonts/FontLoader.cs` with class `FontLoader`
5. Move font loading logic:
   - Font file enumeration from system directories
   - Font data loading
   - Font fallback mechanisms
   - Error handling for missing fonts
6. Create constructor with dependencies
7. Add public methods for loading operations
8. Update `TerminalUiFonts.cs` to:
   - Instantiate `FontLoader`
   - Delegate loading operations
9. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
10. Commit with message:
    ```
    Task 10.1: Extract FontLoader from TerminalUiFonts

    - Create Fonts/ subdirectory
    - Extract font loading logic to FontLoader.cs
    - Update TerminalUiFonts.cs to delegate loading
    - No functionality changes, all tests pass
    ```

**Target:** FontLoader.cs ~150-200 LOC

---

### Task 10.2: Extract FontMetricsCalculator

**Objective:** Extract font metrics calculation into `TerminalUi/Fonts/FontMetricsCalculator.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
2. Identify metrics calculation (character width/height, baseline, ascent/descent, cell sizing)
3. Create `caTTY.Display/Controllers/TerminalUi/Fonts/FontMetricsCalculator.cs` with class `FontMetricsCalculator`
4. Move metrics calculation logic
5. Create constructor with dependencies
6. Update `TerminalUiFonts.cs` to delegate metrics calculations
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 10.2: Extract FontMetricsCalculator from TerminalUiFonts

   - Extract metrics calculation to FontMetricsCalculator.cs
   - Update TerminalUiFonts.cs to delegate metrics operations
   - No functionality changes, all tests pass
   ```

**Target:** FontMetricsCalculator.cs ~150-200 LOC

---

### Task 10.3: Extract FontFamilySelector

**Objective:** Extract font family selection UI into `TerminalUi/Fonts/FontFamilySelector.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
2. Identify font family selection UI (dropdown, list rendering, preview)
3. Create `caTTY.Display/Controllers/TerminalUi/Fonts/FontFamilySelector.cs` with class `FontFamilySelector`
4. Move family selection UI and interaction logic
5. Create constructor with dependencies
6. Update `TerminalUiFonts.cs` to delegate selection UI
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 10.3: Extract FontFamilySelector from TerminalUiFonts

   - Extract family selection UI to FontFamilySelector.cs
   - Update TerminalUiFonts.cs to delegate selection operations
   - No functionality changes, all tests pass
   ```

**Target:** FontFamilySelector.cs ~150-200 LOC

---

### Task 10.4: Extract FontConfigPersistence

**Objective:** Extract font configuration save/load into `TerminalUi/Fonts/FontConfigPersistence.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
2. Identify config persistence (save font settings, load on startup, defaults)
3. Create `caTTY.Display/Controllers/TerminalUi/Fonts/FontConfigPersistence.cs` with class `FontConfigPersistence`
4. Move persistence logic
5. Create constructor with dependencies
6. Update `TerminalUiFonts.cs` to delegate persistence operations
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 10.4: Extract FontConfigPersistence from TerminalUiFonts

   - Extract config persistence to FontConfigPersistence.cs
   - Update TerminalUiFonts.cs to delegate save/load operations
   - No functionality changes, all tests pass
   ```

**Target:** FontConfigPersistence.cs ~100-150 LOC

---

### Task 10.5: Reduce TerminalUiFonts.cs to coordinator

**Objective:** Finalize TerminalUiFonts.cs as coordinator of font management components.

**Steps:**
1. Read current `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs`
2. Ensure all specialized logic moved to component classes
3. Refactor to coordination pattern
4. Keep only high-level font management API
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 10.5: Reduce TerminalUiFonts to coordinator

   - Remove remaining specialized logic
   - Finalize delegation to font components
   - TerminalUiFonts.cs now <250 LOC coordinator
   - No functionality changes, all tests pass
   ```

**Target:** TerminalUiFonts.cs <250 LOC (down from 807 LOC)

---

## Phase 11: Extract TerminalUiResize Components

**Goal:** Reduce TerminalUiResize.cs from 707 LOC to <250 LOC by splitting resize stages.

**Context:** Resize handling involves window-level resize detection, terminal dimension calculation (columns/rows from pixels), and font-driven resize. These are distinct stages.

### Task 11.1: Extract WindowResizeHandler

**Objective:** Extract window resize detection into `TerminalUi/Resize/WindowResizeHandler.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
2. Identify window resize detection (ImGui window size changes, DPI changes)
3. Create directory `caTTY.Display/Controllers/TerminalUi/Resize/`
4. Create `caTTY.Display/Controllers/TerminalUi/Resize/WindowResizeHandler.cs` with class `WindowResizeHandler`
5. Move window-level resize logic
6. Create constructor with dependencies
7. Update `TerminalUiResize.cs` to delegate window resize detection
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 11.1: Extract WindowResizeHandler from TerminalUiResize

   - Create Resize/ subdirectory
   - Extract window resize detection to WindowResizeHandler.cs
   - Update TerminalUiResize.cs to delegate window operations
   - No functionality changes, all tests pass
   ```

**Target:** WindowResizeHandler.cs ~150-200 LOC

---

### Task 11.2: Extract TerminalDimensionCalculator

**Objective:** Extract terminal dimension calculation into `TerminalUi/Resize/TerminalDimensionCalculator.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
2. Identify dimension calculation (pixels → columns/rows based on font metrics)
3. Create `caTTY.Display/Controllers/TerminalUi/Resize/TerminalDimensionCalculator.cs` with class `TerminalDimensionCalculator`
4. Move dimension calculation logic
5. Create constructor with dependencies
6. Update `TerminalUiResize.cs` to delegate dimension calculations
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 11.2: Extract TerminalDimensionCalculator from TerminalUiResize

   - Extract dimension calculation to TerminalDimensionCalculator.cs
   - Update TerminalUiResize.cs to delegate dimension operations
   - No functionality changes, all tests pass
   ```

**Target:** TerminalDimensionCalculator.cs ~150-200 LOC

---

### Task 11.3: Extract FontResizeProcessor

**Objective:** Extract font-driven resize logic into `TerminalUi/Resize/FontResizeProcessor.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
2. Identify font resize handling (font size changes triggering terminal resize)
3. Create `caTTY.Display/Controllers/TerminalUi/Resize/FontResizeProcessor.cs` with class `FontResizeProcessor`
4. Move font resize logic
5. Create constructor with dependencies
6. Update `TerminalUiResize.cs` to delegate font-driven resizes
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 11.3: Extract FontResizeProcessor from TerminalUiResize

   - Extract font resize handling to FontResizeProcessor.cs
   - Update TerminalUiResize.cs to delegate font operations
   - No functionality changes, all tests pass
   ```

**Target:** FontResizeProcessor.cs ~150-200 LOC

---

### Task 11.4: Reduce TerminalUiResize.cs to coordinator

**Objective:** Finalize TerminalUiResize.cs as coordinator of resize stages.

**Steps:**
1. Read current `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs`
2. Ensure all stage logic moved to component classes
3. Refactor to orchestration pattern
4. Keep only high-level resize coordination
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 11.4: Reduce TerminalUiResize to coordinator

   - Remove remaining stage logic
   - Finalize delegation to resize components
   - TerminalUiResize.cs now <250 LOC coordinator
   - No functionality changes, all tests pass
   ```

**Target:** TerminalUiResize.cs <250 LOC (down from 707 LOC)


---

## Completion Criteria

### Phase 7 (SgrParser): 5 tasks
- ✅ SgrParser.cs reduced to <200 LOC
- ✅ 4 component classes created in Sgr/ subdirectory
- ✅ All tests pass

### Phase 8 (CsiParser): 4 tasks
- ✅ CsiParser.cs reduced to <250 LOC
- ✅ 3 component classes created in Csi/ subdirectory
- ✅ All tests pass

### Phase 9 (TerminalUiSettingsPanel): 7 tasks
- ✅ TerminalUiSettingsPanel.cs reduced to <200 LOC
- ✅ 6 menu renderer classes created in Menus/ subdirectory
- ✅ All tests pass

### Phase 10 (TerminalUiFonts): 5 tasks
- ✅ TerminalUiFonts.cs reduced to <250 LOC
- ✅ 4 component classes created in Fonts/ subdirectory
- ✅ All tests pass

### Phase 11 (TerminalUiResize): 4 tasks
- ✅ TerminalUiResize.cs reduced to <250 LOC
- ✅ 3 component classes created in Resize/ subdirectory
- ✅ All tests pass

### Phase 12 (TerminalUiInput): 4 tasks
- ✅ TerminalUiInput.cs reduced to <300 LOC
- ✅ 3 component classes created in Input/ subdirectory
- ✅ All tests pass

### Phase 13 (TerminalEmulator): 3 tasks
- ✅ TerminalEmulator.cs reduced to <500 LOC
- ✅ TerminalEmulatorBuilder.cs created with <400 LOC
- ✅ All tests pass

### Phase 14 (TerminalController): 3 tasks
- ✅ TerminalController.cs reduced to <500 LOC
- ✅ TerminalControllerBuilder.cs created with <400 LOC
- ✅ All tests pass

### Phase 15 (SessionManager/ProcessManager): 2 tasks
- ✅ SessionManager.cs reduced to <400 LOC
- ✅ ProcessManager.cs reduced to <400 LOC
- ✅ All tests pass

### Phase 16 (ScreenBufferManager): 1 task
- ✅ ScreenBufferManager.cs reduced to <400 LOC (if feasible)
- ✅ All tests pass

### Overall Success Criteria
- ✅ All production files ≤500 LOC (with rare exceptions for data structures)
- ✅ Facade + operation class pattern maintained throughout
- ✅ All ~1500 tests pass
- ✅ No functionality changes - logic remains identical
- ✅ Git history shows clean task-by-task commits
- ✅ Architecture is navigable for both humans and AI/LLM tools

---

## Testing Protocol

**CRITICAL:** After EVERY task:
1. Run: `.\scripts\dotnet-test.ps1`
2. Check exit code:
   - 0 = success, all tests passed
   - non-zero = failures detected, output shows detailed failure info
3. DO NOT run `dotnet test` directly - it produces massive stdout (>100k lines) that bloats context
4. If tests fail, review failure details in script output, fix issues, re-run tests
5. Only commit when exit code is 0

---

## Commit Message Format

**Subject line (80 chars max):**
```
Task X.Y: Brief description of task
```

**Body (bullet list of changes):**
```
- Create/move/extract specific file(s)
- Update specific file(s) for delegation
- Specific architectural change made
- Confirmation: no functionality changes, all tests pass
```

**Example:**
```
Task 7.1: Extract SgrParamTokenizer from SgrParser

- Create caTTY.Core/Parsing/Sgr/ directory
- Extract parameter tokenization logic to SgrParamTokenizer.cs (~180 LOC)
- Update SgrParser.cs to instantiate tokenizer and delegate tokenization calls
- No functionality changes, all tests pass
```

---

## Notes for Future Task Runners (AI Agents)

1. **Read before acting:** Always read the target file(s) before refactoring to understand current structure
2. **Preserve exact behavior:** Use Edit tool for surgical changes - do NOT rewrite entire files
3. **Test immediately:** Run `.\scripts\dotnet-test.ps1` after each task, before commit
4. **Small commits:** One task = one commit, don't batch multiple tasks
5. **Follow patterns:** Look at recent refactor commits (Phase 1-6) for pattern examples
6. **Ask if stuck:** If logic is unclear or tests fail repeatedly, ask user for clarification
7. **Respect priorities:** Start with Phase 7, proceed sequentially through phases
8. **Document decisions:** If you deviate from plan (e.g., skip Phase 16), document why in commit

---

## Appendix: Current File Locations

**Files to refactor (with line counts):**
- `caTTY.Core/Terminal/TerminalEmulator.cs` (1,199 LOC)
- `caTTY.Core/Parsing/SgrParser.cs` (879 LOC)
- `caTTY.Core/Parsing/CsiParser.cs` (739 LOC)
- `caTTY.Core/Managers/ScreenBufferManager.cs` (601 LOC)
- `caTTY.Core/Terminal/SessionManager.cs` (552 LOC)
- `caTTY.Core/Terminal/ProcessManager.cs` (539 LOC)
- `caTTY.Display/Controllers/TerminalController.cs` (1,130 LOC)
- `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs` (989 LOC)
- `caTTY.Display/Controllers/TerminalUi/TerminalUiFonts.cs` (807 LOC)
- `caTTY.Display/Controllers/TerminalUi/TerminalUiResize.cs` (707 LOC)
- `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs` (582 LOC)

**Test runner:**
- `scripts/dotnet-test.ps1` - ALWAYS use this, never `dotnet test` directly

**Example operation classes to study:**
- `caTTY.Core/Terminal/EmulatorOps/TerminalCursorMovementOps.cs` (173 LOC)
- `caTTY.Core/Terminal/EmulatorOps/TerminalEraseInDisplayOps.cs` (107 LOC)
- `caTTY.Core/Terminal/ParserHandlers/SgrHandler.cs` (148 LOC)

**Example helper classes to study:**
- `caTTY.Core/Terminal/Process/ConPtyOutputPump.cs` (76 LOC)
- `caTTY.Core/Terminal/Sessions/SessionCreator.cs` (79 LOC)
- `caTTY.Core/Parsing/Engine/CsiStateHandler.cs` (137 LOC)

---

## Expected Outcome

**Before continued refactor:**
- 11 files >500 LOC
- Largest file: 1,199 LOC
- Average facade size: ~800 LOC

**After continued refactor:**
- 0 files >500 LOC (excluding unavoidable data structures)
- Largest file: ~500 LOC
- Average facade size: ~350 LOC
- All component classes: <250 LOC
- Clean, navigable architecture for humans and AI

**Total tasks:** ~38 tasks across 10 phases
**Estimated commits:** ~38 commits (one per task)
