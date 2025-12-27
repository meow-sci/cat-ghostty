# caTTY Color and Styling Validation Guide

## Overview

This document provides comprehensive testing procedures for validating color and text styling functionality in both the caTTY TestApp and GameMod implementations. It covers SGR (Select Graphic Rendition) sequences, color rendering, and text styling validation.

## Prerequisites

- caTTY TestApp built and functional
- caTTY GameMod built and loaded in KSA
- Windows terminal with ANSI support (Windows Terminal, PowerShell 7+, or ConEmu)
- Access to shell commands that produce colored output

## Testing Methodology

### Phase 1: Console ANSI Support Validation

The TestApp includes built-in console color testing to verify the host console supports ANSI escape sequences.

**Expected Output:**
- Standard colors (8 colors): Black, Red, Green, Yellow, Blue, Magenta, Cyan, White
- Bright colors (8 colors): Bright variants of standard colors
- Background colors: Same colors applied as backgrounds
- 256-color samples: Selected colors from the extended palette
- RGB color samples: True color examples
- Text styling: Bold, Italic, Underline, Strikethrough

### Phase 2: Terminal Emulator Color Processing

Test the terminal emulator's ability to parse and apply SGR sequences correctly.

## Test Commands and Expected Results

### Basic Color Tests

#### 1. Standard 8-Color Foreground
```bash
# Test standard foreground colors (30-37)
echo -e "\033[30mBlack\033[0m \033[31mRed\033[0m \033[32mGreen\033[0m \033[33mYellow\033[0m"
echo -e "\033[34mBlue\033[0m \033[35mMagenta\033[0m \033[36mCyan\033[0m \033[37mWhite\033[0m"
```

**Expected Result:** Each color name should appear in its corresponding color.

#### 2. Standard 8-Color Background
```bash
# Test standard background colors (40-47)
echo -e "\033[40m Black \033[0m \033[41m Red \033[0m \033[42m Green \033[0m \033[43m Yellow \033[0m"
echo -e "\033[44m Blue \033[0m \033[45m Magenta \033[0m \033[46m Cyan \033[0m \033[47m White \033[0m"
```

**Expected Result:** Each text should appear with the corresponding background color.

#### 3. Bright Colors (90-97, 100-107)
```bash
# Test bright foreground colors
echo -e "\033[90mBright Black\033[0m \033[91mBright Red\033[0m \033[92mBright Green\033[0m"
echo -e "\033[93mBright Yellow\033[0m \033[94mBright Blue\033[0m \033[95mBright Magenta\033[0m"
echo -e "\033[96mBright Cyan\033[0m \033[97mBright White\033[0m"
```

**Expected Result:** Brighter/more vivid versions of the standard colors.

### Extended Color Tests

#### 4. 256-Color Palette
```bash
# Test 256-color foreground (38;5;n)
echo -e "\033[38;5;196mRed 196\033[0m \033[38;5;46mGreen 46\033[0m \033[38;5;21mBlue 21\033[0m"
echo -e "\033[38;5;226mYellow 226\033[0m \033[38;5;201mMagenta 201\033[0m \033[38;5;51mCyan 51\033[0m"

# Test 256-color background (48;5;n)
echo -e "\033[48;5;196m Red BG \033[0m \033[48;5;46m Green BG \033[0m \033[48;5;21m Blue BG \033[0m"
```

**Expected Result:** Specific colors from the 256-color palette should be displayed.

#### 5. RGB True Color (24-bit)
```bash
# Test RGB foreground colors (38;2;r;g;b)
echo -e "\033[38;2;255;0;0mRGB Red\033[0m \033[38;2;0;255;0mRGB Green\033[0m \033[38;2;0;0;255mRGB Blue\033[0m"
echo -e "\033[38;2;255;165;0mRGB Orange\033[0m \033[38;2;128;0;128mRGB Purple\033[0m"

# Test RGB background colors (48;2;r;g;b)
echo -e "\033[48;2;255;0;0m RGB Red BG \033[0m \033[48;2;0;255;0m RGB Green BG \033[0m"
```

**Expected Result:** Precise RGB colors should be displayed.

#### 6. Colon-Separated Color Format
```bash
# Test colon-separated RGB format (38:2:r:g:b)
echo -e "\033[38:2:255:0:0mColon RGB Red\033[0m \033[38:2:0:255:0mColon RGB Green\033[0m"
```

**Expected Result:** Same colors as semicolon format, testing parser flexibility.

### Text Styling Tests

#### 7. Basic Text Styles
```bash
# Test basic text styling
echo -e "\033[1mBold Text\033[0m"
echo -e "\033[3mItalic Text\033[0m"
echo -e "\033[4mUnderlined Text\033[0m"
echo -e "\033[9mStrikethrough Text\033[0m"
```

**Expected Result:** Text should appear with the corresponding style applied.

#### 8. Combined Styles
```bash
# Test combined styling
echo -e "\033[1;3mBold Italic\033[0m"
echo -e "\033[1;4mBold Underlined\033[0m"
echo -e "\033[31;1mRed Bold\033[0m"
echo -e "\033[42;37;1mGreen BG White Bold\033[0m"
```

**Expected Result:** Multiple styles should be applied simultaneously.

