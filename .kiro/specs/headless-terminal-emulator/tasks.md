# Implementation Plan

- [x] 1. Set up core data structures and types
  - [x] 1.1 Create Cell interface with character and attribute fields
    - Define Color type (default, indexed, rgb)
    - Define UnderlineStyle enum
    - Create Cell interface with char, width, fg, bg, bold, italic, underline, inverse, strikethrough, url
    - _Requirements: 1.3, 1.4_
  
  - [x] 1.2 Create Line interface
    - Define Line with cells array and wrapped flag
    - _Requirements: 1.1_
  
  - [x] 1.3 Create CursorState interface
    - Define CursorState with row, col, visible, blinking
    - _Requirements: 2.1, 2.5_
  
  - [x] 1.4 Create Attributes interface
    - Define Attributes for current text styling
    - _Requirements: 6.2_

- [x] 2. Implement ScreenBuffer class
  - [x] 2.1 Implement buffer initialization and cell access
    - Create ScreenBuffer class with cols and rows
    - Implement getCell and setCell methods
    - Implement getLine method
    - _Requirements: 1.1, 1.4_
  
  - [x] 2.2 Write property test for buffer initialization
    - **Property 1: Buffer initialization creates correct dimensions**
    - **Validates: Requirements 1.1**
  
  - [x] 2.3 Write property test for cell structure
    - **Property 3: Cell structure completeness**
    - **Validates: Requirements 1.3, 1.4**
  
  - [x] 2.4 Implement buffer clearing operations
    - Implement clear method
    - Implement clearRegion method
    - _Requirements: 5.6, 5.7_
  
  - [x] 2.5 Implement scrolling operations
    - Implement scrollUp method with optional scroll region
    - Implement scrollDown method with optional scroll region
    - _Requirements: 5.8, 5.9, 10.1, 10.2_
  
  - [x] 2.6 Write property test for scroll operations
    - **Property 19: Scroll operations move content correctly**
    - **Validates: Requirements 5.8, 5.9**
  
  - [x] 2.7 Write property test for scroll region isolation
    - **Property 35: Scroll region content isolation**
    - **Validates: Requirements 10.2**
  
  - [x] 2.8 Implement line and cell insertion/deletion
    - Implement insertLines method
    - Implement deleteLines method
    - Implement insertCells method
    - Implement deleteCells method
    - _Requirements: 17.1, 17.2, 17.3, 17.4_
  
  - [x] 2.9 Write property tests for insertion and deletion
    - **Property 51: Character insertion shifts content**
    - **Property 52: Character deletion shifts content**
    - **Property 53: Line insertion shifts content**
    - **Property 54: Line deletion shifts content**
    - **Validates: Requirements 17.1, 17.2, 17.3, 17.4, 17.5**
  
  - [x] 2.10 Implement buffer resizing
    - Implement resize method that preserves content
    - _Requirements: 1.2_
  
  - [x] 2.11 Write property test for resize preservation
    - **Property 2: Resize preserves overlapping content**
    - **Validates: Requirements 1.2**

- [x] 3. Implement ScrollbackBuffer class
  - [x] 3.1 Create circular buffer for scrollback
    - Implement ScrollbackBuffer with push, get, clear methods
    - Handle maximum size limit
    - _Requirements: 8.1, 8.2, 8.5_
  
  - [x] 3.2 Write property test for scrollback capture
    - **Property 25: Scrollback captures scrolled content**
    - **Validates: Requirements 8.1, 8.5**
  
  - [x] 3.3 Write property test for scrollback size limit
    - **Property 26: Scrollback buffer size limit**
    - **Validates: Requirements 8.2**

