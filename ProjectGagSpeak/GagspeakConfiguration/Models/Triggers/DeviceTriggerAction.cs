using GagSpeak.ChatMessages;
using GagspeakAPI.Enums;
using GagspeakAPI.Data;

namespace GagSpeak.GagspeakConfiguration.Models;

[Serializable]
public record DeviceTriggerAction
{
    // the recommended device name.
    public string DeviceName { get; init; } = "Wildcard Device";
    public bool Vibrate { get; set; } = false;
    public bool Rotate { get; set; } = false;
    public int VibrateMotorCount { get; init; }
    public int RotateMotorCount { get; init; }
    public List<MotorAction> VibrateActions { get; set; } = new List<MotorAction>();
    public List<MotorAction> RotateActions { get; set; } = new List<MotorAction>();
    // Can add linear and oscillation actions here later if anyone actually needs them. But I doubt it.
    public DeviceTriggerAction(string Name, int vibeCount, int MotorCount)
    {
        DeviceName = Name;
        VibrateMotorCount = vibeCount;
        RotateMotorCount = MotorCount;
    }
}

[Serializable]
public record MotorAction
{
    public MotorAction(uint motorIndex)
    {
        MotorIndex = motorIndex;
    }

    public uint MotorIndex { get; init; } = 0;

    // the type of action being executed
    public TriggerActionType ExecuteType { get; set; } = TriggerActionType.Vibration;

    // ONLY USED WHEN TYPE IS VIBRATION
    public byte Intensity { get; set; } = 0;
    
    // ONLY USED WHEN TYPE IS PATTERN
    public Guid PatternIdentifier { get; set; } = Guid.Empty;
    // (if we want to start at a certain point in the pattern.)
    public TimeSpan StartPoint { get; set; } = TimeSpan.Zero;
}

