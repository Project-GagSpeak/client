using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Toybox;

/// <summary>
/// Updates devices of all users in a group that is not the caller.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record RoomInfoDto
{
    public string NewRoomName { get; set; } = string.Empty;
    public PrivateRoomUser RoomHost { get; set; } = new();
    public List<PrivateRoomUser> ConnectedUsers { get; set; } = [];
}
