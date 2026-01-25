using System;
using Brutal.Numerics;
using Brutal.ImGuiApi;
using StarMap.API;
using caTTY.SkunkworksGameMod.Camera;
using caTTY.SkunkworksGameMod.Camera.Animation;
using caTTY.SkunkworksGameMod.UI;

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

    // Camera system components
    private ICameraService? _cameraService;
    private ICameraAnimationPlayer? _animationPlayer;
    private CameraDebugPanel? _cameraDebugPanel;

    // Animation state tracking
    private bool _wasAnimationPlaying = false;

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

            // Initialize camera system
            _cameraService = new KsaCameraService();
            _animationPlayer = new CameraAnimationPlayer();
            _cameraDebugPanel = new CameraDebugPanel(_cameraService, _animationPlayer);

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

            // Track animation state transitions
            bool isCurrentlyPlaying = _animationPlayer?.IsPlaying ?? false;

            // Update camera service (handles manual follow updates)
            _cameraService?.Update(dt);

            // Update animation player and apply frame to camera
            var animFrame = _animationPlayer?.Update(dt);
            if (animFrame.HasValue && _cameraService != null)
            {
                var frame = animFrame.Value;

                // Apply position offset (relative to target)
                var targetPos = _cameraService.GetTargetPosition();
                _cameraService.Position = targetPos + frame.Offset;

                // Look at target (overrides YPR from keyframes)
                _cameraService.LookAt(targetPos);

                // Apply FOV
                _cameraService.FieldOfView = frame.Fov;
            }

            // Restore camera state when animation ends
            if (_wasAnimationPlaying && !isCurrentlyPlaying && _cameraService != null)
            {
                // Animation just ended - restore camera to safe state
                Console.WriteLine("Skunkworks: Animation ended, restoring camera state");

                // Stop manual following and restore normal camera follow
                if (_cameraService.IsManualFollowing)
                {
                    _cameraService.StopManualFollow();
                }

                // The camera should now be back in normal follow mode
                // You may want to re-follow the target here if needed
            }

            _wasAnimationPlaying = isCurrentlyPlaying;

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
        // Set initial window size (larger for camera controls)
        ImGui.SetNextWindowSize(new float2(600, 700), ImGuiCond.FirstUseEver);

        // Begin window
        if (ImGui.Begin("Skunkworks Mod", ref _windowVisible))
        {
            // Header
            ImGui.TextColored(new float4(0.0f, 1.0f, 0.0f, 1.0f), "Camera Orbit Test Panel");
            ImGui.Separator();

            // Status information
            ImGui.Text("Status: Running");
            ImGui.Text("Version: 0.1.0 - Camera Orbit");

            // Calculate uptime
            var uptime = DateTime.Now - _startTime;
            ImGui.Text($"Uptime: {uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}");

            ImGui.Separator();

            // Camera debug panel
            if (_cameraDebugPanel != null)
            {
                _cameraDebugPanel.Render();
            }
            else
            {
                ImGui.TextColored(new float4(1, 0, 0, 1), "Camera system not initialized");
            }

            ImGui.Spacing();
            ImGui.Separator();

            // Close button
            if (ImGui.Button("Close"))
            {
                _windowVisible = false;
            }
        }
        ImGui.End();
    }
}
