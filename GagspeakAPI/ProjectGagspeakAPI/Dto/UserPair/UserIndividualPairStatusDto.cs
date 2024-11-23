using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.UserPair;

/// <summary>
/// 
/// Used primarily to transfer a user's pair status to another connected user.
/// 
/// </summary>
/// <param name="User"> the user the pair status belongs to </param>
/// <param name="IndividualPairStatus"> their pair status with you </param>
[MessagePackObject(keyAsPropertyName: true)]
public record UserIndividualPairStatusDto(UserData User, IndividualPairStatus IndividualPairStatus) : UserDto(User);