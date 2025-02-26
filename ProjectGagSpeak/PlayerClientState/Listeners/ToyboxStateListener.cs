using Dalamud.Game.ClientState.Objects.SubKinds;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Models;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Configs;
using GagSpeak.Services.Mediator;
using GagSpeak.Toybox.Services;
using GagSpeak.UpdateMonitoring;
using GagSpeak.VibeLobby;
using GagSpeak.WebAPI;

namespace GagSpeak.PlayerState.Listener;

/// <summary> Listens for incoming changes to Alarms, Patterns, Triggers, and WIP, Vibe Server Lobby System. </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public sealed class ToyboxStateListener
{
    private readonly GagspeakMediator _mediator;
    private readonly PairManager      _pairs;

    // Managers:
    // - SexToyManager (Manages what kind of toy is connected (actual vs simulated), and handles various reactions.
    // MAYBE CONSIDER ADDING:
    // - VibeControlLobbyManager (Future WIP Section)
    // - SpatialAudioManager (Huge WIP, highly dependant on if SCD's data can be properly parsed out or whatever.
    //      I've tried to get it working for so long ive lost hope.)
    private readonly PatternManager _patternManager;
    private readonly AlarmManager   _alarmManager;
    private readonly TriggerManager _triggerManager;
/*    private readonly VibeRoomManager _vibeLobbyManager;*/    // Currently Caused circular dependancy with the mainhub, see how to fix later.
    private readonly SexToyManager _toyManager;
    // private readonly ShockCollarManager = _shockCollarManager;
    // private readonly SpatialAudioManager _spatialAudioManager;

    private readonly ClientMonitor  _clientMonitor;
    private readonly OnFrameworkService _frameworkUtils;
    public ToyboxStateListener(
        GagspeakMediator mediator,
        PairManager pairs,
        PatternManager patternManager,
        AlarmManager alarmManager,
        TriggerManager triggerManager,
        SexToyManager toyManager,
/*        VibeRoomManager vibeLobbyManager,*/
        ClientMonitor clientMonitor,
        OnFrameworkService frameworkUtils)
    {
        _mediator = mediator;
        _pairs = pairs;
        _patternManager = patternManager;
        _alarmManager = alarmManager;
        _triggerManager = triggerManager;
/*        _vibeLobbyManager = vibeLobbyManager;*/
        _toyManager = toyManager;
        _clientMonitor = clientMonitor;
        _frameworkUtils = frameworkUtils;
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            _mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }

    public void PatternSwitched(Guid newPattern, string enactor)
    {
        _patternManager.SwitchPattern(newPattern, enactor);
        PostActionMsg(enactor, InteractionType.SwitchPattern, "Pattern Switched");

    }

    public void PatternStarted(Guid patternId, string enactor)
    {
        _patternManager.EnablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StartPattern, "Pattern Enabled");
    }

    public void PatternStopped(Guid patternId, string enactor)
    {
        _patternManager.DisablePattern(patternId, enactor);
        PostActionMsg(enactor, InteractionType.StopPattern, "Pattern Disabled");
    }

    public void AlarmToggled(Guid alarmId, string enactor)
    {
        _alarmManager.ToggleAlarm(alarmId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleAlarm, "Alarm Toggled");
    }

    public void TriggerToggled(Guid triggerId, string enactor)
    {
        _triggerManager.ToggleTrigger(triggerId, enactor);
        PostActionMsg(enactor, InteractionType.ToggleTrigger, "Trigger Toggled");
    }
}
