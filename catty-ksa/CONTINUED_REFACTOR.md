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

## Phase 12: Extract TerminalUiInput Components

**Goal:** Reduce TerminalUiInput.cs from 582 LOC to <300 LOC by splitting input handling concerns.

**Context:** Input handling includes keyboard input processing, special key handling, and focus management.

### Task 12.1: Extract KeyboardInputHandler

**Objective:** Extract keyboard input processing into `TerminalUi/Input/KeyboardInputHandler.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
2. Identify keyboard input processing (key events, character input, modifiers)
3. Create directory `caTTY.Display/Controllers/TerminalUi/Input/`
4. Create `caTTY.Display/Controllers/TerminalUi/Input/KeyboardInputHandler.cs` with class `KeyboardInputHandler`
5. Move keyboard input logic
6. Create constructor with dependencies
7. Update `TerminalUiInput.cs` to delegate keyboard operations
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
9. Commit with message:
   ```
   Task 12.1: Extract KeyboardInputHandler from TerminalUiInput

   - Create Input/ subdirectory
   - Extract keyboard input to KeyboardInputHandler.cs
   - Update TerminalUiInput.cs to delegate keyboard operations
   - No functionality changes, all tests pass
   ```

**Target:** KeyboardInputHandler.cs ~150-200 LOC

---

### Task 12.2: Extract SpecialKeyHandler

**Objective:** Extract special key handling into `TerminalUi/Input/SpecialKeyHandler.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
2. Identify special key handling (function keys, navigation keys, control sequences)
3. Create `caTTY.Display/Controllers/TerminalUi/Input/SpecialKeyHandler.cs` with class `SpecialKeyHandler`
4. Move special key logic
5. Create constructor with dependencies
6. Update `TerminalUiInput.cs` to delegate special key operations
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 12.2: Extract SpecialKeyHandler from TerminalUiInput

   - Extract special key handling to SpecialKeyHandler.cs
   - Update TerminalUiInput.cs to delegate special key operations
   - No functionality changes, all tests pass
   ```

**Target:** SpecialKeyHandler.cs ~100-150 LOC

---

### Task 12.3: Extract InputFocusManager

**Objective:** Extract focus management into `TerminalUi/Input/InputFocusManager.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
2. Identify focus management (focus detection, focus events, active state)
3. Create `caTTY.Display/Controllers/TerminalUi/Input/InputFocusManager.cs` with class `InputFocusManager`
4. Move focus logic
5. Create constructor with dependencies
6. Update `TerminalUiInput.cs` to delegate focus operations
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 12.3: Extract InputFocusManager from TerminalUiInput

   - Extract focus management to InputFocusManager.cs
   - Update TerminalUiInput.cs to delegate focus operations
   - No functionality changes, all tests pass
   ```

**Target:** InputFocusManager.cs ~80-120 LOC

---

### Task 12.4: Reduce TerminalUiInput.cs to coordinator

**Objective:** Finalize TerminalUiInput.cs as coordinator of input components.

**Steps:**
1. Read current `caTTY.Display/Controllers/TerminalUi/TerminalUiInput.cs`
2. Ensure all specialized logic moved to component classes
3. Refactor to coordination pattern
4. Keep only high-level input API
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 12.4: Reduce TerminalUiInput to coordinator

   - Remove remaining specialized logic
   - Finalize delegation to input components
   - TerminalUiInput.cs now <300 LOC coordinator
   - No functionality changes, all tests pass
   ```

**Target:** TerminalUiInput.cs <300 LOC (down from 582 LOC)

---

## Phase 13: Extract TerminalEmulator Initialization

**Goal:** Reduce TerminalEmulator.cs from 1,199 LOC to <500 LOC by extracting initialization complexity.

**Context:** TerminalEmulator constructor is ~400 LOC with 39+ operation class instantiations. This should use builder pattern to separate construction from business logic.

### Task 13.1: Create TerminalEmulatorBuilder

**Objective:** Extract initialization into `Terminal/TerminalEmulatorBuilder.cs`.

**Steps:**
1. Read `caTTY.Core/Terminal/TerminalEmulator.cs` constructor
2. Create `caTTY.Core/Terminal/TerminalEmulatorBuilder.cs` with class `TerminalEmulatorBuilder`
3. Create builder methods for initialization stages:
   - `BuildManagers()` - create all manager instances
   - `BuildOperations()` - create all operation instances
   - `BuildParserHandlers()` - create handler instances
   - `BuildEmulator()` - final assembly
