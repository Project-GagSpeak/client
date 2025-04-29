using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestraintManager _manager;
    public PadlockRestraintsClient(ILogger log, GagspeakMediator mediator, RestraintManager manager, 
        Func<int, CharaActiveRestraint> activeSlotGenerator) : base(activeSlotGenerator, log)
    {
        _mediator = mediator;
        _manager = manager;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.ClientLocks;

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition()
        => !MonitoredItem.CanLock() || MonitoredItem.Padlock == SelectedLock;

    public void DrawLockCombo(float width, string tooltip)
    => DrawLockCombo("ClientRestraintLock", width, 0, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, string tooltip)
        => DrawUnlockCombo("ClientRestraintUnlock", width, 0, string.Empty, tooltip);

    protected override void OnLockButtonPress(int _)
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
    protected override void OnUnlockButtonPress(int _)
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
