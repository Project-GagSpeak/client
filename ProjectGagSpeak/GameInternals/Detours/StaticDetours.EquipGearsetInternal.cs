using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace GagSpeak.GameInternals.Detours;
public unsafe partial class StaticDetours
{
    /// <summary>
    ///     While EquipGearset allows you to attempt equipping a gearset, EquipGearsetInternal
    ///     Informs us whenever the client Processes a valid EquipGearset Request, prior to it applying.
    /// </summary>
    /// <remarks> This is called BEFORE anything is applied to the character. </remarks>
    internal Hook<RaptureGearsetModule.Delegates.EquipGearsetInternal> GearsetInternalHook;


    /// <summary>
    ///     While EquipGearset allows you to attempt equipping a gearset, EquipGearsetInternal
    ///     Informs us whenever the client Processes a valid EquipGearset Request, prior to it applying.
    /// </summary>
    /// <remarks> This is called BEFORE anything is applied to the character. </remarks>
    private unsafe int GearsetInternalDetour(RaptureGearsetModule* module, int gearsetId, byte glamourPlateId)
    {
        var priorGearsetId = module->CurrentGearsetIndex;
        // process the original now.
        var ret = GearsetInternalHook.Original(module, gearsetId, glamourPlateId);

        // if it is different, we need to inform the handler.
        if(gearsetId != priorGearsetId)
            _glamourHandler.OnEquipGearsetInternal(gearsetId, glamourPlateId);

        return ret;
    }

}
