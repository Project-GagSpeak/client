using GagSpeak.Kinksters;
using GagSpeak.WebAPI;
using GagspeakAPI.Data;
using GagspeakAPI.Extensions;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakAPI.Util;

namespace GagSpeak.CustomCombos.Padlock;

public class PairGagPadlockCombo : CkPadlockComboBase<ActiveGagSlot>
{
    private Action PostButtonPress;
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairGagPadlockCombo(ILogger log, MainHub hub, Kinkster k, Action postButtonPress)
        : base(() => [ .. k.LastGagData.GagSlots ], () => [ ..PadlockEx.GetLocksForPair(k.PairPerms) ], log)
    {
        _mainHub = hub;
        _ref = k;
        PostButtonPress = postButtonPress;
    }

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();
    
    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    protected override async Task<bool> OnLockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanLock() || !_ref.PairPerms.LockGags)
            return false;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

        Log.LogInformation($"Locking with a final time of {finalTime} for {SelectedLock.ToName()} which is a timespan of {finalTime - DateTimeOffset.UtcNow} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);

        var newData = new PushKinksterGagSlotUpdate(_ref.UserData, DataUpdateType.Locked)
        {
            Layer = layerIdx,
            Padlock = SelectedLock,
            Password = Password,
            Timer = finalTime,
            PadlockAssigner = MainHub.UID,
        };
        var result = await _mainHub.UserChangeKinksterGagState(newData);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform LockGag with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
            DisplayToastErrorAndReset(result.ErrorCode, SelectedLock, false);
            return false;
        }
        else
        {
            Log.LogDebug($"Locking Gag with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            PostButtonPress?.Invoke();
            return true;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(string label, int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanUnlock() || !_ref.PairPerms.UnlockGags)
        {
            Log.LogDebug($"Cannot unlock Gag with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason: Cannot Unlock", LoggerType.StickyUI);
            return false;
        }

        var dto = new PushKinksterGagSlotUpdate(_ref.UserData, DataUpdateType.Unlocked)
        {
            Layer = layerIdx,
            Padlock = Items[layerIdx].Padlock,
            Password = Password, // Our guessed password.
            PadlockAssigner = MainHub.UID,
        };

        var result = await _mainHub.UserChangeKinksterGagState(dto);
        if (result.ErrorCode is not GagSpeakApiEc.Success)
        {
            Log.LogDebug($"Failed to perform UnlockGag with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}, Reason:{result}", LoggerType.StickyUI);
            DisplayToastErrorAndReset(result.ErrorCode, Items[layerIdx].Padlock, true);
            return false;
        }
        else
        {
            Log.LogDebug($"Unlocking Gag with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            RefreshStorage(label);
            SelectedLock = Padlocks.None;
            PostButtonPress?.Invoke();
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

            case GagSpeakApiEc.InvalidTime when padlock is Padlocks.TimerPadlock or Padlocks.TimerPasswordPadlock or Padlocks.OwnerTimerPadlock or Padlocks.DevotionalTimerPadlock:
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
