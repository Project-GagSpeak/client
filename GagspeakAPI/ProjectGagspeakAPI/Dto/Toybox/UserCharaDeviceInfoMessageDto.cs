using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Toybox;

[MessagePackObject(keyAsPropertyName: true)]
public record UserCharaDeviceInfoMessageDto(
    PrivateRoomUser User, 
    string RoomName, 
    string Devicename,
    string DeviceDisplayName, 
    List<int> MotorsAndTypes
    );
