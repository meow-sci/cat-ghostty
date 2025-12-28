# Mouse Input Support Design

## Overview

This design document specifies the implementation of comprehensive mouse input support for the catty-ksa C# terminal emulator. The implementation follows the established headless architecture pattern, with core mouse logic in caTTY.Core and ImGui integration in caTTY.Display. The design achieves feature parity with the TypeScript reference implementation while maintaining performance and reliability requirements.

## Architecture

The mouse input system follows a layered architecture that separates concerns and maintains testability:

```
┌─────────────────────────────────────────────────────────────┐
│                    caTTY.Display                            │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ TerminalController │  │ MouseInputHandler │  │ CoordinateConverter │ │
│  │   (ImGui Glue)   │  │  (Event Detection)│  │ (Pixel→Cell)  │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     caTTY.Core                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌──────────────┐ │
│  │ MouseTrackingManager │ │ MouseEventProcessor │ │ EscapeSequenceGenerator │ │
│  │  (Mode Management) │  │ (Event Routing) │  │ (Sequence Creation) │ │
│  └─────────────────┘  └─────────────────┘  └──────────────┘ │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ MouseStateManager │  │ SelectionManager │                   │
│  │ (State Tracking) │  │ (Text Selection) │                   │
│  └─────────────────┘  └─────────────────┘                   │
└─────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Core Components (caTTY.Core)

#### MouseTrackingManager
Manages mouse tracking modes and state transitions:

```csharp
public class MouseTrackingManager
{
    public MouseTrackingMode CurrentMode { get; private set; }
    public bool SgrEncodingEnabled { get; private set; }
    
    public void SetTrackingMode(MouseTrackingMode mode);
    public void SetSgrEncoding(bool enabled);
    public bool ShouldReportEvent(MouseEventType eventType);
    public void Reset();
}

public enum MouseTrackingMode
{
    Off = 0,
    Click = 1000,    // X10 compatibility
    Button = 1002,   // Button event tracking
    Any = 1003       // Any event tracking
}
```

#### MouseEventProcessor
Processes mouse events and routes them appropriately:

```csharp
public class MouseEventProcessor
{
    public event EventHandler<MouseEventArgs>? MouseEventGenerated;
    
    public void ProcessMouseEvent(MouseEvent mouseEvent);
    public bool ShouldHandleLocally(MouseEvent mouseEvent);
    private void RouteToApplication(MouseEvent mouseEvent);
    private void RouteToSelection(MouseEvent mouseEvent);
}

public struct MouseEvent
{
    public MouseEventType Type { get; init; }
    public MouseButton Button { get; init; }
    public int X1 { get; init; }  // 1-based coordinates
    public int Y1 { get; init; }
    public KeyModifiers Modifiers { get; init; }
    public DateTime Timestamp { get; init; }
}
```

#### EscapeSequenceGenerator
Generates terminal escape sequences for mouse events:

```csharp
public static class EscapeSequenceGenerator
{
    public static string GenerateMousePress(MouseButton button, int x1, int y1, 
        KeyModifiers modifiers, bool sgrEncoding);
    public static string GenerateMouseRelease(MouseButton button, int x1, int y1, 
        KeyModifiers modifiers, bool sgrEncoding);
    public static string GenerateMouseMotion(MouseButton button, int x1, int y1, 
        KeyModifiers modifiers, bool sgrEncoding);
    public static string GenerateMouseWheel(bool directionUp, int x1, int y1, 
        KeyModifiers modifiers, bool sgrEncoding);
}
```

#### MouseStateManager
Tracks mouse button state and drag operations:

```csharp
public class MouseStateManager
{
    public MouseButton? PressedButton { get; private set; }
    public bool IsDragging { get; private set; }
    public (int X1, int Y1)? LastPosition { get; private set; }
    
    public void SetButtonPressed(MouseButton button, int x1, int y1);
    public void SetButtonReleased(MouseButton button);
    public void UpdatePosition(int x1, int y1);
    public void Reset();
    public bool IsConsistent();
}
```

### Display Components (caTTY.Display)

#### MouseInputHandler
Handles ImGui mouse input detection and processing:

```csharp
public class MouseInputHandler
{
    private readonly MouseEventProcessor _eventProcessor;
    private readonly CoordinateConverter _coordinateConverter;
    private readonly MouseStateManager _stateManager;
    
    public void HandleMouseInput();
    public void HandleMouseCapture();
    private void ProcessMouseDown(ImGuiMouseButton button);
    private void ProcessMouseUp(ImGuiMouseButton button);
    private void ProcessMouseMove();
    private void ProcessMouseWheel(float wheelDelta);
}
```

#### CoordinateConverter
Converts between screen pixels and terminal cell coordinates:

```csharp
public class CoordinateConverter
{
    private float _characterWidth;
    private float _lineHeight;
    private float2 _terminalOrigin;
    
