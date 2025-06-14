using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;

namespace GagSpeak.Services;
public class KinkPlateFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly KinksterRequests _globals;
    private readonly CosmeticService _cosmetics;

    public KinkPlateFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        KinksterRequests playerData, CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _globals = playerData;
        _cosmetics = cosmetics;
    }

    public KinkPlate CreateProfileData(KinkPlateContent kinkPlateInfo, string Base64ProfilePicture)
    {
        return new KinkPlate(_loggerFactory.CreateLogger<KinkPlate>(), _mediator,
            _globals, _cosmetics, kinkPlateInfo, Base64ProfilePicture);
    }
}
