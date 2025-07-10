using Buttplug.Client;
using Buttplug.Core.Messages;
using CkCommons;
using DebounceThrottle;
using GagSpeak.Interop;
using GagSpeak.Utils;
using GagspeakAPI.Attributes;

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

    private ButtplugClientDevice _device = null!; // This is set by the constructor or UpdateDevice.
    private uint _deviceIdx = uint.MaxValue;
    private bool _hasBattery = false;
    public IntifaceBuzzToy()
    { }

    public IntifaceBuzzToy(ButtplugClientDevice device)
    {
        UpdateDevice(device);
    }

    public IntifaceBuzzToy(BuzzToy baseDevice, bool keepId)
    : base(baseDevice, keepId)
    { }

    public IntifaceBuzzToy(IntifaceBuzzToy other, bool keepId)
        : base(other, keepId)
    { }

    public override IntifaceBuzzToy Clone(bool keepId)
        => new IntifaceBuzzToy(this, keepId);

    public void ApplyChanges(IntifaceBuzzToy other)
        => base.ApplyChanges(other);

    public override SexToyType Type => SexToyType.Real;
    public override CoreIntifaceElement FactoryName { get; protected set; } = CoreIntifaceElement.UnknownDevice;
    public override string LabelName { get; set; } = "UNK Device";
    public override bool ValidForRemotes => _device != null && DeviceConnected && Interactable;

    public bool DeviceConnected => IpcCallerIntiface.IsConnected && _deviceIdx != uint.MaxValue;
    public uint DeviceIdx => _deviceIdx;

    public void UpdateDevice(ButtplugClientDevice newDevice)
    {
        _device = newDevice;
        _deviceIdx = newDevice.Index;
        _hasBattery = newDevice.HasBattery;
        FactoryName = GsExtensions.FromFactoryName(newDevice.Name);

        if(LabelName == "UNK Device")
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

    public override bool VibrateAll(double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if(base.VibrateAll(intensity))
            VibeDebouncer.Debounce(() => _device!.VibrateAsync(intensity));
        return true;
    }

    public override bool Vibrate(uint motorIdx, double intensity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if(base.Vibrate(motorIdx, intensity))
            VibeDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(motorIdx, intensity, ActuatorType.Vibrate)));
        return true;
    }

    public override bool VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if(base.VibrateDistinct(newValues))
            VibeDebouncer.Debounce(() => _device.VibrateAsync(newValues));
        return true;
    }

    public override bool OscillateAll(double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if (base.OscillateAll(speed))
            OscillateDebouncer.Debounce(() => _device!.OscillateAsync(speed));
        return true;
    }

    public override bool Oscillate(uint motorIdx, double speed)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if (base.Oscillate(motorIdx, speed))
            OscillateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand((uint)motorIdx, speed, ActuatorType.Oscillate)));
        return true;
    }

    public override bool OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Values
        if(base.OscillateDistinct(newValues))
            OscillateDebouncer.Debounce(() => _device.OscillateAsync(newValues));
        return true;
    }

    public override bool Rotate(double speed, bool clockwise)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Value
        if (base.Rotate(speed, clockwise))
            RotateDebouncer.Debounce(() => _device.RotateAsync(speed, clockwise));
        return true;
    }

    public override bool Constrict(double severity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Value
        if(base.Constrict(severity))
            ConstrictDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(ConstrictMotor.MotorIdx, severity, ActuatorType.Constrict)));
        return true;
    }

    public override bool Inflate(double severity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Value
        if(base.Inflate(severity))
            InflateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(InflateMotor.MotorIdx, severity, ActuatorType.Inflate)));
        return true;
    }

    public override async Task UpdateBattery()
    {
        if (!_hasBattery || !DeviceConnected)
            return;

        await Generic.Safe(async () => BatteryLevel = await _device.BatteryAsync());
    }

    public static IntifaceBuzzToy FromToken(JToken token)
    {
        return new IntifaceBuzzToy()
        {
            Id = Guid.TryParse(token["Id"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            FactoryName = Enum.TryParse<CoreIntifaceElement>(token["FactoryName"]?.ToObject<string>(), out var name) ? name : CoreIntifaceElement.UnknownDevice,
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
