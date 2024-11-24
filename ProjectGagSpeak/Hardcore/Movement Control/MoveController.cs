using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Numerics;
using System.Runtime.InteropServices;

namespace GagSpeak.Hardcore.Movement;
#nullable enable
public class MoveController : IDisposable
{
#pragma warning disable CS8602
    private readonly ILogger<MoveController> _logger;
    private readonly IObjectTable _objectTable;
    public bool AllMovementIsDisabled => ForceDisableMovement > 0;

    #region Pointer Signature Fuckery
    // Ref:
    // https://github.com/PunishXIV/Orbwalker/blob/4d171bc7c79a492bf9159d705dafa7bc97f0c174/Orbwalker/Memory.cs#L74
    // controls the complete blockage of movement from the player (Blocks /follow movement)
    [Signature("F3 0F 10 05 ?? ?? ?? ?? 0F 2E C7", ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
    private readonly nint forceDisableMovementPtr;
    internal unsafe ref int ForceDisableMovement => ref *(int*)(forceDisableMovementPtr + 4);

    // Ref:
    // https://github.com/Drahsid/HybridCamera/blob/2e18760d64be14d2dc16405168d5a7a8f236ff3c/HybridCamera/MovementHook.cs#L216
    // prevents LMB+RMB moving by processing it prior to the games update movement check.
    public unsafe delegate void MoveOnMousePreventor2Delegate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction);
    [Signature("48 8b c4 48 89 70 ?? 48 89 78 ?? 55 41 56 41 57", DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto)]
    public static Hook<MoveOnMousePreventor2Delegate>? MouseAutoMove2Hook { get; set; } = null!;
    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe void MovementUpdate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction)
    {
        if (thisx->Unk_0x3F != 0)
        {
            return;
        }

        MouseAutoMove2Hook.Original(thisx, wishdir_h, wishdir_v, arg4, align_with_camera, direction);
    }


    public unsafe delegate void TestDelegate(UnkTargetFollowStruct* unk1);
    [Signature("48 89 5c 24 ?? 48 89 74 24 ?? 57 48 83 ec ?? 48 8b d9 48 8b fa 0f b6 89 ?? ?? 00 00 be 00 00 00 e0", DetourName = nameof(TestUpdate), Fallibility = Fallibility.Auto)]
    public static Hook<TestDelegate>? UnfollowHook { get; set; }

    [return: MarshalAs(UnmanagedType.U1)]
    public unsafe void TestUpdate(UnkTargetFollowStruct* unk1)
    {
        UnkTargetFollowStruct* temp = unk1;

        var targetFollowVar = unk1;
        //_logger.LogDebug($"PRE:       UnkTargetFollowStruct: {((IntPtr)unk1).ToString("X")}", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"---------------------------------", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"PRE: Unk_0x450.Unk_GameObjectID0: {unk1->Unk_0x450.Unk_GameObjectID0.ToString("X")};", LoggerType.HardcoreMovement);
        try
        {
            //_logger.LogDebug($"PRE      Struct target4 Unk_0x10: {unk1->Unk_0x450.Unk_0x10};", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"PRE      Struct target4 Unk_0x54: {unk1->Unk_0x450.Unk_0x54};", LoggerType.HardcoreMovement);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error converting Unk_0x10 to string: {ex}", LoggerType.HardcoreMovement);
        }
        //_logger.LogDebug($"PRE:             FollowingTarget: {unk1->FollowingTarget.ToString("X")}", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"PRE:                 Follow Type: {unk1->FollowType.ToString("X")}", LoggerType.HardcoreMovement);

        foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject obj in _objectTable)
        {
            if (obj.GameObjectId == unk1->GameObjectIDToFollow)
            {
                //_logger.LogDebug($"Game Object To Follow: {unk1->GameObjectIDToFollow.ToString("X")}: {obj.Name.TextValue}", LoggerType.HardcoreMovement);
                break;
            }
        }
        // if this condition it true, it means that the function is attempting to call a cancelation 
        if (unk1->Unk_0x450.Unk_0x54 == 256)
        {
            //_logger.LogDebug($"Unfollow Hook was valid, performing Early escaping to prevent canceling follow!", LoggerType.HardcoreMovement);
            return; // do an early return to prevent processing
        }
        else
        {
            //_logger.LogDebug($"Unk_0x450.Unk_0x54 was {unk1->Unk_0x450.Unk_0x54}. Performing early return of original.", LoggerType.HardcoreMovement);
            // output the original
            UnfollowHook.Original(unk1);
        }

