# Design Document

## Overview

This design document describes a headless terminal emulator implementation in TypeScript that integrates with the libghostty-vt WASM library. The architecture follows a strict MVC pattern where the Model (terminal emulator core) is completely headless and framework-agnostic, the Controller handles DOM events and coordinates data flow, and the View renders the terminal state to HTML.

The terminal emulator will provide VT100/xterm-compatible terminal emulation, supporting standard escape sequences, cursor control, screen manipulation, text attributes, and advanced features like alternate screen buffers and scrollback. Additionally, the terminal supports the Kitty Graphics Protocol for displaying inline images with features including multiple image formats, transparency, scrolling, and Unicode placeholder integration.

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                   Browser (Frontend)                     │
│  ┌───────────────────────────────────────────────────┐  │
│  │                  Controller                        │  │
│  │  ┌──────────────┐         ┌──────────────┐       │  │
│  │  │ Input Handler│────────▶│ View Renderer│       │  │
│  │  └──────┬───────┘         └──────▲───────┘       │  │
│  │         │                        │                │  │
│  │         │    ┌──────────────┐    │                │  │
│  │         └───▶│  WebSocket   │◀───┘                │  │
│  │              │    Client    │                     │  │
│  │              └──────┬───────┘                     │  │
│  └─────────────────────┼─────────────────────────────┘  │
│                        │                                 │
│  ┌─────────────────────┼─────────────────────────────┐  │
│  │                     ▼         Model (Headless)    │  │
│  │  ┌──────────────────────────────────────────────┐│  │
│  │  │           Terminal Emulator Core             ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │   Screen   │  │    Parser    │          ││  │
│  │  │  │   Buffer   │  │   (CSI/ESC)  │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │   Cursor   │  │  Scrollback  │          ││  │
│  │  │  │   State    │  │    Buffer    │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  │  ┌────────────┐  ┌──────────────┐          ││  │
│  │  │  │   Image    │  │   Graphics   │          ││  │
│  │  │  │  Manager   │  │    Parser    │          ││  │
│  │  │  └────────────┘  └──────────────┘          ││  │
│  │  └──────────────────────────────────────────────┘│  │
│  │                                                   │  │
│  │  ┌──────────────────────────────────────────────┐│  │
│  │  │         libghostty-vt WASM Integration       ││  │
│  │  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ││  │
│  │  │  │   SGR    │  │   OSC    │  │   Key    │  ││  │
│  │  │  │  Parser  │  │  Parser  │  │ Encoder  │  ││  │
│  │  │  └──────────┘  └──────────┘  └──────────┘  ││  │
│  │  └──────────────────────────────────────────────┘│  │
│  └─────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────┘
                        │
                        │ WebSocket
                        │
┌─────────────────────────────────────────────────────────┐
│                 Node.js Backend Server                   │
│  ┌───────────────────────────────────────────────────┐  │
│  │              WebSocket Server                      │  │
│  │  ┌──────────────┐         ┌──────────────┐       │  │
│  │  │  Connection  │────────▶│     PTY      │       │  │
│  │  │   Manager    │         │   Manager    │       │  │
│  │  └──────┬───────┘         └──────┬───────┘       │  │
│  │         │                        │                │  │
│  │         └────────────────────────┘                │  │
│  └───────────────────────────────────────────────────┘  │
│                        │                                 │
│  ┌─────────────────────▼─────────────────────────────┐  │
│  │              PTY Process                           │  │
│  │  ┌──────────────────────────────────────────────┐│  │
│  │  │  bash / zsh / powershell.exe                 ││  │
│  │  │  (@lydell/node-pty)                          ││  │
│  │  └──────────────────────────────────────────────┘│  │
│  └─────────────────────────────────────────────────────┘
└─────────────────────────────────────────────────────────┘
```

### Component Separation

1. **Model Layer (Headless)**
   - Pure TypeScript, no DOM dependencies
   - Terminal emulator core logic
   - Screen buffer management
   - Escape sequence parsing
   - State management
   - Event emission

2. **Controller Layer**
   - DOM event handling (keyboard, mouse, focus)
   - Coordination between model and view
   - Key encoding via libghostty-vt
   - View rendering orchestration
   - WebSocket connection management
   - Backend communication

3. **View Layer**
   - HTML rendering using absolute-positioned spans
   - CSS styling for colors and attributes
   - Cursor visualization

4. **Backend Layer (Node.js)**
   - WebSocket server for client connections
   - PTY process management using `@lydell/node-pty`
   - Bidirectional data forwarding
   - Connection lifecycle management
   - Resource cleanup

## Components and Interfaces

### Core Data Structures

#### Cell

Represents a single character position in the terminal grid.

```typescript
interface Cell {
  char: string;           // The character (may be empty for wide char continuation)
  width: number;          // Character width (1 for normal, 2 for wide, 0 for continuation)
  fg: Color;              // Foreground color
  bg: Color;              // Background color
  bold: boolean;
  italic: boolean;
  underline: UnderlineStyle;
  inverse: boolean;
  strikethrough: boolean;
  url?: string;           // Hyperlink URL (OSC 8)
}

type Color = 
  | { type: 'default' }
  | { type: 'indexed', index: number }  // 0-255
  | { type: 'rgb', r: number, g: number, b: number };

