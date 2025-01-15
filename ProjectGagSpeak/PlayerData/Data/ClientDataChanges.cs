using Dalamud.Utility;
using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Dto.Permissions;
using System.Reflection;
using GagspeakAPI.Extensions;
using FFXIVClientStructs.FFXIV.Client.UI;
using GagSpeak.Services.Events;
using GagSpeak.StateManagers;

namespace GagSpeak.PlayerData.Data;

/// <summary>
/// Handles the player character data.
/// <para>
/// Applies callback updates to clientConfig data
/// Compiles client config data into API format for server transfer.
/// </para>
/// </summary>
public class ClientDataChanges : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PairManager _pairManager;
    private readonly ClientData _data;
    private readonly AppearanceManager _appearance;

    public ClientDataChanges(ILogger<ClientDataChanges> logger, GagspeakMediator mediator, 
        ClientConfigurationManager clientConfigs, PairManager pairManager,
        ClientData data, AppearanceManager appearance) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;
        _data = data;
        _appearance = appearance;

        Mediator.Subscribe<PlayerCharAppearanceChanged>(this, (msg) => PushAppearanceDataToAPI(msg));
        Mediator.Subscribe<PlayerCharWardrobeChanged>(this, (msg) => PushWardrobeDataToAPI(msg));
        Mediator.Subscribe<PlayerCharOrdersChanged>(this, (msg) => PushOrdersDataToAPI(msg));
        Mediator.Subscribe<PlayerCharAliasChanged>(this, (msg) => PushAliasListDataToAPI(msg));
        Mediator.Subscribe<PlayerCharToyboxChanged>(this, (msg) => PushToyboxDataToAPI(msg));
        Mediator.Subscribe<PlayerCharStorageUpdated>(this, _ => PushLightStorageToAPI());

        Mediator.Subscribe<DelayedFrameworkUpdateMessage>(this, _ => CheckForUnlocks());
    }

    // helper method to decompile a received composite data message
    public CharaCompositeData CompileCompositeDataToSend()
    {
        // make use of the various compiling methods to construct our composite data.
        CharaAppearanceData appearanceData = _data.AppearanceData ?? new CharaAppearanceData();
        CharaWardrobeData wardrobeData = _clientConfigs.CompileWardrobeToAPI();
        CharaOrdersData ordersData = _clientConfigs.CompileOrdersDataToAPI();

        Dictionary<string, CharaAliasData> aliasData = _clientConfigs.GetCompiledAliasData();
        CharaToyboxData toyboxData = _clientConfigs.CompileToyboxToAPI();

        CharaStorageData lightStorageData = _clientConfigs.CompileLightStorageToAPI();

        return new CharaCompositeData
        {
            AppearanceData = appearanceData,
            WardrobeData = wardrobeData,
            OrdersData = ordersData,
            AliasData = aliasData,
            ToyboxData = toyboxData,
            LightStorageData = lightStorageData
        };
    }

    public void PushAppearanceDataToAPI(PlayerCharAppearanceChanged msg)
    {
        var dataToPush = _data.AppearanceData ?? new CharaAppearanceData();
        Mediator.Publish(new AppearanceDataCreatedMessage(dataToPush, msg.AffectedLayer, msg.UpdateType, msg.PreviousLock));
    }

    public void PushWardrobeDataToAPI(PlayerCharWardrobeChanged msg)
    {
        var dataToPush = _clientConfigs.CompileWardrobeToAPI();
        Mediator.Publish(new WardrobeDataCreatedMessage(dataToPush, msg.UpdateKind, msg.AffectedItem));
    }

    public void PushOrdersDataToAPI(PlayerCharOrdersChanged msg)
    {
        var dataToPush = _clientConfigs.CompileOrdersDataToAPI();
        Mediator.Publish(new OrdersDataCreatedMessage(dataToPush, msg.UpdateKind, msg.AffectedId));
    }

    public void PushAliasListDataToAPI(PlayerCharAliasChanged msg)
    {
        UserData? userPair = _pairManager.GetUserDataFromUID(msg.UpdatedPairUID);
        if (userPair == null)
        {
            Logger.LogError("User pair not found for Alias update.");
            return;
        }

        var AliasStorage = _clientConfigs.FetchAliasStorageForPair(userPair.UID);
        CharaAliasData dataToPush = new CharaAliasData
        {
            // don't include names here to secure privacy.
            AliasList = AliasStorage.AliasList
        };

        Mediator.Publish(new AliasDataCreatedMessage(dataToPush, userPair, PuppeteerUpdateType.AliasListUpdated));
    }

    public void PushToyboxDataToAPI(PlayerCharToyboxChanged msg)
    {
        var dataToPush = _clientConfigs.CompileToyboxToAPI();
        Mediator.Publish(new ToyboxDataCreatedMessage(dataToPush, msg.UpdateKind));
    }

    public void PushLightStorageToAPI()
    {
        var dataToPush = _clientConfigs.CompileLightStorageToAPI();
        Mediator.Publish(new LightStorageDataCreatedMessage(dataToPush));
    }

    private void CheckForUnlocks()
    {
        // check if any of then gags need to be unlocked.
        if (_data.AppearanceData is null || _data.AnyGagLocked is false)
            return;

        // If a gag does have a padlock, ensure it is a timer padlock
        for (int i = 0; i < _data.AppearanceData.GagSlots.Length; i++)
        {
            var gagSlot = _data.AppearanceData.GagSlots[i];
            if (gagSlot.Padlock.ToPadlock().IsTimerLock() && gagSlot.Timer - DateTimeOffset.UtcNow <= TimeSpan.Zero)
            {
                Logger.LogTrace("Sending off Lock Removed Event to server!", LoggerType.PadlockHandling);
                {
                    if(gagSlot.Padlock.ToPadlock() is Padlocks.TimerPadlock)
                        _appearance.GagUnlocked((GagLayer)i, gagSlot.Password, "Client", true, false);
                    else
                        _appearance.GagUnlocked((GagLayer)i, gagSlot.Password, gagSlot.Assigner, true, false);

                }
            }
        }
    }
}