        try
        {
            //_logger.LogDebug($"POST       UnkTargetFollowStruct: {((IntPtr)unk1).ToString("X")}", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"---------------------------------", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"POST Unk_0x450.Unk_GameObjectID0: {unk1->Unk_0x450.Unk_GameObjectID0.ToString("X")};", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"POST     Struct target4 Unk_0x54: {unk1->Unk_0x450.Unk_0x54};", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"POST             FollowingTarget: {unk1->FollowingTarget.ToString("X")}", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"POST                 Follow Type: {unk1->FollowType.ToString("X")}", LoggerType.HardcoreMovement);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error {ex}", LoggerType.HardcoreMovement);
        }

        foreach (Dalamud.Game.ClientState.Objects.Types.IGameObject obj in _objectTable)
        {
            if (obj.GameObjectId == unk1->GameObjectIDToFollow)
            {
                _logger.LogDebug($"POST ObjectIDtoFollow: {unk1->GameObjectIDToFollow.ToString("X")}: {obj.Name.TextValue}", LoggerType.HardcoreMovement);
                break;
            }
        }
    }

    public unsafe MoveController(ILogger<MoveController> logger,
        IGameInteropProvider interopProvider, IObjectTable objectTable)
    {
        _logger = logger;
        _objectTable = objectTable;
        // initialize from attributes
        interopProvider.InitializeFromAttributes(this);
    }
    #endregion Pointer Signature Fuckery

#pragma warning restore CS8602
    public void EnableMovementLock()
    {
        // If all Movement is already disabled by this plugin or any other, dont interact with it.
        if (AllMovementIsDisabled)
            return;

        // If it is at 0, we need to change it to 1.
        _logger.LogTrace("Disabling All Movement due to Hardcore Active State condition starting", LoggerType.HardcoreMovement);
        ForceDisableMovement = 1;
    }

    public void DisableMovementLock()
    {
        // If the movement is already re-enabled by this plugin or any other, dont interact with it.
        if (!AllMovementIsDisabled)
            return;

        // Otherwise, re-enable movement.
        _logger.LogTrace("Enabling All Movement due to Hardcore Active State condition ending", LoggerType.HardcoreMovement);
        ForceDisableMovement = 0;
    }

    public void EnableUnfollowHook()
    {
        // If our unfollow hook is ready but has not been enabled, we should enable it.
        if (UnfollowHook is not null && !UnfollowHook.IsEnabled)
        {
            UnfollowHook.Enable();
            _logger.LogTrace($"UnfollowHook is enabled due to Hardcore Active State", LoggerType.HardcoreMovement);
        }
    }

    public void DisableUnfollowHook()
    {
        // If our unfollow hook is ready and enabled, we should disable it.
        if (UnfollowHook is not null && UnfollowHook.IsEnabled)
        {
            UnfollowHook.Disable();
            _logger.LogTrace($"UnfollowHook is disabled due to Hardcore Active State ending", LoggerType.HardcoreMovement);
        }
    }

    public void EnableMouseAutoMoveHook()
    {
        // If our unfollow hook is ready but has not been enabled, we should enable it.
        if (MouseAutoMove2Hook is not null && !MouseAutoMove2Hook.IsEnabled)
        {
            MouseAutoMove2Hook.Enable();
            _logger.LogTrace($"UnfollowHook is enabled due to Hardcore Active State", LoggerType.HardcoreMovement);
        }
    }

    public void DisableMouseAutoMoveHook()
    {
        // If our unfollow hook is ready and enabled, we should disable it.
        if (MouseAutoMove2Hook is not null && MouseAutoMove2Hook.IsEnabled)
        {
            MouseAutoMove2Hook.Disable();
            _logger.LogTrace($"UnfollowHook is disabled due to Hardcore Active State ending", LoggerType.HardcoreMovement);
        }
    }


    // the disposer
    public void Dispose()
    {
        _logger.LogDebug($"Disposing of MoveController", LoggerType.HardcoreMovement);
        // Free the player and disable unfollow hooks
        DisableMovementLock();
        DisableUnfollowHook();
        DisableMouseAutoMoveHook();

        // dispose of the hooks
        UnfollowHook?.Dispose();
        MouseAutoMove2Hook?.Dispose();
        // make sure they are disposed of
        UnfollowHook = null;
        MouseAutoMove2Hook = null;
    }

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
}