enum UnderlineStyle {
  None = 0,
  Single = 1,
  Double = 2,
  Curly = 3,
  Dotted = 4,
  Dashed = 5
}
```

#### Line

Represents a single line in the terminal.

```typescript
interface Line {
  cells: Cell[];
  wrapped: boolean;  // True if this line was wrapped from previous
}
```

#### CursorState

Tracks cursor position and appearance.

```typescript
interface CursorState {
  row: number;
  col: number;
  visible: boolean;
  blinking: boolean;
}
```

#### Attributes

Current text attributes for new characters.

```typescript
interface Attributes {
  fg: Color;
  bg: Color;
  bold: boolean;
  italic: boolean;
  underline: UnderlineStyle;
  inverse: boolean;
  strikethrough: boolean;
  url?: string;
}
```

### Terminal Emulator Core

The main terminal emulator class.

```typescript
interface TerminalConfig {
  cols: number;
  rows: number;
  scrollback: number;
}

interface TerminalEvents {
  onBell: () => void;
  onTitleChange: (title: string) => void;
  onClipboard: (content: string) => void;
  onDataOutput: (data: Uint8Array) => void;
  onResize: (cols: number, rows: number) => void;
  onStateChange: () => void;
}

class Terminal {
  constructor(config: TerminalConfig, events: Partial<TerminalEvents>);
  
  // Input processing
  write(data: string | Uint8Array): void;
  
  // Screen access
  getLine(row: number): Line;
  getScrollbackLine(index: number): Line;
  getScrollbackSize(): number;
  
  // Cursor access
  getCursor(): CursorState;
  
  // Viewport control
  getViewportOffset(): number;
  setViewportOffset(offset: number): void;
  
  // Resize
  resize(cols: number, rows: number): void;
  
  // Image management
  getVisibleImagePlacements(): ImagePlacement[];
  getScrollbackImagePlacements(): ImagePlacement[];
  
  // Cleanup
  dispose(): void;
}
```

### Screen Buffer

Manages the 2D grid of cells.

```typescript
class ScreenBuffer {
  constructor(cols: number, rows: number);
  
  getCell(row: number, col: number): Cell;
  setCell(row: number, col: number, cell: Cell): void;
  getLine(row: number): Line;
  
  clear(): void;
  clearRegion(startRow: number, endRow: number, startCol: number, endCol: number): void;
  
  scrollUp(lines: number, scrollRegion?: { top: number, bottom: number }): void;
  scrollDown(lines: number, scrollRegion?: { top: number, bottom: number }): void;
  
  insertLines(row: number, count: number): void;
  deleteLines(row: number, count: number): void;
  insertCells(row: number, col: number, count: number): void;
  deleteCells(row: number, col: number, count: number): void;
  
  resize(cols: number, rows: number): void;
}
```

### Parser

Parses escape sequences and control characters.

```typescript
enum ParserState {
  Ground,
  Escape,
  EscapeIntermediate,
  CsiEntry,
  CsiParam,
  CsiIntermediate,
  CsiIgnore,
  OscString,
  Utf8
}

class Parser {
  constructor(terminal: Terminal);
  
  parse(data: Uint8Array): void;
  
  private handlePrintable(char: string): void;
  private handleControl(byte: number): void;
  private handleEscape(byte: number): void;
  private handleCsi(params: number[], intermediates: string, final: string): void;
  private handleOsc(command: number, data: string): void;
}
```

**SGR Parsing Design Decision**: All SGR (Select Graphic Rendition) parsing will be delegated to libghostty-vt WASM. The library accepts both the standard semicolon ';' separator and the non-standard colon ':' separator for compatibility with some non-compliant software. This behavior is expected and correct - the Parser should pass SGR sequences directly to libghostty-vt without attempting to validate or normalize the separator format.

### Controller

Handles DOM integration and user interaction.

```typescript
interface ControllerConfig {
  terminal: Terminal;
  inputElement: HTMLInputElement;
  displayElement: HTMLElement;
  wasmInstance: GhosttyVtInstance;
}

class TerminalController {
  constructor(config: ControllerConfig);
  
  // Lifecycle
  mount(): void;
  unmount(): void;
  
  // Rendering
  render(): void;
  
  // Input handling
  private handleKeyDown(event: KeyboardEvent): void;
  private handlePaste(event: ClipboardEvent): void;
  
  // Focus management
  private handleFocus(): void;
  private handleBlur(): void;
  
  // Selection
  private handleMouseDown(event: MouseEvent): void;
  private handleMouseMove(event: MouseEvent): void;
  private handleMouseUp(event: MouseEvent): void;
  private handleCopy(event: ClipboardEvent): void;
}
```

### Renderer

Renders terminal state to HTML.

```typescript
class Renderer {
  constructor(displayElement: HTMLElement);
  
  render(terminal: Terminal): void;
  
  private renderLine(line: Line, row: number): HTMLElement;
  private renderCell(cell: Cell, col: number): HTMLElement;
  private renderCursor(cursor: CursorState): HTMLElement;
  private applyStyles(element: HTMLElement, cell: Cell): void;
  private renderImages(placements: ImagePlacement[]): void;
  private renderImagePlacement(placement: ImagePlacement): HTMLElement;
}
```

### SampleShell

A demonstration shell backend for testing terminal functionality.

```typescript
interface ShellConfig {
  onOutput: (data: string) => void;
}

class SampleShell {
  constructor(config: ShellConfig);
  
  // Process input from terminal
  processInput(data: string): void;
  
  // Command handlers
  private handleLs(): void;
  private handleEcho(args: string): void;
  private handleClearScreen(): void;
  private handleUnknownCommand(command: string): void;
  
  // State
  private currentLine: string;
  private prompt: string;
}
```

**Design Decision**: SampleShell is a simple demonstration backend that processes commands and generates terminal output. It maintains minimal state (current input line) and responds to basic commands. The shell is designed to be easily replaceable with a real shell backend in the future.

### Backend Server

A Node.js server that manages PTY processes and WebSocket connections for real shell integration.

```typescript
interface BackendServerConfig {
  port: number;
  shell?: string;  // Optional shell override (defaults to bash/powershell)
}

