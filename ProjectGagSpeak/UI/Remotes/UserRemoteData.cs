using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;

// maybe convert to class, not sure.
public readonly record struct RemoteAccess(
    bool WindowClosable = true,
    bool RemotePower = false,
    bool DeviceStates = false,
    bool MotorStates = false,
    bool MotorFunctions = false,
    bool MotorControl = false
    )
{
    public static readonly RemoteAccess Previewing = new(true, false, false, false, false, false);

    public static readonly RemoteAccess ForcedPlayback = new(false, false, false, false, false, false);

    public static readonly RemoteAccess Playback = new(true, true, false, false, false, false);
    
    public static readonly RemoteAccess Recording = new(false, true, true, true, true, true);

    public static readonly RemoteAccess All = new(true, true, true, true, true, true);
}

// a shared index reference class to hold pattern data that can be ref'd in all associated motors.
public class RemotePlaybackRef
{
    public Guid PatternId { get; set; } = Guid.Empty;
    public int Idx { get; set; } = -1;
    public int Length { get; set; } = -1;
    public bool Looping { get; set; } = false;

    public void Reset()
    {
        PatternId = Guid.Empty;
        Idx = -1;
        Length = -1;
        Looping = false;
    }

    public void SetPattern(Guid patternId, int length, bool looping = false)
    {
        // do not set if the idx is not -1.
        if (Idx != -1)
            return;

        PatternId = patternId;
        Idx = 0;
        Length = length;
        Looping = looping;
    }
}

public sealed class ParticipantPlotedDevices : UserPlotedDevices
{
    public TimeSpan LastCompileTime { get; private set; } = TimeSpan.Zero;
    public ParticipantPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user)
        : base(mediator, user, RemoteAccess.Previewing)
    { }

    protected override void OnControlBegin(string enactor)
    {
        // fire any achievements related to starting control on another participant's devices.

        // do the base startup control logic
        _timeAlive.Start();
    }

    protected override void OnControlEnd(string enactor)
    {
        // stop the timer.
        _timeAlive.Stop();
        // fire any achievements related to ending control on another participant's devices.
    }

    public override void OnUpdateTick()
    {
        // run inside a generic safe task to ensure cancellation and exceptions are handled.
        foreach (var device in _devices)
            device.RecordAndUpdatePosition();
    }

    // Compiles the latest datastream from the participant to be sent off.
    public UserDeviceStream CompileFromRecordingForUser()
    {
        var deviceStreams = new List<DeviceStream>();
        // Compile together the full pattern data from all devices recorded data.
        foreach (var device in _devices)
        {
            // compile the motor data for this device.
            var motorData = device.MotorDotMap.Values
                .Select(m => new MotorStream(m.Motor.Type, m.Motor.MotorIdx, m.RecordedData.ToArray()))
                .ToArray();
            // add the device data if it exists.
            if (motorData.Length > 0)
                deviceStreams.Add(new DeviceStream(device.FactoryName, motorData));
        }

        return new UserDeviceStream(Owner.User, deviceStreams.ToArray());
    }
}

public sealed class ClientPlotedDevices : UserPlotedDevices
{
    // In the below fields, -1 implies that the data is no longer present or running.
    private RemotePlaybackRef _patternInfo = new();
    private RemotePlaybackRef _injectedInfo = new();

