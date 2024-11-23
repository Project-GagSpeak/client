using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.Permissions;

/// <summary> 
/// 
/// DTO responsible for transferring the a user's UID from the users table in the DB.
/// 
/// <para>
/// 
/// Could potentially transfer the vanity tier along inside the UserData if we need it later. 
/// We'll see.
/// 
/// </para>
/// </summary>
/// <param name="User"> The user who is having their permission modified </param>
/// <param name="changedPermission"> nameof(permission name) permission being changed to (object) new value </param>
[MessagePackObject(keyAsPropertyName: true)]
public record UserPairPermChangeDto(UserData User, KeyValuePair<string, object> ChangedPermission)
    : UserDto(User);