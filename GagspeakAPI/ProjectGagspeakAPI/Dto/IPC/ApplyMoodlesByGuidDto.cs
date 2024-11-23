using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.IPC;

/// <summary>
/// Sends a list of GUID's for the pair to enable in their status manager.
/// This pair will then call to their moodles client to apply these statuses.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record ApplyMoodlesByGuidDto(UserData User, List<Guid> Statuses, IpcToggleType Type) : UserDto(User);
