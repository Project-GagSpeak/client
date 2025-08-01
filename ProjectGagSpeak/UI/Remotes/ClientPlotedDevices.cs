using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;
public sealed class ClientPlotedDevices : UserPlotedDevices
{
    // In the below fields, -1 implies that the data is no longer present or running.
    private RemotePlaybackRef _patternInfo = new();
    private RemotePlaybackRef _injectedInfo = new();

    public ClientPlotedDevices(ILogger log, GagspeakMediator mediator, RoomParticipantBase user, RemoteAccess access)
        : base(log, mediator, user, access)
    { }

    public bool InRecordingMode { get; private set; } = false;
    public Guid ActivePattern => _patternInfo.PatternId;
    public bool IsPlayingPattern => _patternInfo.Idx != -1;
    public bool IsPlayingVibeData => _injectedInfo.Idx != -1;

    public bool TryUpdateRemoteForRecording()
    {
        if (InRecordingMode)
            return false;

        // we should reset the current recording state, and update access for recording.
        TrySetRemotePower(false, MainHub.UID);

        // Update recording mode and access.
        InRecordingMode = true;
        Access = RemoteAccess.RecordingStartup;
        Mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));
        return true;
    }

    /// <summary>
    ///     Should be called whenever the remote window closed, and halt any current state.
    /// </summary>
    public void OnRemoteWindowClosed()
    {
        Log.LogInformation($"Remote window closed for {Owner.DisplayName}, halting all current state.");
        // if we are in recording mode, we should end the recording and compile the data.
        SetPowerInternal(false, MainHub.UID);
    }


    /// <summary>
    ///     Override method for startup on remote power for the client user. <para />
    ///     If you intend to fire achievements or custom operations for Recording Start, add them here. <para />
    ///     If you want to fire events on custom startup for patterns or injections, use their respective methods.
    /// </summary>
    /// <remarks> You will want to fire any achievements in this method, server calls outside it.</remarks>
    protected override void OnControlBegin(string enactor)
    {
        if (!InRecordingMode && !IsPlayingPattern && !IsPlayingVibeData)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PersonalStart, Guid.Empty, enactor);

        if (InRecordingMode)
            OnRecordingBegin(MainHub.UID);

        if (IsPlayingPattern)
            OnPatternPlaybackBegin(enactor);

        // do the base startup control logic
        _timeAlive.Start();
    }

    protected override void OnControlEnd(string enactor)
    {
        var timeAlive = TimeAlive;
        _timeAlive.Reset();

        if (!InRecordingMode && !IsPlayingPattern && !IsPlayingVibeData && timeAlive > TimeSpan.Zero)
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PersonalEnd, Guid.Empty, enactor);

        // handle logic for recording end.
        if (InRecordingMode)
            OnRecordingEnd(enactor, timeAlive);

        // If we were in the midst of playing back a pattern (was not stopped prior to it ending),
        if (IsPlayingPattern)
            OnPatternPlaybackEnd(enactor, RemoteSource.PowerOff);

        if (IsPlayingVibeData)
            OnInjectedPlaybackEnd(enactor);

        // perform a cleanup on all device dot motor dot data.
        foreach (var device in _devices)
        {
            // cleanup the data and remove it from the plotted devices.
            device.CleanupData();
            Log.LogInformation($"Powered down device {device.FactoryName} for kinkster {Owner.DisplayName}.");
        }
    }

    /// <summary>
    ///     The update loop task is the essential component of a user's device data. <para />
    ///     Once the timer is running, the update loop task will execute every 20milliseconds. <para />
    ///     Data will be played back, recorded, or update devices based on various conditions. <para />
    /// </summary>
    public override void OnUpdateTick()
    {
        //Log.LogDebug("Processing Update Tick for UserPlotedDevices.");
        // perform the latest data update based on the current state of the plottedDevices.
        if (IsPlayingPattern)
        {
            // perform a playback index update to the devices.
            foreach (var device in _devices)
                device.PlaybackLatestPos();
            // update the playback index for the next cycle.
            _patternInfo.Idx++;
            // if the playback index is greater than the length of the playback data, reset it.
            if (_patternInfo.Idx >= _patternInfo.Length)
            {
                // if looping, simply reset it.
                if (_patternInfo.Looping)
                    _patternInfo.Idx = 0;
                else
                    OnPatternPlaybackEnd(MainHub.UID, RemoteSource.UpdateTick);
            }
        }
        else if (IsPlayingVibeData)
        {
            // perform a playback of the injected vibe data.
            foreach (var device in _devices)
                device.PlaybackLatestPos();
            _injectedInfo.Idx++;
            // if we reached the end, set the index back to -1.
            if (_injectedInfo.Idx >= _injectedInfo.Length)
                OnInjectedPlaybackEnd(MainHub.UID);
        }
        else if (InRecordingMode)
        {
            // if we are recording data, update the latest positions of all devices.
            foreach (var device in _devices)
                device.RecordAndUpdatePosition();
        }
        else
        {
            // if we are recording data, update the latest positions of all devices.
            foreach (var device in _devices)
                device.UpdatePosition();
        }
    }

    // Clears the current pattern and injects a new one for playback.
    public bool SwitchPatternPlaybackData(Pattern newPattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        if (!CanExecuteForDevices(newPattern))
        {
            Log.LogWarning($"Cannot switch playback data for {Owner.DisplayName} as the pattern is not valid for the current devices.");
            return false;
        }

        // shut down the current pattern, if one is present.
        OnPatternPlaybackEnd(enactor, RemoteSource.PatternSwitch);

        // Now start up the new playback data.
        if (!TryStartPatternPlayback(newPattern, startPoint, duration, enactor))
        {
            Log.LogWarning($"Failed to start playback data for {Owner.DisplayName} with the new pattern.");
            return false;
        }

        Log.LogInformation($"Switched playback data for {Owner.DisplayName} to new pattern {newPattern.Label} at start point {startPoint} with duration {duration}.");
        return true;
    }

    private void OnRecordingBegin(string enactor)
    {
        Log.LogInformation($"User {enactor} started recording data for {Owner.DisplayName}.");
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternRecordStart, Guid.Empty, enactor);
        Access = RemoteAccess.Recording;
    }

    private void OnRecordingEnd(string enactor, TimeSpan timeAlive)
    {
        if (!InRecordingMode)
            return;
        Log.LogInformation($"User {enactor} ended recording data for {Owner.DisplayName}.");
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternRecordEnd, Guid.Empty, enactor);
        if (timeAlive > TimeSpan.Zero)
            Mediator.Publish(new PatternSavePromptMessage(CompileFromRecording(), timeAlive));
        // reset the recording data state.
        InRecordingMode = false;
        Access = RemoteAccess.Full; // reset access to all.
    }

    // The assumes the _patternInfo is already set.
    private void OnPatternPlaybackBegin(string enactor)
    {
        Log.LogInformation($"User {enactor} started playback data for {Owner.DisplayName} with pattern ID {ActivePattern}.");
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackStart, ActivePattern, enactor);
        Access = enactor == MainHub.UID ? RemoteAccess.Playback : RemoteAccess.ForcedPlayback;
        // Server-side, any time an active pattern update is recieved, it sends that update back to us and all our paired Kinksters.
        // Thus, if only send the update if the call is not self-invoked.
        if (enactor == MainHub.UID)
            Mediator.Publish(new ActivePatternChangedMessage(DataUpdateType.PatternExecuted, ActivePattern));
    }

    // This assumes the enactor is already valid to do so. May break the flow otherwise.
    public void OnPatternPlaybackEnd(string enactor, RemoteSource callSource)
    {
        if (!IsPlayingPattern)
            return;

        Log.LogInformation($"User {enactor} ended currently playing Pattern {ActivePattern} for {Owner.DisplayName}.");
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackEnd, ActivePattern, enactor);
        _patternInfo.Reset();
        Access = RemoteAccess.Full;
        // Server-side, any time an active pattern update is recieved, it sends that update back to us and all our paired Kinksters.
        // Thus, if only send the update if the call is not self-invoked.
        if (enactor == MainHub.UID && callSource is not RemoteSource.PatternSwitch)
            Mediator.Publish(new ActivePatternChangedMessage(DataUpdateType.PatternStopped, Guid.Empty));

        // if the call source is not from a power on or down, we must perform cleanup.
        if (callSource is not RemoteSource.PowerOn and not RemoteSource.PowerOff)
            foreach (var device in _devices)
                device.OnPlaybackEnd();
    }

    public void OnInjectedPlaybackEnd(string enactor)
    {
        if (!IsPlayingVibeData)
            return;

        Log.LogInformation($"User {enactor} ended currently playing Vibe Data for {Owner.DisplayName}.");
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.VibeDataStreamEnd, Guid.Empty, enactor);
        _injectedInfo.Reset();
    }

    /// <summary>
    ///     Determines if a pattern can be executed with the current devices assigned to the remote.
    /// </summary>
    public bool CanExecuteForDevices(Pattern pattern)
    {
        foreach (var toy in pattern.PlaybackData.DeviceData.Select(d => d.Toy))
            if (_devices.Any(d => d.FactoryName == toy && d.ValidForRemote))
            {
                Log.LogDebug($"At least one active toy was valid for this pattern!");
                return true;
            }

        Log.LogDebug($"No valid toys found for playback pattern {pattern.Label} for {Owner.DisplayName}.");
        return false;
    }

    /// <summary>
    ///     You are expected to validate this action with <seealso cref="CanExecuteForDevices(Pattern)"/>, as this will not check for validity.
    /// </summary>
    public bool TryStartPatternPlayback(Pattern pattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        // reject if a pattern is already being played back.
        if (IsPlayingPattern)
        {
            Log.LogWarning($"Cannot inject playback data for {Owner.DisplayName} as a pattern is already being played back.");
            return false;
        }

        Log.LogInformation($"Kinkster: {enactor} requested Playback with the pattern ({pattern.Label}) for remote owner ({Owner.DisplayName}) at timestamp.");
        var startIndex = (int)(startPoint.TotalMilliseconds / 20);
        var count = (int)(duration.TotalMilliseconds / 20);
        Log.LogInformation($"StartingIdx: {startIndex} ({startPoint.ToString()}), Count: {count} ({duration.ToString()}) for pattern playback.");

        foreach (var deviceData in pattern.PlaybackData.DeviceData)
        {
            Log.LogInformation($"Processing Device: {deviceData.Toy} for playback injection.");
            // if the device is not a device that we own, and is usable for remotes, do not add it.
            if (_devices.FirstOrDefault(cd => cd.FactoryName == deviceData.Toy) is not { } device || !device.ValidForRemote)
                continue;

            Log.LogInformation("Device Match Found: " + device.FactoryName);
            foreach (var motor in deviceData.MotorData)
            {
                // if the motor is not present in the device, skip it.
                if (!device.MotorDotMap.TryGetValue(motor.MotorIdx, out var motorDot))
                {
                    Log.LogWarning($"Motor {motor.MotorIdx} not found in device {device.FactoryName}. Skipping injection.");
                    continue;
                }

                // Slice the data via startpoint and duration, and inject it into the motor's recorded positions.
                var sliced = motor.Data.Skip(startIndex).Take(count).ToList();
                // add missing elements to ensure unified size.
                if (sliced.Count < count)
                {
                    var missing = count - sliced.Count;
                    sliced.AddRange(Enumerable.Repeat(0.0, missing));
                }
                // inject data.
                motorDot.InjectPlaybackData(sliced, _patternInfo);
                Log.LogInformation($"Injected {sliced.Count()} data points into motor [{motor.MotorIdx}] on device [{device.FactoryName}] for playback.");
            }

            // enable the device if it is not.
            device.IsEnabled = true;
        }

        // Handle updating pattern info, achievements, and UI
        _patternInfo.SetPattern(pattern.Identifier, count, pattern.ShouldLoop);

        // activate if not yet active.
        if (!RemotePowerActive)
        {
            Log.LogInformation($"Remote was not active, powering on to begin playback.");
            if (!TrySetRemotePower(true, enactor))
            {
                Log.LogError($"Failed to power on remote for playback injection for {Owner.DisplayName}. Enactor: {enactor}");
                return false;
            }
        }
        else
        {
            // Remote is already alive, meaning OnControlBegin already called, so perform OnControlBegin pattern logic here.
            OnPatternPlaybackBegin(enactor);
        }

        // Open the remote if not opened already, and begin control if not already in constrol.
        if (!UiService.IsRemoteUIOpen())
        {
            Log.LogInformation($"Opening Remote UI for {Owner.DisplayName} to begin pattern playback.");
            Mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));
        }
        
        return true;
    }

    // inject the 2 second vibe data stream for our devices where valid at the timestamp index provided, compared against the start time.
    // If no data is found at the time of injection, fire any achievements for being controlled in a vibe lobby beginning.
    public bool TryInjectVibeDataStream(DeviceStream[] dataStream, long timeStamp, string enactor)
    {
        // Reject this if we are not currently being controlled.
        if (!RemotePowerActive)
            return false;

        // get the maximum length among all motors.
        var maxDataLen = dataStream.Max(d => d.MotorData.Max(m => m.Data.Length));

        // it is very important that the data added from this is appended to the motors at the precise index it is intended for.
        foreach (var stream in dataStream)
        {
            // get the device matching the brandName it's intended for. Skip over unfound toys.
            if (_devices.FirstOrDefault(d => d.FactoryName == stream.Toy) is not { } device)
                continue;

            // inject the device data into each of the mapped motors.
            foreach (var motorStream in stream.MotorData)
            {
                // if the motor is not present in the device, skip it.
                if (!device.MotorDotMap.TryGetValue(motorStream.MotorIdx, out var motor))
                    continue;

                // add any missing data as 0.0
                var missingDataCount = maxDataLen - motorStream.Data.Length;
                if (missingDataCount > 0)
                {
                    var missingData = Enumerable.Repeat(0.0, missingDataCount).ToArray();
                    motor.InjectPlaybackData(motorStream.Data.Concat(missingData).ToArray(), _injectedInfo);
                }
                else
                {
                    motor.InjectPlaybackData(motorStream.Data, _injectedInfo);
                }
            }
        }

        // add the max length to the current injected data length.
        _injectedInfo.Length += maxDataLen;
        // update the injected data playback idx, if not yet playing.
        if (_injectedInfo.Idx == -1)
        {
            _injectedInfo.Idx = 0;
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.VibeDataStreamStart, Guid.Empty, enactor);
        }
        
        // open and begin playback if not active.
        if (!UiService.IsRemoteUIOpen())
            Mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));

        if (!RemotePowerActive && TrySetRemotePower(true, enactor))
            Log.LogInformation($"User {MainHub.UID} started vibe data playback for {Owner.DisplayName} at timestamp {timeStamp}.");

        return true;
    }

    // can be made private, or placed into the end power off function. If recording was true,
    // will compile data and send off to a pattern save popup!
    private FullPatternData CompileFromRecording()
    {
        var patternDevices = new List<DeviceStream>();

        // Compile together the full pattern data from all devices recorded data.
        foreach (var device in _devices)
        {
            // compile the motor data for this device.
            var motorData = device.MotorDotMap.Values
                .Select(m => new MotorStream(m.Motor.Type, m.Motor.MotorIdx, m.RecordedData.ToArray()))
                .ToArray();

            // add the device data if it exists.
            if (motorData.Any())
                patternDevices.Add(new DeviceStream(device.FactoryName, motorData));
        }

        return new FullPatternData(patternDevices.ToArray());
    }
}
