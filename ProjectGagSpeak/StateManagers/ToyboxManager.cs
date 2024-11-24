using GagSpeak.GagspeakConfiguration.Models;
using GagSpeak.PlayerData.Data;
using GagSpeak.PlayerData.Pairs;
using GagSpeak.Services.ConfigurationServices;
using GagSpeak.Services.Mediator;
using GagSpeak.UI.Components;
using GagSpeak.UpdateMonitoring;

namespace GagSpeak.PlayerData.Handlers;

/// <summary>
/// State Manager that manages the latest state of Everything within the Toybox.
/// The managers purpose is to funnel calls from both client and server callback through 
/// the same function so that achievements and updates are handled properly.
/// </summary>
public sealed class ToyboxManager : DisposableMediatorSubscriberBase
{
    private readonly ClientConfigurationManager _clientConfigs;
    private readonly PlayerCharacterData _playerData;
    private readonly PatternPlayback _patternPlayback;
    private readonly PairManager _pairManager;
    private readonly OnFrameworkService _frameworkUtils;

    public ToyboxManager(ILogger<ToyboxManager> logger, GagspeakMediator mediator,
        ClientConfigurationManager clientConfigs, PlayerCharacterData playerData,
        PatternPlayback patternPlayback, PairManager pairManager,
        OnFrameworkService frameworkUtils) : base(logger, mediator)
    {
        _clientConfigs = clientConfigs;
        _playerData = playerData;
        _patternPlayback = patternPlayback;
        _pairManager = pairManager;
        _frameworkUtils = frameworkUtils;

        // stop pattern if it stops naturally
        Mediator.Subscribe<PlaybackStateToggled>(this, (msg) => DisablePattern(msg.PatternId));
    }

