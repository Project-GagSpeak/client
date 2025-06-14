using GagspeakAPI.Data;

namespace GagSpeak.State.Models;
public class HypnoticOverlay : OverlayEffect
{
    public HypnoticEffect Effect { get; set; } = new();

    public HypnoticOverlay()
    { }

    public HypnoticOverlay(HypnoticOverlay other) 
        : base(other)
    {
        Effect = other.Effect;
    }

    public override OverlayEffect Clone()
        => new HypnoticOverlay(this);
}
