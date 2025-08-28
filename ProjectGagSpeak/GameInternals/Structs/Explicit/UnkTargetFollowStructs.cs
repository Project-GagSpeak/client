using System.Runtime.InteropServices;

namespace GagSpeak.GameInternals.Structs;

// this and it's siblings have member functions in vtable
[StructLayout(LayoutKind.Explicit, Size = 0x56)]
public unsafe struct UnkTargetFollowStruct_Unk0x470
{
    [FieldOffset(0x00)] public IntPtr vtable;
    [FieldOffset(0x30)] public Vector3 PlayerPosition;
    [FieldOffset(0x40)] public uint Unk_GameObjectID0; // seemingly always E0000000 when relating to targets
    [FieldOffset(0x48)] public uint Unk_GameObjectID1; // seemingly always E0000000 when relating to targets
    [FieldOffset(0x50)] public int Unk_0x50;
    [FieldOffset(0x54)] public short Unk_0x54; // becomes 256 when requesting to unfollow
}

// possibly FFXIVClientStructs.FFXIV.Client.Game.Control.InputManager, ctor is ran after CameraManager and TargetSystem
[StructLayout(LayoutKind.Explicit)]
public unsafe struct UnkTargetFollowStruct
{
    [FieldOffset(0x30)] public IntPtr Unk_0x10;
    [FieldOffset(0x450)] public uint GameObjectIDToFollow;
    // start of UnkTargetFollowStruct_Unk0x470 (used for FollowType == 4?)
    [FieldOffset(0x470)] public UnkTargetFollowStruct_Unk0x470 Unk_0x470;
    [FieldOffset(0x4C0)] public int Unk_0x4A0; // intersects UnkTargetFollowStruct_Unk0x470->0x50
    [FieldOffset(0x4C4)] public byte Unk_0x4A4; // intersects UnkTargetFollowStruct_Unk0x470->0x54
    [FieldOffset(0x4C5)] public byte FollowingTarget; // nonzero when following target (intersects UnkTargetFollowStruct_Unk0x470->0x54)
    // end of substruct

    [FieldOffset(0x4D0)] public ulong Unk_0x4B0; // start of some substruct (dunno where this one ends) (used for FollowType == 2?)
    [FieldOffset(0x4D8)] public uint Unk_GameObjectID1;
    [FieldOffset(0x4E0)] public byte Unk_0x4C0; // start of some substruct (dunno where this one ends) (used for FollowType == 1?)

    [FieldOffset(0x574)] public int Unk_0x554; // seems to be some sort of counter or timer
    [FieldOffset(0x581)] public byte FollowType; // 2 faces the player away, 3 runs away, 4 runs towards, 0 is none
                                                    // unknown but known possible values: 1, 5
}