    public void UpdateMetrics(float charWidth, float lineHeight, float2 origin);
    public (int X1, int Y1)? PixelToCell(float2 pixelPos, int terminalWidth, int terminalHeight);
    public float2 CellToPixel(int x1, int y1);
    public bool IsWithinBounds(float2 pixelPos, float2 terminalSize);
}
```

## Data Models

### Mouse Event Types
```csharp
public enum MouseEventType
{
    Press,
    Release,
    Motion,
    Wheel
}

public enum MouseButton
{
    Left = 0,
    Middle = 1,
    Right = 2,
    WheelUp = 64,
    WheelDown = 65
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1,
    Alt = 2,
    Ctrl = 4
}
```

### Mouse Tracking Configuration
```csharp
public struct MouseTrackingConfig
{
    public MouseTrackingMode Mode { get; init; }
    public bool SgrEncodingEnabled { get; init; }
    public bool SelectionPriority { get; init; }  // Shift key bypass
    
    public static MouseTrackingConfig Default => new()
    {
        Mode = MouseTrackingMode.Off,
        SgrEncodingEnabled = false,
        SelectionPriority = true
    };
}
```

### Selection Integration
```csharp
public class SelectionMouseHandler
{
    public bool IsSelecting { get; private set; }
    public TextSelection CurrentSelection { get; private set; }
    
    public void StartSelection(int x1, int y1);
    public void UpdateSelection(int x1, int y1);
    public void EndSelection();
    public void ClearSelection();
    public bool HandleMouseEvent(MouseEvent mouseEvent);
}
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated to eliminate redundancy:

- **Mouse tracking mode properties** (R1.1-R1.3) can be combined into a single comprehensive property about event reporting based on mode
- **Encoding format properties** (R2.1-R2.2) can be combined into a single property about format selection
- **Coordinate conversion properties** (R3.1-R3.3) can be combined into a comprehensive coordinate handling property
- **Button identification properties** (R4.1-R4.3) can be combined into a single button detection property
- **Modifier encoding properties** (R9.1-R9.4) can be combined into a single modifier handling property

### Core Properties

**Property 1: Mouse Tracking Mode Event Reporting**
*For any* mouse event and tracking mode configuration, the terminal should report events to the application only when the event type is supported by the current tracking mode (click events in mode 1000+, drag events in mode 1002+, motion events in mode 1003 only)
**Validates: Requirements R1.1, R1.2, R1.3, R4.4, R4.5**

**Property 2: Mouse Tracking Mode Resolution**
*For any* combination of enabled mouse tracking modes, the terminal should use the highest numbered mode as the active mode (1003 > 1002 > 1000)
**Validates: Requirements R1.5**

**Property 3: Mouse Event Local Handling**
*For any* mouse event when tracking is disabled or shift key is held, the terminal should route the event to local handlers (selection, scrolling) instead of generating escape sequences
**Validates: Requirements R1.4, R6.1, R6.2**

**Property 4: Mouse Encoding Format Selection**
*For any* mouse event requiring escape sequence generation, the terminal should use SGR format when mode 1006 is enabled and standard X10/X11 format otherwise
**Validates: Requirements R2.1, R2.2**

**Property 5: Mouse Coordinate Encoding Range**
*For any* mouse coordinates above 223, SGR encoding should handle them correctly while standard encoding should clamp or handle them according to X10/X11 limitations
**Validates: Requirements R2.3**

**Property 6: Mouse Event Modifier Encoding**
*For any* mouse event with modifier keys held, the generated escape sequence should correctly encode all active modifiers (shift, alt, ctrl) in the appropriate format
**Validates: Requirements R2.4, R9.1, R9.2, R9.3, R9.4**

**Property 7: Mouse Button Release Sequences**
*For any* mouse button release event, the terminal should generate the appropriate release sequence for the current encoding format
**Validates: Requirements R2.5**

**Property 8: Coordinate Conversion Accuracy**
*For any* valid screen pixel position within terminal bounds, conversion to 1-based terminal coordinates should be accurate and consistent with character metrics
**Validates: Requirements R3.1, R3.2, R3.4**

**Property 9: Coordinate Boundary Handling**
*For any* mouse position outside terminal bounds, coordinate conversion should clamp to valid terminal ranges and handle gracefully
**Validates: Requirements R3.3, R3.5**

**Property 10: Mouse Button Detection**
*For any* mouse button press event, the terminal should correctly identify the button (left=0, middle=1, right=2) and coordinates
**Validates: Requirements R4.1, R4.2, R4.3**

**Property 11: Mouse Wheel Event Routing**
*For any* mouse wheel event, the terminal should route to application as button 64/65 when tracking is enabled, to local scrollback when in primary screen with tracking disabled, or to arrow keys when in alternate screen with tracking disabled
**Validates: Requirements R5.1, R5.2, R5.3**

