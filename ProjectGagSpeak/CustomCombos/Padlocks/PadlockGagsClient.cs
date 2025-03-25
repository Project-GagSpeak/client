using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlockable;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly GagspeakMediator _mediator;
    public PadlockGagsClient(ILogger log, GagspeakMediator mediator, Func<int, ActiveGagSlot> monitoredItemGenerator)
        : base(monitoredItemGenerator, log)
    {
        _mediator = mediator;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => GsPadlockEx.ClientLocks;

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    protected override bool DisableCondition()
        => MonitoredItem.GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo("ClientGagLock_"+ layerIdx, width, layerIdx, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo("ClientGagUnlock_" + layerIdx, width, layerIdx, string.Empty, tooltip);

    protected override void OnLockButtonPress(int layerIdx)
    {
        if (MonitoredItem.CanLock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = Timer.GetEndTimeUTC(),
                PadlockAssigner = MainHub.UID // use the same assigner. (To remove devotional timers)
            };
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Locked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }

    protected override void OnUnlockButtonPress(int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (MonitoredItem.CanUnlock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = MonitoredItem.Padlock,
                Password = MonitoredItem.Password,
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Unlocked, layerIdx, newData));
            ResetSelection();
        }
        else
        {
            ResetInputs();
        }
    }
}
