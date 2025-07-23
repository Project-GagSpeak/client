using CkCommons;
using GagSpeak.FileSystems;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.Services;

/// <summary> Creates various calls to the server based on invoked events. </summary>
public sealed class DataDistributionService : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly ClientAchievements _achievements;
    private readonly KinksterManager _kinksters;
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly CursedLootManager _cursedManager;
    private readonly PuppeteerManager _puppetManager;
    private readonly PatternManager _patternManager;
    private readonly AlarmManager _alarmManager;
    private readonly TriggerManager _triggerManager;
    private readonly TraitAllowanceManager _traitManager;

    private SemaphoreSlim _updateSlim = new SemaphoreSlim(1, 1);
    private readonly HashSet<UserData> _newVisibleKinksters = [];
    private readonly HashSet<UserData> _newOnlineKinksters = [];

    public DataDistributionService(ILogger<DataDistributionService> logger,
        GagspeakMediator mediator,
        MainHub hub,
        ClientAchievements achievements,
        KinksterManager kinksters,
        GagRestrictionManager gags,
        RestrictionManager restrictions,
        RestraintManager restraints,
        CursedLootManager cursedLoot,
        PuppeteerManager puppetManager,
        PatternManager patterns,
        AlarmManager alarms,
        TriggerManager triggers,
        TraitAllowanceManager traitAllowances)
        : base(logger, mediator)
    {
        _hub = hub;
        _achievements = achievements;
        _kinksters = kinksters;
        _gagManager = gags;
        _restrictionManager = restrictions;
        _restraintManager = restraints;
        _cursedManager = cursedLoot;
        _puppetManager = puppetManager;
        _patternManager = patterns;
        _alarmManager = alarms;
        _triggerManager = triggers;
        _traitManager = traitAllowances;

        // Achievement Handling
        Mediator.Subscribe<SendAchievementData>(this, (_) => UpdateAchievementData().ConfigureAwait(false));
        Mediator.Subscribe<UpdateCompletedAchievements>(this, (_) => UpdateTotalEarned().ConfigureAwait(false));

        // Updates.
        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => DelayedFrameworkOnUpdate());

        // Kinkster Pair management.
        Mediator.Subscribe<PairWentOnlineMessage>(this, arg =>
        {
            if (!MainHub.IsConnectionDataSynced)
                return;
            _newOnlineKinksters.Add(arg.UserData);
        });
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, msg => _newVisibleKinksters.Add(msg.Player.OnlineUser.User));

        // Generic Updaters
        Mediator.Subscribe<PushGlobalPermChange>(this, arg => _hub.UserChangeOwnGlobalPerm(arg.PermName, arg.NewValue).ConfigureAwait(false));
        // Visible Data Updaters
        Mediator.Subscribe<MoodlesApplyStatusToPair>(this, msg => _hub.UserApplyMoodlesByStatus(msg.StatusDto).ConfigureAwait(false));
        Mediator.Subscribe<IpcDataChangedMessage>(this, msg => DistributeDataVisible(_kinksters.GetVisibleUsers(), msg.NewIpcData, msg.UpdateType).ConfigureAwait(false));

        // Online Data Updaters
        Mediator.Subscribe<MainHubConnectedMessage>(this, _ => PushCompositeData(_kinksters.GetOnlineUserDatas()).ConfigureAwait(false));
        Mediator.Subscribe<ActiveGagsChangeMessage>(this, arg => PushActiveGagSlotUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveRestrictionsChangeMessage>(this, arg => PushActiveRestrictionUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveRestraintSetChangeMessage>(this, arg => PushActiveRestraintUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<AliasGlobalUpdateMessage>(this, arg => DistributeDataGlobalAlias(arg).ConfigureAwait(false));
        Mediator.Subscribe<AliasPairUpdateMessage>(this, arg => DistributeDataUniqueAlias(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActivePatternChangedMessage>(this, arg => PushActivePatternUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveAlarmsChangedMessage>(this, arg => PushActiveAlarmsUpdate(arg).ConfigureAwait(false));
        Mediator.Subscribe<ActiveTriggersChangedMessage>(this, arg => PushActiveTriggersUpdate(arg).ConfigureAwait(false));

        Mediator.Subscribe<ConfigGagRestrictionChanged>(this, msg => DistributeGagUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigRestrictionChanged>(this, msg => DistributeRestrictionUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigRestraintSetChanged>(this, msg => DistributeRestraintSetUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigCursedItemChanged>(this, msg => DistributeCursedItemUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigPatternChanged>(this, msg => DistributePatternUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigAlarmChanged>(this, msg => DistributeAlarmUpdate(msg.Item, msg.Type).ConfigureAwait(false));
        Mediator.Subscribe<ConfigTriggerChanged>(this, msg => DistributeTriggerUpdate(msg.Item, msg.Type).ConfigureAwait(false));
    }

    // Idk why we need this really, anymore, but whatever i guess. If it helps it helps.
    private CharaIPCData _prevIpcData = new CharaIPCData();
    private ActiveGagSlot? _prevGagData;
    private ActiveRestriction? _prevRestrictionData;
    private CharaActiveRestraint? _prevRestraintData;

    private void DelayedFrameworkOnUpdate()
    {
        // Do not process if not data synced.
        if (!MainHub.IsConnectionDataSynced)
            return;

        // Handle Online Players.
        if (_newOnlineKinksters.Count > 0)
        {
            var newOnlinekinksters = _newOnlineKinksters.ToList();
            _newOnlineKinksters.Clear();
            PushCompositeData(newOnlinekinksters).ConfigureAwait(false);
        }

        // Handle Visible Players.
        if (PlayerData.Available && _newVisibleKinksters.Count > 0)
        {
            var newVisiblePlayers = _newVisibleKinksters.ToList();
            _newVisibleKinksters.Clear();
            DistributeDataVisible(newVisiblePlayers, _prevIpcData, DataUpdateType.UpdateVisible).ConfigureAwait(false);
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

    private CharaLightStorageData GetLatestLightStorage()
    {
        return new CharaLightStorageData()
        {
            GagItems = _gagManager.Storage.ToLightStorage().ToArray(),
            Restrictions = _restrictionManager.Storage.Select(r => r.ToLightItem()).ToArray(),
            Restraints = _restraintManager.Storage.Select(r => r.ToLightItem()).ToArray(),
            CursedItems = _cursedManager.Storage.ActiveItems.Select(x => x.ToLightItem()).ToArray(),
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
        var result = await _hub.UserUpdateAchievementData(new(MainHub.PlayerUserData, dataString)).ConfigureAwait(false);
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

    // An updater slim is nessisary for this.
    private async Task UpdateTotalEarned()
    {
        await _updateSlim.WaitAsync();
        try
        {
            KinkPlateContent currentContent;
            if (KinkPlateService.KinkPlates.TryGetValue(MainHub.PlayerUserData, out var existing))
                currentContent = existing.KinkPlateInfo;
            else
            {
                var response = await _hub.UserGetKinkPlate(new KinksterBase(MainHub.PlayerUserData));
                currentContent = response.Info;
            }

            Logger.LogDebug($"Updating KinkPlate™ with {ClientAchievements.Completed} Completions.", LoggerType.Achievements);
            currentContent.CompletedAchievementsTotal = ClientAchievements.Completed;
            await _hub.UserSetKinkPlateContent(new(MainHub.PlayerUserData, currentContent));
        }
        catch (Exception ex)
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
                ActiveCursedItems = _cursedManager.Storage.ActiveItems.Select(x => x.Identifier).ToList(),
                GlobalAliasData = _puppetManager.GlobalAliasStorage,
                PairAliasData = _puppetManager.PairAliasStorage.ToDictionary(),
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
        catch (Exception ex)
        {
            Logger.LogError("Failed to push Composite Data to server: " + ex);
            return;
        }
    }

    private async Task DistributeDataVisible(List<UserData> visChara, CharaIPCData newData, DataUpdateType kind)
    {
        // Do not process if not data synced.
        if (!MainHub.IsConnectionDataSynced)
        {
            _newVisibleKinksters.UnionWith(visChara);
            return;
        }

        if (DataIsDifferent(_prevIpcData, newData) is false)
            return;

        _prevIpcData = newData;

        Logger.LogDebug($"Pushing IPCData to {string.Join(", ", visChara.Select(v => v.AliasOrUID))} [{kind}]", LoggerType.VisiblePairs);
        if (await _hub.UserPushActiveIpc(new(visChara, newData, kind)).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push IpcData to server Reason: " + res);
    }

    /// <summary>
    ///     A publically accessible variant that is not mediator dependant.
    /// </summary>
    /// <remarks> Useful for handlers and services that must await the callback. </remarks>
    /// <returns> True if the operation returned successfully, false if it failed. </returns>
    public async Task<GagSpeakApiEc> PushGagTriggerAction(int layerIdx, ActiveGagSlot newData, DataUpdateType type)
        => await PushNewActiveGagSlot(_kinksters.GetOnlineUserDatas(), layerIdx, newData, type);

    /// <summary> Pushes the new GagData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<GagSpeakApiEc> PushActiveGagSlotUpdate(ActiveGagsChangeMessage msg)
        => await PushNewActiveGagSlot(_kinksters.GetOnlineUserDatas(), msg.Layer, msg.NewData, msg.UpdateType);

    public async Task<GagSpeakApiEc> PushNewActiveGagSlot(int layer, ActiveGagSlot slot, DataUpdateType type)
        => await PushNewActiveGagSlot(_kinksters.GetOnlineUserDatas(), layer, slot, type);

    public async Task<GagSpeakApiEc> PushNewActiveGagSlot(List<UserData> onlinePlayers, int layer, ActiveGagSlot slot, DataUpdateType type)
    {
        if (DataIsDifferent(_prevGagData, slot) is false)
            return GagSpeakApiEc.DuplicateEntry;

        _prevGagData = slot;
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

        if (await _hub.UserPushActiveGags(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push GagData to server [{res}]");
            return res.ErrorCode;
        }
        else
        {
            return GagSpeakApiEc.Success;
        }
    }

    /// <summary> Pushes the new RestrictionData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<GagSpeakApiEc> PushActiveRestrictionUpdate(ActiveRestrictionsChangeMessage msg)
        => await PushNewActiveRestriction(_kinksters.GetOnlineUserDatas(), msg.Layer, msg.NewData, msg.UpdateType);

    public async Task<GagSpeakApiEc> PushNewActiveRestriction(int layerIdx, ActiveRestriction newData, DataUpdateType type)
        => await PushNewActiveRestriction(_kinksters.GetOnlineUserDatas(), layerIdx, newData, type);

    public async Task<GagSpeakApiEc> PushNewActiveRestriction(List<UserData> onlinePlayers, int layerIdx, ActiveRestriction newData, DataUpdateType type)
    {
        if (DataIsDifferent(_prevRestrictionData, newData) is false)
            return GagSpeakApiEc.DuplicateEntry;

        _prevRestrictionData = newData;
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

        if (await _hub.UserPushActiveRestrictions(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestrictionData to server [{res}]");
            return res.ErrorCode;
        }

        return GagSpeakApiEc.Success;
    }

    /// <summary> Pushes the new RestraintData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<GagSpeakApiEc> PushActiveRestraintUpdate(ActiveRestraintSetChangeMessage msg)
        => await PushActiveRestraintUpdate(_kinksters.GetOnlineUserDatas(), msg.NewData, msg.UpdateType);

    public async Task<GagSpeakApiEc> PushActiveRestraintUpdate(CharaActiveRestraint newData, DataUpdateType type)
        => await PushActiveRestraintUpdate(_kinksters.GetOnlineUserDatas(), newData, type);

    public async Task<GagSpeakApiEc> PushActiveRestraintUpdate(List<UserData> onlinePlayers, CharaActiveRestraint newData, DataUpdateType type)

    {
        if (DataIsDifferent(_prevRestraintData, newData) is false)
            return GagSpeakApiEc.DuplicateEntry;

        _prevRestraintData = newData;
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

        if (await _hub.UserPushActiveRestraint(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestraintData to server [{res}]");
            return res.ErrorCode;
        }

        return GagSpeakApiEc.Success;
    }

    /// <summary> Pushes the new Global AliasTrigger update to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataGlobalAlias(AliasGlobalUpdateMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing Updated Global AliasTrigger to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientAliasGlobalUpdate(onlinePlayers, msg.AliasId, msg.NewData);
        if (await _hub.UserPushAliasGlobalUpdate(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push Global AliasTrigger to server Reason: " + res);
    }

    /// <summary> Pushes the new AliasTrigger specific to a Kinkster to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataUniqueAlias(AliasPairUpdateMessage msg)
    {
        Logger.LogDebug($"Pushing AliasPairUpdate to {msg.IntendedUser.AliasOrUID}", LoggerType.OnlinePairs);

        var dto = new PushClientAliasUniqueUpdate(msg.IntendedUser, msg.AliasId, msg.NewData);
        if (await _hub.UserPushAliasUniqueUpdate(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push AliasPairUpdate to server. [{res}]");
    }

    private async Task PushActivePatternUpdate(ActivePatternChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActivePatternUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientActivePattern(onlinePlayers, msg.NewActivePattern, msg.UpdateType);
        if (await _hub.UserPushActivePattern(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActivePattern update to server. Reason: [{res}]");
    }

    private async Task PushActiveAlarmsUpdate(ActiveAlarmsChangedMessage msg)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing ActiveAlarmsUpdate to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientActiveAlarms(onlinePlayers, msg.ActiveAlarms, msg.ChangedItem, msg.UpdateType);
        if (await _hub.UserPushActiveAlarms(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError($"Failed to push ActiveAlarms update to server. Reason: [{res}]");
    }

    private async Task PushActiveTriggersUpdate(ActiveTriggersChangedMessage msg)
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

    private async Task DistributePatternUpdate(Pattern item, StorageChangeType kind)
    {
        var onlinePlayers = _kinksters.GetOnlineUserDatas();
        Logger.LogDebug($"Pushing PatternChange [{kind}] to online pairs.", LoggerType.OnlinePairs);
        var dto = new PushClientDataChangePattern(onlinePlayers, item.Identifier, item.ToLightItem());
        // do the distribution magic thing Y I P E E
    }

    private async Task DistributeAlarmUpdate(Alarm item, StorageChangeType kind)
    {
        // do the distribution magic thing Y I P E E
    }

    private async Task DistributeTriggerUpdate(Trigger item, StorageChangeType kind)
    {
        // do the distribution magic thing
    }

    private async Task DistributeAllowancesUpdate(GagspeakModule module, List<string> allowedUids)
    {

    }
}