class BackendServer {
  constructor(config: BackendServerConfig);
  
  // Lifecycle
  start(): Promise<void>;
  stop(): Promise<void>;
  
  // WebSocket handling
  private handleConnection(ws: WebSocket): void;
  private handleDisconnection(ws: WebSocket): void;
  
  // PTY management
  private spawnPty(cols: number, rows: number): IPty;
  private terminatePty(pty: IPty): void;
  
  // Data flow
  private handleClientData(ws: WebSocket, data: string): void;
  private handlePtyData(ws: WebSocket, data: string): void;
  private handleResize(pty: IPty, cols: number, rows: number): void;
}
```

**Design Decision**: The backend server uses the `@lydell/node-pty` package to spawn real shell processes. Each WebSocket connection gets its own dedicated PTY process. The server acts as a bidirectional bridge, forwarding data between the WebSocket client and the PTY process. When either side disconnects, the server cleans up both the WebSocket and PTY resources.

### WebSocket Client Integration

The controller will be extended to support WebSocket connections for real shell integration.

```typescript
interface WebSocketConfig {
  url: string;
  onConnect?: () => void;
  onDisconnect?: () => void;
  onError?: (error: Error) => void;
}

class TerminalController {
  // ... existing methods ...
  
  // WebSocket connection
  connectWebSocket(config: WebSocketConfig): void;
  disconnectWebSocket(): void;
  
  // Connection state
  isConnected(): boolean;
  
  // Private WebSocket handlers
  private handleWebSocketOpen(): void;
  private handleWebSocketMessage(data: string): void;
  private handleWebSocketClose(): void;
  private handleWebSocketError(error: Event): void;
  
  // Send data to backend
  private sendToBackend(data: Uint8Array): void;
}
```

**Design Decision**: The WebSocket integration is optional and can coexist with SampleShell. When a WebSocket connection is established, the controller switches from SampleShell to the real PTY backend. If the connection fails or is unavailable, the controller can fall back to SampleShell for demonstration purposes.

## Data Models

### Terminal State

The terminal maintains several pieces of state:

1. **Primary Screen Buffer**: The main visible screen (cols × rows)
2. **Alternate Screen Buffer**: Secondary buffer for full-screen apps
3. **Scrollback Buffer**: Historical lines (configurable size)
4. **Cursor State**: Position, visibility, blinking
5. **Current Attributes**: Text styling for new characters
6. **Parser State**: Current position in escape sequence parsing
7. **Terminal Modes**: Auto-wrap, cursor keys mode, bracketed paste, etc.
8. **Tab Stops**: Set of column positions for tab alignment
9. **Scroll Region**: Optional restricted scrolling area
10. **Character Sets**: G0-G3 character set designations
11. **Viewport Offset**: Scrollback viewing position

### Backend Connection State

The controller maintains connection state for backend integration:

1. **Connection Type**: SampleShell (demo) or WebSocket (real PTY)
2. **WebSocket Instance**: Active WebSocket connection (if connected)
3. **Connection Status**: Disconnected, connecting, connected, error
4. **Reconnection Strategy**: Optional automatic reconnection logic
5. **Fallback Mode**: Whether to fall back to SampleShell on connection failure

### Image Management State

The terminal maintains state for Kitty Graphics Protocol support:

1. **Image Store**: Map of image ID to decoded image data (as ImageBitmap or similar)
2. **Placement Store**: Map of placement ID to ImagePlacement objects
3. **Active Placements**: List of placements currently visible on screen
4. **Scrollback Placements**: List of placements in scrollback buffer
5. **Transmission State**: Tracking for chunked image transmissions in progress
6. **Cell Associations**: Mapping between grid cells and image placements (for Unicode placeholders)

### Buffer Management

The terminal uses a circular buffer for scrollback to efficiently manage memory:

```typescript
class ScrollbackBuffer {
  private buffer: Line[];
  private maxSize: number;
  private startIndex: number;
  private length: number;
  
  push(line: Line): void;
  get(index: number): Line;
  clear(): void;
}
```

### Alternate Screen

The terminal maintains two complete screen buffers and switches between them:

```typescript
interface ScreenState {
  buffer: ScreenBuffer;
  cursor: CursorState;
  savedCursor?: CursorState;
}

class AlternateScreenManager {
  private primary: ScreenState;
  private alternate: ScreenState;
  private current: 'primary' | 'alternate';
  
  switchToAlternate(): void;
  switchToPrimary(): void;
  getCurrentBuffer(): ScreenBuffer;
}
```

### Kitty Graphics Protocol

The terminal supports inline image display via the Kitty Graphics Protocol.

#### Image Data Structures

```typescript
interface ImageData {
  id: number;
  data: ImageBitmap | HTMLImageElement;
  width: number;
  height: number;
  format: 'png' | 'jpeg' | 'gif';
}

interface ImagePlacement {
  placementId: number;
  imageId: number;
  row: number;
  col: number;
  width: number;   // in cells
  height: number;  // in cells
  sourceX?: number;
  sourceY?: number;
  sourceWidth?: number;
  sourceHeight?: number;
  zIndex?: number;
  unicodePlaceholder?: string;
}

interface TransmissionState {
  imageId: number;
  chunks: Uint8Array[];
  format: string;
  expectedSize?: number;
  complete: boolean;
}
```

#### Graphics Parser

```typescript
class KittyGraphicsParser {
  constructor(terminal: Terminal);
  
  parseGraphicsCommand(sequence: string): void;
  
  private handleTransmission(params: GraphicsParams, payload: string): void;
  private handleDisplay(params: GraphicsParams): void;
  private handleDelete(params: GraphicsParams): void;
  private decodeImageData(payload: string, format: string): Promise<ImageBitmap>;
  private createPlacement(params: GraphicsParams): ImagePlacement;
}

