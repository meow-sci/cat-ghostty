using Brutal.Numerics;
using caTTY.SkunkworksGameMod.Camera.Animation;

namespace caTTY.SkunkworksGameMod.Camera.Actions;

/// <summary>
/// Shared context for camera action execution.
/// Contains camera state and action parameters.
/// </summary>
public sealed class CameraActionContext
{
    /// <summary>
    /// Camera service for accessing and manipulating the camera.
    /// </summary>
    public required ICameraService Camera { get; init; }

    /// <summary>
    /// Position of the target being followed (in ECL coordinates).
    /// </summary>
    public required double3 TargetPosition { get; init; }

    /// <summary>
    /// Current camera offset from the target.
    /// </summary>
    public required double3 CurrentOffset { get; init; }

    /// <summary>
    /// Current field of view in degrees.
    /// </summary>
    public required float CurrentFov { get; init; }

    /// <summary>
    /// Current camera rotation quaternion.
    /// </summary>
    public required doubleQuat CurrentRotation { get; init; }

    /// <summary>
    /// Animation duration in seconds.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Distance parameter (usage varies by action).
    /// For orbit: orbit radius in meters.
    /// </summary>
    public float Distance { get; init; }

    /// <summary>
    /// Whether to smoothly lerp to the start position before beginning the main animation.
    /// </summary>
    public bool UseLerp { get; init; }

    /// <summary>
    /// Duration of the lerp phase in seconds (if UseLerp is true).
    /// </summary>
    public float LerpTime { get; init; }

    /// <summary>
    /// Easing function to apply to the animation.
    /// </summary>
    public EasingType Easing { get; init; } = EasingType.EaseInOut;

    /// <summary>
    /// Whether to orbit counter-clockwise (for orbit action).
    /// </summary>
    public bool CounterClockwise { get; init; }
}
