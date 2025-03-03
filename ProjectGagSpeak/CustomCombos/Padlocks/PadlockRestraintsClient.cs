using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.UI;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;
using ImGuiNET;

namespace GagSpeak.CustomCombos.Padlockable;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestraintManager _restraints;
    public PadlockRestraintsClient(GagspeakMediator mediator, RestraintManager restraints, ILogger log, string label)
        : base(() => restraints.ActiveRestraintData ?? new CharaActiveRestraint(), log, label)
    {
        _mediator = mediator;
        _restraints = restraints;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.ClientLocks;

    protected override string ItemName(CharaActiveRestraint item)
        => _restraints.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    public void DrawPadlockComboSection(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // grab the latest padlock. If it is not none, we should draw the unlock base, otherwise, draw the lock base.
        if (MonitoredItem.IsLocked())
            DrawUnlockCombo(width, tt, btt, flags);
        else
            DrawLockCombo(width, tt, btt, flags);
    }

    protected override bool DisableCondition()
        => _restraints.ActiveRestraintData is null
        || MonitoredItem.CanLock() is false
        || MonitoredItem.Padlock == SelectedLock;

    protected override void OnLockButtonPress()
    {
        if (MonitoredItem.CanLock())
        {
            var newData = new CharaActiveRestraint()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Locked, newData));
            ResetSelection();
            return;
        }

        ResetInputs();
    }
    protected override void OnUnlockButtonPress()
    {
        if (MonitoredItem.CanUnlock())
        {
            var newData = new CharaActiveRestraint()
            {
                Padlock = MonitoredItem.Padlock,
                Password = MonitoredItem.Password,
                PadlockAssigner = MainHub.UID
            };

            _mediator.Publish(new RestraintDataChangedMessage(DataUpdateType.Unlocked, newData));
            ResetSelection();
            return;
        }

        ResetInputs();
    }
}
