using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Permissions;
using MessagePack;

namespace GagspeakAPI.Dto.Permissions;

/// <summary> 
/// ONLY CALLED BY SELF
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserAllGlobalPermChangeDto(UserData User, UserGlobalPermissions GlobalPermissions) : UserDto(User);