- [x] 4. Implement AlternateScreenManager class
  - [x] 4.1 Create dual buffer management
    - Implement primary and alternate ScreenState
    - Implement switchToAlternate and switchToPrimary methods
    - _Requirements: 9.1, 9.2_
  
  - [x] 4.2 Write property test for buffer isolation
    - **Property 29: Alternate screen buffer isolation**
    - **Validates: Requirements 9.1**
  
  - [x] 4.3 Write property test for buffer round-trip
    - **Property 30: Alternate screen round-trip**
    - **Validates: Requirements 9.2**
  
  - [x] 4.4 Write property test for alternate screen no scrollback
    - **Property 31: Alternate screen no scrollback**
    - **Validates: Requirements 9.3**
  
  - [x] 4.5 Write property test for independent cursor state
    - **Property 32: Buffer-independent cursor state**
    - **Validates: Requirements 9.4**
  
  - [x] 4.6 Write property test for alternate buffer initialization
    - **Property 33: Alternate buffer initialization**
    - **Validates: Requirements 9.5**

- [ ] 5. Implement Parser class for escape sequences
  - [x] 5.1 Create parser state machine
    - Define ParserState enum
    - Implement state transitions
    - Handle UTF-8 multi-byte sequences
    - _Requirements: 3.3, 5.1-5.9_
  
  - [x] 5.2 Implement control character handling
    - Handle LF, CR, BS, HT, BEL
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_
  
  - [x] 5.3 Implement CSI sequence parsing
    - Parse CSI parameters and intermediates
    - Handle cursor movement (A, B, C, D, H)
    - Handle erase operations (J, K)
    - Handle scroll operations (S, T)
    - Handle character/line insertion/deletion (@, P, L, M)
    - Handle scroll region (r)
    - Handle tab operations (H, g)
    - _Requirements: 5.1-5.9, 10.1, 14.3, 14.4, 17.1-17.4_
  
  - [x] 5.4 Write property tests for CSI cursor movement
    - **Property 15: CSI cursor movement correctness**
    - **Property 16: CSI cursor positioning absolute**
    - **Validates: Requirements 5.1, 5.2, 5.3, 5.4, 5.5**
  
  - [x] 5.5 Write property tests for erase operations
    - **Property 17: Erase in display clears correct region**
    - **Property 18: Erase in line clears correct region**
    - **Validates: Requirements 5.6, 5.7**
  
  - [x] 5.6 Integrate libghostty-vt for SGR parsing
    - Create wrapper for ghostty_sgr_* functions
    - Parse SGR sequences and update attributes
    - Note: libghostty-vt accepts both standard semicolon ';' and non-standard colon ':' separators for compatibility
    - Delegate all SGR parsing to libghostty-vt without validating separator format
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_
  
  - [x] 5.7 Write property test for SGR parsing
    - **Property 20: SGR parsing updates attributes**
    - Tests must instantiate the WASM bundle using node patterns (see LoadWasm.test.ts as an example) and pass them to class instances that will interface with libghostty-vt.  This way unit tests invoked by node will be responsible for the node variant of WASM.
    - Test with both semicolon and colon separators to verify libghostty-vt handles both correctly
    - **Validates: Requirements 6.1, 6.2**
  
  - [x] 5.8 Write property test for SGR attribute persistence
    - **Property 21: SGR attributes persist across characters**
    - **Validates: Requirements 6.4, 6.5**
  
  - [x] 5.9 Integrate libghostty-vt for OSC parsing
    - Create wrapper for ghostty_osc_* functions
    - Handle OSC 0/2 (title), OSC 8 (hyperlink), OSC 52 (clipboard)
    - Emit appropriate events
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_
  
  - [x] 5.10 Write property test for OSC parsing
    - **Property 22: OSC parsing triggers appropriate actions**
    - **Validates: Requirements 7.1**
  
  - [x] 5.11 Write property test for OSC 8 hyperlinks
    - **Property 23: OSC 8 hyperlink association**
    - **Validates: Requirements 7.3**
  
  - [x] 5.12 Write property test for unknown OSC handling
    - **Property 24: Unknown OSC sequences are ignored**
    - **Validates: Requirements 7.5**
  
  - [x] 5.13 Implement character set handling
    - Track G0-G3 character set designations
    - Handle SI/SO control characters
    - Implement DEC Special Graphics mapping
    - _Requirements: 19.1, 19.2, 19.3, 19.4_
  
  - [x] 5.14 Write property tests for character sets
    - **Property 56: Character set designation tracking**
    - **Property 57: Character set switching**
    - **Property 58: DEC Special Graphics mapping**
    - **Property 59: Character set affects written characters**
    - **Validates: Requirements 19.1, 19.2, 19.3, 19.4**

