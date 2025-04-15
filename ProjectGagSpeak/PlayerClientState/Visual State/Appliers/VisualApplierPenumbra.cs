using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Storage;
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

    public bool SetOrUpdateTempMod(ModSettingsPreset modAttachment, bool redraw = false)
    {
        return _penumbra.SetOrUpdateTemporaryMod(modAttachment) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(ModSettingsPreset modAttachment, bool redraw = false)
    {
        return _penumbra.RemoveTemporaryMod(modAttachment) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(IEnumerable<ModSettingsPreset> modAttachments)
    {
        foreach (var modAttachment in modAttachments)
            if (_penumbra.RemoveTemporaryMod(modAttachment) != PenumbraApiEc.Success)
                return false;

        return true;
    }

    public bool RemoveAllTempMods(bool redraw = false)
    {
        return _penumbra.ClearAllTemporaryMods() == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }
}
