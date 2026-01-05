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