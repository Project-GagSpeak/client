using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.Mediator;
using GagSpeak.Services.Textures;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.Toybox;

namespace GagSpeak.PlayerData.Factories;

public class PrivateRoomFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MainHub _apiHubMain;
    private readonly CosmeticService _cosmetics;
    private readonly ParticipantFactory _participantFactory;
    private readonly GagspeakMediator _mediator;

    public PrivateRoomFactory(ILoggerFactory loggerFactory, GagspeakMediator mediator,
        MainHub mainHub, CosmeticService cosmetics, ParticipantFactory participantFactory)
    {
        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _apiHubMain = mainHub;
        _cosmetics = cosmetics;
        _participantFactory = participantFactory;
    }

    public PrivateRoom Create(RoomInfoDto roomInfo)
    {
        return new PrivateRoom(_loggerFactory.CreateLogger<PrivateRoom>(),
            _mediator, _apiHubMain, _cosmetics, _participantFactory, roomInfo);
    }
}
