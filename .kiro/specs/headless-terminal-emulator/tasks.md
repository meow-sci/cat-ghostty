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
  - [x] 12.1 Set up Node.js backend project structure in caTTY-node-pty
    - Update caTTY-node-pty/package.json with correct name and scripts
    - Install `ws` (WebSocket library) dependency in caTTY-node-pty
    - Configure TypeScript for Node.js backend (target: ES2020, module: NodeNext) in caTTY-node-pty/tsconfig.json
    - Add build and start scripts to caTTY-node-pty/package.json
    - _Requirements: 22.1_
  
  - [x] 12.2 Implement BackendServer class in caTTY-node-pty
    - Create caTTY-node-pty/src/BackendServer.ts with BackendServerConfig interface
    - Implement constructor accepting port and optional shell configuration
    - Implement start() method to initialize WebSocket server
    - Implement stop() method for graceful shutdown
    - Track active connections in a Map<WebSocket, PTY>
    - _Requirements: 22.1_
  
  - [x] 12.3 Implement PTY spawning on connection in caTTY-node-pty
    - Handle WebSocket 'connection' event in BackendServer
    - Spawn PTY process using `@lydell/node-pty` with appropriate shell (bash for Unix, powershell.exe for Windows)
    - Configure PTY with default dimensions (80 cols, 40 rows)
    - Store PTY instance in connection map
    - Send initial shell prompt to client
    - _Requirements: 22.2, 22.3, 22.4_
  
  - [x] 12.4 Write property test for PTY spawn
    - **Property 65: PTY spawn on connection**
    - **Validates: Requirements 22.2, 22.4**
  
  - [x] 12.5 Implement PTY to WebSocket data forwarding in caTTY-node-pty
    - Listen for PTY 'data' events in BackendServer
    - Forward PTY output to WebSocket client using ws.send()
    - Handle both text and binary data appropriately
    - Add error handling for send failures
    - _Requirements: 22.5, 23.2_
  
  - [x] 12.6 Write property test for PTY output forwarding in caTTY-node-pty
    - **Property 66: PTY output forwarding**
    - **Validates: Requirements 22.5**
  
  - [x] 12.7 Implement WebSocket to PTY data forwarding in caTTY-node-pty
    - Listen for WebSocket 'message' events in BackendServer
    - Write client data to PTY process using pty.write()
    - Handle both string and Buffer message types
    - Add error handling for write failures
    - _Requirements: 23.1_
  
  - [x] 12.8 Write property test for client input forwarding in caTTY-node-pty
    - **Property 67: Client input forwarding**
    - **Validates: Requirements 23.1, 23.2**
  
  - [x] 12.9 Implement resize message handling in caTTY-node-pty
    - Define resize message protocol (JSON: {type: 'resize', cols: number, rows: number})
    - Parse resize messages from WebSocket client in BackendServer
    - Call pty.resize(cols, rows) with new dimensions
    - Add validation for dimension values
    - _Requirements: 23.5_
  
  - [x] 12.10 Write property test for resize propagation in caTTY-node-pty
    - **Property 68: Terminal resize propagation**
    - **Validates: Requirements 23.5**
  
  - [x] 12.11 Implement connection cleanup in caTTY-node-pty
    - Handle WebSocket 'close' event in BackendServer
    - Terminate associated PTY process using pty.kill()
    - Remove connection from tracking map
    - Remove all PTY event listeners
    - Log disconnection with connection details
    - _Requirements: 24.1, 24.5_
  
  - [x] 12.12 Write property test for connection cleanup in caTTY-node-pty
    - **Property 69: Connection cleanup on disconnect**
    - **Validates: Requirements 24.1, 24.5**
  
  - [x] 12.13 Implement PTY exit handling in caTTY-node-pty
    - Listen for PTY 'exit' events in BackendServer
    - Close associated WebSocket connection using ws.close()
    - Log exit code and signal
    - Remove connection from tracking map
    - _Requirements: 24.2_
  
  - [x] 12.14 Write property test for PTY exit cleanup in caTTY-node-pty
    - **Property 70: PTY exit cleanup**
    - **Validates: Requirements 24.2**
  
  - [x] 12.15 Implement error handling in caTTY-node-pty
    - Handle PTY spawn errors with try-catch in BackendServer
    - Handle WebSocket 'error' events
    - Log all errors with context (connection ID, error details)
    - Clean up resources on error (close WebSocket, kill PTY)
    - Send error messages to client when appropriate
    - _Requirements: 24.4_
  
  - [x] 12.16 Write integration tests for backend server in caTTY-node-pty
    - Test complete connection lifecycle
    - Test bidirectional data flow
    - Test error scenarios
    - _Requirements: 22.1-22.5, 23.1-23.5, 24.1-24.5_