#### 9. Advanced Text Styles
```bash
# Test advanced styling
echo -e "\033[2mDim Text\033[0m"
echo -e "\033[5mBlinking Text\033[0m"
echo -e "\033[7mInverse Video\033[0m"
echo -e "\033[8mHidden Text\033[0m (should be invisible)"
```

**Expected Result:** Each style should be applied correctly (blinking may not be supported in all contexts).

#### 10. Underline Styles and Colors
```bash
# Test underline variations (if supported)
echo -e "\033[4:1mSingle Underline\033[0m"
echo -e "\033[4:2mDouble Underline\033[0m"
echo -e "\033[4:3mCurly Underline\033[0m"
echo -e "\033[58;5;196;4mRed Underline\033[0m"
```

**Expected Result:** Different underline styles and colors (support may vary).

### Reset and State Tests

#### 11. SGR Reset Tests
```bash
# Test SGR reset
echo -e "\033[31;1;4mRed Bold Underlined\033[0mNormal Text"
echo -e "\033[32;3mGreen Italic\033[22mNot Bold\033[23mNot Italic\033[0m"
```

**Expected Result:** Styles should be reset correctly at specified points.

#### 12. Selective Reset Tests
```bash
# Test selective attribute reset
echo -e "\033[1;3;4mBold Italic Underlined\033[22mNot Bold\033[24mNot Underlined\033[23mNot Italic\033[0m"
```

**Expected Result:** Individual attributes should be reset while others remain.

## System Command Tests

### PowerShell Commands
```powershell
# Test PowerShell colored output
Get-ChildItem | Format-Table -AutoSize
Write-Host "Red Text" -ForegroundColor Red
Write-Host "Blue Background" -BackgroundColor Blue
```

### Command Prompt Commands
```cmd
# Test colored directory listing (if supported)
dir
echo This is a test
```

### Git Commands (if available)
```bash
git status
git log --oneline --graph --decorate --all
git diff
```

**Expected Result:** Git's colored output should display correctly.

## Validation Checklist

### TestApp Validation

- [ ] Console color test displays correctly at startup
- [ ] Basic 8-color foreground colors work
- [ ] Basic 8-color background colors work
- [ ] Bright colors (90-97) display correctly
- [ ] 256-color palette colors work
- [ ] RGB true color (24-bit) works
- [ ] Colon-separated color format works
- [ ] Bold text styling works
- [ ] Italic text styling works
- [ ] Underlined text styling works
- [ ] Strikethrough text styling works
- [ ] Combined styles work correctly
- [ ] SGR reset (ESC[0m) works
- [ ] Selective attribute reset works
- [ ] Shell commands with colors work
- [ ] No color bleeding between lines
- [ ] Cursor positioning remains accurate

### GameMod Validation

- [ ] F12 toggles terminal correctly
- [ ] All color tests from TestApp work in game
- [ ] Terminal integrates properly with game UI
- [ ] No performance impact on game
- [ ] Colors render correctly in game context
- [ ] Font styling works with game fonts
- [ ] Terminal can be resized without color issues
- [ ] Game shutdown cleans up terminal properly

## Known Issues and Limitations

### Expected Limitations

1. **Console Host Limitations**: Some consoles may not support all ANSI features
2. **Font Limitations**: Italic may not be available for all fonts
3. **Blinking**: May not be supported in ImGui context
4. **Underline Styles**: Advanced underline styles may fall back to single underline
5. **Performance**: Complex styling may impact rendering performance

### Common Issues

1. **Color Not Displaying**: Check console ANSI support
2. **Wrong Colors**: Verify SGR parsing implementation
3. **Style Not Applied**: Check font availability for styling
4. **Color Bleeding**: Verify SGR state management
5. **Reset Not Working**: Check SGR reset sequence handling

## Troubleshooting

### If Colors Don't Display

1. Verify console supports ANSI (run console color test)
2. Check SGR parser implementation
3. Verify color resolution in ImGui controller
4. Test with simpler color sequences first

### If Styles Don't Work

1. Check font availability (Bold, Italic variants)
2. Verify ImGui styling implementation
3. Test individual styles before combinations
4. Check SGR attribute state management

### If Performance Issues Occur

1. Monitor frame rate during color rendering
2. Check for excessive draw calls
3. Verify efficient color caching
4. Test with reduced color complexity

## Success Criteria

### Minimum Requirements

- [ ] All 8 standard colors work (foreground and background)
- [ ] Bold text styling works
- [ ] Basic underline works
- [ ] SGR reset works correctly
- [ ] No color bleeding or state corruption

### Full Implementation

- [ ] All color formats work (8-color, 256-color, RGB)
- [ ] All text styles work (bold, italic, underline, strikethrough)
- [ ] Advanced features work (dim, inverse, underline colors)
- [ ] Performance remains acceptable
- [ ] Integration works in both TestApp and GameMod

## Documentation Requirements

After validation, document:

1. **Working Features**: List all successfully validated features
2. **Limitations**: Document any features that don't work as expected
3. **Performance Impact**: Note any performance considerations
4. **Integration Issues**: Document any game integration problems
5. **Recommendations**: Suggest improvements or workarounds

---

**Validation Date**: December 26, 2024  
**Task**: 3.10 Test and validate color and styling  
**Status**: Ready for user validation