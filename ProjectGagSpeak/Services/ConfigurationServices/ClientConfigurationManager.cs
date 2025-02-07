using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using GagSpeak.GagspeakConfiguration;
using GagSpeak.GagspeakConfiguration.Configurations;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.Hardcore.ForcedStay;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.UpdateMonitoring;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;
using Microsoft.IdentityModel.Tokens;
using Penumbra.GameData.Enums;
using System.Diagnostics.CodeAnalysis;

namespace GagSpeak.Services.ConfigurationServices;

/// <summary>
/// This configuration manager helps manage the various interactions with all config files related to server-end activity.
/// <para> It provides a comprehensive interface for configuring servers, managing tags and nicknames, and handling authentication keys. </para>
/// </summary>
public class ClientConfigurationManager : DisposableMediatorSubscriberBase
{
    private readonly OnFrameworkService _frameworkUtils;            // a utilities class with methods that work with the Dalamud framework
    private readonly GagspeakConfigService _configService;          // the primary gagspeak config service.
    private readonly GagStorageConfigService _gagStorageConfig;     // the config for the gag storage service (gag storage)
    private readonly WardrobeConfigService _wardrobeConfig;         // the config for the wardrobe service (restraint sets)
    private readonly CursedLootConfigService _cursedLootConfig;     // the config for the cursed loot service (cursed loot storage)
    private readonly AliasConfigService _aliasConfig;               // the config for the alias lists (puppeteer stuff)
    private readonly PatternConfigService _patternConfig;           // the config for the pattern service (toybox pattern storage))
    private readonly AlarmConfigService _alarmConfig;               // the config for the alarm service (toybox alarm storage)
    private readonly TriggerConfigService _triggerConfig;           // the config for the triggers service (toybox triggers storage)

    public ClientConfigurationManager(ILogger<ClientConfigurationManager> logger,
        GagspeakMediator GagspeakMediator, OnFrameworkService onFrameworkService,
        GagspeakConfigService configService, GagStorageConfigService gagStorageConfig,
        WardrobeConfigService wardrobeConfig, CursedLootConfigService cursedLootConfig,
        AliasConfigService aliasConfig, PatternConfigService patternConfig,
        AlarmConfigService alarmConfig, TriggerConfigService triggersConfig) : base(logger, GagspeakMediator)
    {
        // create a new instance of the static universal logger that pulls from the client logger.
        // because this loads our configs before the logger initialized, we use a simply hack to
        // set the static logger to the clientConfigManager logger.
        // its not ideal, but it works. If there is a better way please tell me.
        StaticLogger.Logger = logger;

        _frameworkUtils = onFrameworkService;
        _configService = configService;
        _gagStorageConfig = gagStorageConfig;
        _wardrobeConfig = wardrobeConfig;
        _cursedLootConfig = cursedLootConfig;
        _aliasConfig = aliasConfig;
        _patternConfig = patternConfig;
        _alarmConfig = alarmConfig;
        _triggerConfig = triggersConfig;
        InitConfigs();

        // Subscribe to the connected message update so we know when to update our global permissions
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ =>
        {
            // if our connection dto is null, we cannot proceed. (but it should never even happen.
            if (MainHub.ConnectionDto is null)
            {
                Logger.LogError("MainHubConnectedMessage received with null value. (this shouldnt even be happening!!");
                return;
            }
            // update our configs to point to the new user.
            if (MainHub.UID != _configService.Current.LastUidLoggedIn)
            {
                UpdateConfigs(MainHub.UID);
            }
            // update the last logged in UID
            _configService.Current.LastUidLoggedIn = MainHub.ConnectionDto.User.UID;
            Save();
        });

