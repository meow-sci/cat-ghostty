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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
   - Run this command EXACTLY, don't bother redirecting or checking head/tail, it is already optimized for minimal output and shows errors when tests fail.  Do not redirect stdout as this slows down tests by 10x.
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
