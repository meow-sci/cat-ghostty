// ImGui DPI Scaling Fix Example for Windows
// Demonstrates the problem AND solution for mouse offset issues

using System;
using System.Runtime.InteropServices;
using System.Numerics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using ImGuiNET;

using Vector2i = OpenTK.Mathematics.Vector2i;

namespace ImGuiDpiExample;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== ImGui DPI Scaling Fix Example ===\n");
        
        // CRITICAL: Set DPI awareness BEFORE creating any windows
        DpiHelper.SetDpiAwareness();
        
        var nativeSettings = new NativeWindowSettings
        {
            ClientSize = new Vector2i(1000, 700),
            Title = "ImGui DPI Fix - Interactive Demo",
            Flags = ContextFlags.ForwardCompatible,
        };
        
        using var window = new DpiTestWindow(GameWindowSettings.Default, nativeSettings);
        window.Run();
    }
}

static class DpiHelper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    
    [DllImport("shcore.dll", SetLastError = true)]
    private static extern int SetProcessDpiAwareness(int awareness);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDPIAware();
    
    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
    
    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new(-4);
    
    public static string DpiMethod = "None";
    
    public static void SetDpiAwareness()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        
        try
        {
            if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
            {
                DpiMethod = "PerMonitorAwareV2 (Win10 1703+)";
                Console.WriteLine($"[OK] DPI: {DpiMethod}");
                return;
            }
        }
        catch { }
        
        try
        {
            if (SetProcessDpiAwareness(2) == 0)
            {
                DpiMethod = "PerMonitorDpiAware (Win8.1+)";
                Console.WriteLine($"[OK] DPI: {DpiMethod}");
                return;
            }
        }
        catch { }
        
        try
        {
            if (SetProcessDPIAware())
            {
                DpiMethod = "SetProcessDPIAware (Legacy)";
                Console.WriteLine($"[OK] DPI: {DpiMethod}");
                return;
            }
        }
        catch { }
        
        DpiMethod = "FAILED - Expect mouse offset!";
        Console.WriteLine("[WARN] Could not set DPI awareness!");
    }
    
    public static float GetDpiScale(IntPtr hwnd)
    {
        try
        {
            var monitor = MonitorFromWindow(hwnd, 2);
            if (monitor != IntPtr.Zero && GetDpiForMonitor(monitor, 0, out uint dpiX, out _) == 0)
                return dpiX / 96.0f;
        }
        catch { }
        return 1.0f;
    }
}

class DpiTestWindow : GameWindow
{
    private ImGuiController _controller = null!;
    private float _dpiScale = 1.0f;
    private bool _simulateBroken = false;
    private int _correctClicks = 0;
    private int _missedClicks = 0;
    private Vector2 _lastClickPos;
    private Vector2 _targetPos = new(400, 400);
    private Random _rng = new();
    
    public DpiTestWindow(GameWindowSettings gameSettings, NativeWindowSettings nativeSettings)
        : base(gameSettings, nativeSettings) { }
    
