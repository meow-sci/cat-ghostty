# Design Document

## Overview

This design extends the existing ECMA-48/VT100 compliant terminal emulator with xterm terminal emulation extensions. The implementation maintains the established MVC architecture with a headless parser, stateful terminal core, and UI controller bridge. The design focuses on window management and cursor management extensions first, with graphics-related features planned for future phases.

The extension leverages the existing parser infrastructure and adds new message types for xterm-specific control sequences. The StatefulTerminal component will be enhanced to manage additional state (alternate screen buffers, mouse reporting modes, etc.), while the TerminalController will handle UI-specific aspects like title updates and mouse event capture.

## Architecture

The xterm extensions integrate into the existing three-layer architecture:

### Parser Layer (Headless)
- Extends existing CSI, OSC, and ESC sequence parsing
- Adds new message types for xterm-specific sequences
- Maintains stateless operation for all parsing logic
- Handles DCS (Device Control String) sequences for advanced features

### StatefulTerminal Layer (Core State)
- Manages alternate screen buffers and switching logic
- Tracks cursor modes (application cursor keys, visibility, styles)
- Maintains mouse reporting state and coordinate tracking
- Handles scroll region boundaries and behavior
- Manages character set selection state

### TerminalController Layer (UI Bridge)
- Processes window title and property changes
- Captures and reports mouse events when enabled
- Handles terminal size queries and responses
- Manages cursor appearance updates in the display
- Coordinates between terminal state and DOM updates

### Terminal Theme System
The terminal theme system provides a structured approach to color management using CSS custom properties:

#### Theme Data Structure
```typescript
interface TerminalTheme {
  name: string;
  type: "dark" | "light";
  colors: {
    // Standard 16 ANSI colors
    black: string;
    red: string;
    green: string;
    yellow: string;
    blue: string;
    magenta: string;
    cyan: string;
    white: string;
    brightBlack: string;
    brightRed: string;
    brightGreen: string;
    brightYellow: string;
    brightBlue: string;
    brightMagenta: string;
    brightCyan: string;
    brightWhite: string;
    
    // Terminal UI colors
    foreground: string;
    background: string;
    cursor: string;
    selection: string;
  };
}
```

#### Default Dark Theme
The system will include a default dark theme using standard terminal colors:
- Black: #000000, Bright Black: #555555
- Red: #AA0000, Bright Red: #FF5555
- Green: #00AA00, Bright Green: #55FF55
- Yellow: #AA5500, Bright Yellow: #FFFF55
- Blue: #0000AA, Bright Blue: #5555FF
- Magenta: #AA00AA, Bright Magenta: #FF55FF
- Cyan: #00AAAA, Bright Cyan: #55FFFF
- White: #AAAAAA, Bright White: #FFFFFF

#### CSS Variable Integration
Themes are applied using CSS custom properties:
```css
:root {
  --terminal-color-black: #000000;
  --terminal-color-red: #AA0000;
  --terminal-color-green: #00AA00;
  /* ... additional color variables */
  --terminal-foreground: #AAAAAA;
  --terminal-background: #000000;
}
```

#### SGR Color Resolution
SGR styling references CSS variables for theme-aware colors:
- Standard colors (30-37, 40-47): Map to CSS variables
- Bright colors (90-97, 100-107): Map to bright CSS variables
- 256-color palette: Use predefined color values
- 24-bit RGB: Use direct color values

### SGR styling applicationwith CSS
- generate raw CSS strings for current
- hash the raw CSS strings using xxh3 hash from xxh3-ts package, example:
    ```typescript
    import { XXH64 } from 'xxh3-ts';
    import { Buffer } from 'buffer';
    let hash: bigint = XXH64(Buffer.from("color: #00ffff;\ncursor:pointer;"));
    ```
- use DOM manipulation (with a in-memory JS object cache which is caching known style blocks to avoid having to check if it exists using DOM APIs) manage `<style>` tags in the page where each <style> tag will contain a single CSS class whose name is the result of the xxh3 hash.
- each cell's class will be updated to match the hash ids of the SGR styles that should be applied to that cell
- ensure that cell classes are reset when needed