interface GraphicsParams {
  action: 't' | 'd' | 'D';  // transmit, display, delete
  imageId?: number;
  placementId?: number;
  format?: string;
  width?: number;
  height?: number;
  x?: number;
  y?: number;
  rows?: number;
  cols?: number;
  sourceX?: number;
  sourceY?: number;
  sourceWidth?: number;
  sourceHeight?: number;
  more?: boolean;  // chunked transmission
  unicodePlaceholder?: number;
}
```

#### Image Manager

```typescript
class ImageManager {
  private images: Map<number, ImageData>;
  private placements: Map<number, ImagePlacement>;
  private activePlacements: ImagePlacement[];
  private scrollbackPlacements: ImagePlacement[];
  private transmissions: Map<number, TransmissionState>;
  
  storeImage(id: number, data: ImageBitmap, format: string): void;
  getImage(id: number): ImageData | undefined;
  deleteImage(id: number): void;
  
  createPlacement(placement: ImagePlacement): void;
  getPlacement(id: number): ImagePlacement | undefined;
  deletePlacement(id: number): void;
  deleteAllPlacements(): void;
  
  getVisiblePlacements(): ImagePlacement[];
  getScrollbackPlacements(): ImagePlacement[];
  
  handleScroll(direction: 'up' | 'down', lines: number): void;
  handleClear(region: 'screen' | 'line', row?: number): void;
  handleResize(oldCols: number, oldRows: number, newCols: number, newRows: number): void;
  
