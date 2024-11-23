using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using GagspeakAPI.Dto.User;
using MessagePack;

namespace GagspeakAPI.Dto.IPC;

/// <summary>
/// Sends a tuple set of our client's Moodle Statuses (Individual or preset) to another pair.
/// This pair will then call to their moodles client to apply these statuses.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record ApplyMoodlesByStatusDto(UserData User, List<MoodlesStatusInfo> Statuses, IpcToggleType Type) : UserDto(User);
