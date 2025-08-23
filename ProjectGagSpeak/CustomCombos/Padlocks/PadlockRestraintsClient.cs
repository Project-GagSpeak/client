using GagSpeak.Services;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestraintsClient : CkPadlockComboBase<CharaActiveRestraint>
{
    private readonly DistributorService _dds;
    private readonly VisualStateListener _visuals;
    private readonly RestraintManager _manager;
    public PadlockRestraintsClient(ILogger log, DistributorService dds, VisualStateListener visuals, RestraintManager manager)
        : base(() => [ manager.ServerData ?? new CharaActiveRestraint() ], PadlockEx.ClientLocks, log)
    {
        _dds = dds;
        _visuals = visuals;
        _manager = manager;
    }

    protected override string ItemName(CharaActiveRestraint item)
        => _manager.Storage.TryGetRestraint(item.Identifier, out var restraint) ? restraint.Label : "None";

    protected override bool DisableCondition(int _)
        => Items[0].Identifier == Guid.Empty;

    public void DrawLockCombo(float width, string tooltip)
        => DrawLockCombo("##ClientRestraintLock", width, 0, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, string tooltip)
        => DrawUnlockCombo("##ClientRestraintUnlock", width, 0, string.Empty, tooltip);

    protected override async Task<bool> OnLockButtonPress(string label, int _)
    {
        if (!Items[0].CanLock())
            return false;

        if (!ValidateLock())
            return false;

        // get new data.
        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = new CharaActiveRestraint() { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await SelfBondageHelper.RestraintUpdateRetTask(newData, DataUpdateType.Locked, _dds, _visuals))
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
        Log.LogDebug($"Failed to perform LockRestraint with {SelectedLock.ToName()} on self", LoggerType.StickyUI);
        ResetSelection();
        ResetInputs();
        return false;
    }
    protected override async Task<bool> OnUnlockButtonPress(string label, int _)
    {
        if (!Items[0].CanUnlock())
            return false;

        if (!ValidateUnlock())
            return false;

        var newData = Items[0] with { Padlock = Items[0].Padlock, Password = Items[0].Password, PadlockAssigner = MainHub.UID };
        if (await SelfBondageHelper.RestraintUpdateRetTask(newData, DataUpdateType.Unlocked, _dds, _visuals))
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
        var valid = Items[0].Padlock switch
        {
            Padlocks.Metal or Padlocks.FiveMinutes or Padlocks.Timer => true,
            Padlocks.Combination => Items[0].Password == Password,
            Padlocks.Password => Items[0].Password == Password,
            Padlocks.TimerPassword => Items[0].Password == Password,
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
