using System.Runtime.InteropServices;

// taken from hybrid camera as a means to gain control over the camera object
namespace GagSpeak.GameInternals.Structs;

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.CameraBase with some additional fields </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x120)]
public unsafe struct GameCameraBase {
    [FieldOffset(0x00)] public void** vtbl;
    [FieldOffset(0x10)] public FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera SceneCamera;
    [FieldOffset(0x60)] public float X;
    [FieldOffset(0x64)] public float Z;
    [FieldOffset(0x68)] public float Y;
    [FieldOffset(0x90)] public float LookAtX;
    [FieldOffset(0x94)] public float LookAtZ;
    [FieldOffset(0x98)] public float LookAtY;
    [FieldOffset(0x110)] public uint UnkUInt;
    [FieldOffset(0x118)] public uint UnkFlags;
}

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.Camera with some additional fields. </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x2C0)]
public unsafe struct GameCamera {
    [FieldOffset(0x00)] public GameCameraBase CameraBase;
    [FieldOffset(0x124)] public float Distance; // "CurrentZoom"
    [FieldOffset(0x128)] public float MinDistance; // "MinZoom"
    [FieldOffset(0x12C)] public float MaxDistance; // "MaxZoom"

    [FieldOffset(0x130)] public float FoV; // "CurrentFoV"
    [FieldOffset(0x134)] public float MinFoV;
    [FieldOffset(0x138)] public float MaxFoV;
    [FieldOffset(0x13C)] public float AddedFoV;

    //[FieldOffset(0x140)] public float Yaw; // "CurrentHRotation"
    //[FieldOffset(0x144)] public float Pitch; // "CurrentVRotation"
    //[FieldOffset(0x148)] public float YawDelta; // "HRotationDelta"
    [FieldOffset(0x140)] public float DirH; // 0 is north, increases CW
    [FieldOffset(0x144)] public float DirV; // 0 is horizontal, positive is looking up, negative looking down
    [FieldOffset(0x148)] public float MinPitch; // "MinVRotation", radians
    [FieldOffset(0x14C)] public float MaxPitch; // "MaxVRotation", radians
    [FieldOffset(0x150)] public float InputDeltaH;
    [FieldOffset(0x154)] public float InputDeltaV;
    [FieldOffset(0x158)] public float DirVMin; // -85deg by default
    [FieldOffset(0x15C)] public float DirVMax; // +45deg by default
    [FieldOffset(0x160)] public float Roll; // "Tilt", radians

    [FieldOffset(0x180)] public int Mode; // CameraControlMode
    [FieldOffset(0x184)] public int ControlType; // CameraControlType

    [FieldOffset(0x18C)] public float InterpDistance; // "InterpolatedZoom"
    [FieldOffset(0x198)] public float SavedDistance;
    [FieldOffset(0x190)] public float Transition; // new->unchanging, matches max fov? old->Seems to be related to the 1st <-> 3rd camera transition
    [FieldOffset(0x1A4)] public float TransitionFoV;
    [FieldOffset(0x1B0)] public float ViewX;
    [FieldOffset(0x1B4)] public float ViewZ;
    [FieldOffset(0x1B8)] public float ViewY;
    
    //[FieldOffset(0x1E4)] public byte IsFlipped; // this has moved
}

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.LobbyCamera with some additional fields </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x300)]
public unsafe struct GameLobbyCamera {
    [FieldOffset(0x00)] public GameCamera Camera;
    [FieldOffset(0x2F8)] public void* LobbyExcelSheet;
}

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.Camera3 with some additional fields </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x300)]
public struct GameCamera3 {
    [FieldOffset(0x00)] public GameCamera Camera;
}

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.LowCutCamera with some additional fields </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x2E0)]
public struct GameLowCutCamera {
    [FieldOffset(0x00)] public GameCameraBase CameraBase;
}

/// <summary> FFXIVClientStructs.FFXIV.Client.Game.Camera4 with some additional fields. </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x350)]
public struct GameCamera4 {
    [FieldOffset(0x00)] public GameCameraBase CameraBase;

    [FieldOffset(0x110)] public FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera SceneCamera0;
    [FieldOffset(0x200)] public FFXIVClientStructs.FFXIV.Client.Graphics.Scene.Camera SceneCamera1;
}

