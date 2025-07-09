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
        Interactable = virtualBuzzToy.Interactable;
        VibeMotors = virtualBuzzToy.VibeMotors.Select(m => new SexToyMotor(m)).ToArray();
        RotateMotor = new SexToyMotor(virtualBuzzToy.RotateMotor);
        OscillateMotors = virtualBuzzToy.OscillateMotors.Select(m => new SexToyMotor(m)).ToArray();
    }

    public override SexToyType Type => SexToyType.Simulated;

    public override void VibrateAll(double intensity)
    {
        base.VibrateAll(intensity);
        // adjust audio here.
    }

    public override void Vibrate(uint motorIdx, double intensity)
    {
        base.Vibrate(motorIdx, intensity);
        // adjust audio here.
    }

    public override void VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        base.VibrateDistinct(newValues);
        // adjust audio here.
    }

    public override void OscillateAll(double speed)
    {
        base.OscillateAll(speed);
        // adjust audio here.
    }

    public override void Oscillate(uint motorIdx, double speed)
    {
        base.Oscillate(motorIdx, speed);
        // adjust audio here.
    }

    public override void OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        base.OscillateDistinct(newValues);
        // adjust audio here.
    }

    public override void Rotate(double speed, bool clockwise)
    {
        base.Rotate(speed, clockwise);
        // adjust audio here.
    }

    public override void Constrict(double severity)
    {
        base.Constrict(severity);
        // adjust audio here.
    }

    public override void Inflate(double severity)
    {
        base.Inflate(severity);
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