    public bool ToyboxEnabled => _playerData.GlobalPerms?.ToyboxEnabled ?? false;
    private List<PatternData> Patterns => _clientConfigs.PatternConfig.PatternStorage.Patterns;
    private List<Alarm> Alarms => _clientConfigs.AlarmConfig.AlarmStorage.Alarms;
    private List<Trigger> Triggers => _clientConfigs.TriggerConfig.TriggerStorage.Triggers;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        // Stop any active patterns before disposal.
        if (_clientConfigs.AnyPatternIsPlaying)
            DisablePattern(_clientConfigs.ActivePatternGuid());
    }

    /// <summary>
    /// This logic will occur after a Restraint Set has been enabled via the WardrobeHandler.
    /// </summary>
    public void EnablePattern(Guid id, string enactorUID, bool fireToServer = true, bool fireAchievement = true)
    {
        if (_clientConfigs.AnyPatternIsPlaying)
        {
            Logger.LogWarning("Cannot enable a pattern while another is playing.", LoggerType.ToyboxPatterns);
            return;
        }

        // make sure that the pattern actually exists too.
        var pattern = Patterns.FirstOrDefault(x => x.UniqueIdentifier == id);
        if (pattern is null)
        {
            Logger.LogWarning("Attempted to enable a pattern that does not exist.", LoggerType.ToyboxPatterns);
            return;
        }

        // Go ahead and enable the pattern.
        pattern.IsActive = true;
        _clientConfigs.SavePatterns();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxPatternExecuted));

        // If we are triggering an achievement, do so now.
        if (fireAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Started, pattern.UniqueIdentifier, false);

        // Initialize the playback service to begin its execution.
        var clonedDataForPlayback = pattern.DeepCloneData();
        clonedDataForPlayback.UniqueIdentifier = pattern.UniqueIdentifier;
        CalculateSubsetPatternByteData(clonedDataForPlayback, clonedDataForPlayback.StartPoint, clonedDataForPlayback.PlaybackDuration);
        // store the recalculated byte data in the cloned pattern as the pattern to play.
        _patternPlayback.StartPlayback(clonedDataForPlayback);
    }

    public void DisablePattern(Guid id, bool fireToServer = true, bool fireAchievement = true)
    {
        // make sure that the pattern actually exists too.
        var pattern = Patterns.FirstOrDefault(x => x.UniqueIdentifier == id);
        if (pattern is null)
        {
            Logger.LogWarning("Attempted to disable a pattern that does not exist.", LoggerType.ToyboxPatterns);
            return;
        }

        // Go ahead and disable the pattern.
        pattern.IsActive = false;
        _clientConfigs.SavePatterns();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxPatternStopped));

        // If we are triggering an achievement, do so now.
        if (fireAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Stopped, pattern.UniqueIdentifier, false);

        // Stop the playback service.
        _patternPlayback.StopPlayback();
    }

    public void EnableAlarm(Guid id, bool fireToServer = true, bool fireAchievement = true)
    {
        // make sure that the alarm actually exists too.
        var alarm = Alarms.FirstOrDefault(x => x.Identifier == id);
        if (alarm is null)
        {
            Logger.LogWarning("Attempted to enable an alarm that does not exist.", LoggerType.ToyboxAlarms);
            return;
        }

        // Go ahead and enable the alarm.
        alarm.Enabled = true;
        _clientConfigs.SavePatterns();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxAlarmToggled));

        // If we are triggering an achievement, do so now.
        if (fireAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled, NewState.Enabled);
    }

    public void DisableAlarm(Guid id, bool fireToServer = true, bool fireAchievement = true)
    {
        // make sure that the alarm actually exists too.
        var alarm = Alarms.FirstOrDefault(x => x.Identifier == id);
        if (alarm is null)
        {
            Logger.LogWarning("Attempted to disable an alarm that does not exist.", LoggerType.ToyboxAlarms);
            return;
        }

        // Go ahead and disable the alarm.
        alarm.Enabled = false;
        _clientConfigs.SavePatterns();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxAlarmToggled));

        // If we are triggering an achievement, do so now.
        if (fireAchievement)
            UnlocksEventManager.AchievementEvent(UnlocksEvent.AlarmToggled, NewState.Disabled);
    }

    public void FireAlarmPattern(Alarm alarm)
    {
        // grab the pattern to play from the alarm.
        var pattern = Patterns.FirstOrDefault(x => x.UniqueIdentifier == alarm.PatternToPlay);
        if (pattern is null)
        {
            Logger.LogWarning("Attempted to fire an alarm with a pattern that does not exist.", LoggerType.ToyboxAlarms);
            return;
        }

        // if there is a pattern actively playing, stop it first.
        if (_clientConfigs.AnyPatternIsPlaying)
        {
            // get the active pattern guid and stop it.
            DisablePattern(_clientConfigs.ActivePatternGuid());
        }

        // Go ahead and enable the pattern.
        pattern.IsActive = true;
        _clientConfigs.SavePatterns();

        Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxPatternExecuted));
        UnlocksEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Started, pattern.UniqueIdentifier, true);

        // afterwards, let's enable the pattern for the alarm.
        var clonedDataForPlayback = pattern.DeepCloneData();
        clonedDataForPlayback.UniqueIdentifier = pattern.UniqueIdentifier;
        CalculateSubsetPatternByteData(clonedDataForPlayback, alarm.PatternStartPoint, alarm.PatternDuration);
        // store the recalculated byte data in the cloned pattern as the pattern to play.
        _patternPlayback.StartPlayback(clonedDataForPlayback);
    }

    public void EnableTrigger(Guid id, string enactorUID, bool fireToServer = true)
    {
        // make sure that the trigger actually exists too.
        var trigger = Triggers.FirstOrDefault(x => x.TriggerIdentifier == id);
        if (trigger is null)
        {
            Logger.LogWarning("Attempted to enable a trigger that does not exist.", LoggerType.ToyboxTriggers);
            return;
        }

        // Go ahead and enable the trigger.
        trigger.Enabled = true;
        _clientConfigs.SaveTriggers();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxTriggerToggled));
    }

    public void DisableTrigger(Guid id, bool fireToServer = true, bool fireAchievement = true)
    {
        // make sure that the trigger actually exists too.
        var trigger = Triggers.FirstOrDefault(x => x.TriggerIdentifier == id);
        if (trigger is null)
        {
            Logger.LogWarning("Attempted to disable a trigger that does not exist.", LoggerType.ToyboxTriggers);
            return;
        }

        // Go ahead and disable the trigger.
        trigger.Enabled = false;
        _clientConfigs.SaveTriggers();

        // If we are pushing to the server, do so now.
        if (fireToServer)
            Mediator.Publish(new PlayerCharToyboxChanged(DataUpdateKind.ToyboxTriggerToggled));
    }

    public void ExecuteTrigger(Trigger triggerToFire)
    {
        // if this trigger doesnt exist in the list, we cannot execute it.
        if (!Triggers.Contains(triggerToFire))
        {
            Logger.LogWarning("Attempted to execute a trigger that does not exist.", LoggerType.ToyboxTriggers);
            return;
        }

        // otherwise, do stuff with it.
    }

    public void SafewordUsed()
    {
        // Disable everything from the toybox components.
    }

    /// <summary>
    /// This operation is designed to be done on cloned pattern data. 
    /// Do not attempt prior to making a clone or you will affect the original.
    /// </summary>
    private void CalculateSubsetPatternByteData(PatternData data, TimeSpan startPoint, TimeSpan duration)
    {
        Logger.LogDebug($"Start point at " + startPoint + " and duration at " + duration, LoggerType.ToyboxPatterns);
        Logger.LogDebug("Total byte count of original pattern data: " + data.PatternByteData.Count, LoggerType.ToyboxPatterns);

        // Convert start point and duration to indices
        int _startIndex = (int)(startPoint.TotalSeconds * 50);
        int _endIndex = duration == TimeSpan.Zero
            ? data.PatternByteData.Count
            : _startIndex + (int)(duration.TotalSeconds * 50);

        // Ensure indices are within bounds
        _startIndex = Math.Max(0, _startIndex);
        _endIndex = Math.Min(data.PatternByteData.Count, _endIndex);

        // Log the details
        Logger.LogDebug($"Calculating subset pattern byte data from " + _startIndex + " to " + _endIndex, LoggerType.ToyboxPatterns);
        // Get the subset of the pattern byte data
        List<byte> newData = data.PatternByteData.Skip(_startIndex).Take(_endIndex - _startIndex).ToList();
        // set the data for this object.
        data.PatternByteData = newData;
    }
}
