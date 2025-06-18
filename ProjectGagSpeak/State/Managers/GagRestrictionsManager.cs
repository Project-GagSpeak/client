using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using System.Diagnostics.CodeAnalysis;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;

namespace GagSpeak.State.Managers;

public sealed class GagRestrictionManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly FavoritesManager _favorites;
    private readonly ModSettingPresetManager _modPresets;
    private readonly CacheStateManager _cacheManager;
    private readonly ConfigFileProvider _fileNames;
    private readonly ItemService _items;
    private readonly MufflerService _muffler;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<GarblerRestriction> _itemEditor = new();
    private CharaActiveGags? _serverGagData = null;

    public GagRestrictionManager(
        ILogger<GagRestrictionManager> logger,
        GagspeakMediator mediator,
        FavoritesManager favorites,
        ModSettingPresetManager modPresets,
        CacheStateManager cacheManager,
        ConfigFileProvider fileNames,
        ItemService items,
        MufflerService muffler,
        HybridSaveService saver) : base(logger, mediator)
    {
        _favorites = favorites;
        _modPresets = modPresets;
        _cacheManager = cacheManager;
        _fileNames = fileNames;
        _items = items;
        _muffler = muffler;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => CheckForExpiredLocks());
    }

    public CharaActiveGags? ServerGagData => _serverGagData;
    public GagRestrictionStorage Storage { get; private set; } = new GagRestrictionStorage();
    public GarblerRestriction? ItemInEditor => _itemEditor.ItemInEditor;

    // Meant to serve as a placeholder reference. Maybe remove later, idk.
    public GarblerRestriction[] AppliedGags { get; private set; } = Enumerable.Repeat(new GarblerRestriction(GagType.None), Constants.MaxGagSlots).ToArray();


    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <param name="serverData"> The data from the server to update with. </param>
    /// <remarks> MUST CALL AFTER LOADING PROFILE STORAGE. (Also updates cache and active restrictions. </remarks>
    public void LoadServerData(CharaActiveGags serverData)
    {
        Logger.LogWarning("Loading in all Server-Releated Gag Data!");
        _serverGagData = serverData;
        Logger.LogInformation("Processing active gags to apply the visuals of.");
        for (var slotIdx = 0; slotIdx < serverData.GagSlots.Length; slotIdx++)
        {
            var slot = serverData.GagSlots[slotIdx];
            if (slot.GagItem is not GagType.None)
            {
                Logger.LogInformation($"GagSlot {slotIdx} has GagItem {slot.GagItem.GagName()}");
                if (Storage.TryGetEnabledGag(slot.GagItem, out var item))
                {
                    Logger.LogInformation($"Found GagRestriction for {slot.GagItem.GagName()} in Storage, applying visuals.");
                    AppliedGags[slotIdx] = item;
                }
            }
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(GarblerRestriction restriction) => _itemEditor.StartEditing(Storage, restriction);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogTrace("Saved changes to Edited GagRestriction.");
            // update the cache somehow here, idk how, my brain is fried.
            Mediator.Publish(new ConfigGagRestrictionChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void ToggleEnabledState(GagType gagItem)
    {
        if (Storage.TryGetGag(gagItem, out var item))
        {
            item.IsEnabled = !item.IsEnabled;
            Mediator.Publish(new ConfigGagRestrictionChanged(StorageChangeType.Modified, item));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(GarblerRestriction restriction) => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(GarblerRestriction restriction) => _favorites.RemoveGag(restriction.GagType);

    #region Validators
    public bool CanApply(int layer, GagType newGag) => ServerGagData is { } data && data.GagSlots[layer].CanApply();
    public bool CanLock(int layer) => ServerGagData is { } data && data.GagSlots[layer].CanLock();
    public bool CanUnlock(int layer) => ServerGagData is { } data && data.GagSlots[layer].CanUnlock();
    public bool CanRemove(int layer) => ServerGagData is { } data && data.GagSlots[layer].CanRemove();

    #endregion Validators

    #region Performers
    /// <summary>
    ///     Applies the gag to the spesified layer if possible, and updates the active items.
    /// </summary>
    /// <returns> true if it contained visual changes, false otherwise. </returns>
    public bool ApplyGag(int layer, GagType newGag, string enactor, [NotNullWhen(true)] out GarblerRestriction? item)
    {
        item = null;

        if (ServerGagData is not { } data)
            return false;

        // update values & Garbler, then fire achievement ping.
        Logger.LogTrace($"Applying Gag {newGag.GagName()} to layer {layer} by {enactor}");
        data.GagSlots[layer].GagItem = newGag;
        data.GagSlots[layer].Enabler = enactor;

        Logger.LogTrace($"Updating Garbler Logic for gag {newGag.GagName()} to layer {layer} by {enactor}");
        _muffler.UpdateGarblerLogic(data.CurrentGagNames());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagStateChange, true, layer, newGag, enactor);

        // Mark what parts of this item will end up having effective changes.
        Logger.LogTrace($"Attempting to fetch gag from storage if visuals are enabled.");
        if (Storage.TryGetEnabledGag(newGag, out item))
        {
            Logger.LogTrace($"Found GagRestriction for {newGag.GagName()} in Storage, applying visuals.");
            AppliedGags[layer] = item;
            return true;
        }

        return false;
    }

    public void LockGag(int layer, Padlocks padlock, string pass, DateTimeOffset timer, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (ServerGagData is not { } data)
            return;

        data.GagSlots[layer].Padlock = padlock;
        data.GagSlots[layer].Password = pass;
        data.GagSlots[layer].Timer = timer;
        data.GagSlots[layer].PadlockAssigner = enactor;
        // Fire that the gag was locked for this layer.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, true, layer, padlock, enactor);
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

    public bool RemoveGag(int layer, string enactor, [NotNullWhen(true)] out GarblerRestriction? item)
    {
        item = null;

        if (ServerGagData is not { } data)
            return false;

        // store what gag we are removing, then update data and fire achievement ping.
        var removedGagType = data.GagSlots[layer].GagItem;
        data.GagSlots[layer].GagItem = GagType.None;
        data.GagSlots[layer].Enabler = string.Empty;
        _muffler.UpdateGarblerLogic(data.CurrentGagNames());
        GagspeakEventManager.AchievementEvent(UnlocksEvent.GagStateChange, false, layer, removedGagType, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetEnabledGag(removedGagType, out item))
        {
            // Reset this index to default values.
            AppliedGags[layer] = new GarblerRestriction(GagType.None);
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
        Logger.LogInformation("Loading GagRestrictions Config from {0}", file);

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
            Logger.LogWarning("Gag Restrictions Config not found. Attempting to find old config.");
            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "gag-storage.json");
            if (File.Exists(oldFormatFile))
            {
                jsonText = File.ReadAllText(oldFormatFile);
                jObject = JObject.Parse(jsonText);
                jObject = ConfigMigrator.MigrateGagRestrictionsConfig(jObject, _fileNames, oldFormatFile);
            }
            else
            {
                GagSpeak.StaticLog.Warning("No Config file found for: " + oldFormatFile);
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
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.Gag));
    }

    private void LoadV0(JToken? data)
    {
        if(data is not JObject sortedListData)
            return;

        try
        {
            // otherwise, parse it out and stuff YIPPEE
            foreach (var (gagName, gagData) in sortedListData)
            {
                var gagType = gagName.ToGagType();
                if (gagType is GagType.None)
                {
                    Logger.LogWarning("Invalid GagType: {0}", gagName);
                    continue;
                }

                var newGagItem = GarblerRestriction.FromToken(gagData, gagType, _items, _modPresets);
                Storage[gagType] = newGagItem;
            }
        }
        catch (Exception e)
        {
            Logger.LogError("Failed to load Gag Restrictions: {0}", e);
            return;
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }
    #endregion HybridSavable
    
    private void CheckForExpiredLocks()
    {
        if (!MainHub.IsConnected)
            return;

        if (ServerGagData is null || !ServerGagData.AnyGagLocked())
            return;

        foreach(var (gagSlot, index) in ServerGagData.GagSlots.Select((slot, index) => (slot, index)))
            if (gagSlot.Padlock.IsTimerLock() && gagSlot.HasTimerExpired())
            {
                Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.Gags);
                // only set data relevant to the new change.
                var newData = new ActiveGagSlot()
                {
                    Padlock = gagSlot.Padlock, // match the padlock
                    Password = gagSlot.Password, // use the same password.
                    PadlockAssigner = gagSlot.PadlockAssigner // use the same assigner. (To remove devotional timers)
                };
                Mediator.Publish(new GagDataChangedMessage(DataUpdateType.Unlocked, index, newData));
            }
    }
}
