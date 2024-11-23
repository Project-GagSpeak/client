using GagSpeak.Services.Mediator;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Toybox;

namespace GagSpeak.PlayerData.PrivateRooms;

/// <summary>
/// A class representing the participant in a private room.
/// </summary>
public class Participant
{
    private readonly ILogger<Participant> _logger;
    private readonly GagspeakMediator _mediator;
    public Participant(ILogger<Participant> logger, PrivateRoomUser user,
        GagspeakMediator mediator)
    {

        _logger = logger;
        _mediator = mediator;
        User = user;
    }

    public PrivateRoomUser User { get; set; }
    public List<UserCharaDeviceInfoMessageDto> UserDevices { get; set; }


    // TODO: Rework method handles later.
    public void ApplyDeviceData(UserCharaDeviceInfoMessageDto dto)
    {
        if (UserDevices == null)
        {
            UserDevices = new();
        }
        UserDevices.Add(dto);
    }

    public void RemoveDevice(UserCharaDeviceInfoMessageDto dto)
    {
        if (UserDevices == null)
        {
            return;
        }
        UserDevices.Remove(dto);
    }

    public void ClearDevices()
    {
        UserDevices?.Clear();
    }


    public void MarkOffline()
    {
        _logger.LogInformation("Marking participant " + User.ChatAlias + " as offline", LoggerType.PrivateRooms);
        ClearDevices();
    }
}
