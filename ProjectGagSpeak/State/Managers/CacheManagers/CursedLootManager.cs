using CkCommons.Helpers;
using CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;

public sealed class CursedLootManager : IHybridSavable
{
    private readonly ILogger<CursedLootManager> _logger;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _mainConfig;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<CursedItem> _itemEditor = new();

    public CursedLootManager(ILogger<CursedLootManager> logger, GagspeakMediator mediator,
        MainConfig config, GagRestrictionManager gags, RestrictionManager restrictions,
        FavoritesManager favorites, ConfigFileProvider fileNames, HybridSaveService saver)
    {
        _logger = logger;
        _mediator = mediator;
        _mainConfig = config;
        _gags = gags;
        _restrictions = restrictions;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;
    }

    public CursedLootStorage Storage { get; private set; } = new CursedLootStorage();
    public CursedItem? ItemInEditor => _itemEditor.ItemInEditor;
    public IEnumerable<CursedItem> AppliedCursedItems => Storage.ActiveItems;

    #region Generic Methods
    public CursedItem CreateNew(string lootName)
    {
        lootName = RegexEx.EnsureUniqueName(lootName, Storage, (t) => t.Label);
        var newItem = new CursedGagItem() 
        { 
            Label = lootName,
            RefItem = _gags.Storage.Values.First() // Default to BallGag.
        };
        _logger.LogInformation("Created new cursed item: " + lootName, LoggerType.CursedItems);
        Storage.Add(newItem);
        _saver.Save(this);
        _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Created, newItem, null));
        return newItem;
    }

    public CursedItem CreateClone(CursedItem other, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        CursedItem clonedItem = other switch
        {
            CursedGagItem cgi => new CursedGagItem(cgi, false) { Label = newName },
            CursedRestrictionItem cri => new CursedRestrictionItem(cri, false) { Label = newName },
            _ => throw new NotImplementedException("Unknown Cursted Item type."),
        };
        Storage.Add(clonedItem);
        _saver.Save(this);

        _logger.LogInformation("Created new cursed item: " + newName, LoggerType.CursedItems);
        _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }

    public void ChangeCursedLootType(CursedItem oldCursedItem, CursedLootKind newType)
    {
        if (ItemInEditor is null || (newType is CursedLootKind.Restriction && _restrictions.Storage.Count is 0))
            return;

        CursedItem convertedLoot = newType switch
        {
            CursedLootKind.Gag => new CursedGagItem(oldCursedItem, true) { RefItem = _gags.Storage.Values.First() },
            CursedLootKind.Restriction => new CursedRestrictionItem(oldCursedItem, true) { RefItem = _restrictions.Storage.First() },
            _ => throw new NotImplementedException("Unknown cursed loot type."),
        };

        // Update the editor item to reflect that of the new type.
        _itemEditor.ItemInEditor = convertedLoot;
    }

    public void Rename(CursedItem lootItem, string newName)
    {
        var prevName = lootItem.Label;
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        lootItem.Label = newName;
        _saver.Save(this);

        _logger.LogInformation($"Renamed cursed item: {prevName} to {newName}", LoggerType.CursedItems);
        _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Renamed, lootItem, prevName));
    }

    public void Delete(CursedItem lootItem)
    {
        if (Storage.Remove(lootItem))
        {
            _logger.LogDebug($"Deleted cursed item: {lootItem.Label}.", LoggerType.CursedItems);
            _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Deleted, lootItem, null));
            _saver.Save(this);
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(CursedItem lootItem) => _itemEditor.StartEditing(Storage, lootItem);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            _logger.LogTrace("Saved changes to Edited CursedItem.");
            _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Modified, sourceItem, null));
            _saver.Save(this);
        }
    }

    public void AddFavorite(CursedItem loot) => _favorites.TryAddRestriction(FavoriteIdContainer.CursedLoot, loot.Identifier);
    public void RemoveFavorite(CursedItem loot) => _favorites.RemoveRestriction(FavoriteIdContainer.CursedLoot, loot.Identifier);
    #endregion Generic Methods
    // Called from safeword service, deactivates all items.
    public void InvalidateAllActive()
    {
        foreach (var item in Storage.ActiveItems.ToArray())
        {
            item.AppliedTime = DateTimeOffset.MinValue;
            item.ReleaseTime = DateTimeOffset.MinValue;
        }
        _saver.Save(this);
    }

    public void ForceSave() => _saver.Save(this);

    public void TogglePoolState(CursedItem item)
    {
        item.InPool = !item.InPool;
        _saver.Save(this);
        _mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Modified, item, null));
    }

    // does not relate to the cached item, handle this seperately in the visual listener.
    public void ActivateItem(CursedItem item, DateTimeOffset endTimeUtc)
    {
        item.AppliedTime = DateTimeOffset.UtcNow;
        item.ReleaseTime = endTimeUtc;
        _saver.Save(this);
    }

    public void SetInactive(Guid lootId)
    {
        if (!Storage.TryGetLoot(lootId, out var item))
            return;
        item.AppliedTime = DateTimeOffset.MinValue;
        item.ReleaseTime = DateTimeOffset.MinValue;
        _saver.Save(this);
    }

    public void SetLowerLimit(TimeSpan time)
    {
        LockRangeLower = time;
        _saver.Save(this);
    }

    public void SetUpperLimit(TimeSpan time)
    {
        LockRangeUpper = time;
        _saver.Save(this);
    }

    public void SetLockChance(int chance)
    {
        LockChance = chance;
        _saver.Save(this);
    }

    #region HybridSavable
    public TimeSpan LockRangeLower { get; private set; } = TimeSpan.Zero;
    public TimeSpan LockRangeUpper { get; private set; } = TimeSpan.FromMinutes(1);
    public int LockChance { get; private set; } = 0;

    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = false, files.CursedLoot).Item2;
    public void WriteToStream(StreamWriter writer) => throw new NotImplementedException();
    public string JsonSerialize()
    {
        // Construct the array of CursedLootItems.
        var cursedItems = new JArray();
        foreach (var loot in Storage)
            cursedItems.Add(loot.Serialize());

        // construct the config object to serialize.
        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["LockRangeLower"] = LockRangeLower.ToString(),
            ["LockRangeUpper"] = LockRangeUpper.ToString(),
            ["LockChance"] = LockChance,
            ["CursedItems"] = cursedItems,
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.CursedLoot;
        _logger.LogInformation("Loading in CursedLoot Config for file: " + file);
        
        Storage.Clear();

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
            _logger.LogWarning("Cursed Loot Config file not found. Attempting to find old config.");
            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "cursedloot.json");
            if (File.Exists(oldFormatFile))
            {
                jsonText = File.ReadAllText(oldFormatFile);
                jObject = JObject.Parse(jsonText);
                jObject = ConfigMigrator.MigrateCursedLootConfig(jObject, _fileNames, oldFormatFile);
            }
            else
            {
                _logger.LogWarning("No Config file found for: " + oldFormatFile);
                // create a new file with default values.
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
                LoadV0(jObject);
                break;
            default:
                _logger.LogError("Invalid Version!");
                return;
        }
        // run a save after the load.
        _saver.Save(this);
        _mediator.Publish(new ReloadFileSystem(GagspeakModule.CursedLoot));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JObject cursedLootData)
            return;

        // set the lock range lower and upper.
        LockRangeLower = TimeSpan.TryParse(cursedLootData["LockRangeLower"]?.Value<string>(), out var lower) ? lower : TimeSpan.Zero;
        LockRangeUpper = TimeSpan.TryParse(cursedLootData["LockRangeUpper"]?.Value<string>(), out var upper) ? upper : TimeSpan.FromMinutes(1);
        LockChance = cursedLootData["LockChance"]?.Value<int>() ?? 0;

        // get the array of cursed loot items from the token
        if (cursedLootData["CursedItems"] is not JArray lootItemsList)
            return;

        // load in all the items.
        foreach (var lootItemToken in lootItemsList)
        {
            try
            {
                if (!Enum.TryParse(lootItemToken["Type"]?.ToString(), out CursedLootKind lootType))
                    continue;

                // Otherwise, try and parse it out.
                CursedItem? lootAbstract = lootType switch
                {
                    CursedLootKind.Gag => LoadCursedGag(lootItemToken),
                    CursedLootKind.Restriction => LoadCursedRestriction(lootItemToken),
                    _ => throw new NotImplementedException("Unknown Cursed Loot Type found during load."),
                };
                // if valid, add it.
                if (lootAbstract is not null)
                    Storage.Add(lootAbstract);
            }
            catch (Bagagwa ex)
            {
                _logger.LogError($"Failed to load in a cursed loot item. removing item!: {ex}");
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    // Move this into the cursed loot manager.
    public CursedGagItem? LoadCursedGag(JToken? lootObject)
    {
        if (lootObject is not JObject token)
            return null;

        try
        {
            var applyTime = token["AppliedTime"]?.Value<DateTime>() ?? DateTime.MinValue;
            var releaseTime = token["ReleaseTime"]?.Value<DateTime>() ?? DateTime.MinValue;
            // get the gag by the gagtype.
            if (token["GagRef"] is not JValue gagRefValue)
                return null;

            if (!Enum.TryParse<GagType>(gagRefValue.Value<string>(), out var gagType))
                throw new Exception("Invalid Gag Reference!");

            var gagRef = _gags.Storage[gagType];
            // Initialize CursedItem
            var item = new CursedGagItem()
            {
                Identifier = token["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
                Label = token["Label"]?.Value<string>() ?? string.Empty,
                InPool = token["InPool"]?.Value<bool>() ?? false,
                AppliedTime = new DateTimeOffset(applyTime, TimeSpan.Zero),
                ReleaseTime = new DateTimeOffset(releaseTime, TimeSpan.Zero),
                Precedence = Enum.TryParse<Precedence>(token["Precedence"]?.Value<string>(), out var precedence) ? precedence : Precedence.Default,
                RefItem = gagRef
            };
            return item;
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Failed to deserialize loot item: {ex}");
            return null;
        }
    }

    public CursedRestrictionItem? LoadCursedRestriction(JToken? lootObject)
    {
        if (lootObject is not JObject token)
            return null;
        try
        {
            var applyTime = token["AppliedTime"]?.Value<DateTime>() ?? DateTime.MinValue;
            var releaseTime = token["ReleaseTime"]?.Value<DateTime>() ?? DateTime.MinValue;
            // get the restriction by the GUID.
            if (token["RestrictionRef"] is not JValue restRefValue)
                return null;

            if (!Guid.TryParse(restRefValue.Value<string>(), out var restGuid))
                throw new Exception("Invalid Restriction Reference!");
            if (!_restrictions.Storage.TryGetRestriction(restGuid, out var restRef))
                throw new Exception("Failed to find Restriction Reference!");
            // Initialize CursedItem
            var item = new CursedRestrictionItem()
            {
                Identifier = token["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
                Label = token["Label"]?.Value<string>() ?? string.Empty,
                InPool = token["InPool"]?.Value<bool>() ?? false,
                AppliedTime = new DateTimeOffset(applyTime, TimeSpan.Zero),
                ReleaseTime = new DateTimeOffset(releaseTime, TimeSpan.Zero),
                Precedence = Enum.TryParse<Precedence>(token["Precedence"]?.Value<string>(), out var precedence) ? precedence : Precedence.Default,
                RefItem = restRef
            };
            return item;
        }
        catch (Bagagwa ex)
        {
            _logger.LogError($"Failed to deserialize loot item: {ex}");
            return null;
        }
    }


    #endregion HybridSavable
}
