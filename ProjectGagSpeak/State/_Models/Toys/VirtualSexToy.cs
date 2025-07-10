using Buttplug.Core.Messages;
using GagSpeak.Utils;

namespace GagSpeak.State.Models;

public class VirtualBuzzToy : BuzzToy
{
    public override string FactoryName { get; protected set; } = "Virtual Toy";
    public override string LabelName { get; set; } = "UNK";

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

    public void SetFactoryName(CoreIntifaceTexture newName)
    {
        if ((int)newName < 5)
            return;
        FactoryName = newName.ToFactoryName();
    }

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
