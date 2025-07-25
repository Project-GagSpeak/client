using Buttplug.Core.Messages;
using CkCommons.Audio;
using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Network;

namespace GagSpeak.State.Models;

public class VirtualBuzzToy : BuzzToy, IEditableStorageItem<VirtualBuzzToy>
{
    public override ToyBrandName FactoryName { get; protected set; } = ToyBrandName.Unknown;
    public override string LabelName { get; set; } = "UNK Device";

    public VirtualBuzzToy()
    { }

    public VirtualBuzzToy(ToyBrandName deviceToSimulate)
        => SetFactoryName(deviceToSimulate);

    public VirtualBuzzToy(VirtualBuzzToy other, bool keepId)
    {
        Id = keepId ? other.Id : Guid.NewGuid();
        ApplyChanges(other);
    }

    public VirtualBuzzToy Clone(bool keepId) 
        => new VirtualBuzzToy(this, keepId);

    public void ApplyChanges(VirtualBuzzToy other)
    {
        FactoryName = other.FactoryName;
        LabelName = other.LabelName;
        BatteryLevel = other.BatteryLevel;
        Interactable = other.Interactable;
        _motorMap.Clear();
        _motorTypeMap.Clear();
        // Copy the motor map and type map from the other toy. (must do manually as it is readonly.
        foreach (var (idx, motor) in other._motorMap)
            _motorMap.TryAdd(idx, motor);

        foreach (var (motor, motorList) in other._motorTypeMap)
            _motorTypeMap.TryAdd(motor, motorList);
    }

    public override SexToyType Type => SexToyType.Simulated;
    public override bool ValidForRemotes => Interactable; // Always valid for virtual toys.

    public void SetFactoryName(ToyBrandName newName)
    {
        if (newName is ToyBrandName.Unknown)
            return;

        Svc.Logger.Information($"Setting FactoryName to {newName} for {FactoryName}");

        FactoryName = newName;
        LabelName = newName.ToName();
        var props = newName.GetDeviceInfo();

        Svc.Logger.Information($"LabelName set to {LabelName} for {newName}");

        // Clear existing motors and motor map.
        _motorMap.Clear();
        _motorTypeMap.Clear();

        // Create new motors based on the properties of the new device, and add to map.
        foreach (var (motor, idx, steps) in props)
        {
            var m = new BuzzToyMotor(idx, steps, motor);
            // map the referece to the motor map.
            _motorMap[idx] = m;
            // place into the motorTypeMap, or add to the value list if it already exists.
            if(!_motorTypeMap.TryGetValue(motor, out var motorList))
            {
                motorList = new List<BuzzToyMotor>();
                _motorTypeMap[motor] = motorList;
            }
            // Add the motor to the list.
            motorList.Add(m);
        }
    }

    // private string GetAudioSystemKey(ToyMotor motor, uint motorIdx) => $"{Id}_{motor}_{motorIdx}";

    public override bool VibrateAll(double intensity)
    {
        if (!base.VibrateAll(intensity))
            return false;

        return false;
    }

    public override bool Vibrate(uint motorIdx, double intensity)
    { 
        if (!base.Vibrate(motorIdx, intensity))
            return false;

        return false;
    }

    public override bool OscillateAll(double speed)
    {
        if (!base.OscillateAll(speed))
            return false;

        return false;
    }

    public override bool Oscillate(uint motorIdx, double speed)
    {
        if (!base.Oscillate(motorIdx, speed))
            return false;

        return false;
    }

    public override bool Rotate(double speed, bool clockwise)
    {
        if (!base.Rotate(speed, clockwise))
            return false;

        return false;
    }

    public override bool Constrict(double severity)
    {
        if (!base.Constrict(severity))
            return false;

        return false;
    }

    public override bool Inflate(double severity)
    {
        if (!base.Inflate(severity))
            return false;
        
        return false;
    }

    public override Task UpdateBattery()
    {
        // Subtract a random between 0.005 and 0.012 every time this is fired.
        BatteryLevel = Math.Max(0.0, BatteryLevel - Random.Shared.NextDouble() * (0.012 - 0.005) + 0.005);
        return Task.CompletedTask;
    }

    public static VirtualBuzzToy FromToken(JToken token)
    {
        var toy = new VirtualBuzzToy()
        {
            Id = Guid.TryParse(token["Id"]?.Value<string>(), out var guid) ? guid : throw new InvalidOperationException("Invalid GUID"),
            FactoryName = Enum.TryParse<ToyBrandName>(token["FactoryName"]?.ToObject<string>(), out var stim) ? stim : ToyBrandName.Unknown,
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

    // aim to avoid this if possible but it might be for the best in the end.
    public static VirtualBuzzToy FromToyInfo(ToyInfo info)
    {
        var newToy = new VirtualBuzzToy();
        newToy.Id = Guid.NewGuid();
        newToy.FactoryName = info.BrandName;
        newToy.LabelName = info.BrandName.ToName();
        newToy.Interactable = info.Interactable;
        // set data for the motors.
        foreach (var (idx, motor) in info.Motors)
        {
            var buzzMotor = new BuzzToyMotor(motor);
            newToy._motorMap[idx] = buzzMotor;
            // place into the motorTypeMap, or add to the value list if it already exists.
            if (!newToy._motorTypeMap.TryGetValue(motor.Type, out var motorList))
            {
                motorList = new List<BuzzToyMotor>();
                newToy._motorTypeMap[motor.Type] = motorList;
            }
            // Add the motor to the list.
            motorList.Add(buzzMotor);
        }
        return newToy;
    }
}
