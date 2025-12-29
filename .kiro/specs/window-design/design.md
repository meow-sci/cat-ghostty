# Design Document

## Overview

This design transforms the current basic ImGui terminal window from a simple info bar + canvas layout into a structured, feature-rich interface. The redesign introduces a hierarchical layout with menu bar, tab area, settings area, and terminal canvas, preparing the foundation for future multi-terminal support while maintaining single terminal functionality in this phase.

The design follows ImGui best practices and maintains the existing headless terminal architecture, focusing purely on the display controller layer without affecting the core terminal emulation logic.

## Architecture

### Current Layout Structure
```
┌─────────────────────────────────────┐
│ Terminal Window                     │
├─────────────────────────────────────┤
│ Info Bar: Terminal: 80x24 PID: 123 │
├─────────────────────────────────────┤
│                                     │
│         Terminal Canvas             │
│                                     │
└─────────────────────────────────────┘
```

### New Layout Structure
```
┌─────────────────────────────────────┐
│ Menu Bar: File | Edit | View        │
├─────────────────────────────────────┤
│ Tab Area: [Terminal 1]          [+] │
├─────────────────────────────────────┤
│ Settings: [Font] [Colors] [Info]    │
├─────────────────────────────────────┤
│                                     │
│         Terminal Canvas             │
│                                     │
└─────────────────────────────────────┘
```

### Layout Hierarchy

The window layout follows a top-down hierarchy with fixed-height header areas and a flexible terminal canvas:

1. **Menu Bar** (Fixed height: ~25px)
2. **Tab Area** (Fixed height: ~30px) 
3. **Settings Area** (Fixed height: ~40px)
4. **Terminal Canvas** (Flexible: remaining space)

## Components and Interfaces

### TerminalController Modifications

The existing `TerminalController.Render()` method will be refactored to support the new layout structure:

```csharp
public void Render()
{
    if (!_isVisible) return;
    
    EnsureFontsLoaded();
    PushMonospaceFont(out bool fontUsed);
    
    try
    {
        ImGui.Begin("Terminal", ref _isVisible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        
        UpdateFocusState(ImGui.IsWindowFocused());
        ManageInputCapture();
        
        // NEW: Structured layout rendering
        RenderMenuBar();
        RenderTabArea();
        RenderSettingsArea();
        RenderTerminalCanvas(); // Replaces RenderTerminalContent()
        
        RenderFocusIndicators();
        
        if (HasFocus)
        {
            HandleInput();
        }
        
        ImGui.End();
    }
    finally
    {
        MaybePopFont(fontUsed);
    }
}
```

### New Rendering Methods

#### RenderMenuBar()
```csharp
private void RenderMenuBar()
{
    if (ImGui.BeginMenuBar())
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("New Terminal", "Ctrl+Shift+T", false, false))
            {
                // Future: Create new terminal tab
                ShowNotImplementedMessage("Multi-terminal support");
            }
            if (ImGui.MenuItem("Close Terminal", "Ctrl+Shift+W", false, false))
            {
                // Future: Close current terminal tab
                ShowNotImplementedMessage("Multi-terminal support");
            }
            ImGui.Separator();
            if (ImGui.MenuItem("Exit", "Alt+F4"))
            {
                _isVisible = false;
            }
            ImGui.EndMenu();
        }
        
        if (ImGui.BeginMenu("Edit"))
        {
            if (ImGui.MenuItem("Copy", "Ctrl+C", false, !_currentSelection.IsEmpty))
            {
                CopySelectionToClipboard();
            }
            if (ImGui.MenuItem("Paste", "Ctrl+V"))
            {
                PasteFromClipboard();
            }
            if (ImGui.MenuItem("Select All", "Ctrl+A"))
            {
                SelectAllText();
            }
            ImGui.EndMenu();
        }
        
        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Reset Zoom", "Ctrl+0"))
            {
                ResetFontSize();
            }
            if (ImGui.MenuItem("Zoom In", "Ctrl++"))
            {
                IncreaseFontSize();
            }
            if (ImGui.MenuItem("Zoom Out", "Ctrl+-"))
            {
                DecreaseFontSize();
            }
            ImGui.EndMenu();
        }
        
        ImGui.EndMenuBar();
    }
}
```

