using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class StaticDetours
{

    /// <summary>
    ///     Intercepts the application of Glamour Plates. <para />
    ///     Do not attempt to invoke upon this action if the glamour plate is not open or not valid for use. <para />
    ///     This purpose is primarily to act as a listener for when a glamour plate is applied.
    /// </summary>
    private unsafe delegate void ApplyGlamourPlateDelegate(MirageManager* glamPlatePtr, uint glamPlateIdx);
    [Signature(Signatures.ApplyGlamourPlate, DetourName = nameof(ApplyGlamourPlateDetour), Fallibility = Fallibility.Auto)]
    private Hook<ApplyGlamourPlateDelegate> ApplyGlamourPlateHook { get; set; } = null!;
    private unsafe void ApplyGlamourPlateDetour(MirageManager* glamPlatePtr, uint glamPlateIdx)
    {
        // always return first so it can be processed.
        ApplyGlamourPlateHook.Original(glamPlatePtr, glamPlateIdx);
        // Svc.Logger.Warning($"Someone Applied A Glamour Plate!");
        _glamourHandler.OnAppliedGlamourPlate(glamPlateIdx);
    }


    /// <summary>
    ///     While EquipGearset allows you to attempt equipping a gearset, EquipGearsetInternal
    ///     Informs us whenever the client Processes a valid EquipGearset Request, prior to it applying.
    /// </summary>
    /// <remarks> This is called BEFORE anything is applied to the character. </remarks>
    internal Hook<RaptureGearsetModule.Delegates.EquipGearsetInternal> GearsetInternalHook;
    private unsafe int GearsetInternalDetour(RaptureGearsetModule* module, int gearsetId, byte glamourPlateId)
    {
        var priorGearsetId = module->CurrentGearsetIndex;
        //Svc.Logger.Warning($"GearsetInternalDetour called with GearsetId: {gearsetId}, GlamourPlateId: {glamourPlateId}, PriorGearsetId: {priorGearsetId}");
        var ret = GearsetInternalHook.Original(module, gearsetId, glamourPlateId);
        // fire if different.
        if (priorGearsetId != gearsetId)
            _glamourHandler.OnEquipGearsetInternal(gearsetId, glamourPlateId);

        return ret;
    }
}
