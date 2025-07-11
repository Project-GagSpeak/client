using CkCommons;
using GagSpeak.FileSystems;
using GagSpeak.Gui.Remote;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;

namespace GagSpeak.Services;

/// <summary>
///     Service to maintain the active devices selected for recording, 
///     and cache their recorded states.
/// </summary>
public sealed class RemoteService
{
    private readonly ILogger<RemoteService> _logger;
    private readonly GagspeakMediator _mediator;
    public enum RemoteMode
    {
        None,
        Personal,
        Recording,
        Playback,
        VibeRoom,
    }

    // The current mode being supported by the service.
    private RemoteMode _currentMode = RemoteMode.None;

    // Stores all cached Kinkster Devices for all cached Kinkster UID's.
    private Dictionary<string, HashSet<DevicePlotState>> _managedKinksterDevices = new();

    // Stopwatch played in tandem with the UpdateLoopTask.
    private Stopwatch _remoteStopwatch = new();

    // The current cached pattern data and pattern playback idx.
    private Guid _patternId = Guid.Empty;
    private FullPatternData _cachedPatternData = FullPatternData.Empty;
    private int _patternPlaybackIdx = 0;
    private bool _loopPattern = false;
    private TimeSpan _patternStartPoint = TimeSpan.Zero;
    private TimeSpan _patternDuration = TimeSpan.Zero;

    // Kinkster Selection. Default to the main hub UID.
    private string _selectedUid = MainHub.UID;