- [-] 13. Extend TerminalController for WebSocket integration (caTTY-ts project)
  - [x] 13.1 Add WebSocket connection management to TerminalController in caTTY-ts
    - Add private websocket: WebSocket | null property to TerminalController
    - Add private connectionState: 'disconnected' | 'connecting' | 'connected' | 'error' property
    - Implement connectWebSocket(url: string) method to connect to caTTY-node-pty backend
    - Implement disconnectWebSocket() method
    - Implement isConnected() method to check connection state
    - _Requirements: 25.1_
  
  - [x] 13.2 Write property test for connection establishment in caTTY-ts
    - **Property 71: WebSocket connection establishment**
    - **Validates: Requirements 25.1**
  
  - [x] 13.3 Implement WebSocket event handlers in TerminalController (caTTY-ts)
    - Implement handleWebSocketOpen() to set connectionState to 'connected'
    - Implement handleWebSocketMessage(event: MessageEvent) to receive data from caTTY-node-pty backend
    - Implement handleWebSocketClose() to set connectionState to 'disconnected'
    - Implement handleWebSocketError(event: Event) to set connectionState to 'error'
    - Bind and attach event listeners in connectWebSocket()
    - _Requirements: 23.3, 23.4_
  
  - [x] 13.4 Implement data forwarding from WebSocket to terminal in caTTY-ts
    - In handleWebSocketMessage, extract data from event.data (from caTTY-node-pty)
    - Convert data to Uint8Array if needed (handle both string and ArrayBuffer)
    - Call terminal.write() with received data
    - Add error handling for write failures
    - _Requirements: 23.4, 25.2, 25.4_
  
  - [x] 13.5 Write property test for shell output display in caTTY-ts
    - **Property 72: Real shell output display**
    - **Validates: Requirements 25.2, 25.4**
  
  - [x] 13.6 Implement data forwarding from terminal to WebSocket backend in caTTY-ts
    - Modify terminal's onDataOutput event handler to check if WebSocket is connected
    - If connected, send data through WebSocket to caTTY-node-pty using websocket.send()
    - If not connected, fall back to SampleShell behavior
    - Handle both string and Uint8Array data types
    - Add error handling for send failures
    - _Requirements: 23.3, 25.3_
  
  - [x] 13.7 Write property test for command execution in caTTY-ts
    - **Property 73: Command execution through PTY**
    - **Validates: Requirements 25.3**
  
  - [x] 13.8 Implement resize message sending to backend in caTTY-ts
    - Listen for terminal resize events
    - Create resize message object: {type: 'resize', cols: number, rows: number}
    - Serialize to JSON and send through WebSocket to caTTY-node-pty
    - Only send if WebSocket is connected
    - _Requirements: 23.5_
  
  - [x] 13.9 Implement connection failure handling in caTTY-ts
    - In handleWebSocketError, log error details
    - Display error message in terminal using terminal.write()
    - Set connectionState to 'error'
    - Optionally initialize SampleShell as fallback
    - Provide visual indicator of connection status
    - _Requirements: 25.5_
  
  - [x] 13.10 Write property test for connection failure fallback in caTTY-ts
    - **Property 74: Connection failure fallback**
    - **Validates: Requirements 25.5**
  
  - [x] 13.11 Update unmount method for WebSocket cleanup in caTTY-ts
    - Check if WebSocket exists and is connected
    - Call disconnectWebSocket() to close connection to caTTY-node-pty
    - Remove all WebSocket event listeners
    - Set websocket property to null
    - _Requirements: 24.3_
  
  - [x] 13.12 Write integration tests for WebSocket controller in caTTY-ts
    - Test connection establishment and data flow with caTTY-node-pty
    - Test fallback to SampleShell on failure
    - Test cleanup on page unload
    - _Requirements: 25.1-25.5_

