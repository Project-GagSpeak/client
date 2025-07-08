namespace GagSpeak.State.Models;

public class SexToyMotor
{
    public uint StepCount { get; }
    public double Interval { get; }
    // I would have used bytes here but ButtplugClient uses
    // doubles so this erases casting.
    public double Intensity { get; set; }

    public SexToyMotor(uint stepCount)
    {
        StepCount = stepCount > 0 ? stepCount : 1;
        Interval = 1.0 / StepCount;
        Intensity = 0.0;
    }

    public SexToyMotor(SexToyMotor other)
        : this(other.StepCount)
    {
        Intensity = other.Intensity;
    }

    public void Stop() => Intensity = 0f;
}
