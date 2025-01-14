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

    public ClientDataChanges(ILogger<ClientDataChanges> logger, GagspeakMediator mediator, 
        ClientConfigurationManager clientConfigs, PairManager pairManager,
        ClientData data) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _pairManager = pairManager;
        _data = data;

        Mediator.Subscribe<PlayerCharAppearanceChanged>(this, (msg) => PushAppearanceDataToAPI(msg));
        Mediator.Subscribe<PlayerCharWardrobeChanged>(this, (msg) => PushWardrobeDataToAPI(msg));
        Mediator.Subscribe<PlayerCharAliasChanged>(this, (msg) => PushAliasListDataToAPI(msg));
        Mediator.Subscribe<PlayerCharToyboxChanged>(this, (msg) => PushToyboxDataToAPI(msg));
        Mediator.Subscribe<PlayerCharStorageUpdated>(this, _ => PushLightStorageToAPI());

        Mediator.Subscribe<IpcDataCreatedMessage>(this, (msg) => _data.LastIpcData = msg.CharaIPCData);

        Mediator.Subscribe<DalamudLogoutMessage>(this, (_) =>
        {
            _data.GlobalPerms = null;
            _data.AppearanceData = null;
            _data.LastIpcData = null;
            _data.CustomizeProfiles = new();
        });
    }

    // helper method to decompile a received composite data message
    public CharaCompositeData CompileCompositeDataToSend()
    {
        // make use of the various compiling methods to construct our composite data.
        CharaAppearanceData appearanceData = _data.CompileAppearanceToAPI();
        CharaWardrobeData wardrobeData = _clientConfigs.CompileWardrobeToAPI();

        Dictionary<string, CharaAliasData> aliasData = _clientConfigs.GetCompiledAliasData();
        CharaToyboxData toyboxData = _clientConfigs.CompileToyboxToAPI();

        CharaStorageData lightStorageData = _clientConfigs.CompileLightStorageToAPI();

        return new CharaCompositeData
        {
            AppearanceData = appearanceData,
            WardrobeData = wardrobeData,
            AliasData = aliasData,
            ToyboxData = toyboxData,
            LightStorageData = lightStorageData
        };
    }

    public CharaAliasData CompileAliasToAPI(string UserUID)
    {
        var AliasStorage = _clientConfigs.FetchAliasStorageForPair(UserUID);
        CharaAliasData dataToPush = new CharaAliasData
        {
            // don't include names here to secure privacy.
            AliasList = AliasStorage.AliasList
        };

        return dataToPush;
    }

    public CharaToyboxData CompileToyboxToAPI()
    {
        return _clientConfigs.CompileToyboxToAPI();
    }

    public void PushAppearanceDataToAPI(PlayerCharAppearanceChanged msg)
    {
        var dataToPush = _data.CompileAppearanceToAPI();
        Mediator.Publish(new AppearanceDataCreatedMessage(dataToPush, msg.AffectedLayer, msg.UpdateType, msg.PreviousLock));
    }

    public void PushWardrobeDataToAPI(PlayerCharWardrobeChanged msg)
    {
        var dataToPush = _clientConfigs.CompileWardrobeToAPI();
        Mediator.Publish(new WardrobeDataCreatedMessage(dataToPush, msg.UpdateKind, msg.AffectedItem));
    }

    public void PushAliasListDataToAPI(PlayerCharAliasChanged msg)
    {
        UserData? userPair = _pairManager.GetUserDataFromUID(msg.UpdatedPairUID);
        if (userPair == null)
        {
            Logger.LogError("User pair not found for Alias update.");
            return;
        }

        var dataToPush = CompileAliasToAPI(userPair.UID);
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
}
