using GagSpeak.Services.Mediator;
using System.Numerics;

namespace GagSpeak.Toybox.Services;
// contains common things all remotes share and do not need unique instances to contain.
// also should be repurposed when possible to.
public class ToyboxRemoteService
{
    private readonly ILogger<ToyboxRemoteService> _logger;
    private readonly GagspeakMediator _mediator;

    public ToyboxRemoteService(ILogger<ToyboxRemoteService> logger,
        GagspeakMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public Vector4 VibrantPink = new Vector4(.977f, .380f, .640f, .914f);
    public Vector4 VibrantPinkHovered = new Vector4(.986f, .464f, .691f, .955f);
    public Vector4 VibrantPinkPressed = new Vector4(.846f, .276f, .523f, .769f);
    public Vector4 LushPinkLine = new Vector4(.806f, .102f, .407f, 1);
    public Vector4 LushPinkButton = new Vector4(1, .051f, .462f, 1);
    public Vector4 LovenseScrollingBG = new Vector4(0.042f, 0.042f, 0.042f, 0.930f);
    public Vector4 LovenseDragButtonBG = new Vector4(0.110f, 0.110f, 0.110f, 0.930f);
    public Vector4 LovenseDragButtonBGAlt = new Vector4(0.1f, 0.1f, 0.1f, 0.930f);
    public Vector4 ButtonDrag = new Vector4(0.097f, 0.097f, 0.097f, 0.930f);
    public Vector4 SideButton = new Vector4(0.451f, 0.451f, 0.451f, 1);
    public Vector4 SideButtonBG = new Vector4(0.451f, 0.451f, 0.451f, .25f);

    // stuff
}


