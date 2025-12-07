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
- **PTY (Pseudo-Terminal)**: A pair of virtual devices that provide bidirectional communication between a terminal emulator and a shell process
- **WebSocket**: A protocol providing full-duplex communication channels over a single TCP connection
- **Backend Server**: A Node.js server that manages PTY processes and WebSocket connections
- **Kitty Graphics Protocol**: A terminal graphics protocol for displaying images inline in the terminal
- **Graphics Command**: An escape sequence starting with ESC_G that encodes image data and display parameters
- **Transmission Medium**: The method used to transfer image data (direct, file, temporary file, shared memory)
- **Image Placement**: The positioning and sizing of an image within the terminal grid
- **Image ID**: A unique identifier for an image that allows referencing and manipulation
- **Placement ID**: A unique identifier for a specific placement of an image on screen
- **Virtual Placement**: An image placement that exists in the scrollback buffer or alternate screen

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
6. WHEN SampleShell receives the "red" command with arguments THEN the SampleShell SHALL output the arguments in red color using SGR escape sequences
7. WHEN SampleShell receives the "green" command with arguments THEN the SampleShell SHALL output the arguments in green color using SGR escape sequences

### Requirement 22

**User Story:** As a developer, I want a Node.js backend server that manages PTY processes, so that the terminal emulator can connect to real shell sessions. This is exposed via a node webserver which accepts websocket connections to 

#### Acceptance Criteria

1. WHEN the backend server starts THEN the Backend Server SHALL listen for WebSocket connections on a configured port
2. WHEN a WebSocket connection is established THEN the Backend Server SHALL spawn a new PTY process using the @lydell/node-pty package
3. WHEN spawning a PTY process THEN the Backend Server SHALL use the appropriate shell for the operating system (bash for Unix-like systems, powershell.exe for Windows)
4. WHEN a PTY process is spawned THEN the Backend Server SHALL configure it with terminal dimensions (cols and rows are 80x40)
5. WHEN the PTY process outputs data THEN the Backend Server SHALL forward the data to the connected WebSocket client

### Requirement 23

**User Story:** As a developer, I want bidirectional data flow between the terminal emulator and PTY process, so that user input reaches the shell and shell output reaches the terminal.

#### Acceptance Criteria

1. WHEN the WebSocket receives data from the client THEN the Backend Server SHALL write the data to the PTY process
2. WHEN the PTY process emits data THEN the Backend Server SHALL send the data through the WebSocket to the client
3. WHEN the terminal emulator sends user input THEN the Controller SHALL transmit the data through the WebSocket connection
4. WHEN the WebSocket receives data from the server THEN the Controller SHALL write the data to the Terminal Emulator
5. WHEN the terminal is resized THEN the Controller SHALL send a resize message through the WebSocket to update the PTY dimensions

### Requirement 24

**User Story:** As a developer, I want proper connection lifecycle management, so that resources are cleaned up when connections close.

#### Acceptance Criteria

1. WHEN a WebSocket connection closes THEN the Backend Server SHALL terminate the associated PTY process
2. WHEN a PTY process exits THEN the Backend Server SHALL close the associated WebSocket connection
3. WHEN the terminal page is unloaded THEN the Controller SHALL close the WebSocket connection
4. WHEN a connection error occurs THEN the system SHALL log the error and clean up associated resources
5. WHEN the PTY process is terminated THEN the Backend Server SHALL remove all event listeners for that process

### Requirement 25

**User Story:** As a terminal user, I want to connect to a real shell through the terminal emulator, so that I can execute actual commands and interact with my system.

#### Acceptance Criteria

1. WHEN the terminal page loads THEN the Controller SHALL establish a WebSocket connection to the backend server
2. WHEN the WebSocket connection is established THEN the terminal SHALL display output from the real shell
3. WHEN the user types commands THEN the commands SHALL be executed in the real shell via the PTY process
4. WHEN the shell outputs data THEN the data SHALL be displayed in the terminal emulator with correct formatting
5. WHEN the WebSocket connection fails THEN the terminal SHALL display an error message and optionally fall back to SampleShell

