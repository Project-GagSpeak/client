using CkCommons;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using GagSpeak.GameInternals.Structs;
using System.Runtime.InteropServices;
 
namespace GagSpeak.GameInternals.Detours;
#nullable enable
#pragma warning disable CS0649 // Missing XML comment for publicly visible type or member
public partial class MovementDetours : IDisposable
{
    public unsafe delegate void AutoMoveUpdateDelegate(IntPtr unk1, IntPtr unk2);
    [Signature(Signatures.UnkAutoMoveUpdate, DetourName = nameof(AutoMoveUpdateDetour), Fallibility = Fallibility.Auto)]
    private Hook<AutoMoveUpdateDelegate> AutoMoveUpdateHook = null!;
    public unsafe void AutoMoveUpdateDetour(IntPtr unk1, IntPtr unk2)
    {
        // Svc.Logger.Warning($"Detouring [A1: ({unk1.ToString("X")}) && A2: ({unk2.ToString("X")})");
        if (*(byte*)(unk2 + 8) == 3)
        {
            Svc.Logger.Information("Attempted to request AutoMove!");
            return;
        }
        AutoMoveUpdateHook?.Original(unk1, unk2);
    }

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
    [Signature(Signatures.MouseMoveBlock, DetourName = nameof(MovementDirectionUpdate), Fallibility = Fallibility.Auto)]
    private Hook<MovementDirectionUpdateDelegate> MoveUpdateHook = null!;
    [return: MarshalAs(UnmanagedType.U1)]
    private unsafe void MovementDirectionUpdate(MoveControllerSubMemberForMine* thisx, float* wishdir_h, float* wishdir_v, float* rotatedir, byte* align_with_camera, byte* autorun, byte dont_rotate_with_camera)
    {
        MoveUpdateHook?.Original(thisx, wishdir_h, wishdir_v, rotatedir, align_with_camera, autorun, dont_rotate_with_camera);

        if (thisx->Unk_0x3F != 0)
        {
            thisx->Unk_0x3F = 0;
            thisx->WishdirChanged = 0;
            *wishdir_v = 0;
            return; // prevent original from executing to stop forcefollow from occuring.
        }
    }

    /// <summary>
    ///     Prevents the player from unfollowing a target, which is used to prevent the player from canceling follow.
    /// </summary>
    /// <remarks> This fires the entire duration you are following someone, so it is best not to log everything. </remarks>
    public unsafe delegate void UnfollowTargetDelegate(UnkTargetFollowStruct* unk1, IntPtr unk2);
    [Signature(Signatures.UnfollowTarget, DetourName = nameof(UnfollowTargetDetour), Fallibility = Fallibility.Auto)]
    private Hook<UnfollowTargetDelegate> UnfollowHook = null!;
    [return: MarshalAs(UnmanagedType.U1)]
    private unsafe void UnfollowTargetDetour(UnkTargetFollowStruct* targetStruct, IntPtr unk2)
    {
        if (targetStruct->FollowType4Data.FollowingTarget == 0x100) // 256, it's max value.
            return;
        // ret original otherwise.
        UnfollowHook?.Original(targetStruct, unk2);
    }
}
