using GagSpeak.Gui.Remote;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.User;

namespace GagSpeak.Services;

/// <summary> Creates various calls to the server for the VibeLobby. </summary>
/// <remarks> Intended to be created in, and called by a UI element to avoid circular dependancy. </remarks>
public sealed class VibeLobbyDistributor : DisposableMediatorSubscriberBase
{
    private readonly MainHub _hub;
    private readonly MainConfig _config;
    private readonly BuzzToyManager _clientToys;
    private readonly VibeLobbyManager _lobbies;
    private readonly RemoteService _remotes;

    public VibeLobbyDistributor(
        ILogger<VibeLobbyDistributor> logger,
        GagspeakMediator mediator,
        MainHub hub, 
        MainConfig config,
        BuzzToyManager clientToys,
        VibeLobbyManager lobbies,
        RemoteService remotes)
        : base(logger, mediator)
    {
        _hub = hub;
        _config = config;
        _clientToys = clientToys;
        _lobbies = lobbies;
        _remotes = remotes;

        Mediator.Subscribe<VibeRoomSendDataStream>(this, (msg) => RoomSendDataStream(msg.ToyStreamToSend).ConfigureAwait(false));
    }
    
    public async Task SearchForRooms(SearchBase dto)
    {
        // If not connection data synced refuse to perform the room search.
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Search For Rooms, you are not connected to server or data synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Searching for Rooms with criteria: {dto}", LogFilter.VibeLobbies);
        var result = await _hub.SearchForRooms(dto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            Logger.LogDebug("Successfully fetched Room Listings from server", LogFilter.VibeLobbies);
            _lobbies.SetPublicVibeRooms(result.Value ?? new List<RoomListing>());
            return;
        }

        Logger.LogError($"Failed to fetch Room Listings from server. [{result.ErrorCode}]");
    }

    public async Task<bool> TryCreateRoom(string name, string desc, string pass, List<string> tags)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Create Room, connection data not synced.", LogFilter.VibeLobbies);
            return false;
        }

        Logger.LogDebug($"Creating Room: {name}", LogFilter.VibeLobbies);
        var hostInfo = new RoomParticipant(MainHub.OwnUserData, _config.Data.NicknameInVibeRooms)
        {
            AllowedUids = new List<string>(),
            Devices = _clientToys.InteractableToys.Select(toy => toy.ToToyInfo()).ToList()
        };
        var creationRequest = new RoomCreateRequest(name, hostInfo)
        {
            Description = desc,
            Tags = tags.ToArray(),
            Password = pass,
        };

        var result = await _hub.RoomCreate(creationRequest).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            Logger.LogDebug("Room successfully created.", LogFilter.VibeLobbies);
            _lobbies.OnRoomCreated(name, pass, hostInfo);
            Mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));
            return true;
        }
        else
        {
            Logger.LogError($"Failed to create room. [{result.ErrorCode}]", LogFilter.VibeLobbies);
            return false;
        }
    }

    // Done Via UI.
    public async Task<bool> TryRoomJoin(string name, string? password = null)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Join Room, connection data not synced.", LogFilter.VibeLobbies);
            return new();
        }

        Logger.LogDebug($"Joining room: {name}", LogFilter.VibeLobbies);
        var hostParticipantData = new RoomParticipant(MainHub.OwnUserData, _config.Data.NicknameInVibeRooms)
        {
            AllowedUids = new List<string>(),
            Devices = _clientToys.InteractableToys.Select(toy => toy.ToToyInfo()).ToList()
        };
        var result = await _hub.RoomJoin(name, password, hostParticipantData).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            Logger.LogDebug("Successfully joined room.", LogFilter.VibeLobbies);
            // set the active room to the room we joined, and previous room to the last joined room.
            _lobbies.OnRoomJoined(name, result.Value?.ToDictionary(k => k.User.UID, k => k) ?? new());
            // update the service with the new devices.
            _remotes.AddVibeRoomParticipants(_lobbies.CurrentParticipants);
            return true;
        }
        else
        {
            Logger.LogError($"Failed to join room. [{result.ErrorCode}]", LogFilter.VibeLobbies);
            return false;
        }
    }

    // Done Via UI.
    public async Task TryLeaveVibeRoom()
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Leave Room, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Leaving room: {_lobbies.CurrentRoomName}", LogFilter.VibeLobbies);
        var result = await _hub.RoomLeave().ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
        {
            Logger.LogDebug("Successfully left room.", LogFilter.VibeLobbies);
            _lobbies.OnRoomLeft(out var removed);
            _remotes.RemoveVibeRoomParticipants(removed);
            return;
        }
        else
        {
            Logger.LogError($"Failed to leave room. [{result.ErrorCode}]", LogFilter.VibeLobbies);
            return;
        }
    }

    public async Task SendRoomInvite(RoomInvite dto)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Send Room Invite, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Sending Room Invite to {dto.User}", LogFilter.VibeLobbies);
        var result = await _hub.SendRoomInvite(dto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Room Invite successfully sent.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to send room invite. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }

    public async Task ChangeRoomPassword(string name, string newPass)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Change Room Password, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Changing password for room '{name}'", LogFilter.VibeLobbies);
        var result = await _hub.ChangeRoomPassword(name, newPass).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Room password successfully changed.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to change room password. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }

    public async Task RoomGrantAccess(UserDto dto)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Grant Room Access, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Granting room access to: {dto}", LogFilter.VibeLobbies);
        var result = await _hub.RoomGrantAccess(dto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Room access successfully granted.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to grant room access. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }

    public async Task RoomRevokeAccess(UserDto dto)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to Revoke Room Access, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Revoking room access from: {dto}", LogFilter.VibeLobbies);
        var result = await _hub.RoomRevokeAccess(dto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Room access successfully revoked.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to revoke room access. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }

    public async Task RoomPushDeviceUpdate(ToyInfo dto)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to push device update, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Pushing device update: {dto}", LogFilter.VibeLobbies);
        var result = await _hub.RoomPushDeviceUpdate(dto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Device update successfully sent.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to send device update. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }

    public async Task RoomSendDataStream(ToyDataStream streamDto)
    {
        if (!MainHub.IsConnectionDataSynced)
        {
            Logger.LogDebug("Refusing to send data stream, connection data not synced.", LogFilter.VibeLobbies);
            return;
        }

        Logger.LogDebug($"Sending data stream: {streamDto}", LogFilter.VibeLobbies);
        var result = await _hub.RoomSendDataStream(streamDto).ConfigureAwait(false);
        if (result.ErrorCode is GagSpeakApiEc.Success)
            Logger.LogDebug("Data stream successfully sent.", LogFilter.VibeLobbies);
        else
            Logger.LogError($"Failed to send data stream. [{result.ErrorCode}]", LogFilter.VibeLobbies);
    }
}
