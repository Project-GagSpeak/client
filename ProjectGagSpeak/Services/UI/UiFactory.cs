using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagSpeak.Toybox.Services;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.Handlers;
using GagSpeak.CkCommons.Gui.Permissions;
using GagSpeak.CkCommons.Gui.Profile;
using GagSpeak.UpdateMonitoring;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Services;

public class UiFactory
{
    // Generic Classes
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly GagspeakConfigService _config;
    private readonly PiShockProvider _shockies;
    private readonly PairCombos _pairCombos;
    private readonly PermissionsDrawer _permDrawer;
    private readonly PermissionData _permData;
    private readonly ImageImportTool _imageImport;

    // Managers
    private readonly ClientMonitor _clientMonitor;
    private readonly GagGarbler _garbler;
    private readonly PairManager _pairManager;
    private readonly GlobalData _globals;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly IdDisplayHandler _displayHandler;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly OnFrameworkService _frameworkUtils;
    private readonly PresetLogicDrawer _presetService;
    private readonly TextureService _textures;

    private readonly SexToyManager _vibeService;
    private readonly TutorialService _guides;

    // API Hubs
    private readonly MainHub _hub;

    public UiFactory(
        ILoggerFactory loggerFactory,
        GagspeakMediator mediator,
        GagspeakConfigService config,
        PiShockProvider shockies,
        PairCombos pairCombos,
        PermissionsDrawer permDrawer,
        PermissionData permActData,
        ImageImportTool imageImport,
        // Managers
        ClientMonitor clientMonitor,
        GagGarbler garbler,
        PairManager pairManager,
        GlobalData globals,
        // Services
        CosmeticService cosmetics,
        IdDisplayHandler displayHandler,
        KinkPlateLight kinkPlateLight,
        KinkPlateService kinkPlates,
        OnFrameworkService frameworkUtils,
        PresetLogicDrawer presetService,
        TextureService textures,
        // API Hubs
        MainHub hub)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _config = config;
        _shockies = shockies;
        _pairCombos = pairCombos;
        _permDrawer = permDrawer;
        _permData = permActData;
        _imageImport = imageImport;
        
        _clientMonitor = clientMonitor;
        _garbler = garbler;
        _pairManager = pairManager;
        _globals = globals;
        
        _cosmetics = cosmetics;
        _displayHandler = displayHandler;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
        _frameworkUtils = frameworkUtils;
        _presetService = presetService;
        _textures = textures;
        
        _hub = hub;
    }

    public KinkPlateUI CreateStandaloneKinkPlateUi(Pair pair)
    {
        return new KinkPlateUI(_loggerFactory.CreateLogger<KinkPlateUI>(), _mediator,
            _pairManager, _kinkPlates, _cosmetics, _textures, pair);
    }

    public KinkPlateLightUI CreateStandaloneKinkPlateLightUi(UserData pairUserData)
    {
        return new KinkPlateLightUI(_loggerFactory.CreateLogger<KinkPlateLightUI>(), _mediator,
            _kinkPlateLight, _kinkPlates, _pairManager, pairUserData);
    }

    // create a new instance window of the userpair permissions window every time a new pair is selected.
    public PairStickyUI CreateStickyPairPerms(Pair pair, StickyWindowType drawType)
    {
        return new PairStickyUI(_loggerFactory.CreateLogger<PairStickyUI>(), _mediator, pair, drawType,
            _permData, _permDrawer, _pairCombos, _presetService, _hub, _globals, _shockies,
            _pairManager, _clientMonitor);
    }

    public ThumbnailUI CreateThumbnailUi(ImageMetadataGS thumbnailInfo)
    {
        return new ThumbnailUI(_loggerFactory.CreateLogger<ThumbnailUI>(), _mediator, _imageImport,
            _config, _cosmetics, thumbnailInfo);
    }
}
