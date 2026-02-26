using GagSpeak.State.Managers;
using GagSpeak.State.Models;
using GagspeakAPI.Data;
using OtterGui.Extensions;

namespace GagSpeak.Services.Tutorial;

public class RestrictionGuideCache : GuideCache
{
    // dependencies
    private readonly RestrictionManager _manager;
    private readonly SelfBondageService _selfBondage;

    public RestrictionGuideCache(RestrictionManager manager, SelfBondageService selfBondage)
    {
        _manager = manager;
        _selfBondage = selfBondage;
    }

    public RestrictionItem? TutorialHypnoItem { get; set; }
    public RestrictionItem? TutorialBasicItem { get; set; }

    public override void OnExit()
    {
        // Cancel the editor for the items we have.
        if (_manager.ItemInEditor is { } item && (item.Identifier.Equals(TutorialHypnoItem?.Identifier) || item.Identifier.Equals(TutorialBasicItem?.Identifier)))
            _manager.StopEditing();

        // do some async cleanup.
        UiService.SetUITask(async () =>
        {
            // try to unequip the items first
            if (_manager.ServerRestrictionData is { } data)
            {
                foreach (var (res, index) in data.Restrictions.WithIndex())
                {
                    if (res.Identifier == TutorialBasicItem?.Identifier && res.CanRemove())
                        await _selfBondage.DoSelfBindResult(index, new ActiveRestriction(), DataUpdateType.Removed);

                    if (res.Identifier == TutorialHypnoItem?.Identifier && res.CanRemove())
                        await _selfBondage.DoSelfBindResult(index, new ActiveRestriction(), DataUpdateType.Removed);
                }
            }

            // delete leftover items if they exist.
            if (TutorialHypnoItem is { } cachedItem)
                _manager.Delete(cachedItem);
            if (TutorialBasicItem is { } cachedItem2)
                _manager.Delete(cachedItem2);

            TutorialHypnoItem = null;
            TutorialBasicItem = null;
        });
    }
}
