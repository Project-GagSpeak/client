using CkCommons.Gui;
using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using OtterGui.Extensions;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;

public readonly record struct OccupiedRestriction(RestrictionItem Item, ManagerPriority Source);

public sealed class RestrictionManager : IHybridSavable
{
    private readonly ILogger<RestrictionManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly FavoritesConfig _favorites;
    private readonly ModPresetManager _modPresets;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<RestrictionItem> _itemEditor = new();
    private CharaActiveRestrictions? _serverRestrictionData = null;

    private Dictionary<int, RestrictionItem> _activeItems = new();
    private Dictionary<Guid, RestrictionItem> _lootItems = new();
    // a map that serves a duel purpose of both locating all used restriction GUID's, and fetching their layers.
    private Dictionary<Guid, int> _idToLayerMap = new();
    private int _cursedKeyIdCounter = 0;

    public RestrictionManager(
        ILogger<RestrictionManager> logger,
        GagspeakMediator mediator,
        FavoritesConfig favorites,
        ModPresetManager modPresets,
        ConfigFileProvider fileNames,
        HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _favorites = favorites;
        _modPresets = modPresets;
        _fileNames = fileNames;
        _saver = saver;
    }

    // ----------- STORAGE --------------
    public RestrictionStorage Storage { get; private set; } = new RestrictionStorage();
    public RestrictionItem? ItemInEditor => _itemEditor.ItemInEditor;

    // ----------- ACTIVE DATA --------------
    public CharaActiveRestrictions? ServerRestrictionData => _serverRestrictionData;
    public IReadOnlyDictionary<int, RestrictionItem> ActiveItems => _activeItems;
    public IReadOnlyDictionary<Guid, RestrictionItem> LootItems => _lootItems;
    public IReadOnlyDictionary<Guid, int> IdToLayerMap => _idToLayerMap;

    public bool IsItemApplied(Guid id) => _idToLayerMap.ContainsKey(id);
    private int GenCursedKeyId(Precedence priority) => (int)priority * 1000 + 1000 + _cursedKeyIdCounter++;

    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <remarks> The CacheStateManager must be handled seperately here. </remarks>
    public void LoadInternalData(CharaActiveRestrictions serverData, List<CursedRestrictionItem> cursedItems)
    {
        _serverRestrictionData = serverData;
        _activeItems.Clear();
        _lootItems.Clear();
        _idToLayerMap.Clear();
        _cursedKeyIdCounter = 0;

        // iterate through each of the server's restriction data. If the identifer is not empty, add it.
        foreach (var (slot, idx) in serverData.Restrictions.WithIndex())
        {
            if (slot.Identifier == Guid.Empty)
                continue;

            if (Storage.TryGetRestriction(slot.Identifier, out var item))
            {
                _activeItems.TryAdd(idx, item);
                _idToLayerMap.TryAdd(slot.Identifier, idx);
            }
        }
        _logger.LogInformation("Synchronized all Active Server Restrictions with Client-Side Manager.");

        // iterate through each of the cursed items, and add them to the loot items if they are valid.
        foreach (var item in cursedItems.Where(i => i.RefItem is not null))
        {
            _lootItems.TryAdd(item.Identifier, item.RefItem);
            _idToLayerMap.TryAdd(item.RefItem.Identifier, GenCursedKeyId(item.Precedence));
        }
        _logger.LogInformation("Synchronized all Active Cursed Restrictions with Client-Side Manager.");
    }

