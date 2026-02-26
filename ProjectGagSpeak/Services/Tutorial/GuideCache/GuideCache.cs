namespace GagSpeak.Services.Tutorial;

/// <summary>
///     To be treated as a base class that should be overriden by parent
///     classes wishing to use these methods
/// </summary>
public class GuideCache
{
    /// <summary>
    ///     The highlight color used by the guide.
    /// </summary>
    public uint HighlightColor { get; set; } = 0xFF20FFFF;

    /// <summary>
    ///     The color used for the border of the guide.
    /// </summary>
    public uint BorderColor { get; set; } = 0xD00000FF;

    /// <summary>
    ///     The current step of the guide.
    /// </summary>
    public int CurrentStep { get; set; } = -1;

    // Usually, when you create a parent class cache, you would run a function
    // to extract data from your cache like ((PuppetCache)Cache).MyPersonalData
    // if you need to run the exit function it is a commonly shared method.
    public virtual void OnExit()
    { }
}
