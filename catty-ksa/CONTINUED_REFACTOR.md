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

## Phase 9: Extract TerminalUiSettingsPanel Menu Renderers

**Goal:** Reduce TerminalUiSettingsPanel.cs from 989 LOC to <200 LOC by extracting individual menu renderers.

**Context:** The settings panel contains ImGui menu rendering for File, Edit, Sessions, Font, Theme, and Settings menus. Each should be extracted to its own renderer class.

### Task 9.1: Extract FileMenuRenderer

**Objective:** Extract File menu rendering into `TerminalUi/Menus/FileMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify all File menu rendering code (likely in a method like `RenderFileMenu()`)
3. Create directory `caTTY.Display/Controllers/TerminalUi/Menus/`
4. Create `caTTY.Display/Controllers/TerminalUi/Menus/FileMenuRenderer.cs` with class `FileMenuRenderer`
5. Move File menu logic:
   - Menu item rendering (New, Open, Save, Exit, etc.)
   - Associated action handlers
   - File dialog interactions
6. Create constructor accepting necessary dependencies (TerminalController reference, settings, etc.)
7. Add `public void Render()` method as entry point
8. Update `TerminalUiSettingsPanel.cs` to:
   - Instantiate `FileMenuRenderer` in constructor
   - Call `_fileMenuRenderer.Render()` from appropriate location
9. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
10. Commit with message:
    ```
    Task 9.1: Extract FileMenuRenderer from TerminalUiSettingsPanel

    - Create Menus/ subdirectory
    - Extract File menu rendering to FileMenuRenderer.cs
    - Update TerminalUiSettingsPanel.cs to delegate file menu
    - No functionality changes, all tests pass
    ```

**Target:** FileMenuRenderer.cs ~100-150 LOC

---

### Task 9.2: Extract EditMenuRenderer

**Objective:** Extract Edit menu rendering into `TerminalUi/Menus/EditMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify Edit menu code (Copy, Paste, Select All, Clear, etc.)
3. Create `caTTY.Display/Controllers/TerminalUi/Menus/EditMenuRenderer.cs` with class `EditMenuRenderer`
4. Move Edit menu logic and action handlers
5. Create constructor with dependencies
6. Add `public void Render()` method
7. Update `TerminalUiSettingsPanel.cs` to delegate
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 9.2: Extract EditMenuRenderer from TerminalUiSettingsPanel

   - Extract Edit menu rendering to EditMenuRenderer.cs
   - Update TerminalUiSettingsPanel.cs to delegate edit menu
   - No functionality changes, all tests pass
   ```

**Target:** EditMenuRenderer.cs ~80-120 LOC

---

### Task 9.3: Extract SessionsMenuRenderer

**Objective:** Extract Sessions menu rendering into `TerminalUi/Menus/SessionsMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify Sessions menu code (New Session, Switch Session, Close Session, etc.)
3. Create `caTTY.Display/Controllers/TerminalUi/Menus/SessionsMenuRenderer.cs` with class `SessionsMenuRenderer`
4. Move Sessions menu logic and session management UI
5. Create constructor with dependencies
6. Add `public void Render()` method
7. Update `TerminalUiSettingsPanel.cs` to delegate
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 9.3: Extract SessionsMenuRenderer from TerminalUiSettingsPanel

   - Extract Sessions menu rendering to SessionsMenuRenderer.cs
   - Update TerminalUiSettingsPanel.cs to delegate sessions menu
   - No functionality changes, all tests pass
   ```

**Target:** SessionsMenuRenderer.cs ~100-150 LOC

---

### Task 9.4: Extract FontMenuRenderer

**Objective:** Extract Font menu rendering into `TerminalUi/Menus/FontMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify Font menu code (Font selection, size adjustment, style options)
3. Create `caTTY.Display/Controllers/TerminalUi/Menus/FontMenuRenderer.cs` with class `FontMenuRenderer`
4. Move Font menu UI logic
5. Create constructor with dependencies
6. Add `public void Render()` method
7. Update `TerminalUiSettingsPanel.cs` to delegate
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 9.4: Extract FontMenuRenderer from TerminalUiSettingsPanel

   - Extract Font menu rendering to FontMenuRenderer.cs
   - Update TerminalUiSettingsPanel.cs to delegate font menu
   - No functionality changes, all tests pass
   ```

**Target:** FontMenuRenderer.cs ~100-150 LOC

---

### Task 9.5: Extract ThemeMenuRenderer

**Objective:** Extract Theme menu rendering into `TerminalUi/Menus/ThemeMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify Theme menu code (Theme selection, color customization)
3. Create `caTTY.Display/Controllers/TerminalUi/Menus/ThemeMenuRenderer.cs` with class `ThemeMenuRenderer`
4. Move Theme menu logic
5. Create constructor with dependencies
6. Add `public void Render()` method
7. Update `TerminalUiSettingsPanel.cs` to delegate
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 9.5: Extract ThemeMenuRenderer from TerminalUiSettingsPanel

   - Extract Theme menu rendering to ThemeMenuRenderer.cs
   - Update TerminalUiSettingsPanel.cs to delegate theme menu
   - No functionality changes, all tests pass
   ```

**Target:** ThemeMenuRenderer.cs ~100-150 LOC

---

### Task 9.6: Extract GeneralSettingsMenuRenderer

**Objective:** Extract general Settings menu rendering into `TerminalUi/Menus/GeneralSettingsMenuRenderer.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Identify general Settings menu code (scrollback, cursor, misc options)
3. Create `caTTY.Display/Controllers/TerminalUi/Menus/GeneralSettingsMenuRenderer.cs` with class `GeneralSettingsMenuRenderer`
4. Move Settings menu logic
5. Create constructor with dependencies
6. Add `public void Render()` method
7. Update `TerminalUiSettingsPanel.cs` to delegate
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 9.6: Extract GeneralSettingsMenuRenderer from TerminalUiSettingsPanel

   - Extract Settings menu rendering to GeneralSettingsMenuRenderer.cs
   - Update TerminalUiSettingsPanel.cs to delegate settings menu
   - No functionality changes, all tests pass
   ```

**Target:** GeneralSettingsMenuRenderer.cs ~100-150 LOC

---

### Task 9.7: Reduce TerminalUiSettingsPanel.cs to coordinator

**Objective:** Finalize TerminalUiSettingsPanel.cs as minimal coordinator of menu renderers.

**Steps:**
1. Read current `caTTY.Display/Controllers/TerminalUi/TerminalUiSettingsPanel.cs`
2. Ensure all menu rendering logic moved to renderer classes
3. Refactor to:
   - Instantiate all menu renderers in constructor
   - Coordinate menu bar layout
   - Call appropriate renderer methods
   - Handle only top-level coordination
4. Remove any remaining complex rendering logic
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 9.7: Reduce TerminalUiSettingsPanel to coordinator

   - Remove remaining rendering logic from TerminalUiSettingsPanel.cs
   - Finalize delegation to menu renderers
   - TerminalUiSettingsPanel.cs now <200 LOC coordinator
   - No functionality changes, all tests pass
   ```

**Target:** TerminalUiSettingsPanel.cs <200 LOC (down from 989 LOC)



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
