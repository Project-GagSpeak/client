using MessagePack;

namespace GagspeakAPI.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record LightAlarm
{
    /// <summary> 
    /// The Identifier of the alarm set. 
    /// </summary>
    public Guid Identifier = Guid.Empty;

    /// <summary> 
    /// The name given to the alarm. 
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// The time the alarm will go off. (In UTC Format) 
    /// </summary>
    public DateTimeOffset SetTimeUTC { get; set; } = DateTimeOffset.MinValue;

    /// <summary> 
    /// References the name that this pattern plays. 
    /// </summary>
    public string PatternThatPlays { get; set; } = string.Empty;
}