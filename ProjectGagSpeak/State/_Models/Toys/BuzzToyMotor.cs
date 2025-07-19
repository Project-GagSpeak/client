using GagspeakAPI.Attributes;
using GagspeakAPI.Network;

namespace GagSpeak.State.Models;

public class BuzzToyMotor
{
    public ToyMotor Type { get; }
    /// <summary>
    ///     The MotorIdx defined upon ButtplugProtocol connection for motor-spesific instructions.
    /// </summary>
    public uint MotorIdx { get; }
    public uint StepCount { get; }
    public double Interval { get; }
    public double Intensity { get; set; }

    public BuzzToyMotor(uint deviceMotorIdx, uint stepCount, ToyMotor type)
    {
        Type = type;
        MotorIdx = deviceMotorIdx;
        StepCount = stepCount > 0 ? stepCount : 1;
        Interval = 1.0 / StepCount;
        Intensity = 0.0;
    }

    public BuzzToyMotor(BuzzToyMotor other)
        : this(other.MotorIdx, other.StepCount, other.Type)
    {
        Intensity = other.Intensity;
    }

    public BuzzToyMotor(Motor networkMotor)
    {
        Type = networkMotor.Type;
        MotorIdx = (uint)networkMotor.MotorIdx;
        StepCount = (uint)networkMotor.StepCount;
    }

    public static BuzzToyMotor Empty => new BuzzToyMotor(uint.MaxValue, uint.MaxValue, ToyMotor.Unknown);

    public void Stop() => Intensity = 0f;

    internal string SerializeCompact()
        => $"({MotorIdx},{StepCount},{(int)Type})";

    /// <summary>
    ///     This will throw if the format is invalid.
    /// </summary>
    public static BuzzToyMotor FromCompact(string? compact)
    {
        if (string.IsNullOrWhiteSpace(compact))
            throw new ArgumentException("Compact string cannot be null or empty.", nameof(compact));

        // Expect format like "(motorIdx,stepCount)"
        var trimmed = compact.Trim('(', ')');
        var parts = trimmed.Split(',');

        // Parse directly - if invalid format or parse fails, it'll throw naturally
        var motorIdx = uint.Parse(parts[0]);
        var stepCount = uint.Parse(parts[1]);
        var type = parts.Length > 2 && Enum.TryParse<ToyMotor>(parts[2], out var parsedType) ? parsedType : ToyMotor.Vibration;

        return new BuzzToyMotor(motorIdx, stepCount, type);
    }
}
