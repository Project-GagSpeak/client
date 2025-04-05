using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
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
using OtterGui.Classes;
using Penumbra.GameData.Enums;
using System.Linq;

namespace GagSpeak.PlayerState.Visual;

public sealed class GagRestrictionManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly GagGarbler _garbler;
    private readonly GlobalData _clientData;
    private readonly FavoritesManager _favorites;
    private readonly ConfigFileProvider _fileNames;
    private readonly ItemService _items;
    private readonly HybridSaveService _saver;

    public GagRestrictionManager(ILogger<GagRestrictionManager> logger, GagspeakMediator mediator,
        GagGarbler garbler, GlobalData clientData, FavoritesManager favorites, 
        ConfigFileProvider fileNames, ItemService items, HybridSaveService saver) : base(logger, mediator)
    {
        _garbler = garbler;
        _clientData = clientData;
        _favorites = favorites;
        _fileNames = fileNames;
        _items = items;
        _saver = saver;
    }

    // Cached Information
    public GarblerRestriction? ActiveEditorItem { get; private set; }
    public VisualAdvancedRestrictionsCache LatestVisualCache { get; private set; } = new();
    // Using a sorted list over a fixed array size, as at any point a Garbler restriction on any layer could not exist.
    public SortedList<int, GarblerRestriction> ActiveRestrictions { get; private set; } = new();

    // Stored Information.
    public CharaActiveGags? ActiveGagsData { get; private set; }
    public GagRestrictionStorage Storage { get; private set; } = new GagRestrictionStorage();


    /// <summary> Updates the manager with the latest data from the server. </summary>
    /// <param name="serverData"> The data from the server to update with. </param>
    /// <remarks> MUST CALL AFTER LOADING PROFILE STORAGE. (Also updates cache and active restrictions. </remarks>
    public void LoadServerData(CharaActiveGags serverData)
    {
        ActiveGagsData = serverData;
        for (var slotIdx = 0; slotIdx < serverData.GagSlots.Length; slotIdx++)
        {
            var slot = serverData.GagSlots[slotIdx];
            if (slot.GagItem is not GagType.None)
            {
                if (Storage.TryGetEnabledGag(slot.GagItem, out var item))
                {
                    ActiveRestrictions[slotIdx] = item;
                }
            }
        }
        LatestVisualCache.UpdateCache(ActiveRestrictions);
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    /// <param name="gagType"> The GagType to get the GagRestriction of for editing. </param>
    public void StartEditing(GarblerRestriction restriction)
    {
        if (restriction is not null && restriction.GagType is not GagType.None)
            ActiveEditorItem = new GarblerRestriction(restriction);
    }

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() 
        => ActiveEditorItem = null;

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (ActiveEditorItem is null)
            return;
        // Update the active restriction with the new data, update the cache, and clear the edited restriction.
        Storage[ActiveEditorItem.GagType] = ActiveEditorItem;
        LatestVisualCache.UpdateCache(ActiveRestrictions);
        ActiveEditorItem = null;
        _saver.Save(this);
    }

    /// <summary> Attempts to add the gag restriction as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool AddFavorite(GarblerRestriction restriction) 
        => _favorites.TryAddGag(restriction.GagType);
    /// <summary> Attempts to remove the gag restriction as a favorite. </summary>
    /// <returns> True if successful, false otherwise. </returns>
    public bool RemoveFavorite(GarblerRestriction restriction) 
        => _favorites.RemoveGag(restriction.GagType);

    #region Validators
    public bool CanApply(int layer, GagType newGag)
    {
        if (ActiveGagsData is { } data && data.GagSlots[layer].CanApply())
            return true;
        Logger.LogTrace("Not able to Apply at this time due to errors!");
        return false;
    }

    public bool CanLock(int layer)
    {
        if (ActiveGagsData is { } data && data.GagSlots[layer].CanLock())
            return true;
        Logger.LogTrace("Not able to Lock at this time due to errors!");
        return false;
    }

    public bool CanUnlock(int layer)
    {
        if (ActiveGagsData is { } data && data.GagSlots[layer].CanUnlock())
            return true;
        Logger.LogTrace("Not able to Unlock at this time due to errors!");
        return false;
    }

    public bool CanRemove(int layer)
    {
        if (ActiveGagsData is { } data && data.GagSlots[layer].CanRemove())
            return true;
        Logger.LogTrace("Not able to Remove at this time due to errors!");
        return false;
    }
    #endregion Validators

    #region Performers
    public VisualUpdateFlags ApplyGag(int layer, GagType newGag, string enactor, out GarblerRestriction? item)
    {
        item = null; var flags = VisualUpdateFlags.None;

        if (ActiveGagsData is not { } data)
            return flags;

        // update values & Garbler, then fire achievement ping.
        data.GagSlots[layer].GagItem = newGag;
        data.GagSlots[layer].Enabler = enactor;
        _garbler.UpdateGarblerLogic(data.CurrentGagNames());
        UnlocksEventManager.AchievementEvent(UnlocksEvent.GagStateChange, true, layer, newGag, enactor);

        // Mark what parts of this item will end up having effective changes.
        if (Storage.TryGetEnabledGag(newGag, out item))
        {
            // Begin by assuming everything changes.
            flags = VisualUpdateFlags.AllGag;

            // Look over this later but some logic may be flawed.
            foreach (var restriction in ActiveRestrictions)
            {
                // these properties should not be updated if an item with higher priority contains it.
                if (restriction.Key > layer)
                {
                    if (item.Glamour is not null && restriction.Value.Glamour.Slot == item.Glamour.Slot)
                        flags &= ~VisualUpdateFlags.Glamour;

                    if (item.Mod is not null && restriction.Value.Mod.ModInfo == item.Mod.ModInfo)
                        flags &= ~VisualUpdateFlags.Mod;

                    if(restriction.Value.HeadgearState != OptionalBool.Null)
                        flags &= ~VisualUpdateFlags.Helmet;

                    if(restriction.Value.VisorState != OptionalBool.Null)
                        flags &= ~VisualUpdateFlags.Visor;
                }

                // these properties should not be updated if any item contains it.
                if (restriction.Value.Moodle == item.Moodle)
                    flags &= ~VisualUpdateFlags.Moodle;

                if (restriction.Value.ProfileGuid == item.ProfileGuid && restriction.Value.ProfilePriority >= item.ProfilePriority)
                    flags &= ~VisualUpdateFlags.CustomizeProfile;
            }
            // Update the activeVisualState, and the cache.
            ActiveRestrictions[layer] = item;
            LatestVisualCache.UpdateCache(ActiveRestrictions);
        }
        return flags;
    }

    public void LockGag(int layer, Padlocks padlock, string pass, DateTimeOffset timer, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (ActiveGagsData is not { } data)
            return;

        data.GagSlots[layer].Padlock = padlock;
        data.GagSlots[layer].Password = pass;
        data.GagSlots[layer].Timer = timer;
        data.GagSlots[layer].PadlockAssigner = enactor;
        // Fire that the gag was locked for this layer.
        UnlocksEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, true, layer, padlock, enactor);
    }

    public void UnlockGag(int layer, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (ActiveGagsData is not { } data)
            return;

        var prevLock = data.GagSlots[layer].Padlock;

        data.GagSlots[layer].Padlock = Padlocks.None;
        data.GagSlots[layer].Password = string.Empty;
        data.GagSlots[layer].Timer = DateTimeOffset.MinValue;
        data.GagSlots[layer].PadlockAssigner = string.Empty;
        // Fire that the gag was unlocked for this layer.
        UnlocksEventManager.AchievementEvent(UnlocksEvent.GagLockStateChange, false, layer, prevLock, enactor);
    }

    public VisualUpdateFlags RemoveGag(int layer, string enactor, out GarblerRestriction? item)
    {
        item = null; var flags = VisualUpdateFlags.None;

        if (ActiveGagsData is not { } data)
            return flags;

        // store what gag we are removing, then update data and fire achievement ping.
        var removedGag = data.GagSlots[layer].GagItem;
        data.GagSlots[layer].GagItem = GagType.None;
        data.GagSlots[layer].Enabler = string.Empty;
        _garbler.UpdateGarblerLogic(data.CurrentGagNames());
        UnlocksEventManager.AchievementEvent(UnlocksEvent.GagStateChange, false, layer, removedGag, enactor);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetEnabledGag(removedGag, out var matchedItem))
        {
            // Do recalculations first since it doesnt madder here.
            ActiveRestrictions.Remove((int)layer);
            LatestVisualCache.UpdateCache(ActiveRestrictions);

            // begin by assuming all aspects are removed.
            flags = VisualUpdateFlags.AllGag;
            // Glamour Item will always be valid so don't worry about it.
            if(matchedItem.Mod is null) flags &= ~VisualUpdateFlags.Mod;
            if(matchedItem.HeadgearState == OptionalBool.Null) flags &= ~VisualUpdateFlags.Helmet;
            if(matchedItem.VisorState == OptionalBool.Null) flags &= ~VisualUpdateFlags.Visor;
            if(matchedItem.Moodle is null) flags &= ~VisualUpdateFlags.Moodle;
            if(matchedItem.ProfileGuid == Guid.Empty) flags &= ~VisualUpdateFlags.CustomizeProfile;
        }
        return flags;
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

        foreach (var (gagtype, gagData) in Storage)
            gagRestrictions[gagtype.GagName()] = gagData.Serialize();

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

        string jsonText = "";
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
        Mediator.Publish(new ReloadFileSystem(ModuleSection.Gag));
    }

    private void LoadV0(JToken? data)
    {
        if(data is not JObject sortedListData)
            return;

        // otherwise, parse it out and stuff YIPPEE
        foreach (var (gagName, gagData) in sortedListData)
        {
            var gagType = gagName.ToGagType();
            if (gagType == GagType.None)
            {
                Logger.LogWarning("Invalid GagType: {0}", gagName);
                continue;
            }

            if (gagData is not JObject gagObject)
            {
                Logger.LogWarning("Invalid GagData: {0}", gagName);
                continue;
            }

            var garblerRestriction = new GarblerRestriction(gagType);
            garblerRestriction.LoadRestriction(gagObject, _items);
            Storage[gagType] = garblerRestriction;
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

        if (ActiveGagsData is null || !ActiveGagsData.AnyGagLocked())
            return;

        foreach(var (gagSlot, index) in ActiveGagsData.GagSlots.Select((slot, index) => (slot, index)))
            if (gagSlot.Padlock.IsTimerLock() && gagSlot.HasTimerExpired())
            {
                Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.PadlockHandling);
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
