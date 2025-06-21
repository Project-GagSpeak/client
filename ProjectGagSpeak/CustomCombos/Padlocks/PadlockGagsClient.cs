using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlock;

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

    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientGagLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientGagUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(int layerIdx)
    {
        if (Items[layerIdx].CanLock())
        {
            var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock 
                ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

            var newData = new ActiveGagSlot()
            {
                Padlock = SelectedLock,
                Password = Password,
                Timer = finalTime,
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
        if (Items[layerIdx].CanUnlock())
        {
            var newData = new ActiveGagSlot()
            {
                Padlock = Items[layerIdx].Padlock,
                Password = Items[layerIdx].Password,
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
