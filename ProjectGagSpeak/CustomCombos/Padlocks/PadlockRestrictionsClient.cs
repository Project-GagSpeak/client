using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Listeners;
using GagSpeak.State.Managers;
using GagSpeak.Utils;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly DataDistributor _dds;
    private readonly VisualStateListener _visuals;
    private readonly RestrictionManager _manager;
    public PadlockRestrictionsClient(ILogger log, DataDistributor dds, VisualStateListener visuals, RestrictionManager manager)
        : base(() => manager.ServerRestrictionData?.Restrictions ?? [], PadlockEx.ClientLocks, log)
    {
        _dds = dds;
        _visuals = visuals;
        _manager = manager;
    }

    protected override string ItemName(ActiveRestriction item)
        => _manager.Storage.TryGetRestriction(item.Identifier, out var restriction) ? restriction.Label : "None";

    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].Identifier == Guid.Empty;

    public void DrawLockCombo(float width, int layerIdx, string tooltip)
        => DrawLockCombo($"##ClientLock-{layerIdx}", width, layerIdx, string.Empty, tooltip, true);

    public void DrawUnlockCombo(float width, int layerIdx, string tooltip)
        => DrawUnlockCombo($"##ClientUnlock-{layerIdx}", width, layerIdx, string.Empty, tooltip);

    protected override async Task<bool> OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanLock())
            return false;

        // validate the lock, if it is not valid, we will display an error and reset inputs.
        if (!ValidateLock(layerIdx))
            return false;

        var time = SelectedLock is Padlocks.FiveMinutes ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();
        var newData = Items[layerIdx] with { Padlock = SelectedLock, Password = Password, Timer = time, PadlockAssigner = MainHub.UID };
        if (await _dds.PushNewActiveRestriction(layerIdx, newData, DataUpdateType.Locked) is null)
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
        else
        {
            Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on self.", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return false;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (!Items[layerIdx].CanUnlock())
            return false;

        if (!ValidateUnlock(layerIdx))
            return false;

        var newData = Items[layerIdx] with { Padlock = Items[layerIdx].Padlock, Password = Items[layerIdx].Password, PadlockAssigner = MainHub.UID };
        if (await SelfBondageHelper.RestrictionUpdateRetTask(layerIdx, newData, DataUpdateType.Unlocked, _dds, _visuals))
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
        bool valid = Items[layerIdx].Padlock switch
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
        switch (Items[layerIdx].Padlock)
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