    public ClientPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user, RemoteAccess access)
        : base(mediator, user, access)
    { }

    public bool InRecordingMode { get; private set; } = false;
    public Guid ActivePattern => _patternInfo.PatternId;
    public bool IsPlayingPattern => _patternInfo.Idx != -1;
    public bool IsPlayingVibeData => _injectedInfo.Idx != -1;

    public bool TryUpdateRemoteForRecording()
    {
        if (InRecordingMode)
        {
            Svc.Logger.Warning($"Cannot set remote for recording as it is already in recording mode.");
            return false;
        }

        // we should reset the current recording state, and update access for recording.
        TrySetRemotePower(false, MainHub.UID);
        Access = RemoteAccess.All;
        InRecordingMode = true;
        _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));
        return true;
    }

    /// <summary>
    ///     Should be called whenever the remote window closed, and halt any current state.
    /// </summary>
    public void OnRemoteWindowClosed()
    {
        Svc.Logger.Information($"Remote window closed for {Owner.DisplayName}, halting all current state.");
        // if we are in recording mode, we should end the recording and compile the data.
        if (TrySetRemotePower(false, MainHub.UID, true))
            Svc.Logger.Information($"Remote power for {Owner.DisplayName} was successfully turned off.");
        else
            Svc.Logger.Warning($"Failed to turn off remote power for {Owner.DisplayName}.");
    }


    /// <summary>
    ///     Override method for startup on remote power for the client user. <para />
    ///     If you intend to fire achievements or custom operations for Recording Start, add them here. <para />
    ///     If you want to fire events on custom startup for patterns or injections, use their respective methods.
    /// </summary>
    /// <remarks> You will want to fire any achievements in this method, server calls outside it.</remarks>
    protected override void OnControlBegin(string enactor)
    {
        if (InRecordingMode)
        {
            // Achievements for recording start logic here.

        }

        if (IsPlayingPattern)
        {
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackStart, ActivePattern, enactor);
        }
        // do the base startup control logic
        _timeAlive.Start();
    }

    protected override void OnControlEnd(string enactor)
    {
        var timeAlive = TimeAlive;
        _timeAlive.Reset();

        // handle logic for recording end.
        if (InRecordingMode)
        {
            // publish out to the pattern save popup handle.
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and recording ended after {timeAlive.TotalSeconds} seconds.");
            _mediator.Publish(new PatternSavePromptMessage(CompileFromRecording(), timeAlive));
            // reset the recording data state.
            InRecordingMode = false;
        }

        // If we were in the midst of playing back a pattern (was not stopped prior to it ending),
        // manually reset and fire achievements here.
        if (IsPlayingPattern)
        {
            // Process achievements here.
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and playback of pattern ended.");
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackEnd, ActivePattern, enactor);
            _patternInfo.Reset();
            // push new toybox data out so we know the pattern stopped.
            // maybe make another mediator method for it or change how toybox data updates.
        }

        if (IsPlayingVibeData)
        {
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and vibe data playback ended.");
            // fire any achievements on vibe data ending here, once added.
            // No additional logic needs to be performed here.
            _injectedInfo.Idx = -1;
            _injectedInfo.Length = -1;

        }

        // perform a cleanup on all device dot motor dot data.
        foreach (var device in _devices)
        {
            // cleanup the data and remove it from the plotted devices.
            device.CleanupData();
            Svc.Logger.Information($"Powered down device {device.FactoryName} for kinkster {Owner.DisplayName}.");
        }
    }

    /// <summary>
    ///     The update loop task is the essential component of a user's device data. <para />
    ///     Once the timer is running, the update loop task will execute every 20milliseconds. <para />
    ///     Data will be played back, recorded, or update devices based on various conditions. <para />
    /// </summary>
    public override void OnUpdateTick()
    {
        //Svc.Logger.Debug("Processing Update Tick for UserPlotedDevices.");
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
                {
                    // otherwise, set the playback idx to -1 and fire achievements for playback end.
                    _patternInfo.Idx = -1;
                    _patternInfo.Length = -1;
                    Svc.Logger.Information($"Playback ended for {Owner.DisplayName}'s remote data.");
                }
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
            {
                // reset the injected data index to -1, and fire achievements for vibe data end.
                _injectedInfo.Reset();
                Svc.Logger.Information($"Vibe data playback ended for {Owner.DisplayName}'s remote data.");
            }
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
    public bool SwitchPlaybackData(Pattern newPattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        if(!EndPlaybackData(enactor))
        {
            Svc.Logger.Warning($"Failed to end playback data for {Owner.DisplayName} before switching to new pattern playback.");
            return false;
        }
        // Now start up the new playback data.
        if (!StartPlaybackData(newPattern, startPoint, duration, enactor))
        {
            Svc.Logger.Warning($"Failed to start playback data for {Owner.DisplayName} with the new pattern.");
            return false;
        }

        Svc.Logger.Information($"Switched playback data for {Owner.DisplayName} to new pattern {newPattern.Label} at start point {startPoint} with duration {duration}.");
        return true;
    }

    // This assumes the enactor is already valid to do so. May break the flow otherwise.
    public bool EndPlaybackData(string enactor)
    {
        Svc.Logger.Information($"User {enactor} ended currently playing Pattern {ActivePattern} for {Owner.DisplayName}.");
        // fire any achievement events for ending the pattern playback.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackEnd, ActivePattern, enactor);
        // reset the playback data idx and length.
        _patternInfo.Reset();
        return true;
    }

    public bool CanPlaybackPattern(Pattern pattern) // maybe add actor idk
    {
        // get all toys used by the pattern. Motors should not madder, since toys with matching type, have the matching motors.
        foreach (var toy in pattern.PlaybackData.DeviceData.Select(d => d.Toy))
            // check if the devices have one of these, if they do, return true.
            if (_devices.Any(d => d.FactoryName == toy && d.ValidForRemote))
                return true;
        // Otherwise return false.
        return false;
    }

    /// <summary>
    ///     You are expected to validate this action with <seealso cref="CanPlaybackPattern(Pattern)"/>, as this will not check for validity.
    /// </summary>
    /// <returns> True if the pattern started, false otherwise. </returns>
    public bool StartPlaybackData(Pattern pattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        // reject if a pattern is already being played back.
        if (IsPlayingPattern)
        {
            Svc.Logger.Warning($"Cannot inject playback data for {Owner.DisplayName} as a pattern is already being played back.");
            return false;
        }

        Svc.Logger.Information($"Kinkster: {enactor} requested Playback with the pattern ({pattern.Label}) for remote owner ({Owner.DisplayName}) at timestamp.");
        var startIndex = (int)(startPoint.TotalMilliseconds / 20);
        var count = (int)(duration.TotalMilliseconds / 20); // universal truth for size.
        Svc.Logger.Information($"StartingIdx: {startIndex} ({startPoint.ToString()}), Count: {count} ({duration.ToString()}) for pattern playback.");

        // incase the duration is invalid, check and update the count.
        var maxDataLength = pattern.PlaybackData.DeviceData.Max(d => d.MotorData.Max(m => m.Data.Length));
        if (count < maxDataLength)
            count = maxDataLength;

        foreach (var deviceData in pattern.PlaybackData.DeviceData)
        {
            Svc.Logger.Information($"Processing Device: {deviceData.Toy} for playback injection.");
            // if the device is not a device that we own, and is usable for remotes, do not add it.
            if (_devices.FirstOrDefault(cd => cd.FactoryName == deviceData.Toy) is not { } device || !device.ValidForRemote)
                continue;

            Svc.Logger.Information("Device Match Found: " + device.FactoryName);
            foreach (var motor in deviceData.MotorData)
            {
                // if the motor is not present in the device, skip it.
                if (!device.MotorDotMap.TryGetValue(motor.MotorIdx, out var motorDot))
                {
                    Svc.Logger.Warning($"Motor {motor.MotorIdx} not found in device {device.FactoryName}. Skipping injection.");
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
                Svc.Logger.Information($"Injected {sliced.Count()} data points into motor [{motor.MotorIdx}] on device [{device.FactoryName}] for playback.");
            }

            // enable the device if it is not.
            device.IsEnabled = true;
        }

        // Update the playback idx for pattern to be 0 so we know to prioritize it and disable manual control.
        _patternInfo.SetPattern(pattern.Identifier, count, pattern.ShouldLoop);

        // activate if not yet active.
        if (!RemotePowerActive)
        {
            Svc.Logger.Information($"Remote was not active, powering on to begin playback.");
            if (!TrySetRemotePower(true, enactor))
            {
                Svc.Logger.Error($"Failed to power on remote for playback injection for {Owner.DisplayName}. Enactor: {enactor}");
                return false;
            }
            Svc.Logger.Information($"{enactor} powered on the remote sucessfully. Started Pattern Playback for {Owner.DisplayName}'s remote.");
        }

        // Open the remote if not opened already, and begin control if not already in constrol.
        if (!UiService.IsRemoteUIOpen())
        {
            Svc.Logger.Information($"Opening Remote UI for {Owner.DisplayName} to begin pattern playback.");
            _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));
        }
        
        return true;
    }

    // inject the 2 second vibe data stream for our devices where valid at the timestamp index provided, compared against the start time.
    // If no data is found at the time of injection, fire any achievements for being controlled in a vibe lobby beginning.
    public void InjectVibeDataStream(DeviceStream[] dataStream, long timeStamp, string enactor)
    {
        // Reject this if we are not currently being controlled.
        if (!RemotePowerActive)
            return;

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
        // Fire any achievements for vibe data injection here.


        // update the injected data playback idx, if not yet playing.
        if (_injectedInfo.Idx == -1)
        {
            _injectedInfo.Idx = 0;
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.VibeDataStreamStart, Guid.Empty, enactor);
        }
        
        // open and begin playback if not active.
        if (!UiService.IsRemoteUIOpen())
            _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI)));

        if (!RemotePowerActive && TrySetRemotePower(true, enactor))
            Svc.Logger.Information($"User {MainHub.UID} started vibe data playback for {Owner.DisplayName} at timestamp {timeStamp}.");
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


