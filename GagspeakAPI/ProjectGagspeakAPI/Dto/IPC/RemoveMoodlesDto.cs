using GagspeakAPI.Data;
using GagspeakAPI.Data.IPC;
using MessagePack;

namespace GagspeakAPI.Dto.User;

/// <summary>
/// Notifies the RecipientUser to removes the listed Moodles from their status manager.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record RemoveMoodlesDto(UserData User, List<Guid> Statuses) : UserDto(User);
