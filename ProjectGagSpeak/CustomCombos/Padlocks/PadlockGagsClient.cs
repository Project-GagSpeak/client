using GagSpeak.Services;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;
using OtterGui.Classes;

namespace GagSpeak.CustomCombos.Padlock;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly SelfBondageService _selfBondage;
    public PadlockGagsClient(ILogger log, GagRestrictionManager manager, SelfBondageService selfBondage)
        : base(new LazyList<ActiveGagSlot>(() => [ ..manager.ServerGagData?.GagSlots ?? [new ActiveGagSlot()] ]), PadlockEx.ClientLocks, log)
    {
        _selfBondage = selfBondage;
    }

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientGagLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, false);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientGagUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override async Task<bool> OnLockButtonPress(string label, int layerIdx)
    {
        if (!Items[layerIdx].CanLock())
            return false;

        if (!ValidateLock(layerIdx))
            return false;

        // we know it was valid, so begin assigning the new data to send off.
        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = Items[layerIdx] with { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfGagResult(layerIdx, newData, DataUpdateType.Locked))
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            return false;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        if (!Items[layerIdx].CanUnlock())
            return false;

        if(!ValidateUnlock(layerIdx))
            return false;

        var newData = Items[layerIdx] with { Padlock = Items[layerIdx].Padlock, Password = Items[layerIdx].Password, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfGagResult(layerIdx, newData, DataUpdateType.Unlocked))
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            SelectedLock = Padlocks.None;
            return true;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            return false;
        }
    }

    private bool ValidateLock(int layerIdx)
    {
        // Determine if we have access to unlock.
        var valid = SelectedLock switch
        {
            Padlocks.Metal or Padlocks.FiveMinutes => true,
            Padlocks.Combination => PadlockValidation.IsValidCombo(Password),
            Padlocks.Password => PadlockValidation.IsValidPass(Password),
            Padlocks.Timer => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)),
            Padlocks.PredicamentTimer => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)),
            Padlocks.TimerPassword => PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)) && PadlockValidation.IsValidPass(Password),
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.Combination:
                Svc.Toasts.ShowError("Invalid Combination. Must be 4 digits (0-9).");
                break;

            case Padlocks.Password:
            case Padlocks.TimerPassword when !PadlockValidation.IsValidPass(Password):
                Svc.Toasts.ShowError("Invalid Password Format. Must be 4-20 characters.");
                break;

            case Padlocks.Timer:
            case Padlocks.PredicamentTimer:
            case Padlocks.TimerPassword when !PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)):
                Svc.Toasts.ShowError("Invalid Timer Format. Must be a valid time format (Ex: 0h2m7s).");
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
        var valid = Items[layerIdx].Padlock switch
        {
            Padlocks.Metal or Padlocks.FiveMinutes or Padlocks.Timer => true,
            Padlocks.Combination => Items[layerIdx].Password == Password,
            Padlocks.Password => Items[layerIdx].Password == Password,
            Padlocks.TimerPassword => Items[layerIdx].Password == Password,
            _ => false
        };

        if (valid)
            return true;

        // If we don't, display the appropriate error and reset inputs.
        switch (SelectedLock)
        {
            case Padlocks.Combination:
            case Padlocks.Password:
            case Padlocks.TimerPassword:
                Svc.Toasts.ShowError("Password does not match!");
                break;

            case Padlocks.PredicamentTimer:
                Svc.Toasts.ShowError("Cannot be removed by yourself!");
                break;

            default:
                Svc.Toasts.ShowError("Can't unlock this padlock!");
                break;
        }

        ResetInputs();
        return false;
    }
}
