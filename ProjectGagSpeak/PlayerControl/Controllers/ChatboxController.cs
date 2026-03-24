using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
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
    private bool _blockingInput = false;
    private bool _hideChatBoxes = false;
    private bool _hideChatInput = false;

    public ChatboxController(ILogger<ChatboxController> logger, GagspeakMediator mediator,
        PlayerControlCache cache) : base(logger, mediator)
    {
        _cache = cache;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostShow, "ChatLog", ChatLogPostShow);
    }

    protected override void Dispose(bool disposing) {
        if (!disposing) return;
        Svc.AddonLifecycle.UnregisterListener(ChatLogPostShow);
        base.Dispose(disposing);
    }

    private void ChatLogPostShow(AddonEvent type, AddonArgs args)
    {
        Logger.LogTrace("ChatLog Visibility changed. Checking if helplessness needs reinforcing.", LoggerType.HardcoreActions);
        if (_hideChatInput)
            Svc.Framework.RunOnTick(() => AddonChatLog.SetChatInputVisibility(!_hideChatInput), delayTicks:1);

        if (_hideChatBoxes)
            Svc.Framework.RunOnTick(() => AddonChatLog.SetChatPanelVisibility(!_hideChatBoxes), delayTicks:1);
    }

    private void FrameworkUpdate()
    {
        if (_blockingInput == _blockInput) return;
        AddonChatLog.DisableInput(_blockInput);
        _blockingInput = _blockInput;
    }

    // Update our local value to reflect the latest state in the cache.
    public void UpdateHardcoreStatus()
    {
        _blockInput = _cache.BlockChatInput;
        _hideChatBoxes = _cache.HideChatBoxes;
        _hideChatInput = _cache.HideChatInput;
    }
}
