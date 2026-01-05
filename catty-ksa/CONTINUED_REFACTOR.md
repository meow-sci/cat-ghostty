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

## Phase 7: Extract SgrParser Components

**Goal:** Reduce SgrParser.cs from 879 LOC to <200 LOC by splitting into focused subdirectory components.

**Context:** SGR (Select Graphic Rendition) parsing handles text styling and colors. The parser tokenizes parameters, parses color sequences (8-bit, 24-bit RGB), and constructs messages. All logic should be split without changing behavior.

### Task 7.1: Create Sgr directory and extract SgrParamTokenizer

**Objective:** Extract parameter tokenization logic from SgrParser.cs into new `Sgr/SgrParamTokenizer.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/SgrParser.cs` to understand current structure
2. Create directory `caTTY.Core/Parsing/Sgr/`
3. Create `caTTY.Core/Parsing/Sgr/SgrParamTokenizer.cs` with class `SgrParamTokenizer`
4. Move parameter tokenization logic from SgrParser:
   - Methods that parse/tokenize parameter lists
   - Methods that handle parameter delimiters (`:`, `;`)
   - Parameter validation logic
5. Create appropriate constructor to accept any needed dependencies
6. Update `SgrParser.cs` to instantiate `SgrParamTokenizer` and delegate tokenization calls
7. Verify no functionality changes - all method signatures preserved
8. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = tests failed, review failures shown in output, fix issues
9. Commit with message:
   ```
   Task 7.1: Extract SgrParamTokenizer from SgrParser

   - Create caTTY.Core/Parsing/Sgr/ directory
   - Extract parameter tokenization logic to SgrParamTokenizer.cs
   - Update SgrParser.cs to delegate to tokenizer
   - No functionality changes, all tests pass
   ```

**Target:** SgrParamTokenizer.cs ~150-200 LOC

---

### Task 7.2: Extract SgrColorParsers

**Objective:** Extract color parsing logic (8-bit indexed, 24-bit RGB) into `Sgr/SgrColorParsers.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/SgrParser.cs` to identify color parsing methods
2. Create `caTTY.Core/Parsing/Sgr/SgrColorParsers.cs` with class `SgrColorParsers`
3. Move color parsing logic:
   - 8-bit indexed color parsing (256-color palette)
   - 24-bit RGB color parsing
   - Parameter extraction for foreground/background colors
   - Color validation logic
4. Create constructor to accept dependencies (if any)
5. Update `SgrParser.cs` to instantiate `SgrColorParsers` and delegate color parsing
6. Ensure all color-related method calls updated
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues shown
8. Commit with message:
   ```
   Task 7.2: Extract SgrColorParsers from SgrParser

   - Extract 8-bit and 24-bit color parsing to SgrColorParsers.cs
   - Update SgrParser.cs to delegate color parsing
   - No functionality changes, all tests pass
   ```

**Target:** SgrColorParsers.cs ~150-200 LOC

---

### Task 7.3: Extract SgrAttributeApplier

**Objective:** Extract attribute application logic into `Sgr/SgrAttributeApplier.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/SgrParser.cs` to identify attribute application methods
2. Create `caTTY.Core/Parsing/Sgr/SgrAttributeApplier.cs` with class `SgrAttributeApplier`
3. Move attribute application logic:
   - Methods that apply parsed SGR parameters to state
   - Bold, italic, underline, blink, reverse, etc. attribute application
   - Reset/default handling
4. Create constructor to accept dependencies
5. Update `SgrParser.cs` to instantiate `SgrAttributeApplier` and delegate application
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 7.3: Extract SgrAttributeApplier from SgrParser

   - Extract attribute application logic to SgrAttributeApplier.cs
   - Update SgrParser.cs to delegate attribute operations
   - No functionality changes, all tests pass
   ```

**Target:** SgrAttributeApplier.cs ~150-200 LOC

---

### Task 7.4: Extract SgrMessageFactory

**Objective:** Extract message construction logic into `Sgr/SgrMessageFactory.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/SgrParser.cs` to identify message building methods
2. Create `caTTY.Core/Parsing/Sgr/SgrMessageFactory.cs` with class `SgrMessageFactory`
3. Move message construction logic:
   - Methods that build `SgrMessage` instances from parsed parameters
   - Message validation
   - Default message creation
4. Create constructor with needed dependencies (SgrParamTokenizer, SgrColorParsers, SgrAttributeApplier)
5. Update `SgrParser.cs` to instantiate `SgrMessageFactory` and delegate message building
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 7.4: Extract SgrMessageFactory from SgrParser

   - Extract message construction to SgrMessageFactory.cs
   - Update SgrParser.cs to delegate message building
   - No functionality changes, all tests pass
   ```

