# Requirements Document

## Introduction

This document specifies the requirements for a headless terminal emulator implemented in TypeScript that integrates with the libghostty-vt WASM library. The terminal emulator will provide complete VT100/xterm-compatible terminal emulation capabilities in a web browser, following an MVC architecture where the model is entirely headless and framework-agnostic.

## Glossary

- **Terminal Emulator**: A software system that emulates a physical terminal, processing input sequences and maintaining screen state
- **libghostty-vt**: A WebAssembly library providing key encoding, SGR parsing, and OSC parsing capabilities
- **Screen Buffer**: A two-dimensional array representing the terminal screen content and attributes
- **Cursor**: The current position in the terminal where the next character will be written
- **SGR (Select Graphic Rendition)**: ANSI escape sequences that control text styling (colors, bold, italic, etc.)
- **OSC (Operating System Command)**: Terminal escape sequences for advanced features like setting window title
- **CSI (Control Sequence Introducer)**: ANSI escape sequences starting with ESC[ for cursor movement and screen manipulation
- **Viewport**: The visible portion of the terminal screen
- **Alternate Screen Buffer**: A secondary screen buffer used by applications like vim and less
- **Scrollback Buffer**: Historical lines that have scrolled off the top of the visible screen
- **Cell**: A single character position in the terminal grid containing a character and its attributes
- **Controller**: The component that handles DOM events and coordinates between model and view
- **View**: The HTML rendering of the terminal state
- **SampleShell**: A simple demonstration shell backend that processes commands and generates terminal output

## Requirements

### Requirement 1

**User Story:** As a terminal user, I want the terminal to maintain a screen buffer with configurable dimensions, so that I can view text content in a grid layout.

#### Acceptance Criteria

1. WHEN the terminal is initialized THEN the Terminal Emulator SHALL create a screen buffer with specified width and height in characters
2. WHEN the screen dimensions are changed THEN the Terminal Emulator SHALL resize the buffer and preserve existing content where possible
3. WHILE the terminal is active, THE Terminal Emulator SHALL maintain each cell with a character and associated SGR attributes
4. WHEN a cell is accessed THEN the Terminal Emulator SHALL provide the character, foreground color, background color, and text attributes (bold, italic, underline, etc.)
5. WHEN the terminal is initialized THEN the Terminal Emulator SHALL support a minimum size of 1x1 and maximum size of 1000x1000 characters

### Requirement 2

**User Story:** As a terminal user, I want the terminal to track cursor position and visibility, so that I know where text input will appear.

#### Acceptance Criteria

1. WHEN the terminal is initialized THEN the Terminal Emulator SHALL set the cursor position to row 0, column 0
2. WHEN a character is written THEN the Terminal Emulator SHALL update the cursor position to the next column
3. WHEN the cursor reaches the right edge THEN the Terminal Emulator SHALL wrap to the beginning of the next line if auto-wrap is enabled
4. WHEN a cursor movement sequence is received THEN the Terminal Emulator SHALL update the cursor position according to the sequence
5. WHEN cursor visibility is toggled THEN the Terminal Emulator SHALL track the visibility state for rendering
6. WHEN the cursor position is queried THEN the Terminal Emulator SHALL return the current row and column

### Requirement 3

**User Story:** As a terminal user, I want the terminal to process printable characters, so that I can see text output.

#### Acceptance Criteria

1. WHEN a printable ASCII character is received THEN the Terminal Emulator SHALL write it to the current cursor position
2. WHEN a character is written THEN the Terminal Emulator SHALL apply the current SGR attributes to that cell
3. WHEN a UTF-8 multi-byte character is received THEN the Terminal Emulator SHALL decode and display it correctly
4. WHEN a wide character (CJK) is received THEN the Terminal Emulator SHALL occupy two cell positions
5. WHEN writing would exceed the line width THEN the Terminal Emulator SHALL handle according to auto-wrap mode

### Requirement 4

**User Story:** As a terminal user, I want the terminal to process control characters, so that applications can control cursor movement and formatting.

#### Acceptance Criteria

1. WHEN a newline character (LF, 0x0A) is received THEN the Terminal Emulator SHALL move the cursor to the next line
2. WHEN a carriage return (CR, 0x0D) is received THEN the Terminal Emulator SHALL move the cursor to column 0
3. WHEN a backspace (BS, 0x08) is received THEN the Terminal Emulator SHALL move the cursor one position left if not at column 0
4. WHEN a tab character (HT, 0x09) is received THEN the Terminal Emulator SHALL move the cursor to the next tab stop
5. WHEN a bell character (BEL, 0x07) is received THEN the Terminal Emulator SHALL trigger a bell event

### Requirement 5

**User Story:** As a terminal user, I want the terminal to process CSI escape sequences, so that applications can control cursor positioning and screen manipulation.

#### Acceptance Criteria

1. WHEN a cursor up sequence (CSI A) is received THEN the Terminal Emulator SHALL move the cursor up by the specified number of rows
2. WHEN a cursor down sequence (CSI B) is received THEN the Terminal Emulator SHALL move the cursor down by the specified number of rows
3. WHEN a cursor forward sequence (CSI C) is received THEN the Terminal Emulator SHALL move the cursor right by the specified number of columns
4. WHEN a cursor backward sequence (CSI D) is received THEN the Terminal Emulator SHALL move the cursor left by the specified number of columns
5. WHEN a cursor position sequence (CSI H) is received THEN the Terminal Emulator SHALL move the cursor to the specified row and column
6. WHEN an erase in display sequence (CSI J) is received THEN the Terminal Emulator SHALL clear portions of the screen according to the parameter
7. WHEN an erase in line sequence (CSI K) is received THEN the Terminal Emulator SHALL clear portions of the current line according to the parameter
8. WHEN a scroll up sequence (CSI S) is received THEN the Terminal Emulator SHALL scroll the screen up by the specified number of lines
9. WHEN a scroll down sequence (CSI T) is received THEN the Terminal Emulator SHALL scroll the screen down by the specified number of lines

### Requirement 6

**User Story:** As a terminal user, I want the terminal to integrate with libghostty-vt for SGR parsing, so that text styling is correctly applied.

**IMPORTANT** libghostty-vt accepts EITHER the standard semicolon ';' as a separator or the non-standard ':' colon.  This is to be compatible with some non-compliant software.  We can trust the libghostty-vt will behave correctly.  All SGR parsing must be delegated to libghostty-vt WASM and it's results should be trustworthy (but validated with unit tests)

**NOTE** use the data in `ghostty-c-api/include` as reference documentation for the SGR tag behavior, because ghostty supports more modern capabilities then VT100/xterm-compatible does.

#### Acceptance Criteria

1. WHEN an SGR sequence is received THEN the Terminal Emulator SHALL parse it using libghostty-vt
2. WHEN SGR attributes are parsed THEN the Terminal Emulator SHALL update the current text attributes state
3. WHEN a reset SGR sequence (CSI 0 m) is received THEN the Terminal Emulator SHALL reset all attributes to defaults
4. WHEN foreground or background color attributes are parsed THEN the Terminal Emulator SHALL store them for subsequent character writes
5. WHEN text style attributes (bold, italic, underline) are parsed THEN the Terminal Emulator SHALL apply them to subsequent characters

### Requirement 7

**User Story:** As a terminal user, I want the terminal to integrate with libghostty-vt for OSC parsing, so that advanced terminal features work correctly.

#### Acceptance Criteria

1. WHEN an OSC sequence is received THEN the Terminal Emulator SHALL parse it using libghostty-vt
2. WHEN an OSC 0 or OSC 2 sequence (set window title) is received THEN the Terminal Emulator SHALL emit a title change event
3. WHEN an OSC 8 sequence (hyperlink) is received THEN the Terminal Emulator SHALL associate the URL with subsequent characters
4. WHEN an OSC 52 sequence (clipboard) is received THEN the Terminal Emulator SHALL emit a clipboard event
5. WHEN an unknown OSC sequence is received THEN the Terminal Emulator SHALL ignore it without error

### Requirement 8

**User Story:** As a terminal user, I want the terminal to support scrolling and scrollback, so that I can review previous output.

#### Acceptance Criteria

1. WHEN content scrolls off the top of the screen THEN the Terminal Emulator SHALL add it to the scrollback buffer
2. WHEN the scrollback buffer exceeds the maximum size THEN the Terminal Emulator SHALL remove the oldest lines
3. WHEN the terminal is scrolled THEN the Terminal Emulator SHALL maintain the viewport offset
4. WHEN new content is written while scrolled THEN the Terminal Emulator SHALL optionally auto-scroll to the bottom
5. WHEN the scrollback is queried THEN the Terminal Emulator SHALL provide access to historical lines

### Requirement 9

**User Story:** As a terminal user, I want the terminal to support alternate screen buffer, so that full-screen applications like vim work correctly.

#### Acceptance Criteria

1. WHEN the alternate screen buffer is activated THEN the Terminal Emulator SHALL switch to a separate screen buffer
2. WHEN the alternate screen buffer is deactivated THEN the Terminal Emulator SHALL restore the primary screen buffer
3. WHILE in alternate screen mode, THE Terminal Emulator SHALL not add content to scrollback
4. WHEN switching buffers THEN the Terminal Emulator SHALL preserve cursor position and attributes independently
5. WHEN the alternate buffer is activated THEN the Terminal Emulator SHALL clear it to default state

### Requirement 10

**User Story:** As a terminal user, I want the terminal to support scroll regions, so that applications can create split-screen effects.

#### Acceptance Criteria

1. WHEN a scroll region is set (CSI r) THEN the Terminal Emulator SHALL restrict scrolling to the specified rows
2. WHEN content scrolls within a region THEN the Terminal Emulator SHALL not affect content outside the region
3. WHEN the scroll region is reset THEN the Terminal Emulator SHALL restore full-screen scrolling
4. WHEN the cursor moves outside the scroll region THEN the Terminal Emulator SHALL allow normal positioning
5. WHEN a scroll region is active THEN the Terminal Emulator SHALL apply it to scroll up and scroll down operations

### Requirement 11

**User Story:** As a web developer, I want a controller component that handles keyboard input, so that user keystrokes are converted to terminal input.

#### Acceptance Criteria

1. WHEN a key is pressed in the input element THEN the Controller SHALL capture the keydown event
2. WHEN a keydown event is captured THEN the Controller SHALL convert it to a KeyEvent structure
3. WHEN a KeyEvent is created THEN the Controller SHALL use libghostty-vt to encode it to terminal escape sequences
4. WHEN encoded sequences are generated THEN the Controller SHALL send them to the terminal emulator for processing
5. WHEN special keys (arrows, function keys) are pressed THEN the Controller SHALL encode them according to terminal mode

### Requirement 12

**User Story:** As a web developer, I want a controller component that renders the terminal state to HTML, so that users can see the terminal output.

#### Acceptance Criteria

1. WHEN the terminal state changes THEN the Controller SHALL update the HTML view
2. WHEN rendering the screen THEN the Controller SHALL create span elements for each character with appropriate styling
3. WHEN rendering cells THEN the Controller SHALL apply CSS for foreground color, background color, and text attributes
4. WHEN rendering the cursor THEN the Controller SHALL display it at the current position with appropriate styling
5. WHEN rendering wide characters THEN the Controller SHALL ensure they occupy the correct visual space

### Requirement 13

**User Story:** As a web developer, I want the controller to handle focus management, so that keyboard input is properly captured.

#### Acceptance Criteria

1. WHEN the terminal view is clicked THEN the Controller SHALL focus the input element
2. WHEN the input element loses focus THEN the Controller SHALL indicate the unfocused state visually
3. WHEN the input element gains focus THEN the Controller SHALL indicate the focused state visually
4. WHILE the terminal is focused, THE Controller SHALL prevent default browser keyboard shortcuts where appropriate
5. WHEN the terminal is initialized THEN the Controller SHALL automatically focus the input element

### Requirement 14

**User Story:** As a terminal user, I want the terminal to support tab stops, so that tab characters align correctly.

#### Acceptance Criteria

1. WHEN the terminal is initialized THEN the Terminal Emulator SHALL set default tab stops every 8 columns
2. WHEN a tab character is received THEN the Terminal Emulator SHALL move the cursor to the next tab stop
3. WHEN a set tab stop sequence (CSI H) is received THEN the Terminal Emulator SHALL set a tab stop at the current column
4. WHEN a clear tab stop sequence (CSI g) is received THEN the Terminal Emulator SHALL clear tab stops according to the parameter
5. WHEN the cursor is at or past the last column THEN the Terminal Emulator SHALL handle tab according to auto-wrap mode

### Requirement 15

**User Story:** As a terminal user, I want the terminal to support various terminal modes, so that applications can control terminal behavior.

#### Acceptance Criteria

1. WHEN auto-wrap mode is enabled THEN the Terminal Emulator SHALL wrap cursor to next line at right edge
2. WHEN auto-wrap mode is disabled THEN the Terminal Emulator SHALL keep cursor at right edge when writing
3. WHEN cursor visibility mode is changed THEN the Terminal Emulator SHALL update the cursor visibility state
4. WHEN application cursor keys mode is enabled THEN the Terminal Emulator SHALL encode arrow keys differently
5. WHEN bracketed paste mode is enabled THEN the Terminal Emulator SHALL wrap pasted content with escape sequences

### Requirement 16

**User Story:** As a web developer, I want the terminal emulator to emit events for external actions, so that the application can respond to terminal requests.

#### Acceptance Criteria

1. WHEN a bell character is received THEN the Terminal Emulator SHALL emit a bell event
2. WHEN a title change OSC is received THEN the Terminal Emulator SHALL emit a title change event with the new title
3. WHEN a clipboard OSC is received THEN the Terminal Emulator SHALL emit a clipboard event with the content
4. WHEN the terminal needs to send data to a shell THEN the Terminal Emulator SHALL emit a data output event
5. WHEN a resize occurs THEN the Terminal Emulator SHALL emit a resize event

### Requirement 17

**User Story:** As a terminal user, I want the terminal to handle character insertion and deletion, so that line editing works correctly.

#### Acceptance Criteria

1. WHEN an insert character sequence (CSI @) is received THEN the Terminal Emulator SHALL insert blank cells at the cursor position
2. WHEN a delete character sequence (CSI P) is received THEN the Terminal Emulator SHALL delete cells at the cursor position
3. WHEN an insert line sequence (CSI L) is received THEN the Terminal Emulator SHALL insert blank lines at the cursor row
4. WHEN a delete line sequence (CSI M) is received THEN the Terminal Emulator SHALL delete lines at the cursor row
5. WHEN characters are inserted or deleted THEN the Terminal Emulator SHALL shift existing content appropriately

### Requirement 18

**User Story:** As a web developer, I want the terminal emulator to provide a clean TypeScript API, so that it can be easily integrated and tested.

#### Acceptance Criteria

1. WHEN creating a terminal instance THEN the API SHALL accept width, height, and scrollback size parameters
2. WHEN writing data to the terminal THEN the API SHALL accept string or Uint8Array input
3. WHEN querying terminal state THEN the API SHALL provide methods to access screen buffer, cursor position, and attributes
4. WHEN the terminal state changes THEN the API SHALL provide a way to subscribe to state change notifications
5. WHEN disposing the terminal THEN the API SHALL clean up all resources including WASM memory

### Requirement 19

**User Story:** As a terminal user, I want the terminal to handle character set selection, so that line-drawing characters display correctly.

#### Acceptance Criteria

1. WHEN a character set designation sequence is received THEN the Terminal Emulator SHALL track the designated character set
2. WHEN shift-in (SI) or shift-out (SO) control characters are received THEN the Terminal Emulator SHALL switch between character sets
3. WHEN the DEC Special Graphics character set is active THEN the Terminal Emulator SHALL map characters to line-drawing glyphs
4. WHEN a character is written THEN the Terminal Emulator SHALL apply the current character set mapping
5. WHEN the terminal is reset THEN the Terminal Emulator SHALL restore default character set mappings

### Requirement 20

**User Story:** As a web developer, I want the controller to handle selection and copying, so that users can copy terminal content.

#### Acceptance Criteria

1. WHEN the user drags the mouse over terminal content THEN the Controller SHALL track the selection range
2. WHEN a selection is active THEN the Controller SHALL highlight the selected cells visually
3. WHEN the user copies THEN the Controller SHALL extract text from the selected cells
4. WHEN extracting text THEN the Controller SHALL preserve line breaks and handle wide characters correctly
5. WHEN copying THEN the Controller SHALL place the text on the system clipboard

### Requirement 21

**User Story:** As a terminal user, I want a demonstration shell backend, so that I can interact with the terminal emulator and verify its functionality.

#### Acceptance Criteria

1. WHEN the terminal demo page is initialized THEN the system SHALL use SampleShell as the default shell backend
2. WHEN SampleShell receives the "ls" command THEN the SampleShell SHALL output a list of five dummy filenames
3. WHEN SampleShell receives the "echo" command with arguments THEN the SampleShell SHALL output the arguments back to the terminal
4. WHEN SampleShell receives Ctrl+L keystroke THEN the SampleShell SHALL send escape sequences to clear the screen and reset cursor position
5. WHEN SampleShell receives an unknown command THEN the SampleShell SHALL output an appropriate error message
