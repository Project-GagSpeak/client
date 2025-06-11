using GagspeakAPI.Data;

namespace GagSpeak.PlayerState.Models;
public class OverlayHypnotic : OverlayEffect
{
    public HypnoticEffect Effect { get; set; } = new();

    public OverlayHypnotic()
    { }

    public OverlayHypnotic(OverlayHypnotic other) 
        : base(other)
    {
        Effect = other.Effect;
    }

    public override OverlayEffect Clone()
        => new OverlayHypnotic(this);
}