    // The Task containing the UpdateLoopTask operation, so that it can be canceled by the CTS.
    private Task _updateLoopTask;
    private CancellationTokenSource _updateCTS = new();
    public RemoteService(ILogger<RemoteService> logger, GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public TimeSpan ElapsedTime => _remoteStopwatch.Elapsed;
    public string SelectedKinkster => _selectedUid;
    public bool RemoteIsActive => _remoteStopwatch.IsRunning;
    // If the client is being actively teased. (placeholder value? Maybe?)
    public bool ClientIsBeingBuzzed => ClientDevices.Any(t => t.IsPoweredOn) && RemoteIsActive;
    public RemoteMode CurrentMode => _currentMode;
    public IEnumerable<string> KinksterIdList => _managedKinksterDevices.Keys;
    public IEnumerable<DevicePlotState> ClientDevices => _managedKinksterDevices.GetValueOrDefault(MainHub.UID) ?? new();
    public IEnumerable<DevicePlotState> DevicesForKinkster(string kinksterUid) => _managedKinksterDevices.GetValueOrDefault(kinksterUid) ?? new();

    public void Dispose()
    {
        _logger.LogInformation("Disposing RemoteService and stopping all timers.");
        _remoteStopwatch.Stop();
        _updateCTS.SafeCancel();
        Generic.Safe(() => _updateLoopTask?.Wait(), true);
        _updateCTS.SafeDispose();
    }

    public IEnumerable<DevicePlotState> GetManagedDevicesByMode()
    {
        // Return the devices based on the current mode.
        return _currentMode switch
        {
            RemoteMode.Personal => ClientDevices,
            RemoteMode.Recording => ClientDevices,
            RemoteMode.Playback => ClientDevices,
            RemoteMode.VibeRoom => DevicesForKinkster(_selectedUid),
            _ => Enumerable.Empty<DevicePlotState>(),
        };
    }

    // Can cleanup once we are finished debug logging things.
    public bool TryAddDeviceForKinkster(string uid, BuzzToy device)
    {
        // Ignore if the device is not valid for remotes.
        if (!device.ValidForRemotes)
        {
            _logger.LogWarning($"Device {device.LabelName} is not valid for remotes, cannot add to kinkster {uid}.");
            return false;
        }

        if (!_managedKinksterDevices.TryGetValue(uid, out var kinksterDevices))
        {
            kinksterDevices = new HashSet<DevicePlotState>();
            _managedKinksterDevices[uid] = kinksterDevices;
        }

        // Create a new DevicePlotState and attempt to add (can maybe remove Uid if not nessisary)
        if (kinksterDevices.Add(new DevicePlotState(device, uid)))
        {
            _logger.LogInformation($"Added device {device.LabelName} for kinkster {uid}.");
            return true;
        }
        else
        {
            _logger.LogWarning($"Failed to add device {device.LabelName} for kinkster {uid}, it may already exist.");
            return false;
        }
    }

    // Can cleanup once we are finished debug logging things.
    public bool TryRemoveDeviceForKinkster(string uid, BuzzToy device)
    {
        if (_managedKinksterDevices.TryGetValue(uid, out var kinksterDevices))
        {
            if (kinksterDevices.FirstOrDefault(d => d.Device.Id == device.Id) is { } match)
            {
                // If the device is powered on, power it down first.
                match.PowerDown();
                _logger.LogInformation($"Powered down device {device.LabelName} for kinkster {uid} before removal.");

                // Remove the device from the kinkster's devices.
                if (kinksterDevices.Remove(match))
                {
                    _logger.LogInformation($"Removed device {device.LabelName} for kinkster {uid}.");
                    return true;
                }
            }
        }
        _logger.LogWarning($"Failed to remove device {device.LabelName} for kinkster {uid}, it may not exist.");
        return false;
    }

    // the task update loop handled while in our personal remote.
    private async Task PersonalRemoteLoop()
    {
        await Generic.Safe(async () =>
        {
            while (!_updateCTS.IsCancellationRequested)
            {
                // Send an update to all the client's valid device motors
                foreach (var device in ClientDevices)
                    device.SendLatestToMotors();
                // await 20ms for the next update. (the delay of our debouncers)
                await Task.Delay(20, _updateCTS.Token);
            }
        });

    }
    private async Task PatternRecorderLoop()
    {
        await Generic.Safe(async () =>
        {
            while (!_updateCTS.IsCancellationRequested)
            {
                // Record and update all the client's valid device motors
                foreach (var device in ClientDevices)
                    device.SendLatestToMotors();
                // await 20ms for the next update. (the delay of our debouncers)
                await Task.Delay(20, _updateCTS.Token);
            }
        });
    }
    private async Task PatternPlaybackLoop()
    {
        await Generic.Safe(async () =>
        {
            while (!_updateCTS.IsCancellationRequested)
            {
                // Update all the client's valid device motors to the intensity of the pattern playback idx.
                foreach (var device in ClientDevices)
                    device.SendIndexedPlaybackToMotors(_patternPlaybackIdx);
                
                _patternPlaybackIdx++;

                // If we have hit our cap, but should loop, reset the index, otherwise, break out and stop.
                if (_patternPlaybackIdx >= _patternDuration.Milliseconds / 20)
                {
                    if (_loopPattern)
                    {
                        _patternPlaybackIdx = 0; // Reset playback index if looping.
                        _logger.LogInformation("Pattern playback looped back to start.");
                    }
                    else
                    {
                        _logger.LogInformation("Pattern playback completed.");
                        break;
                    }
                }
                await Task.Delay(20, _updateCTS.Token);
            }
        });
    }

    private async Task VibeRoomRemoteLoop()
    {
        return; // not yet implemented.

        await Generic.Safe(async () =>
        {
            while (!_updateCTS.IsCancellationRequested)
            {
                // Send any updated recieved on your end from others to your client devices, or, update the data sent by self.
                // TODO

                // for any devices that we can send data to, and are sending data to, record the data chunk onto them.
                // TODO

                // await 20ms for the next update. (the delay of our debouncers)
                await Task.Delay(20, _updateCTS.Token);
            }
        });
    }

    public void PowerOnRemote(string uidToPowerOn)
    {
        // if the remote is already powered on, return with an error.
        if (_remoteStopwatch.IsRunning)
        {
            _logger.LogWarning("Cannot use Remote in an undefined mode.");
            return;
        }

        // The Mode determines how we startup the devices.
        switch (_currentMode)
        {
            case RemoteMode.None:
                _logger.LogWarning("Cannot power on Remote in None mode.");
                return;

            case RemoteMode.Personal:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Personal Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }

                // Cleanup all previous data to free up memory.
                foreach (var device in ClientDevices)
                    device.CleanupData(false);

                // Begin the stopwatch and personal remote update loop.
                _logger.LogInformation("Powering on Personal Remote.");
                _remoteStopwatch.Restart();
                _updateCTS = _updateCTS.SafeCancelRecreate();
                _updateLoopTask = PersonalRemoteLoop();
                break;

            case RemoteMode.Recording:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Pattern Recorder Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }

                // Cleanup all previous data to free up memory.
                foreach (var device in ClientDevices)
                    device.CleanupData(false);

                // Begin the stopwatch and pattern recorder update loop.
                _logger.LogInformation("Powering on Recording Remote.");
                _remoteStopwatch.Restart();
                _updateCTS = _updateCTS.SafeCancelRecreate();
                _updateLoopTask = PatternRecorderLoop();
                break;

            case RemoteMode.Playback:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Pattern Playback Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }
                // Cleanup any excess data in the containers..
                foreach (var device in ClientDevices)
                    device.CleanupData(false);

