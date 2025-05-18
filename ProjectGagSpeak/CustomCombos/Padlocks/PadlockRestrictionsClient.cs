using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly GagspeakMediator _mediator;
    private readonly RestrictionManager _manager;
    public PadlockRestrictionsClient(ILogger log, GagspeakMediator mediator, RestrictionManager manager, 
        Func<int, ActiveRestriction> activeSlotGenerator) : base(activeSlotGenerator, log)
    {
        _mediator = mediator;
        _manager = manager;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.ClientLocks;

    protected override string ItemName(ActiveRestriction item)
        => _manager.Storage.TryGetRestriction(item.Identifier, out var restriction) ? restriction.Label : "None";


    protected override bool DisableCondition()
        => MonitoredItem.CanLock() is false || MonitoredItem.Padlock == SelectedLock;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo("##ClientRestrictionLock", width, layerIdx, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo("##ClientRestrictionUnlock", width, layerIdx, string.Empty, tooltip);

    protected override void OnLockButtonPress(int layerIdx)
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
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Locked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }

    protected override void OnUnlockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanUnlock())
        {
            var newData = new ActiveRestriction()
            {
                Padlock = MonitoredItem.Padlock,
                Password = MonitoredItem.Password,
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new RestrictionDataChangedMessage(DataUpdateType.Unlocked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }
}
