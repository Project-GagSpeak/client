using GagSpeak.GameInternals.Addons;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Caches;


namespace GagSpeak.Services.Controller;

/// <summary>
///     Controls when the user is able to interact with the chatbox at all.
///     If stuck in this state, remember you have CTRL+ALT+BACKSPACE.
/// </summary>
public sealed class ChatboxController : DisposableMediatorSubscriberBase
{
    private readonly PlayerControlCache _cache;
    private bool _blockInput = false;
    public ChatboxController(ILogger<KeystateController> logger, GagspeakMediator mediator,
        PlayerControlCache cache) : base(logger, mediator)
    {
        _cache = cache;
        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    private unsafe void FrameworkUpdate()
    {
        if (!_blockInput)
            return;
        // assuming that this causes issues when ran outside framework thread.
        AddonChatLog.EnsureNoChatInputFocus();
    }

    // Update our local value to reflect the latest state in the cache.
    public void UpdateHardcoreStatus()
        => _blockInput = _cache.BlockChatInput;
}
