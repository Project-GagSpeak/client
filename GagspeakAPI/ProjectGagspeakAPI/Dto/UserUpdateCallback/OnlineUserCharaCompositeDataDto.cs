using GagspeakAPI.Data;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Character;
using MessagePack;
using GagspeakAPI.Enums;

namespace GagspeakAPI.Dto.Connection;

/// <summary>
/// Ultimately this should be removed, or if kept, only used for initial sign-in's and sign-offs.
/// The most resource heavy function we have.
/// <para><b>User == The user Updated (can be client caller or other pair)</b></para>
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record OnlineUserCompositeDataDto(UserData User, CharaCompositeData CompositeData, DataUpdateKind UpdateKind) : UserDto(User);
