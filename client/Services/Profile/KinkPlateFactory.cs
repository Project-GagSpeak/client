using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GagSpeak.PlayerData.Data;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.UI;
using GagspeakAPI.Data;

namespace GagSpeak.Services;
public class KinkPlateFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly GagspeakMediator _mediator;
    private readonly PlayerCharacterData _playerData;
    private readonly CosmeticService _cosmetics;
    private readonly UiSharedService _uiShared;

    public KinkPlateFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        PlayerCharacterData playerData, CosmeticService cosmetics, UiSharedService uiShared)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _playerData = playerData;
        _cosmetics = cosmetics;
        _uiShared = uiShared;
    }

    public KinkPlate CreateProfileData(KinkPlateContent kinkPlateInfo, string Base64ProfilePicture)
    {
        return new KinkPlate(_loggerFactory.CreateLogger<KinkPlate>(), _mediator,
            _playerData, _cosmetics, kinkPlateInfo, Base64ProfilePicture);
    }
}
