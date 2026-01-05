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


## Task 4.16: Extract Delete Characters Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalDeleteCharsOps.cs`
2. Move: `DeleteCharactersInLine(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.16: Extract delete characters operations

- Created TerminalDeleteCharsOps.cs
- Extracted DeleteCharactersInLine method
- All tests pass"
```

## Task 4.17: Extract Erase Characters Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalEraseCharsOps.cs`
2. Move: `EraseCharactersInLine(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.17: Extract erase characters operations

- Created TerminalEraseCharsOps.cs
- Extracted EraseCharactersInLine method
- All tests pass"
```

## Task 4.18: Extract Insert Mode Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalInsertModeOps.cs`
2. Move:
   - `SetInsertMode(bool enabled)`
   - `ShiftCharactersRight(int count)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.18: Extract insert mode operations

- Created TerminalInsertModeOps.cs
- Extracted SetInsertMode method
- Extracted ShiftCharactersRight method
- All tests pass"
```

## Task 4.19: Extract DEC Mode Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalDecModeOps.cs`
2. Move: `SetDecMode(int mode, bool enabled)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.19: Extract DEC mode operations

- Created TerminalDecModeOps.cs
- Extracted SetDecMode method
- All tests pass"
```

## Task 4.20: Extract Alternate Screen Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalAlternateScreenOps.cs`
2. Move: `HandleAlternateScreenMode(bool enabled)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.20: Extract alternate screen operations

- Created TerminalAlternateScreenOps.cs
- Extracted HandleAlternateScreenMode method
- All tests pass"
```

## Task 4.21: Extract Private Modes Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalPrivateModesOps.cs`
2. Move:
   - `SavePrivateModes(int[] modes)`
   - `RestorePrivateModes(int[] modes)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.21: Extract private modes operations

- Created TerminalPrivateModesOps.cs
- Extracted SavePrivateModes method
- Extracted RestorePrivateModes method
- All tests pass"
```

## Task 4.22: Extract Bracketed Paste Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalBracketedPasteOps.cs`
2. Move:
   - `WrapPasteContent(string pasteContent)`
   - `WrapPasteContent(ReadOnlySpan<char> pasteContent)`
   - `IsBracketedPasteModeEnabled()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.22: Extract bracketed paste operations

- Created TerminalBracketedPasteOps.cs
- Extracted WrapPasteContent(string) method
- Extracted WrapPasteContent(ReadOnlySpan<char>) method
- Extracted IsBracketedPasteModeEnabled method
- All tests pass"
```

## Task 4.23: Extract OSC Title/Icon Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscTitleIconOps.cs`
2. Move:
   - `SetWindowTitle(string title)`
   - `SetIconName(string name)`
   - `SetTitleAndIcon(string text)`
   - `GetWindowTitle()`
   - `GetIconName()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.23: Extract OSC title/icon operations

- Created TerminalOscTitleIconOps.cs
- Extracted SetWindowTitle, SetIconName, SetTitleAndIcon
- Extracted GetWindowTitle, GetIconName
- All tests pass"
```

## Task 4.24: Extract OSC Window Manipulation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscWindowManipulationOps.cs`
2. Move: `HandleWindowManipulation(int operation, int[] params)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.24: Extract OSC window manipulation operations

- Created TerminalOscWindowManipulationOps.cs
- Extracted HandleWindowManipulation method
- All tests pass"
```

## Task 4.25: Extract OSC Clipboard Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscClipboardOps.cs`
2. Move:
   - `HandleClipboard(string data)`
   - `OnClipboardRequest()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.25: Extract OSC clipboard operations

- Created TerminalOscClipboardOps.cs
- Extracted HandleClipboard method
- Extracted OnClipboardRequest method
- All tests pass"
```

## Task 4.26: Extract OSC Hyperlink Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscHyperlinkOps.cs`
2. Move: `HandleHyperlink(string params, string uri)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.26: Extract OSC hyperlink operations

- Created TerminalOscHyperlinkOps.cs
- Extracted HandleHyperlink method
- All tests pass"
```

## Task 4.27: Extract OSC Color Query Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalOscColorQueryOps.cs`
2. Move:
   - `GetCurrentForegroundColor()`
   - `GetCurrentBackgroundColor()`
   - `GetDefaultForegroundColor()`
   - `GetDefaultBackgroundColor()`
   - `GetNamedColorRgb(int colorIndex)`
   - `GetIndexedColorRgb(int index)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.27: Extract OSC color query operations

- Created TerminalOscColorQueryOps.cs
- Extracted color query methods
- Extracted palette helper methods
- All tests pass"
```

## Task 4.28: Extract Charset Designation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCharsetDesignationOps.cs`
2. Move: `DesignateCharacterSet(char designator, char charset)`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.28: Extract charset designation operations

- Created TerminalCharsetDesignationOps.cs
- Extracted DesignateCharacterSet method
- All tests pass"
```

## Task 4.29: Extract Charset Translation Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalCharsetTranslationOps.cs`
2. Move:
   - `HandleShiftIn()`
   - `HandleShiftOut()`
   - `TranslateCharacter(char c)`
   - `GenerateCharacterSetQueryResponse()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.29: Extract charset translation operations

- Created TerminalCharsetTranslationOps.cs
- Extracted HandleShiftIn, HandleShiftOut
- Extracted TranslateCharacter
- Extracted GenerateCharacterSetQueryResponse
- All tests pass"
```

## Task 4.30: Extract Line Feed Operations

**Steps:**
1. Create file: `caTTY.Core/Terminal/EmulatorOps/TerminalLineFeedOps.cs`
2. Move: `HandleLineFeed()`

**Validation:**
```bash
dotnet build caTTY.Core
.\scripts\dotnet-test.ps1
```

**Git Commit:**
```bash
git add .
git commit -m "Task 4.30: Extract line feed operations

- Created TerminalLineFeedOps.cs
- Extracted HandleLineFeed method
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
