using GagSpeak.Localization;
using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;

namespace GagSpeak.Services.Tutorial;

/// <summary>
///     To be treated as a base class that should be overriden by parent 
///     classes wishing to use these methods
/// </summary>
public class RestraintGuideCache : GuideCache
{
    private readonly RestraintManager _manager;
    private readonly SelfBondageService _selfBondage;
    public RestraintGuideCache(RestraintManager manager, SelfBondageService selfBondage)
    {
        _manager = manager;
        _selfBondage = selfBondage;
    }

    public RestraintSet? TutorialSet { get; set; } = null;

    // Ensure on exit that our tutorial alias has been cleared out.
    public override void OnExit()
    {
        if (_manager.ItemInEditor is { } item && item.Identifier.Equals(TutorialSet?.Identifier))
            _manager.StopEditing();
        // Init the async portion of this.
        UiService.SetUITask(async () =>
        {
            if (_manager.ServerData is { } set && set.CanRemove() && set.Identifier.Equals(TutorialSet?.Identifier))
                await _selfBondage.DoSelfRestraintResult(new(), DataUpdateType.Removed);

            // After removed, delete from manager.
            if (TutorialSet is { } cachedSet)
                _manager.Delete(cachedSet);

            // Clear the tutorial set reference.
            TutorialSet = null;
        });
    }
}
