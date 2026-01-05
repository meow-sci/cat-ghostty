

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