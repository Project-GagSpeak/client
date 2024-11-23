using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Dto.UserPair;

namespace GagspeakAPI.SignalR;

/// <summary> The interface for the ToyboxHub
/// <para>
/// This interface is the server end of the SignalR calls made by the client.
/// </para>
/// </summary>
public interface IToyboxHub
{
    const string Path = "/toybox";       // Path to the API on the hosted server

    Task<bool> CheckToyboxClientHealth(); // REQUIRED to stay connected on redi's AKA maintain a connection.

    Task<ToyboxConnectionDto> GetToyboxConnectionDto(); // Get the connection details of the client to the serve

    Task<List<UserData>> ToyboxUserGetOnlinePairs(List<string> uids); // get the current online users paired with this client
    Task Client_ToyboxUserSendOffline(UserDto dto);
    Task Client_ToyboxUserSendOnline(UserDto dto);
    /************ CALLBACKS ***************/
    Task Client_ReceiveToyboxServerMessage(MessageSeverity messageSeverity, string message);
    Task Client_UserReceiveRoomInvite(RoomInviteDto dto); // Receives a room invite from another user.
    Task Client_PrivateRoomJoined(RoomInfoDto dto); // whenever you joined a room.
    Task Client_PrivateRoomOtherUserJoined(RoomParticipantDto dto); // when a user joins the room. Sets them to active. (creates if not there yet)
    Task Client_PrivateRoomOtherUserLeft(RoomParticipantDto dto); // when a user goes inactive from the room.
    Task Client_PrivateRoomRemovedUser(RoomParticipantDto dto); // when a user is fully removed from a room. Dispose of them from the list.
    Task Client_PrivateRoomUpdateUser(RoomParticipantDto dto); // Recieved upon a user updating their status.
    Task Client_PrivateRoomMessage(RoomMessageDto dto); // Recieves a message from another user in the room.
    Task Client_PrivateRoomReceiveUserDevice(UserCharaDeviceInfoMessageDto dto); // Receives device info from another connected user.
    Task Client_PrivateRoomDeviceUpdate(UpdateDeviceDto dto); // Updates the clients device with the new update.
    Task Client_PrivateRoomClosed(string roomName); // Informs the client that the room has been closed.

    /************** CALLERS **********/
    /// <summary> Creates a new private room. Use in a try-catch block to detect any failures in room creation.
    /// <para> use _apiController.UserCreateNewRoom(dto).Result; </para>
    /// </summary>
    Task<bool> PrivateRoomCreate(RoomCreateDto dto); // Creates a new room with the given name.
    Task<bool> PrivateRoomInviteUser(RoomInviteDto dto); // Sends an invite to a room to the given user.
    Task PrivateRoomJoin(RoomParticipantDto dto); // Joins the user to the given room.
    Task PrivateRoomSendMessage(RoomMessageDto dto); // Sends a message to the room.
    Task PrivateRoomPushDevice(UserCharaDeviceInfoMessageDto dto); // Requests the device info of the given user.
    Task PrivateRoomAllowVibes(string roomName); // Adds the user to the contextGroup for vibe communication
    Task PrivateRoomDenyVibes(string roomName); // Removes the user from the contextGroup for vibe communication
    Task PrivateRoomUpdateUserDevice(UpdateDeviceDto dto); // Updates the device of the user.
    Task PrivateRoomUpdateAllUserDevices(UpdateDeviceDto dto); // Updates the devices of the group.
    Task PrivateRoomLeave(RoomParticipantDto dto); // merely marks user inactive for room.
    Task PrivateRoomRemove(string roomName); // fully removes the room, can only be called by host from the room.
}
