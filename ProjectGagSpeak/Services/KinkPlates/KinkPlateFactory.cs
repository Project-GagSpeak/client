using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagspeakAPI.Data;

namespace GagSpeak.Services;
public class KinkPlateFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;

    public KinkPlateFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
    }

    public KinkPlate CreateProfileData(KinkPlateContent kinkPlateInfo, string Base64ProfilePicture)
    {
        return new KinkPlate(_loggerFactory.CreateLogger<KinkPlate>(), _mediator, kinkPlateInfo, Base64ProfilePicture);
    }
}