#### RenderTabArea()
```csharp
private void RenderTabArea()
{
    float availableWidth = ImGui.GetContentRegionAvail().X;
    float addButtonWidth = 30.0f;
    float tabWidth = availableWidth - addButtonWidth - 10.0f; // 10px spacing
    
    // Single tab for current terminal
    ImGui.PushStyleColor(ImGuiCol.Button, new float4(0.2f, 0.3f, 0.5f, 1.0f)); // Active tab color
    ImGui.Button($"Terminal 1##tab_0", new float2(tabWidth, 25.0f));
    ImGui.PopStyleColor();
    
    // Add button on the right
    ImGui.SameLine();
    if (ImGui.Button("+##add_terminal", new float2(addButtonWidth, 25.0f)))
    {
        ShowNotImplementedMessage("Multi-terminal support will be added in a future phase");
    }
    
    if (ImGui.IsItemHovered())
    {
        ImGui.SetTooltip("Add new terminal (Coming soon)");
    }
}
```

#### RenderSettingsArea()
```csharp
private void RenderSettingsArea()
{
    // Font size controls
    ImGui.Text("Font Size:");
    ImGui.SameLine();
    
    float currentSize = _fontConfig.FontSize;
    if (ImGui.SliderFloat("##font_size", ref currentSize, 8.0f, 72.0f, "%.1f"))
    {
        if (Math.Abs(currentSize - _fontConfig.FontSize) > 0.1f)
        {
            var newConfig = _fontConfig with { FontSize = currentSize };
            UpdateFontConfig(newConfig);
        }
    }
    
    ImGui.SameLine();
    
    // Terminal info (moved from top)
    ImGui.Text($"Terminal: {_terminal.Width}x{_terminal.Height}");
    ImGui.SameLine();
    ImGui.Text($"Cursor: ({_terminal.Cursor.Row}, {_terminal.Cursor.Col})");
    ImGui.SameLine();
    ImGui.Text($"Process: {(_processManager.IsRunning ? $"PID: {_processManager.ProcessId}" : "Stopped")}");
    
    if (_processManager.ExitCode.HasValue)
    {
        ImGui.SameLine();
        ImGui.Text($"Exit: {_processManager.ExitCode}");
    }
}
```

#### RenderTerminalCanvas()
```csharp
private void RenderTerminalCanvas()
{
    // Calculate available space for terminal content
    float2 availableSize = ImGui.GetContentRegionAvail();
    
    // Update terminal dimensions based on available space
    HandleCanvasResize(availableSize);
    
    // Render terminal content (existing logic from RenderTerminalContent)
    ImDrawListPtr drawList = ImGui.GetWindowDrawList();
    float2 windowPos = ImGui.GetCursorScreenPos();
    
    // ... existing terminal rendering logic ...
    RenderTerminalContentInternal(drawList, windowPos, availableSize);
}
```

### Helper Methods

#### ShowNotImplementedMessage()
```csharp
private void ShowNotImplementedMessage(string feature)
{
    // Simple popup or status message for future features
    Console.WriteLine($"TerminalController: {feature} not implemented in this phase");
    
    // Could show ImGui popup in future
    // ImGui.OpenPopup("Not Implemented");
}
```

#### HandleCanvasResize()
```csharp
private void HandleCanvasResize(float2 availableSize)
{
    // Similar to existing HandleWindowResize() but for canvas-specific sizing
    var newDimensions = CalculateTerminalDimensions(availableSize);
    if (newDimensions.HasValue)
    {
        var (newCols, newRows) = newDimensions.Value;
        if (newCols != _terminal.Width || newRows != _terminal.Height)
        {
            _terminal.Resize(newCols, newRows);
            if (_processManager.IsRunning)
            {
                _processManager.Resize(newCols, newRows);
            }
        }
    }
}
```

## Data Models

### Layout Constants
```csharp
private static class LayoutConstants
{
    public const float MENU_BAR_HEIGHT = 25.0f;
    public const float TAB_AREA_HEIGHT = 30.0f;
    public const float SETTINGS_AREA_HEIGHT = 40.0f;
    public const float ADD_BUTTON_WIDTH = 30.0f;
    public const float ELEMENT_SPACING = 5.0f;
    public const float WINDOW_PADDING = 10.0f;
}
```

