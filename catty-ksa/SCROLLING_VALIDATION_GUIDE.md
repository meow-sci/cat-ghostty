# Scrolling Functionality Validation Guide

This guide provides comprehensive tests to validate scrolling functionality in both the TestApp and GameMod applications.

## Prerequisites

1. **Build the solution**: Ensure all projects compile successfully
2. **TestApp**: Run from `catty-ksa/caTTY.TestApp/` directory
3. **GameMod**: Load in KSA game and press F12 to toggle terminal

## Test Categories

### 1. Basic Scrolling Tests

#### Test 1.1: Long Command Output Scrolling
**Purpose**: Verify that long command output scrolls correctly and content is preserved in scrollback.

**Steps**:
1. Open terminal (TestApp or GameMod)
2. Run command that produces long output:
   ```bash
   # Windows
   dir /s C:\Windows\System32
   
   # Or generate numbered lines
   for /L %i in (1,1,100) do echo Line %i with some content to test scrolling behavior
   ```
3. **Expected**: 
   - Content scrolls up as new lines appear
   - Terminal stays at bottom (auto-scroll enabled)
   - Old content is preserved in scrollback buffer

#### Test 1.2: Manual Scrollback Navigation
**Purpose**: Test viewport navigation and auto-scroll behavior.

**Steps**:
1. After running long command from Test 1.1
2. Scroll up using mouse wheel or Page Up
3. **Expected**:
   - Can scroll through historical content
   - Auto-scroll is disabled when scrolled up
   - New output doesn't yank viewport while reviewing history
4. Scroll back to bottom
5. **Expected**:
   - Auto-scroll re-enabled
   - New output causes terminal to stay at bottom

#### Test 1.3: Scrollback Capacity Management
**Purpose**: Verify scrollback buffer manages capacity correctly.

**Steps**:
1. Generate more output than scrollback capacity (default 1000 lines):
   ```bash
   for /L %i in (1,1,1200) do echo Line %i - Testing scrollback capacity management
   ```
2. **Expected**:
   - Oldest lines are removed when capacity exceeded
   - Buffer maintains FIFO (First In, First Out) ordering
   - No memory leaks or performance degradation

### 2. Screen Buffer Resize Tests

#### Test 2.1: Resize During Normal Operation
**Purpose**: Validate resize handling preserves content appropriately.

**Steps**:
1. Fill terminal with content:
   ```bash
   for /L %i in (1,1,20) do echo Line %i with content that should be preserved during resize
   ```
2. Resize terminal window (drag corners in TestApp, or resize game window in GameMod)
3. **Expected**:
   - Content is preserved where possible
   - Cursor position remains valid
   - No crashes or visual artifacts
   - Scrollback content remains accessible

#### Test 2.2: Resize While Scrolled Up
**Purpose**: Test resize behavior when viewport is not at bottom.

**Steps**:
1. Generate content and scroll up to view history
2. Resize terminal window
3. **Expected**:
   - Viewport position is maintained relative to content
   - Can still navigate scrollback after resize
   - Auto-scroll state is preserved

### 3. Advanced Scrolling Features

#### Test 3.1: Scroll Region Operations
**Purpose**: Test scroll region (DECSTBM) functionality.

**Steps**:
1. Set scroll region using escape sequence:
   ```bash
   # Set scroll region from line 5 to line 15
   echo -e "\033[5;15r"
   ```
2. Generate content that should scroll within region
3. **Expected**:
   - Only content within scroll region moves
   - Content outside region remains static
   - Cursor movement respects scroll region boundaries

#### Test 3.2: Alternate Screen Buffer
**Purpose**: Verify alternate screen doesn't affect scrollback.

**Steps**:
1. Generate content in primary screen
2. Switch to alternate screen (run `vim` or similar full-screen app)
3. Generate content in alternate screen
4. Exit back to primary screen
5. **Expected**:
   - Primary screen content is restored
   - Scrollback only contains primary screen content
   - No alternate screen content in scrollback

### 4. Performance and Stress Tests

#### Test 4.1: High-Volume Output
**Purpose**: Test performance with rapid, high-volume output.

**Steps**:
1. Generate rapid output:
   ```bash
   # Fast output generation
   for /L %i in (1,1,1000) do echo Line %i - High volume test with timestamp %time%
   ```
