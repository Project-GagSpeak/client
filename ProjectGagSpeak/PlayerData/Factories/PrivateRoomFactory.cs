using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.Toybox;

namespace GagSpeak.PlayerData.Factories;

public class PrivateRoomFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly MainHub _apiHubMain;
    private readonly ParticipantFactory _participantFactory;
    private readonly GagspeakMediator _mediator;

    public PrivateRoomFactory(ILoggerFactory loggerFactory, MainHub mainHub,
        ParticipantFactory participantFactory, GagspeakMediator mediator)
    {
        _loggerFactory = loggerFactory;
        _apiHubMain = mainHub;
        _participantFactory = participantFactory;
        _mediator = mediator;
    }

    public PrivateRoom Create(RoomInfoDto roomInfo)
    {
        return new PrivateRoom(_loggerFactory.CreateLogger<PrivateRoom>(),
            _mediator, _apiHubMain, _participantFactory, roomInfo);
    }
}
