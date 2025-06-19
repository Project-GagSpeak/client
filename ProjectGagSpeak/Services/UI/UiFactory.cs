using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.CkCommons.Gui;
using GagSpeak.CkCommons.Gui.Components;
using GagSpeak.CkCommons.Gui.Permissions;
using GagSpeak.CkCommons.Gui.Profile;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Services;

public class UiFactory
{
    // Generic Classes
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly PiShockProvider _shockies;
    private readonly MoodleIcons _iconDisplayer;
    private readonly ImageImportTool _imageImport;

    // Managers
    private readonly PairManager _pairManager;
    private readonly GlobalPermissions _globals;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly PresetLogicDrawer _presetService;
    private readonly TextureService _textures;

    // API Hubs
    private readonly MainHub _hub;

    public UiFactory(
        ILoggerFactory loggerFactory,
        GagspeakMediator mediator,
        MainConfig config,
        PiShockProvider shockies,
        MoodleIcons iconDisplayer,
        ImageImportTool imageImport,
        // Managers
        PairManager pairManager,
        GlobalPermissions globals,
        // Services
        CosmeticService cosmetics,
        KinkPlateLight kinkPlateLight,
        KinkPlateService kinkPlates,
        PresetLogicDrawer presetService,
        TextureService textures,
        // API Hubs
        MainHub hub)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _config = config;
        _shockies = shockies;
        _iconDisplayer = iconDisplayer;
        _imageImport = imageImport;
        
        _pairManager = pairManager;
        _globals = globals;
        
        _cosmetics = cosmetics;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
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
            _hub, _globals, _presetService, _iconDisplayer, _pairManager, _shockies);
    }

    public ThumbnailUI CreateThumbnailUi(ImageMetadataGS thumbnailInfo)
    {
        return new ThumbnailUI(_loggerFactory.CreateLogger<ThumbnailUI>(), _mediator, _imageImport,
            _config, _cosmetics, thumbnailInfo);
    }
}
