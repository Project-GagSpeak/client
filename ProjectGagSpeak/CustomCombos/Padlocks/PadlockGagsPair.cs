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
    private readonly MainHub _mainHub;
    private Kinkster _ref;
    public PairGagPadlockCombo(ILogger log, MainHub hub, Kinkster k)
        : base(() => [ .. k.LastGagData.GagSlots ], () => [ ..PadlockEx.GetLocksForPair(k.PairPerms) ], log)
    {
        _mainHub = hub;
        _ref = k;
    }

    protected override string ItemName(ActiveGagSlot item)
        => item.GagItem.GagName();
    
    protected override bool DisableCondition(int layerIdx)
        => Items[layerIdx].GagItem is GagType.None;

    protected override async Task<bool> OnLockButtonPress(int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanLock() || !_ref.PairPerms.LockGags)
            return false;

        // we know it was valid, so begin assigning the new data to send off.
        var finalTime = SelectedLock == Padlocks.FiveMinutesPadlock
            ? DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(5)) : Timer.GetEndTimeUTC();

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
            DisplayToastErrorAndReset(result.ErrorCode);
            return false;
        }
        else
        {
            Log.LogDebug($"Locking Gag with {SelectedLock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return true;
        }
    }

    protected override async Task<bool> OnUnlockButtonPress(int layerIdx)
    {
        // return if we cannot lock.
        if (!Items[layerIdx].CanUnlock() || !_ref.PairPerms.UnlockGags)
            return false;

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
            DisplayToastErrorAndReset(result.ErrorCode);
            return false;
        }
        else
        {
            Log.LogDebug($"Unlocking Gag with {Items[layerIdx].Padlock.ToName()} on {_ref.GetNickAliasOrUid()}", LoggerType.StickyUI);
            ResetSelection();
            ResetInputs();
            return true;
        }
    }

    private bool DisplayToastErrorAndReset(GagSpeakApiEc errorCode)
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

            case GagSpeakApiEc.InvalidPassword when SelectedLock is Padlocks.CombinationPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4 digits (0-9).");
                break;

            case GagSpeakApiEc.InvalidPassword when SelectedLock is Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock:
                Svc.Toasts.ShowError("Invalid Syntax. Must be 4-20 characters.");
                break;

            case GagSpeakApiEc.InvalidTime when SelectedLock is Padlocks.TimerPadlock or Padlocks.TimerPasswordPadlock:
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
                Svc.Toasts.ShowError("Cannot remove this item as you are did not apply it.");
                break;

            case GagSpeakApiEc.NotItemAssigner:
                Svc.Toasts.ShowError("Cannot remove lock, it can only be removed by it's assigner.");
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
