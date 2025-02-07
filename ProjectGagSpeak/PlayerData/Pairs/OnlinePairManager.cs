using GagSpeak.PlayerData.Handlers;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagspeakAPI.Enums;
using GagspeakAPI.Data.Permissions;

namespace GagSpeak.PlayerData.Pairs;

/// <summary>
/// Manages various Data Component Sending to Online Pairs.
/// </summary>
public class OnlinePairManager : DisposableMediatorSubscriberBase
{
    private readonly MainHub _apiHubMain;
    private readonly OnFrameworkService _frameworkUtil;
    private readonly ClientDataChanges _playerManager;
    private readonly PairManager _pairManager;

    // NewOnlinePairs
    private readonly HashSet<UserData> _newOnlinePairs = [];

    // Store the most recently sent component of our API formats from our player character
    private CharaAppearanceData? _lastAppearanceData;
    private CharaWardrobeData? _lastWardrobeData;
    private CharaOrdersData? _lastOrdersData;
    private CharaAliasData? _lastAliasData;
    private CharaToyboxData? _lastToyboxData;
    private CharaStorageData? _lastLightStorage;
    private string _lastShockPermShareCode = string.Empty;

    public OnlinePairManager(ILogger<OnlinePairManager> logger,
        MainHub apiHubMain, OnFrameworkService dalamudUtil,
        ClientDataChanges playerCharacterManager,
        PairManager pairManager, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        _apiHubMain = apiHubMain;
        _frameworkUtil = dalamudUtil;
        _playerManager = playerCharacterManager;
        _pairManager = pairManager;


        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, (_) => FrameworkOnUpdate());

        // Subscriber to update our composite data after a safeword.
        Mediator.Subscribe<UpdateAllOnlineWithCompositeMessage>(this, (_) => PushCharacterCompositeData(_pairManager.GetOnlineUserDatas()));

        // Push Composite data to all online players when connected.
        Mediator.Subscribe<MainHubConnectedMessage>(this, (_) => PushCharacterCompositeData(_pairManager.GetOnlineUserDatas()));
        // Push Composite data to any new pairs that go online.
        Mediator.Subscribe<PairWentOnlineMessage>(this, (msg) => _newOnlinePairs.Add(msg.UserData));

        // Fired whenever our Appearance data updates. We then send this data to all online pairs.
        Mediator.Subscribe<AppearanceDataCreatedMessage>(this, (msg) =>
        {

            var newAppearanceData = msg.NewData;
            if (_lastAppearanceData == null || !Equals(newAppearanceData, _lastAppearanceData))
            {
                _lastAppearanceData = newAppearanceData.DeepCloneData();
                PushCharacterAppearanceData(_pairManager.GetOnlineUserDatas(), msg.AffectedLayer, msg.UpdateType, msg.PreviousLock);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
                Logger.LogDebug("Last Appearance Data:\n " + _lastAppearanceData.ToGagString(), LoggerType.OnlinePairs);
                Logger.LogDebug("New Appearance Data:\n " + newAppearanceData.ToGagString(), LoggerType.OnlinePairs);
            }
        });

