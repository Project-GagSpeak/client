using GagSpeak.PlayerState.Listener;
using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;

namespace GagSpeak.PlayerData.Pairs;

public class OnlinePairManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly GagRestrictionManager _gagManager;
    private readonly RestrictionManager _restrictionManager;
    private readonly RestraintManager _restraintManager;
    private readonly PairManager _pairManager;

    // if you are able to get the VisualStateListener in here without any conflicts that would be the ideal solution to this hell.
    private readonly VisualStateListener _visualListener;

    private readonly HashSet<UserData> _newOnlinePairs = [];

    // A temporary storage of the last send data of our client to other players.
    // We use this for comparison to know if the data even changed at all.
    private ActiveGagSlot? _lastGagData;
    private ActiveRestriction? _lastRestrictionData;
    private CharaActiveRestraint? _lastRestraintData;
    private CharaOrdersData? _lastOrdersData;
    private CharaAliasData? _lastAliasData;
    private CharaToyboxData? _lastToyboxData;
    private CharaLightStorageData? _lastLightStorage;
    private string _lastShockPermShareCode = string.Empty;

    public OnlinePairManager(ILogger<OnlinePairManager> logger, GagspeakMediator mediator,
        MainHub hub, PairManager pairManager) : base(logger, mediator)
    {
        _hub = hub;
        _pairManager = pairManager;

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => FrameworkOnUpdate());

        // Subscriber to update our composite data after a safeword.
        Mediator.Subscribe<UpdateAllOnlineWithCompositeMessage>(this, (_) => PushCompositeData(_pairManager.GetOnlineUserDatas()));

        Mediator.Subscribe<MainHubConnectedMessage>(this, (_) => PushCompositeData(_pairManager.GetOnlineUserDatas()));
        Mediator.Subscribe<PairWentOnlineMessage>(this, (msg) => _newOnlinePairs.Add(msg.UserData));

        Mediator.Subscribe<GagDataChangedMessage>(this, (msg) =>
        {
            var newData = msg.NewData;
            if (_lastGagData is null || !Equals(newData, _lastGagData))
            {
                _lastGagData = newData;
                PushGagData(_pairManager.GetOnlineUserDatas(), msg.UpdateType, msg.Layer);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<RestrictionDataChangedMessage>(this, (msg) =>
        {
            var newData = msg.NewData;
            if (_lastRestrictionData is null || !Equals(newData, _lastRestrictionData))
            {
                _lastRestrictionData = newData;
                PushRestriction(_pairManager.GetOnlineUserDatas(), msg.UpdateType, msg.Layer);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<RestraintDataChangedMessage>(this, (msg) =>
        {
            var newData = msg.NewData;
            if (_lastRestraintData is null || !Equals(newData, _lastRestraintData))
            {
                _lastRestraintData = newData;
                PushActiveRestraint(_pairManager.GetOnlineUserDatas(), msg.UpdateType);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<OrdersDataChangedMessage>(this, (msg) =>
        {
            // dont push this idk. Its not added yet.
            // PushOrdersData(_pairManager.GetOnlineUserDatas(), msg.UpdateType);
        });

        Mediator.Subscribe<AliasDataChangedMessage>(this, (msg) =>
        {
            var newData = msg.NewData;
            if (_lastAliasData is null || !Equals(newData, _lastAliasData))
            {
                _lastAliasData = newData;
                PushAliasListData(msg.IntendedUser, msg.UpdateType);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<ToyboxDataChangedMessage>(this, (msg) =>
        {
            var newData = msg.NewData;
            if (_lastToyboxData is null || !Equals(newData, _lastToyboxData))
            {
                _lastToyboxData = newData;
                PushToyboxData(_pairManager.GetOnlineUserDatas(), msg.UpdateType);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<LightStorageDataChangedMessage>(this, (msg) =>
        {
            var newLightStorageData = msg.CharacterStorageData;
            if (_lastLightStorage == null || !Equals(newLightStorageData, _lastLightStorage))
            {
                _lastLightStorage = newLightStorageData;
                PushLightStorageData(_pairManager.GetOnlineUserDatas());
            }
            else
            {
                Logger.LogDebug("Light-Storage Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });
    }

    private void FrameworkOnUpdate()
    {
        if (!MainHub.IsConnected || !_newOnlinePairs.Any()) 
            return;

        // Otherwise, copy the list, then clear it, and push our composite data to the users in that list.
        var newOnlinePairs = _newOnlinePairs.ToList();
        _newOnlinePairs.Clear();
        PushCompositeData(newOnlinePairs);
    }

    private void SendUpdateLog(List<UserData> onlinePlayers, DataUpdateType type)
    {
        if (onlinePlayers.Any()) 
            Logger.LogDebug("Pushing Restrictions data to " + string.Join(", ", onlinePlayers.Select(v => v.AliasOrUID)) + "[" + type + "]", LoggerType.OnlinePairs);
        else 
            Logger.LogDebug("Updating Restrictions data to active Restrictions", LoggerType.OnlinePairs);
    }


    /// <summary> Pushes all our Player Data to all online pairs once connected. </summary>
    private void PushCompositeData(List<UserData> newOnlinePairs)
    {
        if (newOnlinePairs.Any())
        {
            _ = Task.Run(async () =>
            {
                var compiledComposite = new CharaCompositeData(); // For now during debugging until the rest works out, send fresh updates.
                /*                {
                                    Gags = _gagManager.ActiveGagsData,
                                    Restrictions = _restrictionManager.ActiveRestrictionsData,
                                    Restraint = _restraintManager.ActiveRestraintData,
                                    CursedItems = _cursedLootConfig.Config.Storage.CursedItems.Select(x => x.Identifier).ToList(),
                                    AliasData = _aliasConfig.Config.FromAliasStorage(),
                                    ToyboxData = new CharaToyboxData()
                                    {
                                        ActivePattern = _patternsConfig.Config.Storage.Patterns.Where(p => p.IsActive).Select(p => p.UniqueIdentifier).FirstOrDefault(),
                                        ActiveAlarms = _alarmConfig.Config.Storage.Alarms.Where(x => x.Enabled).Select(x => x.Identifier).ToList(),
                                        ActiveTriggers = _triggersConfig.Config.Storage.Triggers.Where(x => x.Enabled).Select(x => x.Identifier).ToList(),
                                    },
                                    LightStorageData = new CharaStorageData(), // Handle this later.
                                };*/
                await Task.Delay(1);
                Logger.LogDebug("new Online Pairs Identified, pushing latest Composite data", LoggerType.OnlinePairs);
                // await _hub.UserPushData(new(newOnlinePairs, compiledComposite, false)).ConfigureAwait(false);
            });
        }
    }

    private void PushGagData(List<UserData> onlinePlayers, DataUpdateType type, int layer)
    {
        if (_lastGagData is null)
            return;

        _ = Task.Run(async () =>
        {
            SendUpdateLog(onlinePlayers, type);
            var sentDto = new PushGagDataUpdateDto(onlinePlayers, type)
            {
                Layer = layer,
                Gag = _lastGagData.GagItem,
                Enabler = _lastGagData.Enabler,
                Padlock = _lastGagData.Padlock,
                Password = _lastGagData.Password,
                Timer = _lastGagData.Timer,
                Assigner = _lastGagData.PadlockAssigner
            };
            await _hub.UserPushDataGags(sentDto).ConfigureAwait(false);
        });
    }

    private void PushRestriction(List<UserData> onlinePlayers, DataUpdateType type, int layer)
    {
        if (_lastRestrictionData is null)
            return;

        _ = Task.Run(async () =>
        {
            SendUpdateLog(onlinePlayers, type);
            var sentDto = new PushRestrictionDataUpdateDto(onlinePlayers, type)
            {
                Layer = layer,
                Identifier = _lastRestrictionData.Identifier,
                Enabler = _lastRestrictionData.Enabler,
                Padlock = _lastRestrictionData.Padlock,
                Password = _lastRestrictionData.Password,
                Timer = _lastRestrictionData.Timer,
                Assigner = _lastRestrictionData.PadlockAssigner
            };
            await _hub.UserPushDataRestrictions(sentDto).ConfigureAwait(false);
        });
    }

    private void PushActiveRestraint(List<UserData> onlinePlayers, DataUpdateType type)
    {
        if (_lastRestraintData is null)
            return;

        _ = Task.Run(async () =>
        {
            SendUpdateLog(onlinePlayers, type);
            var sentDto = new PushRestraintDataUpdateDto(onlinePlayers, type)
            {
                ActiveSetId = _lastRestraintData.Identifier,
                LayersBitfield = _lastRestraintData.LayersBitfield,
                Enabler = _lastRestraintData.Enabler,
                Padlock = _lastRestraintData.Padlock,
                Password = _lastRestraintData.Password,
                Timer = _lastRestraintData.Timer,
                Assigner = _lastRestraintData.PadlockAssigner
            };
            await _hub.UserPushDataRestraint(sentDto).ConfigureAwait(false);
        });
    }

    private void PushOrdersData(List<UserData> onlinePlayers, DataUpdateType updateKind)
    {
        if (_lastOrdersData is null)
            return;

        _ = Task.Run(async () =>
        {
            SendUpdateLog(onlinePlayers, updateKind);
            await _hub.UserPushDataOrders(new(onlinePlayers, updateKind)).ConfigureAwait(false);
        });
    }

    /// <summary> Pushes the character alias list to the respective pair we updated it for. </summary>
    private void PushAliasListData(UserData intendedPair, DataUpdateType updateKind)
    {
        if (_lastAliasData is null)
            return;

        _ = Task.Run(async () =>
        {
            Logger.LogDebug("Pushing Character Alias data to " + intendedPair.AliasOrUID + "[" + updateKind + "]", LoggerType.OnlinePairs);
            await _hub.UserPushDataAlias(new(intendedPair, _lastAliasData, updateKind)).ConfigureAwait(false);
        });
    }

    /// <summary> Pushes the character toybox data to the server for the visible players </summary>
    private void PushToyboxData(List<UserData> onlinePlayers, DataUpdateType updateKind)
    {
        if (_lastToyboxData != null)
        {
            _ = Task.Run(async () =>
            {
                SendUpdateLog(onlinePlayers, updateKind);
                await _hub.UserPushDataToybox(new(onlinePlayers, _lastToyboxData, updateKind)).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Toybox data to push to online players");
        }
    }

    /// <summary> Pushes the character toybox data to the server for the visible players </summary>
    private void PushLightStorageData(List<UserData> onlinePlayers)
    {
        if (_lastLightStorage != null)
        {
            _ = Task.Run(async () =>
            {
                SendUpdateLog(onlinePlayers, DataUpdateType.StorageUpdated);

                await _hub.UserPushDataLightStorage(new(onlinePlayers, _lastLightStorage)).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Toybox data to push to online players");
        }
    }
}
