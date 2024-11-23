using GagspeakAPI.Enums;
using MessagePack;

namespace GagspeakAPI.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record LightTrigger
{
    /// <summary> 
    /// The Identifier of the trigger.
    /// </summary>
    public Guid Identifier = Guid.Empty;

    /// <summary> 
    /// The name of the trigger. 
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary> 
    /// The type of trigger it is. 
    /// </summary>
    public TriggerKind Type { get; set; } = TriggerKind.Chat;

    /// <summary> 
    /// The Action done when trigger is fired. 
    /// </summary>
    public TriggerActionKind ActionOnTrigger { get; set; } = TriggerActionKind.SexToy;
}