        // Fired whenever our Wardrobe data updates. We then send this data to all online pairs.
        Mediator.Subscribe<WardrobeDataCreatedMessage>(this, (msg) =>
        {
            var newWardrobeData = msg.CharaWardrobeData;
            if (_lastWardrobeData == null || !Equals(newWardrobeData, _lastWardrobeData))
            {
                _lastWardrobeData = newWardrobeData;
                PushCharacterWardrobeData(_pairManager.GetOnlineUserDatas(), msg.UpdateKind, msg.AffectedItem);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        Mediator.Subscribe<OrdersDataCreatedMessage>(this, (msg) =>
        {
            var newOrdersData = msg.CharaTimedData;
            if (_lastOrdersData == null || !Equals(newOrdersData, _lastOrdersData))
            {
                _lastOrdersData = newOrdersData;
                PushCharacterOrdersData(_pairManager.GetOnlineUserDatas(), msg.UpdateKind, msg.AffectedItem);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        // Fired whenever our Alias data updates. We then send this data to all online pairs.
        Mediator.Subscribe<AliasDataCreatedMessage>(this, (msg) =>
        {
            var newAliasData = msg.CharaAliasData;
            if (_lastAliasData == null || !Equals(newAliasData, _lastAliasData))
            {
                _lastAliasData = newAliasData;
                PushCharacterAliasListData(msg.userData, msg.UpdateKind);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        // Fired whenever our Toybox data updates. We then send this data to all online pairs.
        Mediator.Subscribe<ToyboxDataCreatedMessage>(this, (msg) =>
        {
            var newToyboxData = msg.CharaToyboxData;
            if (_lastToyboxData == null || !Equals(newToyboxData, _lastToyboxData))
            {
                _lastToyboxData = newToyboxData;
                PushCharacterToyboxData(_pairManager.GetOnlineUserDatas(), msg.UpdateKind);
            }
            else
            {
                Logger.LogDebug("Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });

        // Fired whenever our Alias data updates. We then send this data to all online pairs.
        Mediator.Subscribe<LightStorageDataCreatedMessage>(this, (msg) =>
        {
            var newLightStorageData = msg.CharacterStorageData;
            if (_lastLightStorage == null || !Equals(newLightStorageData, _lastLightStorage))
            {
                _lastLightStorage = newLightStorageData;
                PushCharacterLightStorageData(_pairManager.GetOnlineUserDatas());
            }
            else
            {
                Logger.LogDebug("Light-Storage Data was no different. Not sending data", LoggerType.OnlinePairs);
            }
        });
    }

    private void FrameworkOnUpdate()
    {
        // quit out if not connected or the new online pairs list is empty.
        if (!MainHub.IsConnected || !_newOnlinePairs.Any()) return;

        // Otherwise, copy the list, then clear it, and push our composite data to the users in that list.
        var newOnlinePairs = _newOnlinePairs.ToList();
        _newOnlinePairs.Clear();
        PushCharacterCompositeData(newOnlinePairs);
    }


    /// <summary> Pushes all our Player Data to all online pairs once connected. </summary>
    private void PushCharacterCompositeData(List<UserData> newOnlinePairs)
    {
        if (newOnlinePairs.Any())
        {
            // Send the data to all online players.
            _ = Task.Run(async () =>
            {
                CharaCompositeData compiledDataToSend = _playerManager.CompileCompositeDataToSend();
                Logger.LogDebug("new Online Pairs Identified, pushing latest Composite data", LoggerType.OnlinePairs);
                await _apiHubMain.PushCharacterCompositeData(compiledDataToSend, newOnlinePairs).ConfigureAwait(false);
            });
        }
    }


    /// <summary> Pushes the character wardrobe data to the server for the visible players </summary>
    private void PushCharacterAppearanceData(List<UserData> onlinePlayers, GagLayer affectedLayer, GagUpdateType updateType, Padlocks previousLock)
    {
        if (_lastAppearanceData != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterAppearanceData(_lastAppearanceData, onlinePlayers, affectedLayer, updateType, previousLock).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Appearance data to push to online players");
        }
    }

    /// <summary> Pushes the character wardrobe data to the server for the visible players </summary>
    private void PushCharacterWardrobeData(List<UserData> onlinePlayers, WardrobeUpdateType updateKind, string affectedItem)
    {
        if (_lastWardrobeData != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterWardrobeData(_lastWardrobeData, onlinePlayers, updateKind, affectedItem).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Wardrobe data to push to online players");
        }
    }

    private void PushCharacterOrdersData(List<UserData> onlinePlayers, OrdersUpdateType updateKind, string affectedId)
    {
        if (_lastOrdersData != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterOrdersData(_lastOrdersData, onlinePlayers, updateKind, affectedId).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Timed Items data to push to online players");
        }
    }

    /// <summary> Pushes the character alias list to the respective pair we updated it for. </summary>
    private void PushCharacterAliasListData(UserData onlinePairToPushTo, PuppeteerUpdateType updateKind)
    {
        if (_lastAliasData != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterAliasListData(_lastAliasData, onlinePairToPushTo, PuppeteerUpdateType.AliasListUpdated).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Alias data to push to online players");
        }
    }

    /// <summary> Pushes the character toybox data to the server for the visible players </summary>
    private void PushCharacterToyboxData(List<UserData> onlinePlayers, ToyboxUpdateType updateKind)
    {
        if (_lastToyboxData != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterToyboxData(_lastToyboxData, onlinePlayers, updateKind).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Toybox data to push to online players");
        }
    }

    /// <summary> Pushes the character toybox data to the server for the visible players </summary>
    private void PushCharacterLightStorageData(List<UserData> onlinePlayers)
    {
        if (_lastLightStorage != null)
        {
            _ = Task.Run(async () =>
            {
                await _apiHubMain.PushCharacterLightStorageData(_lastLightStorage, onlinePlayers).ConfigureAwait(false);
            });
        }
        else
        {
            Logger.LogWarning("No Toybox data to push to online players");
        }
    }
}
