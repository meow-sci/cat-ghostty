using Brutal.Numerics;

namespace caTTY.SkunkworksGameMod.Camera;

/// <summary>
/// Abstraction over KSA camera access and manipulation.
/// Provides a clean interface for camera operations without exposing KSA internals.
/// </summary>
public interface ICameraService
{
    /// <summary>
    /// Whether the camera is available and can be controlled.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Camera position in ECL (Ecliptic) coordinates.
    /// </summary>
    double3 Position { get; set; }

    /// <summary>
    /// Camera rotation as a quaternion in world space.
    /// </summary>
    doubleQuat Rotation { get; set; }

    /// <summary>
    /// Field of view in degrees.
    /// </summary>
    float FieldOfView { get; set; }

    /// <summary>
    /// Camera forward direction vector (normalized).
    /// </summary>
    double3 Forward { get; }

    /// <summary>
    /// Camera right direction vector (normalized).
    /// </summary>
    double3 Right { get; }

    /// <summary>
    /// Camera up direction vector (normalized).
    /// </summary>
    double3 Up { get; }

    /// <summary>
    /// The object the camera is currently following (or null).
    /// </summary>
    object? FollowTarget { get; }

    /// <summary>
    /// Gets the position of the current follow target.
    /// </summary>
    /// <returns>Target position in ECL coordinates.</returns>
    double3 GetTargetPosition();

    /// <summary>
    /// Starts manual follow mode with a specific offset from the target.
    /// This unfollows the target in KSA but maintains tracking manually.
    /// </summary>
    /// <param name="offset">Offset from target in ECL coordinates.</param>
    void StartManualFollow(double3 offset);

    /// <summary>
    /// Stops manual follow mode.
    /// </summary>
    void StopManualFollow();

    /// <summary>
    /// Whether manual follow mode is active.
    /// </summary>
    bool IsManualFollowing { get; }

    /// <summary>
    /// Orients the camera to look at a target position.
    /// </summary>
    /// <param name="target">Target position in ECL coordinates.</param>
    void LookAt(double3 target);

    /// <summary>
    /// Applies yaw/pitch/roll rotation to the camera.
    /// </summary>
    /// <param name="yaw">Yaw in degrees (around Z axis).</param>
    /// <param name="pitch">Pitch in degrees (around X axis).</param>
    /// <param name="roll">Roll in degrees (around Y axis).</param>
    void ApplyRotation(float yaw, float pitch, float roll);

    /// <summary>
    /// Updates the camera service (should be called each frame).
    /// Handles manual follow position updates.
    /// </summary>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    void Update(double deltaTime);
}