**Property 12: Mouse Wheel Event Accumulation**
*For any* sequence of wheel events with fractional deltas, the terminal should accumulate deltas and generate appropriate multiple events for large accumulated values
**Validates: Requirements R5.4, R5.5**

**Property 13: Selection Copy Operations**
*For any* right-click or Ctrl+C event with active text selection, the terminal should copy the selected text to clipboard instead of normal event processing
**Validates: Requirements R6.4, R6.5**

**Property 14: Focus-Based Event Capture**
*For any* mouse event, the terminal should process it only when the terminal has focus and the mouse is within terminal bounds
**Validates: Requirements R7.1, R7.3**

**Property 15: Focus State Transitions**
*For any* focus gain or loss event, the terminal should reset mouse state appropriately and handle ongoing operations gracefully
**Validates: Requirements R7.2, R7.4, R7.5**

**Property 16: Mouse State Consistency**
*For any* sequence of mouse events, the terminal should maintain consistent button state tracking and handle state corruption with proper recovery
**Validates: Requirements R8.1, R8.3, R8.4, R8.5**

**Property 17: Mouse Capture During Drag**
*For any* mouse drag operation, the terminal should capture mouse input to receive events outside terminal bounds and release capture on button release
**Validates: Requirements R8.2, R8.3**

**Property 18: Modifier State Updates**
*For any* modifier key state change during mouse operations, the terminal should report updated modifier state in subsequent events
**Validates: Requirements R9.5**

**Property 19: Mouse Tracking Mode Transitions**
*For any* mouse tracking mode change, the terminal should immediately apply the new mode behavior and handle ongoing operations gracefully
**Validates: Requirements R10.1, R10.2, R10.3, R10.4, R10.5**

**Property 20: Error Handling Robustness**
*For any* error condition in mouse processing (coordinate conversion, capture, sequence generation, state corruption), the terminal should handle gracefully with logging and continue operation
**Validates: Requirements R11.1, R11.2, R11.3, R11.4, R11.5**

**Property 21: Mouse Processing Performance**
*For any* typical mouse event, processing should complete within performance requirements (1ms) using efficient algorithms and minimal allocation
**Validates: Requirements R12.1, R12.2, R12.3, R12.4, R12.5**

**Property 22: System Integration Consistency**
*For any* mouse operation, the terminal should integrate correctly with existing systems (focus management, input encoding, scroll configuration, selection, terminal state)
**Validates: Requirements R13.1, R13.2, R13.3, R13.4, R13.5**

## Error Handling

The mouse input system implements comprehensive error handling at multiple levels:

### Coordinate Conversion Errors
- Invalid pixel positions → Fallback to (1,1) coordinates with logging
- Division by zero in metrics → Use fallback metrics (8px width, 16px height)
- Out-of-bounds coordinates → Clamp to valid terminal range

### Mouse Capture Errors
- ImGui capture failure → Continue without capture, log warning
- Capture state inconsistency → Reset capture state and continue

### Escape Sequence Generation Errors
- Invalid parameters → Skip event generation, log error
- String building failure → Use fallback sequence format

### State Management Errors
- Inconsistent button state → Reset to known good state
- Corrupted mouse state → Full state reset with logging
- Exception in state updates → Catch, log, and continue

### Integration Errors
- Focus management failure → Assume unfocused state
- Selection system failure → Disable selection temporarily
- Terminal state access failure → Use safe defaults

## Testing Strategy

The mouse input system uses a dual testing approach combining unit tests and property-based tests:

### Unit Tests
Unit tests focus on specific examples, edge cases, and error conditions:
- Specific mouse event sequences and expected escape sequences
- Boundary conditions for coordinate conversion
- Error handling scenarios with invalid inputs
- Integration points with existing terminal systems
- Performance benchmarks for critical paths

### Property-Based Tests
Property tests verify universal properties across all inputs using FsCheck.NUnit with minimum 100 iterations:
- **Mouse Event Processing**: Generate random mouse events and verify correct routing based on tracking mode
- **Coordinate Conversion**: Generate random pixel positions and verify accurate conversion to terminal coordinates
- **Escape Sequence Generation**: Generate random mouse events and verify escape sequences match expected formats
- **State Management**: Generate random event sequences and verify consistent state tracking
- **Error Recovery**: Generate invalid inputs and verify graceful error handling

### Test Configuration
Each property test includes:
- Minimum 100 iterations for thorough coverage
- Reference to corresponding design document property
- Tag format: **Feature: mouse-input-support, Property N: [property description]**

### Integration Testing
- End-to-end mouse interaction scenarios
- Integration with existing terminal systems
- Performance testing under high-frequency mouse events
- Compatibility testing with TypeScript reference implementation

The testing strategy ensures comprehensive coverage while maintaining fast execution and clear traceability to requirements and design properties.