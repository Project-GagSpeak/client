using GagSpeak.Interop;
using GagSpeak.PlayerClient;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using Penumbra.Api.Enums;

namespace GagSpeak.State.Handlers;

public class ModHandler
{
    private readonly ILogger<ModHandler> _logger;
    private readonly IpcCallerPenumbra _ipc;
    private readonly ModCache _cache;
    private readonly ModSettingPresetManager _manager;

    public ModHandler(
        ILogger<ModHandler> logger,
        IpcCallerPenumbra ipc,
        ModCache cache,
        ModSettingPresetManager manager)
    {
        _logger = logger;
        _ipc = ipc;
        _cache = cache;
        _manager = manager;
    }

    public Task ApplyModCache()
    {
        _logger.LogDebug("Applying Mod Cache.");
        SetOrUpdateTempMod(_cache.FinalMods);
        return Task.CompletedTask;
    }

    public Task RestoreAndReapplyCache(IEnumerable<ModSettingsPreset> modsToRemove)
    {
        _logger.LogDebug("Restoring and reapplying Mod Cache.");
        // Remove control over the mods no longer in the cache.
        RemoveTempMod(modsToRemove);
        // Then reapply the cache.
        ApplyModCache();
        return Task.CompletedTask;
    }

    /// <summary> Creates a new temporary mod with a lock, or updates the existing locked mod's temporary settings. </summary>
    /// <returns> If the operation was successful. </returns>
    private void SetOrUpdateTempMod(ModSettingsPreset modPreset, bool redraw = false)
    {
        if (_ipc.SetOrUpdateTemporaryMod(modPreset) is not PenumbraApiEc.Success)
            _logger.LogWarning($"Failed to set ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
        else
            _logger.LogDebug($"Successfully set ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
    }

    /// <summary> Creates multiple new temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful for all <paramref name="modPresets"/></returns>
    private void SetOrUpdateTempMod(IEnumerable<ModSettingsPreset> modPresets)
    {
        _logger.LogDebug($"Setting or updating {modPresets.Count()} temporary mods.");
        Parallel.ForEach(modPresets, preset => SetOrUpdateTempMod(preset));
    }

    /// <summary> Removes a temporary mod with a lock, or updates the existing locked mod's temporary settings. </summary>
    /// <returns> If the operation was successful. </returns>
    private void RemoveTempMod(ModSettingsPreset modPreset, bool redraw = false)
    {
        if (_ipc.RemoveTemporaryMod(modPreset) is not PenumbraApiEc.Success)
            _logger.LogWarning($"Failed to remove ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
        else
            _logger.LogDebug($"Removed ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
     }

    /// <summary> Removes multiple temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful for all <paramref name="modPresets"/></returns>
    private void RemoveTempMod(IEnumerable<ModSettingsPreset> modPresets)
    {
        _logger.LogDebug($"Removing {modPresets.Count()} temporary mods.");
        Parallel.ForEach(modPresets, preset => RemoveTempMod(preset));
    }

    /// <summary> Removes all temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful. </returns>
    private void RemoveAllTempMods(bool redraw = false)
    {
        if(_ipc.ClearAllTemporaryMods() is PenumbraApiEc.Success)
            _logger.LogDebug("Successfully cleared all temporary mods.");
        else
            _logger.LogWarning("Failed to clear all temporary mods.");
    }
}