4. Move constructor logic to builder methods
5. Update `TerminalEmulator.cs`:
   - Add private constructor accepting initialized components
   - Add static factory method `Create()` that uses builder
6. Update all instantiation sites to use `TerminalEmulator.Create(...)`
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 13.1: Extract TerminalEmulatorBuilder

   - Create TerminalEmulatorBuilder.cs with staged initialization
   - Move constructor logic to builder
   - Add TerminalEmulator.Create() factory method
   - Update instantiation sites to use factory
   - No functionality changes, all tests pass
   ```

**Target:** TerminalEmulatorBuilder.cs ~300-400 LOC, TerminalEmulator constructor ~50 LOC

---

### Task 13.2: Extract operation instantiation methods

**Objective:** Further reduce builder by splitting operation instantiation.

**Steps:**
1. Read `caTTY.Core/Terminal/TerminalEmulatorBuilder.cs`
2. If `BuildOperations()` is >150 LOC, split into:
   - `BuildCursorOps()` - cursor-related operations
   - `BuildEraseOps()` - erase operations
   - `BuildScrollOps()` - scroll operations
   - `BuildModeOps()` - mode operations
   - `BuildOscOps()` - OSC operations
   - `BuildMiscOps()` - remaining operations
3. Call sub-methods from `BuildOperations()`
4. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
5. Commit with message:
   ```
   Task 13.2: Split TerminalEmulatorBuilder operation methods

   - Split BuildOperations into category methods
   - Reduce complexity of builder class
   - No functionality changes, all tests pass
   ```

**Target:** Each build method <100 LOC

---

### Task 13.3: Reduce TerminalEmulator.cs to core logic

**Objective:** Ensure TerminalEmulator.cs contains only delegation and essential business logic.

**Steps:**
1. Read current `caTTY.Core/Terminal/TerminalEmulator.cs`
2. Verify all initialization moved to builder
3. Ensure only contains:
   - Private constructor
   - Static factory method
   - Field declarations
   - Public API methods (simple delegation)
   - Essential coordination logic (if any)
4. Remove any remaining initialization complexity
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 13.3: Finalize TerminalEmulator as clean facade

   - Verify all initialization in builder
   - TerminalEmulator.cs now <500 LOC
   - Clean delegation pattern throughout
   - No functionality changes, all tests pass
   ```

**Target:** TerminalEmulator.cs <500 LOC (down from 1,199 LOC)

---

## Phase 14: Extract TerminalController Initialization

**Goal:** Reduce TerminalController.cs from 1,130 LOC to <500 LOC using builder pattern.

**Context:** Similar to TerminalEmulator, TerminalController has large constructor with many UI subsystem instantiations.

### Task 14.1: Create TerminalControllerBuilder

**Objective:** Extract initialization into `Controllers/TerminalControllerBuilder.cs`.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalController.cs` constructor
2. Create `caTTY.Display/Controllers/TerminalControllerBuilder.cs` with class `TerminalControllerBuilder`
3. Create builder methods:
   - `BuildUiSubsystems()` - UI component instantiation
   - `BuildInputHandlers()` - input handler instantiation
   - `BuildRenderComponents()` - rendering component instantiation
   - `BuildController()` - final assembly
4. Move constructor logic to builder
5. Update `TerminalController.cs`:
   - Add private constructor accepting initialized components
   - Add static factory method `Create()`
6. Update instantiation sites to use factory
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 14.1: Extract TerminalControllerBuilder

   - Create TerminalControllerBuilder.cs with staged initialization
   - Move constructor logic to builder
   - Add TerminalController.Create() factory method
   - Update instantiation sites
   - No functionality changes, all tests pass
   ```

**Target:** TerminalControllerBuilder.cs ~300-400 LOC, TerminalController constructor ~50 LOC

---

### Task 14.2: Split TerminalControllerBuilder methods if needed

**Objective:** Ensure builder methods are <150 LOC each.

**Steps:**
1. Read `caTTY.Display/Controllers/TerminalControllerBuilder.cs`
2. If any build method >150 LOC, split into subcategories
3. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
4. Commit with message:
   ```
   Task 14.2: Split TerminalControllerBuilder methods

   - Split large build methods into subcategories
   - Reduce builder complexity
   - No functionality changes, all tests pass
   ```

**Target:** Each build method <150 LOC

---

### Task 14.3: Reduce TerminalController.cs to core logic

**Objective:** Ensure TerminalController.cs is clean facade.

