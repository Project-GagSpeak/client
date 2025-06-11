using GagSpeak.GameInternals.Addons;
using GagSpeak.PlayerClient;
using GagSpeak.Services.Mediator;
using GagSpeak.Utils;

namespace GagSpeak.PlayerState.Visual;

public sealed class HardcoreHandler : DisposableMediatorSubscriberBase
{
    public HardcoreHandler(ILogger<HardcoreHandler> logger, GagspeakMediator mediator)
        : base(logger, mediator)
    {
        Mediator.Subscribe<FrameworkUpdateMessage>(this, _ => FrameworkUpdate());
    }

    private unsafe void FrameworkUpdate()
    {
        // Detect Emergency Safeword
        if (KeyMonitor.CtrlPressed() && KeyMonitor.AltPressed() && KeyMonitor.BackPressed())
            Mediator.Publish(new SafewordHardcoreUsedMessage());

        // Block ChatInput if the hardcore permission is active.
        if (GlobalPermissions.ChatInputBlocked)
            AddonChatLog.EnsureNoChatInputFocus();
    }
}
