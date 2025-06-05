using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Storage;
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
        _logger.LogDebug($"Setting or updating ModPreset {modAttachment.Label} for mod {modAttachment.Container.ModName}");
        return _penumbra.SetOrUpdateTemporaryMod(modAttachment) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool SetOrUpdateTempMod(IEnumerable<ModSettingsPreset> modAttachments)
    {
        _logger.LogDebug($"Setting or updating {modAttachments.Count()} temporary mods.");
        foreach (var modAttachment in modAttachments)
        {
            _logger.LogDebug($"Setting or updating ModPreset {modAttachment.Label} for mod {modAttachment.Container.ModName}");
            if (_penumbra.SetOrUpdateTemporaryMod(modAttachment) != PenumbraApiEc.Success)
                return false;
        }
        return true;
    }

    public bool RemoveTempMod(ModSettingsPreset modAttachment, bool redraw = false)
    {
        _logger.LogDebug($"Removing ModPreset {modAttachment.Label} for mod {modAttachment.Container.ModName}");
        return _penumbra.RemoveTemporaryMod(modAttachment) == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }

    public bool RemoveTempMod(IEnumerable<ModSettingsPreset> modAttachments)
    {
        _logger.LogDebug($"Removing {modAttachments.Count()} temporary mods.");
        foreach (var modAttachment in modAttachments)
        {
            _logger.LogDebug($"Removing ModPreset {modAttachment.Label} for mod {modAttachment.Container.ModName}");
            if (_penumbra.RemoveTemporaryMod(modAttachment) != PenumbraApiEc.Success)
                return false;
        }
        return true;
    }

    public bool RemoveAllTempMods(bool redraw = false)
    {
        _logger.LogDebug("Removing all temporary mods.");
        return _penumbra.ClearAllTemporaryMods() == PenumbraApiEc.Success;
        // maybe something with redraw later but for now i have no idea.
    }
}
