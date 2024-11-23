using GagspeakAPI.Data;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Toybox;
using Microsoft.AspNetCore.SignalR.Client;

namespace GagSpeak.WebAPI;

#pragma warning disable MA0040
public partial class ToyboxHub
{
    /// <summary>
    /// Take ownership of the PrivateRoomManager by hosting your own room in it. 
    /// Send the new room creation to server.
    /// </summary>
    public async Task<bool> PrivateRoomCreate(RoomCreateDto userDto)
    {
        if (!IsConnected) return false;

        return await GagSpeakHubToybox!.InvokeAsync<bool>(nameof(PrivateRoomCreate), userDto).ConfigureAwait(false);
    }

    public async Task<List<UserData>> ToyboxUserGetOnlinePairs(List<string> uids)
    {
        return await GagSpeakHubToybox!.InvokeAsync<List<UserData>>(nameof(ToyboxUserGetOnlinePairs), uids).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a room invite to another user.
    /// </summary>
    public async Task<bool> PrivateRoomInviteUser(RoomInviteDto dto)
    {
        if (!IsConnected) return false;
        Logger.LogInformation("Sending Invite to " + dto.UserInvited.AliasOrUID, LoggerType.ApiCore);
        return await GagSpeakHubToybox!.InvokeAsync<bool>(nameof(PrivateRoomInviteUser), dto).ConfigureAwait(false);
    }

    /// <summary>
    /// Join a room by name.
    /// </summary>
    public async Task PrivateRoomJoin(RoomParticipantDto roomName)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomJoin), roomName).ConfigureAwait(false);
    }

    /// <summary>
    /// Send a message to the room.
    /// </summary>
    public async Task PrivateRoomSendMessage(RoomMessageDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomSendMessage), dto).ConfigureAwait(false);
    }

    /// <summary>
    /// Push the information about a device to another user in the connected private room.
    public async Task PrivateRoomPushDevice(UserCharaDeviceInfoMessageDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomPushDevice), dto).ConfigureAwait(false);
    }

    public async Task PrivateRoomAllowVibes(string roomName)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomAllowVibes), roomName).ConfigureAwait(false);
    }

    public async Task PrivateRoomDenyVibes(string roomName)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomDenyVibes), roomName).ConfigureAwait(false);
    }

    public async Task PrivateRoomUpdateUserDevice(UpdateDeviceDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomUpdateUserDevice), dto).ConfigureAwait(false);
    }

    public async Task PrivateRoomUpdateAllUserDevices(UpdateDeviceDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomUpdateAllUserDevices), dto).ConfigureAwait(false);
    }

    public async Task PrivateRoomLeave(RoomParticipantDto dto)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomLeave), dto).ConfigureAwait(false);
    }
    public async Task PrivateRoomRemove(string roomName)
    {
        if (!IsConnected) return;
        await GagSpeakHubToybox!.SendAsync(nameof(PrivateRoomRemove), roomName).ConfigureAwait(false);
    }
}

#pragma warning restore MA0040