    public RestrictionItem CreateNew(string name, RestrictionType type)
    {
        name = CkGui.TooltipTokenRegex().Replace(name, string.Empty);
        name = RegexEx.EnsureUniqueName(name, Storage, x => x.Label);
        var restriction = type switch
        {
            RestrictionType.Hypnotic => new HypnoticRestriction() { Label = name },
            RestrictionType.Blindfold => new BlindfoldRestriction() { Label = name },
            _ => new RestrictionItem() { Label = name }
        };
        Storage.Add(restriction);
        _saver.Save(this);
        _logger.LogDebug($"Created New [{type}] Restriction ({restriction.Label}) with ID: {restriction.Identifier}.");
        _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Created, restriction, null));
        return restriction;
    }

    public RestrictionItem CreateClone(RestrictionItem clone, string newName)
    {
        newName = CkGui.TooltipTokenRegex().Replace(newName, string.Empty);
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        var clonedItem = clone switch
        {
            BlindfoldRestriction b => new BlindfoldRestriction(b, false) { Label = newName },
            HypnoticRestriction  h => new HypnoticRestriction(h, false) { Label = newName },
            RestrictionItem      r => new RestrictionItem(r, false) { Label = newName },
            _ => throw new NotImplementedException("Unknown restriction type."),
        };
        Storage.Add(clonedItem);
        _saver.Save(this);

        _logger.LogDebug($"Cloned restriction {clonedItem.Identifier}.");
        _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void Delete(RestrictionItem restriction)
    {
        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(restriction))
        {
            _logger.LogDebug($"Deleted restriction {restriction.Identifier}.");
            _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Deleted, restriction, null));
            _saver.Save(this);
        }
    }

    public void Rename(RestrictionItem restriction, string newName)
    {
        var oldName = restriction.Label;
        if (oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        newName = CkGui.TooltipTokenRegex().Replace(newName, string.Empty);
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        restriction.Label = newName;
        _saver.Save(this);
        _logger.LogDebug($"Renamed restriction {restriction.Identifier}.");
        _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Renamed, restriction, oldName));
    }

    public void UpdateThumbnail(RestrictionItem restriction, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if (Storage.Contains(restriction))
        {
            _logger.LogDebug($"Thumbnail updated for {restriction.Label} to {restriction.ThumbnailPath}");
            restriction.ThumbnailPath = newPath;
            _saver.Save(this);
            _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Modified, restriction, null));
        }
    }

    /// <summary>
    ///     Begin the editing process, making a clone of the item we want to edit.
    /// </summary>
    public void StartEditing(RestrictionItem item)
        => _itemEditor.StartEditing(Storage, item);

    /// <summary> 
    ///     Cancel the editing process without saving anything.
    /// </summary>
    public void StopEditing()
        => _itemEditor.QuitEditing();

    /// <summary>
    ///     Injects all the changes made to the GagRestriction and applies them to the actual item. <para />
    ///     All changes are saved to the config once this completes.
    /// </summary>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            // _managerCache.UpdateCache(AppliedRestrictions);
            _saver.Save(this);

            _logger.LogTrace("Saved changes to Edited RestrictionItem.");
            _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Modified, sourceItem, null));
        }
    }

    public void ToggleVisibility(Guid restrictionItem)
    {
        if (Storage.TryGetRestriction(restrictionItem, out var item))
        {
            item.IsEnabled = !item.IsEnabled;
            _mediator.Publish(new ConfigRestrictionChanged(StorageChangeType.Modified, item));
            _saver.Save(this);
        }
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    public bool AddFavorite(GarblerRestriction restriction) => _favorites.TryAddGag(restriction.GagType);

    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    public bool RemoveFavorite(GarblerRestriction restriction) => _favorites.RemoveGag(restriction.GagType);

    /// <summary> 
    ///     Applies a cursed items restriction ref to the active restrictions cache. <para />
    ///     If successful, the stored layer is placed inside <paramref name="item"/>.
    /// </summary>
    public bool ApplyCursedItem(CursedRestrictionItem item, out int layer)
    {
        layer = -1;
        if (IsItemApplied(item.Identifier))
            return false;

        _lootItems.TryAdd(item.Identifier, item.RefItem);
        layer = GenCursedKeyId(item.Precedence);
        _idToLayerMap.TryAdd(item.RefItem.Identifier, layer);
        _logger.LogInformation($"Added {item.Label}'s Restriction ({item.RefItem.Label}) to idx [{layer}]");
        return true;
    }

    /// <summary> Not the most pretty way to handle this, but it works, and it's functional. </summary>
    public bool RemoveCursedItem(CursedRestrictionItem item, out int remLayer)
    {
        remLayer = -1;
        if (!_lootItems.ContainsKey(item.Identifier))
            return false;

        _lootItems.Remove(item.Identifier);
        _idToLayerMap.Remove(item.RefItem.Identifier, out remLayer);
        _logger.LogInformation($"Removed {item.Label}'s Restriction ({item.RefItem.Label}) from idx [{remLayer}]");
        return true;
    }

    public bool CanApply(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanApply();
    public bool CanLock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanLock();
    public bool CanUnlock(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanUnlock();
    public bool CanRemove(int layer) => _serverRestrictionData is { } d && d.Restrictions[layer].CanRemove();

    #region Active Restriction Updates
    public bool ApplyRestriction(int layer, ActiveRestriction newData, string enactor, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = null;

        if (_serverRestrictionData is not { } data)
            return false;

        // update the values and fire achievement ping. ( None yet )
        data.Restrictions[layer].Identifier = newData.Identifier;
        data.Restrictions[layer].Enabler = newData.Enabler;
        // Invoke the Mediator of this change.
        _mediator.Publish(new RestrictionStateChanged(NewState.Enabled, layer, data.Restrictions[layer], enactor, MainHub.UID));

        // assign the information if present.
        if (Storage.TryGetRestriction(newData.Identifier, out item))
        {
            _activeItems[layer] = item;
            _idToLayerMap[newData.Identifier] = layer;
            return true;
        }

        return false;
    }

    public void LockRestriction(int layer, ActiveRestriction newData, string enactor)
    {
        if (_serverRestrictionData is not { } data)
            return;

        data.Restrictions[layer].Padlock = newData.Padlock;
        data.Restrictions[layer].Password = newData.Password;
        data.Restrictions[layer].Timer = newData.Timer;
        data.Restrictions[layer].PadlockAssigner = newData.PadlockAssigner;
        // Invoke the Mediator of this change.
        _mediator.Publish(new RestrictionStateChanged(NewState.Locked, layer, data.Restrictions[layer], enactor, MainHub.UID));
    }

    public void UnlockRestriction(int layer, string enactor)
    {
        if (_serverRestrictionData is not { } data)
            return;

        var prev = data.Restrictions[layer] with { };

        data.Restrictions[layer].Padlock = Padlocks.None;
        data.Restrictions[layer].Password = string.Empty;
        data.Restrictions[layer].Timer = DateTimeOffset.MinValue;
        data.Restrictions[layer].PadlockAssigner = string.Empty;
        // Invoke the Mediator of this change.
        _mediator.Publish(new RestrictionStateChanged(NewState.Unlocked, layer, prev, enactor, MainHub.UID));
    }

    public bool RemoveRestriction(int layer, string enactor, [NotNullWhen(true)] out RestrictionItem? item)
    {
        item = null;

        if (_serverRestrictionData is not { } data)
            return false;

        var prev = data.Restrictions[layer] with { };

        // Update data
        data.Restrictions[layer].Identifier = Guid.Empty;
        data.Restrictions[layer].Enabler = string.Empty;
        // Inform mediator of this change.
        _mediator.Publish(new RestrictionStateChanged(NewState.Disabled, layer, prev, enactor, MainHub.UID));

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetRestriction(prev.Identifier, out item))
        {
            _activeItems.Remove(layer);
            _idToLayerMap.Remove(prev.Identifier);
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
            _logger.LogDebug("Serializing item: " + item.ToString());
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
        _logger.LogInformation("Loading in Restrictions Config for file: " + file);
        Storage.Clear();
        if (!File.Exists(file))
        {
            _logger.LogWarning("No Restrictions file found at {0}", file);
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
                _logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GSModule.Restriction));
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
                _logger.LogError($"Unknown RestrictionType: {typeString}");
                continue;
            }

            try
            {
                // Create an instance of the correct type
                var restrictionItem = restrictionType switch
                {
                    RestrictionType.Hypnotic => HypnoticRestriction.FromToken(itemJson, _modPresets),
                    RestrictionType.Blindfold => BlindfoldRestriction.FromToken(itemJson, _modPresets),
                    _ => RestrictionItem.FromToken(itemJson, _modPresets),
                };
                Storage.Add(restrictionItem);
            }
            catch (Bagagwa ex)
            {
                _logger.LogError(ex, "Failed to load restriction item from JSON: {0}", itemJson);
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    #endregion HybridSavable
}