- [x] 14. Update terminal page for backend integration (caTTY-ts project)
  - [x] 14.1 Add WebSocket connection configuration to TerminalPage in caTTY-ts
    - Add WebSocket URL state (default: 'ws://localhost:4444' to connect to caTTY-node-pty)
    - Add connection mode state: 'sampleshell' | 'websocket' (default 'websocket')
    - Add connection status state: 'disconnected' | 'connecting' | 'connected' | 'error'
    - Create UI controls for toggling connection mode
    - Display current connection status in UI
    - _Requirements: 25.1_
  
  - [x] 14.2 Implement automatic connection on page load in caTTY-ts
    - In useEffect hook, check connection mode on mount
    - If mode is 'websocket', call controller.connectWebSocket(url) to connect to caTTY-node-pty
    - If mode is 'sampleshell', initialize SampleShell
    - Handle connection success by updating status state
    - Handle connection failure by updating status and optionally falling back to SampleShell
    - _Requirements: 25.1, 25.5_
  
  - [x] 14.3 Add connection status indicator UI in caTTY-ts
    - Create status indicator component showing connection state to caTTY-node-pty
    - Display colored badge (green=connected, yellow=connecting, red=error, gray=disconnected)
    - Show error messages in terminal or status area for connection failures
    - Add reconnect button that appears when connection fails
    - Implement reconnect button handler to retry WebSocket connection to caTTY-node-pty
    - _Requirements: 25.5_
  
  - [x] 14.4 Update terminal page documentation in caTTY-ts
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