**Steps:**
1. Read current `caTTY.Display/Controllers/TerminalController.cs`
2. Verify all initialization in builder
3. Ensure only delegation and essential coordination
4. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
5. Commit with message:
   ```
   Task 14.3: Finalize TerminalController as clean facade

   - Verify all initialization in builder
   - TerminalController.cs now <500 LOC
   - Clean delegation pattern
   - No functionality changes, all tests pass
   ```

**Target:** TerminalController.cs <500 LOC (down from 1,130 LOC)

---

## Phase 15: Further Reduce SessionManager and ProcessManager

**Goal:** Reduce SessionManager.cs from 552 LOC to <400 LOC and ProcessManager.cs from 539 LOC to <400 LOC through additional delegation.

**Context:** Both already have helper classes in subdirectories but still contain logic that could be further delegated.

### Task 15.1: Review and delegate SessionManager logic

**Objective:** Identify and extract remaining logic from SessionManager.cs.

**Steps:**
1. Read `caTTY.Core/Terminal/SessionManager.cs`
2. Read existing helper classes in `caTTY.Core/Terminal/Sessions/`
3. Identify remaining complex logic not yet delegated
4. Extract into new helpers or enhance existing ones:
   - Possibly `SessionValidator.cs` for validation logic
   - Possibly `SessionStateManager.cs` for state tracking
5. Update SessionManager to delegate additional operations
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 15.1: Further delegate SessionManager logic

   - Extract remaining logic to helper classes
   - SessionManager.cs now <400 LOC
   - No functionality changes, all tests pass
   ```

**Target:** SessionManager.cs <400 LOC (down from 552 LOC)

---

### Task 15.2: Review and delegate ProcessManager logic

**Objective:** Identify and extract remaining logic from ProcessManager.cs.

**Steps:**
1. Read `caTTY.Core/Terminal/ProcessManager.cs`
2. Read existing helper classes in `caTTY.Core/Terminal/Process/`
3. Identify remaining complex logic not yet delegated
4. Extract into new helpers or enhance existing ones:
   - Possibly `ProcessStateManager.cs` for state tracking
   - Possibly `ProcessErrorHandler.cs` for error handling
5. Update ProcessManager to delegate additional operations
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 15.2: Further delegate ProcessManager logic

   - Extract remaining logic to helper classes
   - ProcessManager.cs now <400 LOC
   - No functionality changes, all tests pass
   ```

**Target:** ProcessManager.cs <400 LOC (down from 539 LOC)

---

## Phase 16: Split ScreenBufferManager (Optional)

**Goal:** If feasible, reduce ScreenBufferManager.cs from 601 LOC to <400 LOC by splitting read vs write concerns.

**Context:** Screen buffer operations can be conceptually divided into read operations (getting cell contents, querying state) and write operations (setting cells, scrolling). This is optional if the split doesn't make semantic sense.

### Task 16.1: Evaluate ScreenBufferManager split feasibility (COMPLETED - KEPT UNIFIED)

**Objective:** Determine if splitting ScreenBufferManager makes sense.

**Status:** COMPLETED - Decision made to keep unified at 601 LOC

**Analysis Results:**
- Read operations: 5 methods (Width, Height, GetCell, GetRow, CopyTo)
- Write operations: 3 methods (SetCell, Clear, ClearRegion)
- Complex operations (read+write): 12 methods
  * Scroll operations (4): ScrollUp, ScrollDown, ScrollUpInRegion, ScrollDownInRegion
  * Line manipulation (2): InsertLinesInRegion, DeleteLinesInRegion
  * Character manipulation (3): InsertCharactersInLine, DeleteCharactersInLine, EraseCharactersInLine
  * Configuration (2): SetScrollbackIntegration, Resize

**Decision Reasoning:**
1. Heavy interdependencies - 60% of methods (12/20) require both read and write operations
2. All complex operations follow atomic pattern: read from position A → write to position B → clear/fill remaining
3. Single Responsibility Principle already satisfied - class has cohesive screen buffer management API
4. 601 LOC is reasonable for this complexity level
5. No natural seam for clean split - separating would create awkward cross-class coupling
6. Splitting would harm rather than help code clarity and maintainability

**Outcome:** ScreenBufferManager.cs remains at 601 LOC as a justified exception to <500 LOC guideline

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
- ✅ Task 16.1 completed - evaluated and decided to keep unified at 601 LOC
- ✅ Decision: Split not feasible due to heavy interdependencies (12/20 methods require both read+write)
- ✅ All tests pass

### Overall Success Criteria
- ✅ All production files ≤500 LOC (with justified exceptions: ScreenBufferManager at 601 LOC)
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