  startTransmission(imageId: number, format: string): void;
  addChunk(imageId: number, chunk: Uint8Array): void;
  completeTransmission(imageId: number): Promise<void>;
  cancelTransmission(imageId: number): void;
}
```

**Design Decision**: The Kitty Graphics Protocol implementation separates concerns between parsing (KittyGraphicsParser), storage (ImageManager), and rendering (Renderer). Images are decoded to ImageBitmap for efficient rendering. Placements track both screen and scrollback positions. Unicode placeholders create bidirectional associations between grid cells and image placements, allowing images to be removed when their placeholder cells are modified.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*


### Core Terminal Properties

Property 1: Buffer initialization creates correct dimensions
*For any* valid width and height parameters, initializing a terminal should create a screen buffer with exactly those dimensions
**Validates: Requirements 1.1**

Property 2: Resize preserves overlapping content
*For any* terminal with content, resizing should preserve all content that fits within the overlapping region of old and new dimensions
**Validates: Requirements 1.2**

Property 3: Cell structure completeness
*For any* cell in the terminal buffer, it should always have a character value and complete SGR attributes (foreground, background, bold, italic, underline, etc.)
**Validates: Requirements 1.3, 1.4**

Property 4: Cursor advances on character write
*For any* printable character written to the terminal, the cursor column should increase by the character width (1 for normal, 2 for wide)
**Validates: Requirements 2.2**

Property 5: Auto-wrap at line end
*For any* terminal with auto-wrap enabled, writing a character at the last column should move the cursor to column 0 of the next line
**Validates: Requirements 2.3**

Property 6: Cursor movement sequences update position
*For any* valid cursor movement CSI sequence, the cursor position after processing should match the expected position based on the sequence parameters
**Validates: Requirements 2.4**

Property 7: Cursor visibility state tracking
*For any* sequence of cursor visibility toggles, querying the cursor state should return the visibility value from the most recent toggle
**Validates: Requirements 2.5**

Property 8: Cursor position query accuracy
*For any* cursor position, querying the cursor should return the exact row and column where it was last positioned
**Validates: Requirements 2.6**

Property 9: Printable characters appear at cursor
*For any* printable ASCII character written, the cell at the cursor position should contain that character
**Validates: Requirements 3.1**

Property 10: SGR attributes apply to written characters
*For any* SGR attributes set and any character written, the cell containing that character should have those exact attributes
**Validates: Requirements 3.2**

Property 11: UTF-8 decoding correctness
*For any* valid UTF-8 multi-byte sequence, the terminal should decode and display the correct Unicode character
**Validates: Requirements 3.3**

Property 12: Wide characters occupy two cells
*For any* wide character (CJK) written, it should occupy exactly two cell positions with the second cell marked as a continuation
**Validates: Requirements 3.4**

Property 13: Line-end behavior respects auto-wrap mode
*For any* terminal, writing past the last column should wrap to next line if auto-wrap is enabled, or stay at last column if disabled
**Validates: Requirements 3.5, 15.1, 15.2**

Property 14: Tab moves to next tab stop
*For any* cursor position and tab stop configuration, receiving a tab character should move the cursor to the next tab stop position
**Validates: Requirements 4.4, 14.2**

Property 15: CSI cursor movement correctness
*For any* CSI cursor movement sequence (up, down, left, right) with parameter N, the cursor should move exactly N positions in the specified direction, bounded by screen edges
**Validates: Requirements 5.1, 5.2, 5.3, 5.4**

Property 16: CSI cursor positioning absolute
*For any* CSI cursor position sequence with row R and column C, the cursor should move to exactly position (R, C)
**Validates: Requirements 5.5**

Property 17: Erase in display clears correct region
*For any* screen content and erase in display parameter (0, 1, or 2), only the specified region (cursor to end, start to cursor, or entire screen) should be cleared
**Validates: Requirements 5.6**

Property 18: Erase in line clears correct region
*For any* line content and erase in line parameter (0, 1, or 2), only the specified region of the current line should be cleared
**Validates: Requirements 5.7**

Property 19: Scroll operations move content correctly
*For any* screen content and scroll up/down sequence with parameter N, content should shift by exactly N lines in the specified direction
**Validates: Requirements 5.8, 5.9**

Property 20: SGR parsing updates attributes
*For any* valid SGR sequence, parsing it should update the current text attributes to match the sequence parameters
**Validates: Requirements 6.1, 6.2**

Property 21: SGR attributes persist across characters
*For any* SGR attributes set, all subsequently written characters should have those attributes until they are changed
**Validates: Requirements 6.4, 6.5**

Property 22: OSC parsing triggers appropriate actions
*For any* valid OSC sequence, parsing it should either emit the appropriate event or update the appropriate state
**Validates: Requirements 7.1**

Property 23: OSC 8 hyperlink association
*For any* OSC 8 sequence with URL, all subsequently written characters should have that URL associated until the hyperlink is cleared
**Validates: Requirements 7.3**

Property 24: Unknown OSC sequences are ignored
*For any* invalid or unknown OSC sequence, the terminal should continue processing without error
**Validates: Requirements 7.5**

Property 25: Scrollback captures scrolled content
*For any* terminal with scrollback enabled, when content scrolls off the top of the screen, it should be retrievable from the scrollback buffer
**Validates: Requirements 8.1, 8.5**

Property 26: Scrollback buffer size limit
*For any* scrollback buffer with maximum size M, when more than M lines are added, the oldest lines should be removed to maintain size M
**Validates: Requirements 8.2**

Property 27: Viewport offset tracking
*For any* viewport scroll operation, the viewport offset should be updated to reflect the new scroll position
**Validates: Requirements 8.3**

Property 28: Auto-scroll behavior
*For any* terminal with auto-scroll enabled, writing new content while scrolled should reset the viewport to the bottom
**Validates: Requirements 8.4**

Property 29: Alternate screen buffer isolation
*For any* content written to the primary buffer, switching to alternate buffer should show a separate, independent buffer
**Validates: Requirements 9.1**

Property 30: Alternate screen round-trip
*For any* primary buffer state, switching to alternate and back to primary should restore the exact original primary buffer state
**Validates: Requirements 9.2**

Property 31: Alternate screen no scrollback
*For any* content written in alternate screen mode, it should not appear in the scrollback buffer
**Validates: Requirements 9.3**

Property 32: Buffer-independent cursor state
*For any* cursor position set in primary buffer, switching to alternate should have an independent cursor position
**Validates: Requirements 9.4**

Property 33: Alternate buffer initialization
*For any* terminal, switching to alternate buffer should show a cleared buffer with default attributes
**Validates: Requirements 9.5**

Property 34: Scroll region restricts scrolling
*For any* scroll region set from row T to row B, scroll operations should only affect rows within [T, B]
**Validates: Requirements 10.1, 10.5**

Property 35: Scroll region content isolation
*For any* content outside a scroll region, scrolling within the region should not modify that content
**Validates: Requirements 10.2**

Property 36: Scroll region reset restores full scrolling
*For any* scroll region set, resetting it should allow scrolling to affect the entire screen
**Validates: Requirements 10.3**

Property 37: Cursor movement ignores scroll region
*For any* scroll region set, cursor positioning sequences should be able to move the cursor anywhere on screen
**Validates: Requirements 10.4**

Property 38: KeyEvent conversion preserves key information
*For any* keyboard event, converting it to a KeyEvent structure should preserve the key code, modifiers, and character information
**Validates: Requirements 11.2**

Property 39: Key encoding produces valid sequences
*For any* KeyEvent, encoding it via libghostty-vt should produce a valid terminal escape sequence or character
**Validates: Requirements 11.3**

Property 40: Key encoding round-trip
*For any* key press, encoding it and processing the result through the terminal should produce the expected terminal state change
**Validates: Requirements 11.4**

Property 41: Mode-dependent key encoding
*For any* special key (arrow, function key), encoding it in different terminal modes (normal vs application cursor keys) should produce different sequences
**Validates: Requirements 11.5, 15.4**

Property 42: View synchronization with terminal state
*For any* terminal state change, the rendered view should reflect the new state
**Validates: Requirements 12.1**

Property 43: Rendering creates correct element structure
*For any* terminal screen, rendering should create span elements for each visible character
**Validates: Requirements 12.2**

Property 44: Cell styling reflects attributes
*For any* cell with specific attributes, the rendered span should have CSS styles matching those attributes
**Validates: Requirements 12.3**

Property 45: Cursor renders at correct position
*For any* cursor position, the rendered cursor should appear at the corresponding screen coordinates
**Validates: Requirements 12.4**

Property 46: Wide character rendering spacing
*For any* wide character, the rendered output should occupy the visual space of two normal characters
**Validates: Requirements 12.5**

Property 47: Tab stop setting and usage
*For any* tab stop set at column C, a tab character should move the cursor to column C if it's the next tab stop
**Validates: Requirements 14.3**

Property 48: Tab stop clearing
*For any* tab stop cleared at column C, a tab character should skip column C and move to the next tab stop
**Validates: Requirements 14.4**

Property 49: Bracketed paste mode wrapping
*For any* pasted content, with bracketed paste mode enabled, the content should be wrapped with ESC[200~ and ESC[201~ sequences
**Validates: Requirements 15.5**

Property 50: Data output event emission
*For any* terminal operation that generates output (like key encoding), a data output event should be emitted with the correct data
**Validates: Requirements 16.4**

Property 51: Character insertion shifts content
*For any* line content and insert character sequence with parameter N, N blank cells should be inserted at cursor position and existing content should shift right
**Validates: Requirements 17.1, 17.5**

Property 52: Character deletion shifts content
*For any* line content and delete character sequence with parameter N, N cells should be deleted at cursor position and remaining content should shift left
**Validates: Requirements 17.2, 17.5**

Property 53: Line insertion shifts content
*For any* screen content and insert line sequence with parameter N, N blank lines should be inserted at cursor row and existing lines should shift down
**Validates: Requirements 17.3, 17.5**

Property 54: Line deletion shifts content
*For any* screen content and delete line sequence with parameter N, N lines should be deleted at cursor row and remaining lines should shift up
**Validates: Requirements 17.4, 17.5**

Property 55: API accepts multiple input types
*For any* valid string or Uint8Array input, the terminal write method should process it correctly
**Validates: Requirements 18.2**

Property 56: Character set designation tracking
*For any* character set designation sequence, the terminal should track which character set is designated to which slot (G0-G3)
**Validates: Requirements 19.1**

Property 57: Character set switching
*For any* shift-in or shift-out control character, the active character set should switch between G0 and G1
**Validates: Requirements 19.2**

Property 58: DEC Special Graphics mapping
*For any* character written while DEC Special Graphics is active, it should be mapped to the corresponding line-drawing glyph
**Validates: Requirements 19.3**

Property 59: Character set affects written characters
*For any* character written, it should be mapped according to the currently active character set
**Validates: Requirements 19.4**

Property 60: Text extraction preserves content
*For any* selected region of terminal content, extracting the text should preserve the characters, line breaks, and handle wide characters correctly
**Validates: Requirements 20.3, 20.4**

### SampleShell Properties

Property 61: ls command output format
*For any* invocation of the "ls" command, SampleShell should output exactly five dummy filenames
**Validates: Requirements 21.2**

Property 62: echo command reflects input
*For any* string argument passed to the "echo" command, SampleShell should output that exact string back to the terminal
**Validates: Requirements 21.3**

Property 63: Ctrl+L clears screen
*For any* terminal state, when Ctrl+L is received, SampleShell should send escape sequences that clear the screen and reset cursor to position (0, 0)
**Validates: Requirements 21.4**

Property 64: Unknown command error handling
*For any* unrecognized command, SampleShell should output an error message indicating the command was not found
**Validates: Requirements 21.5**

### Backend Server Properties

Property 65: PTY spawn on connection
*For any* WebSocket connection established, the backend server should spawn exactly one PTY process with the specified terminal dimensions
**Validates: Requirements 22.2, 22.4**

Property 66: PTY output forwarding
*For any* data emitted by the PTY process, the backend server should forward it to the connected WebSocket client
**Validates: Requirements 22.5**

Property 67: Client input forwarding
*For any* data received from the WebSocket client, the backend server should write it to the PTY process
**Validates: Requirements 23.1, 23.2**

Property 68: Terminal resize propagation
*For any* resize message received from the client, the backend server should update the PTY dimensions to match
**Validates: Requirements 23.5**

Property 69: Connection cleanup on disconnect
*For any* WebSocket connection that closes, the backend server should terminate the associated PTY process and remove all event listeners
**Validates: Requirements 24.1, 24.5**

Property 70: PTY exit cleanup
*For any* PTY process that exits, the backend server should close the associated WebSocket connection
**Validates: Requirements 24.2**

Property 71: WebSocket connection establishment
*For any* terminal page load, the controller should attempt to establish a WebSocket connection to the backend server
**Validates: Requirements 25.1**

Property 72: Real shell output display
*For any* data received through the WebSocket, the terminal should display it with correct formatting
**Validates: Requirements 25.2, 25.4**

Property 73: Command execution through PTY
*For any* user command typed in the terminal, it should be executed in the real shell via the PTY process
**Validates: Requirements 25.3**

Property 74: Connection failure fallback
*For any* WebSocket connection failure, the terminal should display an error message and optionally fall back to SampleShell
**Validates: Requirements 25.5**

### Kitty Graphics Protocol Properties

Property 75: Graphics command parsing
*For any* valid Kitty graphics escape sequence, the parser should extract the action and payload correctly
**Validates: Requirements 26.1**

Property 76: Image data decoding
*For any* base64-encoded image data in a supported format, the terminal should decode it to a displayable image
**Validates: Requirements 26.2, 30.1, 30.2, 30.3**

Property 77: Image storage with ID
*For any* image transmitted with an ID, the terminal should store it such that it can be retrieved by that ID
**Validates: Requirements 26.3, 34.1**

Property 78: Placement creation at cursor
*For any* display command, the terminal should create an image placement at the current cursor position
**Validates: Requirements 26.4, 26.5**

Property 79: Grid coordinate positioning
*For any* image placement with specified rows and columns, the image should appear at those exact grid coordinates
**Validates: Requirements 27.1**

Property 80: Pixel to cell conversion
*For any* image placement with pixel dimensions, the terminal should convert them to cell dimensions based on cell size
**Validates: Requirements 27.2**

Property 81: Source rectangle cropping
*For any* image placement with a source rectangle, only that region of the image should be displayed
**Validates: Requirements 27.3**

Property 82: Native dimension fallback
*For any* image placement without specified dimensions, the terminal should use the image's native dimensions
**Validates: Requirements 27.4**

Property 83: Screen boundary clipping
*For any* image placement that extends beyond screen boundaries, the image should be clipped at the edges
**Validates: Requirements 27.5**

Property 84: Image scrolling with content
*For any* image placement in a scrolled region, the placement should move with the scrolled content
**Validates: Requirements 28.1**

Property 85: Scrollback buffer image preservation
*For any* image placement that scrolls off the top, it should be moved to the scrollback buffer
**Validates: Requirements 28.2**

Property 86: Reverse scroll image removal
*For any* image placement that scrolls off the bottom during reverse scroll, it should be removed
**Validates: Requirements 28.3**

Property 87: Scrollback image display
*For any* scrollback view, image placements in the scrollback buffer should be displayed
**Validates: Requirements 28.4**

Property 88: Alternate screen no image scrollback
*For any* image in alternate screen mode, it should not be preserved in scrollback when scrolled
**Validates: Requirements 28.5**

Property 89: Image deletion by image ID
*For any* delete command with an image ID, all placements of that image should be removed
**Validates: Requirements 29.1**

Property 90: Placement deletion by placement ID
*For any* delete command with a placement ID, only that specific placement should be removed
**Validates: Requirements 29.2**

Property 91: Delete all visible placements
*For any* delete command with no IDs, all visible image placements should be removed
**Validates: Requirements 29.3**

Property 92: Image data memory cleanup
*For any* deleted image, the associated image data should be freed from memory
**Validates: Requirements 29.4**

Property 93: Display update on placement deletion
*For any* deleted placement, the display should be updated to remove the image
**Validates: Requirements 29.5**

Property 94: Animated GIF support
*For any* animated GIF image, the terminal should display the animation
**Validates: Requirements 30.4**

Property 95: Unsupported format error handling
*For any* image in an unsupported format, the terminal should emit an error event and ignore the image
**Validates: Requirements 30.5**

Property 96: Chunked transmission accumulation
*For any* image transmitted in chunks, the terminal should accumulate all chunks until transmission is complete
**Validates: Requirements 31.1**

Property 97: Non-blocking chunked transmission
*For any* chunked transmission in progress, the terminal should continue processing other output
**Validates: Requirements 31.2**

Property 98: Transmission completion finalization
*For any* transmission marked complete, the terminal should finalize the image and make it available for placement
**Validates: Requirements 31.3**

Property 99: Transmission failure cleanup
*For any* failed transmission, the terminal should discard partial data and emit an error event
**Validates: Requirements 31.4**

Property 100: Concurrent transmission independence
*For any* multiple concurrent image transmissions, each should be handled independently
**Validates: Requirements 31.5**

Property 101: Image element creation for placements
*For any* visible image placement, the renderer should create an image element
**Validates: Requirements 32.1**

Property 102: Image element positioning
*For any* rendered image placement, the image element should be positioned at the correct grid coordinates
**Validates: Requirements 32.2**

Property 103: Image element sizing
*For any* rendered image placement, the image element should be sized according to placement dimensions
**Validates: Requirements 32.3**

Property 104: CSS clipping for source rectangle
*For any* image placement with a source rectangle, CSS clipping should show only that region
**Validates: Requirements 32.4**

Property 105: Image element removal
*For any* removed placement, the corresponding image element should be removed from the DOM
**Validates: Requirements 32.5**

Property 106: Clear screen removes images
*For any* screen clear operation, all image placements in the cleared region should be removed
**Validates: Requirements 33.1**

Property 107: Line erase removes images
*For any* line erase operation, image placements on that line should be removed
**Validates: Requirements 33.2**

Property 108: Line insertion shifts images
*For any* line insertion, image placements should shift down accordingly
**Validates: Requirements 33.3**

Property 109: Line deletion shifts images
*For any* line deletion, image placements should shift up and those in deleted lines should be removed
**Validates: Requirements 33.4**

Property 110: Resize repositions images
*For any* terminal resize, image placements should be repositioned based on new cell dimensions
**Validates: Requirements 33.5**

Property 111: Image ID reuse replaces data
*For any* image ID reused, the previous image data should be replaced with new data
**Validates: Requirements 34.3**

Property 112: Placement ID reuse replaces placement
*For any* placement ID reused, the previous placement should be replaced with the new placement
**Validates: Requirements 34.4**

Property 113: Automatic ID generation
*For any* image or placement without specified ID, the terminal should generate a unique ID automatically
**Validates: Requirements 34.5**

Property 114: Alpha channel preservation
*For any* image with alpha channel, the transparency should be preserved
**Validates: Requirements 35.1**

Property 115: Transparent pixel rendering
*For any* transparent image, the terminal background should show through transparent pixels
**Validates: Requirements 35.2**

Property 116: Opaque image handling
*For any* image without alpha channel, it should be treated as fully opaque
**Validates: Requirements 35.3**

Property 117: Image text layering
*For any* transparent image overlapping text, the image should be layered appropriately
**Validates: Requirements 35.4**

Property 118: Background color change updates transparency
*For any* terminal background color change, transparent images should update their appearance
**Validates: Requirements 35.5**

Property 119: Unicode placeholder association
*For any* image placement with Unicode placeholder, the placeholder character should be written to the grid and associated with the placement
**Validates: Requirements 36.1, 36.2**

Property 120: Placeholder erase removes image
*For any* Unicode placeholder that is erased, the associated image placement should be removed
**Validates: Requirements 36.3**

Property 121: Placeholder scroll moves image
*For any* Unicode placeholder that scrolls, the image placement should move with it
**Validates: Requirements 36.4**

Property 122: Placeholder overwrite removes image
*For any* Unicode placeholder overwritten by text, the associated image placement should be removed
**Validates: Requirements 36.5**

## Error Handling

### Input Validation

The terminal emulator will validate all inputs and handle errors gracefully:

1. **Invalid Dimensions**: Terminal creation with invalid dimensions (< 1 or > 1000) should throw an error
2. **Invalid Escape Sequences**: Malformed escape sequences should be ignored without crashing
3. **Invalid UTF-8**: Invalid UTF-8 sequences should be replaced with the Unicode replacement character (U+FFFD)
4. **Out of Bounds Access**: Attempts to access cells outside the buffer should return empty cells or be clamped
5. **WASM Errors**: Errors from libghostty-vt should be caught and logged without crashing the terminal
6. **SGR Parameter Separators**: Both semicolon ';' and colon ':' separators in SGR sequences are valid and should be accepted (libghostty-vt handles both for compatibility with non-compliant software)

### Resource Management

1. **Memory Cleanup**: All WASM memory allocations must be freed when no longer needed
2. **Event Listener Cleanup**: All DOM event listeners must be removed when the controller is unmounted
3. **Disposal**: The terminal dispose method must clean up all resources including WASM instances
4. **WebSocket Cleanup**: WebSocket connections must be properly closed when the page unloads or the terminal is disposed
5. **PTY Process Cleanup**: Backend server must terminate PTY processes when WebSocket connections close
6. **Event Listener Removal**: Backend server must remove all PTY event listeners when processes are terminated

### Graceful Degradation

1. **Missing WASM**: If libghostty-vt fails to load, provide clear error message
2. **Unsupported Sequences**: Unknown escape sequences should be ignored
3. **Browser Compatibility**: Fallback for browsers without clipboard API support
4. **WebSocket Connection Failure**: If WebSocket connection fails, display error and optionally fall back to SampleShell
5. **Backend Server Unavailable**: Terminal should handle backend unavailability gracefully with clear user feedback
6. **PTY Process Errors**: Backend server should log PTY errors and close connections cleanly

## Testing Strategy

### Unit Testing

Unit tests will verify specific behaviors and edge cases:

1. **Initialization Tests**: Verify terminal initializes with correct default state
2. **Control Character Tests**: Test specific control characters (LF, CR, BS, BEL, etc.)
3. **Edge Cases**: Test boundary conditions (cursor at edges, buffer limits, etc.)
4. **Event Emission Tests**: Verify events are emitted with correct data
5. **API Tests**: Test public API methods return expected values

### Property-Based Testing

Property-based tests will verify universal properties across many inputs using a PBT library (fast-check for TypeScript):

- **Library**: fast-check (https://github.com/dubzzz/fast-check)
- **Configuration**: Each property test should run a minimum of 100 iterations
- **Tagging**: Each property-based test must include a comment with the format:
  ```typescript
  // Feature: headless-terminal-emulator, Property N: <property text>
  ```

Property tests will:

1. Generate random terminal dimensions, content, and sequences
2. Verify invariants hold across all generated inputs
3. Test round-trip properties (e.g., alternate screen switching)
4. Verify state consistency after operations
5. Test that operations compose correctly

### Integration Testing

Integration tests will verify the complete system:

1. **WASM Integration**: Test that libghostty-vt integration works correctly
2. **Controller Integration**: Test that controller correctly coordinates model and view
3. **End-to-End Scenarios**: Test complete user workflows (typing, scrolling, copying)
4. **WebSocket Integration**: Test bidirectional data flow between terminal and backend
5. **PTY Backend Integration**: Test that backend server correctly manages PTY processes and WebSocket connections
6. **Connection Lifecycle**: Test connection establishment, data transfer, and cleanup scenarios

### Test Organization

```
caTTY-ts/src/ts/terminal/
  __tests__/
    unit/
      ScreenBuffer.test.ts
      Parser.test.ts
      Cursor.test.ts
    property/
      TerminalProperties.test.ts
      BufferProperties.test.ts
      ParserProperties.test.ts
    integration/
      TerminalIntegration.test.ts
      ControllerIntegration.test.ts
