using Buttplug.Core.Messages;
using GagSpeak.Utils;

namespace GagSpeak.State.Models;

public class VirtualBuzzToy : BuzzToy
{
    public override CoreIntifaceElement FactoryName { get; protected set; } = CoreIntifaceElement.UnknownDevice;
    public override string LabelName { get; set; } = "UNK Device";

    public VirtualBuzzToy()
    { }

    public VirtualBuzzToy(BuzzToy baseDevice, bool keepId)
        : base(baseDevice, keepId)
    { }

    public VirtualBuzzToy(VirtualBuzzToy other, bool keepId)
        : base(other, keepId)
    { }

    public override VirtualBuzzToy Clone(bool keepId) 
        => new VirtualBuzzToy(this, keepId);

    public void ApplyChanges(VirtualBuzzToy other)
        => base.ApplyChanges(other);

    public override SexToyType Type => SexToyType.Simulated;
    public override bool ValidForRemotes => Interactable; // Always valid for virtual toys.

    public void SetFactoryName(CoreIntifaceElement newName)
    {
        if ((int)newName < 5)
            return;
        FactoryName = newName;
    }

    public override bool VibrateAll(double intensity)
    {
        return base.VibrateAll(intensity);
        // adjust audio here.
    }

    public override bool Vibrate(uint motorIdx, double intensity)
    {
        return base.Vibrate(motorIdx, intensity);
        // adjust audio here.
    }

    public override bool VibrateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        return base.VibrateDistinct(newValues);
        // adjust audio here.
    }

    public override bool OscillateAll(double speed)
    {
        return base.OscillateAll(speed);
        // adjust audio here.
    }

    public override bool Oscillate(uint motorIdx, double speed)
    {
        return base.Oscillate(motorIdx, speed);
        // adjust audio here.
    }

    public override bool OscillateDistinct(IEnumerable<ScalarCmd.ScalarCommand> newValues)
    {
        return base.OscillateDistinct(newValues);
        // adjust audio here.
    }

    public override bool Rotate(double speed, bool clockwise)
    {
        return base.Rotate(speed, clockwise);
        // adjust audio here.
    }

    public override bool Constrict(double severity)
    {
        return base.Constrict(severity);
        // adjust audio here.
    }

    public override bool Inflate(double severity)
    {
        return base.Inflate(severity);
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
            FactoryName = Enum.TryParse<CoreIntifaceElement>(token["FactoryName"]?.ToObject<string>(), out var stim) ? stim : CoreIntifaceElement.UnknownDevice,
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
