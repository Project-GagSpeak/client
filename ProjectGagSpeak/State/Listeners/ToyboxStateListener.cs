using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagspeakAPI.Data;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Listens for callback changes related to Alarms, Patterns, Triggers. <para/>
///     Unfortunitely was unable to fit most vibe lobby calls in here, maybe will figure out how in the future.
/// </summary>
public sealed class ToyboxStateListener
{
    private readonly GagspeakMediator _mediator;
    private readonly KinksterManager _pairs;

    private readonly VibeLobbyManager _vibeLobbies;
    private readonly PatternManager _patterns;
    private readonly AlarmManager   _alarms;
    private readonly TriggerManager _triggers;
    public ToyboxStateListener(GagspeakMediator mediator, KinksterManager pairs,
        VibeLobbyManager vibeLobbies, PatternManager patterns, AlarmManager alarms,
        TriggerManager triggers)
    {
        _mediator = mediator;
        _pairs = pairs;
        _vibeLobbies = vibeLobbies;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
    }

    public Guid ActivePattern => _patterns.ActivePatternId;

    public void KinksterJoinedRoom(RoomParticipant newKinkster)
        => _vibeLobbies.OnKinksterJoinedRoom(newKinkster);

    public void KinksterLeftRoom(UserData kinkster)
        => _vibeLobbies.OnKinksterLeftRoom(kinkster);

    public void VibeRoomInviteReceived(RoomInvite invite)
        => _vibeLobbies.OnInviteReceived(invite);

    public void VibeRoomHostChanged(UserData newHost)
        => _vibeLobbies.OnHostChanged(newHost);

    public void KinksterUpdatedDevice(UserData kinkster, ToyInfo newDeviceInfo)
        => _vibeLobbies.OnKinksterUpdatedDevice(kinkster, newDeviceInfo);

    public void ReceivedBuzzToyDataStream(ToyDataStreamResponse dataStreamChunk)
        => _vibeLobbies.OnReceivedBuzzToyDataStream(dataStreamChunk);

    public void KinksterGrantedAccess(UserData participantWhoGranted)
        => _vibeLobbies.OnKinksterGrantedAccess(participantWhoGranted);

    public void KinksterRevokedAccess(UserData participantWhoRevoked)
        => _vibeLobbies.OnKinksterRevokedAccess(participantWhoRevoked);

    public bool PatternSwitched(Guid newPattern, string enactor)
    {
        if (!_patterns.SwitchPattern(newPattern, enactor))
            return false;
        
        PostActionMsg(enactor, InteractionType.SwitchPattern, "Pattern Switched");
        return true;
    }

    public bool PatternStarted(Guid patternId, string enactor)
    {
        if (!_patterns.EnablePattern(patternId, enactor))
            return false;
        
        PostActionMsg(enactor, InteractionType.StartPattern, "Pattern Enabled");
        return true;
    }

    public bool PatternStopped(Guid patternId, string enactor)
    {
        if (!_patterns.DisablePattern(patternId, enactor))
            return false;
        
        PostActionMsg(enactor, InteractionType.StopPattern, "Pattern Disabled");
        return true;
    }

    public bool AlarmToggled(Guid alarmId, string enactor)
    {
        if (!_alarms.ToggleAlarm(alarmId, enactor))
            return false;
        
        PostActionMsg(enactor, InteractionType.ToggleAlarm, "Alarm Toggled");
        return true;
    }

    public bool TriggerToggled(Guid triggerId, string enactor)
    {
        if (!_triggers.ToggleTrigger(triggerId, enactor))
            return false;

        PostActionMsg(enactor, InteractionType.ToggleTrigger, "Trigger Toggled");
        return true;
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            _mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }
}
