using System;
using Brutal.Numerics;
using Brutal.ImGuiApi;
using StarMap.API;

namespace caTTY.SkunkworksGameMod;

[StarMapMod]
public class SkunkworksMod
{
    public bool ImmediateUnload => false;

    private bool _isInitialized = false;
    private bool _isDisposed = false;
    private bool _windowVisible = false;
    private int _frameCount = 0;
    private DateTime _startTime;

    [StarMapImmediateLoad]
    public void OnImmediateLoad()
    {
        Console.WriteLine("Skunkworks OnImmediateLoad");
    }

    [StarMapAllModsLoaded]
    public void OnFullyLoaded()
    {
        try
        {
            Console.WriteLine("Skunkworks OnFullyLoaded");
            Patcher.Patch();
            _isInitialized = true;
            _startTime = DateTime.Now;
            Console.WriteLine("Skunkworks: Initialized successfully. Press F11 to toggle window.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skunkworks: Error during initialization: {ex}");
        }
    }

    [StarMapBeforeGui]
    public void OnBeforeUi(double dt)
    {
        // No pre-UI logic needed
    }

    [StarMapAfterGui]
    public void OnAfterUi(double dt)
    {
        try
        {
            if (!_isInitialized || _isDisposed)
                return;

            // Check F11 key press
            if (ImGui.IsKeyPressed(ImGuiKey.F11))
            {
                _windowVisible = !_windowVisible;
            }

            // Render window if visible
            if (_windowVisible)
            {
                RenderWindow();
            }

            _frameCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skunkworks: Error in OnAfterUi: {ex}");
        }
    }

    [StarMapUnload]
    public void Unload()
    {
        try
        {
            Console.WriteLine("Skunkworks Unload");
            Patcher.Unload();
            _isDisposed = true;
            Console.WriteLine("Skunkworks: Unloaded successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skunkworks: Error during unload: {ex}");
        }
    }

    private void RenderWindow()
    {
        // Set initial window size
        ImGui.SetNextWindowSize(new float2(400, 300), ImGuiCond.FirstUseEver);

        // Begin window
        if (ImGui.Begin("Skunkworks Mod", ref _windowVisible))
        {
            // Header
            ImGui.TextColored(new float4(0.0f, 1.0f, 0.0f, 1.0f), "Hello from Skunkworks!");
            ImGui.Separator();

            // Status information
            ImGui.Text("Status: Running");
            ImGui.Text("Version: 0.1.0");

            // Calculate uptime
            var uptime = DateTime.Now - _startTime;
            ImGui.Text($"Uptime: {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}");
            ImGui.Text($"Frame count: {_frameCount}");

            ImGui.Separator();

            // Informative text
            ImGui.TextWrapped("This is a minimal KSA mod template demonstrating:");
            ImGui.BulletText("StarMap.API lifecycle hooks");
            ImGui.BulletText("ImGui window rendering");
            ImGui.BulletText("Harmony patching infrastructure");
            ImGui.BulletText("F11 keybind for window toggle");

            ImGui.Spacing();

            // Close button
            if (ImGui.Button("Close"))
            {
                _windowVisible = false;
            }
        }
        ImGui.End();
    }
}
