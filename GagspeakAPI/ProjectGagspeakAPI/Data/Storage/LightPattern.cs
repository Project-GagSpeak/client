using MessagePack;

namespace GagspeakAPI.Data;

[MessagePackObject(keyAsPropertyName: true)]
public record LightPattern
{
    /// <summary> 
    /// The Identifier of the Pattern set. 
    /// </summary>
    public Guid Identifier = Guid.Empty;

    /// <summary> 
    /// The name given to the restraint set. 
    /// </summary>
    public string Name = string.Empty;

    /// <summary> 
    /// The duration of the pattern 
    /// </summary>
    public TimeSpan Duration = TimeSpan.Zero;

    /// <summary> 
    /// If the pattern should loop 
    /// </summary>
    public bool ShouldLoop = false;
}