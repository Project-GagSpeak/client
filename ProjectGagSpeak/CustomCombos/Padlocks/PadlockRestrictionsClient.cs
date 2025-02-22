using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlockable;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestrictionManager _restrictions;
    private static int CurrentLayer;

    public PadlockRestrictionsClient(int layer, GagspeakMediator mediator, RestrictionManager restrictions, ILogger log, UiSharedService ui)
        : base(() => GetActiveRestriction(restrictions), log, ui, "Restrictions" + layer)
    {
        _mediator = mediator;
        _restrictions = restrictions;
        CurrentLayer = layer;
    }

    private static ActiveRestriction GetActiveRestriction(RestrictionManager restrictions)
        => restrictions.ActiveRestrictionsData?.Restrictions[CurrentLayer] ?? new ActiveRestriction();

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.ClientLocks;

    protected override string ItemName(ActiveRestriction item)
        => _restrictions.Storage.TryGetRestriction(item.Identifier, out var restriction) ? restriction.Label : "None";

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (MonitoredItem.IsLocked())
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override bool DisableCondition()
        => _restrictions.ActiveRestrictionsData is null
        || MonitoredItem.CanLock() is false
        || MonitoredItem.Padlock == SelectedLock;

    protected override void OnLockButtonPress()
    {
        if (MonitoredItem.CanLock())
        {
            var newData = new ActiveRestriction()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Locked, CurrentLayer, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }

    protected override void OnUnlockButtonPress()
    {
        if (MonitoredItem.CanUnlock())
        {
            var newData = new ActiveRestriction()
            {
                Padlock = MonitoredItem.Padlock,
                Password = MonitoredItem.Password,
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Unlocked, CurrentLayer, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }
}