- [x] 6. Implement Terminal class (main emulator)
  - [x] 6.1 Create Terminal class with initialization
    - Accept TerminalConfig (cols, rows, scrollback)
    - Initialize screen buffer, cursor, attributes
    - Set up event emitters
    - _Requirements: 1.1, 2.1, 18.1_
  
  - [x] 6.2 Implement write method
    - Accept string or Uint8Array
    - Pass data to parser
    - _Requirements: 18.2_
  
  - [x] 6.3 Write property test for input type handling
    - **Property 55: API accepts multiple input types**
    - **Validates: Requirements 18.2**
  
  - [x] 6.4 Implement character writing logic
    - Write printable characters to buffer at cursor position
    - Apply current attributes
    - Handle wide characters (CJK)
    - Advance cursor
    - Handle auto-wrap mode
    - _Requirements: 2.2, 3.1, 3.2, 3.4, 3.5, 15.1, 15.2_
  
  - [x] 6.5 Write property tests for character writing
    - **Property 4: Cursor advances on character write**
    - **Property 9: Printable characters appear at cursor**
    - **Property 10: SGR attributes apply to written characters**
    - **Property 12: Wide characters occupy two cells**
    - **Property 13: Line-end behavior respects auto-wrap mode**
    - **Validates: Requirements 2.2, 3.1, 3.2, 3.4, 3.5, 15.1, 15.2**
  
  - [x] 6.6 Write property test for UTF-8 handling
    - **Property 11: UTF-8 decoding correctness**
    - **Validates: Requirements 3.3**
  
  - [x] 6.7 Implement cursor movement methods
    - Implement moveCursor, setCursorPosition
    - Handle boundary conditions
    - _Requirements: 2.4, 5.1-5.5_
  
  - [x] 6.8 Write property tests for cursor operations
    - **Property 5: Auto-wrap at line end**
    - **Property 6: Cursor movement sequences update position**
    - **Property 7: Cursor visibility state tracking**
    - **Property 8: Cursor position query accuracy**
    - **Validates: Requirements 2.3, 2.4, 2.5, 2.6**
  
  - [x] 6.9 Implement scrolling with scrollback
    - When scrolling up, push top line to scrollback
    - Respect scrollback size limit
    - Handle alternate screen (no scrollback)
    - _Requirements: 8.1, 8.2, 9.3_
  
  - [x] 6.10 Implement viewport management
    - Track viewport offset
    - Implement getViewportOffset and setViewportOffset
    - Handle auto-scroll on write
    - _Requirements: 8.3, 8.4_
  
  - [x] 6.11 Write property tests for viewport
    - **Property 27: Viewport offset tracking**
    - **Property 28: Auto-scroll behavior**
    - **Validates: Requirements 8.3, 8.4**
  
  - [x] 6.12 Implement terminal modes
    - Track auto-wrap mode
    - Track cursor visibility mode
    - Track application cursor keys mode
    - Track bracketed paste mode
    - _Requirements: 15.1, 15.2, 15.3, 15.4, 15.5_
  
  - [x] 6.13 Write property test for mode-dependent behavior
    - **Property 49: Bracketed paste mode wrapping**
    - **Validates: Requirements 15.5**
  
  - [x] 6.14 Implement tab stop management
    - Initialize default tab stops every 8 columns
    - Implement setTabStop and clearTabStop
    - Implement tab character handling
    - _Requirements: 14.1, 14.2, 14.3, 14.4_
  
  - [x] 6.15 Write property tests for tab stops
    - **Property 14: Tab moves to next tab stop**
    - **Property 47: Tab stop setting and usage**
    - **Property 48: Tab stop clearing**
    - **Validates: Requirements 14.2, 14.3, 14.4**
  
  - [x] 6.16 Implement scroll region management
    - Track scroll region top and bottom
    - Apply region to scroll operations
    - Allow cursor movement outside region
    - _Requirements: 10.1, 10.3, 10.4, 10.5_
  
  - [x] 6.17 Write property tests for scroll regions
    - **Property 34: Scroll region restricts scrolling**
    - **Property 36: Scroll region reset restores full scrolling**
    - **Property 37: Cursor movement ignores scroll region**
    - **Validates: Requirements 10.1, 10.3, 10.4, 10.5**
  
  - [x] 6.18 Implement event emission
    - Emit bell event
    - Emit title change event
    - Emit clipboard event
    - Emit data output event
    - Emit resize event
    - Emit state change event
    - _Requirements: 16.1, 16.2, 16.3, 16.4, 16.5_
  
  - [x] 6.19 Write property test for data output events
    - **Property 50: Data output event emission**
    - **Validates: Requirements 16.4**
  
  - [x] 6.20 Implement resize method
    - Resize screen buffer
    - Emit resize event
    - _Requirements: 1.2, 16.5_
  
  - [x] 6.21 Implement dispose method
    - Clean up WASM resources
    - Clear buffers
    - Remove event listeners
    - _Requirements: 18.5_
  
  - [x] 6.22 Implement query methods
    - getLine, getScrollbackLine, getScrollbackSize
    - getCursor
    - _Requirements: 18.3_

