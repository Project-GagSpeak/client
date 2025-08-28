using System.Runtime.InteropServices;

namespace GagSpeak.GameInternals.Structs;

// https://github.com/Drahsid/HybridCamera/blob/master/HybridCamera/MovementHook.cs
// For upkeep reference on all below structs.

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public unsafe struct UnkTargetFollowStruct_Unk0x2A0
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 1
    [FieldOffset(0x10)] public float Unk_0x10;
}

[StructLayout(LayoutKind.Explicit, Size = 0x18)]
public unsafe struct UnkTargetFollowStruct_Unk0x118
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 1
}

[StructLayout(LayoutKind.Explicit, Size = 0x48)]
public unsafe struct UnkTargetFollowStruct_Unk0xC8
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 1
    [FieldOffset(0x08)] public IntPtr vtable2; // 0 = ctor, length 1
}

/// <summary>
///     The main Follow object info used with /follow.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 0x60)]
public unsafe struct UnkFollowType4Struct
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 3
    [FieldOffset(0x10)] public float Unk_0x10;
    [FieldOffset(0x14)] public float Unk_0x14;
    [FieldOffset(0x18)] public float Unk_0x18;
    [FieldOffset(0x20)] public float Unk_0x20;
    [FieldOffset(0x28)] public float Unk_0x28;
    [FieldOffset(0x30)] public Vector3 PlayerPosition;
    [FieldOffset(0x48)] public uint GameObjectID0;
    [FieldOffset(0x4C)] public uint GameObjectID1;
    [FieldOffset(0x54)] public short FollowingTarget; // nonzero when following target
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct UnkFollowType3Struct
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 3
    [FieldOffset(0x08)] public byte Unk_0x8;
    [FieldOffset(0x09)] public byte Unk_0x9;
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct UnkFollowType2Struct
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 3
    [FieldOffset(0x08)] public uint GameObjectID;
}

[StructLayout(LayoutKind.Explicit, Size = 0x10)]
public unsafe struct UnkFollowType1Struct
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 3
    [FieldOffset(0x08)] public byte Unk_0x8;
}


// Initialized after Client::Game::Control::CameraManager.ctor and Client::Game::Control::TargetSystem.Initialize
[StructLayout(LayoutKind.Explicit, Size = 0x590)]
public unsafe struct UnkTargetFollowStruct
{
    [FieldOffset(0x00)] public IntPtr vtable; // 0 = ctor, length 2
    [FieldOffset(0x10)] public IntPtr vtbl_Client__Game__Control__MoveControl__MoveControllerSubMemberForMine;
    [FieldOffset(0x30)] public ulong Unk_0x30;
    [FieldOffset(0x4C)] public byte Unk_0x4C;
    [FieldOffset(0x4D)] public byte Unk_0x4D;
    [FieldOffset(0x50)] public byte Unk_0x50;
    [FieldOffset(0x0C8)] public UnkTargetFollowStruct_Unk0xC8 Unk_0xC8;
    [FieldOffset(0x118)] public UnkTargetFollowStruct_Unk0x118 Unk_0x118;
    [FieldOffset(0x150)] public ulong Unk_0x150; // Client::Graphics::Vfx::VfxDataListenner (sizeof = 0xB0)

    // think some of these floats are arrays of 4
    [FieldOffset(0x2A0)] public UnkTargetFollowStruct_Unk0x2A0 Unk_0x2A0;
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

    // 6.5 -> 7.3 added 0x20 bytes of floats?
    [FieldOffset(0x36C)] public float Unk_0x36C;
    [FieldOffset(0x370)] public float Unk_0x370;
    [FieldOffset(0x374)] public float Unk_0x374;
    [FieldOffset(0x378)] public float Unk_0x378;
    [FieldOffset(0x37C)] public float Unk_0x37C;
    [FieldOffset(0x380)] public float Unk_0x380;
    [FieldOffset(0x384)] public float Unk_0x384;
    [FieldOffset(0x388)] public float Unk_0x388;

    [FieldOffset(0x3C0)] public IntPtr Unk_0x3C0;
    [FieldOffset(0x410)] public ulong Unk_0x410;
    [FieldOffset(0x430)] public uint Unk_0x430;
    [FieldOffset(0x434)] public uint Unk_0x434;
    [FieldOffset(0x438)] public uint Unk_0x438;
    [FieldOffset(0x440)] public uint Unk_0x440;
    [FieldOffset(0x444)] public uint Unk_0x444;
    [FieldOffset(0x448)] public uint Unk_0x448;
    [FieldOffset(0x450)] public uint GameObjectIDToFollow;
    [FieldOffset(0x458)] public uint Unk_0x458;

    [FieldOffset(0x460)] public UnkFollowType3Struct FollowType3Data;
    [FieldOffset(0x470)] public UnkFollowType4Struct FollowType4Data;
    [FieldOffset(0x4D0)] public UnkFollowType2Struct FollowType2Data;
    [FieldOffset(0x4E0)] public UnkFollowType1Struct FollowType1Data;

    [FieldOffset(0x4F0)] public IntPtr Unk_0x4F0; // some sort of array (indexed by Unk_0x578?) unsure how large

    [FieldOffset(0x568)] public ulong Unk_0x568; // param_1->Unk_0x568 = (lpPerformanceCount->QuadPart * 1000) / lpFrequency->QuadPart;
    [FieldOffset(0x570)] public float Unk_0x570;
    [FieldOffset(0x574)] public int Unk_0x574; // seems to be some sort of counter or timer
    [FieldOffset(0x578)] public byte Unk_0x578; // used as an index (?)
    [FieldOffset(0x579)] public byte FollowType; // 2 faces the player away, 3 runs away, 4 runs towards, 0 is none
                                                 // unknown but known possible values: 1, 5
    [FieldOffset(0x57B)] public byte Unk_0x57B;
    [FieldOffset(0x57C)] public byte Unk_0x57C;
    [FieldOffset(0x57D)] public byte Unk_0x57D;
    [FieldOffset(0x57E)] public byte Unk_0x57E;
    [FieldOffset(0x57F)] public byte Unk_0x57F;
    [FieldOffset(0x580)] public byte Unk_0x580;
}