```

## Implementation Notes

### Performance Considerations

1. **Incremental Rendering**: Only re-render changed portions of the screen
2. **Virtual Scrolling**: For large scrollback buffers, only render visible lines
3. **Batch Updates**: Batch multiple terminal writes before rendering
4. **Efficient Data Structures**: Use typed arrays where appropriate for performance

### Browser Compatibility

1. **Target**: Modern browsers with ES2020 support
2. **WASM**: Requires WebAssembly support
3. **Clipboard**: Use Clipboard API with fallback for older browsers
4. **Input Method**: Support for IME (Input Method Editor) for international text
5. **WebSocket**: Requires WebSocket API support (available in all modern browsers)

### Backend Server Implementation

1. **Package**: Use `@lydell/node-pty` for PTY process management
2. **WebSocket Library**: Use `ws` package for WebSocket server
3. **Shell Selection**: Automatically detect OS and use appropriate shell (bash for Unix-like, powershell.exe for Windows)
4. **Port Configuration**: Configurable port for WebSocket server (default: 3000)
5. **Error Handling**: Log all PTY and WebSocket errors, clean up resources on failure
6. **Process Management**: Track active PTY processes and clean up on server shutdown

### Accessibility

1. **Screen Reader Support**: Provide ARIA labels and live regions
2. **Keyboard Navigation**: Ensure all functionality is keyboard accessible
3. **High Contrast**: Support high contrast mode
4. **Focus Indicators**: Clear visual focus indicators

### Future Enhancements

1. **Sixel Graphics**: Support for alternative inline image protocol
2. **True Color**: 24-bit color support (may already be supported via libghostty-vt)
3. **Ligatures**: Font ligature support for programming fonts
4. **Search**: Text search within terminal content
5. **Bidirectional Text**: Support for RTL languages
6. **Reconnection Logic**: Automatic reconnection with exponential backoff for WebSocket failures
7. **Multiple Sessions**: Support for multiple concurrent terminal sessions with tab management
8. **Session Persistence**: Save and restore terminal sessions across page reloads
9. **Image Compression**: Support for additional transmission mediums (file, temporary file, shared memory)
10. **Image Caching**: Optimize repeated image display with client-side caching
11. **Image Animations**: Enhanced control over animated image playback (pause, speed control)
