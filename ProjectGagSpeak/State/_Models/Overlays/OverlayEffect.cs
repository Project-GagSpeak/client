namespace GagSpeak.State.Models;
public class OverlayEffect
{
    public bool ForceFirstPerson { get; set; } = false;
    public string OverlayPath { get; set; } = string.Empty;

    public OverlayEffect(string path = "")
    {
        OverlayPath = path;
    }

    public OverlayEffect(OverlayEffect other)
    {
        ForceFirstPerson = other.ForceFirstPerson;
        OverlayPath = other.OverlayPath;
    }

    public virtual OverlayEffect Clone()
        => new OverlayEffect(this);
}