                // Compile and store FullPaternData into the collection of all valid client devices.
                InjectDataIntoClientDevices(_cachedPatternData, _patternStartPoint, _patternDuration);
                _patternPlaybackIdx = 0;

                // Begin the stopwatch and pattern playback update loop.
                _logger.LogInformation("Powering on Playback Remote.");
                _remoteStopwatch.Restart();
                _updateCTS = _updateCTS.SafeCancelRecreate();
                _updateLoopTask = PatternPlaybackLoop();
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Started, _patternId, false);
                break;

            case RemoteMode.VibeRoom:
                _logger.LogInformation("Powering on Vibe Room Remote.");
                return; // not yet implemented.

                // TODO: Handle power on logic here later.

                // Will need to perform cleanup and startup on both client and connected vibeRoomUsers.
                _remoteStopwatch.Restart();
                _updateCTS = _updateCTS.SafeCancelRecreate();
                _updateLoopTask = VibeRoomRemoteLoop();
                break;

        }
    }

    public void PowerOffRemote(string uidToPowerOn)
    {
        // if the remote is already powered on, return with an error.
        if (!_remoteStopwatch.IsRunning)
        {
            _logger.LogError($"Cannot power off a remote that isnt powered on!");
            return;
        }

        // The Mode determines how we startup the devices.
        switch (_currentMode)
        {
            case RemoteMode.None:
                _logger.LogWarning("Cannot use Remote in an undefined mode.");
                return;

            case RemoteMode.Personal:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Personal Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }

                // stop the timer and cancel the task.
                _logger.LogInformation("Powering off Personal Remote.");
                _remoteStopwatch.Stop();
                _updateCTS.SafeCancel();

                // Power down all client devices, and free up memory.
                _logger.LogInformation("Freeing up memory from client device containers.");
                foreach (var device in ClientDevices)
                {
                    device.PowerDown();
                    device.CleanupData(false);
                }
                break;

            case RemoteMode.Recording:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Pattern Recorder Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }

                // stop the timer and cancel the task.
                _logger.LogInformation("Powering off Personal Remote.");
                var duration = _remoteStopwatch.Elapsed;
                _remoteStopwatch.Stop();
                _updateCTS.SafeCancel();

                // Power down all client devices, and free up memory, but keep the recorded data.
                _logger.LogInformation("Freeing up memory from client device containers.");
                foreach (var device in ClientDevices)
                {
                    device.PowerDown();
                    device.CleanupData(true);
                }

                var compiledData = PatternFromDevices(ClientDevices, duration);
                _mediator.Publish(new PatternSavePromptMessage(compiledData, duration));
                _mediator.Publish(new UiToggleMessage(typeof(BuzzToyRemoteUI), ToggleType.Hide));
                break;

            case RemoteMode.Playback:
                if (uidToPowerOn != MainHub.UID)
                {
                    _logger.LogError($"The Pattern Playback Remote is for the client only! UID {uidToPowerOn} is not valid!");
                    return;
                }

                // Stop the timer and cancel the task.
                _logger.LogInformation("Powering off Playback Remote.");
                _remoteStopwatch.Stop();
                _updateCTS.SafeCancel();

                // Power down all client devices, and free up memory, but keep the recorded data.
                _logger.LogInformation("Freeing up memory from client device containers.");
                foreach (var device in ClientDevices)
                {
                    device.PowerDown();
                    device.CleanupData(true);
                }
                GagspeakEventManager.AchievementEvent(UnlocksEvent.PatternAction, PatternInteractionKind.Stopped, _patternId, false);
                break;

            case RemoteMode.VibeRoom:
                _logger.LogInformation("Powering on Vibe Room Remote.");
                return; // not yet implemented.

                // TODO: Handle power on logic here later.

                // Will need to perform cleanup and startup on both client and connected vibeRoomUsers.
                _remoteStopwatch.Restart();
                _updateCTS = _updateCTS.SafeCancelRecreate();
                _updateLoopTask = VibeRoomRemoteLoop();
                break;

        }
    }

    public static FullPatternData PatternFromDevices(IEnumerable<DevicePlotState> deviceStates, TimeSpan duration)
    {
        var patternDevices = new List<PatternDeviceData>();

        foreach (var state in deviceStates)
        {
            if (!state.IsPoweredOn || state.Device is null)
                continue;

            var motorData = new List<PatternMotorData>();
            // Add all Vibe Dots
            for (int i = 0; i < state.VibeDots.Count; i++)
            {
                var dot = state.VibeDots[i];
                if (dot.RecordedData.Count > 0)
                    motorData.Add(new PatternMotorData(
                        CoreIntifaceElement.MotorVibration, state.Device.VibeMotors[i].MotorIdx, dot.RecordedData.ToArray()));
            }

            // Add all Oscillate Dots
            for (int i = 0; i < state.OscillateDots.Count; i++)
            {
                var dot = state.OscillateDots[i];
                if (dot.RecordedData.Count > 0)
                    motorData.Add(new PatternMotorData(
                        CoreIntifaceElement.MotorOscillation, state.Device.OscillateMotors[i].MotorIdx, dot.RecordedData.ToArray()));
            }

            // Add Rotate
            if (state.Device.CanRotate && state.RotateDot.RecordedData.Count > 0)
                motorData.Add(new PatternMotorData(
                    CoreIntifaceElement.MotorRotation, state.Device.RotateMotor.MotorIdx, state.RotateDot.RecordedData.ToArray()));

            // Add Constrict
            if (state.Device.CanConstrict && state.ConstrictDot.RecordedData.Count > 0)
                motorData.Add(new PatternMotorData(
                    CoreIntifaceElement.MotorConstriction, state.Device.ConstrictMotor.MotorIdx, state.ConstrictDot.RecordedData.ToArray()));

            // Add Inflate
            if (state.Device.CanInflate && state.InflateDot.RecordedData.Count > 0)
                motorData.Add(new PatternMotorData(
                    CoreIntifaceElement.MotorInflation, state.Device.InflateMotor.MotorIdx, state.InflateDot.RecordedData.ToArray()));

            if (motorData.Count > 0)
                patternDevices.Add(new PatternDeviceData(state.Device.FactoryName, motorData.ToArray()));
        }

        return new FullPatternData(patternDevices.ToArray());
    }

    // Parses out the client devices for some recorded playback data, at a custom startpoint and duration.
    public void InjectDataIntoClientDevices(FullPatternData pattern, TimeSpan startPoint, TimeSpan duration)
    {
        var results = new List<DevicePlotState>();
        var startIndex = (int)(startPoint.TotalMilliseconds / 20);
        var count = (int)(duration.TotalMilliseconds / 20);

        foreach (var deviceData in pattern.DeviceData)
        {
            // if the device is not a device that we own, and is usable for remotes, do not add it.
            if (ClientDevices.FirstOrDefault(cd => cd.Device.FactoryName == deviceData.DeviceBrand) is not { } matchedDevice)
                continue;

            // Otherwise, insert the recorded data into the devices corrisponding motors, if they are present.
            foreach (var motor in deviceData.MotorDots)
            {
                var sliced = motor.Data.Skip(startIndex).Take(count);

                switch (motor.Type)
                {
                    case CoreIntifaceElement.MotorVibration:
                        if (motor.Index < matchedDevice.Device.VibeMotors.Length)
                            matchedDevice.VibeDots[(int)motor.Index].InjectRecordedPositions(sliced);
                        break;

                    case CoreIntifaceElement.MotorOscillation:
                        if (motor.Index < matchedDevice.Device.OscillateMotors.Length)
                            matchedDevice.OscillateDots[(int)motor.Index].InjectRecordedPositions(sliced);
                        break;

                    case CoreIntifaceElement.MotorRotation:
                        if (matchedDevice.Device.CanRotate)
                            matchedDevice.RotateDot.InjectRecordedPositions(sliced);
                        break;

                    case CoreIntifaceElement.MotorConstriction:
                        if (matchedDevice.Device.CanConstrict)
                            matchedDevice.ConstrictDot.InjectRecordedPositions(sliced);
                        break;

                    case CoreIntifaceElement.MotorInflation:
                        if (matchedDevice.Device.CanInflate)
                            matchedDevice.InflateDot.InjectRecordedPositions(sliced);
                        break;
                }
            }
        }
    }
}
