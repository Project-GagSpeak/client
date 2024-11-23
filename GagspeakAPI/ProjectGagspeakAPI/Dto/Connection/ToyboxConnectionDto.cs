using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using MessagePack;

namespace GagspeakAPI.Dto.Connection;

/// <summary>
/// Much more simpler connection dto for the toybox server.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record ToyboxConnectionDto(UserData User)
{
    // version of the gagspeak API
    public int ServerVersion { get; set; }

    // the room the user is hosting @ time of join, if any
    public RoomInfoDto HostedRoom { get; set; } = new();

    // the room the user is a participant of @ time of join, if any
    public List<RoomInfoDto> ConnectedRooms { get; set; } = new();

}
