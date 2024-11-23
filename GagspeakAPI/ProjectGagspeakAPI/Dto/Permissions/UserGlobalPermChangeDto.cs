using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.Permissions;

/// <summary> 
/// DTO responsible for transferring the a user's UID from the users table in the DB.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserGlobalPermChangeDto(UserData User, KeyValuePair<string, object> ChangedPermission, UserData Enactor) : UserDto(User);
