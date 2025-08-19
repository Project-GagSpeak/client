using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Handlers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.State.Managers;

public class VibeLobbyManager : DisposableMediatorSubscriberBase
{
    private readonly MainConfig _config;
    private readonly BuzzToyManager _clientToys;

    // Information publically accessible.
    private List<RoomInvite> _currentInvites = new();
    private List<RoomListing> _publicVibeRooms = new();
    // Make more of these public as we discover needing them.
    private string? _previousRoomName = null;
    private Dictionary<string, RoomParticipant> _currentParticipants = new();

    public VibeLobbyManager(ILogger<VibeLobbyManager> logger, GagspeakMediator mediator,
        MainConfig config, BuzzToyManager clientToys)
        : base(logger, mediator)
    {
        _config = config;
        _clientToys = clientToys;

        // Update the latest invites upon connection.
        Mediator.Subscribe<PostConnectionDataReceivedMessage>(this, _ =>
        {
            _currentInvites = _.Info.RoomInvites;
            // Reconnect to room if possible.
            if (!string.IsNullOrEmpty(CurrentRoomName))
            {
                // try to maybe rejoin? idk.
            }
        });

        Mediator.Subscribe<MainHubDisconnectedMessage>(this, (msg) =>
        {
            Logger.LogInformation("Disconnected from MainHub, clearing Vibe Room data.");

            // Try to leave the vibe room.
        });
    }

    public string CurrentRoomName { get; private set; } = string.Empty;
    public string CurrentHostUid { get; private set; } = string.Empty;
    public bool IsRoomHost => MainHub.UID.Equals(CurrentHostUid);
    public bool IsInVibeRoom => !string.IsNullOrEmpty(CurrentRoomName);

    public IReadOnlyList<RoomInvite> CurrentInvites => _currentInvites;
    public IReadOnlyList<RoomListing> PublicVibeRooms => _publicVibeRooms;
    public IReadOnlyList<RoomParticipant> CurrentParticipants => _currentParticipants.Values.ToList();
    internal void OnKinksterJoinedRoom(RoomParticipant newKinkster)
    {
        // Add the kinkster to the list of room participants, including their toy data.
        // However, do not add it to the remote service yet, as they have not given us access.

    }

    internal void OnKinksterLeftRoom(UserData kinkster)
    {
        // remove the kinkster from the active room, if present, and also remove their toy data from the remote handler or service as well.


    }

    internal void OnInviteReceived(RoomInvite invite)
    {
        // Add the invite to the current invites list, and notify the user of the new invite.
        _currentInvites.Add(invite);
        Logger.LogInformation($"Received new room invite for room {invite.RoomName}.");
    }

    internal void OnHostChanged(UserData newHost)
    {
        // If the host changed, we should update the room information and notify the user.
        Logger.LogInformation($"Room host changed for the current room, and they now have vibe control!");
        // Update any necessary state related to the host change.
    }

    internal void OnKinksterUpdatedDevice(UserData kinkster, ToyInfo newDeviceInfo)
    {
        // locate which device this kinkster is referring to, and update its information in the toy handler or service.

    }

    internal void OnReceivedBuzzToyDataStream(ToyDataStreamResponse dataStreamChunk)
    {
        // Inject this period of data into the playback stream of the client's devices, switching it from update to playback mode.

        // As long as the client has any queued received data streams, it should prevent access to the remote.

    }

    internal void OnKinksterGrantedAccess(UserData participantWhoGranted)
    {
        // If the participent granted remote access here, we should add their devices to the remote service to interact with.

    }

    internal void OnKinksterRevokedAccess(UserData participantWhoRevoked)
    {
        // Upon a participant revoking access, remove all device control for this user, and subsequently cleave their toys from the data stream.


    }

    /// <summary> Called whenever the client successfully creates a new room, initializing all room information. </summary>
    internal void OnRoomCreated(string name, string pass, RoomParticipant hostInfo)
    {
        // Update the current room.
        _previousRoomName = CurrentRoomName;
        CurrentRoomName = name;
        CurrentHostUid = MainHub.UID;
        _currentParticipants = new() { { hostInfo.User.UID, hostInfo } };

        // we dont need to add any of our devices since they are already hooked up.
        var isPublic = string.IsNullOrWhiteSpace(pass);
        Logger.LogInformation($"Created a new {(isPublic ? "public" : "private")} Vibe Room: {name}.");
    }

    internal void SetPublicVibeRooms(List<RoomListing> rooms)
        => _publicVibeRooms = rooms;

    // May need some changing.
    internal void OnRoomJoined(string roomId, Dictionary<string, RoomParticipant> participants)
    {
        // if for whatever reason we joined one room while connected to another we should leave it first, but whatever.

        // Update our room information.
        _previousRoomName = CurrentRoomName;
        CurrentRoomName = roomId;
        // add all the users already in the room, to the room.
        _currentParticipants = participants;
        Logger.LogInformation($"Joined Vibe Room: {roomId} with {participants.Count} participants.");
    }

    internal void OnRoomLeft(out IEnumerable<string> uidsRemoved)
    {
        uidsRemoved = Enumerable.Empty<string>();
        if (!string.IsNullOrEmpty(CurrentRoomName))
        {
            Logger.LogInformation($"Left Vibe Room: {CurrentRoomName}");
            _previousRoomName = CurrentRoomName;
            CurrentRoomName = string.Empty;
            CurrentHostUid = string.Empty;
            // remove kinkster data.
            uidsRemoved = _currentParticipants.Keys;
            _currentParticipants.Clear();
            Logger.LogInformation("Cleared current room data and participants.");
        }

    }

    public RoomParticipant GetOwnParticipantInfo()
    {
        return new RoomParticipant(MainHub.PlayerUserData, _config.Current.NicknameInVibeRooms)
        {
            AllowedUids = new List<string>(),
            Devices = _clientToys.InteractableToys.Select(toy => toy.ToToyInfo()).ToList()
        };
    }
}
