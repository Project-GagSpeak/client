using System.Runtime.InteropServices;

namespace GagSpeak.GameInternals.Structs;

// this and it's siblings have member functions in vtable
[StructLayout(LayoutKind.Explicit, Size = 0x56)]
public unsafe struct UnkTargetFollowStruct_Unk0x450
{
    [FieldOffset(0x00)] public IntPtr vtable;
    [FieldOffset(0x10)] public float Unk_0x10;
    [FieldOffset(0x14)] public float Unk_0x14;
    [FieldOffset(0x18)] public float Unk_0x18;
    [FieldOffset(0x20)] public float Unk_0x20;
    [FieldOffset(0x28)] public float Unk_0x28;
    [FieldOffset(0x30)] public Vector3 PlayerPosition;
    [FieldOffset(0x40)] public uint Unk_GameObjectID0; // seemingly always E0000000 when relating to targets
    [FieldOffset(0x48)] public uint Unk_GameObjectID1; // seemingly always E0000000 when relating to targets
    [FieldOffset(0x50)] public int Unk_0x50;
    [FieldOffset(0x54)] public short Unk_0x54;
}

// possibly FFXIVClientStructs.FFXIV.Client.Game.Control.InputManager, ctor is ran after CameraManager and TargetSystem
[StructLayout(LayoutKind.Explicit)]
public unsafe struct UnkTargetFollowStruct
{
    [FieldOffset(0x10)] public IntPtr Unk_0x10;
    [FieldOffset(0x30)] public ulong Unk_0x30;
    [FieldOffset(0x4C)] public byte Unk_0x4C;
    [FieldOffset(0x4D)] public byte Unk_0x4D;
    [FieldOffset(0x50)] public byte Unk_0x50;
    [FieldOffset(0x150)] public ulong Unk_0x150;
    [FieldOffset(0x180)] public ulong Unk_0x180;
    [FieldOffset(0x188)] public ulong Unk_0x188;
    [FieldOffset(0x1B6)] public byte Unk_0x1B6;
    [FieldOffset(0x1C0)] public byte Unk_0x1C0; // used like a bitfield
    [FieldOffset(0x1EC)] public uint Unk_0x1EC;

    // think some of these floats are arrays of floats
    [FieldOffset(0x2A0)] public float Unk_0x2A0;
    [FieldOffset(0x2B0)] public float Unk_0x2B0;
    [FieldOffset(0x2C0)] public float Unk_0x2C0;
    [FieldOffset(0x2D0)] public float Unk_0x2D0;
    [FieldOffset(0x2E0)] public float Unk_0x2E0;
    [FieldOffset(0x2E4)] public float Unk_0x2E4;
    [FieldOffset(0x2F4)] public float Unk_0x2F4;
    [FieldOffset(0x304)] public float Unk_0x304;
    [FieldOffset(0x314)] public float Unk_0x314;
    [FieldOffset(0x324)] public float Unk_0x324;
    [FieldOffset(0x328)] public float Unk_0x328;
    [FieldOffset(0x338)] public float Unk_0x338;
    [FieldOffset(0x348)] public float Unk_0x348;
    [FieldOffset(0x358)] public float Unk_0x358;
    [FieldOffset(0x368)] public float Unk_0x368;

    [FieldOffset(0x3A0)] public IntPtr Unk_0x3A0;
    [FieldOffset(0x3F0)] public ulong Unk_0x3F0;
    [FieldOffset(0x410)] public uint Unk_0x410;
    [FieldOffset(0x414)] public uint Unk_0x414;
    [FieldOffset(0x418)] public uint Unk_0x418;
    [FieldOffset(0x420)] public uint Unk_0x420;
    [FieldOffset(0x424)] public uint Unk_0x424;
    [FieldOffset(0x428)] public uint Unk_0x428;
    [FieldOffset(0x430)] public uint GameObjectIDToFollow;
    [FieldOffset(0x438)] public uint Unk_0x438;

    // possible union below ...

    // start of some substruct (used for FollowType == 3?)
    [FieldOffset(0x440)] public byte Unk_0x440;
    [FieldOffset(0x448)] public byte Unk_0x448;
    [FieldOffset(0x449)] public byte Unk_0x449;
    // end of substruct

    // start of UnkTargetFollowStruct_Unk0x450 (used for FollowType == 4?)
    [FieldOffset(0x450)] public UnkTargetFollowStruct_Unk0x450 Unk_0x450;
    [FieldOffset(0x4A0)] public int Unk_0x4A0; // intersects UnkTargetFollowStruct_Unk0x450->0x50
    [FieldOffset(0x4A4)] public byte Unk_0x4A4; // intersects UnkTargetFollowStruct_Unk0x450->0x54
    [FieldOffset(0x4A5)] public byte FollowingTarget; // nonzero when following target (intersects UnkTargetFollowStruct_Unk0x450->0x54)
    // end of substruct

    [FieldOffset(0x4B0)] public ulong Unk_0x4B0; // start of some substruct (dunno where this one ends) (used for FollowType == 2?)
    [FieldOffset(0x4B8)] public uint Unk_GameObjectID1;
    [FieldOffset(0x4C0)] public byte Unk_0x4C0; // start of some substruct (dunno where this one ends) (used for FollowType == 1?)
    [FieldOffset(0x4C8)] public byte Unk_0x4C8;

    // possible union probably ends around here

    [FieldOffset(0x4D0)] public IntPtr Unk_0x4D0; // some sort of array (indexed by Unk_0x558?) unsure how large

    [FieldOffset(0x548)] public ulong Unk_0x548; // param_1->Unk_0x548 = (lpPerformanceCount->QuadPart * 1000) / lpFrequency->QuadPart;
    [FieldOffset(0x550)] public float Unk_0x550;
    [FieldOffset(0x554)] public int Unk_0x554; // seems to be some sort of counter or timer
    [FieldOffset(0x558)] public byte Unk_0x558; // used as an index (?)
    [FieldOffset(0x561)] public byte FollowType; // 2 faces the player away, 3 runs away, 4 runs towards, 0 is none
                                                    // unknown but known possible values: 1, 5
    [FieldOffset(0x55B)] public byte Unk_0x55B;
    [FieldOffset(0x55C)] public byte Unk_0x55C;
    [FieldOffset(0x55D)] public byte Unk_0x55D;
    [FieldOffset(0x55E)] public byte Unk_0x55E;
    [FieldOffset(0x55F)] public byte Unk_0x55F;
    [FieldOffset(0x560)] public byte Unk_0x560;
}