### Requirement 26

**User Story:** As a terminal user, I want to display images inline in the terminal, so that I can view graphics output from applications.

#### Acceptance Criteria

1. WHEN a Kitty graphics command is received THEN the Terminal Emulator SHALL parse the escape sequence and extract the action and payload
2. WHEN an image transmission command is received THEN the Terminal Emulator SHALL decode the base64-encoded image data
3. WHEN image data transmission is complete THEN the Terminal Emulator SHALL store the image data associated with its image ID
4. WHEN a display command is received THEN the Terminal Emulator SHALL create an image placement at the specified position
5. WHEN an image is placed THEN the Terminal Emulator SHALL associate the placement with the current cursor position and cell dimensions

### Requirement 27

**User Story:** As a terminal user, I want images to be positioned correctly in the terminal grid, so that they appear where applications intend them.

#### Acceptance Criteria

1. WHEN an image placement specifies rows and columns THEN the Terminal Emulator SHALL position the image at those grid coordinates
2. WHEN an image placement specifies pixel dimensions THEN the Terminal Emulator SHALL convert pixels to cell dimensions based on cell size
3. WHEN an image placement specifies source rectangle THEN the Terminal Emulator SHALL crop the image to that region before display
4. WHEN an image placement does not specify dimensions THEN the Terminal Emulator SHALL use the image's native dimensions
5. WHEN an image placement extends beyond the screen THEN the Terminal Emulator SHALL clip the image at the screen boundaries

### Requirement 28

**User Story:** As a terminal user, I want images to scroll with terminal content, so that they remain associated with their context.

#### Acceptance Criteria

1. WHEN content scrolls and an image placement is in the scrolled region THEN the Terminal Emulator SHALL move the placement with the scrolled content
2. WHEN an image placement scrolls off the top of the screen THEN the Terminal Emulator SHALL move it to the scrollback buffer
3. WHEN an image placement scrolls off the bottom during reverse scroll THEN the Terminal Emulator SHALL remove the placement
4. WHEN the terminal is scrolled to view scrollback THEN the Terminal Emulator SHALL display image placements in the scrollback buffer
5. WHILE in alternate screen mode, WHEN content scrolls THEN the Terminal Emulator SHALL not preserve image placements in scrollback

### Requirement 29

**User Story:** As a terminal user, I want to manage displayed images, so that I can control what graphics are shown.

#### Acceptance Criteria

1. WHEN a delete command with image ID is received THEN the Terminal Emulator SHALL remove all placements of that image
2. WHEN a delete command with placement ID is received THEN the Terminal Emulator SHALL remove only that specific placement
3. WHEN a delete command with no IDs is received THEN the Terminal Emulator SHALL remove all visible image placements
4. WHEN an image is deleted THEN the Terminal Emulator SHALL free the associated image data from memory
5. WHEN a placement is deleted THEN the Terminal Emulator SHALL update the display to remove the image

### Requirement 30

**User Story:** As a terminal user, I want images to support various formats, so that I can display different types of graphics.

#### Acceptance Criteria

1. WHEN a PNG image is transmitted THEN the Terminal Emulator SHALL decode and display the image
2. WHEN a JPEG image is transmitted THEN the Terminal Emulator SHALL decode and display the image
3. WHEN a GIF image is transmitted THEN the Terminal Emulator SHALL decode and display the image
4. WHEN an animated GIF is transmitted THEN the Terminal Emulator SHALL display the animation
5. WHEN an unsupported format is transmitted THEN the Terminal Emulator SHALL emit an error event and ignore the image

### Requirement 31

**User Story:** As a terminal user, I want images to be transmitted efficiently, so that large images don't block terminal output.

#### Acceptance Criteria

