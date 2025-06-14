using GagSpeak.GameInternals.Addons;
using GagSpeak.Services.Mediator;
using GagSpeak.State;


namespace GagSpeak.Services.Control;

/// <summary>
///     Controls when the user is able to interact with the chatbox at all.
///     If stuck in this state, remember you have CTRL+ALT+BACKSPACE.
/// </summary>
public sealed class ChatboxController : DisposableMediatorSubscriberBase
{
    // Dictates controlling the player's KeyState blocking.
    private PlayerControlSource _sources = PlayerControlSource.None;

    public ChatboxController(ILogger<KeystateController> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    private unsafe void FrameworkUpdate()
    {
        if(_sources is not 0)
            AddonChatLog.EnsureNoChatInputFocus();
    }

    public void AddControlSources(PlayerControlSource sources)
    => _sources |= sources;

    public void RemoveControlSources(PlayerControlSource sources)
        => _sources &= ~sources;
}
