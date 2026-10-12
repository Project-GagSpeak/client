using Buttplug.Client;
using Buttplug.Core.Messages;
using CkCommons;
using DebounceThrottle;
using GagSpeak.Interop;
using GagspeakAPI.Attributes;
using GagspeakAPI.Extensions;

namespace GagSpeak.State.Models;

public class IntifaceBuzzToy : BuzzToy
{
    // maxDelay forces a flush every 50ms during continuous input.
    private DebounceDispatcher VibeDebouncer = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50));
    private DebounceDispatcher RotateDebouncer = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50));
    private DebounceDispatcher OscillateDebouncer = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50));
    private DebounceDispatcher ConstrictDebouncer = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50));
    private DebounceDispatcher InflateDebouncer = new(TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(50));

    // Serializes device commands to prevent out-of-order delivery.
    private readonly object _sendLock = new();
    private Task _sendQueue = Task.CompletedTask;

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

    public void ClearDevice()
    {
        _device = null!;
        _deviceIdx = uint.MaxValue;
    }

    public override bool VibrateAll(double intensity)
    {
        if (!DeviceConnected)
            return false;
        if(base.VibrateAll(intensity))
            VibeDebouncer.Debounce(() => SendScalars(ToyMotor.Vibration, ActuatorType.Vibrate));
        return true;
    }

    public override bool Vibrate(uint motorIdx, double intensity)
    {
        if (!DeviceConnected)
            return false;

        if (base.Vibrate(motorIdx, intensity))
            VibeDebouncer.Debounce(() => SendScalars(ToyMotor.Vibration, ActuatorType.Vibrate));
        return true;
    }

    public override bool OscillateAll(double speed)
    {
        if (!DeviceConnected)
            return false;
        if (base.OscillateAll(speed))
            OscillateDebouncer.Debounce(() => SendScalars(ToyMotor.Oscillation, ActuatorType.Oscillate));
        return true;
    }

    public override bool Oscillate(uint motorIdx, double speed)
    {
        if (!DeviceConnected)
            return false;
        if (base.Oscillate(motorIdx, speed))
            OscillateDebouncer.Debounce(() => SendScalars(ToyMotor.Oscillation, ActuatorType.Oscillate));
        return true;
    }

    public override bool Rotate(double speed, bool clockwise)
    {
        if (!DeviceConnected)
            return false;
        if (base.Rotate(speed, clockwise))
            RotateDebouncer.Debounce(() => Enqueue(() => _device.RotateAsync(speed, clockwise)));
        return true;
    }

    public override bool Constrict(double severity)
    {
        if (!DeviceConnected)
            return false;
        if(base.Constrict(severity))
            ConstrictDebouncer.Debounce(() => SendScalars(ToyMotor.Constriction, ActuatorType.Constrict));
        return true;
    }

    public override bool Inflate(double severity)
    {
        if (!DeviceConnected)
            return false;
        if(base.Inflate(severity))
            InflateDebouncer.Debounce(() => SendScalars(ToyMotor.Inflation, ActuatorType.Inflate));
        return true;
    }

    // Batches all motors of the specified type into a single payload so debouncing does not overwrite or drop concurrent motor updates.
    private void SendScalars(ToyMotor type, ActuatorType actuator)
        => Enqueue(() => _device.ScalarAsync(_motorTypeMap[type].Select(m => new ScalarCmd.ScalarSubcommand(m.MotorIdx, m.Intensity, actuator)).ToList()));

    private void Enqueue(Func<Task> send)
    {
        lock (_sendLock)
            _sendQueue = _sendQueue.ContinueWith(_ => send()).Unwrap();
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
            catch (Bagagwa ex)
            {
                Svc.Logger.Error(ex, "Failed to parse motor from token: {Token}", mToken);
            }
        }
        return toy;
    }
}
