# ImGui DPI Scaling Fix - Interactive Demo

An interactive demonstration of Windows DPI scaling issues with ImGui and how to fix them.

## Run It

```bash
cd C:\temp\ImGuiDpiExample
dotnet run
```

## What It Shows

This demo lets you **visually see** the DPI offset problem by toggling between fixed and broken states:

### Fixed Mode (Green)
- Yellow crosshair follows your actual cursor
- Clicking targets works correctly
- Buttons and sliders respond where you click

### Simulated Broken Mode (Red)  
- Yellow crosshair is OFFSET from your cursor
- You can't hit targets accurately
- Demonstrates what happens without DPI awareness

## Controls

- **SPACE** - Toggle between fixed/broken modes
- **Left Click** - Try to hit the colored target circles
- **ESC** - Exit

## The Three Fixes Demonstrated

### 1. Application Manifest (`app.manifest`)
```xml
<dpiAwareness>PerMonitorV2</dpiAwareness>
```

### 2. Windows API Call (before window creation)
```csharp
SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
```

### 3. Proper ImGui IO Configuration
```csharp
io.DisplayFramebufferScale = new Vector2(
    framebufferSize.X / windowSize.X,
    framebufferSize.Y / windowSize.Y);
```

## Files

| File | Purpose |
|------|---------|
| `app.manifest` | Declares DPI awareness to Windows |
| `Program.cs` | Main app + DpiHelper with Windows API calls |
| `ImGuiController.cs` | OpenGL ImGui backend with offset simulation |

## For Brutal.ImGui / GLFW Users

The same principles apply:
1. Add a DPI manifest to your application
2. Call `SetProcessDpiAwarenessContext` before ANY window creation
3. Ensure `io.DisplayFramebufferScale = FramebufferSize / WindowSize`
4. Don't manually scale mouse coordinates - they should be correct when DPI-aware
