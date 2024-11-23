using GagspeakAPI.Data.Character;
using MessagePack;
using global::GagspeakAPI.Data;
using GagspeakAPI.Dto.User;

namespace GagspeakAPI.Dto.Toybox;

/// <summary>
/// Updates a single user's device.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UpdateDeviceDto(string User, string RoomName, DeviceUpdate NewUpdate);