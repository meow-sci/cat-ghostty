using System;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using caTTY.SkunkworksGameMod.Camera;
using caTTY.SkunkworksGameMod.Camera.Animation;
using caTTY.SkunkworksGameMod.Rpc.Actions;

namespace caTTY.SkunkworksGameMod.UI;

/// <summary>
/// ImGui debug panel for camera control and animation testing.
/// </summary>
public class CameraDebugPanel
{
    private readonly ICameraService _cameraService;
    private readonly ICameraAnimationPlayer _animationPlayer;
    private readonly CameraOrbitRpcAction _orbitAction;
    private readonly KeyframePreviewPanel _previewPanel;

    // Orbit action parameters (UI state)
    private float _duration = 5.0f;
    private float _distance = 100.0f;
    private bool _useLerp = false;
    private float _lerpTime = 1.0f;
    private bool _counterClockwise = false;
    private int _easingIndex = 3; // EaseInOut

    public CameraDebugPanel(
        ICameraService cameraService,
        ICameraAnimationPlayer animationPlayer)
    {
        _cameraService = cameraService;
        _animationPlayer = animationPlayer;
        _orbitAction = new CameraOrbitRpcAction(cameraService, animationPlayer);
        _previewPanel = new KeyframePreviewPanel();
    }

    /// <summary>
    /// Renders the camera debug panel.
    /// </summary>
    public void Render()
    {
        ImGui.SeparatorText("Camera Info");
        RenderCameraInfo();

        ImGui.Spacing();
        ImGui.SeparatorText("Orbit Action");
        RenderOrbitControls();

        ImGui.Spacing();
        ImGui.SeparatorText("Animation Status");
        RenderAnimationStatus();

        ImGui.Spacing();
        ImGui.SeparatorText("Keyframe Preview");
        _previewPanel.Render(_cameraService);
    }

    private void RenderCameraInfo()
    {
        if (!_cameraService.IsAvailable)
        {
            ImGui.TextColored(new float4(1, 0, 0, 1), "Camera not available");
            return;
        }

        var pos = _cameraService.Position;
        ImGui.Text($"Position: ({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1})");

        var fov = _cameraService.FieldOfView;
        ImGui.Text($"FOV: {fov:F1}°");

        var target = _cameraService.FollowTarget;
        if (target != null)
        {
            ImGui.TextColored(new float4(0, 1, 0, 1), "Following target");
            if (_cameraService.IsManualFollowing)
            {
                ImGui.SameLine();
                ImGui.TextDisabled("(manual)");
            }
        }
        else
        {
            ImGui.TextColored(new float4(1, 0.5f, 0, 1), "No follow target");
        }
    }

