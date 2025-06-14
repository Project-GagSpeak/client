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
    private readonly PlayerData _player;
    private readonly PairManager _pairs;
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly CursedLootManager _cursedManager;
    private readonly PuppeteerManager _puppetManager;
    private readonly PatternManager _patternManager;
    private readonly AlarmManager _alarmManager;
    private readonly TriggerManager _triggerManager;
    private readonly TraitAllowanceManager _traitManager;

    private readonly HashSet<UserData> _newVisibleKinksters = [];
    private readonly HashSet<UserData> _newOnlineKinksters = [];

    public DataDistributionService(
        ILogger<DataDistributionService> logger,
        GagspeakMediator mediator,
        MainHub hub,
        PlayerData player,
        PairManager pairManager,
        GagRestrictionManager gagManager,
        RestrictionManager restrictionManager,
        RestraintManager restraintManager,
        CursedLootManager cursedManager,
        PuppeteerManager puppetManager,
        PatternManager patternManager,
        AlarmManager alarmManager,
        TriggerManager triggerManager,
        TraitAllowanceManager traitManager)
        : base(logger, mediator)
    {
        _hub = hub;
        _player = player;
        _pairs = pairManager;
        _gagManager = gagManager;
        _restrictionManager = restrictionManager;
        _restraintManager = restraintManager;
        _cursedManager = cursedManager;
        _puppetManager = puppetManager;
        _patternManager = patternManager;
        _alarmManager = alarmManager;
        _triggerManager = triggerManager;
        _traitManager = traitManager;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => DelayedFrameworkOnUpdate());

        Mediator.Subscribe<MainHubConnectedMessage>(this, _     => PushCompositeData(_pairs.GetOnlineUserDatas()).ConfigureAwait(false));
        Mediator.Subscribe<PairWentOnlineMessage>(this, arg     => _newOnlineKinksters.Add(arg.UserData));
        Mediator.Subscribe<PairHandlerVisibleMessage>(this, msg => _newVisibleKinksters.Add(msg.Player.OnlineUser.User));

        // Generic Updaters
        Mediator.Subscribe<PushGlobalPermChange>(this, arg      => _hub.UserChangeOwnGlobalPerm(new(MainHub.PlayerUserData, new KeyValuePair<string, object>
            (arg.PermName, arg.NewValue), UpdateDir.Own, MainHub.PlayerUserData)).ConfigureAwait(false));

        // Visible Data Updaters
        Mediator.Subscribe<MoodlesApplyStatusToPair>(this, msg  => _hub.UserApplyMoodlesByStatus(msg.StatusDto).ConfigureAwait(false));
        Mediator.Subscribe<IpcDataChangedMessage>(this, msg     => DistributeDataVisible(_pairs.GetVisibleUsers(), msg.NewIpcData, msg.UpdateType).ConfigureAwait(false));

        // Online Data Updaters
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
        if (!MainHub.IsConnected) 
            return;

        // Handle Online Players.
        if (_newOnlineKinksters.Count > 0)
        {
            var newOnlinePairs = _newOnlineKinksters.ToList();
            _newOnlineKinksters.Clear();
            PushCompositeData(newOnlinePairs).ConfigureAwait(false);
        }

        // Handle Visible Players.
        if (_player.IsPresent && _newVisibleKinksters.Count > 0)
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

    /// <summary> Pushes all our Player Data to all online pairs once connected. </summary>
    private async Task PushCompositeData(List<UserData> newOnlinePairs)
    {
        if (newOnlinePairs.Count <= 0)
            return;

        try
        {
            var newLightStorage = GetLatestLightStorage();

            var data = new CharaCompositeData()
            {
                Gags = _gagManager.ServerGagData ?? throw new Exception("ActiveGagData was null!"),
                Restrictions = _restrictionManager.ServerRestrictionData ?? throw new Exception("ActiveRestrictionsData was null!"),
                Restraint = _restraintManager.ServerRestraintData ?? throw new Exception("ActiveRestraintData was null!"),
                ActiveCursedItems = _cursedManager.Storage.ActiveItems.Select(x => x.Identifier),
                GlobalAliasData = _puppetManager.GlobalAliasStorage,
                PairAliasData = _puppetManager.PairAliasStorage,
                ToyboxData = new CharaToyboxData()
                {
                    ActivePattern = _patternManager.ActivePattern?.Identifier ?? Guid.Empty,
                    ActiveAlarms = _alarmManager.ActiveAlarms.Where(a => a.Enabled).Select(x => x.Identifier).ToList(),
                    ActiveTriggers = _triggerManager.Storage.Where(a => a.Enabled).Select(x => x.Identifier).ToList(),
                },
                LightStorageData = newLightStorage,
            };
            
            Logger.LogDebug("new Online Pairs Identified, pushing latest Composite data", LoggerType.OnlinePairs);
            if (await _hub.UserPushData(new(newOnlinePairs, data, false)) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
                Logger.LogError("Failed to push Gag Data to server Reason: " + res.ErrorCode);

            _prevLightStorageData = newLightStorage;
        }
        catch (Exception ex)
        {
            Logger.LogError("Failed to push Composite Data to server: " + ex);
            return;
        }
    }

    private async Task DistributeDataVisible(List<UserData> visChara, CharaIPCData newData, DataUpdateType kind)
    {
        if (DataIsDifferent(_prevIpcData, newData) is false)
            return;

        _prevIpcData = newData;

        Logger.LogDebug($"Pushing IPCData to {string.Join(", ", visChara.Select(v => v.AliasOrUID))} [{kind}]", LoggerType.VisiblePairs);
        if (await _hub.UserPushDataIpc(new(visChara, newData, kind)).ConfigureAwait(false) is { } res && res.ErrorCode is not GagSpeakApiEc.Success)
            Logger.LogError("Failed to push Gag Data to server Reason: " + res);
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
            LayersBitfield = msg.NewData.LayersBitfield,
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
