# Terminal Specification Coverage

This document tracks the implementation status of terminal escape sequences in caTTY based on ECMA-48 and xterm specifications.

## Legend
- ✅ **Implemented**: Feature is parsed and fully implemented with behavior
- ❌ **Not Implemented**: Feature is not parsed or implemented
- 🟡 **Parsed Only**: Feature is parsed but not implemented (stubbed)

## Control Characters (C0)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| BEL (0x07) | Bell | ECMA-48 | ✅ | Audio/visual bell |
| BS (0x08) | Backspace | ECMA-48 | ✅ | Move cursor left |
| HT (0x09) | Horizontal Tab | ECMA-48 | ✅ | Move to next tab stop |
| LF (0x0A) | Line Feed | ECMA-48 | ✅ | Move cursor down |
| FF (0x0C) | Form Feed | ECMA-48 | ✅ | Treated as line feed |
| CR (0x0D) | Carriage Return | ECMA-48 | ✅ | Move cursor to column 0 |

## ESC Sequences (Two-byte)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| ESC 7 | DECSC - Save Cursor | xterm | ✅ | Save cursor position |
| ESC 8 | DECRC - Restore Cursor | xterm | ✅ | Restore cursor position |
| ESC ( X | Designate G0 Character Set | ECMA-48 | ✅ | Character set designation |
| ESC ) X | Designate G1 Character Set | ECMA-48 | ✅ | Character set designation |
| ESC * X | Designate G2 Character Set | ECMA-48 | ✅ | Character set designation |
| ESC + X | Designate G3 Character Set | ECMA-48 | ✅ | Character set designation |

## CSI Sequences (Cursor Movement)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI n A | CUU - Cursor Up | ECMA-48 | ✅ | Move cursor up n lines |
| CSI n B | CUD - Cursor Down | ECMA-48 | ✅ | Move cursor down n lines |
| CSI n C | CUF - Cursor Forward | ECMA-48 | ✅ | Move cursor right n columns |
| CSI n D | CUB - Cursor Backward | ECMA-48 | ✅ | Move cursor left n columns |
| CSI n E | CNL - Cursor Next Line | ECMA-48 | ✅ | Move cursor to beginning of next line |
| CSI n F | CPL - Cursor Previous Line | ECMA-48 | ✅ | Move cursor to beginning of previous line |
| CSI n G | CHA - Cursor Horizontal Absolute | ECMA-48 | ✅ | Move cursor to column n |
| CSI n d | VPA - Vertical Position Absolute | ECMA-48 | ✅ | Move cursor to row n |
| CSI n ; m H | CUP - Cursor Position | ECMA-48 | ✅ | Move cursor to row n, column m |
| CSI n ; m f | HVP - Horizontal Vertical Position | ECMA-48 | ✅ | Same as CUP |

## CSI Sequences (Erasing)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI n J | ED - Erase in Display | ECMA-48 | ✅ | Clear screen (modes 0,1,2,3) |
| CSI n K | EL - Erase in Line | ECMA-48 | ✅ | Clear line (modes 0,1,2) |
| CSI n X | ECH - Erase Character | ECMA-48 | ✅ | Erase n characters |

## CSI Sequences (Scrolling)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI n S | SU - Scroll Up | ECMA-48 | ✅ | Scroll up n lines |
| CSI n T | SD - Scroll Down | ECMA-48 | 🟡 | Parsed but ignored |
| CSI n ; m r | DECSTBM - Set Top/Bottom Margins | xterm | ✅ | Set scroll region |

## CSI Sequences (Cursor Save/Restore)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI s | SCP - Save Cursor Position | xterm | ✅ | Save cursor (ANSI version) |
| CSI u | RCP - Restore Cursor Position | xterm | ✅ | Restore cursor (ANSI version) |

## CSI Sequences (Modes)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 4 h | IRM - Insert Mode Set | ECMA-48 | 🟡 | Parsed but not implemented |
| CSI 4 l | IRM - Insert Mode Reset | ECMA-48 | 🟡 | Parsed but not implemented |

## CSI Sequences (DEC Private Modes)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI ? 1 h/l | DECCKM - Cursor Keys Mode | xterm | ✅ | Application cursor keys |
| CSI ? 25 h/l | DECTCEM - Text Cursor Enable | xterm | ✅ | Show/hide cursor |
| CSI ? 47 h/l | Alternate Screen Buffer | xterm | ✅ | Switch screen buffers |
| CSI ? 1047 h/l | Alternate Screen + Cursor Save | xterm | ✅ | Alt screen with cursor save |
| CSI ? 1049 h/l | Alternate Screen + Clear + Cursor | xterm | ✅ | Alt screen with clear and cursor save |
| CSI ? 2027 h/l | UTF-8 Mode | xterm | ✅ | Enable/disable UTF-8 |

