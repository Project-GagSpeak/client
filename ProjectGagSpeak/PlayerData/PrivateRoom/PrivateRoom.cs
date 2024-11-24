using GagSpeak.PlayerData.Factories;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils.ChatLog;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.Toybox;

namespace GagSpeak.PlayerData.PrivateRooms;

/// <summary>
/// Manages the activity of the currently joined Private Room.
/// </summary>
public class PrivateRoom : DisposableMediatorSubscriberBase
{
    private readonly ParticipantFactory _participantFactory;
    private readonly ConcurrentDictionary<string, Participant> _participants; // UserUID, Participant
    // create a lazy list of the participants in the room.
    private Lazy<List<Participant>> _directParticipantsInternal;

    public PrivateRoom(ILogger<PrivateRoom> logger, GagspeakMediator mediator,
        ParticipantFactory participantFactory, RoomInfoDto roomInfo) : base(logger, mediator)
    {
        _participantFactory = participantFactory;
        _participants = new(StringComparer.Ordinal);
        LastReceivedRoomInfo = roomInfo;

        // initialize the list of participants in the room.
        foreach (var participant in roomInfo.ConnectedUsers)
        {
            _participants[participant.UserUID] = _participantFactory.Create(participant);
        }

        // set the chat log up.
        PrivateRoomChatlog = new ChatLog(Mediator);

        // add a dummy message to it.
        PrivateRoomChatlog.AddMessage(new ChatMessage(new("System"), "System", "Welcome to the room!"));

        // initialize the lazy list of participants.
        _directParticipantsInternal = DirectParticipantsLazy();

        // subscribe to the room left event to cleanup chatlog history
        Mediator.Subscribe<ToyboxPrivateRoomLeft>(this, (msg) =>
        {
            if (msg.RoomName == RoomName)
                PrivateRoomChatlog.ClearMessages();

            PrivateRoomChatlog.AddMessage(new ChatMessage(new("System"), "System", "Welcome to the room!"));
        });
    }

    /// <summary>
    /// contains all information related to the room. Heaviest to call Data-wise.
    /// 
    /// NEVER TRUST THIS ROOMS PARTICIPANTS LIST, ALWAYS USE THE PARTICIPANTS PROPERTY. (real list updates lots)
    /// </summary>
    public RoomInfoDto? LastReceivedRoomInfo { get; private set; }

    // the chatlog buffer for the room
    public ChatLog PrivateRoomChatlog { get; private set; }
    public Participant HostParticipant => Participants.First(p => p.User.UserUID == LastReceivedRoomInfo?.RoomHost.UserUID);
    public string RoomName => LastReceivedRoomInfo?.NewRoomName ?? "No Room Name";
    public List<Participant> Participants => _directParticipantsInternal.Value;

    public void AddParticipantToRoom(PrivateRoomUser newUser, bool addToLastAddedParticipant = true)
    {
        Logger.LogTrace("Scanning all participants to see if added user already exists", LoggerType.PrivateRooms);
        // if the user is not in the room's participant list, create a new participant for them.
        if (!_participants.ContainsKey(newUser.UserUID))
        {
            Logger.LogDebug("User " + newUser.ChatAlias + " not found in participants, creating new participant", LoggerType.PrivateRooms);
            // create a new participant object for the user through the participant factory
            _participants[newUser.UserUID] = _participantFactory.Create(newUser);
        }
        // if the user is in the room's participant list, apply the last received data to the participant.
        else
        {
            Logger.LogDebug("User " + newUser.ChatAlias + " found in participants, applying last received data instead.", LoggerType.PrivateRooms);
            // apply the last received data to the participant.
            _participants[newUser.UserUID].User = newUser;
        }
        RecreateLazy();
    }

    // marks the room as inactive, clearing any cached data while still
    // keeping reference data as to not dispose it entirely.
    public void MarkInactive(PrivateRoomUser userToMark)
    {
        if (_participants.TryGetValue(userToMark.UserUID, out var participant))
        {
            // mark them as offline, after modifying their data.
            // (maybe include the below two lines inside the participant class?)
            _participants[userToMark.UserUID].User.ActiveInRoom = false;
            _participants[participant.User.UserUID].User.VibeAccess = userToMark.VibeAccess;
            participant.MarkOffline();
        }
        RecreateLazy();
    }

    // completely removes the user from the room's participant list. and RoomInfo.
    public void RemoveRoomParticipant(PrivateRoomUser dto)
    {
        if (_participants.TryGetValue(dto.UserUID, out var participant))
        {
            // clear stored information
            participant.MarkOffline();
            // try and remove the participant from the room
            _participants.TryRemove(dto.UserUID, out _);
        }
        RecreateLazy();
    }

