using GagspeakAPI.Data;
using MessagePack;

namespace GagspeakAPI.Dto.Toybox;

/// <summary>
/// Sends a chat message to the other users in the room.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record GlobalChatMessageDto(UserData MessageSender, string Message);
