using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.Connection;

/// <summary>
/// DTO for handling updated to a pair or self's Wardrobe data
/// </summary>
/// <param name="enactor">The user who initiated the update.</param>
/// <param name="User"> The User that will be affected by this change.</param>
/// <param name="WardrobeData"> The updated wardrobe data.</param>
/// <param name="UpdateKind"> The kind of update this change makes.</param>
[MessagePackObject(keyAsPropertyName: true)]
public record OnlineUserCharaWardrobeDataDto(UserData User, CharaWardrobeData WardrobeData, UserData Enactor, DataUpdateKind UpdateKind) : UserDto(User);
