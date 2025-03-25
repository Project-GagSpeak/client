using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;

namespace GagSpeak.PlayerState.Visual;

public sealed class RestrictionManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GlobalData _globals;
    private readonly GlobalData _clientData;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly ItemService _items;
    private readonly HybridSaveService _saver;

    public RestrictionManager(ILogger<RestrictionManager> logger, GagspeakMediator mediator,
    GagGarbler garbler, GlobalData clientData, FavoritesManager favorites,
    ConfigFileProvider fileNames, ItemService items, HybridSaveService saver) : base(logger, mediator)
    {
        _clientData = clientData;
        _favorites = favorites;
        _fileNames = fileNames;
        _items = items;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckForExpiredLocks());
    }

    // Cached Information.
    public RestrictionItem? ActiveEditorItem { get; private set; }
    public VisualRestrictionsCache LatestVisualCache { get; private set; } = new();
    public SortedList<int, RestrictionItem> ActiveRestrictions { get; private set; } = new();


    // Stored Information.
    /// <summary> Holds any restriction active from OTHER SOURCES. Is not used in Caching information. </summary>
    /// <remarks> <b>Source will ALWAYS be VeryLow</b> unless from a CursedItem, in which it is used for comparison.</remarks>
    public HashSet<(RestrictionItem Item, ManagerPriority Source)> OccupiedRestrictions { get; private set; }
    public CharaActiveRestrictions? ActiveRestrictionsData { get; private set; }
    public RestrictionStorage Storage { get; private set; } = new RestrictionStorage();

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <param name="serverData"> The data from the server to update with. </param>
    /// <remarks> MUST CALL AFTER LOADING PROFILE STORAGE. (Also updates cache and active restrictions. </remarks>
    public void LoadServerData(CharaActiveRestrictions serverData)
    {
        ActiveRestrictionsData = serverData;

        for (var slotIdx = 0; slotIdx < serverData.Restrictions.Length; slotIdx++)
        {
            var slot = serverData.Restrictions[slotIdx];
            if (!slot.Identifier.IsEmptyGuid() && Storage.TryGetRestriction(slot.Identifier, out var item))
            {
                ActiveRestrictions[slotIdx] = item;
                AddOccupiedRestriction(item, ManagerPriority.Restrictions);
            }
        }
        LatestVisualCache.UpdateCache(ActiveRestrictions);
    }

    /// <summary> Create a new Restriction, where the item can be any restriction item. </summary>
    public RestrictionItem CreateNew(string restrictionName, RestrictionType type)
    {
        var restriction = type switch
        {
            RestrictionType.Blindfold => new BlindfoldRestriction(),
            RestrictionType.Collar => new CollarRestriction(),
            _ => new RestrictionItem()
        };
        restriction.Label = restrictionName;
        Storage.Add(restriction);
        _saver.Save(this);
        Logger.LogDebug($"Created new restriction {restriction.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Created, restriction, null));
        return restriction;
    }

    /// <summary> Create a clone of a Restriction. </summary>
    public RestrictionItem CreateClone(RestrictionItem clone, string newName)
    {
        var clonedItem = clone switch
        {
            BlindfoldRestriction b => new BlindfoldRestriction(b, false) { Label = newName },
            CollarRestriction    c => new CollarRestriction(c, false) { Label = newName },
            RestrictionItem      r => new RestrictionItem(r, false) { Label = newName },
            _ => throw new NotImplementedException("Unknown restriction type."),
        };
        Storage.Add(clonedItem);
        _saver.Save(this);
        Logger.LogDebug($"Cloned restriction {clonedItem.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Created, clonedItem, null));
        return clonedItem;
    }


    /// <summary> Delete a Restriction. </summary>
    public void Delete(RestrictionItem restriction)
    {
        if (ActiveEditorItem is null)
            return;

        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(restriction))
        {
            Logger.LogDebug($"Deleted restriction {restriction.Identifier}.");
            Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Deleted, restriction, null));
            _saver.Save(this);
        }
    }


    /// <summary> Rename a Restriction. </summary>
    public void Rename(RestrictionItem restriction, string newName)
    {
        var oldName = restriction.Label;
        if (oldName == newName)
            return;

        restriction.Label = newName;
        _saver.Save(this);
        Logger.LogDebug($"Renamed restriction {restriction.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Renamed, restriction, oldName));
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(RestrictionItem item)
    {
        // create an exact clone of the passed in cursed item for editing, so long as it exists in storage.
        if (Storage.Contains(item))
        {
            ActiveEditorItem = item switch
            {
                BlindfoldRestriction b => new BlindfoldRestriction(b, true),
                CollarRestriction c => new CollarRestriction(c, true),
                RestrictionItem r => new RestrictionItem(r, true),
                _ => throw new NotImplementedException("Unknown restriction type."),
            };
        }
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => ActiveEditorItem = null;

    /// <summary> Injects all the changes made to the Restriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (ActiveEditorItem is null)
            return;
        // Update the active restriction with the new data, update the cache, and clear the edited restriction.
        if (Storage.ByIdentifier(ActiveEditorItem.Identifier) is { } item)
        {
            item = ActiveEditorItem;
            ActiveEditorItem = null;
            Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Modified, item, null));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool AddFavorite(GarblerRestriction restriction)
        => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool RemoveFavorite(GarblerRestriction restriction)
        => _favorites.RemoveGag(restriction.GagType);

    public void AddOccupiedRestriction(RestrictionItem item, ManagerPriority source)
    {
        // dont add the item if it is already existing in the hash set.
        if (!OccupiedRestrictions.Any(i => i.Item == item))
            OccupiedRestrictions.Add((item, source));
    }

    public void RemoveOccupiedRestriction(RestrictionItem item, ManagerPriority source)
        => OccupiedRestrictions.Remove((item, source));

    #region Validators
    public bool CanApply(int layer)
    {
        if (ActiveRestrictionsData is { } data && data.Restrictions[layer].CanApply())
            return true;
        Logger.LogTrace("Not able to Apply at this time due to errors!");
        return false;
    }

    public bool CanLock(int layer)
    {
        if (ActiveRestrictionsData is { } data && data.Restrictions[layer].CanLock())
            return true;
        Logger.LogTrace("Not able to Lock at this time due to errors!");
        return false;
    }

    public bool CanUnlock(int layer)
    {
        if (ActiveRestrictionsData is { } data && data.Restrictions[layer].CanUnlock())
            return true;
        Logger.LogTrace("Not able to Unlock at this time due to errors!");
        return false;
    }

    public bool CanRemove(int layer)
    {
        if (ActiveRestrictionsData is { } data && data.Restrictions[layer].CanRemove())
            return true;
        Logger.LogTrace("Not able to Remove at this time due to errors!");
        return false;
    }
    #endregion Validators




    #region Active Restriction Updates
    public VisualUpdateFlags ApplyRestriction(int layerIdx, Guid id, string enactor, out RestrictionItem? item)
    {
        item = null; var flags = VisualUpdateFlags.None;

        if (ActiveRestrictionsData is not { } data)
            return flags;

        // update the values and fire achievement ping. ( None yet )
        data.Restrictions[layerIdx].Identifier = id;
        data.Restrictions[layerIdx].Enabler = enactor;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestrictionStateChange, true, layerIdx, id, enactor);

        // assign the information if present.
        if (Storage.TryGetRestriction(id, out item))
        {
            // Assume everything is set.
            flags = VisualUpdateFlags.AllRestriction;
            // set the restriction item at the defined index.
            foreach (var restriction in ActiveRestrictions)
            {
                // these properties should not be updated if an item with higher priority contains it.
                if (restriction.Key > layerIdx)
                {
                    if (item.Glamour is not null && restriction.Value.Glamour.Slot == item.Glamour.Slot)
                        flags &= ~VisualUpdateFlags.Glamour;

                    if (item.Mod is not null && restriction.Value.Mod.ModInfo == item.Mod.ModInfo)
                        flags &= ~VisualUpdateFlags.Mod;
                }

                // these properties should not be updated if any item contains it.
                if (restriction.Value.Moodle == item.Moodle)
                    flags &= ~VisualUpdateFlags.Moodle;
            }
            // Update the activeVisualState, and the cache.
            ActiveRestrictions[layerIdx] = item;
            AddOccupiedRestriction(item, ManagerPriority.Restrictions);
            LatestVisualCache.UpdateCache(ActiveRestrictions);
        }
        return flags;
    }

    public void LockRestriction(int layerIdx, Padlocks padlock, string pass, DateTimeOffset timer, string enactor)
    {
        if (ActiveRestrictionsData is not { } data)
            return;

        data.Restrictions[layerIdx].Padlock = padlock;
        data.Restrictions[layerIdx].Password = pass;
        data.Restrictions[layerIdx].Timer = timer;
        data.Restrictions[layerIdx].PadlockAssigner = enactor;
        // Fire that the gag was locked for this layer.
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestrictionLockStateChange, true, layerIdx, padlock, enactor);
    }

    public void UnlockRestriction(int layerIdx, string enactor)
    {
        if (ActiveRestrictionsData is not { } data)
            return;

        var prevLock = data.Restrictions[layerIdx].Padlock;

        data.Restrictions[layerIdx].Padlock = Padlocks.None;
        data.Restrictions[layerIdx].Password = string.Empty;
        data.Restrictions[layerIdx].Timer = DateTimeOffset.MinValue;
        data.Restrictions[layerIdx].PadlockAssigner = string.Empty;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestrictionLockStateChange, false, layerIdx, prevLock, enactor);
    }

    public VisualUpdateFlags RemoveRestriction(int layerIdx, string enactor, out RestrictionItem? item)
    {
        item = null; var flags = VisualUpdateFlags.None;

        if (ActiveRestrictionsData is not { } data)
            return flags;

        // store the new data, then fire the achievement.
        var removedRestriction = data.Restrictions[layerIdx].Identifier;
        data.Restrictions[layerIdx].Identifier = Guid.Empty;
        data.Restrictions[layerIdx].Enabler = string.Empty;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestrictionStateChange, false, layerIdx, removedRestriction, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetRestriction(removedRestriction, out var matchedItem))
        {
            // Do recalculations first since it doesnt madder here.
            ActiveRestrictions.Remove(layerIdx);
            RemoveOccupiedRestriction(matchedItem, ManagerPriority.Restrictions);
            LatestVisualCache.UpdateCache(ActiveRestrictions);

            // begin by assuming all aspects are removed.
            flags = VisualUpdateFlags.AllGag;
            // Glamour Item will always be valid so don't worry about it.
            if (matchedItem.Mod is null) flags &= ~VisualUpdateFlags.Mod;
            if (matchedItem.Moodle is null) flags &= ~VisualUpdateFlags.Moodle;
        }
        return flags;
    }
    #endregion Active Restriction Updates

    #region HybridSavable
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.Restrictions).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // Construct the array of CursedLootItems.
        var restrictionItems = new JArray();
        foreach (var item in Storage)
            restrictionItems.Add(item.Serialize());

        // construct the config object to serialize.
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["RestrictionItems"] = restrictionItems,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.Restrictions;
        Logger.LogInformation("Loading in Restrictions Config for file: " + file);
        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Restrictions file found at {0}", file);
            // create a new file with default values.
            _saver.Save(this);
            return;
        }

        // Read the json from the file.
        var jsonText = File.ReadAllText(file);
        var jObject = JObject.Parse(jsonText);
        var version = jObject["Version"]?.Value<int>() ?? 0;

        // Perform Migrations if any, and then load the data.
        switch (version)
        {
            case 0:
                LoadV0(jObject["RestrictionItems"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(ModuleSection.Restriction));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray restrictions)
            return;

        // Load all items
        foreach (var itemToken in restrictions)
        {
            if (itemToken is not JObject itemJson)
                continue;

            // Identify the type of restriction
            var typeString = itemJson["Type"]?.ToObject<string>();
            if (!Enum.TryParse<RestrictionType>(typeString, out var restrictionType))
            {
                Logger.LogError($"Unknown RestrictionType: {typeString}");
                continue;
            }

            // Create an instance of the correct type
            var restrictionItem = restrictionType switch
            {
                RestrictionType.Blindfold => new BlindfoldRestriction(),
                RestrictionType.Collar => new CollarRestriction(),
                _ => new RestrictionItem() // Fallback to base class
            };

            // Deserialize the item
            restrictionItem.LoadRestriction(itemJson, _items);
            Storage.Add(restrictionItem);
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable

    public void CheckForExpiredLocks()
    {
        if (!MainHub.IsConnected)
            return;

        if (ActiveRestrictionsData is null || !ActiveRestrictionsData.Restrictions.Any(i => i.IsLocked()))
            return;

        foreach (var (restriction, index) in ActiveRestrictionsData.Restrictions.Select((slot, index) => (slot, index)))
            if (restriction.Padlock.IsTimerLock() && restriction.HasTimerExpired())
            {
                Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.PadlockHandling);
                // only set data relevant to the new change.
                var newData = new ActiveRestriction()
                {
                    Padlock = restriction.Padlock, // match the padlock
                    Password = restriction.Password, // use the same password.
                    PadlockAssigner = restriction.PadlockAssigner // use the same assigner. (To remove devotional timers)
                };
                Mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Unlocked, index, newData));
            }
    }
}
