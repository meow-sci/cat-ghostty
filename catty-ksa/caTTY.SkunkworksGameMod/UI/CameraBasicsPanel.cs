using Brutal.ImGuiApi;
using caTTY.SkunkworksGameMod.Camera;

namespace caTTY.SkunkworksGameMod.UI;

/// <summary>
/// ImGui panel for testing basic camera control operations.
/// Provides UI for mode switching and manual camera movement.
/// </summary>
public class CameraBasicsPanel
{
    private readonly ICameraService _cameraService;
    
    // Movement parameters (UI state)
    // These fields will be used in Tasks 3.2 and 3.3
#pragma warning disable CS0414 // Field is assigned but its value is never used
    private float _moveDistance = 10.0f; // meters
    private float _rotationDegrees = 15.0f; // degrees

    // SetFollow flag experiment UI state
    private bool _setFollowUnknown0 = false;
    private bool _setFollowChangeControl = false;
    private bool _setFollowAlert = false;
#pragma warning restore CS0414
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CameraBasicsPanel"/> class.
    /// </summary>
    /// <param name="cameraService">The camera service to use for operations.</param>
    public CameraBasicsPanel(ICameraService cameraService)
    {
        _cameraService = cameraService;
    }
    
    /// <summary>
    /// Renders the camera basics panel.
    /// </summary>
    public void Render()
    {
        RenderCameraModeSection();
        ImGui.Spacing();
        ImGui.Separator();
        RenderCameraMovementSection();
    }
    
    /// <summary>
    /// Renders the camera mode section.
    /// To be implemented in Task 3.2.
    /// </summary>
    private void RenderCameraModeSection()
    {
        // To be implemented in Task 3.2
    }
    
    /// <summary>
    /// Renders the camera movement section.
    /// To be implemented in Task 3.3.
    /// </summary>
    private void RenderCameraMovementSection()
    {
        // To be implemented in Task 3.3
    }
}
