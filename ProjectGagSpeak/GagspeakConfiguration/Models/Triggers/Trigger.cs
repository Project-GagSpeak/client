using GagspeakAPI.Enums;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;

namespace GagSpeak.GagspeakConfiguration.Models;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[Serializable]
public abstract record Trigger
{
    // Define which kind of trigger it is
    public abstract TriggerKind Type { get; }

    // required attributes
    public Guid TriggerIdentifier { get; set; } = Guid.NewGuid();
    public bool Enabled { get; set; } = false;
    public int Priority { get; set; } = 0;

    // generic attributes
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TimeSpan StartAfter { get; set; } = TimeSpan.Zero;
    public TimeSpan EndAfter { get; set; } = TimeSpan.Zero;

    // The actions to execute to each motor on the list of appended devices.
    // Below are dummy values that are set to default when not used.
    public TriggerActionKind TriggerActionKind { get; set; } = TriggerActionKind.SexToy;
    public List<DeviceTriggerAction> TriggerAction { get; set; } = new List<DeviceTriggerAction>();
    public ShockTriggerAction ShockTriggerAction { get; set; } = new ShockTriggerAction();
    public LightRestraintData RestraintTriggerAction { get; set; } = new LightRestraintData();
    public GagType GagTypeAction { get; set; } = GagType.None;
    public Guid MoodlesIdentifier { get; set; } = Guid.Empty; // can be a status or preset, depending on TriggerActionKind

    public LightTrigger ToLightData()
    {
        return new LightTrigger
        {
            Identifier = TriggerIdentifier,
            Name = Name,
            Type = Type,
            ActionOnTrigger = TriggerActionKind
        };
    }
    public abstract Trigger DeepClone();
}


