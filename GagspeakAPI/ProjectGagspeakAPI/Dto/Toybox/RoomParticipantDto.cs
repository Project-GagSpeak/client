using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Connection;

[MessagePackObject(keyAsPropertyName: true)]
public record RoomParticipantDto(PrivateRoomUser User, string RoomName);

