using GagSpeak.GameInternals.Structs;
using ImGuiNET;
using static FFXIVClientStructs.FFXIV.Client.Game.Control.CameraManager.Delegates;

namespace GagSpeak.GameInternals.Addons;

/// <summary>
///     Provides helper methods for <see cref="GameCameraManager"/>, not included in FFXIVClientStructs by default.
///     To provide easier access and interaction.
/// </summary>
public static unsafe class AddonCameraManager
{
    /// <summary>
    ///     Provides access to the <see cref="GameCameraManager"/> instance.
    /// </summary>
    public static GameCameraManager* CameraManager => GameCameraManager.Instance();

    /// <summary>
    ///    Checks if the <see cref="GameCameraManager"/> is valid and initialized.
    /// </summary>
    public static bool IsManagerValid => CameraManager is not null;

    /// <summary>
    ///   Checks if the active camera is valid and initialized.
    /// </summary>
    public static bool IsActiveCameraValid => CameraManager->Camera is not null;

    public static GameCamera* ActiveCamera => CameraManager->Camera;


    // Additional Cameras that are probably not that useful.
    public static GameCamera3* SpectatorCamera => CameraManager->Camera3;

    /// <summary>
    ///     Gets the mode of the active camera in integer form.
    /// </summary>
    public static int GetMode()
    {
        if (!IsActiveCameraValid)
            return -1;
        return ActiveCamera->Mode;
    }

    /// <summary>
    ///   Gets the mode of the active camera in <see cref="CameraControlMode"/> format.
    /// </summary>
    public static CameraControlMode ActiveMode
    {
        get
        {
            var raw = GetMode();
            return Enum.IsDefined(typeof(CameraControlMode), raw) ? (CameraControlMode)raw : CameraControlMode.Unknown;
        }
    }

    /// <summary>
    ///   Sets the mode of the active camera to the specified <see cref="CameraControlMode"/>.
    /// </summary>
    /// <param name="mode"> The mode to set the camera to. </param>
    public static void SetMode(CameraControlMode mode)
    {
        if (!IsActiveCameraValid)
            return;

        if (ActiveMode == mode)
            return;

        // Set the mode.
        ActiveCamera->Mode = (int)mode;
    }

    public static void PrintToUI()
    {
        ImGui.Text("CameraManagerValid?: " + IsManagerValid);
        ImGui.Text("IsActiveCameraValid?: " + IsActiveCameraValid);
        if (IsActiveCameraValid)
        {
            unsafe
            {
                var camera = ActiveCamera;
                ImGui.Text($"Camera Zoom: {ActiveCamera->Distance}");
                ImGui.Text($"Camera Min Zoom: {ActiveCamera->MinDistance}");
                ImGui.Text($"Camera Max Zoom: {ActiveCamera->MaxDistance}");

                ImGui.Text($"Camera FoV: {ActiveCamera->FoV}");
                ImGui.Text($"Camera Min FoV: {ActiveCamera->MinFoV}");
                ImGui.Text($"Camera Max FoV: {ActiveCamera->MaxFoV}");
                ImGui.Text($"Camera Added FoV: {ActiveCamera->AddedFoV}");

                ImGui.Text($"Camera Mode: {ActiveCamera->Mode}");
                ImGui.Text($"Camera ControlType: {ActiveCamera->ControlType}");
            }
        }
    }
}