        Mediator.Publish(new UpdateChatListeners());
    }

    // define public access to various storages (THESE ARE ONLY GETTERS, NO SETTERS)
    public GagspeakConfig GagspeakConfig => _configService.Current; // UNIVERSAL
    public GagStorageConfig GagStorageConfig => _gagStorageConfig.Current; // PER PLAYER
    public WardrobeConfig WardrobeConfig => _wardrobeConfig.Current; // PER PLAYER
    public CursedLootConfig CursedLootConfig => _cursedLootConfig.Current; // PER PLAYER
    public AliasConfig AliasConfig => _aliasConfig.Current; // PER PLAYER
    public PatternConfig PatternConfig => _patternConfig.Current; // PER PLAYER
    public AlarmConfig AlarmConfig => _alarmConfig.Current; // PER PLAYER
    public TriggerConfig TriggerConfig => _triggerConfig.Current; // PER PLAYER

    // Handcore runtime variable storage.
    internal string LastSeenNodeName { get; set; } = string.Empty; // The Node Visible Name
    internal string LastSeenNodeLabel { get; set; } = string.Empty; // The Label of the nodes Prompt
    internal (int Index, string Text)[] LastSeenListEntries { get; set; } = []; // The nodes Options
    internal string LastSeenListSelection { get; set; } = string.Empty; // Option we last selected
    internal int LastSeenListIndex { get; set; } // Index in the list that was selected
    internal TextEntryNode LastSelectedListNode { get; set; } = new();

    public void UpdateConfigs(string loggedInPlayerUID)
    {
        _gagStorageConfig.UpdateUid(loggedInPlayerUID);
        _wardrobeConfig.UpdateUid(loggedInPlayerUID);
        _cursedLootConfig.UpdateUid(loggedInPlayerUID);
        _aliasConfig.UpdateUid(loggedInPlayerUID);
        _triggerConfig.UpdateUid(loggedInPlayerUID);
        _alarmConfig.UpdateUid(loggedInPlayerUID);

        InitConfigs();
    }

    /// <summary> Saves the GagspeakConfig. </summary>
    public void Save()
    {
        var caller = new StackTrace().GetFrame(1)?.GetMethod()?.ReflectedType?.Name ?? "Unknown";
        //Logger.LogDebug("{caller} Calling config save", caller);
        _configService.Save();
    }

    // This is a terrible approach and there is probably a way to deal with it better,
    // but at the moment this handles any commonly known possible holes in config generation.
    public void InitConfigs()
    {
        if (_configService.Current.LoggerFilters.Count is 0 || _configService.Current.LoggerFilters.Contains(LoggerType.SpatialAudioLogger))
        {
            // reset with recommended.
            _configService.Current.LoggerFilters = LoggerFilter.GetAllRecommendedFilters();
            Save();
        }
    }

    /* -------------------- Update Monitoring & Hardcore Methods -------------------- */
    #region Update Monitoring And Hardcore
    public List<string> GetPlayersToListenFor()
    {
        // select from the aliasStorages, the character name and world where the values are not string.empty.
        return AliasConfig.AliasStorage
            .Where(x => x.Value.CharacterNameWithWorld != string.Empty)
            .Select(x => x.Value.CharacterNameWithWorld)
            .ToList();
    }

    public IEnumerable<ITextNode> GetAllNodes()
    {
        return new ITextNode[] { GagspeakConfig.ForcedStayPromptList }
            .Concat(GetAllNodes(GagspeakConfig.ForcedStayPromptList.Children));
    }

    public IEnumerable<ITextNode> GetAllNodes(IEnumerable<ITextNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node is TextFolderNode folder)
            {
                var children = GetAllNodes(folder.Children);
                foreach (var childNode in children)
                    yield return childNode;
            }
        }
    }

    public bool TryFindParent(ITextNode node, out TextFolderNode? parent)
    {
        foreach (var candidate in GetAllNodes())
        {
            if (candidate is TextFolderNode folder && folder.Children.Contains(node))
            {
                parent = folder;
                return true;
            }
        }

        parent = null;
        return false;
    }

    public void AddLastSeenNode()
    {
        var newNode = new TextEntryNode()
        {
            Enabled = false,
            FriendlyName = (LastSeenNodeLabel.IsNullOrEmpty() ? LastSeenNodeName : LastSeenNodeLabel) + "(Friendly Name)",
            TargetNodeName = LastSeenNodeName,
            TargetRestricted = true,
            TargetNodeLabel = LastSeenNodeLabel,
            SelectedOptionText = LastSeenListSelection,
        };
        GagspeakConfig.ForcedStayPromptList.Children.Add(newNode);
        _configService.Save();
    }

    public void CreateTextNode()
    {
        // create a new blank one
        var newNode = new TextEntryNode()
        {
            Enabled = false,
            FriendlyName = "Placeholder Friendly Name",
            TargetNodeName = "Name Of Node You Interact With",
            TargetRestricted = true,
            TargetNodeLabel = "Label given to interacted node's prompt menu",
            SelectedOptionText = "Option we select from the prompt.",
        };
        GagspeakConfig.ForcedStayPromptList.Children.Add(newNode);
        _configService.Save();
    }

    public void CreateChamberNode()
    {
        var newNode = new ChambersTextNode()
        {
            Enabled = false,
            FriendlyName = "New ChamberNode",
            TargetRestricted = true,
            TargetNodeName = "Name Of Node You Interact With",
            ChamberRoomSet = 0,
            ChamberListIdx = 0,
        };
        GagspeakConfig.ForcedStayPromptList.Children.Add(newNode);
        _configService.Save();
    }

    #endregion Update Monitoring And Hardcore

    public string EnsureUniqueName<T>(string baseName, IEnumerable<T> collection, Func<T, string> nameSelector)
    {
        // Regex to match the base name and the (X) suffix if it exists
        var suffixPattern = @"^(.*?)(?: \((\d+)\))?$";
        var match = System.Text.RegularExpressions.Regex.Match(baseName, suffixPattern);

        string namePart = match.Groups[1].Value; // The base part of the name
        int currentNumber = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;

        // Increment current number for the new copy
        currentNumber = Math.Max(1, currentNumber);

        string newName = baseName;

        // Ensure the name is unique by appending (X) and incrementing if necessary
        while (collection.Any(item => nameSelector(item) == newName))
        {
            newName = $"{namePart} ({currentNumber++})";
        }

        return newName;
    }


    public bool HasGlamourerAlterations => GetActiveSetIdx() != -1 || ActiveCursedItems.Count > 0;




    /* --------------------- Gag Storage Config Methods --------------------- */
    #region Gag Storage Methods
    internal bool IsGagEnabled(GagType gagType)
    {
        return GagStorageConfig.GagStorage.GagEquipData.TryGetValue(gagType, out var gagData)
            && gagData.IsEnabled && gagData.GameItem.ItemId != ItemIdVars.NothingItem(gagData.Slot).ItemId;
    }

    internal GagDrawData GetDrawData(GagType gagType)
        => GagStorageConfig.GagStorage.GagEquipData[gagType];
    internal GagDrawData GetDrawDataWithHighestPriority(List<GagType> gagTypes)
        => GagStorageConfig.GagStorage.GagEquipData.Where(x => gagTypes.Contains(x.Key)).OrderBy(x => x.Value.CustomizePriority).FirstOrDefault().Value;
    internal EquipSlot GetGagTypeEquipSlot(GagType gagType)
        => _gagStorageConfig.Current.GagStorage.GagEquipData.FirstOrDefault(x => x.Key == gagType).Value.Slot;

    internal void UpdateGagStorageDictionary(Dictionary<GagType, GagDrawData> newGagStorage)
    {
        _gagStorageConfig.Current.GagStorage.GagEquipData = newGagStorage;
        _gagStorageConfig.Save();
        Logger.LogInformation("GagStorage Config Saved");
    }

    internal IReadOnlyList<byte> GetGagTypeStainIds(GagType gagType)
    {
        var GameStains = _gagStorageConfig.Current.GagStorage.GagEquipData.FirstOrDefault(x => x.Key == gagType).Value.GameStain;
        return [GameStains.Stain1.Id, GameStains.Stain2.Id];
    }

    internal void UpdateGagItem(GagType gagType, GagDrawData newData)
    {
        _gagStorageConfig.Current.GagStorage.GagEquipData[gagType] = newData;
        _gagStorageConfig.Save();
        Logger.LogInformation("GagStorage Config Saved");
    }

    internal void SaveGagStorage()
    {
        _gagStorageConfig.Save();
        Logger.LogInformation("GagStorage Config Saved");
    }

    #endregion Gag Storage Methods
    /* --------------------- Wardrobe Config Methods --------------------- */
    #region Wardrobe Config Methods
    /// <summary> 
    /// I swear to god, so not set anything inside this object through this fetch. Treat it as readonly.
    /// </summary>
    internal List<RestraintSet> StoredRestraintSets => WardrobeConfig.WardrobeStorage.RestraintSets;

    internal RestraintSet? GetActiveSet() => WardrobeConfig.WardrobeStorage.RestraintSets.FirstOrDefault(x => x.Enabled)!; // this can be null.
    internal bool TryGetActiveSet([MaybeNullWhen(false)] out RestraintSet activeSet)
    {
        activeSet = WardrobeConfig.WardrobeStorage.RestraintSets.FirstOrDefault(x => x.Enabled);
        return activeSet is not null;
    }
    internal bool TryGetSet(Guid id, [MaybeNullWhen(false)] out RestraintSet restraintSet)
    {
        return WardrobeConfig.WardrobeStorage.RestraintSets.TryGetItem(x => x.RestraintId == id, out restraintSet);
    }

    internal int GetActiveSetIdx() => WardrobeConfig.WardrobeStorage.RestraintSets.FindIndex(x => x.Enabled);
    internal int GetSetIdxByGuid(Guid id) => WardrobeConfig.WardrobeStorage.RestraintSets.FindIndex(x => x.RestraintId == id);
    internal string GetSetNameByGuid(Guid id) => WardrobeConfig.WardrobeStorage.RestraintSets.FirstOrDefault(x => x.RestraintId == id)?.Name ?? "Unknown";
    internal RestraintSet GetRestraintSet(int setIndex) => WardrobeConfig.WardrobeStorage.RestraintSets[setIndex];
    internal int GetRestraintSetIdxByName(string name) => WardrobeConfig.WardrobeStorage.RestraintSets.FindIndex(x => x.Name == name);

    internal void CloneRestraintSet(RestraintSet setToClone)
    {
        var clonedSet = setToClone.DeepCloneSet();
        clonedSet.Name = EnsureUniqueName(clonedSet.Name, WardrobeConfig.WardrobeStorage.RestraintSets, set => set.Name);
        _wardrobeConfig.Current.WardrobeStorage.RestraintSets.Add(clonedSet);
        _wardrobeConfig.Save();
        Logger.LogInformation("Restraint Set added to wardrobe", LoggerType.Restraints);
        // publish to mediator
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void AddNewRestraintSet(RestraintSet newSet)
    {
        // Ensure the set has a unique name before adding it.
        newSet.Name = EnsureUniqueName(newSet.Name, WardrobeConfig.WardrobeStorage.RestraintSets, set => set.Name);

        _wardrobeConfig.Current.WardrobeStorage.RestraintSets.Add(newSet);
        _wardrobeConfig.Save();
        Logger.LogInformation("Restraint Set added to wardrobe", LoggerType.Restraints);
        // publish to mediator
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void AddNewRestraintSets(List<RestraintSet> newSets)
    {
        foreach (var newSet in newSets)
        {
            // add 1 to the name until it is unique.
            while (WardrobeConfig.WardrobeStorage.RestraintSets.Any(x => x.Name == newSet.Name))
            {
                newSet.Name += "(copy)";
            }
            _wardrobeConfig.Current.WardrobeStorage.RestraintSets.Add(newSet);
        }
        _wardrobeConfig.Save();
        Logger.LogInformation("Added " + newSets.Count + " Restraint Sets to wardrobe", LoggerType.Restraints);
        // publish to mediator
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    // remove a restraint set
    internal void RemoveRestraintSet(int setIndex)
    {
        _wardrobeConfig.Current.WardrobeStorage.RestraintSets.RemoveAt(setIndex);
        _wardrobeConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void SaveWardrobe() => _wardrobeConfig.Save();

    internal EquipDrawData GetBlindfoldItem() => WardrobeConfig.WardrobeStorage.BlindfoldInfo.BlindfoldItem;

    internal void SetBlindfoldItem(EquipDrawData drawData)
    {
        WardrobeConfig.WardrobeStorage.BlindfoldInfo.BlindfoldItem = drawData;
        _wardrobeConfig.Save();
    }

    internal void UpdateRestraintSet(RestraintSet updatedSet, int idxOfOriginal)
    {
        WardrobeConfig.WardrobeStorage.RestraintSets[idxOfOriginal] = updatedSet;
        _wardrobeConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
        // Invoke the restraint updated achievement.
        UnlocksEventManager.AchievementEvent(UnlocksEvent.RestraintUpdated, updatedSet);
    }

    #endregion Wardrobe Config Methods

    /* --------------------- Cursed Loot Config Methods --------------------- */
    #region Cursed Loot Config Methods
    internal List<Guid> ActiveCursedItems => CursedLootConfig.CursedLootStorage.CursedItems
        .Where(x => x.AppliedTime != DateTimeOffset.MinValue)
        .OrderByDescending(x => x.AppliedTime) // reverse this if wrong order.
        .Select(x => x.LootId)
        .ToList();

    internal bool IsGuidInItems(Guid lootId) => CursedLootConfig.CursedLootStorage.CursedItems.Any(x => x.LootId == lootId);

    internal void AddCursedItem(CursedItem newItem)
    {
        newItem.Name = EnsureUniqueName(newItem.Name, CursedLootConfig.CursedLootStorage.CursedItems, item => item.Name);
        CursedLootConfig.CursedLootStorage.CursedItems.Add(newItem);
        _cursedLootConfig.Save();
    }

    internal void RemoveCursedItem(Guid lootIdToRemove)
    {
        var idx = CursedLootConfig.CursedLootStorage.CursedItems.FindIndex(x => x.LootId == lootIdToRemove);
        CursedLootConfig.CursedLootStorage.CursedItems.RemoveAt(idx);
        _cursedLootConfig.Save();
    }

    internal void ActivateCursedItem(Guid lootId, DateTimeOffset endTimeUtc)
    {
        var idx = CursedLootConfig.CursedLootStorage.CursedItems.FindIndex(x => x.LootId == lootId);
        if (idx != -1)
        {
            CursedLootConfig.CursedLootStorage.CursedItems[idx].AppliedTime = DateTimeOffset.UtcNow;
            CursedLootConfig.CursedLootStorage.CursedItems[idx].ReleaseTime = endTimeUtc;
            _cursedLootConfig.Save();
        }
    }

    internal void DeactivateCursedItem(Guid lootId)
    {
        var idx = CursedLootConfig.CursedLootStorage.CursedItems.FindIndex(x => x.LootId == lootId);
        if (idx != -1)
        {
            CursedLootConfig.CursedLootStorage.CursedItems[idx].AppliedTime = DateTimeOffset.MinValue;
            CursedLootConfig.CursedLootStorage.CursedItems[idx].ReleaseTime = DateTimeOffset.MinValue;
            _cursedLootConfig.Save();
        }
    }

    internal void SaveCursedLoot() => _cursedLootConfig.Save();


    #endregion Cursed Loot Config Methods


    /* --------------------- Puppeteer Alias Configs --------------------- */
    #region Alias Config Methods

    public Dictionary<string, CharaAliasData> GetCompiledAliasData() => AliasConfig.FromAliasStorage();

    public string? GetUidMatchingSender(string nameWithWorld)
        => AliasConfig.AliasStorage.FirstOrDefault(x => x.Value.CharacterNameWithWorld == nameWithWorld).Key;

    public AliasStorage FetchAliasStorageForPair(string userId)
    {
        if (!AliasConfig.AliasStorage.ContainsKey(userId))
        {
            Logger.LogDebug("User " + userId + " does not have an alias list, creating one.", LoggerType.Puppeteer);
            // If not, initialize it with a new AliasList object
            AliasConfig.AliasStorage[userId] = new AliasStorage();
            _aliasConfig.Save();
        }
        return AliasConfig.AliasStorage[userId];
    }

    // Called whenever set is saved.
    internal void UpdateAliasList(string userId, List<AliasTrigger> aliasList)
    {
        // try and get the alias storage from the dictionary by the key.
        if (!AliasConfig.AliasStorage.ContainsKey(userId))
        {
            AliasConfig.AliasStorage[userId] = new AliasStorage() { AliasList = aliasList };
            _aliasConfig.Save();
            Mediator.Publish(new PlayerCharAliasChanged(userId, PuppeteerUpdateType.AliasListUpdated));
        }
        else
        {
            AliasConfig.AliasStorage[userId].AliasList = aliasList;
            _aliasConfig.Save();
            Mediator.Publish(new PlayerCharAliasChanged(userId, PuppeteerUpdateType.AliasListUpdated));
        }
    }

    // called from a callback update from other paired client. Never called by self. Meant to set another players name to our config.
    internal void UpdateAliasStoragePlayerInfo(string userId, string charaNameWorld)
    {
        if(!AliasConfig.AliasStorage.ContainsKey(userId))
        {
            AliasConfig.AliasStorage[userId] = new AliasStorage() { CharacterNameWithWorld = charaNameWorld };
            _aliasConfig.Save();
            Mediator.Publish(new UpdateChatListeners());
        }
        else
        {
            AliasConfig.AliasStorage[userId].CharacterNameWithWorld = charaNameWorld;
            _aliasConfig.Save();
            Mediator.Publish(new UpdateChatListeners());
        }
    }

    internal void SaveAlias() => _aliasConfig.Save();

    #endregion Alias Config Methods
    /* --------------------- Toybox Pattern Configs --------------------- */
    #region Pattern Config Methods

    /// <summary> Fetches the currently Active Alarm to have as a reference accessor. Can be null. </summary>
    public bool AnyPatternIsPlaying => PatternConfig.PatternStorage.Patterns.Any(p => p.IsActive);
    public PatternData FetchPattern(int idx) => PatternConfig.PatternStorage.Patterns[idx];
    public PatternData? FetchPatternById(Guid id) => PatternConfig.PatternStorage.Patterns.FirstOrDefault(p => p.UniqueIdentifier == id);
    public bool PatternExists(Guid id) => PatternConfig.PatternStorage.Patterns.Any(p => p.UniqueIdentifier == id);
    public Guid ActivePatternGuid() => PatternConfig.PatternStorage.Patterns.Where(p => p.IsActive).Select(p => p.UniqueIdentifier).FirstOrDefault();

    public void AddNewPattern(PatternData newPattern)
    {
        // esure uniqueness.
        newPattern.Name = EnsureUniqueName(newPattern.Name, PatternConfig.PatternStorage.Patterns, pattern => pattern.Name);
        PatternConfig.PatternStorage.Patterns.Add(newPattern);
        _patternConfig.Save();
        // publish to mediator one was added
        Logger.LogInformation("Pattern Added: " + newPattern.Name, LoggerType.ToyboxPatterns);
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    // Bulk variant.
    public void AddNewPatterns(List<PatternData> newPattern)
    {
        foreach (var pattern in newPattern)
        {
            // if a pattern from the patternstorage with the same name already exists, continue.
            if (PatternConfig.PatternStorage.Patterns.Any(x => x.Name == pattern.Name)) continue;
            // otherwise, add it
            PatternConfig.PatternStorage.Patterns.Add(pattern);
        }
        _patternConfig.Save();
        // publish to mediator one was added
        Logger.LogInformation("Added: " + newPattern.Count + " Patterns to Toybox", LoggerType.ToyboxPatterns);
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    public void RemovePattern(Guid identifierToRemove)
    {
        // find the pattern to remove by scanning the storage for the unique identifier.
        var indexToRemove = PatternConfig.PatternStorage.Patterns.FindIndex(x => x.UniqueIdentifier == identifierToRemove);
        PatternConfig.PatternStorage.Patterns.RemoveAt(indexToRemove);
        _patternConfig.Save();

        // iterate through the alarms to see if we need to remove patterns from any of them.
        foreach (var alarm in AlarmConfig.AlarmStorage.Alarms)
        {
            if (alarm.PatternToPlay == identifierToRemove)
            {
                alarm.PatternToPlay = Guid.Empty;
                alarm.PatternStartPoint = TimeSpan.Zero;
                alarm.PatternDuration = TimeSpan.Zero;
            }
        }
        _alarmConfig.Save();

        // publish to mediator one was removed and any potential alarms were updated.
        Mediator.Publish(new PlayerCharStorageUpdated());

    }

    public void UpdatePattern(PatternData pattern, int idx)
    {
        PatternConfig.PatternStorage.Patterns[idx] = pattern;
        _patternConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void SavePatterns() => _patternConfig.Save();

    #endregion Pattern Config Methods
    /* --------------------- Toybox Alarm Configs --------------------- */
    #region Alarm Config Methods
    public int ActiveAlarmCount => AlarmConfig.AlarmStorage.Alarms.Count(x => x.Enabled);
    internal bool IsGuidInAlarms(Guid alarmId) => AlarmConfig.AlarmStorage.Alarms.Any(x => x.Identifier == alarmId);

    public void AddNewAlarm(Alarm alarm)
    {
        alarm.Name = EnsureUniqueName(alarm.Name, AlarmConfig.AlarmStorage.Alarms, alarm => alarm.Name);
        AlarmConfig.AlarmStorage.Alarms.Add(alarm);
        _alarmConfig.Save();

        Logger.LogInformation("Alarm Added: " + alarm.Name, LoggerType.ToyboxAlarms);
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    public void RemoveAlarm(Alarm alarmToRemove)
    {
        Logger.LogInformation("Alarm Removed: " + alarmToRemove.Name, LoggerType.ToyboxAlarms);
        AlarmConfig.AlarmStorage.Alarms.RemoveAll(x => x.Identifier == alarmToRemove.Identifier);
        _alarmConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    public void UpdateAlarm(Alarm alarm, int idx)
    {
        AlarmConfig.AlarmStorage.Alarms[idx] = alarm;
        _alarmConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void SaveAlarms() => _alarmConfig.Save();

    #endregion Alarm Config Methods

    /* --------------------- Toybox Trigger Configs --------------------- */
    #region Trigger Config Methods
    public List<Trigger> ActiveTriggers => TriggerConfig.TriggerStorage.Triggers.Where(x => x.Enabled).ToList();
    public IEnumerable<SpellActionTrigger> ActiveSpellActionTriggers => TriggerConfig.TriggerStorage.Triggers.OfType<SpellActionTrigger>().Where(x => x.Enabled);
    public IEnumerable<HealthPercentTrigger> ActiveHealthPercentTriggers => TriggerConfig.TriggerStorage.Triggers.OfType<HealthPercentTrigger>().Where(x => x.Enabled);
    public IEnumerable<RestraintTrigger> ActiveRestraintTriggers => TriggerConfig.TriggerStorage.Triggers.OfType<RestraintTrigger>().Where(x => x.Enabled);
    public IEnumerable<GagTrigger> ActiveGagStateTriggers => TriggerConfig.TriggerStorage.Triggers.OfType<GagTrigger>().Where(x => x.Enabled);
    public IEnumerable<SocialTrigger> ActiveSocialTriggers => TriggerConfig.TriggerStorage.Triggers.OfType<SocialTrigger>().Where(x => x.Enabled);

    internal bool IsGuidInTriggers(Guid triggerId) => TriggerConfig.TriggerStorage.Triggers.Any(x => x.Identifier == triggerId);

    internal void AddNewTrigger(Trigger trigger)
    {
        trigger.Name = EnsureUniqueName(trigger.Name, TriggerConfig.TriggerStorage.Triggers, trigger => trigger.Name);
        TriggerConfig.TriggerStorage.Triggers.Add(trigger);
        _triggerConfig.Save();

        Logger.LogInformation("Trigger Added: " + trigger.Name, LoggerType.ToyboxTriggers);
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void RemoveTrigger(Trigger triggerToRemove)
    {
        Logger.LogInformation("Trigger Removed: " + triggerToRemove.Name, LoggerType.ToyboxTriggers);
        TriggerConfig.TriggerStorage.Triggers.RemoveAll(x => x.Identifier == triggerToRemove.Identifier);
        _triggerConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void UpdateTrigger(Trigger trigger, int idx)
    {
        TriggerConfig.TriggerStorage.Triggers[idx] = trigger;
        _triggerConfig.Save();
        Mediator.Publish(new PlayerCharStorageUpdated());
    }

    internal void SaveTriggers() => _triggerConfig.Save();

    #endregion Trigger Config Methods

    public Task DisableEverythingDueToSafeword()
    {
        // disable any active alarms.
        foreach (var alarm in AlarmConfig.AlarmStorage.Alarms)
            alarm.Enabled = false;
        _alarmConfig.Save();

        // disable any active triggers.
        foreach (var trigger in TriggerConfig.TriggerStorage.Triggers)
            trigger.Enabled = false;
        _triggerConfig.Save();
        Mediator.Publish(new PlayerCharToyboxChanged(ToyboxUpdateType.Safeword));

        return Task.CompletedTask;
    }


    #region API Compilation

    public CharaWardrobeData CompileWardrobeToAPI()
    {
        // attempt to locate the active restraint set
        var activeSet = GetActiveSet();
        return new CharaWardrobeData
        {
            ActiveSetId = activeSet?.RestraintId ?? Guid.Empty,
            ActiveSetEnabledBy = activeSet?.EnabledBy ?? string.Empty,
            Padlock = activeSet?.Padlock ?? Padlocks.None.ToName(),
            Password = activeSet?.Password ?? "",
            Timer = activeSet?.Timer ?? DateTimeOffset.MinValue,
            Assigner = activeSet?.Assigner ?? "",
        };
    }

    public CharaOrdersData CompileOrdersDataToAPI()
    {
        return new CharaOrdersData
        {
            ActiveOrders = new HashSet<Guid>(),
        };
    }

    public CharaToyboxData CompileToyboxToAPI()
    {
        return new CharaToyboxData
        {
            ActivePatternId = ActivePatternGuid(),
            ActiveAlarms = AlarmConfig.AlarmStorage.Alarms.Where(x => x.Enabled).Select(x => x.Identifier).ToList(),
            ActiveTriggers = TriggerConfig.TriggerStorage.Triggers.Where(x => x.Enabled).Select(x => x.Identifier).ToList(),
        };
    }

    public CharaStorageData CompileLightStorageToAPI()
    {
        return new CharaStorageData
        {
            GagItems = GagStorageConfig.GagStorage.GetAppliedSlotGagData(),
            Restraints = WardrobeConfig.WardrobeStorage.RestraintSets.Select(x => x.ToLightData()).ToList(),
            CursedItems = CursedLootConfig.CursedLootStorage.CursedItems.Select(x => x.ToLightData()).ToList(),
            BlindfoldItem = WardrobeConfig.WardrobeStorage.BlindfoldInfo.GetAppliedSlot(),
            Patterns = PatternConfig.PatternStorage.Patterns.Select(x => x.ToLightData()).ToList(),
            Alarms = AlarmConfig.AlarmStorage.Alarms.Select(x => x.ToLightData()).ToList(),
            Triggers = TriggerConfig.TriggerStorage.Triggers.Select(x => x.ToLightData()).ToList(),
        };
    }
    #endregion API Compilation

}
