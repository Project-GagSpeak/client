using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using GagSpeak.GameInternals.Structs;
using System.Runtime.InteropServices;
 
namespace GagSpeak.GameInternals.Detours;
#nullable enable
#pragma warning disable CS0649 // Missing XML comment for publicly visible type or member
public partial class MovementDetours : IDisposable
{
    /// <summary>
    ///     Controls the complete blockage of movement from the player (Blocks /follow movement)
    /// </summary>
    [Signature(Signatures.ForceDisableMovement, ScanType = ScanType.StaticAddress, Fallibility = Fallibility.Infallible)]
    private readonly nint forceDisableMovementPtr;
    internal unsafe ref int ForceDisableMovement => ref *(int*)(forceDisableMovementPtr + 4);

    /// <summary>
    ///     prevents LMB+RMB moving by processing it prior to the games update movement check.
    ///     If this fails, check our HybridCamera's new movement detection method.
    /// </summary>
    public unsafe delegate void MoveOnMousePreventor2Delegate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction);
    [Signature(Signatures.MouseAutoMove2, DetourName = nameof(MovementUpdate), Fallibility = Fallibility.Auto)]
    public static Hook<MoveOnMousePreventor2Delegate>? MouseAutoMove2Hook { get; set; } = null!;

    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe void MovementUpdate(MoveControllerSubMemberForMine* thisx, float wishdir_h, float wishdir_v, char arg4, byte align_with_camera, Vector3* direction)
    {
        if (thisx->Unk_0x3F != 0)
            return;

        MouseAutoMove2Hook?.Original(thisx, wishdir_h, wishdir_v, arg4, align_with_camera, direction);
    }

    /// <summary>
    ///     Prevents the player from unfollowing a target, which is used to prevent the player from canceling follow.
    /// </summary>
    /// <remarks> This fires the entire duration you are following someone, so it is best not to log everything. </remarks>
    public unsafe delegate void UnfollowTargetDelegate(UnkTargetFollowStruct* unk1);
    [Signature(Signatures.UnfollowTarget, DetourName = nameof(UnfollowTargetDetour), Fallibility = Fallibility.Auto)]
    public static Hook<UnfollowTargetDelegate>? UnfollowHook { get; set; }

    [return: MarshalAs(UnmanagedType.U1)]
    public unsafe void UnfollowTargetDetour(UnkTargetFollowStruct* unk1)
    {
        var temp = unk1;
        var targetFollowVar = unk1;
        //_logger.LogDebug($"PRE:       UnkTargetFollowStruct: {((IntPtr)unk1).ToString("X")}", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"---------------------------------", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"PRE: Unk_0x450.Unk_GameObjectID0: {unk1->Unk_0x450.Unk_GameObjectID0.ToString("X")};", LoggerType.HardcoreMovement);
        try
        {
            //_logger.LogDebug($"PRE      Struct target4 Unk_0x10: {unk1->Unk_0x450.Unk_0x10};", LoggerType.HardcoreMovement);
            //_logger.LogDebug($"PRE      Struct target4 Unk_0x54: {unk1->Unk_0x450.Unk_0x54};", LoggerType.HardcoreMovement);
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error converting Unk_0x10 to string: {ex}", LoggerType.HardcoreMovement);
        }
        //_logger.LogDebug($"PRE:             FollowingTarget: {unk1->FollowingTarget.ToString("X")}", LoggerType.HardcoreMovement);
        //_logger.LogDebug($"PRE:                 Follow Type: {unk1->FollowType.ToString("X")}", LoggerType.HardcoreMovement);

        //foreach (var obj in _objectTable)
        //{
        //    if (obj.GameObjectId == unk1->GameObjectIDToFollow)
        //    {
        //        _logger.LogDebug($"Game Object To Follow: {unk1->GameObjectIDToFollow.ToString("X")}: {obj.Name.TextValue}", LoggerType.HardcoreMovement);
        //        break;
        //    }
        //}
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
            UnfollowHook?.Original(unk1);
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
        catch (Bagagwa ex)
        {
            _logger.LogError($"Error {ex}", LoggerType.HardcoreMovement);
        }

        //foreach (var obj in _objectTable)
        //{
        //    if (obj.GameObjectId == unk1->GameObjectIDToFollow)
        //    {
        //        _logger.LogDebug($"POST ObjectIDtoFollow: {unk1->GameObjectIDToFollow.ToString("X")}: {obj.Name.TextValue}", LoggerType.HardcoreMovement);
        //        break;
        //    }
        //}
    }
}
