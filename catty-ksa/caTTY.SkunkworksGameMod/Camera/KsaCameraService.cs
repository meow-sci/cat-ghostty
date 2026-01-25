using System;
using System.Reflection;
using Brutal.Numerics;
using KSA;

namespace caTTY.SkunkworksGameMod.Camera;

/// <summary>
/// KSA-specific camera service implementation using reflection.
/// Handles camera access, manual follow mode, and rotation/position updates.
/// </summary>
public class KsaCameraService : ICameraService
{
    private KSA.Camera? _camera;
    private dynamic? _followedObject;
    private double3 _followOffset;
    private bool _isManualFollowing;

    public bool IsAvailable => GetCamera() != null;

    public double3 Position
    {
        get => GetCamera()?.PositionEcl ?? double3.Zero;
        set
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera.PositionEcl = value;
            }
        }
    }

    public doubleQuat Rotation
    {
        get => GetCamera()?.WorldRotation ?? new doubleQuat(0, 0, 0, 1);
        set
        {
            var camera = GetCamera();
            if (camera != null)
            {
                camera.WorldRotation = value;
            }
        }
    }

    public float FieldOfView
    {
        get
        {
            var camera = GetCamera();
            if (camera == null) return 60.0f;
            // GetFieldOfView returns radians, convert to degrees
            return camera.GetFieldOfView() * 57.2958f;
        }
        set
        {
            var camera = GetCamera();
            if (camera != null)
            {
                // SetFieldOfView accepts degrees
                camera.SetFieldOfView(value);
            }
        }
    }

    public double3 Forward => GetCamera()?.GetForward() ?? new double3(0, 1, 0);
    public double3 Right => GetCamera()?.GetRight() ?? new double3(1, 0, 0);
    public double3 Up => GetCamera()?.GetUp() ?? new double3(0, 0, 1);

    public object? FollowTarget => _isManualFollowing ? _followedObject : GetCamera()?.Following;

    public bool IsManualFollowing => _isManualFollowing;

    public double3 GetTargetPosition()
    {
        var target = FollowTarget;
        if (target == null)
        {
            return Position;
        }

        try
        {
            // Call GetPositionEcl() on the dynamic target
            dynamic dynTarget = target;
            return dynTarget.GetPositionEcl();
        }
        catch
        {
            return Position;
        }
    }

    public void StartManualFollow(double3 offset)
    {
        var camera = GetCamera();
        if (camera == null) return;

        var currentFollowing = camera.Following;
        if (currentFollowing != null)
        {
            _followedObject = currentFollowing;
            _followOffset = offset;
            _isManualFollowing = true;
            camera.Unfollow();
        }
    }

    public void StopManualFollow()
    {
        _isManualFollowing = false;
        _followedObject = null;
        _followOffset = double3.Zero;
    }

    public void LookAt(double3 target)
    {
        var camera = GetCamera();
        if (camera == null) return;

        // ECL up vector (Z-axis)
        var upEcl = new double3(0, 0, 1);
        camera.LookAt(target, upEcl);
    }

    public void ApplyRotation(float yaw, float pitch, float roll)
    {
        var camera = GetCamera();
        if (camera == null) return;

        // Convert degrees to radians
        var yawRad = yaw * (Math.PI / 180.0);
        var pitchRad = pitch * (Math.PI / 180.0);
        var rollRad = roll * (Math.PI / 180.0);

        // Create rotation quaternions in ECL space
        // Yaw around Z (Up), Pitch around X (Right), Roll around Y (Forward)
        var yawQuat = doubleQuat.CreateFromAxisAngle(new double3(0, 0, 1), yawRad);
        var pitchQuat = doubleQuat.CreateFromAxisAngle(new double3(1, 0, 0), pitchRad);
        var rollQuat = doubleQuat.CreateFromAxisAngle(new double3(0, 1, 0), rollRad);

        // Combine rotations (extrinsic ZXY order)
        var newRot = yawQuat * pitchQuat * rollQuat;

        // Convert to float quaternion for matrix creation
        var fQuat = new floatQuat(
            (float)newRot.X,
            (float)newRot.Y,
            (float)newRot.Z,
            (float)newRot.W
        );

        // Create rotation matrix
        var rotMatrix = float4x4.CreateFromQuaternion(fQuat);

        // CRITICAL: Preserve position when using SetMatrix
        var savedPos = camera.PositionEcl;
        var savedLocalPos = camera.LocalPosition;

        camera.SetMatrix(rotMatrix);

        // Restore position to prevent drift
        camera.LocalPosition = savedLocalPos;
        camera.PositionEcl = savedPos;
        camera.WorldRotation = newRot;
    }

    public void Update(double deltaTime)
    {
        // Update manual follow position if active
        if (_isManualFollowing && _followedObject != null)
        {
            try
            {
                dynamic? dynTarget = _followedObject;
                double3 targetPos = dynTarget?.GetPositionEcl() ?? double3.Zero;
                var camera = GetCamera();
                if (camera != null)
                {
                    camera.PositionEcl = targetPos + _followOffset;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"KsaCameraService: Error updating manual follow: {ex.Message}");
                _isManualFollowing = false;
                _followedObject = null;
            }
        }
    }

    /// <summary>
    /// Gets the KSA camera instance via reflection.
    /// Caches the result for performance.
    /// </summary>
    private KSA.Camera? GetCamera()
    {
        // Return cached camera if available
        if (_camera != null) return _camera;

        try
        {
            var ksaAssembly = typeof(KSA.Camera).Assembly;
            var programType = ksaAssembly.GetType("KSA.Program");

            if (programType != null)
            {
                // Try GetMainCamera first, fall back to GetCamera
                var getMainCameraMethod = programType.GetMethod("GetMainCamera",
                    BindingFlags.Public | BindingFlags.Static);
                var getCameraMethod = programType.GetMethod("GetCamera",
                    BindingFlags.Public | BindingFlags.Static);

                MethodInfo? methodToUse = getMainCameraMethod ?? getCameraMethod;

                if (methodToUse != null)
                {
                    _camera = methodToUse.Invoke(null, null) as KSA.Camera;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KsaCameraService: Error getting camera: {ex.Message}");
        }

        return _camera;
    }
}
