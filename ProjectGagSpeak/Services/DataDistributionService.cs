using Dalamud.Interface;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
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
    private readonly KinksterManager _pairs;
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

    public DataDistributionService(ILogger<DataDistributionService> logger, GagspeakMediator mediator,
        MainHub hub, ClientAchievements achievements, KinksterManager pairManager, GagRestrictionManager gags,
        RestrictionManager restrictions, RestraintManager restraints, CursedLootManager cursedLoot,
        PuppeteerManager puppetManager, PatternManager patterns, AlarmManager alarms,
        TriggerManager triggers, TraitAllowanceManager traitAllowances)
        : base(logger, mediator)
    {
        _hub = hub;
        _achievements = achievements;
        _pairs = pairManager;
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
        Mediator.Subscribe<PairWentOnlineMessage>(this, arg     => _newOnlineKinksters.Add(arg.UserData));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, msg => _newVisibleKinksters.Add(msg.Player.OnlineUser.User));

        // Generic Updaters
        Mediator.Subscribe<PushGlobalPermChange>(this, arg      => _hub.UserChangeOwnGlobalPerm(new(MainHub.PlayerUserData, new KeyValuePair<string, object>
            (arg.PermName, arg.NewValue), UpdateDir.Own, MainHub.PlayerUserData)).ConfigureAwait(false));

        // Visible Data Updaters
        Mediator.Subscribe<MoodlesApplyStatusToPair>(this, msg  => _hub.UserApplyMoodlesByStatus(msg.StatusDto).ConfigureAwait(false));
        Mediator.Subscribe<IpcDataChangedMessage>(this, msg     => DistributeDataVisible(_pairs.GetVisibleUsers(), msg.NewIpcData, msg.UpdateType).ConfigureAwait(false));

        // Online Data Updaters
        Mediator.Subscribe<MainHubConnectedMessage>(this, _         => PushCompositeData(_pairs.GetOnlineUserDatas()).ConfigureAwait(false));
        Mediator.Subscribe<GagDataChangedMessage>(this, arg         => DistributeDataGag(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
        Mediator.Subscribe<RestrictionDataChangedMessage>(this, arg => DistributeDataRestriction(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
        Mediator.Subscribe<RestraintDataChangedMessage>(this, arg   => DistributeDataRestraint(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
        Mediator.Subscribe<AliasGlobalUpdateMessage>(this, arg      => DistributeDataGlobalAlias(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
        Mediator.Subscribe<AliasPairUpdateMessage>(this, arg        => DistributeDataUniqueAlias(arg).ConfigureAwait(false));
        Mediator.Subscribe<ToyboxDataChangedMessage>(this, arg      => DistributeDataToybox(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
        Mediator.Subscribe<LightStorageDataChangedMessage>(this, arg=> DistributeDataStorage(_pairs.GetOnlineUserDatas(), arg).ConfigureAwait(false));
    }

    // Storage of previously sent data, to avoid excessive calls when nothing changes.
    private CharaIPCData            _prevIpcData = new CharaIPCData();
    private ActiveGagSlot?          _prevGagData;
    private ActiveRestriction?      _prevRestrictionData;
    private CharaActiveRestraint?   _prevRestraintData;
    private AliasTrigger?           _prevGlobalAliasData;
    private AliasTrigger?           _prevPairAliasData;
    private CharaToyboxData?        _prevToyboxData;
    private CharaLightStorageData?  _prevLightStorageData;

    private void DelayedFrameworkOnUpdate()
    {
        // Do not process if not data synced.
        if (!MainHub.IsConnectionDataSynced) 
            return;

        // Handle Online Players.
        if (_newOnlineKinksters.Count > 0)
        {
            var newOnlinePairs = _newOnlineKinksters.ToList();
            _newOnlineKinksters.Clear();
            PushCompositeData(newOnlinePairs).ConfigureAwait(false);
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
            GagItems = _gagManager.Storage.ToLightStorage(),
            Restrictions = _restrictionManager.Storage.Select(r => r.ToLightRestriction()).ToArray(),
            Restraints = _restraintManager.Storage.Select(r => r.ToLightRestraint()).ToArray(),
            CursedItems = _cursedManager.Storage.ActiveItems.Select(x => x.ToLightItem()).ToArray(),
            Patterns = _patternManager.Storage.Select(p => p.ToLightPattern()).ToArray(),
            Alarms = _alarmManager.Storage.Select(a => a.ToLightAlarm()).ToArray(),
            Triggers = _triggerManager.Storage.Select(t => t.ToLightTrigger()).ToArray(),
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



    /// <summary> 
    ///     Pushes all our Player Data to all online pairs once connected, or to any new pairs that go online.
    /// </summary>
    /// <remarks>
    ///     TODO: Make this be more optimized so that the composite data is calculated less if possible? Idk.
    /// </remarks>
    private async Task PushCompositeData(List<UserData> newOnlinePairs)
    {
        // if not connected and data synced just add the pairs to the list. (Extra safety net)
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Not pushing Composite Data, not connected to server or data not synced.", LoggerType.ApiCore);
            _newOnlineKinksters.UnionWith(newOnlinePairs);
            return;
        }

        if (newOnlinePairs.Count <= 0)
            return;

        try
        {
            var newLightStorage = GetLatestLightStorage();

            var data = new CharaCompositeData()
            {
                Gags = _gagManager.ServerGagData ?? throw new Exception("ActiveGagData was null!"),
                Restrictions = _restrictionManager.ServerRestrictionData ?? throw new Exception("ActiveRestrictionsData was null!"),
                Restraint = _restraintManager.ServerData ?? throw new Exception("ActiveRestraintData was null!"),
                ActiveCursedItems = _cursedManager.Storage.ActiveItems.Select(x => x.Identifier).ToList(),
                GlobalAliasData = _puppetManager.GlobalAliasStorage,
                PairAliasData = _puppetManager.PairAliasStorage.ToDictionary(),
                ToyboxData = new CharaToyboxData()
                {
                    ActivePattern = _patternManager.ActivePattern?.Identifier ?? Guid.Empty,
                    ActiveAlarms = _alarmManager.ActiveAlarms.Where(a => a.Enabled).Select(x => x.Identifier).ToList(),
                    ActiveTriggers = _triggerManager.Storage.Where(a => a.Enabled).Select(x => x.Identifier).ToList(),
                },
                LightStorageData = newLightStorage,
            };
            
            Logger.LogDebug($"Pushing CharaCompositeData to: {string.Join(", ", newOnlinePairs.Select(v => v.UID))}", LoggerType.ApiCore);
            var result = await _hub.UserPushData(new(newOnlinePairs, data, true)).ConfigureAwait(false);
            if(result.ErrorCode is GagSpeakApiEc.Success)
            {
                Logger.LogDebug("Successfully pushed Composite Data to server", LoggerType.ApiCore);
                _prevLightStorageData = newLightStorage;
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
        if (await _hub.UserPushDataIpc(new(visChara, newData, kind)).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push IpcData to server Reason: " + res);
    }

    /// <summary>
    ///     A publically accessible variant that is not mediator dependant.
    /// </summary>
    /// <remarks> Useful for handlers and services that must await the callback. </remarks>
    /// <returns> True if the operation returned successfully, false if it failed. </returns>
    public async Task<bool> PushGagTriggerAction(int layerIdx, ActiveGagSlot newData, DataUpdateType type)
        => await DistributeDataGag(_pairs.GetOnlineUserDatas(), new(type, layerIdx, newData));

    /// <summary> Pushes the new GagData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<bool> DistributeDataGag(List<UserData> onlinePlayers, GagDataChangedMessage msg)
    {
        if (DataIsDifferent(_prevGagData, msg.NewData) is false)
            return false;

        _prevGagData = msg.NewData;
        Logger.LogDebug($"Pushing GagChange [{msg.UpdateType}] to: {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientGagSlotUpdate(onlinePlayers, msg.UpdateType)
        {
            Layer = msg.Layer,
            Gag = msg.NewData.GagItem,
            Enabler = msg.NewData.Enabler,
            Padlock = msg.NewData.Padlock,
            Password = msg.NewData.Password,
            Timer = msg.NewData.Timer,
            Assigner = msg.NewData.PadlockAssigner
        };

        if (await _hub.UserPushDataGags(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push GagData to server [{res}]");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     A publically accessible variant that is not mediator dependant.
    /// </summary>
    /// <remarks> Useful for handlers and services that must await the callback. </remarks>
    /// <returns> True if the operation returned successfully, false if it failed. </returns>
    public async Task<bool> PushRestrictionTriggerAction(int layerIdx, ActiveRestriction newData, DataUpdateType type)
        => await DistributeDataRestriction(_pairs.GetOnlineUserDatas(), new(type, layerIdx, newData));

    /// <summary> Pushes the new RestrictionData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<bool> DistributeDataRestriction(List<UserData> onlinePlayers, RestrictionDataChangedMessage msg)
    {
        if (DataIsDifferent(_prevRestrictionData, msg.NewData) is false)
            return false;

        _prevRestrictionData = msg.NewData;
        Logger.LogDebug($"Pushing RestrictionChange [{msg.UpdateType}] to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientRestrictionUpdate(onlinePlayers, msg.UpdateType)
        {
            Layer = msg.Layer,
            Identifier = msg.NewData.Identifier,
            Enabler = msg.NewData.Enabler,
            Padlock = msg.NewData.Padlock,
            Password = msg.NewData.Password,
            Timer = msg.NewData.Timer,
            Assigner = msg.NewData.PadlockAssigner
        };

        if (await _hub.UserPushDataRestrictions(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestrictionData to server [{res}]");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     A publically accessible variant that is not mediator dependant.
    /// </summary>
    /// <remarks> Useful for handlers and services that must await the callback. </remarks>
    /// <returns> True if the operation returned successfully, false if it failed. </returns>
    public async Task<bool> PushRestraintTriggerAction(CharaActiveRestraint newData, DataUpdateType type)
        => await DistributeDataRestraint(_pairs.GetOnlineUserDatas(), new(type, newData));

    /// <summary> Pushes the new RestraintData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task<bool> DistributeDataRestraint(List<UserData> onlinePlayers, RestraintDataChangedMessage msg)
    {
        if (DataIsDifferent(_prevRestraintData, msg.NewData) is false)
            return false;

        _prevRestraintData = msg.NewData;
        Logger.LogDebug($"Pushing RestraintData to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientRestraintUpdate(onlinePlayers, msg.UpdateType)
        {
            ActiveSetId = msg.NewData.Identifier,
            ActiveLayers = msg.NewData.ActiveLayers,
            Enabler = msg.NewData.Enabler,
            Padlock = msg.NewData.Padlock,
            Password = msg.NewData.Password,
            Timer = msg.NewData.Timer,
            Assigner = msg.NewData.PadlockAssigner
        };

        if (await _hub.UserPushDataRestraint(dto) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
        {
            Logger.LogError($"Failed to push RestraintData to server [{res}]");
            return false;
        }

        return true;
    }

    /// <summary> Pushes the new Global AliasTrigger update to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataGlobalAlias(List<UserData> onlinePlayers, AliasGlobalUpdateMessage msg)
    {
        if (DataIsDifferent(_prevGlobalAliasData, msg.NewData) is false)
            return;

        _prevGlobalAliasData = msg.NewData;
        Logger.LogDebug($"Pushing Updated Global AliasTrigger to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))}", LoggerType.OnlinePairs);

        var dto = new PushClientAliasGlobalUpdate(onlinePlayers, msg.NewData);
        if (await _hub.UserPushAliasGlobalUpdate(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push Global AliasTrigger to server Reason: " + res);
    }

    /// <summary> Pushes the new AliasTrigger specific to a Kinkster to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataUniqueAlias(AliasPairUpdateMessage msg)
    {
        if (DataIsDifferent(_prevPairAliasData, msg.NewData) is false)
            return;

        _prevPairAliasData = msg.NewData;
        Logger.LogDebug($"Pushing AliasPairUpdate to {msg.IntendedUser.AliasOrUID}", LoggerType.OnlinePairs);

        var dto = new PushClientAliasUniqueUpdate(msg.IntendedUser, msg.NewData);
        if (await _hub.UserPushAliasUniqueUpdate(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push AliasPairUpdate to server Reason: " + res);
    }

    /// <summary> Pushes the new ToyboxData to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataToybox(List<UserData> onlinePlayers, ToyboxDataChangedMessage msg)
    {
        if (DataIsDifferent(_prevToyboxData, msg.NewData) is false)
            return;

        _prevToyboxData = msg.NewData;
        Logger.LogDebug($"Pushing ToyboxData to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{msg.UpdateType}]", LoggerType.OnlinePairs);

        var dto = new PushClientToyboxUpdate(onlinePlayers, msg.NewData, msg.UpdateType)
        {
            AffectedIdentifier = msg.InteractionId,
        };

        if (await _hub.UserPushDataToybox(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push ToyboxData to server Reason: " + res);
    }

    /// <summary> Pushes the new LightStorage to the server. </summary>
    /// <remarks> If this call fails, the previous data will not be updated. </remarks>
    private async Task DistributeDataStorage(List<UserData> onlinePlayers, LightStorageDataChangedMessage msg)
    {
        if (DataIsDifferent(_prevLightStorageData, msg.NewData) is false)
            return;

        _prevLightStorageData = msg.NewData;
        Logger.LogDebug($"Pushing LightStorage to {string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID))} [{DataUpdateType.StorageUpdated}]", LoggerType.OnlinePairs);

        var dto = new PushClientLightStorageUpdate(onlinePlayers, msg.NewData);

        if (await _hub.UserPushDataLightStorage(dto).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push LightStorage to server Reason: " + res);
    }
}