## CSI Sequences (Cursor Style)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI n SP q | DECSCUSR - Set Cursor Style | xterm | ✅ | Set cursor appearance (0-6) |

## CSI Sequences (Device Queries)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI c | DA1 - Primary Device Attributes | xterm | ✅ | Query terminal capabilities |
| CSI > c | DA2 - Secondary Device Attributes | xterm | ✅ | Query terminal version |
| CSI 6 n | CPR - Cursor Position Report | xterm | ✅ | Request cursor position |
| CSI 18 t | Terminal Size Query | xterm | ✅ | Request terminal dimensions |
| CSI ? 26 n | Character Set Query | xterm | ✅ | Query character set |

## CSI Sequences (Window Manipulation)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI n t | Window Manipulation | xterm | 🟡 | Parsed but ignored (web security) |
| CSI 22;2 t | Push Window Title | xterm | 🟡 | Parsed but ignored (no title stack) |
| CSI 22;1 t | Push Icon Name | xterm | 🟡 | Parsed but ignored (no title stack) |
| CSI 23;2 t | Pop Window Title | xterm | 🟡 | Parsed but ignored (no title stack) |
| CSI 23;1 t | Pop Icon Name | xterm | 🟡 | Parsed but ignored (no title stack) |

## CSI Sequences (Mouse)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI ? 1000 h/l | Mouse Reporting | xterm | 🟡 | Parsed but ignored |
| CSI ? 1002 h/l | Button Event Mouse | xterm | 🟡 | Parsed but ignored |
| CSI ? 1003 h/l | Any Event Mouse | xterm | 🟡 | Parsed but ignored |

## SGR Sequences (Select Graphic Rendition)

### Basic Attributes

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 0 m | Reset | ECMA-48 | ✅ | Resets all SGR attributes to default |
| CSI 1 m | Bold | ECMA-48 | ✅ | Bold text styling with CSS |
| CSI 2 m | Faint | ECMA-48 | ✅ | Faint/dim text styling with CSS |
| CSI 3 m | Italic | ECMA-48 | ✅ | Italic text styling with CSS |
| CSI 4 m | Underline | ECMA-48 | ✅ | Underline text styling with CSS |
| CSI 4:n m | Underline Style | xterm | ✅ | Underline styles (single/double/curly/dotted/dashed) |
| CSI 5 m | Slow Blink | ECMA-48 | ✅ | Blink text styling with CSS |
| CSI 6 m | Rapid Blink | ECMA-48 | ✅ | Blink text styling with CSS |
| CSI 7 m | Inverse | ECMA-48 | ✅ | Inverse video styling with CSS |
| CSI 8 m | Hidden | ECMA-48 | ✅ | Hidden text styling with CSS |
| CSI 9 m | Strikethrough | ECMA-48 | ✅ | Strikethrough text styling with CSS |

### Font Selection

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 10-19 m | Font Selection | ECMA-48 | ✅ | Font selection with CSS |
| CSI 20 m | Fraktur | ECMA-48 | ✅ | Fraktur text styling with CSS |

### Reset Attributes

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 21 m | Double Underline | ECMA-48 | ✅ | Double underline styling with CSS |
| CSI 22 m | Normal Intensity | ECMA-48 | ✅ | Resets bold and faint attributes |
| CSI 23 m | Not Italic | ECMA-48 | ✅ | Disables italic styling |
| CSI 24 m | Not Underlined | ECMA-48 | ✅ | Disables underline styling |
| CSI 25 m | Not Blinking | ECMA-48 | ✅ | Disables blink styling |
| CSI 26 m | Proportional Spacing | ECMA-48 | ✅ | Proportional spacing with CSS |
| CSI 27 m | Not Inverse | ECMA-48 | ✅ | Disables inverse video styling |
| CSI 28 m | Not Hidden | ECMA-48 | ✅ | Disables hidden text styling |
| CSI 29 m | Not Strikethrough | ECMA-48 | ✅ | Disables strikethrough styling |