    private void RenderOrbitControls()
    {
        // Duration slider
        ImGui.SliderFloat("Duration (s)", ref _duration, 0.1f, 30.0f);

        // Distance slider
        ImGui.SliderFloat("Distance (m)", ref _distance, 10.0f, 1000.0f);

        // Lerp checkbox
        ImGui.Checkbox("Lerp to start", ref _useLerp);

        // Lerp time (conditional)
        if (_useLerp)
        {
            ImGui.SliderFloat("Lerp Time (s)", ref _lerpTime, 0.1f, 10.0f);
        }
        else
        {
            ImGui.BeginDisabled();
            float disabledLerpTime = _lerpTime;
            ImGui.SliderFloat("Lerp Time (s)", ref disabledLerpTime, 0.1f, 10.0f);
            ImGui.EndDisabled();
        }

        // Easing radio buttons
        ImGui.Text("Easing:");
        ImGui.RadioButton("Linear", ref _easingIndex, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Ease In", ref _easingIndex, 1);
        ImGui.SameLine();
        ImGui.RadioButton("Ease Out", ref _easingIndex, 2);
        ImGui.SameLine();
        ImGui.RadioButton("Ease In-Out", ref _easingIndex, 3);

        // Counter-clockwise checkbox
        ImGui.Checkbox("Counter-clockwise", ref _counterClockwise);

        ImGui.Spacing();

        // Action buttons
        if (ImGui.Button("Preview Keyframes"))
        {
            PreviewOrbitKeyframes();
        }

        ImGui.SameLine();
        if (ImGui.Button("Execute Orbit"))
        {
            ExecuteOrbit();
        }

        ImGui.SameLine();
        if (ImGui.Button("Stop"))
        {
            StopAnimation();
        }
    }

    private void RenderAnimationStatus()
    {
        if (_animationPlayer.IsPlaying)
        {
            ImGui.TextColored(new float4(0, 1, 0, 1), "PLAYING");
            ImGui.SameLine();
            ImGui.Text($"{_animationPlayer.CurrentTime:F2}s / {_animationPlayer.Duration:F2}s");

            // Progress bar
            float progress = _animationPlayer.Duration > 0
                ? _animationPlayer.CurrentTime / _animationPlayer.Duration
                : 0f;
            ImGui.ProgressBar(progress, new float2(0, 0));
        }
        else
        {
            ImGui.TextDisabled("Not playing");
        }

        ImGui.Text($"Keyframes loaded: {_animationPlayer.Keyframes.Count}");
    }

    private void PreviewOrbitKeyframes()
    {
        try
        {
            var context = BuildOrbitContext();
            if (context == null)
            {
                Console.WriteLine("CameraDebugPanel: Cannot preview - no follow target");
                return;
            }

            var orbitAction = new Camera.Actions.OrbitCameraAction();
            var validation = orbitAction.Validate(context);
            if (!validation.IsValid)
            {
                Console.WriteLine($"CameraDebugPanel: Validation failed - {validation.ErrorMessage}");
                return;
            }

            var keyframes = orbitAction.GenerateKeyframes(context);
            _previewPanel.SetPreviewKeyframes(keyframes);
            Console.WriteLine($"CameraDebugPanel: Generated {System.Linq.Enumerable.Count(keyframes)} preview keyframes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CameraDebugPanel: Error previewing keyframes: {ex.Message}");
        }
    }

    private void ExecuteOrbit()
    {
        try
        {
            // Build JSON params
            var paramsJson = System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                time = _duration,
                distance = _distance,
                lerp = _useLerp,
                lerpTime = _useLerp ? _lerpTime : (float?)null,
                counterClockwise = _counterClockwise,
                easing = GetEasingString()
            });

            var response = _orbitAction.Execute(paramsJson);

            if (response.Success)
            {
                Console.WriteLine("CameraDebugPanel: Orbit started successfully");
            }
            else
            {
                Console.WriteLine($"CameraDebugPanel: Orbit failed - {response.Error}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CameraDebugPanel: Error executing orbit: {ex.Message}");
        }
    }

    private void StopAnimation()
    {
        _animationPlayer.Stop();
        _cameraService.StopManualFollow();
        Console.WriteLine("CameraDebugPanel: Animation stopped");
    }

    private Camera.Actions.CameraActionContext? BuildOrbitContext()
    {
        if (!_cameraService.IsAvailable || _cameraService.FollowTarget == null)
        {
            return null;
        }

        var targetPosition = _cameraService.GetTargetPosition();

        return new Camera.Actions.CameraActionContext
        {
            Camera = _cameraService,
            TargetPosition = targetPosition,
            CurrentOffset = _cameraService.Position - targetPosition,
            CurrentFov = _cameraService.FieldOfView,
            CurrentRotation = _cameraService.Rotation,
            Duration = _duration,
            Distance = _distance,
            UseLerp = _useLerp,
            LerpTime = _lerpTime,
            Easing = GetEasingType(),
            CounterClockwise = _counterClockwise
        };
    }

    private EasingType GetEasingType()
    {
        return _easingIndex switch
        {
            0 => EasingType.Linear,
            1 => EasingType.EaseIn,
            2 => EasingType.EaseOut,
            3 => EasingType.EaseInOut,
            _ => EasingType.EaseInOut
        };
    }

    private string GetEasingString()
    {
        return _easingIndex switch
        {
            0 => "linear",
            1 => "easein",
            2 => "easeout",
            3 => "easeinout",
            _ => "easeinout"
        };
    }
}
