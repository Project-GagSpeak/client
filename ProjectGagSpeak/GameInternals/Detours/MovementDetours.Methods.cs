using CkCommons;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
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
    public unsafe delegate void MovementDirectionUpdateDelegate(MoveControllerSubMemberForMine* thisx, float* wishdir_h, float* wishdir_v, float* rotatedir, byte* align_with_camera, byte* autorun, byte dont_rotate_with_camera);
    [Signature(Signatures.MouseAutoMove2, DetourName = nameof(MovementDirectionUpdate), Fallibility = Fallibility.Auto)]
    public static Hook<MovementDirectionUpdateDelegate>? MoveUpdateHook { get; set; } = null!;

    [return: MarshalAs(UnmanagedType.U1)]
    public static unsafe void MovementDirectionUpdate(MoveControllerSubMemberForMine* thisx, float* wishdir_h, float* wishdir_v, float* rotatedir, byte* align_with_camera, byte* autorun, byte dont_rotate_with_camera)
    {
        if (thisx->Unk_0x3F != 0)
        {
            thisx->Unk_0x3F = 0;
            thisx->WishdirChanged = 0;
            *wishdir_v = 0;
            return; // prevent original from executing to stop forcefollow from occuring.
        }

        MoveUpdateHook?.Original(thisx, wishdir_h, wishdir_v, rotatedir, align_with_camera, autorun, dont_rotate_with_camera);
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
        // if this condition it true, it means that the function is attempting to call a cancelation
        if (unk1->Unk_0x450.Unk_0x54 == 256)
        {
            _logger.LogDebug($"Unfollow Hook was valid, performing Early escaping to prevent canceling follow!", LoggerType.HardcoreMovement);
            return; // do an early return to prevent processing
        }
        else
        {
            _logger.LogDebug($"Unk_0x450.Unk_0x54 was {unk1->Unk_0x450.Unk_0x54}. Performing early return of original.", LoggerType.HardcoreMovement);
            // output the original
            UnfollowHook?.Original(unk1);
        }
    }
}
