using GagSpeak.CkCommons;
using GagSpeak.CkCommons.Helpers;
using GagSpeak.CkCommons.HybridSaver;
using GagSpeak.CkCommons.Newtonsoft;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters.Storage;
using GagSpeak.State;
using GagSpeak.Services;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using Penumbra.GameData.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.State.Managers;
public sealed class RestraintManager : DisposableMediatorSubscriberBase, IHybridSavable
{
    private readonly RestrictionManager _restrictions;
    private readonly ModSettingPresetManager _modPresets;
    private readonly FavoritesManager _favorites;
    private readonly ItemService _items;
    private readonly ConfigFileProvider _fileNames;
    private readonly HybridSaveService _saver;

    private StorageItemEditor<RestraintSet> _itemEditor = new();
    private CharaActiveRestraint? _serverRestraintData = null;

    public RestraintManager(ILogger<RestraintManager> logger, GagspeakMediator mediator,
        RestrictionManager restrictions, ModSettingPresetManager modPresets,
        FavoritesManager favorites, ItemService items, ConfigFileProvider fileNames,
        HybridSaveService saver) : base(logger, mediator)
    {
        _restrictions = restrictions;
        _modPresets = modPresets;
        _favorites = favorites;
        _items = items;
        _fileNames = fileNames;
        _saver = saver;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => CheckForExpiredLocks());
    }

    public CharaActiveRestraint? ServerRestraintData => _serverRestraintData;
    public RestraintStorage Storage { get; private set; } = new RestraintStorage();
    public RestraintSet? ItemInEditor => _itemEditor.ItemInEditor;
    public RestraintSet AppliedRestraint { get; private set; } = new RestraintSet(); // Identifiers here dont madder here, so this is fine.

    /// <summary> Updates the manager with the latest data from the server. </summary>
    public void LoadServerData(CharaActiveRestraint serverData)
    {
        _serverRestraintData = serverData;
        if (Storage.TryGetRestraint(serverData.Identifier, out var item))
        {
            AppliedRestraint = item;
        }
    }

    public RestraintSet CreateNew(string restraintName)
    {
        // Ensure that the new name is unique.
        restraintName = RegexEx.EnsureUniqueName(restraintName, Storage, rs => rs.Label);
        var restraint = new RestraintSet { Label = restraintName };
        Storage.Add(restraint);
        _saver.Save(this);
        Logger.LogDebug($"Created new restraint {restraint.Identifier}.");
        Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Created, restraint, null));
        return restraint;
    }

    public RestraintSet CreateClone(RestraintSet clone, string newName)
    {
        // Ensure that the new name is unique.
        newName = RegexEx.EnsureUniqueName(newName, Storage, rs => rs.Label);
        var clonedItem = new RestraintSet(clone, false) { Label = newName };
        Storage.Add(clonedItem);
        _saver.Save(this);
        Logger.LogDebug($"Cloned restraint {clonedItem.Identifier}.");
        Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Created, clonedItem, null));
        return clonedItem;
    }


    public void Delete(RestraintSet restraint)
    {
        // should never be able to remove active restraints, but if that happens to occur, add checks here.
        if (Storage.Remove(restraint))
        {
            Logger.LogDebug($"Deleted restraint {restraint.Identifier}.");
            Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Deleted, restraint, null));
            _saver.Save(this);
        }
    }

    public void Rename(RestraintSet restraint, string newName)
    {
        var oldName = restraint.Label;
        if (oldName == newName)
            return;

        restraint.Label = newName;
        _saver.Save(this);
        Logger.LogDebug($"Renamed restraint {restraint.Identifier}.");
        Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Renamed, restraint, oldName));
    }

    public void UpdateThumbnail(RestraintSet restraint, string newPath)
    {
        // This could have changed by the time this is called, so get it again.
        if(Storage.Contains(restraint))
        {
            Logger.LogDebug($"Thumbnail updated for {restraint.Label} to {restraint.ThumbnailPath}");
            restraint.ThumbnailPath = newPath;
            _saver.Save(this);
            Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Modified, restraint, null));
        }
    }

    /// <summary> Begin the editing process, making a clone of the item we want to edit. </summary>
    public void StartEditing(RestraintSet item) => _itemEditor.StartEditing(Storage, item);

    /// <summary> Cancel the editing process without saving anything. </summary>
    public void StopEditing() => _itemEditor.QuitEditing();

    /// <summary> Injects all the changes made to the GagRestriction and applies them to the actual item. </summary>
    /// <remarks> All changes are saved to the config once this completes. </remarks>
    public void SaveChangesAndStopEditing()
    {
        if (_itemEditor.SaveAndQuitEditing(out var sourceItem))
        {
            Logger.LogDebug($"Saved changes to restraint {sourceItem.Identifier}.");
            // _managerCache.UpdateCache(AppliedRestraint);
            Mediator.Publish(new ConfigRestraintSetChanged(StorageChangeType.Modified, sourceItem));
            _saver.Save(this);
        }
    }

    public void AddFavorite(RestraintSet rs) => _favorites.TryAddRestriction(FavoriteIdContainer.Restraint, rs.Identifier);
    public void RemoveFavorite(RestraintSet rs) => _favorites.RemoveRestriction(FavoriteIdContainer.Restraint, rs.Identifier);

    public bool CanApply(Guid id) => _serverRestraintData is { } d && (d.Identifier == id && d.CanLock());
    public bool CanLock(Guid id) => _serverRestraintData is { } d && (d.Identifier == id && d.CanLock());
    public bool CanUnlock(Guid id) => _serverRestraintData is { } d && (d.Identifier == id && d.CanUnlock());
    public bool CanRemove(Guid id) => _serverRestraintData is { } d && (d.Identifier == id && d.CanRemove());

    #region Active Set Updates
    public bool ApplyRestraint(Guid restraintId, string enactor, [NotNullWhen(true)] out RestraintSet? set)
    {
        set = null;

        if (_serverRestraintData is not { } data)
            return false;

        // update values & ping achievement.
        data.Identifier = restraintId;
        data.Enabler = enactor;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, restraintId, true, enactor);

        // grab the collective data from the set to return.
        if (Storage.TryGetRestraint(restraintId, out set))
        {
            AppliedRestraint = set;
            return true;
        }

        return false;
    }

    public void LockRestraint(Guid restraintId, Padlocks padlock, string pass, DateTimeOffset timer, string enactor)
    {
        if (_serverRestraintData is not { } data)
            return;

        data.Padlock = padlock;
        data.Password = pass;
        data.Timer = timer;
        data.PadlockAssigner = enactor;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, restraintId, padlock, true, enactor);
    }

    public void UnlockRestraint(Guid restraintId, string enactor)
    {
        // Server validated padlock alteration, so simply assign them here and invoke the achievements.
        if (_serverRestraintData is not { } data)
            return;

        var prevLock = data.Padlock;
        var prevAssigner = data.PadlockAssigner;
        data.Padlock = Padlocks.None;
        data.Password = string.Empty;
        data.Timer = DateTimeOffset.MinValue;
        data.PadlockAssigner = string.Empty;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintLockChange, restraintId, prevLock, false, enactor);

        if ((prevAssigner != MainHub.UID) && (enactor != MainHub.UID) && (enactor != prevAssigner))
            UnlocksEventManager.AchievementEvent(UnlocksEvent.SoldSlave);
    }

    public bool RemoveRestraint(string enactor, [NotNullWhen(true)] out RestraintSet? item)
    {
        item = null;

        if (_serverRestraintData is not { } data)
            return false;

        // store the new data, then fire the achievement.
        var removedRestraint = data.Identifier;
        var setEnabler = data.Enabler;
        data.Identifier = Guid.Empty;
        data.Enabler = string.Empty;
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintStateChange, removedRestraint, false, enactor);

        // set was applied by one person and removed by another where neither was the client.
        if ((setEnabler != MainHub.UID) && (enactor != MainHub.UID) && (enactor != setEnabler))
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AuctionedOff);

        // Update the affected visual states, if item is enabled.
        if (Storage.TryGetRestraint(removedRestraint, out item))
        {
            AppliedRestraint = new RestraintSet();
            return true;
        }

        return false;
    }
    #endregion Active Set Updates

    #region HybridSaver
    public int ConfigVersion => 0;
    public HybridSaveType SaveType => HybridSaveType.Json;
    public DateTime LastWriteTimeUTC { get; private set; } = DateTime.MinValue;
    public string GetFileName(ConfigFileProvider files, out bool isAccountUnique)
        => (isAccountUnique = true, files.RestraintSets).Item2;

    public void WriteToStream(StreamWriter writer)
        => throw new NotImplementedException();

    public string JsonSerialize()
    {
        var restraintSets = new JArray();
        foreach (var set in Storage)
        {
            try
            {
                restraintSets.Add(set.Serialize());
            }
            catch (Exception e)
            {
                Logger.LogError(e.InnerException, "Failed to serialize RestraintSet.");
            }
        }

        return new JObject()
        {
            ["Version"] = ConfigVersion,
            ["RestraintSets"] = restraintSets
        }.ToString(Formatting.Indented);
    }

    // our CUSTOM defined load and migration.
    public void Load()
    {
        var file = _fileNames.RestraintSets;
        Logger.LogInformation("Loading in Restraints Config for file: " + file);

        Storage.Clear();
        if (!File.Exists(file))
        {
            Logger.LogWarning("No Restraints Config file found at {0}", file);
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
                LoadV0(jObject["RestraintSets"]);
                break;
            default:
                Logger.LogError("Invalid Version!");
                return;
        }
        _saver.Save(this);
        Mediator.Publish(new ReloadFileSystem(GagspeakModule.Restraint));
    }

    private void LoadV0(JToken? data)
    {
        if (data is not JArray restraintSetList)
            return;

        // otherwise, parse it out and stuff YIPPEE
        foreach (var setToken in restraintSetList)
        {
            if (TryLoadRestraintSet(setToken, out var loadedSet))
            {
                Storage.Add(loadedSet);
            }
            else
            {
                Logger.LogError("Failed to load RestraintSet from JSON.");
            }
        }

    }

    private void MigrateV0toV1(JObject oldConfigJson)
    {
        // update only the version value to 1, then return it.
        oldConfigJson["Version"] = 1;
    }

    private bool TryLoadRestraintSet(JToken token, [NotNullWhen(true)] out RestraintSet? set)
    {
        try
        {
            if (token is not JObject setJson)
                throw new Exception("Invalid JSON Token.");


            // if the setJson's ["RestraintSlots"] value is not a Dictionary<Slot, RestraintSlotBase>, throw.
            if (setJson["RestraintSlots"] is not JObject slotJson)
                throw new Exception("RestraintSlots Dictionary.");

            // Construct the dictionary item for it.
            var slotDict = new Dictionary<EquipSlot, IRestraintSlot>();
            foreach (var slot in slotJson)
            {
                var slotKey = (EquipSlot)Enum.Parse(typeof(EquipSlot), slot.Key);
                var slotValue = LoadSlot(slot.Value, slotKey);
                slotDict.Add(slotKey, slotValue);
            }

            var layers = new List<IRestraintLayer>();
            if (setJson["RestraintLayers"] is JArray layerArray)
            {
                foreach (var layerToken in layerArray)
                    Generic.ExecuteSafely(() => layers.Add(LoadRestraintLayer(layerToken)));
            }

            var restraintMods = new List<ModSettingsPreset>();
            if(setJson["RestraintMods"] is JArray modArray)
            {
                foreach (var modToken in modArray)
                    Generic.ExecuteSafely(() => restraintMods.Add(ModSettingsPreset.FromRefToken(modToken, _modPresets)));
            }

            var restraintMoodles = new HashSet<Moodle>();
            if(setJson["RestraintMoodles"] is JArray moodleArray)
            {
                foreach (var moodleToken in moodleArray)
                    Generic.ExecuteSafely(() => restraintMoodles.Add(JParser.LoadMoodle(moodleToken)));
            }

            set = new RestraintSet()
            {
                Identifier = Guid.TryParse(setJson["Identifier"]?.Value<string>(), out var guid) ? guid : throw new Exception("InvalidGUID"),
                Label = setJson["Label"]?.Value<string>() ?? string.Empty,
                Description = setJson["Description"]?.Value<string>() ?? string.Empty,
                ThumbnailPath = setJson["ThumbnailPath"]?.Value<string>() ?? string.Empty,
                DoRedraw = setJson["DoRedraw"]?.Value<bool>() ?? false,
                RestraintSlots = slotDict,
                Glasses = _items.ParseBonusSlot(setJson["Glasses"]),
                Layers = layers,
                HeadgearState = JParser.FromJObject(setJson["HeadgearState"]),
                VisorState = JParser.FromJObject(setJson["VisorState"]),
                WeaponState = JParser.FromJObject(setJson["WeaponState"]),
                RestraintMods = restraintMods,
                RestraintMoodles = restraintMoodles,
            };
            Logger.LogInformation($"Loaded RestraintSet {set.Label} with {set.RestraintSlots.Count} slots and {set.Layers.Count} layers.");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to load RestraintSet from JSON.");
            set = null;
            return false;
        }
    }

    /// <summary> Attempts to load a restraint set slot item. This can be Basic or Advanced. </summary>
    /// <param name="slotToken"> The JSON Token for the Slot. </param>
    /// <returns> The loaded slot. </returns>
    /// <exception cref="Exception"> If the JToken is either not valid or the GlamourSlot fails to parse. </exception>
    /// <exception cref="InvalidOperationException"> If the JSON Token is missing required information. </exception>
    private IRestraintSlot LoadSlot(JToken? slotToken, EquipSlot equipSlot)
    {
        if (slotToken is not JObject json)
            throw new Exception("Invalid JSON Token for Slot.");

        var typeStr = json["Type"]?.Value<string>() ?? throw new InvalidOperationException("Missing Type information in JSON.");
        if (!Enum.TryParse(typeStr, out RestraintSlotType type))
            throw new InvalidOperationException($"Unknown RestraintSlotType: {typeStr}");

        IRestraintSlot slot = null!;
        switch (type)
        {
            case RestraintSlotType.Basic:
                slot = LoadBasicSlot(json);
                break;
            case RestraintSlotType.Advanced:
                try
                {
                    slot = LoadAdvancedSlot(json);
                }
                catch (Exception e)
                {
                    // Create fallback for outdated advanced slots.
                    Logger.LogError(e, "Failed to load Advanced Slot. Reference was invalid, resetting to basic slot.");
                    slot = new RestraintSlotBasic(equipSlot);
                }
                break;
            default:
                throw new InvalidOperationException($"Unknown RestraintSlotType: {type}");
        }
        return slot;

    }

    /// <summary> Attempts to load a BasicSlot from the restraint set. </summary>
    /// <param name="slotToken"> The JSON Token for the Slot. </param>
    /// <returns> The loaded BasicSlot. </returns>
    /// <exception cref="Exception"> If the JToken is either not valid or the GlamourSlot fails to parse. </exception>
    /// <remarks> Throws if the JToken is either not valid or the GlamourSlot fails to parse.</remarks>
    private RestraintSlotBasic LoadBasicSlot(JToken? slotToken)
    {
        if (slotToken is not JObject slotJson)
            throw new Exception("Invalid JSON Token for Slot.");

        return new RestraintSlotBasic()
        {
            ApplyFlags = slotJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.IsOverlay,
            Glamour = _items.ParseGlamourSlot(slotJson["Glamour"])
        };
    }

    /// <summary> Attempts to load a Advanced from the restraint set. </summary>
    /// <param name="slotToken"> The JSON Token for the Slot. </param>
    /// <returns> The loaded Advanced. </returns>
    /// <exception cref="Exception"></exception>
    /// <remarks> If advanced slot fails to load, a default, invalid restriction item will be put in place. </remarks>
    private RestraintSlotAdvanced LoadAdvancedSlot(JToken? slotToken)
    {
        if(slotToken is not JObject slotJson)
            throw new Exception("Invalid JSON Token for Slot.");

        var applyFlags = slotJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.Advanced;
        var refId = slotJson["RestrictionRef"]?.ToObject<Guid>() ?? Guid.Empty;
        var stains = JParser.ParseCompactStainIds(slotJson["CustomStains"]);

        if (refId.IsEmptyGuid())
        {
            Logger.LogWarning("No Advanced Restriction was attached to this advanced slot!");
            return new RestraintSlotAdvanced()
            {
                ApplyFlags = applyFlags,
                Ref = new RestrictionItem() { Identifier = Guid.Empty },
                CustomStains = stains,
            };
        }
        else if (_restrictions.Storage.TryGetRestriction(refId, out var match))
            return new RestraintSlotAdvanced() { ApplyFlags = applyFlags, Ref = match, CustomStains = stains };
        else
            throw new Exception("Invalid Reference ID for Advanced Slot.");
    }

    private IRestraintLayer LoadRestraintLayer(JToken? layerToken)
    {
        if (layerToken is not JObject json)
            throw new Exception("Invalid JSON Token for Slot.");

        var typeStr = json["Type"]?.Value<string>() ?? throw new InvalidOperationException("Missing Type information in JSON.");
        if (!Enum.TryParse(typeStr, out RestraintLayerType type))
            throw new InvalidOperationException($"Unknown RestraintLayerType: {typeStr}");

        return type switch
        {
            RestraintLayerType.Restriction => LoadBindLayer(json),
            RestraintLayerType.ModPreset => LoadModPresetLayer(json),
            _ => throw new InvalidOperationException($"Unknown RestraintLayerType: {type}"),
        };
    }

    private RestrictionLayer LoadBindLayer(JToken? layerToken)
    {
        if (layerToken is not JObject layerJson)
            throw new Exception("Invalid JSON Token for Slot.");

        var id = Guid.TryParse(layerJson["ID"]?.Value<string>(), out var guid) ? guid : throw new Exception("InvalidGUID");
        var isActive = layerJson["IsActive"]?.Value<bool>() ?? false;
        var flags = layerJson["ApplyFlags"]?.ToObject<int>() is int v ? (RestraintFlags)v : RestraintFlags.Advanced;
        var customStains = JParser.ParseCompactStainIds(layerJson["CustomStains"]);

        var refId = layerJson["RestrictionRef"]?.ToObject<Guid>() ?? Guid.Empty;
        if (refId.IsEmptyGuid())
        {
            Logger.LogWarning("No Advanced Restriction was attached to this advanced slot!");
            return new RestrictionLayer()
            {
                ID = id,
                IsActive = isActive,
                ApplyFlags = flags,
                Ref = new RestrictionItem() { Identifier = Guid.Empty },
                CustomStains = customStains,
            };
        }
        else if (_restrictions.Storage.TryGetRestriction(refId, out var match))
        {
            return new RestrictionLayer()
            {
                ID = id,
                IsActive = isActive,
                ApplyFlags = flags,
                Ref = match,
                CustomStains = customStains,
            };
        }
        else
            throw new Exception("Invalid Reference ID for Advanced Slot.");
    }

    private ModPresetLayer LoadModPresetLayer(JToken? layerToken)
    {
        if (layerToken is not JObject json)
            throw new Exception("Invalid JSON Token for Slot.");

        // Load the ModRef sub-object using ModSettingsPreset's loader
        var modItem = ModSettingsPreset.FromRefToken(json["Mod"], _modPresets);
        return new ModPresetLayer()
        {
            ID = Guid.TryParse(json["ID"]?.Value<string>(), out var guid) ? guid : throw new Exception("Invalid GUID Data!"),
            IsActive = json["IsActive"]?.Value<bool>() ?? false,
            Mod = modItem,
        };
    }

    #endregion HybridSaver

    private void CheckForExpiredLocks()
    {
        if (!MainHub.IsConnected)
            return;

        if (_serverRestraintData is null || !_serverRestraintData.IsLocked())
            return;

        if (PadlockEx.IsTimerLock(_serverRestraintData.Padlock) && _serverRestraintData.HasTimerExpired())
        {
            Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.Restraints);
            // only set data relevant to the new change.
            var newData = new CharaActiveRestraint()
            {
                Padlock = _serverRestraintData.Padlock, // match the padlock
                Password = _serverRestraintData.Password, // use the same password.
                PadlockAssigner = _serverRestraintData.PadlockAssigner // use the same assigner. (To remove devotional timers)
            };

            Mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Unlocked, newData));
        }
    }
}
