using GagSpeak.GameInternals.Structs;

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
    public static bool IsActiveCameraValid => CameraManager->GetActiveCamera() is not null;

    // Additional Cameras that are probably not that useful.
    public static GameCamera3* SpectatorCamera => CameraManager->Camera3;
    public static GameCamera* ActiveCamera => CameraManager->GetActiveCamera();

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
}
