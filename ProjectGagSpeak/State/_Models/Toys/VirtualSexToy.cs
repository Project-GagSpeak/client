using Buttplug.Core.Messages;
using CkCommons;
using GagSpeak.Interop;
using GagspeakAPI.Data;

namespace GagSpeak.State.Models;

public class VirtualBuzzToy : BuzzToy, IEditableStorageItem<VirtualBuzzToy>
{
    public VirtualBuzzToy()
    {
        FactoryName = "Virtual Toy";
        LabelName = "UNK";
        // would need a way to dynamically adjust the array sizes if we are not going list?
    }

    public VirtualBuzzToy(VirtualBuzzToy other, bool keepId)
    {
        Id = keepId ? other.Id : Guid.NewGuid();
        ApplyChanges(other);
    }

    public VirtualBuzzToy Clone(bool keepId) => new VirtualBuzzToy(this, keepId);

    public void ApplyChanges(VirtualBuzzToy virtualBuzzToy)
    {
        LabelName = virtualBuzzToy.LabelName;
        BatteryLevel = virtualBuzzToy.BatteryLevel;
        CanInteract = virtualBuzzToy.CanInteract;
        VibeMotors = virtualBuzzToy.VibeMotors.Select(m => new SexToyMotor(m)).ToArray();
        RotateMotors = virtualBuzzToy.RotateMotors.Select(m => new SexToyMotor(m)).ToArray();
        OscillateMotors = virtualBuzzToy.OscillateMotors.Select(m => new SexToyMotor(m)).ToArray();
    }

    public override SexToyType Type => SexToyType.Simulated;

    public override void StopAllMotors()
    {
        if (!IpcCallerIntiface.IsConnected)
            return;

        // stop all motors.
        if (CanVibrate)
            VibrateAll(0.0);

        if (CanRotate)
            RotateAll(0.0, true);

        if (CanOscillate)
            OscillateAll(0.0);
    }

    public override void VibrateAll(double intensity)
    {
        if (!CanVibrate)
            return;

        foreach (var motor in VibeMotors)
            motor.Intensity = intensity;

        // adjust audio here.
    }

    public override void Vibrate(int motorIdx, double intensity)
    {
        if (!CanVibrate || !motorIdx.IsInRange(VibeMotors))
            return;

        VibeMotors[motorIdx].Intensity = intensity;
        // adjust audio here.

    }

    public override void ViberateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanVibrate)
            return;

        foreach (var cmd in newValues)
            VibeMotors[cmd.index].Intensity = cmd.scalar;

        // adjust audio here.
    }

    public override void OscillateAll(double speed)
    {
        if (!CanOscillate)
            return;

        foreach (var motor in OscillateMotors)
            motor.Intensity = speed;

        // adjust audio here.
    }

    public override void Oscillate(int motorIdx, double speed)
    {
        if (!CanOscillate || !motorIdx.IsInRange(OscillateMotors))
            return;

        OscillateMotors[motorIdx].Intensity = speed;
        // adjust audio here.
    }

    public override void OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        if (!CanOscillate)
            return;

        foreach (var cmd in newValues)
            OscillateMotors[cmd.index].Intensity = cmd.scalar;

        // adjust audio here.
    }

    public override void RotateAll(double speed, bool clockwise)
    {
        
        // adjust audio here.
    }

    public override Task UpdateBattery()
    {
        // Simulated toys don't have battery levels.
        return Task.CompletedTask;
    }

    public static VirtualBuzzToy FromToken(JToken token)
    {
        return new VirtualBuzzToy()
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