- [x] 7. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. Implement TerminalController class
  - [x] 8.1 Create controller initialization
    - Accept terminal, inputElement, displayElement, wasmInstance
    - Set up event listeners
    - _Requirements: 11.1, 13.1, 13.5_
  
  - [x] 8.2 Implement keyboard input handling
    - Listen for keydown events on input element
    - Convert KeyboardEvent to KeyEvent structure
    - Use libghostty-vt to encode key to escape sequences
    - Send encoded sequences to terminal
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_
  
  - [x] 8.3 Write property tests for key handling
    - **Property 38: KeyEvent conversion preserves key information**
    - **Property 39: Key encoding produces valid sequences**
    - **Property 40: Key encoding round-trip**
    - **Property 41: Mode-dependent key encoding**
    - **Validates: Requirements 11.2, 11.3, 11.4, 11.5, 15.4**
  
  - [x] 8.3 Implement paste handling
    - Listen for paste events
    - Handle bracketed paste mode
    - _Requirements: 15.5_
  
  - [x] 8.4 Implement focus management
    - Listen for focus and blur events
    - Update visual state
    - Prevent default browser shortcuts
    - Auto-focus on initialization
    - _Requirements: 13.1, 13.2, 13.3, 13.4, 13.5_
  
  - [x] 8.5 Implement mouse selection
    - Listen for mousedown, mousemove, mouseup
    - Track selection range
    - Highlight selected cells
    - _Requirements: 20.1, 20.2_
  
  - [x] 8.6 Implement copy handling
    - Listen for copy events
    - Extract text from selected cells
    - Handle wide characters and line breaks
    - Place text on clipboard
    - _Requirements: 20.3, 20.4, 20.5_
  
  - [x] 8.7 Write property test for text extraction
    - **Property 60: Text extraction preserves content**
    - **Validates: Requirements 20.3, 20.4**
  
  - [x] 8.8 Implement mount and unmount methods
    - Add/remove event listeners
    - Clean up resources
    - _Requirements: 18.5_

