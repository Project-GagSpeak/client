using GagSpeak.Kinksters;
using GagSpeak.Services;
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
        TriggerManager triggers, OnFrameworkService frameworkUtils)
    {
        _mediator = mediator;
        _pairs = pairs;
        _vibeLobbies = vibeLobbies;
        _patterns = patterns;
        _alarms = alarms;
        _triggers = triggers;
    }

    public void KinksterJoinedRoom(RoomParticipant newKinkster)
        => _vibeLobbies.OnKinksterJoinedRoom(newKinkster);

    public void KinksterLeftRoom(UserData kinkster)
        => _vibeLobbies.OnKinksterLeftRoom(kinkster);

    public void VibeRoomInviteRecieved(RoomInvite invite)
        => _vibeLobbies.OnInviteRecieved(invite);

    public void VibeRoomHostChanged(UserData newHost)
        => _vibeLobbies.OnHostChanged(newHost);

    public void KinksterUpdatedDevice(UserData kinkster, ToyInfo newDeviceInfo)
        => _vibeLobbies.OnKinksterUpdatedDevice(kinkster, newDeviceInfo);

    public void RecievedBuzzToyDataStream(ToyDataStreamResponse dataStreamChunk)
        => _vibeLobbies.OnRecievedBuzzToyDataStream(dataStreamChunk);

    public void KinksterGrantedAccess(UserData participantWhoGranted)
        => _vibeLobbies.OnKinksterGrantedAccess(participantWhoGranted);

    public void KinksterRevokedAccess(UserData participantWhoRevoked)
        => _vibeLobbies.OnKinksterRevokedAccess(participantWhoRevoked);

    public void PatternSwitched(Guid newPattern, string enactor)
    {
        _patterns.SwitchPattern(newPattern, enactor);
        PostActionMsg(enactor, InteractionType.SwitchPattern, "Pattern Switched");
    }

    public void PatternStarted(Guid patternId, string enactor)
    {
        _patterns.EnablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StartPattern, "Pattern Enabled");
    }

    public void PatternStopped(Guid patternId, string enactor)
    {
        _patterns.DisablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StopPattern, "Pattern Disabled");
    }

    public void AlarmToggled(Guid alarmId, string enactor)
    {
        _alarms.ToggleAlarm(alarmId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleAlarm, "Alarm Toggled");
    }

    public void TriggerToggled(Guid triggerId, string enactor)
    {
        _triggers.ToggleTrigger(triggerId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleTrigger, "Trigger Toggled");
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            _mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }
}
