using GagspeakAPI.Dto.User;
using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Patterns;

/// <summary>
/// Uploads a pattern to the server.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record PatternUploadDto(UserData Publisher, ServerPatternInfo patternInfo, string base64PatternData) : UserDto(Publisher);