- [-] 16. Implement Kitty Graphics Protocol support
  - [x] 16.1 Create image data structures and types
    - Create ImageData interface with id, data (ImageBitmap), width, height, format
    - Create ImagePlacement interface with placementId, imageId, position, dimensions, source rectangle, zIndex, unicodePlaceholder
    - Create TransmissionState interface for tracking chunked transmissions
    - Create GraphicsParams interface for parsed graphics command parameters
    - _Requirements: 26.1, 26.3, 26.4, 26.5_
  
  - [x] 16.2 Implement KittyGraphicsParser class
    - Create KittyGraphicsParser class that parses ESC_G sequences
    - Implement parseGraphicsCommand() to extract action and parameters
    - Implement handleTransmission() for image data transmission commands
    - Implement handleDisplay() for image placement commands
    - Implement handleDelete() for image deletion commands
    - Parse all graphics parameters (action, imageId, placementId, format, dimensions, etc.)
    - _Requirements: 26.1_
  
  - [x] 16.3 Write property test for graphics command parsing
    - **Property 75: Graphics command parsing**
    - **Validates: Requirements 26.1**
  
  - [x] 16.4 Implement image decoding
    - Implement decodeImageData() method to decode base64 image data
    - Support PNG, JPEG, and GIF formats
    - Convert decoded data to ImageBitmap for efficient rendering
    - Handle decoding errors gracefully
    - _Requirements: 26.2, 30.1, 30.2, 30.3_
  
  - [x] 16.5 Write property test for image decoding
    - **Property 76: Image data decoding**
    - **Validates: Requirements 26.2, 30.1, 30.2, 30.3**
  
  - [x] 16.6 Implement ImageManager class
    - Create ImageManager class with image and placement storage
    - Implement storeImage() to store decoded images by ID
    - Implement getImage() and deleteImage() methods
    - Implement createPlacement() to create image placements
    - Implement getPlacement() and deletePlacement() methods
    - Track active placements (visible on screen) separately from scrollback placements
    - _Requirements: 26.3, 26.4, 29.1, 29.2, 29.3, 34.1, 34.2_
  
  - [x] 16.7 Write property tests for image storage
    - **Property 77: Image storage with ID**
    - **Property 78: Placement creation at cursor**
    - **Validates: Requirements 26.3, 26.4, 26.5, 34.1**
  
  - [x] 16.8 Implement chunked transmission support
    - Implement startTransmission() to begin chunked transmission
    - Implement addChunk() to accumulate image data chunks
    - Implement completeTransmission() to finalize and decode image
    - Implement cancelTransmission() for error handling
    - Track multiple concurrent transmissions independently
    - _Requirements: 31.1, 31.2, 31.3, 31.4, 31.5_
  
  - [x] 16.9 Write property tests for chunked transmission
    - **Property 96: Chunked transmission accumulation**
    - **Property 97: Non-blocking chunked transmission**
    - **Property 98: Transmission completion finalization**
    - **Property 99: Transmission failure cleanup**
    - **Property 100: Concurrent transmission independence**
    - **Validates: Requirements 31.1, 31.2, 31.3, 31.4, 31.5**
  
  - [x] 16.10 Implement image placement positioning
    - Implement grid coordinate positioning (row/col)
    - Implement pixel to cell dimension conversion
    - Implement source rectangle cropping logic
    - Implement native dimension fallback
    - Implement screen boundary clipping
    - _Requirements: 27.1, 27.2, 27.3, 27.4, 27.5_
  
  - [x] 16.11 Write property tests for positioning
    - **Property 79: Grid coordinate positioning**
    - **Property 80: Pixel to cell conversion**
    - **Property 81: Source rectangle cropping**
    - **Property 82: Native dimension fallback**
    - **Property 83: Screen boundary clipping**
    - **Validates: Requirements 27.1, 27.2, 27.3, 27.4, 27.5**
  
  - [x] 16.12 Implement image scrolling behavior
    - Implement handleScroll() in ImageManager to move placements with content
    - Move placements to scrollback buffer when scrolling off top
    - Remove placements when scrolling off bottom (reverse scroll)
    - Prevent scrollback preservation in alternate screen mode
    - _Requirements: 28.1, 28.2, 28.3, 28.4, 28.5_
  
  - [x] 16.13 Write property tests for scrolling
    - **Property 84: Image scrolling with content**
    - **Property 85: Scrollback buffer image preservation**
    - **Property 86: Reverse scroll image removal**
    - **Property 87: Scrollback image display**
    - **Property 88: Alternate screen no image scrollback**
    - **Validates: Requirements 28.1, 28.2, 28.3, 28.4, 28.5**
  
  - [x] 16.14 Implement image deletion operations
    - Implement deletion by image ID (removes all placements)
    - Implement deletion by placement ID (removes single placement)
    - Implement delete all visible placements
    - Free image data from memory when deleted
    - Update display when placements are removed
    - _Requirements: 29.1, 29.2, 29.3, 29.4, 29.5_
  
  - [x] 16.15 Write property tests for deletion
    - **Property 89: Image deletion by image ID**
    - **Property 90: Placement deletion by placement ID**
    - **Property 91: Delete all visible placements**
    - **Property 92: Image data memory cleanup**
    - **Property 93: Display update on placement deletion**
    - **Validates: Requirements 29.1, 29.2, 29.3, 29.4, 29.5**
  
  - [x] 16.16 Implement terminal operation integration
    - Implement handleClear() to remove images in cleared regions
    - Remove images on line erase operations
    - Shift images on line insertion/deletion
    - Reposition images on terminal resize
    - _Requirements: 33.1, 33.2, 33.3, 33.4, 33.5_
  
  - [x] 16.17 Write property tests for terminal operations
    - **Property 106: Clear screen removes images**
    - **Property 107: Line erase removes images**
    - **Property 108: Line insertion shifts images**
    - **Property 109: Line deletion shifts images**
    - **Property 110: Resize repositions images**
    - **Validates: Requirements 33.1, 33.2, 33.3, 33.4, 33.5**
  
  - [x] 16.18 Implement ID management
    - Handle image ID reuse (replace previous data)
    - Handle placement ID reuse (replace previous placement)
    - Implement automatic ID generation when not specified
    - _Requirements: 34.3, 34.4, 34.5_
  
  - [x] 16.19 Write property tests for ID management
    - **Property 111: Image ID reuse replaces data**
    - **Property 112: Placement ID reuse replaces placement**
    - **Property 113: Automatic ID generation**
    - **Validates: Requirements 34.3, 34.4, 34.5**
  
  - [x] 16.20 Implement transparency support
    - Preserve alpha channel in images
    - Handle images without alpha channel as opaque
    - Ensure proper layering with text content
    - Update transparent images when background color changes
    - _Requirements: 35.1, 35.2, 35.3, 35.4, 35.5_
  
  - [x] 16.21 Write property tests for transparency
    - **Property 114: Alpha channel preservation**
    - **Property 115: Transparent pixel rendering**
    - **Property 116: Opaque image handling**
    - **Property 117: Image text layering**
    - **Property 118: Background color change updates transparency**
    - **Validates: Requirements 35.1, 35.2, 35.3, 35.4, 35.5**
  
  - [x] 16.22 Implement Unicode placeholder support
    - Write placeholder character to grid when placement created
    - Create bidirectional association between cell and placement
    - Remove placement when placeholder is erased
    - Move placement when placeholder scrolls
    - Remove placement when placeholder is overwritten
    - _Requirements: 36.1, 36.2, 36.3, 36.4, 36.5_
  
  - [ ] 16.23 Write property tests for Unicode placeholders
    - **Property 119: Unicode placeholder association**
    - **Property 120: Placeholder erase removes image**
    - **Property 121: Placeholder scroll moves image**
    - **Property 122: Placeholder overwrite removes image**
    - **Validates: Requirements 36.1, 36.2, 36.3, 36.4, 36.5**
  
  - [ ] 16.24 Integrate graphics parser with terminal
    - Add KittyGraphicsParser instance to Terminal class
    - Route ESC_G sequences to graphics parser
    - Connect parser to ImageManager
    - Emit events for image-related actions
    - _Requirements: 26.1_
  
  - [ ] 16.25 Update Renderer for image display
    - Implement renderImages() method to render all visible placements
    - Implement renderImagePlacement() to create image elements
    - Position image elements at correct grid coordinates
    - Size image elements according to placement dimensions
    - Apply CSS clipping for source rectangles
    - Remove image elements when placements are deleted
    - _Requirements: 32.1, 32.2, 32.3, 32.4, 32.5_
  
  - [ ] 16.26 Write property tests for image rendering
    - **Property 101: Image element creation for placements**
    - **Property 102: Image element positioning**
    - **Property 103: Image element sizing**
    - **Property 104: CSS clipping for source rectangle**
    - **Property 105: Image element removal**
    - **Validates: Requirements 32.1, 32.2, 32.3, 32.4, 32.5**
  
  - [ ] 16.27 Implement animated GIF support
    - Ensure ImageBitmap or img element preserves animation
    - Test that animated GIFs play correctly
    - _Requirements: 30.4_
  
  - [ ] 16.28 Write property test for animated GIFs
    - **Property 94: Animated GIF support**
    - **Validates: Requirements 30.4**
  
  - [ ] 16.29 Implement error handling for images
    - Handle unsupported image formats with error event
    - Handle decoding failures gracefully
    - Handle invalid graphics commands without crashing
    - Log errors for debugging
    - _Requirements: 30.5_
  
  - [ ] 16.30 Write property test for error handling
    - **Property 95: Unsupported format error handling**
    - **Validates: Requirements 30.5**
  
  - [ ] 16.31 Add Terminal API methods for images
    - Implement getVisibleImagePlacements() method
    - Implement getScrollbackImagePlacements() method
    - Update dispose() to clean up image resources
    - _Requirements: 18.5, 26.4_
  
  - [ ] 16.32 Write integration tests for Kitty Graphics Protocol
    - Test complete image transmission and display workflow
    - Test image scrolling through scrollback
    - Test image deletion and cleanup
    - Test Unicode placeholder integration
    - Test interaction with terminal operations (clear, resize, etc.)
    - _Requirements: 26.1-36.5_

