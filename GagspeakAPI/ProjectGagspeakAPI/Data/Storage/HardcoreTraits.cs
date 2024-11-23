using MessagePack;

namespace GagspeakAPI.Data;

/// <summary>
/// A basic authentication class to validate that the information from the client when they attempt to connect is correct.
/// </summary>
[MessagePackObject(keyAsPropertyName: true)]
public record HardcoreTraits
{
    /// <summary> Any action which typically involves fast leg movement is restricted </summary>
    public bool LegsRestrained { get; set; } = false;

    /// <summary> Any action which typically involves fast arm movement is restricted </summary>
    public bool ArmsRestrained { get; set; } = false;

    /// <summary> Any action requiring speech is restricted </summary>
    public bool Gagged { get; set; } = false;

    /// <summary> Any actions requiring awareness or sight is restricted </summary>
    public bool Blindfolded { get; set; } = false;

    /// <summary> Player becomes unable to move in this set </summary>
    public bool Immobile { get; set; } = false;

    /// <summary> Player is forced to only walk while wearing this restraint </summary>
    public bool Weighty { get; set; } = false;

    /// <summary> The level of stimulation the Restraint Set provides </summary>
    public StimulationLevel StimulationLevel { get; set; } = StimulationLevel.None;

    // Helper function to say if any of our properties are enabled.
    public bool AnyEnabled() => LegsRestrained || ArmsRestrained || Gagged || Blindfolded || Immobile || Weighty || StimulationLevel != StimulationLevel.None;
}

public enum StimulationLevel 
{ 
    None, 
    Light, 
    Mild, 
    Heavy 
}