## Components and Interfaces

### Enhanced Parser Components

#### New Message Types
```typescript
// OSC (Operating System Command) messages
interface OscSetTitle extends OscBase {
  _type: "osc.setTitle";
  titleType: "window" | "icon" | "both";
  title: string;
}

interface OscQueryTitle extends OscBase {
  _type: "osc.queryTitle";
  titleType: "window" | "icon";
}

// DEC Private Mode messages
interface CsiDecModeSet extends CsiBase {
  _type: "csi.decModeSet";
  modes: number[];
}

interface CsiDecModeReset extends CsiBase {
  _type: "csi.decModeReset";
  modes: number[];
}

// Device Control String messages
interface DcsMessage {
  _type: "dcs";
  raw: string;
  command: string;
  parameters: string[];
}

// Query/Response messages
interface CsiDeviceAttributes extends CsiBase {
  _type: "csi.deviceAttributes";
  primary: boolean;
}

interface CsiCursorPositionReport extends CsiBase {
  _type: "csi.cursorPositionReport";
}
```

#### Enhanced OSC Parser
The existing OSC parser will be extended to handle xterm-specific OSC sequences:
- OSC 0: Set window title and icon name
- OSC 1: Set icon name
- OSC 2: Set window title
- OSC 21: Query window title

#### Enhanced CSI Parser
The CSI parser will be extended to handle additional xterm sequences:
- DECSET/DECRST modes (1, 25, 47, 1000, 1002, 1003, 1047, 1049)
- Device Attributes queries (DA1, DA2)
- Cursor Position Report (CPR)
- DECSCUSR cursor style control

### Enhanced StatefulTerminal Components

#### Alternate Screen Buffer Management
```typescript
interface ScreenBuffer {
  cells: ScreenCell[][];
  cursorX: number;
  cursorY: number;
  savedCursor: [number, number] | null;
  scrollRegion: { top: number; bottom: number } | null;
}

class AlternateScreenManager {
  private primaryBuffer: ScreenBuffer;
  private alternateBuffer: ScreenBuffer;
  private currentBuffer: "primary" | "alternate";
  
  switchToPrimary(): void;
  switchToAlternate(): void;
  getCurrentBuffer(): ScreenBuffer;
}
```

#### Mouse Reporting State
```typescript
interface MouseState {
  reportingMode: "none" | "basic" | "button" | "any";
  lastReportedPosition: [number, number];
  buttonState: number;
}
```

#### Cursor Management State
```typescript
interface CursorState {
  visible: boolean;
  style: number; // DECSCUSR style codes
  applicationMode: boolean; // Application cursor keys
  position: [number, number];
  savedPosition: [number, number] | null;
  wrapPending: boolean;
}
```

### Enhanced TerminalController Components

#### Window Management
```typescript
interface WindowManager {
  setTitle(title: string): void;
  setIconName(name: string): void;
  getTitle(): string;
  getIconName(): string;
  queryTitle(type: "window" | "icon"): void;
}
```

#### Mouse Event Handler
```typescript
interface MouseEventHandler {
  enableReporting(mode: "basic" | "button" | "any"): void;
  disableReporting(): void;
  handleMouseEvent(event: MouseEvent): void;
  formatMouseReport(x: number, y: number, button: number, action: string): string;
}
```

## Data Models

### Extended Terminal State
```typescript
interface ExtendedTerminalState {
  // Existing state
  cursorX: number;
  cursorY: number;
  cells: ScreenCell[][];
  
  // New xterm state
  alternateScreen: {
    active: boolean;
    buffer: ScreenCell[][];
    savedCursor: [number, number] | null;
  };
  
  mouseReporting: {
    enabled: boolean;
    mode: "none" | "basic" | "button" | "any";
    lastPosition: [number, number];
  };
  
  cursorState: {
    visible: boolean;
    style: number;
    applicationKeys: boolean;
  };
  
  scrollRegion: {
    top: number;
    bottom: number;
  } | null;
  
  characterSet: {
    current: string;
    g0: string;
    g1: string;
  };
  
  windowProperties: {
    title: string;
    iconName: string;
  };
}
```