- [x] 9. Implement Renderer class
  - [x] 9.1 Create rendering infrastructure
    - Accept displayElement
    - Create render method
    - _Requirements: 12.1_
  
  - [x] 9.2 Implement screen rendering
    - Create span elements for each character
    - Apply CSS for colors and attributes
    - Handle wide characters
    - _Requirements: 12.2, 12.3, 12.5_
  
  - [x] 9.3 Write property tests for rendering
    - **Property 42: View synchronization with terminal state**
    - **Property 43: Rendering creates correct element structure**
    - **Property 44: Cell styling reflects attributes**
    - **Property 46: Wide character rendering spacing**
    - **Validates: Requirements 12.1, 12.2, 12.3, 12.5**
  
  - [x] 9.4 Implement cursor rendering
    - Render cursor at correct position
    - Apply cursor styling
    - _Requirements: 12.4_
  
  - [x] 9.5 Write property test for cursor rendering
    - **Property 45: Cursor renders at correct position**
    - **Validates: Requirements 12.4**
  
  - [x] 9.6 Optimize rendering performance
    - Implement incremental rendering (only update changed cells)
    - Batch DOM updates
    - _Design: Performance Considerations_

- [x] 10. Create terminal page integration
  - [x] 10.1 Create TerminalPage React component
    - Load WASM instance
    - Create Terminal instance
    - Create TerminalController instance
    - Render input and display elements
    - _Requirements: 11.1, 12.1, 13.1_
  
  - [x] 10.2 Add terminal page styling
    - Style input element (hidden but functional)
    - Style display element (monospace font, absolute positioning)
    - Style cursor
    - Style selection
    - _Requirements: 12.3, 12.4, 13.2, 13.3, 20.2_
  
  - [x] 10.3 Wire up terminal to shell simulation
    - Create simple echo shell for testing
    - Handle data output events
    - Send responses back to terminal
    - _Requirements: 16.4_

- [x] 11. Implement SampleShell class
  - [x] 11.1 Create SampleShell class with initialization
    - Accept ShellConfig with onOutput callback
    - Initialize prompt string (e.g., "$ ")
    - Track current input line
    - _Requirements: 21.1_
  
  - [x] 11.2 Implement input processing and command parsing
    - Process incoming data character by character
    - Handle backspace and line editing
    - Parse command and arguments on Enter
    - Route to appropriate command handler
    - _Requirements: 21.1_
  
  - [x] 11.3 Implement ls command handler
    - Output five dummy filenames (e.g., "file1.txt", "file2.txt", etc.)
    - Format output with newlines
    - Display prompt after output
    - _Requirements: 21.2_
  
  - [x] 11.4 Write property test for ls command
    - **Property 61: ls command output format**
    - **Validates: Requirements 21.2**
  
  - [x] 11.5 Implement echo command handler
    - Extract arguments after "echo" command
    - Output arguments back to terminal
    - Display prompt after output
    - _Requirements: 21.3_
  
  - [x] 11.6 Write property test for echo command
    - **Property 62: echo command reflects input**
    - **Validates: Requirements 21.3**
  
  - [x] 11.7 Implement Ctrl+L handler
    - Detect Ctrl+L control sequence
    - Send CSI J (clear screen) escape sequence
    - Send CSI H (cursor home) escape sequence
    - Display prompt after clearing
    - _Requirements: 21.4_
  
  - [x] 11.8 Write property test for Ctrl+L
    - **Property 63: Ctrl+L clears screen**
    - **Validates: Requirements 21.4**
  
  - [x] 11.9 Implement unknown command handler
    - Output error message for unrecognized commands
    - Format: "command: command not found" or similar
    - Display prompt after error
    - _Requirements: 21.5_
  
  - [x] 11.10 Write property test for unknown commands
    - **Property 64: Unknown command error handling**
    - **Validates: Requirements 21.5**
  
  - [x] 11.11 Integrate SampleShell with terminal page
    - Replace EchoShell with SampleShell in TerminalPage
    - Wire up data output events to SampleShell.processInput
    - Wire up SampleShell.onOutput to terminal.write
    - Display initial prompt on load
    - _Requirements: 21.1_

