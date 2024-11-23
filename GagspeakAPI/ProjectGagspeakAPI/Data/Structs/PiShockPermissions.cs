using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.IPC;

namespace GagspeakAPI.Data.Struct;
public readonly struct PiShockPermissions
{
    public bool AllowShocks { get; init; } = false;
    public bool AllowVibrations { get; init; } = false;
    public bool AllowBeeps { get; init; } = false;
    public int MaxIntensity { get; init; } = -1;
    public int MaxDuration { get; init; } = -1;

    public PiShockPermissions(bool allowShocks, bool allowVibrations, bool allowBeeps, int maxIntensity, int maxDuration)
    {
        AllowShocks = allowShocks;
        AllowVibrations = allowVibrations;
        AllowBeeps = allowBeeps;
        MaxIntensity = maxIntensity;
        MaxDuration = maxDuration;
    }

    public TimeSpan GetTimespanFromDuration()
    {
        if (MaxDuration > 15 && MaxDuration < 100)
        {
            return TimeSpan.Zero; // invalid range.
        }
        else if (MaxDuration >= 100 && MaxDuration <= 15000)
        {
            return TimeSpan.FromMilliseconds(MaxDuration); // convert to milliseconds
        }
        else
        {
            return TimeSpan.FromSeconds(MaxDuration); // convert to seconds
        }
    }
}
