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
using GagSpeak.UI.Components.Combos;
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
    private readonly PairCombos _pairCombos;

    // Managers
    private readonly ClientMonitorService _clientService;
    private readonly GagGarbler _garbler;
    private readonly PairManager _pairManager;
    private readonly ClientData _clientData;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly IdDisplayHandler _displayHandler;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly MoodlesService _moodlesService;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly PermissionPresetLogic _presetService;
    private readonly SetPreviewComponent _previews;
    private readonly TextureService _textures;
    private readonly ToyboxRemoteService _remoteService;
    private readonly UiSharedService _uiShared;
    private readonly VibratorService _vibeService;
    private readonly TutorialService _guides;

    // API Hubs
    private readonly MainHub _apiHubMain;
    private readonly ToyboxHub _apiHubToybox;

    public UiFactory(ILoggerFactory loggerFactory, GagspeakMediator gagspeakMediator, 
        PiShockProvider shockProvider, ClientData clientData, ClientMonitorService clientService,
        GagGarbler garbler, PairManager pairManager, CosmeticService cosmetics, IdDisplayHandler displayHandler, 
        KinkPlateLight kinkPlateLight, KinkPlateService kinkPlates, MoodlesService moodlesService, 
        OnFrameworkService frameworkUtils, PairCombos pairCombos,PermissionPresetLogic presetService, 
        SetPreviewComponent setPreviews, TextureService textures, ToyboxRemoteService remoteService, 
        UiSharedService uiShared, VibratorService vibeService, TutorialService guides, 
        MainHub apiHubMain, ToyboxHub apiHubToybox)
    {
        _loggerFactory = loggerFactory;
        _gagspeakMediator = gagspeakMediator;
        _shockProvider = shockProvider;
        _pairCombos = pairCombos;

        _clientService = clientService;
        _garbler = garbler;
        _pairManager = pairManager;
        _clientData = clientData;

        _cosmetics = cosmetics;
        _displayHandler = displayHandler;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
        _moodlesService = moodlesService;
        _frameworkUtils = frameworkUtils;
        _presetService = presetService;
        _previews = setPreviews;
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
            _apiHubToybox, _clientData, _garbler, _uiShared, _vibeService, _remoteService, 
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
            drawType, _previews, _apiHubMain, _clientData, _pairCombos, _shockProvider,
            _pairManager, _clientService, _moodlesService, _presetService, _uiShared);
    }
}
