using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.User;

/// <summary>
/// Holds information about the user who's profile is being reported, and why it was reported.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record UserKinkPlateReportDto(UserData User, string ProfileReport) : UserDto(User);