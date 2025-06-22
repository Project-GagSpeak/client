namespace GagSpeak.State.Models;
public interface IOverlayEffect
{
    /// <summary>
    ///    If 1st Person Perspective should be forced during this effects duration.
    /// </summary>
    bool ForceFirstPerson { get; set; }

    /// <summary>
    ///     The FilePath location used to identify the Overlay Image.
    /// </summary>
    public string OverlayPath { get; set; }

    /// <summary>
    ///     Checks if the overlay effect is valid.
    /// </summary>
    public bool IsValid();
}
