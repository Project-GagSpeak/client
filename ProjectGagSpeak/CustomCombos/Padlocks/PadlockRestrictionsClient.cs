using GagSpeak.Kinksters;
using GagSpeak.Services;
using GagSpeak.Services.Mediator;
using GagSpeak.State.Managers;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlock;

public class PadlockRestrictionsClient : CkPadlockComboBase<ActiveRestriction>
{
    private readonly DataDistributionService _dds;
    private readonly RestrictionManager _manager;
    public PadlockRestrictionsClient(ILogger log, DataDistributionService dds, RestrictionManager manager)
        : base(() => manager.ServerRestrictionData?.Restrictions ?? [], PadlockEx.ClientLocks, log)
    {
        _dds = dds;
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

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        var newData = new ActiveRestriction()
        {
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID
        };

        if (await _dds.PushUpdatedRestrictionData(layerIdx, newData, DataUpdateType.Locked) is { } res && res is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on self. Reason:{res}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return false;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        // make a general common sense assumption logic check here, the rest can be handled across the server.
        if (!Items[layerIdx].CanUnlock())
            return false;

        if (!ValidateUnlock(layerIdx))
            return false;

        var newData = new ActiveRestriction()
        {
            Padlock = Items[layerIdx].Padlock,
            Password = Items[layerIdx].Password,
            PadlockAssigner = MainHub.UID
        };

        if (await _dds.PushUpdatedRestrictionData(layerIdx, newData, DataUpdateType.Unlocked) is { } res && res is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockRestriction with {Items[layerIdx].Padlock.ToName()} on self. Reason:{res}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return false;
        }
        else
        {
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            SelectedLock = Padlocks.None;
            return true;
        }
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
        switch (Items[layerIdx].Padlock)
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
