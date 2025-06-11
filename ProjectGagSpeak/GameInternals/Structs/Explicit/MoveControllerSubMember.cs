using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Runtime.InteropServices;

namespace GagSpeak.GameInternals.Structs;

[StructLayout(LayoutKind.Explicit)]
public unsafe struct UnkGameObjectStruct
{
    [FieldOffset(0xD0)] public int Unk_0xD0;
    [FieldOffset(0x101)] public byte Unk_0x101;
    [FieldOffset(0x1C0)] public Vector3 DesiredPosition;
    [FieldOffset(0x1D0)] public float NewRotation;
    [FieldOffset(0x1FC)] public byte Unk_0x1FC;
    [FieldOffset(0x1FF)] public byte Unk_0x1FF;
    [FieldOffset(0x200)] public byte Unk_0x200;
    [FieldOffset(0x2C6)] public byte Unk_0x2C6;
    [FieldOffset(0x3D0)] public GameObject* Actor; // Points to local player
    [FieldOffset(0x3E0)] public byte Unk_0x3E0;
    [FieldOffset(0x3EC)] public float Unk_0x3EC; // This, 0x3F0, 0x418, and 0x419 seem to determine the direction (and where) you turn when turning around or facing left/right
    [FieldOffset(0x3F0)] public float Unk_0x3F0;
    [FieldOffset(0x418)] public byte Unk_0x418;
    [FieldOffset(0x419)] public byte Unk_0x419;
}

[StructLayout(LayoutKind.Explicit)]
public unsafe struct MoveControllerSubMemberForMine
{
    [FieldOffset(0x10)] public Vector3 Direction; // direction?
    [FieldOffset(0x20)] public UnkGameObjectStruct* ActorStruct;
    [FieldOffset(0x28)] public float Unk_0x28;
    [FieldOffset(0x38)] public float Unk_0x38;
    [FieldOffset(0x3C)] public byte Moved;
    [FieldOffset(0x3D)] public byte Rotated; // 1 when the character has rotated
    [FieldOffset(0x3E)] public byte MovementLock; // Pretty much forced auto run when nonzero. Maybe used for scene transitions?
    [FieldOffset(0x3F)] public byte Unk_0x3F;
    [FieldOffset(0x40)] public byte Unk_0x40;
    [FieldOffset(0x44)] public float MoveSpeed;
    [FieldOffset(0x50)] public float* MoveSpeedMaximums;
    [FieldOffset(0x80)] public Vector3 ZoningPosition; // this gets set to your positon when you are in a scene/zone transition
    [FieldOffset(0x90)] public float MoveDir; // Relative direction (in radians) that  you are trying to move. Backwards is -PI, Left is HPI, Right is -HPI
    [FieldOffset(0x94)] public byte Unk_0x94;
    [FieldOffset(0xA0)] public Vector3 MoveForward; // direction output by MovementUpdate
    [FieldOffset(0xB0)] public float Unk_0xB0;
    [FieldOffset(0xB4)] public byte Unk_0xB4; // 
    [FieldOffset(0xF2)] public byte Unk_0xF2;
    [FieldOffset(0xF3)] public byte Unk_0xF3;
    [FieldOffset(0xF4)] public byte Unk_0xF4;
    [FieldOffset(0xF5)] public byte Unk_0xF5;
    [FieldOffset(0xF6)] public byte Unk_0xF6;
    [FieldOffset(0x104)] public byte Unk_0x104; // If you were moving last frame, this value is 0, you moved th is frame, and you moved on only one axis, this can get set to 3
    [FieldOffset(0x110)] public Int32 WishdirChanged; // 1 when your movement direction has changed (0 when autorunning, for example). This is set to 2 if dont_rotate_with_camera is 0, and this is not 1
    [FieldOffset(0x114)] public float Wishdir_Horizontal; // Relative direction on the horizontal axis
    [FieldOffset(0x118)] public float Wishdir_Vertical; // Relative direction on the vertical (forward) axis
    [FieldOffset(0x120)] public byte Unk_0x120;
    [FieldOffset(0x121)] public byte Rotated1; // 1 when the character has rotated, with the exception of standard-mode turn rotation
    [FieldOffset(0x122)] public byte Unk_0x122;
    [FieldOffset(0x123)] public byte Unk_0x123;
    [FieldOffset(0x125)] public byte Unk_0x125; // 1 when walking
    [FieldOffset(0x12A)] public byte Unk_0x12A;
}
