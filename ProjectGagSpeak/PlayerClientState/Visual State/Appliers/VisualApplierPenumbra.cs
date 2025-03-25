using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerState.Models;
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

    public ModSettingOptions GetAllModOptions(ModAssociation mod)
        => _penumbra.GetAllOptionsForMod(mod.ModInfo);

    public bool SetOrUpdateTempMod(Mod mod, ModSettings presetSettings, bool redraw = false)
    {
        return _penumbra.SetOrUpdateTemporaryMod(mod, presetSettings) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(ModAssociation mod, bool redraw = false)
    {
        return _penumbra.RemoveTemporaryMod(mod.ModInfo) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(IEnumerable<ModAssociation> mods)
    {
        foreach (var mod in mods)
            if (_penumbra.RemoveTemporaryMod(mod.ModInfo) != PenumbraApiEc.Success)
                return false;

        return true;
    }

    public bool RemoveAllTempMods(bool redraw = false)
    {
        return _penumbra.ClearAllTemporaryMods() == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }
}
