using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
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
    private bool _hideChatBoxes = false;
    private bool _hideChatInput = false;

    public ChatboxController(ILogger<KeystateController> logger, GagspeakMediator mediator,
        PlayerControlCache cache) : base(logger, mediator)
    {
        _cache = cache;

        Mediator.Subscribe<HcStateCacheChanged>(this, _ => UpdateHardcoreStatus());
        //Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostShow, "ChatLog", ChatLogPostShow);
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostFocusChanged, "ChatLog", ChatLogFocusChanged);
    }

    protected override void Dispose(bool disposing) {
        if (!disposing) return;
        Svc.AddonLifecycle.UnregisterListener(ChatLogPostShow);
        Svc.AddonLifecycle.UnregisterListener(ChatLogFocusChanged);
        base.Dispose(disposing);
    }

    private void ChatLogPostShow(AddonEvent type, AddonArgs args)
    {
        if (_hideChatInput)
            AddonChatLog.SetChatInputVisibility(false);

        if(_hideChatBoxes)
            AddonChatLog.SetChatPanelVisibility(false);
    }

    private void ChatLogFocusChanged(AddonEvent type, AddonArgs args)
    {
        if (_blockInput) 
            AddonChatLog.EnsureNoChatInputFocus();
    }

    /* this shouldn't be necessary any more, let's test.
    private unsafe void FrameworkUpdate()
    {
        // assuming that this causes issues when ran outside framework 
        // todo: move this to addon.lifecycle events when we figure out how
        if (_blockInput)
            AddonChatLog.EnsureNoChatInputFocus();
        if (_hideChatBoxes)
            AddonChatLog.SetChatPanelVisibility(false);
        if (_hideChatInput)
            AddonChatLog.SetChatInputVisibility(false);
    } */

    // Update our local value to reflect the latest state in the cache.
    public void UpdateHardcoreStatus()
    {
        _blockInput = _cache.BlockChatInput;
        _hideChatBoxes = _cache.HideChatBoxes;
        _hideChatInput = _cache.HideChatInput;
    }
}
