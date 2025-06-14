using GagSpeak.Kinksters;
using GagSpeak.PlayerClient;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.Toybox;

namespace GagSpeak.State.Listeners;

/// <summary>
///     Listens for callback changes related to Alarms, Patterns, Triggers, and future Vibe Server Lobby System.
/// </summary>
public sealed class ToyboxStateListener
{
    private readonly ILogger<ToyboxStateListener> _logger;
    private readonly GagspeakMediator   _mediator;
    private readonly PairManager        _pairs;

    // Managers:
    // - SexToyManager (Manages what kind of toy is connected (actual vs simulated), and handles various reactions.
    // MAYBE CONSIDER ADDING:
    // - VibeControlLobbyManager (Future WIP Section)
    // - SpatialAudioManager (Huge WIP, highly dependant on if SCD's data can be properly parsed out or whatever.
    //      I've tried to get it working for so long ive lost hope.)
    private readonly PatternManager _patterns;
    private readonly AlarmManager   _alarms;
    private readonly TriggerManager _triggers;
/*    private readonly VibeRoomManager _vibeLobbyManager;*/    // Currently Caused circular dependancy with the mainhub, see how to fix later.
    private readonly SexToyManager _toys;
    // private readonly ShockCollarManager = _shockCollars;
    // private readonly SpatialAudioManager _spatialAudio;

    private readonly OnFrameworkService _frameworkUtils;
    public ToyboxStateListener(
        ILogger<ToyboxStateListener> logger,
        GagspeakMediator mediator,
        PairManager pairs,
        PatternManager patterns,
        AlarmManager alarmManager,
        TriggerManager triggers,
        SexToyManager toys,
        OnFrameworkService frameworkUtils)
    {
        _logger = logger;
        _mediator = mediator;
        _pairs = pairs;
        _patterns = patterns;
        _alarms = alarmManager;
        _triggers = triggers;
        // _vibeLobbyManager = vibeLobbyManager;
        _toys = toys;
        _frameworkUtils = frameworkUtils;
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            _mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }

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
}
