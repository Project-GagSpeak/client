using CkCommons;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.Services;

// Orchistrates server calls to distribute data off to the server.
// This should not be included by any serices that should not be interacting with MainHub circular dependancy.
public sealed class DistributorService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly ClientAchievements _achievements;
    private readonly KinksterManager _kinksters;
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly CollarManager _collar;
    private readonly CursedLootManager _cursedManager;
    private readonly BuzzToyManager _toyManager;
    private readonly PuppeteerManager _puppetManager;
    private readonly PatternManager _patternManager;
    private readonly AlarmManager _alarmManager;
    private readonly TriggerManager _triggerManager;
    private readonly AllowancesConfig _traitManager;

    private SemaphoreSlim _updateSlim = new SemaphoreSlim(1, 1);
    private readonly HashSet<UserData> _newVisibleKinksters = [];
    private readonly HashSet<UserData> _newOnlineKinksters = [];

    public DistributorService(ILogger<DistributorService> logger,
        GagspeakMediator mediator,
        MainHub hub,
        ClientAchievements achievements,
        KinksterManager kinksters,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CollarManager collars,
        CursedLootManager cursedLoot,
        BuzzToyManager toys,
        PuppeteerManager puppetManager,
        PatternManager patterns,
        AlarmManager alarms,
        TriggerManager triggers,
        AllowancesConfig traits)
        : base(logger, mediator)
    {
        _hub = hub;
        _achievements = achievements;
        _kinksters = kinksters;
        _gagManager = gags;
        _restrictionManager = restrictions;
        _restraintManager = restraints;
        _collar = collars;
        _cursedManager = cursedLoot;
        _toyManager = toys;
        _puppetManager = puppetManager;
        _patternManager = patterns;
        _alarmManager = alarms;
        _triggerManager = triggers;
        _traitManager = traits;

        // Achievement Handling
        Mediator.Subscribe<SendAchievementData>(this, (_) => UpdateAchievementData().ConfigureAwait(false));
        Mediator.Subscribe<UpdateCompletedAchievements>(this, (_) => UpdateTotalEarned().ConfigureAwait(false));

        // Updates.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => DelayedFrameworkOnUpdate());

        // Kinkster Pair management.
        Mediator.Subscribe<KinksterOnline>(this, arg =>
        {
            if (!MainHub.IsConnectionDataSynced)
                return;
            _newOnlineKinksters.Add(arg.Kinkster.UserData);
        });
        Mediator.Subscribe<KinksterPlayerRendered>(this, msg => _newVisibleKinksters.Add(msg.Kinkster.UserData));

        // Visible Data Updaters
        Mediator.Subscribe<MoodlesApplyStatusToPair>(this, msg => ApplyMoodleToKinkster(msg.ApplyStatusTupleDto).ConfigureAwait(false));

        // Online Data Updaters
        Mediator.Subscribe<ConnectedMessage>(this, _ =>
        {
            _newVisibleKinksters.UnionWith(_kinksters.GetVisibleConnected());
            PushCompositeData(_kinksters.GetOnlineUserDatas()).ConfigureAwait(false);
        });

        Mediator.Subscribe<AliasStateChangedMessage>(this, arg => PushAliasStateChange(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveAliasesChangedMessage>(this, arg => PushActiveAliasesUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ValidToysChangedMessage>(this, arg => PushValidToysUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActivePatternChangedMessage>(this, arg => PushActivePatternUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveAlarmsChangedMessage>(this, arg => PushActiveAlarmsUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveTriggersChangedMessage>(this, arg => PushActiveTriggersUpdate(arg).ConfigureAwait(false));

        Mediator.Subscribe<ConfigGagRestrictionChanged>(this, msg => DistributeGagUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigRestrictionChanged>(this, msg => DistributeRestrictionUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigRestraintSetChanged>(this, msg => DistributeRestraintSetUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigCollarChanged>(this, msg => DistributeCollarUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigCursedItemChanged>(this, msg => DistributeCursedItemUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigAliasItemChanged>(this, msg => DistributeAliasItemUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigPatternChanged>(this, msg => DistributePatternUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigAlarmChanged>(this, msg => DistributeAlarmUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigTriggerChanged>(this, msg => DistributeTriggerUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<AllowancesChanged>(this, msg => DistributeAllowancesUpdate(msg.Module, msg.AllowedUids).ConfigureAwait(false));
    }

    // Do this for now, later, migrate the newly visible kinksters into an update service that multiple different distribution services could pull from.
    internal static string LastMoodlesDataString = string.Empty;

    // Idk why we need this really, anymore, but whatever i guess. If it helps it helps.
    private ActiveGagSlot? _prevGagData;
    private ActiveRestriction? _prevRestrictionData;
    private CharaActiveRestraint? _prevRestraintData;
    private CharaActiveCollar? _prevCollarData;

    private void DelayedFrameworkOnUpdate()
    {
        // Do not process if not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        // Handle Online Players.
        if (_newOnlineKinksters.Count > 0)
        {
            var newOnlineKinksters = _newOnlineKinksters.ToList();
            _newOnlineKinksters.Clear();
            PushCompositeData(newOnlineKinksters).ConfigureAwait(false);
        }

        // Handle Visible Players.
        if (PlayerData.Available && _newVisibleKinksters.Count > 0)
        {
            var newVisiblePlayers = _newVisibleKinksters.ToList();
            _newVisibleKinksters.Clear();
            UpdateVisibleFull(newVisiblePlayers).ConfigureAwait(false);
        }
    }

    /// <summary> Informs us if the new data being passed in is different from the previous stored. </summary>
    /// <remarks> This does not update the object, you should update it if this returns true. </remarks>
    private bool DataIsDifferent<T>(T? prevData, T newData) where T : class
    {
        if (prevData is null || !Equals(newData, prevData))
            return true;

        Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
        return false;
    }

    /// <summary>
    ///     Method used for updating all provided visible kinkster's with our moodles and appearance data. <para />
    ///     Called whenever a new visible pair enters our render range.
    /// </summary>
    private async Task UpdateVisibleFull(List<UserData> visibleCharas)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Not pushing Visible Full Data, not connected to server or data not synced.", LoggerType.ApiCore);
            _newVisibleKinksters.UnionWith(visibleCharas);
            return;
        }

        Logger.LogDebug($"Pushing Appearance and Moodles data to ({string.Join(", ", visibleCharas.Select(v => v.AliasOrUID))})", LoggerType.VisiblePairs);
        await DistributeFullMoodlesData(visibleCharas);
    }

    /// <summary>
    ///     This IPC Method should ONLY be sent to the newly visible kinksters, as it is the heaviest weight IPC call. <para />
    ///     Never generate for _kinksters.GetVisibleConnected() as this will cause a lot of unnecessary data to be sent.
    /// </summary>
    public async Task DistributeFullMoodlesData(List<UserData> visibleCharas)
    {
        // if we are not yet connected, append the visible character list to the _newVisibleKinksters
        if (!MainHub.IsConnectionDataSynced)
        {
            _newVisibleKinksters.UnionWith(visibleCharas);
            return;
        }

        LastMoodlesDataString = MoodleCache.IpcData.DataString;
        // Distribute the full IPC Data to the list of visible characters passed in.
        Logger.LogDebug($"Pushing Full IPCData to ({string.Join(", ", visibleCharas.Select(v => v.AliasOrUID))})", LoggerType.VisiblePairs);
        await _hub.UserPushMoodlesFull(new(visibleCharas, MoodleCache.IpcData));
    }

    private async Task ApplyMoodleToKinkster(ApplyMoodleStatus dto)
    {
        Logger.LogDebug($"Pushing ApplyMoodlesByStatus to: {dto.User.AliasOrUID}", LoggerType.ApiCore);
        if (await _hub.UserApplyMoodlesByStatus(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ApplyMoodlesByStatus to server. [{res.ErrorCode}]");
        else
            Logger.LogDebug($"Successfully pushed ApplyMoodlesByStatus to the server", LoggerType.ApiCore);
    }

    private CharaLightStorageData GetLatestLightStorage()
    {
        return new CharaLightStorageData()
        {
            GagItems = _gagManager.Storage.ToLightStorage().ToArray(),
            Restrictions = _restrictionManager.Storage.Select(r => r.ToLightItem()).ToArray(),
            Restraints = _restraintManager.Storage.Select(r => r.ToLightItem()).ToArray(),
            Collar = _collar.ClientCollar.ToLightItem(),
            CursedItems = _cursedManager.Storage.Select(x => x.ToLightItem()).ToArray(),
            Patterns = _patternManager.Storage.Select(p => p.ToLightItem()).ToArray(),
            Alarms = _alarmManager.Storage.Select(a => a.ToLightItem()).ToArray(),
            Triggers = _triggerManager.Storage.Select(t => t.ToLightItem()).ToArray(),
            Allowances = _traitManager.GetLightAllowances(),
        };
    }

    private async Task UpdateAchievementData()
    {
        // Prevent uploads if CanUpload() is false.
        if (ClientAchievements.HadUnhandledDC)
        {
            Logger.LogWarning("Will not update savedata while still recovering from an unhandled Disconnect!");
            return;
        }

        // Ensure data is valid.
        if (!ClientAchievements.HasValidData)
        {
            Logger.LogWarning("Cannot update achievement data, save data is invalid!");
            return;
        }

        // obtain the data to upload.
        Logger.LogInformation("Sending updated achievement data to the server", LoggerType.Achievements);
        var dataString = _achievements.SerializeData();
        Logger.LogInformation("Connected with AchievementData String:\n" + dataString);
        var result = await _hub.UserUpdateAchievementData(new(MainHub.OwnUserData, dataString)).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            Logger.LogDebug("Successfully pushed latest Achievement Data to server", LoggerType.Achievements);
        }
        else
        {
            Logger.LogError($"Failed to push Achievement Data to server. [{result.ErrorCode}]");
            return;
        }
    }

    // An updater slim is nessisary for this, or else it can cause some conflicts in where multiple achievements are completed simotaniously.
    private async Task UpdateTotalEarned()
    {
        await _updateSlim.WaitAsync();
        try
        {
            KinkPlateContent currentContent;
            if (KinkPlateService.KinkPlates.TryGetValue(MainHub.OwnUserData, out var existing))
                currentContent = existing.Info;
            else
            {
                var response = await _hub.UserGetKinkPlate(new KinksterBase(MainHub.OwnUserData));
                currentContent = response.Info;
            }

            Logger.LogDebug($"Updating KinkPlate™ with {ClientAchievements.Completed} Completions.", LoggerType.Achievements);
            currentContent.CompletedTotal = ClientAchievements.Completed;
            await _hub.UserSetKinkPlateContent(new(MainHub.OwnUserData, currentContent));
        }
        catch (Bagagwa ex)
        {
            Logger.LogError($"Failed to update KinkPlate™ with latest achievement count: {ex}", LoggerType.Achievements);
        }
        finally
        {
            _updateSlim.Release();
        }
    }

    private async Task PushCompositeData(List<UserData> newOnlinekinksters)
    {
        // if not connected and data synced just add the kinksters to the list. (Extra safety net)
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Not pushing Composite Data, not connected to server or data not synced.", LoggerType.ApiCore);
            _newOnlineKinksters.UnionWith(newOnlinekinksters);
            return;
        }

        if (newOnlinekinksters.Count <= 0)
            return;

        try
        {
            var newLightStorage = GetLatestLightStorage();

            var data = new CharaCompositeActiveData()
            {
                Gags = _gagManager.ServerGagData ?? throw new Exception("ActiveGagData was null!"),
                Restrictions = _restrictionManager.ServerRestrictionData ?? throw new Exception("ActiveRestrictionsData was null!"),
                Restraint = _restraintManager.ServerData ?? throw new Exception("ActiveRestraintData was null!"),
                Collar = _collar.SyncedData ?? throw new Exception("ActiveCollarData was null!"),
                ActiveCursedItems = _cursedManager.Storage.AppliedLootIds.ToList(),
                AliasData = _puppetManager.Storage,
                ListeningTo = _puppetManager.Puppeteers.Keys.ToList(),
                ValidToys = _toyManager.ValidToysForRemotes,
                ActivePattern = _patternManager.ActivePatternId,
                ActiveAlarms = _alarmManager.ActiveAlarms.Select(x => x.Identifier).ToList(),
                ActiveTriggers = _triggerManager.Storage.Select(x => x.Identifier).ToList(),
                LightStorageData = newLightStorage,
            };

            Logger.LogDebug($"Pushing CharaCompositeActiveData to: {string.Join(", ", newOnlinekinksters.Select(v => v.UID))}", LoggerType.ApiCore);
            var result = await _hub.UserPushActiveData(new(newOnlinekinksters, data, false)).ConfigureAwait(false);
            if (result.ErrorCode is GagSpeakApiEc.Success)
            {
                Logger.LogDebug("Successfully pushed Composite Data to server", LoggerType.ApiCore);
            }
            else
            {
                Logger.LogError($"Failed to push Composite Data to server. [{result.ErrorCode}]");
                return;
            }
        }
        catch (Bagagwa ex)
        {
            Logger.LogError("Failed to push Composite Data to server: " + ex);
            return;
        }
    }

    public async Task<ActiveGagSlot?> PushNewActiveGagSlot(int layer, ActiveGagSlot slot, DataUpdateType type)
        => await PushNewActiveGagSlot(_kinksters.GetOnlineUserDatas(), layer, slot, type);

    public async Task<ActiveGagSlot?> PushNewActiveGagSlot(List<UserData> onlinePlayers, int layer, ActiveGagSlot slot, DataUpdateType type)
    {
        if (type is not DataUpdateType.Unlocked && !DataIsDifferent(_prevGagData, slot))
            return null;

        Logger.LogDebug($"Pushing GagChange [{type}] to: {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientActiveGagSlot(onlinePlayers, type)
        {
            Layer = layer,
            Gag = slot.GagItem,
            Enabler = slot.Enabler,
            Padlock = slot.Padlock,
            Password = slot.Password,
            Timer = slot.Timer,
            Assigner = slot.PadlockAssigner
        };
        var res = await _hub.UserPushActiveGags(dto).ConfigureAwait(false);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push GagData to server [{res}]");
            return null;
        }

        _prevGagData = res.Value;
        return res.Value;
    }

    public async Task<ActiveRestriction?> PushNewActiveRestriction(int layerIdx, ActiveRestriction newData, DataUpdateType type)
        => await PushNewActiveRestriction(_kinksters.GetOnlineUserDatas(), layerIdx, newData, type);

    public async Task<ActiveRestriction?> PushNewActiveRestriction(List<UserData> onlinePlayers, int layerIdx, ActiveRestriction newData, DataUpdateType type)
    {
        if (type is not DataUpdateType.Unlocked && !DataIsDifferent(_prevRestrictionData, newData))
            return null;

        Logger.LogDebug($"Pushing RestrictionChange [{type}] to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientActiveRestriction(onlinePlayers, type)
        {
            Layer = layerIdx,
            Identifier = newData.Identifier,
            Enabler = newData.Enabler,
            Padlock = newData.Padlock,
            Password = newData.Password,
            Timer = newData.Timer,
            Assigner = newData.PadlockAssigner
        };
        var res = await _hub.UserPushActiveRestrictions(dto).ConfigureAwait(false);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestrictionData to server [{res}]");
            return null;
        }

        _prevRestrictionData = res.Value;
        return res.Value;
    }

    public async Task<CharaActiveRestraint?> PushNewActiveRestraint(CharaActiveRestraint newData, DataUpdateType type)
        => await PushNewActiveRestraint(_kinksters.GetOnlineUserDatas(), newData, type);

    public async Task<CharaActiveRestraint?> PushNewActiveRestraint(List<UserData> onlinePlayers, CharaActiveRestraint newData, DataUpdateType type)
    {
        if (type is not DataUpdateType.Unlocked && !DataIsDifferent(_prevRestraintData, newData))
            return null;

        Logger.LogDebug($"Pushing RestraintData to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{type}]", LoggerType.OnlinePairs);

        var dto = new PushClientActiveRestraint(onlinePlayers, type)
        {
            ActiveSetId = newData.Identifier,
            ActiveLayers = newData.ActiveLayers,
            Enabler = newData.Enabler,
            Padlock = newData.Padlock,
            Password = newData.Password,
            Timer = newData.Timer,
            Assigner = newData.PadlockAssigner
        };
        var res = await _hub.UserPushActiveRestraint(dto).ConfigureAwait(false);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestraintData to server [{res}]");
            return null;
        }

        _prevRestraintData = res.Value;
        return res.Value;
    }

    public async Task<CharaActiveCollar?> PushNewActiveCollar(CharaActiveCollar newData, DataUpdateType type)
        => await PushNewActiveCollar(_kinksters.GetOnlineUserDatas(), newData, type);

    public async Task<CharaActiveCollar?> PushNewActiveCollar(List<UserData> onlinePlayers, CharaActiveCollar newData, DataUpdateType type)
    {
        if (DataIsDifferent(_prevCollarData, newData) is false)
            return null;

        _prevCollarData = newData;
        Logger.LogDebug($"Pushing CollarChange [{type}] to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);
        // Visuals DataTypeUpdate covers toggling the visual state.
        var dto = new PushClientActiveCollar(onlinePlayers, type)
        {
            Applied = newData.Applied,
            OwnerUIDs = newData.OwnerUIDs,
            Visuals = newData.Visuals,
            Dye1 = newData.Dye1,
            Dye2 = newData.Dye2,
            Moodle = newData.Moodle,
            Writing = newData.Writing,
            EditAccess = newData.CollaredAccess,
            OwnerEditAccess = newData.OwnerAccess
        };

        var res = await _hub.UserPushActiveCollar(dto).ConfigureAwait(false);
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push CollarData to server [{res}]");
            return null;
        }

        _prevCollarData = res.Value;
        return res.Value;
    }

    public async Task<AppliedCursedItem?> PushActiveCursedLoot(List<Guid> activeItems, Guid changeItem, AppliedItem? lootItem)
        => await PushActiveCursedLoot(_kinksters.GetOnlineUserDatas(), activeItems, changeItem, lootItem);

    public async Task<AppliedCursedItem?> PushActiveCursedLoot(List<UserData> onlinePlayers, List<Guid> activeItems, Guid changeItem, AppliedItem? lootItem)
    {
        Logger.LogDebug($"Pushing ActiveCursedLoot to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);
        var dto = new PushClientActiveLoot(onlinePlayers, activeItems, changeItem, lootItem);

        // push out the result and get the data.
        var res = await _hub.UserPushActiveLoot(dto).ConfigureAwait(false);
        // if not successful, log and return null.
        if (res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push ActiveCursedLoot to server [{res}]");
            return null;
        }
        // otherwise return the value.
        return res.Value;
    }

    private async Task PushAliasStateChange(AliasStateChangedMessage msg)
    {
        var toSend = _kinksters.GetOnlineUserDatas().Where(u => msg.Alias.WhitelistedUIDs.Contains(u.UID)).ToList();
        Logger.LogDebug($"Pushing AliasStateChange to {string.Join(", ", toSend.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientAliasState(toSend, msg.Alias.Identifier, msg.Alias.Enabled);
        if (await _hub.UserPushAliasState(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push AliasStateChange update to server. Reason: [{res}]");
    }

    private async Task PushActiveAliasesUpdate(ActiveAliasesChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActiveAliasesUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientActiveAliases(onlinePlayers, msg.ActiveAliases);
        if (await _hub.UserPushActiveAliases(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActiveAliases update to server. Reason: [{res}]");
    }

    public async Task PushValidToysUpdate(ValidToysChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ValidToysUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);
        var dto = new PushClientValidToys(onlinePlayers, msg.ValidToys);
        if (await _hub.UserPushValidToys(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ValidToys update to server. Reason: [{res}]");
    }

    public async Task PushActivePatternUpdate(ActivePatternChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActivePatternUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientActivePattern(onlinePlayers, msg.NewActivePattern, msg.UpdateType);
        if (await _hub.UserPushActivePattern(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActivePattern update to server. Reason: [{res}]");
    }

    public async Task PushActiveAlarmsUpdate(ActiveAlarmsChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActiveAlarmsUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientActiveAlarms(onlinePlayers, msg.ActiveAlarms, msg.ChangedItem, msg.UpdateType);
        if (await _hub.UserPushActiveAlarms(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActiveAlarms update to server. Reason: [{res}]");
    }

    public async Task PushActiveTriggersUpdate(ActiveTriggersChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActivePatternUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientActiveTriggers(onlinePlayers, msg.ActiveTriggers, msg.ChangedItem, msg.UpdateType);
        if (await _hub.UserPushActiveTriggers(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActiveTriggers update to server. Reason: [{res}]");
    }

    private async Task DistributeGagUpdate(GarblerRestriction item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing GagChange [{kind}] to pnline pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeGag(onlinePlayers, item.GagType, item.ToLightItem());
        if (await _hub.UserPushNewGagData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push GagData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed GagData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeRestrictionUpdate(RestrictionItem item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing RestrictionChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeRestriction(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewRestrictionData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push RestrictionData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed RestrictionData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeRestraintSetUpdate(RestraintSet item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing RestraintSetChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeRestraint(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewRestraintData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push RestraintSetData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed RestraintSetData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeCollarUpdate(GagSpeakCollar collar, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing CollarChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeCollar(onlinePlayers, collar.ToLightItem());
        if (await _hub.UserPushNewCollarData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push CollarData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed CollarData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeCursedItemUpdate(CursedItem item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing CursedItemChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeLoot(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewLootData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push CursedItemData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed CursedItemData to server", LoggerType.OnlinePairs);
    }

    // Recipients will know if they should remove the item if they are not contained within the new whitelist, or if it is null.
    private async Task DistributeAliasItemUpdate(AliasTrigger item, StorageChangeType kind)
    {
        var online = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing AliasTriggerChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeAlias(online, item.Identifier, item);
        if (await _hub.UserPushNewAliasData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push CursedItemData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed CursedItemData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributePatternUpdate(Pattern item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing PatternChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangePattern(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewPatternData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push PatternData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed PatternData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeAlarmUpdate(Alarm item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing AlarmChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeAlarm(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewAlarmData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push AlarmData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed AlarmData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeTriggerUpdate(Trigger item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing TriggerChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangeTrigger(onlinePlayers, item.Identifier, item.ToLightItem());
        if (await _hub.UserPushNewTriggerData(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push TriggerData to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed TriggerData to server", LoggerType.OnlinePairs);
    }

    private async Task DistributeAllowancesUpdate(GSModule module, IEnumerable<string> allowedUids)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing AllowancesUpdate for GagspeakModule [{module}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientAllowances(onlinePlayers, module, allowedUids.ToArray());
        if (await _hub.UserPushNewAllowances(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push AllowancesUpdate to paired Kinksters. [{res}]");
        else
            Logger.LogDebug("Successfully pushed AllowancesUpdate to server", LoggerType.OnlinePairs);
    }
}
