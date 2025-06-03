using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Services.Mediator;

/// <summary> Fired whenever a Kinkster joins a VibeRoom. </summary>
/// <param name="User"> The Kinkster that joined the room. </param>
public record VibeRoomUserJoined(RoomParticipant User) : MessageBase;

/// <summary> Fired whenever a Kinkster leaves a VibeRoom. </summary>
/// <param name="User"> The Kinkster that left the room. </param>
public record VibeRoomUserLeft(RoomParticipant User) : MessageBase;

/// <summary> Fired upon receiving an invite to another VibeRoom. </summary>
/// <param name="Invite"> The invite received. </param>
public record VibeRoomInvite(RoomInvite Invite) : MessageBase;

/// <summary> Whenever another Kinkster in a VibeRoom updates their Connected Device Status. </summary>
/// <param name="User"> The Kinkster that updated their device. </param>
/// <param name="Device"> The device that was updated. </param>
public record VibeRoomUserUpdatedDevice(UserData User, ToyInfo Device) : MessageBase;

/// <summary> Contains a chunk of vibrator data sent by another Kinkster in the VibeRoom. </summary>
/// <param name="dto"> The data stream received. </param>
public record VibeRoomDataStreamReceived(ToyDataStreamResponse dto) : MessageBase;

/// <summary> Notifies you that a Kinkster in the VibeRoom granted you access to use their toys. </summary>
/// <param name="User"> The Kinkster that granted you access. </param>
public record VibeRoomUserAccessGranted(UserData User) : MessageBase;

/// <summary> Notifies you that a Kinkster in the VibeRoom revoked your access to use their toys. </summary>
/// <param name="User"> The Kinkster that revoked your access. </param>
public record VibeRoomUserAccessRevoked(UserData User) : MessageBase;

/// <summary> Notifies you that a Kinkster in the VibeRoom has sent a message. </summary>
/// <param name="User"> The Kinkster that sent the message. </param>
public record VibeRoomChatMessage(UserData User, string Message) : MessageBase;

