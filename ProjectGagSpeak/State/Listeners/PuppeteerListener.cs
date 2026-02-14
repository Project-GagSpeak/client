using GagSpeak.Kinksters;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;

namespace GagSpeak.State.Listeners;

/// <summary> Listeners for components that are not in the toybox compartment nor are visual components. </summary>
/// <remarks> May be catagorized later, but are filtered into here for now. </remarks>
public sealed class PuppeteerListener
{
    private readonly GagspeakMediator _mediator;
    private readonly KinksterManager _kinksters;
    private readonly PuppeteerManager _aliasManager;
    public PuppeteerListener(
        GagspeakMediator mediator,
        KinksterManager pairs,
        PuppeteerManager aliasManager)
    {
        _mediator = mediator;
        _kinksters = pairs;
        _aliasManager = aliasManager;
    }

    private void PostActionMsg(string enactor, InteractionType type, string message)
    {
        if (_kinksters.TryGetNickAliasOrUid(enactor, out var nick))
            _mediator.Publish(new EventMessage(new(nick, enactor, type, message)));
    }

    /// <summary>
    ///     OUR CLIENT recieved a name from ANOTHER KINKSTER. <para />
    ///     This means we should be updating the PUPPETEERS since 
    ///     we are now listening to THEM.
    /// </summary>
    public void UpdateListener(string pairUid, string listenerName)
    {
        // Update the Puppeteers
        if (_aliasManager.Puppeteers.TryGetValue(pairUid, out var puppeteer))
            puppeteer.NameWithWorld = listenerName;
        else
            _aliasManager.Puppeteers.Add(pairUid, new PuppeteerPlayer() { NameWithWorld = listenerName });
        // Ensure the puppeteer changes save after this.
        _aliasManager.Save();
        _mediator.Publish(new FolderUpdatePuppeteers());

        PostActionMsg(pairUid, InteractionType.ListenerName, $"Obtained Listener name from Kinkster");
    }
}