- [ ] 17. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 18. Performance Optimizations
  - [x] 18.1 Implement dirty row tracking (HIGHEST PRIORITY)
    - **Impact:** Massive (20-50x faster for typical terminal usage)
    - **Effort:** Low
    - **Problem:** Currently, every state change triggers a full screen render of all 1,920 cells (80×24), even when only 1-2 rows changed
    - **Solution:** Track which rows have been modified and only render those rows
    - **Implementation Steps:**
      1. Add `private dirtyRows: Set<number> = new Set()` to Terminal class (caTTY-ts/src/ts/terminal/Terminal.ts)
      2. Mark rows as dirty in all methods that modify buffer content:
         - `handlePrintable()`: Add `this.dirtyRows.add(cursor.row)` after writing character
         - `handleLineFeed()`: Add `this.dirtyRows.add(cursor.row)` when moving to new row
         - `scrollUp()`: Add all visible rows to dirty set (0 to config.rows-1)
         - `scrollDown()`: Add all visible rows to dirty set
         - `eraseInDisplay()`: Add affected rows based on erase mode (0/1/2)
         - `eraseInLine()`: Add `this.dirtyRows.add(cursor.row)`
         - `insertLines()`, `deleteLines()`: Add affected row range
         - `clearRegion()`: Add all rows in the cleared region
      3. Add public method `getDirtyRows(): Set<number>` to Terminal class
      4. Add public method `clearDirtyRows(): void` to Terminal class
      5. Modify `Renderer.render()` method (caTTY-ts/src/ts/terminal/Renderer.ts):
         - Call `const dirtyRows = terminal.getDirtyRows()` at start
         - Add early return if `dirtyRows.size === 0`
         - Change main loop from `for (let row = 0; row < config.rows; row++)` to `for (const row of dirtyRows)`
         - Call `terminal.clearDirtyRows()` at end of render
      6. Handle cursor separately since it may move without dirtying rows
      7. Add special case: when cursor moves between rows, mark both old and new cursor rows as dirty
    - **Testing:**
      - Write property test verifying only dirty rows are rendered
      - Test that typing a character only marks one row as dirty
      - Test that scrolling marks all rows as dirty
      - Test that cursor movement marks appropriate rows as dirty
    - **Property 75: Dirty row tracking correctness**
    - **Validates: Performance requirement - minimize unnecessary rendering**
  
  - [x] 18.2 Implement cell batching for consecutive same-styled cells (HIGH PRIORITY)
    - **Impact:** High (70-90% reduction in DOM elements, 2-3x faster rendering)
    - **Effort:** Medium
    - **Problem:** Each character is rendered as an individual `<span>` element with absolute positioning. For 80×24 terminal = 1,920 DOM elements
    - **Solution:** Batch consecutive cells with identical styling into single span elements
    - **Implementation Steps:**
      1. Create helper method `cellStylesEqual(a: Cell, b: Cell): boolean` in Renderer class
         - Compare fg, bg, bold, italic, underline, inverse, strikethrough, url
         - Return true only if all style properties match
         - Ignore char and width in comparison
      2. Modify `renderLine()` method in Renderer class:
         - Track `runStart` index at beginning of line
         - Iterate through cells, checking if next cell has same style
         - When style changes or line ends, create single span for the run
         - Span should contain concatenated text from all cells in run
         - Position span at `runStart` column using `left: ${runStart}ch`
         - Set span width to run length if needed
      3. Create `createRunSpan(cell: Cell, col: number, text: string): HTMLElement` method:
         - Create span element with absolute positioning
         - Set `left: ${col}ch`
         - Set `textContent` to the concatenated text
         - Apply styles using existing `applyStyles()` method
         - Return the span element
      4. Update incremental rendering cache to track runs instead of individual cells:
         - Cache key should include run start position and length
         - Compare cached run with current run to detect changes
         - Only recreate span if run content or style changed
      5. Handle wide characters carefully:
         - Wide characters (width=2) should not break runs unnecessarily
         - Skip continuation cells (width=0) when building run text
      6. Handle empty cells (spaces) in runs:
         - Include spaces in run text to maintain positioning
         - Don't break runs just because cell.char is empty
    - **Testing:**
      - Write property test verifying consecutive same-styled cells are batched
      - Test that style changes create new spans
      - Test that wide characters are handled correctly in runs
      - Measure DOM element count before/after optimization
    - **Property 76: Cell batching reduces DOM elements**
    - **Property 77: Cell batching preserves visual output**
    - **Validates: Performance requirement - minimize DOM element count**
  
  - [ ] 18.3 Optimize cursor rendering (MEDIUM PRIORITY)
    - **Impact:** Medium (eliminates unnecessary DOM thrashing on every render)
    - **Effort:** Very Low
    - **Problem:** Cursor element is removed and recreated on every render, even when it hasn't moved
    - **Solution:** Update cursor position via CSS properties instead of recreating element
    - **Implementation Steps:**
      1. Modify `render()` method in Renderer class:
         - Change cursor update logic from remove/recreate to update-in-place
         - Only create cursor element if it doesn't exist (`this.cursorElement === null`)
      2. Create new method `updateCursor(cursor: CursorState): void`:
         - If `this.cursorElement` is null, call `renderCursor()` and append to display
         - Otherwise, update existing element properties:
           - `this.cursorElement.style.left = \`${cursor.col}ch\``
           - `this.cursorElement.style.top = \`${cursor.row}em\``
           - `this.cursorElement.style.opacity = cursor.visible ? '0.5' : '0'`
           - Update animation if blinking state changed
      3. Replace cursor rendering block in `render()` with call to `updateCursor()`
      4. Ensure cursor element is properly cleaned up in `clearCache()` method
    - **Testing:**
      - Write property test verifying cursor element is reused
      - Test that cursor position updates correctly
      - Test that cursor visibility updates correctly
      - Verify no memory leaks from cursor element
    - **Property 78: Cursor element reuse**
    - **Validates: Performance requirement - minimize DOM manipulation**
  
  - [ ] 18.4 Implement conditional style application (MEDIUM PRIORITY)
    - **Impact:** Medium (reduces browser style recalculation overhead)
    - **Effort:** Low
    - **Problem:** `applyStyles()` resets ALL styles first, then reapplies them, forcing browser recalculation even when nothing changed
    - **Solution:** Only set styles that differ from current element state
    - **Implementation Steps:**
      1. Create `WeakMap<HTMLElement, CachedCell>` to track last applied styles per element
      2. Modify `applyStyles(element: HTMLElement, cell: Cell)` method:
         - Look up cached styles for this element
         - If no cache entry, apply all styles (first time)
         - If cache exists, compare each style property:
           - Only set `element.style.color` if `cell.fg` differs from cached fg
           - Only set `element.style.backgroundColor` if `cell.bg` differs from cached bg
           - Only set `element.style.fontWeight` if `cell.bold` differs from cached bold
           - Only set `element.style.fontStyle` if `cell.italic` differs from cached italic
           - Only set `element.style.textDecoration` if underline/strikethrough changed
           - Only set `element.style.cursor` if url changed
         - Update cache entry after applying changes
      3. Alternative simpler approach (if WeakMap overhead is concern):
         - Store last applied styles as data attributes on element
         - Compare data attributes before setting styles
         - Example: `element.dataset.fgColor`, `element.dataset.bold`, etc.
      4. Clear style cache when element is removed or reused for different cell
      5. Ensure cache is cleared in `clearCache()` method
    - **Testing:**
      - Write property test verifying styles are only set when changed
      - Measure style recalculation count before/after optimization
      - Test that visual output remains correct
      - Verify cache is properly maintained and cleared
    - **Property 79: Conditional style application correctness**
    - **Validates: Performance requirement - minimize style recalculation**
  
  - [ ] 18.5 Add performance monitoring and metrics (LOW PRIORITY)
    - **Impact:** Low (enables measurement and future optimization)
    - **Effort:** Low
    - **Problem:** No visibility into actual performance characteristics
    - **Solution:** Add optional performance monitoring
    - **Implementation Steps:**
      1. Create `PerformanceMonitor` class in `caTTY-ts/src/ts/terminal/PerformanceMonitor.ts`:
         - Track render count, render time, cells rendered, DOM operations
         - Use `performance.now()` for timing measurements
         - Provide `getMetrics()` method returning statistics
         - Provide `reset()` method to clear metrics
      2. Add optional `performanceMonitor?: PerformanceMonitor` to Renderer constructor
      3. Instrument `render()` method:
         - Record start time at beginning
         - Count dirty rows processed
         - Count cells updated
         - Count DOM elements created/updated
         - Record end time and calculate duration
         - Report metrics to monitor if present
      4. Add development-only UI to display metrics:
         - Show FPS (frames per second)
         - Show average render time
         - Show cells rendered per frame
         - Show DOM element count
         - Toggle visibility with keyboard shortcut (e.g., Ctrl+Shift+P)
      5. Add `?debug=performance` query parameter to enable monitoring in production
    - **Testing:**
      - Verify metrics are accurately recorded
      - Test that monitoring has minimal performance overhead
      - Verify metrics UI displays correctly
    - **Property 80: Performance metrics accuracy**
    - **Validates: Performance requirement - measurable performance characteristics**
  
  - [ ] 18.6 Write comprehensive performance benchmarks
    - **Impact:** Low (validation and regression prevention)
    - **Effort:** Medium
    - **Problem:** No automated way to detect performance regressions
    - **Solution:** Create benchmark suite for common terminal operations
    - **Implementation Steps:**
      1. Create `caTTY-ts/src/ts/terminal/__tests__/performance.bench.ts`
      2. Use Vitest's `bench()` API for benchmarking
      3. Create benchmarks for:
         - Writing 1000 characters to terminal
         - Scrolling 100 lines
         - Rendering full screen (80×24)
         - Rendering single line update
         - Cursor movement across screen
         - SGR attribute changes
         - Wide character rendering
      4. Set performance baselines:
         - Single character write: < 0.1ms
         - Full screen render: < 5ms
         - Single line render: < 0.5ms
         - Scroll operation: < 2ms
      5. Add benchmark script to package.json: `"bench": "vitest bench"`
      6. Document expected performance characteristics in README
      7. Add CI job to run benchmarks and detect regressions
    - **Testing:**
      - Run benchmarks before and after optimizations
      - Verify optimizations meet performance targets
      - Document performance improvements in commit messages
    - **Property 81: Performance benchmarks establish baselines**
    - **Validates: Performance requirement - measurable performance improvements**