    protected override void OnLoad()
    {
        base.OnLoad();
        _controller = new ImGuiController(ClientSize.X, ClientSize.Y);
        
        unsafe
        {
            var hwnd = (IntPtr)GLFW.GetWin32Window(WindowPtr);
            _dpiScale = DpiHelper.GetDpiScale(hwnd);
        }
        
        Console.WriteLine($"DPI Scale: {_dpiScale:F2}x ({_dpiScale * 96:F0} DPI)");
        ImGui.GetIO().FontGlobalScale = _dpiScale > 1.0f ? _dpiScale : 1.0f;
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
        _controller.WindowResized(ClientSize.X, ClientSize.Y);
    }
    
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
        if (KeyboardState.IsKeyPressed(Keys.Space)) _simulateBroken = !_simulateBroken;
    }
    
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButton.Left)
        {
            var io = ImGui.GetIO();
            _lastClickPos = io.MousePos;
            
            // Check if click hit the target
            float dist = Vector2.Distance(_lastClickPos, _targetPos);
            if (dist < 25)
            {
                _correctClicks++;
                // Move target to new random position
                _targetPos = new Vector2(100 + _rng.Next(800), 150 + _rng.Next(400));
            }
            else if (!ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow))
            {
                _missedClicks++;
            }
        }
    }
    
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        _controller.Update(this, (float)e.Time, _simulateBroken ? _dpiScale : 1.0f);
        
        GL.ClearColor(0.15f, 0.15f, 0.2f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        
        RenderImGui();
        _controller.Render();
        SwapBuffers();
    }

    private void RenderImGui()
    {
        var io = ImGui.GetIO();
        var mousePos = io.MousePos;
        var dl = ImGui.GetBackgroundDrawList();
        var dlFg = ImGui.GetForegroundDrawList();
        
        // Colors
        uint red = 0xFF0000FF;
        uint green = 0xFF00FF00;
        uint yellow = 0xFF00FFFF;
        uint white = 0xFFFFFFFF;
        uint darkBg = 0xFF1a1a2e;
        
        // Draw target circle
        uint targetColor = _simulateBroken ? red : green;
        dl.AddCircleFilled(_targetPos, 25, targetColor);
        dl.AddCircle(_targetPos, 25, white, 0, 2);
        dl.AddText(_targetPos - new Vector2(15, 5), white, "CLICK");
        
        // Draw crosshair at mouse position
        dlFg.AddLine(mousePos - new Vector2(20, 0), mousePos + new Vector2(20, 0), yellow, 2);
        dlFg.AddLine(mousePos - new Vector2(0, 20), mousePos + new Vector2(0, 20), yellow, 2);
        dlFg.AddCircle(mousePos, 10, yellow, 12, 2);
        
        // === INFO PANEL ===
        ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(380, 320), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("DPI Scaling Demo", ImGuiWindowFlags.NoCollapse))
        {
            // Status header
            if (_simulateBroken)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                ImGui.Text("STATUS: SIMULATING BROKEN DPI");
                ImGui.PopStyleColor();
                ImGui.TextWrapped("Mouse offset is artificially applied. Notice the crosshair doesn't match your cursor!");
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 1, 0.3f, 1));
                ImGui.Text("STATUS: DPI FIX ACTIVE");
                ImGui.PopStyleColor();
                ImGui.TextWrapped("Crosshair follows cursor correctly. Clicks register where expected.");
            }
            
            ImGui.Separator();
            ImGui.Spacing();
            
            // Toggle button
            string btnText = _simulateBroken ? "ENABLE FIX (Space)" : "SIMULATE BROKEN (Space)";
            Vector4 btnColor = _simulateBroken ? new Vector4(0.2f, 0.7f, 0.2f, 1) : new Vector4(0.7f, 0.2f, 0.2f, 1);
            ImGui.PushStyleColor(ImGuiCol.Button, btnColor);
            if (ImGui.Button(btnText, new Vector2(360, 35)))
                _simulateBroken = !_simulateBroken;
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            ImGui.Separator();
            
            // Stats
            ImGui.Text($"Target Hits: {_correctClicks}");
            ImGui.Text($"Misses: {_missedClicks}");
            ImGui.Text($"Mouse Position: {mousePos.X:F0}, {mousePos.Y:F0}");
            
            ImGui.Spacing();
            ImGui.Separator();
            
            // System info
            ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), "System Info:");
            ImGui.Text($"  DPI Scale: {_dpiScale:F2}x ({_dpiScale * 96:F0} DPI)");
            ImGui.Text($"  Window: {ClientSize.X}x{ClientSize.Y}");
            ImGui.Text($"  Framebuffer: {FramebufferSize.X}x{FramebufferSize.Y}");
            ImGui.Text($"  Fix Method: {DpiHelper.DpiMethod}");
        }
        ImGui.End();
        
        // === EXPLANATION PANEL ===
        ImGui.SetNextWindowPos(new Vector2(10, 340), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(380, 350), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("The Fix Explained", ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "WHY DOES THIS HAPPEN?");
            ImGui.TextWrapped(
                "When Windows DPI scaling is >100%, apps that aren't " +
                "'DPI-aware' get their coordinates virtualized. The app " +
                "thinks it's at 100% scale, but Windows is secretly scaling it.");
            
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "THE SOLUTION:");
            ImGui.Spacing();
            
            ImGui.TextColored(new Vector4(0, 1, 1, 1), "1. App Manifest (app.manifest)");
            ImGui.TextWrapped("  <dpiAwareness>PerMonitorV2</dpiAwareness>");
            
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0, 1, 1, 1), "2. Windows API (before window creation)");
            ImGui.TextWrapped("  SetProcessDpiAwarenessContext(...)");
            
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(0, 1, 1, 1), "3. ImGui IO Setup");
            ImGui.TextWrapped("  io.DisplayFramebufferScale = framebufferSize / windowSize");
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), 
                "Toggle 'Simulate Broken' to see the offset problem!");
        }
        ImGui.End();
        
        // === INSTRUCTIONS ===
        ImGui.SetNextWindowPos(new Vector2(600, 10), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSize(new Vector2(380, 130), ImGuiCond.FirstUseEver);
        
        if (ImGui.Begin("Instructions", ImGuiWindowFlags.NoCollapse))
        {
            ImGui.BulletText("Click the colored circle targets");
            ImGui.BulletText("Press SPACE to toggle broken/fixed");
            ImGui.BulletText("Yellow crosshair shows where ImGui thinks mouse is");
            ImGui.BulletText("When broken, crosshair won't match your cursor!");
        }
        ImGui.End();
    }
    
    protected override void OnUnload()
    {
        _controller.Dispose();
        base.OnUnload();
    }
}
