using GagSpeak.State.Managers;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Tutorial;

/// <summary>
///     To be treated as a base class that should be overriden by parent 
///     classes wishing to use these methods
/// </summary>
public class PuppeteerGuideCache : GuideCache
{
    private readonly PuppeteerManager _manager;
    public PuppeteerGuideCache(PuppeteerManager manager)
    {
        _manager = manager;
    }

    public AliasTrigger? TutorialAlias { get; set; } = null;

    // Ensure on exit that our tutorial alias has been cleared out.
    public override void OnExit()
    {
        if (_manager.ItemInEditor is { } item && item.Identifier.Equals(TutorialAlias?.Identifier))
            _manager.StopEditing();
        // If the manager contains the item, remove it from the manager.
        if (_manager.Storage.Items.FirstOrDefault(x => x == TutorialAlias) is { } guideAlias)
            _manager.Delete(guideAlias);

        // Clear the cached Alias
        TutorialAlias = null;
    }
}
