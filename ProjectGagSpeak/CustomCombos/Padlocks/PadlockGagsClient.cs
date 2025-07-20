using Dalamud.Game.Gui.Toast;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlock;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly GagspeakMediator _mediator;
    public PadlockGagsClient(ILogger log, GagspeakMediator mediator, GagRestrictionManager manager)
        : base(() => [ ..manager.ServerGagData?.GagSlots ?? [new ActiveGagSlot()] ], PadlockEx.ClientLocks, log)
    {
        _mediator = mediator;
    }

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientGagLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientGagUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override Task<bool> OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanLock())
            return Task.FromResult(false);

        // validate the lock, if it is not valid, we will display an error and reset inputs.
        if (!ValidateLock(layerIdx))
            return Task.FromResult(false);

        // we know it was valid, so begin assigning the new data to send off.
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
        RefreshStorage(label);
        return Task.FromResult(true);
    }

    protected override Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (!Items[layerIdx].CanUnlock())
            return Task.FromResult(false);

        if(!ValidateUnlock(layerIdx))
            return Task.FromResult(false);

        // we can send off the data, it is valid.
        var newData = new ActiveGagSlot()
        {
            Padlock = Items[layerIdx].Padlock,
            Password = Items[layerIdx].Password,
            PadlockAssigner = MainHub.UID
        };
        _mediator.Publish(new GagDataChangedMessage(DataUpdateType.Unlocked, layerIdx, newData));
        ResetSelection();
        ResetInputs();
        RefreshStorage(label);
        return Task.FromResult(true);
    }

    private bool ValidateLock(int layerIdx)
    {
        // Determine if we have access to unlock.
        bool valid = SelectedLock switch
        {
            Padlocks.MetalPadlock or Padlocks.FiveMinutesPadlock => true,
            Padlocks.CombinationPadlock => PadlockValidation.IsValidCombo(Password),
            Padlocks.PasswordPadlock => PadlockValidation.IsValidPass(Password),
            Padlocks.TimerPadlock => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)),
            Padlocks.TimerPasswordPadlock => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)) && PadlockValidation.IsValidPass(Password),
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock when !PadlockValidation.IsValidPass(Password):
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case Padlocks.TimerPadlock:
            case Padlocks.TimerPasswordPadlock when !PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)):
                Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 0h2m7s).");
                break;

            default:
                Svc.Toasts.ShowError("Can't lock this Padlock.");
                break;
        }

        ResetInputs();
        return false;
    }

    private bool ValidateUnlock(int layerIdx)
    {
        // Determine if we have access to unlock.
        bool valid = Items[layerIdx].Padlock switch
        {
            Padlocks.MetalPadlock or Padlocks.FiveMinutesPadlock => true,
            Padlocks.CombinationPadlock => Items[layerIdx].Password == Password,
            Padlocks.PasswordPadlock => Items[layerIdx].Password == Password,
            Padlocks.TimerPadlock => Items[layerIdx].PadlockAssigner == MainHub.UID,
            Padlocks.TimerPasswordPadlock => Items[layerIdx].Password == Password,
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.CombinationPadlock:
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Password does not match!");
                break;

            case Padlocks.TimerPadlock:
                Svc.Toasts.ShowError("Can only be removed early by Assigner!");
                break;

            default:
                Svc.Toasts.ShowError("Can't unlock this padlock!");
                break;
        }

        ResetInputs();
        return false;
    }
}
