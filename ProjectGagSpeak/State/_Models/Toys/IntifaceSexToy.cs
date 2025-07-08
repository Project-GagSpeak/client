using Buttplug.Client;
using Buttplug.Core.Messages;
using CkCommons;
using DebounceThrottle;
using GagSpeak.Interop;

namespace GagSpeak.State.Models;

/// <summary>
///     IntifaceBuzzToy is a little special, as it can be deserialized to hold its cache. <para/>
///     
///     However, it should not be interactable until the <see cref="_device"/> is valid. <para/>
/// 
///     In other words, be sure to know the toy is valid before interacting with it!
/// </summary>
public class IntifaceBuzzToy : BuzzToy
{
    // create a new Debouncer with a 20ms delay. (extend if too fast or run into issues, but this allows for max accuracy)
    private DebounceDispatcher VibeDebouncer = new(TimeSpan.FromMilliseconds(20));
    private DebounceDispatcher RotateDebouncer = new(TimeSpan.FromMilliseconds(20));
    private DebounceDispatcher OscillateDebouncer = new(TimeSpan.FromMilliseconds(20));

    private ButtplugClientDevice _device;
    private uint _deviceIdx = uint.MaxValue;

    public IntifaceBuzzToy()
    {
        // Default constructor for deserialization.
        _device = null!;
        _deviceIdx = uint.MaxValue;
    }

    public IntifaceBuzzToy(ButtplugClientDevice device)
    {
        UpdateDevice(device);
    }

    public override SexToyType Type => SexToyType.Real;
    public uint DeviceIdx => _deviceIdx;
    public bool IsValid => _device is not null && _deviceIdx != uint.MaxValue;

    public void UpdateDevice(ButtplugClientDevice newDevice)
    {
        _device = newDevice;
        _deviceIdx = newDevice.Index;
        FactoryName = newDevice.Name;

        if(LabelName == string.Empty)
            LabelName = (string.IsNullOrEmpty(newDevice.DisplayName) 
                ? newDevice.Name : newDevice.DisplayName);

        VibeMotors = newDevice.VibrateAttributes.OrderBy(a => a.Index).Select(attr => new SexToyMotor(attr.StepCount)).ToArray();
        RotateMotors = newDevice.RotateAttributes.OrderBy(a => a.Index).Select(attr => new SexToyMotor(attr.StepCount)).ToArray();
        OscillateMotors = newDevice.OscillateAttributes.OrderBy(a => a.Index).Select(attr => new SexToyMotor(attr.StepCount)).ToArray();
    }

    public override void StopAllMotors()
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values (which calls all other methods for this)
        base.StopAllMotors();
    }

    public override void VibrateAll(double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.VibrateAll(intensity);
        // Debounce
        VibeDebouncer.Debounce(() => _device!.VibrateAsync(intensity));
    }

    public override void Vibrate(int motorIdx, double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.Vibrate(motorIdx, intensity);
        // Debounce
        VibeDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand((uint)motorIdx, intensity, ActuatorType.Vibrate)));
    }

    public override void ViberateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.ViberateDistinct(newValues);
        // Debounce
        VibeDebouncer.Debounce(() => _device.VibrateAsync(newValues));
    }

    public override void OscillateAll(double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.OscillateAll(speed);
        // Debounce
        OscillateDebouncer.Debounce(() => _device!.OscillateAsync(speed));
    }

    public override void Oscillate(int motorIdx, double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.Oscillate(motorIdx, speed);
        // Debounce
        OscillateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand((uint)motorIdx, speed, ActuatorType.Oscillate)));
    }

    public override void OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.OscillateDistinct(newValues);
        // Debounce
        OscillateDebouncer.Debounce(() => _device.OscillateAsync(newValues));
    }

    public override void RotateAll(double speed, bool clockwise)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.RotateAll(speed, clockwise);
        // Debounce
        RotateDebouncer.Debounce(() => _device.RotateAsync(speed, clockwise));
    }

    public override async Task UpdateBattery()
        => await Generic.Safe(async () => BatteryLevel = await _device.BatteryAsync());

    public static IntifaceBuzzToy FromToken(JToken token)
    {
        return new IntifaceBuzzToy()
        {
            FactoryName = token["FactoryName"]?.Value<string>() ?? string.Empty,
            LabelName = token["LabelName"]?.Value<string>() ?? string.Empty,
            BatteryLevel = token["BatteryLevel"]?.Value<double>() ?? 0.0,
            CanInteract = token["CanInteract"]?.Value<bool>() ?? false,
            VibeMotors = token["VibeMotors"]?.Values<int>().Select(v => new SexToyMotor((uint)v)).ToArray() ?? Array.Empty<SexToyMotor>(),
            RotateMotors = token["RotateMotors"]?.Values<int>().Select(v => new SexToyMotor((uint)v)).ToArray() ?? Array.Empty<SexToyMotor>(),
            OscillateMotors = token["OscillateMotors"]?.Values<int>().Select(v => new SexToyMotor((uint)v)).ToArray() ?? Array.Empty<SexToyMotor>(),
        };
    }
}
