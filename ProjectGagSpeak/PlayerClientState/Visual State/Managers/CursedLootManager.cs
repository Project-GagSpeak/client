using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Storage;
using GagSpeak.PlayerState.Components;
using GagSpeak.PlayerState.Models;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.PlayerState.Visual;

public sealed class CursedLootManager : DisposableMediatorSubscriberBase, IVisualManager, IHybridSavable
{
    private readonly GlobalData _globals;
    private readonly GagRestrictionManager _gags;
    private readonly RestrictionManager _restrictions;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    public CursedLootManager(ILogger<CursedLootManager> logger, GagspeakMediator mediator,
        GlobalData clientData, GagRestrictionManager gags, RestrictionManager restrictions,
        FavoritesManager favorites, ConfigFileProvider fileNames, HybridSaveService saver) 
        : base(logger, mediator)
    {
        _globals = clientData;
        _gags = gags;
        _restrictions = restrictions;
        _favorites = favorites;
        _fileNames = fileNames;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => CheckLockedItems());
    }

    // Cached Information.
    public CursedItem? ActiveEditorItem { get; private set; }
    public VisualAdvancedRestrictionsCache LatestVisualCache { get; private set; } = new();

    // Stored Information.
    public IReadOnlyList<CursedItem> ActiveCursedItems => Storage.ActiveItems;
    public CursedLootStorage Storage { get; private set; } = new CursedLootStorage();

    public void OnLogin() { }

    public void OnLogout() { }

    public CursedItem CreateNew(string lootName)
    {
        var newItem = new CursedItem()
        {
            Label = lootName
        };
        // Append the item to the storage.
        Storage.Add(newItem);
        Logger.LogInformation("Created new cursed item: " + lootName, LoggerType.CursedLoot);
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageItemChangeType.Created, newItem, null));
        return newItem;
    }

    public CursedItem CreateClone(CursedItem clone, string newName)
    {
        // generate a new design based off the passed in clone. Be sure to give it a new identifier after.
        var newItem = new CursedItem(clone, false)
        {
            Label = newName
        };

        // Append the item to the storage.
        Storage.Add(newItem);
        Logger.LogInformation("Created new cursed item: " + newName, LoggerType.CursedLoot);
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageItemChangeType.Created, newItem, null));
        return newItem;
    }

    public void Delete(CursedItem lootItem)
    {
        // should never be able to remove active restrictions, but if that happens to occur, add checks here.
        if (Storage.Remove(lootItem))
        {
            Logger.LogDebug($"Deleted cursed item: {lootItem.Label}.", LoggerType.CursedLoot);
            Mediator.Publish(new ConfigCursedItemChanged(StorageItemChangeType.Deleted, lootItem, null));
            _saver.Save(this);
        }
    }

    public void Rename(CursedItem lootItem, string newName)
    {
        var oldName = lootItem.Label;
        if (oldName == newName || string.IsNullOrWhiteSpace(newName))
            return;

        lootItem.Label = newName;
        Logger.LogInformation("Renamed cursed item: " + oldName + " to " + newName, LoggerType.CursedLoot);
        _saver.Save(this);
        Mediator.Publish(new ConfigCursedItemChanged(StorageItemChangeType.Renamed, lootItem, oldName));
    }

    public void StartEditing(CursedItem lootItem)
    {
        // create an exact clone of the passed in cursed item for editing, so long as it exists in storage.
        if (Storage.Contains(lootItem))
        {
            ActiveEditorItem = new CursedItem(lootItem, true);
        }
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing()
        => ActiveEditorItem = null;

    /// <summary> Injects all the changes made to the Cursed Loot and applies them to the actual item. </summary>
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
            Mediator.Publish(new ConfigCursedItemChanged(StorageItemChangeType.Modified, item, null));
            _saver.Save(this);
        }
    }

    public void AddFavorite(CursedItem cursedLoot)
        => _favorites.TryAddRestriction(FavoriteIdContainer.CursedLoot, cursedLoot.Identifier);

    public void RemoveFavorite(CursedItem cursedLoot)
        => _favorites.RemoveRestriction(FavoriteIdContainer.CursedLoot, cursedLoot.Identifier);


    public void ActivateCursedItem(CursedItem item, DateTimeOffset endTimeUtc)
    {
        if (!Storage.Contains(item))
        {
            Logger.LogError("Attempted to activate a cursed item that does not exist in storage!");
            return;
        }

        item.AppliedTime = DateTimeOffset.UtcNow;
        item.ReleaseTime = endTimeUtc;
        _saver.Save(this);

        // if it was a restriction manager, be sure to apply its item.
        if (item.RestrictionRef is RestrictionItem nonGagRestriction)
            _restrictions.AddOccupiedRestriction(nonGagRestriction, ManagerPriority.CursedLoot);
    }

    // Scan by id so we dont spam deactivation.
    public void DeactivateCursedItem(Guid lootId)
    {
        if(Storage.ByIdentifier(lootId) is not { } item)
            return;

        item.AppliedTime = DateTimeOffset.MinValue;
        item.ReleaseTime = DateTimeOffset.MinValue;
        _saver.Save(this);

        // if it was a restriction manager, be sure to remove its item.
        if (item.RestrictionRef is RestrictionItem nonGagRestriction)
            _restrictions.RemoveOccupiedRestriction(nonGagRestriction, ManagerPriority.CursedLoot);
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

    private void Load()
    {
        var file = _fileNames.CursedLoot;
        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No GagRestrictions file found at {0}", file);
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
                LoadV0(jObject);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
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
                if (Guid.TryParse(restrictionString, out var restrictionGuid))
                {
                    if(_restrictions.Storage.ByIdentifier(restrictionGuid) is not { } restrictionItem)
                        throw new Exception("Failed to retrieve restriction. Identifier not valid in storage!");
                    // If valid, assign the IRestriction as a ref to the restriction item.
                    restrictionRef = restrictionItem;
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
            StaticLogger.Logger.LogError($"Failed to deserialize loot item: {ex}");
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
            if (item.ReleaseTime - DateTimeOffset.UtcNow <= TimeSpan.Zero) { }
        //DeactivateCursedItem(item.Identifier).ConfigureAwait(false);
    }
}
