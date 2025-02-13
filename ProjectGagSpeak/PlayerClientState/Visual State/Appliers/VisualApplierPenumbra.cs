using GagSpeak.Interop.Ipc;
using GagSpeak.Restrictions;
using Penumbra.Api.Enums;

namespace GagSpeak.PlayerState.Visual;

/// <summary> Handles the application of temporary mods from active items via all sources. </summary>
public class VisualApplierPenumbra
{
    private readonly ILogger<VisualApplierPenumbra> _logger;
    private readonly IpcCallerPenumbra _penumbra;
    public VisualApplierPenumbra(ILogger<VisualApplierPenumbra> logger, IpcCallerPenumbra penumbra)
    {
        _logger = logger;
        _penumbra = penumbra;
    }

    /// <summary> Gets all of the clients mods from penumbra. </summary>
    /// <remarks> The settings returned with this only provide the options selected, not all options. </remarks>
    public IEnumerable<ModAssociation> GetClientMods()
    {
        var res = _penumbra.GetModInfos();
        return res.Select(x => new ModAssociation(x.Mod, x.Settings));
    }

    public ModSettingOptions GetAllModOptions(ModAssociation mod)
        => _penumbra.GetAllOptionsForMod(mod.ModInfo);

    public bool SetOrUpdateTempMod(ModAssociation mod, bool redraw = false)
    {
        return _penumbra.SetOrUpdateTemporaryMod(mod) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(ModAssociation mod, bool redraw = false)
    {
        return _penumbra.RemoveTemporaryMod(mod) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveAllTempMods(bool redraw = false)
    {
        return _penumbra.ClearAllTemporaryMods() == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }
}
