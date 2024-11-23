using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Data.Permissions;
using MessagePack;

namespace GagspeakAPI.Data;

/// <summary> Contains the record DTO for a userpair of the client.
/// <para>
/// Clients global permissions are not stored on initialization as they are not needed
/// and are stored in the player character manager.
/// </para>
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record PrivateRoomUser
{
    public string UserUID { get; set; } = string.Empty;
    public string ChatAlias { get; set; } = string.Empty;
    public bool ActiveInRoom { get; set; } = false;
    public bool VibeAccess { get; set; } = false;
};
