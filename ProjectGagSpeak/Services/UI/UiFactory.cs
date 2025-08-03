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
    private readonly KinksterManager _kinksters;
    private readonly CosmeticService _cosmetics;
    private readonly KinkPlateLight _kinkPlateLight;
    private readonly KinkPlateService _kinkPlates;
    private readonly TextureService _textures;

    public UiFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        MainConfig config, ImageImportTool imageImport, KinksterManager kinksters,
        CosmeticService cosmetics, KinkPlateLight lightPlate, KinkPlateService kinkPlates,
        TextureService textures, TutorialService guides)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _kinksters = kinksters;
        _cosmetics = cosmetics;
        _kinkPlateLight = lightPlate;
        _kinkPlates = kinkPlates;
        _textures = textures;
    }

    public KinkPlateUI CreateStandaloneKinkPlateUi(Kinkster pair)
    {
        return new KinkPlateUI(_loggerFactory.CreateLogger<KinkPlateUI>(), _mediator,
            _kinksters, _kinkPlates, _cosmetics, _textures, pair);
    }

    public KinkPlateLightUI CreateStandaloneKinkPlateLightUi(UserData pairUserData)
    {
        return new KinkPlateLightUI(_loggerFactory.CreateLogger<KinkPlateLightUI>(), _mediator,
            _kinkPlateLight, _kinkPlates, _kinksters, pairUserData);
    }
}
