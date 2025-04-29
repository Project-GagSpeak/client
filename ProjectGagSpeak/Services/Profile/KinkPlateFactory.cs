using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.CkCommons.Gui;
using GagspeakAPI.Data;

namespace GagSpeak.Services;
public class KinkPlateFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly GlobalData _playerData;
    private readonly CosmeticService _cosmetics;

    public KinkPlateFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        GlobalData playerData, CosmeticService cosmetics)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _playerData = playerData;
        _cosmetics = cosmetics;
    }

    public KinkPlate CreateProfileData(KinkPlateContent kinkPlateInfo, string Base64ProfilePicture)
    {
        return new KinkPlate(_loggerFactory.CreateLogger<KinkPlate>(), _mediator,
            _playerData, _cosmetics, kinkPlateInfo, Base64ProfilePicture);
    }
}
