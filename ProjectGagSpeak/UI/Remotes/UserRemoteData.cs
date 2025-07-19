using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Network;
using Penumbra.GameData.Interop;

namespace GagSpeak.Gui.Remote;

public sealed class ParticipantPlotedDevices : UserPlotedDevices
{
    public TimeSpan LastCompileTime { get; private set; } = TimeSpan.Zero;
    public ParticipantPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user)
        : base(mediator, user)
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

    private bool _recordingData = false;

    /// <summary>
    ///     Informs us the current index in the playback the pattern is at. <para />
    ///     If -1, it implies not pattern is stored, and replacable.
    /// </summary>
    /// <remarks> This idx is passed down to all RemoteDevice items on each update tick. </remarks>
    private int _playbackDataIdx = -1;
    private int _playbackLength = -1;
    private bool _loopingPattern = false;
    public Guid ActivePattern { get; private set; } = Guid.Empty;

    /// <summary>
    ///     The playback index of the injected vibeData from a vibeLobby. <para />
    ///     This index may need to be shifted around to keep the playback data 
    ///     at a reasonable size, but if -1, it implies no data is injected.
    /// </summary>
    /// <remarks> This idx is passed down to all RemoteDevice items on each update tick. </remarks>
    private int _injectedDataIdx = -1;
    private int _injectedDataLength = -1;

    public ClientPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user) 
        : base(mediator, user)
    { }

    /// <summary>
    ///     If we are recording all pattern input data. Should be set when wishing to record a pattern.
    /// </summary>
    public bool RecordingData { get; set; } = false;

    public bool IsPlayingPattern => _playbackDataIdx != -1;

    public bool IsPlayingVibeData => _injectedDataIdx != -1;

    /// <summary>
    ///     Override method for startup on remote power for the client user. <para />
    ///     If you intend to fire achievements or custom operations for Recording Start, add them here. <para />
    ///     If you want to fire events on custom startup for patterns or injections, use their respective methods.
    /// </summary>
    /// <param name="enactorUid"> who dun did it. </param>
    protected override void OnControlBegin(string enactor)
    {
        if (RecordingData)
        {
            // Achievements for recording start logic here.

        }
        // do the base startup control logic
        _timeAlive.Start();
    }

    protected override void OnControlEnd(string enactor)
    {
        var timeAlive = TimeAlive;
        _timeAlive.Stop();

        // handle logic for recording end.
        if (RecordingData)
        {
            // publish out to the pattern save popup handle.
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and recording ended after {timeAlive.TotalSeconds} seconds.");
            _mediator.Publish(new PatternSavePromptMessage(CompileFromRecording(), timeAlive));
            // reset the recording data state.
            RecordingData = false;
        }

        // If we were in the midst of playing back a pattern (was not stopped prior to it ending),
        // manually reset and fire achievements here.
        if (IsPlayingPattern)
        {
            // Process achievements here.
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and playback of pattern ended.");
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackEnd, ActivePattern, enactor);
            _playbackDataIdx = -1;
            _playbackLength = -1;
            // push new toybox data out so we know the pattern stopped.
            // maybe make another mediator method for it or change how toybox data updates.
        }

        if (IsPlayingVibeData)
        {
            Svc.Logger.Information($"User ended control of {Owner.DisplayName}, and vibe data playback ended.");
            // fire any achievements on vibe data ending here, once added.
            // No additional logic needs to be performed here.
            _injectedDataIdx = -1;
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
                device.PlaybackLatestPos(_playbackDataIdx);
            // update the playback index for the next cycle.
            _playbackDataIdx++;
            // if the playback index is greater than the length of the playback data, reset it.
            if (_playbackDataIdx >= _playbackLength)
            {
                // if looping, simply reset it.
                if (_loopingPattern)
                    _playbackDataIdx = 0;
                else
                {
                    // otherwise, set the playback idx to -1 and fire achievements for playback end.
                    _playbackDataIdx = -1;
                    _playbackLength = -1;
                    Svc.Logger.Information($"Playback ended for {Owner.DisplayName}'s remote data.");
                }
            }
        }
        else if (IsPlayingVibeData)
        {
            // perform a playback of the injected vibe data.
            foreach (var device in _devices)
                device.PlaybackLatestPos(_injectedDataIdx);
            _injectedDataIdx++;
            // if we reached the end, set the index back to -1.
            if (_injectedDataIdx >= _injectedDataLength)
            {
                // reset the injected data index to -1, and fire achievements for vibe data end.
                _injectedDataIdx = -1;
                _injectedDataLength = -1;
                Svc.Logger.Information($"Vibe data playback ended for {Owner.DisplayName}'s remote data.");
            }
        }
        else if (RecordingData)
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
    public void SwitchPlaybackData(Pattern newPattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        EndPlaybackData(enactor);
        // Now start up the new playback data.
        StartPlaybackData(newPattern, startPoint, duration, enactor);
    }

    // This assumes the enactor is already valid to do so. May break the flow otherwise.
    public void EndPlaybackData(string enactor)
    {
        Svc.Logger.Information($"User {enactor} ended currently playing Pattern {ActivePattern} for {Owner.DisplayName}.");
        // fire any achievement events for ending the pattern playback.
        GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackEnd, ActivePattern, enactor);
        // reset the playback data idx and length.
        _playbackDataIdx = -1;
        _playbackLength = -1;
        ActivePattern = Guid.Empty;
    }

    // inject pattern data for playback. After injection, we should invoke any achievements related to pattern playback start.
    // as the next update loop cycle will begin playback. (even over injected vibe data).
    public void StartPlaybackData(Pattern pattern, TimeSpan startPoint, TimeSpan duration, string enactor)
    {
        // reject if a pattern is already being played back.
        if (IsPlayingPattern)
        {
            Svc.Logger.Warning($"Cannot inject playback data for {Owner.DisplayName} as a pattern is already being played back.");
            return;
        }

        try
        {
            var results = new List<DeviceStream>();
            var startIndex = (int)(startPoint.TotalMilliseconds / 20);
            var count = (int)(duration.TotalMilliseconds / 20);

            foreach (var deviceData in pattern.PlaybackData.DeviceData)
            {
                // if the device is not a device that we own, and is usable for remotes, do not add it.
                if (_devices.FirstOrDefault(cd => cd.FactoryName == deviceData.Toy) is not { } device)
                    continue;

                // foreach motor in the device data, locate the motor dot via the mapping, and inject the data into the recorded positions.
                foreach (var motor in deviceData.MotorData)
                {
                    // if the motor is not present in the device, skip it.
                    if (!device.MotorDotMap.TryGetValue(motor.MotorIdx, out var motorDot))
                        continue;

                    // Slice the data via startpoint and duration, and inject it into the motor's recorded positions.
                    var sliced = motor.Data.Skip(startIndex).Take(count);
                    motorDot.InjectPlaybackData(sliced);
                }
            }

            // Update the playback idx for pattern to be 0 so we know to prioritize it and disable manual control.
            _playbackDataIdx = 0;
            _playbackLength = count;
            _loopingPattern = pattern.ShouldLoop;
            ActivePattern = pattern.Identifier;
            GagspeakEventManager.AchievementEvent(UnlocksEvent.RemoteAction, RemoteInteraction.PatternPlaybackStart, ActivePattern, enactor);

            // Open the remote if not opened already, and begin control if not already in constrol.
            if (!UiService.IsRemoteUIOpen())
                _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Show));

            if (!RemotePowerActive && TrySetRemotePower(true, enactor))
                Svc.Logger.Information($"User {MainHub.UID} started Pattern Playback using {pattern.Label} vibe data playback for {Owner.DisplayName} at timestamp.");
            // should also fire a toybox update that we now have an active pattern being played or something i guess?
        }
        catch (Exception ex)
        {
            // prevent any possible unforseen divide by zero's here.
            Svc.Logger.Error($"Error injecting playback data: {ex}");
        }
    }

    // inject the 2 second vibe data stream for our devices where valid at the timestamp index provided, compared against the start time.
    // If no data is found at the time of injection, fire any achievements for being controlled in a vibe lobby beginning.
    public void InjectVibeDataStream(DeviceStream[] dataStream, long timeStamp, string enactor)
    {
        // Reject this if we are not currently being controlled.
        if (!RemotePowerActive)
            return;

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
                if (device.MotorDotMap.TryGetValue(motorStream.MotorIdx, out var motor))
                    motor.InjectPlaybackData(motorStream.Data);
            }
        }

        // Fire any achievements for vibe data injection here.


        // update the injected data playback idx, if not yet playing.
        if (_injectedDataIdx == -1)
        {
            _injectedDataIdx = 0;
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
    public UserPlotedDevices(GagspeakMediator mediator, RoomParticipantBase user)
    {
        _mediator = mediator;
        Owner = user;
    }

    public bool CanControl => _devices.Count > 0;

    public bool RemotePowerActive { get; private set; } = false;

    /// <summary>
    ///     Reflects the current state of the plotted devices. <para />
    ///     If the value is true, it implies that the power is active and they are part of the update loop. <para />
    ///     This value should not be set outside of a centralized control method (update loop).
    /// </summary>
    public bool TrySetRemotePower(bool newValue, string enactor)
    {
        // if we cannot turn on and we are trying to turn on, reject it.
        if (!CanControl && newValue)
        {
            Svc.Logger.Warning($"Cannot set RemotePowerActive to true for {Owner.DisplayName} as they have no devices.");
            return false;
        }

        if (RemotePowerActive == newValue)
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
    protected virtual void OnControlEnd(string enactor) => _timeAlive.Stop();

    /// <summary>
    ///     The update loop task is the essential component of a user's device data. <para />
    ///     Once the timer is running, the update loop task will execute every 20milliseconds. <para />
    ///     Data will be played back, recorded, or update devices based on various conditions. <para />
    /// </summary>
    public virtual void OnUpdateTick()
        => throw new NotImplementedException("OnUpdateTick must be implemented in derived classes.");

}
