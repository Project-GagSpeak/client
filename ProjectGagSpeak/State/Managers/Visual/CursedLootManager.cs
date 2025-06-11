using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.FileSystems;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerState.Visual;

public sealed class CursedLootManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly MainConfigService _mainConfig;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<CursedItem> _itemEditor = new();

    public CursedLootManager(ILogger<CursedLootManager> logger, GagspeakMediator mediator,
        MainConfigService config, GagRestrictionManager gags, RestrictionManager restrictions,
        FavoritesManager favorites, ConfigFileProvider fileNames, HybridSaveService saver)
        : base(logger, mediator)
    {
        _mainConfig = config;
        _gags = gags;
        _restrictions = restrictions;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckLockedItems());
    }

    public CursedLootStorage Storage { get; private set; } = new CursedLootStorage();
    public CursedItem? ItemInEditor => _itemEditor.ItemInEditor;
    public IEnumerable<CursedItem> AppliedCursedItems => Storage.ActiveItems;

    #region Generic Methods
    public CursedItem CreateNew(string lootName)
    {
        var newItem = new CursedItem()
        {
            Label = lootName,
            RestrictionRef = _gags.Storage.Values.First() // Default to BallGag.
        };
        // Append the item to the storage.
        Storage.Add(newItem);
        Logger.LogInformation("Created new cursed item: " + lootName, LoggerType.CursedItems);
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Created, newItem, null));
        return newItem;
    }

    public CursedItem CreateClone(CursedItem clone, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);
        var newItem = new CursedItem(clone, false) { Label = newName };
        Storage.Add(newItem);
        _saver.Save(this);

        Logger.LogDebug("Created new cursed item: " + newName, LoggerType.CursedItems);
        Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Created, newItem, null));
        return newItem;
    }

    public void Delete(CursedItem lootItem)
    {
        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(lootItem))
        {
            Logger.LogDebug($"Deleted cursed item: {lootItem.Label}.", LoggerType.CursedItems);
            Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Deleted, lootItem, null));
            _saver.Save(this);
        }
    }

    public void Rename(CursedItem lootItem, string newName)
    {
        newName = RegexEx.EnsureUniqueName(newName, Storage, x => x.Label);

        var oldName = lootItem.Label;
        if (oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        lootItem.Label = newName;
        Logger.LogInformation("Renamed cursed item: " + oldName + " to " + newName, LoggerType.CursedItems);
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Renamed, lootItem, oldName));
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
            // _managerCache.UpdateCache(AppliedCursedItems, _mainConfig.Config.CursedItemsApplyTraits);
            _saver.Save(this);

            Logger.LogTrace("Saved changes to Edited CursedItem.");
            Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Modified, sourceItem, null));
        }
    }

    public void TogglePoolState(CursedItem item)
    {
        item.InPool = !item.InPool;
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageChangeType.Modified, item, null));
    }

    public void AddFavorite(CursedItem loot) => _favorites.TryAddRestriction(FavoriteIdContainer.CursedLoot, loot.Identifier);
    public void RemoveFavorite(CursedItem loot) => _favorites.RemoveRestriction(FavoriteIdContainer.CursedLoot, loot.Identifier);
    #endregion Generic Methods

    public void ActivateCursedItem(CursedItem item, DateTimeOffset endTimeUtc)
    {
        item.AppliedTime = DateTimeOffset.UtcNow;
        item.ReleaseTime = endTimeUtc;
        _saver.Save(this);

        // if it was a restriction manager, be sure to apply its item.
        if (item.RestrictionRef is RestrictionItem nonGagRestriction)
            _restrictions.AddOccupiedRestriction(nonGagRestriction, ManagerPriority.CursedLoot);

        // Update the cache regardless.
        // _managerCache.UpdateCache(AppliedCursedItems, _mainConfig.Config.CursedItemsApplyTraits);
    }

    // Scan by id so we dont spam deactivation.
    public void DeactivateCursedItem(Guid lootId)
    {
        if(!Storage.TryGetLoot(lootId, out var item))
            return;

        item.AppliedTime = DateTimeOffset.MinValue;
        item.ReleaseTime = DateTimeOffset.MinValue;
        _saver.Save(this);

        // if it was a restriction manager, be sure to remove its item.
        if (item.RestrictionRef is RestrictionItem nonGagRestriction)
            _restrictions.RemoveOccupiedRestriction(nonGagRestriction, ManagerPriority.CursedLoot);

        // _managerCache.UpdateCache(AppliedCursedItems, _mainConfig.Config.CursedItemsApplyTraits);
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
            ["CursedItems"] = cursedItems,
            ["LockRangeLower"] = LockRangeLower.ToString(),
            ["LockRangeUpper"] = LockRangeUpper.ToString(),
            ["LockChance"] = LockChance
        }.ToString(Formatting.Indented);
    }

    public void Load()
    {
        var file = _fileNames.CursedLoot;
        Logger.LogInformation("Loading in CursedLoot Config for file: " + file);
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
            GagSpeak.StaticLog.Warning("Cursed Loot Config file not found. Attempting to find old config.");
            var oldFormatFile = Path.Combine(_fileNames.CurrentPlayerDirectory, "cursedloot.json");
            if (File.Exists(oldFormatFile))
            {
                jsonText = File.ReadAllText(oldFormatFile);
                jObject = JObject.Parse(jsonText);
                jObject = ConfigMigrator.MigrateCursedLootConfig(jObject, _fileNames, oldFormatFile);
            }
            else
            {
                GagSpeak.StaticLog.Warning("No Config file found for: " + oldFormatFile);
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
                Logger.LogError("Invalid Version!");
                return;
        }
        // run a save after the load.
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.CursedLoot));
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
        foreach (var cursedItem in lootItemsList)
        {
            var readCursedItem = new CursedItem();
            if (TryLoadLootItem(cursedItem, out var item))
            {
                Storage.Add(item);
            }
        }
    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    // Move this into the cursed loot manager.
    public bool TryLoadLootItem(JToken? lootObject, [NotNullWhen(true)] out CursedItem? item)
    {
        item = null;
        if (lootObject is not JObject jsonObject)
            return false;

        try
        {
            var applyTime = jsonObject["AppliedTime"]?.Value<DateTime>() ?? DateTime.MinValue;
            var releaseTime = jsonObject["ReleaseTime"]?.Value<DateTime>() ?? DateTime.MinValue;

            // Attempt to deserialize RestrictionRef
            IRestriction? restrictionRef = null;
            if (jsonObject["RestrictionRef"] is JValue restrictionValue)
            {
                var restrictionString = restrictionValue.Value<string>();
                if (Guid.TryParse(restrictionString, out var refGuid))
                {
                    if(_restrictions.Storage.TryGetRestriction(refGuid, out var match))
                        restrictionRef = match;
                }
                else if (Enum.TryParse<GagType>(restrictionString, out var gagType))
                {
                    // Assign it as a ref to the gag type.
                    restrictionRef = _gags.Storage[gagType];
                }
                else
                {
                    throw new Exception("Invalid Restriction Reference!");
                }
            }
            else
            {
                throw new Exception("No Restriction Reference was stored here!");
            }

            // Initialize CursedItem
            item = new CursedItem()
            {
                Identifier = jsonObject["Identifier"]?.ToObject<Guid>() ?? throw new ArgumentNullException("Identifier"),
                Label = jsonObject["Label"]?.Value<string>() ?? string.Empty,
                InPool = jsonObject["InPool"]?.Value<bool>() ?? false,
                AppliedTime = new DateTimeOffset(applyTime, TimeSpan.Zero),
                ReleaseTime = new DateTimeOffset(releaseTime, TimeSpan.Zero),
                CanOverride = jsonObject["CanOverride"]?.Value<bool>() ?? false,
                Precedence = Enum.TryParse<Precedence>(jsonObject["Precedence"]?.Value<string>(), out var precedence) ? precedence : Precedence.Default,
                RestrictionRef = restrictionRef
            };

            return true;
        }
        catch (Exception ex)
        {
            GagSpeak.StaticLog.Error($"Failed to deserialize loot item: {ex}");
            return false;
        }
    }


    #endregion HybridSavable

    // This might work better in the listener but im not sure.
    private void CheckLockedItems()
    {
        if (!Storage.ActiveItems.Any())
            return;

        foreach (var item in Storage.ActiveItems)
        {
            if (item.ReleaseTime - DateTimeOffset.UtcNow <= TimeSpan.Zero)
            {
                // This should be calling to the server, not from here.
                DeactivateCursedItem(item.Identifier);
            }
        }
    }
}
