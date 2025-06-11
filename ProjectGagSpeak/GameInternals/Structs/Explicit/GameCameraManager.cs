using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

// taken from hybrid camera as a means to gain control over the camera object
namespace GagSpeak.GameInternals.Structs;

/// <summary>
///     FFXIVClientStructs.FFXIV.Client.Game.Control.GameCameraManager with some additional fields
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x180)]
public unsafe partial struct GameCameraManager {
    public static GameCameraManager* Instance() => (GameCameraManager*)Control.Instance();

    [FieldOffset(0x00)] public GameCamera* Camera; // "WorldCamera"
    [FieldOffset(0x08)] public GameLowCutCamera* LowCutCamera; // "IdleCamera"
    [FieldOffset(0x10)] public GameLobbyCamera* LobbCamera; // "MenuCamera"
    [FieldOffset(0x18)] public GameCamera3* Camera3; // "SpectatorCamera"
    [FieldOffset(0x20)] public GameCamera4* Camera4;

    [FieldOffset(0x48)] public int ActiveCameraIndex;
    [FieldOffset(0x4C)] public int PreviousCameraIndex;

    [FieldOffset(0x60)] public CameraBase UnkCamera; // not a pointer

    public GameCamera* GetActiveCamera() => (GameCamera*)CameraManager.Instance();
}

