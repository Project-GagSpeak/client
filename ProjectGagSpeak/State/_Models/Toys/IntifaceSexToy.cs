using Buttplug.Client;
using Buttplug.Core.Messages;
using CkCommons;
using DebounceThrottle;
using GagSpeak.Interop;

namespace GagSpeak.State.Models;

/// <summary>
///     IntifaceBuzzToy is a little special, as it can be Deserialization to hold its cache. <para/>
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
    private DebounceDispatcher ConstrictDebouncer = new(TimeSpan.FromMilliseconds(20));
    private DebounceDispatcher InflateDebouncer = new(TimeSpan.FromMilliseconds(20));

    private ButtplugClientDevice _device;
    private uint _deviceIdx = uint.MaxValue;
    private bool _hasBattery = false;
    public IntifaceBuzzToy()
    {
        // Default constructor for deserialization.
        _device = null!;
        _deviceIdx = uint.MaxValue;
        _hasBattery = false;
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
        _hasBattery = newDevice.HasBattery;
        FactoryName = newDevice.Name;

        if(LabelName == string.Empty)
            LabelName = (string.IsNullOrEmpty(newDevice.DisplayName) 
                ? newDevice.Name : newDevice.DisplayName);

        // obtain and assign vibration motors.
        VibeMotors = newDevice.VibrateAttributes.OrderBy(a => a.Index).Select(attr => new SexToyMotor(attr.Index, attr.StepCount)).ToArray();
        
        // obtain and assign oscillation motors.
        OscillateMotors = newDevice.OscillateAttributes.OrderBy(a => a.Index).Select(attr => new SexToyMotor(attr.Index, attr.StepCount)).ToArray();
        
        // obtain and assign the rotation motor, if it exists.
        if(newDevice.RotateAttributes.FirstOrDefault() is { } attr)
            RotateMotor = new SexToyMotor(attr.Index, attr.StepCount);

        // obtain the constrict motor, if it exists.
        if (newDevice.GenericAcutatorAttributes(ActuatorType.Constrict).FirstOrDefault() is { } constrictAttr)
            ConstrictMotor = new SexToyMotor(constrictAttr.Index, constrictAttr.StepCount);

        // obtain the inflate motor, if it exists.
        if (newDevice.GenericAcutatorAttributes(ActuatorType.Inflate).FirstOrDefault() is { } inflateAttr)
            InflateMotor = new SexToyMotor(inflateAttr.Index, inflateAttr.StepCount);
    }

    public override void VibrateAll(double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.VibrateAll(intensity);
        VibeDebouncer.Debounce(() => _device!.VibrateAsync(intensity));
    }

    public override void Vibrate(uint motorIdx, double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.Vibrate(motorIdx, intensity);
        VibeDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(motorIdx, intensity, ActuatorType.Vibrate)));
    }

    public override void VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.VibrateDistinct(newValues);
        VibeDebouncer.Debounce(() => _device.VibrateAsync(newValues));
    }

    public override void OscillateAll(double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.OscillateAll(speed);
        OscillateDebouncer.Debounce(() => _device!.OscillateAsync(speed));
    }

    public override void Oscillate(uint motorIdx, double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.Oscillate(motorIdx, speed);
        OscillateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand((uint)motorIdx, speed, ActuatorType.Oscillate)));
    }

    public override void OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Values
        base.OscillateDistinct(newValues);
        OscillateDebouncer.Debounce(() => _device.OscillateAsync(newValues));
    }

    public override void Rotate(double speed, bool clockwise)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Value
        base.Rotate(speed, clockwise);
        RotateDebouncer.Debounce(() => _device.RotateAsync(speed, clockwise));
    }

    public override void Constrict(double severity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Value
        base.Constrict(severity);
        ConstrictDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(ConstrictMotor.MotorIdx, severity, ActuatorType.Constrict)));
    }

    public override void Inflate(double severity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return;
        // Update Value
        base.Inflate(severity);
        InflateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(InflateMotor.MotorIdx, severity, ActuatorType.Inflate)));
    }

    public override async Task UpdateBattery()
    {
        if (!_hasBattery || !IsValid)
            return;

        await Generic.Safe(async () => BatteryLevel = await _device.BatteryAsync());
    }

    public static IntifaceBuzzToy FromToken(JToken token)
    {
        return new IntifaceBuzzToy()
        {
            Id = Guid.TryParse(token["Id"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            FactoryName = token["FactoryName"]?.Value<string>() ?? string.Empty,
            LabelName = token["LabelName"]?.Value<string>() ?? string.Empty,
            BatteryLevel = token["BatteryLevel"]?.Value<double>() ?? 0.0,
            Interactable = token["Interactable"]?.Value<bool>() ?? false,

            VibeMotors = token["VibeMotors"] is JArray vArray ? vArray.Select(t => SexToyMotor.FromCompact(t?.ToString())).ToArray() : Array.Empty<SexToyMotor>(),
            OscillateMotors = token["OscillateMotors"] is JArray oArray ? oArray.Select(t => SexToyMotor.FromCompact(t?.ToString())).ToArray() : Array.Empty<SexToyMotor>(),
            RotateMotor = token["RotateMotor"]?.ToString() is { } rotStr ? SexToyMotor.FromCompact(rotStr) : SexToyMotor.Empty,
            ConstrictMotor = token["ConstrictMotor"]?.ToString() is { } constrictStr ? SexToyMotor.FromCompact(constrictStr) : SexToyMotor.Empty,
            InflateMotor = token["InflateMotor"]?.ToString() is { } inflateStr ? SexToyMotor.FromCompact(inflateStr) : SexToyMotor.Empty,
        };
    }
}
