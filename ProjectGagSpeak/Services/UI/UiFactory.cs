using GagSpeak.Gui;
using GagSpeak.Gui.Components;
using GagSpeak.Gui.Profile;
using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.Services.Tutorial;
using GagspeakAPI.Data;

namespace GagSpeak.Services;

public class UiFactory
{
    // Generic Classes
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly MainConfig _config;
    private readonly ImageImportTool _imageImport;

    // Managers
    private readonly KinksterManager _pairManager;

    // Services
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly TextureService _textures;

    public UiFactory(
        ILoggerFactory loggerFactory,
        GagspeakMediator mediator,
        MainConfig config,
        ImageImportTool imageImport,
        // Managers
        KinksterManager pairManager,
        // Services
        CosmeticService cosmetics,
        KinkPlateLight kinkPlateLight,
        KinkPlateService kinkPlates,
        TextureService textures)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _config = config;
        _imageImport = imageImport;
        
        _pairManager = pairManager;
        
        _cosmetics = cosmetics;
        _kinkPlateLight = kinkPlateLight;
        _kinkPlates = kinkPlates;
        _textures = textures;
    }

    public KinkPlateUI CreateStandaloneKinkPlateUi(Kinkster pair)
    {
        return new KinkPlateUI(_loggerFactory.CreateLogger<KinkPlateUI>(), _mediator,
            _pairManager, _kinkPlates, _cosmetics, _textures, pair);
    }

    public KinkPlateLightUI CreateStandaloneKinkPlateLightUi(UserData pairUserData)
    {
        return new KinkPlateLightUI(_loggerFactory.CreateLogger<KinkPlateLightUI>(), _mediator,
            _kinkPlateLight, _kinkPlates, _pairManager, pairUserData);
    }

    // we only ever want one of these open at once.
    // Change it to a scoped service instead of a factory generated item?
    public ThumbnailUI CreateThumbnailUi(ImageMetadataGS thumbnailInfo)
    {
        return new ThumbnailUI(_loggerFactory.CreateLogger<ThumbnailUI>(), _mediator, _imageImport,
            _config, _cosmetics, thumbnailInfo);
    }
}