    public void ParticipantUpdate(PrivateRoomUser dto)
    {
        // if the user in the Dto is not in our private room's participant list, throw an exception.
        if (!_participants.TryGetValue(dto.UserUID, out var participant)) throw new InvalidOperationException("No user found for " + dto.ChatAlias);

        // apply the update to the participant
        _participants[dto.UserUID].User = dto;

        RecreateLazy();
    }

    public void UpdateRoomInfo(RoomInfoDto dto)
    {
        // update the last received room info
        LastReceivedRoomInfo = dto;
        Logger.LogTrace("Updating Room Info for " + dto.NewRoomName, LoggerType.PrivateRooms);
        try
        {
            // update the host
            _participants[dto.RoomHost.UserUID].User.ChatAlias = dto.RoomHost.ChatAlias;
            _participants[dto.RoomHost.UserUID].User.ActiveInRoom = dto.RoomHost.ActiveInRoom;
            _participants[dto.RoomHost.UserUID].User.VibeAccess = dto.RoomHost.VibeAccess;

            // update the participants in the room besides the host.
            foreach (var participant in dto.ConnectedUsers)
            {
                // if the participant is not equal to the stored participant with the same UID, update it.
                if (!_participants.TryGetValue(participant.UserUID, out var storedParticipant))
                {
                    Logger.LogTrace("User " + participant.ChatAlias + " not found in participants, adding them to the room", LoggerType.PrivateRooms);
                    // this means the participant is not in the room, so add them.
                    AddParticipantToRoom(participant);
                }
                else
                {
                    Logger.LogTrace("User " + participant.ChatAlias + " found in participants, updating their data", LoggerType.PrivateRooms);
                    // the participant is already in the room, so update their data with the latest
                    storedParticipant.User.ChatAlias = participant.ChatAlias;
                    storedParticipant.User.ActiveInRoom = participant.ActiveInRoom;
                    storedParticipant.User.VibeAccess = participant.VibeAccess;
                }
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error updating room info for {room}", dto);
        }

        RecreateLazy();
    }

    // fetch a PrivateRoomUser in the participant of the provided userUID.
    public Participant GetParticipant(string userUID) => _participants[userUID];

    // create a concatinating string of all participants chat aliases, seperated by a common,
    public string GetParticipantList() => string.Join(", ", Participants.Select(p => p.User.ChatAlias));

    // get how many participants in the room have their PrivateRoomUser.ActiveInRoom set to true.
    public int GetActiveParticipants() => _participants.Count(p => p.Value.User.ActiveInRoom);

    // see if a particular Participant at the given UserUID key exists in the room with InRoom as true.
    public bool IsParticipantActiveInRoom(string userUID) => _participants.TryGetValue(userUID, out var participant) && participant.User.ActiveInRoom;

    public void AddChatMessage(RoomMessageDto message)
    {
        PrivateRoomChatlog.AddMessage(new ChatMessage(new(message.SenderName.UserUID), message.SenderName.ChatAlias, message.Message));
    }

    /// <summary> Retrieves a participant's device information push data. </summary>
    public void ReceiveParticipantDeviceData(UserCharaDeviceInfoMessageDto dto)
    {
        // if the user in the Dto is not in our private room's participant list, throw an exception.
        if (!_participants.TryGetValue(dto.User.UserUID, out var participant)) throw new InvalidOperationException("No user found for " + dto.User);

        // append the device data to the user's device information list.
        _participants[dto.User.UserUID].ApplyDeviceData(dto);
    }

    public void ApplyDeviceUpdate(UpdateDeviceDto dto)
    {
        // next, see if they are the current room host, as only the host is allowed to update your devices.
        // TODO: Inject this logic, currently participant name system is fucked up.

        // insure the roomname the update is being applied to matches the name of the Room
        if (dto.RoomName != RoomName) throw new InvalidOperationException("Room being applied to is not your room!");

        // If reached here, apply the update to your connected devices.
        Logger.LogDebug("Applying Device Update from " + dto.User);
        // TODO: Inject this logic to update the active devices using the device handler.
    }

    // helper function to see if a particular userData is a participant in the room
    public bool IsUserInRoom(string userUID) => _participants.ContainsKey(userUID);

    public bool IsUserActiveInRoom(string userUID) =>
        _participants.TryGetValue(userUID, out var participant) && participant.User.ActiveInRoom;


    protected override void Dispose(bool disposing)
    {

        if (disposing)
        {
            DisposeParticipants();
        }
        base.Dispose(disposing);
    }

    private Lazy<List<Participant>> DirectParticipantsLazy() => new(() => _participants.Select(k => k.Value).ToList());

    private void DisposeParticipants()
    {
        // log the action about to occur
        Logger.LogDebug("Disposing all Participants of the Private Room", LoggerType.PrivateRooms);
        Parallel.ForEach(_participants, item =>
        {
            item.Value.MarkOffline();
        });
        RecreateLazy();
    }

    private void RecreateLazy()
    {
        _directParticipantsInternal = DirectParticipantsLazy();
    }
}