**Target:** SgrMessageFactory.cs ~150-200 LOC

---

### Task 7.5: Reduce SgrParser.cs to facade

**Objective:** Finalize SgrParser.cs as minimal facade coordinating extracted components.

**Steps:**
1. Read current `caTTY.Core/Parsing/SgrParser.cs`
2. Ensure all business logic moved to component classes
3. Refactor SgrParser to:
   - Instantiate component classes in constructor
   - Provide simple public API methods that delegate to components
   - Keep only coordination logic
4. Remove any remaining complex logic
5. Add XML documentation comments if not present
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 7.5: Reduce SgrParser to facade pattern

   - Remove remaining business logic from SgrParser.cs
   - Finalize delegation to component classes
   - SgrParser.cs now <200 LOC facade
   - No functionality changes, all tests pass
   ```

**Target:** SgrParser.cs <200 LOC (down from 879 LOC)

---

## Phase 8: Extract CsiParser Components

**Goal:** Reduce CsiParser.cs from 739 LOC to <250 LOC by splitting into focused subdirectory components.

**Context:** CSI (Control Sequence Introducer) parsing handles cursor movement, erasing, scrolling, and device queries. Similar pattern to SgrParser extraction.

### Task 8.1: Create Csi directory and extract CsiTokenizer

**Objective:** Extract parameter tokenization from CsiParser.cs into `Csi/CsiTokenizer.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/CsiParser.cs` to understand tokenization logic
2. Create directory `caTTY.Core/Parsing/Csi/`
3. Create `caTTY.Core/Parsing/Csi/CsiTokenizer.cs` with class `CsiTokenizer`
4. Move tokenization logic:
   - Byte parsing and parameter extraction
   - Intermediate byte handling
   - Parameter list construction
5. Create constructor for dependencies
6. Update `CsiParser.cs` to use `CsiTokenizer`
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 8.1: Extract CsiTokenizer from CsiParser

   - Create caTTY.Core/Parsing/Csi/ directory
   - Extract tokenization logic to CsiTokenizer.cs
   - Update CsiParser.cs to delegate tokenization
   - No functionality changes, all tests pass
   ```

**Target:** CsiTokenizer.cs ~150-200 LOC

---

### Task 8.2: Extract CsiParamParsers

**Objective:** Extract parameter parsing helpers into `Csi/CsiParamParsers.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/CsiParser.cs` to identify parameter parsing methods
2. Create `caTTY.Core/Parsing/Csi/CsiParamParsers.cs` with class `CsiParamParsers`
3. Move parameter parsing helpers:
   - Integer parameter extraction
   - Default value handling
   - Parameter validation
   - Optional parameter parsing
4. Create constructor for dependencies
5. Update `CsiParser.cs` to use `CsiParamParsers`
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 8.2: Extract CsiParamParsers from CsiParser

   - Extract parameter parsing helpers to CsiParamParsers.cs
   - Update CsiParser.cs to delegate parameter operations
   - No functionality changes, all tests pass
   ```

**Target:** CsiParamParsers.cs ~100-150 LOC

---

### Task 8.3: Extract CsiMessageFactory

**Objective:** Extract message construction into `Csi/CsiMessageFactory.cs`.

**Steps:**
1. Read `caTTY.Core/Parsing/CsiParser.cs` to identify message building
2. Create `caTTY.Core/Parsing/Csi/CsiMessageFactory.cs` with class `CsiMessageFactory`
3. Move message construction:
   - Building `CsiMessage` instances from parsed data
   - Message type determination
   - Final character to message mapping
4. Create constructor with dependencies (CsiTokenizer, CsiParamParsers)
5. Update `CsiParser.cs` to use `CsiMessageFactory`
6. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
7. Commit with message:
   ```
   Task 8.3: Extract CsiMessageFactory from CsiParser

   - Extract message construction to CsiMessageFactory.cs
   - Update CsiParser.cs to delegate message building
   - No functionality changes, all tests pass
   ```

**Target:** CsiMessageFactory.cs ~200-250 LOC

---

### Task 8.4: Reduce CsiParser.cs to facade

**Objective:** Finalize CsiParser.cs as minimal facade.

**Steps:**
1. Read current `caTTY.Core/Parsing/CsiParser.cs`
2. Ensure all business logic moved to component classes
3. Refactor to simple delegation pattern
4. Keep only public API and coordination
5. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
6. Commit with message:
   ```
   Task 8.4: Reduce CsiParser to facade pattern

   - Remove remaining business logic from CsiParser.cs
   - Finalize delegation to component classes
   - CsiParser.cs now <250 LOC facade
   - No functionality changes, all tests pass
   ```

**Target:** CsiParser.cs <250 LOC (down from 739 LOC)



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