### Settings State
```csharp
// Terminal-specific settings (for future multi-terminal support)
private class TerminalSettings
{
    public float FontSize { get; set; }
    public string FontName { get; set; } = "HackNerdFontMono-Regular";
    public bool ShowLineNumbers { get; set; } = false;
    public bool WordWrap { get; set; } = false;
    
    // Future: Color scheme, cursor style, etc.
}

private TerminalSettings _currentTerminalSettings = new();
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property Reflection

After analyzing all acceptance criteria, several properties can be consolidated:
- Layout positioning properties (menu bar above tabs, tabs above settings, etc.) can be combined into a single hierarchical layout property
- Width spanning properties (menu bar, tab area, settings area all span full width) can be combined into a single full-width property
- Height consistency properties during resize can be combined into a single resize stability property
- Widget type verification properties can be combined into appropriate widget usage property

### Property 1: Hierarchical Layout Order
*For any* terminal window render, the layout elements should appear in the correct hierarchical order: menu bar at top, followed by tab area, then settings area, then terminal canvas at bottom
**Validates: Requirements 1.5, 2.1, 3.1, 4.1**

### Property 2: Full Width Layout Elements
*For any* terminal window size, the menu bar, tab area, and settings area should all span the full width of the window content area
**Validates: Requirements 1.4, 2.1, 3.1**

### Property 3: Single Terminal Instance Constraint
*For any* terminal window state, exactly one terminal instance should be managed and exactly one tab should be displayed in the tab area
**Validates: Requirements 2.2, 6.1, 6.4**

### Property 4: Settings Apply to Current Terminal
*For any* settings modification in the settings area, the changes should apply only to the current terminal instance and be immediately reflected in terminal behavior
**Validates: Requirements 3.3, 5.2, 5.3**

### Property 5: Terminal Canvas Space Utilization
*For any* window resize operation, the terminal canvas should utilize all remaining space after accounting for the fixed-height header areas (menu bar, tab area, settings area)
**Validates: Requirements 4.3, 4.5, 7.1, 7.5**

### Property 6: Layout Stability During Resize
*For any* window resize operation, the menu bar and tab area should maintain consistent heights while the settings area adapts its content layout to the available width
**Validates: Requirements 7.2, 7.3, 7.4**

### Property 7: Terminal Functionality Preservation
*For any* terminal operation (input, output, cursor movement, scrolling), the terminal canvas should continue to display content and respond to input exactly as it did before the layout redesign
**Validates: Requirements 4.2, 4.4, 6.2, 6.3**

### Property 8: ImGui Widget Usage Compliance
*For any* UI area (menu bar, tab area, settings area), the appropriate ImGui widgets should be used: BeginMenuBar/MenuItem for menus, Button for tabs, and appropriate controls for settings
**Validates: Requirements 8.1, 8.2, 8.3, 8.4**

### Property 9: Info Bar Integration
*For any* terminal state change (resize, process status change), the info bar information should be displayed in the settings area and update dynamically without interfering with other elements
**Validates: Requirements 5.1, 5.2, 5.3, 5.4**

## Error Handling

### Layout Calculation Errors
- If terminal dimensions cannot be calculated due to invalid window size, fall back to minimum viable dimensions (80x24)
- If font metrics are unavailable, use fallback character dimensions from configuration
- Log layout calculation failures for debugging without crashing the application

### ImGui API Errors
- Wrap all ImGui calls in try-catch blocks to prevent crashes from ImGui state issues
- Provide fallback rendering for menu items if BeginMenuBar() fails
- Handle font loading failures gracefully by falling back to default fonts

### Settings Validation
- Validate font size ranges (8.0f to 72.0f) before applying changes
- Prevent invalid terminal dimensions from being applied
- Revert to previous settings if new settings cause rendering failures

### Future Feature Handling
- Show appropriate "not implemented" messages for multi-terminal features
- Disable menu items that are not yet functional
- Provide tooltips explaining future functionality

## Testing Strategy

### Unit Tests
Unit tests will verify specific examples and edge cases:
- Menu bar contains expected menu items ("File", "Edit", "View")
- Tab area displays exactly one tab with correct label
- Add button is present and shows appropriate tooltip
- Settings area contains expected controls (font size slider, info display)
- Layout constants are within reasonable ranges
- Error handling for invalid window dimensions

### Property-Based Tests
Property-based tests will verify universal properties across many inputs:
- **Layout hierarchy property**: Generate random window sizes and verify element ordering
- **Full-width property**: Test various window widths and verify elements span correctly
- **Single terminal property**: Verify exactly one terminal instance across different states
- **Settings application property**: Generate random settings changes and verify they apply correctly
- **Space utilization property**: Test various window sizes and verify canvas uses remaining space
- **Resize stability property**: Generate resize sequences and verify layout stability
- **Functionality preservation property**: Test terminal operations and verify behavior unchanged
- **Widget compliance property**: Verify correct ImGui widget usage across all areas
- **Info integration property**: Test state changes and verify info updates correctly

Each property test will run a minimum of 100 iterations to ensure comprehensive coverage through randomization. Tests will be tagged with the format: **Feature: window-design, Property N: [property description]** to link back to design properties.

### Integration Tests
- Test complete window rendering pipeline from initialization to display
- Verify interaction between layout areas (e.g., settings changes affecting terminal canvas)
- Test window resize handling across the entire layout
- Verify focus management works correctly with new layout structure