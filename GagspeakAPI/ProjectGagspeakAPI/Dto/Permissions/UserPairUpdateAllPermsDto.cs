using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Permissions;
using MessagePack;

namespace GagspeakAPI.Dto.Permissions;

/// <summary> 
/// Used whenever a pair selected a preset to update all of their permissions.
/// <para>
/// DirectionIsClientToPair, when true, means that we have updated our permission preset for this pair, 
/// so apply our end of the pair permissions with the new info.
/// </para>
/// <para>
/// DirectionIsClientToPair, when false, means that the other user has updated their permission preset for this pair,
/// so apply their end of the pair permissions with the new info.
/// </para>
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserPairUpdateAllPermsDto(UserData User, UserGlobalPermissions GlobalPermissions, UserPairPermissions PairPermissions,
    UserEditAccessPermissions EditAccessPermissions, bool DirectionIsClientToPair) : UserDto(User);