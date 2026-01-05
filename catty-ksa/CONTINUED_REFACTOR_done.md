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

1