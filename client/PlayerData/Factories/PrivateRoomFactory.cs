using GagSpeak.PlayerData.PrivateRooms;
using GagSpeak.Services.Mediator;
using GagspeakAPI.Dto.Toybox;

namespace GagSpeak.PlayerData.Factories;

public class PrivateRoomFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ParticipantFactory _participantFactory;
    private readonly GagspeakMediator _mediator;

    public PrivateRoomFactory(ILoggerFactory loggerFactory, ParticipantFactory participantFactory, GagspeakMediator mediator)
    {
        _loggerFactory = loggerFactory;
        _participantFactory = participantFactory;
        _mediator = mediator;
    }

    public PrivateRoom Create(RoomInfoDto roomInfo)
    {
        return new PrivateRoom(_loggerFactory.CreateLogger<PrivateRoom>(),
            _mediator, _participantFactory, roomInfo);
    }
}
