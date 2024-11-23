using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Permissions;
using MessagePack;

namespace GagspeakAPI.Dto.Permissions;

/// <summary> 
/// Updated in bulk the permissions the client caller has for another user.
/// Only used by Safeword and preset selections.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserPairUpdateAllUniqueDto(UserData User, UserPairPermissions UniquePerms, UserEditAccessPermissions UniqueAccessPerms) : UserDto(User);