### Message Flow Data
```typescript
interface XtermMessage {
  source: "parser" | "terminal" | "controller";
  type: string;
  payload: any;
  timestamp: number;
}
```

## Data Models

### Screen Buffer Model
```typescript
interface ScreenBuffer {
  cells: ScreenCell[][];
  dimensions: { cols: number; rows: number };
  cursor: {
    x: number;
    y: number;
    visible: boolean;
    style: number;
  };
  scrollRegion: { top: number; bottom: number } | null;
  characterAttributes: SgrState;
}
```

### Mouse Event Model
```typescript
interface MouseEventData {
  x: number;
  y: number;
  button: number; // 0=left, 1=middle, 2=right
  action: "press" | "release" | "move";
  modifiers: {
    shift: boolean;
    alt: boolean;
    ctrl: boolean;
  };
}
```

### Terminal Theme Model
```typescript
interface TerminalTheme {
  name: string;
  type: "dark" | "light";
  colors: TerminalColorPalette;
}

interface TerminalColorPalette {
  // Standard 16 ANSI colors
  black: string;
  red: string;
  green: string;
  yellow: string;
  blue: string;
  magenta: string;
  cyan: string;
  white: string;
  brightBlack: string;
  brightRed: string;
  brightGreen: string;
  brightYellow: string;
  brightBlue: string;
  brightMagenta: string;
  brightCyan: string;
  brightWhite: string;
  
  // Terminal UI colors
  foreground: string;
  background: string;
  cursor: string;
  selection: string;
}

interface ThemeManager {
  currentTheme: TerminalTheme;
  applyTheme(theme: TerminalTheme): void;
  generateCssVariables(theme: TerminalTheme): string;
  resolveColor(colorCode: number, isBright: boolean): string;
}
```

Now I need to use the prework tool before writing the Correctness Properties section:

<function_calls>
<invoke name="prework">
<parameter name="featureName">xterm-extensions

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Based on the prework analysis, the following correctness properties have been identified for the xterm extensions:

**Property 1: OSC title setting consistency**
*For any* valid OSC 0 sequence with title text, both window title and icon name should be set to the same value
**Validates: Requirements 1.1**

**Property 2: OSC icon name isolation**
*For any* valid OSC 1 sequence with icon text, only the icon name should change while window title remains unchanged
**Validates: Requirements 1.2**

**Property 3: OSC window title isolation**
*For any* valid OSC 2 sequence with title text, only the window title should change while icon name remains unchanged
**Validates: Requirements 1.3**

**Property 4: Title query round-trip**
*For any* title string, setting it via OSC 2 then querying via OSC 21 should return the same title
**Validates: Requirements 1.4**

**Property 5: Alternate screen buffer switching**
*For any* terminal state, switching to alternate screen buffer via DECSET 47 should result in using alternate buffer
**Validates: Requirements 2.1**

**Property 6: Screen buffer round-trip**
*For any* terminal state, switching to alternate screen then back to normal should restore the original screen buffer
**Validates: Requirements 2.2**

**Property 7: Buffer content preservation**
*For any* content written to one screen buffer, switching to the other buffer and back should preserve the original content unchanged
**Validates: Requirements 2.7**

**Property 8: Application cursor key mode**
*For any* arrow key input when application cursor keys are enabled, the output should be SS3 sequences instead of CSI sequences
**Validates: Requirements 3.3**

**Property 9: Cursor state round-trip**
*For any* cursor state (position, attributes, wrap state), saving then restoring should produce identical state
**Validates: Requirements 3.4**

**Property 10: Mouse event reporting**
*For any* mouse click when mouse reporting is enabled, the terminal should generate the correct mouse event sequence
**Validates: Requirements 5.4**

**Property 11: Cursor position query round-trip**
*For any* cursor position, setting it then querying via cursor position report should return the same coordinates
**Validates: Requirements 6.2**

**Property 12: Scroll region containment**
*For any* valid scroll region boundaries, scrolling should only affect lines within the specified region
**Validates: Requirements 7.1**

