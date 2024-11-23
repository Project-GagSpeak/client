using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Toybox;

/// <summary>
///  Use userdata over the new private room user because they are not yet set as one.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record RoomInviteDto(UserData UserInvited, string RoomName);
