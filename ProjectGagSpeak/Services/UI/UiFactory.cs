using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Services;
using GagSpeak.UI;
using GagSpeak.UI.Components.Combos;
using GagSpeak.UI.Handlers;
using GagSpeak.UI.Permissions;
using GagSpeak.UI.Profile;
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
    private readonly ClientMonitor _clientMonitor;
    private readonly GagGarbler _garbler;
    private readonly PairManager _pairManager;
    private readonly GlobalData _clientData;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly IdDisplayHandler _displayHandler;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly PermissionPresetLogic _presetService;
    private readonly TextureService _textures;
    private readonly UiSharedService _uiShared;
    private readonly SexToyManager _vibeService;
    private readonly TutorialService _guides;

    // API Hubs
    private readonly MainHub _hub;

    public UiFactory(ILoggerFactory loggerFactory, GagspeakMediator gagspeakMediator, 
        PiShockProvider shockProvider, GlobalData clientData, ClientMonitor clientMonitor,
        GagGarbler garbler, PairManager pairManager, CosmeticService cosmetics, IdDisplayHandler displayHandler, 
        KinkPlateLight kinkPlateLight, KinkPlateService kinkPlates, OnFrameworkService frameworkUtils, 
        PairCombos pairCombos, PermissionPresetLogic presetService, TextureService textures, UiSharedService uiShared, 
        SexToyManager vibeService, TutorialService guides, MainHub hub)
    {
        _loggerFactory = loggerFactory;
        _gagspeakMediator = gagspeakMediator;
        _shockProvider = shockProvider;
        _pairCombos = pairCombos;

        _clientMonitor = clientMonitor;
        _garbler = garbler;
        _pairManager = pairManager;
        _clientData = clientData;

        _cosmetics = cosmetics;
        _displayHandler = displayHandler;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
        _frameworkUtils = frameworkUtils;
        _presetService = presetService;
        _textures = textures;
        _uiShared = uiShared;
        _vibeService = vibeService;
        _guides = guides;

        _hub = hub;
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
            drawType, _hub, _clientData, _pairCombos, _shockProvider,
            _pairManager, _clientMonitor, _presetService, _uiShared);
    }
}
