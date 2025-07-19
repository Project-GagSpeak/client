using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;

namespace GagSpeak.Services.Mediator;

///// <summary> Fires upon a request to search for public vibe rooms. </summary>
///// <remarks> Intended to be dealt with by some kind of service that can access MainHub. </remarks>
//public record (RoomParticipant Kinkster) : MessageBase;

///// <summary> Fired whenever a Kinkster leaves a VibeRoom. </summary>
///// <param name="User"> The Kinkster that left the room. </param>
//public record VibeRoomUserLeft(RoomParticipant Kinkster) : MessageBase;

///// <summary> Fired upon receiving an invite to another VibeRoom. </summary>
///// <param name="Invite"> The invite received. </param>
//public record VibeRoomInvite(RoomInvite Invite) : MessageBase;

///// <summary> Whenever another Kinkster in a VibeRoom updates their Connected Device Status. </summary>
///// <param name="User"> The Kinkster that updated their device. </param>
///// <param name="Device"> The device that was updated. </param>
//public record VibeRoomUserUpdatedDevice(UserData Kinkster, ToyInfo Device) : MessageBase;

///// <summary> Contains a chunk of vibrator data sent by another Kinkster in the VibeRoom. </summary>
///// <param name="dto"> The data stream received. </param>
public record VibeRoomSendDataStream(ToyDataStream ToyStreamToSend) : MessageBase;

///// <summary> Notifies you that a Kinkster in the VibeRoom granted you access to use their toys. </summary>
///// <param name="User"> The Kinkster that granted you access. </param>
//public record VibeRoomUserAccessGranted(UserData Kinkster) : MessageBase;

///// <summary> Notifies you that a Kinkster in the VibeRoom revoked your access to use their toys. </summary>
///// <param name="User"> The Kinkster that revoked your access. </param>
//public record VibeRoomUserAccessRevoked(UserData Kinkster) : MessageBase;

