using System;
using System.Text.Json;
using caTTY.SkunkworksGameMod.Camera;
using caTTY.SkunkworksGameMod.Camera.Actions;
using caTTY.SkunkworksGameMod.Camera.Animation;

namespace caTTY.SkunkworksGameMod.Rpc.Actions;

/// <summary>
/// RPC action handler for "camera-orbit" command.
/// Generates and executes an orbital camera animation around the followed target.
/// </summary>
public class CameraOrbitRpcAction : ISocketRpcAction
{
    public string ActionName => "camera-orbit";

    private readonly ICameraService _cameraService;
    private readonly ICameraAnimationPlayer _animationPlayer;
    private readonly OrbitCameraAction _orbitAction;

    public CameraOrbitRpcAction(
        ICameraService cameraService,
        ICameraAnimationPlayer animationPlayer)
    {
        _cameraService = cameraService;
        _animationPlayer = animationPlayer;
        _orbitAction = new OrbitCameraAction();
    }

    public SocketRpcResponse Execute(JsonElement? @params)
    {
        try
        {
            // Parse parameters
            var orbitParams = ParseParams(@params);

            // Validate lerp parameters
            if (orbitParams.Lerp && !orbitParams.LerpTime.HasValue)
            {
                return SocketRpcResponse.Fail("lerpTime required when lerp=true");
            }

            // Check camera availability
            if (!_cameraService.IsAvailable)
            {
                return SocketRpcResponse.Fail("Camera not available");
            }

            if (_cameraService.FollowTarget == null)
            {
                return SocketRpcResponse.Fail("No follow target - camera must be following an object");
            }

            // Build context
            var targetPosition = _cameraService.GetTargetPosition();
            var context = new CameraActionContext
            {
                Camera = _cameraService,
                TargetPosition = targetPosition,
                CurrentOffset = _cameraService.Position - targetPosition,
                CurrentFov = _cameraService.FieldOfView,
                CurrentRotation = _cameraService.Rotation,
                Duration = orbitParams.Time,
                Distance = orbitParams.Distance,
                UseLerp = orbitParams.Lerp,
                LerpTime = orbitParams.LerpTime ?? 0f,
                Easing = orbitParams.Easing,
                CounterClockwise = orbitParams.CounterClockwise
            };

            // Validate
            var validation = _orbitAction.Validate(context);
            if (!validation.IsValid)
            {
                return SocketRpcResponse.Fail(validation.ErrorMessage ?? "Validation failed");
            }

            // Generate keyframes
            var keyframes = _orbitAction.GenerateKeyframes(context);

            // Load keyframes and start animation
            _animationPlayer.ClearKeyframes();
            _animationPlayer.SetKeyframes(keyframes);

            // Set up manual follow with zero offset (offset handled by animation)
            _cameraService.StartManualFollow(Brutal.Numerics.double3.Zero);

            // Play animation
            _animationPlayer.Play();

            return SocketRpcResponse.Ok(new
            {
                status = "playing",
                duration = orbitParams.Time,
                distance = orbitParams.Distance,
                useLerp = orbitParams.Lerp,
                counterClockwise = orbitParams.CounterClockwise
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CameraOrbitRpcAction: Error executing orbit: {ex.Message}");
            return SocketRpcResponse.Fail($"Failed to execute orbit: {ex.Message}");
        }
    }

    private OrbitParams ParseParams(JsonElement? @params)
    {
        var result = new OrbitParams();

        if (@params == null || @params.Value.ValueKind == JsonValueKind.Null)
        {
            return result;
        }

        var elem = @params.Value;

        if (elem.ValueKind == JsonValueKind.Object)
        {
            if (elem.TryGetProperty("time", out var timeProp))
            {
                result.Time = timeProp.GetSingle();
            }

            if (elem.TryGetProperty("distance", out var distanceProp))
            {
                result.Distance = distanceProp.GetSingle();
            }

            if (elem.TryGetProperty("lerp", out var lerpProp))
            {
                result.Lerp = lerpProp.GetBoolean();
            }

            if (elem.TryGetProperty("lerpTime", out var lerpTimeProp))
            {
                result.LerpTime = lerpTimeProp.GetSingle();
            }

            if (elem.TryGetProperty("counterClockwise", out var ccwProp))
            {
                result.CounterClockwise = ccwProp.GetBoolean();
            }

            if (elem.TryGetProperty("easing", out var easingProp))
            {
                var easingStr = easingProp.GetString();
                result.Easing = easingStr?.ToLowerInvariant() switch
                {
                    "linear" => EasingType.Linear,
                    "easein" => EasingType.EaseIn,
                    "easeout" => EasingType.EaseOut,
                    "easeinout" => EasingType.EaseInOut,
                    _ => EasingType.EaseInOut
                };
            }
        }

        return result;
    }

    private class OrbitParams
    {
        public float Time { get; set; } = 5.0f;
        public float Distance { get; set; } = 100.0f;
        public bool Lerp { get; set; } = false;
        public float? LerpTime { get; set; }
        public bool CounterClockwise { get; set; } = false;
        public EasingType Easing { get; set; } = EasingType.EaseInOut;
    }
}
