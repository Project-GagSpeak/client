using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Services;
using GagSpeak.UI;
using GagSpeak.UI.Components;
using GagSpeak.UI.Handlers;
using GagSpeak.UI.Permissions;
using GagSpeak.UI.Profile;
using GagSpeak.UI.UiRemote;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Services;

public class UiFactory
{
    // Generic Classes
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _gagspeakMediator;
    private readonly PiShockProvider _shockProvider;

    // Managers
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly ClientMonitorService _clientService;
    private readonly GagManager _gagManager;
    private readonly PairManager _pairManager;
    private readonly PlayerCharacterData _playerManager;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly IdDisplayHandler _displayHandler;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly MoodlesService _moodlesService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly PermissionPresetService _presetService;
    private readonly PermActionsComponents _permActionHelpers;
    private readonly TextureService _textures;
    private readonly ToyboxRemoteService _remoteService;
    private readonly UiSharedService _uiShared;
    private readonly VibratorService _vibeService;
    private readonly TutorialService _guides;

    // API Hubs
    private readonly MainHub _apiHubMain;
    private readonly ToyboxHub _apiHubToybox;

    public UiFactory(ILoggerFactory loggerFactory, GagspeakMediator gagspeakMediator, 
        PiShockProvider shockProvider, ClientConfigurationManager clientConfigs, 
        ClientMonitorService clientService, GagManager gagManager, PairManager pairManager, 
        PlayerCharacterData playerManager, CosmeticService cosmetics, IdDisplayHandler displayHandler, 
        KinkPlateLight kinkPlateLight, KinkPlateService kinkPlates, MoodlesService moodlesService, 
        OnFrameworkService frameworkUtils, PermissionPresetService presetService, 
        PermActionsComponents permActionHelpers, TextureService textures, ToyboxRemoteService remoteService, 
        UiSharedService uiShared, VibratorService vibeService, TutorialService guides, MainHub apiHubMain,
        ToyboxHub apiHubToybox)
    {
        _loggerFactory = loggerFactory;
        _gagspeakMediator = gagspeakMediator;
        _shockProvider = shockProvider;
        _clientConfigs = clientConfigs;
        _clientService = clientService;
        _gagManager = gagManager;
        _pairManager = pairManager;
        _playerManager = playerManager;
        _cosmetics = cosmetics;
        _displayHandler = displayHandler;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
        _moodlesService = moodlesService;
        _frameworkUtils = frameworkUtils;
        _presetService = presetService;
        _permActionHelpers = permActionHelpers;
        _textures = textures;
        _remoteService = remoteService;
        _uiShared = uiShared;
        _vibeService = vibeService;
        _guides = guides;
        _apiHubMain = apiHubMain;
        _apiHubToybox = apiHubToybox;
    }

    public RemoteController CreateControllerRemote(PrivateRoom privateRoom)
    {
        return new RemoteController(_loggerFactory.CreateLogger<RemoteController>(), _gagspeakMediator,
            _apiHubToybox, _playerManager, _gagManager, _uiShared, _vibeService, _remoteService, 
            _guides, privateRoom);
    }

    public KinkPlateUI CreateStandaloneKinkPlateUi(Pair pair)
    {
        return new KinkPlateUI(_loggerFactory.CreateLogger<KinkPlateUI>(), _gagspeakMediator,
            _pairManager, _kinkPlates, _cosmetics, _textures, _uiShared, pair);
    }

    public KinkPlateLightUI CreateStandaloneKinkPlateLightUi(UserData pairUserData)
    {
        return new KinkPlateLightUI(_loggerFactory.CreateLogger<KinkPlateLightUI>(), _gagspeakMediator,
            _kinkPlateLight, _kinkPlates, _pairManager, _uiShared, pairUserData);
    }

    // create a new instance window of the userpair permissions window every time a new pair is selected.
    public PairStickyUI CreateStickyPairPerms(Pair pair, StickyWindowType drawType)
    {
        return new PairStickyUI(_loggerFactory.CreateLogger<PairStickyUI>(), _gagspeakMediator, pair,
            drawType, _displayHandler, _apiHubMain, _playerManager, _permActionHelpers, _shockProvider,
            _pairManager, _clientConfigs, _clientService, _moodlesService, _presetService, _uiShared);
    }
}
