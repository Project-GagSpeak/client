using GagSpeak.GagspeakConfiguration;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.PlayerState.Toybox;
using GagSpeak.Services.Mediator;
using GagSpeak.StateManagers;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.PlayerState.Listener;

/// <summary> Listens for incoming changes to Alarms, Patterns, Triggers, and WIP, Vibe Server Lobby System. </summary>
/// <remarks> May or may not have future integration weaved into this listener for the vibe server lobby system. </remarks>
public sealed class ToyboxStateListener : DisposableMediatorSubscriberBase
{
    private readonly GagspeakConfigService _mainConfig;
    private readonly PairManager           _pairs;

    // Managers:
    // - SexToyManager (Manages what kind of toy is connected (actual vs simulated), and handles various reactions.
    // MAYBE CONSIDER ADDING:
    // - VibeControlLobbyManager (Future WIP Section)
    // - SpatialAudioManager (Huge WIP, highly dependant on if SCD's data can be properly parsed out or whatever.
    //      I've tried to get it working for so long ive lost hope.)
    private readonly PatternManager _patternManager;
    private readonly AlarmManager   _alarmManager;
    private readonly TriggerManager _triggerManager;
/*    private readonly VibeControlLobbyManager _vibeLobbyManager;
    private readonly SexToyManager = _sexToyManager;
    private readonly ShockCollarManager = _shockCollarManager;*/
/*    private readonly SpatialAudioManager _spatialAudioManager;*/

    // If any of the managers need a seperate applier, append them here.
    private readonly TriggerApplier _triggerApplier;
    private readonly ClientMonitorService  _clientService;
    public ToyboxStateListener(
        ILogger<ToyboxStateListener> logger,
        GagspeakMediator mediator,
        GagspeakConfigService mainConfig,
        PairManager pairs,
        PatternManager patternManager,
        AlarmManager alarmManager,
        TriggerManager triggerManager,
/*        VibeControlLobbyManager vibeLobbyManager, */
        TriggerApplier triggerApplier,
        ClientMonitorService clientService) : base(logger, mediator)
    {
        _mainConfig = mainConfig;
        _pairs = pairs;
        _patternManager = patternManager;
        _alarmManager = alarmManager;
        _triggerManager = triggerManager;
        _triggerApplier = triggerApplier;
        _clientService = clientService;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_pairs.TryGetNickAliasOrUid(enactor, out var nick))
            Mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
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
