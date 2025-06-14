namespace GagSpeak.State.Models;
public class BlindfoldOverlay : OverlayEffect
{
    public BlindfoldOverlay()
    { }

    public BlindfoldOverlay(BlindfoldOverlay other) 
        : base(other)
    { }

    public override OverlayEffect Clone()
        => new BlindfoldOverlay(this);
}
