using Dalamud.Plugin;
using GagSpeak.CustomCombos.EditorCombos;
using GagSpeak.Interop.Ipc;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services.Mediator;
using NAudio.SoundFont;
using OtterGui.Text.Widget.Editors;
using Penumbra.Api.Enums;
using Penumbra.Api.IpcSubscribers;
using System.Threading.Tasks;

namespace GagSpeak.PlayerState.Visual;

// Handles all Glamour Caching and applier interactions.
public sealed partial class ModHandler : DisposableMediatorSubscriberBase
{
    private readonly IpcCallerPenumbra _ipc;
    private readonly ModSettingPresetManager _manager;

    public ModHandler(ILogger<ModHandler> logger, GagspeakMediator mediator,
        IpcCallerPenumbra ipc, ModSettingPresetManager manager, 
        IDalamudPluginInterface pi) : base(logger, mediator)
    {
        _ipc = ipc;
        _manager = manager;

        _ipc.OnModMoved = ModMoved.Subscriber(pi, OnModInfoChanged);
        _ipc.OnModAdded = ModAdded.Subscriber(pi, OnModAdded);
        _ipc.OnModDeleted = ModDeleted.Subscriber(pi, OnModDeleted);
        Mediator.Subscribe<PenumbraInitializedMessage>(this, (msg) => OnPenumbraInitialized());

        // if penumbra api is connected, immediately run a OnPenumbraInitialized after our load.
        if (IpcCallerPenumbra.APIAvailable)
            OnPenumbraInitialized();
    }

    protected override void Dispose(bool disposing)
    {
        // unsubscribe from the penumbra events.
        base.Dispose(disposing);
        _ipc.OnModMoved?.Dispose();
        _ipc.OnModAdded?.Dispose();
        _ipc.OnModDeleted?.Dispose();
    }

    private void OnPenumbraInitialized()
    {
        Logger.LogInformation("Penumbra initialized. Retrieving Mod Info.");
        _manager.PenumbraInitialized(_ipc.GetModInfo());
    }

    /// <summary> Fired whenever a MOD DIRECTORY (not mod name) is moved or renamed in penumbra. We should get a full recalculation if this occurs. </summary>
    private void OnModInfoChanged(string oldPath, string newPath)
    {
        // TODO: (Handle how this affects other dependent sources, (Should not be an issue for us but we will see).
    }

    private void OnModAdded(string addedDirectory)
    {
        // TODO: Get the mod name for the directory, and its data necessary to construct a ModInfo object.
    }

    private void OnModDeleted(string deletedDirectory)
    {
        // TODO: Handle logic that updates anything using this directory to be removed.
    }

    public Task ApplyModCache()
    {
        Logger.LogDebug("Applying Mod Cache.");
        SetOrUpdateTempMod(_finalMods);
        return Task.CompletedTask;
    }

    public Task RestoreAndReapplyCache(IEnumerable<ModSettingsPreset> modsToRemove)
    {
        Logger.LogDebug("Restoring and reapplying Mod Cache.");
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
            Logger.LogWarning($"Failed to set ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
        else
            Logger.LogDebug($"Successfully set ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
    }

    /// <summary> Creates multiple new temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful for all <paramref name="modPresets"/></returns>
    private void SetOrUpdateTempMod(IEnumerable<ModSettingsPreset> modPresets)
    {
        Logger.LogDebug($"Setting or updating {modPresets.Count()} temporary mods.");
        Parallel.ForEach(modPresets, preset => SetOrUpdateTempMod(preset));
    }

    /// <summary> Removes a temporary mod with a lock, or updates the existing locked mod's temporary settings. </summary>
    /// <returns> If the operation was successful. </returns>
    private void RemoveTempMod(ModSettingsPreset modPreset, bool redraw = false)
    {
        if (_ipc.RemoveTemporaryMod(modPreset) is not PenumbraApiEc.Success)
            Logger.LogWarning($"Failed to remove ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
        else
            Logger.LogDebug($"Removed ModPreset [{modPreset.Label}] for mod [{modPreset.Container.ModName}].");
     }

    /// <summary> Removes multiple temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful for all <paramref name="modPresets"/></returns>
    private void RemoveTempMod(IEnumerable<ModSettingsPreset> modPresets)
    {
        Logger.LogDebug($"Removing {modPresets.Count()} temporary mods.");
        Parallel.ForEach(modPresets, preset => RemoveTempMod(preset));
    }

    /// <summary> Removes all temporary mods with a lock, or updates the existing locked mods temporary settings. </summary>
    /// <returns> If the operation was successful. </returns>
    private void RemoveAllTempMods(bool redraw = false)
    {
        if(_ipc.ClearAllTemporaryMods() is PenumbraApiEc.Success)
            Logger.LogDebug("Successfully cleared all temporary mods.");
        else
            Logger.LogWarning("Failed to clear all temporary mods.");
    }
}