2. **Expected**:
   - Terminal remains responsive
   - No dropped content
   - Smooth scrolling animation
   - Memory usage remains stable

#### Test 4.2: Mixed Content Types
**Purpose**: Test scrolling with various content types.

**Steps**:
1. Generate mixed content:
   ```bash
   # Mix of short and long lines, special characters
   echo Short line
   echo This is a very long line that might wrap depending on terminal width and should test horizontal scrolling behavior
   echo Line with special chars: àáâãäåæçèéêë ñòóôõö ùúûüý
   echo -e "Line with colors: \033[31mRed\033[32mGreen\033[34mBlue\033[0mNormal"
   ```
2. **Expected**:
   - All content types scroll correctly
   - Colors and formatting preserved in scrollback
   - Wide characters handled properly

### 5. Integration Tests

#### Test 5.1: TestApp Specific Tests
**Purpose**: Validate TestApp-specific scrolling behavior.

**Steps**:
1. Run TestApp from correct directory
2. Verify fonts load correctly
3. Test all scrolling scenarios above
4. **Expected**:
   - Consistent behavior with GameMod
   - Proper font rendering during scrolling
   - No ImGui-specific issues

#### Test 5.2: GameMod Specific Tests
**Purpose**: Validate GameMod-specific scrolling behavior.

**Steps**:
1. Load GameMod in KSA
2. Toggle terminal with F12
3. Test all scrolling scenarios above
4. **Expected**:
   - Terminal integrates properly with game
   - No interference with game input/rendering
   - Proper resource cleanup on toggle

### 6. Error Conditions and Edge Cases

#### Test 6.1: Empty Terminal Scrolling
**Purpose**: Test scrolling behavior with minimal content.

**Steps**:
1. Start with empty terminal
2. Try scrolling up/down
3. **Expected**:
   - No crashes or errors
   - Graceful handling of empty scrollback
   - Proper viewport state management

#### Test 6.2: Single Line Content
**Purpose**: Test edge case with minimal content.

**Steps**:
1. Enter single line of content
2. Try various scrolling operations
3. **Expected**:
   - Proper handling of minimal content
   - Viewport behaves correctly
   - No visual artifacts

## Validation Checklist

Use this checklist to track validation progress:

### Basic Functionality
- [ ] Long command output scrolls correctly
- [ ] Content preserved in scrollback buffer
- [ ] Manual scrollback navigation works
- [ ] Auto-scroll enables/disables correctly
- [ ] Viewport doesn't yank during history review
- [ ] Scrollback capacity management works
- [ ] FIFO ordering maintained

### Resize Handling
- [ ] Content preserved during resize
- [ ] Cursor position remains valid after resize
- [ ] Scrollback accessible after resize
- [ ] Viewport position maintained when scrolled up
- [ ] Auto-scroll state preserved during resize

### Advanced Features
- [ ] Scroll regions work correctly
- [ ] Alternate screen doesn't affect scrollback
- [ ] Mixed content types scroll properly
- [ ] Colors/formatting preserved in scrollback
- [ ] Wide characters handled correctly

### Performance
- [ ] High-volume output handled smoothly
- [ ] Terminal remains responsive during rapid output
- [ ] Memory usage remains stable
- [ ] No dropped content during stress tests

### Application-Specific
- [ ] TestApp scrolling works correctly
- [ ] GameMod scrolling works correctly
- [ ] Font rendering during scrolling is proper
- [ ] No integration issues with game/ImGui

### Error Handling
- [ ] Empty terminal scrolling handled gracefully
- [ ] Single line content edge cases work
- [ ] No crashes during edge case scenarios
- [ ] Proper error recovery

## Known Issues and Limitations

Document any issues found during validation:

### Issues Found
- [ ] Issue 1: [Description]
- [ ] Issue 2: [Description]
- [ ] Issue 3: [Description]

### Limitations
- [ ] Limitation 1: [Description]
- [ ] Limitation 2: [Description]

## Validation Results

### TestApp Results
- **Date**: [Date of testing]
- **Tester**: [Name]
- **Overall Status**: [PASS/FAIL/PARTIAL]
- **Notes**: [Additional observations]

### GameMod Results
- **Date**: [Date of testing]
- **Tester**: [Name]
- **Overall Status**: [PASS/FAIL/PARTIAL]
- **Notes**: [Additional observations]

## Recommendations

Based on validation results, document any recommendations for improvements or fixes.