using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.CkCommons.Newtonsoft;
using GagSpeak.FileSystems;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using OtterGui;
using System.Diagnostics.CodeAnalysis;
using GagSpeak.PlayerClient;
using GagSpeak.PlayerClient;
using GagSpeak.State.Models;

namespace GagSpeak.State.Managers;

public readonly record struct OccupiedRestriction(RestrictionItem Item, ManagerPriority Source);

public sealed class RestrictionManager : DisposableMediatorSubscriberBase, IHybridSavable, IDisposable
{
    private readonly FavoritesManager _favorites;
    private readonly ModSettingPresetManager _modPresets;
    private readonly ConfigFileProvider _fileNames;
    private readonly ItemService _items;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<RestrictionItem> _itemEditor = new();
    private CharaActiveRestrictions? _serverRestrictionData = null;
    private Dictionary<int, RestrictionItem> _activeItems = new();
    // Maybe change this to a restriction item if we need to for later? idk.
    private SortedList<Guid, GagspeakModule> _activeItemsAll = new();
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

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => CheckForExpiredLocks());
    }

    // ----------- STORAGE --------------
    public RestrictionStorage Storage { get; private set; } = new RestrictionStorage();
    public RestrictionItem? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE DATA --------------
    public CharaActiveRestrictions? ServerRestrictionData => _serverRestrictionData;
    public IReadOnlyDictionary<int, RestrictionItem> ActiveItems => _activeItems;
    public IReadOnlyDictionary<Guid, GagspeakModule> ActiveItemsAll => _activeItemsAll;

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled seperately here. </remarks>
    public void LoadServerData(CharaActiveRestrictions serverData)
    {
        _serverRestrictionData = serverData;
        // iterate through each of the server's restriction data. If the identifer is not empty, add it.
        _activeItems.Clear();
        foreach (var (slot, idx) in serverData.Restrictions.WithIndex())
            if (slot.Identifier != Guid.Empty && Storage.TryGetRestriction(slot.Identifier, out var item))
            {
                _activeItems.TryAdd(idx, item);
                _activeItemsAll.TryAdd(slot.Identifier, GagspeakModule.Restriction);
            }
        Logger.LogInformation("Syncronized all Active Restrictions with Client-Side Manager.");
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
        Logger.LogDebug($"Created New [{type}] Restriction ({restriction.Label}) with ID: {restriction.Identifier}.");
        Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Created, restriction, null));
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
        Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Delete(RestrictionItem restriction)
    {
        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(restriction))
        {
            Logger.LogDebug($"Deleted restriction {restriction.Identifier}.");
            Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Deleted, restriction, null));
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
        Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Renamed, restriction, oldName));
    }

    public void UpdateThumbnail(RestrictionItem restriction, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if (Storage.Contains(restriction))
        {
            Logger.LogDebug($"Thumbnail updated for {restriction.Label} to {restriction.ThumbnailPath}");
            restriction.ThumbnailPath = newPath;
            _saver.Save(this);
            Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Modified, restriction, null));
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
            // _managerCache.UpdateCache(AppliedRestrictions);
            _saver.Save(this);

            Logger.LogTrace("Saved changes to Edited RestrictionItem.");
            Mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Modified, sourceItem, null));
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(GarblerRestriction restriction) => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(GarblerRestriction restriction) => _favorites.RemoveGag(restriction.GagType);

    /// <summary> Attempts to add a now occupied restriction. Intended for other sources only. </summary>
    public bool TryAddOccupied(RestrictionItem item, GagspeakModule source)
    {
        if (source is GagspeakModule.Restriction)
            return false;

        return _activeItemsAll.TryAdd(item.Identifier, source);
    }

    /// <summary> Do not allow this if it exists in the active state. </summary>
    public bool TryRemoveRemoveOccupied(RestrictionItem item)
    {
        if (_activeItemsAll.TryGetValue(item.Identifier, out var s) && s == GagspeakModule.Restriction)
            return false;

        return _activeItemsAll.Remove(item.Identifier);
    }

    public bool CanApply(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanApply();
    public bool CanLock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanLock();
    public bool CanUnlock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanUnlock();
    public bool CanRemove(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanRemove();

    #region Active Restriction Updates
    public bool ApplyRestriction(int layerIdx, Guid id, string enactor, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = null;

        if (_serverRestrictionData is not { } data)
            return false;

        // update the values and fire achievement ping. ( None yet )
        data.Restrictions[layerIdx].Identifier = id;
        data.Restrictions[layerIdx].Enabler = enactor;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestrictionStateChange, true, layerIdx, id, enactor);

        // assign the information if present.
        if (Storage.TryGetRestriction(id, out item))
        {
            _activeItems[layerIdx] = item;
            _activeItemsAll[item.Identifier] = GagspeakModule.Restriction;
            return true;
        }

        return false;
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
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestrictionLockStateChange, true, layerIdx, padlock, enactor);
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
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestrictionLockStateChange, false, layerIdx, prevLock, enactor);
    }

    public bool RemoveRestriction(int layerIdx, string enactor, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = null;

        if (_serverRestrictionData is not { } data)
            return false;

        // store the new data, then fire the achievement.
        var removedRestriction = data.Restrictions[layerIdx].Identifier;
        data.Restrictions[layerIdx].Identifier = Guid.Empty;
        data.Restrictions[layerIdx].Enabler = string.Empty;
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RestrictionStateChange, false, layerIdx, removedRestriction, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetRestriction(removedRestriction, out item))
        {
            _activeItems.Remove(layerIdx);
            _activeItemsAll.Remove(item.Identifier);
            return true;
        }

        return false;
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
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.Restriction));
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
                RestrictionType.Collar => CollarRestriction.FromToken(itemJson, _items, _modPresets),
                RestrictionType.Hypnotic => HypnoticRestriction.FromToken(itemJson, _items, _modPresets),
                RestrictionType.Blindfold => BlindfoldRestriction.FromToken(itemJson, _items, _modPresets),
                _ => RestrictionItem.FromToken(itemJson, _items, _modPresets),
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

    private void CheckForExpiredLocks()
    {
        if (!MainHub.IsConnected)
            return;

        if (_serverRestrictionData is null || !_serverRestrictionData.Restrictions.Any(i => i.IsLocked()))
            return;

        foreach (var (restriction, index) in _serverRestrictionData.Restrictions.Select((slot, index) => (slot, index)))
            if (restriction.Padlock.IsTimerLock() && restriction.HasTimerExpired())
            {
                Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.Restrictions);
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