**Property 13: UTF-8 processing correctness**
*For any* valid multi-byte UTF-8 sequence, enabling UTF-8 mode should process the sequence correctly without corruption
**Validates: Requirements 8.2**

**Property 14: Backward compatibility preservation**
*For any* existing ECMA-48/VT100 sequence, the extended terminal should process it identically to the original implementation
**Validates: Requirements 9.4**

**Property 15: State integrity during operations**
*For any* sequence of save/restore operations, the terminal state should maintain integrity without corruption
**Validates: Requirements 10.4**

**Property 16: SGR color application consistency**
*For any* valid SGR color sequence, the generated CSS should correctly represent the specified color values
**Validates: Requirements 3.4, 4.1**

**Property 17: CSS hash generation determinism**
*For any* identical CSS string, the xxh3 hash should always generate the same class name
**Validates: Requirements 4.1, 4.2**

**Property 18: Theme color resolution consistency**
*For any* standard ANSI color code (0-15), the resolved color should match the corresponding CSS variable from the current theme
**Validates: Requirements 3.4, 4.1**

## Error Handling

The xterm extensions will implement robust error handling following these principles:

### Invalid Sequence Handling
- Malformed OSC sequences: Ignore and continue parsing
- Invalid DEC mode numbers: Log warning and ignore
- Out-of-range parameters: Clamp to valid ranges
- Incomplete sequences: Buffer until complete or timeout

### State Consistency
- Screen buffer switches: Validate buffer exists before switching
- Cursor operations: Ensure coordinates remain within bounds
- Mouse reporting: Validate coordinates before reporting
- Character set changes: Fall back to default on invalid sets

### Resource Management
- Alternate screen buffer: Limit buffer size to prevent memory exhaustion
- Mouse event queue: Implement circular buffer to prevent overflow
- Title strings: Limit length to prevent excessive memory usage
- Response queues: Implement backpressure for query responses

### Graceful Degradation
- Unsupported features: Log and continue without breaking
- Hardware limitations: Adapt behavior to available capabilities
- Network issues: Queue responses and retry when possible
- Memory constraints: Prioritize core functionality over extensions

## Testing Strategy

The testing strategy employs both unit testing and property-based testing to ensure comprehensive coverage:

### Unit Testing Approach
Unit tests will verify specific examples and integration points:
- OSC sequence parsing with known title strings
- DEC mode state transitions for specific mode numbers
- Mouse event generation for specific click coordinates
- Screen buffer switching with predefined content
- Cursor style changes for specific DECSCUSR values

### Property-Based Testing Approach
Property-based tests will verify universal properties across all inputs using **fast-check** (already available in the project):
- Each property-based test will run a minimum of 100 iterations
- Tests will use smart generators that constrain to valid input spaces
- Each test will be tagged with comments referencing the design document property

**Property-Based Testing Requirements:**
- Use fast-check library for property-based testing implementation
- Configure each property test to run minimum 100 iterations
- Tag each property test with format: `**Feature: xterm-extensions, Property {number}: {property_text}**`
- Each correctness property must be implemented by a single property-based test
- Generate smart test data that focuses on valid terminal sequences and edge cases

### Test Data Generation
- **OSC sequences**: Generate valid OSC commands with random but valid parameters
- **DEC modes**: Generate mode numbers from known xterm mode ranges
- **Cursor positions**: Generate coordinates within and at screen boundaries
- **Mouse events**: Generate click events with valid button codes and coordinates
- **UTF-8 strings**: Generate valid multi-byte UTF-8 sequences for title testing
- **Screen content**: Generate printable character sequences for buffer testing

### Integration Testing
- Terminal controller integration with DOM updates
- Parser integration with new message types
- State management integration across screen buffer switches
- Mouse event integration from DOM to terminal output

### Regression Testing
- Ensure all existing ECMA-48/VT100 tests continue to pass
- Verify no performance degradation in core parsing
- Confirm UI responsiveness with new features enabled
- Validate memory usage remains within acceptable bounds