1. WHEN image data is transmitted in chunks THEN the Terminal Emulator SHALL accumulate chunks until transmission is complete
2. WHEN a chunked transmission is in progress THEN the Terminal Emulator SHALL continue processing other terminal output
3. WHEN a transmission is marked as complete THEN the Terminal Emulator SHALL finalize the image and make it available for placement
4. WHEN a transmission fails THEN the Terminal Emulator SHALL discard partial data and emit an error event
5. WHEN multiple images are transmitted concurrently THEN the Terminal Emulator SHALL handle each transmission independently

### Requirement 32

**User Story:** As a web developer, I want the renderer to display images at their placements, so that users can see the graphics.

#### Acceptance Criteria

1. WHEN rendering the terminal THEN the Renderer SHALL create image elements for all visible placements
2. WHEN rendering an image placement THEN the Renderer SHALL position the image element at the correct grid coordinates
3. WHEN rendering an image placement THEN the Renderer SHALL size the image element according to the placement dimensions
4. WHEN an image placement has a source rectangle THEN the Renderer SHALL apply CSS clipping to show only that region
5. WHEN an image placement is removed THEN the Renderer SHALL remove the corresponding image element from the DOM

### Requirement 33

**User Story:** As a terminal user, I want images to respect terminal operations, so that they behave consistently with text content.

#### Acceptance Criteria

1. WHEN the screen is cleared THEN the Terminal Emulator SHALL remove all image placements in the cleared region
2. WHEN a line is erased THEN the Terminal Emulator SHALL remove image placements on that line
3. WHEN lines are inserted THEN the Terminal Emulator SHALL shift image placements down accordingly
4. WHEN lines are deleted THEN the Terminal Emulator SHALL shift image placements up and remove those in deleted lines
5. WHEN the terminal is resized THEN the Terminal Emulator SHALL reposition image placements based on new cell dimensions

### Requirement 34

**User Story:** As a terminal user, I want image placements to have unique identifiers, so that I can reference and manipulate specific images.

#### Acceptance Criteria

1. WHEN an image is transmitted with an ID THEN the Terminal Emulator SHALL store the image data with that ID
2. WHEN a placement is created with a placement ID THEN the Terminal Emulator SHALL associate that ID with the placement
3. WHEN an image ID is reused THEN the Terminal Emulator SHALL replace the previous image data with the new data
4. WHEN a placement ID is reused THEN the Terminal Emulator SHALL replace the previous placement with the new placement
5. WHEN no ID is specified THEN the Terminal Emulator SHALL generate a unique ID automatically

### Requirement 35

**User Story:** As a terminal user, I want images to support transparency, so that they blend correctly with the terminal background.

#### Acceptance Criteria

1. WHEN an image with alpha channel is displayed THEN the Terminal Emulator SHALL preserve the transparency
2. WHEN rendering a transparent image THEN the Renderer SHALL allow the terminal background to show through transparent pixels
3. WHEN an image has no alpha channel THEN the Terminal Emulator SHALL treat it as fully opaque
4. WHEN a transparent image overlaps text THEN the Renderer SHALL layer the image appropriately
5. WHEN the terminal background color changes THEN the Renderer SHALL update the appearance of transparent images

### Requirement 36

**User Story:** As a terminal user, I want images to support Unicode placeholders, so that text-mode applications can reserve space for images.

#### Acceptance Criteria

1. WHEN an image placement is created with Unicode placeholder THEN the Terminal Emulator SHALL write the placeholder character to the grid
2. WHEN a Unicode placeholder is written THEN the Terminal Emulator SHALL associate it with the image placement
3. WHEN the placeholder character is erased THEN the Terminal Emulator SHALL remove the associated image placement
4. WHEN the placeholder character scrolls THEN the Terminal Emulator SHALL move the image placement with it
5. WHEN text overwrites a placeholder THEN the Terminal Emulator SHALL remove the associated image placement
