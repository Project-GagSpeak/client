using GagSpeak.PlayerState.Visual;
using GagSpeak.Services.Mediator;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlockable;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly GagspeakMediator _mediator;
    public PadlockGagsClient(ILogger log, GagspeakMediator mediator, GagRestrictionManager manager)
        : base(() => [ ..manager.ServerGagData?.GagSlots ?? [new ActiveGagSlot()] ], log)
    {
        _mediator = mediator;
    }

    protected override IEnumerable<Padlocks> ExtractPadlocks()
        => PadlockEx.ClientLocks;

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    protected override bool DisableCondition(int _)
        => Items[0].GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientGagLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientGagUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(int layerIdx)
    {
        if (Items[0].CanLock())
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
            ResetInputs();
            return Task.FromResult(true);
        }

        // if we are not able to lock, we need to reset the inputs.
        ResetInputs();
        return Task.FromResult(false);
    }

    protected override Task<bool> OnUnlockButtonPress(int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (Items[0].CanUnlock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = Items[0].Padlock,
                Password = Items[0].Password,
                PadlockAssigner = MainHub.UID
            };
            _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Unlocked, layerIdx, newData));
            ResetSelection();
            ResetInputs();
            return Task.FromResult(true);
        }

        // if we are not able to unlock, we need to reset the inputs.
        ResetInputs();
        return Task.FromResult(false);
    }
}