### Standard Colors

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 30-37 m | Foreground Colors | ECMA-48 | ✅ | Theme-aware CSS variables (black/red/green/yellow/blue/magenta/cyan/white) |
| CSI 38;5;n m | 256-Color Foreground | xterm | ✅ | Direct color values with 256-color palette |
| CSI 38;2;r;g;b m | RGB Foreground | xterm | ✅ | 24-bit RGB color support |
| CSI 39 m | Default Foreground | ECMA-48 | ✅ | Reset to theme default foreground |
| CSI 40-47 m | Background Colors | ECMA-48 | ✅ | Theme-aware CSS variables (black/red/green/yellow/blue/magenta/cyan/white) |
| CSI 48;5;n m | 256-Color Background | xterm | ✅ | Direct color values with 256-color palette |
| CSI 48;2;r;g;b m | RGB Background | xterm | ✅ | 24-bit RGB color support |
| CSI 49 m | Default Background | ECMA-48 | ✅ | Reset to theme default background |

### Extended Attributes

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 50 m | Disable Proportional | ECMA-48 | ✅ | Disables proportional spacing |
| CSI 51 m | Framed | ECMA-48 | ✅ | Framed text styling with CSS |
| CSI 52 m | Encircled | ECMA-48 | ✅ | Encircled text styling with CSS |
| CSI 53 m | Overlined | ECMA-48 | ✅ | Overlined text styling with CSS |
| CSI 54 m | Not Framed | ECMA-48 | ✅ | Disables framed styling |
| CSI 55 m | Not Overlined | ECMA-48 | ✅ | Disables overlined styling |
| CSI 58;5;n m | Underline Color (256) | xterm | ✅ | 256-color underline with CSS |
| CSI 58;2;r;g;b m | Underline Color (RGB) | xterm | ✅ | RGB underline color with CSS |
| CSI 59 m | Default Underline Color | xterm | ✅ | Resets underline color to default |

### Ideogram Attributes

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 60-65 m | Ideogram Attributes | ECMA-48 | ✅ | Ideogram styling with CSS |

### Superscript/Subscript

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 73 m | Superscript | xterm | ✅ | Superscript text styling with CSS |
| CSI 74 m | Subscript | xterm | ✅ | Subscript text styling with CSS |
| CSI 75 m | Neither Super/Sub | xterm | ✅ | Disables super/subscript styling |

### Bright Colors

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI 90-97 m | Bright Foreground | xterm | ✅ | Theme-aware CSS variables for bright colors |
| CSI 100-107 m | Bright Background | xterm | ✅ | Theme-aware CSS variables for bright colors |

### Special SGR Sequences (Vi Compatibility)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| CSI > n ; m m | Enhanced SGR Mode | xterm | ✅ | Parsed and gracefully ignored |
| CSI ? n m | Private SGR Mode | xterm | ✅ | Parsed and gracefully ignored |
| CSI n % m | SGR with Intermediate | xterm | ✅ | Parsed, CSI 0%m resets attributes |

## OSC Sequences (Operating System Commands)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| OSC 0 ; text BEL/ST | Set Title and Icon | xterm | ✅ | Set window title and icon |
| OSC 1 ; text BEL/ST | Set Icon Name | xterm | ✅ | Set icon name |
| OSC 2 ; text BEL/ST | Set Window Title | xterm | ✅ | Set window title |
| OSC 21 BEL/ST | Query Window Title | xterm | ✅ | Query current title |
| OSC 10 ; ? BEL/ST | Query Foreground Color | xterm | ✅ | Returns theme foreground color |
| OSC 11 ; ? BEL/ST | Query Background Color | xterm | ✅ | Returns theme background color |

## DCS Sequences (Device Control String)

| Sequence | Name | Spec | Status | Notes |
|----------|------|------|--------|-------|
| DCS ... ST | Device Control | ECMA-48 | 🟡 | Parsed but not implemented |

## Summary

- **Total Sequences**: ~130 documented sequences
- **Fully Implemented**: ~85 sequences (✅)
- **Parsed Only**: ~40 sequences (🟡) 
- **Not Implemented**: ~5 sequences (❌)

The terminal emulator has comprehensive parsing coverage and full SGR styling implementation through CSS-based rendering. Core functionality includes cursor movement, screen manipulation, terminal modes, complete color rendering with theme support, and full text styling (bold, italic, underline, strikethrough, etc.). Vi-specific sequences are now supported for better compatibility with full-screen terminal applications.

### Recent Additions (Vi Compatibility)
- Enhanced SGR sequences with special prefixes (`>`, `?`) and intermediates (`%`)
- OSC color query sequences for foreground/background color detection
- Window manipulation sequences (parsed but gracefully ignored for web security)
- Complete SGR styling system with CSS generation and theme integration