using Buttplug.Client;
using Buttplug.Core.Messages;
using CkCommons;
using DebounceThrottle;
using GagSpeak.Interop;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Models;

// MAINTAINERS NOTE: for the intiface toy, and maybe all toys,
// efficiency can likely be increased by using the Scalar and Rotate commands directly,
// in order to execute multiple instructions via a single message.

// Something to look into if we need to optimize further, but for now a switch statement will do.
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
        => UpdateDevice(device);

    public override SexToyType Type => SexToyType.Real;
    public override ToyBrandName FactoryName { get; protected set; } = ToyBrandName.Unknown;
    public override string LabelName { get; set; } = "UNK Device";
    public override bool ValidForRemotes => _device != null && DeviceConnected && Interactable;

    public bool DeviceConnected => IpcCallerIntiface.IsConnected && _deviceIdx != uint.MaxValue;
    public uint DeviceIdx => _deviceIdx;

    public void UpdateDevice(ButtplugClientDevice newDevice)
    {
        _device = newDevice;
        _deviceIdx = newDevice.Index;
        _hasBattery = newDevice.HasBattery;
        FactoryName = ToyExtensions.ToBrandName(newDevice.Name);

        if(LabelName == "UNK Device")
            LabelName = (string.IsNullOrEmpty(newDevice.DisplayName) 
                ? newDevice.Name : newDevice.DisplayName);

        // Clear the existing motor mappings.
        _motorMap.Clear();
        _motorTypeMap.Clear();
        // grab the individual motors from the device.
        foreach (var vm in newDevice.VibrateAttributes.Select(attr => new BuzzToyMotor(attr.Index, attr.StepCount, ToyMotor.Vibration)))
        {
            _motorMap.TryAdd(vm.MotorIdx, vm);
            if(!CanVibrate)
                _motorTypeMap.TryAdd(ToyMotor.Vibration, [ vm ]);
            else
                _motorTypeMap[ToyMotor.Vibration].Add(vm);
        }
        // add the oscillation motors.
        foreach (var om in newDevice.OscillateAttributes.Select(attr => new BuzzToyMotor(attr.Index, attr.StepCount, ToyMotor.Oscillation)))
        {
            _motorMap.TryAdd(om.MotorIdx, om);
            if (!CanOscillate)
                _motorTypeMap.TryAdd(ToyMotor.Oscillation, [ om ]);
            else
                _motorTypeMap[ToyMotor.Oscillation].Add(om);
        }
        // add the rotation, constriction, and inflation motors, if they exist.
        if (newDevice.RotateAttributes.FirstOrDefault() is { } rotateAttr)
        {
            var motor = new BuzzToyMotor(rotateAttr.Index, rotateAttr.StepCount, ToyMotor.Rotation);
            _motorMap.TryAdd(motor.MotorIdx, motor);
            if (!CanRotate)
                _motorTypeMap.TryAdd(ToyMotor.Rotation, [motor]);
            else
                _motorTypeMap[ToyMotor.Rotation].Add(motor);
        }

        if (newDevice.GenericAcutatorAttributes(ActuatorType.Constrict).FirstOrDefault() is { } constrictAttr)
        {
            var motor = new BuzzToyMotor(constrictAttr.Index, constrictAttr.StepCount, ToyMotor.Constriction);
            _motorMap.TryAdd(motor.MotorIdx, motor);
            if (!CanConstrict)
                _motorTypeMap.TryAdd(ToyMotor.Constriction, [motor]);
            else
                _motorTypeMap[ToyMotor.Constriction].Add(motor);
        }

        if (newDevice.GenericAcutatorAttributes(ActuatorType.Inflate).FirstOrDefault() is { } inflateAttr)
        {
            var motor = new BuzzToyMotor(inflateAttr.Index, inflateAttr.StepCount, ToyMotor.Inflation);
            _motorMap.TryAdd(motor.MotorIdx, motor);
            if (!CanInflate)
                _motorTypeMap.TryAdd(ToyMotor.Inflation, [motor]);
            else
                _motorTypeMap[ToyMotor.Inflation].Add(motor);
        }
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

        if (base.Vibrate(motorIdx, intensity))
            VibeDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(motorIdx, intensity, ActuatorType.Vibrate)));


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
            OscillateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand(motorIdx, speed, ActuatorType.Oscillate)));
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
            ConstrictDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand
                (_motorTypeMap[ToyMotor.Constriction][0].MotorIdx, severity, ActuatorType.Constrict)));
        return true;
    }

    public override bool Inflate(double severity)
    {
        if (!IpcCallerIntiface.IsConnected)
            return false;
        // Update Value
        if(base.Inflate(severity))
            InflateDebouncer.Debounce(() => _device!.ScalarAsync(new ScalarCmd.ScalarSubcommand
                (_motorTypeMap[ToyMotor.Inflation][0].MotorIdx, severity, ActuatorType.Inflate)));
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
        var toy = new IntifaceBuzzToy()
        {
            Id = Guid.TryParse(token["Id"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            FactoryName = Enum.TryParse<ToyBrandName>(token["FactoryName"]?.ToObject<string>(), out var name) ? name : ToyBrandName.Unknown,
            LabelName = token["LabelName"]?.Value<string>() ?? string.Empty,
            BatteryLevel = token["BatteryLevel"]?.Value<double>() ?? 0.0,
            Interactable = token["Interactable"]?.Value<bool>() ?? false,
        };

        // load in the device motors.
        if (token["DeviceMotors"] is not JArray motorsArray)
            throw new InvalidOperationException("DeviceMotors token is not an array or is missing.");

        foreach (var mToken in motorsArray)
        {
            try
            {
                var motor = BuzzToyMotor.FromCompact(mToken?.ToString());
                toy._motorMap[motor.MotorIdx] = motor;
                // place into the motorTypeMap, or add to the value list if it already exists.
                if (!toy._motorTypeMap.TryGetValue(motor.Type, out var motorList))
                {
                    motorList = new List<BuzzToyMotor>();
                    toy._motorTypeMap[motor.Type] = motorList;
                }
                // Add the motor to the list.
                motorList.Add(motor);
            }
            catch (Exception ex)
            {
                Svc.Logger.Error(ex, "Failed to parse motor from token: {Token}", mToken);
            }
        }
        return toy;
    }
}
