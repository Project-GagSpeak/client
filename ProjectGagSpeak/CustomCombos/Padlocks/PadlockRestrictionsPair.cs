using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;

namespace GagSpeak.CustomCombos.Padlock;

public class PairRestrictionPadlockCombo : CkPadlockComboBase<ActiveRestriction>
{
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairRestrictionPadlockCombo(ILogger log, MainHub hub, Kinkster k)
        : base(() => [..k.LastRestrictionsData.Restrictions], () => [..PadlockEx.GetLocksForPair(k.PairPerms)], log)
    {
        _mainHub = hub;
        _ref = k;
    }

    protected override string ItemName(ActiveRestriction item)
        => _ref.LastLightStorage.Restrictions.FirstOrDefault(r => r.Id == item.Identifier) is { } restriction
            ? restriction.Label : "None";
    protected override bool DisableCondition(int layerIdx)
        => !_ref.PairPerms.ApplyRestrictions || SelectedLock == Items[layerIdx].Padlock || !Items[layerIdx].CanLock();

    protected override async Task<bool> OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanLock() || !_ref.PairPerms.LockRestrictions)
            return false;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        var dto = new PushKinksterRestrictionUpdate(_ref.UserData, DataUpdateType.Locked)
        {
            Layer = layerIdx,
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterRestrictionState(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockRestriction with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, SelectedLock, false);
            return false;
        }
        else
        {
            Log.LogDebug($"Locking Restriction with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        if (!Items[0].CanUnlock() || !_ref.PairPerms.UnlockRestrictions)
            return false;

        var dto = new PushKinksterRestrictionUpdate(_ref.UserData, DataUpdateType.Unlocked)
        {
            Layer = layerIdx,
            Padlock = Items[layerIdx].Padlock,
            Password = Password,
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterRestrictionState(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockRestriction with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{LoggerType.StickyUI}");
            DisplayToastErrorAndReset(result.ErrorCode, Items[layerIdx].Padlock, true);
            return false;
        }
        else
        {
            Log.LogDebug($"Unlocking Restriction with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            return true;
        }
    }

    private bool DisplayToastErrorAndReset(GagSpeakApiEc errorCode, Padlocks padlock, bool unlocking)
    {
        // Determine if we have access to unlock.
        switch (errorCode)
        {
            case GagSpeakApiEc.BadUpdateKind:
                Svc.Toasts.ShowError("Invalid Update Kind. Please try again.");
                break;

            case GagSpeakApiEc.InvalidLayer:
                Svc.Toasts.ShowError("Attempted to apply to a layer that was invalid.");
                break;

            case GagSpeakApiEc.InvalidPassword when unlocking:
                Svc.Toasts.ShowError("Incorrectly guessed this padlocks password!");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock is Padlocks.CombinationPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case GagSpeakApiEc.InvalidPassword when padlock is Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case GagSpeakApiEc.InvalidTime when padlock is Padlocks.TimerPadlock or Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Invalid Timer Syntax. Must be a valid time format (Ex: 0h2m7s).");
                break;

            case GagSpeakApiEc.LackingPermissions:
                Svc.Toasts.ShowError("You do not have permission to perform this action.");
                break;

            case GagSpeakApiEc.ItemIsLocked:
                Svc.Toasts.ShowError("This item is already locked.");
                break;

            case GagSpeakApiEc.ItemNotLocked:
                Svc.Toasts.ShowError("Cannot unlock item, as it is not yet locked.");
                break;

            case GagSpeakApiEc.NoActiveItem:
                Svc.Toasts.ShowError("No active item is present.");
                break;

            case GagSpeakApiEc.NotItemAssigner:
                Svc.Toasts.ShowError("This padlock can only be removed by its assigner.");
                break;

            default:
                Svc.Logger.Warning($"UNK Padlock Error: {errorCode}.");
                break;
        }
        ResetSelection();
        ResetInputs();
        return false;
    }
}
