using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.User;

/// <summary> 
/// 
/// DTO responsible for transferring the a user's UID from the users table in the DB.
/// 
/// <para>
/// 
/// Could potentially transfer the vanity tier along inside the UserData if we need it later. We'll see.
/// 
/// </para>
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserDto(UserData User);