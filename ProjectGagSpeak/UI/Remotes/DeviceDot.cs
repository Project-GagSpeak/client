using GagSpeak.State.Models;
using GagspeakAPI.Attributes;
using GagspeakAPI.Network;

namespace GagSpeak.Gui.Remote;

public class DeviceDot : IEquatable<DeviceDot>
{
    private readonly BuzzToy _device;
    private readonly Dictionary<uint, MotorDot> _motorDotMap = new();

    public DeviceDot(ToyInfo info)
    {
        _device = VirtualBuzzToy.FromToyInfo(info);
        // init the motor map for a toy info object.
        _motorDotMap = info.Motors.ToDictionary(kvp => kvp.Key, kvp => new MotorDot(new(kvp.Value)));
    }

    public DeviceDot(BuzzToy device)
    {
        _device = device;
        // init the motor map for a BuzzToy device.
        _motorDotMap = device.MotorMap.ToDictionary(kvp => kvp.Key, kvp => new MotorDot(kvp.Value));
    }

    public bool IsEnabled { get; set; } = false;
    public bool IsClockwise { get; set; } = true;
    public bool ValidForRemote => _device.ValidForRemotes;
    public IReadOnlyDictionary<uint, MotorDot> MotorDotMap => _motorDotMap;
    public ToyBrandName FactoryName => _device.FactoryName;
    public string LabelName => _device.LabelName;

    public void OnPlaybackEnd()
    {
        foreach (var dot in _motorDotMap.Values)
            dot.OnPlaybackEnd();
    }

    public void CleanupData()
    {
        foreach (var dot in _motorDotMap.Values)
            dot.ClearData();
        _device.StopAllMotors();
    }

    // can optimize this for datapackets by not sending items that only contain 0.0 ??
    public void RecordAndUpdatePosition()
    {
        foreach (var dot in _motorDotMap.Values)
        {
            dot.RecordPosition(IsEnabled); // records 0.0 if not enabled.
            switch (dot.Motor.Type)
            {
                case ToyMotor.Vibration:
                    _device.Vibrate(dot.MotorIdx, dot.RecordedData.LastOrDefault());
                    break;
                case ToyMotor.Oscillation:
                    _device.Oscillate(dot.MotorIdx, dot.RecordedData.LastOrDefault());
                    break;
                case ToyMotor.Rotation:
                    _device.Rotate(dot.RecordedData.LastOrDefault(), IsClockwise);
                    break;
                case ToyMotor.Constriction:
                    _device.Constrict(dot.RecordedData.LastOrDefault());
                    break;
                case ToyMotor.Inflation:
                    _device.Inflate(dot.RecordedData.LastOrDefault());
                    break;
            }
        }
        // we can avoid this switch statement by compiling the instructions together as sclars and rotaters as we process them,
        // then execute all at once as a single instruction afterwards.
        // Individual calls means more debouncers and more network traffic, so prefer to avoid for smoother performance.
    }

    // can optimize this for datapackets by not sending items that only contain 0.0 ??
    public void UpdatePosition()
    {
        foreach (var dot in _motorDotMap.Values)
        {
            switch (dot.Motor.Type)
            {
                case ToyMotor.Vibration:
                    _device.Vibrate(dot.MotorIdx, Math.Max(dot.LatestIntervalPos(IsEnabled), dot.OverlayIntensity));
                    break;
                case ToyMotor.Oscillation:
                    _device.Oscillate(dot.MotorIdx, Math.Max(dot.LatestIntervalPos(IsEnabled), dot.OverlayIntensity));
                    break;
                case ToyMotor.Rotation:
                    _device.Rotate(Math.Max(dot.LatestIntervalPos(IsEnabled), dot.OverlayIntensity), IsClockwise);
                    break;
                case ToyMotor.Constriction:
                    _device.Constrict(Math.Max(dot.LatestIntervalPos(IsEnabled), dot.OverlayIntensity));
                    break;
                case ToyMotor.Inflation:
                    _device.Inflate(Math.Max(dot.LatestIntervalPos(IsEnabled), dot.OverlayIntensity));
                    break;
            }
        }
        // we can avoid this switch statement by compiling the instructions together as sclars and rotaters as we process them,
        // then execute all at once as a single instruction afterwards.
        // Individual calls means more debouncers and more network traffic, so prefer to avoid for smoother performance.
    }

    public void PlaybackLatestPos()
    {
        try
        {
            // dont bother playing if not enabled.
            if (!IsEnabled)
                return;

            foreach (var motor in _motorDotMap.Values)
            {
                // process the intensity to play, with any layered pattern taking priority if higher.
                var playbackPos = motor.PlaybackRef.Idx == -1 ? 0.0 : Math.Clamp(motor.RecordedData[motor.PlaybackRef.Idx], 0.0, 1.0);
                var intensityToPlay = Math.Max(playbackPos, motor.OverlayIntensity);
                switch (motor.Motor.Type)
                {
                    case ToyMotor.Vibration:
                        _device.Vibrate(motor.MotorIdx, intensityToPlay);
                        break;

                    case ToyMotor.Oscillation:
                        _device.Oscillate(motor.MotorIdx, intensityToPlay);
                        break;

                    case ToyMotor.Rotation:
                        _device.Rotate(intensityToPlay, IsClockwise);
                        break;

                    case ToyMotor.Constriction:
                        _device.Constrict(intensityToPlay);
                        break;

                    case ToyMotor.Inflation:
                        _device.Inflate(intensityToPlay);
                        break;
                }
            }
            // we can avoid this switch statement by compiling the instructions together as sclars and rotaters as we process them,
            // then execute all at once as a single instruction afterwards.
            // Individual calls means more debouncers and more network traffic, so prefer to avoid for smoother performance.
        }
        // This should never happen so if it does, figure out why.
        catch (ArgumentOutOfRangeException)
        {
            // If the playback index is out of range, we can ignore it.
            // This can happen if the user tries to playback a position that doesn't exist.
            Svc.Logger.Warning($"Playback index is out of range for device {_device.FactoryName}. Recorded data count: {_motorDotMap.Values.First().RecordedData.Count}");
        }
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as DeviceDot);
    }

    public bool Equals(DeviceDot? other)
    {
        if (other is null)
            return false;

        // ensure same runtime type.
        if (_device.GetType() != other._device.GetType())
            return false;

        // match factory name.
        return FactoryName == other.FactoryName;
    }

    public override int GetHashCode()
    {
        // Combine both FactoryName and runtime type for safety
        return HashCode.Combine(_device.FactoryName, GetType());
    }
}
