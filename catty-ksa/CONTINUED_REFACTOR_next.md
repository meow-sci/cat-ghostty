



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

### Task 16.1: Evaluate ScreenBufferManager split feasibility

**Objective:** Determine if splitting ScreenBufferManager makes sense.

**Steps:**
1. Read `caTTY.Core/Managers/ScreenBufferManager.cs`
2. Analyze methods and categorize:
   - Read operations (queries, getters)
   - Write operations (setters, modifications)
   - Shared/complex operations
3. Evaluate if split is clean or creates awkward coupling
4. Document decision in task commit
5. If split is feasible, proceed with extraction (create `ScreenBufferReader.cs` and `ScreenBufferWriter.cs`)
6. If split is not clean, skip this phase and document reasoning
7. Run tests: `.\scripts\dotnet-test.ps1`
   - ONLY use this to run dotnet tests
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
   - Exit code 0 = success, proceed to commit
   - Exit code non-zero = fix issues
8. Commit with message:
   ```
   Task 16.1: Evaluate and optionally split ScreenBufferManager

   - [If split] Extract read/write operations to separate classes
   - [If split] ScreenBufferManager.cs now <400 LOC
   - [If not split] Document reasoning for keeping unified
   - No functionality changes, all tests pass
   ```

**Target:** ScreenBufferManager.cs <400 LOC (down from 601 LOC) IF split is clean, otherwise remain at 601 LOC