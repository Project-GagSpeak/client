using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.User;

/// <summary>
/// The Record DTO for the User's KinkPlate.
/// </summary>
/// <param name="User">The UserData (UID) that this profile belongs to.</param>
/// <param name="ProfileInfo">The Additional Information that structures the KinkPlate</param>
/// <param name="ProfilePictureBase64">The profile picture embedded in the profile</param>
[MessagePackObject(keyAsPropertyName: true)]
public record UserKinkPlateDto(UserData User, KinkPlateContent Info, string? ProfilePictureBase64) : UserDto(User);
