using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.Connection;

/// <summary>
/// The Data Transfer Object for an online user. 
/// </summary>
/// <param name="User">The UserData object containing the UID</param>
/// <param name="Ident">The Ident??? (Not sure what this is)</param>
[MessagePackObject(keyAsPropertyName: true)]
public record OnlineUserIdentDto(UserData User, string Ident) : UserDto(User);
