using System.Diagnostics.CodeAnalysis;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using OtterGui.Extensions;

namespace GagSpeak.State.Managers;

public sealed class GagRestrictionManager : IHybridSavable
{
    private readonly ILogger<GagRestrictionManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly FavoritesConfig _favorites;
    private readonly ModPresetManager _modPresets;
    private readonly ConfigFileProvider _fileNames;
    private readonly MufflerService _muffler;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<GarblerRestriction> _itemEditor = new();
    private CharaActiveGags? _serverGagData = null;
    private Dictionary<int, GarblerRestriction> _activeItems = new();

    public GagRestrictionManager(
        ILogger<GagRestrictionManager> logger,
        GagspeakMediator mediator,
        FavoritesConfig favorites,
        ModPresetManager modPresets,
        ConfigFileProvider fileNames,
        MufflerService muffler,
        HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _favorites = favorites;
        _modPresets = modPresets;
        _fileNames = fileNames;
        _muffler = muffler;
        _saver = saver;
    }

    // ----------- STORAGE --------------
    public GagRestrictionStorage Storage { get; private set; } = new GagRestrictionStorage();
    public GarblerRestriction? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE DATA --------------
    public CharaActiveGags? ServerGagData => _serverGagData;
    public IReadOnlyDictionary<int, GarblerRestriction> ActiveItems => _activeItems;

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled seperately here. </remarks>
    public void LoadServerData(CharaActiveGags serverData)
    {
        _serverGagData = serverData;
        // iterate through each of the server's gag data.
        _activeItems.Clear();
        foreach (var (slot, idx) in serverData.GagSlots.WithIndex())
            if (slot.GagItem is not GagType.None && Storage.TryGetGag(slot.GagItem, out var item))
                _activeItems.TryAdd(idx, item);
        // resync the active chat garbler data if any were set.
        _muffler.UpdateGarblerLogic(serverData.CurrentGagNames(), MufflerService.MuffleType(serverData.GagSlots.Select(g => g.GagItem)));
        _logger.LogInformation("Synchronized all Active GagSlots with Client-Side Manager.");
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(GarblerRestriction restriction)
        => _itemEditor.StartEditing(Storage, restriction);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogTrace("Saved changes to Edited GagRestriction.");
            // update the cache somehow here, idk how, my brain is fried.
            _mediator.Publish(new ConfigGagRestrictionChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void ToggleVisibility(GagType gagItem)
    {
        if (Storage.TryGetGag(gagItem, out var item))
        {
            item.IsEnabled = !item.IsEnabled;
            _mediator.Publish(new ConfigGagRestrictionChanged(StorageChangeType.Modified, item));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(GarblerRestriction restriction)
        => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(GarblerRestriction restriction)
        => _favorites.RemoveGag(restriction.GagType);

    #region Validators
    public bool CanApply(int layer)
        => ServerGagData is { } data && data.GagSlots[layer].CanApply();
    public bool CanLock(int layer)
        => ServerGagData is { } data && data.GagSlots[layer].CanLock();
    public bool CanUnlock(int layer)
        => ServerGagData is { } data && data.GagSlots[layer].CanUnlock();
    public bool CanRemove(int layer)
        => ServerGagData is { } data && data.GagSlots[layer].CanRemove();

    #endregion Validators

    #region Performers
    /// <summary> Applies the gag to the specified layer if possible, and updates the active items. </summary>
    /// <returns> true if it contained visual changes, false otherwise. </returns>
    public bool ApplyGag(int layer, GagType newGag, string enactor, [NotNullWhen(true)] out GarblerRestriction? item)
    {
        item = null;

        if (ServerGagData is not { } data)
            return false;

        // update values & Garbler, then fire achievement ping.
        _logger.LogTrace($"Applying {newGag.GagName()} to layer <{layer}> (By: {enactor}");
        data.GagSlots[layer].GagItem = newGag;
        data.GagSlots[layer].Enabler = enactor;

        _logger.LogTrace($"Updating Garbler Logic for gag {newGag.GagName()} to layer {layer} by {enactor}");
        _muffler.UpdateGarblerLogic(data.CurrentGagNames(), MufflerService.MuffleType(data.GagSlots.Select(g => g.GagItem)));
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagStateChange, layer, newGag, true, enactor);

        // Mark what parts of this item will end up having effective changes.
        _logger.LogTrace($"Attempting to fetch gag from storage if visuals are enabled.");
        if (Storage.TryGetGag(newGag, out item))
        {
            _logger.LogTrace($"Found GagRestriction for {newGag.GagName()} in Storage.");
            _activeItems[layer] = item;
            return item.IsEnabled;
        }

        return false;
    }

    public void LockGag(int layer, ActiveGagSlot newData, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (ServerGagData is not { } data)
            return;

        data.GagSlots[layer].Padlock = newData.Padlock;
        data.GagSlots[layer].Password = newData.Password;
        data.GagSlots[layer].Timer = newData.Timer;
        data.GagSlots[layer].PadlockAssigner = newData.PadlockAssigner;
        // Fire that the gag was locked for this layer.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, true, layer, newData.Padlock, enactor);
    }

    public void UnlockGag(int layer, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (ServerGagData is not { } data)
            return;

        var prevLock = data.GagSlots[layer].Padlock;

        data.GagSlots[layer].Padlock = Padlocks.None;
        data.GagSlots[layer].Password = string.Empty;
        data.GagSlots[layer].Timer = DateTimeOffset.MinValue;
        data.GagSlots[layer].PadlockAssigner = string.Empty;
        // Fire that the gag was unlocked for this layer.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, false, layer, prevLock, enactor);
    }

    /// <summary> Applies the gag to the spesified layer if possible, and updates the active items. </summary>
    /// <returns> true if it contained visual changes, false otherwise. </returns>
    public bool RemoveGag(int layer, string enactor, [NotNullWhen(true)] out GarblerRestriction? item)
    {
        item = null;

        if (ServerGagData is not { } data)
            return false;

        // store what gag we are removing, then update data and fire achievement ping.
        var removedGag = data.GagSlots[layer].GagItem;
        data.GagSlots[layer].GagItem = GagType.None;
        data.GagSlots[layer].Enabler = string.Empty;
        _muffler.UpdateGarblerLogic(data.CurrentGagNames(), MufflerService.MuffleType(data.GagSlots.Select(g => g.GagItem)));
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagStateChange, layer, removedGag, false, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetGag(removedGag, out item))
        {
            // always revert the visuals.
            _activeItems.Remove(layer);
            return true;
        }

        return false;
    }
    #endregion Performers

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.GagRestrictions).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        var gagRestrictions = new JObject();

        foreach (var (gagType, gagData) in Storage)
            gagRestrictions[gagType.GagName()] = gagData.Serialize();

        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["GagRestrictions"] = gagRestrictions
        }.ToString(Formatting.Indented);
    }

    // our CUSTOM defined load and migration.
    public void Load()
    {
        var file = _fileNames.GagRestrictions;
        Storage = new GagRestrictionStorage();
        // Migrate to the new filetype if necessary.
        _logger.LogInformation("Loading GagRestrictions Config from {0}", file);

        var jsonText = "";
        JObject jObject = new();

        // if the main file does not exist, attempt to load the text from the backup.
        if (File.Exists(file))
        {
            jsonText = File.ReadAllText(file);
            jObject = JObject.Parse(jsonText);
        }
        else
        {
            _logger.LogWarning("Gag Restrictions Config not found. Attempting to find old config.");
            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "gag-storage.json");
            if (File.Exists(oldFormatFile))
            {
                jsonText = File.ReadAllText(oldFormatFile);
                jObject = JObject.Parse(jsonText);
                jObject = ConfigMigrator.MigrateGagRestrictionsConfig(jObject, _fileNames, oldFormatFile);
            }
            else
            {
                _logger.LogWarning("No Config file found for: " + oldFormatFile);
                _saver.Save(this);
                return;
            }
        }

        // Read the json from the file.
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject["GagRestrictions"]);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GSModule.Gag));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject sortedListData)
            return;

        try
        {
            // otherwise, parse it out and stuff YIPPEE
            foreach (var (gagName, gagData) in sortedListData)
            {
                var gagType = gagName.ToGagType();
                if (gagType is GagType.None)
                {
                    _logger.LogWarning("Invalid GagType: {0}", gagName);
                    continue;
                }

                var newGagItem = GarblerRestriction.FromToken(gagData, gagType, _modPresets);
                Storage[gagType] = newGagItem;
            }
        }
        catch (Bagagwa e)
        {
            _logger.LogError("Failed to load Gag Restrictions: {0}", e);
            return;
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSavable
}
