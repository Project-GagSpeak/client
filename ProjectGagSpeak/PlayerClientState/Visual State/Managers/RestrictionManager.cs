using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.CkCommons.Newtonsoft;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui;

namespace GagSpeak.PlayerState.Visual;

public readonly record struct OccupiedRestriction(RestrictionItem Item, ManagerPriority Source);

public sealed class RestrictionManager : DisposableMediatorSubscriberBase, IHybridSavable, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly ModSettingPresetManager _modPresets;
    private readonly ConfigFileProvider _fileNames;
    private readonly ItemService _items;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<RestrictionItem> _itemEditor = new();
    private VisualRestrictionsCache _managerCache = new();
    private CharaActiveRestrictions? _serverRestrictionData = null;

    public RestrictionManager(
        ILogger<RestrictionManager> logger,
        GagspeakMediator mediator,
        FavoritesManager favorites,
        ModSettingPresetManager modPresets, 
        ConfigFileProvider fileNames,
        ItemService items,
        HybridSaveService saver) : base(logger, mediator)
    {
        _favorites = favorites;
        _modPresets = modPresets;
        _fileNames = fileNames;
        _items = items;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckForExpiredLocks());
    }

    public VisualRestrictionsCache VisualCache => _managerCache;
    public CharaActiveRestrictions? ServerRestrictionData => _serverRestrictionData;
    public RestrictionStorage Storage { get; private set; } = new RestrictionStorage();
    public RestrictionItem? ItemInEditor => _itemEditor.ItemInEditor;
    public RestrictionItem[] AppliedRestrictions { get; private set; } = Enumerable.Repeat(new RestrictionItem(), Constants.MaxRestrictionSlots).ToArray();

    /// <summary> Holds any restriction active from OTHER SOURCES. Is not used in Caching information. </summary>
    public HashSet<OccupiedRestriction> OccupiedRestrictions { get; private set; }


    /// <summary> Updates the manager with the latest data from the server. </summary>
    public void LoadServerData(CharaActiveRestrictions serverData)
    {
        _serverRestrictionData = serverData;
        foreach (var (slot, idx) in AppliedRestrictions.WithIndex())
        {
            if (!slot.Identifier.IsEmptyGuid() && Storage.TryGetRestriction(slot.Identifier, out var item))
            {
                AppliedRestrictions[idx] = item;
                AddOccupiedRestriction(item, ManagerPriority.Restrictions);
            }
        }
        _managerCache.UpdateCache(AppliedRestrictions);
    }

    public RestrictionItem CreateNew(string name, RestrictionType type)
    {
        name = RegexEx.EnsureUniqueName(name, Storage, x => x.Label);
        var restriction = type switch
        {
            RestrictionType.Collar => new CollarRestriction() { Label = name },
            RestrictionType.Hypnotic => new HypnoticRestriction() { Label = name },
            RestrictionType.Blindfold => new BlindfoldRestriction() { Label = name },
            _ => new RestrictionItem() { Label = name }
        };
        Storage.Add(restriction);
        _saver.Save(this);
        Logger.LogDebug($"Created new restriction {restriction.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Created, restriction, null));
        return restriction;
    }

    public RestrictionItem CreateClone(RestrictionItem clone, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        var clonedItem = clone switch
        {
            BlindfoldRestriction b => new BlindfoldRestriction(b, false) { Label = newName },
            HypnoticRestriction  h => new HypnoticRestriction(h, false) { Label = newName },
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

    public void Delete(RestrictionItem restriction)
    {
        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(restriction))
        {
            Logger.LogDebug($"Deleted restriction {restriction.Identifier}.");
            Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Deleted, restriction, null));
            _saver.Save(this);
        }
    }

    public void Rename(RestrictionItem restriction, string newName)
    {
        var oldName = restriction.Label;
        if (oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        restriction.Label = newName;
        _saver.Save(this);
        Logger.LogDebug($"Renamed restriction {restriction.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Renamed, restriction, oldName));
    }

    public void UpdateThumbnail(RestrictionItem restriction, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if (Storage.Contains(restriction))
        {
            Logger.LogDebug($"Thumbnail updated for {restriction.Label} to {restriction.ThumbnailPath}");
            restriction.ThumbnailPath = newPath;
            _saver.Save(this);
            Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Modified, restriction, null));
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(RestrictionItem item) => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _managerCache.UpdateCache(AppliedRestrictions);
            _saver.Save(this);

            Logger.LogTrace("Saved changes to Edited RestrictionItem.");
            Mediator.Publish(new ConfigRestrictionChanged(StorageItemChangeType.Modified, sourceItem, null));
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(GarblerRestriction restriction) => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(GarblerRestriction restriction) => _favorites.RemoveGag(restriction.GagType);

    /// <summary> Appends a restriction item being used by other components to this list. </summary>
    /// <remarks> The Occupied Restrictions list is scanned against to prevent duplicate application by other modules. </remarks>
    public void AddOccupiedRestriction(RestrictionItem item, ManagerPriority source)
    {
        // dont add the item if it is already existing in the hash set.
        if (!OccupiedRestrictions.Any(i => i.Item == item))
            OccupiedRestrictions.Add(new(item, source));
    }

    public void RemoveOccupiedRestriction(RestrictionItem item, ManagerPriority source)
        => OccupiedRestrictions.Remove(new(item, source));

    public bool CanApply(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanApply();
    public bool CanLock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanLock();
    public bool CanUnlock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanUnlock();
    public bool CanRemove(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanRemove();

    #region Active Restriction Updates
    public VisualUpdateFlags ApplyRestriction(int layerIdx, Guid id, string enactor, out RestrictionItem? item)
    {
        item = null; var flags = VisualUpdateFlags.None;

        if (_serverRestrictionData is not { } data)
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
            foreach (var (appliedItem, idx) in AppliedRestrictions.WithIndex())
            {
                // these properties should not be updated if an item with higher priority contains it.
                if (idx > layerIdx)
                {
                    if (item.Glamour is not null && appliedItem.Glamour.Slot == item.Glamour.Slot)
                        flags &= ~VisualUpdateFlags.Glamour;

                    if (item.Mod.HasData && appliedItem.Mod.Label == item.Mod.Label)
                        flags &= ~VisualUpdateFlags.Mod;
                }

                // these properties should not be updated if any item contains it.
                if (appliedItem.Moodle == item.Moodle)
                    flags &= ~VisualUpdateFlags.Moodle;
            }

            AppliedRestrictions[layerIdx] = item;
            AddOccupiedRestriction(item, ManagerPriority.Restrictions);
            _managerCache.UpdateCache(AppliedRestrictions);
        }
        return flags;
    }

    public void LockRestriction(int layerIdx, Padlocks padlock, string pass, DateTimeOffset timer, string enactor)
    {
        if (_serverRestrictionData is not { } data)
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
        if (_serverRestrictionData is not { } data)
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

        if (_serverRestrictionData is not { } data)
            return flags;

        // store the new data, then fire the achievement.
        var removedRestriction = data.Restrictions[layerIdx].Identifier;
        data.Restrictions[layerIdx].Identifier = Guid.Empty;
        data.Restrictions[layerIdx].Enabler = string.Empty;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestrictionStateChange, false, layerIdx, removedRestriction, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetRestriction(removedRestriction, out var matchedItem))
        {
            // Do recalculations first since it doesn't madder here.
            AppliedRestrictions[layerIdx] = new RestrictionItem();
            RemoveOccupiedRestriction(matchedItem, ManagerPriority.Restrictions);
            _managerCache.UpdateCache(AppliedRestrictions);

            // begin by assuming all aspects are removed.
            flags = VisualUpdateFlags.AllGag;
            // Glamour Item will always be valid so don't worry about it.
            if (!matchedItem.Mod.HasData) flags &= ~VisualUpdateFlags.Mod;
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
        {
            Logger.LogInformation("Serializing item: " + item.ToString());
            restrictionItems.Add(item.Serialize());
        }

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
                RestrictionType.Blindfold => JParserBinds.FromBlindfoldToken(itemJson, _items, _modPresets),
                RestrictionType.Collar => JParserBinds.FromCollarToken(itemJson, _items, _modPresets),
                _ => JParserBinds.FromNormalToken(itemJson, _items, _modPresets),
            };
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

        if (_serverRestrictionData is null || !_serverRestrictionData.Restrictions.Any(i => i.IsLocked()))
            return;

        foreach (var (restriction, index) in _serverRestrictionData.Restrictions.Select((slot, index) => (slot, index)))
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
