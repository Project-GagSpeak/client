using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly RestraintManager _manager;
    private readonly SelfBondageService _selfBondage;

    public PadlockRestraintsClient(ILogger log, RestraintManager manager, SelfBondageService selfBondage)
        : base(() => [..PadlockEx.ClientLocks], log)
    {
        _manager = manager;
        _selfBondage = selfBondage;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => ActiveItem.Identifier == Guid.Empty;

    public void DrawLockCombo(float width, string tooltip)
    {
        ActiveItem = _manager.ServerData ?? new CharaActiveRestraint();
        DrawLockCombo("##ClientRestraintLock", width, 0, string.Empty, tooltip, true);
    }

    public void DrawUnlockCombo(float width, string tooltip)
    {
        ActiveItem = _manager.ServerData ?? new CharaActiveRestraint();
        DrawUnlockCombo("##ClientRestraintUnlock", width, 0, string.Empty, tooltip);
    }

    protected override async Task OnLockButtonPress(string label, int _)
    {
        if (!ActiveItem.CanLock())
            return;

        if (!ValidateLock())
            return;

        // get new data.
        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = new CharaActiveRestraint() { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfRestraintResult(newData, DataUpdateType.Locked))
        {
            ActiveItem = new CharaActiveRestraint();
        }
        else
        {
            Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on self.", LoggerType.StickyUI);
        }
        ResetSelection();
        ResetInputs();
    }

    protected override async Task OnUnlockButtonPress(string label, int _)
    {
        if (!ActiveItem.CanUnlock())
            return;

        if (!ValidateUnlock())
            return;

        var newData = ActiveItem with { Padlock = ActiveItem.Padlock, Password = ActiveItem.Password, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfRestraintResult(newData, DataUpdateType.Unlocked))
        {
            ActiveItem = new CharaActiveRestraint();
            SelectedLock = Padlocks.None;
        }
        else
        {
            Log.LogDebug("Failed to perform UnlockRestraint on self.");
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
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case Padlocks.Password:
            case Padlocks.TimerPassword when !PadlockValidation.IsValidPass(Password):
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case Padlocks.Timer:
            case Padlocks.PredicamentTimer:
            case Padlocks.TimerPassword when !PadlockValidation.IsValidTime(Timer, TimeSpan.FromDays(999)):
                Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 0h2m7s).");
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