public class UserPlotedDevices
{
    protected readonly GagspeakMediator _mediator;

    protected HashSet<DeviceDot> _devices = new();
    protected Stopwatch _timeAlive = new();
    public UserPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user, RemoteAccess access)
    {
        _mediator = mediator;
        Owner = user;
    }

    public RemoteAccess Access { get; protected set; } = RemoteAccess.Previewing;

    public bool CanControl => _devices.Count > 0;

    public bool RemotePowerActive { get; private set; } = false;

    /// <summary>
    ///     Reflects the current state of the plotted devices. <para />
    ///     If the value is true, it implies that the power is active and they are part of the update loop. <para />
    ///     This value should not be set outside of a centralized control method (update loop).
    /// </summary>
    public bool TrySetRemotePower(bool newValue, string enactor, bool forcedStop = false)
    {
        // if we cannot turn on and we are trying to turn on, reject it.
        if (!forcedStop && (!CanControl && newValue))
        {
            Svc.Logger.Warning($"Cannot set RemotePowerActive to true for {Owner.DisplayName} as they have no devices.");
            return false;
        }

        if (!forcedStop && (RemotePowerActive == newValue))
            return false;

        // if new value is true
        if (newValue)
        {
            RemotePowerActive = true;
            OnControlBegin(enactor);
            return true;
        }
        // if new value is false
        else
        {
            RemotePowerActive = false;
            OnControlEnd(enactor);
            return true;
        }
    }

    public bool UserIsBeingBuzzed => RemotePowerActive && _timeAlive.IsRunning;
    public TimeSpan TimeAlive => _timeAlive.Elapsed;

    public readonly RoomParticipantBase Owner;
    public IReadOnlySet<DeviceDot> Devices => _devices;

    public bool AddDevice(DeviceDot device)
    {
        if (!_devices.Add(device))
        {
            Svc.Logger.Warning($"Failed to add device to {Owner.DisplayName}'s remote data, as it already exists.");
            return false;
        }
        Svc.Logger.Verbose($"Added Device to {Owner.DisplayName}'s remote data.");
        return true;
    }

    public bool RemoveDevice(DeviceDot device)
    {
        // probably a good idea to grab the item from here and clean it up first ye?
        device.CleanupData();
        Svc.Logger.Information($"Powered down device {device.FactoryName} for kinkster.");
        if (!_devices.Remove(device))
        {
            Svc.Logger.Warning($"Failed to remove device from {Owner.DisplayName}'s remote data, as it does not exist.");
            return false;
        }
        Svc.Logger.Verbose($"Removed Device from {Owner.DisplayName}'s remote data.");
        return true;
    }

    public void RemoveAll()
    {
        // cleanup and remove all data within.
        foreach (var device in _devices)
        {
            device.CleanupData();
            Svc.Logger.Information($"Powered down device {device.FactoryName} for kinkster.");
            _devices.Remove(device);
        }
        Svc.Logger.Verbose($"Removed all devices from their remote data.");
    }

    protected virtual void OnControlBegin(string enactor) => _timeAlive.Start();
    protected virtual void OnControlEnd(string enactor) => _timeAlive.Reset();

    /// <summary>
    ///     The update loop task is the essential component of a user's device data. <para />
    ///     Once the timer is running, the update loop task will execute every 20milliseconds. <para />
    ///     Data will be played back, recorded, or update devices based on various conditions. <para />
    /// </summary>
    public virtual void OnUpdateTick()
        => throw new NotImplementedException("OnUpdateTick must be implemented in derived classes.");

}
