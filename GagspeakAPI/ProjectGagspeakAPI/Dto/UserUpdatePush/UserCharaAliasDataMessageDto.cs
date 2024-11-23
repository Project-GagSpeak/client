using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Dto.User;

/// <summary>
/// DTO for handling the updating of our own data to our online user pairs.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserCharaAliasDataMessageDto(UserData RecipientUser, CharaAliasData AliasData, DataUpdateKind UpdateKind);