- [ ] 12. Implement Backend Server for PTY integration (caTTY-node-pty project)
  - [ ] 12.1 Set up Node.js backend project structure in caTTY-node-pty
    - Update caTTY-node-pty/package.json with correct name and scripts
    - Install `ws` (WebSocket library) dependency in caTTY-node-pty
    - Configure TypeScript for Node.js backend (target: ES2020, module: NodeNext) in caTTY-node-pty/tsconfig.json
    - Add build and start scripts to caTTY-node-pty/package.json
    - _Requirements: 22.1_
  
  - [ ] 12.2 Implement BackendServer class in caTTY-node-pty
    - Create caTTY-node-pty/src/BackendServer.ts with BackendServerConfig interface
    - Implement constructor accepting port and optional shell configuration
    - Implement start() method to initialize WebSocket server
    - Implement stop() method for graceful shutdown
    - Track active connections in a Map<WebSocket, PTY>
    - _Requirements: 22.1_
  
  - [ ] 12.3 Implement PTY spawning on connection in caTTY-node-pty
    - Handle WebSocket 'connection' event in BackendServer
    - Spawn PTY process using `@lydell/node-pty` with appropriate shell (bash for Unix, powershell.exe for Windows)
    - Configure PTY with default dimensions (80 cols, 40 rows)
    - Store PTY instance in connection map
    - Send initial shell prompt to client
    - _Requirements: 22.2, 22.3, 22.4_
  
  - [ ]* 12.4 Write property test for PTY spawn
    - **Property 65: PTY spawn on connection**
    - **Validates: Requirements 22.2, 22.4**
  
  - [ ] 12.5 Implement PTY to WebSocket data forwarding in caTTY-node-pty
    - Listen for PTY 'data' events in BackendServer
    - Forward PTY output to WebSocket client using ws.send()
    - Handle both text and binary data appropriately
    - Add error handling for send failures
    - _Requirements: 22.5, 23.2_
  
  - [ ]* 12.6 Write property test for PTY output forwarding in caTTY-node-pty
    - **Property 66: PTY output forwarding**
    - **Validates: Requirements 22.5**
  
  - [ ] 12.7 Implement WebSocket to PTY data forwarding in caTTY-node-pty
    - Listen for WebSocket 'message' events in BackendServer
    - Write client data to PTY process using pty.write()
    - Handle both string and Buffer message types
    - Add error handling for write failures
    - _Requirements: 23.1_
  
  - [ ]* 12.8 Write property test for client input forwarding in caTTY-node-pty
    - **Property 67: Client input forwarding**
    - **Validates: Requirements 23.1, 23.2**
  
  - [ ] 12.9 Implement resize message handling in caTTY-node-pty
    - Define resize message protocol (JSON: {type: 'resize', cols: number, rows: number})
    - Parse resize messages from WebSocket client in BackendServer
    - Call pty.resize(cols, rows) with new dimensions
    - Add validation for dimension values
    - _Requirements: 23.5_
  
  - [ ]* 12.10 Write property test for resize propagation in caTTY-node-pty
    - **Property 68: Terminal resize propagation**
    - **Validates: Requirements 23.5**
  
  - [ ] 12.11 Implement connection cleanup in caTTY-node-pty
    - Handle WebSocket 'close' event in BackendServer
    - Terminate associated PTY process using pty.kill()
    - Remove connection from tracking map
    - Remove all PTY event listeners
    - Log disconnection with connection details
    - _Requirements: 24.1, 24.5_
  
  - [ ]* 12.12 Write property test for connection cleanup in caTTY-node-pty
    - **Property 69: Connection cleanup on disconnect**
    - **Validates: Requirements 24.1, 24.5**
  
  - [ ] 12.13 Implement PTY exit handling in caTTY-node-pty
    - Listen for PTY 'exit' events in BackendServer
    - Close associated WebSocket connection using ws.close()
    - Log exit code and signal
    - Remove connection from tracking map
    - _Requirements: 24.2_
  
  - [ ]* 12.14 Write property test for PTY exit cleanup in caTTY-node-pty
    - **Property 70: PTY exit cleanup**
    - **Validates: Requirements 24.2**
  
  - [ ] 12.15 Implement error handling in caTTY-node-pty
    - Handle PTY spawn errors with try-catch in BackendServer
    - Handle WebSocket 'error' events
    - Log all errors with context (connection ID, error details)
    - Clean up resources on error (close WebSocket, kill PTY)
    - Send error messages to client when appropriate
    - _Requirements: 24.4_
  
  - [ ]* 12.16 Write integration tests for backend server in caTTY-node-pty
    - Test complete connection lifecycle
    - Test bidirectional data flow
    - Test error scenarios
    - _Requirements: 22.1-22.5, 23.1-23.5, 24.1-24.5_

