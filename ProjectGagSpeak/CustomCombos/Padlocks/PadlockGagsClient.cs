using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlock;

// These are displayed seperately so dont use a layer updater.
public class PadlockGagsClient : CkPadlockComboBase<ActiveGagSlot>
{
    private readonly SelfBondageService _selfBondage;
    private readonly GagRestrictionManager _manager;

    public PadlockGagsClient(ILogger log, GagRestrictionManager manager, SelfBondageService selfBondage)
        : base(() => [..PadlockEx.ClientLocks], log)
    {
        _selfBondage = selfBondage;
        _manager = manager;
    }

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();

    protected override bool DisableCondition(int layerIdx)
        => ActiveItem.GagItem is GagType.None;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
    {
        ActiveItem = _manager.ServerGagData?.GagSlots[layerIdx] ?? new ActiveGagSlot();
        DrawLockCombo($"##ClientGagLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, false);
    }

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
    {
        ActiveItem = _manager.ServerGagData?.GagSlots[layerIdx] ?? new ActiveGagSlot();
        DrawUnlockCombo($"##ClientGagUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);
    }

    protected override async Task OnLockButtonPress(string label, int layerIdx)
    {
        if (!ActiveItem.CanLock())
            return;

        if (!ValidateLock())
            return;

        // we know it was valid, so begin assigning the new data to send off.
        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = ActiveItem with { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfGagResult(layerIdx, newData, DataUpdateType.Locked))
        {
            ActiveItem = new ActiveGagSlot();
        }
        else
        {
            Log.LogDebug($"Failed to perform LockGag with {SelectedLock.ToName()} on self.", LoggerType.StickyUI);
        }
        ResetSelection();
        ResetInputs();
    }

    protected override async Task OnUnlockButtonPress(string label, int layerIdx)
    {
        if (!ActiveItem.CanUnlock())
            return;

        if (!ValidateUnlock())
            return;

        //the server never uses this data, so why are we setting it?
        //var newData = ActiveItem with { Padlock = ActiveItem.Padlock, Password = ActiveItem.Password, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfGagResult(layerIdx, ActiveItem, DataUpdateType.Unlocked))
        {
            ActiveItem = new ActiveGagSlot();
            SelectedLock = Padlocks.None;
        }
        else
        {
            Log.LogDebug("Failed to perform UnlockGag on self.");
        }
        ResetSelection();
        ResetInputs();
    }

    private bool ValidateLock()
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

    private bool ValidateUnlock()
    {
        // Determine if we have access to unlock.
        var valid = ActiveItem.Padlock switch
        {
            Padlocks.Metal or Padlocks.FiveMinutes or Padlocks.Timer => true,
            Padlocks.Combination => ActiveItem.Password == Password,
            Padlocks.Password => ActiveItem.Password == Password,
            Padlocks.TimerPassword => ActiveItem.Password == Password,
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
