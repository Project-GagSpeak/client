namespace GagSpeak.State.Models;

public class SexToyMotor
{
    /// <summary>
    ///     The MotorIdx defined upon ButtplugProtocol connection for motor-spesific instructions.
    /// </summary>
    public uint MotorIdx { get; }
    public uint StepCount { get; }
    public double Interval { get; }
    public double Intensity { get; set; }

    public SexToyMotor(uint deviceMotorIdx, uint stepCount)
    {
        MotorIdx = deviceMotorIdx;
        StepCount = stepCount > 0 ? stepCount : 1;
        Interval = 1.0 / StepCount;
        Intensity = 0.0;
    }

    public SexToyMotor(SexToyMotor other)
        : this(other.MotorIdx, other.StepCount)
    {
        Intensity = other.Intensity;
    }

    public static SexToyMotor Empty => new SexToyMotor(uint.MaxValue, uint.MaxValue);

    public void Stop() => Intensity = 0f;

    internal string SerializeCompact()
        => $"({MotorIdx},{StepCount})";

    /// <summary>
    ///     This will throw if the format is invalid.
    /// </summary>
    public static SexToyMotor FromCompact(string? compact)
    {
        if (string.IsNullOrWhiteSpace(compact))
            throw new ArgumentException("Compact string cannot be null or empty.", nameof(compact));

        // Expect format like "(motorIdx,stepCount)"
        var trimmed = compact.Trim('(', ')');
        var parts = trimmed.Split(',');

        // Parse directly - if invalid format or parse fails, it'll throw naturally
        var motorIdx = uint.Parse(parts[0]);
        var stepCount = uint.Parse(parts[1]);

        return new SexToyMotor(motorIdx, stepCount);
    }
}