- [ ] 13. Extend TerminalController for WebSocket integration (caTTY-ts project)
  - [ ] 13.1 Add WebSocket connection management to TerminalController in caTTY-ts
    - Add private websocket: WebSocket | null property to TerminalController
    - Add private connectionState: 'disconnected' | 'connecting' | 'connected' | 'error' property
    - Implement connectWebSocket(url: string) method to connect to caTTY-node-pty backend
    - Implement disconnectWebSocket() method
    - Implement isConnected() method to check connection state
    - _Requirements: 25.1_
  
  - [ ]* 13.2 Write property test for connection establishment in caTTY-ts
    - **Property 71: WebSocket connection establishment**
    - **Validates: Requirements 25.1**
  
  - [ ] 13.3 Implement WebSocket event handlers in TerminalController (caTTY-ts)
    - Implement handleWebSocketOpen() to set connectionState to 'connected'
    - Implement handleWebSocketMessage(event: MessageEvent) to receive data from caTTY-node-pty backend
    - Implement handleWebSocketClose() to set connectionState to 'disconnected'
    - Implement handleWebSocketError(event: Event) to set connectionState to 'error'
    - Bind and attach event listeners in connectWebSocket()
    - _Requirements: 23.3, 23.4_
  
  - [ ] 13.4 Implement data forwarding from WebSocket to terminal in caTTY-ts
    - In handleWebSocketMessage, extract data from event.data (from caTTY-node-pty)
    - Convert data to Uint8Array if needed (handle both string and ArrayBuffer)
    - Call terminal.write() with received data
    - Add error handling for write failures
    - _Requirements: 23.4, 25.2, 25.4_
  
  - [ ]* 13.5 Write property test for shell output display in caTTY-ts
    - **Property 72: Real shell output display**
    - **Validates: Requirements 25.2, 25.4**
  
  - [ ] 13.6 Implement data forwarding from terminal to WebSocket backend in caTTY-ts
    - Modify terminal's onDataOutput event handler to check if WebSocket is connected
    - If connected, send data through WebSocket to caTTY-node-pty using websocket.send()
    - If not connected, fall back to SampleShell behavior
    - Handle both string and Uint8Array data types
    - Add error handling for send failures
    - _Requirements: 23.3, 25.3_
  
  - [ ]* 13.7 Write property test for command execution in caTTY-ts
    - **Property 73: Command execution through PTY**
    - **Validates: Requirements 25.3**
  
  - [ ] 13.8 Implement resize message sending to backend in caTTY-ts
    - Listen for terminal resize events
    - Create resize message object: {type: 'resize', cols: number, rows: number}
    - Serialize to JSON and send through WebSocket to caTTY-node-pty
    - Only send if WebSocket is connected
    - _Requirements: 23.5_
  
  - [ ] 13.9 Implement connection failure handling in caTTY-ts
    - In handleWebSocketError, log error details
    - Display error message in terminal using terminal.write()
    - Set connectionState to 'error'
    - Optionally initialize SampleShell as fallback
    - Provide visual indicator of connection status
    - _Requirements: 25.5_
  
  - [ ]* 13.10 Write property test for connection failure fallback in caTTY-ts
    - **Property 74: Connection failure fallback**
    - **Validates: Requirements 25.5**
  
  - [ ] 13.11 Update unmount method for WebSocket cleanup in caTTY-ts
    - Check if WebSocket exists and is connected
    - Call disconnectWebSocket() to close connection to caTTY-node-pty
    - Remove all WebSocket event listeners
    - Set websocket property to null
    - _Requirements: 24.3_
  
  - [ ]* 13.12 Write integration tests for WebSocket controller in caTTY-ts
    - Test connection establishment and data flow with caTTY-node-pty
    - Test fallback to SampleShell on failure
    - Test cleanup on page unload
    - _Requirements: 25.1-25.5_

