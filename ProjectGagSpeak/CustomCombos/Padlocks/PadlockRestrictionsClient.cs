using GagSpeak.Services;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly RestrictionManager _manager;
    private readonly SelfBondageService _selfBondage;

    public PadlockRestrictionsClient(ILogger log, RestrictionManager manager, SelfBondageService selfBondage)
        : base(() => [..PadlockEx.ClientLocks], log)
    {
        _manager = manager;
        _selfBondage = selfBondage;
    }

    protected override string ItemName(ActiveRestriction item)
        => _manager.Storage.TryGetRestriction(item.Identifier, out var restriction) ? restriction.Label : "None";

    protected override bool DisableCondition(int layerIdx)
        => ActiveItem.Identifier == Guid.Empty;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
    {
        ActiveItem = _manager.ServerRestrictionData?.Restrictions[layerIdx] ?? new ActiveRestriction();
        DrawLockCombo($"##ClientLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, true);
    }

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
    {
        ActiveItem = _manager.ServerRestrictionData?.Restrictions[layerIdx] ?? new ActiveRestriction();
        DrawUnlockCombo($"##ClientUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);
    }

    protected override async Task OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!ActiveItem.CanLock())
            return;

        // validate the lock, if it is not valid, we will display an error and reset inputs.
        if (!ValidateLock(layerIdx))
            return;

        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = ActiveItem with { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfBindResult(layerIdx, newData, DataUpdateType.Locked))
        {
            ActiveItem = new ActiveRestriction();
        }
        else
        {
            Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on self.", LoggerType.StickyUI);
        }

        ResetSelection();
        ResetInputs();
    }

    protected override async Task OnUnlockButtonPress(string label, int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (!ActiveItem.CanUnlock())
            return;

        if (!ValidateUnlock(layerIdx))
            return;

        var newData = ActiveItem with { Padlock = ActiveItem.Padlock, Password = ActiveItem.Password, PadlockAssigner = MainHub.UID };
        if (await _selfBondage.DoSelfBindResult(layerIdx, newData, DataUpdateType.Unlocked))
        {
            ActiveItem = new ActiveRestriction();
            SelectedLock = Padlocks.None;
        }
        else
        {
            Log.LogDebug("Failed to perform UnlockRestriction on self.");
        }
        ResetSelection();
        ResetInputs();
    }

    private bool ValidateLock(int layerIdx)
    {
        // Determine if we have access to unlock.
        bool valid = SelectedLock switch
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

    private bool ValidateUnlock(int layerIdx)
    {
        // Determine if we have access to unlock.
        bool valid = ActiveItem.Padlock switch
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
        switch (ActiveItem.Padlock)
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