- [ ] 14. Update terminal page for backend integration (caTTY-ts project)
  - [ ] 14.1 Add WebSocket connection configuration to TerminalPage in caTTY-ts
    - Add WebSocket URL state (default: 'ws://localhost:3000' to connect to caTTY-node-pty)
    - Add connection mode state: 'sampleshell' | 'websocket'
    - Add connection status state: 'disconnected' | 'connecting' | 'connected' | 'error'
    - Create UI controls for toggling connection mode
    - Display current connection status in UI
    - _Requirements: 25.1_
  
  - [ ] 14.2 Implement automatic connection on page load in caTTY-ts
    - In useEffect hook, check connection mode on mount
    - If mode is 'websocket', call controller.connectWebSocket(url) to connect to caTTY-node-pty
    - If mode is 'sampleshell', initialize SampleShell
    - Handle connection success by updating status state
    - Handle connection failure by updating status and optionally falling back to SampleShell
    - _Requirements: 25.1, 25.5_
  
  - [ ] 14.3 Add connection status indicator UI in caTTY-ts
    - Create status indicator component showing connection state to caTTY-node-pty
    - Display colored badge (green=connected, yellow=connecting, red=error, gray=disconnected)
    - Show error messages in terminal or status area for connection failures
    - Add reconnect button that appears when connection fails
    - Implement reconnect button handler to retry WebSocket connection to caTTY-node-pty
    - _Requirements: 25.5_
  
  - [ ] 14.4 Update terminal page documentation in caTTY-ts
    - Add comments in TerminalPage.tsx explaining WebSocket integration with caTTY-node-pty
    - Document WebSocket URL configuration and how to change it
    - Add troubleshooting section for common connection issues (CORS, port conflicts, firewall)
    - Document the message protocol (data messages vs resize messages)
    - Reference caTTY-node-pty README for backend server setup
    - _Requirements: 25.1_

- [ ] 15. Create backend server entry point (caTTY-node-pty project)
  - [ ] 15.1 Create server.ts entry point in caTTY-node-pty
    - Create caTTY-node-pty/src/server.ts file
    - Import BackendServer class
    - Read port from environment variable PORT or default to 3000
    - Instantiate BackendServer with configuration
    - Call server.start() and log startup message with port
    - Add graceful shutdown handlers for SIGINT and SIGTERM signals
    - In shutdown handler, call server.stop() and log shutdown message
    - Add error handling for startup failures
    - _Requirements: 22.1_
  
  - [ ] 15.2 Add npm scripts for backend in caTTY-node-pty
    - Update caTTY-node-pty/package.json name to "catty-backend" or similar
    - Add "build" script: "tsc" to compile TypeScript
    - Add "start" script: "node dist/server.js" to run compiled server
    - Add "dev" script: "tsx watch src/server.ts" for development with auto-reload (requires tsx package)
    - Add "clean" script to remove dist directory
    - Install tsx as dev dependency in caTTY-node-pty for development mode
    - _Requirements: 22.1_
  
  - [ ] 15.3 Create backend README in caTTY-node-pty
    - Create caTTY-node-pty/README.md
    - Document installation: "pnpm install" in caTTY-node-pty directory
    - Document build process: "pnpm build"
    - Document starting server: "pnpm start" (production) and "pnpm dev" (development)
    - Document configuration: PORT environment variable (default: 3000)
    - Document WebSocket protocol: connection flow, message types (data vs resize), message format
    - Add troubleshooting section: port conflicts, firewall issues, PTY spawn errors
    - Add example client connection code from caTTY-ts
    - Document how caTTY-ts connects to this backend
    - _Requirements: 22.1_

- [ ] 16. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
